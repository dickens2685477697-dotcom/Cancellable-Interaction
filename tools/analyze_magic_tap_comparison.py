"""Compare separate normal-selection and slow-hover Study3 sessions without changing logs."""
from __future__ import annotations

import argparse
import csv
import json
import os
import sys
from collections import Counter
from pathlib import Path

import numpy as np


def read_csv(path: Path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream)
        rows = list(reader)
    if any(None in r or any(v is None for v in r.values()) for r in rows):
        raise ValueError(f"Column mismatch: {path}")
    return rows


def read_trials(path: Path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.reader(stream)
        header, rows = next(reader), list(reader)
    # Fitts16 writes 16 coordinate triplets but its legacy header names only 14.
    # The first 26 metadata columns and final 4 trigger fields have stable positions.
    for r in rows:
        if len(r) != 81 or len(header) != 75:
            raise ValueError(f"Unexpected Fitts16 layout: {path}: {len(header)}, {len(r)}")
    return [dict(zip(header[:26], r[:26])) | dict(zip(header[-4:], r[-4:]))
            | {"_row": i + 2} for i, r in enumerate(rows)]


def key_time(t):
    return round(float(t), 6)


def position(row):
    return np.array([row[k] for k in ("indexLocX", "indexLocY", "indexLocZ")], dtype=np.float64)


def replay_magic(rows, dt, case, target):
    previous_position = None
    previous_velocity = None
    speed = acceleration = None
    accepted = False
    skipped = 0
    boundary_corrections = 0
    for row in rows:
        p = position(row)
        accepted = False
        if previous_position is None:
            previous_position = p
            continue
        displacement = p - previous_position
        distance = float(np.linalg.norm(displacement))
        accept = distance >= .001
        # Seven-digit position strings can straddle the exact 1 mm cutoff.
        # When available, the logged pre-switch tracker state resolves that branch.
        if (abs(distance - .001) < .000001 and speed is not None
                and int(row['Case']) == case and int(row['index']) == target
                and float(row['V_vec']) > 0):
            observed = float(row['V_vec'])
            if abs(observed - speed) < .00005 and abs(observed - distance / dt) > .00005:
                if accept: boundary_corrections += 1
                accept = False
        if not accept:
            skipped += 1
            continue
        velocity = displacement / dt
        speed = float(np.linalg.norm(velocity))
        acceleration = (float(np.linalg.norm((velocity - previous_velocity) / dt))
                        if previous_velocity is not None else None)
        previous_position, previous_velocity = p, velocity
        accepted = True
    return speed, acceleration, accepted, skipped, boundary_corrections


def stats(values):
    a = np.asarray(values, dtype=float)
    return dict(n=len(a), mean=float(a.mean()), median=float(np.median(a)),
                sd=float(a.std(ddof=1)), q1=float(np.quantile(a, .25)),
                q3=float(np.quantile(a, .75)), p90=float(np.quantile(a, .9)),
                p95=float(np.quantile(a, .95)), minimum=float(a.min()), maximum=float(a.max()))


def ensure_legacy_session(settings):
    if any(row.get("motionRulesVersion", "").startswith("shared-") for row in settings):
        raise ValueError("This analysis replays the legacy 1 mm / fixed-step implementation. "
                         "New shared motion sessions must use their common-motion event snapshots; "
                         "do not mix them with the October 25 historical comparison.")


def analyze(root, output):
    details = []
    quality = {}
    for folder in sorted(p for p in root.iterdir() if p.is_dir()):
        mode = "Magic Tap 2.0" if "Magic" in folder.name else "动态阈值"
        context = "慢速逗留" if "逗留" in folder.name else "正常选择"
        time_path = next(folder.glob("*-time.csv"))
        settings_path = next(folder.glob("*-session-settings.csv"))
        trajectory_path = next(folder.glob("*-trajectory.csv"))
        trials = read_trials(time_path)
        settings = read_csv(settings_path)
        ensure_legacy_session(settings)
        ends = [r for r in settings if r["event"] == "trial_end"]
        trajectory = read_csv(trajectory_path)
        by_motion_time = {key_time(r["motionFixedTime"]): r for r in trajectory}
        by_fixed_time = {key_time(r["fixedTime"]): r for r in trajectory}
        if len(by_motion_time) != len(trajectory) or len(ends) != len(trials):
            raise ValueError(f"Non-unique sampling / trial boundaries: {folder}")
        events = read_csv(next(folder.glob("*-ixdl-trigger.csv"))) if mode == "动态阈值" else []
        live_events = [e for e in events if e["source"] == "target" and e["experimentStarted"] == "True"]
        events_by_case = {int(e["case"]): e for e in live_events}
        if mode == "动态阈值" and len(events_by_case) != 48:
            raise ValueError("Expected 48 unique experimental target events")
        modes = {s["mode"] for s in settings}
        if len(modes) != 1:
            raise ValueError("Session mode changed")
        differences, reconstruction_failures = [], []
        for trial, end in zip(trials, ends):
            case = int(trial["Case"])
            if case != int(end["case"]) or trial["ID"] != end["id"]:
                raise ValueError("Trial and settings event do not align")
            t = float(end["fixedTime"])
            dt = float(end["fixedDeltaTime"])
            snapshot = by_motion_time[key_time(t)]
            if int(snapshot["index"]) != int(trial["ballIndex"]):
                raise ValueError("Trigger sample position belongs to another target")
            row_before = by_motion_time.get(key_time(t - dt))
            previous_fixed = by_fixed_time.get(key_time(t - dt))
            prior_boundaries = [s for s in settings if s["event"] in ("experiment_start", "trial_end")
                                and float(s["fixedTime"]) < t - 1e-6]
            start = max(float(s["fixedTime"]) for s in prior_boundaries)
            samples = [r for r in trajectory if start + 1e-6 < float(r["motionFixedTime"]) <= t + 1e-6]
            raw_speed = float(snapshot["ori_v"])
            if mode == "动态阈值":
                event = events_by_case[case]
                if key_time(event["fixedTime"]) != key_time(t) or event["ballIndex"] != trial["ballIndex"]:
                    raise ValueError("Dynamic event time / target mismatch")
                decision_speed = float(event["triggerSpeed"])
                acceleration = float(event["triggerAcceleration"])
                threshold = float(event["speedThreshold"])
                previous_speed = float(event["previousSpeed"])
                differences.append(abs(decision_speed - raw_speed))
                if abs(decision_speed - float(trial["triggerSpeed"])) > 1e-7:
                    raise ValueError("Tail triggerSpeed extraction failed")
                speed_source = "exact ixdl-trigger event"
                skipped = 0
                corrections = 0
            else:
                decision_speed, acceleration, accepted, skipped, corrections = replay_magic(samples, dt, case, int(trial["ballIndex"]))
                threshold = float(end["magicTap2SpeedThreshold"])
                previous_speed = None
                if (not accepted or acceleration is None or decision_speed >= threshold + 1e-4
                        or acceleration >= float(end["magicTap2AccelerationThreshold"]) + 1e-3):
                    reconstruction_failures.append(case)
                speed_source = "replayed from aligned tip positions (rounded CSV coordinates)"
            details.append(dict(
                mode=mode, context=context, case=case, id=int(trial["ID"]),
                target=int(trial["ballIndex"]), trigger_time=t, frame=int(end["frame"]),
                trigger_latency_ms=float(trial["triggerTime"]) * 1000,
                selection_total_ms=float(trial["usedTime"]) * 1000,
                decision_speed_mps=decision_speed, raw_trigger_speed_mps=raw_speed,
                trigger_acceleration=acceleration, speed_threshold_mps=threshold,
                previous_speed_mps=previous_speed,
                entries=int(trial["inTargetTimes"]), cancellations=int(trial["cancelTimes"]),
                same_render_frame_as_previous_step=bool(previous_fixed and int(previous_fixed["frame"]) == int(end["frame"])),
                identical_position_to_previous_step=bool(row_before and np.linalg.norm(position(snapshot)-position(row_before)) < 1e-10),
                sample_log_delay_ms=(float(snapshot["fixedTime"]) - t) * 1000,
                speed_source=speed_source, replay_skipped_samples=skipped,
                replay_boundary_corrections=corrections,
                time_file=str(time_path), time_file_row=trial["_row"],
                trajectory_file=str(trajectory_path), settings_file=str(settings_path)))
        group = [r for r in details if r["mode"] == mode and r["context"] == context]
        quality[folder.name] = dict(
            trial_count=len(trials), trial_ends=len(ends), dynamic_events=len(live_events),
            warmup_events_excluded=len(events)-len(live_events),
            raw_speed_event_max_abs_error=max(differences) if differences else None,
            magic_replay_gate_failures=reconstruction_failures,
            all_tip_and_distal_tracked=all(r["tipPoseValid"] == r["distalPoseValid"] == "True" for r in trajectory),
            requested_hz=sorted({r["requestedSamplingHz"] for r in trajectory}),
            actual_fixed_hz=sorted({r["fixedSamplingHz"] for r in trajectory}),
            settings_changes=sum(s["event"] == "settings_changed" for s in settings),
            source_files=[str(time_path), str(settings_path), str(trajectory_path)]
                         + [str(next(folder.glob("*-ixdl-trigger.csv")))] if events else
                         [str(time_path), str(settings_path), str(trajectory_path)])
    summaries = []
    for context in ("正常选择", "慢速逗留"):
        for mode in ("Magic Tap 2.0", "动态阈值"):
            g = [r for r in details if r["mode"] == mode and r["context"] == context]
            summaries.append(dict(mode=mode, context=context, n=len(g),
                speed=stats([r["decision_speed_mps"] for r in g]),
                raw_speed=stats([r["raw_trigger_speed_mps"] for r in g]),
                latency_ms=stats([r["trigger_latency_ms"] for r in g]),
                total_ms=stats([r["selection_total_ms"] for r in g]),
                threshold=stats([r["speed_threshold_mps"] for r in g]),
                zero_speed_triggers=sum(r["decision_speed_mps"] < 1e-10 for r in g),
                zero_speed_same_frame=sum(r["decision_speed_mps"] < 1e-10 and r["same_render_frame_as_previous_step"] for r in g),
                same_render_frame_triggers=sum(r["same_render_frame_as_previous_step"] for r in g),
                trigger_within_100ms=sum(r["trigger_latency_ms"] <= 100.01 for r in g),
                trigger_within_200ms=sum(r["trigger_latency_ms"] <= 200.01 for r in g),
                trigger_within_500ms=sum(r["trigger_latency_ms"] <= 500.01 for r in g),
                trigger_within_1000ms=sum(r["trigger_latency_ms"] <= 1000.01 for r in g),
                reentry_trials=sum(r["entries"] > 1 for r in g),
                cancelled_trials=sum(r["cancellations"] > 0 for r in g),
                cancellations=sum(r["cancellations"] for r in g),
                by_id={i:{"latency_ms":stats([r["trigger_latency_ms"] for r in g if r["id"] == i]),
                          "speed":stats([r["decision_speed_mps"] for r in g if r["id"] == i]),
                          "total_ms":stats([r["selection_total_ms"] for r in g if r["id"] == i])}
                       for i in (1, 2, 3)}))
    result=dict(root=str(root), summaries=summaries, quality=quality, details=details)
    output.mkdir(parents=True, exist_ok=True)
    (output / "comparison.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    with (output / "trial_details.csv").open("w",encoding="utf-8-sig",newline="") as f:
        writer=csv.DictWriter(f,fieldnames=list(details[0]));writer.writeheader();writer.writerows(details)
    write_report_and_plot(result, output)
    for s in summaries:
        print(f"{s['context']} / {s['mode']}: n={s['n']}; "
              f"speed mean={s['speed']['mean']:.6f} median={s['speed']['median']:.6f}; "
              f"latency mean={s['latency_ms']['mean']:.2f} median={s['latency_ms']['median']:.2f} "
              f"p95={s['latency_ms']['p95']:.2f} ms; zero={s['zero_speed_triggers']}")
    print('Replay failures:', {k:v['magic_replay_gate_failures'] for k,v in quality.items()})
    print('Outputs:', output.resolve())
    return result


def write_report_and_plot(result, output):
    lookup = {(s['context'], s['mode']): s for s in result['summaries']}
    summary_rows = "\n".join(f"| {s['context']} | {s['mode']} | {s['speed']['mean']:.4f} | {s['speed']['median']:.4f} | {s['latency_ms']['mean']:.0f} | {s['latency_ms']['median']:.0f} | {s['latency_ms']['p95']:.0f} |" for s in result['summaries'])
    zero_rows, by_id_rows = [], []
    for context in ('正常选择', '慢速逗留'):
        zero = [r for r in result['details'] if r['mode'] == '动态阈值' and r['context'] == context and r['decision_speed_mps'] == 0]
        same_frame = sum(r['same_render_frame_as_previous_step'] for r in zero)
        previous_mean = sum(r['previous_speed_mps'] for r in zero) / len(zero) if zero else float('nan')
        zero_rows.append(f"| {context} | {len(zero)}/48（{len(zero)/48:.1%}） | {same_frame}/{len(zero)} | {previous_mean:.3f} |")
        for i in (1, 2, 3):
            m = lookup[(context, 'Magic Tap 2.0')]['by_id'][i]['latency_ms']
            d = lookup[(context, '动态阈值')]['by_id'][i]['latency_ms']
            by_id_rows.append(f"| {context} | {i} | {m['mean']:.0f} | {d['mean']:.0f} | {m['median']:.0f} | {d['median']:.0f} |")
    zero_rows, by_id_rows = "\n".join(zero_rows), "\n".join(by_id_rows)
    report = rf"""# 【旧实现数据】动态阈值与 Magic Tap 2.0：正常选择及球内画圈实验

> **版本状态：旧实现实验报告。** 本文192个试次采集于2026-10-25，保留原图表和统计用于诊断，不是共同规则版本的性能验证；于2026-10-26标记为历史版本。

> **标记原因：** 旧版Magic跳过不足1 mm位移，动态模式接受零位移且使用有符号加速度；两种模式的采样处理、加速度定义和目标内判定不同，结果不能单独归因于动态阈值。新版已统一这些处理，仍需Quest Pro重新采集。参见[共同规则修订与复测说明](https://kvxm3gyd38.feishu.cn/docx/Krvid94LAoLCVjxupfDc5TYkn8f)。

实验日期：2026-10-25。分析对象：4 个分别采集的会话，共 192 个完成试次。

## 1. 实验目的与方法

### 1.1 要回答的问题

本实验比较 Magic Tap 2.0 与动态阈值选择方式在两种情境下的触发行为：正常选择时能否及时完成，以及手指在目标球内持续画圈时，系统会多快触发。后者沿用“慢速逗留”情境名称，具体动作是球内画圈直到触发。重点指标为触发判定速度、入球后的等待分布，以及低速触发记录是否对应可靠的运动状态。

### 1.2 实验如何进行

本次由实验者本人作为唯一参与者，使用 **Quest Pro** 完成，为单参与者自测。实验在 Study3 环形目标选择任务中分别运行 Magic Tap 2.0 和动态阈值模式（ixdlDynamic）。每种模式各采集正常选择、慢速逗留两个会话，形成“2 种模式 × 2 种情境”的四组数据。每个会话完成 ID1、ID2、ID3 三个目标条件，每个条件 16 个正式试次，共 48 个。ID 是任务条件编号，不是参与者编号。

任务采用 Fitts16 序列：选择不计入正式试次的开始球后，依程序指定的环上顺序选择目标，每个条件计 16 次正式完成。当前目标若未触发便退出，试次继续，直至重新进入并触发。四个会话分别采集，不能视为同一批试次逐条配对的算法回放。

| 情境 | 模式 | 会话起始标识 | 正式试次 |
|---|---|---|---:|
| 正常选择 | 动态阈值 | 20261025-15-34-00 | 48 |
| 正常选择 | Magic Tap 2.0 | 20261025-16-27-21 | 48 |
| 慢速逗留 | Magic Tap 2.0 | 20261025-16-48-19 | 48 |
| 慢速逗留 | 动态阈值 | 20261025-16-50-42 | 48 |

正常选择以完成目标选择为目的。“慢速逗留”的操作要求是：将手指保持在目标球内持续画圈，直到系统触发；系统触发即结束本次画圈过程。因此该情境考察持续运动中的触发，并非静止保持，也不是预先等待统一时长后再主动选择。画圈半径、速率和圈数未提供定量要求，本报告不假定它们在两模式间相同。上述参与者、设备与画圈操作由实验者于 2026-10-26 补充确认。日志没有独立标记画圈开始及中途动作变化；每组 48 次均为同一参与者的重复测量，不能视为 48 名独立参与者。

四组运动判定均为 **50 Hz**，固定步长 **20 ms**，timeScale=1；雷球、随机障碍和预览均关闭。以下参数取自本次实际配置与程序逻辑。

### 1.3 两种方式的工作原理

**Magic Tap 2.0：** 由食指尖运动计算速度和加速度。在目标区域内，同一次接受的运动采样同时满足速度 < **0.1 m/s**、速度向量变化的加速度模长 < **11.661 m/s²**，并已有有效运动历史时触发。位移不足 1 mm 的采样被跳过，不推进历史位置和速度。目标内判定为食指尖或远端关节至少一个位于碰撞体世界轴对齐包围盒内。

**动态阈值：** 由相邻固定步的食指尖位置计算速度，依据前一次速度调整本次门限。映射为 `速度门限 = 0.02 + 0.01 × clamp(前次速度 / 0.6, 0, 1)`：前次速度从 0 增至 0.6 m/s 时，门限从 0.02 增至 0.03 m/s，超出范围后截断。本次速度低于门限，且有符号速率变化的加速度 < **0.375 m/s²**，并满足目标内触发条件时记录选择。减速对应负加速度，也能满足“小于 0.375”。球形碰撞体按食指尖是否在球体内判断，不采用 Magic 的指尖/远端关节包围盒规则。

本次比较的是两种完整实现。门限范围、加速度定义、位移过滤和目标内判定均不同，结果不能只归因于门限是否动态变化。

### 1.4 术语与指标

| 名词 | 定义及解释 |
|---|---|
| 试次（trial、Case） | 一次最终完成的目标选择，可包含退出和重进 |
| 触发判定速度 | 算法触发时用于比较门限的速度；动态有事件快照，Magic 由轨迹和过滤规则重建 |
| 触发等待（triggerTime） | 最后一次进入目标至触发，包含20 ms量化；画圈条件下是最后一段连续入球的等待，不是独立标记的画圈开始时间；不含此前退出和重进成本 |
| 整次完成时间（usedTime） | 从前一个目标完成到当前目标完成，含移动、退出和重进成本 |
| 均值、中位数、P95 | 均值受长等待影响；中位数表示一半试次不超过该值；P95 为约95%试次不超过的值，按线性插值计算 |
| 同渲染帧重复位姿零速事件 | 两个连续固定步处于同一渲染帧，记录位置相同，位移为零使算法算出速度零 |
| 采样与新姿态 | 固定判定回调、画面渲染、传感器姿态更新是不同过程；位姿有效不保证每步取得新姿态 |

Magic 的速度为重建估计，保留四位小数便于复核，不代表该精度的测量能力。两种速度计算历史不同，不能直接当作完全相同口径的真实手速。

### 1.5 数据处理

将试次日志与 session-settings 中的 trial_end 按 ID+Case 连接，再按运动固定时间对齐轨迹和动态快照。动态会话各有 3 个开始球事件，已排除。最终保留 192 个正式试次，四组各 48 个，均作描述性分析，不进行参与者总体的显著性推断。

## 2. 数据分析与结果

### 2.1 触发速度与等待时间分布

![图1：两种模式在正常选择和慢速逗留中的触发速度与等待分布](comparison.png)

图1左列为正常选择，右列为慢速逗留，即在目标球内持续画圈直到触发；上排为触发判定速度，下排为最后一次入球至触发的等待。每点代表一个试次，每组 48 次；箱体为第25–75百分位，中线为中位数，菱形为均值，须线按1.5倍四分位距绘制。橙色虚线为 Magic 的 0.1 m/s 门限，蓝色阴影为动态门限 0.02–0.03 m/s 范围；红圈标记同渲染帧重复位姿零速事件。下排左右纵轴范围不同，比较时应读取刻度。

| 情境 | 模式 | 判定速度均值 m/s | 判定速度中位数 m/s | 等待均值 ms | 等待中位数 ms | 等待P95 ms |
|---|---|---:|---:|---:|---:|---:|
{summary_rows}

### 2.2 正常选择：平均等待和分布集中程度

图1左下中，Magic 等待多数集中在约120–200 ms，动态阈值更分散，并有更长等待。精确均值分别为 **157.5 ms** 与 **185.8 ms**，动态增加 28.3 ms（18.0%）；中位数仅增加10 ms，但P95增加87 ms，标准差约113 ms对56 ms。这说明差异也体现在较长等待出现的程度。

200 ms 内触发比例，Magic 为 **41/48（85.4%）**，动态为 **27/48（56.3%）**。本会话中 Magic 的正常选择等待较短且更集中。整次完成时间均值为0.970 s对1.281 s；0.311 s差异还包含目标间移动与实际操作，不能全部归因于进入后的算法等待。

### 2.3 慢速逗留（球内画圈）：更长均值来自长等待尾部

图1右下中，两模式中位数接近，但动态出现更多很长等待，均值菱形明显高于中位线。动态均值为 **607.9 ms**，Magic 为 **451.3 ms**，增加156.7 ms（34.7%）；动态中位数为 **270 ms**，比 Magic 的 **280 ms** 略短。动态P95为 **2621 ms**，明显高于 Magic 的 **1465 ms**。

在持续画圈任务中，较长等待表示系统容许这段球内运动持续更久才触发。动态较长均值不能解释为所有试次都能维持更久：200 ms内触发比例为动态 **21/48（43.8%）**、Magic **19/48（39.6%）**；500 ms内分别为 **34/48（70.8%）** 和 **33/48（68.8%）**。动态中位数未增加，较早触发比例也未降低，因此本次没有显示动态比Magic更稳定地延后球内画圈触发。

画圈条件中，两模式均有25/48个完成试次出现多次进入；动态累计记录取消53次，Magic为36次，整次完成时间均值分别2.367 s和1.844 s。最后一次进入至触发不足以概括此前退出和重进成本。按照持续画圈的操作要求，这些触发是在运动任务中的选择事件，不应解读为参与者主动停手选择。每次以触发为终点，最终均触发是终止规则的一部分；上述200/500 ms比例仅表示已记录试次最后一段入球的短时触发比例，不能替代统一观察时窗内、包含全部未触发或中止尝试的误触率。

### 2.4 速度图中的零值代表什么

上排显示动态触发速度明显低于 Magic，慢速逗留大量集中在零。逐事件核对如下：

| 情境 | 动态零速触发 / 48 | 其中与前一固定步同渲染帧 | 前一步速度均值 m/s |
|---|---:|---:|---:|
{zero_rows}

60 个零速事件全部位于同一渲染帧内的连续 FixedUpdate，前一步速度明显非零，后一步记录位置却与前一步相同。因此零值首先是位置差计算结果，不能直接解释为手已平稳停止。画圈条件要求持续运动，其中44/48个事件呈现此模式，其0.0013 m/s均值和零中位数尤其不能作为动态更准确识别真实停手的证据；操作要求也不能代替对实际连续手部运动的测量。

### 2.5 按目标条件拆分

| 情境 | ID | Magic等待均值 ms | 动态等待均值 ms | Magic等待中位数 ms | 动态等待中位数 ms |
|---|---:|---:|---:|---:|---:|
{by_id_rows}

每格16个试次。正常选择三个ID的Magic均值都较低；慢速逗留中各ID均值和中位数关系不一致，ID1等待明显长于ID3。目标条件、操作过程和会话顺序均可能影响分布，不能将合并均值视为跨条件固定存在的算法延迟。

## 3. 结果的原因分析

### 3.1 重复位姿同时满足动态速度与加速度条件

动态直接使用连续固定步位置差。重复位置使当前速度为零，前一步速度为正，有符号加速度因此为负；零速低于正的动态门限，负加速度满足小于0.375 m/s²，两项门控可同时通过。这一计算路径与60个零速事件一致。

日志确认了相邻位置相同、回调处于同一渲染帧及由此计算的零速；缺少硬件姿态时间戳，仍不能断言每一事件的传感器内部状态。同渲染帧重复位置不应被当作独立的物理停手证据。Magic 跳过不足1 mm位移，因此不会按同一计算路径接受该零位移样本。

由此推论，动态极低的触发速度很大程度上反映了采样调用与姿态更新节奏、零位移处理的关系。单纯提高固定判定频率不能保证更多新姿态，反而可能增加重复读取机会。

### 3.2 参数和运动量定义差异影响等待

动态实际触发门限为正常选择0.02038–0.03000 m/s、逗留0.02052–0.03000 m/s，均值0.02228与0.02449 m/s，明显低于Magic的0.1 m/s。较低门限可能要求更深减速，从而增加一部分等待；重复位置又能直接给出零速，形成较早触发。这两个方向不同的机制与“较早事件仍多、同时长尾更长”相容，但观察数据无法分别估计贡献。

动态加速度是标量速率变化，可取负值；Magic 是速度向量差的模长，非负。在画圈中，即使速率近似不变，运动方向仍不断改变，Magic的向量变化量仍可增加，而动态的速率变化量可能接近零。因此这两种加速度门控对画圈动作的响应并不等价，是两模式差异的另一可能来源；未统一记录的画圈半径与速率使其贡献无法量化。入球范围还存在指尖球体与指尖/远端关节包围盒判定差异，也可能改变接触时刻、退出和重进统计。因此本实验无法隔离动态映射本身的效果。

Magic 过滤不足1 mm位移后不推进历史位置，后续接受样本会相对更早位置累计位移，但仍除以一个固定步长。正常选择的重建判定速度均值为0.0757 m/s，统一按原始相邻固定步计算则为0.0669 m/s；逗留两者均约0.0822 m/s。因此速度图适合说明算法在什么数值下触发，不适合直接比较完全同口径的真实瞬时手速。

### 3.3 排除零速事件仅是敏感性检查

删除同渲染帧零速事件后，动态正常选择剩32次，速度均值0.0145 m/s、等待均值223.7 ms；逗留仅剩4次，速度均值0.0151 m/s、等待均值155.0 ms。这说明原始汇总明显受零速事件组成影响。

该删除是对已发生事件的事后筛选。若修复算法，原来提前触发的试次可能继续运动并在其他时刻触发，不能把删除后的均值视为修复后的预测结果；逗留剩余4次也不足以稳定评价动态映射。

## 4. 推论与结论

1. **在本人使用Quest Pro的本次正常选择自测中，Magic Tap 2.0 的进入后等待较短、分布更集中。** 动态均值多28.3 ms，200 ms内触发比例更低，P95更大；这是单参与者重复试次的描述性结果，尚不能推广为参与者总体的固定优势。
2. **本次持续画圈结果未显示动态阈值比Magic更稳定地抵抗球内运动干扰。** 动态较长均值伴随长等待尾部，中位数未增加，200 ms和500 ms内仍有相近或更多触发；不能将较长均值视为普遍更强的防误触保护。由于任务持续到触发，且允许退出重进，本次等待分布也不能替代统一观察时窗中的误触率，更不能直接推广到静止逗留。
3. **动态的低记录速度不能作为可靠识别真实停手的证据。** 正常选择16/48、逗留44/48的零速事件与同渲染帧重复位置一致；结合速度与有符号加速度计算，推论重复位姿处理是当前触发行为的重要机制，应先解决姿态新鲜度和零位移判定问题。
4. **尚不能判断动态映射本身优于或劣于固定阈值。** 后续应统一门限范围、加速度定义、位移过滤、目标内判定和新姿态采样，仅改变是否动态映射；保存两模式精确快照，并固定画圈半径、速率范围和观察时长，区分持续画圈与主动选择，记录全部未触发和中止尝试并控制会话顺序，才能分别评价正常选择效率与球内运动期间的误触风险。

## 附录：数据来源与复核

原始数据位于 `D:\动态阈值对比数据` 下的 `MagicTap2.0正常选择`、`MagicTap2.0慢速逗留`、`动态阈值正常选择`、`动态阈值慢速逗留` 四个目录，对应上文会话标识。各目录使用 time.csv、session-settings.csv、trajectory.csv；动态另使用 ixdl-trigger.csv。附件保留 comparison.json、trial_details.csv、PNG和SVG。

192条试次与192条trial_end一一匹配。96个动态快照速度与按motionFixedTime对齐的原始速度误差小于5×10⁻⁹ m/s；轨迹打印晚一个固定步，已按运动时间处理。Magic的96个事件重放均满足双阈值；正常选择Case47的1 mm边界舍入歧义由已记录历史速度状态核对，但速度仍为重建估计。time.csv历史表头少6列，分析使用前26列元数据并从行尾提取4个触发字段，未按错位表头取值，原始文件未修改。
"""
    (output/'分析报告.md').write_text(report, encoding='utf-8')

    local_plot_dependencies=Path(__file__).resolve().parents[1]/'Temp'/'MagicTapAnalysisDependencies'
    if local_plot_dependencies.exists():
        sys.path.insert(0,str(local_plot_dependencies))
    os.environ.setdefault('MPLCONFIGDIR',str(Path(__file__).resolve().parents[1]/'Temp'/'MagicTapMatplotlibCache'))
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    from matplotlib import font_manager
    font=Path('C:/Windows/Fonts/msyh.ttc')
    if font.exists():
        font_manager.fontManager.addfont(str(font))
        plt.rcParams['font.family']=font_manager.FontProperties(fname=str(font)).get_name()
    plt.rcParams['axes.unicode_minus']=False
    plt.rcParams['font.size']=10
    fig, axes=plt.subplots(2,2,figsize=(12,8),layout='constrained')
    colors=['#B8793E','#2866A6']
    random=np.random.default_rng(42)
    for col,context in enumerate(('正常选择','慢速逗留')):
        for row,field in enumerate(('decision_speed_mps','trigger_latency_ms')):
            ax=axes[row,col]
            for x,mode in enumerate(('Magic Tap 2.0','动态阈值'),1):
                g=[r for r in result['details'] if r['mode']==mode and r['context']==context]
                values=np.array([r[field] for r in g])
                jitter=random.uniform(-.14,.14,len(g))
                bp=ax.boxplot([values],positions=[x],widths=.42,showfliers=False,patch_artist=True,
                              medianprops=dict(color='#222222',linewidth=1.6),
                              whiskerprops=dict(color=colors[x-1]),capprops=dict(color=colors[x-1]))
                bp['boxes'][0].set(facecolor=colors[x-1],alpha=.16,edgecolor=colors[x-1])
                ax.scatter(x+jitter,values,s=22,color=colors[x-1],alpha=.6,zorder=3,edgecolors='none')
                ax.scatter([x],[values.mean()],marker='D',s=40,color='white',edgecolors='#222222',zorder=5)
                if row==0 and mode=='动态阈值':
                    repeats=np.array([r['same_render_frame_as_previous_step'] for r in g])
                    ax.scatter(x+jitter[repeats],values[repeats],s=35,facecolors='none',edgecolors='#C43A3A',linewidths=1,zorder=4)
                    ax.text(.98,.93,f'零速重复位姿触发：{int(repeats.sum())}/48',transform=ax.transAxes,ha='right',va='top',fontsize=10)
            ax.set_xticks([1,2],['Magic Tap 2.0','动态阈值'])
            ax.spines[['top','right']].set_visible(False)
            ax.grid(axis='y',color='#D9DEE4',linewidth=.6,alpha=.65)
            ax.set_axisbelow(True)
            ax.set_xlim(.5,2.5)
            if row==0:
                ax.set_title(context+'：触发判定速度',fontsize=12,pad=10)
                ax.set_ylabel('速度 (m/s)')
                ax.set_ylim(-.004,.115)
                ax.hlines(.1,.67,1.33,color=colors[0],linestyle='--',linewidth=1)
                ax.fill_between([1.67,2.33],.02,.03,color=colors[1],alpha=.09)
            else:
                ax.set_title(context+'：最后一次入球至触发',fontsize=12,pad=10)
                ax.set_ylabel('时间 (ms)')
                ax.set_ylim(-20,650 if col==0 else 3350)
    fig.suptitle('动态阈值与 Magic Tap 2.0 的触发对比',fontsize=16)
    fig.supxlabel('每组48次。点=试次，箱=四分位，中线=中位数，菱形=均值。\n红圈=同渲染帧重复位姿；Magic速度为轨迹重建估计。两张时间图的纵轴范围不同。',fontsize=9)
    fig.savefig(output/'comparison.png',dpi=180,facecolor='white')
    fig.savefig(output/'comparison.svg',facecolor='white')
    plt.close(fig)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    parser=argparse.ArgumentParser()
    parser.add_argument("root",type=Path)
    parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args()
    analyze(args.root,args.output)

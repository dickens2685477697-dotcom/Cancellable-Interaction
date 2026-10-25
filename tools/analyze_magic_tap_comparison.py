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
    lookup={(s['context'],s['mode']):s for s in result['summaries']}
    lines=[
        '# 动态阈值与 Magic Tap 2.0：触发速度及触发时间',
        '',
        '分析对象：4个独立采集会话，每个会话48条完成试次（ID 1、2、3各16条），合计192条。动态模式为 `ixdlDynamic`。',
        '',
        '## 主要结果',
        '',
        '|情境|模式|判定速度均值 (m/s)|判定速度中位数 (m/s)|触发时间均值 (ms)|触发时间中位数 (ms)|触发时间P95 (ms)|',
        '|---|---|---:|---:|---:|---:|---:|',
    ]
    for s in result['summaries']:
        v,t=s['speed'],s['latency_ms']
        lines.append(f"|{s['context']}|{s['mode']}|{v['mean']:.4f}|{v['median']:.4f}|{t['mean']:.0f}|{t['median']:.0f}|{t['p95']:.0f}|")
    lines += [
        '',
        '**速度口径：** 动态阈值使用 `ixdl-trigger` 中触发前保存的精确判定速度；Magic Tap 2.0 未单独保存触发速度，按对齐的轨迹位置及其1毫米过滤规则重建，属于估计值，保留4位小数仅便于复核，不代表该精度的测量能力。',
        '',
        '**时间口径：** `time.csv` 的 `triggerTime`，从最后一次入球到触发，包含20 ms固定步量化；不等于从上一个目标到本次触发的整次用时，多次出入球时也不等于第一次入球后的总等待时间。P95按线性插值计算。',
        '',
        '## 正常选择',
        '',
        'Magic Tap 2.0的平均触发等待为157.5 ms，动态阈值为185.8 ms，动态阈值多28.3 ms（18.0%）。中位数只差10 ms，但动态阈值P95增加87 ms，标准差113 ms，约为Magic Tap 2.0的56 ms的2倍，说明等待更不稳定。',
        '',
        '200 ms内触发：Magic Tap 2.0为41/48（85.4%），动态阈值为27/48（56.3%）。整次选择平均用时分别为0.970 s、1.281 s；这0.311 s差异同时包含入球前的运动时间和行为差异，不能全部归因于触发算法。',
        '',
        '## 慢速逗留',
        '',
        '动态阈值的平均触发等待为607.9 ms，Magic Tap 2.0为451.3 ms，多156.7 ms（34.7%）。但中位数分别为270 ms和280 ms，动态阈值反而早10 ms。动态阈值的P95为2.621 s，Magic Tap 2.0为1.465 s，均值差主要来自较长的尾部。',
        '',
        '200 ms内触发：动态阈值21/48（43.8%），Magic Tap 2.0为19/48（39.6%）；500 ms内触发：动态阈值34/48（70.8%），Magic Tap 2.0为33/48（68.8%）。因此当前数据没有显示动态阈值能稳定推迟慢速逗留触发。',
        '',
        '慢速逗留中两种模式均有25/48条完成试次记录了多次入球；动态阈值累计取消53次，Magic Tap 2.0为36次。整次用时均值分别为2.367 s和1.844 s。只看最后一次入球的triggerTime会遗漏前面退出、再进入的成本。',
        '',
        '## 最重要的采样问题：动态阈值被重复位姿的零速触发',
        '',
        '|情境|动态阈值零速触发|其中与前一物理步同渲染帧|零速前一采样的速度均值 (m/s)|',
        '|---|---:|---:|---:|',
    ]
    for context in ('正常选择','慢速逗留'):
        g=[r for r in result['details'] if r['mode']=='动态阈值' and r['context']==context and r['decision_speed_mps']==0]
        lines.append(f"|{context}|{len(g)}/48 ({len(g)/48:.1%})|{sum(r['same_render_frame_as_previous_step'] for r in g)}/{len(g)}|{np.mean([r['previous_speed_mps'] for r in g]):.3f}|")
    lines += [
        '',
        '这60个零速事件全部发生于同一渲染帧中的连续FixedUpdate。前一步速度仍明显非零，而下一物理步没有新的手部位姿，位置差为0，动态模式便计算出速度0；其有符号加速度同时变为负值，满足加速度小于0.375的条件。',
        '',
        '因此这些事件的“速度很低”不能解释为手已经平稳停止。慢速逗留的44/48条（91.7%）尤其受此机制影响。Magic Tap 2.0的1毫米过滤会跳过这种零位移样本，当前两模式对重复位姿的处理并不相同。',
        '',
        '敏感性检查：去掉同渲染帧零速事件后，动态阈值正常选择剩32条，平均触发速度0.0145 m/s，平均触发等待223.7 ms；慢速逗留仅剩4条，平均速度0.0151 m/s，平均等待155.0 ms。慢速逗留的有效对照太少，不能据此稳健评价动态机制。这里仅删除已记录的事件，不代表修复算法后会得到相同的触发结果。',
        '',
        '## 两种模式的参数与计算口径并不等价',
        '',
        '- 本次Magic Tap 2.0速度阈值实际为0.1 m/s，加速度向量模长阈值为11.661 m/s²。',
        '- 动态阈值在触发事件中为0.02038–0.03000 m/s（正常）及0.02052–0.03000 m/s（逗留），均值分别0.02228与0.02449 m/s。记录的前次速度与阈值吻合当前线性映射：`0.02 + 0.01 × clamp(previousSpeed / 0.6, 0, 1)`。',
        '- 动态模式的加速度为有符号速率变化，阈值0.375 m/s²；Magic Tap 2.0使用速度向量变化的模长，并有1毫米位移过滤，且两模式入球区域计算也不同。因此这次结果不能单独归因于“动态阈值”这一因素。',
        '',
        '统一采用每个物理步的原始速率（同一口径的ori_v）时，Magic Tap 2.0正常/逗留的均值为0.0669/0.0822 m/s，动态阈值为0.0096/0.0013 m/s。Magic正常选择的重建判定速度均值0.0757 m/s高于原始单步速率，因为过滤后会累计相对上个接受位置的位移，但仍除以一个fixedDeltaTime。两种速度应明确区分。',
        '',
        '## 按ID拆分（各16条）',
        '',
        '|情境|ID|Magic平均触发时间 (ms)|动态平均触发时间 (ms)|Magic中位时间 (ms)|动态中位时间 (ms)|',
        '|---|---:|---:|---:|---:|---:|',
    ]
    for context in ('正常选择','慢速逗留'):
        for i in (1,2,3):
            m=lookup[(context,'Magic Tap 2.0')]['by_id'][i]['latency_ms']
            d=lookup[(context,'动态阈值')]['by_id'][i]['latency_ms']
            lines.append(f"|{context}|{i}|{m['mean']:.0f}|{d['mean']:.0f}|{m['median']:.0f}|{d['median']:.0f}|")
    lines += [
        '',
        'ID间和会话内差异较大。每个组合仅一个会话，未提供被试编号；48次试次不能当作48个独立被试，以上为描述性结果，未作被试层面的显著性推断。',
        '',
        '## 数据核对与适用边界',
        '',
        '- 192条time记录与192条trial_end按Case/ID一一匹配。动态事件额外的每会话3个开始球触发不属于正式试次，已剔除。',
        '- 96个动态事件的精确速度与按motionFixedTime对齐的ori_v最大误差小于5×10⁻⁹ m/s。轨迹打印比运动样本晚一个20 ms物理步，不能按Case切换后的V_vec直接取触发速度。',
        '- Magic的96个事件经重放均满足双阈值；正常选择Case 47在1毫米边界受坐标舍入影响，使用该试次已记录的历史V_vec状态消除了过滤分支歧义。Magic速度仍是轨迹重建估计，不等于新采集的原始事件值。',
        '- 四组均为50 Hz、timeScale=1；记录的食指尖和远端关节位姿标志均有效，但有效标志不保证每步都有新传感器样本。',
        '- 四组都关闭雷球、随机障碍和预览。文件夹标记了实验情境，但日志没有独立记录“开始逗留/结束逗留/主动选择”的意图时刻。',
        '- 只记录了最终完成的48次触发，缺少全部逗留尝试、未触发尝试及预定逗留时长的分母，不能由此计算逗留误触率。',
        '- time.csv的表头比Fitts16实际数据少6列。分析保留前26列元数据，并从行尾提取4个触发字段；不按错误表头位置提取triggerSpeed，原文件未改动。',
        '',
        '## 下一步判断',
        '',
        '本次最可信的结果是：Magic Tap 2.0正常选择更快、等待更集中；动态阈值虽在更低的记录速度下触发，却没有形成稳定的逗留等待保护。先避免同一份位姿被重复当作新运动样本，并给Magic增加精确的触发事件快照，再保持速度门限范围、加速度定义和采样规则一致做对照，才能判断动态映射本身的效果。',
        '',
        '![触发速度和时间分布](comparison.png)',
        '',
        '## 原始数据来源',
        '',
    ]
    for name,q in result['quality'].items():
        lines += [f'### {name}', '']
        lines += [f'- [{Path(p).name}](<{Path(p).as_posix()}>)' for p in q['source_files']]
        lines += ['']
    (output/'分析报告.md').write_text('\n'.join(lines),encoding='utf-8')

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

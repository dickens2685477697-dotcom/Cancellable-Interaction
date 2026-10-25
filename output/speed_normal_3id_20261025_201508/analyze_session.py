"""Read-only source analysis of the 20261025 20:15:08 Speed pilot session."""
from pathlib import Path
from collections import Counter, defaultdict
import csv
import hashlib
import json
import math
import os
import statistics as stats
import sys

SOURCE = Path(r"D:\2026.7.18PilotStudy\30hz_50hz_analysis\speed正常选择3ID")
OUTPUT = Path(__file__).resolve().parent
sys.path.insert(0, str(OUTPUT / ".plot_dependencies"))
os.environ["MPLCONFIGDIR"] = str(OUTPUT / ".matplotlib_cache")


def load(suffix):
    path = next(SOURCE.glob(f"*-{suffix}.csv"))
    with path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream)
        rows = list(reader)
    assert all(None not in r and all(v is not None for v in r.values()) for r in rows), path
    return path, rows


def save_csv(name, rows):
    with (OUTPUT / name).open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


source_hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in SOURCE.glob("*.csv")}
entry_path, entries = load("speed-entry-summary")
boundary_path, boundary = load("speed-boundary-debug")
settings_path, settings = load("session-settings")
time_path = next(SOURCE.glob("*-time.csv"))
with time_path.open(encoding="utf-8-sig", newline="") as stream:
    time_raw = list(csv.reader(stream))
# The original time header describes 15 target positions, while this session writes 17.
# The first 26 scalar columns remain aligned. Do not infer the trailing coordinate headers.
trials = [dict(zip(time_raw[0][:26], row[:26])) for row in time_raw[1:]]
assert len({(r["ID"], r["Case"]) for r in trials}) == len(trials)
formal = [r for r in entries if r["entryExperimentStarted"] == "1"
          and r["settingsChanged"] == "0" and r["mixedSamplingRate"] == "0"]
assert len({r["entrySequence"] for r in entries}) == len(entries)
assert {r["entrySequence"] for r in boundary if r["event"] == "enter"} == {r["entrySequence"] for r in entries}
assert all(r["mode"] == "speed" and r["source"] == "target" and r["isMineBall"] == "0" for r in formal)
assert all(r["trackingLossObserved"] == "0" and r["enableTrigger"] == "1" for r in formal)
by_entry = defaultdict(list)
for row in boundary:
    by_entry[row["entrySequence"]].append(row)

failed = [r for r in formal if r["outcome"] == "exit"]
candidates = [r for r in failed if r["boundaryHighSpeedCandidate"] == "1"]
failure_rows = []
for row in failed:
    inside = [r for r in by_entry[row["entrySequence"]] if r["event"] == "sample"]
    last = inside[-1]
    assert math.isclose(float(last["decisionSpeed"]), float(row["lastInsideSpeed"]), abs_tol=1e-10)
    gap = int(row["motionSampleSequence"]) - int(last["motionSampleSequence"])
    fixed_ms = 1000 * (float(row["motionFixedTime"]) - float(last["motionFixedTime"]))
    wall_ms = 1000 * (float(row["realtime"]) - float(last["realtime"]))
    displacement = math.dist([float(row["index" + a]) for a in "XYZ"],
                             [float(last["index" + a]) for a in "XYZ"])
    recomputed = displacement / float(row["motionDeltaTime"])
    assert gap == 1 and math.isclose(fixed_ms, 20, abs_tol=0.001)
    assert math.isclose(recomputed, float(row["exitFrameSpeed"]), abs_tol=1e-5)
    candidate_check = (row["armedEver"] == "1" and row["speedMaxObservedAfterArmed"] == "0"
                       and row["trackingLossObserved"] == "0" and row["tipPoseValid"] == "1"
                       and row["distalPoseValid"] == "1" and row["tipInside"] == "0"
                       and row["distalInside"] == "0"
                       and float(row["lastInsideSpeed"]) < float(row["maxSpeedThreshold"])
                       <= float(row["exitFrameSpeed"]))
    assert candidate_check == (row["boundaryHighSpeedCandidate"] == "1")
    category = ("boundary_high_speed_candidate" if candidate_check else
                "not_armed" if row["armedEver"] == "0" else "armed_but_exit_below_threshold")
    failure_rows.append({
        "id": int(row["id"]), "case": int(row["case"]), "entry_sequence": int(row["entrySequence"]),
        "category": category, "last_inside_speed_m_s": float(row["lastInsideSpeed"]),
        "exit_speed_m_s": float(row["exitFrameSpeed"]), "threshold_m_s": float(row["maxSpeedThreshold"]),
        "armed_ever": int(row["armedEver"]), "candidate": int(candidate_check),
        "motion_sequence_gap": gap, "fixed_gap_ms": fixed_ms, "wall_gap_ms": wall_ms,
        "render_frame_gap": int(row["frame"]) - int(last["frame"]),
        "displacement_mm": 1000 * displacement, "exit_speed_recomputed_m_s": recomputed,
        "inside_samples": int(row["insideSampleCount"]), "entry_elapsed_s": float(row["entryElapsed"]),
        "source_summary_row": entries.index(row) + 2,
    })
save_csv("failure_detail_verified.csv", failure_rows)
save_csv("boundary_candidates_verified.csv", [r for r in failure_rows if r["candidate"]])

summary = []
trial_detail = []
for trial in trials:
    relevant = [r for r in formal if r["id"] == trial["ID"] and r["case"] == trial["Case"]]
    assert len(relevant) == int(trial["inTargetTimes"])
    assert sum(r["outcome"] == "trigger" for r in relevant) == 1
    assert relevant[-1]["outcome"] == "trigger"
    trial_detail.append({
        "id": int(trial["ID"]), "case": int(trial["Case"]), "entries": len(relevant),
        "failed_entries": sum(r["outcome"] == "exit" for r in relevant),
        "boundary_candidates": sum(r["boundaryHighSpeedCandidate"] == "1" for r in relevant),
        "not_armed_entries": sum(r["outcome"] == "exit" and r["armedEver"] == "0" for r in relevant),
        "used_time_s": float(trial["usedTime"]),
    })
for id_value in (1, 2, 3):
    rows = [r for r in formal if int(r["id"]) == id_value]
    ts = [r for r in trial_detail if r["id"] == id_value]
    fs = [r for r in failure_rows if r["id"] == id_value]
    ntrigger = sum(r["outcome"] == "trigger" for r in rows)
    ncandidate = sum(r["candidate"] for r in fs)
    success = [r for r in rows if r["outcome"] == "trigger"]
    summary.append({
        "id": id_value, "fixed_hz": 50,
        "target_width_mm": round(stats.median(2000 * float(r["boundsExtentX"]) for r in rows), 3),
        "trials": len(ts), "entries": len(rows), "trigger_entries": ntrigger,
        "failed_entries": len(fs), "entry_trigger_rate": ntrigger / len(rows),
        "reentry_trials": sum(r["entries"] > 1 for r in ts),
        "trial_reentry_rate": sum(r["entries"] > 1 for r in ts) / len(ts),
        "boundary_candidates": ncandidate,
        "candidate_share_of_failed_entries": ncandidate / len(fs) if fs else None,
        "candidate_trials": sum(r["boundary_candidates"] > 0 for r in ts),
        "not_armed_failures": sum(r["category"] == "not_armed" for r in fs),
        "armed_exit_below_threshold_failures": sum(r["category"] == "armed_but_exit_below_threshold" for r in fs),
        "mean_trial_time_s": stats.mean(r["used_time_s"] for r in ts),
        "median_trial_time_s": stats.median(r["used_time_s"] for r in ts),
        "median_successful_entry_time_s": stats.median(float(r["entryElapsed"]) for r in success),
    })
save_csv("id_summary.csv", summary)
save_csv("trial_detail.csv", trial_detail)

intervals = []
for rows in by_entry.values():
    observations = [r for r in rows if r["event"] in ("sample", "exit") and r["entryExperimentStarted"] == "1"]
    for previous, current in zip(observations, observations[1:]):
        if int(current["motionSampleSequence"]) - int(previous["motionSampleSequence"]) == 1:
            intervals.append((1000 * (float(current["realtime"]) - float(previous["realtime"])),
                              int(current["frame"]) - int(previous["frame"])))
verified_candidates = [r for r in failure_rows if r["candidate"]]
quality = {
    "source_directory": str(SOURCE), "source_sha256": source_hashes,
    "all_entries": len(entries), "formal_entries": len(formal),
    "warmup_entries_excluded": len(entries) - len(formal),
    "warmup_candidates_excluded": sum(r["entryExperimentStarted"] == "0" and r["boundaryHighSpeedCandidate"] == "1" for r in entries),
    "formal_failure_categories": dict(Counter(r["category"] for r in failure_rows)),
    "all_enter_events_have_summary": True, "trial_entries_reconcile_with_time_csv": True,
    "candidate_flags_recomputed": True, "source_coordinates_reproduce_exit_speed": True,
    "time_header_columns": len(time_raw[0]), "time_body_column_counts": dict(Counter(len(r) for r in time_raw[1:])),
    "time_parse_policy": "Only the first 26 scalar columns were used; trailing target coordinate headers are misaligned.",
    "configured_hz": sorted({float(r["fixedSamplingHz"]) for r in settings}),
    "thresholds_m_s": sorted({(float(r["minSpeedThreshold"]), float(r["maxSpeedThreshold"])) for r in formal}),
    "tracking_loss_formal_entries": sum(r["trackingLossObserved"] == "1" for r in formal),
    "consecutive_observation_count": len(intervals),
    "consecutive_observation_wall_gap_ms_mean": stats.mean(r[0] for r in intervals),
    "consecutive_observation_wall_gap_ms_median": stats.median(r[0] for r in intervals),
    "consecutive_observation_render_frame_gaps": dict(Counter(r[1] for r in intervals)),
    "candidate_wall_gap_ms_range": [min(r["wall_gap_ms"] for r in verified_candidates), max(r["wall_gap_ms"] for r in verified_candidates)],
    "candidate_render_frame_gaps": sorted({r["render_frame_gap"] for r in verified_candidates}),
}
assert source_hashes == {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in SOURCE.glob("*.csv")}
(OUTPUT / "verification.json").write_text(json.dumps(quality, ensure_ascii=False, indent=2), encoding="utf-8")

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.font_manager import FontProperties
font = FontProperties(fname=r"C:\Windows\Fonts\msyh.ttc")
plt.rcParams.update({"font.family": font.get_name(), "axes.unicode_minus": False, "font.size": 10})
fig, axes = plt.subplots(1, 2, figsize=(13, 5.3), gridspec_kw={"width_ratios": [1, 1.35]})
colors = ["#277C73", "#DC8A28", "#A6AFB9", "#D7DDE3"]
labels = ["成功触发", "边界高速候选", "未完成低速准备", "已准备但退出仍未达高速"]
categories = [[r["trigger_entries"] for r in summary], [r["boundary_candidates"] for r in summary],
              [r["not_armed_failures"] for r in summary], [r["armed_exit_below_threshold_failures"] for r in summary]]
bottom = [0, 0, 0]
for label, color, values in zip(labels, colors, categories):
    axes[0].bar(range(3), values, bottom=bottom, label=label, color=color, width=0.6)
    for i, value in enumerate(values):
        if value:
            axes[0].text(i, bottom[i] + value / 2, str(value), ha="center", va="center", color="white" if color == colors[0] else "#162233")
        bottom[i] += value
axes[0].set_xticks(range(3), ["ID1 · 90 mm", "ID2 · 60 mm", "ID3 · 30 mm"])
axes[0].set_ylabel("正式进入次数")
axes[0].set_title("相同 50 Hz 条件下的进入结果", loc="left", pad=14)
axes[0].set_ylim(0, 42)
axes[0].legend(frameon=False, loc="upper left", fontsize=9)
axes[0].spines[["top", "right"]].set_visible(False)
for i, row in enumerate(summary):
    axes[0].text(i, bottom[i] + 1, f"触发率 {row['entry_trigger_rate']:.1%}", ha="center", fontsize=9)
ys = list(range(len(verified_candidates)))
for y, row in zip(ys, verified_candidates):
    axes[1].plot([row["last_inside_speed_m_s"], row["exit_speed_m_s"]], [y, y], color="#ABB4BF", linewidth=1.5, zorder=1)
axes[1].scatter([r["last_inside_speed_m_s"] for r in verified_candidates], ys, color="#277C73", label="最后球内速度", zorder=3, s=28)
axes[1].scatter([r["exit_speed_m_s"] for r in verified_candidates], ys, color="#DC8A28", label="首次球外速度", zorder=3, s=28)
axes[1].axvline(0.375, color="#A83F40", linestyle="--", linewidth=1.1, label="高速阈值 0.375")
axes[1].set_yticks(ys, [f"ID{r['id']} / Case{r['case']} / E{r['entry_sequence']}" for r in verified_candidates], fontsize=8)
axes[1].invert_yaxis()
axes[1].set_xlabel("判定速度（m/s）；每对为相邻运动采样")
axes[1].set_title("13 次正式边界高速候选", loc="left", pad=14)
axes[1].set_xlim(-0.12, 6)
axes[1].legend(frameon=False, loc="upper right", fontsize=8)
axes[1].spines[["top", "right"]].set_visible(False)
fig.suptitle("Speed 正常选择 · 3 个 ID · 2026-10-25 20:15:08", x=0.07, ha="left", fontsize=14)
fig.text(0.07, 0.025, "排除 6 次预热进入。边界候选表示退出采样才观察到高速，不能单独证明掉帧。", fontsize=9, color="#586273")
fig.tight_layout(rect=[0.025, 0.07, 1, 0.93], w_pad=3)
fig.savefig(OUTPUT / "speed_boundary_analysis.png", dpi=180, facecolor="white")
plt.close(fig)

table = "\n".join(f"| {r['id']} | {r['target_width_mm']:.0f} | {r['trials']} | {r['entries']} | {r['trigger_entries']} | {r['failed_entries']} | {r['entry_trigger_rate']:.1%} | {r['reentry_trials']} | {r['boundary_candidates']} | {r['candidate_trials']} |" for r in summary)
candidate_table = "\n".join(f"| {r['id']} | {r['case']} | {r['entry_sequence']} | {r['last_inside_speed_m_s']:.6f} | {r['exit_speed_m_s']:.6f} | {r['wall_gap_ms']:.3f} |" for r in verified_candidates)
report = f"""# Speed 正常选择 3 ID：边界高速与漏触发分析

数据目录：`{SOURCE}`。会话：20261025-20-15-08。分析日期：2026-10-25。

## 主要发现

全部为 **50 Hz** 配置；没有 30 Hz 条件。低速准备阈值 0.067 m/s，高速触发阈值 0.375 m/s，timeScale=1，mode=speed，未启用雷球、障碍球或预览。3 个 ID 是条件编号，目标宽度分别为 90、60、30 mm；不是 3 名参与者的编号。环半径同时约为 250、300、350 mm，因此这里不能单独估计目标大小的因果效应。

共有 48 个完成的正式 trial，每个 ID 16 个。排除 6 次预热进入（其中有 2 次候选）后，共 72 次正式进入：48 次触发、24 次未触发退出，没有取消、aborted 或缺失结果。48 个 trial 最终全部完成，但 11 个 trial 需要重新进入。

**13/24（54.2%）次未触发退出满足边界高速候选条件，分布在 9/48（18.8%）个 trial。** 其中 ID2 有 2 次，ID3 有 11 次。候选占所有正式进入的 13/72（18.1%）；9 个涉及候选的 trial 占 11 个重进 trial 的 81.8%，但同一 trial 可以同时包含其他失败原因。

| ID | 目标宽度 mm | 正式 trial | 正式进入 | 触发 | 未触发退出 | 每次进入触发率 | 重进 trial | 边界候选次数 | 涉及候选的 trial |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
{table}

每次进入触发率 = trigger / (trigger + exit + cancel)。重进 trial = inTargetTimes > 1。候选次数与涉及的 trial 数不可混用。同一 trial 内的重复进入并不独立，不对这一个会话做显著性检验。

## 失败原因分解

- 13 次：已经完成低速准备，准备后球内未观察到高速，最后球内速度 < 0.375，首次球外速度 >= 0.375。属于边界高速候选。
- 7 次：退出前未观察到速度 <= 0.067，未完成低速准备。其中 6 次退出速度虽达到高速阈值，也不能归到上述候选。
- 4 次：已经完成低速准备，但退出速度仍低于高速阈值，不能用“下一帧已高速”解释。

如果仅检查 exitFrameSpeed >= 0.375，会筛出 19 次，其中混入 6 次未准备的退出。若只检查 lastInsideSpeed < 0.375 <= exitFrameSpeed，则有 16 次，其中混入 3 次未准备的退出。严格筛选得到 13 次。

## 前后采样核验

13 次候选的 lastInsideSpeed 均与边界日志最后一个 sample 的 decisionSpeed 一致；其后退出样本的 motionSampleSequence 均只增加 1，游戏固定时间均增加约 20 ms。退出坐标的 previousIndex 与最后球内 index 一致，按位移 / motionDeltaTime 复算速度均与日志相符（绝对误差 < 0.00001 m/s）。退出时 tipInside=0 且 distalInside=0，SDK 查询有效，未观察到追踪丢失，enableTrigger=1，reentryRequired=0。

表中的墙钟间隔来自记录回调的 realtime 差值，不是硬件姿态自身的采集时间戳。

| ID | Case | entrySequence | 最后球内 m/s | 首次球外 m/s | 墙钟间隔 ms |
|---|---:|---:|---:|---:|---:|
{candidate_table}

## 帧率与时间间隔能说明什么

所有 13 次候选发生在相邻运动采样之间：固定游戏时间 20 ms，墙钟间隔 27.275–28.266 ms，跨 2 个渲染帧。全体正式连续观察区间共有 1687 对，墙钟间隔平均约 20.036 ms；渲染帧跨度为 0 帧 36 对、1 帧 914 对、2 帧 715 对、3 帧 22 对。三段实验的平均渲染频率约 70.6–70.8 FPS。

因此，约 28 ms 的候选间隔处在本次正常渲染/FixedUpdate 调度的范围内，**不能仅据此认定发生了异常掉帧**。可以确认的是：当前离散采样与球内门控组合，确实存在“球内最后一次未达高速，下一次观察高速时已在球外”的失败模式，而且即使配置为 50 Hz 也存在。

AcceStudy3 的速度是相邻位置位移除以固定游戏步长，并不是连续真实瞬时速度。SDK 查询成功也不保证获得一份全新的硬件姿态。数据不能进一步确定：真实速度何时在球面内越过阈值、追踪延迟/滤波/姿态跳变各贡献多少、提高采样率能挽回多少次。

## trial 耗时与重进

ID1/ID2/ID3 的平均 trial usedTime 分别为 {summary[0]['mean_trial_time_s']:.3f} / {summary[1]['mean_trial_time_s']:.3f} / {summary[2]['mean_trial_time_s']:.3f} 秒；中位数分别为 {summary[0]['median_trial_time_s']:.3f} / {summary[1]['median_trial_time_s']:.3f} / {summary[2]['median_trial_time_s']:.3f} 秒。ID3 有 8/16（50.0%）个 trial 需要重进。Case48 共进入 7 次，其中 6 次未触发退出，3 次是边界候选。

成功那一次进入到触发的中位数约为 580/550/490 ms，这个指标不包括之前失败重进和 trial 之间的移动，不能用它代替完整 trial 用时。

## 数据质量

- 78 个 enter 事件均有且只有一个结果；正式进入无参数变化、追踪丢失或未闭合记录。
- 逐个 ID+Case 连接 time.csv，进入次数与 inTargetTimes 完全一致，每个正式 trial 恰有一次最终 trigger。
- 原始 time.csv 表头有 75 列，48 行数据均有 81 列。检查代码后，差异来自末尾目标坐标组：表头描述 15 个坐标，实际写出包含中心的 17 个坐标。本分析仅使用前 26 列标量字段；末尾错位列未用于结论。没有修改原文件。
- verification.json 记录源文件 SHA-256、检查结果及采样间隔统计；读写分析前后源文件散列一致。

## 适合向导师报告的表述

在本次 50 Hz Speed 正常选择的 48 个正式 trial 中，观察到 24 次未触发退出，其中 13 次（54.2%）在已完成低速准备后呈现“最后球内采样低于高速阈值、下一次球外采样达到阈值”的边界高速模式，涉及 9 个 trial，主要集中于 ID3。这支持离散采样与球内判定门控可能导致漏触发的解释，但不能单独证明异常掉帧或确定采样率的因果效应。还需要相同 ID、阈值、设备与操作条件下的 30 Hz 数据作比较。

## 来源与可复核文件

- 进入结果与候选标记：`{entry_path}`。
- 逐事件/坐标/相邻采样核验：`{boundary_path}`。
- 频率、阈值与实验起止：`{settings_path}`。
- trial 数、重进次数与用时：`{time_path}`，仅使用前 26 列。
- id_summary.csv：3 个 ID 的汇总；trial_detail.csv：48 个 trial 明细。
- failure_detail_verified.csv：24 次未触发退出；boundary_candidates_verified.csv：13 次候选及采样核验。
- speed_boundary_analysis.png：进入结果和候选两帧速度对照图。
- 本目录同时保留原项目分析脚本生成的 speed_sampling_summary.csv、speed_entry_detail.csv、boundary_candidates.csv 与 quality_report.json。
"""
(OUTPUT / "analysis_report.md").write_text(report, encoding="utf-8")
print(json.dumps({"summary": summary, "failures": len(failed), "candidates": len(candidates),
                  "report": str(OUTPUT / "analysis_report.md"), "checks": "passed"}, ensure_ascii=False, indent=2))

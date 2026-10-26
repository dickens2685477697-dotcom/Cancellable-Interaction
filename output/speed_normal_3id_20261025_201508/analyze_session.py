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

summary_rows = "\n".join(f"| {r['id']} | {r['entries']} | {r['trigger_entries']} | {r['failed_entries']} | {r['entry_trigger_rate']:.1%} | {r['reentry_trials']} | {r['boundary_candidates']} | {r['candidate_trials']} |" for r in summary)
candidate_rows = "\n".join(f"| {r['id']} | {r['case']} | {r['entry_sequence']} | {r['last_inside_speed_m_s']:.6f} | {r['exit_speed_m_s']:.6f} | {r['wall_gap_ms']:.3f} |" for r in verified_candidates)
time_rows = "\n".join(f"| {r['id']} | {r['mean_trial_time_s']:.3f} | {r['median_trial_time_s']:.3f} | {r['median_successful_entry_time_s']*1000:.0f} |" for r in summary)
report = rf"""# Speed 正常选择实验：边界高速与未触发退出分析

实验日期：2026-10-25。采集会话：20261025-20-15-08。

## 1. 实验目的与方法

### 1.1 要回答的问题

本实验检查 Study3 的 Speed 选择方式是否存在以下情况：手已经快速抽出目标，但系统直到手离开目标后才记录到达到阈值的速度，因此没有触发选择。分析重点是未触发退出前后的两个相邻运动采样，以及当时是否已经满足 Speed 的准备条件。

### 1.2 实验如何进行

实验使用 Study3 的环形目标选择任务，采用 Fitts16 序列。每个条件先选择开始球，开始球不计入正式试次；随后按程序指定的环上目标顺序完成 16 次选择。环上有 16 个位置，程序每次按设定方向跨 7 个位置切换目标。若一次进入目标后未触发便退出，当前试次继续，重新进入并成功触发后才计为完成一次正式选择。

本次使用 Speed 模式完成 ID1、ID2、ID3 三个条件，每个条件 16 个正式试次，共 48 个。Speed 的操作过程是将手指移入目标，先达到低速准备状态，再通过加速运动触发选择；本分析关注抽手离开目标时的失败记录。三个 ID 是目标条件编号，不是参与者编号，也不是直接计算得到的 Fitts 难度数值。

| 条件 | 目标宽度 | 目标环半径 | 正式试次数 |
|---|---:|---:|---:|
| ID1 | 90 mm | 250 mm | 16 |
| ID2 | 60 mm | 300 mm | 16 |
| ID3 | 30 mm | 350 mm | 16 |

目标宽度和环半径同时变化，因此 ID3 同时涉及较小目标和较大的目标间移动距离。本实验没有把这两个因素分别控制。

三个条件的运动判定频率均为 **50 Hz**，固定时间步长为 **20 ms**，timeScale=1。低速准备阈值为 **0.067 m/s**，高速触发阈值为 **0.375 m/s**；雷球、随机障碍和预览均关闭。虽然原始数据父目录名包含“30hz_50hz_analysis”，本会话实际没有 30 Hz 对照条件。

本次由实验者本人作为唯一参与者，使用 **Quest Pro** 完成，为单参与者自测。报告依据该采集会话进行描述性分析，48 个试次是同一参与者的重复测量，不用于参与者总体的显著性推断。参与者与设备信息由实验者于 2026-10-26 补充确认，实验采集日期为 2026-10-25。

### 1.3 Speed 的判定过程

程序在每次 FixedUpdate（固定时间步回调）读取食指尖位置，用相邻位置的位移除以固定步长计算速度。随后判断手指是否仍在目标判定区域内。食指尖或食指远端关节任一个位于目标碰撞体的世界轴对齐包围盒内，即记为“球内”；两者都在外面才记为退出。因此本文的“球内/球外”指程序实际判定区域，不等同于精确几何球面。

进入后，程序需要先在球内观察到速度 ≤ 0.067 m/s，完成低速准备；准备后再在球内观察到速度 ≥ 0.375 m/s，才触发选择。首次球外采样即使已经达到高速阈值，也只记录退出并结束本次进入状态，不会补触发。

### 1.4 术语与统计口径

| 名词 | 本报告中的含义 |
|---|---|
| 试次（trial、Case） | 从前一个目标完成到当前目标最终成功选择；允许包含多次进入、退出 |
| 一次进入（entry） | 从球外进入，直到触发或未触发退出的连续接触过程；entrySequence 是其日志序号 |
| 未触发退出 | 一次进入没有触发就离开；不代表整个试次最终失败 |
| 重进试次 | 同一试次进入次数大于 1，需要重新进入后才能完成 |
| 最后球内速度、首次球外速度 | 退出前最后一个球内运动采样与紧随其后的第一个球外运动采样的速度 |
| 边界高速候选 | 已完成低速准备，准备后球内未观察到高速；最后球内速度 < 0.375 m/s，首次球外速度 ≥ 0.375 m/s |
| 采样频率与渲染帧率 | 前者是固定运动判定回调频率，后者是画面更新频率；都不保证传感器提供了新姿态 |
| 墙钟间隔 | 两次程序回调实际经过的时间，不是硬件姿态自身的采集时间间隔 |

“最后一帧和下一帧”在本分析中严格按相邻运动采样理解，避免与渲染帧混淆。速度是离散位置差计算的区间速度估计，不是连续真实瞬时速度。

### 1.5 数据处理

使用进入结果日志识别触发与退出，使用边界调试日志核对速度、位置、时间、准备状态和追踪标志，再按 ID+Case 与试次日志连接。共记录 78 次进入，其中 6 次为非正式预热进入，已排除；这 6 次中有 2 次边界候选，也不计入正式结果。

正式分析保留 48 个完成试次中的 72 次进入。边界候选除满足上述条件外，还要求退出时指尖和远端关节均在外、SDK 位姿查询有效、未观察到追踪丢失、允许触发且没有额外重进限制。未完成低速准备的退出单独分类。

## 2. 数据分析与结果

### 2.1 进入结果及边界速度图

![图1：各ID的进入结果与13次边界候选的相邻采样速度](speed_boundary_analysis.png)

图1左侧以一次进入为单位堆叠统计，绿色为成功触发，橙色为边界高速候选，两种灰色分别为未完成准备和已准备但退出仍未达高速。柱顶触发率的分母是该 ID 的全部正式进入次数。右侧每行代表一次候选退出，绿点是最后球内速度，橙点是首次球外速度，连线连接同一次退出的两个相邻采样；红色虚线为 0.375 m/s 阈值。

| ID | 正式进入次数 | 成功触发 | 未触发退出 | 每次进入触发率 | 重进试次 / 16 | 候选次数 | 涉及候选的试次 |
|---|---:|---:|---:|---:|---:|---:|---:|
{summary_rows}

### 2.2 图中数据的含义

三个条件均最终完成了 16 个试次，绿色部分相同；额外的柱高全部来自未触发退出。ID1 每个试次一次进入即可完成；ID2 为完成 16 个试次进行了 19 次进入；ID3 则进行了 37 次进入，其中 21 次退出时未触发。ID3 的 43.2% 是每次进入的触发率，不能解释成只有 43.2% 的试次完成。48 个正式试次最终全部完成，其中 11 个需要重进。

图1右侧的 13 行均表现为绿点低于阈值、橙点达到或超过阈值，说明球内最后一次观察时尚未记录到高速，下一次观察到高速时已经判为球外。这直接对应本次要检查的边界失败模式。13 次候选占 24 次未触发退出的 **54.2%（13/24）**，占 72 次正式进入的 **18.1%（13/72）**，分布在 **9/48 个试次（18.8%）**。同一试次可能反复出现候选，因此候选次数不能替代受影响试次数。

候选主要集中在 ID3：该条件有 11 次，占全部候选的 84.6%；ID2 有 2 次，ID1 没有。ID3 中 8/16 个试次需要重进，7 个出现过边界候选。Case48 共进入 7 次、未触发退出 6 次，其中 3 次属于候选，显示反复退出可以明显增加同一试次的完成成本。

### 2.3 其余未触发退出

| 分类 | 次数 | 占24次未触发退出 | 判定依据 |
|---|---:|---:|---|
| 边界高速候选 | 13 | 54.2% | 已准备，最后球内未达高速，首次球外达到高速 |
| 未完成低速准备 | 7 | 29.2% | 退出前没有满足低速准备条件 |
| 已准备但退出仍低于高速阈值 | 4 | 16.7% | 无法用“下一采样已达高速”解释 |

百分比因四舍五入合计可能略有偏差。若仅检查首次球外速度 ≥ 0.375 m/s，会得到 19 次，其中混入 6 次未准备退出；若只检查两次速度跨越阈值，会得到 16 次，其中仍混入 3 次未准备退出。将准备状态纳入后，才得到 13 次严格候选。

### 2.4 相邻采样与完成时间核验

13 次候选的 motionSampleSequence 均只增加 1，固定游戏时间均增加约 20 ms。最后球内速度与前一条记录一致；退出样本保存的上一位置也与最后球内位置一致。按位移除以 motionDeltaTime 复算首次球外速度，与日志误差均小于 0.00001 m/s。因此这些配对确实是相邻运动采样，未因错误对齐制造速度跳变。

| ID | Case | entrySequence | 最后球内速度 m/s | 首次球外速度 m/s | 墙钟间隔 ms |
|---|---:|---:|---:|---:|---:|
{candidate_rows}

| ID | 整次试次平均用时 s | 整次试次中位用时 s | 最后一次成功进入至触发的中位时间 ms |
|---|---:|---:|---:|
{time_rows}

ID3 的整次完成时间明显增加。最后一次成功进入的等待反而较短，但该指标不包含此前失败、退出和重进的时间，也不包含入球前移动，不能由此认为 ID3 更容易完成。

## 3. 结果的原因分析

### 3.1 离散采样与球内门控共同造成触发窗口错过

程序只在球内检查高速触发。若手指在两个采样之间加速并离开，就可能先读到“仍在球内但速度未达阈值”，再读到“速度达到阈值但已经球外”。13 次候选都符合这一观测顺序，且已排除未准备、额外触发限制和已记录追踪丢失，因此离散采样与球内门控的组合能够解释这些记录为什么没有触发。

这一解释仍有边界：首次球外速度是跨两个位置的区间估计，不能确定连续真实速度在离开之前还是之后达到阈值。日志支持“高速第一次被观察到时已经太晚”，不能还原两次采样之间全部真实运动。

### 3.2 候选集中于小目标、高难条件的可能原因

ID3 判定区域较小，手在其中可供程序观察到高速的空间和时间可能更短，这与候选集中在 ID3 的结果一致。但环半径也随 ID 增加，移动距离、实际动作和实验顺序均可能影响结果。“小目标压缩球内高速观察窗口”是有结果支持的机制推论，尚未作为单独因素得到验证。

### 3.3 现有时间证据不能认定异常掉帧

13 次候选的墙钟间隔为 27.275–28.266 ms，均跨 2 个渲染帧。全体正式连续观察共有 1687 对，墙钟间隔均值为 20.036 ms；渲染帧跨度为 0 帧 36 对、1 帧 914 对、2 帧 715 对、3 帧 22 对。三个条件平均渲染频率约为 70.6–70.8 FPS。

跨 2 个渲染帧的固定回调间隔在本会话较常见，不能仅凭候选约 28 ms 的间隔认定异常掉帧。50 Hz 是程序固定判定配置，SDK 查询有效也不保证每一步取得新硬件姿态。追踪延迟、滤波和姿态更新节奏仍可能影响速度估计，但现有日志不能分别量化其贡献。

## 4. 推论与结论

1. **本次观察到快速抽出后仍未触发的边界记录模式。** 24 次未触发退出中有 13 次满足严格候选条件，涉及 9/48 个试次；问题会影响完成过程，最终全部完成不能说明每次进入都能可靠触发。
2. **最直接的机制推论是离散采样与球内高速判定之间存在时间窗口冲突。** 球内最后一次未记录到高速，首次记录到高速时已球外，当前逻辑又不会补触发。这一机制尤其值得在 ID3 条件下进一步检验。
3. **54.2% 不能解释为由低采样率造成的失败比例。** 它是满足边界候选特征的退出比例；本实验只有 50 Hz，且没有证据将候选间隔认定为异常掉帧。结果支持采样时序相关的漏触发假设，尚未建立采样频率的因果效应，也不能保证提高固定频率即可解决。
4. **后续需同时检验频率和姿态新鲜度。** 在同一 Quest Pro、相同目标、阈值和操作要求下比较 30/50 Hz 或更高频率，记录硬件姿态时间戳或更新序号，并比较边界候选率、重进率和整次完成时间。若新姿态覆盖增加且候选和重进同步减少，才能更有力地将改善归因于采样；边界宽限或连续轨迹判定应作为独立算法因素验证。

## 附录：数据来源与复核

原始目录：`D:\2026.7.18PilotStudy\30hz_50hz_analysis\speed正常选择3ID`，文件前缀为 `20261025-20-15-08-`。

- `speed-entry-summary.csv`：进入、触发、退出和准备状态。
- `speed-boundary-debug.csv`：相邻采样速度、坐标、时间和判定标志。
- `session-settings.csv`：频率、阈值和实验配置。
- `time.csv`：完成时间、进入次数和条件编号。
- 分析附件保留 `id_summary.csv`、`trial_detail.csv`、`failure_detail_verified.csv`、`boundary_candidates_verified.csv`、`verification.json` 及结果图。

78 次进入均有且只有一个结果；正式进入没有观察到参数变化、追踪丢失或未闭合记录。逐试次进入次数与 inTargetTimes 完全一致，每个正式试次恰有一次最终触发。原始 time.csv 表头为 75 列，48 行数据均为 81 列，差异来自目标坐标组的历史表头；本分析仅使用前 26 列可靠标量，未使用错位坐标列。分析前后原始文件 SHA-256 一致，原文件未修改。
"""
(OUTPUT / "analysis_report.md").write_text(report, encoding="utf-8")
print(json.dumps({"summary": summary, "failures": len(failed), "candidates": len(candidates),
                  "report": str(OUTPUT / "analysis_report.md"), "checks": "passed"}, ensure_ascii=False, indent=2))

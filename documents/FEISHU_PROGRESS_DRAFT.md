# 实验程序进度同步草稿

整理日期：2026-10-20（北京时间）
项目：HLStudy4_22
目标文档：https://kvxm3gyd38.feishu.cn/wiki/JXDrwfIyviAVBWkvCP0c9INznOc
同步状态：已于2026-10-25同步到目标飞书多维表格。新增10条、更新9条，116个字段回读一致。当前同步内容见 FEISHU_PROGRESS_SYNC_20261025.md；本文件保留10月20日初稿内容，不代表最新进度。
依据：当前工作区代码与已有项目说明。下文“已实现”表示存在对应实现，不代表本次已完成真机运行或实验采集。

## 一、项目与程序结构

项目采用 Unity 2020.3.37f1c1，包含 MRTK、Oculus、OpenXR 与 XR Interaction Toolkit 相关资源，主要研究手指/手势选择、触发与取消交互。

| 模块 | 主要程序 | 当前职责 |
| --- | --- | --- |
| 通用交互 | Assets/AcceStimulate.cs | 根据手指进入/离开、速度、加速度等条件产生交互事件 |
| Study1 | Assets/Scripts/Study1/ | 起始球、目标球、倒计时、反馈与日志 |
| Study3 调度 | Assets/Scripts/Study3/BallSpawner.cs | 选择技术配置、目标调度与实验流程 |
| Study3 目标与障碍 | Assets/Scripts/Study3/S2AccMode.cs、S2Obstacles.cs | 目标选择、障碍误触、取消与视觉反馈 |
| Study3 判定 | Assets/Scripts/Study3/ReactTap.cs、ReleaseGesture.cs、Pinch.cs | 运动、释放和捏合手势判定 |
| 共享参数 | Assets/Scripts/Study3/Study3SelectionSettings.cs | 集中管理并应用选择技术参数 |
| Fast Tap 阈值实验 | Assets/Scripts/fast_tap_thershold/FastTapThresholdExperiment.cs | pilot/formal、试次调度、颜色任务、触发/取消 |
| Fast Tap 日志 | Assets/Scripts/fast_tap_thershold/FastTapThresholdLogger.cs | 试次结果与指尖轨迹 CSV |

## 二、Study3 Dynamic Stop

已存在 Dynamic Stop 模式，Inspector 显示为“16. Dynamic Stop”，枚举值为 30。运动算法位于 DynamicStopMotionTracker.cs；BallSpawner 与共享设置包含对应参数。

算法根据实际经过时间计算指尖路程速度与加速度，以近期速度历史调整判定阈值。指尖位于目标球内，且速度、加速度均满足阈值时触发。第一段样本用于建立速度历史；触发后同次接触不重复记录，移出或丢失追踪时复位。详细行为以 documents/STUDY3_DYNAMIC_STOP.md 及相关代码为准。

代码默认参数（场景和 ScriptableObject 序列化配置可能覆盖默认值）：

| 参数 | 默认值 |
| --- | --- |
| 最短测量间隔 | 16 ms |
| 速度阈值下限 / 上限 | 0.02 / 0.12 m/s |
| 速度增益 | 0.10 |
| 加速度阈值下限 / 上限 | 0.5 / 4 m/s² |
| 加速度增益 | 3 s⁻¹ |
| 近期速度历史衰减尺度 | 0.18 s |

已有说明记载：实现时通过 Runtime/Editor C# 编译、10 项纯运动算法回归检查，以及 8 个参数的序列化、标签、Tooltip 和共享路由检查。本次未重新运行这些检查。该说明同时记载原生 Unity 检查和设备验证当时受许可证问题影响而未完成；当前许可证状态本次未核实。

待验证：快速穿过是否误触、停止后是否触发、急减速当段是否提前触发、小幅抖动容忍度、触发后继续移动是否重复记录、移出/追踪丢失/重入复位，以及真实设备上的阈值标定。

## 三、Fast Tap Threshold 实验

主场景：Assets/Scenes/fast_tap_thershold/pilot_fast_tap.unity。

现有程序与说明包含 pilot → formal → pilot 的循环流程。代码默认 pilot 每 block 6 次、formal 每 block 10 次；说明规定两种颜色各占一半并随机呈现。试次先触碰起始球，再操作目标球；首次进入后显示颜色任务。

- 绿色：在时间阈值内离开对应 trigger，作为成功目标。
- 黄色：在球内停留达到时间阈值对应 cancel，作为成功目标。
- 阈值预设包含 500、600、700、800、900、1000 ms 与 Custom；代码默认 500 ms。实际运行值需以场景 Inspector 为准。
- 程序包含成功/失败反馈及阶段切换。

日志写入 Application.persistentDataPath/Logs/<timestamp>/：

- trials.csv：phase、trial_index、target_color、cancel_threshold_ms、used_time_ms、success、time_in_target_ms、final_index_x/y/z。
- trajectory.csv：phase、trial_index、elapsed_ms、x/y/z。

本次未启动 Unity、运行完整试次或分析被试数据，因此不报告成功率、用时比较或显著性结论。

## 四、下一步

1. 完成飞书 MCP 授权，读取上方已指定的目标进度文档。
2. 读取目标文档现有结构，将本草稿中对应内容追加或更新到合适章节。
3. 确认本轮实验重点、实际场景参数和设备条件。
4. 按上述待验证项完成设备验证，再记录实验采集与分析结果。

## 核对来源

- documents/PROJECT_STRUCTURE_AND_CODE_SUMMARY.md（历史结构概览，生成于 2026-05-23）
- documents/STUDY3_DYNAMIC_STOP.md
- Assets/Scenes/fast_tap_thershold/README_fast_tap_threshold.md
- ProjectSettings/ProjectVersion.txt
- Assets/Scripts/Study3/BallSpawner.cs、Study3SelectionSettings.cs
- Assets/Scripts/fast_tap_thershold/FastTapThresholdExperiment.cs、FastTapThresholdLogger.cs

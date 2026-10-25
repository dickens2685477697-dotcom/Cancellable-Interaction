# Speed 采样率实验日志

在 `study3` 的 Spawner Inspector 顶部将 **Sampling Rate (Hz)** 设为 50 或 30，再开始实验。
两组保持目标尺寸、速度阈值和取消参数一致。当前共享设置的低速/高速阈值分别为 0.067 / 0.375 m/s。
这是 Unity FixedUpdate 频率的对比，设备的追踪刷新率仍由设备决定。

运行时修改频率仍然可用，但请在实验前设置。日志会记录实际时间步与请求值，且标记一次进入过程中发生的频率或阈值变化。

## 文件和关联

日志保存在 `Application.persistentDataPath/Logs/<时间>/`。trajectory、time、errorLoc 和新增的日志共用 Spawner 的时间标识。

| 文件 | 用途 |
| --- | --- |
| `*-speed-boundary-debug.csv` | 每次进入、球内采样、速度检查、低速准备、触发、取消、退出、追踪丢失和未完成停用的记录 |
| `*-speed-entry-summary.csv` | 每次进入恰好一条结果：`trigger`、`cancel`、`exit` 或 `aborted`，用于直接计算触发率 |
| `*-session-settings.csv` | 开始实验、trial 结束、结束实验及参数变化时的频率、阈值、方向/释放/腕倾取消参数等 |
| `*-trajectory.csv` | 保留原有列，并追加时间、采样率、distal 坐标和运动样本来源 |
| `*-time.csv` | 保留原有 trial 汇总格式和未触发原因；原脚本仍可读取 |

覆盖 Speed、Speed-Dwell、Speed-Release、Speed-Direction、Speed-Wrist-Tilt；同时记录目标球、雷球和障碍球。
预览场景的 state-only 容器不会写文件。

`entrySequence` 在同一个 Spawner 日志会话内唯一，不随 trial 重置。
`case/id/mode/ballIndex/source/sourceInstanceId` 固定为进入时的值，因此触发后即使程序已切换 trial，也不会把这次事件归到下一个 trial。
`currentCase/currentId` 额外保存写入当时的程序值。

## 关键字段

- `decisionSpeed`：实际 Speed 判定调用所用的速度；通常来自当前目标/障碍上的 `AcceStudy3.v`。
- `speedMinArmed` / `armedEver`：此时是否准备、这次进入是否曾完成准备。
- `lastInsideSpeed` / `exitFrameSpeed`：最后球内采样的速度 / 退出事件速度。非退出记录的 exitFrameSpeed 为 NA，触发速度使用 decisionSpeed。
- `speedMaxObservedInside`：球内任何时刻是否观察到高速，包含准备之前。
- `speedMaxObservedAfterArmed`：准备后是否观察到球内高速。与 `allowHighSpeedTrigger` 配合区分方向门控和速度不足。
- `insideSampleCount`：进入期间独立运动样本数；同一 SDK 查询结果在 Update 被重复检查不会重复计数。
- `tipInside/distalInside`：原算法采样时的 `Collider.bounds.Contains` 结果，判定为两者 OR；它是世界坐标包围盒，不是理想球面。
- `index/distal/previousIndex` 坐标、`boundsCenter/Extent`：用于恢复边界和位移，单位米。
- `tipPoseValid/distalPoseValid`：对应 SDK 查询是否成功。成功不代表硬件一定提供了一份新数据。
- `motionTrackingGapSeconds` / `trackingLossObserved`：相邻成功指尖查询之间的固定时间间隔 / 本次进入是否观察到追踪中断。
- `entryExperimentStarted`：进入时是否已开始正式实验；0 是起始球/预热，不计入正式结果。
- `outcome=aborted`：停用对象时尚未完成这次进入，不能直接视作触发失败。
- 正常关闭日志或结束实验时仍未完成的进入也写为 aborted；这类关闭记录保留最后一次观察快照，不表示退出时又采到了一份新姿态。

事件 `sample` 在原球内回调完成进入初始化后、速度与取消判定之前写入。
事件 `check` 记录每次实际速度检查的上下文；`armed`、`trigger`、`cancel` 记录相应转换。
`exit` 在原退出回调清空状态之前记录。触发/取消已写汇总后，后续退出或停用不会重复写汇总。
若应用异常终止，可能只有 enter 而没有结果，分析脚本会单列这些未闭合进入。

## 50 Hz / 30 Hz 时间对齐

`fixedSamplingHz = 1 / fixedDeltaTime`，是每游戏秒的配置频率：30 Hz 的时间步约 0.033333335 s，50 Hz 约 0.02 s。
`requestedSamplingHz` 记录 Inspector 请求值，`entryFixedSamplingHz` 记录进入时的实际频率。
`mixedSamplingRate=1` 表示进入期间观察到时间步变化，`settingsChanged` 还覆盖速度阈值、Dwell 超时和 timeScale 的变化。
其他取消参数的变化记录在 session-settings 中，比较前也应核对。

`time/fixedTime/realtime` 使用高精度 Unity 时钟。`realtime` 是墙钟经过时间，time/fixedTime 为游戏时间；分析实际节奏时同时检查 timeScale。
同一渲染帧可能有多个固定更新，因此 frame 不能单独用作采样编号。
`sourceInstanceId + motionSampleSequence` 标识同一组件的一次固定采样；`motionFixedTime/motionDeltaTime` 标记速度原始计算时刻与除数。
`inFixedStep=0` 表示原程序在 Update 中检查了最近一次固定采样的速度，不要当作新增运动样本。
原程序保留了 FixedUpdate 和 Update 两处 Speed 检查，日志观察器不改变这套行为。

trajectory 的 ori_v 仍来自 temp 上的 AcceStimulate；它的实际 motionSampleSequence 和 pose-valid 字段也一同保存。
trajectory 中的原 indexLoc 来自 Spawner/错误快照，可能与 ori_v 的更新时间不同，不能直接把两者当作同一原子采样。
分析临界触发应使用 speed-boundary-debug 中同源的 decisionSpeed 和指尖坐标。

## 检查退出时才观察到高速

在正式、参数稳定的进入中筛选：

```text
event = exit
outcome = exit
armedEver = 1
speedMaxObservedAfterArmed = 0
lastInsideSpeed < maxSpeedThreshold
exitFrameSpeed >= maxSpeedThreshold
tipInside = 0 且 distalInside = 0
tipPoseValid = 1 且 distalPoseValid = 1
trackingLossObserved = 0
```

符合这些条件的记录已标记 `boundaryHighSpeedCandidate=1`。
它说明最后球内样本不足高速阈值、退出样本达到了阈值；不能单独证明真实连续运动在球内已经达到高速，也不能证明采样率是唯一原因。

## 汇总脚本

```powershell
python tools/analyze_speed_sampling.py "D:\采集回来的Logs" --output output/speed_sampling_comparison
```

只使用本次版本新采集的数据，旧文件不能凭空补出退出状态。
输出按实际 Hz、技术、ID、目标/障碍、雷球标识、速度阈值、Dwell 超时和 timeScale 分组：

- `speed_sampling_summary.csv`：进入数、触发/取消/退出/未完成数、低速准备数、边界候选数、触发耗时。
- `speed_entry_detail.csv`：各会话的进入结果明细。
- `boundary_candidates.csv`：满足上述条件的进入。
- `quality_report.json`：预热、参数混合、追踪丢失、重复汇总、缺少结果的进入等检查。

这里的 trigger_rate = 触发进入数 / (触发 + 取消 + 未触发退出的进入数)，是**每次进入的触发率**。
它与旧分析用 `inTargetTimes > 1` 表示的“需要重进的 trial 比例”口径不同，不应直接混用。
障碍/雷球的 trigger_rate 表示误触发比例，普通目标则表示触发成功比例。
aborted、预热和进入中参数变化的数据不进入正常触发率分母；clean_trigger_rate 进一步排除追踪丢失的进入。
分析前应先结束实验/Play 或让文件完成刷新；仍在运行的会话可能有未结束进入。

新增高频 CSV 和 trajectory 用持久缓冲流写入，每 0.5 秒刷新，在暂停、停用和正常退出时刷新/关闭。
异常断电或崩溃仍可能丢失最后尚未刷新的记录。

## 验证

`tools/ValidateSpeedTelemetry.ps1` 编译实际观察器源文件并执行独立状态/CSV 回归检查；它使用最小 Unity 输入桩，不替代 Unity 场景与真机验证。
`python -m unittest discover -s tools/tests -p "test_*.py"` 验证汇总分母、分组和质量检查。

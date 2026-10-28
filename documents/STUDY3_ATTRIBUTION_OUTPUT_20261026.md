# Study3 兼容归因日志补充说明

实施日期：2026-10-26。新增日志规则版本：`study3-attribution-v1`。

本次为已有实验补充可追溯记录。旧文件名、表头、关键序列化函数和选择阈值保留；新增观测不会再次查询手部关节，也不修改旧的触发、取消、目标顺序或随机规则。独立 Fast Tap 场景同样保留原结果和轨迹文件。

2026-10-27 补充：现在会自动增加 `*-hand-updates.csv` 和 `*-hand-reads.csv`，规则为 `hand-sampling-v1`。覆盖 Study3 中的全部选择模式（包括原版 Magic Tap、Magic Tap 2.0、IxDL 和 Dynamic Stop）及独立 Fast Tap 阈值场景。选择算法和 SDK 姿态读取频率保持原有行为；每次原关节查询仍只执行一次，随后记录查询结果和缓存来源。

`hand-updates` 分别记录 `sdk_read`、`provider_update`、`source_detected/source_lost/provider_unregistered` 和会话开始、结束。保存现有 `OVRPlugin.GetHandState` 返回的 `SampleTimeStamp/RequestedTimeStamp`、应用读取开始/完成时间、Render/Physics 阶段、成功状态、追踪状态及置信度；MRTK 更新另有接收时间、手别、SourceId、更新编号和食指尖/远端关节、拇指尖、手腕坐标。SDK 的读取编号是应用自行计数，不能当成硬件帧号。

SDK 元数据随 `SkeletonPoseData` 传到实际应用的骨骼姿态。仅当来源为 OculusHand，且同侧唯一活动骨架的三个关节坐标完全匹配时，MRTK 更新才关联这份已应用姿态的 SDK 读取编号。`unique_oculus_skeleton_pose_match` 表示这种姿态匹配；骨架不匹配、来源为其他提供器或匹配不唯一时明确标记，SDK 字段为 `NA`。位置相等仍不能证明传感器内部的物理帧身份。

`hand-reads` 记录各检测器实际读取的关节、坐标、组件编号、方法、Unity 帧/固定步时间、查询是否成功、控制器位置是否有效，以及对应 `providerUpdateId`。同一组件、方法、关节再次读取同一个非零更新编号时，`sameProviderUpdateAsPreviousRead=1`；这证明应用重复消费同一 MRTK 更新，不能直接视为误触发。新更新即使坐标相同，也不会因此被标成重复消费。`motionSequence` 对应 AcceStudy3 的运动编号，可与速度日志关联；归因 JSON 的 `legacyMotionOwnerInstanceId` 对应读取行的组件编号。共享管线另以 `sharedSampleSequence` 与 `common-motion` 的 `sampleSequence` 对齐。

`sampleTimestampRelation` 分手实例、Render/Physics 阶段比较 SDK 时间，区分首次/中断后首次、相同、前进、回退、无效、读取失败和未追踪。SDK 时间与 Unity realtime 属于不同时间域，不能直接相减；MRTK `EventTime` 是应用创建事件的时间。本地 1.76 SDK 的常规 GetHandState 姿态可能包含预测，因此这些字段不标为已核实的摄像头原始采集时间。

新增文件沿用 `NA` 表示缺失值、`1/0` 表示布尔值；每 0.5 秒刷新缓冲，暂停和关闭时刷新，关闭行保存 SDK/MRTK/消费记录数。日志写入失败会报错并停止附加输出，选择流程继续。文件位于现有 `Application.persistentDataPath/Logs/<会话>/` 下，不需要在 Inspector 手动开启。应先在 Quest 上录制一段短数据，确认 `sdk_read` 存在、时间戳有效、匹配状态和实际日志开销；本机编译与回放验证不代表已完成实机采集。历史 Pilot 日志无法补回这些字段。

## 1. 新数据应如何读取

优先读取新增的 `*-trial-results-v2.csv`。它保留原 `time.csv` 前 26 个汇总字段，随后以圆心、`targetPositionsJson` 和四个真实触发字段保存数据，并追加会话、试次、参数版本和任务标记。其 46 个列名唯一，列数与数据一致。

旧 `time.csv` 仍按原样输出，包括已发现的 75 列表头与 81 列数据问题。这是为保留现有格式而作的兼容选择；不要继续把旧表头位置映射出的 `triggerSpeed` 当成真实触发速度。真实末尾四个触发值从旧格式化行的尾部复制，不能从错位的表头位置取得。

读取入口：`tools/study3_trial_reader.py`。传入旧 time 文件时，如果同目录有配套 v2，会优先读取 v2；历史数据没有 v2 时，按已知前缀、原始坐标列和真实尾部进行只读恢复。其他不认识的格式明确报错，未记录的触发值保持 NA，不会编造。

PowerShell 示例：

`python tools/study3_trial_reader.py "D:\采集日志\会话\会话-time.csv"`

默认只报告记录数与结构检查，不改原始文件。需要另存读取结果时，可显式传入 `--json-output 新路径.json`。

## 2. 新增文件

### trial-results-v2.csv：列结构正确的完成试次结果

前 26 列继续使用旧字段名。新几何列为 `centerX/centerY/centerZ/targetPositionsJson`；其后是 `triggerSpeed/previousSpeed/speedThreshold/triggerAcceleration`。其他模式未记录这些触发值时仍为 NA。

追加列为 `sessionId/trialSequence/settingsRevision/expectedAction/actualAction/taskResult/participantId/protocolVersion/blockLabel/taskCondition/declaredIntent/schemaVersion`。这里 `schemaVersion=2`；文件只对应旧流程已经完成的试次，不能拿它作为所有进入尝试的分母。

### full-settings.csv：完整有效配置

字段为 `sessionId/settingsRevision/event/frame/time/realtime/case/id/mode/parametersJson/schemaVersion`。

`parametersJson` 保存 BallSpawner 的 75 个有效标量配置，以及固定步长、timeScale、种子和规则标识等运行信息。包含此前缺少的 Pinch 松开距离、独立 Pinch-Direction 参数、Dwell 等待时间、Thumb-Swipe 参数、Dynamic Stop 参数、目标尺寸、雷球显色延迟和非目标触发开关。

配置内容变化才增加 `settingsRevision` 并写新快照；事件和结果行引用该版本。实际检测器可能经过限幅或独立设置，因此事件内还记录 `effectiveDetectorSettings`，例如 Pinch 的真实 ActivateDistance/DeactivateDistance、实际 Dwell WaitTime、React 配置和 Release 配置。不能把请求值自动等同于检测器最终使用值。

### attribution-events.csv：所有技术的观测和分支

该文件与 entry-summary 使用同一套 35 列。身份字段为 `sessionId/trialSequence/entrySequence/eventSequence/case/id/mode/source/sourceInstanceId/ballIndex/isMineBall/experimentStarted`。`source` 区分目标与障碍，雷球由独立布尔值标记。

时间字段为 `frame/time/fixedTime/realtime`。frame 是渲染帧编号；游戏时间、固定游戏时间与实际经过时间均以秒记录。各检测器自己的样本编号、时间和 dt 放在 `decisionJson` 内，不应把所有检测器缓存解释为同一原子采样。

结果与质量字段为 `event/reason/outcome/expectedAction/actualAction/taskResult/inside/trackingValid/trackingLossObserved/entryStartObserved/entryElapsedSeconds/settingsRevision/entrySettingsRevision/settingsChangedDuringEntry/currentCase/currentId/observationBasis/decisionJson/schemaVersion`。

记录接触、React/等待条件满足、捏合开始/松开、Pinch/Release/方向/腕倾实际检查、Speed 检查、共同运动和 Dynamic Stop 测量，以及实际提交、取消、未触发退出、中止和失追状态变化。`decisionJson` 同时保存程序已有状态和该检查的实际输入、门限、几何、准备历史及通过标志；未发生的检查或无效姿态不伪造数值。

Pinch 检查使用 Pinch.Update 已经取得的两指位置、距离和查询有效性。即使旧算法在无效姿态下仍使用回退值，也会同时记录 `distanceUsed` 与有效性，避免把该回退值当作可靠物理距离。Release 在其旧工作状态清空前复制距离增量、张开速度、历史是否准备和判断结果。腕倾记录旧查询实际得到的角度。

Dynamic Stop 另记录实际接受测量的序号、时间、dt、近期速度和当次两个阈值；测量间隔不足时的保留值标为 `sampleAccepted=false`。Magic2/IxDL 延续共同运动样本的来源、序号、接收时间、接受状态及严格比较门限；不会把接收时间称为硬件采集时间。

### attribution-entry-summary.csv：一次进入／尝试的一条最终记录

每个 entry 最多一个最终结果：`trigger/cancel/exit_no_trigger/aborted`。单个试次可有多次进入，先失败后成功是正常情况。进入身份在开始时复制，后续目标推进不会改写其 case/id/source。

旧模式使用其已有接触回调的有效输入区域组合进行观测；这与旧 `inTargetTimes` 的计数口径不保证完全相同。退出先记待定，允许同一原回调完成“退出确认”，随后才结算未触发，避免把成功退出提前算失败。

没有观察到进入起点、但观察到真实终止分支时，写 `entryStartObserved=0`，耗时保持 NA；不会补造起点。失追、停用、模式清理、会话关闭和未完成的目标推进会保留中止/未知状态。参数或检测器有效配置在 entry 内改变时，`settingsChangedDuringEntry=1`。

`taskResult` 是该 entry 相对于记录任务指令的结果，不是参与者心理意图，也不是整个 trial 的最终成败。普通目标提交与雷球误提交必须分开。中止、未知指令、没有完整观察窗口的“持续不触发”等情况保持 unknown。

### trial-lifecycle.csv：所有正式试次的开始、结束和中止

字段为 `sessionId/trialSequence/event/frame/time/fixedTime/realtime/case/id/mode/expectedAction/actualAction/outcome/reason/settingsRevision/participantId/protocolVersion/blockLabel/taskCondition/declaredIntent/targetLayoutJson/schemaVersion`。

记录 `trial_start/trial_end/trial_aborted` 及暂停、恢复、正常关闭标记。`targetLayoutJson` 包含圆心、实际目标坐标、目标尺寸、环半径、雷球索引和种子信息。未完成试次不再因为没有旧 time 行就消失。

### attribution-manifest.json：软件和设备来源

保存日志版本、应用 build GUID、Unity 版本、平台、设备型号、系统、被试/协议标记，以及构建指纹。正常关闭后 `loggingStatus=closed`；日志异常停止时尽力写 `failed` 和错误信息，不会伪装成正常关闭。崩溃、强制终止或未刷盘可能留下 open/未闭合记录，应单列检查，不能默认为失败。

新构建会自动生成 `Assets/Resources/Study3AttributionBuild.json`，包含 Git HEAD、工作区状态和 Study3/Fast Tap 源码 SHA-256。运行时清楚记录该指纹是否可用；没有生成资源的旧构建不会伪装成新构建。Editor 中也可使用 Tools → Study3 → Refresh attribution build stamp 刷新指纹。

## 3. 开始采集前的元数据

Spawner 新增 `studyParticipantId/studyProtocolVersion/studyBlockLabel/studyTaskCondition/studyDeclaredIntent`，只写日志，不参与原实验判定。

- 建议填被试编号、协议版本和 block 标签，避免再次把条件 ID 当成被试编号。
- taskCondition 可填 `normal_selection/circling/pass_through`；未识别的非空条件保持未知解释。
- declaredIntent 是研究者记录的任务指令，可填 `select/cancel/avoid_selection/unspecified`，不表示程序知道参与者的真实意图。
- 元数据留空不会阻止原实验；日志保留空值或 unknown。普通目标/雷球仍可依据已有任务规则区分期望提交与期望取消。

## 4. 质量检查与分析入口

`python tools/analyze_attribution_logs.py "D:\新采集日志" --output "D:\HLStudy4_22\output\attribution_quality.json"`

检查重复结果、未闭合 entry/trial、未观察到起点、失追、参数混用及非正常关闭的 manifest。常规结果统计排除 aborted、重复、起点未观察及参数混用的 entry；unknown 不自动转成失败。没有新增文件时明确标记缺失。

多项条件可以同时未满足。应结合完整 gate/状态、实际分支和源代码版本解释，不把第一条 reason 当成唯一根因；Speed 的边界候选仍不能单独证明真实连续运动在球内越阈或帧率是唯一原因。

## 5. 独立 Fast Tap Threshold 补充

原 trials/trajectory 文件保留，新增 `*-fast-tap-attribution.csv`，20 列为 `sessionId/blockSequence/attemptSequence/phase/trialIndex/event/time/realtime/trackingValid/insideBeforeDecision/colorAssigned/targetColor/thresholdMs/actualAction/legacyTaskSuccess/reason/participantId/protocolVersion/detailsJson/schemaVersion`。

它补充循环 block 和 attempt 的唯一编号、输入有效性、颜色分配、真实完成分支、停用中止，以及当前几何/阈值/颜色序列。`insideBeforeDecision` 明确是检查前缓存状态。

旧 Fast Tap 逻辑可能在球内失追时按 trigger 完成试次。本次没有改这条规则；新增 reason 会标为 `legacy_treats_tracking_loss_as_trigger`，并与原 `legacyTaskSuccess` 一同保留，不能把它解释为确认观察到了物理离球。该场景另可填 attributionParticipantId/attributionProtocolVersion。

## 6. 验证和边界

- 原输出表头与关键序列化函数契约检查通过；实际历史 81 列数据可只读恢复，未改原始 CSV。
- 新归因状态机/CSV 回归 85 条断言通过；实际记录器文件写入、正常/异常关闭和资源释放验证 25 条通过。
- Dynamic Stop 观测及原门控行为 15 条通过；原 Speed 遥测 108 条、共同运动计算 64 条和输入缓存 17 条通过。
- Python 分析和兼容读取回归 18 项通过。
- 使用 Unity 2020.3.37f1c1 的实际工程程序集引用完成运行时与编辑器离线编译。

这些是实现、文件和离线回归验证，不替代 Unity 场景及 Quest Pro 真机复测。新增高频日志的设备开销也应在重新打包部署后的短会话中核对。旧数据不能补出当时没有记录的姿态、门限、意图或中止原因。

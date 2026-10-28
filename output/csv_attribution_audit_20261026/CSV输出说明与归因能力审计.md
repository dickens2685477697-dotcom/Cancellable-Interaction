# Study3 实验输出 CSV 说明与成功失败归因审计

核查日期：2026-10-26。代码基准：D:/HLStudy4_22，HEAD 7e82baa。依据当前已保存源码及被试 2 的两组 2026-10-26 新输出；本次没有改动实验代码、参数或原始数据，也没有新增真机试次。

## 1. 结论

**目前还不能对所有成功和失败做完整归因。** 已有输出足以支持部分程序判定复核，其中 Speed 系列、Magic Tap 2.0 与 IxDL Dynamic 较强；Pinch、Dwell、010 和 Dynamic Stop 的状态机、参数或终止记录仍不完整。另外，当前 time.csv 的列结构存在已在真实新数据中复现的错误，必须优先处理，才能可靠按字段名进行自动分析。

“归因”需区分三层：一是程序当时满足/未满足哪些条件；二是结果是否符合实验任务（普通目标应提交、雷球应取消）；三是为什么人做出了该动作、是否想选择。前两层可逐步用日志完善；第三层需要任务意图标记、观察/访谈等证据，不能仅凭手速或一条失败原因文本证明。

## 2. 最优先的问题：time.csv 表头与行数据错位

被试 2 的 20261026-20-29-04（Pinch-Dwell）及 20261026-20-32-52（Pinch-Direction）均为 48 条完成记录。两组每行 **81 列，但表头只有 75 列**。表头只声明 15 组三维坐标，FormatMsg 实际输出圆心加 16 个目标，即 17 组，差 6 列。重复 targetX/Y/Z 也无法唯一识别每个位置。

以两组第一行为例，按当前表头位置读取 triggerSpeed/previousSpeed/speedThreshold/triggerAcceleration，会得到 0.2474873、0.8525126、0.537、0.3233579；这些实际是目标坐标的一部分。真正末尾四列是 NA、NA、NA、NA，这才符合两个非 Magic2/IxDL 模式的输出。按列名位置配对的读取器会误标这些值，严格解析器也可能报错或采用额外索引；不能把这种读取结果当成有效触发指标。

建议新输出固定使用 centerX/centerY/centerZ 和 target00X…target15Z 等唯一列名，保证表头/数据列数一致；或把目标几何写入独立目标表。旧原始文件应保留，解析副本先按原始列序和实际 schema 恢复，不能直接覆盖原始记录。本次只审计，没有实施修复。

## 3. 成功／失败覆盖矩阵

| 需要回答的问题 | 当前证据 | 仍有的边界 |
| --- | --- | --- |
| 普通目标是否最终提交、雷球是否成功取消、障碍是否误触 | time 的任务/结果字段、errorLoc 和专用事件 | time 先修正结构；未完成 trial 不一定有结果行，trigger 也不天然表示任务成功 |
| Speed 未准备、准备后未越高速、方向阻断、退出后才高速 | speed-boundary-debug、speed-entry-summary、session-settings | 边界候选只能证明离散采样现象，不能证明球内连续真实速度或帧率是唯一原因 |
| Magic2 / IxDL 触发时速度和加速度是否通过 | common-motion；time 四项快照；IxDL 另有 ixdl-trigger | time 当前错位；需按 sampleAccepted、hasAcceleration、inside、selected 解读，且任务意图仍需独立标记 |
| 方向取消为什么发生／未发生 | direction-debug 的窗口、方向角、速度门限及通过标志 | 没有通用 entry 唯一键；部分是中间检查状态，不是试次最终失败；平滑/模式参数快照不完整 |
| Pinch-Release-Trigger／Thumb-Swipe 的开始、松开、接触距离 | pinch-release-distance 的事件和阈值 | 记录时会重新查询姿态，不保证是判定原子样本；不是所有 Pinch 模式都覆盖 |
| Pinch-Dwell／Pinch-Direction 松开门限与每步失败 | time 原因文本，Direction 另有方向调试 | 距离日志不覆盖这两种模式；session-settings 也未保存 pinchEndDistanceThreshold，仍无法完整复现 |
| Dwell／010／Release／腕倾／Dynamic Stop 全部判定过程 | time 的原因串及部分轨迹、方向记录 | 缺统一状态转换、关键数值和实际门限快照；Dynamic Stop 无 common-motion 专用逐判定流 |
| 未触发后主动放弃、对象停用、实验中断、未闭合尝试 | Speed 有 aborted、未闭合检查；共同运动有部分失追状态 | 其他模式没有统一 trial/entry 终止事件；崩溃/未刷盘仍可能只留下部分日志 |
| 意外触发是否违背参与者意图、失败是否由于理解或疲劳 | 雷球颜色任务给出一部分任务真值 | 正常选择/球内画圈/无选择意图未统一写入；不能仅凭低速或触发就归因于人的意图 |

## 4. 当前主要缺口及建议优先级

1. **先修复 CSV 结构。** 新 time 表头和行数据应一致且无重复字段名；分析入口拒绝不匹配文件，旧数据建立只读迁移/解析规则。
2. **补全参数与构建快照。** session-settings 已有 37 列，但缺 Pinch 开始/松开距离、独立 Pinch-Direction 参数、longPressTimeThreshold、Thumb-Swipe 参数、Dynamic Stop 的八项配置、完整目标几何/碰撞体、非目标触发/显色延迟等。还缺 participant/session/block/attempt、协议版本、build/git/package、设备/输入来源、随机种子/顺序。现有 schemaVersion 或 motionRulesVersion 不是完整构建身份。
3. **为所有模式统一记录状态转换和结果。** 建议 trial_start、entry、armed、trigger、cancel、exit_no_trigger、tracking_lost、abort、trial_end；每条含 session/trial/entry/event 唯一键、稳定对象和雷球标志、实际判定输入、每项 gate、门限、区域几何和来源时间。成功与未成功都要记，不能只在最终完成时补一条原因串。
4. **保证失败和未完成分母完整。** 不把没有 time 行的尝试当作不存在；主动中止、实验结束、对象停用、失追和崩溃未闭合分别标注。Speed 的 enter-summary 模型可以推广，但 common-motion 目前还没有相同的统一 entry 结果表。
5. **增加意图与任务真值。** 显式记录普通目标/取消目标、正常选择/持续画圈/仅穿过/主动放弃，以及观察时窗和任务条件。结果分类按“期望动作 × 实际动作”计算，避免把所有 trigger 当成功，或把所有未触发都当失败。
6. **区分直接证据和推断。** 多个条件同时未满足时保留完整 gate 向量和 blocker 集合；另写程序实际采用的优先分支。现有 no trigger reason 有回退文案和检查顺序，不能等同唯一根因。硬件丢帧、算法漏检、追踪失效、人的策略应分别表述。

## 5. 当前输出文件及生成条件

所有 Study3 文件共用 Spawner 的会话时间戳，设备路径为 Application.persistentDataPath/Logs/<会话时间>/。没有某个专用文件可能表示未运行适用模式，不一定是文件丢失。

| 文件后缀 | 一行代表什么 | 适用范围 |
| --- | --- | --- |
| time.csv | 最终完成的一个试次汇总 | Study3 各技术；没有全部未完成尝试 |
| trajectory.csv | 固定更新中的一条轨迹/状态快照 | 正式实验；结束后可另写少量收尾行 |
| errorLoc.csv | 试次内一个障碍误触的位置 | 试次完成时批量写出 |
| direction-debug.csv | 一次方向判断过程 | Direction 相关技术，目标/障碍按调用来源区分 |
| pinch-release-distance.csv | 一次捏合/松开/贴合事件观测 | 目标侧只覆盖 Pinch-Release-Trigger 和 Pinch-Thumb-Swipe；Pinch-Dwell/Direction 可只有表头 |
| speed-boundary-debug.csv | Speed 进入期间事件与实际检查 | Speed、Speed-Dwell、Speed-Release、Speed-Direction、Speed-Wrist-Tilt；目标/雷球/障碍 |
| speed-entry-summary.csv | 一次进入的最终结果 | 与 Speed debug 同 schema；trigger/cancel/exit/aborted |
| session-settings.csv | 开始、结束、trial 结束或受监测参数变化的快照 | 各 Study3 模式；当前 schema=4，参数覆盖不完整 |
| common-motion.csv | 缓存更新读取与是否接受/触发 | Magic Tap 2.0、IxDL Dynamic；schema=1，shared-provider-v3 |
| ixdl-trigger.csv | 一个真实 IxDL 触发事件 | 目标、雷球、障碍；schema=2，不包含未触发的全过程 |

同名字段跨文件不一定具有相同来源。下面给出当前代码写入的全部字段，向量列按轴合并说明。字段名保留原样，不把建议新增列伪装成现有输出。


## 6.1 time.csv 字段

当前表头 75 列，当前 Fitts16 实际行 81 列。必须先按结构处理。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `Case`、`ID` | Case 是试次编号；ID 是难度/几何条件，不是被试编号。 |
| `inTargetTimes`、`errorTimes`、`errorInTimes` | 目标进入次数、障碍误触次数、障碍进入次数；是试次累计计数。 |
| `enable_preview`、`enableMineBall`、`enabledOb` | 预览、雷球任务、障碍物开关。 |
| `usedTime`、`reactTime`、`triggerTime`、`cancelTime` | 单位秒。试次用时与 React/触发/首次取消计时；普通目标无取消时 cancelTime=-1。triggerTime 写入时会按 lastBallEntryTimestamp 重算，不应未经核对当成所有模式统一的“手势开始到提交延迟”。 |
| `triggerCondition`、`start`、`direction(0 for )`、`modeSelected`、`ballIndex` | 触发分支字符、初始目标索引、旋转方向、技术名称、目标索引；分支字符不是任务成功标签。 |
| `react_frequency`、`obstacle_react_frequency` | 目标和障碍的 React 阶段进入/反应计数。 |
| `isMineBall(need cancel)`、`successfully_Canceled` | 前者是本试次是否应取消；后者仅在雷球任务中记录取消是否成功，普通目标为 NA。 |
| `cancelReason`、`no trigger reason`、`cancelTimes` | 取消原因串、按 entry 序号拼接的未触发原因串、累计取消次数。可能一试次内多次失败后最终成功；原因串不是逐时刻全条件快照。 |
| `directionAngleFrame`、`directionSpeedMin` | 最后记录的方向角/相对进入帧及最低速度条件摘要；不是完整方向时间序列。 |
| `targetX`、`targetY`、`targetZ` | 表头重复 15 组；实际写圆心加全部目标点。当前 16 球输出为 17 组三维坐标，必须按实际列数解析，不能按重复列名字典覆盖。 |
| `triggerSpeed`、`previousSpeed`、`speedThreshold`、`triggerAcceleration` | 仅 Magic Tap 2.0 / IxDL Dynamic 的目标实际触发保存四项快照，其他情况 NA。速度单位 m/s，加速度单位 m/s²；新共同规则使用速度向量差的模长。现有 time 表头错位，按列名读取前必须先处理结构。 |

## 6.2 trajectory.csv 字段

44 列；位置、速度与 poseValid 可能来自不同组件，优先使用专用判定流做临界归因。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `Case`、`ID`、`modeSelected`、`area_type`、`index` | 试次、条件、技术、区域类型、对象索引；area_type/index 是程序上下文，不直接代表成功。 |
| `indexLocX`、`indexLocY`、`indexLocZ`、`thumbX`、`thumbY`、`thumbZ` | Spawner 的指尖与拇指坐标（m）；误触时可替换为待写错误快照。 |
| `ori_a`、`ori_v` | temp 对象 AcceStimulate 的加速度和速率；不能默认与当前活动球判定速度同源。 |
| `angle`、`wristX`、`wristY`、`wristZ`、`wristSpeed` | 食指/手腕夹角（度）、手腕位置（m）、手腕速度（m/s）；取活动对象 ReactTap，必要时回退 temp。 |
| `isAOK`、`isVOK`、`A_vec`、`V_vec` | 模式对应的加速度/速度条件布尔值和数值；Magic2、IxDL、Dynamic Stop 有各自路由。不能跨模式直接混用定义。 |
| `errorTriggered`、`isReact`、`currentInBallDistance` | 本行是否错误快照、是否处于 React 阶段、球内移动距离（m）。 |
| `frame`、`time`、`fixedTime`、`realtime` | 渲染帧编号、游戏时间、固定游戏时间、实际经过时间（后三项秒）。frame 不是运动采样唯一编号。 |
| `fixedDeltaTime`、`fixedSamplingHz`、`requestedSamplingHz`、`timeScale` | 固定步长（s）、1/步长配置频率（Hz）、请求采样率（Hz）、游戏时间倍率。 |
| `trajectorySampleSequence`、`motionSampleSequence`、`motionFixedTime`、`motionDeltaTime` | 本轨迹写入序号、temp 运动序号及该运动计算时间和除数（s）；不证明硬件新帧。 |
| `tipPoseValid`、`distalPoseValid`、`distalX`、`distalY`、`distalZ`、`velocitySource`、`schemaVersion` | temp 姿态查询有效性、distal 坐标、速度来源标识、格式版本。有效标记不自动验证 Spawner 的 index/thumb 坐标。 |

## 6.3 errorLoc.csv 字段

8 列；适合定位误触空间位置，不足以独立重放判定。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `Case`、`modeSelected` | 发生障碍误触所属试次和技术。没有 ID、时间、事件序号或速度阈值快照。 |
| `indexLocXError`、`indexLocYError`、`indexLocZError`、`obstacleX`、`obstacleY`、`obstacleZ` | 误触指尖位置和障碍中心（m）；按试次结束批量写出，不能单独精确定位同帧多事件顺序。 |

## 6.4 direction-debug.csv 字段

42 列；高频中间判断，不能直接逐行计算试次失败率。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `frame`、`time`、`case`、`id`、`mode`、`source`、`ballIndex`、`ballKind` | 帧、游戏时间（s）、试次、条件、技术数值枚举、调用来源、对象索引和类型；没有统一 entrySequence/sourceInstanceId。 |
| `failReason`、`finalCancelPass`、`handInBall`、`directionReentryRequired`、`cancelAlreadyTriggered` | 本次方向判断路径、最终方向取消是否通过、接触/重入/取消状态。中间 failReason 可能是正常预备阶段，不等于试次失败。 |
| `sampleCount`、`windowFrameCount`、`windowReady` | 窗口已有样本、配置窗口长度、窗口是否就绪；低速样本可能跳过。 |
| `windowMovementX`、`windowMovementY`、`windowMovementZ`、`windowMovementMagnitude` | 窗口位移向量及模长（m）。 |
| `smoothedDirX`、`smoothedDirY`、`smoothedDirZ`、`hasSmoothedDirection`、`userFacingDirX`、`userFacingDirY`、`userFacingDirZ` | 平滑方向、有效性、比较参照方向（单位向量）；当前世界 -Z 模式参考为 (0,0,-1)。 |
| `angleDeg`、`angleThresholdDeg`、`anglePass`、`indexSpeed`、`minSpeedThreshold`、`speedPass` | 方向角/门限（度）与速度/门限（m/s）及各自通过标志。Pinch-Direction 的方向/速度逻辑按中心点实现解释，列名不能替代源码来源。 |
| `ballCenterX`、`ballCenterY`、`ballCenterZ`、`indexPosX`、`indexPosY`、`indexPosZ`、`viewerPosX`、`viewerPosY`、`viewerPosZ` | 目标中心、食指位置、观察者位置（m）；参照轴模式下 viewer 不必是实际参照来源。 |

## 6.5 pinch-release-distance.csv 字段

31 列；非适用模式只有表头是当前代码行为。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `frame`、`time`、`case`、`id`、`mode`、`event`、`reason`、`ballIndex`、`ballKind`、`isMineBall` | 事件时间和对象上下文；mode 为数值枚举；没有通用进入唯一编号。 |
| `pending`、`isPinch`、`indexInBall`、`thumbInBall`、`triggeredBy`、`entryElapsed` | 是否等待提交、捏合状态、两指入球状态、触发来源、进入后时间（s）。 |
| `poseValid`、`activateThresholdMeters`、`deactivateThresholdMeters`、`distanceMeters`、`distanceMillimeters` | 两指姿态有效性，捏合开始/松开门限（m），两指距离（m 和 mm）。这是事件记录时重新查询的姿态，不保证与 Pinch.Update 判断是同一个原子样本。 |
| `swipeContactValid`、`swipeContactDistanceMeters`、`swipeContactDistanceMillimeters`、`swipeContactThresholdMeters` | 拇指贴合食指数据的有效性、距离（m/mm）及门限（m）；无效写 NA。 |
| `indexX`、`indexY`、`indexZ`、`thumbX`、`thumbY`、`thumbZ` | 两指位置（m）；poseValid=false 时 NA。 |

## 6.6 speed-boundary-debug.csv 字段

77 列；按来源样本序号去重，保留 Update 检查事件。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `frame`、`time`、`fixedTime`、`realtime`、`inFixedStep` | 渲染帧、游戏/固定游戏/实际时间（s）、是否在 FixedUpdate 中调用。Update 可重复检查最近一次固定样本。 |
| `case`、`id`、`mode`、`entrySequence`、`ballIndex`、`ballKind`、`isMineBall`、`source`、`sourceInstanceId` | 进入时锁定的试次/对象上下文；entrySequence 在单一会话内唯一，source 区分 target/obstacle。跨会话需加 sessionId。 |
| `event`、`reason`、`outcome` | enter/sample/check/armed/trigger/cancel/exit/tracking_lost/disabled 等观测；最终 outcome 为 trigger/cancel/exit/aborted，pending 是过程状态。实际停用写 aborted 结果，不能按文件文档中的标签猜枚举。 |
| `handInBall`、`speedMinArmed`、`speedTriggered`、`enableTrigger`、`reentryRequired`、`allowHighSpeedTrigger` | 当前球内/低速准备/触发状态及总触发许可、重入门控、方向高速许可。 |
| `speedMaxObservedInside`、`speedMaxObservedAfterArmed`、`armedEver`、`insideSampleCount` | 本次进入是否观察到球内高速、准备后高速、是否曾准备、独立固定运动样本数；不是独立硬件样本数。 |
| `decisionSpeed`、`lastInsideSpeed`、`exitFrameSpeed`、`minSpeedThreshold`、`maxSpeedThreshold` | 实际判定速度、最后球内速度、退出速度、低/高门限（m/s）；非退出事件 exitFrameSpeed=NA。 |
| `entryElapsed`、`entryTime`、`armedLatencyMs`、`lastInsideFixedTime` | 进入后时间、进入时间、准备延迟（ms）、最后球内固定时间；除标注 ms 外均为秒。 |
| `tipInside`、`distalInside`、`indexX`、`indexY`、`indexZ`、`distalX`、`distalY`、`distalZ`、`previousIndexX`、`previousIndexY`、`previousIndexZ` | 参与判定的入区域状态与本次/前次指尖、distal 坐标（m）；旧算法区域是 Collider.bounds 的包围盒，tip OR distal。 |
| `boundsCenterX`、`boundsCenterY`、`boundsCenterZ`、`boundsExtentX`、`boundsExtentY`、`boundsExtentZ` | 判定包围盒中心和半长（m），用于离线复核几何边界。 |
| `fixedDeltaTime`、`fixedSamplingHz`、`requestedSamplingHz`、`timeScale`、`motionSampleSequence`、`motionFixedTime`、`motionDeltaTime`、`velocitySource` | 采样配置、时间倍率、原判定组件的运动序号、时间和 dt、速度来源。 |
| `tipPoseValid`、`distalPoseValid`、`motionTrackingGapSeconds`、`trackingLossObserved` | 对应组件 SDK 查询有效性、追踪间隔（s）、进入期间是否观察到失追/异常间隔。 |
| `boundaryHighSpeedCandidate` | 边界高速候选：已准备，最后球内未达高速，退出样本达高速，且满足追踪/区域条件；不是“已经证实帧率导致漏触”。 |
| `currentCase`、`currentId`、`entryFixedSamplingHz`、`entryMinSpeedThreshold`、`entryMaxSpeedThreshold`、`mixedSamplingRate`、`settingsChanged` | 写入时试次/条件、进入时频率和门限，以及进入中频率/速度门限/Dwell取消时长/timeScale 是否改变；不覆盖所有技术参数。 |
| `dwellTimeoutSeconds`、`entryExperimentStarted`、`currentExperimentStarted`、`schemaVersion` | Speed-Dwell 超时秒数、进入/当前是否正式实验、格式版本；预热和中止应单列。 |

speed-entry-summary.csv 的全部 77 列与本表相同。debug 包含过程行，summary 只存每次进入的一条最终结果；不得把两者直接合并后重复计数。

## 6.7 session-settings.csv 字段

37 列，schema=4；dwellTimeoutSeconds 来自 pinchDwellCancelTime。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `event`、`time`、`fixedTime`、`realtime`、`frame`、`case`、`id`、`mode` | 会话开始、实验开始/结束、试次结束、受监测参数变化；时间和当前试次/技术上下文。没有每次进入状态机。 |
| `requestedSamplingHz`、`fixedDeltaTime`、`fixedSamplingHz`、`timeScale` | 请求与实际配置采样率（Hz）、固定步长（s）、时间倍率。 |
| `minSpeedThreshold`、`maxSpeedThreshold`、`dwellTimeoutSeconds` | Speed 低/高速度门限（m/s）、pinchDwellCancelTime（s）；dwellTimeoutSeconds 不是 longPressTimeThreshold 的完整替代。 |
| `directionAngleThreshold`、`directionSmoothingAlpha`、`directionMinSpeed`、`directionWindowSamples`、`useEntryDirectionAxis`、`entryDirectionConeHalfAngle` | direction010 方向角/平滑/速度/窗口和世界轴开关/半锥角；没有独立 Pinch-Direction 三项和所有其他方向组。 |
| `releaseDistanceDeltaThreshold`、`wristTiltThreshold` | 释放距离单帧增量（m）、腕倾门限（度）。 |
| `targetSelectionMode`、`enableMineBall`、`enabledOb`、`enablePreview` | 目标序列与任务开关；不包含全部目标几何、非目标触发开关和显色延迟。 |
| `magicTap2SpeedThreshold`、`magicTap2AccelerationThreshold`、`ixdlAccelerationThreshold`、`ixdlInputSpeedMin`、`ixdlInputSpeedMax`、`ixdlSpeedThresholdMin`、`ixdlSpeedThresholdMax` | Magic 独立速度/加速度门限与 IxDL 独立加速度、线性映射端点（速度 m/s，加速度 m/s²）。 |
| `motionRulesVersion`、`motionTimestampKind`、`schemaVersion` | 共同规则和时间类型及 schema=4。旧模式记录 legacy_other_mode / legacy_fixed_step；不是完整应用构建版本。 |

## 6.8 common-motion.csv 字段

36 列，schema=1；triggerSpeed 在这里也用于非触发样本。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `frame`、`time`、`fixedTime`、`realtime`、`case`、`id`、`mode`、`source`、`sourceInstanceId`、`ballIndex`、`ballKind`、`experimentStarted` | 读取时刻与试次/对象上下文；无 isMineBall 和统一 entrySequence，需结合试次结果，未完成试次仍有真值关联缺口。 |
| `trackingValid`、`inside`、`sampleSourceId`、`sampleSequence`、`sampleTimestamp`、`timestampKind`、`sampleAgeSeconds`、`trackingEpoch` | 追踪状态、实际几何区域、MRTK 来源/更新序号/接收时间（s）、时间类型、缓存年龄（s）、失追世代。接收时间不是硬件采集时间。 |
| `acceptedMotionDtSeconds`、`sampleAccepted`、`sampleStatus`、`hasVelocity`、`hasAcceleration` | 最近一次接受运动的 dt（s）、本次是否接受及原因、速度/加速度历史是否准备。未接受行运动数值是保留状态，不能当新测量。 |
| `triggerSpeed`、`previousSpeed`、`speedThreshold`、`triggerAcceleration`、`accelerationThreshold`、`selected` | 当前/前次速率、当次速度门限、向量加速度及门限、是否本次选中。这里 triggerSpeed 在非触发行也是当前速率；只有 selected=true 才是触发事件。 |
| `indexX`、`indexY`、`indexZ`、`motionRulesVersion`、`schemaVersion` | 缓存的指尖位置（m）、共同规则 shared-provider-v3、格式版本 1；没有统一 entry 终止 outcome 或显式每项 gate 位。 |

## 6.9 ixdl-trigger.csv 字段

29 列，schema=2；当前向量加速度定义优先于旧说明。

| 字段 | 含义、单位与读取边界 |
| --- | --- |
| `frame`、`time`、`fixedTime`、`realtime`、`case`、`id`、`mode`、`source`、`sourceInstanceId`、`ballIndex`、`ballKind`、`isMineBall`、`experimentStarted` | 实际 IxDL 触发时锁定上下文；source=target/obstacle，isMineBall 决定目标触发是正确提交还是雷球误触。 |
| `triggerSpeed`、`previousSpeed`、`speedThreshold`、`triggerAcceleration`、`accelerationThreshold` | 实际触发的速率/前次速率/速度门限/向量加速度和当模式独立加速度门限（m/s 或 m/s²）；不是旧有符号速率变化率。 |
| `fixedDeltaTime`、`requestedSamplingHz`、`indexX`、`indexY`、`indexZ` | 固定配置步长（s）、请求频率（Hz）和指尖位置（m）；共同规则的真实运动 dt 应看 sampleDtSeconds。 |
| `motionRulesVersion`、`sampleSequence`、`sampleTimestamp`、`sampleDtSeconds`、`timestampKind`、`schemaVersion` | 共同规则版本、接受更新序号/接收时间/实际 dt（s）和时间类型；格式版本 2。仅写实际触发，不覆盖未触发/取消全过程。 |

## 7. 推荐的关联与判读顺序

- 先核对会话、schemaVersion、motionRulesVersion、mode、参数是否变化和实际列数；模式字符串与数值枚举需按同版本 BallSpawner.Mode 映射。
- Speed 以会话 + entrySequence 关联 debug/summary；entry 的 case/id/source 是锁定上下文，currentCase/currentId 是写入时状态，不能混作同一个编号。
- Magic2/IxDL 以会话 + sourceInstanceId + case/id/ballIndex + sampleSourceId/sampleSequence/接收时间关联；共同运动更新可被多个对象读取，不能仅凭 sampleSequence 跨对象去重。ixdl-trigger 未带 sampleSourceId 时应结合对象和时间，不能假定单列序号全局唯一。
- common-motion 先检查 trackingValid、sampleAccepted、hasAcceleration、inside，再比较实际 speed/acceleration 门限。sampleAccepted=false 的数值是保留状态，不是新的运动证据。
- time 的四个触发值只代表完成试次的触发快照，不能当完整运动流；当前必须先处理六列错位。errorLoc、原因串与 trajectory 是补充证据，不能替代专用事件的同源输入。
- 按“普通目标提交／雷球取消／雷球误提交／障碍误触／普通目标取消或未触发退出／中止／未知”分别统计。一次试次可有多个 entry，某次 entry 失败后最终成功不矛盾。
- Speed 边界候选、重复读取、失追、参数混合、预热和未闭合各自单列；不把 aborted 或 unknown 默认并入失败率分母。对照研究另报告所有尝试和排除原因。

## 8. 实际新数据抽查

2026-10-26 被试 2 两会话共 96 个完成试次：Pinch-Dwell 48、Pinch-Direction 48；ID3 → ID1 → ID2 各 16 条。两组 session-settings 各 55 行（session_start 1、experiment_start 3、trial_end 48、experiment_end 3），已记录字段未见 settings_changed；这不保证未被记录的参数没有变化。

两组 trajectory 分别为 2783、3012 条，44 列均匹配；Pinch-Direction 调试为 387 条，42 列匹配；两组 pinch-release-distance 均为空表头。time 两组各 48 条，75 列表头 / 81 列数据错误完全一致。因此，“现在有 session-settings”仍不足以补回这两组的实际 Pinch 松开距离。

本次抽查是日志结构和可追溯性检查，不是对参与者表现、动作意图或所有技术效果的统计检验。

## 9. 独立 Fast Tap Threshold 输出

该场景另有两个文件；不能与 Study3 的同名 trajectory 或 time 混合。绿色任务要求阈值内离开而触发，黄色任务要求停留达到阈值而取消；success 由该颜色任务规则评估。结果行只在 CompleteTrial 写出，缺少统一的未完成/失追/中止状态。


### fast-tap-trials

| 字段 | 含义与单位 |
| --- | --- |
| `phase`、`trial_index`、`target_color`、`cancel_threshold_ms` | pilot/formal、阶段内试次编号、绿/黄任务、取消时间阈值（ms）；阶段反复循环时缺少 block 唯一编号。 |
| `used_time_ms`、`success`、`time_in_target_ms`、`final_index_x`、`final_index_y`、`final_index_z` | 完成耗时、按颜色任务判定成功（1/0）、目标内停留（ms）、最终指尖位置（m）；没有统一中止原因/失追状态/设备构建快照。 |

### fast-tap-trajectory

| 字段 | 含义与单位 |
| --- | --- |
| `phase`、`trial_index`、`elapsed_ms`、`x`、`y`、`z` | 阶段/试次、经过时间（ms）、指尖轨迹（m）；不能单独区分无采样是失追、停用还是未开始。 |

## 10. 文档与源码不一致之处

IXDL_TRIGGER_OUTPUT.md 仍描述“有符号加速度 / 前一次固定采样”，与当前 shared-provider-v3 的向量加速度及前一次接受更新不一致；该旧说明不能作为当前算法定义。以当前 Study3MotionTracker、IxDLDynamicMotionTracker、LogWriter3 和 COMMON_MOTION_RULES_20261026.md 为准。旧 10 月 25 日采集的数据不能因为今天修改了代码，就自动拥有新日志字段或新算法定义。

## 11. 证据与复核范围

源码：Assets/Scripts/Study3/LogWriter3.cs（试次、原因、参数、专用流）、LogWriter2.cs（轨迹来源）、LogWriter4.cs（误触位置）、SpeedStudyTelemetry.cs、S2AccMode.cs、S2Obstacles.cs、BallSpawner.cs、Study3MotionTracker.cs、MagicTap2MotionTracker.cs、IxDLDynamicMotionTracker.cs、DynamicStopMotionTracker.cs，以及 fast_tap_thershold/FastTapThresholdLogger.cs 和 FastTapThresholdExperiment.cs。

直接样本：D:/2026.7.18PilotStudy/2/Trigger/20261026-20-29-04/ 和 20261026-20-32-52/。完整 schema、来源哈希和结构核验摘要保存在本地同目录的 schema_inventory.json 和 audit_evidence.json。

本次结论是：已有日志能解释一部分程序判定，但尚未满足全部技术、全部结果和全部尝试的完整追溯。下一步优先修列结构、补全配置快照和统一事件生命周期，再进行设备验证；本说明没有把这些建议写成已完成修改。

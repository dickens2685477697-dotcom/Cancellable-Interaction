# Unity 项目结构与代码资源总结

生成日期：2026-05-23  
项目路径：`D:\HLStudy4_22`

## 1. 项目概览

这是一个基于 Unity XR 技术栈的交互实验项目，核心方向是手势、手指进入/离开目标区域、速度/加速度/角度/腕速等条件触发，以及 Study1、Study3、键盘、AI magic tap 等实验场景。

基础环境：

| 项目 | 内容 |
| --- | --- |
| Unity 版本 | `2020.3.37f1c1` |
| 主要平台/技术 | MRTK、Oculus、OpenXR、XR Interaction Toolkit |
| 渲染管线 | URP `10.10.1`，项目中也存在 `HDRPDefaultResources` 历史资源 |
| 输入/交互 | MRTK hand joints、Oculus hand skeleton、Unity XR、XRI 设置 |
| 数据输出 | 多个 `LogWriter*` 将 CSV 写入 `Application.persistentDataPath/Logs/<timestamp>/` |
| 代码组织 | 自有脚本与第三方 SDK 混放在 `Assets` 下，没有统一 asmdef 切分 |

当前工作区存在大量未提交的增删改。本文档只描述当前文件系统与代码状态，不判断这些改动是否应提交。

## 2. 顶层目录结构

以下结构为压缩视图，排除了 `Library/`、`Temp/`、`obj/`、`.vs/`、自动生成 `.csproj/.sln` 的详细展开。

```text
D:\HLStudy4_22
|-- Assets/
|   |-- Scripts/                         # 主要自有业务代码，Study1/Study3/键盘/菜单/AI 分支
|   |-- Scenes/                          # 自有实验和展示场景
|   |-- Prefabs/                         # 实验球、障碍物等预制体
|   |-- Shaders/                         # URP asset、ShaderGraph、材质
|   |-- Resources/                       # Oculus 配置、音频、Mesh 等运行时资源
|   |-- Keyboard/                        # 自定义键盘模型、贴图、材质、脚本
|   |-- VRKeys/                          # VRKeys 键盘插件/改造资源
|   |-- Demo/                            # 早期气泡/菜单演示代码和素材
|   |-- Py/                              # Python 点位生成和实验辅助脚本
|   |-- Oculus/                          # Oculus SDK、Interaction、Voice、Platform、Samples
|   |-- MRTK/                            # MRTK shader/standard assets 局部资源
|   |-- MixedRealityToolkit.Generated/   # MRTK 生成配置和自定义 Profiles
|   |-- XRI/                             # XR Interaction Toolkit 设置和样例设置资源
|   |-- XR/                              # XR Management、Oculus/OpenXR loader 与 settings
|   |-- TextMesh Pro/                    # TextMeshPro 默认资源
|   |-- Samples/                         # Unity/XRI 样例资源
|   |-- Plugins/                         # CodeAssist 等编辑器插件
|   |-- HDRPDefaultResources/            # 历史 HDRP 默认资源
|   |-- Assets/                          # 历史导入资源子目录，含按钮 FBX/材质
|   |-- keyboardAsset/                   # 键盘相关 prefab 小目录
|   `-- ProjectSettings/, UserSettings/  # Assets 内误放或历史拷贝的配置文件
|-- Packages/
|   |-- manifest.json                    # Unity package 依赖
|   |-- packages-lock.json
|   |-- MixedReality/                    # 本地 MRTK/OpenXR tgz 包
|   |-- MathNet.Numerics.5.0.0/          # NuGet 包目录
|   `-- System.ValueTuple.4.4.0/         # NuGet 包目录
|-- ProjectSettings/                     # Unity 项目设置
|-- UserSettings/                        # Unity 用户设置，通常不应作为项目核心内容
|-- Library/, Temp/, Logs/, obj/         # Unity/IDE 生成目录
|-- README.md                            # 当前为空
|-- app.config, packages.config          # NuGet/.NET 配置
`-- adaptive_velocity_mapping.html       # 独立 HTML 文件
```

主要目录文件数量概览：

| 目录 | 文件数 | 说明 |
| --- | ---: | --- |
| `Assets/Oculus` | 5934 | Oculus SDK、Interaction、Voice、Platform、Samples 等第三方资源 |
| `Assets/Keyboard` | 315 | 自定义键盘模型、贴图、材质、脚本 |
| `Assets/Scripts` | 198 | 自有 C# 脚本及 meta 文件 |
| `Assets/Samples` | 90 | Unity/XRI 样例资源 |
| `Assets/Plugins` | 84 | CodeAssist 等编辑器插件 |
| `Assets/VRKeys` | 82 | VRKeys 插件/改造键盘 |
| `Assets/Shaders` | 68 | ShaderGraph、材质、URP asset |
| `Assets/Scenes` | 41 | 自有场景及 meta |
| `Assets/MixedRealityToolkit.Generated` | 35 | MRTK 生成配置/Profile |
| `Assets/Resources` | 24 | Oculus 配置、音频、Mesh |
| `Assets/Py` | 15 | Python 采样脚本和 IDE 配置 |
| `Assets/MRTK` | 23 | MRTK shader 等资源 |

## 3. 依赖与项目配置

### Unity Packages

`Packages/manifest.json` 中的核心依赖：

| 包 | 版本/来源 | 用途 |
| --- | --- | --- |
| `com.microsoft.mixedreality.openxr` | `file:MixedReality/com.microsoft.mixedreality.openxr-1.5.1.tgz` | Microsoft OpenXR 功能 |
| `com.microsoft.mixedreality.toolkit.foundation` | `file:MixedReality/...foundation-2.8.2.tgz` | MRTK Foundation |
| `com.microsoft.mixedreality.toolkit.standardassets` | `file:MixedReality/...standardassets-2.8.2.tgz` | MRTK 标准资源 |
| `com.unity.xr.openxr` | `1.7.0` | Unity OpenXR |
| `com.unity.xr.oculus` | `1.13.1` | Oculus XR 插件 |
| `com.unity.xr.interaction.toolkit` | `2.3.0` | XR Interaction Toolkit |
| `com.unity.render-pipelines.universal` | `10.10.1` | URP |
| `com.unity.textmeshpro` | `3.0.6` | UI 文本 |
| `com.unity.probuilder` | `4.5.2` | 原型建模/编辑 |
| `com.unity.test-framework` | `1.1.33` | Unity Test Framework |

### 本地和 NuGet 依赖

- `Packages/MixedReality/` 下有 3 个本地 tgz：MRTK Foundation、MRTK StandardAssets、Microsoft MixedReality OpenXR。
- `packages.config` 引入 `MathNet.Numerics 5.0.0` 和 `System.ValueTuple 4.4.0`。
- `app.config` 为若干 .NET assembly binding redirects，主要面向 `System.Buffers`、`System.Net.Http`、`System.Numerics.Vectors`。

### XR 与渲染配置

- `Assets/XR/` 包含 `XRGeneralSettings`、Oculus Loader、OpenXR Loader、Oculus/OpenXR Settings。
- `Assets/XR/Settings/OpenXR Package Settings.asset` 中可见 OculusXR、Oculus Quest、Oculus Touch Controller、Hand Tracking、Eye Gaze、Microsoft Hand Interaction 等 feature/profile。
- `ProjectSettings/GraphicsSettings.asset` 指向 URP 管线资源，`Assets/Shaders/UniversalRenderPipelineAsset.asset` 是项目中的 URP asset。
- `ProjectSettings/QualitySettings.asset` 在最高质量层级引用 URP asset。

### Build Settings 场景

`ProjectSettings/EditorBuildSettings.asset` 中列出的自有场景：

| 场景 | enabled | 说明 |
| --- | --- | --- |
| `Assets/Scenes/Study1.unity` | 0 | Study1 实验场景 |
| `Assets/Scenes/keyboard/KeyboardS.unity` | 0 | 键盘主场景，文件较大 |
| `Assets/Scenes/Study3/study3.unity` | 0 | Study3 实验场景 |
| `Assets/Scenes/show.unity` | 1 | 当前唯一启用的 Build 场景 |

## 4. 自有代码模块总结

### 4.1 交互判定核心

| 代表脚本 | 职责 |
| --- | --- |
| `Assets/AcceStimulate.cs` | 通用交互判定核心。读取 MRTK `IndexTip` 等手部关节，计算速度、加速度、进入时长，并通过 `InEvent`、`BeforeHesEvent`、`HesEvent`、`OutEvent`、`AfterOutEvent` 串联交互反馈和执行逻辑。支持 Delay、Fast、FastForKeyboard、Acce、AcceOnly、AcceOld、Velo 等模式。 |
| `Assets/AcceStimulateForKeyboard.cs` | 面向键盘按键的交互判定版本，与键盘输入/取消区域配合。 |
| `Assets/AcceStimulateForKeyboardRestructured.cs` | 键盘交互判定的重构版本，引入 `FingerInstance` 来封装手指关节状态。 |
| `Assets/S1AccMode.cs`、`Assets/S1AccModePrototypeFB.cs` | 根据 Study1 或原型反馈场景的模式，为 `AcceStimulate` 绑定事件监听，决定长按、快速点击、01/010 等交互路径。 |
| `Assets/Scripts/Study3/ReactTap.cs` | Study3 的复杂触发器。除基础速度/加速度外，还计算手指-手腕角度、腕部速度、球内移动距离、自适应进入速度/实时指速等，并提供 `ChangeEvent`、`CancelEvent`。 |
| `Assets/Scripts/Study3/S2AccMode.cs` | Study3 目标球模式装配器。按 Tap、longPress、magicTap、reactTap、pinch、fastTap、rainbow 等模式绑定目标球事件、反馈、取消逻辑、彩虹 arc/frame 可视化。 |
| `Assets/Scripts/Study3/S2Obstacles.cs` | Study3 障碍物模式装配器。逻辑与目标球类似，但用于错误触发、障碍反馈和取消路径。 |

整体事件模型：

```text
MRTK/Oculus 手部关节
  -> 计算 IndexTip / ThumbTip / Wrist pose
  -> 判断是否进入 Collider/越过平面/满足速度加速度角度条件
  -> UnityEvent: InEvent / BeforeHesEvent / HesEvent / OutEvent / AfterOutEvent
  -> 改变材质、执行目标、取消操作、记录 CSV、推进实验轮次
```

### 4.2 Study1 实验

| 代表脚本 | 职责 |
| --- | --- |
| `Assets/Scripts/Study1/S1ButtonBehavior.cs`、`S1ButtonBehavior2.cs` | 绑定 `AcceStimulate` 和 `S1Clock`，管理进入绿球、倒计时、离开判定、完成次数、材质变化。 |
| `Assets/Scripts/Study1/S1Clock.cs` | 控制倒计时 UI、剩余次数、提示文本、成功/警告/结束状态。 |
| `Assets/Scripts/Study1/StartBallBehavior.cs`、`StartBallBehavior2.cs` | 起始球状态控制，决定是否允许正式触发目标球流程。 |
| `Assets/Scripts/Study1/S1Player.cs`、`S1PlayerWithFeedBack.cs` | 玩家/目标视觉反馈，包括改色、透明度变化、反馈态。 |
| `Assets/Scripts/Study1/LogWriter.cs` | 写入 Study1 CSV：`Case,RunTime,Object,EnterTime,Velocity,Acceleration,Condition,PrepTime`。 |
| `Assets/Scripts/Study1/ExperimentCounter.cs`、`CountDown.cs`、`DataAnalysis.cs` | 计数、倒计时、数据分析占位或辅助逻辑。 |

Study1 的主线是：起始球激活 -> 手指进入目标球 -> `S1Clock` 倒计时 -> 成功后要求移开 -> 离开后计数并写日志。

### 4.3 Study3 实验

| 代表脚本 | 职责 |
| --- | --- |
| `Assets/Scripts/Study3/BallSpawner.cs` | Study3 核心流程控制器。负责目标球/障碍物生成、圆形布局和随机布局、ID 轮换、模式选择、对象池、错误快照、实验开始/结束事件、音效反馈。 |
| `Assets/Scripts/Study3/ReactTap.cs` | 目标/障碍上的高级触发检测。包含速度、加速度、腕速、角度、球内移动距离、自适应速度阈值等。 |
| `Assets/Scripts/Study3/S2AccMode.cs` | 为目标球配置所选交互模式及反馈，包括 `fastTap`、`rainbow`、`reactTap` 等。 |
| `Assets/Scripts/Study3/S2Obstacles.cs` | 为障碍物配置交互/错误触发模式，并向 `BallSpawner` 注册错误。 |
| `Assets/Scripts/Study3/S2Player.cs` | 目标球视觉状态：普通、浅色、黄色/绿色 mine ball、圆环显示等。 |
| `Assets/Scripts/Study3/Pinch.cs` | Pinch 手势事件输入。 |
| `Assets/Scripts/Study3/SetReactTap.cs` | 配置 `ReactTap` 的角度模式。 |
| `Assets/Scripts/Study3/AcceStudy3.cs`、`AcceStudy3_2.cs` | Study3 版本的加速度/速度触发器，和早期 `AcceStimulate` 逻辑相近。 |
| `Assets/Scripts/Study3/DataCapture.cs` | CSV 捕获辅助，支持 debug path。 |
| `Assets/Scripts/Study3/LogWriter2.cs` | 轨迹日志，记录 Case、ID、手指位置、原始速度加速度、模式、区域类型、角度、腕部位置/速度、A/V 判定、错误触发、thumb 位置等。 |
| `Assets/Scripts/Study3/LogWriter3.cs`、`LogWriter4.cs`、`LogWriter5.cs`、`LogWriter_ob.cs` | 不同粒度的实验、目标、障碍或回合日志。 |

Study3 是当前最重的业务模块。`BallSpawner` 负责实验生命周期与空间布局，`ReactTap`/`S2AccMode`/`S2Obstacles` 负责目标与障碍的触发和取消，多个日志组件持续记录实验数据。

### 4.4 键盘交互

键盘相关资源分成三套：

| 路径 | 内容 |
| --- | --- |
| `Assets/Keyboard/` | 自定义键盘模型、键帽贴图、材质、prefab、脚本。 |
| `Assets/Scripts/KeyBoard/` | 较新的键盘触发逻辑，包括平面穿越、约束、提示面、输入提交。 |
| `Assets/VRKeys/` | VRKeys 插件或改造版本，含 keyboard prefab、按键脚本、布局、材质、字体和示例场景。 |

关键脚本：

| 代表脚本 | 职责 |
| --- | --- |
| `Assets/Scripts/KeyBoard/KeyTriggeringBehavior.cs` | 按键触发主逻辑。组合 `AcceStimulateForKeyboard`、`GetText`、`CrossPlane`，处理进入、离开、取消区域、提示面、文本提交。 |
| `Assets/Scripts/KeyBoard/CrossPlane.cs` | 判断手指是否穿越键盘平面、是否在键盘边界/取消区域内，并暴露进入/离开事件。 |
| `Assets/Scripts/KeyBoard/Constraint.cs`、`DoubleTriggerConstraint.cs` | 控制触发约束，避免重复或错误触发。 |
| `Assets/Scripts/KeyBoard/HandAttributeDetector.cs` | 检测手掌旋转、相机到手掌距离、手掌到键盘距离、pinch 等手部属性。 |
| `Assets/Keyboard/Scrips/GetText.cs`、`Textinput.cs`、`DeleteBehavior.cs`、`KeyOption.cs` | 输入文本、删除、键选项和键盘 UI 行为。 |
| `Assets/Keyboard/Scrips/PalmAway.cs`、`PalmToFace.cs`、`BloomGesture.cs`、`RotationDetect.cs` | 手势/掌心方向检测。 |
| `Assets/VRKeys/Scripts/*.cs` | VRKeys 的键盘、按键、控制器、布局、Backspace/Enter/Clear/Cancel/Shift/Space 等实现。 |

### 4.5 菜单与气泡系统

| 代表脚本 | 职责 |
| --- | --- |
| `Assets/Scripts/BubbleOption.cs` | 气泡选项的通用状态和视觉行为，支持子选项展开/收起、形状/颜色变化、Collider 调整等。 |
| `Assets/Scripts/TouchBlast.cs` | 实现 `IMixedRealityTouchHandler`，把 MRTK touch started/updated/completed 转成 UnityEvent。 |
| `Assets/Scripts/MenuSystem/*.cs` | 一组按模式和层级拆开的气泡菜单脚本，例如 `BubbleFast*`、`BubbleAcc*`、`BubbleTouch*`、`BubbleWithCancel`、`BubbleWithExecute`。 |
| `Assets/Scripts/MenuSystem0/*.cs` | 早期或备用菜单系统，实现 A/B/C 层级气泡、组移动、Blast 行为、玩家反馈。 |
| `Assets/Demo/*.cs` | 早期演示版 Bubble/010/011 行为，与取消/执行按钮配合。 |

这部分代码大量使用 `AddListener` 绑定 `AcceStimulate` 或 `TouchBlast` 事件，目标是演示和比较不同菜单触发策略。

### 4.6 AI magic tap

| 代表脚本/资源 | 职责 |
| --- | --- |
| `Assets/Scripts/AI_magicTap/ExpController.cs` | AI magic tap 实验控制器。管理起点球/目标球、每轮点击次数、目标位置生成、回合推进。 |
| `Assets/Scripts/AI_magicTap/Set4AIStart.cs`、`Set4AITarget.cs` | 起点球和目标球触发模式设置，暴露 start/target clicked 事件。 |
| `Assets/Scripts/AI_magicTap/HandLogWriter.cs`、`EyeLogWriter.cs`、`RoundLogWriter.cs` | 记录手部、眼动、轮次数据。 |
| `Assets/Scripts/AI_magicTap/Infer.cs`、`Infer_py.cs` | 推理入口或 Python 推理调用占位。 |
| `Assets/Scripts/AI_magicTap/Body_Lock.cs` | AI magic tap 分支的 body lock 行为。 |
| `Assets/Py/SpawnBall.py` | 2D Poisson disk sampling 点位生成。 |
| `Assets/Py/SpawnBall2.py`、`SpawnBall3.py` | 3D Poisson disk sampling 点位生成。 |

注意：`ExpController.RunPythonScript` 和相关脚本中存在本机绝对路径，例如 `C:\Users\Liu_Yixuan\Downloads\...` 和 `C:\Users\Neo-R\miniconda3\python.exe`。这会导致项目在其他机器上不可直接运行对应 Python 流程。

### 4.7 展示与辅助工具

| 代表脚本 | 职责 |
| --- | --- |
| `Assets/Scripts/Show/NewAcce.cs`、`NewAcce2.cs` | 展示场景中的速度/加速度交互判定，文件较长，和核心触发逻辑相似。 |
| `Assets/Scripts/Show/Set2.cs`、`Set3.cs` | 展示场景模式绑定。 |
| `Assets/DrawCircle.cs` | 生成圆形点位或圆形可视化。 |
| `Assets/StartGame.cs` | 起始交互绑定，进入后触发 `GameStart`。 |
| `Assets/ButtonUtils.cs`、`CancelRainbow.cs` | 按钮辅助和彩虹取消相关行为。 |
| `Assets/Scripts/BodyLock.cs`、`AI_magicTap/Body_Lock.cs` | 将对象跟随或锁定到用户身体/头显相对位置。 |
| `Assets/Scripts/FakeCamera.cs`、`FollowCamera.cs`、`TextFollowController.cs` | 相机/文本跟随辅助。 |
| `Assets/Scripts/HandMarker.cs`、`InputDisplay.cs`、`NextController.cs`、`ActivateConstraint.cs`、`CheckRoot.cs` | 调试、输入显示、移动/约束等辅助逻辑。 |

## 5. 场景资源总结

自有场景主要分布：

| 路径 | 说明 |
| --- | --- |
| `Assets/Scenes/Study1.unity` | Study1 主实验场景 |
| `Assets/Scenes/Study_Longpress.unity` | 长按实验或 Study1 变体 |
| `Assets/Scenes/Study3/study3.unity` | Study3 主实验场景 |
| `Assets/Scenes/Study3/MRTKStudy1.unity`、`MRTKStudy1with1.unity` | 历史/组合实验场景 |
| `Assets/Scenes/keyboard/KeyboardS.unity` | 键盘主场景，约 7 MB |
| `Assets/Scenes/keyboard/keyboard.unity` | 键盘简化或早期场景 |
| `Assets/Scenes/keyboard/One_button_prototype*.unity` | 单按钮原型 |
| `Assets/Scenes/AI_magicTap/*.unity` | AI magic tap、body lock、world、infer 场景 |
| `Assets/Scenes/show.unity`、`showDwell.unity`、`dwellfitts.unity` | 展示和 dwell/fitts 实验相关场景 |
| `Assets/Scenes/MRTK-20221123-Study1 1.unity` | 历史 MRTK Study1 场景 |

第三方示例场景位于 `Assets/Oculus/**/Scenes`、`Assets/Oculus/**/Samples`、`Assets/VRKeys/Scenes` 等，不建议在业务总结中逐个展开。

## 6. 预制体、材质、模型与音频

### 预制体

| 路径 | 说明 |
| --- | --- |
| `Assets/Prefabs/Study2/S2Trigger.prefab` | Study2/Study3 风格目标触发球 |
| `Assets/Prefabs/Study2/S2Obstacle.prefab` | 障碍物预制体 |
| `Assets/Prefabs/Study2/Grayball.prefab` | 灰球资源 |
| `Assets/Prefabs/Ex1TypeF.prefab` | 实验/原型预制体 |
| `Assets/Prefabs/MoveRightSphere.prefab` | 移动/下一步控制相关球体 |
| `Assets/Keyboard/Prefab/*.prefab` | 自定义键盘基础 prefab |
| `Assets/VRKeys/Prefabs/*.prefab` | VRKeys 键盘 prefab |

### 材质与 Shader

`Assets/Shaders/` 包含：

- `UniversalRenderPipelineAsset.asset`、`UniversalRenderPipelineAsset_Renderer.asset`
- `LightRing*.shadergraph`、`Dissolve.shadergraph`、`Standard.shadergraph`、`hand.shadergraph`
- `S2TargetCurrent*.mat`、`S2TargetNext*.mat`、`S2succeed*.mat`
- `ball.mat`、`ballGreen.mat`、`ballRed.mat`
- `LRD*`、`LRD_Glow*`、`circle.mat`、`hand.mat`

`Assets/Keyboard/Materials/` 包含键盘高亮/非激活/执行/取消/文本框/透明触发材质，以及 `MaskedTxt.shader`。

### 模型与贴图

- `Assets/Keyboard/Keys/` 中有大量按键 FBX、JPG、inverted PNG，覆盖字母、空格、回车、删除、语音、拼音、大小写、语言等键位。
- `Assets/Demo/` 包含演示表情图、Undo/Redo/Lock 图标、rainbow FBX、反向键盘 OBJ。
- `Assets/Resources/Mesh/rainbow v1.fbx` 是运行时可加载的彩虹 mesh。
- `Assets/rainbow-Ca.obj`、`Assets/Demo/反向键盘.obj` 是 OBJ 模型资源。

### 音频

`Assets/Resources/Audio/` 包含：

- MRTK manipulation start/end 音效。
- `success.wav`
- `error.wav`

Study3 的 `BallSpawner` 中有 `sourceRight`、`sourceWrong`，用于正确/错误反馈。

## 7. Python 与数据处理资源

`Assets/Py/` 当前主要脚本：

| 文件 | 说明 |
| --- | --- |
| `SpawnBall.py` | 2D Poisson disk sampling，输出归一化 x/y 坐标。 |
| `SpawnBall2.py` | 3D Poisson disk sampling，输出 x/y/z 坐标。 |
| `SpawnBall3.py` | 带固定 seed 的 3D Poisson disk sampling 变体，输出点位数量。 |
| `test.py` | 测试脚本。 |

Git 状态中还显示若干 Python 文件处于新增/删除混合状态，例如 `analyse.py`、`case_in_line.py`、`draw.py`、`get_mean.py`、`main_old.py`、`pre.py`、`remove.py`。文档生成时未修改这些文件。

## 8. 第三方和生成资源边界

以下目录应视为第三方、生成或工具资源，业务总结只做概览：

| 路径 | 类型 | 建议 |
| --- | --- | --- |
| `Assets/Oculus/` | Oculus SDK、Interaction、Voice、Platform、Samples | 不建议手改 SDK 源码；升级时整体替换或按官方迁移。 |
| `Assets/MRTK/` | MRTK 资源 | 保持与 MRTK 包版本一致。 |
| `Assets/MixedRealityToolkit.Generated/` | MRTK 生成配置/Profile | 可通过 MRTK 配置工具维护，手改需谨慎。 |
| `Assets/TextMesh Pro/` | TMP 默认资源 | 通常只在字体/材质确有需要时修改。 |
| `Assets/Samples/` | Unity/XRI 样例 | 可参考，不建议作为核心业务依赖。 |
| `Assets/Plugins/CodeAssist/` | 编辑器插件 | 与运行时代码分离，问题排查时注意 Editor-only。 |
| `Library/`、`Temp/`、`obj/`、`Logs/`、`.vs/` | Unity/IDE 生成目录 | 不应进入业务文档详细结构，也通常不应提交。 |

## 9. 风险与注意事项

1. 工作区很脏：当前 Git 状态存在大量 `M`、`D`、`AD`、`??`，包括脚本、场景、材质、包配置、ProjectSettings、第三方样例资源等。任何后续重构前应先确认哪些是有效业务改动。
2. Python 调用有绝对路径：`AI_magicTap` 和部分实验流程中写死了本机 Python 和脚本路径，跨机器运行风险高。
3. 编码存在乱码：多个 C# 注释或字符串出现 mojibake，例如中文注释被错误编码显示。后续整理文档或重构时需统一文件编码。
4. 第三方资源体量大：`Assets/Oculus` 文件数量远超业务代码，搜索和统计时必须排除 SDK 目录，否则容易误判项目规模。
5. Build Settings 只启用 `show.unity`：虽然 Study1、KeyboardS、Study3 都在 Build Settings 中列出，但当前只有展示场景启用。
6. 自有脚本无 asmdef 分层：大部分业务脚本处于默认 `Assembly-CSharp`，编译边界不清，第三方依赖和实验分支耦合较重。
7. 事件绑定分散：大量逻辑通过 `AddListener` 在 `Start()` 中组装，运行时行为依赖场景对象组件是否齐全，静态阅读难以完全覆盖场景引用。
8. 日志写入频繁打开关闭文件：多个 `LogWriter*` 在帧级或事件级打开/关闭 `FileStream`，实现简单但有性能和 IO 风险。
9. `Application.persistentDataPath` 数据不在项目目录：实验 CSV 默认不会出现在 `Assets/Logs`，排查数据时要看运行平台的 persistent data 目录。
10. 历史分支资源并存：Study1、Study2、Study3、keyboard、VRKeys、AI_magicTap、show/dwell 等资源同时存在，命名和职责有重叠。

## 10. 后续维护建议

- 先冻结当前有效实验场景清单，再清理 Build Settings 中不用的历史场景。
- 为自有代码建立 asmdef，例如 `Study1.Runtime`、`Study3.Runtime`、`Keyboard.Runtime`、`Editor.Tools`，降低编译和依赖混乱。
- 把 Python 路径改成可配置项，例如 `StreamingAssets`、`Application.dataPath` 相对路径或 ScriptableObject 配置。
- 为 `AcceStimulate`、`ReactTap`、`BallSpawner` 补充最小可复现场景说明和参数表。
- 将第三方 SDK、样例、业务资源分目录隔离，减少搜索、迁移和升级成本。
- 统一 C# 文件编码为 UTF-8，修复乱码注释，避免后续协作误读。

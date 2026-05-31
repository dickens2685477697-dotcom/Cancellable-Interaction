# Fast Tap Threshold 实验说明

## 实验做了什么
这个实验用于测试被试在 `Fast Tap` 交互模式下，对目标球进行“快速离开”与“停留取消”两类动作时的表现。

实验包含两个循环阶段：

1. `pilot` 阶段
2. `formal` 阶段

运行流程如下：

1. 先进入 `pilot` welcome 面板，显示 `welcome pilot expirement`
2. 点击 `start` 后进入 `pilot` block
3. `pilot` block 共 `6` 次试次，其中 `3` 次黄球、`3` 次绿球，顺序随机
4. `pilot` 完成后进入正式实验 welcome 面板，显示 `welcome to this expirement`
5. 点击 `start` 后进入 `formal` block
6. `formal` block 共 `10` 次试次，其中 `5` 次黄球、`5` 次绿球，顺序随机
7. `formal` 完成后再次回到 `pilot` welcome 面板

也就是说整体是循环结构：

`pilot -> formal -> pilot -> formal ...`

## 单次试次逻辑
每个试次都包含两个球：

1. 起始球
2. 目标球

具体规则：

1. 被试先触碰起始球开始当前试次
2. 随后在前方出现蓝色目标球
3. 食指尖第一次进入目标球后，目标球会变为半透明，并随机变成黄色或绿色
4. 系统按当前阈值判断两种行为：
   - 在阈值时间内离开目标球：`trigger`
   - 在目标球内停留达到阈值：`cancel`

颜色与成败关系：

- 绿色球：`trigger` 为成功，`cancel` 为失败
- 黄色球：`cancel` 为成功，`trigger` 为失败

## pilot 与 formal 的区别
- `pilot` 阶段用于练习
- `formal` 阶段用于正式记录
- 两个阶段都使用同样的起始球、目标球和时间阈值逻辑
- 两个阶段的开始面板都会显示动作提示说明

## 可在 Inspector 中调节的内容
挂载对象：`FastTapThresholdExperimentRoot`

主要参数：

- `startBallPosition`
  起始球位置
- `targetDistanceMeters`
  目标球相对起始球的前向距离
- `targetForwardAxis`
  目标球生成方向
- `welcomePanelZPosition`
  welcome 面板的 Z 轴位置
- `thresholdPreset`
  阈值预设：500 / 600 / 700 / 800 / 900 / 1000 / Custom
- `customThresholdMs`
  自定义阈值（毫秒）
- `pilotTrialsPerBlock`
  pilot 阶段试次数，当前为 6
- `formalTrialsPerBlock`
  formal 阶段试次数，当前为 10
- `greenInstructionDescription`
  开始面板中绿色球对应的中文说明
- `yellowInstructionDescription`
  开始面板中黄色球对应的中文说明
- `successClip`
  成功音效
- `failureClip`
  失败音效

## 当前场景中各文件的作用

### 场景文件
- [pilot_fast_tap.unity](/D:/HLStudy4_22/Assets/Scenes/fast_tap_thershold/pilot_fast_tap.unity)
  实验主场景。包含 MRTK 基础环境、实验根对象 `FastTapThresholdExperimentRoot`，以及当前实验运行所需的场景配置。

### 脚本文件
- [FastTapThresholdExperiment.cs](/D:/HLStudy4_22/Assets/Scripts/fast_tap_thershold/FastTapThresholdExperiment.cs)
  实验主控制脚本。负责：
  - `pilot/formal` 两阶段流程控制
  - welcome 面板生成与显示
  - 起始球和目标球生成
  - 食指尖进入/离开球的判定
  - `trigger/cancel` 逻辑
  - 成功/失败反馈音效
  - trial 调度与 block 切换

- [FastTapThresholdLogger.cs](/D:/HLStudy4_22/Assets/Scripts/fast_tap_thershold/FastTapThresholdLogger.cs)
  日志输出脚本。负责：
  - 创建日志目录
  - 输出主 trial CSV
  - 输出轨迹 CSV
  - 为每条记录写入 `phase`

### 音频资源
- [success.wav](/D:/HLStudy4_22/Assets/Resources/Audio/success.wav)
  成功音效

- [error.wav](/D:/HLStudy4_22/Assets/Resources/Audio/error.wav)
  失败音效

## CSV 输出内容
日志目录位于：

`Application.persistentDataPath/Logs/<timestamp>/`

Windows 下通常类似：

`C:\Users\<用户名>\AppData\LocalLow\Angelo Morty And Cindy Co\show4.0\Logs\<timestamp>\`

### 主 CSV
文件名：

`<timestamp>-pilot_fast_tap-trials.csv`

列：

- `phase`
- `trial_index`
- `target_color`
- `cancel_threshold_ms`
- `used_time_ms`
- `success`
- `time_in_target_ms`
- `final_index_x`
- `final_index_y`
- `final_index_z`

### 轨迹 CSV
文件名：

`<timestamp>-pilot_fast_tap-trajectory.csv`

列：

- `phase`
- `trial_index`
- `elapsed_ms`
- `x`
- `y`
- `z`

## 备注
- 起始球当前为中性灰
- 目标球初始为蓝色，进入后再变为黄/绿
- 开始面板中的动作说明文字已经支持在 Inspector 中直接修改

# Study3 1–15 选择技术讲解图

## 视觉与生成规范

- 风格：技术线稿 / HCI 论文示意图，白色背景，黑色细线手部轮廓。
- 布局：每张图四个横向步骤；每一步严格只有一只右手和一个虚拟球。
- 球体：浅青蓝色表面（约 `#98CEE5`）。
- 接触/准备/触发：绿色光晕；取消：红橙色光晕。
- 准备状态：中灰色圆环。
- 捏合：食指指腹与拇指指腹必须直接贴合，零可见空隙；虚拟球可穿透手部，不能画成被手抓住。
- 成功：一个绿色勾；取消：一个红色叉。

## 文件与过程

| 编号 | 技术 | 文件 | 触发 | 取消 |
| --- | --- | --- | --- | --- |
| 1 | Pinch-Release-Trigger | `01-pinch-release-trigger.png` | 球内捏合后松开 | 保持捏合离开球 |
| 2 | Pinch-Dwell | `02-pinch-dwell.png` | 捏合后在 0.7 秒内松开 | 捏合保持超时 |
| 3 | Pinch-Direction | `03-pinch-direction.png` | 未进入取消方向并在球内松开 | 闭合捏合朝取消方向移动 |
| 4 | Pinch-Thumb-Swipe | `04-pinch-thumb-swipe.png` | 松开时拇指远离食指线段 | 松开时拇指仍贴近食指 |
| 5 | Dwell | `05-dwell.png` | 稳定停留 0.7 秒 | 达到时长前离开 |
| 6 | Dwell-Release | `06-dwell-release.png` | 未做 Release，停留 0.7 秒 | 中指、无名指、小指快速张开 |
| 7 | Dwell-Direction | `07-dwell-direction.png` | 未进入取消方向，稳定停留 0.7 秒 | 停留期间朝取消方向移动 |
| 8 | Speed | `08-speed.png` | 先减速到准备阈值，再加速到触发阈值 | 未触发即离球 |
| 9 | Speed-Dwell | `09-speed-dwell.png` | 先减速准备，再加速触发 | 球内停留超时 |
| 10 | Speed-Release | `10-speed-release.png` | 先减速准备，再加速触发 | 准备后中/无名/小指快速张开 |
| 11 | Speed-Direction | `11-speed-direction.png` | 减速准备后沿安全方向加速 | 准备后朝取消方向移动 |
| 12 | 010-Release | `12-010-release.png` | 进入 010 状态后，不做 Release 并出球 | 球内中/无名/小指快速张开 |
| 13 | 010-Time | `13-010-time.png` | 进入 010 状态后在 0.7 秒内出球 | 球内停留超时 |
| 14 | 010-Direction | `14-010-direction.png` | 未触发方向取消并出球 | 球内朝取消方向移动 |
| 15 | 010-Contact | `15-010-contact.png` | 从球背侧触发区域出球 | 从其他位置出球 |

## 代码依据

- `Assets/Scripts/Study3/BallSpawner.cs`：1–15 名称、阈值和灰色准备环开关。
- `Assets/Scripts/Study3/S2AccMode.cs`：各技术的进入、准备、触发与取消逻辑。
- `Assets/Scripts/Study3/S2Player.cs`：球体视觉状态切换。
- `Assets/Scripts/Study3/ReleaseGesture.cs`：Release 手势检测。

生成方式：Codex 内置 ImageGen。第 1 张经局部编辑建立母版，其余图片以母版作为风格参考分别生成。

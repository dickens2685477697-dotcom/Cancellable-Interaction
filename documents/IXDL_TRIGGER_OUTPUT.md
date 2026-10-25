# IxDL Dynamic 触发记录

适用于 `17.IxDL Dynamic`。日志与现有实验输出共用目录：
`Application.persistentDataPath/Logs/<会话时间>/`。

`*-time.csv` 末尾新增四列，记录该试次目标球触发时实际用于判定的值：

| 字段 | 含义 | 单位 |
| --- | --- | --- |
| `triggerSpeed` | 触发采样的食指速率 | m/s |
| `previousSpeed` | 前一次固定采样的速率，用于生成本次阈值 | m/s |
| `speedThreshold` | 本次实际使用的动态速度阈值 | m/s |
| `triggerAcceleration` | 触发采样的有符号速率变化率 | m/s² |

数值在条件从假变真时复制保存，随后才切换目标。后续采样、对象销毁或
运动历史重置不会改变它们。每个试次结束后清空，避免沿用上一试次。
零速触发写 `0`；没有 IxDL 目标触发的试次及其他模式写 `NA`。
雷球通过取消成功完成试次时，也不会伪造速度触发记录。

`*-ixdl-trigger.csv` 每次实际触发写一行，覆盖目标、雷球和障碍球：

- `source=target` 是目标或雷球，`isMineBall` 区分雷球。
- `source=obstacle` 是障碍球误触，不会覆盖 `time.csv` 的目标触发值。
- `case/id/mode/ballIndex/sourceInstanceId` 在切换目标前写入。
- 同时记录时间、四项判定值、加速度阈值、采样时间步及指尖位置。
- `experimentStarted=False` 表示起始球或预热，不应混入正式试次。

事件行写入后立即刷新。暂停、停用及退出时也会刷新并按现有日志生命周期关闭。
新增列与文件从本版本新采集的日志开始生效。

当前加速度仍为 `(本次速率 - 前次速率) / fixedDeltaTime`，减速时为负数。

const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const bundle = createRequire('C:/Users/tangx/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/package.json');
const sharp = bundle('sharp');
const out = 'D:/HLStudy4_22/output/sampling_issue_feishu_20261028';
fs.mkdirSync(out, { recursive: true });
const esc = s => String(s).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
const shapes = [];
const palette = { hardware: ['#f5f3ff', '#a78bfa', '#5b21b6'], sdk: ['#eff6ff', '#60a5fa', '#1d4ed8'],
  mrtk: ['#ecfeff', '#22b8cf', '#0e7490'], old: ['#fff1f2', '#fb7185', '#be123c'], current: ['#ecfdf5', '#34d399', '#047857'], neutral: ['#f8fafc', '#cbd5e1', '#334155'] };
function text(x, y, value, size = 19, color = '#334155', bold = false) {
  shapes.push(`<text x="${x}" y="${y}" font-size="${size}" fill="${color}" font-weight="${bold ? 700 : 400}">${esc(value)}</text>`);
}
function box(x, y, w, h, title, lines, kind = 'neutral', size = 19) {
  const [fill, stroke, color] = palette[kind];
  shapes.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="12" fill="${fill}" stroke="${stroke}" stroke-width="1.8"/>`);
  text(x + 19, y + 30, title, 22, color, true);
  lines.forEach((line, i) => text(x + 19, y + 59 + i * 26, line, size));
}
function arrow(d, color = '#64748b', dashed = false) {
  shapes.push(`<path d="${d}" fill="none" stroke="${color}" stroke-width="2.7" ${dashed ? 'stroke-dasharray="7 5"' : ''} marker-end="url(#arrow)"/>`);
}
text(38, 42, '从传感器到 FixedUpdate：旧调用逻辑与重复读取问题', 31, '#0f172a', true);
text(38, 76, 'Quest Pro + Unity Editor + Quest Link；箭头表示数据传递，不表示同一帧执行。频率为本次软件观察值。', 19, '#64748b');
box(38, 103, 626, 84, '① 头显追踪摄像头 / 传感器', ['真实手指运动 → 图像输入；原始相机帧与硬件采样率未采集。'], 'hardware', 18);
arrow('M351 187 V208');
box(38, 215, 626, 105, '② Meta 手追踪运行时 + Quest Link → PC XR 运行时', [
  '图像 → 手部关节估计 / 跟踪；经 Link 向电脑提供姿态。',
  '运行时输出预测姿态；内部推理、Link 传输与缓存节奏未测。'], 'hardware', 18);
arrow('M351 320 V339');
box(38, 346, 626, 119, '③ Oculus SDK：OVRHand.Update() 读取姿态', [
  'OVRPlugin.GetHandState(Render) → _handState 缓存。',
  '本次右手读取约 71.02 Hz；读取成功不等于相机生成新帧。',
  '本次 OpenXR 后端：OVRHand.FixedUpdate 不读 Physics。'], 'sdk', 18);
arrow('M351 465 V485');
box(38, 492, 626, 98, '④ OVRSkeleton：SDK 状态 → 骨架 Transform / Bones', [
  'Update / FixedUpdate 应用已有状态；不额外采集传感器。',
  '即使骨架在固定步更新，MRTK 关节字典也不会自动刷新。'], 'sdk', 18);
arrow('M351 590 V610');
box(38, 617, 626, 144, '⑤ MRTK 输入 Provider → 关节缓存 / 事件', [
  'OculusXRSDKDeviceManager.Update → OculusHand.UpdateController',
  '→ UpdateHandData：读取 Bones → jointPoses 关节字典',
  '→ RaiseHandJointsUpdated 发布事件；每个来源约 71.02 Hz。',
  '当前同一 Provider 注册两次：同一右手状态产生两个 SourceId。'], 'mrtk', 17.5);

box(716, 103, 646, 129, 'Unity 调度：应用帧与固定步是两种时间尺度', [
  'Update：每个 Unity 应用帧一次；本次约 71.02 FPS。',
  'FixedUpdate：按逻辑步长 20 ms 推进，目标约 50 Hz。',
  '一帧可执行 0 次、1 次或多次固定步；“补跑”可连续读旧缓存。'], 'neutral', 18);
box(716, 255, 646, 129, '顺序：MRTK -100 → OVRHand -90 → OVRSkeleton -80', [
  '当前 MRTK.Update 可能使用前一应用帧应用的骨架。',
  '本次候选关联：SDK 帧 N → MRTK 帧 N+1 → FixedUpdate 帧 N+2。',
  '以上帧均为 Unity 应用帧；骨架位置匹配不等于硬件帧 ID。'], 'neutral', 18);
box(716, 407, 646, 153, '把三种现象分开，避免误称“硬件重复采样”', [
  '硬件帧是否重复：没有原始硬件帧 ID，当前无法判断。',
  'Provider 双来源发布：同一 SDK 状态 → 两个 MRTK 来源。',
  'FixedUpdate 重复消费读取：同一来源的同一序号再被读取。',
  '两个 Oculus Provider ≠ 两个 MRTK 主系统 ≠ 左右手各一个。'], 'neutral', 18);
box(716, 583, 646, 178, '本次正式测试：28.56 秒，动态阈值 ixdlDynamic', [
  'FixedUpdate 消费读取 1,429 次，实际约 50.01 Hz。',
  '同帧同序号重复 39 次（2.73%），当前代码全部跳过。',
  '补跑两次读取实际相隔中位数 0.422 ms，逻辑时间仍增 20 ms。',
  'SDK 返回 → FixedUpdate 消费读取：中位数约 27.36 ms。',
  '时间差基于候选软件链路，不包含相机采集到 SDK 的延迟。'], 'current', 18);

arrow('M351 761 V781 H348 V801');
arrow('M351 781 H1038 V801');
box(38, 810, 626, 222, '旧分支：AcceStudy3.FixedUpdate() 消费读取', [
  'HandJointUtils.TryGetJointPose(IndexTip) → MRTK 缓存',
  '不核验 MRTK 更新序号：v = |Δp| / 0.02；a = (v − v前) / 0.02',
  '重复位置 → v = 0；前次速度非零时，还会得到 a < 0。',
  'Speed：InEvent → S2AccMode 检查低速 → armed。',
  '原版 Magic Tap：JudgeAcce() → Prepared。',
  '后续达到确认条件才触发；准备状态不等于必然选择。'], 'old', 17.5);
box(716, 810, 646, 222, '当前分支：共同预处理 + FixedUpdate 消费读取', [
  '事件 → Study3HandPoseSource 保存来源、Sequence 与接收时间',
  'S2AccMode.FixedUpdate → UpdateSharedMotionSelection()',
  'TryGetSample → Study3MotionTracker.TrySample',
  '同来源、同追踪期且序号未变：不推进运动历史，不触发选择。',
  '新样本：按实际 MRTK 接收时间差计算速度与加速度。',
  'Magic Tap 2.0 / IxDL Dynamic 共用；其他机制尚未全部统一。'], 'current', 17.5);
text(38, 1067, '旧路径风险：缓存重复可建立低速 / 减速准备。当前 39 条已跳过；仍需分别处理 Provider 配置与数据更新时机。', 20, '#334155', true);
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1400" height="1100" viewBox="0 0 1400 1100"><defs><marker id="arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0 0L8 4L0 8" fill="#64748b"/></marker></defs><rect width="1400" height="1100" fill="white"/><g font-family="Microsoft YaHei,SimHei,Arial,sans-serif">${shapes.join('')}</g></svg>`;
fs.writeFileSync(path.join(out, 'sensor_to_fixedupdate_sampling_logic.svg'), svg);

const run = (content, bold = false, link) => ({ text_run: { content, text_element_style: { ...(bold ? { bold: true } : {}), ...(link ? { link: { url: link } } : {}) } } });
const textBlock = (...elements) => ({ block_type: 2, text: { elements: elements.map(e => typeof e === 'string' ? run(e) : e) } });
const heading = content => ({ block_type: 4, heading2: { elements: [run(content)] } });
const bullet = (...elements) => ({ block_type: 12, bullet: { elements: elements.map(e => typeof e === 'string' ? run(e) : e) } });
const glossary = [
  heading('0. 先说明代码名称与术语'),
  bullet(run('Update() / FixedUpdate()：', true), 'Unity 的应用帧回调 / 固定步回调。本文“FixedUpdate 消费读取”专指选择逻辑在固定步读取 MRTK 最新样本；逻辑步长 20 ms 不代表真实时间上整齐间隔 20 ms。下文“帧”均指 Unity 应用帧（Time.frameCount）。'),
  bullet(run('OVRPlugin.GetHandState / OVRHand / OVRSkeleton：', true), '分别是 Oculus SDK 的姿态读取接口、保存 SDK 状态的 Unity 组件、将状态应用到骨架 Transform/Bones 的组件。它们不是传感器；GetHandState 返回预测姿态，重复调用不保证得到新的相机帧。'),
  bullet(run('MRTK / OculusXRSDKDeviceManager / OculusHand.jointPoses：', true), '分别是输入框架、接入 Oculus 数据的输入 Provider、MRTK 保存关节位姿的字典。Provider 读取 Bones 后通过 RaiseHandJointsUpdated 发布事件。当前只有一个 MRTK 主组件，但同一个 Oculus Provider 配置了两次，产生同一只右手的两个 SourceId。'),
  bullet(run('HandJointUtils.TryGetJointPose(IndexTip)：', true), '读取 MRTK 缓存中的食指指尖位置；这次调用不会要求摄像头重新采样。IndexTip 是食指指尖关节名。'),
  bullet(run('AcceStudy3.FixedUpdate() / MotionSampleSequence：', true), '旧运动计算入口 / 该组件的固定步计数。旧代码按 fixedDeltaTime 计算速度 v 和带正负号的标量加速度 a；固定步计数增加，不代表 MRTK 发布了新样本。'),
  bullet(run('S2AccMode / S2Obstacles / InEvent / HesEvent：', true), '目标球 / 障碍球的选择判定组件；InEvent 为旧运动组件在球内触发的回调，HesEvent 为满足确认条件后的事件。Speed 的 CheckSpeedReleaseIndexTrigger() 可建立 armed（低速准备）；原版 Magic Tap 的 JudgeAcce() 可建立 Prepared，再经 HesEvent、SetHesMode() 等完成选择。'),
  bullet(run('Study3HandPoseSource / Study3MotionTracker.TrySample()：', true), '当前共享样本缓存 / 运动预处理。前者在 MRTK 事件到达时保存 SourceId、Sequence 和接收时间，读取时保持同一有效来源；后者跳过同来源、同追踪期的重复或过时序号，用实际接收时间差计算运动。Sequence 是应用事件序号，不是硬件帧 ID。MagicTap2MotionTracker 与 IxDLDynamicMotionTracker 共用此规则。'),
  bullet(run('Study3HandSamplingDiagnostics / Study3HandSamplingSession：', true), '新增的三层观察与 CSV 写入代码。hand-updates 记录 SDK 读取和 MRTK 事件，hand-reads 记录消费读取，common-motion 记录当前模式是否接受样本及是否触发。“接受的运动样本”指通过新样本检查并推进运动历史的记录。'),
];
const test = [
  heading('3. 新增实机测试（2026-10-28）'),
  textBlock(run('测试方法：', true), '电脑 Unity Editor + Quest Pro / Quest Link，Study3 的 ixdlDynamic 模式，保持 FixedUpdate 50 Hz、timeScale=1。点击起始球后按静止约10秒、移动约10秒、停手约10秒的流程采集并停止 Play；有效追踪窗口28.56秒，无精确动作分段，报告合并区间。同步记录 SDK、MRTK、FixedUpdate 三层日志，结束计数及六份原日志哈希一致，关键统计经独立解析复核。'),
  textBlock(run('数据结果：', true), '右手 SDK 读取约71.02 Hz；MRTK 两个来源各约71.02 Hz，同一 SDK 状态被各发布一次。FixedUpdate 消费读取约50.01 Hz：1,429次中39次（2.73%）为同应用帧、同样本序号的重复；这39次全部跳过，没有据此触发选择。补跑两次读取实际相隔中位数0.422 ms，逻辑时间仍增20 ms。候选软件链路“SDK返回→FixedUpdate消费读取”时间差中位数27.36 ms，P99为41.20 ms。'),
  textBlock(run('结论：', true), '实测确认补跑会再次读到旧 MRTK 样本，当前动态阈值的序号去重有效；Provider 重复注册是另外一个独立问题，尚未修改。此次未分别实测 Magic Tap 2.0 或全部15种机制。2.73%与 Pilot 的1.44%、17.4%分母不同，不能直接比较改善幅度。SDK时间戳不是已验证的原始相机帧身份，因此不能据此判断硬件重复率或保证硬件第一时间输出；上述时间差也不包含传感器到 SDK 的延迟。'),
];
const updates = [
  { block_id: 'GMdcd4cn2oHgZYxwgY3c94XNn1c', update_text: { elements: [run('结论：旧代码可能把重复读取的 MRTK 缓存计算成零速度或突然减速，影响选择准备；新增实机测试捕获到39/1,429次（2.73%）FixedUpdate重复消费读取，当前动态阈值已全部跳过。', true)] } },
  { block_id: 'ZQvfdV2N7o6yF7x2vljcI6QxnZ5', update_text: { elements: [run('Pilot 已观察到旧判定路径受到影响；原版 Magic Tap 也有同类代码路径。以下先解释名称，再展示完整链路，并分别列出 Pilot 证据与本次实机测试。')] } },
  { block_id: 'doxcnl7QsnO6xnYDAeo8dCSsPJh', update_text: { elements: [run('固定步配置50 Hz（逻辑步长20 ms）不等于每秒收到50份新数据。传感器输入经 Meta 运行时、Quest Link、SDK 状态、骨架和 MRTK 缓存后才供 FixedUpdate 消费读取；骨架在固定步更新不会自动刷新 MRTK 字典。旧代码未核验 MRTK 更新身份，同一缓存位置再次参与差分时 v=0，前次速度非零时还会得到 a<0。图中硬件与封闭运行时内部频率未测，软件频率来自此次测试。')] } },
  { block_id: 'doxcnvSxUCOKckOYXhAcZLksEof', update_text: { elements: [run('2. Pilot 与旧比较实验中的证据')] } },
  { block_id: 'doxcnkR4p1TfMQ5yvv1R1yFl3db', update_text: { elements: [run('发现背景：2026-10-25 旧实现对比实验。', true), run('动态模式正常选择有16/48次、球内持续画圈有44/48次在零速记录上触发；这60次均呈现同一 Unity 应用帧、连续固定步位置重复。当时 Magic Tap 2.0 会跳过不足1 mm位移（跳过本次运动计算和触发判断），两者预处理不同，旧结果不能单独归因于动态阈值策略。')] } },
  { block_id: 'doxcnTOilxqpBKoAr4o3Gjuup4g', update_text: { elements: [run('17.4% 不是误触率。', true), run('日志确认的是判定路径，不能推断过滤后这25次会失败或反应时间会改变多少。这批旧日志缺少硬件采样时间戳与硬件帧身份，不能逐条证明传感器内部状态；新增诊断也不能补回历史记录中未采集的信息。')] } },
  { block_id: 'doxcnlG1WRnXkTEwXm9QyRNyX3d', update_text: { elements: [run('4. 影响范围与处理现状')] } },
  { block_id: 'doxcnYLVNH6UzUjK71KwBKOojrb', update_text: { elements: [run('当前 Magic Tap 2.0 与 IxDL Dynamic 共用 MRTK 应用事件序号去重、稳定来源和实际接收时间差；本次实测验证了动态阈值分支，其他正式技术尚未全部统一。下一步先单独消除 Oculus Provider 重复注册并复测，再比较 FixedUpdate 与 MRTK 发布新样本后的消费方式，保留各技术的触发、取消和计时条件。本次未改更新方式或 Provider 配置。')] } },
  { block_id: 'doxcncZYaOskjxAbHE1SecYPCuA', update_text: { elements: [run('依据：'), run('旧实现比较报告', false, 'https://kvxm3gyd38.feishu.cn/docx/BHzTdceBNoTTF3xrMz8ci5oknxu'), run('；'), run('共同运动规则修订', false, 'https://kvxm3gyd38.feishu.cn/docx/Krvid94LAoLCVjxupfDc5TYkn8f'), run('；Pilot：20261027-11-14-50、11-41-48、11-47-05。新增实机会话：20261028-19-41-51；原始日志快照、逐条关联和校验记录保存在项目 output/hand_sampling_baseline_20261028/20261028-19-41-51。更新：2026-10-28。')] } },
];
const sources = [textBlock('平台说明：', run('Meta 手追踪与 Link', false, 'https://developers.meta.com/vr/documentation/unity/unity-handtracking-overview/'), '；', run('预测姿态与原始时间戳接口的区别', false, 'https://developers.meta.com/vr/documentation/unity/unity-handtracking-unextrapolated-poses/'), '；', run('Unity 2020.3 回调执行顺序', false, 'https://docs.unity3d.com/2020.3/Documentation/Manual/ExecutionOrder.html'), '。当前 SDK wrapper 为1.76.0；较新的原始姿态接口另有版本与后端要求，当前未接入。')];
const plan = { documentId: 'WNCJdIhNaoVqg6xA6Q8c1oBrn2I', imageBlockId: 'doxcnIEekQyrFVTCGRg0Jwp65iL', imageWidth: 1400, imageHeight: 1100,
  glossaryInsertIndex: 3, glossary, testInsertBefore: 'doxcnlG1WRnXkTEwXm9QyRNyX3d', test, updates, sources };
fs.writeFileSync(path.join(out, 'update-plan.json'), JSON.stringify(plan, null, 2));
const before = JSON.parse(fs.readFileSync(path.join(out, 'document-before.json'), 'utf8'));
const root = before.items.find(b => b.block_type === 1);
const map = new Map(before.items.map(b => [b.block_id, b]));
const byId = new Map(updates.map(r => [r.block_id, r.update_text.elements]));
const desired = root.children.map(id => {
  const b = structuredClone(map.get(id)); if (byId.has(id)) (b.text || b.heading2 || b.bullet).elements = byId.get(id); return b;
});
desired.splice(3, 0, ...glossary);
desired.splice(desired.findIndex(b => b.block_id === plan.testInsertBefore), 0, ...test);
desired.push(...sources);
const md = desired.map(b => b.block_type === 27 ? `![完整链路图](${out}/sensor_to_fixedupdate_sampling_logic.png)`
  : (b.block_type === 4 ? '## ' : b.block_type === 12 ? '- ' : '') + (b.text || b.heading2 || b.bullet).elements.map(e => {
    const t = e.text_run; return t.text_element_style?.link ? `[${t.content}](${t.text_element_style.link.url})` : t.text_element_style?.bold ? `**${t.content}**` : t.content;
  }).join('')).join('\n\n');
fs.writeFileSync(path.join(out, '重复手部采样问题_更新稿.md'), '# 重复手部采样问题：旧调用逻辑与 Pilot 证据\n\n' + md + '\n');
sharp(Buffer.from(svg)).resize(2800, 2200).png().toFile(path.join(out, 'sensor_to_fixedupdate_sampling_logic.png'))
  .then(info => console.log(JSON.stringify({ image: info, glossaryBlocks: glossary.length, testBlocks: test.length, updatedBlocks: updates.length })))
  .catch(e => { console.error(e.message); process.exitCode = 1; });

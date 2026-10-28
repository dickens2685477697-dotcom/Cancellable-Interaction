const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const bundled = createRequire('C:/Users/tangx/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/package.json');
const sharp = bundled('sharp');
const out = 'D:/HLStudy4_22/output/mrtk_frequency_analysis_20261027';
fs.mkdirSync(out, { recursive: true });
const parts = [];
const palettes = {
  hardware: ['#f4f0ff', '#a78bfa', '#5b21b6'],
  sdk: ['#eff6ff', '#60a5fa', '#1d4ed8'],
  mrtk: ['#ecfeff', '#22b8cf', '#0e7490'],
  app: ['#ecfdf5', '#34d399', '#047857'],
  warn: ['#fff1f2', '#fb7185', '#be123c'],
  neutral: ['#f8fafc', '#cbd5e1', '#334155'],
};
const esc = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
function text(x, y, value, size = 25, color = '#334155', bold = false) {
  parts.push(`<text x="${x}" y="${y}" font-size="${size}" fill="${color}" font-weight="${bold ? 700 : 400}">${esc(value)}</text>`);
}
function box(x, y, w, h, title, lines, kind = 'neutral', size = 25) {
  const [fill, stroke, titleColor] = palettes[kind];
  parts.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="16" fill="${fill}" stroke="${stroke}" stroke-width="2"/>`);
  text(x + 24, y + 42, title, 29, titleColor, true);
  lines.forEach((line, i) => text(x + 24, y + 82 + i * 36, line, size));
}
function arrow(d, color = '#64748b', dashed = false) {
  parts.push(`<path d="${d}" fill="none" stroke="${color}" stroke-width="3" ${dashed ? 'stroke-dasharray="9 7"' : ''} marker-end="url(#${color === '#e11d48' ? 'redArrow' : 'arrow'})"/>`);
}

text(60, 58, '指尖数据怎样到达选择算法：硬件 → Oculus SDK → MRTK → Study3', 39, '#0f172a', true);
text(60, 102, '当前项目代码路径；F = 实际应用帧率，P = 实际 FixedUpdate 回调率（稳定、timeScale=1 时约 50 Hz）', 24, '#475569');
text(60, 137, '区间按稳定运行时每只手、每条路径统计；0 表示停更或无有效数据。调用、缓存发布、独立物理新采样分别计数。', 23, '#64748b');

box(60, 180, 850, 130, '① 头显摄像头｜真实手指 → 图像', [
  '图像是系统手追踪的输入，应用当前没有直接读取原始摄像头帧。',
  '有效输入下限：0 Hz；本设备 / 当前模式的摄像头上限：未核实。',
], 'hardware', 24);
arrow('M485 310 V342');
box(60, 350, 850, 165, '② Meta 系统 Runtime｜图像 → 估计的手部关节姿态', [
  '本项目请求 LOW 档；实际有效新姿态率：下限 0，上限待实测。',
  '官方 HIGH 档目标约 60 Hz，仅作能力参考，非当前实测上限。',
  '跟踪算法输出、预测姿态与原始摄像头帧并非一一对应。',
], 'hardware', 24);
arrow('M485 515 V547');
box(60, 555, 850, 200, '③ Oculus SDK｜OVRPlugin.GetHandState → OVRHand', [
  'Render：每个 Update 读取一次；成功读取数 0～F 次/秒。',
  'Physics：仅非 OpenXR 后端每个固定步读取；0～P 次/秒。',
  '合计上限：OpenXR 为 F；非 OpenXR 为 F + P。',
  '读取保存到 _handState；再次成功读取不保证新的物理采样。',
], 'sdk', 24);
arrow('M485 755 V787');
box(60, 795, 850, 130, '④ Oculus SDK｜OVRSkeleton 应用骨架 Transform', [
  'Update + FixedUpdate 使用 OVRHand 已保存的数据应用骨架。',
  '成功应用次数：0～(F + P) 次/秒；这一步不会额外采集摄像头帧。',
], 'sdk', 24);
arrow('M485 925 V957');
box(60, 965, 850, 165, '⑤ MRTK｜OculusHand.jointPoses 关节缓存', [
  'DeviceManager.Update → UpdateHandData（按应用帧更新）。',
  '复制骨架关节位置，再发布 HandJointsUpdated 事件。',
  '每只手更新次数：0～F 次/秒；Physics 读取不会自动刷新此字典。',
], 'mrtk', 23);

box(970, 180, 770, 240, 'Unity 引擎：安排调用时机，不产生硬件采样', [
  'Update：每个应用帧一次；实际频率记作 F。',
  'FixedUpdate：按固定步累计补跑；实际频率记作 P。',
  '当前请求 50 Hz，每个模拟步长为 20 ms。',
  '暂停 / 卡顿时可能停更，也可能同帧补跑多个固定步。',
  '50 Hz 是模拟目标，不是瞬时调用频率的硬上限。',
], 'neutral', 24);
box(970, 455, 770, 275, '数字示例｜假设 F = 70、P = 50 次/秒', [
  'Oculus SDK 成功读取：OpenXR 0～70 次/秒；',
  '非 OpenXR 0～120 次/秒。',
  '骨架应用：0～120 次/秒；MRTK 发布：0～70 次/秒。',
  '旧运动计算：0～50 次/秒，其中可能反复用同一缓存。',
  '共享管线接受新样本：0～50 次/秒，受两端频率约束。',
  '以上都是软件次数，不代表摄像头或物理新采样数。',
], 'neutral', 24);
box(970, 765, 770, 200, '已知设置与实测范围', [
  'Pilot 三个会话平均应用帧率：69.84～70.01 FPS。',
  '这是会话均值范围，不是瞬时帧率的上下限。',
  'samplingRateHz 代码下限 1 Hz，无显式最大值；',
  '实际执行仍受 Unity、设备性能与 timeScale 限制。',
], 'neutral', 23);
box(970, 1000, 770, 165, '同一 Update 阶段的静态执行顺序', [
  'MRTK(-100) → OVRHand(-90) → OVRSkeleton(-80)',
  'MRTK 可能先读取本帧 Render 刷新前的骨架。',
  'Physics 分支可能提前刷新；额外延迟需实机核验。',
], 'neutral', 24);

arrow('M485 1130 V1180 H330 V1192');
arrow('M485 1180 H900 V1192');
arrow('M900 1180 H1470 V1192');
box(60, 1200, 540, 245, '⑥A 旧运动计算：AcceStudy3', [
  'TryGetJointPose(IndexTip) → 读缓存。',
  '有效计算次数：0～P，稳定时约 50 Hz。',
  'v = 位移 / 20 ms；a = 速度差 / 20 ms。',
  '同一缓存可被当作新位置再次参与计算。',
  '用于原版 Magic Tap、Speed 等。',
], 'warn', 23);
box(630, 1200, 540, 245, '⑥B Magic Tap 2.0 / IxDL 共享运动', [
  'MRTK 事件 → SourceId / Sequence / 接收时间。',
  'FixedUpdate 读取；重复序号跳过运动计算。',
  '稳态接受率：0～min(F, P) 次/秒。',
  '速度使用被接受样本的接收时间差。',
  '仍不等于已按硬件物理帧去重。',
], 'app', 23);
box(1200, 1200, 540, 245, '⑥C Update 路径：DynamicStop 等', [
  '主循环每帧执行；单条轮询路径 0～F。',
  'TryGetJointPose 返回 MRTK 已知的指尖。',
  'DynamicStop、Fast Tap、Pinch 有此类路径。',
  '同帧可读多关节、多目标；总查询可高于 F。',
  '各路径并未统一做新采样身份判定。',
], 'app', 23);

box(60, 1490, 1680, 205, '旧问题发生在 MRTK 缓存 → 旧运动计算之间', [], 'warn');
box(90, 1558, 450, 90, 'Fixed 第一次：读取位置 A', [], 'neutral');
box(665, 1558, 450, 90, 'Fixed 第二次：仍读 A，v = 0', [], 'warn');
box(1240, 1558, 470, 90, '随后 MRTK.Update 刷新缓存', [], 'mrtk');
arrow('M540 1603 H657', '#e11d48');
arrow('M1115 1603 H1232', '#e11d48');
text(90, 1680, '即使 Physics 已更新 SDK / 骨架，旧主程序通过 MRTK 读取时仍可能拿到同一份缓存。调用成功 ≠ 收到新采样。', 24, '#9f1239');

text(60, 1745, '边界说明：0 是有效数据率的全局下限；无法给出实机保证的非零下限。F/P 随运行变化，节点中的上限是代码调用边界。', 22, '#475569');
text(60, 1780, 'HIGH 的 60 Hz 是官方目标参考，部分地区 / 运行条件可能约 50 Hz；当前旧 SDK + LOW 档不能直接套用这一数值。', 22, '#475569');
text(60, 1815, '依据：本地 SDK / MRTK / Study3 源码；Pilot 三段 Speed 日志；Meta Hand Tracking / Fast Motion Mode 官方文档。2026-10-27', 21, '#64748b');

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1800" height="1850" viewBox="0 0 1800 1850">
<defs><marker id="arrow" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9" fill="#64748b"/></marker><marker id="redArrow" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9" fill="#e11d48"/></marker></defs>
<rect width="1800" height="1850" fill="#fff"/><g font-family="Microsoft YaHei,SimHei,Arial,sans-serif">${parts.join('')}</g></svg>`;
const stem = path.join(out, 'hardware_sdk_mrtk_study3_pipeline');
fs.writeFileSync(stem + '.svg', svg);
sharp(Buffer.from(svg)).png().toFile(stem + '.png').then(() => process.stdout.write(stem + '.png\n'));

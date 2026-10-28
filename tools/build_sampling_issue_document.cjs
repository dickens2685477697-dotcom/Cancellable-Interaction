const fs = require('node:fs');
const path = require('node:path');
const {createRequire} = require('node:module');
const bundle = createRequire('C:/Users/tangx/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/package.json');
const sharp = bundle('sharp');
const out = 'D:/HLStudy4_22/output/sampling_issue_feishu_20261027';
fs.mkdirSync(out, {recursive:true});
const esc = s => s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');
const parts = [];
function box(x,y,w,h,title,lines,red=false){
  parts.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="14" fill="${red?'#fff1f2':'#f1f5f9'}" stroke="${red?'#e11d48':'#94a3b8'}" stroke-width="2"/>`);
  parts.push(`<text x="${x+24}" y="${y+38}" font-size="25" font-weight="700" fill="${red?'#be123c':'#0f172a'}">${esc(title)}</text>`);
  lines.forEach((line,i)=>parts.push(`<text x="${x+24}" y="${y+74+i*32}" font-size="22" fill="${red?'#9f1239':'#334155'}">${esc(line)}</text>`));
}
function arrow(d,red=false){parts.push(`<path d="${d}" fill="none" stroke="${red?'#e11d48':'#475569'}" stroke-width="3" ${red?'stroke-dasharray="8 6"':''} marker-end="url(#${red?'redArrow':'arrow'})"/>`);}
box(40,110,620,100,'MRTK 缓存关节位姿',['接收新姿态的节奏独立于 FixedUpdate']);
box(40,260,620,115,'AcceStudy3.FixedUpdate()',['HandJointUtils.TryGetJointPose(IndexTip)','默认每 20 ms 轮询一次缓存位置']);
box(40,425,620,115,'用固定步长计算运动',['v = ||p₂ − p₁|| / fixedDeltaTime','a = (v − vprev) / fixedDeltaTime']);
arrow('M350 210 V250'); arrow('M350 375 V415');
box(740,110,620,175,'问题入口：没有核验手部更新身份',['同一渲染帧可能执行多个 FixedUpdate','缓存尚未更新，也再次参与计算','内部固定步序号 ≠ MRTK 更新序号'],true);
arrow('M740 205 H705 V315 H670',true);
box(740,405,620,135,'同一旧位置被再次用于差分',['p₂ = p₁ → v = 0','若 vprev > 0，则 a < 0（突然减速）'],true);
arrow('M740 470 H670',true);
arrow('M350 540 V580 H350 V620');
arrow('M350 580 H1050 V620');
box(40,630,620,155,'8–11 Speed 系列',['InEvent → S2AccMode 回调','CheckSpeedReleaseIndexTrigger()','v ≤ Speed Min Threshold → armed']);
box(740,630,620,155,'原版 Magic Tap',['Acce = true → JudgeAcce()','满足低速 A 或减速 B 条件','→ Prepared']);
arrow('M350 785 V810'); arrow('M1050 785 V810');
box(40,820,620,110,'后续高速达到门限时确认',['11号还要求方向就绪且在取消方向外']);
box(740,820,620,110,'通过后续确认时间与取消检查',['HesEvent → SetHesMode → CompleteSelection']);
parts.push('<rect x="40" y="980" width="1320" height="90" rx="14" fill="#fff1f2" stroke="#e11d48" stroke-width="2"/>');
parts.push('<text x="65" y="1018" font-size="25" font-weight="700" fill="#be123c">风险：准备状态可能由重复读取产生的零速 / 减速建立</text>');
parts.push('<text x="65" y="1050" font-size="22" fill="#9f1239">这种记录不能直接作为新的物理停手证据；进入准备也不等于必然触发。</text>');
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1400" height="1100" viewBox="0 0 1400 1100"><defs><marker id="arrow" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9" fill="#475569"/></marker><marker id="redArrow" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9" fill="#e11d48"/></marker></defs><rect width="1400" height="1100" fill="white"/><g font-family="Microsoft YaHei,SimHei,Arial,sans-serif"><text x="40" y="55" font-size="32" font-weight="700" fill="#0f172a">旧代码调用链与重复采样问题</text><text x="40" y="88" font-size="21" fill="#64748b">原版 Magic Tap 与 Speed 系列；红色标出旧逻辑的问题路径</text>${parts.join('')}</g></svg>`;
fs.writeFileSync(path.join(out,'old_sampling_logic.svg'),svg);

const run = (text,bold=false,link) => ({text_run:{content:text,text_element_style:{...(bold?{bold:true}:{}),...(link?{link:{url:link}}:{})}}});
const text = (...elements) => ({block_type:2,text:{elements:elements.map(e=>typeof e==='string'?run(e):e)}});
const heading = content => ({block_type:4,heading2:{elements:[run(content)]}});
const bullet = content => ({block_type:12,bullet:{elements:[run(content)]}});
const blocks = [
  text('本问题是在我做动态阈值与 Magic Tap 2.0 比较实验时发现的。',run('结论：旧代码可能重复读取缓存手部位姿，并把它计算成零速度或突然减速，进而影响选择判定。',true),'Pilot 日志已观察到它改变 Speed 系列的低速准备；原版 Magic Tap 也存在同类代码路径。'),
  heading('1. 旧代码调用逻辑与问题'),
  {block_type:27,image:{width:1400,height:1100}},
  text('固定回调 50 Hz（20 ms）不等于每秒收到 50 次新的手部更新。旧代码没有核验 MRTK 更新身份；同一缓存位置再次参与差分时，速度为 0，前次速度非零时还会得到负加速度。'),
  heading('2. 现有数据中的问题'),
  text(run('发现背景：2026-10-25 旧实现对比实验。',true),'动态模式正常选择有 16/48 次、球内持续画圈有 44/48 次在零速记录上触发；这 60 次均呈现同渲染帧、连续固定步位置重复。当时 Magic Tap 2.0 会跳过不足 1 mm 位移，两者预处理不同，旧结果不能单独归因于动态阈值策略。'),
  text(run('Pilot：2号，2026-10-27，三种技术各48次成功选择。',true),'依次列出“同帧重复位置零速 / 在该记录上进入准备 / 随后本次进入触发”：'),
  bullet('8 Speed：20次 / 12次 / 11次。'),
  bullet('9 Speed-Dwell：14次 / 5次 / 5次。'),
  bullet('10 Speed-Release：23次 / 10次 / 9次。'),
  text('合计 57/3,963 个球内运动测量（1.44%）呈现上述重复零速；',run('27/167 次低速准备（16.2%）在这些记录上建立，25/144 次成功选择（17.4%）随后由该次进入触发。',true)),
  text(run('17.4% 不是误触率。',true),'日志确认的是判定路径，不能推断过滤后这25次会失败或反应时间会改变多少。同帧证据缺少硬件采样时间戳，不能逐条证明传感器内部状态。'),
  text('较早的1、2、3、5号共2,256个 Trigger 试次中，1,259个（55.8%）出现相邻位置相同；这些日志缺少更新身份信息，也可能包含真实静止，不能直接当作旧采样读取率。'),
  heading('3. 影响范围与处理现状'),
  text('优先处理 8–11 Speed 系列及原版 Magic Tap。3、7、14 的 Direction 机制需复核方向窗口与取消时机；尚无依据把15种机制全部判为同等受影响。'),
  text('当前 Magic Tap 2.0 与 IxDL Dynamic 已采用共同的应用更新序号去重；其他正式技术尚未全部统一。下一步应统一新样本识别与实际时间间隔，同时保留各技术的触发、取消和计时条件，并在设备上复测。'),
  text('依据：',run('旧实现比较报告',false,'https://kvxm3gyd38.feishu.cn/docx/BHzTdceBNoTTF3xrMz8ci5oknxu'),'；',run('共同运动规则修订',false,'https://kvxm3gyd38.feishu.cn/docx/Krvid94LAoLCVjxupfDc5TYkn8f'),'；Pilot 原始 speed-boundary-debug：20261027-11-14-50、11-41-48、11-47-05。统计核对日期：2026-10-27。')
];
fs.writeFileSync(path.join(out,'document_blocks.json'),JSON.stringify(blocks,null,2));
const plain = element => element.text_run?.content || '';
const markdown = blocks.map(block=>block.block_type===27
  ? '![旧代码调用链与重复采样问题](D:/HLStudy4_22/output/sampling_issue_feishu_20261027/old_sampling_logic.png)'
  : (block.block_type===4?'## ':block.block_type===12?'- ':'')
    +(block.text||block.heading2||block.bullet).elements.map(plain).join('')).join('\n\n');
fs.writeFileSync(path.join(out,'重复手部采样问题.md'),'# 重复手部采样问题：旧调用逻辑与 Pilot 证据\n\n'+markdown+'\n');
fs.writeFileSync(path.join(out,'create_document.json'),JSON.stringify({data:{title:'重复手部采样问题：旧调用逻辑与 Pilot 证据'},useUAT:true},null,2));
sharp(Buffer.from(svg)).png().toFile(path.join(out,'old_sampling_logic.png')).then(info=>console.log(JSON.stringify({blocks:blocks.length,image:info}))).catch(e=>{console.error(e.message);process.exitCode=1;});

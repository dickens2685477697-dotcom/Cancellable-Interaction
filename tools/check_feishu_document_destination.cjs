const fs = require('node:fs');
const path = require('node:path');
const pkg = 'C:/Users/tangx/AppData/Local/Packages/OpenAI.Codex_2p2nqsd0c76g0/LocalCache/Local/CodexFeishuMcp/node_modules/.pnpm/@larksuiteoapi+lark-mcp@0.5.1_debug@4.4.3_zod@4.6.7/node_modules/@larksuiteoapi/lark-mcp';
const {authStore} = require(path.join(pkg,'dist/auth/store.js'));
(async()=>{
  const token = await authStore.getLocalAccessToken(process.env.APP_ID);
  if (!token) throw new Error('No existing Feishu user login.');
  const response = await fetch('https://open.feishu.cn/open-apis/authen/v1/user_info', {
    headers: {Authorization: 'Bearer '+token}
  });
  const result = await response.json();
  const safe = {code:result.code, message:result.msg, name:result.data?.name,
    tenantKey:result.data?.tenant_key, openId:result.data?.open_id};
  fs.writeFileSync('D:/HLStudy4_22/output/sampling_issue_feishu_20261027/destination_identity.json',JSON.stringify(safe,null,2));
  console.log(JSON.stringify(safe));
  process.exit(result.code===0?0:1);
})().catch(e=>{console.error(e.message);process.exit(1);});

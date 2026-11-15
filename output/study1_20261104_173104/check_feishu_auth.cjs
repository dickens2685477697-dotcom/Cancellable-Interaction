const path=require('path');
const p='C:/Users/tangx/AppData/Local/Packages/OpenAI.Codex_2p2nqsd0c76g0/LocalCache/Local/CodexFeishuMcp/node_modules/.pnpm/@larksuiteoapi+lark-mcp@0.5.1_debug@4.4.3_zod@4.6.7/node_modules/@larksuiteoapi/lark-mcp';
const {authStore}=require(path.join(p,'dist/auth/store.js'));
(async()=>{ const t=await authStore.getLocalAccessToken(process.env.APP_ID); const i=t?await authStore.getToken(t):null; console.log(JSON.stringify({hasToken:!!t,hasRecord:!!i,expired:i?i.expiresAt<=Date.now()/1000:null,hasRefreshToken:!!i?.extra?.refreshToken}));authStore.destroy(); })().catch(e=>{console.error('Token status unavailable');process.exitCode=1});

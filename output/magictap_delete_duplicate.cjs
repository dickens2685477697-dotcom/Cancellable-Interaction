const fs=require('node:fs');
const path=require('node:path');
const {createRequire}=require('node:module');
const pkg='C:/Users/tangx/AppData/Local/Packages/OpenAI.Codex_2p2nqsd0c76g0/LocalCache/Local/CodexFeishuMcp/node_modules/.pnpm/@larksuiteoapi+lark-mcp@0.5.1_debug@4.4.3_zod@4.6.7/node_modules/@larksuiteoapi/lark-mcp';
const req=createRequire(path.join(pkg,'package.json'));
const noLog={info(){},error(){},warn(){},debug(){},trace(){}};
let token;
async function main(){
 Object.assign(require(path.join(pkg,'dist/utils/logger.js')).logger,noLog);
 const {authStore}=require(path.join(pkg,'dist/auth/store.js'));
 token=await authStore.getLocalAccessToken(process.env.APP_ID); const info=token&&await authStore.getToken(token); authStore.destroy();
 if(!token||!info||info.expiresAt<=Date.now()/1000) throw Error('User authorization unavailable');
 const sdk=req('@larksuiteoapi/node-sdk'); const api=new sdk.Client({appId:process.env.APP_ID,appSecret:process.env.APP_SECRET,logger:noLog}); const user=sdk.withUserAccessToken(token);
 const id='R5syd6LHqoA7Wfx3VA1cgZZEnPg';
 const expected=[['doxcnlXEwCOcYNFes0qwvmTSNhd','doxcngTwt5YYWLISeVqzAl6CnFJ','doxcn8Qucl1lE656S7fzqkbuRBc','doxcniKfws4JOw5cDdJamMHur7d'],['doxcnxmnPUghELSx2YmaqqfrODd','doxcntGaSuMZCPs70zjnoccnoZf','doxcnTFsnnSEOeotTzd0e5DjLfg','doxcn1XjZueW0faSwV66gAdicGg']];
 const call=async f=>{const r=await f();if(r.code!==undefined&&r.code!==0)throw Error('Feishu error '+r.code+' '+r.msg);return r.data||r};
 for(const group of expected){
  const x=await call(()=>api.docx.documentBlockChildren.get({path:{document_id:id,block_id:id},params:{page_size:100}},user));
  const start=x.items.findIndex(b=>b.block_id===group[0]);
  if(start<0||group.some((b,i)=>x.items[start+i]?.block_id!==b))throw Error('Original text blocks changed; stopping');
  await call(()=>api.docx.documentBlockChildren.batchDelete({path:{document_id:id,block_id:id},data:{start_index:start,end_index:start+group.length}},user));
  console.log('Removed verified duplicate lines '+group.length);
 }
 const x=await call(()=>api.docx.documentBlockChildren.get({path:{document_id:id,block_id:id},params:{page_size:100}},user));
 if(expected.flat().some(id=>x.items.some(b=>b.block_id===id)))throw Error('Duplicate text remains');
 if(!['doxcnh94F4QOcKulBesjIaauKth','doxcnrPTK848SnHjqfQSRYEnkMc'].every(id=>x.items.some(b=>b.block_id===id)))throw Error('Table missing');
 console.log('Verified both tables remain and duplicate text is removed.');
}
main().catch(e=>{console.error(String(e.message).replaceAll(token||'__TOKEN__','[redacted]').replaceAll(process.env.APP_SECRET||'__SECRET__','[redacted]'));process.exitCode=1});

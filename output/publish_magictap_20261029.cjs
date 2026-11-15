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
  token=await authStore.getLocalAccessToken(process.env.APP_ID);
  let info=token&&await authStore.getToken(token);
  if(info?.extra?.refreshToken && info.expiresAt<=Date.now()/1000){
    const variant=info.extra.token?.data?'oidc':'oauth';
    const mod=require(path.join(pkg,'dist/auth/provider',variant+'.js'));
    const Provider=variant==='oidc'?mod.LarkOIDC2OAuthServerProvider:mod.LarkOAuth2OAuthServerProvider;
    const provider=new Provider({appId:process.env.APP_ID,appSecret:process.env.APP_SECRET,domain:'https://open.feishu.cn',host:'localhost',port:3000,callbackUrl:'http://localhost:3000/callback'});
    const renewed=await provider.exchangeRefreshToken({client_id:info.clientId,redirect_uris:[]},info.extra.refreshToken,info.scopes);
    await authStore.storeLocalAccessToken(renewed.access_token,process.env.APP_ID);
    token=renewed.access_token; info=await authStore.getToken(token);
  }
  authStore.destroy();
  if(!token||!info||info.expiresAt<=Date.now()/1000) throw Error('User authorization unavailable');
  const sdk=req('@larksuiteoapi/node-sdk');
  const api=new sdk.Client({appId:process.env.APP_ID,appSecret:process.env.APP_SECRET,logger:noLog});
  const user=sdk.withUserAccessToken(token);
  const receipt='D:/HLStudy4_22/output/magictap_feishu_receipt.json';
  const title='MagicTap 两组实验合并分析（2026-10-29）';
  let state=fs.existsSync(receipt)?JSON.parse(fs.readFileSync(receipt,'utf8')):{};
  const invoke=async(f)=>{const x=await f();if(x.code!==undefined&&x.code!==0)throw Error('Feishu error '+x.code+' '+x.msg);return x.data||x};
  if(!state.document_id){
    const created=await invoke(()=>api.docx.document.create({data:{title}},user));
    state.document_id=created.document.document_id;state.url='https://kvxm3gyd38.feishu.cn/docx/'+state.document_id;
    fs.writeFileSync(receipt,JSON.stringify(state,null,2));
  }
  const source=fs.readFileSync('D:/HLStudy4_22/output/magictap_20261029_合并分析.md','utf8').replace(/^\uFEFF/,'');
  const lines=source.split(/\r?\n/).slice(1).filter(x=>x.trim()&&!/^\|[\s:|-]+\|$/.test(x.trim()));
  const blocks=lines.map(line=>{
    const heading=line.match(/^(#{2,3})\s+(.*)$/);
    let content=heading?heading[2]:line.replace(/^[-] /,'• ').replace(/\*\*/g,'');
    if(content.startsWith('|')) content=content.slice(1,-1).split('|').map(x=>x.trim()).join('　｜　');
    const key=heading?(heading[1].length===2?'heading2':'heading3'):'text';
    return {block_type:key==='heading2'?4:key==='heading3'?5:2,[key]:{elements:[{text_run:{content}}]}};
  });
  const id=state.document_id;
  const read=async()=>{let items=[],page;do{const x=await invoke(()=>api.docx.documentBlockChildren.get({path:{document_id:id,block_id:id},params:{page_size:500,...(page?{page_token:page}:{})}},user));items.push(...x.items);page=x.has_more?x.page_token:undefined}while(page);return items};
  const textOf=b=>Object.values(b).find(x=>x&&Array.isArray(x.elements))?.elements.map(e=>e.text_run?.content||'').join('')||'';
  let actual=await read();
  if(actual.length>blocks.length)throw Error('Unexpected existing content');
  actual.forEach((x,i)=>{if(x.block_type!==blocks[i].block_type||textOf(x)!==textOf(blocks[i]))throw Error('Existing content differs at '+i)});
  for(let i=actual.length;i<blocks.length;i+=20){
    await invoke(()=>api.docx.documentBlockChildren.create({path:{document_id:id,block_id:id},data:{index:i,children:blocks.slice(i,i+20)}},user));
  }
  actual=await read();
  if(actual.length!==blocks.length)throw Error('Readback count mismatch');
  actual.forEach((x,i)=>{if(x.block_type!==blocks[i].block_type||textOf(x)!==textOf(blocks[i]))throw Error('Readback differs at '+i)});
  const doc=await invoke(()=>api.docx.document.get({path:{document_id:id}},user));
  if(doc.document.title!==title)throw Error('Title mismatch');
  state.verified_blocks=actual.length;state.verified=true;fs.writeFileSync(receipt,JSON.stringify(state,null,2));
  console.log(JSON.stringify(state));
}
main().catch(e=>{console.error(String(e.message).replaceAll(token||'__TOKEN__','[redacted]').replaceAll(process.env.APP_SECRET||'__SECRET__','[redacted]'));process.exitCode=1});

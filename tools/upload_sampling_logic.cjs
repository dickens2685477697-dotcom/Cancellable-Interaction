const fs = require('node:fs');
const path = require('node:path');
const pkg = 'C:/Users/tangx/AppData/Local/Packages/OpenAI.Codex_2p2nqsd0c76g0/LocalCache/Local/CodexFeishuMcp/node_modules/.pnpm/@larksuiteoapi+lark-mcp@0.5.1_debug@4.4.3_zod@4.6.7/node_modules/@larksuiteoapi/lark-mcp';
const {authStore} = require(path.join(pkg,'dist/auth/store.js'));
async function main(){
  try {
    const token = await authStore.getLocalAccessToken(process.env.APP_ID);
    if(!token) throw new Error('No existing Feishu user login.');
    const filePath = 'D:/HLStudy4_22/output/sampling_issue_feishu_20261027/old_sampling_logic.png';
    const file = fs.readFileSync(filePath);
    const form = new FormData();
    form.append('file_name',path.basename(filePath));
    form.append('parent_type','docx_image');
    form.append('parent_node','doxcnIEekQyrFVTCGRg0Jwp65iL');
    form.append('size',String(file.length));
    form.append('file',new Blob([file],{type:'image/png'}),path.basename(filePath));
    const response=await fetch('https://open.feishu.cn/open-apis/drive/v1/medias/upload_all',{
      method:'POST',headers:{Authorization:'Bearer '+token},body:form});
    const result=await response.json();
    fs.writeFileSync('D:/HLStudy4_22/output/sampling_issue_feishu_20261027/upload_receipt.json',JSON.stringify(result,null,2));
    console.log(JSON.stringify({status:response.status,code:result.code,message:result.msg,fileToken:result.data?.file_token}));
    if(result.code!==0) process.exitCode=1;
  } finally {authStore.destroy();}
}
main().catch(error=>{console.error(error.message);process.exitCode=1;});

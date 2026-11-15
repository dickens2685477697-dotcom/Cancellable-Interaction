/* Upload an image to the already-authorized document block using existing login.
 * This fills the connected tool's missing multipart upload capability.
 */
const fs = require('node:fs');
const path = require('node:path');
const pkg = 'C:/Users/tangx/AppData/Local/Packages/OpenAI.Codex_2p2nqsd0c76g0/LocalCache/Local/CodexFeishuMcp/node_modules/.pnpm/@larksuiteoapi+lark-mcp@0.5.1_debug@4.4.3_zod@4.6.7/node_modules/@larksuiteoapi/lark-mcp';
const { authStore } = require(path.join(pkg, 'dist/auth/store.js'));
async function main() {
  try {
    const [imagePath, imageBlock, receiptPath] = process.argv.slice(2);
    if (!imagePath || imageBlock !== 'doxcnIEekQyrFVTCGRg0Jwp65iL' || !receiptPath) throw new Error('Expected the authorized existing image block and local PNG/receipt paths');
    const localTokens = await authStore.getAllLocalAccessTokens();
    const appIds = Object.keys(localTokens);
    const appId = process.env.APP_ID || (appIds.length === 1 ? appIds[0] : undefined);
    if (!appId) throw new Error(`Cannot select the existing Feishu app (${appIds.length} cached app identities)`);
    const token = await authStore.getLocalAccessToken(appId);
    if (!token) throw new Error('No existing Feishu user login');
    const prior = JSON.parse(fs.readFileSync('D:/HLStudy4_22/output/sampling_issue_feishu_20261027/destination_identity.json', 'utf8'));
    const identityResponse = await fetch('https://open.feishu.cn/open-apis/authen/v1/user_info', { headers: { Authorization: 'Bearer ' + token } });
    const identity = await identityResponse.json();
    if (identity.code !== 0 || identity.data?.open_id !== prior.openId || identity.data?.tenant_key !== prior.tenantKey) throw new Error('Current login does not match the previously authorized Feishu account');
    const bytes = fs.readFileSync(imagePath);
    const form = new FormData();
    form.append('file_name', path.basename(imagePath)); form.append('parent_type', 'docx_image');
    form.append('parent_node', imageBlock); form.append('size', String(bytes.length));
    form.append('file', new Blob([bytes], { type: 'image/png' }), path.basename(imagePath));
    const response = await fetch('https://open.feishu.cn/open-apis/drive/v1/medias/upload_all', { method: 'POST', headers: { Authorization: 'Bearer ' + token }, body: form });
    const result = await response.json();
    if (!response.ok || result.code !== 0) throw new Error(`Image upload failed: HTTP ${response.status}, code ${result.code}, ${result.msg}`);
    fs.writeFileSync(receiptPath, JSON.stringify(result, null, 2) + '\n');
    console.log(JSON.stringify({ account: identity.data.name, code: result.code, fileToken: result.data.file_token }));
  } finally { authStore.destroy(); }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });

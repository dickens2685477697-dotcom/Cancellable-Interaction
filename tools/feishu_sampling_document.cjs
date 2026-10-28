'use strict';
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const pkg = 'C:/Users/tangx/AppData/Local/Packages/OpenAI.Codex_2p2nqsd0c76g0/LocalCache/Local/CodexFeishuMcp/node_modules/.pnpm/@larksuiteoapi+lark-mcp@0.5.1_debug@4.4.3_zod@4.6.7/node_modules/@larksuiteoapi/lark-mcp';
const req = createRequire(path.join(pkg, 'package.json'));
const { Client } = req('@modelcontextprotocol/sdk/client/index.js');
const { StdioClientTransport } = req('@modelcontextprotocol/sdk/client/stdio.js');

async function main() {
  const transport = new StdioClientTransport({ command: process.execPath,
    args: [path.join(pkg, 'dist/cli.js'), 'mcp', '--oauth', '--token-mode', 'user_access_token',
      '--language', 'zh', '--scope', 'docx:document wiki:wiki:readonly offline_access', '--tools',
      ['docx.v1.document.create', 'docx.v1.document.convert', 'drive.v1.media.uploadAll',
       'docx.v1.documentBlockChildren.create', 'docx.v1.documentBlock.batchUpdate',
       'docx.v1.documentBlock.list', 'docx.v1.document.rawContent'].join(',')],
    stderr: 'pipe', env: process.env });
  const client = new Client({ name: 'study3-sampling-document', version: '1.0.0' });
  try {
    await client.connect(transport);
    const result = process.argv[2] === 'list-tools' ? await client.listTools()
      : await client.callTool({ name: process.argv[2],
        arguments: JSON.parse(fs.readFileSync(process.argv[3], 'utf8').replace(/^\uFEFF/, '')) }, undefined, { timeout: 60000 });
    if (process.argv[4]) fs.writeFileSync(process.argv[4], JSON.stringify(result, null, 2));
    else console.log(JSON.stringify(result));
    if (result.isError) process.exitCode = 2;
  } finally {
    await client.close();
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });

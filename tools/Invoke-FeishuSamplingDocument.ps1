param([Parameter(Mandatory=$true)][string]$Tool,[string]$ArgumentsPath,[string]$OutputPath)
$ErrorActionPreference = 'Stop'
$feishuCredential = Import-Clixml -LiteralPath 'C:\Users\tangx\.codex\integrations\feishu\credentials.xml'
$env:APP_ID = $feishuCredential.UserName
$env:APP_SECRET = $feishuCredential.GetNetworkCredential().Password
try {
    & 'C:\Users\tangx\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' (Join-Path $PSScriptRoot 'feishu_sampling_document.cjs') $Tool $ArgumentsPath $OutputPath
    $publishExitCode = $LASTEXITCODE
} finally {
    $env:APP_ID = $null
    $env:APP_SECRET = $null
    $feishuCredential = $null
}
exit $publishExitCode

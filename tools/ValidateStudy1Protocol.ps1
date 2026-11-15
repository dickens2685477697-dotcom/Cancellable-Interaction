$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Join-Path $PSScriptRoot 'tests\Study1ProtocolStubs.cs'),
 (Join-Path $projectRoot 'Assets\Scripts\Study1\ExperimentCounter.cs'),
 (Join-Path $PSScriptRoot 'tests\Study1ProtocolChecks.cs')) -CompilerOptions '/nowarn:0414'
$count = [Study1ProtocolChecks]::Run()
Write-Output "Study1 protocol validation passed: $count assertions (isolated replay; not Unity or headset)."

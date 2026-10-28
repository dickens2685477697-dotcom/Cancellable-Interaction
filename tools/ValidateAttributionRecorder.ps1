$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $projectRoot 'Assets\Scripts\Study3'
$folder = Join-Path $projectRoot ('Temp\AttributionRecorderFileTest-' + [guid]::NewGuid().ToString('N'))
Add-Type -Path @(
 (Join-Path $PSScriptRoot 'tests\AttributionRecorderStubs.cs'),
 (Join-Path $sourceRoot 'Study3LogEncoding.cs'),
 (Join-Path $sourceRoot 'Study3LegacyTrialCsv.cs'),
 (Join-Path $sourceRoot 'Study3AttributionLedger.cs'),
 (Join-Path $sourceRoot 'Study3AttributionSettings.cs'),
 (Join-Path $sourceRoot 'Study3AttributionRecorder.cs'),
 (Join-Path $PSScriptRoot 'tests\AttributionRecorderFileChecks.cs')
)
$count = [AttributionRecorderFileChecks]::Run($folder)
Write-Output "Attribution recorder file checks passed: $count assertions; fixtures: $folder"

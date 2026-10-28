$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $projectRoot 'Assets\Scripts\Study3'
Add-Type -Path @(
    (Join-Path $sourceRoot 'Study3LogEncoding.cs'),
    (Join-Path $sourceRoot 'Study3LegacyTrialCsv.cs'),
    (Join-Path $sourceRoot 'Study3AttributionLedger.cs'),
    (Join-Path $PSScriptRoot 'tests\AttributionLoggingChecks.cs')
)
$checkCount = [AttributionLoggingChecks]::Run()
Write-Output "Attribution logging validation passed: $checkCount assertions (observer/CSV regression; not a Quest run)."

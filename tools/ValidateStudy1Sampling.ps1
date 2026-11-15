$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Join-Path $PSScriptRoot 'tests\Study1SamplingStubs.cs'),
 (Join-Path $projectRoot 'Assets\Scripts\Study3\Study3JointSample.cs'),
 (Join-Path $projectRoot 'Assets\AcceStimulate.cs'),
 (Join-Path $projectRoot 'Assets\Scripts\Study1\LogWriter.cs'),
 (Join-Path $PSScriptRoot 'tests\Study1LogChecks.cs'),
 (Join-Path $PSScriptRoot 'tests\Study1SamplingChecks.cs'))
$count = [Study1SamplingChecks]::Run()
$logCount = [Study1LogChecks]::Run()
Write-Output "Study1 sampling validation passed: $count sampling and $logCount CSV assertions (isolated replay; not a Unity or Quest run)."

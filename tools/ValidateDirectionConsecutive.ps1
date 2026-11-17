$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Add-Type -Path @(
    (Join-Path $PSScriptRoot 'tests\DirectionConsecutiveStubs.cs'),
    (Join-Path $projectRoot 'Assets\Scripts\Study3\Study3DirectionConsecutiveTracker.cs'),
    (Join-Path $PSScriptRoot 'tests\DirectionConsecutiveChecks.cs')
)
$count = [DirectionConsecutiveChecks]::Run()
Write-Output "Direction consecutive-angle validation passed: $count assertions (simulated samples, not a device run)."

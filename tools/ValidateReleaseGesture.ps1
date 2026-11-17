$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Add-Type -Path @(
    (Join-Path $PSScriptRoot 'tests\ReleaseGestureStubs.cs'),
    (Join-Path $projectRoot 'Assets\Scripts\Study3\ReleaseGesture.cs'),
    (Join-Path $PSScriptRoot 'tests\ReleaseGestureChecks.cs')
)
$count = [ReleaseGestureChecks]::Run()
Write-Output "Release gesture validation passed: $count assertions (simulated hand poses, not a device run)."

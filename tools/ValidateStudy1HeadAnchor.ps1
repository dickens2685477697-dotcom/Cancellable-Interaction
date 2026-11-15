$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Join-Path $PSScriptRoot 'tests\Study1HeadAnchorStubs.cs'),
 (Join-Path $projectRoot 'Assets\Scripts\Study1\Study1HeadAnchor.cs'),
 (Join-Path $PSScriptRoot 'tests\Study1HeadAnchorChecks.cs'))
$count = [Study1HeadAnchorChecks]::Run()
Write-Output "Study1 head anchor validation passed: $count assertions (isolated replay, not Unity or Quest)."

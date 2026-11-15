$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Add-Type -CompilerOptions '/define:UNITY_ANDROID' -Path @((Join-Path $PSScriptRoot 'tests\Study1HeadAnchorStubs.cs'),
 (Join-Path $projectRoot 'Assets\Scripts\Study1\Study1HeadAnchor.cs'),
 (Join-Path $PSScriptRoot 'tests\Study1HeadAnchorChecks.cs'))
$count = [Study1HeadAnchorChecks]::Run()
Write-Output "Study1 Android head anchor validation passed: $count assertions (isolated stubs, not Unity or Quest)."

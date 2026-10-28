$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testDirectory = Join-Path $projectRoot 'Temp\DynamicStopObservability'
[void](New-Item -ItemType Directory -Path $testDirectory -Force)
$existing = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ValidateCommonMotion.ps1') -Raw
$match = [regex]::Match($existing, '(?s)\$stubs = @''\r?\n(.*?)\r?\n''@')
if (!$match.Success) { throw 'Common motion validation stub block was not found.' }
$stubs = $match.Groups[1].Value.Replace(
    'public static Vector3 zero =>',
    'public static float Distance(Vector3 a,Vector3 b) => (a-b).magnitude; public static Vector3 zero =>'
)
$stubPath = Join-Path $testDirectory 'Stubs.cs'
$stubs | Set-Content -LiteralPath $stubPath -Encoding utf8
Add-Type -Path @($stubPath,
    (Join-Path $projectRoot 'Assets\Scripts\Study3\DynamicStopMotionTracker.cs'),
    (Join-Path $PSScriptRoot 'tests\DynamicStopObservabilityChecks.cs'))
$checks = [DynamicStopObservabilityChecks]::Run()
Write-Output "Dynamic Stop observability passed: $checks assertions (original gate behavior and measurement identity)."

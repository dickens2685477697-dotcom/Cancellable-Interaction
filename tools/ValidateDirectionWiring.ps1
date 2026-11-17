$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
function Get-SpeedDirectionSection([string]$source) {
    $start = $source.IndexOf('    public void SetSpeedDirectionMode()')
    $end = $source.IndexOf('    public void SetSpeedDwellMode()', $start)
    if ($start -lt 0 -or $end -le $start) { throw 'Could not locate Speed-Direction callback.' }
    return $source.Substring($start, $end - $start)
}
$target = Get-SpeedDirectionSection (Get-Content -LiteralPath (Join-Path $root 'Assets\Scripts\Study3\S2AccMode.cs') -Raw -Encoding utf8)
$obstacle = Get-SpeedDirectionSection (Get-Content -LiteralPath (Join-Path $root 'Assets\Scripts\Study3\S2Obstacles.cs') -Raw -Encoding utf8)
$harness = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'tests\DirectionWiringChecks.cs') -Raw -Encoding utf8
$harness = $harness.Replace('/* TARGET_METHOD */',$target).Replace('/* OBSTACLE_METHOD */',$obstacle)
$dir = Join-Path $root 'Temp\DirectionWiringTests'
[void](New-Item -ItemType Directory -Path $dir -Force)
$testPath = Join-Path $dir 'DirectionWiringGenerated.cs'
[System.IO.File]::WriteAllText($testPath,$harness,[System.Text.UTF8Encoding]::new($false))
Add-Type -Path @((Join-Path $PSScriptRoot 'tests\DirectionConsecutiveStubs.cs'),
    (Join-Path $root 'Assets\Scripts\Study3\Study3DirectionConsecutiveTracker.cs'), $testPath)
$count = [DirectionWiringChecks]::Run()
Write-Output "Direction wiring validation passed: $count assertions (actual target/obstacle callbacks with simulated hand updates)."

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$folder = Join-Path $root ('Temp\HandSamplingReplay-' + [guid]::NewGuid().ToString('N'))
Add-Type -Path @(
    (Join-Path $PSScriptRoot 'tests\HandSamplingStubs.cs'),
    (Join-Path $PSScriptRoot 'tests\HandSamplingChecks.cs'),
    (Join-Path $root 'Assets\Oculus\VR\Scripts\Util\OVRHandSamplingMetadata.cs'),
    (Join-Path $root 'Assets\Scripts\Study3\Study3LogEncoding.cs'),
    (Join-Path $root 'Assets\Scripts\Study3\Study3SamplingCorrelation.cs'),
    (Join-Path $root 'Assets\Scripts\Study3\Study3HandSamplingSession.cs'),
    (Join-Path $root 'Assets\Scripts\Study3\Study3HandSamplingDiagnostics.cs')
)
$count = [HandSamplingChecks]::Run($folder)
Write-Output "Hand sampling replay passed: $count assertions. Fixtures: $folder (not a headset test)."

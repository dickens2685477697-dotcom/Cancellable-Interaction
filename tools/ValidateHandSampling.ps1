param([switch]$Android)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$unityData = 'C:\ProgramData\Unity\2020.3.37f1c1\Editor\Data'
$folder = Join-Path $projectRoot ('Temp\HandSamplingValidation' + $(if ($Android) { '-Android' } else { '' }))
[void](New-Item -ItemType Directory -Path $folder -Force)
$compiler = Join-Path $unityData 'Tools\Roslyn\csc.exe'

function Compile-UnityAssembly([string]$assemblyName, [string[]]$additionalSources) {
    [xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot ($assemblyName + '.csproj')) -Raw
    $defines = @($project.SelectNodes('//DefineConstants'))[0].InnerText.Split(';')
    if ($Android) { $defines = @($defines | Where-Object { $_ -notmatch '^UNITY_(EDITOR|STANDALONE)' }) + 'UNITY_ANDROID' + 'ANDROID' }
    $arguments = [System.Collections.Generic.List[string]]::new()
    $arguments.Add('/nologo'); $arguments.Add('/nostdlib+'); $arguments.Add('/unsafe+'); $arguments.Add('/target:library'); $arguments.Add('/langversion:8.0')
    $arguments.Add('/out:"' + (Join-Path $folder ($assemblyName + '.dll')) + '"')
    $arguments.Add('/define:' + ($defines -join ';'))
    $references = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($node in $project.SelectNodes('//Reference/HintPath')) {
        $path = [IO.Path]::GetFullPath((Join-Path $projectRoot $node.InnerText))
        if ([IO.Path]::IsPathRooted($node.InnerText)) { $path = $node.InnerText }
        if (Test-Path -LiteralPath $path) { [void]$references.Add($path) }
    }
    foreach ($dll in Get-ChildItem (Join-Path $projectRoot 'Library\ScriptAssemblies') -Filter '*.dll') {
        if ($dll.BaseName -eq $assemblyName -or $dll.BaseName -match 'Editor' -or $dll.BaseName -eq 'Assembly-CSharp') { continue }
        if ($assemblyName -eq 'Oculus.VR' -and $dll.BaseName -notmatch '^Unity\.') { continue }
        if ($assemblyName -eq 'Assembly-CSharp' -and $dll.BaseName -eq 'Oculus.VR') { continue }
        [void]$references.Add($dll.FullName)
    }
    if ($assemblyName -eq 'Assembly-CSharp') {
        foreach ($path in @($references)) { if ([IO.Path]::GetFileName($path) -eq 'Oculus.VR.dll') { [void]$references.Remove($path) } }
        [void]$references.Add((Join-Path $folder 'Oculus.VR.dll'))
    }
    foreach ($path in $references) { $arguments.Add('/reference:"' + $path + '"') }
    $sources = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($node in $project.SelectNodes('//Compile')) { [void]$sources.Add((Join-Path $projectRoot $node.Include)) }
    foreach ($path in $additionalSources) { [void]$sources.Add($path) }
    foreach ($path in $sources) { $arguments.Add('"' + $path + '"') }
    $responsePath = Join-Path $folder ($assemblyName + '.rsp')
    [IO.File]::WriteAllLines($responsePath, $arguments, [Text.UTF8Encoding]::new($false))
    $output = & $compiler '/noconfig' ('@' + $responsePath) 2>&1
    $exitCode = $LASTEXITCODE
    $output | Set-Content -LiteralPath (Join-Path $folder ($assemblyName + '.log'))
    if ($exitCode -ne 0) { $output | Select-String 'error|错误'; throw "$assemblyName compilation failed ($exitCode); see $folder" }
    Write-Output "$assemblyName compiled: $($sources.Count) sources, $($references.Count) references."
}

Compile-UnityAssembly 'Oculus.VR' @((Join-Path $projectRoot 'Assets\Oculus\VR\Scripts\Util\OVRHandSamplingMetadata.cs'))
$runtimeSources = @(Get-ChildItem (Join-Path $projectRoot 'Assets\Scripts\Study3'),(Join-Path $projectRoot 'Assets\Scripts\fast_tap_thershold') -Filter '*.cs' -Recurse | Where-Object { $_.FullName -notmatch '\\Editor\\' } | ForEach-Object { $_.FullName })
Compile-UnityAssembly 'Assembly-CSharp' $runtimeSources
Write-Output "Hand sampling compilation passed (Android=$Android). Artifacts: $folder"

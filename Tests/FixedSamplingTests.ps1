param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$controllerPath = Join-Path $ProjectRoot 'Assets/Scripts/Study3/Study3FixedSampling.cs'
$controllerSource = Get-Content -Raw -LiteralPath $controllerPath

# Compile the production controller against a small Unity API substitute so
# ownership and timestep restoration can be exercised without a Unity install.
$testSource = $controllerSource + @'

namespace UnityEngine
{
    public class Object { }
    public static class Application { public static bool isPlaying; }
    public static class Time { public static float fixedDeltaTime; }
    public static class Debug
    {
        public static int WarningCount;
        public static void LogWarning(string message, Object context) { WarningCount++; }
    }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : System.Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }
    public sealed class InspectorNameAttribute : System.Attribute
    {
        public InspectorNameAttribute(string name) { }
    }
}

public static class FixedSamplingBehaviorTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new System.Exception(message);
        checks++;
    }
    private static void Step(float expected, string message)
    {
        Check(System.Math.Abs(UnityEngine.Time.fixedDeltaTime - expected) < 0.0000001f, message);
    }
    public static string Run()
    {
        var first = new UnityEngine.Object();
        var second = new UnityEngine.Object();
        UnityEngine.Time.fixedDeltaTime = 0.025f;
        UnityEngine.Application.isPlaying = false;
        Check(!Study3FixedSampling.Apply(first, Study3SamplingMode.Hz30), "Edit Mode must not change time settings");
        Step(0.025f, "Edit Mode changed the timestep");

        UnityEngine.Application.isPlaying = true;
        Check(Study3FixedSampling.Apply(first, Study3SamplingMode.ProjectDefault), "Project Default failed");
        Step(0.025f, "Project Default must preserve the project timestep");
        Check(Study3FixedSampling.Apply(first, Study3SamplingMode.Hz30), "30 Hz failed");
        Step(1f / 30f, "30 Hz did not update the actual fixed timestep");

        Check(Study3FixedSampling.Apply(second, Study3SamplingMode.ProjectDefault), "Other default Spawner failed");
        Step(1f / 30f, "A default Spawner overwrote the active override");
        Check(!Study3FixedSampling.Apply(second, Study3SamplingMode.Hz50), "A conflicting Spawner stole ownership");
        Check(UnityEngine.Debug.WarningCount == 1, "Conflicting overrides must warn");
        Study3FixedSampling.Release(second);
        Step(1f / 30f, "A non-owner restored the timestep");

        Check(Study3FixedSampling.Apply(first, Study3SamplingMode.Hz50), "Owner could not select 50 Hz");
        Step(1f / 50f, "50 Hz did not update the actual fixed timestep");
        Check(Study3FixedSampling.Apply(first, Study3SamplingMode.ProjectDefault), "Owner could not release via Project Default");
        Step(0.025f, "Restore must use the original project timestep, not assume 50 Hz");

        Check(Study3FixedSampling.Apply(second, Study3SamplingMode.Hz30), "New owner could not acquire after release");
        Study3FixedSampling.Release(second);
        Step(0.025f, "Disabling the new owner did not restore the project timestep");
        Check(!Study3FixedSampling.Apply(null, Study3SamplingMode.Hz30), "Null requester was accepted");
        return checks + " fixed sampling behavior checks passed.";
    }
}
'@

Add-Type -TypeDefinition $testSource
[FixedSamplingBehaviorTests]::Run()

# Syntax-check the complete changed scripts, including the existing large
# controllers, using the Roslyn assemblies bundled with PowerShell.
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$scriptPaths = @(
    'Assets/Scripts/Study3/Study3FixedSampling.cs',
    'Assets/Scripts/Study3/BallSpawner.cs',
    'Assets/Scripts/Study3/Editor/BallSpawnerEditor.cs',
    'Assets/Scripts/Study3/LogWriter2.cs',
    'Assets/Scripts/Study3/LogWriter3.cs'
)
foreach ($scriptPath in $scriptPaths) {
    $source = Get-Content -Raw -LiteralPath (Join-Path $ProjectRoot $scriptPath)
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($source)
    $errors = @($tree.GetDiagnostics() | Where-Object { $_.Severity -eq 'Error' })
    if ($errors.Count -gt 0) {
        throw ($scriptPath + ': ' + (($errors | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine))
    }
}
'All 5 changed C# scripts passed Roslyn syntax checks.'

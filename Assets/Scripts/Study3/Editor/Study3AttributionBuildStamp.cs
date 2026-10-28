using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class Study3AttributionBuildStamp : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;
    public void OnPreprocessBuild(BuildReport report) => Refresh();

    [MenuItem("Tools/Study3/Refresh attribution build stamp")]
    public static void Refresh()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        var sources = new SortedDictionary<string, object>(StringComparer.Ordinal);
        var files = new List<string>(Directory.GetFiles(Path.Combine(root, "Assets/Scripts/Study3"), "*.cs", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(Path.Combine(root, "Assets/Scripts/fast_tap_thershold"), "*.cs", SearchOption.AllDirectories));
        files.Add(Path.Combine(root, "Assets/Oculus/VR/Scripts/Util/OVRHand.cs"));
        files.Add(Path.Combine(root, "Assets/Oculus/VR/Scripts/Util/OVRSkeleton.cs"));
        files.Add(Path.Combine(root, "Assets/Oculus/VR/Scripts/Util/OVRHandSamplingMetadata.cs"));
        foreach (string file in files)
        {
            using (var sha = SHA256.Create())
                sources[file.Substring(root.Length + 1).Replace('\\', '/')] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant();
        }
        string json = Study3LogEncoding.Json(new Dictionary<string, object> {
            { "telemetryVersion", Study3AttributionRecorder.Version }, { "generatedUtc", DateTime.UtcNow.ToString("o") },
            { "gitHead", Git(root, "rev-parse HEAD") }, { "gitTrackedStatus", Git(root, "status --porcelain --untracked-files=no") },
            { "sourceSha256", new Dictionary<string, object>(sources) }, { "unityVersion", Application.unityVersion }
        });
        string directory = Path.Combine(root, "Assets/Resources"); Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "Study3AttributionBuild.json"), json, new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset("Assets/Resources/Study3AttributionBuild.json");
    }

    private static string Git(string root, string arguments)
    {
        try
        {
            using (var process = Process.Start(new ProcessStartInfo("git", "-C \"" + root + "\" " + arguments)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
            {
                if (!process.WaitForExit(2000)) { process.Kill(); return "unavailable: git timeout"; }
                return process.ExitCode == 0 ? process.StandardOutput.ReadToEnd().Trim() : "unavailable: git command failed";
            }
        }
        catch { return "unavailable: git not found"; }
    }
}

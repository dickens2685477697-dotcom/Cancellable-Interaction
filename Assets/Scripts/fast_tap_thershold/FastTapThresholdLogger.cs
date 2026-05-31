using System;
using System.Globalization;
using System.IO;
using UnityEngine;

[Serializable]
public struct FastTapTrialResult
{
    public string Phase;
    public int TrialIndex;
    public string TargetColor;
    public int CancelThresholdMs;
    public float UsedTimeMs;
    public bool Success;
    public float TimeInTargetMs;
    public Vector3 FinalIndexPosition;
}

public class FastTapThresholdLogger : MonoBehaviour
{
    private string sessionTimestamp;
    private string logDirectory;
    private string trialLogPath;
    private string trajectoryLogPath;

    public void Initialize(string experimentName)
    {
        sessionTimestamp = DateTime.Now.ToString("yyyyMMdd-HH-mm-ss", CultureInfo.InvariantCulture);
        logDirectory = Path.Combine(Application.persistentDataPath, "Logs", sessionTimestamp);
        Directory.CreateDirectory(logDirectory);

        trialLogPath = Path.Combine(logDirectory, sessionTimestamp + "-" + experimentName + "-trials.csv");
        trajectoryLogPath = Path.Combine(logDirectory, sessionTimestamp + "-" + experimentName + "-trajectory.csv");

        if (!File.Exists(trialLogPath))
        {
            File.WriteAllText(
                trialLogPath,
                "phase,trial_index,target_color,cancel_threshold_ms,used_time_ms,success,time_in_target_ms,final_index_x,final_index_y,final_index_z" + Environment.NewLine);
        }

        if (!File.Exists(trajectoryLogPath))
        {
            File.WriteAllText(
                trajectoryLogPath,
                "phase,trial_index,elapsed_ms,x,y,z" + Environment.NewLine);
        }
    }

    public void WriteTrial(FastTapTrialResult result)
    {
        AppendLine(
            trialLogPath,
            string.Join(",",
                result.Phase,
                result.TrialIndex.ToString(CultureInfo.InvariantCulture),
                result.TargetColor,
                result.CancelThresholdMs.ToString(CultureInfo.InvariantCulture),
                result.UsedTimeMs.ToString("F3", CultureInfo.InvariantCulture),
                result.Success ? "1" : "0",
                result.TimeInTargetMs.ToString("F3", CultureInfo.InvariantCulture),
                result.FinalIndexPosition.x.ToString("F6", CultureInfo.InvariantCulture),
                result.FinalIndexPosition.y.ToString("F6", CultureInfo.InvariantCulture),
                result.FinalIndexPosition.z.ToString("F6", CultureInfo.InvariantCulture)));
    }

    public void WriteTrajectorySample(string phase, int trialIndex, float elapsedMs, Vector3 position)
    {
        AppendLine(
            trajectoryLogPath,
            string.Join(",",
                phase,
                trialIndex.ToString(CultureInfo.InvariantCulture),
                elapsedMs.ToString("F3", CultureInfo.InvariantCulture),
                position.x.ToString("F6", CultureInfo.InvariantCulture),
                position.y.ToString("F6", CultureInfo.InvariantCulture),
                position.z.ToString("F6", CultureInfo.InvariantCulture)));
    }

    public string GetLogDirectory()
    {
        return logDirectory;
    }

    private static void AppendLine(string filePath, string line)
    {
        using StreamWriter writer = new StreamWriter(filePath, true);
        writer.WriteLine(line);
    }
}

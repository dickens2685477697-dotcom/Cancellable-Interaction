using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class AttributionRecorderFileChecks
{
    public static int Run(string directory)
    {
        int checks = 0;
        Action<bool, string> require = (condition, message) => { checks++; if (!condition) throw new Exception(message); };
        Directory.CreateDirectory(directory);
        var host = new BallSpawner { LogSessionTimestamp = "file-test", modeSelected = BallSpawner.Mode.pinchDwell,
            currentCaseNumber = 1, currentRoundID = 3, studyParticipantId = "p2", studyDeclaredIntent = "unspecified", pinchEndDistanceThreshold = .03f };
        for (int i = 0; i < 16; i++) host.tatgetPos.Add(new Vector3(i * .01f, 1.1f, .537f));
        var recorder = new Study3AttributionRecorder(host, directory);
        require(recorder.SettingsRevision == 1, "Initial full settings missing");
        recorder.CaptureSettings("same"); require(recorder.SettingsRevision == 1, "Unchanged settings produced another revision");
        host.pinchEndDistanceThreshold = .024f; recorder.CaptureSettings("changed");
        require(recorder.SettingsRevision == 2, "Pinch threshold edit was not persisted");
        recorder.BeginTrial("experiment_start");
        var sample = new Study3AttributionSample { SessionId = "file-test", Case = 1, Id = 3, Mode = "pinchDwell", Source = "target",
            SourceInstanceId = 10, BallIndex = 2, Formal = true, Inside = true, TrackingValid = true, Realtime = .1, Time = .1, MotionSequence = 1 };
        recorder.Observe(sample, "contact"); sample = sample.Copy(); sample.Realtime = .2;
        recorder.Finish(sample, "trigger", "selected"); recorder.CompleteTrial();
        var legacy = Enumerable.Repeat<object>("NA", 26).ToList(); legacy[0] = 1; legacy[1] = 3; legacy[19] = false;
        legacy[21] = "cancel, \"quoted\""; legacy.AddRange(new object[] { 0, 1.1, .537 });
        for (int i = 0; i < 16; i++) legacy.AddRange(new object[] { i * .01, 1.1, .537 });
        legacy.AddRange(new object[] { "NA", "NA", "NA", "NA" });
        recorder.WriteNormalized(Study3LogEncoding.Row(legacy.ToArray()));
        host.currentCaseNumber = 2; recorder.BeginTrial("target_ready");
        sample = sample.Copy(); sample.Case = 2; sample.MotionSequence = 2; sample.Realtime = .3;
        recorder.Observe(sample, "contact");
        recorder.Dispose();
        var resultLines = File.ReadAllLines(Path.Combine(directory, "file-test-trial-results-v2.csv"));
        var header = Study3LogEncoding.ParseCsv(resultLines[0]); var result = Study3LogEncoding.ParseCsv(resultLines[1]);
        require(resultLines.Length == 2 && header.Count == result.Count && header.Distinct().Count() == header.Count, "Normalized result width/uniqueness wrong");
        require(result[header.IndexOf("triggerSpeed")] == "NA", "Target coordinate became a trigger value");
        require(result[header.IndexOf("participantId")] == "p2" && result[header.IndexOf("taskResult")] == "success", "Trial metadata/result missing");
        var entries = File.ReadAllLines(Path.Combine(directory, "file-test-attribution-entry-summary.csv"));
        var entryHeader = Study3LogEncoding.ParseCsv(entries[0]);
        var outcomes = entries.Skip(1).Select(line => Study3LogEncoding.ParseCsv(line)).ToArray();
        require(outcomes.Length == 2, "Entry result duplicated or lost");
        require(outcomes[0][entryHeader.IndexOf("outcome")] == "trigger" && outcomes[1][entryHeader.IndexOf("outcome")] == "aborted", "Close did not preserve unfinished entry");
        var lifecycle = File.ReadAllLines(Path.Combine(directory, "file-test-trial-lifecycle.csv"));
        require(lifecycle.Any(line => line.Contains("trial_aborted")) && lifecycle.Any(line => line.Contains("session_closed")), "Trial or session close marker missing");
        string settings = File.ReadAllText(Path.Combine(directory, "file-test-full-settings.csv"));
        require(settings.Contains("pinchEndDistanceThreshold") && settings.Contains("0.024"), "Effective Pinch setting absent");
        string manifest = File.ReadAllText(Path.Combine(directory, "file-test-attribution-manifest.json"));
        require(manifest.Contains("\"loggingStatus\":\"closed\"") && manifest.Contains("\"hardwareCaptureTimestampAvailable\":false"), "Manifest close/provenance incorrect");
        bool rejected = false;
        try { new Study3AttributionRecorder(host, directory); } catch (IOException) { rejected = true; }
        require(rejected, "Session identity reuse silently overwrote provenance");
        foreach (string file in Directory.GetFiles(directory, "*.csv"))
        {
            using (var exclusive = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                require(exclusive.Length > 0, "Supplementary file empty");
        }
        string faultDirectory = Path.Combine(directory, "logging-failure");
        host.LogSessionTimestamp = "file-fault";
        var faulted = new Study3AttributionRecorder(host, faultDirectory);
        faulted.BeginTrial("experiment_start");
        faulted.Observe(sample, "contact");
        faulted.StopAfterFailure(new IOException("injected observer write failure"));
        string faultManifest = File.ReadAllText(Path.Combine(faultDirectory, "file-fault-attribution-manifest.json"));
        require(faultManifest.Contains("\"loggingStatus\":\"failed\"") && faultManifest.Contains("injected observer write failure"), "Logging failure disguised as normal close");
        string[] faultEntries = File.ReadAllLines(Path.Combine(faultDirectory, "file-fault-attribution-entry-summary.csv"));
        require(Study3LogEncoding.ParseCsv(faultEntries[1])[0] == "file-fault", "Recorder did not enforce its canonical session identity");
        string faultTrials = File.ReadAllText(Path.Combine(faultDirectory, "file-fault-trial-lifecycle.csv"));
        require(faultTrials.Contains("logging_failed") && !faultTrials.Contains("session_closed"), "Failed recorder wrote normal-close lifecycle marker");
        foreach (string file in Directory.GetFiles(faultDirectory, "*.csv"))
        {
            using (var exclusive = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                require(exclusive.Length > 0, "Faulted recorder retained a file lock");
        }
        return checks;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class Study3AttributionRecorder : IDisposable
{
    public const string Version = "study3-attribution-v1";
    public const string SettingsHeader = "sessionId,settingsRevision,event,frame,time,realtime,case,id,mode,parametersJson,schemaVersion";
    public const string TrialHeader = "sessionId,trialSequence,event,frame,time,fixedTime,realtime,case,id,mode,expectedAction,actualAction,outcome,reason,settingsRevision,participantId,protocolVersion,blockLabel,taskCondition,declaredIntent,targetLayoutJson,schemaVersion";
    public const string PrefixHeader = Study3LegacyTrialCsv.PrefixHeader;
    public const string ResultHeader = Study3LegacyTrialCsv.Header;
    private readonly BallSpawner host;
    private readonly string session;
    private readonly string manifestPath;
    private readonly Dictionary<string, object> manifest;
    private readonly StreamWriter events, entries, settings, trials, results;
    public readonly Study3AttributionLedger Ledger;
    public long TrialSequence { get; private set; }
    public int SettingsRevision { get; private set; }
    private bool trialOpen, closed;
    private int trialCase, trialId;
    private string trialMode, lastSettings, lastAction = "unknown";
    private string loggingFailure;
    private double nextFlush;

    public Study3AttributionRecorder(BallSpawner host, string directory)
    {
        this.host = host; session = host.LogSessionTimestamp;
        Directory.CreateDirectory(directory);
        if (File.Exists(Path.Combine(directory, session + "-attribution-manifest.json")))
            throw new IOException("Attribution session identity already exists; start a new session rather than reuse entry identifiers.");
        try
        {
            events = Open(directory, "attribution-events", Study3AttributionLedger.Header);
            entries = Open(directory, "attribution-entry-summary", Study3AttributionLedger.Header);
            settings = Open(directory, "full-settings", SettingsHeader);
            trials = Open(directory, "trial-lifecycle", TrialHeader);
            results = Open(directory, "trial-results-v2", ResultHeader);
            Ledger = new Study3AttributionLedger((row, summary) =>
            {
                events.WriteLine(row); if (summary) { entries.WriteLine(row); Flush(); }
            });
            manifest = new Dictionary<string, object> {
                { "telemetryVersion", Version }, { "sessionId", session }, { "schemaVersion", 1 },
                { "createdUtc", DateTime.UtcNow.ToString("o") }, { "applicationVersion", Application.version },
                { "applicationBuildGuid", Application.buildGUID }, { "unityVersion", Application.unityVersion },
                { "platform", Application.platform.ToString() }, { "deviceModel", SystemInfo.deviceModel },
                { "operatingSystem", SystemInfo.operatingSystem }, { "participantId", host.studyParticipantId },
                { "protocolVersion", host.studyProtocolVersion }, { "declaredIntent", host.studyDeclaredIntent },
                { "legacyOutputsPreserved", true }, { "hardwareCaptureTimestampAvailable", false }, { "loggingStatus", "open" }
            };
            TextAsset build = Resources.Load<TextAsset>("Study3AttributionBuild");
            manifest["buildStampJson"] = build != null ? build.text : "unavailable; build stamp was not generated";
            manifestPath = Path.Combine(directory, session + "-attribution-manifest.json");
            File.WriteAllText(manifestPath, Study3LogEncoding.Json(manifest), new UTF8Encoding(false));
            CaptureSettings("session_start"); Flush();
        }
        catch
        {
            closed = true;
            foreach (StreamWriter stream in new[] { events, entries, settings, trials, results })
                try { stream?.Dispose(); } catch { }
            throw;
        }
    }

    private StreamWriter Open(string directory, string suffix, string header)
    {
        string path = Path.Combine(directory, session + "-" + suffix + ".csv");
        bool needsHeader = !File.Exists(path) || new FileInfo(path).Length == 0;
        if (!needsHeader)
        {
            using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                if (reader.ReadLine() != header) throw new FormatException("Supplementary CSV schema changed inside an existing session: " + suffix);
        }
        var writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read), new UTF8Encoding(false), 65536);
        if (needsHeader) writer.WriteLine(header);
        return writer;
    }

    public string Expected(bool mine)
    {
        if (mine) return "cancel";
        string intent = (host.studyDeclaredIntent ?? "").Trim().ToLowerInvariant();
        if (intent == "avoid_selection" || intent == "cancel" || intent == "select") return intent;
        if (intent.Length > 0 && intent != "unspecified") return "unspecified";
        // The normal target task is known; the participant's psychological intention remains unspecified.
        string condition = (host.studyTaskCondition ?? "").Trim().ToLowerInvariant();
        if (condition == "circling" || condition == "pass_through") return "avoid_selection";
        return condition.Length == 0 || condition == "normal_selection" ? "select" : "unspecified";
    }

    public void Prepare(Study3AttributionSample sample, string stage)
    {
        if (closed) return;
        CaptureSettings(stage);
        sample.SessionId = session;
        sample.TrialSequence = TrialSequence; sample.SettingsRevision = SettingsRevision;
        sample.ExpectedAction = sample.Source == "obstacle" ? "avoid_selection" : Expected(sample.IsMineBall);
    }

    public void BeginTrial(string reason)
    {
        if (closed) return;
        string mode = host.modeSelected.ToString();
        if (trialOpen && trialCase == host.currentCaseNumber && trialId == host.currentRoundID && trialMode == mode) return;
        if (trialOpen) EndTrial("aborted", "unknown", "target_changed_before_trial_result");
        Ledger.AbortAll("new_trial_before_entry_outcome");
        TrialSequence++; trialOpen = true; trialCase = host.currentCaseNumber; trialId = host.currentRoundID; trialMode = mode;
        lastAction = "unknown"; CaptureSettings("trial_start"); Lifecycle("trial_start", "unknown", "pending", reason);
    }

    public void Observe(Study3AttributionSample sample, string kind)
    {
        if (closed) return;
        if (sample.Formal && !trialOpen) BeginTrial("first_observation_after_start_or_resume");
        Prepare(sample, "observed_settings_changed"); Ledger.Observe(sample, kind);
    }

    public void Finish(Study3AttributionSample sample, string action, string reason)
    {
        if (closed) return;
        if (sample.Formal && !trialOpen) BeginTrial("terminal_without_trial_start");
        Prepare(sample, "terminal_settings_changed"); Ledger.Finish(sample, action, reason);
        if (sample.Source == "target") lastAction = action;
    }

    public void CompleteTrial()
    {
        if (closed) return;
        if (!trialOpen) BeginTrial("legacy_result_without_trial_start");
        EndTrial("completed", lastAction, "legacy_trial_completed");
    }

    public void AbortTrial(string reason)
    {
        if (closed) return;
        if (trialOpen) EndTrial("aborted", "unknown", reason);
        else Ledger.AbortAll(reason);
        Flush();
    }

    public void Marker(string kind)
    {
        if (!closed) { Lifecycle(kind, "none", "pending", "application_lifecycle"); Flush(); }
    }

    private void EndTrial(string outcome, string action, string reason)
    {
        CaptureSettings("trial_end");
        Ledger.AbortAll(outcome == "completed" ? "trial_completed_before_other_entry_outcome" : reason);
        Lifecycle(outcome == "completed" ? "trial_end" : "trial_aborted", action, outcome, reason);
        trialOpen = false;
    }

    private void Lifecycle(string kind, string action, string outcome, string reason)
    {
        bool mine = host.enableMineBall && host.mineIndices.Contains(host.TargetEnd);
        var layout = new Dictionary<string, object> { { "center", Study3AttributionCapture.Vector(host.largeCirclePoint) },
            { "targetIndex", host.TargetEnd }, { "targetDiameter", host.BallSize }, { "ringRadius", host.CircleRadius },
            { "targetPositions", Positions(host.tatgetPos) }, { "mineIndices", host.mineIndices.ToArray() },
            { "seed", host.seed }, { "seeds", host.seeds != null ? host.seeds.ToArray() : null } };
        trials.WriteLine(Study3LogEncoding.Row(session, TrialSequence, kind, Time.frameCount, Time.timeAsDouble,
            Time.fixedTimeAsDouble, Time.realtimeSinceStartupAsDouble, trialCase, trialId, trialMode, Expected(mine),
            action, outcome, reason, SettingsRevision, host.studyParticipantId, host.studyProtocolVersion,
            host.studyBlockLabel, host.studyTaskCondition, host.studyDeclaredIntent, Study3LogEncoding.Json(layout), 1));
    }

    private static object[] Positions(List<Vector3> positions)
    {
        var result = new object[positions.Count];
        for (int i = 0; i < result.Length; i++) result[i] = Study3AttributionCapture.Vector(positions[i]);
        return result;
    }

    public void WriteNormalized(string legacyRow)
    {
        if (closed) return;
        List<string> raw = Study3LogEncoding.ParseCsv(legacyRow);
        if (raw.Count < 26) throw new FormatException("Legacy result is shorter than the trial prefix.");
        bool mine = raw[19].Equals("True", StringComparison.OrdinalIgnoreCase);
        string expected = Expected(mine);
        results.WriteLine(Study3LegacyTrialCsv.Normalize(legacyRow, host.tatgetPos.Count,
            session, TrialSequence, SettingsRevision, expected, lastAction,
            Study3AttributionLedger.TaskResult(expected, lastAction, lastAction == "unknown" ? "aborted" : "completed"),
            host.studyParticipantId, host.studyProtocolVersion, host.studyBlockLabel, host.studyTaskCondition, host.studyDeclaredIntent, 2));
    }

    public void CaptureSettings(string stage)
    {
        if (closed) return;
        string json = Study3LogEncoding.Json(Study3AttributionSettings.Capture(host));
        if (json == lastSettings) return;
        lastSettings = json; SettingsRevision++;
        settings.WriteLine(Study3LogEncoding.Row(session, SettingsRevision, stage, Time.frameCount, Time.timeAsDouble,
            Time.realtimeSinceStartupAsDouble, host.currentCaseNumber, host.currentRoundID, host.modeSelected, json, 1));
    }

    public void Tick()
    {
        if (closed || Time.realtimeSinceStartupAsDouble < nextFlush) return;
        CaptureSettings("settings_changed"); Flush(); nextFlush = Time.realtimeSinceStartupAsDouble + 0.5;
    }
    public void Flush() { if (!closed) { events.Flush(); entries.Flush(); settings.Flush(); trials.Flush(); results.Flush(); } }
    public void StopAfterFailure(Exception error)
    {
        if (closed) return;
        loggingFailure = error == null ? "unknown logging failure" : error.GetType().Name + ": " + error.Message;
        Dispose();
    }
    public void Dispose()
    {
        if (closed) return;
        try
        {
            bool failed = loggingFailure != null;
            AbortTrial(failed ? "logging_failed_before_outcome" : "session_closed_before_outcome");
            Marker(failed ? "logging_failed" : "session_closed"); Flush();
            manifest["loggingStatus"] = failed ? "failed" : "closed";
            if (failed) manifest["failureReason"] = loggingFailure;
            manifest["closedUtc"] = DateTime.UtcNow.ToString("o");
            File.WriteAllText(manifestPath, Study3LogEncoding.Json(manifest), new UTF8Encoding(false));
        }
        catch (Exception error)
        {
            manifest["loggingStatus"] = "failed";
            manifest["failureReason"] = loggingFailure ?? error.GetType().Name + ": " + error.Message;
            try { File.WriteAllText(manifestPath, Study3LogEncoding.Json(manifest), new UTF8Encoding(false)); } catch { }
            throw;
        }
        finally
        {
            closed = true;
            foreach (StreamWriter stream in new[] { events, entries, settings, trials, results })
                try { stream?.Dispose(); } catch { }
        }
    }
}

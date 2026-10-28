using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class AttributionLoggingChecks
{
    private static int count;
    private static void Require(bool condition, string message) { count++; if (!condition) throw new Exception(message); }
    private sealed class Sink
    {
        public readonly List<Dictionary<string, string>> Events = new List<Dictionary<string, string>>();
        public readonly List<Dictionary<string, string>> Outcomes = new List<Dictionary<string, string>>();
        public readonly Study3AttributionLedger Ledger;
        public Sink()
        {
            Ledger = new Study3AttributionLedger((row, summary) =>
            {
                var fields = Study3LogEncoding.ParseCsv(row); var header = Study3AttributionLedger.Header.Split(',');
                Require(fields.Count == header.Length, "Attribution CSV width mismatch");
                var values = header.Zip(fields, (h, v) => new { h, v }).ToDictionary(v => v.h, v => v.v);
                Events.Add(values); if (summary) Outcomes.Add(values);
            });
        }
    }
    private static Study3AttributionSample S(int tick, bool inside = true, int owner = 20)
    {
        return new Study3AttributionSample { SessionId = "session", TrialSequence = 1, Case = 7, Id = 3,
            Source = "target", SourceInstanceId = owner, BallIndex = 2, Mode = "pinchDwell", ExpectedAction = "select",
            Frame = tick, Time = tick * .02, FixedTime = tick * .02, Realtime = tick * .021,
            MotionSequence = tick, Inside = inside, TrackingValid = true, Formal = true,
            ObservationBasis = "test_actual_callback", Values = new Dictionary<string, object> { { "speed", .04 } } };
    }
    public static int Run()
    {
        count = 0;
        var sink = new Sink(); var first = S(1); sink.Ledger.Observe(first, "contact");
        first.Case = 99; first.Id = 99; first.ExpectedAction = "cancel";
        var done = S(2); sink.Ledger.Finish(done, "trigger", "actual_branch");
        sink.Ledger.Finish(done, "trigger", "duplicate"); sink.Ledger.AbortSource(20, "disable");
        Require(sink.Outcomes.Count == 1, "A resolved contact was counted more than once");
        Require(sink.Outcomes[0]["case"] == "7" && sink.Outcomes[0]["id"] == "3", "Entry identity was not copied");
        Require(sink.Outcomes[0]["taskResult"] == "success", "Frozen instruction changed");
        // Exit is observed before the original callback decides; a same-callback trigger wins.
        sink = new Sink(); sink.Ledger.Observe(S(1), "contact"); sink.Ledger.Observe(S(2, false), "contact_exit");
        Require(sink.Outcomes.Count == 0, "Exit was finalized before the interaction callback");
        sink.Ledger.Finish(S(2, false), "trigger", "exit_confirmation"); sink.Ledger.ResolvePendingExit(20);
        Require(sink.Outcomes.Count == 1 && sink.Outcomes[0]["outcome"] == "trigger", "Exit-to-confirm became a failure");
        // A genuine exit and immediate reentry must produce two distinct entries.
        sink = new Sink(); sink.Ledger.Observe(S(1), "contact"); sink.Ledger.Observe(S(2, false), "contact_exit");
        sink.Ledger.Observe(S(3), "contact"); sink.Ledger.Finish(S(4), "trigger", "selected");
        Require(sink.Outcomes.Count == 2, "Reentry lost an unresolved exit");
        Require(sink.Outcomes[0]["outcome"] == "exit_no_trigger" && sink.Outcomes[1]["outcome"] == "trigger", "Reentry outcomes wrong");
        Require(sink.Outcomes[0]["entrySequence"] != sink.Outcomes[1]["entrySequence"], "Entry id reused");
        // Same ball index on different objects is not the same entry.
        sink = new Sink(); sink.Ledger.Observe(S(1, true, 20), "contact"); sink.Ledger.Observe(S(1, true, 21), "contact");
        sink.Ledger.Finish(S(2, true, 20), "trigger", "selected"); sink.Ledger.AbortAll("session_end");
        Require(sink.Outcomes.Count == 2 && sink.Outcomes.Count(v => v["outcome"] == "aborted") == 1, "Object identity or abort denominator lost");
        // Recovered tracking remains a quality flag on the terminal record.
        sink = new Sink(); sink.Ledger.Observe(S(1), "contact"); var lost = S(2); lost.TrackingValid = false;
        sink.Ledger.Observe(lost, "tracking_check"); sink.Ledger.Observe(S(3), "tracking_check"); sink.Ledger.Finish(S(4), "trigger", "selected");
        Require(sink.Events.Any(v => v["event"] == "tracking_lost") && sink.Events.Any(v => v["event"] == "tracking_recovered"), "Tracking transitions missing");
        Require(sink.Outcomes[0]["trackingLossObserved"] == "1", "Recovered tracking erased its history");
        sink = new Sink(); var initial = S(1); initial.SettingsRevision = 1; initial.DetectorSettingsJson = "old";
        sink.Ledger.Observe(initial, "contact"); var adjusted = S(2); adjusted.SettingsRevision = 2; adjusted.DetectorSettingsJson = "new";
        sink.Ledger.Finish(adjusted, "trigger", "selected_after_edit");
        Require(sink.Outcomes[0]["entrySettingsRevision"] == "1" && sink.Outcomes[0]["settingsRevision"] == "2", "Settings identities lost");
        Require(sink.Outcomes[0]["settingsChangedDuringEntry"] == "1", "Mixed settings were not marked");
        sink = new Sink(); initial = S(1); initial.DetectorSettingsJson = "a"; sink.Ledger.Observe(initial, "contact");
        adjusted = S(2); adjusted.DetectorSettingsJson = "b"; sink.Ledger.Finish(adjusted, "trigger", "detector_edit");
        Require(sink.Outcomes[0]["settingsChangedDuringEntry"] == "1", "Direct detector override was hidden by unchanged host settings");
        // A context change closes the old attempt and opens a new observed one.
        sink = new Sink(); sink.Ledger.Observe(S(1), "contact"); var next = S(2); next.TrialSequence = 2; next.Case = 8;
        sink.Ledger.Observe(next, "contact"); sink.Ledger.Finish(next, "trigger", "selected");
        Require(sink.Outcomes.Count == 2 && sink.Outcomes[0]["outcome"] == "aborted" && sink.Outcomes[1]["case"] == "8", "Trial transition merged entries");
        // Unobserved starts remain visibly unknown and have no invented duration.
        sink = new Sink(); sink.Ledger.Finish(S(1), "trigger", "unobserved_start");
        Require(sink.Outcomes[0]["entryStartObserved"] == "0" && sink.Outcomes[0]["entryElapsedSeconds"] == "NA", "Inferred entry acquired a fake start time");
        sink = new Sink(); sink.Ledger.Observe(S(1), "contact"); sink.Ledger.Finish(S(2), "cancel", "Dwell");
        var annotation = S(2); annotation.Values["reason"] = "Dwell, quoted \"reason\"";
        sink.Ledger.Observe(annotation, "cancel_reason");
        Require(sink.Outcomes.Count == 1 && sink.Events.Last()["event"] == "cancel_reason", "Reason annotation became a second result");
        sink.Ledger.Observe(S(3), "pinch_start"); sink.Ledger.Finish(S(4), "trigger", "new_gesture");
        Require(sink.Outcomes.Count == 2, "A legitimate new gesture within contact was suppressed");
        // Physical interruptions never become deliberate cancel successes.
        sink = new Sink(); var mine = S(1); mine.IsMineBall = true; mine.ExpectedAction = "cancel";
        sink.Ledger.Observe(mine, "contact"); sink.Ledger.Finish(mine, "aborted", "tracking lost");
        Require(sink.Outcomes[0]["outcome"] == "aborted" && sink.Outcomes[0]["taskResult"] == "unknown", "Tracking interruption counted as successful cancellation");
        Require(Study3AttributionLedger.TaskResult("cancel", "trigger", "trigger") == "failure", "Mine trigger counted as success");
        Require(Study3AttributionLedger.TaskResult("avoid_selection", "trigger", "trigger") == "failure", "Obstacle trigger counted as success");
        Require(Study3AttributionLedger.TaskResult("avoid_selection", "none", "exit_no_trigger") == "unknown", "Circling success invented without observation window");
        Require(Study3AttributionLedger.TaskResult("unspecified", "cancel", "cancel") == "unknown", "Unknown instruction invented");
        // Locale, quoted reason fields, finite values, and the historical six-column mismatch.
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            string row = Study3LogEncoding.Row(1.25, "a,b\n\"c\"", null);
            var parsed = Study3LogEncoding.ParseCsv(row);
            Require(parsed.SequenceEqual(new[] { "1.25", "a,b\n\"c\"", "NA" }), "Invariant CSV roundtrip failed");
            Require(Study3LogEncoding.Json(new Dictionary<string, object> { { "x", double.NaN }, { "y", 1.25 } }) == "{\"x\":null,\"y\":1.25}", "Invalid JSON numbers or locale");
        }
        finally { CultureInfo.CurrentCulture = culture; }
        var legacy = Enumerable.Repeat<object>("NA", 26).ToList(); legacy[21] = "cancel, \"quote\"";
        legacy.AddRange(new object[] { 0, 1.1, .537 });
        for (int i = 0; i < 16; i++) legacy.AddRange(new object[] { i * .01, 1.1, .537 });
        legacy.AddRange(new object[] { .02, .04, .03, .1 });
        string oldRow = Study3LogEncoding.Row(legacy.ToArray());
        Require(Study3LogEncoding.ParseCsv(oldRow).Count == 81, "Historical row fixture differs");
        var metadata = new object[] { "session", 1, 2, "select", "trigger", "success", "p2", "protocol", "block", "normal", "unspecified", 2 };
        string normalized = Study3LegacyTrialCsv.Normalize(oldRow, 16, metadata);
        var columns = Study3LegacyTrialCsv.Header.Split(','); var normalizedValues = Study3LogEncoding.ParseCsv(normalized);
        Require(columns.Distinct().Count() == columns.Length && normalizedValues.Count == columns.Length, "Normalized schema width/uniqueness wrong");
        Require(normalizedValues[Array.IndexOf(columns, "triggerSpeed")] == "0.02", "Coordinate was misread as triggerSpeed");
        Require(normalizedValues[Array.IndexOf(columns, "triggerAcceleration")] == "0.1", "Trigger tail was not preserved");
        Require(normalizedValues[21] == "cancel, \"quote\"" && normalizedValues[29].StartsWith("[[0,"), "Reason or geometry damaged");
        bool rejected = false; try { Study3LegacyTrialCsv.Normalize(oldRow, 15, metadata); } catch (FormatException) { rejected = true; }
        Require(rejected, "Mismatched row was silently accepted");
        rejected = false; try { Study3LogEncoding.ParseCsv("\"unfinished"); } catch (FormatException) { rejected = true; }
        Require(rejected, "Broken quoting was silently accepted");
        return count;
    }
}

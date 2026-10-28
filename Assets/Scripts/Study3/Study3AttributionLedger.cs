using System;
using System.Collections.Generic;

public sealed class Study3AttributionSample
{
    public string SessionId, Source, Mode, ExpectedAction = "unspecified", ObservationBasis;
    public string DetectorSettingsJson = "{}";
    public long TrialSequence, MotionSequence;
    public int Case, Id, SourceInstanceId, BallIndex, SettingsRevision, Frame;
    public bool Formal, IsMineBall, Inside, TrackingValid;
    public double Time, FixedTime, Realtime;
    public Dictionary<string, object> Values = new Dictionary<string, object>();
    public Study3AttributionSample Copy()
    {
        var copy = (Study3AttributionSample)MemberwiseClone();
        copy.Values = new Dictionary<string, object>(Values);
        return copy;
    }
}

// Pure observer: no pose queries, callbacks into interaction logic, or changes to thresholds.
public sealed class Study3AttributionLedger
{
    public const string Header = "sessionId,trialSequence,entrySequence,eventSequence,frame,time,fixedTime,realtime,case,id,mode,source,sourceInstanceId,ballIndex,isMineBall,experimentStarted,event,reason,outcome,expectedAction,actualAction,taskResult,inside,trackingValid,trackingLossObserved,entryStartObserved,entryElapsedSeconds,settingsRevision,entrySettingsRevision,settingsChangedDuringEntry,currentCase,currentId,observationBasis,decisionJson,schemaVersion";
    private sealed class Entry
    {
        public Study3AttributionSample Start, Last;
        public long Sequence, LastMotion = long.MinValue;
        public bool Finished, PendingExit, Contact, StartObserved, TrackingValid, TrackingLossObserved, SettingsChanged;
        public string LastAction, LastOutcome;
    }
    private readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
    private readonly Action<string, bool> write;
    private long nextEntry, nextEvent;

    public Study3AttributionLedger(Action<string, bool> write) { this.write = write; }
    public bool HasOpenEntry(int sourceId) => entries.TryGetValue(sourceId, out Entry e) && !e.Finished;

    public void Observe(Study3AttributionSample sample, string eventName)
    {
        entries.TryGetValue(sample.SourceInstanceId, out Entry e);
        if (e != null && !e.Finished && (e.Start.TrialSequence != sample.TrialSequence || e.Start.Mode != sample.Mode))
        {
            FinishExisting(e, e.Last, "aborted", "trial_or_mode_changed_before_outcome", "none");
            e.Contact = false;
        }
        if (e != null && e.PendingExit && sample.Inside)
        {
            FinishExisting(e, e.Last, "exit_no_trigger", "contact_ended_before_outcome", "none");
            e.Contact = false;
        }
        bool newContact = sample.Inside && (e == null || !e.Contact);
        bool newGesture = sample.Inside && e != null && e.Finished && (eventName == "pinch_start" || eventName == "armed");
        if (newContact || newGesture)
        {
            e = Begin(sample, true, newGesture ? "new_gesture_after_resolution" : "observed_contact");
        }
        if (e == null) return;
        bool wasInside = e.Contact;
        e.Contact = sample.Inside;
        if (e.Finished)
        {
            if (eventName == "cancel_reason" || eventName == "direction_check")
                Emit(e, sample, eventName, "post_resolution_annotation", "resolved", "none", false);
            return;
        }
        e.TrackingLossObserved |= !sample.TrackingValid;
        e.SettingsChanged |= sample.SettingsRevision != e.Start.SettingsRevision || sample.DetectorSettingsJson != e.Start.DetectorSettingsJson;
        if (sample.TrackingValid != e.TrackingValid)
            Emit(e, sample, sample.TrackingValid ? "tracking_recovered" : "tracking_lost", "cached_detector_status", "pending", "none", false);
        e.TrackingValid = sample.TrackingValid;
        if (wasInside && !sample.Inside)
        {
            e.PendingExit = true;
            Emit(e, sample, "exit_observed", "awaiting_same_callback_outcome", "pending", "none", false);
        }
        // Keep explicit decision/gesture events even when they reuse a cached motion sample.
        bool plainSample = eventName == "sample" || eventName == "contact" || eventName == "tracking_check";
        if (!plainSample || sample.MotionSequence != e.LastMotion)
            Emit(e, sample, eventName, "observer", "pending", "none", false);
        e.LastMotion = sample.MotionSequence;
        e.Last = sample.Copy();
    }

    public void Finish(Study3AttributionSample sample, string action, string reason)
    {
        entries.TryGetValue(sample.SourceInstanceId, out Entry e);
        if (e != null && !e.Finished && (e.Start.TrialSequence != sample.TrialSequence || e.Start.Mode != sample.Mode))
            FinishExisting(e, e.Last, "aborted", "context_changed_before_terminal_event", "none");
        if (e == null || e.Finished)
        {
            if (e != null && (e.LastAction == action || (action == "aborted" && e.LastOutcome == "aborted")) && e.Start.TrialSequence == sample.TrialSequence) return;
            e = Begin(sample, false, "terminal_without_observed_entry");
        }
        string outcome = action == "aborted" ? "aborted" : action == "cancel" ? "cancel" : "trigger";
        FinishExisting(e, sample, outcome, reason, action == "aborted" ? "none" : action);
    }

    public void ResolvePendingExit(int sourceId)
    {
        if (entries.TryGetValue(sourceId, out Entry e) && e.PendingExit && !e.Finished)
            FinishExisting(e, e.Last, "exit_no_trigger", "contact_ended_before_outcome", "none");
    }

    public void AbortSource(int sourceId, string reason)
    {
        if (!entries.TryGetValue(sourceId, out Entry e)) return;
        if (!e.Finished)
            FinishExisting(e, e.Last, e.PendingExit ? "exit_no_trigger" : "aborted", e.PendingExit ? "contact_ended_before_outcome" : reason, "none");
        entries.Remove(sourceId);
    }

    public void AbortAll(string reason)
    {
        foreach (Entry e in entries.Values)
            if (!e.Finished) FinishExisting(e, e.Last, e.PendingExit ? "exit_no_trigger" : "aborted", e.PendingExit ? "contact_ended_before_outcome" : reason, "none");
        entries.Clear();
    }

    private Entry Begin(Study3AttributionSample sample, bool observed, string reason)
    {
        var e = new Entry { Start = sample.Copy(), Last = sample.Copy(), Sequence = ++nextEntry, Contact = sample.Inside,
            StartObserved = observed, TrackingValid = sample.TrackingValid, TrackingLossObserved = !sample.TrackingValid };
        entries[sample.SourceInstanceId] = e;
        Emit(e, sample, "entry", reason, "pending", "none", false);
        return e;
    }

    private void FinishExisting(Entry e, Study3AttributionSample sample, string outcome, string reason, string action)
    {
        if (e.Finished) return;
        e.Finished = true; e.PendingExit = false; e.LastAction = action; e.LastOutcome = outcome; e.Last = sample.Copy();
        e.TrackingLossObserved |= !sample.TrackingValid;
        e.SettingsChanged |= sample.SettingsRevision != e.Start.SettingsRevision || sample.DetectorSettingsJson != e.Start.DetectorSettingsJson;
        Emit(e, sample, outcome, reason, outcome, action, true);
    }

    public static string TaskResult(string expected, string action, string outcome)
    {
        if (outcome == "pending" || outcome == "aborted" || outcome == "resolved") return "unknown";
        if (expected == "unspecified") return "unknown";
        if (expected == "avoid_selection") return action == "trigger" ? "failure" : "unknown";
        if (expected == "select") return action == "trigger" ? "success" : "failure";
        if (expected == "cancel") return action == "cancel" ? "success" : action == "trigger" ? "failure" : "unknown";
        return "unknown";
    }

    private void Emit(Entry e, Study3AttributionSample s, string eventName, string reason, string outcome, string action, bool summary)
    {
        Study3AttributionSample context = e.Start;
        string row = Study3LogEncoding.Row(context.SessionId, context.TrialSequence, e.Sequence, ++nextEvent,
            s.Frame, s.Time, s.FixedTime, s.Realtime, context.Case, context.Id, context.Mode, context.Source,
            context.SourceInstanceId, context.BallIndex, context.IsMineBall, context.Formal, eventName, reason,
            outcome, context.ExpectedAction, action, TaskResult(context.ExpectedAction, action, outcome), s.Inside,
            s.TrackingValid, e.TrackingLossObserved, e.StartObserved, e.StartObserved ? (object)(s.Realtime - context.Realtime) : null,
            s.SettingsRevision, context.SettingsRevision, e.SettingsChanged, s.Case, s.Id, s.ObservationBasis, Study3LogEncoding.Json(s.Values), 1);
        write(row, summary);
    }
}

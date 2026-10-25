using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// A snapshot of the inputs actually used by the interaction, without another SDK pose query.
public sealed class SpeedStudySnapshot
{
    public int Frame, Case, Id, Mode, BallIndex, BallKind, SourceInstanceId;
    public string Source, VelocitySource;
    public double Time, FixedTime, Realtime, MotionFixedTime;
    public double MotionTrackingGapSeconds;
    public long MotionSampleSequence;
    public float FixedDeltaTime, MotionDeltaTime, RequestedHz, TimeScale;
    public float Speed, MinSpeed, MaxSpeed, DwellTimeout;
    public bool InFixedStep, IsMineBall, HandInBall, Armed, Triggered, ReentryRequired;
    public bool ExperimentStarted;
    public bool EnableTrigger, AllowHighSpeedTrigger, TipTracked, DistalTracked, TipInside, DistalInside;
    public Vector3 Tip, PreviousTip, Distal;
    public Bounds SampleBounds;

    public static SpeedStudySnapshot Capture(Component owner, BallSpawner host, AcceStudy3 acce,
        int mode, int ballIndex, int ballKind, bool mineBall, string source, float speed,
        bool handInBall, bool armed, bool triggered, bool reentryRequired, bool allowHighSpeedTrigger)
    {
        return new SpeedStudySnapshot
        {
            Frame = UnityEngine.Time.frameCount,
            Time = UnityEngine.Time.timeAsDouble,
            FixedTime = UnityEngine.Time.fixedTimeAsDouble,
            Realtime = UnityEngine.Time.realtimeSinceStartupAsDouble,
            InFixedStep = UnityEngine.Time.inFixedTimeStep,
            FixedDeltaTime = UnityEngine.Time.fixedDeltaTime,
            RequestedHz = host.samplingRateHz,
            TimeScale = UnityEngine.Time.timeScale,
            Case = host.currentCaseNumber, Id = host.currentRoundID, Mode = mode,
            Source = source, SourceInstanceId = owner.GetInstanceID(),
            BallIndex = ballIndex, BallKind = ballKind, IsMineBall = mineBall,
            HandInBall = handInBall, Armed = armed, Triggered = triggered,
            ReentryRequired = reentryRequired, EnableTrigger = host.enable_trigger,
            AllowHighSpeedTrigger = allowHighSpeedTrigger,
            Speed = speed, MinSpeed = Mathf.Max(0f, host.speedMinThreshold),
            MaxSpeed = Mathf.Max(0f, host.speedMaxThreshold), DwellTimeout = host.pinchDwellCancelTime,
            VelocitySource = acce != null && acce.v != float.MaxValue ? "AcceStudy3.v" : "ReactTap.V_vec_or_zero",
            MotionSampleSequence = acce != null ? acce.MotionSampleSequence : 0,
            MotionFixedTime = acce != null ? acce.MotionSampleFixedTime : double.NaN,
            MotionDeltaTime = acce != null ? acce.MotionSampleDeltaTime : float.NaN,
            MotionTrackingGapSeconds = acce != null ? acce.MotionTrackingGapSeconds : double.NaN,
            TipTracked = acce != null && acce.IndexTipPoseValid,
            DistalTracked = acce != null && acce.IndexDistalPoseValid,
            TipInside = acce != null && acce.SampleTipInside,
            DistalInside = acce != null && acce.SampleDistalInside,
            Tip = acce != null ? acce.IndexTipPose2.Position : Vector3.zero,
            PreviousTip = acce != null ? acce.MotionPreviousTipPosition : Vector3.zero,
            Distal = acce != null ? acce.IndexDistalPose.Position : Vector3.zero,
            SampleBounds = acce != null ? acce.MotionSampleBounds : new Bounds()
        };
    }
}

// One observer per target/obstacle. Entry context survives Case changes and state resets.
public sealed class SpeedStudyTelemetry
{
    public const string Header = "frame,time,case,id,mode,event,reason,entrySequence,ballIndex,ballKind,isMineBall,handInBall,speedMinArmed,speedMaxObservedInside,speedTriggered,lastInsideSpeed,exitFrameSpeed,minSpeedThreshold,maxSpeedThreshold,entryElapsed,tipInside,distalInside,indexX,indexY,indexZ,distalX,distalY,distalZ,fixedTime,fixedDeltaTime,fixedSamplingHz,requestedSamplingHz,realtime,timeScale,inFixedStep,source,sourceInstanceId,motionSampleSequence,motionFixedTime,motionDeltaTime,tipPoseValid,distalPoseValid,velocitySource,decisionSpeed,previousIndexX,previousIndexY,previousIndexZ,boundsCenterX,boundsCenterY,boundsCenterZ,boundsExtentX,boundsExtentY,boundsExtentZ,enableTrigger,reentryRequired,allowHighSpeedTrigger,speedMaxObservedAfterArmed,armedEver,insideSampleCount,lastInsideFixedTime,entryTime,armedLatencyMs,outcome,boundaryHighSpeedCandidate,currentCase,currentId,dwellTimeoutSeconds,schemaVersion,entryFixedSamplingHz,mixedSamplingRate,entryMinSpeedThreshold,entryMaxSpeedThreshold,settingsChanged,entryExperimentStarted,currentExperimentStarted,motionTrackingGapSeconds,trackingLossObserved";

    private readonly Action<string, bool> write;
    private static readonly char[] CsvSpecialCharacters = { ',', '"', '\r', '\n' };
    private readonly Func<long> nextEntry;
    private SpeedStudySnapshot entry;
    private SpeedStudySnapshot lastSnapshot;
    private long entrySequence, lastInsideSample = -1, lastTrackingLossSample = -1;
    private int insideSamples;
    private float lastInsideSpeed = float.NaN;
    private double lastInsideFixedTime = double.NaN, armedTime = double.NaN;
    private bool active, completed, observedMax, observedMaxAfterArmed, armedEver;
    private bool mixedSamplingRate, settingsChanged;
    private bool trackingLossObserved;
    private string outcome = "pending";

    public SpeedStudyTelemetry(Action<string, bool> write, Func<long> nextEntry)
    {
        this.write = write;
        this.nextEntry = nextEntry;
    }

    public static bool IsSpeedMode(int mode)
    {
        return mode == (int)BallSpawner.Mode.speed || mode == (int)BallSpawner.Mode.speedDwell
            || mode == (int)BallSpawner.Mode.speedRelease || mode == (int)BallSpawner.Mode.speedDirection
            || mode == (int)BallSpawner.Mode.speedWristTilt;
    }

    public void Inside(SpeedStudySnapshot sample)
    {
        if (!active)
        {
            entry = sample;
            entrySequence = nextEntry();
            active = true;
            completed = observedMax = observedMaxAfterArmed = armedEver = false;
            mixedSamplingRate = settingsChanged = false;
            trackingLossObserved = false;
            lastInsideSample = lastTrackingLossSample = -1;
            insideSamples = 0;
            lastInsideSpeed = float.NaN;
            lastInsideFixedTime = armedTime = double.NaN;
            outcome = "pending";
            Record(sample, "enter", "accepted_contact", false);
        }

        if (lastInsideSample == sample.MotionSampleSequence)
        {
            return;
        }

        lastInsideSample = sample.MotionSampleSequence;
        insideSamples++;
        lastInsideSpeed = sample.Speed;
        lastInsideFixedTime = sample.MotionFixedTime;
        ObserveThresholds(sample);
        Record(sample, "sample", "before_speed_check", false);
    }

    public void Check(SpeedStudySnapshot sample)
    {
        if (!active || completed) return;
        ObserveThresholds(sample);
        Record(sample, "check", sample.ReentryRequired ? "reentry_required"
            : !sample.EnableTrigger && sample.Source == "target" ? "trigger_disabled"
            : !sample.AllowHighSpeedTrigger ? "high_speed_gate_blocked" : "speed_gate", false);
    }

    public void Armed(SpeedStudySnapshot sample)
    {
        if (!active || completed) return;
        ObserveThresholds(sample);
        Record(sample, "armed", "minimum_speed_met", false);
    }

    public void Finish(SpeedStudySnapshot sample, string result, string reason)
    {
        if (!active || completed) return;
        ObserveThresholds(sample);
        completed = true;
        outcome = result;
        Record(sample, result, reason, true);
    }

    public void Exit(SpeedStudySnapshot sample)
    {
        if (!active) return;
        if (!completed)
        {
            Finish(sample, "exit", !armedEver ? "minimum_speed_not_reached_before_exit"
                : observedMaxAfterArmed ? "high_speed_observed_but_not_triggered"
                : "maximum_speed_not_reached_before_exit");
        }
        else
        {
            Record(sample, "exit", "after_" + outcome, false);
        }
        active = false;
    }

    public void TrackingLost(SpeedStudySnapshot sample)
    {
        if (!active || completed || sample.TipTracked || lastTrackingLossSample == sample.MotionSampleSequence) return;
        lastTrackingLossSample = sample.MotionSampleSequence;
        trackingLossObserved = true;
        Record(sample, "tracking_lost", "sdk_tip_pose_unavailable", false);
    }

    public void Disable(SpeedStudySnapshot sample)
    {
        Finish(sample, "aborted", "source_disabled_before_outcome");
        active = false;
    }

    public void AbortPending(string reason)
    {
        if (lastSnapshot != null) Finish(lastSnapshot, "aborted", reason);
        active = false;
    }

    private void ObserveThresholds(SpeedStudySnapshot sample)
    {
        mixedSamplingRate |= sample.FixedDeltaTime != entry.FixedDeltaTime;
        settingsChanged |= mixedSamplingRate || sample.MinSpeed != entry.MinSpeed
            || sample.MaxSpeed != entry.MaxSpeed || sample.TimeScale != entry.TimeScale
            || sample.DwellTimeout != entry.DwellTimeout;
        trackingLossObserved |= !sample.TipTracked
            || sample.MotionTrackingGapSeconds > sample.MotionDeltaTime * 1.5;
        if (sample.Armed && !armedEver)
        {
            armedEver = true;
            armedTime = sample.Time;
        }
        // Exit speed must never be counted as an inside observation.
        if ((sample.TipInside || sample.DistalInside) && sample.Speed >= sample.MaxSpeed)
        {
            observedMax = true;
            observedMaxAfterArmed |= sample.Armed;
        }
    }

    private void Record(SpeedStudySnapshot s, string eventName, string reason, bool summary)
    {
        lastSnapshot = s;
        bool exit = eventName == "exit";
        bool candidate = exit && outcome == "exit" && armedEver && !observedMaxAfterArmed
            && !trackingLossObserved
            && s.TipTracked && s.DistalTracked && !s.TipInside && !s.DistalInside
            && lastInsideSpeed < s.MaxSpeed && s.Speed >= s.MaxSpeed;
        var fields = new List<string>
        {
            N(s.Frame), N(s.Time), N(entry.Case), N(entry.Id), ((BallSpawner.Mode)entry.Mode).ToString(),
            eventName, reason, N(entrySequence), N(entry.BallIndex), N(entry.BallKind), B(entry.IsMineBall),
            B(s.HandInBall), B(s.Armed), B(observedMax), B(s.Triggered), N(lastInsideSpeed),
            exit ? N(s.Speed) : "NA", N(s.MinSpeed), N(s.MaxSpeed), N(s.Time - entry.Time),
            B(s.TipInside), B(s.DistalInside)
        };
        V(fields, s.Tip); V(fields, s.Distal);
        fields.AddRange(new[]
        {
            N(s.FixedTime), N(s.FixedDeltaTime), N(1f / s.FixedDeltaTime), N(s.RequestedHz),
            N(s.Realtime), N(s.TimeScale), B(s.InFixedStep), entry.Source, N(entry.SourceInstanceId),
            N(s.MotionSampleSequence), N(s.MotionFixedTime), N(s.MotionDeltaTime), B(s.TipTracked),
            B(s.DistalTracked), s.VelocitySource, N(s.Speed)
        });
        V(fields, s.PreviousTip); V(fields, s.SampleBounds.center); V(fields, s.SampleBounds.extents);
        fields.AddRange(new[]
        {
            B(s.EnableTrigger), B(s.ReentryRequired), B(s.AllowHighSpeedTrigger), B(observedMaxAfterArmed),
            B(armedEver), N(insideSamples), N(lastInsideFixedTime), N(entry.Time),
            N((armedTime - entry.Time) * 1000), outcome, B(candidate), N(s.Case), N(s.Id),
            N(s.DwellTimeout), "1", N(1f / entry.FixedDeltaTime), B(mixedSamplingRate),
            N(entry.MinSpeed), N(entry.MaxSpeed), B(settingsChanged)
        });
        fields.AddRange(new[] { B(entry.ExperimentStarted), B(s.ExperimentStarted),
            N(s.MotionTrackingGapSeconds), B(trackingLossObserved) });
        for (int i = 0; i < fields.Count; i++) fields[i] = Csv(fields[i]);
        write(string.Join(",", fields), summary);
    }

    private static string N(double value) => double.IsNaN(value) || double.IsInfinity(value)
        ? "NA" : value.ToString("R", CultureInfo.InvariantCulture);
    private static string N(float value) => float.IsNaN(value) || float.IsInfinity(value)
        ? "NA" : value.ToString("R", CultureInfo.InvariantCulture);
    private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string N(long value) => value.ToString(CultureInfo.InvariantCulture);
    private static string B(bool value) => value ? "1" : "0";
    private static void V(List<string> fields, Vector3 value)
    {
        fields.Add(N(value.x)); fields.Add(N(value.y)); fields.Add(N(value.z));
    }
    private static string Csv(string value)
    {
        value = value ?? "";
        return value.IndexOfAny(CsvSpecialCharacters) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

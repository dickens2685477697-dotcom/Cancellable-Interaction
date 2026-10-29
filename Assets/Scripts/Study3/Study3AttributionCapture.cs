using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class Study3AttributionCapture
{
    public static object Vector(Vector3 value) => new object[] { value.x, value.y, value.z };

    public static Study3AttributionSample Sample(Component owner, BallSpawner host, LogWriter3 writer,
        AcceStudy3 acce, AcceStudy3_2 thumb, ReactTap react, Pinch pinch, ReleaseGesture release,
        int mode, int ballIndex, bool mine, string source)
    {
        var s = new Study3AttributionSample { SessionId = host.LogSessionTimestamp, Source = source,
            SourceInstanceId = owner.GetInstanceID(), BallIndex = ballIndex, IsMineBall = mine,
            Case = host.currentCaseNumber, Id = host.currentRoundID, Mode = ((BallSpawner.Mode)mode).ToString(),
            Formal = writer.IsExperimentRunning, Frame = Time.frameCount, Time = Time.timeAsDouble,
            FixedTime = Time.fixedTimeAsDouble, Realtime = Time.realtimeSinceStartupAsDouble,
            MotionSequence = acce != null ? acce.MotionSampleSequence : -1,
            TrackingValid = acce != null && acce.IndexTipPoseValid, ObservationBasis = "cached_legacy_callbacks" };
        var v = s.Values;
        v["enableTrigger"] = host.enable_trigger;
        v["participantId"] = host.studyParticipantId;
        v["protocolVersion"] = host.studyProtocolVersion;
        v["blockLabel"] = host.studyBlockLabel;
        v["taskCondition"] = host.studyTaskCondition;
        v["declaredIntent"] = host.studyDeclaredIntent;
        v["motionSequence"] = s.MotionSequence;
        if (acce != null)
        {
            v["legacyMotionOwnerInstanceId"] = acce.GetInstanceID();
            v["indexSpeed"] = acce.v == float.MaxValue ? null : (object)acce.v;
            v["signedRateAcceleration"] = acce.a == float.MaxValue ? null : (object)acce.a;
            v["motionDt"] = acce.MotionSampleDeltaTime; v["motionFixedTime"] = acce.MotionSampleFixedTime;
            v["motionTimestamp"] = acce.MotionSampleTimestamp;
            v["motionTimestampKind"] = "mrtk_update_received_realtime";
            v["tipValid"] = acce.IndexTipPoseValid; v["distalValid"] = acce.IndexDistalPoseValid;
            v["index"] = acce.IndexTipPoseValid ? Vector(acce.IndexTipPose2.Position) : null;
            v["previousIndex"] = Vector(acce.MotionPreviousTipPosition);
            v["distal"] = acce.IndexDistalPoseValid ? Vector(acce.IndexDistalPose.Position) : null;
            v["tipInside"] = acce.SampleTipInside; v["distalInside"] = acce.SampleDistalInside;
            v["boundsCenter"] = Vector(acce.MotionSampleBounds.center); v["boundsExtent"] = Vector(acce.MotionSampleBounds.extents);
            v["dwellElapsed"] = acce.UpdateT; v["actualDwellThreshold"] = acce.WaitTime;
            v["dwellComparison"] = ">"; v["dwellTimePass"] = acce.UpdateT > acce.WaitTime; v["delayMode"] = acce.Delay;
            v["prepared"] = acce.Prepared; v["hesitated"] = acce.Hesitated;
            v["Vb"] = acce.Vb; v["Vbc"] = acce.Vbc; v["Ab"] = acce.Ab; v["Vupper"] = acce.Vupper;
        }
        if (thumb != null)
        {
            v["thumbValid"] = thumb.AttributionPoseValid;
            v["thumb"] = thumb.AttributionPoseValid ? Vector(thumb.ThumbTipPose2.Position) : null;
            v["thumbRegionRadius"] = thumb.triggerDis;
        }
        if (pinch != null)
        {
            v["pinchSampleSequence"] = pinch.AttributionSampleSequence;
            v["pinchSampleTime"] = pinch.AttributionSampleTime;
            v["pinchPoseValid"] = pinch.AttributionIndexValid && pinch.AttributionThumbValid;
            v["pinchDistance"] = pinch.AttributionIndexValid && pinch.AttributionThumbValid ? (object)pinch.distance : null;
            v["rawPinchDistanceUsed"] = pinch.distance;
            v["isPinch"] = pinch.isPinch; v["pinchActivateDistance"] = pinch.ActivateDistance;
            v["pinchDeactivateDistance"] = pinch.DeactivateDistance;
        }
        if (react != null)
        {
            v["reactEntered"] = react.Entered; v["reactPrepared"] = react.Prepared;
            v["reactSpeed"] = react.V_vec; v["reactAcceleration"] = react.A_vec;
            v["reactSpeedGate"] = react.V_down; v["reactAccelerationGate"] = react.A_;
            v["reactFingerSpeedThreshold"] = react.fingerSpeedThershold;
            v["reactMovementThreshold"] = react.InBallMovementThreshold;
            v["reactAngleThreshold"] = react.angleChangeThreshold_new;
            v["reactEnterSatisfiesGate"] = react.enterSatisfiesSpeedAcceCondition;
        }
        if (release != null)
        {
            v["releaseObservation"] = release.AttributionObservation;
            v["releaseSampleTime"] = release.AttributionSampleTime;
            v["releasePoseValid"] = release.AttributionPoseValid;
            v["releaseDistanceDelta"] = release.AttributionDistanceDelta;
            v["releaseOpeningSpeed"] = release.AttributionOpeningSpeed;
            v["releaseThreshold"] = release.AttributionThreshold;
            v["releaseGatePassed"] = release.AttributionGatePassed;
            v["releaseHistoryReady"] = release.AttributionHistoryReady;
        }
        var effective = new Dictionary<string, object>();
        if (pinch != null) { effective["pinchActivate"] = pinch.ActivateDistance; effective["pinchDeactivate"] = pinch.DeactivateDistance; }
        if (acce != null)
        {
            effective["dwellWaitTime"] = acce.WaitTime; effective["Vb"] = acce.Vb; effective["Vbc"] = acce.Vbc;
            effective["Ab"] = acce.Ab; effective["Vupper"] = acce.Vupper; effective["HesTime"] = acce.HesTime;
        }
        if (react != null)
        {
            effective["reactVDown"] = react.useAdaptiveEnterSpeed || react.useAdaptiveRealtimeFingerSpeed ? null : (object)react.V_down;
            effective["reactAdaptiveEnter"] = react.useAdaptiveEnterSpeed;
            effective["reactAdaptiveRealtime"] = react.useAdaptiveRealtimeFingerSpeed;
            effective["reactAdaptiveMin"] = react.adaptiveVDownMin; effective["reactAdaptiveMax"] = react.adaptiveVDownMax;
            effective["reactAdaptiveEnterThreshold"] = react.adaptiveEnterSpeedThreshold;
            effective["reactAdaptiveEnterGain"] = react.adaptiveEnterSpeedK;
            effective["reactAcceleration"] = react.A_;
            effective["reactFingerSpeed"] = react.fingerSpeedThershold; effective["reactMovement"] = react.InBallMovementThreshold;
            effective["reactAngle"] = react.angleChangeThreshold_new; effective["reactEnterGate"] = react.enterSatisfiesSpeedAcceCondition;
        }
        if (release != null) effective["releaseDistanceDelta"] = release.distanceDeltaThreshold;
        v["effectiveDetectorSettings"] = effective;
        s.DetectorSettingsJson = Study3LogEncoding.Json(effective);
        Collider collider = owner.GetComponent<Collider>();
        if (collider != null)
        {
            v["colliderType"] = collider.GetType().Name; v["colliderEnabled"] = collider.enabled;
            v["colliderCenter"] = Vector(collider.bounds.center); v["colliderExtent"] = Vector(collider.bounds.extents);
            if (collider is SphereCollider sphere)
            {
                Vector3 scale = sphere.transform.lossyScale;
                v["sphereCenter"] = Vector(sphere.transform.TransformPoint(sphere.center));
                v["sphereRadius"] = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            }
        }
        return s;
    }
}

// Registers before the existing technique callbacks, observes only their cached data.
public sealed class Study3AttributionBridge
{
    private readonly Component owner;
    private readonly LogWriter3 writer;
    private readonly Func<Study3AttributionSample> capture;
    private readonly List<Action> detach = new List<Action>();
    private bool primaryInside, thumbInside, reactInside;
    private bool completedSource;
    private Study3AttributionSample pendingCancel;
    private string pendingCancelReason;
    public Study3AttributionBridge(Component owner, LogWriter3 writer, Func<Study3AttributionSample> capture)
    { this.owner = owner; this.writer = writer; this.capture = capture; }

    private void Guard(Action action)
    {
        if (writer == null || writer.AttributionUnavailable) return;
        try { action(); } catch (Exception error) { writer.FailAttribution(error); }
    }

    private void Bind(UnityEvent signal, UnityAction callback)
    { if (signal != null) { signal.AddListener(callback); detach.Add(() => signal.RemoveListener(callback)); } }

    public void Attach(AcceStudy3 acce, AcceStudy3_2 thumb, ReactTap react, Pinch pinch, ReleaseGesture release, int mode)
    {
        Guard(() =>
        {
        bool custom = mode == 30 || mode == 31 || mode == 32;
        if (!custom && acce != null)
        {
            Bind(acce.InEvent, () => { primaryInside = true; Observe("contact"); });
            Bind(acce.AfterOutEvent, () => { primaryInside = false; Observe("contact_exit"); });
            Bind(acce.BeforeHesEvent, () => Observe("condition_met"));
        }
        bool usesThumb = mode == 4 || mode == 8 || mode == 9 || mode == 13 || mode == 16 || mode == 17 || mode == 27;
        if (!custom && usesThumb && thumb != null)
        {
            Bind(thumb.InEvent, () => { thumbInside = true; Observe("thumb_contact"); });
            Bind(thumb.AfterOutEvent, () => { thumbInside = false; Observe("thumb_exit"); });
        }
        bool usesReact = mode == 3 || mode == 5 || mode == 6 || mode == 7 || mode == 14 || mode == 18 || mode == 23 || mode == 24 || mode == 25 || mode == 29;
        if (!custom && usesReact && react != null)
        {
            Bind(react.InEvent, () => { reactInside = true; Observe("react_stage"); });
            Bind(react.AfterOutEvent, () => { reactInside = false; Observe("react_exit"); });
        }
        if (!custom && pinch != null)
        {
            Bind(pinch.PinchStart, () => Observe("pinch_start"));
            Bind(pinch.PinchEnd, () => Observe("pinch_end"));
            if (usesThumb)
            {
                Action check = () => Decision("pinch_check", new Dictionary<string, object> {
                    { "sampleSequence", pinch.AttributionSampleSequence }, { "sampleTime", pinch.AttributionSampleTime },
                    { "distanceUsed", pinch.distance }, { "indexValid", pinch.AttributionIndexValid }, { "thumbValid", pinch.AttributionThumbValid },
                    { "activateThreshold", pinch.ActivateDistance }, { "deactivateThreshold", pinch.DeactivateDistance },
                    { "wasPinching", pinch.isPinch }, { "activatePass", pinch.distance < pinch.ActivateDistance },
                    { "deactivatePass", pinch.distance > pinch.DeactivateDistance } });
                pinch.AttributionObserved += check; detach.Add(() => pinch.AttributionObserved -= check);
            }
        }
        if (!custom && release != null)
        {
            Action check = () => Decision("release_check", new Dictionary<string, object> {
                { "kind", release.AttributionObservation }, { "sampleTime", release.AttributionSampleTime },
                { "poseValid", release.AttributionPoseValid }, { "delta", release.AttributionDistanceDelta },
                { "openingSpeed", release.AttributionOpeningSpeed }, { "threshold", release.AttributionThreshold },
                { "passed", release.AttributionGatePassed }, { "historyReady", release.AttributionHistoryReady } });
            release.AttributionObserved += check; detach.Add(() => release.AttributionObserved -= check);
        }
            });
    }

    public Study3AttributionSample Snapshot()
    {
        Study3AttributionSample sample = capture();
        sample.Inside = primaryInside || thumbInside || reactInside;
        sample.Values["primaryContact"] = primaryInside; sample.Values["thumbContact"] = thumbInside;
        sample.Values["reactContact"] = reactInside;
        return sample;
    }
    public void Observe(string kind) => Guard(() => { if (!completedSource) writer.ObserveAttribution(Snapshot(), kind); });
    public void Decision(string kind, Dictionary<string, object> decision = null)
    {
        Guard(() =>
        {
        if (completedSource) return;
        // Global gesture checks can run on every ball. Inactive balls do not need
        // to allocate the full attribution snapshot on every frame.
        if ((kind == "pinch_check" || kind == "release_check") && !primaryInside && !thumbInside && !reactInside
            && !writer.HasOpenAttribution(owner.GetInstanceID())) return;
        var s = Snapshot(); if (decision != null) { s.Values["actualDecision"] = decision; s.ObservationBasis = "actual_" + kind; }
        if (kind == "pinch_check" && decision != null)
            s.TrackingValid &= (bool)decision["indexValid"] && (bool)decision["thumbValid"];
        if (kind == "release_check" && decision != null) s.TrackingValid &= (bool)decision["poseValid"];
        writer?.ObserveAttribution(s, kind);
            });
    }
    public void Custom(bool inside, bool tracking, string kind, Dictionary<string, object> decision)
    {
        Guard(() =>
        {
        if (completedSource) return;
        primaryInside = inside;
        if (!inside && !writer.HasOpenAttribution(owner.GetInstanceID())) return;
        var s = Snapshot(); s.TrackingValid = tracking; s.ObservationBasis = "actual_motion_pipeline";
        s.Values["actualDecision"] = decision; writer?.ObserveAttribution(s, kind);
            });
    }
    public void Finish(string action, string reason)
    {
        Guard(() =>
        {
        if (completedSource) return;
        pendingCancel = null;
        writer?.FinishAttribution(Snapshot(), action, reason);
        if (reason == "legacy_complete_selection") completedSource = true;
            });
    }
    private static string CancelAction(string reason)
    {
        string lower = (reason ?? "").ToLowerInvariant();
        return lower.Contains("tracking lost") || lower.Contains("history reset") || lower.Contains("invalid motion") ? "aborted" : "cancel";
    }
    public void CancelBranch(string reason, bool defer)
    {
        Guard(() =>
        {
        if (completedSource) return;
        var s = Snapshot(); s.Values["cancelBranch"] = reason;
        writer?.ObserveAttribution(s, "cancel_branch");
        if (defer) { pendingCancel = s; pendingCancelReason = "branch:" + reason; }
        else writer?.FinishAttribution(s, CancelAction(reason), "branch:" + reason);
            });
    }
    public void CancelReason(string reason)
    {
        Guard(() =>
        {
        if (completedSource) return;
        pendingCancel = null;
        var s = Snapshot(); s.Values["actualCancelReason"] = reason;
        writer?.ObserveAttribution(s, "cancel_reason");
        writer?.FinishAttribution(s, CancelAction(reason), reason);
            });
    }
    public void Tick()
    { if (!completedSource && writer != null && writer.HasOpenAttribution(owner.GetInstanceID())) Observe("tracking_check"); }
    public void LateTick()
    {
        Guard(() =>
        {
        if (pendingCancel != null && writer != null && writer.HasOpenAttribution(owner.GetInstanceID()))
            writer.FinishAttribution(pendingCancel, CancelAction(pendingCancelReason), pendingCancelReason);
        pendingCancel = null;
        writer?.ResolveAttributionExit(owner.GetInstanceID());
            });
    }
    public void Disable(string reason)
    {
        Guard(() => { LateTick(); writer.AbortAttributionSource(owner.GetInstanceID(), reason); });
        foreach (Action action in detach) { try { action(); } catch { } }
        detach.Clear();
    }

}

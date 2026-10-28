using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class LogWriter3
{
    private Study3AttributionRecorder attributionRecorder;
    private bool attributionFailed, attributionClosed;
    private readonly Dictionary<int, Study3AttributionBridge> attributionBridges = new Dictionary<int, Study3AttributionBridge>();
    public bool AttributionUnavailable => attributionFailed || attributionClosed || stateOnly;
    public void FailAttribution(Exception error)
    {
        if (attributionFailed) return;
        attributionFailed = true;
        try { attributionRecorder?.StopAfterFailure(error); } catch { }
        Debug.LogError("Study3 attribution sidecars stopped; legacy interaction/logging continues. " + error.Message);
    }

    // A sidecar write failure must never turn a failed logging action into an interaction failure.
    private void WithAttribution(Action<Study3AttributionRecorder> action)
    {
        if (attributionFailed || attributionClosed || stateOnly || ballSpawner == null
            || ballSpawner.executionMode != BallSpawner.ExecutionMode.Study3Experiment) return;
        try
        {
            if (attributionRecorder == null)
                attributionRecorder = new Study3AttributionRecorder(ballSpawner,
                    Path.Combine(Application.persistentDataPath, "Logs", ballSpawner.LogSessionTimestamp));
            action(attributionRecorder);
        }
        catch (Exception error)
        {
            FailAttribution(error);
        }
    }

    public void RegisterAttributionBridge(int id, Study3AttributionBridge bridge)
    { if (!stateOnly) attributionBridges[id] = bridge; }
    public bool HasOpenAttribution(int id) => !attributionFailed && attributionRecorder != null && attributionRecorder.Ledger.HasOpenEntry(id);
    public void ObserveAttribution(Study3AttributionSample sample, string kind) => WithAttribution(r => r.Observe(sample, kind));
    public void FinishAttribution(Study3AttributionSample sample, string action, string reason) => WithAttribution(r => r.Finish(sample, action, reason));
    public void ResolveAttributionExit(int id) { if (attributionRecorder != null) WithAttribution(r => r.Ledger.ResolvePendingExit(id)); }
    public void AbortAttributionSource(int id, string reason)
    {
        if (attributionRecorder != null) WithAttribution(r => r.Ledger.AbortSource(id, reason));
        attributionBridges.Remove(id);
    }
    public void BeginAttributionTrial(string reason) => WithAttribution(r => r.BeginTrial(reason));
    public void AbortAttributionTrial(string reason) { if (attributionRecorder != null) WithAttribution(r => r.AbortTrial(reason)); }
    private void CompleteAttributionTrial() => WithAttribution(r => r.CompleteTrial());
    private void WriteAttributionNormalized(string row) => WithAttribution(r => r.WriteNormalized(row));
    private void TickAttribution() => WithAttribution(r => r.Tick());
    private void FlushAttribution() { if (attributionRecorder != null) WithAttribution(r => r.Flush()); }
    private void AttributionMarker(string kind) => WithAttribution(r => r.Marker(kind));
    private void CloseAttribution()
    {
        if (attributionClosed) return;
        if (attributionRecorder != null) WithAttribution(r => r.Dispose());
        attributionClosed = true; attributionBridges.Clear();
    }

    public void RecordAttributionObstacleTrigger(S2Obstacles source)
    {
        if (source != null && attributionBridges.TryGetValue(source.GetInstanceID(), out Study3AttributionBridge bridge))
            bridge.Finish("trigger", "actual_obstacle_error_branch");
    }

    public void RecordAttributionSpeed(Component owner, SpeedStudySnapshot sample, string kind, string reason)
    {
        if (owner == null || !attributionBridges.TryGetValue(owner.GetInstanceID(), out Study3AttributionBridge bridge)) return;
        bridge.Custom(sample.TipInside || sample.DistalInside, sample.TipTracked, "speed_" + kind,
            new Dictionary<string, object> {
                { "eventReason", reason }, { "speed", sample.Speed }, { "minSpeedThreshold", sample.MinSpeed },
                { "maxSpeedThreshold", sample.MaxSpeed }, { "armed", sample.Armed }, { "enableTrigger", sample.EnableTrigger },
                { "minimumGatePass", sample.Speed <= sample.MinSpeed }, { "maximumGatePass", sample.Speed >= sample.MaxSpeed },
                { "reentryRequired", sample.ReentryRequired }, { "allowHighSpeedTrigger", sample.AllowHighSpeedTrigger },
                { "motionSequence", sample.MotionSampleSequence }, { "motionDt", sample.MotionDeltaTime },
                { "motionFixedTime", sample.MotionFixedTime }, { "tipValid", sample.TipTracked },
                { "distalValid", sample.DistalTracked }, { "tipInside", sample.TipInside }, { "distalInside", sample.DistalInside },
                { "velocitySource", sample.VelocitySource }, { "inFixedStep", sample.InFixedStep },
                { "index", Study3AttributionCapture.Vector(sample.Tip) }, { "previousIndex", Study3AttributionCapture.Vector(sample.PreviousTip) },
                { "distal", Study3AttributionCapture.Vector(sample.Distal) },
                { "boundsCenter", Study3AttributionCapture.Vector(sample.SampleBounds.center) },
                { "boundsExtent", Study3AttributionCapture.Vector(sample.SampleBounds.extents) }
            });
    }

    private void ObserveCommonAttribution(Study3MotionSample sample, Study3MotionTracker motion,
        int ownerId, bool tracking, bool inside, float speedLimit, float accelerationLimit, bool selected)
    {
        if (!attributionBridges.TryGetValue(ownerId, out Study3AttributionBridge bridge)) return;
        bridge.Custom(inside, tracking, "common_motion", new Dictionary<string, object> {
            { "sampleSourceId", sample.SourceId }, { "sampleSequence", sample.Sequence }, { "sampleTimestamp", sample.Timestamp },
            { "sampleDt", motion.DeltaTime }, { "sampleAccepted", motion.AcceptedLastSample }, { "sampleStatus", motion.Status },
            { "trackingEpoch", sample.TrackingEpoch }, { "hasVelocity", motion.HasVelocity }, { "hasAcceleration", motion.HasAcceleration },
            { "speed", motion.Speed }, { "previousSpeed", motion.PreviousSpeed }, { "speedThreshold", speedLimit },
            { "acceleration", motion.Acceleration }, { "accelerationThreshold", accelerationLimit },
            { "speedPass", motion.HasAcceleration && motion.Speed < speedLimit },
            { "accelerationPass", motion.HasAcceleration && motion.Acceleration < accelerationLimit },
            { "inside", inside }, { "selected", selected }, { "comparison", "<" }, { "index", Study3AttributionCapture.Vector(sample.Position) }
        });
    }

    private void ObserveDirectionAttribution(Component owner, string failReason, bool finalPass,
        int sampleCount, int window, bool ready, float angle, float angleLimit, bool anglePass,
        float speed, float minSpeed, bool speedPass, Vector3 movement, Vector3 direction, Vector3 reference)
    {
        if (owner == null || !attributionBridges.TryGetValue(owner.GetInstanceID(), out Study3AttributionBridge bridge)) return;
        bridge.Decision("direction_check", new Dictionary<string, object> {
            { "reason", failReason }, { "finalCancelPass", finalPass }, { "sampleCount", sampleCount },
            { "windowSamples", window }, { "windowReady", ready }, { "angle", angle }, { "angleThreshold", angleLimit },
            { "anglePass", anglePass }, { "speed", speed }, { "minSpeedThreshold", minSpeed }, { "speedPass", speedPass },
            { "windowMovement", Study3AttributionCapture.Vector(movement) }, { "direction", Study3AttributionCapture.Vector(direction) },
            { "reference", Study3AttributionCapture.Vector(reference) }
        });
    }
}

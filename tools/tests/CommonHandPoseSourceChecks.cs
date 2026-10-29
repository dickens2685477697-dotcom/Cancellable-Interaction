using System;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public static class CommonHandPoseSourceChecks
{
    private sealed class Source : IMixedRealityInputSource { public uint SourceId { get; set; } }
    private sealed class Hand : IMixedRealityHand {
        public IMixedRealityInputSource InputSource { get; set; }
        public bool IsPositionAvailable { get; set; } = true;
    }
    private sealed class Input : IMixedRealityInputSystem {
        public HashSet<IMixedRealityController> DetectedControllers { get; } = new HashSet<IMixedRealityController>();
        public readonly List<object> Handlers = new List<object>();
        public void RegisterHandler<T>(T handler) { Handlers.Add(handler); }
        public void UnregisterHandler<T>(T handler) { Handlers.Remove(handler); }
    }
    private static int checks;
    private static void Require(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    public static int Run()
    {
        checks = 0;
        var input = new Input(); var source = new Source { SourceId = 17 };
        var hand = new Hand { InputSource = source }; input.DetectedControllers.Add(hand);
        var updates = new List<Study3JointSample>();
        var losses = new List<uint>();
        var phaseOrder = new List<string>();
        Action<Study3JointSample> onUpdate = sample => updates.Add(sample);
        Action<Study3JointSample> firstUpdate = sample => phaseOrder.Add("update1");
        Action<Study3JointSample> secondUpdate = sample => phaseOrder.Add("update2");
        Action<Study3JointSample> firstProcessed = sample => phaseOrder.Add("processed1");
        Action<Study3JointSample> secondProcessed = sample => phaseOrder.Add("processed2");
        Action<Study3JointSample> onGesture = sample => phaseOrder.Add("gesture");
        Action<Study3JointSample> firstEvaluated = sample => phaseOrder.Add("evaluated1");
        Action<Study3JointSample> secondEvaluated = sample => phaseOrder.Add("evaluated2");
        Action<uint> onLoss = id => losses.Add(id);
        Study3HandPoseSource.JointSampleUpdated += onUpdate;
        Study3HandPoseSource.JointSampleUpdated += firstUpdate;
        Study3HandPoseSource.JointSampleUpdated += secondUpdate;
        Study3HandPoseSource.JointSampleProcessed += firstProcessed;
        Study3HandPoseSource.JointSampleProcessed += secondProcessed;
        Study3HandPoseSource.JointSampleGesture += onGesture;
        Study3HandPoseSource.JointSampleEvaluated += firstEvaluated;
        Study3HandPoseSource.JointSampleEvaluated += secondEvaluated;
        Study3HandPoseSource.SelectedSourceLost += onLoss;
        CoreServices.InputSystem = input; Time.realtimeSinceStartupAsDouble = 20;
        Require(!Study3HandPoseSource.TryGetSample(out Study3MotionSample first), "Invented initial pose");
        Require(input.Handlers.Count == 2, "Provider event handlers not registered exactly once");
        var handler = (Study3HandPoseSource)input.Handlers[0];
        var data = new InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> {
            InputSource = source, Handedness = Handedness.Right,
            InputData = new Dictionary<TrackedHandJoint, MixedRealityPose> {
                { TrackedHandJoint.IndexTip, new MixedRealityPose { Position = Vector3.zero } },
                { TrackedHandJoint.Wrist, new MixedRealityPose { Position = Vector3.right } }
            }
        };
        handler.OnHandJointsUpdated(data);
        Require(Study3HandPoseSource.TryGetSample(out first), "Fresh provider event missing");
        Require(updates.Count == 1 && updates[0].SourceId == source.SourceId, "Selected provider update was not emitted");
        var firstEvent = updates[0];
        Require(firstEvent.Sequence == first.Sequence && firstEvent.Timestamp == 20 && firstEvent.TrackingEpoch == first.TrackingEpoch,
            "Event identity and timestamp diverged from cached provider sample");
        Require(firstEvent.TryGetJointPose(TrackedHandJoint.Wrist, out MixedRealityPose wrist) && wrist.Position.x == 1,
            "Joint snapshot omitted wrist");
        Time.realtimeSinceStartupAsDouble = 20.01;
        Require(Study3HandPoseSource.TryGetSample(out Study3MotionSample again), "Repeated reader lost cached pose");
        Require(first.Sequence == again.Sequence && first.Timestamp == again.Timestamp, "Repeated read invented a fresh ID/time");
        Require(updates.Count == 1, "Reading the cached pose emitted a new event");
        Require(input.Handlers.Count == 2, "Repeated read registered handlers again");
        // Copying the event prevents later changes to MRTK's mutable dictionary altering old samples.
        data.InputData[TrackedHandJoint.IndexTip] = new MixedRealityPose { Position = Vector3.right };
        data.InputData[TrackedHandJoint.Wrist] = new MixedRealityPose { Position = Vector3.zero };
        Require(Study3HandPoseSource.TryGetSample(out again) && again.Position.x == 0, "Mutable event changed cached pose");
        Require(firstEvent.TryGetJointPose(TrackedHandJoint.Wrist, out wrist) && wrist.Position.x == 1,
            "Mutable provider dictionary changed emitted joint snapshot");
        Time.realtimeSinceStartupAsDouble = 20.02;
        data.InputData[TrackedHandJoint.IndexTip] = new MixedRealityPose { Position = Vector3.zero };
        handler.OnHandJointsUpdated(data);
        Require(Study3HandPoseSource.TryGetSample(out again), "New identical pose was discarded");
        Require(again.Sequence > first.Sequence && again.Timestamp > first.Timestamp, "New provider update did not advance identity");
        Require(updates.Count == 2 && updates[1].Sequence > firstEvent.Sequence && updates[1].Timestamp == 20.02,
            "New provider event did not preserve its receive time and sequence");
        var other = new Source { SourceId = 18 };
        var otherHand = new Hand { InputSource = other };
        input.DetectedControllers.Add(otherHand);
        var otherData = new InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> {
            InputSource = other, Handedness = Handedness.Left, InputData = data.InputData
        };
        handler.OnHandJointsUpdated(otherData);
        Require(updates.Count == 2, "Unselected hand emitted a motion event");
        int beforeOtherLoss = losses.Count;
        handler.OnSourceLost(new SourceStateEventData { InputSource = other });
        Require(losses.Count == beforeOtherLoss, "Unselected hand loss cleared the selected hand");
        input.DetectedControllers.Remove(otherHand);
        hand.IsPositionAvailable = false;
        Require(!Study3HandPoseSource.TryGetSample(out again), "Untracked controller accepted");
        hand.IsPositionAvailable = true; Time.realtimeSinceStartupAsDouble = 20.5;
        Require(!Study3HandPoseSource.TryGetSample(out again), "Stale cache accepted");
        handler.OnHandJointsUpdated(data);
        Require(Study3HandPoseSource.TryGetSample(out again), "Fresh provider did not recover");
        long oldEpoch = again.TrackingEpoch;
        int beforeSelectedLoss = losses.Count;
        handler.OnSourceLost(new SourceStateEventData { InputSource = source });
        Require(losses.Count == beforeSelectedLoss + 1 && losses[losses.Count - 1] == source.SourceId, "Selected source loss was not emitted");
        handler.OnHandJointsUpdated(data);
        Require(Study3HandPoseSource.TryGetSample(out again) && again.TrackingEpoch != oldEpoch,
            "Loss and recovery between reads did not invalidate history");
        handler.OnSourceLost(new SourceStateEventData { InputSource = source });
        Require(!Study3HandPoseSource.TryGetSample(out again), "Lost source remained usable");
        handler.OnHandJointsUpdated(data); data.InputData.Clear(); handler.OnHandJointsUpdated(data);
        Require(!Study3HandPoseSource.TryGetSample(out again), "Missing fingertip retained prior cache");
        CoreServices.InputSystem = null;
        Require(!Study3HandPoseSource.TryGetSample(out again), "Missing input service retained cache");
        Require(input.Handlers.Count == 0, "Input service change leaked event registrations");
        Require(phaseOrder.Count > 0 && phaseOrder.Count % 7 == 0, "Unexpected number of phase callbacks");
        for (int i = 0; i < phaseOrder.Count; i += 7)
            Require(phaseOrder[i] == "update1" && phaseOrder[i + 1] == "update2"
                && phaseOrder[i + 2] == "processed1" && phaseOrder[i + 3] == "processed2"
                && phaseOrder[i + 4] == "gesture" && phaseOrder[i + 5] == "evaluated1" && phaseOrder[i + 6] == "evaluated2",
                "Motion calculations and triggers ran out of phase");
        Study3HandPoseSource.JointSampleUpdated -= onUpdate;
        Study3HandPoseSource.JointSampleUpdated -= firstUpdate;
        Study3HandPoseSource.JointSampleUpdated -= secondUpdate;
        Study3HandPoseSource.JointSampleProcessed -= firstProcessed;
        Study3HandPoseSource.JointSampleProcessed -= secondProcessed;
        Study3HandPoseSource.JointSampleGesture -= onGesture;
        Study3HandPoseSource.JointSampleEvaluated -= firstEvaluated;
        Study3HandPoseSource.JointSampleEvaluated -= secondEvaluated;
        Study3HandPoseSource.SelectedSourceLost -= onLoss;
        return checks;
    }
}

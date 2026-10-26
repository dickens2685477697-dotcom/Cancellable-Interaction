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
        CoreServices.InputSystem = input; Time.realtimeSinceStartupAsDouble = 20;
        Require(!Study3HandPoseSource.TryGetSample(out Study3MotionSample first), "Invented initial pose");
        Require(input.Handlers.Count == 2, "Provider event handlers not registered exactly once");
        var handler = (Study3HandPoseSource)input.Handlers[0];
        var data = new InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> {
            InputSource = source, Handedness = Handedness.Right,
            InputData = new Dictionary<TrackedHandJoint, MixedRealityPose> {
                { TrackedHandJoint.IndexTip, new MixedRealityPose { Position = Vector3.zero } }
            }
        };
        handler.OnHandJointsUpdated(data);
        Require(Study3HandPoseSource.TryGetSample(out first), "Fresh provider event missing");
        Time.realtimeSinceStartupAsDouble = 20.01;
        Require(Study3HandPoseSource.TryGetSample(out Study3MotionSample again), "Repeated reader lost cached pose");
        Require(first.Sequence == again.Sequence && first.Timestamp == again.Timestamp, "Repeated read invented a fresh ID/time");
        Require(input.Handlers.Count == 2, "Repeated read registered handlers again");
        // Copying the event prevents later changes to MRTK's mutable dictionary altering old samples.
        data.InputData[TrackedHandJoint.IndexTip] = new MixedRealityPose { Position = Vector3.right };
        Require(Study3HandPoseSource.TryGetSample(out again) && again.Position.x == 0, "Mutable event changed cached pose");
        Time.realtimeSinceStartupAsDouble = 20.02;
        data.InputData[TrackedHandJoint.IndexTip] = new MixedRealityPose { Position = Vector3.zero };
        handler.OnHandJointsUpdated(data);
        Require(Study3HandPoseSource.TryGetSample(out again), "New identical pose was discarded");
        Require(again.Sequence > first.Sequence && again.Timestamp > first.Timestamp, "New provider update did not advance identity");
        hand.IsPositionAvailable = false;
        Require(!Study3HandPoseSource.TryGetSample(out again), "Untracked controller accepted");
        hand.IsPositionAvailable = true; Time.realtimeSinceStartupAsDouble = 20.5;
        Require(!Study3HandPoseSource.TryGetSample(out again), "Stale cache accepted");
        handler.OnHandJointsUpdated(data);
        Require(Study3HandPoseSource.TryGetSample(out again), "Fresh provider did not recover");
        long oldEpoch = again.TrackingEpoch;
        handler.OnSourceLost(new SourceStateEventData { InputSource = source });
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
        return checks;
    }
}

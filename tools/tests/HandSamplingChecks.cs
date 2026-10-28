using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.XRSDK.Oculus.Input;
using UnityEngine;

public static class HandSamplingChecks
{
    private static int assertions;
    private static void Check(bool test, string message) { assertions++; if (!test) throw new Exception(message); }
    private static List<Dictionary<string, string>> Read(string path)
    {
        var lines = File.ReadAllLines(path); var header = Study3LogEncoding.ParseCsv(lines[0]);
        return lines.Skip(1).Select(line => {
            var fields = Study3LogEncoding.ParseCsv(line); Check(fields.Count == header.Count, "CSV width");
            return header.Select((h, i) => new { h, value = fields[i] }).ToDictionary(p => p.h, p => p.value);
        }).ToList();
    }
    public static int Run(string directory)
    {
        var history = new Study3SdkStampHistory();
        Check(history.Observe(true, true, 10) == "first_after_gap", "initial SDK stamp");
        Check(history.Observe(true, true, 10) == "same_sdk_timestamp", "same SDK stamp");
        Check(history.Observe(true, true, 11) == "advanced_sdk_timestamp", "advanced SDK stamp");
        Check(history.Observe(true, true, 9) == "regressed_sdk_timestamp", "regressed SDK stamp");
        Check(history.Observe(false, true, 9) == "read_failed", "failed reads cannot reuse stale time");
        Check(history.Observe(true, true, 9) == "first_after_gap", "failure resets comparison");
        Check(history.Observe(true, false, 9) == "not_tracked", "not tracked");
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            Check(history.Observe(true, true, invalid) == "invalid_timestamp", "invalid SDK time");
        Check(!Study3SamplingCorrelation.SamePosition(new Vector3(float.NaN, 0, 0), new Vector3(float.NaN, 0, 0)), "NaN never matches");
        Check(!Study3SamplingCorrelation.SamePosition(new Vector3(1, 0, 0), new Vector3(1.000001f, 0, 0)), "no tolerance merges different poses");

        var culture = CultureInfo.CurrentCulture; CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var source = new InputSource { SourceId = 42 };
            var hand = new OculusHand { InputSource = source, ControllerHandedness = Handedness.Right };
            hand.Joints[TrackedHandJoint.IndexTip] = new MixedRealityPose { Position = new Vector3(1.25f, 2, 3) };
            hand.Joints[TrackedHandJoint.ThumbTip] = new MixedRealityPose { Position = new Vector3(4, 5, 6) };
            hand.Joints[TrackedHandJoint.IndexDistalJoint] = new MixedRealityPose { Position = new Vector3(7, 8, 9) };
            CoreServices.InputSystem.DetectedControllers.Add(hand);
            var sdk = new OVRHandSamplingMetadata { HandInstanceId = 7, Hand = OVRPlugin.Hand.HandRight, ReadSequence = 1,
                ReadSucceeded = true, IsTracked = true, SampleTimeStamp = 12, RequestedTimeStamp = 12.05 };
            var skeleton = new OVRSkeleton { Side = OVRSkeleton.SkeletonType.HandRight, AppliedSamplingMetadata = sdk };
            foreach (var pair in new[] { (TrackedHandJoint.IndexTip, OVRSkeleton.BoneId.Hand_IndexTip),
                (TrackedHandJoint.ThumbTip, OVRSkeleton.BoneId.Hand_ThumbTip), (TrackedHandJoint.IndexDistalJoint, OVRSkeleton.BoneId.Hand_Index3) })
                skeleton.Bones.Add(new OVRSkeleton.Bone { Id = pair.Item2, Transform = new Transform { position = hand.Joints[pair.Item1].Position } });
            UnityEngine.Object.Found = new UnityEngine.Object[] { skeleton };
            var data = new InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> { InputSource = source, Handedness = Handedness.Right, InputData = hand.Joints };
            var owner = new Component();
            using (var session = Study3HandSamplingDiagnostics.StartSession(directory, "test", () => new Dictionary<string, object> { { "mode", "speed" }, { "label", "中文,trace" } }))
            {
                Time.realtimeSinceStartupAsDouble = 100;
                OVRHand.Publish(sdk);
                var physics = sdk; physics.Step = OVRPlugin.Step.Physics; physics.SampleTimeStamp = 9;
                OVRHand.Publish(physics);
                var otherHand = sdk; otherHand.HandInstanceId = 8; otherHand.SampleTimeStamp = 99;
                OVRHand.Publish(otherHand);
                Study3HandSamplingDiagnostics.ProviderUpdate(data, 1000, 99.9);
                int initialQueries = HandJointUtils.Queries;
                Check(Study3HandSamplingDiagnostics.TryGetJointPose(owner, TrackedHandJoint.IndexTip, Handedness.Any, out var pose, 8, "consume"), "unchanged success");
                Check(pose.Position.x == 1.25f && HandJointUtils.Queries == initialQueries + 1, "one original query with unchanged pose");
                Study3HandSamplingDiagnostics.TryGetJointPose(owner, TrackedHandJoint.IndexTip, Handedness.Any, out pose, 9, "consume");
                // New event, identical position and SDK stamp: a new provider event, not a cache reread.
                Study3HandSamplingDiagnostics.ProviderUpdate(data, 1001, 100);
                Study3HandSamplingDiagnostics.TryGetJointPose(owner, TrackedHandJoint.IndexTip, Handedness.Any, out pose, 10, "consume");
                OVRHand.Publish(sdk);
                // A later SDK state must not relabel the older applied skeleton pose.
                sdk.SampleTimeStamp = 13; sdk.ReadSequence = 2; OVRHand.Publish(sdk);
                Study3HandSamplingDiagnostics.ProviderUpdate(data, 1002, 100.1);
                Study3HandSamplingDiagnostics.SharedRead(owner, true, new Study3MotionSample { SourceId = 42, Sequence = 1002, Position = pose.Position }, "shared");
                Study3HandSamplingDiagnostics.SharedRead(owner, true, new Study3MotionSample { SourceId = 42, Sequence = 1001, Position = pose.Position }, "shared");
                hand.IsPositionAvailable = false;
                Study3HandSamplingDiagnostics.TryGetJointPose(owner, TrackedHandJoint.IndexTip, Handedness.Any, out pose, 11, "consume");
                hand.Joints[TrackedHandJoint.IndexTip] = new MixedRealityPose { Position = new Vector3(99, 0, 0) };
                Study3HandSamplingDiagnostics.TryGetJointPose(owner, TrackedHandJoint.IndexTip, Handedness.Any, out pose, 12, "consume");
                hand.Joints.Remove(TrackedHandJoint.IndexTip);
                Check(!Study3HandSamplingDiagnostics.TryGetJointPose(owner, TrackedHandJoint.IndexTip, Handedness.Any, out pose, 13, "consume"), "unchanged failure");
                Study3HandSamplingDiagnostics.SourceEvent(42, "source_lost");
                hand.Joints[TrackedHandJoint.IndexTip] = new MixedRealityPose { Position = new Vector3(1.25f, 2, 3) };
                Study3HandSamplingDiagnostics.TryGetJointPose(owner, TrackedHandJoint.IndexTip, Handedness.Any, out pose, 14, "consume");
                // Multiple matching skeletons cannot be attributed to one SDK state.
                UnityEngine.Object.Found = new UnityEngine.Object[] { skeleton, skeleton };
                Time.realtimeSinceStartupAsDouble = 102;
                Study3HandSamplingDiagnostics.ProviderUpdate(data, 1003);
                sdk.ReadSucceeded = false; sdk.SampleTimeStamp = double.NaN; OVRHand.Publish(sdk);
            }
            var reads = Read(Path.Combine(directory, "test-hand-reads.csv"));
            var updates = Read(Path.Combine(directory, "test-hand-updates.csv"));
            Check(reads[0]["x"] == "1.25" && reads[0]["motionSequence"] == "8", "invariant numbers and motion linkage");
            Check(reads[0]["sameProviderUpdateAsPreviousRead"] == "0", "first read");
            Check(reads[1]["sameProviderUpdateAsPreviousRead"] == "1", "same provider reused");
            Check(reads[2]["sameProviderUpdateAsPreviousRead"] == "0", "identical new pose is fresh provider event");
            Check(reads[3]["matchStatus"] == "exact_shared_sample_sequence", "shared sample linked");
            Check(reads[4]["providerUpdateId"] == "0", "old shared sequence not linked by equal position");
            Check(reads[5]["poseValid"] == "1" && reads[5]["controllerPositionAvailable"] == "0", "tracking availability separate from pose query");
            Check(reads[6]["providerUpdateId"] == "0", "mismatching pose unlinked");
            Check(reads[7]["poseValid"] == "0" && reads[7]["x"] == "NA", "invalid pose has no stale coordinates");
            Check(reads[8]["providerUpdateId"] == "0", "source lost clears event linkage");
            var providerRows = updates.Where(r => r["event"] == "provider_update").ToArray();
            Check(providerRows[2]["sdkSampleTimeStamp"] == "12" && providerRows[2]["sdkReadSequence"] == "1", "applied pose time, not latest SDK time");
            Check(providerRows[3]["sdkSampleTimeStamp"] == "NA" && providerRows[3]["detailsJson"].Contains("ambiguous_skeleton_match"), "ambiguous SDK attribution absent");
            Check(updates.Any(r => r["event"] == "sdk_read" && r["detailsJson"].Contains("same_sdk_timestamp")), "same SDK stamp recorded");
            Check(updates.Any(r => r["event"] == "sdk_read" && r["detailsJson"].Contains("Physics")
                && r["detailsJson"].Contains("first_after_gap")), "different SDK phase has separate time history");
            Check(updates.Any(r => r["event"] == "sdk_read" && r["sdkHandInstanceId"] == "8"
                && r["detailsJson"].Contains("first_after_gap")), "different hand instance has separate time history");
            Check(updates.Last(r => r["event"] == "sdk_read")["sdkSampleTimeStamp"] == "NA", "failed SDK call does not export stale timestamp");
            Check(updates.Last()["event"] == "session_end" && updates.Last()["detailsJson"].Contains("poseReadCount"), "close flush and session counters");
            Check(updates[0]["detailsJson"].Contains("中文,trace"), "UTF8 and CSV escaped context preserved");
            Check(OVRHand.SubscriberCount == 0, "SDK observer unsubscribed after close");
            int before = updates.Count; OVRHand.Publish(sdk);
            Check(File.ReadAllLines(Path.Combine(directory, "test-hand-updates.csv")).Length == before + 1, "closed session unsubscribed");
            // Unwritable destination is isolated from the original pose query.
            string blocked = Path.Combine(directory, "file-is-not-directory"); File.WriteAllText(blocked, "x");
            using (var failed = Study3HandSamplingDiagnostics.StartSession(blocked, "failure", null))
                Check(Study3HandSamplingDiagnostics.TryGetJointPose(owner, TrackedHandJoint.IndexTip, Handedness.Any, out var unchanged)
                    && unchanged.Position.x == 1.25f, "I/O failure cannot change algorithm input");
            using (var one = Study3HandSamplingDiagnostics.StartSession(directory, "one", null))
            using (var two = Study3HandSamplingDiagnostics.StartSession(directory, "two", null))
            {
                Check(OVRHand.SubscriberCount == 1, "overlapping sessions share one SDK observer");
                OVRHand.Publish(sdk); one.Dispose(); OVRHand.Publish(sdk);
                Check(OVRHand.SubscriberCount == 1, "remaining session retains observer");
            }
            Check(OVRHand.SubscriberCount == 0, "last overlapping session unsubscribes");
            Check(Read(Path.Combine(directory, "one-hand-updates.csv")).Count(r => r["event"] == "sdk_read") == 1, "closed session no longer receives updates");
            Check(Read(Path.Combine(directory, "two-hand-updates.csv")).Count(r => r["event"] == "sdk_read") == 2, "active session receives each read once");
        }
        finally { CultureInfo.CurrentCulture = culture; }
        return assertions;
    }
}

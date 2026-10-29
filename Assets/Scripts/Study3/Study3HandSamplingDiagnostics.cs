using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

/// <summary>Observation only. Never polls GetHandState and never filters a pose used by an algorithm.</summary>
public static class Study3HandSamplingDiagnostics
{
    public const string Version = "hand-sampling-v1";
    private sealed class Update
    {
        public long Id, SharedSequence;
        public uint Source;
        public Handedness Hand;
        public readonly Dictionary<TrackedHandJoint, MixedRealityPose> Joints = new Dictionary<TrackedHandJoint, MixedRealityPose>();
    }
    private static readonly List<Study3HandSamplingSession> sessions = new List<Study3HandSamplingSession>();
    private static readonly Dictionary<uint, Update> updates = new Dictionary<uint, Update>();
    private static readonly Dictionary<string, Study3SdkStampHistory> sdkHistory = new Dictionary<string, Study3SdkStampHistory>();
    private static readonly Dictionary<string, long> previousReads = new Dictionary<string, long>();
    private static OVRSkeleton[] skeletons;
    private static double nextSkeletonRefresh;
    private static long updateId, readId;

    public static Study3HandSamplingSession StartSession(string directory, string sessionId, Func<object> context)
    {
        var session = new Study3HandSamplingSession(directory, sessionId, context);
        if (sessions.Count == 0)
        {
            updates.Clear(); previousReads.Clear(); sdkHistory.Clear(); skeletons = null;
            OVRHand.SamplingStateRead += OnSdkRead;
        }
        sessions.Add(session);
        Study3HandPoseSource.EnsureAvailable();
        string runtimeApi;
        try { runtimeApi = OVRPlugin.nativeXrApi.ToString(); }
        catch (Exception) { runtimeApi = "unavailable"; }
        session.WriteUpdate("session_start", 0, Handedness.None, 0, default, new Dictionary<string, object> {
            { "version", Version }, { "unityVersion", Application.unityVersion },
            { "deviceModel", SystemInfo.deviceModel }, { "applicationBuildGuid", Application.buildGUID },
            { "sdkWrapperVersion", OVRPlugin.wrapperVersion.ToString() },
            { "runtimeXrApi", runtimeApi },
            { "sdkTimestampClock", "OVRPlugin SDK clock; not Unity realtime" },
            { "sdkTimestampMeaning", "GetHandState timestamps; predicted pose, camera capture time not verified" },
            { "providerMatchMeaning", "unique active Oculus skeleton with matching joint positions; not a hardware frame ID" }
        });
        return session;
    }

    internal static void Remove(Study3HandSamplingSession session)
    {
        sessions.Remove(session);
        if (sessions.Count != 0) return;
        OVRHand.SamplingStateRead -= OnSdkRead;
        updates.Clear(); previousReads.Clear(); sdkHistory.Clear(); skeletons = null;
    }

    private static void Emit(string kind, uint source, Handedness hand, long id,
        OVRHandSamplingMetadata sdk, Dictionary<string, object> details)
    {
        foreach (var session in sessions) session.WriteUpdate(kind, source, hand, id, sdk, details);
    }

    private static void OnSdkRead(OVRHandSamplingMetadata sdk)
    {
        try
        {
            string historyKey = sdk.HandInstanceId + ":" + sdk.Step;
            if (!sdkHistory.TryGetValue(historyKey, out var history))
                sdkHistory[historyKey] = history = new Study3SdkStampHistory();
            Emit("sdk_read", 0, sdk.Hand == OVRPlugin.Hand.HandLeft ? Handedness.Left
                : sdk.Hand == OVRPlugin.Hand.HandRight ? Handedness.Right : Handedness.None, 0, sdk,
                new Dictionary<string, object> {
                    { "readSucceeded", sdk.ReadSucceeded }, { "tracked", sdk.IsTracked },
                    { "status", sdk.Status.ToString() }, { "confidence", sdk.Confidence.ToString() },
                    { "step", sdk.Step.ToString() }, { "sdkReadFrame", sdk.Frame },
                    { "readStartedRealtime", sdk.ReadStartedRealtime }, { "readCompletedRealtime", sdk.ReadCompletedRealtime },
                    { "sampleTimestampRelation", history.Observe(sdk.ReadSucceeded, sdk.IsTracked, sdk.SampleTimeStamp) },
                    { "requestedTimestampValid", Study3SdkStampHistory.IsValid(sdk.RequestedTimeStamp) }
                });
        }
        catch (Exception error) { Debug.LogError("Hand sampling observation failed: " + error.Message); }
    }

    // Called by the existing MRTK receiver, for every mode. The shared sample ID is
    // assigned by that receiver, so linkage does not depend on handler ordering.
    public static void ProviderUpdate(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> data, long sharedSequence,
        double sharedReceivedRealtime = double.NaN)
    {
        if (sessions.Count == 0 || data.InputSource == null) return;
        try
        {
            double receivedRealtime = Time.realtimeSinceStartupAsDouble;
            var update = new Update { Id = ++updateId, Source = data.InputSource.SourceId, Hand = data.Handedness, SharedSequence = sharedSequence };
            if (data.InputData != null)
                foreach (var pair in data.InputData) update.Joints[pair.Key] = pair.Value;
            updates[update.Source] = update;
            var controller = FindController(update.Source);
            string match;
            OVRSkeleton skeleton = null;
            OVRHandSamplingMetadata sdk = default;
            if (controller?.GetType().FullName == "Microsoft.MixedReality.Toolkit.XRSDK.Oculus.Input.OculusHand")
                sdk = MatchSkeleton(update, out skeleton, out match);
            else match = "provider_not_oculus_hand";
            var details = new Dictionary<string, object> {
                { "providerReceivedRealtime", receivedRealtime }, { "sharedSampleReceivedRealtime", sharedReceivedRealtime },
                { "controllerType", controller?.GetType().FullName },
                { "controllerPositionAvailable", controller?.IsPositionAvailable },
                { "matchStatus", match }, { "sharedSampleSequence", sharedSequence },
                { "mrtkEventUtc", data.EventTime.ToUniversalTime().ToString("o") },
                { "mrtkEventTimeMeaning", "application event creation time, not sensor capture" },
                { "jointCount", update.Joints.Count },
                { "skeletonInstanceId", skeleton != null ? (object)skeleton.GetInstanceID() : null },
                { "skeletonApplySequence", skeleton != null ? (object)skeleton.SamplingApplySequence : null },
                { "skeletonAppliedRealtime", skeleton != null ? (object)skeleton.SamplingAppliedRealtime : null },
                { "skeletonPoseValid", skeleton != null && skeleton.SamplingPoseValid },
                { "skeletonDataValid", skeleton != null && skeleton.IsDataValid },
                { "skeletonHighConfidence", skeleton != null && skeleton.IsDataHighConfidence },
                { "sdkReadSucceeded", sdk.ReadSucceeded }, { "sdkTracked", sdk.IsTracked },
                { "sdkConfidence", sdk.Confidence.ToString() },
                { "sdkSampleTimestampValid", sdk.ReadSucceeded && Study3SdkStampHistory.IsValid(sdk.SampleTimeStamp) }
            };
            foreach (var joint in new[] { TrackedHandJoint.IndexTip, TrackedHandJoint.IndexDistalJoint, TrackedHandJoint.ThumbTip, TrackedHandJoint.Wrist })
                details[joint.ToString()] = update.Joints.TryGetValue(joint, out var pose)
                    ? new object[] { pose.Position.x, pose.Position.y, pose.Position.z } : null;
            Emit("provider_update", update.Source, update.Hand, update.Id, sdk, details);
        }
        catch (Exception error) { Debug.LogError("Hand provider observation failed: " + error.Message); }
    }

    private static OVRHandSamplingMetadata MatchSkeleton(Update update, out OVRSkeleton matched, out string status)
    {
        matched = null;
        if (skeletons == null || Time.realtimeSinceStartupAsDouble >= nextSkeletonRefresh)
        {
            skeletons = UnityEngine.Object.FindObjectsOfType<OVRSkeleton>();
            nextSkeletonRefresh = Time.realtimeSinceStartupAsDouble + 1;
        }
        int matches = 0;
        foreach (var skeleton in skeletons)
        {
            if (skeleton == null || !skeleton.isActiveAndEnabled || !skeleton.IsInitialized) continue;
            bool handMatches = update.Hand == Handedness.Left && skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft
                || update.Hand == Handedness.Right && skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight;
            if (!handMatches || skeleton.SamplingApplySequence == 0 || skeleton.AppliedSamplingMetadata.ReadSequence == 0) continue;
            // MRTK OculusHand copies these bone world positions directly. Check three
            // joints; equal stationary poses still do not prove a physical frame ID.
            if (!BoneMatches(update, skeleton, TrackedHandJoint.IndexTip, OVRSkeleton.BoneId.Hand_IndexTip)
                || !BoneMatches(update, skeleton, TrackedHandJoint.ThumbTip, OVRSkeleton.BoneId.Hand_ThumbTip)
                || !BoneMatches(update, skeleton, TrackedHandJoint.IndexDistalJoint, OVRSkeleton.BoneId.Hand_Index3)) continue;
            matched = skeleton; matches++;
        }
        status = matches == 1 ? "unique_oculus_skeleton_pose_match" : matches == 0 ? "no_matching_oculus_skeleton" : "ambiguous_skeleton_match";
        if (matches != 1) { matched = null; return default; }
        return matched.AppliedSamplingMetadata;
    }

    private static bool BoneMatches(Update update, OVRSkeleton skeleton, TrackedHandJoint joint, OVRSkeleton.BoneId id)
    {
        if (!update.Joints.TryGetValue(joint, out var pose)) return false;
        foreach (var bone in skeleton.Bones)
            if (bone.Id == id && bone.Transform != null)
                return Study3SamplingCorrelation.SamePosition(pose.Position, bone.Transform.position);
        return false;
    }

    private static IMixedRealityController FindController(uint source)
    {
        if (CoreServices.InputSystem?.DetectedControllers == null) return null;
        foreach (var controller in CoreServices.InputSystem.DetectedControllers)
            if (controller.InputSource?.SourceId == source) return controller;
        return null;
    }

    public static void SourceEvent(uint source, string kind)
    {
        if (sessions.Count == 0) return;
        try
        {
            updates.Remove(source);
            // A new tracking interval must not inherit a previous read's reuse flag.
            previousReads.Clear();
            Emit(kind, source, Handedness.None, 0, default, null);
        }
        catch (Exception error) { Debug.LogError("Hand source observation failed: " + error.Message); }
    }

    /// <summary>Returns the original HandJointUtils result unchanged.</summary>
    public static bool TryGetJointPose(Component owner, TrackedHandJoint joint, Handedness hand,
        out MixedRealityPose pose, long motionSequence = 0, [CallerMemberName] string method = "")
    {
        bool valid = HandJointUtils.TryGetJointPose(joint, hand, out pose);
        if (sessions.Count == 0) return valid;
        try
        {
            var controller = HandJointUtils.FindHand(hand);
            uint source = controller?.InputSource?.SourceId ?? 0;
            updates.TryGetValue(source, out var update);
            bool linked = valid && update != null && update.Joints.TryGetValue(joint, out var published)
                && Study3SamplingCorrelation.SamePosition(pose.Position, published.Position);
            WriteRead(owner, method, joint, valid, pose.Position, source, controller?.ControllerHandedness ?? hand,
                linked ? update.Id : 0, 0, !valid ? "pose_unavailable" : linked ? "matches_source_event_pose" : "no_matching_source_event",
                controller?.IsPositionAvailable, motionSequence);
        }
        catch (Exception error) { Debug.LogError("Hand consumption observation failed: " + error.Message); }
        return valid;
    }

    public static void SharedRead(Component owner, bool valid, Study3MotionSample sample, [CallerMemberName] string method = "")
    {
        if (sessions.Count == 0) return;
        try
        {
            updates.TryGetValue(sample.SourceId, out var update);
            bool linked = valid && update != null && update.SharedSequence == sample.Sequence;
            WriteRead(owner, method, TrackedHandJoint.IndexTip, valid, sample.Position, sample.SourceId,
                update?.Hand ?? Handedness.None, linked ? update.Id : 0, sample.Sequence,
                !valid ? "shared_sample_unavailable" : linked ? "exact_shared_sample_sequence" : "shared_sequence_not_observed",
                FindController(sample.SourceId)?.IsPositionAvailable);
        }
        catch (Exception error) { Debug.LogError("Shared hand consumption observation failed: " + error.Message); }
    }

    private static void WriteRead(Component owner, string method, TrackedHandJoint joint, bool valid,
        Vector3 pose, uint source, Handedness hand, long providerId, long sharedSequence, string status, bool? tracked, long motionSequence = 0)
    {
        int ownerId = owner != null ? owner.GetInstanceID() : 0;
        string key = ownerId + ":" + method + ":" + joint;
        bool repeated = providerId != 0 && previousReads.TryGetValue(key, out long previous) && previous == providerId;
        previousReads[key] = providerId;
        long id = ++readId;
        foreach (var session in sessions)
            session.WriteRead(id, ownerId, owner != null ? owner.GetType().Name : "", method, joint, valid,
                pose, source, hand, providerId, sharedSequence, status, repeated, tracked, motionSequence);
    }
}

// Sampling baseline diagnostics import verification (2026-10-28); no selection rule change.

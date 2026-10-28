using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object
    {
        public static Object[] Found = new Object[0];
        private static int next; private int id = ++next;
        public int GetInstanceID() => id;
        public static T[] FindObjectsOfType<T>() where T : Object
        { var values = new List<T>(); foreach (var item in Found) if (item is T typed) values.Add(typed); return values.ToArray(); }
    }
    public class Component : Object { }
    public class Transform { public Vector3 position; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public static class Time
    { public static int frameCount; public static double timeAsDouble, fixedTimeAsDouble, realtimeSinceStartupAsDouble; public static bool inFixedTimeStep; }
    public static class Application { public static string unityVersion = "test", buildGUID = "test-build"; }
    public static class SystemInfo { public static string deviceModel = "test-device"; }
    public static class Debug { public static List<string> Errors = new List<string>(); public static void LogError(object value) => Errors.Add(value.ToString()); }
}
namespace Microsoft.MixedReality.Toolkit.Utilities
{
    public enum Handedness { None, Left, Right, Any }
    public enum TrackedHandJoint { IndexTip, IndexDistalJoint, ThumbTip, Wrist }
    public struct MixedRealityPose { public UnityEngine.Vector3 Position; }
}
namespace Microsoft.MixedReality.Toolkit.Input
{
    using Microsoft.MixedReality.Toolkit.Utilities;
    public interface IMixedRealityInputSource { uint SourceId { get; } }
    public class InputSource : IMixedRealityInputSource { public uint SourceId { get; set; } }
    public interface IMixedRealityController { IMixedRealityInputSource InputSource { get; } Handedness ControllerHandedness { get; } bool IsPositionAvailable { get; } }
    public interface IMixedRealityHand : IMixedRealityController { bool TryGetJoint(TrackedHandJoint joint, out MixedRealityPose pose); }
    public class InputSystem { public HashSet<IMixedRealityController> DetectedControllers = new HashSet<IMixedRealityController>(); }
    public class InputEventData<T>
    { public IMixedRealityInputSource InputSource; public Handedness Handedness; public T InputData; public DateTime EventTime = DateTime.UtcNow; }
    public static class HandJointUtils
    {
        public static int Queries;
        public static IMixedRealityHand FindHand(Handedness requested)
        {
            foreach (var c in Microsoft.MixedReality.Toolkit.CoreServices.InputSystem.DetectedControllers)
                if (c is IMixedRealityHand h && (requested == Handedness.Any || c.ControllerHandedness == requested)) return h;
            return null;
        }
        public static bool TryGetJointPose(TrackedHandJoint joint, Handedness requested, out MixedRealityPose pose)
        { Queries++; var hand = FindHand(requested); if (hand != null) return hand.TryGetJoint(joint, out pose); pose = default; return false; }
    }
}
namespace Microsoft.MixedReality.Toolkit { public static class CoreServices { public static Input.InputSystem InputSystem = new Input.InputSystem(); } }
namespace Microsoft.MixedReality.Toolkit.XRSDK.Oculus.Input
{
    using Microsoft.MixedReality.Toolkit.Input;
    using Microsoft.MixedReality.Toolkit.Utilities;
    public class OculusHand : IMixedRealityHand
    {
        public IMixedRealityInputSource InputSource { get; set; }
        public Handedness ControllerHandedness { get; set; }
        public bool IsPositionAvailable { get; set; } = true;
        public Dictionary<TrackedHandJoint, MixedRealityPose> Joints = new Dictionary<TrackedHandJoint, MixedRealityPose>();
        public bool TryGetJoint(TrackedHandJoint joint, out MixedRealityPose pose) => Joints.TryGetValue(joint, out pose);
    }
}
public static class OVRPlugin
{
    public enum Hand { None, HandLeft, HandRight }
    public enum Step { Render, Physics }
    public enum HandStatus { None, HandTracked }
    public enum TrackingConfidence { Low, High }
    public static Version wrapperVersion = new Version(1, 76, 0);
    public static string nativeXrApi = "test-api";
}
public class OVRHand
{
    public static event Action<OVRHandSamplingMetadata> SamplingStateRead;
    public static int SubscriberCount => SamplingStateRead?.GetInvocationList().Length ?? 0;
    public static void Publish(OVRHandSamplingMetadata metadata) => SamplingStateRead?.Invoke(metadata);
}
public class OVRSkeleton : UnityEngine.Object
{
    public enum SkeletonType { HandLeft, HandRight }
    public enum BoneId { Hand_IndexTip, Hand_ThumbTip, Hand_Index3 }
    public class Bone { public BoneId Id; public UnityEngine.Transform Transform = new UnityEngine.Transform(); }
    public bool isActiveAndEnabled = true, IsInitialized = true, IsDataValid = true, IsDataHighConfidence = true, SamplingPoseValid = true;
    public long SamplingApplySequence = 1;
    public double SamplingAppliedRealtime;
    public OVRHandSamplingMetadata AppliedSamplingMetadata;
    public List<Bone> Bones = new List<Bone>();
    public SkeletonType Side;
    public SkeletonType GetSkeletonType() => Side;
}
public static class Study3HandPoseSource { public static void EnsureAvailable() { } }
public struct Study3MotionSample
{
    public uint SourceId; public long Sequence; public UnityEngine.Vector3 Position;
}

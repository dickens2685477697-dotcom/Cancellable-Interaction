using System;
using System.Collections.Generic;
namespace UnityEngine {
    public class MonoBehaviour { }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public struct Vector3 {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static float Distance(Vector3 a, Vector3 b) {
            var x=a.x-b.x; var y=a.y-b.y; var z=a.z-b.z;
            return (float)Math.Sqrt(x*x+y*y+z*z);
        }
    }
    public static class Time { public static double realtimeSinceStartupAsDouble; public static float deltaTime = .02f; }
}
namespace UnityEngine.Events {
    public class UnityEvent { public int Count; public void Invoke() { Count++; } }
}
namespace Microsoft.MixedReality.Toolkit.Input {
    public enum TrackedHandJoint { ThumbTip, MiddleTip, RingTip, PinkyTip, IndexTip }
}
namespace Microsoft.MixedReality.Toolkit.Utilities {
    public enum Handedness { Any }
    public struct MixedRealityPose { public UnityEngine.Vector3 Position; }
}
public static class Study3HandSamplingDiagnostics {
    public static readonly Dictionary<Microsoft.MixedReality.Toolkit.Input.TrackedHandJoint, UnityEngine.Vector3> Positions =
        new Dictionary<Microsoft.MixedReality.Toolkit.Input.TrackedHandJoint, UnityEngine.Vector3>();
    public static bool TryGetJointPose(object owner, Microsoft.MixedReality.Toolkit.Input.TrackedHandJoint joint,
        Microsoft.MixedReality.Toolkit.Utilities.Handedness hand, out Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose pose) {
        UnityEngine.Vector3 value;
        bool found=Positions.TryGetValue(joint, out value);
        pose=new Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose { Position = value };
        return found;
    }
}

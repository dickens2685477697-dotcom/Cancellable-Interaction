using System;
using System.Collections.Generic;

namespace UnityEngine {
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float v) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string v) {} }
    public class MonoBehaviour {
        public Transform transform = new Transform();
        public bool isActiveAndEnabled = true;
        public System.Collections.IEnumerator pending;
        public readonly List<ExperimentCounter> experiments = new List<ExperimentCounter>();
        public void StartCoroutine(System.Collections.IEnumerator routine) { pending=routine; pending.MoveNext(); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:class {
            var result=new List<T>(); foreach(var item in experiments) if(item is T t) result.Add(t); return result.ToArray();
        }
    }
    public class GameObject {
        public bool activeSelf;
        public readonly Transform transform;
        public GameObject(bool active = true) { activeSelf = active; transform = new Transform { gameObject = this }; }
        public void SetActive(bool active) { activeSelf = active; }
    }
    public class Transform {
        public GameObject gameObject;
        public Vector3 position, forward = Vector3.forward;
        public Quaternion rotation = new Quaternion { w=1 };
        public readonly List<Transform> children = new List<Transform>();
        public int childCount => children.Count;
        public Transform GetChild(int i) => children[i];
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { position = p; rotation = r; }
        public Vector3 TransformPoint(Vector3 p) => position + rotation * p;
    }
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 forward => new Vector3(0,0,1);
        public float sqrMagnitude => x*x+y*y+z*z;
        public Vector3 normalized { get { float l=(float)Math.Sqrt(sqrMagnitude); return new Vector3(x/l,y/l,z/l); } }
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b) => new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 ProjectOnPlane(Vector3 v,Vector3 n) => new Vector3(v.x,0,v.z);
        public static float Distance(Vector3 a,Vector3 b) => (float)Math.Sqrt((a-b).sqrMagnitude);
    }
    public struct Quaternion {
        public float x,y,z,w;
        public static Quaternion operator *(Quaternion a,Quaternion b) => new Quaternion {
            x=a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,
            y=a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,
            z=a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,
            w=a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z
        };
        public static Vector3 operator *(Quaternion q,Vector3 v) {
            var p=new Quaternion{x=v.x,y=v.y,z=v.z};
            var inverse=new Quaternion{x=-q.x,y=-q.y,z=-q.z,w=q.w};
            var result=q*p*inverse;
            return new Vector3(result.x,result.y,result.z);
        }
    }
    public static class Mathf {
        public static float Max(float a,float b) => Math.Max(a,b);
        public static float Clamp(float v,float min,float max) => Math.Max(min,Math.Min(max,v));
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
    }
    public class Camera : MonoBehaviour { public static Camera main; }
    public class SphereCollider { public Transform transform = new Transform(); public Vector3 center; }
    public static class Time { public static double realtimeSinceStartupAsDouble; }
    public static class Debug {
        public static int errorCount, warningCount;
        public static void LogFormat(string s, params object[] args) {}
        public static void LogError(string s) { errorCount++; }
        public static void LogWarning(string s) { warningCount++; }
    }
    public static class SubsystemManager {
        public static readonly List<UnityEngine.XR.XRInputSubsystem> current = new List<UnityEngine.XR.XRInputSubsystem>();
        public static void GetInstances(List<UnityEngine.XR.XRInputSubsystem> target) { target.AddRange(current); }
    }
}
public class ExperimentCounter { public bool isActiveAndEnabled=true; public int resetCount; public void OnReferenceSpaceReset(){resetCount++;} }
public static class OVRPlugin { public static bool initialized; public static int recenterCount; public static int GetLocalTrackingSpaceRecenterCount()=>recenterCount; }
namespace UnityEngine.XR {
    public class XRInputSubsystem { public event Action<XRInputSubsystem> trackingOriginUpdated; public void Recenter(){trackingOriginUpdated?.Invoke(this);} }
    public enum XRNode { Head }
    public struct InputFeatureUsage<T> {}
    public static class CommonUsages { public static InputFeatureUsage<bool> isTracked; public static InputFeatureUsage<UnityEngine.Vector3> devicePosition; public static InputFeatureUsage<UnityEngine.Quaternion> deviceRotation; }
    public struct InputDevice {
        public bool isValid => InputDevices.valid;
        public bool TryGetFeatureValue(InputFeatureUsage<bool> usage, out bool v) { v=InputDevices.tracked; return InputDevices.boolFeature; }
        public bool TryGetFeatureValue(InputFeatureUsage<UnityEngine.Vector3> usage, out UnityEngine.Vector3 v) { v=InputDevices.position; return InputDevices.positionFeature; }
        public bool TryGetFeatureValue(InputFeatureUsage<UnityEngine.Quaternion> usage, out UnityEngine.Quaternion v) { v=InputDevices.rotation; return InputDevices.rotationFeature; }
    }
    public static class InputDevices {
        public static bool valid,tracked,boolFeature,positionFeature,rotationFeature;
        public static UnityEngine.Vector3 position;
        public static UnityEngine.Quaternion rotation;
        public static InputDevice GetDeviceAtXRNode(XRNode n) => new InputDevice();
    }
    public static class XRSettings { public static bool enabled; }
}
namespace Microsoft.MixedReality.Toolkit {
    public static class MixedRealityPlayspace { public static UnityEngine.Transform Transform = new UnityEngine.Transform(); }
}

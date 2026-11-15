using System;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
namespace UnityEditor { }
namespace UnityEngine.Rendering { }
namespace UnityEngine.EventSystems { }
namespace UnityEngine.UI { public class CanvasScaler { } }
namespace Microsoft.MixedReality.Toolkit.UI { }
namespace TMPro { public class TMP_Text { public string text; } public class TextMeshProUGUI {} }
namespace UnityEngine.XR.ARFoundation { }
namespace UnityEngine {
 public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public float magnitude => (float)Math.Sqrt(x*x+y*y+z*z); public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude; }
 public struct Quaternion { public float x,y,z,w; }
 public class Transform { public Vector3 position; public Vector3 TransformPoint(Vector3 p)=>new Vector3(position.x+p.x,position.y+p.y,position.z+p.z); }
 public struct Bounds { public bool Contains(Vector3 p)=>p.x >= 0 && p.x <= 1; }
 public class Collider { public Bounds bounds; }
 public class SphereCollider : Collider { public Vector3 center; public Transform transform=new Transform(); }
 public class Material { }
 public class MeshRenderer { public Material material; }
 public struct Scene { public string path; }
 public class GameObject { public Scene scene; public string name="Test"; public bool active=true; private Collider collider=new Collider(); private MeshRenderer renderer=new MeshRenderer(); public AcceStimulate acce; public ExperimentCounter experiment; public Study1HeadAnchor anchor; public SphereCollider sphere; public StartBallBehavior2 startBall; public void SetActive(bool value){active=value;} public T GetComponent<T>() where T:class { if(typeof(T)==typeof(Collider))return collider as T; if(typeof(T)==typeof(SphereCollider))return sphere as T; if(typeof(T)==typeof(MeshRenderer))return renderer as T; if(typeof(T)==typeof(AcceStimulate))return acce as T; if(typeof(T)==typeof(ExperimentCounter))return experiment as T; return null; } }
 public class MonoBehaviour { public GameObject gameObject=new GameObject(); public string name=>gameObject.name; public bool isActiveAndEnabled=true,enabled=true; public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>(); public T GetComponentInParent<T>() where T:class=>gameObject.anchor as T; public T GetComponentInChildren<T>(bool includeInactive) where T:class=>gameObject.startBall as T; public void print(object v){} }
 public static class Time { public static double realtimeSinceStartupAsDouble, fixedTimeAsDouble; public static float fixedDeltaTime=.02f,deltaTime=.02f,timeScale=1f,time; }
 public static class Application { public static string persistentDataPath=System.IO.Path.GetTempPath(); }
 public static class Debug { public static void Log(object value){} }
}
namespace UnityEngine.Events { public class UnityEvent { event Action e; public void AddListener(Action a){e+=a;} public void Invoke(){e?.Invoke();} } public class UnityEvent<T> { } }
namespace Microsoft.MixedReality.Toolkit.Utilities { public enum Handedness { Any } public enum TrackedHandJoint { IndexTip,IndexDistalJoint,IndexMiddleJoint } public struct MixedRealityPose { public Vector3 Position; } }
namespace Microsoft.MixedReality.Toolkit.Input { public static class HandJointUtils { public static int Calls; public static MixedRealityPose Pose; public static bool TryGetJointPose(TrackedHandJoint j,Handedness h,out MixedRealityPose p){Calls++;p=Pose;return true;} } }
public static class Study3MotionTracker { public const double MaximumAgeSeconds=.25; }
public static class Study3HandPoseSource {
 public static event Action<Study3JointSample> JointSampleUpdated,JointSampleProcessed; public static event Action<uint> SelectedSourceLost;
 public static void EnsureAvailable(){} public static void Emit(Study3JointSample s){JointSampleUpdated?.Invoke(s);JointSampleProcessed?.Invoke(s);} public static void Lose(uint s)=>SelectedSourceLost?.Invoke(s);
}
public class ExperimentCounter { public float HoldDuration=3, EndCaptureDelay=1; public int MaxCount=20; public event Action<string,int,double> TrialEvent; public void Emit(string kind,int trial,double t)=>TrialEvent?.Invoke(kind,trial,t); }
public class StartBallBehavior2 : UnityEngine.MonoBehaviour { }
public class Study1HeadAnchor { public bool HasPlacement, PlacementHeadPoseValid, PlacementWasTracked, PlacementCameraPoseValid; public double PlacementRealtime; public UnityEngine.Vector3 PlacementHeadPosition,PlacementHeadForward,PlacementTrackingPosition,PlacementTrackingOriginPosition,PlacementCameraPosition; public UnityEngine.Quaternion PlacementHeadRotation,PlacementTargetRotation,PlacementTrackingRotation,PlacementTrackingOriginRotation,PlacementCameraRotation; public int ReferenceSpaceEpoch; public event Action ReferenceSpaceResetStarted,ReferenceSpaceResetCompleted; public void EmitResetStarted()=>ReferenceSpaceResetStarted?.Invoke(); public void EmitResetCompleted()=>ReferenceSpaceResetCompleted?.Invoke(); }

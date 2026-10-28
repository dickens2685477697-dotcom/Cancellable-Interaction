$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testDirectory = Join-Path $projectRoot 'Temp\CommonMotionValidation'
[void](New-Item -ItemType Directory -Path $testDirectory -Force)
$stubs = @'
using System;
namespace UnityEditor { public class MenuItem : Attribute { public MenuItem(string name) {} } }
namespace UnityEngine {
 public static class Mathf {
  public static float Max(float a,float b) => Math.Max(a,b);
  public static float InverseLerp(float a,float b,float x) => Math.Max(0,Math.Min(1,(x-a)/(b-a)));
  public static float Lerp(float a,float b,float t) => a+(b-a)*t;
 }
 public static class Debug { public static void Log(object value) {} }
 public struct Vector3 {
  public float x,y,z;
  public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
  public static Vector3 zero => new Vector3(); public static Vector3 right => new Vector3(1,0,0);
  public float sqrMagnitude => x*x+y*y+z*z; public float magnitude => (float)Math.Sqrt(sqrMagnitude);
  public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator /(Vector3 a,float b) => new Vector3(a.x/b,a.y/b,a.z/b);
  public static Vector3 operator *(Vector3 a,float b) => new Vector3(a.x*b,a.y*b,a.z*b);
 }
 public class Object { public static void DestroyImmediate(object value) {} }
 public class GameObject {
  public bool activeInHierarchy=true; public GameObject(string name="") {}
  public void SetActive(bool value) { activeInHierarchy=value; }
  public T AddComponent<T>() where T:new() {
   var value=new T(); if(value is Collider c)c.gameObject=this;
   typeof(T).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(value,null);
   return value;
  }
 }
 public class MonoBehaviour : Object { protected static void DontDestroyOnLoad(object value) {} }
 public static class Time { public static double realtimeSinceStartupAsDouble; }
 public struct Bounds { public bool Contains(Vector3 p) => true; }
 public class Collider { public bool enabled=true; public GameObject gameObject=new GameObject(); public Bounds bounds; }
 public class Transform { public Vector3 lossyScale=new Vector3(1,1,1); public Vector3 TransformPoint(Vector3 p) => p; }
 public class SphereCollider : Collider { public Transform transform=new Transform(); public float radius=1; public Vector3 center; }
}
namespace Microsoft.MixedReality.Toolkit.Utilities {
 public enum Handedness { None=0,Left=1,Right=2,Any=3 }
 public enum TrackedHandJoint { IndexTip }
 public struct MixedRealityPose { public UnityEngine.Vector3 Position; }
}
namespace Microsoft.MixedReality.Toolkit.Input {
 using System.Collections.Generic; using Microsoft.MixedReality.Toolkit.Utilities;
 public interface IMixedRealityInputSource { uint SourceId {get;} }
 public interface IMixedRealityController { IMixedRealityInputSource InputSource {get;} bool IsPositionAvailable {get;} }
 public interface IMixedRealityHand : IMixedRealityController {}
 public interface IMixedRealityInputSystem {
  HashSet<IMixedRealityController> DetectedControllers {get;}
  void RegisterHandler<T>(T handler); void UnregisterHandler<T>(T handler);
 }
 public class InputEventData<T> { public IMixedRealityInputSource InputSource; public T InputData; public Handedness Handedness; }
 public class SourceStateEventData { public IMixedRealityInputSource InputSource; }
 public interface IMixedRealityHandJointHandler { void OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint,MixedRealityPose>> data); }
 public interface IMixedRealitySourceStateHandler { void OnSourceDetected(SourceStateEventData data); void OnSourceLost(SourceStateEventData data); }
}
namespace Microsoft.MixedReality.Toolkit {
 public static class CoreServices { public static Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem InputSystem; }
}
public static class Study3HandSamplingDiagnostics {
 public static void ProviderUpdate(Microsoft.MixedReality.Toolkit.Input.InputEventData<System.Collections.Generic.IDictionary<Microsoft.MixedReality.Toolkit.Utilities.TrackedHandJoint,Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose>> data,long sequence,double received=double.NaN) {}
 public static void SourceEvent(uint source,string kind) {}
}
'@
$stubPath = Join-Path $testDirectory 'Stubs.cs'
$stubs | Set-Content -LiteralPath $stubPath -Encoding utf8
$source = Join-Path $projectRoot 'Assets\Scripts\Study3'
Add-Type -Path @($stubPath, (Join-Path $source 'Study3MotionTracker.cs'),
    (Join-Path $source 'MagicTap2MotionTracker.cs'), (Join-Path $source 'IxDLDynamicMotionTracker.cs'),
    (Join-Path $source 'IxDLDynamicTriggerRecord.cs'), (Join-Path $source 'Study3HandPoseSource.cs'),
    (Join-Path $source 'Editor\CommonMotionValidation.cs'), (Join-Path $PSScriptRoot 'tests\CommonHandPoseSourceChecks.cs'))
$count = [CommonMotionValidation]::RunMotionChecks()
$providerCount = [CommonHandPoseSourceChecks]::Run()
Write-Output "Common motion validation passed: $count calculation assertions, $providerCount provider assertions (isolated replay; not a Quest run)."

using System;
using System.Collections;
using UnityEngine;
namespace TMPro { public class TextMeshProUGUI { public string text; public Color color; } }
namespace UnityEngine {
 public class MinAttribute:Attribute { public MinAttribute(float value){} }
 public struct Color { public static Color red,green; }
 public struct Scene { public string path; }
 public class Material {}
 public class MeshRenderer { public Material material; }
 public class Collider { public bool enabled=true; }
 public class WaitForSeconds { public WaitForSeconds(float s){} }
 public class WaitForSecondsRealtime { public WaitForSecondsRealtime(float s){} }
 public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b); }
 public static class Time { public static double realtimeSinceStartupAsDouble; }
 public class GameObject { public Scene scene; public Collider collider=new Collider(); public MeshRenderer renderer=new MeshRenderer(); public AcceStimulate acce=new AcceStimulate(); public StartBallBehavior2 start=new StartBallBehavior2(); public bool active=true; public void SetActive(bool b){active=b;} public static GameObject Find(string n)=>new GameObject(); public T GetComponent<T>() where T:class { if(typeof(T)==typeof(Collider))return collider as T; if(typeof(T)==typeof(MeshRenderer))return renderer as T; if(typeof(T)==typeof(AcceStimulate))return acce as T; return null; } }
 public class MonoBehaviour { public GameObject gameObject=new GameObject(); public IEnumerator pending; public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>(); public T GetComponentInChildren<T>(bool includeInactive=false) where T:class=>gameObject.start as T; public void StartCoroutine(IEnumerator e){pending=e;pending.MoveNext();} public void StopAllCoroutines(){pending=null;} public void FinishCoroutine(){pending.MoveNext();} }
}
public class StubStartObject { public bool active=true; public void SetActive(bool value){active=value;} }
public class StartBallBehavior2 { public bool state; public StubStartObject gameObject=new StubStartObject(); public bool GetState()=>state; public void TureUnstarted(){state=false;gameObject.SetActive(true);} }
public class StubEvent { event Action action; public void AddListener(Action a){action+=a;} public void Invoke()=>action?.Invoke(); }
public class AcceStimulate { public bool enabled=true,UsesMrtkJointUpdates=true; public int referenceResetCount; public double MotionSampleTimestamp; public StubEvent InEvent=new StubEvent(),OutEvent=new StubEvent(),AfterOutEvent=new StubEvent(); public event Action MotionTrackingInterrupted; public void Interrupt()=>MotionTrackingInterrupted?.Invoke(); public void ResetForReferenceSpaceChange(){referenceResetCount++;} }

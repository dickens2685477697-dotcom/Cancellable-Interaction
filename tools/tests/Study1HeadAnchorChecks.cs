using System;
using System.Collections;
using System.Reflection;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;
using UnityEngine.XR;

public static class Study1HeadAnchorChecks {
    private static int checks;
    private static void Require(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    private static void Near(float a, float b, string message) => Require(Math.Abs(a-b)<.00001f, message+": "+a+" vs "+b);
    private static void Same(Vector3 a, Vector3 b, string message) { Near(a.x,b.x,message+" x"); Near(a.y,b.y,message+" y"); Near(a.z,b.z,message+" z"); }
    private static object Call(object instance,string name) => instance.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(instance,null);
    private static void SetTracking(bool available) { InputDevices.valid=available; InputDevices.tracked=available; InputDevices.boolFeature=available; InputDevices.positionFeature=available; InputDevices.rotationFeature=available; }
    public static int Run() {
        checks=0;
#if UNITY_EDITOR
        XRSettings.enabled=true;
#endif
        Camera.main=new Camera();
        Camera.main.transform.position=new Vector3(0,0,0);
        float half=(float)Math.Sqrt(.5);
        MixedRealityPlayspace.Transform.position=new Vector3(4,.2f,-1);
        MixedRealityPlayspace.Transform.rotation=new Quaternion{y=half,w=half};
        InputDevices.position=new Vector3(1,1.4f,2);
        InputDevices.rotation=new Quaternion{w=1};
        SetTracking(true);
        var fixedPosition=new Vector3(0,1.1f,.53f);
        var active=new GameObject(true); var inactive=new GameObject(false);
        var anchor=new Study1HeadAnchor(); anchor.transform.position=fixedPosition;
        Near(anchor.ballCenterDistanceMeters,.36f,"default ball centre distance");
        anchor.targetBall=new SphereCollider(); anchor.targetBall.transform.position=fixedPosition;
        anchor.startBall=new SphereCollider(); anchor.startBall.transform.position=new Vector3(0,1.1f,.35f);
        anchor.ballCenterDistanceMeters=.31f;
        anchor.transform.children.Add(active.transform); anchor.transform.children.Add(inactive.transform);
        Time.realtimeSinceStartupAsDouble=0;
        Call(anchor,"Awake");
        Same(anchor.targetBall.transform.position,fixedPosition,"spacing does not move target");
        Same(anchor.startBall.transform.position,new Vector3(0,1.1f,.22f),"inspector distance moves start ball toward user");
        Near(Vector3.Distance(anchor.targetBall.transform.position,anchor.startBall.transform.position),.31f,"configured world centre distance");
        Require(!active.activeSelf&&!inactive.activeSelf,"children hidden during initialization");
        var startup=(IEnumerator)Call(anchor,"Start");
        Require(startup.MoveNext()&&startup.MoveNext()&&!anchor.HasPlacement,"wait for startup frames");
        Require(!startup.MoveNext()&&anchor.HasPlacement,"initialize fixed layout");
        Require(active.activeSelf&&!inactive.activeSelf,"restore child active states");
        Same(anchor.transform.position,fixedPosition,"startup keeps inspector world position");
        Same(anchor.PlacementHeadPosition,new Vector3(6,1.6f,-2),"head diagnostic uses MRTK tracking origin");
        Same(anchor.PlacementHeadForward,new Vector3(1,0,0),"head diagnostic includes origin rotation");
        Same(anchor.PlacementCameraPosition,new Vector3(0,0,0),"camera pose is separately recorded");
        Require(anchor.PlacementHeadPoseValid&&anchor.PlacementWasTracked&&anchor.PlacementCameraPoseValid,"valid tracked and camera diagnostics");
        Camera.main.transform.position=new Vector3(9,8,7); InputDevices.position=new Vector3(3,2,1);
        Same(anchor.transform.position,fixedPosition,"target does not follow head movement");
        var subsystem=new XRInputSubsystem(); SubsystemManager.current.Add(subsystem); Call(anchor,"OnEnable");
        var experiment=new ExperimentCounter(); anchor.experiments.Add(experiment); int started=0,completed=0;
        anchor.ReferenceSpaceResetStarted+=()=>started++; anchor.ReferenceSpaceResetCompleted+=()=>completed++;
        Time.realtimeSinceStartupAsDouble=10; SetTracking(false); subsystem.Recenter();
        Require(started==1&&experiment.resetCount==1&&anchor.ReferenceSpaceEpoch==1&&!anchor.HasPlacement&&!active.activeSelf,"reset aborts trial and temporarily hides scene");
        subsystem.Recenter(); Require(started==1&&experiment.resetCount==1,"duplicate reset suppressed");
        Require(anchor.pending.MoveNext()&&!anchor.HasPlacement,"reset waits a frame");
        Require(!anchor.pending.MoveNext()&&anchor.HasPlacement&&active.activeSelf&&completed==1,"reset resumes even without a tracked head pose");
        Require(!anchor.PlacementHeadPoseValid&&!anchor.PlacementWasTracked,"unavailable tracking is marked unavailable");
        Same(anchor.transform.position,fixedPosition,"system reset does not move the fixed scene");
        Near(Vector3.Distance(anchor.targetBall.transform.position,anchor.startBall.transform.position),.31f,"system reset preserves ball spacing");
#if UNITY_ANDROID && !UNITY_EDITOR
        OVRPlugin.initialized=true; OVRPlugin.recenterCount=4; Call(anchor,"Update"); Require(started==1,"first native count is a baseline");
        Time.realtimeSinceStartupAsDouble=11; OVRPlugin.recenterCount=5; Call(anchor,"Update");
        Require(started==2&&experiment.resetCount==2&&anchor.ReferenceSpaceEpoch==2,"native reset records new epoch");
        subsystem.Recenter(); Require(started==2,"cross-API duplicate suppressed");
        Require(anchor.pending.MoveNext(),"native reset waits a frame"); Require(!anchor.pending.MoveNext()&&completed==2,"native reset completes");
        Same(anchor.transform.position,fixedPosition,"native reset keeps target fixed"); OVRPlugin.initialized=false;
#endif
        int beforeDisable=started; Call(anchor,"OnDisable"); Time.realtimeSinceStartupAsDouble=12; subsystem.Recenter();
        Require(started==beforeDisable,"disabled component unsubscribes"); SubsystemManager.current.Clear();
        var noCamera=new Study1HeadAnchor(); noCamera.transform.position=fixedPosition; var target=new GameObject(true); noCamera.transform.children.Add(target.transform);
        Camera.main=null; Call(noCamera,"Awake"); var noCameraStart=(IEnumerator)Call(noCamera,"Start");
        Require(noCameraStart.MoveNext()&&noCameraStart.MoveNext()&&!noCameraStart.MoveNext(),"missing camera does not prevent fixed layout");
        Require(noCamera.HasPlacement&&target.activeSelf&&!noCamera.PlacementHeadPoseValid&&!noCamera.PlacementCameraPoseValid,"missing camera diagnostics are unavailable");
        Same(noCamera.transform.position,fixedPosition,"missing camera cannot move scene");
#if UNITY_EDITOR
        Camera.main=new Camera(); Camera.main.transform.position=new Vector3(0,1.7f,0); XRSettings.enabled=false;
        var preview=new Study1HeadAnchor(); preview.transform.position=fixedPosition; Call(preview,"Awake"); var previewStart=(IEnumerator)Call(preview,"Start");
        Require(previewStart.MoveNext()&&previewStart.MoveNext()&&!previewStart.MoveNext(),"editor preview initializes");
        Require(preview.PlacementHeadPoseValid&&!preview.PlacementWasTracked,"editor preview is identified");
        Same(preview.transform.position,fixedPosition,"preview keeps fixed scene"); XRSettings.enabled=true;
#endif
        return checks;
    }
}

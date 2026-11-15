using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
public static class Study1SamplingChecks {
 static int checks;
 static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static void Call(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static Study3JointSample Sample(long seq,double t,float x,long epoch=1) { Time.realtimeSinceStartupAsDouble=t; return new Study3JointSample(1,epoch,seq,t,new Dictionary<TrackedHandJoint,MixedRealityPose>{{TrackedHandJoint.IndexTip,new MixedRealityPose{Position=new Vector3(x,0,0)}}}); }
 public static int Run(){
  var a=new AcceStimulate(); a.gameObject.scene=new Scene{path="Assets/Scenes/Study1.unity"}; Call(a,"Awake"); Call(a,"OnEnable"); Call(a,"Start");
  Check(a.UsesMrtkJointUpdates,"Study1 must select MRTK events"); int inside=0,interrupted=0,exits=0; a.InEvent.AddListener(()=>inside++); a.OutEvent.AddListener(()=>exits++); a.AfterOutEvent.AddListener(()=>exits++); a.MotionTrackingInterrupted+=()=>interrupted++;
  Study3HandPoseSource.Emit(Sample(1,1,.1f)); Check(inside==1&&!a.MotionVelocityValid,"first sample is baseline");
  Study3HandPoseSource.Emit(Sample(2,1.1,.2f)); Check(inside==2&&a.MotionVelocityValid&&Math.Abs(a.v-1)<.0001,"speed uses received interval");
  Study3HandPoseSource.Emit(Sample(2,1.1,.2f)); Check(inside==2,"duplicate sequence must be ignored");
  Study3HandPoseSource.Emit(Sample(3,1.2,.2f)); Check(inside==3&&a.MotionAccelerationValid&&a.v==0,"new stationary update is accepted");
  HandJointUtils.Calls=0; Call(a,"FixedUpdate"); Check(HandJointUtils.Calls==0&&inside==3,"fixed update does not read or re-emit sample");
  Study3HandPoseSource.Lose(1); Check(interrupted==1&&!a.MotionVelocityValid&&exits==0,"tracking loss interrupts without claiming a successful exit");
  Study3HandPoseSource.Emit(Sample(4,1.21,.8f,2)); Check(!a.MotionVelocityValid&&inside==4,"recovery establishes new baseline");
  Call(a,"OnDisable"); Study3HandPoseSource.Emit(Sample(5,1.22,.9f,2)); Check(inside==4,"disabled component unsubscribes");
  Call(a,"OnEnable"); Study3HandPoseSource.Emit(Sample(6,1.23,.9f,2)); Check(inside==5&&!a.MotionVelocityValid,"reenable establishes new baseline");
  Study3HandPoseSource.Emit(Sample(7,1.28,.95f,2)); Check(Math.Abs(a.v-1)<.0001,"variable interval is honored after reenabling");
  int oldExits=exits, oldInterruptions=interrupted; a.ResetForReferenceSpaceChange(); Check(!a.MotionVelocityValid&&!a.MotionAccelerationValid&&exits==oldExits&&interrupted==oldInterruptions,"recenter clears motion without normal exit or tracking-loss event");
  Study3HandPoseSource.Emit(Sample(8,1.30,.1f,2));Check(!a.MotionVelocityValid,"first sample after coordinate reset is a new baseline");
  Study3HandPoseSource.Emit(Sample(9,1.35,.15f,2));Check(a.MotionVelocityValid&&Math.Abs(a.v-1)<.0001,"post-reset velocity excludes pre-reset movement");Call(a,"OnDisable");
  var legacy=new AcceStimulate(); legacy.gameObject.scene=new Scene{path="Assets/Scenes/show.unity"}; Call(legacy,"Awake"); Call(legacy,"OnEnable"); Call(legacy,"Start"); HandJointUtils.Calls=0; Call(legacy,"FixedUpdate"); Check(!legacy.UsesMrtkJointUpdates&&HandJointUtils.Calls>0,"other scenes keep legacy path");
  return checks;
 }
}

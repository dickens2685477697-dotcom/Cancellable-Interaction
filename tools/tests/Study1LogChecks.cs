using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
public static class Study1LogChecks {
 static int checks; static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static void Call(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static Study3JointSample Sample(long seq,double t,float x,long epoch=1) { Time.realtimeSinceStartupAsDouble=t; return new Study3JointSample(2,epoch,seq,t,new Dictionary<TrackedHandJoint,MixedRealityPose>{{TrackedHandJoint.IndexTip,new MixedRealityPose{Position=new Vector3(x,0,0)}}}); }
 public static int Run() {
  var original=CultureInfo.CurrentCulture; CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");
  try {
   var a=new AcceStimulate(); a.gameObject.scene=new Scene{path="Assets/Scenes/Study1.unity"}; a.gameObject.acce=a; Call(a,"Awake");Call(a,"OnEnable");Call(a,"Start");
   var log=new LogWriter(); log.gameObject=a.gameObject; Call(log,"Start");
   Study3HandPoseSource.Emit(Sample(10,2,.1f)); Study3HandPoseSource.Emit(Sample(11,2.1,.2f)); Study3HandPoseSource.Lose(2); Study3HandPoseSource.Emit(Sample(12,2.2,.3f,2));
   Call(log,"OnDisable"); var lines=File.ReadAllLines(log.GetFileFullPath()); var header=lines[0].Split(','); var first=lines[1].Split(','); var second=lines[2].Split(','); var resumed=lines[3].Split(',');
   Check(header.Length==first.Length&&header.Length==second.Length&&header.Length==resumed.Length,"header and row columns align");
   var ix=Array.IndexOf(header,"IndexTip_X"); Check(ix>=0&&first[ix]=="0.10000000149011612" || ix>=0&&double.Parse(first[ix],CultureInfo.InvariantCulture)>.099,"invariant decimal output");
   var distal=Array.IndexOf(header,"IndexDistalJoint_Valid"); Check(distal>=0&&first[distal]=="0"&&first[distal+1]==""&&first[distal+2]==""&&first[distal+3]=="","missing joints preserve four columns");
   var segment=Array.IndexOf(header,"Segment"); Check(first[segment]==second[segment]&&resumed[segment]!=second[segment],"tracking loss starts new segment");
   var velocity=Array.IndexOf(header,"Velocity"); Check(first[velocity]==""&&second[velocity]!=""&&resumed[velocity]=="","velocity unavailable at baseline and after recovery");
   Call(log,"OnDestroy");Call(a,"OnDisable");
   var b=new AcceStimulate(); b.gameObject.name="ContinuousValidation"+Guid.NewGuid().ToString("N"); b.gameObject.scene=new Scene{path="Assets/Scenes/Study1.unity"}; b.gameObject.acce=b; var exp=new ExperimentCounter(); b.gameObject.experiment=exp;
   b.gameObject.anchor=new Study1HeadAnchor{HasPlacement=true,PlacementHeadPoseValid=true,PlacementWasTracked=true,PlacementCameraPoseValid=true,PlacementRealtime=1.25,PlacementHeadPosition=new Vector3(1,2,3),PlacementHeadForward=new Vector3(0,0,1),PlacementHeadRotation=new Quaternion{w=1},PlacementTargetRotation=new Quaternion{w=1},PlacementTrackingPosition=new Vector3(0,1.6f,0),PlacementTrackingRotation=new Quaternion{w=1},PlacementTrackingOriginRotation=new Quaternion{w=1},PlacementCameraPosition=new Vector3(0,0,0),PlacementCameraRotation=new Quaternion{w=1}};
   b.gameObject.sphere=new SphereCollider{transform=new Transform{position=new Vector3(1,2,3.53f)}};
   var start=new StartBallBehavior2();start.gameObject.sphere=new SphereCollider{transform=new Transform{position=new Vector3(1,2,3.35f)}};b.gameObject.startBall=start;
   Call(b,"Awake");Call(b,"Start");
   var continuousLog=new LogWriter(); continuousLog.gameObject=b.gameObject; Call(continuousLog,"Start");
   string[] layouts=Directory.GetFiles(continuousLog.GetFilePath(),"*"+b.gameObject.name+"-session-layout*.csv");
   Check(layouts.Length==1,"one layout file is written");
   var layout=File.ReadAllLines(layouts[0]); var lh=layout[0].Split(','); var lv=layout[1].Split(',');
   Check(lh.Length==lv.Length && lv[Array.IndexOf(lh,"PlacementAvailable")]=="1" && lv[Array.IndexOf(lh,"TrackingMode")]=="xr_tracked","layout fields and tracking mode");
   Check(Math.Abs(double.Parse(lv[Array.IndexOf(lh,"PlacementRealtime")],CultureInfo.InvariantCulture)-1.25)<.00001 && Math.Abs(double.Parse(lv[Array.IndexOf(lh,"HeadToTargetMeters")],CultureInfo.InvariantCulture)-.53)<.00001 && Math.Abs(double.Parse(lv[Array.IndexOf(lh,"StartToTargetMeters")],CultureInfo.InvariantCulture)-.18)<.00001,"layout captures initial time and actual geometry using invariant decimal");
   Check(lv[Array.IndexOf(lh,"SchemaVersion")]=="2" && lv[Array.IndexOf(lh,"LayoutMode")]=="world_fixed" && lv[Array.IndexOf(lh,"HeadPoseSource")]=="xr_head_device" && lv[Array.IndexOf(lh,"CameraWorldY")]=="0" && lv[Array.IndexOf(lh,"RequestedHeadToTargetMeters")]=="","fixed layout does not misrepresent requested head distance and preserves diagnostics");
   b.gameObject.anchor.PlacementWasTracked=false; Call(continuousLog,"WriteSessionLayout");
   var secondLayout=Directory.GetFiles(continuousLog.GetFilePath(),"*"+b.gameObject.name+"-session-layout-2.csv");
   Check(secondLayout.Length==1 && File.ReadAllLines(secondLayout[0])[1].Split(',')[Array.IndexOf(lh,"TrackingMode")]=="editor_preview" && File.ReadAllLines(layouts[0])[1]==layout[1],"preview is marked and a second layout never overwrites the first");
   b.gameObject.anchor.HasPlacement=false; Call(continuousLog,"WriteSessionLayout");
   var missingLayout=Directory.GetFiles(continuousLog.GetFilePath(),"*"+b.gameObject.name+"-session-layout-3.csv"); var missingValues=File.ReadAllLines(missingLayout[0])[1].Split(',');
   Check(missingValues[Array.IndexOf(lh,"PlacementAvailable")]=="0" && missingValues[Array.IndexOf(lh,"HeadX")]=="" && missingValues[Array.IndexOf(lh,"HeadToTargetMeters")]=="","unavailable pose remains blank rather than fabricated");
   b.AfterOutEvent.AddListener(()=>exp.Emit("success",1,Time.realtimeSinceStartupAsDouble));
   Study3HandPoseSource.Emit(Sample(20,3,-.1f)); Study3HandPoseSource.Emit(Sample(21,3.1,.1f)); exp.Emit("enter",1,3.1); exp.Emit("hold_complete",1,3.15); Study3HandPoseSource.Emit(Sample(22,3.2,1.1f)); Study3HandPoseSource.Emit(Sample(23,3.3,1.2f));
   string cp=(string)typeof(LogWriter).GetField("continuousPath",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(continuousLog), ep=(string)typeof(LogWriter).GetField("eventsPath",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(continuousLog);
   Call(continuousLog,"OnDisable"); var frames=File.ReadAllLines(cp); var events=File.ReadAllLines(ep); var ch=frames[0].Split(','); var seq=Array.IndexOf(ch,"SampleSequence"); var incol=Array.IndexOf(ch,"Inside");
   Check(frames.Length==5 && frames[1].Split(',')[incol]=="0" && frames[2].Split(',')[incol]=="1" && frames[3].Split(',')[incol]=="0" && frames[4].Split(',')[incol]=="0","continuous frames include approach, exit and withdrawal exactly once");
   Check(frames[3].Split(',')[seq]=="22" && events.Length==4 && events[3].StartsWith("success,1,") && events[3].Split(',')[3]=="1" && frames[3].Split(',')[0]=="1","success boundary and event retain original case regardless of listener order");
   Check(events[3].Split(',')[8]=="3"&&events[3].Split(',')[9]=="20"&&events[3].Split(',')[10]=="1"&&frames[4].Split(',')[Array.IndexOf(ch,"LogicalTrial")]=="1","events record parameters and final tail keeps completed trial");
   Check(frames[1].Split(',')[Array.IndexOf(ch,"ReferenceSpaceEpoch")]=="0" && events[3].Split(',')[11]=="0","initial motion and event rows record reference-space epoch");
   Call(continuousLog,"OnEnable"); b.gameObject.anchor.HasPlacement=true;
   b.gameObject.anchor.EmitResetStarted(); exp.Emit("recenter_interrupted",1,Time.realtimeSinceStartupAsDouble);
   b.ResetForReferenceSpaceChange(); b.gameObject.anchor.ReferenceSpaceEpoch=1; b.gameObject.anchor.PlacementRealtime=4;
   b.gameObject.anchor.EmitResetCompleted(); Study3HandPoseSource.Emit(Sample(24,4.1,.1f)); Call(continuousLog,"OnDisable");
   frames=File.ReadAllLines(cp); events=File.ReadAllLines(ep);
   Check(events.Length==7 && events[4].StartsWith("reference_space_reset,") && events[5].StartsWith("recenter_interrupted,") && events[6].StartsWith("reference_space_placed,"),"reset and interrupted trial are recorded");
   Check(events[4].Split(',')[11]=="0" && events[6].Split(',')[11]=="1" && frames[frames.Length-1].Split(',')[Array.IndexOf(ch,"ReferenceSpaceEpoch")]=="1","new samples and placement use new reference-space epoch");
   var resetLayout=Directory.GetFiles(continuousLog.GetFilePath(),"*"+b.gameObject.name+"-session-layout-4.csv");
   Check(resetLayout.Length==1 && File.ReadAllLines(resetLayout[0])[1].Split(',')[Array.IndexOf(lh,"ReferenceSpaceEpoch")]=="1","repositioning writes a new layout snapshot");
   b.gameObject.anchor.PlacementHeadPoseValid=false; Call(continuousLog,"WriteSessionLayout");
   var noHead=File.ReadAllLines(Directory.GetFiles(continuousLog.GetFilePath(),"*"+b.gameObject.name+"-session-layout-5.csv")[0])[1].Split(',');
   Check(noHead[Array.IndexOf(lh,"LayoutMode")]=="world_fixed" && noHead[Array.IndexOf(lh,"TargetCenterValid")]=="1" && noHead[Array.IndexOf(lh,"TrackingMode")]=="unavailable" && noHead[Array.IndexOf(lh,"HeadX")]=="" && noHead[Array.IndexOf(lh,"HeadToTargetMeters")]=="","fixed target remains available without fabricating a missing head pose");
   for(int i=0;i<20;i++) continuousLog.increCase(); Check(b.gameObject.active,"Study1 logger does not stop at fifteen exits");
   Call(continuousLog,"OnDestroy"); Call(b,"OnDisable");
  } finally {CultureInfo.CurrentCulture=original;}
  return checks;
 }
}

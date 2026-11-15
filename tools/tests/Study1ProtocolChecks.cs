using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
public static class Study1ProtocolChecks {
 static int checks;
 static void Assert(bool b,string label){checks++;if(!b)throw new Exception(label);}
 static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static void Inside(ExperimentCounter c,double t){c.acce.MotionSampleTimestamp=t;c.acce.InEvent.Invoke();}
 static void Exit(ExperimentCounter c,double t){c.acce.MotionSampleTimestamp=t;c.acce.AfterOutEvent.Invoke();}
 static ExperimentCounter Create(int max,float seconds){var c=new ExperimentCounter();c.gameObject.scene=new Scene{path="Assets/Scenes/Study1.unity"};c.MaxCount=max;c.HoldDuration=seconds;c.DisplayNum=new TextMeshProUGUI();c.TaskPrompt=new TextMeshProUGUI();c.GesturePrompt=new TextMeshProUGUI();Call(c,"Start");return c;}
 public static int Run(){
  var defaults=new ExperimentCounter(); Assert(defaults.MaxCount==20 && defaults.HoldDuration==3f,"Inspector defaults must be 20 and 3 seconds");
  var c=Create(2,3);var events=new List<string>();c.TrialEvent+=(kind,id,t)=>events.Add(kind+":"+id);
  Assert(c.DisplayNum.text=="2","configured remaining count"); Inside(c,1);Exit(c,1.1);Assert(events.Count==0&&c.count==0,"entry without start must not count");
  c.gameObject.start.state=true;Inside(c,10);Assert(c.TaskPrompt.text=="Hold: 3.00 s","entry immediately displays configured duration");Inside(c,11.5);Assert(c.TaskPrompt.text=="Hold: 1.50 s","remaining time decreases according to sample timestamps");Inside(c,12.996);Assert(c.TaskPrompt.text=="Hold: 0.01 s"&&c.count==0,"remaining fractional hundredth must not display zero early");Exit(c,12.996);Assert(c.count==0&&events[events.Count-1]=="early_exit:1"&&c.DisplayNum.text=="2"&&c.TaskPrompt.text.Contains("Too early"),"early exit must not count and clears countdown");
  c.gameObject.start.state=true;Inside(c,20);Assert(c.TaskPrompt.text=="Hold: 3.00 s","retry starts countdown afresh");Inside(c,23);Assert(c.TaskPrompt.text=="0.00s Withdraw"&&c.count==0&&events[events.Count-1]=="hold_complete:1","duration displays zero and prompts withdrawal but does not count");Inside(c,24);Assert(c.TaskPrompt.text=="0.00s Withdraw","completed countdown remains at zero");Exit(c,24.1);Assert(c.count==1&&c.DisplayNum.text=="1"&&events[events.Count-1]=="success:1"&&!c.TaskPrompt.text.Contains("0.00"),"completed exit counts once and clears countdown");Exit(c,24.2);Assert(c.count==1,"duplicate exit must not count");
  c.gameObject.start.state=true;Inside(c,30);Time.realtimeSinceStartupAsDouble=31;c.acce.Interrupt();Assert(c.count==1&&!c.gameObject.start.state&&events[events.Count-1]=="tracking_interrupted:2"&&c.TaskPrompt.text.Contains("Tracking lost")&&!c.TaskPrompt.text.Contains("Hold:"),"tracking interruption resets countdown without count");Exit(c,32);Assert(c.count==1,"exit after interruption must not count");
  c.gameObject.start.state=true;Inside(c,35); Time.realtimeSinceStartupAsDouble=36; c.OnReferenceSpaceReset(); Assert(c.count==1&&!c.gameObject.start.state&&events[events.Count-1]=="recenter_interrupted:2"&&c.acce.referenceResetCount==1&&c.TaskPrompt.text=="Touch green","recenter interrupts unfinished trial, resets input and preserves previous completed count");
  int before=events.Count;c.OnReferenceSpaceReset();Exit(c,36.5);Assert(c.count==1&&events.Count==before,"repeated recenter and exit do not count or emit a second interruption");
  c.gameObject.start.state=true;Inside(c,40);Inside(c,43);Exit(c,43.1);Assert(c.count==2&&c.acce.enabled&&c.TaskPrompt.text=="Thanks","final success preserves tail capture");Assert(!c.gameObject.start.gameObject.active,"final success hides start ball during capture");c.FinishCoroutine();Assert(!c.acce.enabled&&!c.SphereCollider.enabled,"capture delay ends experiment");
  var custom=Create(1,1.25f);custom.gameObject.start.state=true;Inside(custom,50);Assert(custom.TaskPrompt.text=="Hold: 1.25 s","custom duration immediately displayed");Inside(custom,51.2);Exit(custom,51.2);Assert(custom.count==0,"custom hold duration gates early exit");custom.gameObject.start.state=true;Inside(custom,52);Inside(custom,53.25);Assert(custom.TaskPrompt.text=="0.00s Withdraw","custom duration reaches zero");Exit(custom,53.3);Assert(custom.count==1&&custom.TaskPrompt.text=="Thanks","custom hold duration and count work");custom.OnReferenceSpaceReset();Assert(custom.count==1&&!custom.acce.enabled&&!custom.SphereCollider.enabled&&!custom.gameObject.start.gameObject.active&&custom.pending==null,"reset after final success stops tail capture without reopening trial");
  return checks;
 }
}

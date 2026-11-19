using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine { public static class Time { public static float time; } }
public sealed class DirectionTestEvent {
    private readonly List<Action> handlers = new List<Action>();
    public void AddListener(Action action) { handlers.Add(action); }
    public void Invoke() { foreach (var action in handlers) action(); }
}
public sealed class DirectionTestAcce { public DirectionTestEvent InEvent=new DirectionTestEvent(), AfterOutEvent=new DirectionTestEvent(); }
public sealed class DirectionTestSpawner {
    public bool enable_trigger=true,triggered; public int intargetTimes,errorInTimes,area_type,index;
}
public sealed class DirectionTestPlayer {
    public void TurnShallowYellowMineBall() {} public void TurnShallowGreenMineBall() {}
    public void TurnShallow() {} public void TurnShallowBack() {} public void ChangeOpacityBack() {}
}
public sealed class DirectionTestLogger { public bool handInBall; }
public abstract class DirectionWiringHost {
    public DirectionTestAcce Acce=new DirectionTestAcce();
    public DirectionTestSpawner ballSpawner=new DirectionTestSpawner();
    public DirectionTestPlayer Player=new DirectionTestPlayer();
    public DirectionTestLogger logWriter3=new DirectionTestLogger();
    public bool handInBall,handInBall2,inBall,canceled,speedReleaseMinSpeedMet,direction010CancelTriggered;
    public bool speedReleaseReentryRequired,direction010ReentryRequired,speedReleaseTriggered,isMineBall,triggered,selected,canceledByDirection;
    public bool UsesPendingMineBallCue;
    public int ballIndex=1; public float fastTapEntryTimestamp,oldTime,currentSpeed;
    private long sequence; private Vector3 position;
    private readonly Study3DirectionConsecutiveTracker tracker=new Study3DirectionConsecutiveTracker();
    public abstract void Configure();
    public void Sample(float x,float z,float speed) {
        sequence++; Time.time=(float)(sequence*.02); position=new Vector3(x,0,z); currentSpeed=speed; Acce.InEvent.Invoke();
    }
    public void Exit() { Acce.AfterOutEvent.Invoke(); }
    public void ResetSpeedReleaseState() { speedReleaseMinSpeedMet=false; speedReleaseTriggered=false; speedReleaseReentryRequired=false; }
    public void ResetDirection010State() { tracker.Reset(); handInBall=handInBall2=inBall=false; direction010ReentryRequired=false; direction010CancelTriggered=false; }
    public void ResetDirection010ObstacleState() { ResetDirection010State(); }
    public void ResetDirection010TrackingWindow() { tracker.Reset(); }
    public void ResetDirection010ObstacleTrackingWindow() { tracker.Reset(); }
    public void CaptureDirectionCancelEntryAxisIfNeeded() {}
    public void LogSpeedStudy(string text) {}
    public void HideModeReadyRing() {}
    public void ResetFastTapTiming() {}
    public bool IsDirection010CancelMovement(out bool ready) {
        var result=tracker.Evaluate(1,1,sequence,sequence*.02,position,new Vector3(0,0,-1),false,45,.05f,.25);
        ready=result==Study3DirectionConsecutiveTracker.Observation.AngleChecked && tracker.AllowSpeedConfirmation;
        return result==Study3DirectionConsecutiveTracker.Observation.AngleChecked && tracker.CancelPass;
    }
    public void TriggerDirection010Cancel() { canceledByDirection=true; direction010CancelTriggered=true; direction010ReentryRequired=true; }
    public void TriggerDirection010ObstacleCancel() { TriggerDirection010Cancel(); }
    public void CheckSpeedReleaseIndexTrigger(bool directionReady) {
        if (!speedReleaseMinSpeedMet) { if (currentSpeed<=.05f) speedReleaseMinSpeedMet=true; return; }
        if (directionReady && currentSpeed>=.25f) { selected=true; speedReleaseTriggered=true; }
    }
}
public sealed class DirectionTargetWiringHost : DirectionWiringHost {
/* TARGET_METHOD */
    public override void Configure() { SetSpeedDirectionMode(); }
}
public sealed class DirectionObstacleWiringHost : DirectionWiringHost {
/* OBSTACLE_METHOD */
    public override void Configure() { SetSpeedDirectionMode(); }
}
public static class DirectionWiringChecks {
    static int count;
    static void Require(bool value,string why) { count++; if(!value) throw new Exception(why); }
    static void Test(Func<DirectionWiringHost> factory) {
        var h=factory(); h.Configure(); h.Sample(0,0,.1f);
        for(int i=1;i<=4;i++) h.Sample(0,-i*.0002f,.1f);
        Require(!h.canceledByDirection,"Below-threshold movement must not cancel");
        h.Exit(); h=factory(); h.Configure(); h.Sample(0,0,.1f);
        for(int i=1;i<=3;i++) { h.Sample(0,-i*.002f,.1f); Require(!h.canceledByDirection,"Canceled before four angles"); }
        h.Sample(0,-.008f,.1f);
        Require(h.canceledByDirection && !h.speedReleaseMinSpeedMet && !h.selected,"Direction cancel must work before Speed low-speed preparation");
        h.Exit(); h.canceledByDirection=false; h.Sample(0,0,.1f);
        for(int i=1;i<=4;i++) h.Sample(0,-i*.002f,.1f);
        Require(h.canceledByDirection,"Exit/re-entry did not reset direction tracking");
        h=factory(); h.Configure(); h.Sample(0,0,.01f); Require(h.speedReleaseMinSpeedMet,"Low-speed selection preparation failed");
        for(int i=1;i<=3;i++) { h.Sample(i*.002f,0,.3f); Require(!h.selected,"High speed confirmed before four direction observations"); }
        h.Sample(.008f,0,.3f); Require(h.selected && !h.canceledByDirection,"Outside cone plus Speed readiness did not select");
        h=factory(); h.Configure(); h.Sample(0,0,.01f);
        for(int i=1;i<=3;i++) { h.Sample(0,-i*.002f,.3f); Require(!h.selected && !h.canceledByDirection,"Inside cone must wait through three matching angles"); }
        h.Sample(0,-.008f,.3f); Require(h.canceledByDirection && !h.selected,"Fourth cancel angle must win over high-speed selection");
    }
    static void TestPendingCancellation()
    {
        var host = new DirectionTargetWiringHost { UsesPendingMineBallCue = true };
        host.Configure();
        host.Sample(0, 0, .1f);
        for (int i = 1; i <= 4; i++) host.Sample(0, -i * .002f, .1f);
        Require(!host.canceledByDirection, "A target must reach Pending before an experiment cancellation");

        host.Exit();
        host.Sample(0, 0, .01f);
        Require(host.speedReleaseMinSpeedMet, "Low-speed preparation must still arm the target");
        for (int i = 1; i <= 4; i++) host.Sample(0, -i * .002f, .3f);
        Require(host.canceledByDirection && !host.selected, "Direction must still cancel after Pending");
    }

    public static int Run()
    {
        count = 0;
        Test(() => new DirectionTargetWiringHost());
        Test(() => new DirectionObstacleWiringHost());
        TestPendingCancellation();
        return count;
    }
}

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testDirectory = Join-Path $projectRoot 'Temp\SpeedTelemetryTests'
[void](New-Item -ItemType Directory -Path $testDirectory -Force)
$stubs = @'
using System;
namespace UnityEngine {
    public class Component { public int GetInstanceID() => 123; }
    public struct Vector3 { public float x,y,z; public static Vector3 zero => new Vector3(); }
    public struct Bounds { public Vector3 center,extents; }
    public static class Mathf { public static float Max(float a,float b) => Math.Max(a,b); }
    public static class Time {
        public static int frameCount;
        public static double timeAsDouble,fixedTimeAsDouble,realtimeSinceStartupAsDouble;
        public static bool inFixedTimeStep;
        public static float fixedDeltaTime=0.02f,timeScale=1;
    }
}
public class BallSpawner {
    public enum Mode { speed=21,speedDwell=22,speedRelease=15,speedDirection=26,speedWristTilt=20 }
    public float samplingRateHz=50,speedMinThreshold=.067f,speedMaxThreshold=.375f,pinchDwellCancelTime=.7f;
    public int currentCaseNumber=1,currentRoundID=3;
    public bool enable_trigger=true;
}
public class AcceStudy3 {
    public struct Pose { public UnityEngine.Vector3 Position; }
    public float v=.04f,MotionSampleDeltaTime=.02f;
    public long MotionSampleSequence=5;
    public double MotionSampleFixedTime=.1;
    public double MotionTrackingGapSeconds=.02;
    public bool IndexTipPoseValid=true,IndexDistalPoseValid=true,SampleTipInside=true,SampleDistalInside;
    public Pose IndexTipPose2,IndexDistalPose;
    public UnityEngine.Vector3 MotionPreviousTipPosition;
    public UnityEngine.Bounds MotionSampleBounds;
}
'@
$harness = @'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
public static class SpeedTelemetryChecks {
    private static int checks;
    private static void Require(bool condition,string message) {
        checks++; if(!condition) throw new Exception(message);
    }
    private static List<string> Parse(string csv) {
        var values=new List<string>(); var current=new System.Text.StringBuilder(); bool quoted=false;
        for(int i=0;i<csv.Length;i++) {
            char c=csv[i];
            if(c=='"') { if(quoted && i+1<csv.Length && csv[i+1]=='"') {current.Append('"');i++;} else quoted=!quoted; }
            else if(c==',' && !quoted) {values.Add(current.ToString());current.Clear();} else current.Append(c);
        }
        values.Add(current.ToString()); return values;
    }
    private sealed class Sink {
        public List<Dictionary<string,string>> Rows=new List<Dictionary<string,string>>();
        public List<Dictionary<string,string>> Summaries=new List<Dictionary<string,string>>();
        public SpeedStudyTelemetry Tracker; private long next;
        public Sink() { Tracker=new SpeedStudyTelemetry(Write,()=>++next); }
        private void Write(string line,bool summary) {
            var header=Parse(SpeedStudyTelemetry.Header);var fields=Parse(line);
            Require(header.Count==fields.Count,"CSV width mismatch");
            var row=header.Zip(fields,(h,v)=>new {h,v}).ToDictionary(x=>x.h,x=>x.v);
            Rows.Add(row); if(summary) Summaries.Add(row);
        }
    }
    private static SpeedStudySnapshot Sample(long n,float speed,bool inside=true,bool armed=false) {
        return new SpeedStudySnapshot {
            Case=7,Id=3,Mode=21,BallIndex=4,BallKind=1,Source="target",SourceInstanceId=42,
            Time=n*.02,FixedTime=n*.02,Realtime=n*.021,MotionFixedTime=n*.02,MotionSampleSequence=n,
            FixedDeltaTime=.02f,MotionDeltaTime=.02f,RequestedHz=50,TimeScale=1,InFixedStep=true,
            Speed=speed,MinSpeed=.067f,MaxSpeed=.375f,DwellTimeout=.7f,Armed=armed,HandInBall=inside,
            TipInside=inside,TipTracked=true,DistalTracked=true,EnableTrigger=true,AllowHighSpeedTrigger=true,
            VelocitySource="AcceStudy3.v"
        };
    }
    private static Sink Prepared() {
        var sink=new Sink();sink.Tracker.Inside(Sample(1,.04f));sink.Tracker.Armed(Sample(1,.04f,true,true));return sink;
    }
    public static int Run() {
        checks=0;
        // A trigger must retain the entry's Case/ID, even after the live trial advances.
        var sink=Prepared(); var changed=Sample(2,.5f,true,true);changed.Case=8;changed.Id=1;
        sink.Tracker.Inside(changed);changed.Triggered=true;sink.Tracker.Finish(changed,"trigger","selected");
        sink.Tracker.Finish(changed,"trigger","duplicate");sink.Tracker.Exit(Sample(3,.5f,false,true));sink.Tracker.Disable(changed);
        Require(sink.Summaries.Count==1,"Trigger/exit/disable double counted");
        Require(sink.Summaries[0]["case"]=="7" && sink.Summaries[0]["id"]=="3","Frozen context lost");
        Require(sink.Summaries[0]["currentCase"]=="8","Observed Case missing");
        Require(sink.Summaries[0]["exitFrameSpeed"]=="NA","Trigger speed mislabeled as exit speed");
        // An armed contact can cross the boundary before the first observed high speed.
        sink=Prepared();sink.Tracker.Inside(Sample(2,.2f,true,true));sink.Tracker.Exit(Sample(3,.5f,false,true));
        var exit=sink.Summaries.Single();
        Require(exit["boundaryHighSpeedCandidate"]=="1","Boundary candidate missed");
        Require(exit["speedMaxObservedInside"]=="0","Exit speed counted as inside");
        Require(exit["armedEver"]=="1" && exit["insideSampleCount"]=="2","Entry statistics wrong");
        // Fast entry before arming must not hide a later boundary candidate.
        sink=new Sink();sink.Tracker.Inside(Sample(1,.5f));sink.Tracker.Inside(Sample(2,.04f));
        sink.Tracker.Armed(Sample(2,.04f,true,true));sink.Tracker.Exit(Sample(3,.5f,false,true));
        Require(sink.Summaries[0]["boundaryHighSpeedCandidate"]=="1","Pre-armed peak incorrectly excludes candidate");
        // No preparation, invalid tracking, or a distal point still inside are distinct cases.
        sink=new Sink();sink.Tracker.Inside(Sample(1,.2f));sink.Tracker.Exit(Sample(2,.5f,false));
        Require(sink.Summaries[0]["reason"]=="minimum_speed_not_reached_before_exit","Missing-min classification wrong");
        Require(sink.Summaries[0]["boundaryHighSpeedCandidate"]=="0","Unarmed false candidate");
        foreach(var invalid in new[]{"tip","distal","inside"}) {
            sink=Prepared();changed=Sample(2,.5f,false,true);
            if(invalid=="tip") changed.TipTracked=false;
            if(invalid=="distal") changed.DistalTracked=false;
            if(invalid=="inside") changed.DistalInside=true;
            sink.Tracker.Exit(changed);Require(sink.Summaries[0]["boundaryHighSpeedCandidate"]=="0","Invalid exit candidate: "+invalid);
        }
        // Direction-gated peaks are observed separately from allowed speed triggers.
        sink=Prepared();changed=Sample(2,.5f,true,true);changed.AllowHighSpeedTrigger=false;
        sink.Tracker.Inside(changed);sink.Tracker.Check(changed);sink.Tracker.Exit(Sample(3,.5f,false,true));
        Require(sink.Rows.Any(r=>r["reason"]=="high_speed_gate_blocked"),"Direction gate missing");
        Require(sink.Summaries[0]["speedMaxObservedAfterArmed"]=="1","Armed peak missing");
        Require(sink.Summaries[0]["boundaryHighSpeedCandidate"]=="0","Gated peak mislabeled as sampling candidate");
        // One inside sample per motion sequence; Update checks retain their real phase.
        sink=Prepared();changed=Sample(1,.04f,true,true);changed.InFixedStep=false;
        sink.Tracker.Inside(changed);sink.Tracker.Check(changed);sink.Tracker.Finish(changed,"cancel","release, wrist \"test\"");
        sink.Tracker.Exit(Sample(2,.5f,false,true));
        Require(sink.Summaries.Count==1 && sink.Summaries[0]["insideSampleCount"]=="1","Duplicate sample or summary");
        Require(sink.Summaries[0]["reason"]=="release, wrist \"test\"","CSV quoting failed");
        Require(sink.Summaries[0]["inFixedStep"]=="0","Update phase lost");
        // Aborts remain in the data and do not become trigger failures implicitly.
        sink=Prepared();sink.Tracker.Disable(Sample(2,.1f,true,true));sink.Tracker.Disable(Sample(3,.1f));
        Require(sink.Summaries.Count==1 && sink.Summaries[0]["outcome"]=="aborted","Abort accounting wrong");
        sink.Tracker.Inside(Sample(4,.04f));sink.Tracker.Exit(Sample(5,.2f,false));
        Require(sink.Summaries[1]["entrySequence"]=="2" && sink.Summaries[1]["armedEver"]=="0","Reentry did not reset statistics");
        sink=Prepared();sink.Tracker.AbortPending("session_closed_before_outcome");sink.Tracker.AbortPending("duplicate_close");
        Require(sink.Summaries.Count==1 && sink.Summaries[0]["outcome"]=="aborted","Session close dropped or duplicated a pending entry");
        sink=new Sink();changed=Sample(1,.04f);changed.SourceInstanceId=123456789;
        sink.Tracker.Inside(changed);sink.Tracker.Exit(Sample(2,.5f,false));
        Require(sink.Summaries[0]["sourceInstanceId"]=="123456789","Integer source identity lost float precision");
        // Runtime rate/threshold changes must be visible rather than silently pooled.
        sink=Prepared();changed=Sample(2,.2f,true,true);changed.FixedDeltaTime=1f/30;changed.RequestedHz=30;
        changed.MaxSpeed=.4f;sink.Tracker.Inside(changed);sink.Tracker.Exit(changed);
        Require(sink.Summaries[0]["mixedSamplingRate"]=="1" && sink.Summaries[0]["settingsChanged"]=="1","Mixed settings unmarked");
        Require(Math.Abs(double.Parse(sink.Summaries[0]["fixedSamplingHz"],CultureInfo.InvariantCulture)-30)<.00001,"30 Hz metadata wrong");
        Require(sink.Summaries[0]["entryMaxSpeedThreshold"]!=sink.Summaries[0]["maxSpeedThreshold"],"Entry threshold missing");
        // Losing tracking is not a valid fresh speed observation.
        sink=Prepared();changed=Sample(2,.04f);changed.TipTracked=false;
        sink.Tracker.TrackingLost(changed);sink.Tracker.TrackingLost(changed);
        Require(sink.Rows.Count(r=>r["event"]=="tracking_lost")==1,"Tracking loss duplicated");
        changed=Sample(3,.5f,false,true);sink.Tracker.Exit(changed);
        Require(sink.Summaries[0]["boundaryHighSpeedCandidate"]=="0","Tracking loss treated as clean boundary crossing");
        sink=Prepared();changed=Sample(4,.5f,false,true);changed.MotionTrackingGapSeconds=.06;
        sink.Tracker.Exit(changed);
        Require(sink.Summaries[0]["trackingLossObserved"]=="1" && sink.Summaries[0]["boundaryHighSpeedCandidate"]=="0","Fixed-step tracking gap ignored");
        // Capture uses the sampled AcceStudy3 values and keeps the estimator unchanged.
        var acce=new AcceStudy3();var host=new BallSpawner();
        var snapshot=SpeedStudySnapshot.Capture(new UnityEngine.Component(),host,acce,21,4,1,false,"target",acce.v,true,true,false,false,true);
        Require(snapshot.MotionSampleSequence==5 && snapshot.TipTracked && snapshot.TipInside,"Sample provenance missing");
        Require(acce.v==.04f && snapshot.Speed==acce.v,"Telemetry modified velocity");
        Require(SpeedStudyTelemetry.IsSpeedMode(15) && SpeedStudyTelemetry.IsSpeedMode(20) && !SpeedStudyTelemetry.IsSpeedMode(30),"Mode coverage wrong");
        var culture=CultureInfo.CurrentCulture;
        try {CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");sink=Prepared();sink.Tracker.Exit(Sample(2,.5f,false,true));
            Require(sink.Summaries[0]["decisionSpeed"]=="0.5","Culture-dependent numeric CSV");}
        finally {CultureInfo.CurrentCulture=culture;}
        return checks;
    }
}
'@
$stubPath = Join-Path $testDirectory 'Stubs.cs'
$harnessPath = Join-Path $testDirectory 'Checks.cs'
$stubs | Set-Content -LiteralPath $stubPath -Encoding utf8
$harness | Set-Content -LiteralPath $harnessPath -Encoding utf8
Add-Type -Path @($stubPath, (Join-Path $projectRoot 'Assets\Scripts\Study3\SpeedStudyTelemetry.cs'), $harnessPath)
$count = [SpeedTelemetryChecks]::Run()
Write-Output "Speed telemetry validation passed: $count assertions (isolated observer tests; not a Unity scene run)."

using System;
using UnityEngine;
public static class DirectionConsecutiveChecks {
    static int count;
    static void Require(bool condition,string message) { count++; if(!condition) throw new Exception(message); }
    static Study3DirectionConsecutiveTracker.Observation Step(Study3DirectionConsecutiveTracker t,long n,float x,float y,float z,
        uint source=1,long epoch=1,double time=-1,bool horizontal=false,float minimumSpeed=.05f) =>
        t.Evaluate(source,epoch,n,time<0?n*.02:time,new Vector3(x,y,z),new Vector3(0,0,-1),horizontal,45,minimumSpeed,.25);
    public static int Run() {
        count=0; var t=new Study3DirectionConsecutiveTracker();
        Require(Step(t,1,0,0,0)==Study3DirectionConsecutiveTracker.Observation.Primed,"First sample must only prime");
        // Very slow matching movements are filtered and break the consecutive run.
        for(int i=1;i<=4;i++) {
            Require(Step(t,i+1,0,0,-i*.0002f)==Study3DirectionConsecutiveTracker.Observation.BelowMinimumSpeed, "Slow movement was not filtered");
            Require(!t.CancelPass && t.ConsecutiveMatches==0 && !t.SpeedPass, "Low speed must not count as an angle");
        }
        for(int i=1;i<=3;i++) { Step(t,i+5,0,0,-.0008f-i*.002f); Require(!t.CancelPass && t.ConsecutiveMatches==i,"Early or missing match"); }
        Step(t,9,0,0,-.0088f); Require(t.CancelPass && t.ConsecutiveMatches==4,"Four qualifying frames did not cancel");
        Step(t,10,0,0,-.009f); Require(!t.CancelPass && t.ConsecutiveValidFrames==0,"Low speed must reset an existing streak");
        // Zero threshold intentionally allows tiny nonzero movements.
        t.Reset(); Step(t,1,0,0,0,minimumSpeed:0);
        for(int i=1;i<=4;i++) Step(t,i+1,0,0,-i*1e-8f,minimumSpeed:0);
        Require(t.CancelPass,"Zero threshold should allow tiny nonzero motion");
        // Exact binary-representable boundary: .03125 m / .25 s = .125 m/s.
        t.Reset(); Step(t,1,0,0,0,time:0,minimumSpeed:.125f);
        Step(t,2,0,0,-.03125f,time:.25,minimumSpeed:.125f);
        Require(t.SpeedPass && t.ConsecutiveMatches==1 && t.Speed==.125f,"Minimum-speed equality must pass");
        // Actual sample interval matters; 1 mm in 40 ms is below 0.05 m/s.
        t.Reset(); Step(t,1,0,0,0,time:0); Step(t,2,0,0,-.001f,time:.04);
        Require(!t.SpeedPass && t.ConsecutiveMatches==0,"Sample interval not used for speed filter");
        Step(t,3,0,0,-.002f,time:.05); Require(t.SpeedPass && t.ConsecutiveMatches==1,"Faster movement should pass");
        // Horizontal angle mode still filters on the full 3D speed.
        t.Reset(); Step(t,1,0,0,0,horizontal:true); Step(t,2,0,.002f,-.0002f,horizontal:true);
        Require(t.SpeedPass && t.ConsecutiveMatches==1,"Horizontal mode must use 3D speed");
        t.Reset(); Step(t,1,0,0,0); Step(t,2,0,0,-1); Step(t,3,0,0,-2); Step(t,4,0,0,-3);
        Step(t,5,1,0,-3); Require(!t.CancelPass && t.ConsecutiveMatches==0,"Out-of-cone frame failed to reset streak");
        Step(t,6,1,0,-4); Step(t,7,1,0,-5); Step(t,8,1,0,-6); Require(!t.CancelPass && !t.AllowSpeedConfirmation,"Three matching frames prematurely canceled/confirmed");
        Step(t,9,1,0,-7); Require(t.CancelPass,"Four matching frames after reset did not cancel");
        t.Reset(); Step(t,1,0,0,0); Step(t,2,0,0,-1); Step(t,3,0,0,-2);
        Require(Step(t,3,0,0,-2)==Study3DirectionConsecutiveTracker.Observation.Duplicate && t.ConsecutiveMatches==2,"Duplicate counted twice");
        Require(Step(t,4,0,0,-2)==Study3DirectionConsecutiveTracker.Observation.BelowMinimumSpeed && t.ConsecutiveMatches==0,"Stillness must reset streak");
        Step(t,5,0,0,-3); Step(t,6,0,0,-4); Step(t,7,0,0,-5); Require(!t.CancelPass,"Stillness did not break four-frame run");
        Step(t,8,0,0,-6); Require(t.CancelPass,"Four post-stillness matches did not cancel");
        t.Reset(); Step(t,1,0,0,0); Step(t,2,0,0,-1); Step(t,3,0,0,-2);
        Step(t,4,0,0,-3,1,1,1.0); Require(t.ConsecutiveMatches==0,"Tracking gap did not reset streak");
        Step(t,5,0,0,-4,2,1,1.02); Require(t.ConsecutiveMatches==0,"Hand source change did not reset streak");
        Step(t,6,0,0,-5,2,2,1.04); Require(t.ConsecutiveMatches==0,"Tracking epoch change did not reset streak");
        t.Reset(); Step(t,1,0,0,0); for(int i=1;i<=3;i++) Step(t,i+1,i,0,0);
        Require(!t.AllowSpeedConfirmation,"Speed selection allowed before four valid directions");
        Step(t,5,4,0,0); Require(t.AllowSpeedConfirmation && !t.CancelPass,"Four outside directions must permit Speed confirmation");
        Step(t,6,4.0001f,0,0); Require(!t.AllowSpeedConfirmation && t.ConsecutiveValidFrames==0,"Low speed must reset direction readiness");
        for(int i=1;i<=4;i++) Step(t,6+i,4+i,0,0); Require(t.AllowSpeedConfirmation,"Direction readiness should recover after four qualifying updates");
        Step(t,11,8,0,-1); Require(!t.AllowSpeedConfirmation && t.ConsecutiveMatches==1,"Inside cone must defer Speed confirmation");
        t.Reset(); Step(t,1,0,0,0,1,1,-1,true); Step(t,2,0,1,0,1,1,-1,true);
        Require(t.ConsecutiveValidFrames==0,"Pure vertical motion has no horizontal angle");
        t.Reset(); Step(t,1,0,0,0); Step(t,2,float.NaN,0,0);
        Require(t.ConsecutiveValidFrames==0 && !t.CancelPass,"Invalid position must reset");
        return count;
    }
}

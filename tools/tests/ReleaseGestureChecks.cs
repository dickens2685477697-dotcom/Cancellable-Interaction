using System;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
public static class ReleaseGestureChecks {
    static int count;
    static void Require(bool value, string message) { count++; if (!value) throw new Exception(message); }
    static bool Near(float a, float b) { return Math.Abs(a-b)<.00001f; }
    static void Joint(TrackedHandJoint joint, float x) { Study3HandSamplingDiagnostics.Positions[joint]=new Vector3(x,0,0); }
    static void Fingers(float middle, float ring, float pinky) {
        Joint(TrackedHandJoint.ThumbTip,0); Joint(TrackedHandJoint.MiddleTip,middle);
        Joint(TrackedHandJoint.RingTip,ring); Joint(TrackedHandJoint.PinkyTip,pinky);
    }
    static void Fingers(float distance) { Fingers(distance,distance,distance); }
    public static int Run() {
        count=0; Study3HandSamplingDiagnostics.Positions.Clear();
        var g = new ReleaseGesture { distanceDeltaThreshold=.01f };
        Fingers(.03f); Require(!g.CheckRelease(),"No cancellation before pending");
        g.BeginRelease(); Require(Near(g.baselineAverageDistance,.03f),"Pending transition did not capture baseline");
        Fingers(.034f); Require(!g.CheckRelease(),"Canceled below cumulative threshold");
        Fingers(.038f); Require(!g.CheckRelease(),"Canceled below cumulative threshold after second step");
        Fingers(.039f); Require(!g.CheckRelease(),"Canceled just below threshold");
        Fingers(.041f); Require(g.CheckRelease(),"Slow incremental opening did not cancel");
        Require(g.ReleaseEvent.Count==1 && Near(g.AttributionDistanceDelta,.011f),"Cancellation count or delta incorrect");
        Require(Near(g.AttributionBaselineDistance,.03f) && Near(g.AttributionCurrentDistance,.041f),"Attribution lost pending baseline or current distance");
        Fingers(.08f); Require(!g.CheckRelease(),"Canceled again without a new pending state");
        Fingers(.09f); g.BeginRelease(); Fingers(.099f); Require(!g.CheckRelease(),"Reentry reused old baseline");
        Fingers(.101f); Require(g.CheckRelease(),"Reentry did not use new baseline");
        g.distanceDeltaThreshold=.125f; Fingers(.125f); g.BeginRelease(); Fingers(.25f);
        Require(g.CheckRelease(),"Exact threshold must cancel");
        // Enter with an open hand, close, then reopen while still below the entry distance.
        g.distanceDeltaThreshold=.01f; Fingers(.06f); g.BeginRelease(); Fingers(.05f);
        Require(!g.CheckRelease(),"Closing from an initially open hand canceled"); Fingers(.03f);
        Require(!g.CheckRelease() && Near(g.minimumAverageDistance,.03f),"Closing did not update the running minimum");
        Fingers(.035f); Require(!g.CheckRelease(),"Small reopening canceled before the threshold");
        Fingers(.039f); Require(!g.CheckRelease(),"Reopening just below threshold canceled");
        Fingers(.041f); Require(g.CheckRelease(),"Reopening below the entry distance failed to cancel");
        Require(Near(g.AttributionBaselineDistance,.06f) && Near(g.AttributionMinimumDistance,.03f)
            && Near(g.AttributionDistanceDelta,.011f),"Reopening attribution lost the initial or minimum distance");
        // A new lower minimum replaces the first; jittering does not accumulate positive deltas.
        Fingers(.06f); g.BeginRelease(); Fingers(.03f); Require(!g.CheckRelease(),"Closing unexpectedly canceled");
        Fingers(.035f); Require(!g.CheckRelease(),"Partial reopening unexpectedly canceled");
        Fingers(.025f); Require(!g.CheckRelease(),"Further closing unexpectedly canceled");
        Fingers(.034f); Require(!g.CheckRelease(),"New minimum was not applied");
        Fingers(.036f); Require(g.CheckRelease(),"Reopening from the updated minimum was missed");
        Fingers(.04f); g.BeginRelease(); Fingers(.044f); Require(!g.CheckRelease(),"Small change canceled");
        Fingers(.041f); Require(!g.CheckRelease(),"Small closing canceled"); Fingers(.045f);
        Require(!g.CheckRelease(),"Jittering incorrectly accumulated all positive changes");
        Fingers(.051f); Require(g.CheckRelease(),"Net reopening from the minimum was missed");
        g.distanceDeltaThreshold=0f; Fingers(.05f); g.BeginRelease();
        Require(!g.CheckRelease(),"A stationary hand canceled at a zero threshold");
        Fingers(.04f); Require(!g.CheckRelease(),"A closing hand canceled at a zero threshold");
        Fingers(.0401f); Require(g.CheckRelease(),"Positive reopening at a zero threshold was missed");
        g.distanceDeltaThreshold=.01f;
        Fingers(.02f); Study3HandSamplingDiagnostics.Positions.Remove(TrackedHandJoint.PinkyTip); g.BeginRelease();
        Require(!g.CheckRelease(),"Missing pending fingertip accepted"); Fingers(.04f);
        Require(!g.CheckRelease() && Near(g.AttributionBaselineDistance,.04f),"First valid pending sample did not establish baseline");
        Fingers(.045f); Require(!g.CheckRelease(),"Canceled below threshold after recovered baseline");
        Study3HandSamplingDiagnostics.Positions.Remove(TrackedHandJoint.MiddleTip); Require(!g.CheckRelease(),"Missing middle fingertip canceled");
        Fingers(.051f); Require(g.CheckRelease(),"Brief fingertip loss incorrectly changed baseline");
        Fingers(float.NaN); g.BeginRelease(); Require(!g.CheckRelease(),"Non-finite pose accepted");
        Fingers(.03f); Require(!g.CheckRelease(),"Valid pose after non-finite data must establish baseline");
        Fingers(.041f); Require(g.CheckRelease(),"Opening after valid baseline was missed");
        Fingers(.03f); g.BeginRelease(); Fingers(.063f,.03f,.03f);
        Require(g.CheckRelease() && Near(g.AttributionDistanceDelta,.011f),"Three fingertip distances were not averaged");
        Fingers(.03f); Joint(TrackedHandJoint.IndexTip,.02f); g.ResetGesture();
        Require(!g.CheckIndexThumbRelease(.2f),"Pinch opening needs two samples");
        Joint(TrackedHandJoint.IndexTip,.025f); Require(g.CheckIndexThumbRelease(.2f),"Pinch opening-speed detector changed");
        return count;
    }
}

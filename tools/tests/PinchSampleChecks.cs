using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public static class PinchSampleChecks
{
    private static int checks;
    private static void Check(bool result, string message) { checks++; if (!result) throw new Exception(message); }
    private static void Call(Pinch pinch, string method, params object[] values)
    {
        typeof(Pinch).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pinch, values);
    }
    private static Study3JointSample Sample(long sequence, double time, float thumb, bool includeThumb = true)
    {
        var poses = new Dictionary<TrackedHandJoint, MixedRealityPose>();
        poses[TrackedHandJoint.IndexTip] = new MixedRealityPose { Position = Vector3.zero };
        if (includeThumb) poses[TrackedHandJoint.ThumbTip] = new MixedRealityPose { Position = new Vector3(thumb, 0, 0) };
        return new Study3JointSample(17, 1, sequence, time, poses);
    }
    private static void Deliver(Pinch pinch, Study3JointSample sample)
    {
        Call(pinch, "OnJointSampleUpdated", sample);
        Call(pinch, "OnJointSampleGesture", sample);
    }
    public static int Run()
    {
        checks = 0;
        var pinch = new Pinch(); Call(pinch, "Awake");
        int starts = 0, ends = 0;
        pinch.PinchStart.AddListener(() => starts++); pinch.PinchEnd.AddListener(() => ends++);
        Deliver(pinch, Sample(1, 1, .01f));
        Check(pinch.isPinch && starts == 1 && ends == 0, "Pinch start");
        Deliver(pinch, Sample(1, 1, .06f));
        Check(pinch.isPinch && starts == 1 && ends == 0, "Duplicate sample");
        Deliver(pinch, Sample(2, 1.02, .03f));
        Check(pinch.isPinch && ends == 0, "Hysteresis");
        Deliver(pinch, Sample(3, 1.04, .06f));
        Check(!pinch.isPinch && ends == 1, "Release");
        Deliver(pinch, Sample(4, 1.06, .01f));
        Deliver(pinch, Sample(5, 1.08, 0, false));
        Check(!pinch.isPinch && ends == 1, "Missing joint must not emit release");
        Deliver(pinch, Sample(6, 1.10, .01f));
        Call(pinch, "OnSourceLost", (uint)17);
        Check(!pinch.isPinch && ends == 1, "Tracking loss must not emit release");
        return checks;
    }
}

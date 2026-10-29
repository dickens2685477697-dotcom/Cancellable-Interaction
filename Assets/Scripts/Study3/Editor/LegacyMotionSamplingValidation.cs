using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-only replay against the real legacy component handlers; not a device test.</summary>
public static class LegacyMotionSamplingValidation
{
    private static int checks;

    [MenuItem("Study3/Validate legacy MRTK motion")]
    public static void Run()
    {
        checks = 0;
        var go = new GameObject("LegacyMotionValidation");
        go.SetActive(false);
        try
        {
            go.AddComponent<BoxCollider>().size = Vector3.one * 2f;
            var index = go.AddComponent<AcceStudy3>();
            var thumb = go.AddComponent<AcceStudy3_2>();
            var react = go.AddComponent<ReactTap>();
            go.SetActive(true);
            Invoke(index, "Start");
            Invoke(thumb, "Start");
            Invoke(react, "Start");
            thumb.triggerDis = .5f;
            Invoke(index, "ResetMotion", false);
            Invoke(thumb, "ResetMotionBaseline");
            Invoke(react, "ResetMotionBaseline");

            var components = new MonoBehaviour[] { index, thumb, react };
            double start = Time.realtimeSinceStartupAsDouble;
            foreach (var component in components)
            {
                int inEvents = 0;
                int outEvents = 0;
                int selectionEvents = 0;
                var inEvent = (UnityEngine.Events.UnityEvent)component.GetType().GetField("InEvent").GetValue(component);
                var afterOut = (UnityEngine.Events.UnityEvent)component.GetType().GetField("AfterOutEvent").GetValue(component);
                var selection = (UnityEngine.Events.UnityEvent)component.GetType().GetField("OutEvent").GetValue(component);
                inEvent.AddListener(() => inEvents++);
                afterOut.AddListener(() => outEvents++);
                selection.AddListener(() => selectionEvents++);
                Deliver(component, Sample(1, 1, 1, start, 0f));
                Deliver(component, Sample(1, 1, 2, start + .04, .01f));
                Near(Read(component, "v"), .25f, component.name + " elapsed velocity");
                int afterSecond = inEvents;
                Deliver(component, Sample(1, 1, 2, start + .04, .20f));
                Near(Read(component, "v"), .25f, component.name + " duplicate sample");
                Require(inEvents == afterSecond, component.name + " duplicate sample repeated an event");
                Deliver(component, Sample(1, 1, 3, start + .06, .02f));
                Near(Read(component, "v"), .5f, component.name + " nonuniform velocity");
                Near(Read(component, "a"), 12.5f, component.name + " nonuniform acceleration");
                if (!(component is ReactTap))
                {
                    Require(inEvents > afterSecond, component.name + " new inside sample did not emit entry event");
                    Near(Read(component, "UpdateT"), .06f, component.name + " elapsed dwell");
                }

                Deliver(component, Sample(1, 1, 4, start + .08, 2f));
                Require(outEvents == 1, component.name + " did not emit exit on the exit sample");

                Invoke(component, component is AcceStudy3 ? "OnMotionSourceLost" : "OnSelectedSourceLost", (uint)1);
                int beforeRecoveryOut = outEvents;
                Deliver(component, Sample(1, 2, 5, start + .10, 10f));
                Require(Read(component, "v") != .5f && outEvents == beforeRecoveryOut, component.name + " did not reset after loss");
                Deliver(component, Sample(1, 2, 6, start + .14, 10.01f));
                Near(Read(component, "v"), .25f, component.name + " recovery velocity");
                Deliver(component, Sample(1, 2, 7, start + .5, 20f));
                Require(Read(component, "v") != .25f, component.name + " bridged a long gap");
                Deliver(component, Sample(1, 2, 8, start + .54, 0f));
                int beforeLossExit = outEvents;
                int beforeLossSelection = selectionEvents;
                Invoke(component, component is AcceStudy3 ? "OnMotionSourceLost" : "OnSelectedSourceLost", (uint)1);
                Deliver(component, Sample(1, 3, 9, start + .58, 2f));
                Deliver(component, Sample(1, 3, 10, start + .62, 2.01f));
                Require(outEvents == beforeLossExit && selectionEvents == beforeLossSelection,
                    component.name + " produced a normal exit or selection after tracking loss");
                if (component is AcceStudy3)
                {
                    Deliver(component, Sample(1, 4, 11, start + .66, 0f));
                    Deliver(component, Sample(1, 4, 12, start + .70, .01f));
                    int beforeMissingExit = outEvents;
                    int beforeMissingSelection = selectionEvents;
                    Deliver(component, Sample(1, 4, 13, start + .74, 2f, false));
                    Deliver(component, Sample(1, 4, 14, start + .78, 2f));
                    Deliver(component, Sample(1, 4, 15, start + .82, 2.01f));
                    Require(outEvents == beforeMissingExit && selectionEvents == beforeMissingSelection,
                        "Missing distal joint produced a normal exit or selection");
                }
            }
            var pinch = go.AddComponent<Pinch>();
            Invoke(pinch, "Awake");
            int pinchStarts = 0, pinchEnds = 0;
            pinch.PinchStart.AddListener(() => pinchStarts++);
            pinch.PinchEnd.AddListener(() => pinchEnds++);
            double pinchTime = Time.realtimeSinceStartupAsDouble;
            DeliverPinch(pinch, PinchSample(1, pinchTime, .01f));
            Require(pinch.isPinch && pinchStarts == 1, "Pinch start missing");
            DeliverPinch(pinch, PinchSample(1, pinchTime, .06f));
            Require(pinch.isPinch && pinchStarts == 1 && pinchEnds == 0, "Duplicate pinch sample changed state");
            DeliverPinch(pinch, PinchSample(2, pinchTime + .02, .03f));
            Require(pinch.isPinch && pinchEnds == 0, "Pinch hysteresis changed");
            DeliverPinch(pinch, PinchSample(3, pinchTime + .04, .06f));
            Require(!pinch.isPinch && pinchEnds == 1, "Pinch end missing");
            DeliverPinch(pinch, PinchSample(4, pinchTime + .06, .01f));
            Invoke(pinch, "OnSourceLost", (uint)1);
            Require(!pinch.isPinch && pinchEnds == 1, "Tracking loss emitted a normal pinch end");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        Debug.Log("Legacy MRTK motion validation passed: " + checks + " assertions (Editor replay, not headset test).");
    }

    private static void Deliver(MonoBehaviour component, Study3JointSample sample)
    {
        Invoke(component, "OnJointSampleUpdated", sample);
        Invoke(component, "OnJointSampleProcessed", sample);
    }

    private static void DeliverPinch(Pinch pinch, Study3JointSample sample)
    {
        Invoke(pinch, "OnJointSampleUpdated", sample);
        Invoke(pinch, "OnJointSampleGesture", sample);
    }

    private static Study3JointSample PinchSample(long sequence, double timestamp, float thumbX)
    {
        var joints = new Dictionary<TrackedHandJoint, MixedRealityPose>();
        joints[TrackedHandJoint.IndexTip] = new MixedRealityPose(Vector3.zero, Quaternion.identity);
        joints[TrackedHandJoint.ThumbTip] = new MixedRealityPose(new Vector3(thumbX, 0, 0), Quaternion.identity);
        return new Study3JointSample(1, 1, sequence, timestamp, joints);
    }

    private static Study3JointSample Sample(uint source, long epoch, long sequence, double time, float x, bool hasIndexDistal = true)
    {
        var position = new Vector3(x, 0, 0);
        var joints = new Dictionary<TrackedHandJoint, MixedRealityPose>();
        joints[TrackedHandJoint.IndexTip] = new MixedRealityPose(position, Quaternion.identity);
        joints[TrackedHandJoint.ThumbTip] = new MixedRealityPose(position, Quaternion.identity);
        if (hasIndexDistal) joints[TrackedHandJoint.IndexDistalJoint] = new MixedRealityPose(position + Vector3.up, Quaternion.identity);
        joints[TrackedHandJoint.ThumbDistalJoint] = new MixedRealityPose(position + Vector3.up, Quaternion.identity);
        joints[TrackedHandJoint.IndexKnuckle] = new MixedRealityPose(position + Vector3.up, Quaternion.identity);
        joints[TrackedHandJoint.Wrist] = new MixedRealityPose(position + Vector3.left, Quaternion.identity);
        return new Study3JointSample(source, epoch, sequence, time, joints);
    }

    private static void Invoke(object target, string method, params object[] args)
    {
        var member = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        if (member == null) throw new Exception(target.GetType().Name + "." + method + " not found");
        try { member.Invoke(target, args); }
        catch (TargetInvocationException e) { throw e.InnerException ?? e; }
    }

    private static float Read(object target, string name) => (float)target.GetType().GetField(name).GetValue(target);
    private static void Near(float actual, float expected, string message)
        => Require(Math.Abs(actual - expected) < .002f, message + ": " + actual + " expected " + expected);
    private static void Require(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
}

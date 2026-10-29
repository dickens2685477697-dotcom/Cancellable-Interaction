using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Pinch : MonoBehaviour
{
    public float ActivateDistance = .02f; //meters
    public float DeactivateDistance = .05f; //meters
    public float distance = 1.0f;
    public bool isPinch = false;
    public UnityEvent PinchStart, PinchEnd;
    public event System.Action AttributionObserved;
    public long AttributionSampleSequence { get; private set; }
    public double AttributionSampleTime { get; private set; }
    public bool AttributionIndexValid { get; private set; }
    public bool AttributionThumbValid { get; private set; }
    private uint motionSource;
    private long motionEpoch;
    private long lastSequence;
    private double lastTimestamp;
    private bool hasSample;
    private int pendingTransition;

    private void Awake()
    {
        PinchStart ??= new UnityEvent();
        PinchEnd ??= new UnityEvent();
    }

    private void OnEnable()
    {
        Study3HandPoseSource.EnsureAvailable();
        Study3HandPoseSource.JointSampleUpdated += OnJointSampleUpdated;
        Study3HandPoseSource.JointSampleGesture += OnJointSampleGesture;
        Study3HandPoseSource.SelectedSourceLost += OnSourceLost;
    }

    private void OnDisable()
    {
        Study3HandPoseSource.JointSampleUpdated -= OnJointSampleUpdated;
        Study3HandPoseSource.JointSampleGesture -= OnJointSampleGesture;
        Study3HandPoseSource.SelectedSourceLost -= OnSourceLost;
        ResetTracking();
    }

    private void ResetTracking()
    {
        hasSample = false;
        AttributionIndexValid = AttributionThumbValid = false;
        pendingTransition = 0;
        isPinch = false;
        distance = 1f;
    }

    private void OnSourceLost(uint sourceId)
    {
        if (hasSample && motionSource == sourceId) ResetTracking();
    }

    private void FixedUpdate()
    {
        if (hasSample && Time.realtimeSinceStartupAsDouble - lastTimestamp > Study3MotionTracker.MaximumAgeSeconds)
            ResetTracking();
    }

    private static bool Finite(Vector3 p)
    {
        return !float.IsNaN(p.x) && !float.IsInfinity(p.x)
            && !float.IsNaN(p.y) && !float.IsInfinity(p.y)
            && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }

    private void OnJointSampleUpdated(Study3JointSample sample)
    {
        bool indexValid = sample.TryGetJointPose(TrackedHandJoint.IndexTip, out MixedRealityPose indexPose);
        bool thumbValid = sample.TryGetJointPose(TrackedHandJoint.ThumbTip, out MixedRealityPose thumbPose);
        if (!indexValid || !thumbValid || !Finite(indexPose.Position) || !Finite(thumbPose.Position)
            || double.IsNaN(sample.Timestamp) || double.IsInfinity(sample.Timestamp))
        {
            ResetTracking();
            return;
        }
        if (hasSample && sample.SourceId == motionSource && sample.TrackingEpoch == motionEpoch
            && sample.Sequence <= lastSequence) return;
        if (hasSample && (sample.SourceId != motionSource || sample.TrackingEpoch != motionEpoch
            || sample.Timestamp <= lastTimestamp || sample.Timestamp - lastTimestamp > Study3MotionTracker.MaximumAgeSeconds))
            ResetTracking();

        hasSample = true;
        motionSource = sample.SourceId;
        motionEpoch = sample.TrackingEpoch;
        lastSequence = sample.Sequence;
        lastTimestamp = sample.Timestamp;
        AttributionSampleSequence = sample.Sequence;
        AttributionSampleTime = sample.Timestamp;
        AttributionIndexValid = AttributionThumbValid = true;
        distance = Vector3.Distance(indexPose.Position, thumbPose.Position);
        pendingTransition = 0;
        if (distance > DeactivateDistance)
        {
            if (isPinch)
            {
                isPinch = false;
                pendingTransition = -1;
            }
        }
        else if (distance < ActivateDistance)
        {
            if (!isPinch)
            {
                isPinch = true;
                pendingTransition = 1;
            }
        }
    }

    private void OnJointSampleGesture(Study3JointSample sample)
    {
        if (!hasSample || sample.SourceId != motionSource || sample.TrackingEpoch != motionEpoch
            || sample.Sequence != lastSequence) return;
        int transition = pendingTransition;
        pendingTransition = 0;
        AttributionObserved?.Invoke();
        if (transition > 0) PinchStart.Invoke();
        else if (transition < 0) PinchEnd.Invoke();
    }
}

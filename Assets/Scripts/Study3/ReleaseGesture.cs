using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ReleaseGesture : MonoBehaviour
{
    // Release Tap uses the frame-to-frame opening speed of the folded fingers,
    // not the absolute distance between fingers.
    [Tooltip("Per-frame increase in the average thumb-to-middle/ring/pinky tip distance required to detect release, in meters.")]
    public float distanceDeltaThreshold = 0.01f;

    public UnityEvent ReleaseEvent = new UnityEvent();

    public float thumbMiddleDistance;
    public float thumbRingDistance;
    public float thumbPinkyDistance;
    public float currentAverageDistance;
    public float previousAverageDistance;
    public float currentDistanceDelta;
    public float indexThumbDistance;
    public float previousIndexThumbDistance;
    public float indexThumbDistanceDelta;
    public float indexThumbOpeningSpeed;

    public event System.Action AttributionObserved;
    private void PublishAttribution() => AttributionObserved?.Invoke();
    public string AttributionObservation { get; private set; } = "none";
    public double AttributionSampleTime { get; private set; }
    public bool AttributionPoseValid { get; private set; }
    public bool AttributionGatePassed { get; private set; }
    public bool AttributionHistoryReady { get; private set; }
    public float AttributionDistanceDelta { get; private set; }
    public float AttributionOpeningSpeed { get; private set; }
    public float AttributionThreshold { get; private set; }

    private bool hasPreviousDistance = false;
    private bool hasPreviousIndexThumbDistance = false;

    public void ResetGesture()
    {
        hasPreviousDistance = false;
        hasPreviousIndexThumbDistance = false;
        previousAverageDistance = 0f;
        previousIndexThumbDistance = 0f;
        currentDistanceDelta = 0f;
        indexThumbDistanceDelta = 0f;
        indexThumbOpeningSpeed = 0f;
    }

    public bool CheckRelease(Handedness handedness = Handedness.Any)
    {
        AttributionObservation = "folded_fingers_release";
        AttributionSampleTime = Time.realtimeSinceStartupAsDouble;
        AttributionGatePassed = false;
        AttributionHistoryReady = false;
        AttributionDistanceDelta = AttributionOpeningSpeed = float.NaN;
        AttributionThreshold = distanceDeltaThreshold;
        AttributionPoseValid = TryGetAverageDistance(handedness, out float averageDistance);
        if (!AttributionPoseValid)
        {
            ResetGesture();
            PublishAttribution();
            return false;
        }

        currentAverageDistance = averageDistance;

        if (!hasPreviousDistance)
        {
            previousAverageDistance = averageDistance;
            hasPreviousDistance = true;
            PublishAttribution();
            return false;
        }

        // A release is detected only when the average distance increases enough
        // compared with the previous frame.
        currentDistanceDelta = averageDistance - previousAverageDistance;
        AttributionHistoryReady = true;
        previousAverageDistance = averageDistance;

        AttributionDistanceDelta = currentDistanceDelta;
        AttributionGatePassed = currentDistanceDelta >= distanceDeltaThreshold;
        if (currentDistanceDelta >= distanceDeltaThreshold)
        {
            PublishAttribution();
            ReleaseEvent.Invoke();
            ResetGesture();
            return true;
        }

        PublishAttribution();
        return false;
    }

    public bool CheckIndexThumbRelease(float openingSpeedThreshold, Handedness handedness = Handedness.Any)
    {
        AttributionObservation = "index_thumb_release";
        AttributionSampleTime = Time.realtimeSinceStartupAsDouble;
        AttributionGatePassed = false;
        AttributionHistoryReady = false;
        AttributionDistanceDelta = AttributionOpeningSpeed = float.NaN;
        AttributionThreshold = openingSpeedThreshold;
        AttributionPoseValid = TryGetIndexThumbDistance(handedness, out float distance);
        if (!AttributionPoseValid)
        {
            ResetGesture();
            PublishAttribution();
            return false;
        }

        indexThumbDistance = distance;

        if (!hasPreviousIndexThumbDistance)
        {
            previousIndexThumbDistance = distance;
            hasPreviousIndexThumbDistance = true;
            PublishAttribution();
            return false;
        }

        indexThumbDistanceDelta = distance - previousIndexThumbDistance;
        AttributionHistoryReady = true;
        previousIndexThumbDistance = distance;
        indexThumbOpeningSpeed = Time.deltaTime > 0f
            ? indexThumbDistanceDelta / Time.deltaTime
            : 0f;

        AttributionDistanceDelta = indexThumbDistanceDelta;
        AttributionOpeningSpeed = indexThumbOpeningSpeed;
        AttributionGatePassed = indexThumbOpeningSpeed >= openingSpeedThreshold;
        if (indexThumbOpeningSpeed >= openingSpeedThreshold)
        {
            PublishAttribution();
            ReleaseEvent.Invoke();
            ResetGesture();
            return true;
        }

        PublishAttribution();
        return false;
    }

    private bool TryGetAverageDistance(Handedness handedness, out float averageDistance)
    {
        averageDistance = 0f;

        // MRTK hand joints used by this mode: thumb tip against middle, ring,
        // and pinky tips. The index finger is intentionally excluded because it
        // is used for pointing at the ball.
        bool hasThumb = Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.ThumbTip, handedness, out MixedRealityPose thumbPose);
        bool hasMiddle = Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.MiddleTip, handedness, out MixedRealityPose middlePose);
        bool hasRing = Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.RingTip, handedness, out MixedRealityPose ringPose);
        bool hasPinky = Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.PinkyTip, handedness, out MixedRealityPose pinkyPose);

        if (!hasThumb || !hasMiddle || !hasRing || !hasPinky)
        {
            return false;
        }

        thumbMiddleDistance = Vector3.Distance(thumbPose.Position, middlePose.Position);
        thumbRingDistance = Vector3.Distance(thumbPose.Position, ringPose.Position);
        thumbPinkyDistance = Vector3.Distance(thumbPose.Position, pinkyPose.Position);
        averageDistance = (thumbMiddleDistance + thumbRingDistance + thumbPinkyDistance) / 3f;
        return true;
    }

    private bool TryGetIndexThumbDistance(Handedness handedness, out float distance)
    {
        distance = 0f;

        bool hasIndex = Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.IndexTip, handedness, out MixedRealityPose indexPose);
        bool hasThumb = Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.ThumbTip, handedness, out MixedRealityPose thumbPose);

        if (!hasIndex || !hasThumb)
        {
            return false;
        }

        distance = Vector3.Distance(indexPose.Position, thumbPose.Position);
        return true;
    }
}

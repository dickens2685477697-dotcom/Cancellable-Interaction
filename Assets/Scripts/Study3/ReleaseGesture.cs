using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ReleaseGesture : MonoBehaviour
{
    // Distance increase from the smallest average distance observed during pending.
    [Tooltip("Increase in average thumb-to-middle/ring/pinky tip distance from the smallest pending distance, in meters.")]
    public float distanceDeltaThreshold = 0.01f;

    public UnityEvent ReleaseEvent = new UnityEvent();

    public float thumbMiddleDistance;
    public float thumbRingDistance;
    public float thumbPinkyDistance;
    public float currentAverageDistance;
    public float baselineAverageDistance;
    public float minimumAverageDistance;
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
    public float AttributionBaselineDistance { get; private set; } = float.NaN;
    public float AttributionMinimumDistance { get; private set; } = float.NaN;
    public float AttributionCurrentDistance { get; private set; } = float.NaN;
    public float AttributionThreshold { get; private set; }

    private bool releaseTrackingActive = false;
    private bool hasReleaseBaseline = false;
    private bool hasPreviousIndexThumbDistance = false;

    public void ResetGesture()
    {
        releaseTrackingActive = false;
        hasReleaseBaseline = false;
        baselineAverageDistance = 0f;
        minimumAverageDistance = 0f;
        hasPreviousIndexThumbDistance = false;
        previousAverageDistance = 0f;
        previousIndexThumbDistance = 0f;
        currentDistanceDelta = 0f;
        indexThumbDistanceDelta = 0f;
        indexThumbOpeningSpeed = 0f;
    }

    // Call at the actual pending transition. If a fingertip is unavailable,
    // the first valid sample while still pending becomes the baseline.
    public void BeginRelease(Handedness handedness = Handedness.Any)
    {
        ResetGesture();
        releaseTrackingActive = true;
        AttributionBaselineDistance = float.NaN;
        AttributionMinimumDistance = float.NaN;
        AttributionCurrentDistance = float.NaN;
        if (TryGetAverageDistance(handedness, out float averageDistance))
        {
            baselineAverageDistance = averageDistance;
            minimumAverageDistance = averageDistance;
            currentAverageDistance = averageDistance;
            previousAverageDistance = averageDistance;
            hasReleaseBaseline = true;
            AttributionBaselineDistance = averageDistance;
            AttributionMinimumDistance = averageDistance;
            AttributionCurrentDistance = averageDistance;
        }
    }

    public bool CheckRelease(Handedness handedness = Handedness.Any)
    {
        AttributionObservation = "folded_fingers_release_from_pending";
        AttributionSampleTime = Time.realtimeSinceStartupAsDouble;
        AttributionGatePassed = false;
        AttributionHistoryReady = hasReleaseBaseline;
        AttributionDistanceDelta = AttributionOpeningSpeed = float.NaN;
        AttributionThreshold = distanceDeltaThreshold;
        float averageDistance = 0f;
        AttributionPoseValid = releaseTrackingActive && TryGetAverageDistance(handedness, out averageDistance);
        if (!AttributionPoseValid)
        {
            // Preserve the pending minimum through a temporary missing fingertip sample.
            // Contact or hand tracking loss resets it when pending ends.
            AttributionCurrentDistance = float.NaN;
            PublishAttribution();
            return false;
        }

        currentAverageDistance = averageDistance;
        AttributionCurrentDistance = averageDistance;
        if (!hasReleaseBaseline)
        {
            baselineAverageDistance = averageDistance;
            minimumAverageDistance = averageDistance;
            previousAverageDistance = averageDistance;
            hasReleaseBaseline = true;
            AttributionBaselineDistance = averageDistance;
            AttributionMinimumDistance = averageDistance;
            PublishAttribution();
            return false;
        }

        // Closing the hand updates the reference; reopening is measured from
        // the smallest average distance seen during this pending interval.
        if (averageDistance < minimumAverageDistance)
        {
            minimumAverageDistance = averageDistance;
        }
        previousAverageDistance = averageDistance;
        currentDistanceDelta = averageDistance - minimumAverageDistance;
        AttributionMinimumDistance = minimumAverageDistance;
        AttributionHistoryReady = true;
        AttributionDistanceDelta = currentDistanceDelta;
        AttributionGatePassed = currentDistanceDelta > 0f && currentDistanceDelta >= distanceDeltaThreshold;
        if (AttributionGatePassed)
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
        return !float.IsNaN(averageDistance) && !float.IsInfinity(averageDistance);
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

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
        if (!TryGetAverageDistance(handedness, out float averageDistance))
        {
            ResetGesture();
            return false;
        }

        currentAverageDistance = averageDistance;

        if (!hasPreviousDistance)
        {
            previousAverageDistance = averageDistance;
            hasPreviousDistance = true;
            return false;
        }

        // A release is detected only when the average distance increases enough
        // compared with the previous frame.
        currentDistanceDelta = averageDistance - previousAverageDistance;
        previousAverageDistance = averageDistance;

        if (currentDistanceDelta >= distanceDeltaThreshold)
        {
            ReleaseEvent.Invoke();
            ResetGesture();
            return true;
        }

        return false;
    }

    public bool CheckIndexThumbRelease(float openingSpeedThreshold, Handedness handedness = Handedness.Any)
    {
        if (!TryGetIndexThumbDistance(handedness, out float distance))
        {
            ResetGesture();
            return false;
        }

        indexThumbDistance = distance;

        if (!hasPreviousIndexThumbDistance)
        {
            previousIndexThumbDistance = distance;
            hasPreviousIndexThumbDistance = true;
            return false;
        }

        indexThumbDistanceDelta = distance - previousIndexThumbDistance;
        previousIndexThumbDistance = distance;
        indexThumbOpeningSpeed = Time.deltaTime > 0f
            ? indexThumbDistanceDelta / Time.deltaTime
            : 0f;

        if (indexThumbOpeningSpeed >= openingSpeedThreshold)
        {
            ReleaseEvent.Invoke();
            ResetGesture();
            return true;
        }

        return false;
    }

    private bool TryGetAverageDistance(Handedness handedness, out float averageDistance)
    {
        averageDistance = 0f;

        // MRTK hand joints used by this mode: thumb tip against middle, ring,
        // and pinky tips. The index finger is intentionally excluded because it
        // is used for pointing at the ball.
        bool hasThumb = HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out MixedRealityPose thumbPose);
        bool hasMiddle = HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, handedness, out MixedRealityPose middlePose);
        bool hasRing = HandJointUtils.TryGetJointPose(TrackedHandJoint.RingTip, handedness, out MixedRealityPose ringPose);
        bool hasPinky = HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyTip, handedness, out MixedRealityPose pinkyPose);

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

        bool hasIndex = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handedness, out MixedRealityPose indexPose);
        bool hasThumb = HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out MixedRealityPose thumbPose);

        if (!hasIndex || !hasThumb)
        {
            return false;
        }

        distance = Vector3.Distance(indexPose.Position, thumbPose.Position);
        return true;
    }
}

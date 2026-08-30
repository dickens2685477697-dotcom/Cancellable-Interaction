using UnityEngine;

[CreateAssetMenu(
    fileName = "Study3SelectionSettings",
    menuName = "Study3/Selection Settings")]
public sealed class Study3SelectionSettings : ScriptableObject
{
    [Header("Feedback visuals")]
    public BallSpawner.RainbowArcPlacement rainbowArcPlacement = BallSpawner.RainbowArcPlacement.OppositeEntry;
    public bool showContact010TriggerRegion;
    public bool showModeReadyRing = true;
    public bool showDirectionCDArrow;
    public bool showDirectionCancelCone;
    [Min(0.01f)] public float directionCancelConeLength = 0.15f;
    public bool useEntryDirectionAxisForDirectionCancel;
    [Range(0f, 180f)] public float entryDirectionConeHalfAngle = 45f;

    [Header("010 Direction")]
    public float direction010CDAngleThreshold = 20f;
    [Range(0f, 1f)] public float direction010SmoothingAlpha = 0.25f;
    [Min(0f)] public float direction010MinSpeedThreshold = 0.05f;
    [Min(2)] public int direction010WindowFrameCount = 10;

    [Header("Pinch Direction")]
    public float pinchDirectionCDAngleThreshold = 20f;
    [Range(0f, 1f)] public float pinchDirectionSmoothingAlpha = 0.25f;
    [Min(0f)] public float pinchDirectionMinSpeedThreshold = 0.05f;
    [Min(2)] public int pinchDirectionWindowFrameCount = 10;

    [Header("010 Direction Test")]
    public float direction010TestCDAngleThreshold = 20f;
    [Range(0f, 1f)] public float direction010TestSmoothingAlpha = 0.25f;
    [Min(0f)] public float direction010TestMinSpeedThreshold = 0.05f;
    [Min(2)] public int direction010TestWindowFrameCount = 10;

    [Header("Time, release, and speed")]
    public float swing010CancelAngleThreshold = 35f;
    public float releaseDistanceDeltaThreshold = 0.01f;
    public float reactReleaseIndexSpeedThreshold = 0.2f;
    public float speedMinThreshold = 0.05f;
    public float speedMaxThreshold = 0.2f;
    public float longPressTimeThreshold = 0.5f;

    [Header("Pinch")]
    public float pinchReleaseOpeningSpeedThreshold = 0.6f;
    public float pinchEndDistanceThreshold = 0.05f;
    public float pinchThumbSwipeDistanceThreshold = 0.03f;
    public float pinchThumbSwipeContactDistanceThreshold = 0.018f;
    [Range(0f, 1f)] public float pinchThumbSwipeDirectionRatioThreshold = 0.75f;
    public float pinchDwellCancelTime = 0.5f;

    [Header("Wrist, press, and withdraw")]
    public float pinchWristTiltCancelAngleThreshold = 30f;
    public float pressSpeedThershold = 0.2f;
    public float dwellWithdrawCancelAngleThreshold = 120f;

    public void ApplyTo(BallSpawner target)
    {
        if (target == null)
        {
            return;
        }

        target.rainbowArcPlacement = rainbowArcPlacement;
        target.showContact010TriggerRegion = showContact010TriggerRegion;
        target.showModeReadyRing = showModeReadyRing;
        target.showDirectionCDArrow = showDirectionCDArrow;
        target.showDirectionCancelCone = showDirectionCancelCone;
        target.directionCancelConeLength = directionCancelConeLength;
        target.useEntryDirectionAxisForDirectionCancel = useEntryDirectionAxisForDirectionCancel;
        target.entryDirectionConeHalfAngle = entryDirectionConeHalfAngle;

        target.direction010CDAngleThreshold = direction010CDAngleThreshold;
        target.direction010SmoothingAlpha = direction010SmoothingAlpha;
        target.direction010MinSpeedThreshold = direction010MinSpeedThreshold;
        target.direction010WindowFrameCount = direction010WindowFrameCount;

        target.pinchDirectionCDAngleThreshold = pinchDirectionCDAngleThreshold;
        target.pinchDirectionSmoothingAlpha = pinchDirectionSmoothingAlpha;
        target.pinchDirectionMinSpeedThreshold = pinchDirectionMinSpeedThreshold;
        target.pinchDirectionWindowFrameCount = pinchDirectionWindowFrameCount;

        target.direction010TestCDAngleThreshold = direction010TestCDAngleThreshold;
        target.direction010TestSmoothingAlpha = direction010TestSmoothingAlpha;
        target.direction010TestMinSpeedThreshold = direction010TestMinSpeedThreshold;
        target.direction010TestWindowFrameCount = direction010TestWindowFrameCount;

        target.swing010CancelAngleThreshold = swing010CancelAngleThreshold;
        target.releaseDistanceDeltaThreshold = releaseDistanceDeltaThreshold;
        target.reactReleaseIndexSpeedThreshold = reactReleaseIndexSpeedThreshold;
        target.speedMinThreshold = speedMinThreshold;
        target.speedMaxThreshold = speedMaxThreshold;
        target.longPressTimeThreshold = longPressTimeThreshold;

        target.pinchReleaseOpeningSpeedThreshold = pinchReleaseOpeningSpeedThreshold;
        target.pinchEndDistanceThreshold = pinchEndDistanceThreshold;
        target.pinchThumbSwipeDistanceThreshold = pinchThumbSwipeDistanceThreshold;
        target.pinchThumbSwipeContactDistanceThreshold = pinchThumbSwipeContactDistanceThreshold;
        target.pinchThumbSwipeDirectionRatioThreshold = pinchThumbSwipeDirectionRatioThreshold;
        target.pinchDwellCancelTime = pinchDwellCancelTime;

        target.pinchWristTiltCancelAngleThreshold = pinchWristTiltCancelAngleThreshold;
        target.pressSpeedThershold = pressSpeedThershold;
        target.dwellWithdrawCancelAngleThreshold = dwellWithdrawCancelAngleThreshold;
    }
}

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

    [Header("Dynamic Stop (demo motion logic)")]
    [InspectorName("Sample Interval (ms)"), Min(0.1f)]
    [Tooltip("最短运动测量间隔，单位毫秒；使用实际经过时间计算速度，不是长按等待时间。")]
    public float dynamicStopSampleMs = 16f;
    [InspectorName("Speed Minimum (m/s)"), Min(0.0001f)]
    [Tooltip("速度阈值的非零下限，单位米/秒。动态阈值 = min(上限, 下限 + 近期速度 × Speed Gain)。")]
    public float dynamicStopSpeedMin = 0.02f;
    [InspectorName("Speed Maximum (m/s)"), Min(0.0001f)]
    [Tooltip("动态速度阈值的上限，单位米/秒；计算时不会小于 Speed Minimum。")]
    public float dynamicStopSpeedMax = 0.12f;
    [InspectorName("Speed Gain"), Min(0f)]
    [Tooltip("近期速度对速度阈值的增量比例，无单位；设为0则使用固定速度下限。")]
    public float dynamicStopSpeedGain = 0.10f;
    [InspectorName("Acceleration Minimum (m/s²)"), Min(0.0001f)]
    [Tooltip("加速度阈值的非零下限，单位米/秒²。动态阈值 = min(上限, 下限 + 近期速度 × Acceleration Gain)。")]
    public float dynamicStopAccelMin = 0.5f;
    [InspectorName("Acceleration Maximum (m/s²)"), Min(0.0001f)]
    [Tooltip("动态加速度阈值的上限，单位米/秒²；计算时不会小于 Acceleration Minimum。")]
    public float dynamicStopAccelMax = 4f;
    [InspectorName("Acceleration Gain (1/s)"), Min(0f)]
    [Tooltip("近期速度对加速度阈值的增量系数，单位1/秒；设为0则使用固定加速度下限。")]
    public float dynamicStopAccelGain = 3f;
    [InspectorName("Speed History Decay (s)"), Min(0.001f)]
    [Tooltip("高速历史的指数衰减时间尺度，单位秒；不是停止后必须等待的时间。")]
    public float dynamicStopHistorySeconds = 0.18f;

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

        target.dynamicStopSampleMs = dynamicStopSampleMs;
        target.dynamicStopSpeedMin = dynamicStopSpeedMin;
        target.dynamicStopSpeedMax = dynamicStopSpeedMax;
        target.dynamicStopSpeedGain = dynamicStopSpeedGain;
        target.dynamicStopAccelMin = dynamicStopAccelMin;
        target.dynamicStopAccelMax = dynamicStopAccelMax;
        target.dynamicStopAccelGain = dynamicStopAccelGain;
        target.dynamicStopHistorySeconds = dynamicStopHistorySeconds;

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

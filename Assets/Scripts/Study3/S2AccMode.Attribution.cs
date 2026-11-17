using System.Collections.Generic;
using UnityEngine;

public partial class S2AccMode
{
    private Study3AttributionBridge attributionBridge;
    private void InitializeAttributionObservation()
    {
        if (logWriter3 == null || logWriter3.IsStateOnly || ballSpawner == null || ballSpawner.executionMode != BallSpawner.ExecutionMode.Study3Experiment) return;
        attributionBridge?.Disable("source_reconfigured");
        attributionBridge = new Study3AttributionBridge(this, logWriter3, CaptureAttributionSample);
        logWriter3.RegisterAttributionBridge(GetInstanceID(), attributionBridge);
        attributionBridge.Attach(Acce, Acce2, reactTap, pinchob, releaseGesture, modeSelected);
    }
    private Study3AttributionSample CaptureAttributionSample()
    {
        var sample = Study3AttributionCapture.Sample(this, ballSpawner, logWriter3, Acce, Acce2, reactTap,
            pinchob, releaseGesture, modeSelected, ballIndex, isMineBall, "target");
        sample.Values["pinchHandledThisGesture"] = pinchHandledThisGesture;
        sample.Values["pinchReleaseCancelTriggered"] = pinchReleaseCancelTriggered;
        sample.Values["pinchReleaseReentryRequired"] = pinchReleaseReentryRequired;
        sample.Values["pinchReleaseWaitForIndexExit"] = pinchReleaseWaitForIndexExit;
        sample.Values["pinchReleaseWaitForThumbExit"] = pinchReleaseWaitForThumbExit;
        sample.Values["pinchDwellEntryTimestamp"] = pinchDwellEntryTimestamp;
        sample.Values["fastTapCancelTime"] = fastTapCancelTime;
        sample.Values["fastTapEntryTimestamp"] = fastTapEntryTimestamp;
        sample.Values["fastTapReactTimestamp"] = fastTapReactTimestamp;
        sample.Values["fastTapReentryRequired"] = fastTapReentryRequired;
        sample.Values["reactReleaseSpeedAcceMet"] = reactReleaseSpeedAcceMet;
        sample.Values["reactReleaseTriggered"] = reactReleaseTriggered;
        sample.Values["reactReleaseReentryRequired"] = reactReleaseReentryRequired;
        sample.Values["speedReleaseMinSpeedMet"] = speedReleaseMinSpeedMet;
        sample.Values["speedReleaseTriggered"] = speedReleaseTriggered;
        sample.Values["speedReleaseReentryRequired"] = speedReleaseReentryRequired;
        sample.Values["fastTapCancelTriggered"] = fastTapCancelTriggered;
        sample.Values["rainbowCancelTriggered"] = rainbowCancelTriggered;
        sample.Values["rainbowArcActive"] = rainbowArcActive;
        sample.Values["rainbowArcInitialized"] = rainbowArcInitialized;
        sample.Values["dwellReleaseCancelTriggered"] = dwellReleaseCancelTriggered;
        sample.Values["dwellReleaseReentryRequired"] = dwellReleaseReentryRequired;
        sample.Values["dwellReleaseInside"] = dwellReleaseInside;
        sample.Values["dwellReleaseEntryTimestamp"] = dwellReleaseEntryTimestamp;
        sample.Values["dwellWithdrawCancelTriggered"] = dwellWithdrawCancelTriggered;
        sample.Values["dwellWithdrawReentryRequired"] = dwellWithdrawReentryRequired;
        sample.Values["dwellWithdrawInside"] = dwellWithdrawInside;
        sample.Values["dwellWithdrawEntryTimestamp"] = dwellWithdrawEntryTimestamp;
        sample.Values["pinchReleaseTriggerArmed"] = pinchReleaseTriggerArmed;
        sample.Values["pinchReleaseTriggerCompleted"] = pinchReleaseTriggerCompleted;
        sample.Values["pinchReleaseTriggerEntryTimestamp"] = pinchReleaseTriggerEntryTimestamp;
        sample.Values["pinchThumbSwipeTracking"] = pinchThumbSwipeTracking;
        sample.Values["pinchThumbSwipeCompleted"] = pinchThumbSwipeCompleted;
        sample.Values["pinchThumbSwipeCanceled"] = pinchThumbSwipeCanceled;
        sample.Values["pinchThumbSwipeReentryRequired"] = pinchThumbSwipeReentryRequired;
        sample.Values["pinchThumbSwipeEntryTimestamp"] = pinchThumbSwipeEntryTimestamp;
        sample.Values["pinchThumbSwipeStartProjection"] = pinchThumbSwipeStartProjection;
        sample.Values["pinchThumbSwipeCurrentContactDistance"] = pinchThumbSwipeCurrentContactDistance;
        sample.Values["pinchWristTiltInside"] = pinchWristTiltInside;
        sample.Values["pinchWristTiltCompleted"] = pinchWristTiltCompleted;
        sample.Values["pinchWristTiltCanceled"] = pinchWristTiltCanceled;
        sample.Values["pinchWristTiltReentryRequired"] = pinchWristTiltReentryRequired;
        sample.Values["pinchWristTiltPinching"] = pinchWristTiltPinching;
        sample.Values["pinchWristTiltEntryTimestamp"] = pinchWristTiltEntryTimestamp;
        sample.Values["pinchWristTiltHasEntryRotation"] = pinchWristTiltHasEntryRotation;
        sample.Values["directionCancelRule"] = "four_consecutive_angles_min_speed_v2";
        sample.Values["directionCurrentUnit"] = float.IsNaN(directionAngleTracker.Angle) ? null : Study3AttributionCapture.Vector(directionAngleTracker.Direction);
        sample.Values["pinchDirectionCurrentUnit"] = float.IsNaN(pinchDirectionAngleTracker.Angle) ? null : Study3AttributionCapture.Vector(pinchDirectionAngleTracker.Direction);
        sample.Values["directionSpeedGateEnabled"] = true;
        sample.Values["directionCancelMinSpeed"] = Mathf.Max(0f, ballSpawner.directionCancelMinSpeed);
        sample.Values["directionConsecutiveMatches"] = directionAngleTracker.ConsecutiveMatches;
        sample.Values["directionConsecutiveValidFrames"] = directionAngleTracker.ConsecutiveValidFrames;
        sample.Values["pinchDirectionConsecutiveMatches"] = pinchDirectionAngleTracker.ConsecutiveMatches;
        sample.Values["pinchDirectionConsecutiveValidFrames"] = pinchDirectionAngleTracker.ConsecutiveValidFrames;
        sample.Values["direction010CancelTriggered"] = direction010CancelTriggered;
        sample.Values["direction010ReentryRequired"] = direction010ReentryRequired;
        sample.Values["direction010HasSmoothedDirection"] = null;
        sample.Values["direction010FramesSinceEntry"] = direction010FramesSinceEntry;
        sample.Values["direction010HasEntryPosition"] = null;
        sample.Values["direction010SpeedMinMet"] = null;
        sample.Values["direction010SpeedMinFrame"] = null;
        sample.Values["directionCancelLowSpeedMet"] = null;
        sample.Values["directionCancelEntryAxisCaptured"] = directionCancelEntryAxisCaptured;
        sample.Values["direction010HasAcceptedPosition"] = null;
        sample.Values["swing010CancelTriggered"] = swing010CancelTriggered;
        sample.Values["swing010HasEntryDirection"] = swing010HasEntryDirection;
        sample.Values["pinchDirectionCancelTriggered"] = pinchDirectionCancelTriggered;
        sample.Values["pinchDirectionReentryRequired"] = pinchDirectionReentryRequired;
        sample.Values["pinchDirectionHasSmoothedDirection"] = null;
        sample.Values["pinchDirectionFramesSinceEntry"] = pinchDirectionFramesSinceEntry;
        sample.Values["pinchDirectionSpeedMinMet"] = null;
        sample.Values["pinchDirectionSpeedMinFrame"] = null;
        sample.Values["pinchDirectionHasRawPosition"] = null;
        sample.Values["pinchDirectionHasAcceptedPosition"] = null;
        sample.Values["rainbowEntryCaptured"] = rainbowEntryCaptured;
        sample.Values["rainbowFrameHalfExtent"] = rainbowFrameHalfExtent;
        sample.Values["rainbowArcRadius"] = rainbowArcRadius;
        sample.Values["rainbowArcThickness"] = rainbowArcThickness;
        sample.Values["rainbowArcHalfAngle"] = rainbowArcHalfAngle;
        sample.Values["rainbowConfirmBlocked"] = rainbowConfirmBlocked;
        sample.Values["rainbowReentryRequiresExit"] = rainbowReentryRequiresExit;
        sample.Values["rainbowFrameDepth"] = rainbowFrameDepth;
        sample.Values["handInBall"] = handInBall;
        sample.Values["inBall"] = inBall;
        sample.Values["inBall2"] = inBall2;
        sample.Values["canceled"] = canceled;
        sample.Values["speedMinThreshold"] = ballSpawner.speedMinThreshold;
        sample.Values["speedMaxThreshold"] = ballSpawner.speedMaxThreshold;
        sample.Values["longPressTimeThreshold"] = ballSpawner.longPressTimeThreshold;
        sample.Values["pinchDwellCancelTime"] = ballSpawner.pinchDwellCancelTime;
        sample.Values["pinchDirectionThreshold"] = ballSpawner.useEntryDirectionAxisForDirectionCancel ? ballSpawner.entryDirectionConeHalfAngle : ballSpawner.pinchDirectionCDAngleThreshold;
        sample.Values["pinchDirectionMinSpeed"] = Mathf.Max(0f, ballSpawner.directionCancelMinSpeed);
        sample.Values["pinchDirectionWindow"] = Study3DirectionConsecutiveTracker.RequiredFrames;
        sample.Values["directionThreshold"] = GetDirection010AngleThreshold();
        sample.Values["directionMinSpeed"] = Mathf.Max(0f, ballSpawner.directionCancelMinSpeed);
        sample.Values["directionWindow"] = Study3DirectionConsecutiveTracker.RequiredFrames;
        sample.Values["thumbContactDistance"] = pinchThumbSwipeCurrentContactDistance == float.MaxValue ? null : (object)pinchThumbSwipeCurrentContactDistance;
        sample.Values["thumbContactThreshold"] = ballSpawner.pinchThumbSwipeContactDistanceThreshold;
        sample.Values["wristTiltThreshold"] = ballSpawner.pinchWristTiltCancelAngleThreshold;
        sample.Values["releaseDistanceDeltaThreshold"] = ballSpawner.releaseDistanceDeltaThreshold;
        sample.Values["pinchThumbSwipeStartThumbPosition"] = Study3AttributionCapture.Vector(pinchThumbSwipeStartThumbPosition);
        sample.Values["pinchThumbSwipeLastThumbPosition"] = Study3AttributionCapture.Vector(pinchThumbSwipeLastThumbPosition);
        sample.Values["dwellWithdrawEntryVelocityDirection"] = Study3AttributionCapture.Vector(dwellWithdrawEntryVelocityDirection);
        sample.Values["dwellWithdrawCurrentVelocityDirection"] = Study3AttributionCapture.Vector(dwellWithdrawCurrentVelocityDirection);
        sample.Values["direction010SmoothedDirection"] = null;
        sample.Values["direction010EntryPosition"] = null;
        sample.Values["directionCancelEntryAxis"] = Study3AttributionCapture.Vector(directionCancelEntryAxis);
        sample.Values["direction010LastAcceptedPosition"] = null;
        sample.Values["swing010EntryDirection"] = Study3AttributionCapture.Vector(swing010EntryDirection);
        sample.Values["pinchDirectionSmoothedDirection"] = null;
        sample.Values["pinchDirectionPreviousRawPosition"] = null;
        sample.Values["pinchDirectionLastAcceptedPosition"] = null;
        sample.Values["rainbowEntryPosition"] = Study3AttributionCapture.Vector(rainbowEntryPosition);
        sample.Values["rainbowArcCenter"] = Study3AttributionCapture.Vector(rainbowArcCenter);
        sample.Values["rainbowArcNormal"] = Study3AttributionCapture.Vector(rainbowArcNormal);
        sample.Values["rainbowPrevFingerPosition"] = Study3AttributionCapture.Vector(rainbowPrevFingerPosition);
        sample.Values["rainbowFrameTangent"] = Study3AttributionCapture.Vector(rainbowFrameTangent);
        sample.Values["rainbowFrameBitangent"] = Study3AttributionCapture.Vector(rainbowFrameBitangent);
        sample.Values["pinchWristTiltEntryRotation"] = new object[] { pinchWristTiltEntryRotation.x, pinchWristTiltEntryRotation.y, pinchWristTiltEntryRotation.z, pinchWristTiltEntryRotation.w };
        sample.Values["wristAngle"] = attributionWristAngle;
        sample.Values["wristPoseValid"] = attributionWristPoseValid;
        return sample;
    }
    private void AttributionCancelBranch(string branch) => attributionBridge?.CancelBranch(branch, true);
    private void AttributionCancelReason(string reason) => attributionBridge?.CancelReason(reason);
    private void AttributionMotionUnavailable(string reason)
    {
        if (logWriter3 != null && logWriter3.HasOpenAttribution(GetInstanceID()))
        {
            attributionBridge?.Custom(false, false, "motion_unavailable", new Dictionary<string, object> { { "reason", reason } });
            attributionBridge?.Finish("aborted", reason);
        }
    }
    private float attributionWristAngle = float.NaN;
    private bool attributionWristPoseValid;
    private void AttributionWristDecision(float angle, float threshold, bool valid)
    {
        attributionWristAngle = angle; attributionWristPoseValid = valid;
        attributionBridge?.Decision("wrist_check", new Dictionary<string, object> {
            { "angle", angle }, { "threshold", threshold }, { "poseValid", valid }, { "passed", valid && angle >= threshold } });
    }
    private void AttributionTick() => attributionBridge?.Tick();
    private void LateUpdate() => attributionBridge?.LateTick();
    private void AttributionDynamic(bool inside, bool selected)
    {
        attributionBridge?.Custom(inside, true, "dynamic_stop_check", new Dictionary<string, object> {
            { "sampleAccepted", dynamicStopMotion.AttributionSampleAccepted },
            { "measurementSequence", dynamicStopMotion.AttributionMeasurementSequence },
            { "measurementTime", dynamicStopMotion.AttributionMeasurementTime },
            { "measurementDt", dynamicStopMotion.AttributionMeasurementDt }, { "recentSpeed", dynamicStopMotion.RecentSpeed },
            { "speed", dynamicStopMotion.Speed }, { "acceleration", dynamicStopMotion.Acceleration },
            { "speedThreshold", dynamicStopMotion.SpeedLimit }, { "accelerationThreshold", dynamicStopMotion.AccelLimit },
            { "hasAcceleration", dynamicStopMotion.HasAcceleration }, { "speedPass", dynamicStopMotion.SpeedConditionMet },
            { "accelerationPass", dynamicStopMotion.AccelerationConditionMet }, { "selected", selected }, { "comparison", "<=" } });
    }
    private void AttributionBeforeComplete()
    {
        string action = isMineBall && logWriter3.successfully_Canceled ? "cancel" : "trigger";
        attributionBridge?.Finish(action, "legacy_complete_selection");
    }
}

using System.Collections.Generic;
using UnityEngine;

public partial class S2Obstacles
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
            pinchob, releaseGesture, modeSelected, ballIndex, false, "obstacle");
        sample.Values["pinchHandledThisGesture"] = pinchHandledThisGesture;
        sample.Values["fastTapCancelTriggered"] = fastTapCancelTriggered;
        sample.Values["fastTapReentryRequired"] = fastTapReentryRequired;
        sample.Values["fastTapCanceledUntilExit"] = fastTapCanceledUntilExit;
        sample.Values["fastTapEntryTimestamp"] = fastTapEntryTimestamp;
        sample.Values["fastTapReactTimestamp"] = fastTapReactTimestamp;
        sample.Values["reactReleaseSpeedAcceMet"] = reactReleaseSpeedAcceMet;
        sample.Values["reactReleaseTriggered"] = reactReleaseTriggered;
        sample.Values["reactReleaseReentryRequired"] = reactReleaseReentryRequired;
        sample.Values["speedReleaseMinSpeedMet"] = speedReleaseMinSpeedMet;
        sample.Values["speedReleaseTriggered"] = speedReleaseTriggered;
        sample.Values["speedReleaseReentryRequired"] = speedReleaseReentryRequired;
        sample.Values["fastTapCancelTime"] = fastTapCancelTime;
        sample.Values["rainbowCancelTriggered"] = rainbowCancelTriggered;
        sample.Values["rainbowConfirmBlocked"] = rainbowConfirmBlocked;
        sample.Values["rainbowReentryRequiresExit"] = rainbowReentryRequiresExit;
        sample.Values["rainbowArcActive"] = rainbowArcActive;
        sample.Values["rainbowArcInitialized"] = rainbowArcInitialized;
        sample.Values["rainbowEntryCaptured"] = rainbowEntryCaptured;
        sample.Values["rainbowFrameHalfExtent"] = rainbowFrameHalfExtent;
        sample.Values["rainbowArcRadius"] = rainbowArcRadius;
        sample.Values["rainbowArcThickness"] = rainbowArcThickness;
        sample.Values["rainbowFrameDepth"] = rainbowFrameDepth;
        sample.Values["rainbowArcHalfAngle"] = rainbowArcHalfAngle;
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
        sample.Values["pinchThumbSwipeTracking"] = pinchThumbSwipeTracking;
        sample.Values["pinchThumbSwipeCanceled"] = pinchThumbSwipeCanceled;
        sample.Values["pinchThumbSwipeReentryRequired"] = pinchThumbSwipeReentryRequired;
        sample.Values["pinchThumbSwipeStartProjection"] = pinchThumbSwipeStartProjection;
        sample.Values["pinchThumbSwipeCurrentContactDistance"] = pinchThumbSwipeCurrentContactDistance;
        sample.Values["pinchWristTiltInside"] = pinchWristTiltInside;
        sample.Values["pinchWristTiltTriggered"] = pinchWristTiltTriggered;
        sample.Values["pinchWristTiltCanceled"] = pinchWristTiltCanceled;
        sample.Values["pinchWristTiltReentryRequired"] = pinchWristTiltReentryRequired;
        sample.Values["pinchWristTiltPinching"] = pinchWristTiltPinching;
        sample.Values["pinchWristTiltEntryTimestamp"] = pinchWristTiltEntryTimestamp;
        sample.Values["pinchWristTiltHasEntryRotation"] = pinchWristTiltHasEntryRotation;
        sample.Values["direction010CancelTriggered"] = direction010CancelTriggered;
        sample.Values["direction010ReentryRequired"] = direction010ReentryRequired;
        sample.Values["direction010HasSmoothedDirection"] = direction010HasSmoothedDirection;
        sample.Values["swing010CancelTriggered"] = swing010CancelTriggered;
        sample.Values["swing010HasEntryDirection"] = swing010HasEntryDirection;
        sample.Values["pinchDirectionCancelTriggered"] = pinchDirectionCancelTriggered;
        sample.Values["pinchDirectionReentryRequired"] = pinchDirectionReentryRequired;
        sample.Values["pinchDirectionHasSmoothedDirection"] = pinchDirectionHasSmoothedDirection;
        sample.Values["directionCancelLowSpeedMet"] = directionCancelLowSpeedMet;
        sample.Values["directionCancelEntryAxisCaptured"] = directionCancelEntryAxisCaptured;
        sample.Values["direction010HasAcceptedPosition"] = direction010HasAcceptedPosition;
        sample.Values["pinchDirectionHasRawPosition"] = pinchDirectionHasRawPosition;
        sample.Values["pinchDirectionHasAcceptedPosition"] = pinchDirectionHasAcceptedPosition;
        sample.Values["handInBall"] = handInBall;
        sample.Values["inBall"] = inBall;
        sample.Values["inBall2"] = inBall2;
        sample.Values["canceled"] = canceled;
        sample.Values["speedMinThreshold"] = ballSpawner.speedMinThreshold;
        sample.Values["speedMaxThreshold"] = ballSpawner.speedMaxThreshold;
        sample.Values["longPressTimeThreshold"] = ballSpawner.longPressTimeThreshold;
        sample.Values["pinchDwellCancelTime"] = ballSpawner.pinchDwellCancelTime;
        sample.Values["pinchDirectionThreshold"] = ballSpawner.useEntryDirectionAxisForDirectionCancel ? ballSpawner.entryDirectionConeHalfAngle : ballSpawner.pinchDirectionCDAngleThreshold;
        sample.Values["pinchDirectionMinSpeed"] = ballSpawner.pinchDirectionMinSpeedThreshold;
        sample.Values["pinchDirectionWindow"] = ballSpawner.pinchDirectionWindowFrameCount;
        sample.Values["directionThreshold"] = ballSpawner.useEntryDirectionAxisForDirectionCancel ? ballSpawner.entryDirectionConeHalfAngle : ballSpawner.direction010CDAngleThreshold;
        sample.Values["directionMinSpeed"] = ballSpawner.direction010MinSpeedThreshold;
        sample.Values["directionWindow"] = ballSpawner.direction010WindowFrameCount;
        sample.Values["thumbContactDistance"] = pinchThumbSwipeCurrentContactDistance == float.MaxValue ? null : (object)pinchThumbSwipeCurrentContactDistance;
        sample.Values["thumbContactThreshold"] = ballSpawner.pinchThumbSwipeContactDistanceThreshold;
        sample.Values["wristTiltThreshold"] = ballSpawner.pinchWristTiltCancelAngleThreshold;
        sample.Values["releaseDistanceDeltaThreshold"] = ballSpawner.releaseDistanceDeltaThreshold;
        sample.Values["rainbowEntryPosition"] = Study3AttributionCapture.Vector(rainbowEntryPosition);
        sample.Values["rainbowArcCenter"] = Study3AttributionCapture.Vector(rainbowArcCenter);
        sample.Values["rainbowArcNormal"] = Study3AttributionCapture.Vector(rainbowArcNormal);
        sample.Values["rainbowPrevFingerPosition"] = Study3AttributionCapture.Vector(rainbowPrevFingerPosition);
        sample.Values["rainbowFrameTangent"] = Study3AttributionCapture.Vector(rainbowFrameTangent);
        sample.Values["rainbowFrameBitangent"] = Study3AttributionCapture.Vector(rainbowFrameBitangent);
        sample.Values["pinchThumbSwipeStartThumbPosition"] = Study3AttributionCapture.Vector(pinchThumbSwipeStartThumbPosition);
        sample.Values["dwellWithdrawEntryVelocityDirection"] = Study3AttributionCapture.Vector(dwellWithdrawEntryVelocityDirection);
        sample.Values["dwellWithdrawCurrentVelocityDirection"] = Study3AttributionCapture.Vector(dwellWithdrawCurrentVelocityDirection);
        sample.Values["direction010SmoothedDirection"] = Study3AttributionCapture.Vector(direction010SmoothedDirection);
        sample.Values["swing010EntryDirection"] = Study3AttributionCapture.Vector(swing010EntryDirection);
        sample.Values["pinchDirectionSmoothedDirection"] = Study3AttributionCapture.Vector(pinchDirectionSmoothedDirection);
        sample.Values["directionCancelEntryAxis"] = Study3AttributionCapture.Vector(directionCancelEntryAxis);
        sample.Values["direction010LastAcceptedPosition"] = Study3AttributionCapture.Vector(direction010LastAcceptedPosition);
        sample.Values["pinchDirectionPreviousRawPosition"] = Study3AttributionCapture.Vector(pinchDirectionPreviousRawPosition);
        sample.Values["pinchDirectionLastAcceptedPosition"] = Study3AttributionCapture.Vector(pinchDirectionLastAcceptedPosition);
        sample.Values["pinchWristTiltEntryRotation"] = new object[] { pinchWristTiltEntryRotation.x, pinchWristTiltEntryRotation.y, pinchWristTiltEntryRotation.z, pinchWristTiltEntryRotation.w };
        sample.Values["wristAngle"] = attributionWristAngle;
        sample.Values["wristPoseValid"] = attributionWristPoseValid;
        return sample;
    }
    private void AttributionCancelBranch(string branch) => attributionBridge?.CancelBranch(branch, false);
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
}

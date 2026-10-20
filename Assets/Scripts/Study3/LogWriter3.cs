using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LogWriter3 : MonoBehaviour
{
    public BallSpawner ballSpawner;
    private string date, filePath, fileName, fileFullPath, directionDebugFileName, directionDebugFileFullPath;
    private string pinchReleaseDistanceFileName, pinchReleaseDistanceFileFullPath;
    private GameObject[] Targets, Obstacles;
    private FileStream fs;
    private StreamWriter sw;
    private bool started = false;
    public GameObject temp;
    private AcceStimulate acceStimulate;
    public float triggerTime = 0f;
    public float reactTime = 0f;
    public int reactFrequency = 0;
    public int obstacleReactFrequency = 0;
    private float _cancelTime = 0f;
    private int _cancelTimes = 0;
    private bool _handInBall = false;
    private float lastBallEntryTimestamp = -1f;
    private string directionAngleFrameLog = "NA";
    private string directionSpeedMinLog = "met=0;frame=NA";
    public float cancelTime
    {
        get => _cancelTime;
        set
        {
            if (_cancelTimes == 0)
            {
                _cancelTime = value;
            }
        }
    }
    public int cancelTimes
    {
        get => _cancelTimes;
        set
        {
            if (value > _cancelTimes)
            {
                SetCancelTimeFromLastEntry();
            }

            _cancelTimes = value;
            if (_cancelTimes == 0)
            {
                _cancelTime = 0f;
            }
        }
    }
    public bool successfully_Canceled = false;
    public bool isReact = false;
    public bool handInBall
    {
        get => _handInBall;
        set
        {
            if (value && !_handInBall)
            {
                RecordPendingNoTriggerExitIfNeeded();
                MarkBallEntry();
            }
            else if (!value && _handInBall)
            {
                MarkPotentialNoTriggerExit();
            }

            _handInBall = value;
        }
    }
    public bool isMineBall = false;
    private string cancelReasonLog = "";
    private string noTriggerReasonLog = "";
    private int noTriggerEntrySequence = 0;
    private bool noTriggerReasonRecordedForCurrentEntry = false;
    private bool currentEntryTriggered = false;
    private bool pendingNoTriggerExit = false;
    private int pendingNoTriggerExitEntrySequence = 0;
    private string pendingNoTriggerReason = "";
    private S2AccMode activeSelectionSource;
    [SerializeField] private bool stateOnly;

    public bool IsStateOnly => stateOnly;

    public void InitializeStateOnly(BallSpawner host)
    {
        stateOnly = true;
        ballSpawner = host;
        started = false;
        filePath = null;
        fileFullPath = null;
        directionDebugFileFullPath = null;
        pinchReleaseDistanceFileFullPath = null;
    }

    void Start()
    {
        if (ballSpawner == null)
        {
            ballSpawner = GetComponent<BallSpawner>();
        }

        if (stateOnly)
        {
            return;
        }

        date = DateTime.Now.ToString("yyyyMMdd-HH-mm-ss");
        filePath = Application.persistentDataPath + "/Logs/" + date + "/";
        fileName = date + "-" + "time" + ".csv";
        fileFullPath = filePath + fileName;
        directionDebugFileName = date + "-" + "direction-debug" + ".csv";
        directionDebugFileFullPath = filePath + directionDebugFileName;
        pinchReleaseDistanceFileName = date + "-" + "pinch-release-distance" + ".csv";
        pinchReleaseDistanceFileFullPath = filePath + pinchReleaseDistanceFileName;
        Debug.Log(fileFullPath);
        //acce = gameObject.GetComponent<AcceStimulate>();
        InitialPath();
        temp = GameObject.Find("temp");
        acceStimulate = temp.GetComponent<AcceStimulate>();
        // Start writing once entering, no need to set invoked as true because we want it record every frame inside
        //acce.InEvent.AddListener(Write);
        // Increment the case time after getting the finger out
        //acce.AfterOutEvent.AddListener(increCase);
        Targets = GameObject.FindGameObjectsWithTag("Target");
        Obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var target in Targets)
        {
            target.GetComponent<AcceStimulate>();
        }
        ballSpawner = GetComponent<BallSpawner>();
        ballSpawner.ExpStartEvent.AddListener(() =>
        {
            started = true;
        });
        ballSpawner.ExpEndEvent.AddListener(() =>
        {
            started = false;
        });
        ballSpawner.RoundOverEvent.AddListener(Write);
    }
    void FixedUpdate()
    {
        if (handInBall || isReact)
        {
            triggerTime += Time.deltaTime;
        }
        // if (isReact)
        // {
        //     reactTime += Time.deltaTime;
        // }
    }
    public string FormatMsg()
    {
        FinalizeNoTriggerReasonsForRound();
        string ans = ballSpawner.currentCaseNumber + "," + ballSpawner.currentRoundID + "," + ballSpawner.intargetTimes
        + "," + ballSpawner.enable_preview + "," + ballSpawner.enableMineBall + "," + ballSpawner.enabledOb
        + "," + ballSpawner.errorTimes + "," + ballSpawner.errorInTimes + "," + ballSpawner.usedTime + "," + ballSpawner.triggerCondition + "," + ballSpawner.initialBall + "," + ballSpawner.rotationDirection + "," + ballSpawner.modeSelected + "," + ballSpawner.TargetEnd;
        ans += "," + reactTime;
        ans += "," + reactFrequency;
        ans += "," + obstacleReactFrequency;
        ans += "," + triggerTime;
        if (cancelTimes > 0)
        {
            ans += "," + cancelTime;
        }
        else if (isMineBall)
        {
            ans += "," + (triggerTime - reactTime);
        }
        else
        {
            ans += "," + -1;
        }
        ans += "," + isMineBall;
        if (isMineBall)
        {
            ans += "," + successfully_Canceled;
        }
        else
        {
            ans += "," + "NA";
        }
        ans += "," + EscapeCsv(GetCancelReasons());
        ans += "," + EscapeCsv(GetNoTriggerReasons());
        ans += "," + cancelTimes;
        ans += "," + EscapeCsv(directionAngleFrameLog);
        ans += "," + EscapeCsv(directionSpeedMinLog);
        ans += "," + ballSpawner.largeCirclePoint.x;
        ans += "," + ballSpawner.largeCirclePoint.y;
        ans += "," + ballSpawner.largeCirclePoint.z;
        foreach (var temp in ballSpawner.tatgetPos)
        {
            ans += "," + temp.x;
            ans += "," + temp.y;
            ans += "," + temp.z;
        }
        return ans;
    }
    public void InitialPath()
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }
        if (!File.Exists(fileFullPath))
        {
            // Close the operator after creation
            fs = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            sw.WriteLine("Case,ID,inTargetTimes,enable_preview,enableMineBall,enabledOb,errorTimes,errorInTimes,usedTime,triggerCondition,start,direction(0 for ),modeSelected,ballIndex,reactTime,react_frequency,obstacle_react_frequency,triggerTime,cancelTime,isMineBall(need cancel),successfully_Canceled,cancelReason,no trigger reason,cancelTimes,directionAngleFrame,directionSpeedMin,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ");
            sw.Close();
            fs.Close();
        }
        if (!File.Exists(directionDebugFileFullPath))
        {
            fs = new FileStream(directionDebugFileFullPath, FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            sw.WriteLine("frame,time,case,id,mode,source,ballIndex,ballKind,failReason,finalCancelPass,handInBall,directionReentryRequired,cancelAlreadyTriggered,sampleCount,windowFrameCount,windowReady,windowMovementX,windowMovementY,windowMovementZ,windowMovementMagnitude,smoothedDirX,smoothedDirY,smoothedDirZ,hasSmoothedDirection,userFacingDirX,userFacingDirY,userFacingDirZ,angleDeg,angleThresholdDeg,anglePass,indexSpeed,minSpeedThreshold,speedPass,ballCenterX,ballCenterY,ballCenterZ,indexPosX,indexPosY,indexPosZ,viewerPosX,viewerPosY,viewerPosZ");
            sw.Close();
            fs.Close();
        }
        if (!File.Exists(pinchReleaseDistanceFileFullPath))
        {
            fs = new FileStream(pinchReleaseDistanceFileFullPath, FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            sw.WriteLine("frame,time,case,id,mode,event,reason,ballIndex,ballKind,isMineBall,pending,isPinch,indexInBall,thumbInBall,triggeredBy,entryElapsed,poseValid,activateThresholdMeters,deactivateThresholdMeters,distanceMeters,distanceMillimeters,swipeContactValid,swipeContactDistanceMeters,swipeContactDistanceMillimeters,swipeContactThresholdMeters,indexX,indexY,indexZ,thumbX,thumbY,thumbZ");
            sw.Close();
            fs.Close();
        }
    }
    public void Write()
    {
        // New an fs and sw, close an fs and sw after writing
        // because a writer and file stream will lock the memory storage so that other gameobject cannot write
        if (started)
        {
            fs = new FileStream(fileFullPath, FileMode.Append, FileAccess.Write);
            sw = new StreamWriter(fs);
            SetTriggerTimeFromLastEntry();
            string log = FormatMsg();
            sw.WriteLine(log);
            sw.Close();
            fs.Close();
            reactFrequency = 0;
            obstacleReactFrequency = 0;
            cancelTimes = 0;
            ClearDirectionAngleFrame();
            ClearDirectionSpeedMin();
            ClearTriggerTiming();
        }
        ClearCancelReasons();
        ClearNoTriggerReasons();

    }
    public void RecordCancelReason(string reason)
    {
        if (!string.IsNullOrEmpty(reason))
        {
            cancelReasonLog = string.IsNullOrEmpty(cancelReasonLog)
                ? reason
                : cancelReasonLog + ", " + reason;
            string noTriggerReason = GetDefaultNoTriggerReason(ballSpawner.modeSelected, reason);
            if (activeSelectionSource != null)
            {
                string stateReason = activeSelectionSource.GetCurrentNoTriggerReason();
                if (stateReason == noTriggerReason
                    || stateReason.StartsWith(noTriggerReason + ";", StringComparison.Ordinal))
                {
                    noTriggerReason = stateReason;
                }
            }
            RecordNoTriggerReasonForCurrentEntry(noTriggerReason);
        }
    }

    public void RecordNoTriggerReason(string reason)
    {
        if (!string.IsNullOrEmpty(reason))
        {
            RecordNoTriggerReasonForCurrentEntry(reason);
        }
    }

    public void SetActiveSelectionSource(S2AccMode source)
    {
        activeSelectionSource = source;
    }

    public void MarkBallEntry()
    {
        lastBallEntryTimestamp = Time.time;
        noTriggerEntrySequence++;
        noTriggerReasonRecordedForCurrentEntry = false;
        currentEntryTriggered = false;
    }

    public void MarkRoundCompleted()
    {
        if (noTriggerEntrySequence > 0 && !noTriggerReasonRecordedForCurrentEntry)
        {
            currentEntryTriggered = true;
            ClearPendingNoTriggerExit();
        }
    }

    public void SetCancelTimeFromLastEntry()
    {
        _cancelTime = lastBallEntryTimestamp >= 0f
            ? Mathf.Max(0f, Time.time - lastBallEntryTimestamp)
            : 0f;
    }

    public void SetTriggerTimeFromLastEntry()
    {
        triggerTime = lastBallEntryTimestamp >= 0f
            ? Mathf.Max(0f, Time.time - lastBallEntryTimestamp)
            : 0f;
    }

    public void RecordDirectionAngleFrame(float angle, int frameFromEntry)
    {
        directionAngleFrameLog = "angle="
            + angle.ToString("F2", CultureInfo.InvariantCulture)
            + ";frame="
            + Mathf.Max(0, frameFromEntry);
    }

    public void RecordDirectionSpeedMinFrame(bool met, int frameFromEntry)
    {
        directionSpeedMinLog = met
            ? "met=1;frame=" + Mathf.Max(0, frameFromEntry)
            : "met=0;frame=NA";
    }

    public void WriteDirectionDebug(
        string source,
        string failReason,
        bool finalCancelPass,
        int modeSelected,
        int ballIndex,
        int ballKind,
        bool handInBall,
        bool directionReentryRequired,
        bool cancelAlreadyTriggered,
        int sampleCount,
        int windowFrameCount,
        bool windowReady,
        Vector3 windowMovement,
        Vector3 smoothedDirection,
        bool hasSmoothedDirection,
        Vector3 userFacingDirection,
        float angleDeg,
        float angleThresholdDeg,
        bool anglePass,
        float indexSpeed,
        float minSpeedThreshold,
        bool speedPass,
        Vector3 ballCenter,
        Vector3 indexPosition,
        Vector3 viewerPosition)
    {
        if (string.IsNullOrEmpty(directionDebugFileFullPath))
        {
            return;
        }

        fs = new FileStream(directionDebugFileFullPath, FileMode.Append, FileAccess.Write);
        sw = new StreamWriter(fs);
        sw.WriteLine(FormatDirectionDebug(
            source,
            failReason,
            finalCancelPass,
            modeSelected,
            ballIndex,
            ballKind,
            handInBall,
            directionReentryRequired,
            cancelAlreadyTriggered,
            sampleCount,
            windowFrameCount,
            windowReady,
            windowMovement,
            smoothedDirection,
            hasSmoothedDirection,
            userFacingDirection,
            angleDeg,
            angleThresholdDeg,
            anglePass,
            indexSpeed,
            minSpeedThreshold,
            speedPass,
            ballCenter,
            indexPosition,
            viewerPosition));
        sw.Close();
        fs.Close();
    }

    public void WritePinchReleaseDistance(
        string eventType,
        string reason,
        int modeSelected,
        int ballIndex,
        int ballKind,
        bool isMineBall,
        bool pinchArmed,
        bool isPinch,
        bool indexInBall,
        bool thumbInBall,
        string triggeredBy,
        float entryElapsed,
        bool poseValid,
        float activateThresholdMeters,
        float deactivateThresholdMeters,
        float distanceMeters,
        bool swipeContactValid,
        float swipeContactDistanceMeters,
        float swipeContactThresholdMeters,
        Vector3 indexPosition,
        Vector3 thumbPosition)
    {
        if (string.IsNullOrEmpty(pinchReleaseDistanceFileFullPath))
        {
            return;
        }

        fs = new FileStream(pinchReleaseDistanceFileFullPath, FileMode.Append, FileAccess.Write);
        sw = new StreamWriter(fs);
        sw.WriteLine(FormatPinchReleaseDistance(
            eventType,
            reason,
            modeSelected,
            ballIndex,
            ballKind,
            isMineBall,
            pinchArmed,
            isPinch,
            indexInBall,
            thumbInBall,
            triggeredBy,
            entryElapsed,
            poseValid,
            activateThresholdMeters,
            deactivateThresholdMeters,
            distanceMeters,
            swipeContactValid,
            swipeContactDistanceMeters,
            swipeContactThresholdMeters,
            indexPosition,
            thumbPosition));
        sw.Close();
        fs.Close();
    }

    private void ClearTriggerTiming()
    {
        triggerTime = 0f;
        lastBallEntryTimestamp = -1f;
        _handInBall = false;
    }

    private string GetCancelReasons()
    {
        return !string.IsNullOrEmpty(cancelReasonLog) ? cancelReasonLog : "NA";
    }

    private string GetNoTriggerReasons()
    {
        if (!string.IsNullOrEmpty(noTriggerReasonLog))
        {
            return noTriggerReasonLog;
        }

        if (!IsNoTriggerRound())
        {
            return "NA";
        }

        return FormatNoTriggerReasonForCurrentEntry(GetDefaultNoTriggerReason(ballSpawner.modeSelected, GetCancelReasons()));
    }

    private bool IsNoTriggerRound()
    {
        return cancelTimes > 0
            || (isMineBall && successfully_Canceled)
            || !string.IsNullOrEmpty(cancelReasonLog);
    }

    private string GetDefaultNoTriggerReason(BallSpawner.Mode mode, string cancelReasons)
    {
        switch (mode)
        {
            case BallSpawner.Mode.Tap:
                return "tap_entry_not_accepted";
            case BallSpawner.Mode.dwell:
                return "dwell_time_not_reached_before_exit";
            case BallSpawner.Mode.time010:
                return "dwell_cancel_timeout_reached";
            case BallSpawner.Mode.magicTap:
                return "acceleration_threshold_not_reached_before_exit";
            case BallSpawner.Mode.dynamicStop:
                return "dynamic_stop_threshold_not_reached_before_exit";
            case BallSpawner.Mode.reactTap:
                return "react_cancel_condition_detected";
            case BallSpawner.Mode.reactRelease:
                return HasCancelReason(cancelReasons, "before react")
                    ? "react_precondition_not_reached_before_exit"
                    : "release_cancel_before_speed_trigger";
            case BallSpawner.Mode.safeGate:
                return "safe_gate_cancel_region_crossed";
            case BallSpawner.Mode.release010:
                return "release_detected_before_exit_confirmation";
            case BallSpawner.Mode.press:
                return "press_speed_threshold_not_reached_before_exit";
            case BallSpawner.Mode.pinch:
                return "pinch_not_started_before_exit";
            case BallSpawner.Mode.pinchReleaseCancel:
                return HasCancelReason(cancelReasons, "release")
                    ? "opening_release_detected_before_pinch"
                    : "pinch_contact_lost_before_selection";
            case BallSpawner.Mode.pinchDwell:
                return "pinch_dwell_timeout_before_release";
            case BallSpawner.Mode.pinchReleaseTrigger:
                return "contact_exited_while_pinching";
            case BallSpawner.Mode.pinchDirection:
                return "cancel_direction_detected";
            case BallSpawner.Mode.pinchThumbSwipe:
                return "thumb_contact_cancel_detected";
            case BallSpawner.Mode.pinchWristTilt:
                return "wrist_tilt_cancel_threshold_exceeded";
            case BallSpawner.Mode.dwellWristTilt:
                return "wrist_tilt_cancel_threshold_exceeded";
            case BallSpawner.Mode.speedWristTilt:
                return "wrist_tilt_cancel_threshold_exceeded";
            case BallSpawner.Mode.wristTilt010:
                return "wrist_tilt_cancel_threshold_exceeded";
            case BallSpawner.Mode.dwellRelease:
                return "release_detected_before_dwell_complete";
            case BallSpawner.Mode.dwellDirection:
                return "cancel_direction_detected";
            case BallSpawner.Mode.dwellWithdraw:
                return "withdraw_cancel_movement_detected";
            case BallSpawner.Mode.speedRelease:
                return HasCancelReason(cancelReasons, "before speed-min")
                    ? "minimum_speed_not_reached_before_exit"
                    : "release_detected_before_maximum_speed";
            case BallSpawner.Mode.speed:
                return HasCancelReason(cancelReasons, "before speed-min")
                    ? "minimum_speed_not_reached_before_exit"
                    : "maximum_speed_not_reached_before_exit";
            case BallSpawner.Mode.speedDwell:
                return "maximum_speed_not_reached_before_timeout";
            case BallSpawner.Mode.speedDirection:
                return HasCancelReason(cancelReasons, "direction")
                    ? "cancel_direction_detected"
                    : "maximum_speed_not_reached_before_exit";
            case BallSpawner.Mode.contact010:
                return "exit_outside_contact_trigger_region";
            case BallSpawner.Mode.direction010:
            case BallSpawner.Mode.direction010Test:
                return "cancel_direction_detected";
            case BallSpawner.Mode.swing010:
                return "swing_cancel_angle_exceeded";
            default:
                return "attempt_interrupted_before_resolution";
        }
    }

    private bool HasCancelReason(string cancelReasons, string reason)
    {
        return cancelReasons.IndexOf(reason, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string GetFingerPassThroughNoTriggerReason(BallSpawner.Mode mode)
    {
        switch (mode)
        {
            case BallSpawner.Mode.Tap:
                return "tap_entry_not_accepted";
            case BallSpawner.Mode.dwell:
                return "dwell_time_not_reached_before_exit";
            case BallSpawner.Mode.time010:
                return "react_stage_not_reached_before_exit";
            case BallSpawner.Mode.magicTap:
                return "acceleration_threshold_not_reached_before_exit";
            case BallSpawner.Mode.dynamicStop:
                return "dynamic_stop_threshold_not_reached_before_exit";
            case BallSpawner.Mode.reactTap:
                return "react_stage_not_reached_before_exit";
            case BallSpawner.Mode.reactRelease:
                return "react_precondition_not_reached_before_exit";
            case BallSpawner.Mode.safeGate:
                return "safe_gate_confirmation_not_completed";
            case BallSpawner.Mode.release010:
                return "react_stage_not_reached_before_exit";
            case BallSpawner.Mode.press:
                return "press_speed_threshold_not_reached_before_exit";
            case BallSpawner.Mode.pinch:
                return "pinch_not_started_before_exit";
            case BallSpawner.Mode.pinchReleaseCancel:
                return "pinch_not_started_before_exit";
            case BallSpawner.Mode.pinchDwell:
                return "pinch_not_started_before_exit";
            case BallSpawner.Mode.pinchReleaseTrigger:
                return "pinch_not_started_before_exit";
            case BallSpawner.Mode.pinchDirection:
                return "pinch_not_started_before_exit";
            case BallSpawner.Mode.pinchThumbSwipe:
                return "pinch_not_started_before_exit";
            case BallSpawner.Mode.pinchWristTilt:
                return "pinch_not_started_before_exit";
            case BallSpawner.Mode.dwellWristTilt:
                return "dwell_time_not_reached_before_exit";
            case BallSpawner.Mode.speedWristTilt:
                return "minimum_speed_not_reached_before_exit";
            case BallSpawner.Mode.wristTilt010:
                return "wrist_tilt_confirmation_not_completed";
            case BallSpawner.Mode.dwellRelease:
                return "dwell_time_not_reached_before_exit";
            case BallSpawner.Mode.dwellDirection:
                return "dwell_time_not_reached_before_exit";
            case BallSpawner.Mode.dwellWithdraw:
                return "dwell_time_not_reached_before_exit";
            case BallSpawner.Mode.speedRelease:
                return "minimum_speed_not_reached_before_exit";
            case BallSpawner.Mode.speed:
                return "minimum_speed_not_reached_before_exit";
            case BallSpawner.Mode.speedDwell:
                return "maximum_speed_not_reached_before_timeout";
            case BallSpawner.Mode.speedDirection:
                return "minimum_speed_not_reached_before_exit";
            case BallSpawner.Mode.contact010:
                return "exit_outside_contact_trigger_region";
            case BallSpawner.Mode.direction010:
                return "exit_confirmation_not_completed";
            case BallSpawner.Mode.direction010Test:
                return "exit_confirmation_not_completed";
            case BallSpawner.Mode.swing010:
                return "swing_exit_confirmation_not_completed";
            default:
                return "attempt_interrupted_before_resolution";
        }
    }

    private void FinalizeNoTriggerReasonsForRound()
    {
        if (pendingNoTriggerExit)
        {
            if (!currentEntryTriggered)
            {
                RecordPendingNoTriggerExitIfNeeded();
            }
            else
            {
                // A successful technique can set handInBall=false immediately before the round is written.
                ClearPendingNoTriggerExit();
            }
        }

        if (!currentEntryTriggered
            && IsNoTriggerRound()
            && noTriggerEntrySequence > 0
            && !noTriggerReasonRecordedForCurrentEntry)
        {
            RecordNoTriggerReasonForCurrentEntry(
                GetDefaultNoTriggerReason(ballSpawner.modeSelected, GetCancelReasons()));
        }
    }

    private void RecordNoTriggerReasonForCurrentEntry(string reason)
    {
        if (noTriggerReasonRecordedForCurrentEntry)
        {
            return;
        }

        noTriggerReasonRecordedForCurrentEntry = true;
        ClearPendingNoTriggerExit();
        string formattedReason = FormatNoTriggerReason(noTriggerEntrySequence, reason);
        noTriggerReasonLog = string.IsNullOrEmpty(noTriggerReasonLog)
            ? formattedReason
            : noTriggerReasonLog + ", " + formattedReason;
    }

    private string FormatNoTriggerReasonForCurrentEntry(string reason)
    {
        return FormatNoTriggerReason(noTriggerEntrySequence, reason);
    }

    private string FormatNoTriggerReason(int entrySequence, string reason)
    {
        int entryNumber = Mathf.Max(1, entrySequence);
        return "entry " + entryNumber + ": " + reason;
    }

    private void MarkPotentialNoTriggerExit()
    {
        if (noTriggerReasonRecordedForCurrentEntry)
        {
            ClearPendingNoTriggerExit();
            return;
        }

        pendingNoTriggerExit = true;
        pendingNoTriggerExitEntrySequence = noTriggerEntrySequence;
        pendingNoTriggerReason = activeSelectionSource != null
            ? activeSelectionSource.GetCurrentNoTriggerReason()
            : "";
    }

    private void RecordPendingNoTriggerExitIfNeeded()
    {
        if (!pendingNoTriggerExit)
        {
            return;
        }

        if (pendingNoTriggerExitEntrySequence == noTriggerEntrySequence)
        {
            noTriggerReasonRecordedForCurrentEntry = true;
        }

        string formattedReason = FormatNoTriggerReason(
            pendingNoTriggerExitEntrySequence,
            !string.IsNullOrEmpty(pendingNoTriggerReason)
                ? pendingNoTriggerReason
                : GetFingerPassThroughNoTriggerReason(ballSpawner.modeSelected));
        noTriggerReasonLog = string.IsNullOrEmpty(noTriggerReasonLog)
            ? formattedReason
            : noTriggerReasonLog + ", " + formattedReason;
        ClearPendingNoTriggerExit();
    }

    private void ClearPendingNoTriggerExit()
    {
        pendingNoTriggerExit = false;
        pendingNoTriggerExitEntrySequence = 0;
        pendingNoTriggerReason = "";
    }

    private void ClearCancelReasons()
    {
        cancelReasonLog = "";
    }

    private void ClearNoTriggerReasons()
    {
        noTriggerReasonLog = "";
        noTriggerEntrySequence = 0;
        noTriggerReasonRecordedForCurrentEntry = false;
        currentEntryTriggered = false;
        activeSelectionSource = null;
        ClearPendingNoTriggerExit();
    }

    private void ClearDirectionAngleFrame()
    {
        directionAngleFrameLog = "NA";
    }

    private void ClearDirectionSpeedMin()
    {
        directionSpeedMinLog = "met=0;frame=NA";
    }

    private string EscapeCsv(string value)
    {
        if (value == null)
        {
            return "";
        }

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private string FormatDirectionDebug(
        string source,
        string failReason,
        bool finalCancelPass,
        int modeSelected,
        int ballIndex,
        int ballKind,
        bool handInBall,
        bool directionReentryRequired,
        bool cancelAlreadyTriggered,
        int sampleCount,
        int windowFrameCount,
        bool windowReady,
        Vector3 windowMovement,
        Vector3 smoothedDirection,
        bool hasSmoothedDirection,
        Vector3 userFacingDirection,
        float angleDeg,
        float angleThresholdDeg,
        bool anglePass,
        float indexSpeed,
        float minSpeedThreshold,
        bool speedPass,
        Vector3 ballCenter,
        Vector3 indexPosition,
        Vector3 viewerPosition)
    {
        string ans = Time.frameCount.ToString(CultureInfo.InvariantCulture);
        ans += "," + Time.time.ToString("F4", CultureInfo.InvariantCulture);
        ans += "," + ballSpawner.currentCaseNumber;
        ans += "," + ballSpawner.currentRoundID;
        ans += "," + modeSelected;
        ans += "," + EscapeCsv(source);
        ans += "," + ballIndex;
        ans += "," + ballKind;
        ans += "," + EscapeCsv(failReason);
        ans += "," + Bool01(finalCancelPass);
        ans += "," + Bool01(handInBall);
        ans += "," + Bool01(directionReentryRequired);
        ans += "," + Bool01(cancelAlreadyTriggered);
        ans += "," + sampleCount;
        ans += "," + windowFrameCount;
        ans += "," + Bool01(windowReady);
        ans += FormatVectorCsv(windowMovement);
        ans += "," + windowMovement.magnitude.ToString("F5", CultureInfo.InvariantCulture);
        ans += FormatVectorCsv(smoothedDirection);
        ans += "," + Bool01(hasSmoothedDirection);
        ans += FormatVectorCsv(userFacingDirection);
        ans += "," + angleDeg.ToString("F2", CultureInfo.InvariantCulture);
        ans += "," + angleThresholdDeg.ToString("F2", CultureInfo.InvariantCulture);
        ans += "," + Bool01(anglePass);
        ans += "," + indexSpeed.ToString("F5", CultureInfo.InvariantCulture);
        ans += "," + minSpeedThreshold.ToString("F5", CultureInfo.InvariantCulture);
        ans += "," + Bool01(speedPass);
        ans += FormatVectorCsv(ballCenter);
        ans += FormatVectorCsv(indexPosition);
        ans += FormatVectorCsv(viewerPosition);
        return ans;
    }

    private string FormatPinchReleaseDistance(
        string eventType,
        string reason,
        int modeSelected,
        int ballIndex,
        int ballKind,
        bool isMineBall,
        bool pinchArmed,
        bool isPinch,
        bool indexInBall,
        bool thumbInBall,
        string triggeredBy,
        float entryElapsed,
        bool poseValid,
        float activateThresholdMeters,
        float deactivateThresholdMeters,
        float distanceMeters,
        bool swipeContactValid,
        float swipeContactDistanceMeters,
        float swipeContactThresholdMeters,
        Vector3 indexPosition,
        Vector3 thumbPosition)
    {
        string ans = Time.frameCount.ToString(CultureInfo.InvariantCulture);
        ans += "," + Time.time.ToString("F4", CultureInfo.InvariantCulture);
        ans += "," + ballSpawner.currentCaseNumber;
        ans += "," + ballSpawner.currentRoundID;
        ans += "," + modeSelected;
        ans += "," + EscapeCsv(eventType);
        ans += "," + EscapeCsv(reason);
        ans += "," + ballIndex;
        ans += "," + ballKind;
        ans += "," + Bool01(isMineBall);
        ans += "," + Bool01(pinchArmed);
        ans += "," + Bool01(isPinch);
        ans += "," + Bool01(indexInBall);
        ans += "," + Bool01(thumbInBall);
        ans += "," + EscapeCsv(triggeredBy);
        ans += "," + entryElapsed.ToString("F4", CultureInfo.InvariantCulture);
        ans += "," + Bool01(poseValid);
        ans += "," + activateThresholdMeters.ToString("F6", CultureInfo.InvariantCulture);
        ans += "," + deactivateThresholdMeters.ToString("F6", CultureInfo.InvariantCulture);

        if (poseValid)
        {
            ans += "," + distanceMeters.ToString("F6", CultureInfo.InvariantCulture);
            ans += "," + (distanceMeters * 1000f).ToString("F3", CultureInfo.InvariantCulture);
        }
        else
        {
            ans += ",NA,NA";
        }

        ans += "," + Bool01(swipeContactValid);
        if (swipeContactValid)
        {
            ans += "," + swipeContactDistanceMeters.ToString("F6", CultureInfo.InvariantCulture);
            ans += "," + (swipeContactDistanceMeters * 1000f).ToString("F3", CultureInfo.InvariantCulture);
            ans += "," + swipeContactThresholdMeters.ToString("F6", CultureInfo.InvariantCulture);
        }
        else
        {
            ans += ",NA,NA,NA";
        }

        if (poseValid)
        {
            ans += FormatVectorCsv(indexPosition);
            ans += FormatVectorCsv(thumbPosition);
        }
        else
        {
            ans += ",NA,NA,NA,NA,NA,NA";
        }

        return ans;
    }

    private string FormatVectorCsv(Vector3 value)
    {
        return "," + value.x.ToString("F5", CultureInfo.InvariantCulture)
            + "," + value.y.ToString("F5", CultureInfo.InvariantCulture)
            + "," + value.z.ToString("F5", CultureInfo.InvariantCulture);
    }

    private string Bool01(bool value)
    {
        return value ? "1" : "0";
    }
    public string GetFilePath()
    {
        return filePath;
    }
    public string GetFileFullPath()
    {
        return fileFullPath;
    }
}

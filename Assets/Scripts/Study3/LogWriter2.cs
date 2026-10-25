using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LogWriter2 : MonoBehaviour
{
    public BallSpawner ballSpawner;
    public GameObject temp;
    private string date, filePath, fileName, fileFullPath;
    private GameObject[] Targets, Obstacles;
    private FileStream fs;
    private StreamWriter sw;
    private bool started = false;
    private AcceStimulate acceStimulate;
    private int ballIndex = -1;
    private int ballKind = 3;
    private int remainTime = 0;
    public GameObject expCamera;
    private ReactTap reactTap;
    public float currentInBallDistance = 0f;
    private bool wrotePendingErrorSnapshot = false;
    private StreamWriter trajectoryWriter;
    private long trajectorySampleSequence;
    private double nextTrajectoryFlush;
    private bool trajectoryClosed;


    void Start()
    {
        expCamera = GameObject.Find("CenterEyeAnchor");

        ballSpawner = GetComponent<BallSpawner>();
        date = ballSpawner.LogSessionTimestamp;
        filePath = Application.persistentDataPath + "/Logs/" + date + "/";
        fileName = date + "-" + "trajectory" + ".csv";
        fileFullPath = filePath + fileName;
        Debug.Log(fileFullPath);
        //acce = gameObject.GetComponent<AcceStimulate>();
        InitialPath();

        // Start writing once entering, no need to set invoked as true because we want it record every frame inside
        //acce.InEvent.AddListener(Write);
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
            remainTime = 2;
        });
        temp = GameObject.Find("temp");
        acceStimulate = temp.GetComponent<AcceStimulate>();
        reactTap = temp.GetComponent<ReactTap>();
    }

    private void FixedUpdate()
    {
        if (started)
        {
            GetBallIndex();
            Write();
        }
        else if (remainTime > 0)
        {
            Write();
            remainTime--;
        }
    }

    public string FormatMsg()
    {
        if (expCamera == null)
        {
            expCamera = GameObject.Find("CenterEyeAnchor");
        }

        int activeBallKind = ballKind;
        int activeBallIndex = ballIndex;
        Vector3 activeIndexLoc = ballSpawner.indexLoc;
        Vector3 activeThumbLoc = ballSpawner.thumbLoc;
        bool errorTriggered = false;
        wrotePendingErrorSnapshot = false;

        if (ballSpawner.TryGetPendingErrorTriggerSnapshot(out Vector3 snapshotIndexLoc, out Vector3 snapshotThumbLoc, out int snapshotAreaType, out int snapshotIndex, out Vector3 snapshotObstaclePosition))
        {
            activeBallKind = snapshotAreaType;
            activeBallIndex = snapshotIndex;
            activeIndexLoc = snapshotIndexLoc;
            activeThumbLoc = snapshotThumbLoc;
            errorTriggered = true;
            wrotePendingErrorSnapshot = true;
        }

        ReactTap activeReactTap = ResolveActiveReactTap(activeBallKind, activeBallIndex);
        if (activeReactTap == null)
        {
            activeReactTap = reactTap;
        }

        float angeleAC_CD = activeReactTap != null ? activeReactTap.CalculateAngle_between_finger_wrist() : 0f;
        Vector3 wristPosition = activeReactTap != null ? activeReactTap.WristPose.Position : Vector3.zero;
        float wristSpeed = activeReactTap != null ? activeReactTap.wristSpeed : 0f;
        bool usesMagicTap2 = ballSpawner.modeSelected == BallSpawner.Mode.magicTap2;
        bool isAOK = !usesMagicTap2 && activeReactTap != null && activeReactTap.JudgeA_vec();
        bool isVOK = !usesMagicTap2 && activeReactTap != null && activeReactTap.JudgeV_vec();
        float aVec = activeReactTap != null ? activeReactTap.A_vec : 0f;
        float vVec = activeReactTap != null ? activeReactTap.V_vec : 0f;

        if (usesMagicTap2)
        {
            MagicTap2MotionTracker motion = ResolveMagicTap2Motion(activeBallKind, activeBallIndex);
            isAOK = motion != null && motion.AccelerationConditionMet;
            isVOK = motion != null && motion.SpeedConditionMet;
            aVec = motion != null && motion.HasAcceleration ? motion.Acceleration : 0f;
            vVec = motion != null ? motion.Speed : 0f;
        }
        else if (ballSpawner.modeSelected == BallSpawner.Mode.dynamicStop)
        {
            S2AccMode targetMode = activeReactTap != null ? activeReactTap.GetComponent<S2AccMode>() : null;
            S2Obstacles obstacleMode = activeReactTap != null ? activeReactTap.GetComponent<S2Obstacles>() : null;
            DynamicStopMotionTracker motion = targetMode != null
                ? targetMode.DynamicStopMotion
                : obstacleMode != null ? obstacleMode.DynamicStopMotion : null;
            isAOK = motion != null && motion.AccelerationConditionMet;
            isVOK = motion != null && motion.SpeedConditionMet;
            aVec = motion != null && motion.HasAcceleration ? motion.Acceleration : 0f;
            vVec = motion != null ? motion.Speed : 0f;
        }
        else if (ballSpawner.modeSelected == BallSpawner.Mode.ixdlDynamic)
        {
            S2AccMode targetMode = activeReactTap != null ? activeReactTap.GetComponent<S2AccMode>() : null;
            S2Obstacles obstacleMode = activeReactTap != null ? activeReactTap.GetComponent<S2Obstacles>() : null;
            IxDLDynamicMotionTracker motion = targetMode != null
                ? targetMode.IxDLDynamicMotion
                : obstacleMode != null ? obstacleMode.IxDLDynamicMotion : null;
            isAOK = motion != null && motion.AccelerationConditionMet;
            isVOK = motion != null && motion.SpeedConditionMet;
            aVec = motion != null && motion.HasAcceleration ? motion.Acceleration : 0f;
            vVec = motion != null ? motion.Speed : 0f;
        }

        return ballSpawner.currentCaseNumber + "," + ballSpawner.currentRoundID + "," + activeIndexLoc.x + "," + activeIndexLoc.y + "," + activeIndexLoc.z + "," + acceStimulate.a + "," + acceStimulate.v + "," + ballSpawner.modeSelected + "," + activeBallKind + "," + activeBallIndex
            + "," + angeleAC_CD + "," + wristPosition.x + "," + wristPosition.y + "," + wristPosition.z + "," + wristSpeed + "," + isAOK + "," + isVOK + "," + aVec + "," + vVec + "," + errorTriggered + "," + ballSpawner.isReact + "," + currentInBallDistance
            + "," + activeThumbLoc.x + "," + activeThumbLoc.y + "," + activeThumbLoc.z
            + FormattableString.Invariant($",{Time.frameCount},{Time.timeAsDouble:R},{Time.fixedTimeAsDouble:R},{Time.fixedDeltaTime:R},{1f / Time.fixedDeltaTime:R},{ballSpawner.samplingRateHz:R},{Time.realtimeSinceStartupAsDouble:R},{Time.timeScale:R},{trajectorySampleSequence},{acceStimulate.MotionSampleSequence},{acceStimulate.MotionSampleFixedTime:R},{acceStimulate.MotionSampleDeltaTime:R},{acceStimulate.IndexTipPoseValid},{acceStimulate.IndexDistalPoseValid},{acceStimulate.IndexDistalPose.Position.x:R},{acceStimulate.IndexDistalPose.Position.y:R},{acceStimulate.IndexDistalPose.Position.z:R},AcceStimulate.v,1");
    }

    public void InitialPath()
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }
        if (!File.Exists(fileFullPath))
        {
            fs = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            sw.WriteLine("Case,ID,indexLocX,indexLocY,indexLocZ,ori_a,ori_v,modeSelected,area_type,index,angle,wristX,wristY,wristZ,wristSpeed,isAOK,isVOK,A_vec,V_vec,errorTriggered,isReact,currentInBallDistance,thumbX,thumbY,thumbZ,frame,time,fixedTime,fixedDeltaTime,fixedSamplingHz,requestedSamplingHz,realtime,timeScale,trajectorySampleSequence,motionSampleSequence,motionFixedTime,motionDeltaTime,tipPoseValid,distalPoseValid,distalX,distalY,distalZ,velocitySource,schemaVersion");
            sw.Close();
            fs.Close();
        }
    }

    public void Write()
    {
        if (trajectoryClosed) return;
        if (trajectoryWriter == null)
        {
            trajectoryWriter = new StreamWriter(new FileStream(fileFullPath, FileMode.Append, FileAccess.Write, FileShare.Read),
                new System.Text.UTF8Encoding(false), 65536);
        }
        trajectorySampleSequence++;
        string log = FormatMsg();
        trajectoryWriter.WriteLine(log);
        ballSpawner.ConsumePendingErrorTriggerSnapshot();
        if (wrotePendingErrorSnapshot && ballSpawner.area_type == 5)
        {
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
        }
    }

    private void LateUpdate()
    {
        if (Time.realtimeSinceStartupAsDouble >= nextTrajectoryFlush)
        {
            trajectoryWriter?.Flush();
            nextTrajectoryFlush = Time.realtimeSinceStartupAsDouble + 0.5;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) trajectoryWriter?.Flush();
    }

    private void OnDisable() => trajectoryWriter?.Flush();
    private void OnDestroy() => CloseTrajectory();
    private void OnApplicationQuit() => CloseTrajectory();

    private void CloseTrajectory()
    {
        trajectoryClosed = true;
        trajectoryWriter?.Dispose();
        trajectoryWriter = null;
    }

    public string GetFilePath()
    {
        return filePath;
    }

    public string GetFileFullPath()
    {
        return fileFullPath;
    }

    public void GetBallIndex()
    {
        ballIndex = ballSpawner.index;
        ballKind = ballSpawner.area_type;
    }

    private MagicTap2MotionTracker ResolveMagicTap2Motion(int activeBallKind, int activeBallIndex)
    {
        if (ballSpawner == null || activeBallIndex < 0) return null;
        if (activeBallKind == 1 || activeBallKind == 4)
        {
            foreach (GameObject target in ballSpawner.TargetObjects)
            {
                S2AccMode mode = target != null ? target.GetComponent<S2AccMode>() : null;
                if (mode != null && mode.ballIndex == activeBallIndex) return mode.MagicTap2Motion;
            }
        }
        else if (activeBallKind == 2 || activeBallKind == 5)
        {
            foreach (GameObject obstacle in ballSpawner.obstacleObjects)
            {
                S2Obstacles mode = obstacle != null ? obstacle.GetComponent<S2Obstacles>() : null;
                if (mode != null && mode.ballIndex == activeBallIndex) return mode.MagicTap2Motion;
            }
        }
        return null;
    }

    private ReactTap ResolveActiveReactTap(int activeBallKind, int activeBallIndex)
    {
        if (ballSpawner == null || activeBallIndex < 0)
        {
            return null;
        }

        if (activeBallKind == 1 || activeBallKind == 4)
        {
            foreach (GameObject target in ballSpawner.TargetObjects)
            {
                if (target == null)
                {
                    continue;
                }

                S2AccMode accMode = target.GetComponent<S2AccMode>();
                if (accMode != null && accMode.ballIndex == activeBallIndex)
                {
                    return target.GetComponent<ReactTap>();
                }
            }
        }
        else if (activeBallKind == 2 || activeBallKind == 5)
        {
            foreach (GameObject obstacle in ballSpawner.obstacleObjects)
            {
                if (obstacle == null)
                {
                    continue;
                }

                S2Obstacles obstacleMode = obstacle.GetComponent<S2Obstacles>();
                if (obstacleMode != null && obstacleMode.ballIndex == activeBallIndex)
                {
                    return obstacle.GetComponent<ReactTap>();
                }
            }
        }

        return null;
    }
}

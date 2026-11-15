using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Microsoft.MixedReality.Toolkit.Utilities;

public class LogWriter : MonoBehaviour
{
    private string date, filePath, fileName, fileFullPath;
    private AcceStimulate acce;
    private FileStream fs;
    private StreamWriter sw;
    private int caseTime;
    private int segment = 1;
    private ExperimentCounter experiment;
    private Study1HeadAnchor headAnchor;
    private string continuousPath, eventsPath;
    private StreamWriter motionWriter, continuousWriter, eventsWriter;
    private double lastFlushTime;
    private int logicalTrial = 1;
    private long lastContinuousSequence = -1, lastContinuousEpoch = -1;
    private uint lastContinuousSource;
    private long lastExitSequence = -1, lastExitEpoch = -1;
    private uint lastExitSource;
    private long lastInterruptedSequence = -1, lastInterruptedEpoch = -1;
    private uint lastInterruptedSource;
    private static readonly CultureInfo CsvCulture = CultureInfo.InvariantCulture;
    private static readonly TrackedHandJoint[] LoggedJoints = BuildJointList();
    public TextMeshProUGUI times;
    void Start()
    {
        date = DateTime.Now.ToString("yyyyMMdd-HH-mm-ss");
        filePath = Application.persistentDataPath + "/Logs/"+date+"/";
        acce = gameObject.GetComponent<AcceStimulate>();
        fileName = date + "-" + name + (acce.UsesMrtkJointUpdates ? "-mrtk.csv" : ".csv");
        fileFullPath = filePath + fileName;
        Debug.Log(fileFullPath);
        caseTime = 1;
        InitialPath();

        // The Study1 experiment records the complete approach and withdrawal, including outside the target.
        if (acce.UsesMrtkJointUpdates && gameObject.scene.path == "Assets/Scenes/Study1.unity")
        {
            experiment = GetComponent<ExperimentCounter>();
            if (experiment != null)
            {
                headAnchor = GetComponentInParent<Study1HeadAnchor>();
                if (headAnchor != null)
                {
                    headAnchor.ReferenceSpaceResetStarted += OnReferenceSpaceResetStarted;
                    headAnchor.ReferenceSpaceResetCompleted += OnReferenceSpaceResetCompleted;
                }
                WriteSessionLayout();
                continuousPath = filePath + date + "-" + name + "-continuous.csv";
                eventsPath = filePath + date + "-" + name + "-events.csv";
                if (!File.Exists(continuousPath)) File.WriteAllText(continuousPath, MrtkHeader() + ",LogicalTrial,Inside" + Environment.NewLine);
                if (!File.Exists(eventsPath)) File.WriteAllText(eventsPath, "Event,Trial,ReceivedRealtime,Case,Segment,SourceId,TrackingEpoch,SampleSequence,HoldDuration,MaxCount,EndCaptureDelay,ReferenceSpaceEpoch" + Environment.NewLine);
                experiment.TrialEvent += OnTrialEvent;
                Study3HandPoseSource.JointSampleProcessed += OnJointSampleProcessed;
            }
        }

        // Start writing once entering, no need to set invoked as true because we want it record every frame inside
        acce.InEvent.AddListener(Write);
        // Increment the case time after getting the finger out
        acce.AfterOutEvent.AddListener(increCase);
        if (acce.UsesMrtkJointUpdates) acce.MotionTrackingInterrupted += OnTrackingInterrupted;
        OpenWriters();
    }

    private void OnEnable() { OpenWriters(); }
    private void Update() { if (motionWriter != null) FlushPeriodically(); }
    private void OnDisable() { CloseWriters(); }
    private void OnApplicationPause(bool paused) { if (paused) FlushWriters(); }
    private void OnApplicationQuit() { CloseWriters(); }

    private void OpenWriters()
    {
        if (fileFullPath == null || acce == null || !acce.UsesMrtkJointUpdates) return;
        if (motionWriter == null) motionWriter = new StreamWriter(fileFullPath, true);
        if (continuousPath != null && continuousWriter == null) continuousWriter = new StreamWriter(continuousPath, true);
        if (eventsPath != null && eventsWriter == null) eventsWriter = new StreamWriter(eventsPath, true);
        lastFlushTime = Time.realtimeSinceStartupAsDouble;
    }

    private void FlushWriters()
    {
        motionWriter?.Flush(); continuousWriter?.Flush(); eventsWriter?.Flush();
        lastFlushTime = Time.realtimeSinceStartupAsDouble;
    }

    private void CloseWriters()
    {
        if (motionWriter != null) { motionWriter.Dispose(); motionWriter = null; }
        if (continuousWriter != null) { continuousWriter.Dispose(); continuousWriter = null; }
        if (eventsWriter != null) { eventsWriter.Dispose(); eventsWriter = null; }
    }

    private void FlushPeriodically()
    {
        if (Time.realtimeSinceStartupAsDouble - lastFlushTime >= .5) FlushWriters();
    }

    private void OnDestroy()
    {
        CloseWriters();
        if (acce != null) acce.MotionTrackingInterrupted -= OnTrackingInterrupted;
        if (experiment != null) experiment.TrialEvent -= OnTrialEvent;
        if (headAnchor != null)
        {
            headAnchor.ReferenceSpaceResetStarted -= OnReferenceSpaceResetStarted;
            headAnchor.ReferenceSpaceResetCompleted -= OnReferenceSpaceResetCompleted;
        }
        Study3HandPoseSource.JointSampleProcessed -= OnJointSampleProcessed;
    }

    private void OnReferenceSpaceResetStarted()
    {
        OnTrialEvent("reference_space_reset", logicalTrial, Time.realtimeSinceStartupAsDouble);
        FlushWriters();
    }

    private void OnReferenceSpaceResetCompleted()
    {
        WriteSessionLayout();
        OnTrialEvent("reference_space_placed", logicalTrial, headAnchor.PlacementRealtime);
        FlushWriters();
    }

    private void OnJointSampleProcessed(Study3JointSample sample)
    {
        if (!isActiveAndEnabled || acce == null || !acce.enabled) return;
        if (sample.SourceId != acce.MotionSourceId || sample.TrackingEpoch != acce.MotionTrackingEpoch || sample.Sequence != acce.MotionSampleSequence) return;
        WriteContinuous();
    }

    private void WriteContinuous(bool? inside = null, int? sampleCase = null, int? sampleSegment = null)
    {
        if (continuousPath == null) return;
        if (lastContinuousSequence == acce.MotionSampleSequence && lastContinuousEpoch == acce.MotionTrackingEpoch && lastContinuousSource == acce.MotionSourceId) return;
        if (continuousWriter == null) OpenWriters();
        continuousWriter.WriteLine(FormatMrtkMsg(sampleCase, sampleSegment) + "," + logicalTrial + "," + ((inside ?? acce.Entered) ? "1" : "0"));
        lastContinuousSequence = acce.MotionSampleSequence;
        lastContinuousEpoch = acce.MotionTrackingEpoch;
        lastContinuousSource = acce.MotionSourceId;
        FlushPeriodically();
    }

    private void OnTrialEvent(string kind, int trial, double timestamp)
    {
        if (!isActiveAndEnabled || eventsPath == null) return;
        // Capture the boundary sample before a completion handler disables the target.
        logicalTrial = trial;
        bool exit = kind == "early_exit" || kind == "success";
        bool advanced = exit && lastExitSequence == acce.MotionSampleSequence && lastExitEpoch == acce.MotionTrackingEpoch && lastExitSource == acce.MotionSourceId;
        bool interruptedAdvanced = kind == "tracking_interrupted" && lastInterruptedSequence == acce.MotionSampleSequence && lastInterruptedEpoch == acce.MotionTrackingEpoch && lastInterruptedSource == acce.MotionSourceId;
        int eventCase = caseTime - (advanced ? 1 : 0), eventSegment = segment - (advanced || interruptedAdvanced ? 1 : 0);
        if (exit) WriteContinuous(false, eventCase, eventSegment);
        if (eventsWriter == null) OpenWriters();
        eventsWriter.WriteLine(kind + "," + trial + "," + Number(timestamp) + "," + eventCase + "," + eventSegment + "," + acce.MotionSourceId + "," + acce.MotionTrackingEpoch + "," + acce.MotionSampleSequence + "," + Number(experiment.HoldDuration) + "," + experiment.MaxCount + "," + Number(experiment.EndCaptureDelay) + "," + ReferenceSpaceEpoch());
        if (kind == "recenter_interrupted") segment++;
        FlushPeriodically();
    }

    private static TrackedHandJoint[] BuildJointList()
    {
        var joints = new List<TrackedHandJoint>();
        foreach (TrackedHandJoint joint in Enum.GetValues(typeof(TrackedHandJoint)))
            if (joint.ToString() != "None") joints.Add(joint);
        return joints.ToArray();
    }

    private static string Number(double value) => value.ToString("R", CsvCulture);
    private string ReferenceSpaceEpoch() => headAnchor != null ? headAnchor.ReferenceSpaceEpoch.ToString(CsvCulture) : "";

    // This is a snapshot of the pose used for placement, not the later camera pose at LogWriter.Start.
    private void WriteSessionLayout()
    {
        Study1HeadAnchor anchor = GetComponentInParent<Study1HeadAnchor>();
        bool placed = anchor != null && anchor.HasPlacement;
        SphereCollider target = GetComponent<SphereCollider>();
        StartBallBehavior2 start = GetComponentInChildren<StartBallBehavior2>(true);
        SphereCollider startCollider = start != null ? start.GetComponent<SphereCollider>() : null;
        bool targetValid = placed && target != null;
        bool startValid = placed && startCollider != null;
        Vector3 targetCenter = targetValid ? target.transform.TransformPoint(target.center) : default;
        Vector3 startCenter = startValid ? startCollider.transform.TransformPoint(startCollider.center) : default;
        const string header = "SchemaVersion,PlacementAvailable,TrackingMode,PlacementRealtime,TimestampKind,HeadX,HeadY,HeadZ,HeadForwardX,HeadForwardY,HeadForwardZ,HeadRotationX,HeadRotationY,HeadRotationZ,HeadRotationW,TargetRotationX,TargetRotationY,TargetRotationZ,TargetRotationW,TargetCenterValid,TargetCenterX,TargetCenterY,TargetCenterZ,StartCenterValid,StartCenterX,StartCenterY,StartCenterZ,HeadToTargetMeters,StartToTargetMeters,RequestedHeadToTargetMeters,RequestedVerticalOffsetMeters,ReferenceSpaceEpoch,LayoutMode,HeadPoseSource,TrackingHeadX,TrackingHeadY,TrackingHeadZ,TrackingHeadRotationX,TrackingHeadRotationY,TrackingHeadRotationZ,TrackingHeadRotationW,TrackingOriginX,TrackingOriginY,TrackingOriginZ,TrackingOriginRotationX,TrackingOriginRotationY,TrackingOriginRotationZ,TrackingOriginRotationW,CameraPoseValid,CameraWorldX,CameraWorldY,CameraWorldZ,CameraWorldRotationX,CameraWorldRotationY,CameraWorldRotationZ,CameraWorldRotationW";
        bool headPose = placed && anchor.PlacementHeadPoseValid;
        var values = new List<string> { "2", placed ? "1" : "0", headPose ? (anchor.PlacementWasTracked ? "xr_tracked" : "editor_preview") : "unavailable", placed ? Number(anchor.PlacementRealtime) : "", "unity_realtime_since_startup" };
        AddVector(values, headPose, headPose ? anchor.PlacementHeadPosition : default);
        AddVector(values, headPose, headPose ? anchor.PlacementHeadForward : default);
        if (headPose)
        {
            Quaternion q = anchor.PlacementHeadRotation;
            values.Add(Number(q.x)); values.Add(Number(q.y)); values.Add(Number(q.z)); values.Add(Number(q.w));
        }
        else values.AddRange(new[] { "", "", "", "" });
        if (placed)
        {
            Quaternion q = anchor.PlacementTargetRotation;
            values.Add(Number(q.x)); values.Add(Number(q.y)); values.Add(Number(q.z)); values.Add(Number(q.w));
        }
        else values.AddRange(new[] { "", "", "", "" });
        values.Add(targetValid ? "1" : "0"); AddVector(values, targetValid, targetCenter);
        values.Add(startValid ? "1" : "0"); AddVector(values, startValid, startCenter);
        values.Add(targetValid && headPose ? Number(Vector3.Distance(anchor.PlacementHeadPosition, targetCenter)) : "");
        values.Add(targetValid && startValid ? Number(Vector3.Distance(startCenter, targetCenter)) : "");
        // Fixed-world placement does not imply a requested distance or offset from the head.
        values.Add("");
        values.Add("");
        values.Add(anchor != null ? anchor.ReferenceSpaceEpoch.ToString(CsvCulture) : "");
        values.Add(placed ? "world_fixed" : "");
        bool trackedPose = headPose && anchor.PlacementWasTracked;
        bool cameraPose = placed && anchor.PlacementCameraPoseValid;
        values.Add(headPose ? (trackedPose ? "xr_head_device" : "editor_camera") : "");
        AddVector(values, trackedPose, trackedPose ? anchor.PlacementTrackingPosition : default);
        AddQuaternion(values, trackedPose, trackedPose ? anchor.PlacementTrackingRotation : default);
        AddVector(values, trackedPose, trackedPose ? anchor.PlacementTrackingOriginPosition : default);
        AddQuaternion(values, trackedPose, trackedPose ? anchor.PlacementTrackingOriginRotation : default);
        values.Add(placed ? (cameraPose ? "1" : "0") : "");
        AddVector(values, cameraPose, cameraPose ? anchor.PlacementCameraPosition : default);
        AddQuaternion(values, cameraPose, cameraPose ? anchor.PlacementCameraRotation : default);

        string baseName = filePath + date + "-" + name + "-session-layout";
        string path = baseName + ".csv";
        for (int suffix = 2; File.Exists(path); suffix++) path = baseName + "-" + suffix + ".csv";
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
        {
            writer.WriteLine(header);
            writer.WriteLine(string.Join(",", values));
        }
    }

    private static void AddVector(List<string> values, bool valid, Vector3 point)
    {
        values.Add(valid ? Number(point.x) : "");
        values.Add(valid ? Number(point.y) : "");
        values.Add(valid ? Number(point.z) : "");
    }

    private static void AddQuaternion(List<string> values, bool valid, Quaternion rotation)
    {
        values.Add(valid ? Number(rotation.x) : "");
        values.Add(valid ? Number(rotation.y) : "");
        values.Add(valid ? Number(rotation.z) : "");
        values.Add(valid ? Number(rotation.w) : "");
    }

    private string MrtkHeader()
    {
        var result = new StringBuilder("Case,RunTime,Object,EnterTime,Velocity,Acceleration,Condition,PrepTime,Segment,SourceId,TrackingEpoch,SampleSequence,ReceivedRealtime,SampleDeltaTime,VelocityValid,AccelerationValid");
        foreach (var joint in LoggedJoints)
            result.Append(",").Append(joint).Append("_Valid,").Append(joint).Append("_X,").Append(joint).Append("_Y,").Append(joint).Append("_Z");
        result.Append(",ReferenceSpaceEpoch");
        return result.ToString();
    }

    private string FormatMrtkMsg(int? sampleCase = null, int? sampleSegment = null)
    {
        var result = new StringBuilder();
        result.Append(sampleCase ?? caseTime).Append(',').Append(Number(Time.time)).Append(',')
            .Append('"').Append(name.Replace("\"", "\"\"")).Append('"').Append(',')
            .Append(Number(acce.UpdateT)).Append(',')
            .Append(acce.MotionVelocityValid ? Number(acce.v) : "").Append(',')
            .Append(acce.MotionAccelerationValid ? Number(acce.a) : "").Append(',')
            .Append(acce.condition).Append(',').Append(Number(acce.PrepTime)).Append(',')
            .Append(sampleSegment ?? segment).Append(',').Append(acce.MotionSourceId).Append(',')
            .Append(acce.MotionTrackingEpoch).Append(',').Append(acce.MotionSampleSequence).Append(',')
            .Append(Number(acce.MotionSampleTimestamp)).Append(',').Append(Number(acce.MotionSampleDeltaTime)).Append(',')
            .Append(acce.MotionVelocityValid ? "1" : "0").Append(',').Append(acce.MotionAccelerationValid ? "1" : "0");
        foreach (var joint in LoggedJoints)
        {
            if (acce.CurrentJointSample.TryGetJointPose(joint, out MixedRealityPose pose) && Finite(pose.Position))
                result.Append(",1,").Append(Number(pose.Position.x)).Append(',').Append(Number(pose.Position.y)).Append(',').Append(Number(pose.Position.z));
            else result.Append(",0,,,");
        }
        result.Append(',').Append(ReferenceSpaceEpoch());
        return result.ToString();
    }

    private static bool Finite(Vector3 p)
    {
        return !float.IsNaN(p.x) && !float.IsInfinity(p.x)
            && !float.IsNaN(p.y) && !float.IsInfinity(p.y)
            && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }

    private void OnTrackingInterrupted()
    {
        if (!acce.Entered) return;
        lastInterruptedSequence = acce.MotionSampleSequence;
        lastInterruptedEpoch = acce.MotionTrackingEpoch;
        lastInterruptedSource = acce.MotionSourceId;
        segment++;
    }

    public string FormatMsg()
    {
        if (acce.UsesMrtkJointUpdates) return FormatMrtkMsg();
        return caseTime + "," + Time.time + "," + name + "," + acce.UpdateT + "," + acce.v.ToString() + "," + acce.a.ToString() + "," + acce.condition + "," + acce.PrepTime;
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
            sw.WriteLine(acce.UsesMrtkJointUpdates ? MrtkHeader() : "Case,RunTime,Object,EnterTime,Velocity,Acceleration,Condition,PrepTime");
            sw.Close();
            fs.Close();
        }
    }
    public void Write()
    {
        if (acce.UsesMrtkJointUpdates)
        {
            if (motionWriter == null) OpenWriters();
            motionWriter.WriteLine(FormatMrtkMsg());
            FlushPeriodically();
            return;
        }
        // New an fs and sw, close an fs and sw after writing
        // because a writer and file stream will lock the memory storage so that other gameobject cannot write
        fs = new FileStream(fileFullPath, FileMode.Append, FileAccess.Write);
        sw = new StreamWriter(fs);
        string log = FormatMsg();
        sw.WriteLine(log);
        sw.Close();
        fs.Close();
    }
    public void increCase()
    {
        lastExitSequence = acce.MotionSampleSequence;
        lastExitEpoch = acce.MotionTrackingEpoch;
        lastExitSource = acce.MotionSourceId;
        caseTime++;
        segment++;
        
        //times.text = (11 - caseTime).ToString();
        if (caseTime >= 16 && gameObject.scene.path != "Assets/Scenes/Study1.unity")
        {
            gameObject.SetActive(false);

        }
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

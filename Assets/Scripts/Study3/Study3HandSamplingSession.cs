using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

/// <summary>Buffered sidecars. Diagnostic I/O failures never change a selection result.</summary>
public sealed class Study3HandSamplingSession : IDisposable
{
    private StreamWriter updates, reads;
    private readonly Func<object> context;
    private bool failed, closed;
    private long row;
    private long sdkReadCount, providerUpdateCount, poseReadCount;
    private double nextFlush;

    internal Study3HandSamplingSession(string directory, string sessionId, Func<object> context)
    {
        this.context = context;
        try
        {
            Directory.CreateDirectory(directory);
            updates = Open(Path.Combine(directory, sessionId + "-hand-updates.csv"),
                "row,event,frame,time,fixedTime,realtime,inFixedTimeStep,sourceId,handedness,providerUpdateId,sdkHandInstanceId,sdkReadSequence,sdkSampleTimeStamp,sdkRequestedTimeStamp,detailsJson,schemaVersion");
            reads = Open(Path.Combine(directory, sessionId + "-hand-reads.csv"),
                "readId,frame,time,fixedTime,realtime,inFixedTimeStep,ownerInstanceId,ownerType,method,joint,poseValid,x,y,z,sourceId,handedness,providerUpdateId,sharedSampleSequence,matchStatus,sameProviderUpdateAsPreviousRead,controllerPositionAvailable,motionSequence,schemaVersion");
        }
        catch (Exception error) { Fail(error); }
    }

    private static StreamWriter Open(string path, string header)
    {
        bool newFile = !File.Exists(path) || new FileInfo(path).Length == 0;
        var writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read),
            new System.Text.UTF8Encoding(false), 262144);
        if (newFile) writer.WriteLine(header);
        return writer;
    }

    internal void WriteUpdate(string kind, uint source, Handedness hand, long providerId,
        OVRHandSamplingMetadata sdk, Dictionary<string, object> details)
    {
        if (failed || closed) return;
        try
        {
            details = details ?? new Dictionary<string, object>();
            details["context"] = context?.Invoke();
            if (kind == "sdk_read") sdkReadCount++;
            if (kind == "provider_update") providerUpdateCount++;
            updates.WriteLine(Study3LogEncoding.Row(++row, kind, Time.frameCount, Time.timeAsDouble, Time.fixedTimeAsDouble,
                Time.realtimeSinceStartupAsDouble, Time.inFixedTimeStep, source, hand, providerId,
                sdk.HandInstanceId, sdk.ReadSequence, sdk.ReadSucceeded ? (object)sdk.SampleTimeStamp : null,
                sdk.ReadSucceeded ? (object)sdk.RequestedTimeStamp : null, Study3LogEncoding.Json(details), 1));
        }
        catch (Exception error) { Fail(error); }
    }

    internal void WriteRead(long id, int owner, string ownerType, string method, TrackedHandJoint joint,
        bool valid, Vector3 pose, uint source, Handedness hand, long providerId, long sharedSequence, string status, bool repeated, bool? tracked, long motionSequence)
    {
        if (failed || closed) return;
        try
        {
            poseReadCount++;
            reads.WriteLine(Study3LogEncoding.Row(id, Time.frameCount, Time.timeAsDouble, Time.fixedTimeAsDouble,
                Time.realtimeSinceStartupAsDouble, Time.inFixedTimeStep, owner, ownerType, method, joint, valid,
                valid ? (object)pose.x : null, valid ? (object)pose.y : null, valid ? (object)pose.z : null,
                source, hand, providerId, sharedSequence, status, repeated, tracked, motionSequence, 1));
        }
        catch (Exception error) { Fail(error); }
    }

    public void Tick()
    {
        if (Time.realtimeSinceStartupAsDouble < nextFlush) return;
        Flush(); nextFlush = Time.realtimeSinceStartupAsDouble + 0.5;
    }
    public void Flush()
    {
        if (failed || closed) return;
        try { updates?.Flush(); reads?.Flush(); }
        catch (Exception error) { Fail(error); }
    }
    private void Fail(Exception error)
    {
        failed = true;
        try { updates?.Dispose(); } catch { }
        try { reads?.Dispose(); } catch { }
        Debug.LogError("Supplementary hand sampling log stopped; selection continues. " + error.Message);
    }
    public void Dispose()
    {
        if (closed) return;
        WriteUpdate("session_end", 0, Handedness.None, 0, default, new Dictionary<string, object> {
            { "observedSdkReadCount", sdkReadCount }, { "providerUpdateCount", providerUpdateCount }, { "poseReadCount", poseReadCount }
        });
        Flush(); closed = true;
        try { updates?.Dispose(); } catch { }
        try { reads?.Dispose(); } catch { }
        Study3HandSamplingDiagnostics.Remove(this);
    }
}

// Sampling baseline diagnostics import verification (2026-10-28); no selection rule change.

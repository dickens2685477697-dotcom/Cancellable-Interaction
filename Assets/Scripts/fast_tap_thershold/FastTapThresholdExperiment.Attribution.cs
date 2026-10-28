using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class FastTapThresholdExperiment
{
    [Header("Supplementary log metadata")]
    public string attributionParticipantId = "";
    public string attributionProtocolVersion = "";
    private long attributionBlock, attributionAttempt;
    private bool attributionOpen, attributionHasFinger;
    private bool? attributionPreviousTracking;
    private Vector3 attributionPosition;
    private StreamWriter attributionWriter;
    private bool attributionClosed, attributionFailed;
    private string attributionBuildStamp = "unavailable";
    private void CacheAttributionInput(bool valid, Vector3 position) { attributionHasFinger = valid; attributionPosition = position; }
    private void FlushAttributionSafely() { try { attributionWriter?.Flush(); } catch { attributionFailed = true; } }
    private void BeginAttributionBlock() { attributionBlock++; }
    private void BeginAttributionAttempt()
    {
        if (attributionOpen) WriteAttribution("aborted", "new_attempt_before_result");
        attributionAttempt++; attributionOpen = true; attributionPreviousTracking = null;
        WriteAttribution("trial_start", "start_ball_accepted");
    }
    private void ObserveAttributionInput(bool valid, Vector3 position)
    {
        attributionHasFinger = valid; attributionPosition = position;
        if (attributionPreviousTracking.HasValue && attributionPreviousTracking.Value != valid)
            WriteAttribution(valid ? "tracking_recovered" : "tracking_lost", "actual_pose_query_status");
        attributionPreviousTracking = valid;
        WriteAttribution("sample", "before_target_decision");
    }
    private void CompleteAttributionAttempt(bool triggered, bool success)
    {
        string reason = !attributionHasFinger ? "legacy_treats_tracking_loss_as_trigger"
            : triggered ? "observed_exit_trigger" : "cancel_time_reached";
        WriteAttribution("trial_end", reason, triggered ? "trigger" : "cancel", success);
        attributionOpen = false;
    }
    private void WriteAttribution(string kind, string reason, string action = "none", bool? success = null)
    {
        if (attributionClosed || attributionFailed || logger == null) return;
        try
        {
            if (attributionWriter == null)
            {
                TextAsset buildStamp = Resources.Load<TextAsset>("Study3AttributionBuild");
                attributionBuildStamp = buildStamp != null ? buildStamp.text : "unavailable";
                string folder = logger.GetLogDirectory(); if (string.IsNullOrEmpty(folder)) return;
                string file = Path.Combine(folder, Path.GetFileName(folder) + "-fast-tap-attribution.csv");
                bool header = !File.Exists(file) || new FileInfo(file).Length == 0;
                attributionWriter = new StreamWriter(new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.Read), new System.Text.UTF8Encoding(false), 65536);
                if (header) attributionWriter.WriteLine("sessionId,blockSequence,attemptSequence,phase,trialIndex,event,time,realtime,trackingValid,insideBeforeDecision,colorAssigned,targetColor,thresholdMs,actualAction,legacyTaskSuccess,reason,participantId,protocolVersion,detailsJson,schemaVersion");
            }
            var details = new Dictionary<string, object> {
                { "position", attributionHasFinger ? Study3AttributionCapture.Vector(attributionPosition) : null },
                { "targetCenter", targetBallObject != null ? Study3AttributionCapture.Vector(targetBallObject.transform.position) : null },
                { "targetDiameter", BallDiameterMeters }, { "targetEnterTime", targetEnterTime },
                { "timeInTargetSeconds", timeInTargetSeconds }, { "pilotTrialsPerBlock", pilotTrialsPerBlock },
                { "formalTrialsPerBlock", formalTrialsPerBlock }, { "thresholdPreset", thresholdPreset.ToString() },
                { "customThresholdMs", customThresholdMs }, { "plannedColorSequence", blockSequence.ConvertAll(c => c.ToString()).ToArray() },
                { "unityVersion", Application.unityVersion }, { "applicationBuildGuid", Application.buildGUID },
                { "deviceModel", SystemInfo.deviceModel }, { "buildStampJson", attributionBuildStamp }, { "state", state.ToString() }, { "observationBeforeDecision", kind == "sample" }
            };
            attributionWriter.WriteLine(Study3LogEncoding.Row(Path.GetFileName(logger.GetLogDirectory()), attributionBlock, attributionAttempt,
                GetCurrentPhaseName(), currentTrialIndex, kind, Time.timeAsDouble, Time.realtimeSinceStartupAsDouble,
                attributionHasFinger, fingerInsideTarget, currentTrialColorAssigned,
                currentTrialColorAssigned ? currentTrialColor.ToString().ToLowerInvariant() : "unassigned", GetCancelThresholdMs(),
                action, success.HasValue ? (object)success.Value : null, reason, attributionParticipantId, attributionProtocolVersion,
                Study3LogEncoding.Json(details), 1));
            if (kind != "sample") attributionWriter.Flush();
        }
        catch (Exception error)
        {
            attributionFailed = true; try { attributionWriter?.Dispose(); } catch { }
            Debug.LogError("Fast Tap supplementary logging stopped; original output continues. " + error.Message);
        }
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused) handSamplingSession?.Flush();
        WriteAttribution(paused ? "application_paused" : "application_resumed", "lifecycle_marker");
        FlushAttributionSafely();
    }
    private void OnDisable()
    {
        StopHandSamplingLog();
        if (attributionOpen) { WriteAttribution("aborted", "source_disabled_before_outcome"); attributionOpen = false; }
        FlushAttributionSafely();
    }
    private void OnDestroy()
    {
        StopHandSamplingLog();
        if (attributionOpen) WriteAttribution("aborted", "destroyed_before_outcome");
        attributionClosed = true; try { attributionWriter?.Dispose(); } catch { }
    }
}

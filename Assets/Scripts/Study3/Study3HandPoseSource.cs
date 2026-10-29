using System;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

/// <summary>Copies MRTK update events. Readers share an application update ID, not a sensor timestamp.</summary>
public sealed class Study3HandPoseSource : MonoBehaviour, IMixedRealityHandJointHandler, IMixedRealitySourceStateHandler
{
    /// <summary>Each MRTK callback for the selected hand, copied before delivery.</summary>
    public static event Action<Study3JointSample> JointSampleUpdated;
    /// <summary>Runs after all motion consumers processed the same callback.</summary>
    public static event Action<Study3JointSample> JointSampleProcessed;
    /// <summary>Publishes gesture transitions after contact state has been updated.</summary>
    public static event Action<Study3JointSample> JointSampleGesture;
    /// <summary>Runs after component contact state machines processed the current sample.</summary>
    public static event Action<Study3JointSample> JointSampleEvaluated;
    /// <summary>Raised when the selected source can no longer be used.</summary>
    public static event Action<uint> SelectedSourceLost;
    private static Study3HandPoseSource instance;
    private readonly Dictionary<uint, Study3MotionSample> samples = new Dictionary<uint, Study3MotionSample>();
    private readonly Dictionary<uint, long> epochs = new Dictionary<uint, long>();
    private static long nextEpoch;
    private IMixedRealityInputSystem registeredSystem;
    private long sequence;
    private uint? selectedSource;

    public static void EnsureAvailable()
    {
        if (instance != null) return;
        var root = new GameObject("Study3 shared hand updates");
        instance = root.AddComponent<Study3HandPoseSource>();
        DontDestroyOnLoad(root);
    }

    public static bool TryGetSample(out Study3MotionSample sample)
    {
        EnsureAvailable(); instance.Register();
        double now = Time.realtimeSinceStartupAsDouble;
        if (instance.selectedSource.HasValue
            && instance.samples.TryGetValue(instance.selectedSource.Value, out sample)
            && instance.IsTracked(sample.SourceId)
            && now - sample.Timestamp <= Study3MotionTracker.MaximumAgeSeconds) return true;
        instance.InvalidateSelectedSource();
        // Start with the same controller enumeration used by HandJointUtils.Any;
        // retain that source until it is lost rather than mixing two hands.
        if (instance.registeredSystem?.DetectedControllers != null)
        {
            foreach (var controller in instance.registeredSystem.DetectedControllers)
            {
                if (!(controller is IMixedRealityHand) || !controller.IsPositionAvailable || controller.InputSource == null
                    || !instance.samples.TryGetValue(controller.InputSource.SourceId, out sample)
                    || now - sample.Timestamp > Study3MotionTracker.MaximumAgeSeconds) continue;
                instance.selectedSource = sample.SourceId;
                return true;
            }
        }
        sample = default; return false;
    }

    private void OnEnable() => Register();
    private void Update() => Register();
    private bool IsTracked(uint sourceId)
    {
        if (registeredSystem?.DetectedControllers == null) return false;
        foreach (var controller in registeredSystem.DetectedControllers)
            if (controller.InputSource?.SourceId == sourceId)
                return controller is IMixedRealityHand && controller.IsPositionAvailable;
        return false;
    }
    private void Register()
    {
        var input = CoreServices.InputSystem;
        if (ReferenceEquals(input, registeredSystem)) return;
        Unregister(); registeredSystem = input;
        if (registeredSystem == null) return;
        registeredSystem.RegisterHandler<IMixedRealityHandJointHandler>(this);
        registeredSystem.RegisterHandler<IMixedRealitySourceStateHandler>(this);
    }

    private void Unregister()
    {
        InvalidateSelectedSource();
        foreach (uint id in samples.Keys) Study3HandSamplingDiagnostics.SourceEvent(id, "provider_unregistered");
        if (registeredSystem != null)
        {
            registeredSystem.UnregisterHandler<IMixedRealityHandJointHandler>(this);
            registeredSystem.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
        }
        registeredSystem = null; samples.Clear(); epochs.Clear(); selectedSource = null;
    }

    private void InvalidateSelectedSource()
    {
        if (!selectedSource.HasValue) return;
        uint sourceId = selectedSource.Value;
        selectedSource = null;
        SelectedSourceLost?.Invoke(sourceId);
    }

    public void OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
    {
        uint id = eventData.InputSource.SourceId;
        if (eventData.InputData == null || !eventData.InputData.TryGetValue(TrackedHandJoint.IndexTip, out MixedRealityPose pose))
        {
            Study3HandSamplingDiagnostics.ProviderUpdate(eventData, 0);
            if (selectedSource == id) InvalidateSelectedSource();
            samples.Remove(id); epochs.Remove(id); return;
        }
        if (!epochs.ContainsKey(id)) epochs[id] = ++nextEpoch;
        samples[id] = new Study3MotionSample(id, ++sequence, Time.realtimeSinceStartupAsDouble, pose.Position, epochs[id]);
        Study3HandSamplingDiagnostics.ProviderUpdate(eventData, sequence, samples[id].Timestamp);
        // Select a hand with the same sticky source policy as existing readers.
        if (TryGetSample(out Study3MotionSample selected) && selected.SourceId == id)
        {
            Study3JointSample jointSample = new Study3JointSample(id, epochs[id], sequence,
                samples[id].Timestamp, eventData.InputData);
            JointSampleUpdated?.Invoke(jointSample);
            JointSampleProcessed?.Invoke(jointSample);
            JointSampleGesture?.Invoke(jointSample);
            JointSampleEvaluated?.Invoke(jointSample);
        }
    }
    public void OnSourceDetected(SourceStateEventData eventData)
        => Study3HandSamplingDiagnostics.SourceEvent(eventData.InputSource.SourceId, "source_detected");
    public void OnSourceLost(SourceStateEventData eventData)
    {
        uint id = eventData.InputSource.SourceId;
        Study3HandSamplingDiagnostics.SourceEvent(id, "source_lost");
        if (selectedSource == id) InvalidateSelectedSource();
        samples.Remove(id); epochs.Remove(id);
    }
    private void OnDisable() => Unregister();
    private void OnDestroy() { Unregister(); if (instance == this) instance = null; }
}

// Sampling baseline diagnostics import verification (2026-10-28); no selection rule change.

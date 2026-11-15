using System.Collections;
using System.Collections.Generic;
using System;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;
using UnityEngine.XR;

/// <summary>Keep the Study1 layout at its scene transform and record tracking-space resets.</summary>
public sealed class Study1HeadAnchor : MonoBehaviour
{
    [Tooltip("Start-to-target sphere centre distance in metres, applied when the scene starts.")]
    [Min(0f)] public float ballCenterDistanceMeters = 0.36f;
    public SphereCollider targetBall;
    public SphereCollider startBall;
    public bool HasPlacement { get; private set; }
    public double PlacementRealtime { get; private set; }
    public bool PlacementHeadPoseValid { get; private set; }
    public Vector3 PlacementHeadPosition { get; private set; }
    public Vector3 PlacementHeadForward { get; private set; }
    public Quaternion PlacementHeadRotation { get; private set; }
    public Vector3 PlacementTargetPosition { get; private set; }
    public Quaternion PlacementTargetRotation { get; private set; }
    public bool PlacementWasTracked { get; private set; }
    public Vector3 PlacementTrackingPosition { get; private set; }
    public Quaternion PlacementTrackingRotation { get; private set; }
    public Vector3 PlacementTrackingOriginPosition { get; private set; }
    public Quaternion PlacementTrackingOriginRotation { get; private set; }
    public bool PlacementCameraPoseValid { get; private set; }
    public Vector3 PlacementCameraPosition { get; private set; }
    public Quaternion PlacementCameraRotation { get; private set; }
    /// <summary>Incremented on each accepted reference-space reset, before new samples are recorded.</summary>
    public int ReferenceSpaceEpoch { get; private set; }
    public event Action ReferenceSpaceResetStarted;
    public event Action ReferenceSpaceResetCompleted;
    private GameObject[] children;
    private bool[] initiallyActive;
    private bool resetting;
    private double lastResetRequest = double.NegativeInfinity;
    private readonly List<XRInputSubsystem> observedSubsystems = new List<XRInputSubsystem>();
    private readonly List<XRInputSubsystem> currentSubsystems = new List<XRInputSubsystem>();
#if UNITY_ANDROID && !UNITY_EDITOR
    private int? previousRecenterCount;
#endif

    private void Awake()
    {
        ApplyBallSpacing();
        // Prevent a partially positioned layout from triggering or recording a trial.
        children = new GameObject[transform.childCount];
        initiallyActive = new bool[children.Length];
        for (int i = 0; i < children.Length; i++)
        {
            children[i] = transform.GetChild(i).gameObject;
            initiallyActive[i] = children[i].activeSelf;
            if (initiallyActive[i]) children[i].SetActive(false);
        }
    }

    private void ApplyBallSpacing()
    {
        if (targetBall == null || startBall == null)
        {
            Debug.LogError("Study1: assign both ball colliders to configure their centre distance.");
            return;
        }
        Vector3 targetCenter = targetBall.transform.TransformPoint(targetBall.center);
        Vector3 startCenter = startBall.transform.TransformPoint(startBall.center);
        Vector3 desiredCenter = targetCenter - transform.forward * Mathf.Max(0f, ballCenterDistanceMeters);
        startBall.transform.position += desiredCenter - startCenter;
    }

    private void OnEnable() { RefreshSubsystems(); }

    private void OnDisable()
    {
        foreach (var subsystem in observedSubsystems) subsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
        observedSubsystems.Clear();
#if UNITY_ANDROID && !UNITY_EDITOR
        previousRecenterCount = null;
#endif
    }

    private void Update()
    {
        RefreshSubsystems();
#if UNITY_ANDROID && !UNITY_EDITOR
        // OVRDisplay itself uses this counter to detect Quest system recentering.
        if (OVRPlugin.initialized)
        {
            int current = OVRPlugin.GetLocalTrackingSpaceRecenterCount();
            if (previousRecenterCount.HasValue && current != previousRecenterCount.Value)
                BeginReferenceSpaceReset();
            previousRecenterCount = current;
        }
        else previousRecenterCount = null;
#endif
    }

    private void RefreshSubsystems()
    {
        currentSubsystems.Clear();
        SubsystemManager.GetInstances(currentSubsystems);
        for (int i = observedSubsystems.Count - 1; i >= 0; --i)
        {
            if (currentSubsystems.Contains(observedSubsystems[i])) continue;
            observedSubsystems[i].trackingOriginUpdated -= OnTrackingOriginUpdated;
            observedSubsystems.RemoveAt(i);
        }
        foreach (var subsystem in currentSubsystems)
        {
            if (observedSubsystems.Contains(subsystem)) continue;
            observedSubsystems.Add(subsystem);
            subsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
        }
    }

    private void OnTrackingOriginUpdated(XRInputSubsystem subsystem) { BeginReferenceSpaceReset(); }

    private void BeginReferenceSpaceReset()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        // A single physical recenter can be reported by both APIs on adjacent frames.
        if (!isActiveAndEnabled || !HasPlacement || resetting || now - lastResetRequest < .5) return;
        lastResetRequest = now;
        resetting = true;
        ReferenceSpaceResetStarted?.Invoke();
        foreach (var experiment in GetComponentsInChildren<ExperimentCounter>(true))
            if (experiment.isActiveAndEnabled) experiment.OnReferenceSpaceReset();
        ReferenceSpaceEpoch++;
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null) continue;
            initiallyActive[i] = children[i].activeSelf;
            if (initiallyActive[i]) children[i].SetActive(false);
        }
        HasPlacement = false;
        StartCoroutine(WaitAndPlace(true));
    }

    private IEnumerator Start() { return WaitAndPlace(false); }

    private IEnumerator WaitAndPlace(bool afterReset)
    {
        // Keep the scene's fixed world transform, as Study3's Spawner does. Wait for
        // XR to settle only to take a useful diagnostic snapshot, never to position it.
        yield return null;
        yield return null;
        Camera camera = Camera.main;
        HeadPose pose = default;
        PlacementRealtime = Time.realtimeSinceStartupAsDouble;
        PlacementHeadPoseValid = camera != null && TryGetHeadPose(camera, out pose);
        PlacementWasTracked = PlacementHeadPoseValid && pose.tracked;
        PlacementHeadPosition = pose.worldPosition;
        PlacementHeadRotation = pose.worldRotation;
        PlacementHeadForward = PlacementHeadPoseValid ? pose.worldRotation * Vector3.forward : default;
        PlacementTrackingPosition = pose.trackingPosition;
        PlacementTrackingRotation = pose.trackingRotation;
        PlacementTrackingOriginPosition = pose.originPosition;
        PlacementTrackingOriginRotation = pose.originRotation;
        PlacementCameraPoseValid = false;
        if (camera != null)
        {
            PlacementCameraPosition = camera.transform.position;
            PlacementCameraRotation = camera.transform.rotation;
            PlacementCameraPoseValid = Finite(PlacementCameraPosition) && Finite(PlacementCameraRotation);
        }
        PlacementTargetPosition = transform.position;
        PlacementTargetRotation = transform.rotation;
        HasPlacement = true;
        for (int i = 0; i < children.Length; i++)
            if (children[i] != null && initiallyActive[i]) children[i].SetActive(true);
        resetting = false;
        if (afterReset) ReferenceSpaceResetCompleted?.Invoke();
        Debug.LogFormat("Study1: fixed target at {0}, head pose available {1}, tracked {2}, head {3}.",
            transform.position, PlacementHeadPoseValid, PlacementWasTracked, PlacementHeadPosition);
    }

    private struct HeadPose
    {
        public bool tracked;
        public Vector3 worldPosition, trackingPosition, originPosition;
        public Quaternion worldRotation, trackingRotation, originRotation;
    }

    private static bool TryGetHeadPose(Camera camera, out HeadPose pose)
    {
        pose = default;
#if UNITY_EDITOR
        // Allow desktop scene preview when no XR runtime is in use.
        if (!XRSettings.enabled)
        {
            pose.worldPosition = camera.transform.position;
            pose.worldRotation = camera.transform.rotation;
            return PoseReady(pose.worldPosition, pose.worldRotation);
        }
#endif
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        bool tracked;
        Vector3 trackedPosition;
        Quaternion trackedRotation;
        if (!head.isValid || !head.TryGetFeatureValue(CommonUsages.isTracked, out tracked) || !tracked
            || !head.TryGetFeatureValue(CommonUsages.devicePosition, out trackedPosition) || !Finite(trackedPosition)
            || !head.TryGetFeatureValue(CommonUsages.deviceRotation, out trackedRotation) || !Finite(trackedRotation))
            return false;

        // MRTK applies this same playspace to hand joints. The camera may be reparented
        // under an Oculus tracking rig, so its immediate parent is not the XR origin.
        Transform origin = MixedRealityPlayspace.Transform;
        pose.tracked = true;
        pose.trackingPosition = trackedPosition;
        pose.trackingRotation = trackedRotation;
        pose.originPosition = origin.position;
        pose.originRotation = origin.rotation;
        pose.worldPosition = origin.TransformPoint(trackedPosition);
        pose.worldRotation = origin.rotation * trackedRotation;
        return PoseReady(pose.worldPosition, pose.worldRotation);
    }

    private static bool PoseReady(Vector3 position, Quaternion rotation)
    {
        if (!Finite(position) || !Finite(rotation)) return false;
        Vector3 forward = rotation * Vector3.forward;
        return Finite(forward)
            && Vector3.ProjectOnPlane(forward, Vector3.up).sqrMagnitude > .000001f;
    }

    private static bool Finite(Vector3 p)
    {
        return !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y)
            && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }

    private static bool Finite(Quaternion q)
    {
        return !float.IsNaN(q.x) && !float.IsInfinity(q.x) && !float.IsNaN(q.y) && !float.IsInfinity(q.y)
            && !float.IsNaN(q.z) && !float.IsInfinity(q.z) && !float.IsNaN(q.w) && !float.IsInfinity(q.w)
            && q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > .000001f;
    }
}

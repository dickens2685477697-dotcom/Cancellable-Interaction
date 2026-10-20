using System;
using UnityEditor;
using UnityEngine;

/// <summary>Regression checks runnable from the menu or Unity's -executeMethod.</summary>
public static class DynamicStopValidation
{
    private static readonly DynamicStopMotionSettings Defaults =
        new DynamicStopMotionSettings(16f, 0.02f, 0.12f, 0.1f, 0.5f, 4f, 3f, 0.18f);

    [MenuItem("Study3/Validate Dynamic Stop")]
    public static void Run()
    {
        StationaryContactAndLatch();
        FastMotionDoesNotSelect();
        SuddenDecelerationNeedsAnotherMeasurement();
        SmallJitterCanSelect();
        BacktrackingPreservesPathLength();
        TurningContributesAcceleration();
        ActualElapsedTimeAndMinimumInterval();
        RecentSpeedDecaysAndThresholdsAreCapped();
        ResetDiscardsTrackingHistory();
        InvalidSettingsAreClamped();
        SphereBoundaryMatchesVisibleRegion();
        SharedSettingsAndInspectorProperties();
        Debug.Log("Dynamic Stop validation passed: 12 regression checks.");
    }

    private static void StationaryContactAndLatch()
    {
        var motion = new DynamicStopMotionTracker();
        Require(!motion.Sample(Vector3.zero, true, 0, Defaults), "Initial position must not select.");
        Require(!motion.Sample(Vector3.zero, true, 0.02, Defaults), "First velocity segment has no acceleration.");
        Require(!motion.HasAcceleration, "Acceleration must remain unavailable after the first segment.");
        Require(motion.Sample(Vector3.zero, true, 0.04, Defaults), "Stationary contact should select on the next measurement.");
        Require(!motion.Sample(Vector3.right, true, 0.06, Defaults) && motion.IsActive,
            "Moving after selection must keep the latch without selecting twice.");
        Require(!motion.Sample(Vector3.right, false, 0.061, Defaults) && !motion.IsActive,
            "Exit must reset the latch even before the next measurement interval.");
    }

    private static void FastMotionDoesNotSelect()
    {
        var motion = new DynamicStopMotionTracker();
        for (int i = 0; i <= 30; i++)
        {
            Require(!motion.Sample(Vector3.right * (i * 0.02f), true, i * 0.02, Defaults),
                "Continuous fast motion must not select.");
        }
    }

    private static void SuddenDecelerationNeedsAnotherMeasurement()
    {
        var motion = new DynamicStopMotionTracker();
        motion.Sample(Vector3.zero, true, 0, Defaults);
        motion.Sample(Vector3.right * 0.02f, true, 0.02, Defaults);
        motion.Sample(Vector3.right * 0.04f, true, 0.04, Defaults);
        Require(!motion.Sample(Vector3.right * 0.04f, true, 0.06, Defaults),
            "Low speed during abrupt deceleration must not bypass acceleration.");
        Require(motion.SpeedConditionMet && !motion.AccelerationConditionMet, "Only speed should pass during braking.");
        Require(motion.Sample(Vector3.right * 0.04f, true, 0.08, Defaults), "Stable stop should select without a dwell timer.");
        motion.Sample(Vector3.right * 0.04f, false, 0.081, Defaults);
        Require(motion.Sample(Vector3.right * 0.04f, true, 0.10, Defaults), "Re-entry should permit a fresh selection.");
    }

    private static void SmallJitterCanSelect()
    {
        var motion = new DynamicStopMotionTracker();
        motion.Sample(Vector3.zero, true, 0, Defaults);
        motion.Sample(Vector3.right * 0.0001f, true, 0.02, Defaults);
        Require(motion.Sample(Vector3.zero, true, 0.04, Defaults), "Small nonzero jitter below both thresholds should select.");
    }

    private static void BacktrackingPreservesPathLength()
    {
        var motion = new DynamicStopMotionTracker();
        motion.Sample(Vector3.zero, true, 0, Defaults);
        motion.Sample(Vector3.right * 0.01f, true, 0.005, Defaults);
        motion.Sample(Vector3.zero, true, 0.010, Defaults);
        motion.Sample(Vector3.right * 0.01f, true, 0.015, Defaults);
        Require(!motion.Sample(Vector3.zero, true, 0.020, Defaults), "Backtracking must not count as a stop.");
        Near(motion.Speed, 2f, "Observed path length must not cancel at an identical endpoint.");
    }

    private static void TurningContributesAcceleration()
    {
        var motion = new DynamicStopMotionTracker();
        motion.Sample(Vector3.zero, true, 0, Defaults);
        motion.Sample(Vector3.right * 0.02f, true, 0.02, Defaults);
        motion.Sample(Vector3.zero, true, 0.04, Defaults);
        Near(motion.Speed, 1f, "Turning should preserve the scalar speed.");
        Near(motion.Acceleration, 100f, "Velocity reversal must contribute vector acceleration.");
    }

    private static void ActualElapsedTimeAndMinimumInterval()
    {
        var motion = new DynamicStopMotionTracker();
        motion.Sample(Vector3.zero, false, 0, Defaults);
        motion.Sample(Vector3.right * 0.01f, false, 0.01, Defaults);
        Require(motion.Speed == 0f, "Subinterval movement should accumulate without measuring.");
        motion.Sample(Vector3.right * 0.025f, false, 0.025, Defaults);
        Near(motion.Speed, 1f, "Speed must use the actual 25ms elapsed time.");
        motion.Sample(Vector3.right * 0.058f, false, 0.058, Defaults);
        Near(motion.Speed, 1f, "A different refresh interval must yield the same physical speed.");
    }

    private static void RecentSpeedDecaysAndThresholdsAreCapped()
    {
        var motion = new DynamicStopMotionTracker();
        motion.Sample(Vector3.zero, false, 0, Defaults);
        motion.Sample(Vector3.right * 0.2f, false, 0.02, Defaults);
        Near(motion.SpeedLimit, Defaults.SpeedMax, "Speed threshold must respect its cap.");
        Near(motion.AccelLimit, Defaults.AccelMax, "Acceleration threshold must respect its cap.");
        motion.Sample(Vector3.right * 0.2f, false, 0.04, Defaults);
        Near(motion.RecentSpeed, 10f * (float)Math.Exp(-0.02 / 0.18), "Recent high speed must decay exponentially.");
        Require(!motion.IsActive, "Motion history outside the sphere must not select.");
        motion.Sample(Vector3.right * 0.2f, false, 4.04, Defaults);
        Near(motion.SpeedLimit, Defaults.SpeedMin, "Speed threshold should decay back to its nonzero floor.");
        Near(motion.AccelLimit, Defaults.AccelMin, "Acceleration threshold should decay back to its nonzero floor.");
    }

    private static void ResetDiscardsTrackingHistory()
    {
        var motion = new DynamicStopMotionTracker();
        motion.Sample(Vector3.zero, true, 0, Defaults);
        motion.Sample(Vector3.zero, true, 0.02, Defaults);
        motion.Sample(Vector3.zero, true, 0.04, Defaults);
        motion.Reset();
        Require(!motion.IsActive && !motion.IsInside && !motion.HasAcceleration, "Tracking reset must clear all contact state.");
        Require(!motion.Sample(Vector3.one, true, 10, Defaults), "Reacquiring tracking must establish a new baseline.");
        Require(!motion.Sample(Vector3.one, true, 10.02, Defaults), "Reacquisition must collect fresh acceleration history.");
        Require(motion.Sample(Vector3.one, true, 10.04, Defaults), "Fresh stationary samples should select after reacquisition.");
    }

    private static void InvalidSettingsAreClamped()
    {
        var settings = new DynamicStopMotionSettings(0, -1, -2, -1, -1, -2, -1, 0);
        Require(settings.SampleSeconds > 0 && settings.HistorySeconds > 0, "Time parameters must stay positive.");
        Require(settings.SpeedMin > 0 && settings.SpeedMax >= settings.SpeedMin, "Speed bounds must be ordered and nonzero.");
        Require(settings.AccelMin > 0 && settings.AccelMax >= settings.AccelMin, "Acceleration bounds must be ordered and nonzero.");
        Require(settings.SpeedGain == 0 && settings.AccelGain == 0, "Negative gains must be clamped.");
    }

    private static void SphereBoundaryMatchesVisibleRegion()
    {
        var sphereObject = new GameObject("DynamicStopValidationSphere");
        try
        {
            SphereCollider sphere = sphereObject.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            sphereObject.transform.position = new Vector3(1, 2, 3);
            sphereObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Require(DynamicStopMotionTracker.ContainsPoint(sphere, sphereObject.transform.position), "Sphere center must be inside.");
            Require(!DynamicStopMotionTracker.ContainsPoint(sphere, sphereObject.transform.position + new Vector3(0.04f, 0.04f, 0)),
                "Bounding-box corners outside the sphere must not count as contact.");
            sphere.enabled = false;
            Require(!DynamicStopMotionTracker.ContainsPoint(sphere, sphereObject.transform.position), "Disabled colliders must not accept contact.");
        }
        finally { UnityEngine.Object.DestroyImmediate(sphereObject); }
    }

    private static void SharedSettingsAndInspectorProperties()
    {
        var hostObject = new GameObject("DynamicStopValidationHost");
        Study3SelectionSettings settings = ScriptableObject.CreateInstance<Study3SelectionSettings>();
        try
        {
            BallSpawner host = hostObject.AddComponent<BallSpawner>();
            settings.dynamicStopSampleMs = 20;
            settings.dynamicStopSpeedMin = 0.03f;
            settings.dynamicStopSpeedMax = 0.2f;
            settings.dynamicStopSpeedGain = 0.2f;
            settings.dynamicStopAccelMin = 0.7f;
            settings.dynamicStopAccelMax = 5;
            settings.dynamicStopAccelGain = 4;
            settings.dynamicStopHistorySeconds = 0.25f;
            host.SelectionSettings = settings;
            host.ApplySelectionSettings();
            DynamicStopMotionSettings applied = host.GetDynamicStopMotionSettings();
            Near(applied.SampleSeconds, 0.02f, "Shared sample interval must apply.");
            Near(applied.SpeedMin, 0.03f, "Shared speed floor must apply.");
            Near(applied.SpeedMax, 0.2f, "Shared speed cap must apply.");
            Near(applied.SpeedGain, 0.2f, "Shared speed gain must apply.");
            Near(applied.AccelMin, 0.7f, "Shared acceleration floor must apply.");
            Near(applied.AccelMax, 5f, "Shared acceleration cap must apply.");
            Near(applied.AccelGain, 4f, "Shared acceleration gain must apply.");
            Near(applied.HistorySeconds, 0.25f, "Shared speed history must apply.");
            var hostProperties = new SerializedObject(host);
            var settingsProperties = new SerializedObject(settings);
            foreach (string field in new[] { "dynamicStopSampleMs", "dynamicStopSpeedMin", "dynamicStopSpeedMax", "dynamicStopSpeedGain",
                "dynamicStopAccelMin", "dynamicStopAccelMax", "dynamicStopAccelGain", "dynamicStopHistorySeconds" })
            {
                Require(hostProperties.FindProperty(field) != null && settingsProperties.FindProperty(field) != null,
                    "Inspector property is missing: " + field);
            }
            Require((int)BallSpawner.Mode.dynamicStop == 30, "New mode must preserve existing enum values.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hostObject);
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Dynamic Stop validation failed: " + message);
    }

    private static void Near(float actual, float expected, string message)
    {
        Require(Math.Abs(actual - expected) < 0.0001f, message + " actual=" + actual + " expected=" + expected);
    }
}

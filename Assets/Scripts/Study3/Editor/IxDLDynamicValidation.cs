using System;
using UnityEditor;
using UnityEngine;

/// <summary>Checks the previous-sample mapping and fixed-step selection gate.</summary>
public static class IxDLDynamicValidation
{
    private static readonly IxDLDynamicMotionSettings Defaults =
        new IxDLDynamicMotionSettings(0.375f, 0f, 0.6f, 0.03f, 0.06f);

    [MenuItem("Study3/Validate IxDL Dynamic")]
    public static void Run()
    {
        RunMotionChecks();
        RunTriggerLogChecks();
        SharedSettingsAndModeRegistration();
        Debug.Log("IxDL Dynamic validation passed: 10 motion checks, 2 trigger record checks and shared settings/mode registration.");
    }

    public static void RunMotionChecks()
    {
        MappingEndpointsAndClamping();
        RequiresPositionAndTwoSpeedSamples();
        PreviousSpeedDeterminesLimit();
        AccelerationGateBlocksSpeedAlone();
        StrictThresholdBoundaries();
        RisingEdgeAndRearming();
        OutsideCannotSelectAndReentryRearms();
        ResetDiscardsTrackingHistory();
        SamplingRateUsesFixedDeltaTime();
        InvalidDeltaTimeDiscardsHistory();
    }

    private static void MappingEndpointsAndClamping()
    {
        Near(Defaults.GetSpeedLimit(0f), 0.03f, "Mapping start");
        Near(Defaults.GetSpeedLimit(0.3f), 0.045f, "Mapping midpoint");
        Near(Defaults.GetSpeedLimit(0.6f), 0.06f, "Mapping end");
        Near(Defaults.GetSpeedLimit(1.2f), 0.06f, "Upper clamp");
        Near(Defaults.GetSpeedLimit(-1f), 0.03f, "Lower clamp");
    }

    private static void RequiresPositionAndTwoSpeedSamples()
    {
        var motion = new IxDLDynamicMotionTracker();
        Require(!motion.Sample(Vector3.zero, true, 0.02f, Defaults), "Initial pose must not select.");
        Require(!motion.Sample(Vector3.zero, true, 0.02f, Defaults), "First speed has no acceleration history.");
        Require(!motion.HasAcceleration, "Acceleration should still be unavailable.");
        Require(motion.Sample(Vector3.zero, true, 0.02f, Defaults), "Stationary third pose should select.");
    }

    private static void PreviousSpeedDeterminesLimit()
    {
        var motion = new IxDLDynamicMotionTracker();
        motion.Sample(Vector3.zero, false, 0.1f, Defaults);
        motion.Sample(Vector3.right * 2f, false, 0.1f, Defaults);
        Require(motion.Sample(Vector3.right * 2.005f, true, 0.1f, Defaults),
            "Deceleration to 0.05 m/s must pass the prior 20 m/s sample's capped limit.");
        Near(motion.PreviousSpeed, 20f, "Prior speed must survive the update");
        Near(motion.SpeedLimit, 0.06f, "Limit must use prior speed");
        Require(motion.Speed > Defaults.GetSpeedLimit(motion.Speed),
            "This case must distinguish previous-speed mapping from same-sample mapping.");
        Require(motion.Acceleration < 0f, "Deceleration must remain signed.");
    }

    private static void AccelerationGateBlocksSpeedAlone()
    {
        var motion = new IxDLDynamicMotionTracker();
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        Require(!motion.Sample(Vector3.right * 0.0004f, true, 0.02f, Defaults), "Acceleration above 0.375 must block selection.");
        Require(motion.SpeedConditionMet && !motion.AccelerationConditionMet, "Speed alone must not select.");
    }

    private static void StrictThresholdBoundaries()
    {
        var motion = new IxDLDynamicMotionTracker();
        var accelerationBoundary = new IxDLDynamicMotionSettings(0.125f, 0, 10, 0.5f, 0.5f);
        motion.Sample(Vector3.zero, true, 1f, accelerationBoundary);
        motion.Sample(Vector3.zero, true, 1f, accelerationBoundary);
        Require(!motion.Sample(Vector3.right * 0.125f, true, 1f, accelerationBoundary),
            "Equality at the acceleration threshold must not pass.");
        motion.Reset();
        motion.Sample(Vector3.zero, true, 1f, accelerationBoundary);
        motion.Sample(Vector3.right * 0.5f, true, 1f, accelerationBoundary);
        Require(!motion.Sample(Vector3.right, true, 1f, accelerationBoundary),
            "Equality at the speed threshold must not pass.");
        Require(motion.AccelerationConditionMet && !motion.SpeedConditionMet, "Speed boundary must be strict.");
    }

    private static void RisingEdgeAndRearming()
    {
        var motion = new IxDLDynamicMotionTracker();
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        Require(motion.Sample(Vector3.zero, true, 0.02f, Defaults), "First false-to-true transition must select.");
        Require(!motion.Sample(Vector3.zero, true, 0.02f, Defaults), "Continuous true must not select twice.");
        Require(!motion.Sample(Vector3.right * 0.1f, true, 0.02f, Defaults), "Fast motion must rearm the gate.");
        Require(motion.Sample(Vector3.right * 0.1f, true, 0.02f, Defaults), "A later false-to-true transition must select.");
    }

    private static void OutsideCannotSelectAndReentryRearms()
    {
        var motion = new IxDLDynamicMotionTracker();
        for (int i = 0; i < 4; i++)
            Require(!motion.Sample(Vector3.zero, false, 0.02f, Defaults), "Outside motion must not select.");
        Require(motion.Sample(Vector3.zero, true, 0.02f, Defaults), "Entry may use valid motion history from outside.");
        Require(!motion.Sample(Vector3.zero, false, 0.02f, Defaults) && !motion.IsActive, "Exit must clear contact state.");
        Require(motion.Sample(Vector3.zero, true, 0.02f, Defaults), "Reentry must permit another selection.");
    }

    private static void ResetDiscardsTrackingHistory()
    {
        var motion = new IxDLDynamicMotionTracker();
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        motion.Reset();
        Require(!motion.IsInside && !motion.IsActive && !motion.HasAcceleration && !motion.ConditionsMet, "Reset must clear history and contact.");
        Require(!motion.Sample(Vector3.one, true, 0.02f, Defaults), "Reacquisition must establish a fresh baseline.");
        Require(!motion.Sample(Vector3.one, true, 0.02f, Defaults), "Reacquisition must collect fresh speed history.");
        Require(motion.Sample(Vector3.one, true, 0.02f, Defaults), "Fresh stationary history should select.");
    }

    private static void SamplingRateUsesFixedDeltaTime()
    {
        foreach (float dt in new[] { 0.02f, 0.01f })
        {
            var motion = new IxDLDynamicMotionTracker();
            motion.Sample(Vector3.zero, true, dt, Defaults);
            motion.Sample(Vector3.right * (0.02f * dt), true, dt, Defaults);
            Require(motion.Sample(Vector3.right * (0.04f * dt), true, dt, Defaults), "Constant slow motion should pass at both rates.");
            Near(motion.Speed, 0.02f, "Physical speed must account for timestep");
            Near(motion.SpeedLimit, 0.031f, "Prior-speed mapping must be consistent at both rates");
        }
    }

    private static void InvalidDeltaTimeDiscardsHistory()
    {
        foreach (float dt in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            var motion = new IxDLDynamicMotionTracker();
            motion.Sample(Vector3.zero, true, 0.02f, Defaults);
            motion.Sample(Vector3.zero, true, 0.02f, Defaults);
            Require(!motion.Sample(Vector3.zero, true, dt, Defaults) && !motion.IsInside, "Invalid timestep must reset without selecting.");
            Require(!motion.Sample(Vector3.one, true, 0.02f, Defaults), "A new baseline is required after an invalid step.");
        }
    }

    public static void RunTriggerLogChecks()
    {
        TriggerRecordSurvivesMotionReset();
        TriggerCsvPreservesZeroAndCulture();
    }

    private static void TriggerRecordSurvivesMotionReset()
    {
        var motion = new IxDLDynamicMotionTracker();
        motion.Sample(Vector3.zero, false, 0.02f, Defaults);
        motion.Sample(Vector3.right * 0.006f, false, 0.02f, Defaults);
        Require(motion.Sample(Vector3.right * 0.0068f, true, 0.02f, Defaults), "Deceleration must generate a trigger to record.");
        var record = new IxDLDynamicTriggerRecord(motion);
        string csv = record.ToCsv();
        Near(record.TriggerSpeed, 0.04f, "Captured trigger speed");
        Near(record.PreviousSpeed, 0.3f, "Captured prior speed");
        Near(record.SpeedThreshold, 0.045f, "Captured dynamic threshold");
        Near(record.TriggerAcceleration, -13f, "Captured signed acceleration");
        Near(record.AccelerationThreshold, 0.375f, "Captured acceleration threshold");

        motion.Reset();
        motion.Sample(Vector3.one, true, 0.02f, Defaults);
        motion.Sample(Vector3.one, true, 0.02f, Defaults);
        motion.Sample(Vector3.one, true, 0.02f, Defaults);
        Require(record.ToCsv() == csv, "Later motion or object reset must not replace the original decision values.");
    }

    private static void TriggerCsvPreservesZeroAndCulture()
    {
        var motion = new IxDLDynamicMotionTracker();
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        motion.Sample(Vector3.zero, true, 0.02f, Defaults);
        Require(motion.Sample(Vector3.zero, true, 0.02f, Defaults), "Stationary contact must produce a zero-speed trigger.");
        var record = new IxDLDynamicTriggerRecord(motion);
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            string[] columns = record.ToCsv().Split(',');
            Require(columns.Length == IxDLDynamicTriggerRecord.CsvHeader.Split(',').Length, "Decimal commas must not add CSV columns.");
            Near(float.Parse(columns[0], System.Globalization.CultureInfo.InvariantCulture), 0f, "Zero trigger speed must be recorded");
            Near(float.Parse(columns[1], System.Globalization.CultureInfo.InvariantCulture), 0f, "Zero previous speed must be recorded");
            Near(float.Parse(columns[2], System.Globalization.CultureInfo.InvariantCulture), 0.03f, "Threshold decimal separator");
            Near(float.Parse(columns[3], System.Globalization.CultureInfo.InvariantCulture), 0f, "Zero acceleration must be recorded");
            string[] missing = IxDLDynamicTriggerRecord.MissingCsv.Split(',');
            Require(missing.Length == columns.Length && Array.TrueForAll(missing, value => value == "NA"),
                "Missing triggers must be distinguishable from valid zero-speed triggers.");
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = originalCulture; }
    }

    private static void SharedSettingsAndModeRegistration()
    {
        var hostObject = new GameObject("IxDLDynamicValidationHost");
        var settings = ScriptableObject.CreateInstance<Study3SelectionSettings>();
        try
        {
            BallSpawner host = hostObject.AddComponent<BallSpawner>();
            settings.ixdlAccelerationThreshold = 0.25f;
            settings.ixdlInputSpeedMin = 1f;
            settings.ixdlInputSpeedMax = 3f;
            settings.ixdlSpeedThresholdMin = 0.2f;
            settings.ixdlSpeedThresholdMax = 0.6f;
            host.SelectionSettings = settings;
            host.ApplySelectionSettings();
            IxDLDynamicMotionSettings applied = host.GetIxDLDynamicMotionSettings();
            Near(applied.AccelerationLimit, 0.25f, "Shared acceleration limit");
            Near(applied.GetSpeedLimit(2f), 0.4f, "Shared mapping parameters");
            foreach (string field in new[] { "ixdlAccelerationThreshold", "ixdlInputSpeedMin", "ixdlInputSpeedMax", "ixdlSpeedThresholdMin", "ixdlSpeedThresholdMax" })
            {
                Require(new SerializedObject(host).FindProperty(field) != null
                    && new SerializedObject(settings).FindProperty(field) != null, "Missing Inspector field: " + field);
            }
            Require((int)BallSpawner.Mode.ixdlDynamic == 31 && (int)BallSpawner.Mode.dynamicStop == 30
                && (int)BallSpawner.Mode.pinchWristTilt == 17, "Existing serialized mode values must remain unchanged.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hostObject);
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Near(float actual, float expected, string message)
    {
        Require(Math.Abs(actual - expected) < 0.0001f, message + ": " + actual + " != " + expected);
    }
}

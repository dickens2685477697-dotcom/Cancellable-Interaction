using System;
using UnityEditor;
using UnityEngine;

public static class IxDLDynamicValidation
{
    [MenuItem("Study3/Validate IxDL Dynamic")]
    public static void Run()
    {
        int count = CommonMotionValidation.RunMotionChecks();
        SharedSettingsAndModeRegistration();
        Debug.Log("IxDL shared motion validation passed: " + count + " assertions and Inspector settings checks.");
    }
    public static void RunMotionChecks() => CommonMotionValidation.RunMotionChecks();
    public static void RunTriggerLogChecks() => CommonMotionValidation.RunMotionChecks();

    private static void SharedSettingsAndModeRegistration()
    {
        var hostObject = new GameObject("IxDLDynamicValidationHost");
        var settings = ScriptableObject.CreateInstance<Study3SelectionSettings>();
        try
        {
            BallSpawner host = hostObject.AddComponent<BallSpawner>();
            settings.commonMotionAccelerationThreshold = 0.25f;
            settings.ixdlInputSpeedMin = 1f;
            settings.ixdlInputSpeedMax = 3f;
            settings.ixdlSpeedThresholdMin = 0.2f;
            settings.ixdlSpeedThresholdMax = 0.6f;
            host.SelectionSettings = settings;
            host.ApplySelectionSettings();
            IxDLDynamicMotionSettings applied = host.GetIxDLDynamicMotionSettings();
            Near(applied.AccelerationLimit, 0.25f, "Shared acceleration limit");
            Near(applied.GetSpeedLimit(2f), 0.4f, "Shared mapping parameters");
            foreach (string field in new[] { "commonMotionAccelerationThreshold", "ixdlInputSpeedMin", "ixdlInputSpeedMax", "ixdlSpeedThresholdMin", "ixdlSpeedThresholdMax" })
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

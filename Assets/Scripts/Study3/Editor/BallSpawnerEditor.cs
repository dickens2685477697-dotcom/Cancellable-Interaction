using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BallSpawner))]
[CanEditMultipleObjects]
public sealed class BallSpawnerEditor : Editor
{
    private static readonly string[] SharedFieldNames =
    {
        "m_Script",
        "SelectionSettings",
        "samplingRateHz",
        "rainbowArcPlacement",
        "showContact010TriggerRegion",
        "showModeReadyRing",
        "showDirectionCDArrow",
        "showDirectionCancelCone",
        "directionCancelConeLength",
        "useEntryDirectionAxisForDirectionCancel",
        "entryDirectionConeHalfAngle",
        "direction010CDAngleThreshold",
        "direction010SmoothingAlpha",
        "direction010MinSpeedThreshold",
        "direction010WindowFrameCount",
        "pinchDirectionCDAngleThreshold",
        "pinchDirectionSmoothingAlpha",
        "pinchDirectionMinSpeedThreshold",
        "pinchDirectionWindowFrameCount",
        "direction010TestCDAngleThreshold",
        "direction010TestSmoothingAlpha",
        "direction010TestMinSpeedThreshold",
        "direction010TestWindowFrameCount",
        "swing010CancelAngleThreshold",
        "releaseDistanceDeltaThreshold",
        "reactReleaseIndexSpeedThreshold",
        "speedMinThreshold",
        "speedMaxThreshold",
        "longPressTimeThreshold",
        "magicTap2SpeedThreshold",
        "magicTap2AccelerationThreshold",
        "dynamicStopSampleMs",
        "dynamicStopSpeedMin",
        "dynamicStopSpeedMax",
        "dynamicStopSpeedGain",
        "dynamicStopAccelMin",
        "dynamicStopAccelMax",
        "dynamicStopAccelGain",
        "dynamicStopHistorySeconds",
        "ixdlAccelerationThreshold",
        "ixdlInputSpeedMin",
        "ixdlInputSpeedMax",
        "ixdlSpeedThresholdMin",
        "ixdlSpeedThresholdMax",
        "pinchReleaseOpeningSpeedThreshold",
        "pinchEndDistanceThreshold",
        "pinchThumbSwipeDistanceThreshold",
        "pinchThumbSwipeContactDistanceThreshold",
        "pinchThumbSwipeDirectionRatioThreshold",
        "pinchDwellCancelTime",
        "pinchWristTiltCancelAngleThreshold",
        "pressSpeedThershold",
        "dwellWithdrawCancelAngleThreshold"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("samplingRateHz"));
        EditorGUILayout.HelpBox(
            "控制 Study3Experiment 的全局 FixedUpdate 判定频率（默认50 Hz）和trajectory日志。"
            + "Magic Tap 2.0与IxDL共用新MRTK更新，运动计算使用更新接收时间间隔；固定回调频率不等于新姿态频率。停用Spawner后恢复原时间步。Dynamic Stop仍受Sample Interval限制。",
            MessageType.Info);
        EditorGUILayout.Space();

        SerializedProperty settingsProperty = serializedObject.FindProperty("SelectionSettings");
        EditorGUILayout.LabelField("Shared Selection Technique Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(settingsProperty);
        serializedObject.ApplyModifiedProperties();

        Study3SelectionSettings settings = settingsProperty.objectReferenceValue as Study3SelectionSettings;
        if (settings != null && !settingsProperty.hasMultipleDifferentValues)
        {
            DrawInlineSettings(settings);
        }
        else if (!settingsProperty.hasMultipleDifferentValues)
        {
            EditorGUILayout.HelpBox(
                "No shared settings asset is assigned. This Spawner will continue using its legacy serialized values.",
                MessageType.Info);
        }

        EditorGUILayout.Space();
        serializedObject.Update();
        string[] excludedFields = settings != null
            ? SharedFieldNames
            : new[] { "m_Script", "SelectionSettings", "samplingRateHz" };
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (System.Array.IndexOf(excludedFields, property.name) >= 0)
            {
                continue;
            }

            Study3DirectionInspectorGUI.DrawProperty(property, GetSelectedMode());
        }
        serializedObject.ApplyModifiedProperties();
    }

    private BallSpawner.Mode? GetSelectedMode()
    {
        SerializedProperty mode = serializedObject.FindProperty("modeSelected");
        return mode.hasMultipleDifferentValues ? (BallSpawner.Mode?)null : (BallSpawner.Mode)mode.intValue;
    }

    private void DrawInlineSettings(Study3SelectionSettings settings)
    {
        SerializedObject settingsObject = new SerializedObject(settings);
        settingsObject.Update();

        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        SerializedProperty property = settingsObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.name == "m_Script")
            {
                continue;
            }

            Study3DirectionInspectorGUI.DrawProperty(property, GetSelectedMode());
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(settings, "Edit Study3 Selection Settings");
            settingsObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);

            foreach (Object inspectedTarget in targets)
            {
                BallSpawner spawner = inspectedTarget as BallSpawner;
                if (spawner == null)
                {
                    continue;
                }

                Undo.RecordObject(spawner, "Apply Study3 Selection Settings");
                spawner.ApplySelectionSettings();
                EditorUtility.SetDirty(spawner);
            }
        }
        else
        {
            settingsObject.ApplyModifiedProperties();
        }

        EditorGUI.indentLevel--;
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BallSpawner))]
[CanEditMultipleObjects]
public sealed class BallSpawnerEditor : Editor
{
    private static readonly string[] SharedFieldNames =
    {
        "m_Script",
        "fixedSamplingMode",
        "SelectionSettings",
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

        EditorGUILayout.LabelField("Fixed Sampling / 采样频率", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedSamplingMode"),
                new GUIContent("Fixed Sampling Mode"));
        }
        EditorGUILayout.HelpBox(
            "运行前选择 30 Hz 或 50 Hz。控制整个场景的 FixedUpdate；设备追踪和显示刷新率由设备决定。"
            + "多个 Spawner 中只给一个设置覆盖频率，其余使用 Project Default。",
            MessageType.Info);
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Active Fixed Sampling",
                (1f / Time.fixedDeltaTime).ToString("F2") + " Hz ("
                + (Time.fixedDeltaTime * 1000f).ToString("F2") + " ms)");
        }
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
        DrawPropertiesExcluding(serializedObject, SharedFieldNames);
        serializedObject.ApplyModifiedProperties();
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

            EditorGUILayout.PropertyField(property, true);
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

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
        "dynamicStopSampleMs",
        "dynamicStopSpeedMin",
        "dynamicStopSpeedMax",
        "dynamicStopSpeedGain",
        "dynamicStopAccelMin",
        "dynamicStopAccelMax",
        "dynamicStopAccelGain",
        "dynamicStopHistorySeconds",
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
        DrawPropertiesExcluding(serializedObject, settings != null
            ? SharedFieldNames
            : new[] { "m_Script", "SelectionSettings" });
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

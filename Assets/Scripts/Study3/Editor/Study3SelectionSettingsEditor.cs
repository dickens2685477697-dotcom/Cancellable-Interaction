using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Study3SelectionSettings))]
[CanEditMultipleObjects]
public sealed class Study3SelectionSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            using (new EditorGUI.DisabledScope(property.name == "m_Script"))
            {
                Study3DirectionInspectorGUI.DrawProperty(property, null);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}

internal static class Study3DirectionInspectorGUI
{
    public static void DrawProperty(SerializedProperty property, BallSpawner.Mode? selectedMode)
    {
        SerializedObject source = property.serializedObject;
        SerializedProperty axis = source.FindProperty("useEntryDirectionAxisForDirectionCancel");
        if (axis == null)
        {
            EditorGUILayout.PropertyField(property, true);
            return;
        }

        if (property.name == "releaseDistanceDeltaThreshold")
        {
            EditorGUILayout.PropertyField(property, new GUIContent("Release Cancel Distance Threshold (m)",
                "Pending期间持续记录拇指到中指、无名指、小指指尖平均距离的最小值；张开使平均距离相对该最小值增加达到阈值时取消，单位米。"), true);
            return;
        }

        if (property.name == "useEntryDirectionAxisForDirectionCancel")
        {
            EditorGUILayout.PropertyField(property, new GUIContent("Use Entry Direction Axis",
                "开启：使用世界-Z单侧取消方向；关闭：使用朝向用户的水平面方向。"), true);
            EditorGUILayout.HelpBox(
                "Direction取消：连续4次有效手部更新的三维速度均达到最低速度且移动方向均在取消角度内；低速、静止、失追或角度不符合会中断计数。Speed-Direction的速度准备及确认阈值仍单独生效。",
                MessageType.Info);
            return;
        }

        if (property.name == "directionCancelMinSpeed")
        {
            bool applies = !selectedMode.HasValue || IsDirectionMode(selectedMode.Value);
            DrawWithStatus(property, "Direction Cancel Minimum Speed (m/s)", applies, false, true,
                "所有Direction模式共用：连续更新的三维位移除以实际间隔；低于阈值时中断4次连续角度判定。Speed选择的速度阈值单独配置。");
            return;
        }

        if (property.name == "entryDirectionConeHalfAngle")
        {
            bool applies = !selectedMode.HasValue || IsDirectionMode(selectedMode.Value);
            DrawWithStatus(property, "Entry Direction Cone Half Angle (°)", applies,
                axis.hasMultipleDifferentValues, axis.boolValue,
                "仅在Use Entry Direction Axis开启时生效；关闭时改用各组Cancel Angle Threshold。");
            return;
        }

        string group;
        string prefix;
        if (property.name.StartsWith("direction010Test", System.StringComparison.Ordinal))
        {
            group = "test";
            prefix = "direction010Test";
        }
        else if (property.name.StartsWith("direction010", System.StringComparison.Ordinal))
        {
            group = "formal";
            prefix = "direction010";
        }
        else if (property.name.StartsWith("pinchDirection", System.StringComparison.Ordinal))
        {
            group = "pinch";
            prefix = "pinchDirection";
        }
        else
        {
            EditorGUILayout.PropertyField(property, true);
            return;
        }

        string suffix = property.name.Substring(prefix.Length);
        // Retain serialized legacy values but do not expose inactive speed,
        // smoothing or window settings as if they still affected cancellation.
        if (suffix == "MinSpeedThreshold" || suffix == "SmoothingAlpha" || suffix == "WindowFrameCount") return;
        string labelPrefix = group == "formal" ? "Direction" : group == "pinch" ? "Pinch Direction" : "Direction Test";
        string label;
        bool requiresLegacyBranch = false;
        switch (suffix)
        {
            case "CDAngleThreshold":
                label = labelPrefix + " Cancel Angle Threshold (°)";
                requiresLegacyBranch = true;
                break;
            default:
                EditorGUILayout.PropertyField(property, true);
                return;
        }

        bool usedByMode = !selectedMode.HasValue || UsesGroup(selectedMode.Value, group);
        DrawWithStatus(property, label, usedByMode,
            requiresLegacyBranch && axis.hasMultipleDifferentValues,
            !requiresLegacyBranch || !axis.boolValue,
            requiresLegacyBranch
                ? "仅在Use Entry Direction Axis关闭时生效；开启时使用Entry Direction Cone Half Angle。"
                : "本组适用于" + GetGroupModes(group) + "。");

    }

    private static void DrawWithStatus(SerializedProperty property, string label, bool usedByMode,
        bool mixedBranch, bool enabledByBranch, string explanation)
    {
        string status = !usedByMode ? "当前模式不使用" : mixedBranch ? "随各对象开关生效"
            : enabledByBranch ? "生效" : "不生效";
        string tooltip = property.tooltip + "\n" + explanation;
        if (!usedByMode)
        {
            tooltip += "\n当前Mode Selected不使用此参数。";
        }
        using (new EditorGUI.DisabledScope(!mixedBranch && !enabledByBranch))
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label + "（" + status + "）", tooltip), true);
        }
    }

    private static bool IsDirectionMode(BallSpawner.Mode mode)
    {
        return UsesGroup(mode, "formal") || UsesGroup(mode, "pinch") || UsesGroup(mode, "test");
    }

    private static bool UsesGroup(BallSpawner.Mode mode, string group)
    {
        if (group == "pinch") return mode == BallSpawner.Mode.pinchDirection;
        if (group == "test") return mode == BallSpawner.Mode.direction010Test;
        return mode == BallSpawner.Mode.speedDirection || mode == BallSpawner.Mode.dwellDirection
            || mode == BallSpawner.Mode.direction010;
    }

    private static string GetGroupModes(string group)
    {
        return group == "pinch" ? "Pinch-Direction" : group == "test" ? "010-direction-test"
            : "Speed-Direction、Dwell-Direction、010-direction";
    }
}

using System.Globalization;
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

        if (property.name == "useEntryDirectionAxisForDirectionCancel")
        {
            EditorGUILayout.PropertyField(property, new GUIContent("Use Entry Direction Axis",
                "开启：世界-Z单侧取消锥体，使用窗口平均方向；关闭：朝向用户的水平面方向，使用方向平滑。"), true);
            EditorGUILayout.HelpBox(selectedMode.HasValue
                ? IsDirectionMode(selectedMode.Value)
                    ? "以下Direction参数按当前模式和方向轴开关标注。灰色字段保留保存值，但不参与当前判定。"
                    : "当前Mode Selected不是Direction模式；Direction参数当前不参与判定。切换到对应模式后，生效状态会自动更新。"
                : "以下按方向算法分支标注生效状态；具体使用哪组参数取决于Spawner的Mode Selected。灰色字段保留保存值，但不参与该分支判定。",
                MessageType.Info);
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
        string labelPrefix = group == "formal" ? "Direction" : group == "pinch" ? "Pinch Direction" : "Direction Test";
        string label;
        bool requiresLegacyBranch = false;
        switch (suffix)
        {
            case "CDAngleThreshold":
                label = labelPrefix + " Cancel Angle Threshold (°)";
                requiresLegacyBranch = true;
                break;
            case "SmoothingAlpha":
                label = labelPrefix + " Smoothing Alpha";
                requiresLegacyBranch = true;
                break;
            case "MinSpeedThreshold": label = labelPrefix + " Min Speed Threshold (m/s)"; break;
            case "WindowFrameCount": label = labelPrefix + " Window Frame Count"; break;
            default:
                EditorGUILayout.PropertyField(property, true);
                return;
        }

        bool usedByMode = !selectedMode.HasValue || UsesGroup(selectedMode.Value, group);
        DrawWithStatus(property, label, usedByMode,
            requiresLegacyBranch && axis.hasMultipleDifferentValues,
            !requiresLegacyBranch || !axis.boolValue,
            requiresLegacyBranch
                ? "仅在Use Entry Direction Axis关闭时生效；开启时使用Entry Direction Cone Half Angle和窗口平均方向。"
                : "本组适用于" + GetGroupModes(group) + "；速度门槛和窗口帧数在两种方向算法分支中都生效。");

        if (suffix == "WindowFrameCount" && usedByMode)
        {
            DrawSummary(source, axis, group, prefix, selectedMode);
        }
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
        using (new EditorGUI.DisabledScope(!usedByMode || (!mixedBranch && !enabledByBranch)))
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label + "（" + status + "）", tooltip), true);
        }
    }

    private static void DrawSummary(SerializedObject source, SerializedProperty axis, string group,
        string prefix, BallSpawner.Mode? selectedMode)
    {
        string summary = "适用模式：" + GetGroupModes(group) + "。\n";
        if (axis.hasMultipleDifferentValues)
        {
            summary += "所选对象的方向轴开关不同，角度和平滑参数按各对象的开关分别生效。";
        }
        else if (axis.boolValue)
        {
            summary += "当前取消角：" + Format(source.FindProperty("entryDirectionConeHalfAngle"), 0f, 180f)
                + "°，使用Entry Direction Cone Half Angle。方向取窗口各段单位方向的平均；本组Cancel Angle和平滑系数不参与计算。";
        }
        else
        {
            summary += "当前取消角：" + Format(source.FindProperty(prefix + "CDAngleThreshold"), 0f, 90f)
                + "°，使用本组Cancel Angle Threshold。方向投影到水平面并平滑，Alpha："
                + Format(source.FindProperty(prefix + "SmoothingAlpha"), 0f, 1f) + "。";
        }
        if (selectedMode == BallSpawner.Mode.speedDirection)
        {
            summary += "\nSpeed-Direction：速度≤" + Format(source.FindProperty("speedMinThreshold"), 0f, float.MaxValue)
                + " m/s进入准备；方向判定就绪且在取消方向范围外、速度≥"
                + Format(source.FindProperty("speedMaxThreshold"), 0f, float.MaxValue) + " m/s才允许确认。";
        }
        EditorGUILayout.HelpBox(summary, MessageType.Info);
    }

    private static string Format(SerializedProperty property, float minimum, float maximum)
    {
        return property.hasMultipleDifferentValues ? "各对象不同"
            : Mathf.Clamp(property.floatValue, minimum, maximum).ToString("0.###", CultureInfo.InvariantCulture);
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

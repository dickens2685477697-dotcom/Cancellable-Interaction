using System;
using System.Collections.Generic;
using System.Globalization;

// Repairs the structure in a separate file; never rewrites a legacy row or file.
public static class Study3LegacyTrialCsv
{
    public const string PrefixHeader = "Case,ID,inTargetTimes,enable_preview,enableMineBall,enabledOb,errorTimes,errorInTimes,usedTime,triggerCondition,start,direction(0 for ),modeSelected,ballIndex,reactTime,react_frequency,obstacle_react_frequency,triggerTime,cancelTime,isMineBall(need cancel),successfully_Canceled,cancelReason,no trigger reason,cancelTimes,directionAngleFrame,directionSpeedMin";
    public const string Header = PrefixHeader + ",centerX,centerY,centerZ,targetPositionsJson,triggerSpeed,previousSpeed,speedThreshold,triggerAcceleration,sessionId,trialSequence,settingsRevision,expectedAction,actualAction,taskResult,participantId,protocolVersion,blockLabel,taskCondition,declaredIntent,schemaVersion";

    public static string Normalize(string legacyRow, int targetCount, params object[] metadata)
    {
        List<string> raw = Study3LogEncoding.ParseCsv(legacyRow);
        if (targetCount < 0 || raw.Count != 26 + 3 * (targetCount + 1) + 4)
            throw new FormatException("Legacy trial width differs from target geometry; row was not normalized.");
        if (metadata.Length != 12) throw new ArgumentException("Normalized result metadata must have twelve fields.");
        var fields = new List<object>();
        for (int i = 0; i < 29; i++) fields.Add(raw[i]);
        var targets = new object[targetCount];
        for (int i = 0; i < targetCount; i++)
        {
            var position = new object[3];
            for (int axis = 0; axis < 3; axis++)
            {
                if (!double.TryParse(raw[29 + i * 3 + axis], NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                    || double.IsNaN(number) || double.IsInfinity(number))
                    throw new FormatException("Target coordinate is not an invariant finite number.");
                position[axis] = number;
            }
            targets[i] = position;
        }
        fields.Add(Study3LogEncoding.Json(targets));
        for (int i = raw.Count - 4; i < raw.Count; i++) fields.Add(raw[i]);
        fields.AddRange(metadata);
        return Study3LogEncoding.Row(fields.ToArray());
    }
}

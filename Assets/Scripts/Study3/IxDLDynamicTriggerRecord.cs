using System;

/// <summary>Copies the decision values before motion state or the active trial changes.</summary>
public readonly struct IxDLDynamicTriggerRecord
{
    public const string CsvHeader = "triggerSpeed,previousSpeed,speedThreshold,triggerAcceleration";
    public const string MissingCsv = "NA,NA,NA,NA";

    public readonly float TriggerSpeed;
    public readonly float PreviousSpeed;
    public readonly float SpeedThreshold;
    public readonly float TriggerAcceleration;
    public readonly float AccelerationThreshold;

    public IxDLDynamicTriggerRecord(IxDLDynamicMotionTracker motion)
        : this(motion.Motion, motion.SpeedLimit, motion.AccelerationLimit) { }

    public IxDLDynamicTriggerRecord(Study3MotionTracker motion, float speedLimit, float accelerationLimit)
    {
        TriggerSpeed = motion.Speed;
        PreviousSpeed = motion.PreviousSpeed;
        SpeedThreshold = speedLimit;
        TriggerAcceleration = motion.Acceleration;
        AccelerationThreshold = accelerationLimit;
    }

    public string ToCsv()
    {
        return FormattableString.Invariant(
            $"{TriggerSpeed:R},{PreviousSpeed:R},{SpeedThreshold:R},{TriggerAcceleration:R}");
    }
}

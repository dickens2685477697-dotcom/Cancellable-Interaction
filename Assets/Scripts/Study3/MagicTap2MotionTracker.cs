using UnityEngine;

/// <summary>Fixed speed gate over the common timestamped motion pipeline.</summary>
public sealed class MagicTap2MotionTracker
{
    public readonly Study3MotionTracker Motion = new Study3MotionTracker();
    public bool IsInside { get; private set; }
    public bool IsSelected { get; private set; }
    public bool HasAcceleration => Motion.HasAcceleration;
    public float Speed => Motion.Speed;
    public float Acceleration => Motion.Acceleration;
    public bool SpeedConditionMet { get; private set; }
    public bool AccelerationConditionMet { get; private set; }

    public bool Sample(Study3MotionSample sample, bool inside, double now, float speedThreshold, float accelerationThreshold)
    {
        if (!Motion.TrySample(sample, now))
        {
            if (!Motion.HasPosition) { IsInside = IsSelected = SpeedConditionMet = AccelerationConditionMet = false; }
            return false;
        }
        IsInside = inside;
        if (!inside) IsSelected = false;
        SpeedConditionMet = HasAcceleration && Speed < speedThreshold;
        AccelerationConditionMet = HasAcceleration && Acceleration < accelerationThreshold;
        if (!inside || IsSelected || !SpeedConditionMet || !AccelerationConditionMet) return false;
        IsSelected = true;
        return true;
    }

    public void Reset()
    {
        Motion.Reset();
        IsInside = IsSelected = SpeedConditionMet = AccelerationConditionMet = false;
    }
    public static bool ContainsPoint(Collider collider, Vector3 tip) => Study3MotionTracker.ContainsPoint(collider, tip);
}

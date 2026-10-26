using UnityEngine;

/// <summary>Maps the previous accepted hand update's speed to the current speed limit.</summary>
public readonly struct IxDLDynamicMotionSettings
{
    public readonly float AccelerationLimit;
    public readonly float InputSpeedMin;
    public readonly float InputSpeedMax;
    public readonly float SpeedLimitMin;
    public readonly float SpeedLimitMax;

    public IxDLDynamicMotionSettings(float accelerationLimit, float inputSpeedMin,
        float inputSpeedMax, float speedLimitMin, float speedLimitMax)
    {
        AccelerationLimit = accelerationLimit;
        InputSpeedMin = Mathf.Max(0f, inputSpeedMin);
        InputSpeedMax = Mathf.Max(InputSpeedMin + 0.0001f, inputSpeedMax);
        SpeedLimitMin = Mathf.Max(0f, speedLimitMin);
        SpeedLimitMax = Mathf.Max(SpeedLimitMin, speedLimitMax);
    }

    public float GetSpeedLimit(float previousSpeed)
    {
        return Mathf.Lerp(SpeedLimitMin, SpeedLimitMax,
            Mathf.InverseLerp(InputSpeedMin, InputSpeedMax, previousSpeed));
    }
}

/// <summary>Dynamic speed gate over the same timestamped vector motion as Magic Tap 2.0.</summary>
public sealed class IxDLDynamicMotionTracker
{
    public readonly Study3MotionTracker Motion = new Study3MotionTracker();
    public bool IsInside { get; private set; }
    public bool IsActive { get; private set; }
    public bool HasAcceleration => Motion.HasAcceleration;
    public bool ConditionsMet { get; private set; }
    public float Speed => Motion.Speed;
    public float PreviousSpeed => Motion.PreviousSpeed;
    public float Acceleration => Motion.Acceleration;
    public float SpeedLimit { get; private set; }
    public float AccelerationLimit { get; private set; }
    public bool SpeedConditionMet => HasAcceleration && Speed < SpeedLimit;
    public bool AccelerationConditionMet => HasAcceleration && Acceleration < AccelerationLimit;

    public bool Sample(Study3MotionSample sample, bool inside, double now, IxDLDynamicMotionSettings settings)
    {
        if (!Motion.TrySample(sample, now))
        {
            if (!Motion.HasPosition) { IsInside = IsActive = ConditionsMet = false; }
            return false;
        }
        IsInside = inside;
        if (!inside) IsActive = false;
        AccelerationLimit = settings.AccelerationLimit;
        SpeedLimit = settings.GetSpeedLimit(PreviousSpeed);
        ConditionsMet = inside && SpeedConditionMet && AccelerationConditionMet;
        bool selected = ConditionsMet && !IsActive;
        if (selected) IsActive = true;
        return selected;
    }
    public void Reset()
    {
        Motion.Reset();
        IsInside = IsActive = ConditionsMet = false;
        SpeedLimit = AccelerationLimit = 0f;
    }
}

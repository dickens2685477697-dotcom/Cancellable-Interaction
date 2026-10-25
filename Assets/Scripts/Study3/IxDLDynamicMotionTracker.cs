using UnityEngine;

/// <summary>Maps the previous fixed sample's speed to the current speed limit.</summary>
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

/// <summary>
/// Samples once per FixedUpdate. Acceleration is signed, matching AcceStudy3.
/// Contact and both motion conditions form a single rising-edge selection gate.
/// </summary>
public sealed class IxDLDynamicMotionTracker
{
    public bool IsInside { get; private set; }
    public bool IsActive { get; private set; }
    public bool HasAcceleration { get; private set; }
    public bool ConditionsMet { get; private set; }
    public float Speed { get; private set; }
    public float PreviousSpeed { get; private set; }
    public float Acceleration { get; private set; }
    public float SpeedLimit { get; private set; }
    public float AccelerationLimit { get; private set; }
    public bool SpeedConditionMet => HasAcceleration && Speed < SpeedLimit;
    public bool AccelerationConditionMet => HasAcceleration && Acceleration < AccelerationLimit;

    private bool hasPosition;
    private bool hasSpeed;
    private Vector3 previousPosition;

    public bool Sample(Vector3 position, bool inside, float deltaTime, IxDLDynamicMotionSettings settings)
    {
        if (!(deltaTime > 0f) || float.IsInfinity(deltaTime))
        {
            Reset();
            return false;
        }

        IsInside = inside;
        if (!inside) IsActive = false;
        AccelerationLimit = settings.AccelerationLimit;

        if (!hasPosition)
        {
            hasPosition = true;
            previousPosition = position;
            SpeedLimit = settings.SpeedLimitMin;
            return false;
        }

        float currentSpeed = Vector3.Distance(previousPosition, position) / deltaTime;
        previousPosition = position;
        PreviousSpeed = Speed;
        HasAcceleration = hasSpeed;
        // Read the previous speed before replacing it with this sample's speed.
        SpeedLimit = settings.GetSpeedLimit(PreviousSpeed);
        Acceleration = HasAcceleration
            ? (currentSpeed - PreviousSpeed) / deltaTime
            : float.PositiveInfinity;
        Speed = currentSpeed;
        hasSpeed = true;

        bool condition = inside && SpeedConditionMet && AccelerationConditionMet;
        bool risingEdge = condition && !ConditionsMet;
        ConditionsMet = condition;
        if (risingEdge) IsActive = true;
        return risingEdge;
    }

    public void Reset()
    {
        IsInside = IsActive = HasAcceleration = ConditionsMet = false;
        hasPosition = hasSpeed = false;
        previousPosition = Vector3.zero;
        Speed = PreviousSpeed = Acceleration = SpeedLimit = AccelerationLimit = 0f;
    }
}

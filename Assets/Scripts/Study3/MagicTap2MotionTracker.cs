using UnityEngine;

/// <summary>Independent copy of the fixed-step speed/acceleration gate used before React Tap.</summary>
public sealed class MagicTap2MotionTracker
{
    private bool hasPosition;
    private bool hasVelocity;
    private Vector3 previousPosition;
    private Vector3 previousVelocity;

    public bool IsInside { get; private set; }
    public bool IsSelected { get; private set; }
    public bool HasAcceleration { get; private set; }
    public float Speed { get; private set; }
    public float Acceleration { get; private set; }
    public bool SpeedConditionMet { get; private set; }
    public bool AccelerationConditionMet { get; private set; }

    public bool Sample(Vector3 position, bool inside, float deltaTime, float speedThreshold, float accelerationThreshold)
    {
        if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
        {
            Reset();
            return false;
        }

        IsInside = inside;
        if (!inside) IsSelected = false;

        if (!hasPosition)
        {
            previousPosition = position;
            hasPosition = true;
            return false;
        }

        Vector3 displacement = position - previousPosition;
        // Match React Tap: sub-millimetre samples do not advance the motion history.
        if (displacement.magnitude < 0.001f) return false;

        Vector3 velocity = displacement / deltaTime;
        Speed = velocity.magnitude;
        HasAcceleration = hasVelocity;
        Acceleration = hasVelocity ? ((velocity - previousVelocity) / deltaTime).magnitude : 0f;
        previousPosition = position;
        previousVelocity = velocity;
        hasVelocity = true;

        // Both strict comparisons must pass in the same accepted fixed-step sample.
        SpeedConditionMet = Speed < speedThreshold;
        AccelerationConditionMet = HasAcceleration && Acceleration < accelerationThreshold;
        if (!inside || IsSelected || !SpeedConditionMet || !AccelerationConditionMet) return false;

        IsSelected = true;
        return true;
    }

    public void Reset()
    {
        hasPosition = hasVelocity = false;
        previousPosition = previousVelocity = Vector3.zero;
        IsInside = IsSelected = HasAcceleration = false;
        SpeedConditionMet = AccelerationConditionMet = false;
        Speed = Acceleration = 0f;
    }

    public static bool ContainsFinger(Collider collider, Vector3 tip, Vector3 distal)
    {
        return collider != null && collider.enabled && collider.gameObject.activeInHierarchy
            && (collider.bounds.Contains(tip) || collider.bounds.Contains(distal));
    }
}

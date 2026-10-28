using System;
using UnityEngine;

/// <summary>World-space parameters for the demo's dynamic speed/acceleration gate.</summary>
public readonly struct DynamicStopMotionSettings
{
    public readonly float SampleSeconds;
    public readonly float SpeedMin;
    public readonly float SpeedMax;
    public readonly float SpeedGain;
    public readonly float AccelMin;
    public readonly float AccelMax;
    public readonly float AccelGain;
    public readonly float HistorySeconds;

    public DynamicStopMotionSettings(float sampleMs, float speedMin, float speedMax,
        float speedGain, float accelMin, float accelMax, float accelGain, float historySeconds)
    {
        SampleSeconds = Math.Max(0.1f, sampleMs) / 1000f;
        SpeedMin = Math.Max(0.0001f, speedMin);
        SpeedMax = Math.Max(SpeedMin, speedMax);
        SpeedGain = Math.Max(0f, speedGain);
        AccelMin = Math.Max(0.0001f, accelMin);
        AccelMax = Math.Max(AccelMin, accelMax);
        AccelGain = Math.Max(0f, accelGain);
        HistorySeconds = Math.Max(0.001f, historySeconds);
    }
}

/// <summary>
/// Shared by targets and obstacles. Accumulates every observed segment, measures
/// with real elapsed time, and latches a selection until exit or tracking reset.
/// </summary>
public sealed class DynamicStopMotionTracker
{
    public bool IsInside { get; private set; }
    public bool IsActive { get; private set; }
    public bool HasAcceleration { get; private set; }
    public float Speed { get; private set; }
    public float Acceleration { get; private set; }
    public float RecentSpeed { get; private set; }
    public float SpeedLimit { get; private set; }
    public float AccelLimit { get; private set; }
    public long AttributionMeasurementSequence { get; private set; }
    public double AttributionMeasurementTime { get; private set; }
    public float AttributionMeasurementDt { get; private set; }
    public bool AttributionSampleAccepted { get; private set; }
    public bool SpeedConditionMet => HasAcceleration && Speed <= SpeedLimit;
    public bool AccelerationConditionMet => HasAcceleration && Acceleration <= AccelLimit;

    private bool hasPosition;
    private bool hasPreviousVelocity;
    private Vector3 lastPosition;
    private Vector3 measurePosition;
    private Vector3 previousVelocity;
    private double measureTime;
    private float distance;

    /// <returns>True only on the first successful measurement of this contact.</returns>
    public bool Sample(Vector3 position, bool inside, double now, DynamicStopMotionSettings settings)
    {
        AttributionSampleAccepted = false;
        IsInside = inside;
        if (!inside)
        {
            IsActive = false;
        }

        if (!hasPosition)
        {
            hasPosition = true;
            lastPosition = measurePosition = position;
            measureTime = now;
            SpeedLimit = settings.SpeedMin;
            AccelLimit = settings.AccelMin;
            return false;
        }

        // Preserve observed turns and backtracking even if the endpoint is unchanged.
        distance += Vector3.Distance(lastPosition, position);
        lastPosition = position;
        double elapsed = now - measureTime;
        if (elapsed < settings.SampleSeconds)
        {
            return false;
        }

        float dt = (float)elapsed;
        AttributionMeasurementSequence++;
        AttributionMeasurementTime = now;
        AttributionMeasurementDt = dt;
        AttributionSampleAccepted = true;
        float speed = distance / dt;
        Vector3 velocity = (position - measurePosition) / dt;
        HasAcceleration = hasPreviousVelocity;
        float acceleration = HasAcceleration
            ? Math.Max((velocity - previousVelocity).magnitude / dt, Math.Abs(speed - Speed) / dt)
            : float.PositiveInfinity;

        RecentSpeed = Math.Max(speed, RecentSpeed * (float)Math.Exp(-dt / settings.HistorySeconds));
        SpeedLimit = Math.Min(settings.SpeedMax, settings.SpeedMin + RecentSpeed * settings.SpeedGain);
        AccelLimit = Math.Min(settings.AccelMax, settings.AccelMin + RecentSpeed * settings.AccelGain);

        Speed = speed;
        Acceleration = acceleration;
        previousVelocity = velocity;
        hasPreviousVelocity = true;
        measurePosition = position;
        measureTime = now;
        distance = 0f;

        // No dwell, pressure, angle, or subsequent movement confirmation is required.
        if (!IsActive && inside && SpeedConditionMet && AccelerationConditionMet)
        {
            IsActive = true;
            return true;
        }

        return false;
    }

    public void Reset()
    {
        AttributionSampleAccepted = false;
        AttributionMeasurementDt = 0f;
        IsInside = IsActive = HasAcceleration = false;
        hasPosition = hasPreviousVelocity = false;
        lastPosition = measurePosition = previousVelocity = Vector3.zero;
        measureTime = 0;
        distance = Speed = Acceleration = RecentSpeed = SpeedLimit = AccelLimit = 0f;
    }

    public static bool ContainsPoint(Collider collider, Vector3 position)
    {
        if (collider == null || !collider.enabled)
        {
            return false;
        }

        SphereCollider sphere = collider as SphereCollider;
        if (sphere != null)
        {
            Vector3 scale = sphere.transform.lossyScale;
            float radius = sphere.radius * Math.Max(Math.Abs(scale.x), Math.Max(Math.Abs(scale.y), Math.Abs(scale.z)));
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            return (position - center).sqrMagnitude <= radius * radius;
        }

        return collider.bounds.Contains(position);
    }
}

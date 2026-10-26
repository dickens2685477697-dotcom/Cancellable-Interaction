using System;
using UnityEngine;

/// <summary>A copied MRTK provider update, not a hardware capture timestamp.</summary>
public readonly struct Study3MotionSample
{
    public readonly uint SourceId;
    public readonly long Sequence;
    public readonly double Timestamp;
    public readonly Vector3 Position;
    public readonly long TrackingEpoch;
    public Study3MotionSample(uint sourceId, long sequence, double timestamp, Vector3 position, long trackingEpoch = 0)
    { SourceId = sourceId; Sequence = sequence; Timestamp = timestamp; Position = position; TrackingEpoch = trackingEpoch; }
}

/// <summary>Common preprocessing for Magic Tap 2.0 and IxDL Dynamic.</summary>
public sealed class Study3MotionTracker
{
    public const string RulesVersion = "shared-provider-v2";
    public const string TimestampKind = "mrtk_update_received_realtime";
    public const double MaximumAgeSeconds = 0.25;
    public Study3MotionSample LastSample { get; private set; }
    public bool HasPosition { get; private set; }
    public bool HasVelocity { get; private set; }
    public bool HasAcceleration { get; private set; }
    public bool AcceptedLastSample { get; private set; }
    public float DeltaTime { get; private set; }
    public float Speed { get; private set; }
    public float PreviousSpeed { get; private set; }
    public float Acceleration { get; private set; }
    public string Status { get; private set; } = "reset";
    private Vector3 previousVelocity;

    public bool TrySample(Study3MotionSample sample, double now)
    {
        AcceptedLastSample = false;
        if (!Finite(sample.Timestamp) || !Finite(now) || !Finite(sample.Position)
            || sample.Sequence <= 0 || sample.Timestamp > now || now - sample.Timestamp > MaximumAgeSeconds)
        { Reset(); Status = "invalid_or_stale"; return false; }
        if (HasPosition && sample.SourceId == LastSample.SourceId && sample.TrackingEpoch == LastSample.TrackingEpoch
            && sample.Sequence <= LastSample.Sequence)
        { Status = "duplicate_or_out_of_order"; return false; }
        if (!HasPosition || sample.SourceId != LastSample.SourceId || sample.TrackingEpoch != LastSample.TrackingEpoch
            || sample.Timestamp - LastSample.Timestamp > MaximumAgeSeconds)
        {
            Reset(); LastSample = sample; HasPosition = true; AcceptedLastSample = true; Status = "baseline"; return true;
        }
        double elapsed = sample.Timestamp - LastSample.Timestamp;
        if (elapsed <= 0) { Status = "non_monotonic_time"; return false; }
        DeltaTime = (float)elapsed;
        Vector3 velocity = (sample.Position - LastSample.Position) / DeltaTime;
        PreviousSpeed = Speed;
        Speed = velocity.magnitude;
        HasAcceleration = HasVelocity;
        Acceleration = HasAcceleration ? ((velocity - previousVelocity) / DeltaTime).magnitude : 0f;
        if (!Finite(Speed) || !Finite(Acceleration))
        { Reset(); Status = "invalid_motion"; return false; }
        previousVelocity = velocity;
        HasVelocity = true;
        LastSample = sample;
        AcceptedLastSample = true;
        Status = HasAcceleration ? "accepted" : "velocity_warmup";
        return true;
    }

    public void Reset()
    {
        HasPosition = HasVelocity = HasAcceleration = AcceptedLastSample = false;
        LastSample = default;
        previousVelocity = Vector3.zero;
        DeltaTime = Speed = PreviousSpeed = Acceleration = 0f;
        Status = "reset";
    }

    public static bool ContainsPoint(Collider collider, Vector3 point)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) return false;
        SphereCollider sphere = collider as SphereCollider;
        if (sphere != null)
        {
            Vector3 scale = sphere.transform.lossyScale;
            float radius = sphere.radius * Math.Max(Math.Abs(scale.x), Math.Max(Math.Abs(scale.y), Math.Abs(scale.z)));
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            return (point - center).sqrMagnitude <= radius * radius;
        }
        return collider.bounds.Contains(point);
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
}

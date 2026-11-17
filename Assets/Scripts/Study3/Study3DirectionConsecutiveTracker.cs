using System;
using UnityEngine;

/// <summary>Counts consecutive directions from distinct hand updates after a minimum-speed filter.</summary>
public sealed class Study3DirectionConsecutiveTracker
{
    public const int RequiredFrames = 4;
    public enum Observation { Invalid, Primed, Duplicate, BelowMinimumSpeed, UndefinedDirection, AngleChecked }

    private bool hasPrevious;
    private uint sourceId;
    private long epoch, sequence;
    private double timestamp;
    private Vector3 previousPosition;

    public int ConsecutiveMatches { get; private set; }
    public int ConsecutiveValidFrames { get; private set; }
    public bool AnglePass { get; private set; }
    public bool CancelPass => ConsecutiveMatches >= RequiredFrames;
    // Speed-Direction may confirm only when its direction has had four valid
    // observations and the current direction is outside the cancellation cone.
    public bool AllowSpeedConfirmation => ConsecutiveValidFrames >= RequiredFrames && !AnglePass;
    public float Angle { get; private set; } = float.NaN;
    public float Speed { get; private set; } = float.NaN;
    public bool SpeedPass { get; private set; }
    public Vector3 Movement { get; private set; }
    public Vector3 Direction { get; private set; }
    public Vector3 Reference { get; private set; }

    public void Reset()
    {
        hasPrevious = false;
        sourceId = 0; epoch = 0; sequence = 0; timestamp = 0;
        previousPosition = Vector3.zero;
        ClearDecision();
    }

    private void ClearDecision()
    {
        ConsecutiveMatches = 0;
        ConsecutiveValidFrames = 0;
        AnglePass = false;
        Angle = float.NaN;
        Speed = float.NaN;
        SpeedPass = false;
        Movement = Direction = Reference = Vector3.zero;
    }

    public Observation Evaluate(uint newSource, long newEpoch, long newSequence, double newTimestamp,
        Vector3 position, Vector3 reference, bool horizontal, float angleThreshold, float minimumSpeed, double maximumGapSeconds)
    {
        if (newSequence <= 0 || double.IsNaN(newTimestamp) || double.IsInfinity(newTimestamp)
            || !Finite(position) || !Finite(reference) || float.IsNaN(angleThreshold) || float.IsInfinity(angleThreshold)
            || float.IsNaN(minimumSpeed) || float.IsInfinity(minimumSpeed))
        {
            Reset();
            return Observation.Invalid;
        }
        if (hasPrevious && newSource == sourceId && newEpoch == epoch && newSequence <= sequence)
        {
            // A repeated callback is not another hand update and must not count twice.
            return Observation.Duplicate;
        }
        double elapsed = newTimestamp - timestamp;
        if (!hasPrevious || newSource != sourceId || newEpoch != epoch || elapsed <= 0
            || elapsed > maximumGapSeconds)
        {
            Reset();
            Save(newSource, newEpoch, newSequence, newTimestamp, position);
            return Observation.Primed;
        }

        Vector3 movement = position - previousPosition;
        Save(newSource, newEpoch, newSequence, newTimestamp, position);
        // Filter the 3D motion using actual hand-sample timing, before any horizontal angle projection.
        double distance = Math.Sqrt((double)movement.x * movement.x + (double)movement.y * movement.y + (double)movement.z * movement.z);
        double speed = distance / elapsed;
        if (double.IsNaN(speed) || double.IsInfinity(speed))
        {
            ClearDecision();
            return Observation.Invalid;
        }
        if (speed < Math.Max(0f, minimumSpeed))
        {
            ClearDecision();
            Speed = (float)speed;
            return Observation.BelowMinimumSpeed;
        }
        Speed = (float)speed;
        SpeedPass = true;
        if (horizontal) { movement.y = 0f; reference.y = 0f; }
        if (!TryUnit(movement, out Vector3 direction) || !TryUnit(reference, out Vector3 axis))
        {
            // Zero movement has no direction, so it breaks the consecutive run.
            ClearDecision();
            return Observation.UndefinedDirection;
        }

        Movement = movement;
        Direction = direction;
        Reference = axis;
        Angle = Vector3.Angle(direction, axis);
        AnglePass = Angle <= angleThreshold;
        ConsecutiveValidFrames = Math.Min(ConsecutiveValidFrames + 1, RequiredFrames);
        ConsecutiveMatches = AnglePass ? Math.Min(ConsecutiveMatches + 1, RequiredFrames) : 0;
        return Observation.AngleChecked;
    }

    private void Save(uint newSource, long newEpoch, long newSequence, double newTimestamp, Vector3 position)
    {
        hasPrevious = true;
        sourceId = newSource; epoch = newEpoch; sequence = newSequence;
        timestamp = newTimestamp; previousPosition = position;
    }

    private static bool Finite(Vector3 p)
    {
        return !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y)
            && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }

    // Normalize in double precision: a tiny but nonzero movement still has a direction.
    private static bool TryUnit(Vector3 p, out Vector3 unit)
    {
        double length = Math.Sqrt((double)p.x * p.x + (double)p.y * p.y + (double)p.z * p.z);
        if (!(length > 0) || double.IsNaN(length) || double.IsInfinity(length))
        {
            unit = Vector3.zero;
            return false;
        }
        unit = new Vector3((float)(p.x / length), (float)(p.y / length), (float)(p.z / length));
        return true;
    }
}

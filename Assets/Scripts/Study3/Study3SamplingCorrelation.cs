using System;
using UnityEngine;

/// <summary>SDK time and Unity time deliberately remain separate clock domains.</summary>
public sealed class Study3SdkStampHistory
{
    private double previous = double.NaN;
    public static bool IsValid(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
    public string Observe(bool succeeded, bool tracked, double stamp)
    {
        if (!succeeded || !tracked || !IsValid(stamp))
        {
            previous = double.NaN;
            return !succeeded ? "read_failed" : !tracked ? "not_tracked" : "invalid_timestamp";
        }
        string result = double.IsNaN(previous) ? "first_after_gap" : stamp == previous ? "same_sdk_timestamp"
            : stamp > previous ? "advanced_sdk_timestamp" : "regressed_sdk_timestamp";
        previous = stamp;
        return result;
    }
}

public static class Study3SamplingCorrelation
{
    public static bool SamePosition(Vector3 a, Vector3 b) => Finite(a) && Finite(b)
        && a.x.Equals(b.x) && a.y.Equals(b.y) && a.z.Equals(b.z);
    private static bool Finite(Vector3 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x)
        && !float.IsNaN(value.y) && !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
}

// Sampling baseline diagnostics import verification (2026-10-28); no selection rule change.

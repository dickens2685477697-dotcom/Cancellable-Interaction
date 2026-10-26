using System;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic replay checks for the shared application-update contract.</summary>
public static class CommonMotionValidation
{
    private static int checks;
    private static void Require(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Near(float a, float b, string message) => Require(Math.Abs(a - b) < 0.0001f, message);
    private static Study3MotionSample S(long id, double t, float x = 0, uint source = 1)
        => new Study3MotionSample(source, id, t, Vector3.right * x);
    private static readonly IxDLDynamicMotionSettings Fixed = new IxDLDynamicMotionSettings(11.661f, 0, 1, .1f, .1f);

    [MenuItem("Study3/Validate Common Motion")]
    public static void Run() => Debug.Log("Common motion validation passed: " + RunMotionChecks() + " assertions.");

    public static int RunMotionChecks()
    {
        checks = 0;
        var m = new MagicTap2MotionTracker(); var d = new IxDLDynamicMotionTracker();
        foreach (var sample in new[] { S(1, 1), S(2, 1.02, .006f) })
        {
            Require(!m.Sample(sample, true, sample.Timestamp, .1f, 11.661f), "Magic warmup selected");
            Require(!d.Sample(sample, true, sample.Timestamp, Fixed), "Dynamic warmup selected");
        }
        // A duplicate read cannot manufacture a zero speed or advance acceleration history.
        var duplicate = S(2, 1.02, .006f);
        Require(!m.Sample(duplicate, true, 1.03, .1f, 11.661f), "Magic duplicate selected");
        Require(!d.Sample(duplicate, true, 1.03, Fixed), "Dynamic duplicate selected");
        Near(m.Speed, .3f, "Magic duplicate replaced velocity"); Near(d.Speed, .3f, "Dynamic duplicate replaced velocity");
        Require(!m.Motion.AcceptedLastSample && !d.Motion.AcceptedLastSample, "Duplicate was marked accepted");
        // A NEW zero-displacement update is real input: large deceleration is checked by both gates.
        var stop = S(3, 1.04, .006f);
        Require(!m.Sample(stop, true, stop.Timestamp, .1f, 11.661f), "Magic ignored large deceleration");
        Require(!d.Sample(stop, true, stop.Timestamp, Fixed), "Dynamic retained signed deceleration");
        Near(m.Acceleration, 15, "Magic deceleration magnitude"); Near(d.Acceleration, 15, "Dynamic deceleration magnitude");
        Require(!m.Sample(stop, true, 1.05, .1f, 11.661f) && !d.Sample(stop, true, 1.05, Fixed), "Repeated stop selected");
        var still = S(4, 1.06, .006f);
        Require(m.Sample(still, true, still.Timestamp, .1f, 11.661f), "Magic discarded fresh stationary pose");
        Require(d.Sample(still, true, still.Timestamp, Fixed), "Dynamic discarded fresh stationary pose");
        var sameContact = S(5, 1.08, .006f);
        Require(!m.Sample(sameContact, true, 1.08, .1f, 11.661f) && !d.Sample(sameContact, true, 1.08, Fixed), "Contact selected twice");
        var outside = S(6, 1.10, .006f);
        Require(!m.Sample(outside, false, 1.10, .1f, 11.661f) && !d.Sample(outside, false, 1.10, Fixed), "Outside selected");
        var reenter = S(7, 1.12, .006f);
        Require(m.Sample(reenter, true, 1.12, .1f, 11.661f) && d.Sample(reenter, true, 1.12, Fixed), "Reentry did not rearm both modes");

        // Sub-millimetre movement and unequal update intervals have the same physical scale.
        m.Reset(); d.Reset();
        foreach (var sample in new[] { S(1, 2), S(2, 2.014, .00028f), S(3, 2.042, .00084f) })
        {
            bool a = m.Sample(sample, true, sample.Timestamp, .1f, 11.661f);
            bool b = d.Sample(sample, true, sample.Timestamp, Fixed);
            Require(a == b, "Equivalent gates diverged on small motion");
        }
        Require(m.IsSelected && d.IsActive, "Sub-millimetre new updates were filtered");
        Near(m.Speed, .02f, "Magic did not use actual interval"); Near(d.Speed, .02f, "Dynamic did not use actual interval");
        Near(m.Acceleration, 0, "Constant velocity got false acceleration");

        // Turning at constant speed is vector acceleration in BOTH modes.
        m.Reset(); d.Reset();
        var turnSettings = new IxDLDynamicMotionSettings(5, 0, 1, .2f, .2f);
        foreach (var sample in new[] { S(1, 3), S(2, 3.02, .002f), S(3, 3.04, 0) })
        {
            m.Sample(sample, true, sample.Timestamp, .2f, 5);
            d.Sample(sample, true, sample.Timestamp, turnSettings);
        }
        Near(m.Acceleration, 10, "Magic turning acceleration"); Near(d.Acceleration, 10, "Dynamic turning acceleration");
        Require(!m.IsSelected && !d.IsActive, "Constant-speed turn bypassed vector gate");

        // Different selection with identical motion must be attributable to the speed gate.
        m.Reset(); d.Reset();
        var dynamic = new IxDLDynamicMotionSettings(100, 0, .6f, .02f, .06f);
        foreach (var sample in new[] { S(1, 4), S(2, 4.02, .012f), S(3, 4.04, .0128f) })
        { m.Sample(sample, true, sample.Timestamp, .03f, 100); d.Sample(sample, true, sample.Timestamp, dynamic); }
        Near(m.Speed, d.Speed, "Modes used different speed"); Near(m.Acceleration, d.Acceleration, "Modes used different acceleration");
        Require(!m.IsSelected && d.IsActive, "Previous-speed mapping was not isolated");
        Near(d.SpeedLimit, .06f, "Dynamic threshold did not use previous accepted speed");
        Near(dynamic.GetSpeedLimit(-1), .02f, "Mapping lower clamp"); Near(dynamic.GetSpeedLimit(2), .06f, "Mapping upper clamp");

        var motion = new Study3MotionTracker();
        Require(motion.TrySample(S(1, 5), 5), "Baseline rejected");
        Require(!motion.TrySample(S(1, 5, 1), 5.01), "Same ID with changed position accepted");
        Require(!motion.TrySample(S(2, 4.99), 5.01), "Reversed time accepted");
        Require(motion.TrySample(S(3, 5.02, .002f), 5.02), "Valid update after rejected input failed");
        Near(motion.Speed, .1f, "Rejected sample advanced history");
        Require(motion.TrySample(S(4, 5.04, 10, 2), 5.04) && !motion.HasVelocity, "Hand switch bridged history");
        Require(motion.TrySample(S(5, 5.4, 11, 2), 5.4) && !motion.HasVelocity, "Long gap bridged history");
        Require(motion.TrySample(new Study3MotionSample(2, 6, 5.42, Vector3.zero, 10), 5.42)
            && !motion.HasVelocity, "Brief tracking loss bridged the same source ID");
        Require(!motion.TrySample(S(7, 5.4, 11, 2), 5.8) && !motion.HasPosition, "Stale update retained history");
        Require(!motion.TrySample(S(7, 7), 6), "Future timestamp accepted");
        Require(!motion.TrySample(S(0, 6), 6), "Missing update sequence accepted");
        Require(!motion.TrySample(S(8, double.NaN), 6), "NaN timestamp accepted");
        Require(!motion.TrySample(S(8, 6, float.PositiveInfinity), 6), "Infinite position accepted");

        // Strict threshold equality and invalid-input gate cleanup.
        m.Reset(); d.Reset();
        var exact = new IxDLDynamicMotionSettings(1, 0, 1, .125f, .125f);
        foreach (var sample in new[] { S(1, 8), S(2, 8.125, .015625f), S(3, 8.25, .03125f) })
        { m.Sample(sample, true, sample.Timestamp, .125f, 1); d.Sample(sample, true, sample.Timestamp, exact); }
        Require(!m.IsSelected && !d.IsActive, "Speed equality passed");
        var invalid = S(4, double.NaN);
        Require(!m.Sample(invalid, true, 9, 1, 1) && !d.Sample(invalid, true, 9, exact), "Invalid input selected");
        Require(!m.HasAcceleration && !d.HasAcceleration && !m.IsInside && !d.IsInside, "Invalid input kept gate history");

        // Exact trigger records remain immutable and culture-independent.
        d.Reset();
        foreach (var sample in new[] { S(1, 10), S(2, 10.02), S(3, 10.04) }) d.Sample(sample, true, sample.Timestamp, Fixed);
        var record = new IxDLDynamicTriggerRecord(d);
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            string csv = record.ToCsv(); d.Reset();
            Require(csv == record.ToCsv() && csv.Split(',').Length == 4, "Snapshot changed or used decimal comma");
            Near(record.TriggerSpeed, 0, "Fresh zero was not captured");
        } finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
        var sphereObject = new GameObject("CommonMotionGeometryCheck");
        try
        {
            var sphere = sphereObject.AddComponent<SphereCollider>();
            sphere.radius = 1;
            Require(Study3MotionTracker.ContainsPoint(sphere, Vector3.zero), "Sphere center excluded");
            Require(!Study3MotionTracker.ContainsPoint(sphere, new Vector3(.8f, .8f, 0)), "Bounding-box corner counted as sphere interior");
            Require(MagicTap2MotionTracker.ContainsPoint(sphere, Vector3.zero), "Magic did not use common geometry");
            sphere.enabled = false;
            Require(!Study3MotionTracker.ContainsPoint(sphere, Vector3.zero), "Disabled collider accepted contact");
            sphere.enabled = true; sphereObject.SetActive(false);
            Require(!Study3MotionTracker.ContainsPoint(sphere, Vector3.zero), "Inactive collider accepted contact");
        }
        finally { UnityEngine.Object.DestroyImmediate(sphereObject); }
        return checks;
    }
}

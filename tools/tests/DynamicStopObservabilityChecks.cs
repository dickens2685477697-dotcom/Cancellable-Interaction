using System;
using UnityEngine;

public static class DynamicStopObservabilityChecks
{
    public static int Run()
    {
        int checks = 0;
        Action<bool, string> require = (value, message) => { checks++; if (!value) throw new Exception(message); };
        var tracker = new DynamicStopMotionTracker();
        var settings = new DynamicStopMotionSettings(16, .02f, .12f, .1f, .5f, 4f, 3f, .18f);
        require(!tracker.Sample(Vector3.zero, true, 0, settings), "Baseline selected");
        require(!tracker.AttributionSampleAccepted && tracker.AttributionMeasurementSequence == 0, "Baseline mislabeled as velocity sample");
        require(!tracker.Sample(Vector3.zero, true, .005, settings), "Short interval selected");
        require(!tracker.AttributionSampleAccepted, "Rejected interval marked accepted");
        require(!tracker.Sample(new Vector3(.01f, 0, 0), true, .02, settings), "Velocity warmup selected");
        require(tracker.AttributionSampleAccepted && tracker.AttributionMeasurementSequence == 1, "First measurement identity missing");
        require(Math.Abs(tracker.AttributionMeasurementDt - .02f) < 1e-6, "Actual measurement dt wrong");
        require(Math.Abs(tracker.Speed - .5f) < 1e-5 && !tracker.HasAcceleration, "Observability changed speed warmup");
        tracker.Sample(new Vector3(.01f, 0, 0), true, .04, settings);
        require(tracker.HasAcceleration && tracker.Acceleration > 20, "Deceleration calculation changed");
        require(tracker.Sample(new Vector3(.01f, 0, 0), true, .06, settings), "Stable stop no longer selects");
        long sequence = tracker.AttributionMeasurementSequence;
        require(!tracker.Sample(new Vector3(.01f, 0, 0), true, .065, settings), "Repeated contact selected again");
        require(!tracker.AttributionSampleAccepted && tracker.AttributionMeasurementSequence == sequence, "Repeated read invented a measurement");
        tracker.Reset();
        require(!tracker.AttributionSampleAccepted && tracker.AttributionMeasurementDt == 0, "Reset retained an accepted marker");
        tracker.Sample(Vector3.zero, true, .1, settings);
        tracker.Sample(Vector3.zero, true, .12, settings);
        require(tracker.AttributionMeasurementSequence > sequence, "Sequence reused after history reset");
        require(Math.Abs(tracker.AttributionMeasurementTime - .12) < 1e-9, "Measurement timestamp wrong");
        return checks;
    }
}

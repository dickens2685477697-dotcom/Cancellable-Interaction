/// <summary>
/// Diagnostic metadata from an EXISTING GetHandState call. SDK timestamps remain
/// in the SDK clock domain; they are not verified camera capture timestamps.
/// </summary>
public struct OVRHandSamplingMetadata
{
    public int HandInstanceId;
    public OVRPlugin.Hand Hand;
    public long ReadSequence;
    public OVRPlugin.Step Step;
    public int Frame;
    public double ReadStartedRealtime;
    public double ReadCompletedRealtime;
    public bool ReadSucceeded;
    public double SampleTimeStamp;
    public double RequestedTimeStamp;
    public OVRPlugin.HandStatus Status;
    public OVRPlugin.TrackingConfidence Confidence;
    public bool IsTracked;
}

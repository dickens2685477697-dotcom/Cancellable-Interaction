using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities;

/// <summary>A snapshot of one MRTK hand-joint callback, timed at application receipt.</summary>
public readonly struct Study3JointSample
{
    private readonly Dictionary<TrackedHandJoint, MixedRealityPose> joints;

    public uint SourceId { get; }
    public long TrackingEpoch { get; }
    public long Sequence { get; }
    public double Timestamp { get; }

    public Study3JointSample(uint sourceId, long trackingEpoch, long sequence, double timestamp,
        IDictionary<TrackedHandJoint, MixedRealityPose> joints)
    {
        SourceId = sourceId;
        TrackingEpoch = trackingEpoch;
        Sequence = sequence;
        Timestamp = timestamp;
        this.joints = joints == null ? null : new Dictionary<TrackedHandJoint, MixedRealityPose>(joints);
    }

    public bool TryGetJointPose(TrackedHandJoint joint, out MixedRealityPose pose)
    {
        if (joints != null) return joints.TryGetValue(joint, out pose);
        pose = default;
        return false;
    }
}

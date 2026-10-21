using UnityEngine;

public enum Study3SamplingMode
{
    [InspectorName("Project Default")]
    ProjectDefault = 0,
    [InspectorName("30 Hz (33.33 ms)")]
    Hz30 = 1,
    [InspectorName("50 Hz (20 ms)")]
    Hz50 = 2
}

/// <summary>
/// Owns a temporary, scene-wide FixedUpdate frequency override during play.
/// This controls algorithm updates, not the device's tracking or display rate.
/// </summary>
public static class Study3FixedSampling
{
    private static Object owner;
    private static float previousFixedDeltaTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        owner = null;
        previousFixedDeltaTime = 0f;
    }

    public static bool Apply(Object requester, Study3SamplingMode mode)
    {
        if (!Application.isPlaying || requester == null)
        {
            return false;
        }

        if (mode == Study3SamplingMode.ProjectDefault)
        {
            Release(requester);
            return true;
        }

        if (owner != null && owner != requester)
        {
            Debug.LogWarning("Only one active BallSpawner can override the fixed sampling rate. "
                + "Use Project Default on the other Spawners.", requester);
            return false;
        }

        float requestedHz;
        switch (mode)
        {
            case Study3SamplingMode.Hz30:
                requestedHz = 30f;
                break;
            case Study3SamplingMode.Hz50:
                requestedHz = 50f;
                break;
            default:
                return false;
        }

        if (owner == null)
        {
            previousFixedDeltaTime = Time.fixedDeltaTime;
            owner = requester;
        }

        Time.fixedDeltaTime = 1f / requestedHz;
        return true;
    }

    public static void Release(Object requester)
    {
        if (requester == null || owner != requester)
        {
            return;
        }

        Time.fixedDeltaTime = previousFixedDeltaTime;
        owner = null;
        previousFixedDeltaTime = 0f;
    }
}

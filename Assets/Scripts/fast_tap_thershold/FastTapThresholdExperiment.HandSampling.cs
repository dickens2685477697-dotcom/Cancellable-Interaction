using System.Collections.Generic;
using System.IO;

public partial class FastTapThresholdExperiment
{
    private Study3HandSamplingSession handSamplingSession;
    private void EnsureHandSamplingLog()
    {
        if (handSamplingSession != null || logger == null || attributionClosed) return;
        string directory = logger.GetLogDirectory();
        if (string.IsNullOrEmpty(directory)) return;
        handSamplingSession = Study3HandSamplingDiagnostics.StartSession(directory, Path.GetFileName(directory), () =>
            new Dictionary<string, object> {
                { "experiment", "fast_tap_threshold" }, { "phase", GetCurrentPhaseName() },
                { "trial", currentTrialIndex }, { "state", state.ToString() },
                { "participantId", attributionParticipantId }, { "protocolVersion", attributionProtocolVersion }
            });
    }
    private void StopHandSamplingLog()
    {
        handSamplingSession?.Dispose(); handSamplingSession = null;
    }
}

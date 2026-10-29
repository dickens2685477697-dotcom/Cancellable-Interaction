using System.Collections.Generic;

public partial class LogWriter3
{
    private Study3HandSamplingSession handSamplingSession;

    private void EnsureHandSamplingLog()
    {
        if (stateOnly || speedLogsClosed || handSamplingSession != null || string.IsNullOrEmpty(filePath)) return;
        handSamplingSession = Study3HandSamplingDiagnostics.StartSession(filePath, date, () =>
            new Dictionary<string, object> {
                { "session", date }, { "case", ballSpawner.currentCaseNumber }, { "id", ballSpawner.currentRoundID },
                { "mode", ballSpawner.modeSelected.ToString() }, { "experimentStarted", started },
                { "participantId", ballSpawner.studyParticipantId }, { "protocolVersion", ballSpawner.studyProtocolVersion }
            });
    }

    private void StopHandSamplingLog()
    {
        handSamplingSession?.Dispose(); handSamplingSession = null;
    }
}

// Sampling baseline diagnostics import verification (2026-10-28); no selection rule change.

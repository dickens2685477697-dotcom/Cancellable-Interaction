$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $projectRoot 'Assets\Scripts\Study3'

# Compile the actual callback and metadata properties, with a synchronous trial-end
# boundary matching BallSpawner.getNewTarget (write current row, then reset state).
# This deliberately avoids a second implementation of the callback under test.
function Get-SourceSection([string]$text, [string]$startMarker, [string]$endMarker) {
    $startIndex = $text.IndexOf($startMarker, [StringComparison]::Ordinal)
    if ($startIndex -lt 0) { throw "Missing source marker: $startMarker" }
    $endIndex = $text.IndexOf($endMarker, $startIndex, [StringComparison]::Ordinal)
    if ($endIndex -lt 0) { throw "Missing source marker: $endMarker" }
    return $text.Substring($startIndex, $endIndex - $startIndex)
}
$selection = Get-Content -LiteralPath (Join-Path $sourceRoot 'S2AccMode.cs') -Raw -Encoding utf8
$writer = Get-Content -LiteralPath (Join-Path $sourceRoot 'LogWriter3.cs') -Raw -Encoding utf8
$callback = Get-SourceSection $selection '    public void SetHesMode()' '    public void SetDwellReleaseMode()'
$metadata = Get-SourceSection $writer '    private string CurrentMotionRules' '    private string SessionConfiguration()'
$harness = @'
using System;
using System.Collections.Generic;

public sealed class StubEvent {
    private event Action listeners;
    public void AddListener(Action listener) { listeners += listener; }
    public void Invoke() { listeners?.Invoke(); }
}
public sealed class AcceStudy3 {
    public StubEvent InEvent = new StubEvent(), HesEvent = new StubEvent(), AfterOutEvent = new StubEvent();
    public char condition;
    public StubPose IndexTipPose2 = new StubPose();
}
public sealed class StubPose { public StubPosition Position = new StubPosition(); }
public sealed class StubPosition { public float x; }
public sealed class S2Player {
    public void TurnShallowYellowMineBall() {} public void TurnShallowGreenMineBall() {}
    public void ChangeOpacityBack() {}
}
public sealed class BallSpawner {
    public enum Mode { magicTap = 2, magicTap2 = 32, ixdlDynamic = 31, dwell = 1 }
    public Mode modeSelected = Mode.magicTap;
    public int area_type, index, intargetTimes, caseTime, goodSounds, wrongSounds;
    public char triggerCondition = ' ';
    public bool triggered;
    public void PlaySoundWrong() { wrongSounds++; }
    public void PlaySound2() { goodSounds++; }
}
public static class Study3MotionTracker {
    public const string RulesVersion = "shared-provider-v3";
    public const string TimestampKind = "mrtk_update_received_realtime";
}
public partial class LogWriter3 {
    public BallSpawner ballSpawner;
    public bool isMineBall, handInBall, successfully_Canceled;
    public float triggerTime;
    public string Rules => CurrentMotionRules;
    public string TimestampKind => CurrentMotionTimestampKind;
    public void RecordCancelReason(string reason) {}
}
public partial class S2AccMode {
    public readonly AcceStudy3 Acce = new AcceStudy3();
    public readonly S2Player Player = new S2Player();
    public BallSpawner ballSpawner;
    public LogWriter3 logWriter3;
    public bool isMineBall, handInBall2, inBall, useIndex;
    public int ballIndex = 7;
    public readonly List<char> WrittenConditions = new List<char>();
    public readonly List<bool> WrittenCancels = new List<bool>();
    private void CompleteSelection() {
        WrittenConditions.Add(ballSpawner.triggerCondition);
        WrittenCancels.Add(logWriter3.successfully_Canceled);
        // The next trial must remain clean after the old callback resumes.
        ballSpawner.triggerCondition = ' ';
    }
    private void ShowModeReadyRingOnIndexEntryIfNeeded() {}
    private void HideModeReadyRingOnIndexEntryIfNeeded() {}
    private void AttributionCancelReason(string reason) {}
    private static void print(object value) {}
}
public static class MagicTapLoggingChecks {
    private static int count;
    private static void Require(bool ok, string message) { count++; if (!ok) throw new Exception(message); }
    public static int Run() {
        foreach (bool mine in new[] { false, true }) {
            var host = new BallSpawner();
            var log = new LogWriter3 { ballSpawner = host };
            foreach (char condition in new[] { 'A', 'B' }) {
                var mode = new S2AccMode { ballSpawner = host, logWriter3 = log, isMineBall = mine };
                mode.SetHesMode();
                mode.Acce.condition = condition;
                mode.Acce.InEvent.Invoke();
                mode.Acce.HesEvent.Invoke();
                Require(mode.WrittenConditions.Count == 1, "One selection must produce one trial result");
                Require(mode.WrittenConditions[0] == condition, "Current A/B condition was not available at synchronous trial write");
                Require(host.triggerCondition == ' ', "Old callback contaminated the next trial with its A/B condition");
                Require(!mode.WrittenCancels[0], "Selection was logged as successful cancellation");
                Require(!log.handInBall && log.triggerTime == 0, "Post-selection timing cleanup changed");
            }
            Require(host.caseTime == 2 && host.intargetTimes == 2, "Trial or contact counting changed");
            Require(mine ? host.wrongSounds == 2 : host.goodSounds == 2, "Selection feedback changed");
            // Leaving a mine before confirmation remains a cancel, not a stale A/B trigger.
            var exit = new S2AccMode { ballSpawner = host, logWriter3 = log, isMineBall = mine };
            exit.SetHesMode(); exit.Acce.InEvent.Invoke(); exit.Acce.AfterOutEvent.Invoke();
            Require(exit.WrittenConditions.Count == (mine ? 1 : 0), "Exit outcome changed");
            if (mine) Require(exit.WrittenConditions[0] == ' ' && exit.WrittenCancels[0], "Cancel inherited an old trigger condition");
        }
        var writer = new LogWriter3 { ballSpawner = new BallSpawner() };
        Require(writer.Rules == "magictap-provider-v1", "Original Magic Tap must identify its own provider-based rules");
        Require(writer.TimestampKind == "mrtk_update_received_realtime", "Magic Tap is not sampled by a legacy fixed step");
        foreach (var mode in new[] { BallSpawner.Mode.magicTap2, BallSpawner.Mode.ixdlDynamic }) {
            writer.ballSpawner.modeSelected = mode;
            Require(writer.Rules == Study3MotionTracker.RulesVersion, "Shared-motion rule identifier changed");
            Require(writer.TimestampKind == Study3MotionTracker.TimestampKind, "Shared-motion timestamp identifier changed");
        }
        writer.ballSpawner.modeSelected = BallSpawner.Mode.dwell;
        Require(writer.Rules == "legacy_other_mode" && writer.TimestampKind == "legacy_fixed_step", "Unrelated mode metadata changed");
        return count;
    }
}
'@
$harness += "`npublic partial class S2AccMode {`n$callback`n}`npublic partial class LogWriter3 {`n$metadata`n}`n"
Add-Type -TypeDefinition $harness
$count = [MagicTapLoggingChecks]::Run()
Write-Output "Magic Tap logging validation passed: $count assertions (actual callback/property replay with stubbed trial boundary; not Unity or Quest)."

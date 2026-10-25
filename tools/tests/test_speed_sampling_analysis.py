import csv
import importlib.util
import re
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("speed_sampling", ROOT / "tools/analyze_speed_sampling.py")
ANALYSIS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(ANALYSIS)
HEADER = re.search(r'public const string Header = "([^"]+)"',
                   (ROOT / "Assets/Scripts/Study3/SpeedStudyTelemetry.cs").read_text(encoding="utf-8")).group(1).split(",")


class SpeedSamplingAnalysisTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.root = Path(self.directory.name)
        self.summary = self.root / "session-speed-entry-summary.csv"
        self.boundary = self.root / "session-speed-boundary-debug.csv"

    def tearDown(self):
        self.directory.cleanup()

    def row(self, entry, outcome="trigger", **changes):
        row = dict.fromkeys(HEADER, "0")
        row.update(entrySequence=str(entry), outcome=outcome, event=outcome, fixedSamplingHz="50",
                   source="target", mode="speed", id="3", isMineBall="0", minSpeedThreshold="0.067",
                   maxSpeedThreshold="0.375", dwellTimeoutSeconds="0.7", timeScale="1",
                   entryExperimentStarted="1", armedEver="1", entryElapsed="0.1")
        row.update(changes)
        return row

    def write(self, path, rows):
        with path.open("w", newline="", encoding="utf-8") as stream:
            writer = csv.DictWriter(stream, fieldnames=HEADER)
            writer.writeheader()
            writer.writerows(rows)

    def analyze(self, rows, boundary=None):
        self.write(self.summary, rows)
        self.write(self.boundary, boundary if boundary is not None else
                   [dict(row, event="enter") for row in rows])
        return ANALYSIS.summarize([self.root], self.root / "output")

    def test_denominator_and_tracking_sensitivity(self):
        groups, report = self.analyze([
            self.row(1), self.row(2, "cancel"), self.row(3, "exit", boundaryHighSpeedCandidate="1"),
            self.row(4, "aborted"), self.row(5, trackingLossObserved="1"),
        ])
        group = groups[0]
        self.assertEqual(group["resolved_entries"], 4)
        self.assertEqual(group["trigger_rate"], 0.5)
        self.assertEqual(group["aborted_entries"], 1)
        self.assertEqual(group["clean_resolved_entries"], 3)
        self.assertAlmostEqual(group["clean_trigger_rate"], 1 / 3)
        self.assertEqual(group["boundary_high_speed_candidates"], 1)
        self.assertEqual(report["entry_count"], 5)
        with (self.root / "output/boundary_candidates.csv").open(encoding="utf-8-sig") as stream:
            self.assertEqual(len(list(csv.DictReader(stream))), 1)

    def test_warmup_changed_parameters_and_duplicates(self):
        good = self.row(1)
        groups, report = self.analyze([
            good, good, self.row(2, entryExperimentStarted="0"),
            self.row(3, mixedSamplingRate="1"), self.row(4, settingsChanged="1"),
        ])
        self.assertEqual(groups[0]["recorded_entries"], 1)
        self.assertEqual(report["quality_counts"]["duplicate_summary_rows"], 1)
        self.assertEqual(report["quality_counts"]["warmup_entries_excluded"], 1)
        self.assertEqual(report["quality_counts"]["changed_settings_entries_excluded"], 2)

    def test_frequency_source_threshold_and_id_are_not_pooled(self):
        groups, _ = self.analyze([
            self.row(1), self.row(2, fixedSamplingHz="29.999998"),
            self.row(3, source="obstacle", isMineBall="1"), self.row(4, id="1"),
            self.row(5, maxSpeedThreshold="0.4"),
        ])
        self.assertEqual(len(groups), 5)
        self.assertIn(30, [group["fixed_hz"] for group in groups])

    def test_unfinished_entries_are_quality_findings_not_failures(self):
        groups, report = self.analyze([self.row(1)], [self.row(1, event="enter"), self.row(2, event="enter")])
        self.assertEqual(groups[0]["trigger_rate"], 1)
        self.assertEqual(report["entries_missing_outcome"][0]["entrySequence"], "2")

    def test_old_schema_and_truncated_rows_are_rejected(self):
        self.summary.write_text("case,id,outcome\n1,3,trigger\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "current Speed telemetry schema"):
            ANALYSIS.summarize([self.summary], self.root / "output")
        self.write(self.summary, [self.row(1)])
        with self.summary.open("a", encoding="utf-8") as stream:
            stream.write("1,2,3\n")
        with self.assertRaisesRegex(ValueError, "column count mismatch"):
            ANALYSIS.summarize([self.summary], self.root / "output")


if __name__ == "__main__":
    unittest.main()

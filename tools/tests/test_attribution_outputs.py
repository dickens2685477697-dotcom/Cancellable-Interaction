import collections
import csv
import hashlib
import importlib.util
import json
import re
import sys
import tempfile
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
from study3_trial_reader import PREFIX, TRIGGER, read_trial_file, strict_rows
from analyze_attribution_logs import analyze


def save_csv(path, header, rows):
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(header)
        writer.writerows(rows)


class AttributionOutputTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.folder = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def legacy(self, with_trigger=True):
        path = self.folder / "session-time.csv"
        prefix = ["NA"] * 26
        prefix[0:2] = ["1", "3"]
        prefix[21] = 'cancel, "quoted"'
        coordinates = [0, 1.1, .537] + [number for i in range(16) for number in (i * .01, 1.1, .537)]
        header = PREFIX + [axis for _ in range(15) for axis in ("targetX", "targetY", "targetZ")]
        if with_trigger:
            header += TRIGGER
        values = prefix + coordinates + (["0.02", "0.04", "0.03", "0.1"] if with_trigger else [])
        save_csv(path, header, [values])
        return path

    def test_legacy_coordinate_tail_does_not_become_speed(self):
        path = self.legacy()
        before = path.read_bytes()
        row = read_trial_file(path)[0]
        self.assertEqual(row["triggerSpeed"], "0.02")
        self.assertEqual(row["triggerAcceleration"], "0.1")
        self.assertEqual(row["cancelReason"], 'cancel, "quoted"')
        self.assertEqual(len(json.loads(row["targetPositionsJson"])), 16)
        self.assertEqual(row["legacyHeaderWidthMismatch"], "true")
        self.assertEqual(path.read_bytes(), before)

    def test_old_unrecorded_trigger_stays_missing(self):
        row = read_trial_file(self.legacy(False))[0]
        self.assertEqual([row[key] for key in TRIGGER], ["NA"] * 4)

    def test_new_sidecar_is_preferred_without_reading_broken_legacy(self):
        old = self.folder / "session-time.csv"
        old.write_text("invalid input", encoding="utf-8")
        new = self.folder / "session-trial-results-v2.csv"
        fields = PREFIX + TRIGGER + ["sessionId", "trialSequence", "targetPositionsJson", "schemaVersion"]
        values = ["NA"] * len(fields)
        values[fields.index("schemaVersion")] = "2"
        values[fields.index("triggerSpeed")] = "0.025"
        save_csv(new, fields, [values])
        self.assertEqual(read_trial_file(old)[0]["triggerSpeed"], "0.025")

    def test_strict_reader_rejects_width_and_duplicate_names(self):
        path = self.folder / "bad.csv"
        save_csv(path, ["a", "b"], [[1, 2, 3]])
        with self.assertRaises(ValueError):
            strict_rows(path)
        save_csv(path, ["a", "a"], [[1, 2]])
        with self.assertRaises(ValueError):
            strict_rows(path)

    def test_quality_keeps_abort_unknown_and_unclosed_separate(self):
        fields = ["sessionId", "entrySequence", "event", "outcome", "experimentStarted", "entryStartObserved", "trackingLossObserved", "taskResult", "mode", "source", "expectedAction"]
        rows = [
            ["s", "1", "trigger", "trigger", "1", "1", "0", "success", "pinchDwell", "target", "select"],
            ["s", "2", "aborted", "aborted", "1", "1", "1", "unknown", "pinchDwell", "target", "select"],
            ["s", "3", "trigger", "trigger", "1", "0", "0", "success", "pinchDwell", "target", "select"],
        ]
        save_csv(self.folder / "s-attribution-entry-summary.csv", fields, rows)
        events = [["s", str(i), "entry"] for i in range(1, 5)]
        save_csv(self.folder / "s-attribution-events.csv", ["sessionId", "entrySequence", "event"], events)
        save_csv(self.folder / "s-trial-lifecycle.csv", ["sessionId", "trialSequence", "event"], [["s", "1", "trial_start"], ["s", "1", "trial_end"], ["s", "2", "trial_start"]])
        report = analyze(self.folder)
        self.assertEqual(report["observed_resolved_entries"], 1)
        self.assertEqual(report["quality"]["aborted_entries"], 1)
        self.assertEqual(report["quality"]["unclosed_entries"], [["s", "4"]])
        self.assertEqual(report["quality"]["unclosed_trials"], [["s", "2"]])
        self.assertEqual(report["quality"]["terminal_without_observed_entry"], 1)

    def test_duplicate_summary_is_reported_and_not_counted_twice(self):
        fields = ["sessionId", "entrySequence", "event", "outcome", "experimentStarted", "entryStartObserved", "taskResult"]
        row = ["s", "1", "trigger", "trigger", "1", "1", "success"]
        save_csv(self.folder / "s-attribution-entry-summary.csv", fields, [row, row])
        report = analyze(self.folder)
        self.assertEqual(report["quality"]["duplicate_entry_summaries"], [["s", "1"]])
        self.assertEqual(report["observed_resolved_entries"], 0)

    def test_unclean_manifest_is_not_a_normal_completed_session(self):
        (self.folder / "s-attribution-manifest.json").write_text('{"loggingStatus":"open"}', encoding="utf-8")
        self.assertEqual(len(analyze(self.folder)["quality"]["manifests_not_normally_closed"]), 1)

    def test_parameters_changed_inside_entry_are_not_normal_results(self):
        fields = ["sessionId", "entrySequence", "outcome", "experimentStarted", "entryStartObserved", "settingsChangedDuringEntry"]
        save_csv(self.folder / "s-attribution-entry-summary.csv", fields, [["s", "1", "trigger", "1", "1", "1"]])
        report = analyze(self.folder)
        self.assertEqual(report["quality"]["entries_with_settings_change"], 1)
        self.assertEqual(report["observed_resolved_entries"], 0)


class LegacyContractTests(unittest.TestCase):
    def test_existing_headers_and_serializers_are_preserved(self):
        contract = json.loads((Path(__file__).parent / "attribution_legacy_contract.json").read_text(encoding="utf-8"))
        root = TOOLS.parent
        for relative, expected in contract["headers"].items():
            source = (root / relative).read_text(encoding="utf-8-sig")
            headers = re.findall(r'(?:sw\.WriteLine|File\.WriteAllText)\(\s*"([^"\n]+)"', source)
            # Only header strings are compared; actual per-row values are covered by serializer hashes.
            headers = [value for value in headers if "," in value]
            self.assertEqual(headers, expected, relative)
        for record in contract["serializers"]:
            source = (root / record["file"]).read_text(encoding="utf-8-sig").replace("\r\n", "\n")
            start = source.index(record["marker"])
            opening = source.index("{", start)
            depth, end = 1, opening + 1
            while depth:
                depth += (source[end] == "{") - (source[end] == "}")
                end += 1
            body = source[opening:end]
            self.assertEqual(hashlib.sha256(body.encode()).hexdigest(), record["sha256"], record["file"] + record["marker"])

    def test_effective_shared_scalar_settings_all_have_snapshot_fields(self):
        root = TOOLS.parent / "Assets/Scripts/Study3"
        shared = (root / "Study3SelectionSettings.cs").read_text(encoding="utf-8")
        shared = shared[:shared.index("    public void ApplyTo(")]
        fields = re.findall(r"public\s+(?:float|int|bool|BallSpawner\.RainbowArcPlacement)\s+(\w+)\s*(?:=|;)", shared)
        snapshot = (root / "Study3AttributionSettings.cs").read_text(encoding="utf-8")
        self.assertTrue(fields)
        for field in fields:
            self.assertIn(f'p["{field}"] = host.{field};', snapshot, field)


if __name__ == "__main__":
    unittest.main()

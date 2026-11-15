import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import pandas as pd


SCRIPT = Path(__file__).resolve().parents[1] / "analyze_study1.py"
spec = importlib.util.spec_from_file_location("study1_analysis", SCRIPT)
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)


class Study1AnalysisTests(unittest.TestCase):
    def test_failed_retry_and_unfinished(self):
        events = pd.DataFrame([dict(Event=k, Trial=t, ReceivedRealtime=i, SourceId=1, TrackingEpoch=1, ReferenceSpaceEpoch=0) for i, (k,t) in enumerate([
            ("enter",1), ("early_exit",1), ("enter",1), ("hold_complete",1), ("success",1), ("enter",2)])])
        pairs, issues = study.match_events(events)
        self.assertEqual(len(pairs), 1)
        self.assertEqual(pairs[0][0].ReceivedRealtime, 2)
        self.assertIn("unfinished", issues[0])

    def test_context_boundary(self):
        df = pd.DataFrame(dict(SourceId=[1,1,2,2,2], TrackingEpoch=[1,1,1,2,2], ReferenceSpaceEpoch=[0,0,0,0,1]))
        self.assertEqual(study.same_context(df).tolist(), [False, True, False, False, False])

    def test_empty_success_json_is_strict(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            pd.DataFrame([dict(ReceivedRealtime=1, SourceId=1, TrackingEpoch=1, SampleSequence=1, Inside=1, Velocity=None, VelocityValid=0, Acceleration=None, AccelerationValid=0)]).to_csv(root / "test-continuous.csv", index=False)
            pd.DataFrame([dict(Event="enter", Trial=1, ReceivedRealtime=1, SourceId=1, TrackingEpoch=1)]).to_csv(root / "test-events.csv", index=False)
            subprocess.run([sys.executable, str(SCRIPT), str(root), "--output", str(root / "out"), "--no-curves"], check=True, capture_output=True, text=True)
            raw = (root / "out" / "quality_summary.json").read_text(encoding="utf-8")
            data = json.loads(raw, parse_constant=lambda x: self.fail(f"Non-JSON constant {x}"))
            self.assertEqual(data["matched_successes"], 0)
            self.assertIsNone(data["dt_median_seconds"])


if __name__ == "__main__":
    unittest.main()

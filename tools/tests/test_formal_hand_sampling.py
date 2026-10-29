"""Synthetic checks for cross-log joins and time-window boundaries."""
import csv
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from analyze_formal_hand_sampling import analyze


class FormalChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.prefix = self.folder.name
        (self.folder / "baseline-analysis.json").write_text(json.dumps({
            "instrumented_window_counts_reconciled": True, "observed_counts": {}}), encoding="utf-8")
        self.updates = []
        for seq, sdk_time, provider_time, sdk_frame, stamp in [(1, .01, .02, 1, 1000), (2, .05, .06, 3, 1000), (3, .2, .21, 6, 1001)]:
            self.updates.append(dict(event="sdk_read", handedness="Right", sdkHandInstanceId=7,
                sdkReadSequence=seq, sdkSampleTimeStamp=stamp, sdkRequestedTimeStamp=stamp,
                providerUpdateId="", sourceId="", frame=sdk_frame, detailsJson=json.dumps({
                    "readSucceeded": True, "tracked": True, "step": "Render", "readCompletedRealtime": sdk_time})))
            for source, shared in [(42, seq * 2 - 1), (43, seq * 2)]:
                self.updates.append(dict(self.updates[-1], event="provider_update", providerUpdateId=shared,
                    sourceId=source, frame=sdk_frame + 1, detailsJson=json.dumps({
                        "providerReceivedRealtime": provider_time, "sharedSampleSequence": shared,
                        "matchStatus": "unique_oculus_skeleton_pose_match"})))
        self.common = []
        self.reads = []
        for index, (when, fixed, frame, seq, status, accepted, dt) in enumerate([
                (.04, .02, 3, 1, "baseline", True, 0),
                (.0405, .04, 3, 1, "duplicate_or_out_of_order", False, 0),
                (.08, .06, 5, 3, "velocity_warmup", True, .04)]):
            self.common.append(dict(experimentStarted=True, trackingValid=True, realtime=when,
                sourceInstanceId=9, sampleSourceId=42, trackingEpoch=1, mode="ixdlDynamic",
                sampleStatus=status, sampleSequence=seq, fixedTime=fixed, frame=frame,
                sampleTimestamp=.02 if seq == 1 else .06, sampleAccepted=accepted, selected=False,
                acceptedMotionDtSeconds=dt, hasVelocity=bool(dt)))
            self.reads.append(dict(ownerType="S2AccMode", joint="IndexTip", ownerInstanceId=9,
                sourceId=42, sharedSampleSequence=seq, fixedTime=fixed, frame=frame, realtime=when))

    def write(self):
        for suffix, data in [("hand-updates", self.updates), ("hand-reads", self.reads), ("common-motion", self.common)]:
            with (self.folder / (self.prefix + "-" + suffix + ".csv")).open("w", encoding="utf-8", newline="") as handle:
                writer = csv.DictWriter(handle, fieldnames=list(data[0]))
                writer.writeheader()
                writer.writerows(data)

    def test_lineage_duplicate_evidence_and_boundary_counts(self):
        self.write()
        originals = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in self.folder.glob("*.csv")}
        result, lineage, repeats = analyze(self.folder)
        consumer = result["consumer_groups"][0]
        self.assertEqual(consumer["mrtk_read_links"], 3)
        self.assertEqual(consumer["sdk_pose_match_candidate_links"], 3)
        self.assertEqual(consumer["sdk_to_read_frame_differences"], {2: 3})
        self.assertAlmostEqual(consumer["sdk_read_return_to_consumer_observation"]["p50_ms"], 30)
        self.assertEqual(result["duplicate_evidence_rows"], 1)
        self.assertTrue(result["all_duplicate_rows_same_frame_and_sequence"])
        self.assertTrue(result["all_duplicate_rows_skipped_without_selection"])
        self.assertAlmostEqual(repeats[0]["fixed_dt_ms"], 20)
        self.assertAlmostEqual(repeats[0]["callback_record_dt_ms"], .5)
        self.assertEqual(result["sdk_streams"][0]["tracked_reads"], 1)
        self.assertEqual(result["mrtk_hand_events_in_window"], 2)
        self.assertEqual(result["distinct_applied_sdk_states_referenced"], 1)
        self.assertTrue(result["all_states_referenced_by_two_sources_same_hand_same_frame"])
        self.assertEqual(consumer["selected_source_events_published_within_analysis_window"], 1)
        self.assertEqual(consumer["of_those_events_accepted_by_this_consumer"], 1)
        self.assertFalse(result["raw_hardware_first_sample_or_lossless_capture_proven"])
        self.assertEqual(originals, {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in self.folder.glob("*.csv")})

    def test_ambiguous_sdk_match_and_missing_consumer_cannot_invent_links(self):
        for row in self.updates:
            if row["event"] == "provider_update":
                details = json.loads(row["detailsJson"])
                details["matchStatus"] = "ambiguous_skeleton_match"
                row["detailsJson"] = json.dumps(details)
        self.reads[-1]["sourceId"] = 999
        self.write()
        result, lineage, repeats = analyze(self.folder)
        consumer = result["consumer_groups"][0]
        self.assertEqual(consumer["mrtk_read_links"], 2)
        self.assertEqual(consumer["sdk_pose_match_candidate_links"], 0)
        self.assertEqual(consumer["sdk_read_return_to_consumer_observation"]["count"], 0)
        self.assertIsNone(lineage[-1]["read_csv_line"])

    def test_partial_csv_is_rejected(self):
        self.write()
        with (self.folder / (self.prefix + "-common-motion.csv")).open("a", encoding="utf-8") as handle:
            handle.write("True,True\n")
        with self.assertRaisesRegex(ValueError, "malformed row"):
            analyze(self.folder)


if __name__ == "__main__":
    unittest.main()

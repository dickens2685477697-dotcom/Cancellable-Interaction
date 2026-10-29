import csv
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from analyze_hand_sampling_baseline import analyze, decode_json


class BaselineChecks(unittest.TestCase):
    def test_independent_json_decoder_is_strict_and_preserves_values(self):
        expected = {"confidence": "Low", "label": "右手, \"quoted\"\n", "tracked": False, "time": 82.915717799999584, "values": [None, 66127]}
        self.assertEqual(decode_json(json.dumps(expected)), expected)
        with self.assertRaises(ValueError):
            decode_json('{"confidence":"Low" invalid}')

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.updates = self.root / "test-hand-updates.csv"
        self.reads = self.root / "test-hand-reads.csv"
        self.update_rows = []
        self.read_rows = []

        def update(event, time, details, **extra):
            row = dict(row=len(self.update_rows) + 1, event=event, frame=1, time=time, fixedTime=.02,
                       realtime=time, inFixedTimeStep=0, sourceId="42", handedness="Right", providerUpdateId=0,
                       sdkHandInstanceId=7, sdkReadSequence=0, sdkSampleTimeStamp="NA", sdkRequestedTimeStamp="NA",
                       detailsJson=json.dumps(details), schemaVersion=1)
            row.update(extra)
            self.update_rows.append(row)

        update("session_start", 0, {"version": "hand-sampling-v1", "deviceModel": "synthetic fixture"})
        update("sdk_read", .001, {"readSucceeded": True, "tracked": True, "step": "Render", "readCompletedRealtime": .001},
               sdkReadSequence=1, sdkSampleTimeStamp=1000)
        update("provider_update", .002, {"providerReceivedRealtime": .002, "matchStatus": "unique_oculus_skeleton_pose_match"},
               sdkReadSequence=1, sdkSampleTimeStamp=1000, providerUpdateId=1)
        update("provider_update", .003, {"providerReceivedRealtime": .003, "matchStatus": "unique_oculus_skeleton_pose_match"},
               sdkReadSequence=1, sdkSampleTimeStamp=1000, providerUpdateId=2)
        update("sdk_read", .01, {"readSucceeded": True, "tracked": True, "step": "Render", "readCompletedRealtime": .01},
               sdkReadSequence=2, sdkSampleTimeStamp=1000)
        update("provider_update", .011, {"providerReceivedRealtime": .011, "matchStatus": "unique_oculus_skeleton_pose_match"},
               sdkReadSequence=2, sdkSampleTimeStamp=1000, providerUpdateId=3, frame=2)
        update("sdk_read", .015, {"readSucceeded": False, "tracked": False, "step": "Render", "readCompletedRealtime": .015},
               sdkReadSequence=3, frame=2)
        update("session_end", .02, {"observedSdkReadCount": 3, "providerUpdateCount": 3, "poseReadCount": 4}, frame=2)
        for index, (time, identity, repeated) in enumerate([(.004, 1, 0), (.005, 1, 1), (.012, 3, 0), (.016, 0, 0)], 1):
            self.read_rows.append(dict(readId=index, frame=1 if index < 3 else 2, time=time, fixedTime=index * .02,
                realtime=time, inFixedTimeStep=1, ownerInstanceId=9, ownerType="AcceStudy3", method="FixedUpdate",
                joint="IndexTip", poseValid=int(identity != 0), x="1.25" if identity else "NA", y=0, z=0,
                sourceId="42", handedness="Right", providerUpdateId=identity, sharedSampleSequence=0,
                matchStatus="matches_source_event_pose" if identity else "pose_unavailable",
                sameProviderUpdateAsPreviousRead=repeated, controllerPositionAvailable=int(identity != 0),
                motionSequence=index, schemaVersion=1))

    def write(self):
        for path, rows in ((self.updates, self.update_rows), (self.reads, self.read_rows)):
            with path.open("w", encoding="utf-8", newline="") as handle:
                writer = csv.DictWriter(handle, fieldnames=list(rows[0]))
                writer.writeheader()
                writer.writerows(rows)

    def test_separate_three_duplicate_definitions(self):
        self.write()
        result = analyze(self.updates, self.reads)
        sdk = result["sdk_reads"][0]
        provider = result["mrtk_provider_updates"][0]
        consumer = result["consumer_reads"][0]
        self.assertEqual(sdk["same_sdk_timestamp_pair_fraction"], 1)
        self.assertEqual(sdk["counts"]["read_failed"], 1)
        self.assertEqual(provider["counts"]["rows"], 3)
        self.assertEqual(provider["counts"]["consecutive_events_reusing_same_applied_sdk_read"], 1)
        self.assertAlmostEqual(consumer["same_provider_read_fraction"], 1 / 3)
        self.assertEqual(consumer["counts"]["reads_without_observed_provider_link"], 1)
        self.assertEqual(result["provider_events_not_linked_to_any_logged_read"], 1)
        self.assertTrue(result["instrumented_window_counts_reconciled"])
        self.assertFalse(result["hardware_first_result_or_lossless_capture_proven"])

    def test_unity_clock_stage_age_and_burst(self):
        self.write()
        result = analyze(self.updates, self.reads)
        consumer = result["consumer_reads"][0]
        self.assertAlmostEqual(consumer["linked_application_stage_age"]["p50_ms"], 2)
        self.assertAlmostEqual(result["same_frame_consecutive_fixed_intervals"]["min_ms"], 1)
        self.assertEqual(result["instrumented_fixed_ticks"], 4)
        # SDK timestamps near 1000 must never be subtracted from Unity times near zero.
        self.assertLess(result["mrtk_provider_updates"][0]["linked_application_stage_age"]["max_ms"], 10)

    def test_inputs_are_not_changed(self):
        self.write()
        before = [hashlib.sha256(p.read_bytes()).digest() for p in (self.updates, self.reads)]
        analyze(self.updates, self.reads)
        self.assertEqual(before, [hashlib.sha256(p.read_bytes()).digest() for p in (self.updates, self.reads)])

    def test_unclosed_or_wrong_counts_are_not_complete(self):
        self.update_rows.pop()
        self.write()
        self.assertFalse(analyze(self.updates, self.reads)["instrumented_window_counts_reconciled"])
        self.update_rows.append(dict(self.update_rows[0], event="session_end", detailsJson=json.dumps({
            "observedSdkReadCount": 999, "providerUpdateCount": 3, "poseReadCount": 4})))
        self.write()
        self.assertFalse(analyze(self.updates, self.reads)["instrumented_window_counts_reconciled"])

    def test_ambiguous_match_cannot_produce_sdk_age(self):
        for row in self.update_rows:
            if row["event"] == "provider_update":
                row["detailsJson"] = json.dumps({"providerReceivedRealtime": row["realtime"], "matchStatus": "ambiguous_skeleton_match"})
        self.write()
        provider = analyze(self.updates, self.reads)["mrtk_provider_updates"][0]
        self.assertEqual(provider["linked_application_stage_age"]["count"], 0)
        self.assertNotIn("linked_to_observed_sdk_read_candidate", provider["counts"])

    def test_partial_csv_is_rejected(self):
        self.write()
        with self.reads.open("a", encoding="utf-8") as handle:
            handle.write("999,1,0\n")
        with self.assertRaisesRegex(ValueError, "incomplete CSV row"):
            analyze(self.updates, self.reads)


if __name__ == "__main__":
    unittest.main()

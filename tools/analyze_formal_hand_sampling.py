"""Join formal common-motion observations to hand-sampling-v1 sidecars.

Works on closed snapshots. Rates are application observations, not physical
camera capture rates. A unique skeleton pose match remains candidate lineage.
"""
import argparse
from collections import Counter, defaultdict
import csv
import json
from pathlib import Path

from analyze_hand_sampling_baseline import decode_json, distribution, number, yes


def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            if None in row or any(value is None for value in row.values()):
                raise ValueError(f"{path}:{reader.line_num}: malformed row")
            row["_line"] = reader.line_num
            yield row


def event_rate(observations, key):
    if len(observations) < 2:
        return None
    span = observations[-1][key] - observations[0][key]
    return (len(observations) - 1) / span if span > 0 else None


def analyze(folder):
    prefix = folder.name
    full = decode_json((folder / "baseline-analysis.json").read_text(encoding="utf-8"))
    common = [r for r in rows(folder / (prefix + "-common-motion.csv"))
              if yes(r["experimentStarted"]) and yes(r["trackingValid"])]
    if not common:
        raise ValueError("No formal tracking-valid common-motion observations")
    start = min(float(r["realtime"]) for r in common)
    stop = max(float(r["realtime"]) for r in common)
    sdk = {}
    providers = {}
    sdk_window = defaultdict(list)
    provider_window = defaultdict(list)
    matched_states = defaultdict(list)
    matched_events = 0

    for row in rows(folder / (prefix + "-hand-updates.csv")):
        details = decode_json(row["detailsJson"])
        if row["event"] == "sdk_read":
            if not yes(details.get("readSucceeded")):
                continue
            value = {"hand": row["handedness"], "instance": row["sdkHandInstanceId"],
                     "sequence": row["sdkReadSequence"], "step": details.get("step"),
                     "time": number(details.get("readCompletedRealtime")), "frame": int(row["frame"]),
                     "stamp": number(row["sdkSampleTimeStamp"]), "requested": number(row["sdkRequestedTimeStamp"]),
                     "tracked": yes(details.get("tracked")), "line": row["_line"]}
            sdk[(value["instance"], value["sequence"])] = value
            if value["tracked"] and value["time"] is not None and start <= value["time"] <= stop:
                sdk_window[(value["instance"], value["hand"], value["step"])].append(value)
        elif row["event"] == "provider_update":
            value = {"id": row["providerUpdateId"], "source": row["sourceId"], "hand": row["handedness"],
                     "shared": str(details.get("sharedSampleSequence")), "frame": int(row["frame"]),
                     "time": number(details.get("providerReceivedRealtime")), "line": row["_line"],
                     "match": details.get("matchStatus"),
                     "sdk_key": (row["sdkHandInstanceId"], row["sdkReadSequence"])}
            providers[(value["source"], value["shared"])] = value
            if value["hand"] in ("Left", "Right") and value["time"] is not None and start <= value["time"] <= stop:
                provider_window[(value["source"], value["hand"])].append(value)
                if value["match"] == "unique_oculus_skeleton_pose_match":
                    matched_events += 1
                    matched_states[value["sdk_key"]].append(value)

    read_lookup = {}
    for row in rows(folder / (prefix + "-hand-reads.csv")):
        if row["ownerType"] not in ("S2AccMode", "S2Obstacles") or row["joint"] != "IndexTip":
            continue
        key = (row["ownerInstanceId"], row["sourceId"], row["sharedSampleSequence"], row["fixedTime"])
        if key in read_lookup:
            raise ValueError("More than one shared read for one common-motion observation key")
        read_lookup[key] = row

    grouped = defaultdict(list)
    for row in common:
        grouped[(row["sourceInstanceId"], row["sampleSourceId"], row["trackingEpoch"], row["mode"])].append(row)
    consumers = []
    repeat_evidence = []
    all_lineage = []
    for (owner, source, epoch, mode), group in grouped.items():
        status = Counter(r["sampleStatus"] for r in group)
        sdk_to_mrtk, mrtk_to_read, sdk_to_read = [], [], []
        frame_sdk_to_mrtk, frame_mrtk_to_read, frame_sdk_to_read = Counter(), Counter(), Counter()
        linked_reads, sdk_linked_reads = 0, 0
        accepted_published_ids = set()
        previous = None
        for observation in group:
            key = (owner, source, observation["sampleSequence"], observation["fixedTime"])
            read = read_lookup.get(key)
            provider = providers.get((source, observation["sampleSequence"]))
            lineage = {"common_csv_line": observation["_line"], "read_csv_line": read["_line"] if read else None,
                       "provider_csv_line": provider["line"] if provider else None, "sourceId": source,
                       "ownerInstanceId": owner, "sharedSampleSequence": observation["sampleSequence"],
                       "frame": observation["frame"], "sampleStatus": observation["sampleStatus"],
                       "sampleAccepted": observation["sampleAccepted"], "selected": observation["selected"],
                       "sdk_csv_line": None, "sdkHandInstanceId": None, "sdkReadSequence": None,
                       "sdk_to_mrtk_ms": None, "mrtk_to_read_ms": None, "sdk_to_read_ms": None}
            if read and provider:
                linked_reads += 1
                age = float(read["realtime"]) - provider["time"]
                mrtk_to_read.append(age)
                frame_mrtk_to_read[int(read["frame"]) - provider["frame"]] += 1
                lineage["mrtk_to_read_ms"] = age * 1000
                sdk_value = sdk.get(provider["sdk_key"]) if provider["match"] == "unique_oculus_skeleton_pose_match" else None
                if sdk_value and sdk_value["time"] is not None:
                    sdk_linked_reads += 1
                    sdk_age = provider["time"] - sdk_value["time"]
                    total_age = float(read["realtime"]) - sdk_value["time"]
                    sdk_to_mrtk.append(sdk_age)
                    sdk_to_read.append(total_age)
                    frame_sdk_to_mrtk[provider["frame"] - sdk_value["frame"]] += 1
                    frame_sdk_to_read[int(read["frame"]) - sdk_value["frame"]] += 1
                    lineage.update(sdk_csv_line=sdk_value["line"], sdkHandInstanceId=sdk_value["instance"],
                                   sdkReadSequence=sdk_value["sequence"], sdk_to_mrtk_ms=sdk_age * 1000,
                                   sdk_to_read_ms=total_age * 1000)
            if yes(observation["sampleAccepted"]) and provider:
                accepted_published_ids.add(provider["id"])
            if observation["sampleStatus"] == "duplicate_or_out_of_order":
                evidence = dict(lineage)
                evidence.update(previous_common_csv_line=previous["_line"] if previous else None,
                    same_frame=bool(previous and previous["frame"] == observation["frame"]),
                    same_sequence=bool(previous and previous["sampleSequence"] == observation["sampleSequence"]),
                    same_sample_timestamp=bool(previous and previous["sampleTimestamp"] == observation["sampleTimestamp"]),
                    fixed_dt_ms=(float(observation["fixedTime"]) - float(previous["fixedTime"])) * 1000 if previous else None,
                    callback_record_dt_ms=(float(observation["realtime"]) - float(previous["realtime"])) * 1000 if previous else None)
                repeat_evidence.append(evidence)
            all_lineage.append(lineage)
            previous = observation
        span = float(group[-1]["realtime"]) - float(group[0]["realtime"])
        candidates = [p for streams in provider_window.values() for p in streams if p["source"] == source]
        accepted_in_window = len({p["id"] for p in candidates} & accepted_published_ids)
        consumers.append({"ownerInstanceId": owner, "sourceId": source, "trackingEpoch": epoch, "mode": mode,
            "rows": len(group), "span_seconds": span,
            "callback_hz": (len(group) - 1) / span if span > 0 else None,
            "callback_record_intervals": distribution(float(b["realtime"]) - float(a["realtime"]) for a, b in zip(group, group[1:])),
            "estimated_application_fps": (int(group[-1]["frame"]) - int(group[0]["frame"])) / span if span > 0 else None,
            "statuses": dict(status), "repeated_or_out_of_order_reads": status["duplicate_or_out_of_order"],
            "repeated_fraction": status["duplicate_or_out_of_order"] / len(group),
            "accepted_samples_including_warmup": sum(yes(r["sampleAccepted"]) for r in group),
            "accepted_observation_rate_hz": (sum(yes(r["sampleAccepted"]) for r in group) - 1) / span if span > 0 else None,
            "accepted_sample_interval_ms": distribution(float(r["acceptedMotionDtSeconds"]) for r in group if yes(r["sampleAccepted"]) and yes(r["hasVelocity"])),
            "mrtk_read_links": linked_reads, "sdk_pose_match_candidate_links": sdk_linked_reads,
            "sdk_read_return_to_mrtk_receive": distribution(sdk_to_mrtk),
            "mrtk_receive_to_consumer_observation": distribution(mrtk_to_read),
            "sdk_read_return_to_consumer_observation": distribution(sdk_to_read),
            "sdk_to_mrtk_frame_differences": dict(frame_sdk_to_mrtk),
            "mrtk_to_read_frame_differences": dict(frame_mrtk_to_read),
            "sdk_to_read_frame_differences": dict(frame_sdk_to_read),
            "selected_source_events_published_within_analysis_window": len(candidates),
            "of_those_events_accepted_by_this_consumer": accepted_in_window,
            "published_events_not_accepted_within_window": len(candidates) - accepted_in_window,
            "unaccepted_note": "Latest-value polling may overwrite intermediate events; inactive detector/edge effects also matter. Not sensor or transport loss."})

    sdk_results = []
    for (instance, hand, step), group in sdk_window.items():
        stamps = [p["stamp"] for p in group]
        pairs = [(a, b) for a, b in zip(stamps, stamps[1:]) if a is not None and b is not None and a > 0 and b > 0]
        sdk_results.append({"instanceId": instance, "handedness": hand, "step": step, "tracked_reads": len(group),
            "observed_read_hz": event_rate(group, "time"),
            "observed_read_intervals": distribution(b["time"] - a["time"] for a, b in zip(group, group[1:])),
            "valid_sample_timestamps": sum(v is not None and v > 0 for v in stamps),
            "timestamp_comparable_pairs": len(pairs), "equal_timestamp_pairs": sum(a == b for a, b in pairs),
            "requested_equals_sample_rows": sum(p["requested"] is not None and p["requested"] == p["stamp"] for p in group),
            "timestamp_intervals": distribution(b - a for a, b in pairs),
            "note": "SDK timestamp changes do not prove unique raw camera captures; GetHandState is predicted-pose API."})
    report = {"session": prefix, "analysis_start_realtime": start, "analysis_stop_realtime": stop,
        "scope": "Formal, tracking-valid common-motion span; SDK/provider rates restricted to this same time window.",
        "instrumented_window_counts_reconciled": full["instrumented_window_counts_reconciled"],
        "whole_session_observed_counts": full["observed_counts"], "hardware_capture_rate_measured": False,
        "raw_hardware_first_sample_or_lossless_capture_proven": False,
        "latency_scope": "All stage ages use Unity realtime. SDK-to-MRTK uses pose-match candidate lineage. No SDK clock is subtracted from Unity clock.",
        "phase_scope": "The instructed static/move/stop phases have no exact logged boundaries; report only their combined interval.",
        "sdk_streams": sdk_results,
        "mrtk_source_streams": [{"sourceId": key[0], "handedness": key[1], "events": len(value),
                                  "observed_hz": event_rate(value, "time"),
                                  "observed_event_intervals": distribution(b["time"] - a["time"] for a, b in zip(value, value[1:]))} for key, value in provider_window.items()],
        "mrtk_hand_events_in_window": sum(len(value) for value in provider_window.values()),
        "sdk_pose_match_events_in_window": matched_events,
        "distinct_applied_sdk_states_referenced": len(matched_states),
        "mrtk_events_per_referenced_sdk_state": dict(Counter(len(v) for v in matched_states.values())),
        "all_states_referenced_by_two_sources_same_hand_same_frame": all(len(v) == 2 and len({p["source"] for p in v}) == 2
            and len({p["hand"] for p in v}) == 1 and len({p["frame"] for p in v}) == 1 for v in matched_states.values()),
        "consumer_groups": consumers,
        "duplicate_evidence_rows": len(repeat_evidence),
        "all_duplicate_rows_same_frame_and_sequence": all(r["same_frame"] and r["same_sequence"] and r["same_sample_timestamp"] for r in repeat_evidence),
        "all_duplicate_rows_skipped_without_selection": all(not yes(r["sampleAccepted"]) and not yes(r["selected"]) for r in repeat_evidence),
        "repeat_record_intervals": distribution(r["callback_record_dt_ms"] / 1000 for r in repeat_evidence),
    }
    return report, all_lineage, repeat_evidence


def write_csv(path, data):
    if not data:
        return
    with path.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(data[0]))
        writer.writeheader()
        writer.writerows(data)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("snapshot_folder", type=Path)
    args = parser.parse_args()
    report, lineage, repeats = analyze(args.snapshot_folder)
    (args.snapshot_folder / "formal-summary.json").write_text(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    write_csv(args.snapshot_folder / "formal-lineage.csv", lineage)
    write_csv(args.snapshot_folder / "formal-repeated-consumptions.csv", repeats)
    print(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False))

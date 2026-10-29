"""Read-only analysis of hand-sampling-v1 sidecars; never estimates camera FPS.

Each report keeps SDK polling, SDK timestamp reuse, MRTK publications, and
consumer reuse separate. SDK-to-MRTK joins are pose-match candidates, not
hardware frame identities. Inputs are never rewritten.
"""
import argparse
from collections import Counter, defaultdict
import csv
import hashlib
import json
import math
from pathlib import Path


def decode_json(value):
    return json.loads(value)


def number(value):
    try:
        value = float(value)
        return value if math.isfinite(value) else None
    except (ValueError, TypeError):
        return None


def yes(value):
    return str(value).lower() in ("1", "true")


def distribution(values):
    values = sorted(v for v in values if v is not None and math.isfinite(v))
    if not values:
        return {"count": 0, "min_ms": None, "p50_ms": None, "p95_ms": None, "p99_ms": None, "max_ms": None}

    def quantile(q):
        index = (len(values) - 1) * q
        lower = int(index)
        upper = min(lower + 1, len(values) - 1)
        return (values[lower] + (values[upper] - values[lower]) * (index - lower)) * 1000

    return {"count": len(values), "min_ms": values[0] * 1000, "p50_ms": quantile(.5),
            "p95_ms": quantile(.95), "p99_ms": quantile(.99), "max_ms": values[-1] * 1000}


class Stream:
    def __init__(self):
        self.counts = Counter()
        self.times = []
        self.latencies = []
        self.previous = None
        self.stamps = set()

    def observe(self, time):
        self.counts["rows"] += 1
        if time is not None:
            if self.times and time < self.times[-1]:
                self.counts["non_monotonic_record_times"] += 1
            self.times.append(time)

    def result(self):
        span = max(self.times) - min(self.times) if len(self.times) > 1 else 0
        pairs = self.counts["comparable_sdk_timestamp_pairs"]
        identified = self.counts["reads_linked_to_observed_provider"]
        return {"counts": dict(self.counts),
                "observed_span_seconds": span,
                "observed_record_rate_hz": (len(self.times) - 1) / span if span > 0 and not self.counts["non_monotonic_record_times"] else None,
                "consecutive_record_intervals": distribution(b - a for a, b in zip(self.times, self.times[1:]) if b >= a),
                "distinct_sdk_timestamp_values": len(self.stamps),
                "same_sdk_timestamp_pair_fraction": self.counts["same_sdk_timestamp_pairs"] / pairs if pairs else None,
                "same_provider_read_fraction": self.counts["same_provider_flag_on_linked_reads"] / identified if identified else None,
                "linked_application_stage_age": distribution(self.latencies)}


def read_rows(path, required):
    with path.open(encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        missing = set(required) - set(reader.fieldnames or [])
        if missing:
            raise ValueError(f"{path}: missing columns {sorted(missing)}")
        for line, row in enumerate(reader, 2):
            if None in row or any(value is None for value in row.values()):
                raise ValueError(f"{path}:{line}: incomplete CSV row; close/flush and copy the log again")
            yield row


def analyze(updates_path, reads_path):
    sdk_groups = defaultdict(Stream)
    provider_groups = defaultdict(Stream)
    read_groups = defaultdict(Stream)
    sdk_by_read = {}
    providers = {}
    used_providers = set()
    joins = Counter()
    observed = Counter()
    closed_counts = Counter()
    frame_times = {}
    fixed_ticks = {}
    lifecycle = Counter()
    starts = []

    def timing(row, time):
        frame = number(row.get("frame"))
        if frame is not None and time is not None:
            frame = int(frame)
            frame_times[frame] = min(frame_times.get(frame, time), time)
            fixed = number(row.get("fixedTime"))
            if yes(row.get("inFixedTimeStep")) and fixed is not None:
                key = (frame, fixed)
                fixed_ticks[key] = min(fixed_ticks.get(key, time), time)

    def sdk_reference(row):
        return (row.get("sdkHandInstanceId"), row.get("sdkReadSequence"))

    for row in read_rows(updates_path, ["event", "realtime", "detailsJson", "sourceId", "providerUpdateId", "sdkReadSequence"]):
        details = decode_json(row["detailsJson"])
        if not isinstance(details, dict):
            raise ValueError("detailsJson must be an object")
        time = number(row.get("realtime"))
        timing(row, time)
        event = row["event"]
        lifecycle[event] += 1
        if event == "session_start":
            starts.append(details)
        elif event == "session_end":
            for key in ("observedSdkReadCount", "providerUpdateCount", "poseReadCount"):
                value = number(details.get(key))
                if value is not None:
                    closed_counts[key] += int(value)
        elif event == "sdk_read":
            observed["observedSdkReadCount"] += 1
            key = (row.get("sdkHandInstanceId"), row.get("handedness"), details.get("step"))
            group = sdk_groups[key]
            completed = number(details.get("readCompletedRealtime"))
            group.observe(completed)
            successful = yes(details.get("readSucceeded"))
            tracked = yes(details.get("tracked"))
            stamp = number(row.get("sdkSampleTimeStamp"))
            group.counts["read_succeeded" if successful else "read_failed"] += 1
            group.counts["tracked" if tracked else "not_tracked"] += 1
            usable = successful and tracked and stamp is not None and stamp > 0
            if usable:
                group.stamps.add(stamp)
                group.counts["tracked_valid_sdk_timestamp_rows"] += 1
                if group.previous is not None:
                    group.counts["comparable_sdk_timestamp_pairs"] += 1
                    relation = "same" if stamp == group.previous else "advanced" if stamp > group.previous else "regressed"
                    group.counts[relation + "_sdk_timestamp_pairs"] += 1
                group.previous = stamp
            else:
                group.previous = None
            sdk_by_read[sdk_reference(row)] = {"completed": completed, "step": details.get("step"), "stamp": stamp}
        elif event == "provider_update":
            observed["providerUpdateCount"] += 1
            source = row["sourceId"]
            group = provider_groups[(source, row.get("handedness"))]
            received = number(details.get("providerReceivedRealtime"))
            group.observe(received)
            match = details.get("matchStatus", "unavailable")
            group.counts[match] += 1
            joins[match] += 1
            reference = sdk_reference(row)
            sdk = sdk_by_read.get(reference)
            linked = match == "unique_oculus_skeleton_pose_match" and sdk is not None
            if linked:
                group.counts["linked_to_observed_sdk_read_candidate"] += 1
                if group.previous == reference:
                    group.counts["consecutive_events_reusing_same_applied_sdk_read"] += 1
                group.previous = reference
                if received is not None and sdk["completed"] is not None:
                    age = received - sdk["completed"]
                    if age >= 0:
                        group.latencies.append(age)
                    else:
                        group.counts["negative_sdk_to_provider_age"] += 1
            else:
                group.previous = None
            identity = row["providerUpdateId"]
            if identity in providers:
                raise ValueError("Duplicate providerUpdateId in one session file; split app process restarts before analysis")
            providers[identity] = {"received": received, "source": source, "sdk_link_candidate": linked}

    for row in read_rows(reads_path, ["readId", "realtime", "sourceId", "providerUpdateId", "sameProviderUpdateAsPreviousRead"]):
        observed["poseReadCount"] += 1
        time = number(row.get("realtime"))
        timing(row, time)
        key = (row.get("ownerInstanceId"), row.get("ownerType"), row.get("method"), row.get("joint"), row.get("sourceId"), row.get("handedness"))
        group = read_groups[key]
        group.observe(time)
        group.counts["pose_valid" if yes(row.get("poseValid")) else "pose_unavailable"] += 1
        group.counts[row.get("matchStatus", "unavailable")] += 1
        if row.get("controllerPositionAvailable") in ("0", "False", "false"):
            group.counts["controller_position_unavailable"] += 1
        identity = row["providerUpdateId"]
        provider = providers.get(identity) if identity != "0" else None
        if provider and provider["source"] == row["sourceId"]:
            group.counts["reads_linked_to_observed_provider"] += 1
            used_providers.add(identity)
            if yes(row.get("sameProviderUpdateAsPreviousRead")):
                group.counts["same_provider_flag_on_linked_reads"] += 1
            if time is not None and provider["received"] is not None:
                age = time - provider["received"]
                if age >= 0:
                    group.latencies.append(age)
                else:
                    group.counts["negative_provider_to_read_age"] += 1
        else:
            group.counts["reads_without_observed_provider_link"] += 1

    ticks = sorted(((frame, fixed, time) for (frame, fixed), time in fixed_ticks.items()), key=lambda item: item[1])
    burst_intervals = [b[2] - a[2] for a, b in zip(ticks, ticks[1:]) if b[0] == a[0] and b[1] > a[1]]
    frames = sorted(frame_times)
    span = frame_times[frames[-1]] - frame_times[frames[0]] if len(frames) > 1 else 0
    reconciled = lifecycle["session_start"] > 0 and lifecycle["session_start"] == lifecycle["session_end"] and observed == closed_counts

    def export(groups, labels):
        return [dict(zip(labels, key), **value.result()) for key, value in sorted(groups.items(), key=lambda pair: str(pair[0]))]

    return {
        "scope": "Only the instrumented logging window; event rates are not hardware capture rates.",
        "rate_definition": "(timed rows - 1) / (last timestamp - first timestamp), per hand/step or per consumer; inactive gaps remain in the span.",
        "hardware_first_result_or_lossless_capture_proven": False,
        "sdk_to_mrtk_link_semantics": "Unique active skeleton pose match is a candidate attribution, not a hardware frame identity.",
        "read_time_semantics": "hand-reads realtime is sampled during logging after the original query; not sensor capture time or exact query entry time.",
        "sources": [{"path": str(p.resolve()), "sha256": hashlib.sha256(p.read_bytes()).hexdigest()} for p in (updates_path, reads_path)],
        "session_metadata": starts, "lifecycle": dict(lifecycle),
        "observed_counts": dict(observed), "session_end_reported_counts": dict(closed_counts),
        "instrumented_window_counts_reconciled": reconciled,
        "sdk_to_provider_match_counts": dict(joins),
        "provider_events_not_linked_to_any_logged_read": len(providers) - len(used_providers),
        "unconsumed_event_note": "May include the unselected hand, inactive detectors, or deliberately overwritten latest samples; not a hardware loss rate.",
        "estimated_application_fps": (frames[-1] - frames[0]) / span if span > 0 else None,
        "instrumented_fixed_ticks": len(ticks),
        "same_frame_consecutive_fixed_intervals": distribution(burst_intervals),
        "fixed_tick_note": "Deduplicated frame/fixedTime from instrumented callbacks only; missing callbacks outside logging cannot be recovered.",
        "sdk_reads": export(sdk_groups, ["handInstanceId", "handedness", "step"]),
        "mrtk_provider_updates": export(provider_groups, ["sourceId", "handedness"]),
        "consumer_reads": export(read_groups, ["ownerInstanceId", "ownerType", "method", "joint", "sourceId", "handedness"]),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("updates_csv", type=Path)
    parser.add_argument("--reads", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    reads = args.reads or args.updates_csv.with_name(args.updates_csv.name.replace("-hand-updates.csv", "-hand-reads.csv"))
    if reads == args.updates_csv or not args.updates_csv.name.endswith("-hand-updates.csv"):
        parser.error("Input must end with -hand-updates.csv")
    if args.output.resolve() in (args.updates_csv.resolve(), reads.resolve()):
        parser.error("Output must not overwrite an input")
    result = analyze(args.updates_csv, reads)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    print(json.dumps({"report": str(args.output), "observed_counts": result["observed_counts"],
                      "instrumented_window_counts_reconciled": result["instrumented_window_counts_reconciled"],
                      "hardware_first_result_or_lossless_capture_proven": False}, ensure_ascii=False))


if __name__ == "__main__":
    main()

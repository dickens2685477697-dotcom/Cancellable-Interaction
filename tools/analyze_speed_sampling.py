"""Summarize the Speed entry logs produced by Study3 (Python standard library only)."""
from __future__ import annotations

import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path
from statistics import median


def read_rows(path: Path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream)
        required = {"entrySequence", "outcome", "fixedSamplingHz", "entryExperimentStarted",
                    "mixedSamplingRate", "settingsChanged", "boundaryHighSpeedCandidate"}
        if not required.issubset(reader.fieldnames or []):
            raise ValueError(f"{path}: not the current Speed telemetry schema; collect fresh logs")
        for line, row in enumerate(reader, 2):
            if None in row or any(value is None for value in row.values()):
                raise ValueError(f"{path}:{line}: CSV column count mismatch")
            yield row


def write_csv(path: Path, rows: list[dict], fields: list[str]):
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)


def summarize(inputs: list[Path], output: Path):
    files = sorted({path.resolve() for root in inputs
                    for path in ([root] if root.is_file() else root.rglob("*-speed-entry-summary.csv"))
                    if path.name.endswith("-speed-entry-summary.csv")})
    if not files:
        raise ValueError("No *-speed-entry-summary.csv files found; old time/trajectory logs are insufficient")
    details, candidates = [], []
    groups = defaultdict(list)
    quality = Counter()
    seen = set()
    unmatched = []
    for path in files:
        session = str(path)
        session_entries = set()
        for row in read_rows(path):
            key = (session, row["entrySequence"])
            if key in seen:
                quality["duplicate_summary_rows"] += 1
                continue
            seen.add(key)
            session_entries.add(row["entrySequence"])
            row = {"session_file": session, **row}
            details.append(row)
            if row["entryExperimentStarted"] != "1":
                quality["warmup_entries_excluded"] += 1
                continue
            if row["mixedSamplingRate"] == "1" or row["settingsChanged"] == "1":
                quality["changed_settings_entries_excluded"] += 1
                continue
            if row["trackingLossObserved"] == "1":
                quality["tracking_loss_entries"] += 1
            group = (round(float(row["fixedSamplingHz"]), 3), row["source"], row["mode"], row["id"],
                     row["isMineBall"], row["minSpeedThreshold"], row["maxSpeedThreshold"],
                     row["dwellTimeoutSeconds"], row["timeScale"])
            groups[group].append(row)
            if row["boundaryHighSpeedCandidate"] == "1":
                candidates.append(row)
        boundary = path.with_name(path.name.replace("-speed-entry-summary.csv", "-speed-boundary-debug.csv"))
        if boundary.exists():
            entered = {row["entrySequence"] for row in read_rows(boundary) if row["event"] == "enter"}
            unmatched.extend({"session_file": session, "entrySequence": entry}
                             for entry in sorted(entered - session_entries, key=int))
        else:
            quality["missing_boundary_files"] += 1
    summaries = []
    group_fields = ["fixed_hz", "source", "mode", "id", "is_mine_ball", "min_speed_threshold",
                    "max_speed_threshold", "dwell_timeout_seconds", "time_scale"]
    for group, rows in sorted(groups.items()):
        outcomes = Counter(row["outcome"] for row in rows)
        resolved = [row for row in rows if row["outcome"] in {"trigger", "cancel", "exit"}]
        clean = [row for row in resolved if row["trackingLossObserved"] == "0"]
        times = [float(row["entryElapsed"]) * 1000 for row in resolved if row["outcome"] == "trigger"]
        summaries.append(dict(zip(group_fields, group)) | {
            "recorded_entries": len(rows), "resolved_entries": len(resolved),
            "trigger_entries": outcomes["trigger"], "cancel_entries": outcomes["cancel"],
            "exit_without_trigger_entries": outcomes["exit"], "aborted_entries": outcomes["aborted"],
            "trigger_rate": outcomes["trigger"] / len(resolved) if resolved else "NA",
            "armed_entries": sum(row["armedEver"] == "1" for row in resolved),
            "boundary_high_speed_candidates": sum(row["boundaryHighSpeedCandidate"] == "1" for row in resolved),
            "tracking_loss_entries": sum(row["trackingLossObserved"] == "1" for row in resolved),
            "clean_resolved_entries": len(clean),
            "clean_trigger_rate": sum(row["outcome"] == "trigger" for row in clean) / len(clean) if clean else "NA",
            "median_trigger_latency_ms": median(times) if times else "NA",
        })
    output.mkdir(parents=True, exist_ok=True)
    fields = group_fields + ["recorded_entries", "resolved_entries", "trigger_entries", "cancel_entries",
        "exit_without_trigger_entries", "aborted_entries", "trigger_rate", "armed_entries",
        "boundary_high_speed_candidates", "tracking_loss_entries", "clean_resolved_entries",
        "clean_trigger_rate", "median_trigger_latency_ms"]
    detail_fields = list(details[0]) if details else ["session_file", "entrySequence", "outcome"]
    write_csv(output / "speed_sampling_summary.csv", summaries, fields)
    write_csv(output / "speed_entry_detail.csv", details, detail_fields)
    write_csv(output / "boundary_candidates.csv", candidates, detail_fields)
    report = {
        "input_files": [str(path) for path in files], "entry_count": len(details),
        "quality_counts": dict(quality), "entries_missing_outcome": unmatched,
        "definition": "trigger_rate = trigger entries / (trigger + cancel + exit entries). Aborted entries are excluded.",
        "hazard_interpretation": "For obstacle rows or is_mine_ball=1, a trigger is an unintended activation.",
        "cautions": [
            "This is an entry-level rate, not the previous trial-level reentry rate.",
            "Warmup and entries with changing sampling rate/thresholds/time scale are excluded from group summaries.",
            "Tracking-loss entries are retained in trigger_rate; clean_trigger_rate excludes them.",
            "Compare the cancellation settings in session-settings.csv before pooling sessions.",
            "A boundary candidate identifies an observed crossing; it does not prove when the continuous motion crossed the speed threshold.",
            "Configured FixedUpdate Hz is not the device's hardware tracking refresh rate.",
        ],
    }
    (output / "quality_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    return summaries, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("inputs", nargs="+", type=Path, help="Session directory, parent directory, or entry-summary CSV")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    summaries, report = summarize(args.inputs, args.output)
    print(f"Analyzed {report['entry_count']} entries in {len(report['input_files'])} sessions; {len(summaries)} groups.")
    print(f"Entries missing an outcome: {len(report['entries_missing_outcome'])}")
    print(f"Output: {args.output.resolve()}")


if __name__ == "__main__":
    main()

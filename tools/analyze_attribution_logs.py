"""Check attribution denominators and incomplete attempts; never infer missing outcomes."""
from __future__ import annotations

import argparse
import collections
import json
from pathlib import Path

from study3_trial_reader import strict_rows


def truth(value: str) -> bool:
    return str(value).lower() in {"true", "1"}


def analyze(folder: Path) -> dict:
    events, summaries, lifecycle = [], [], []
    errors = []
    for suffix, target in [("attribution-events", events), ("attribution-entry-summary", summaries), ("trial-lifecycle", lifecycle)]:
        for path in sorted(folder.rglob(f"*-{suffix}.csv")):
            try:
                target.extend(strict_rows(path))
            except ValueError as error:
                errors.append(str(error))
    key = lambda row: (row["sessionId"], row["entrySequence"])
    entered = {key(row) for row in events if row.get("event") == "entry"}
    counts = collections.Counter(key(row) for row in summaries)
    duplicates = [list(identity) for identity, count in counts.items() if count > 1]
    formal = [row for row in summaries if truth(row.get("experimentStarted", "0")) and counts[key(row)] == 1]
    resolved = [row for row in formal if row.get("outcome") != "aborted" and truth(row.get("entryStartObserved", "0")) and not truth(row.get("settingsChangedDuringEntry", "0"))]
    quality = {
        "csv_errors": errors,
        "duplicate_entry_summaries": duplicates,
        "unclosed_entries": [list(identity) for identity in sorted(entered - counts.keys())],
        "terminal_without_observed_entry": sum(not truth(row.get("entryStartObserved", "0")) for row in formal),
        "entries_with_tracking_loss": sum(truth(row.get("trackingLossObserved", "0")) for row in formal),
        "aborted_entries": sum(row.get("outcome") == "aborted" for row in formal),
        "entries_with_settings_change": sum(truth(row.get("settingsChangedDuringEntry", "0")) for row in formal),
        "attribution_files_missing": not bool(events or summaries or lifecycle),
    }
    trial_starts = {(row["sessionId"], row["trialSequence"]) for row in lifecycle if row.get("event") == "trial_start"}
    trial_ends = {(row["sessionId"], row["trialSequence"]) for row in lifecycle if row.get("event") in {"trial_end", "trial_aborted"}}
    quality["unclosed_trials"] = [list(identity) for identity in sorted(trial_starts - trial_ends)]
    quality["manifests_not_normally_closed"] = []
    for path in sorted(folder.rglob("*-attribution-manifest.json")):
        try:
            manifest = json.loads(path.read_text(encoding="utf-8"))
            if manifest.get("loggingStatus") != "closed":
                quality["manifests_not_normally_closed"].append(str(path))
        except (ValueError, OSError) as error:
            errors.append(str(error))
    grouped = collections.defaultdict(collections.Counter)
    for row in resolved:
        group = (row.get("mode"), row.get("source"), row.get("expectedAction"))
        grouped[group][row.get("taskResult", "unknown")] += 1
    return {
        "formal_entry_outcomes": dict(collections.Counter(row.get("outcome") for row in formal)),
        "observed_resolved_entries": len(resolved),
        "task_results_by_mode_source_instruction": [
            {"mode": group[0], "source": group[1], "expected_action": group[2], "counts": dict(values)}
            for group, values in sorted(grouped.items())],
        "trial_events": dict(collections.Counter(row.get("event") for row in lifecycle)),
        "quality": quality,
        "interpretation": "aborted, unclosed, duplicate, inferred-start and unknown-instruction records are not silently counted as normal failures; taskResult describes an entry, not final trial success or psychological intention",
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    report = analyze(args.folder)
    text = json.dumps(report, ensure_ascii=False, indent=2)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(text + "\n", encoding="utf-8")
    print(text)
    if report["quality"]["csv_errors"]:
        raise SystemExit(2)


if __name__ == "__main__":
    main()

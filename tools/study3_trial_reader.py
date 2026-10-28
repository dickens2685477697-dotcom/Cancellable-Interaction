"""Read new sidecars or the known legacy Study3 layout without changing inputs."""
from __future__ import annotations

import argparse
import csv
import json
import math
from pathlib import Path

PREFIX = "Case,ID,inTargetTimes,enable_preview,enableMineBall,enabledOb,errorTimes,errorInTimes,usedTime,triggerCondition,start,direction(0 for ),modeSelected,ballIndex,reactTime,react_frequency,obstacle_react_frequency,triggerTime,cancelTime,isMineBall(need cancel),successfully_Canceled,cancelReason,no trigger reason,cancelTimes,directionAngleFrame,directionSpeedMin".split(",")
TRIGGER = ["triggerSpeed", "previousSpeed", "speedThreshold", "triggerAcceleration"]


def strict_rows(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.reader(stream)
        header = next(reader, [])
        if not header or len(set(header)) != len(header):
            raise ValueError(f"{path}: empty or duplicate column names")
        rows = []
        for line, values in enumerate(reader, 2):
            if len(values) != len(header):
                raise ValueError(f"{path}:{line}: {len(values)} values for {len(header)} columns")
            rows.append(dict(zip(header, values)))
        return rows


def read_trial_file(path: str | Path, prefer_v2: bool = True) -> list[dict[str, str]]:
    path = Path(path)
    if path.name.endswith("-time.csv") and prefer_v2:
        sidecar = path.with_name(path.name.removesuffix("-time.csv") + "-trial-results-v2.csv")
        if sidecar.exists():
            return read_trial_file(sidecar, prefer_v2=False)
    if path.name.endswith("-trial-results-v2.csv"):
        result = strict_rows(path)
        required = set(PREFIX + TRIGGER + ["sessionId", "trialSequence", "targetPositionsJson", "schemaVersion"])
        for row in result:
            if not required.issubset(row) or row["schemaVersion"] != "2":
                raise ValueError(f"{path}: unsupported normalized trial schema")
        return result
    with path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.reader(stream)
        header = next(reader, [])
        if header[:26] != PREFIX:
            raise ValueError(f"{path}: unsupported legacy trial prefix")
        has_trigger = header[-4:] == TRIGGER
        result = []
        for line, values in enumerate(reader, 2):
            tail = 4 if has_trigger else 0
            coordinate_count = len(values) - 26 - tail
            if coordinate_count < 3 or coordinate_count % 3:
                raise ValueError(f"{path}:{line}: invalid legacy coordinate width")
            coordinates = []
            for start in range(26, len(values) - tail if tail else len(values), 3):
                point = [float(value) for value in values[start:start + 3]]
                if not all(math.isfinite(value) for value in point):
                    raise ValueError(f"{path}:{line}: nonfinite target coordinate")
                coordinates.append(point)
            row = dict(zip(PREFIX, values[:26]))
            row.update(zip(["centerX", "centerY", "centerZ"], map(str, coordinates[0])))
            row["targetPositionsJson"] = json.dumps(coordinates[1:], separators=(",", ":"))
            row.update(zip(TRIGGER, values[-4:] if has_trigger else ["NA"] * 4))
            row["schemaVersion"] = "legacy-read-only"
            row["sourceFile"] = str(path.resolve())
            row["legacyHeaderWidthMismatch"] = str(len(values) != len(header)).lower()
            result.append(row)
        return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("path", type=Path)
    parser.add_argument("--json-output", type=Path)
    args = parser.parse_args()
    rows = read_trial_file(args.path)
    if args.json_output:
        if args.json_output.resolve() == args.path.resolve():
            parser.error("output must not overwrite input")
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"rows": len(rows), "schema": rows[0].get("schemaVersion") if rows else None,
                      "legacy_header_mismatches": sum(row.get("legacyHeaderWidthMismatch") == "true" for row in rows)}, ensure_ascii=False))


if __name__ == "__main__":
    main()

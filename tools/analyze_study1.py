#!/usr/bin/env python3
"""Analyze one Study 1 session. Run: python tools/analyze_study1.py LOG_DIR --output OUTPUT_DIR.

Requires pandas, numpy and matplotlib. Inputs are a matched *-continuous.csv and
*-events.csv, optionally *-mrtk.csv. Values are descriptive, unfiltered and use
the event-aligned windows documented in the output README.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

import numpy as np
import pandas as pd


TIME = "ReceivedRealtime"
KEYS = ["SourceId", "TrackingEpoch", "SampleSequence"]


def discover(folder: Path, prefix: str | None):
    candidates = sorted(folder.glob("*-continuous.csv"))
    if prefix:
        candidates = [p for p in candidates if p.name == prefix + "-continuous.csv"]
    pairs = [(p, p.with_name(p.name.removesuffix("-continuous.csv") + "-events.csv")) for p in candidates]
    pairs = [(a, b) for a, b in pairs if b.exists()]
    if len(pairs) != 1:
        raise ValueError(f"Expected one matching continuous/events pair in {folder}; found {len(pairs)}. Use --prefix if needed.")
    return pairs[0]


def check_columns(df, cols, filename):
    missing = sorted(set(cols) - set(df.columns))
    if missing:
        raise ValueError(f"{filename}: missing required columns: {', '.join(missing)}")


def epoch_equal(series, value):
    return series.isna() if pd.isna(value) else series.eq(value)


def same_context(df):
    result = pd.Series(True, index=df.index)
    for col in ("SourceId", "TrackingEpoch", "ReferenceSpaceEpoch"):
        if col in df:
            # Blank reference epochs are treated as unknown, not a change by themselves.
            result &= df[col].eq(df[col].shift()) | (df[col].isna() & df[col].shift().isna())
    return result


def match_events(events):
    """Use recorded order. Never pair a success with an earlier failed attempt."""
    success = []
    issues = []
    active = None
    hold = None
    for _, event in events.iterrows():
        kind = event.Event
        if kind == "enter":
            if active is not None:
                issues.append(f"Trial {active.Trial}: new enter before prior attempt ended")
            active, hold = event, None
        elif kind == "hold_complete":
            if active is None or event.Trial != active.Trial or hold is not None:
                issues.append(f"Trial {event.Trial}: unmatched hold_complete")
            else:
                hold = event
        elif kind == "success":
            if active is None or hold is None or event.Trial != active.Trial:
                issues.append(f"Trial {event.Trial}: unmatched success")
            elif not (active[TIME] <= hold[TIME] <= event[TIME]):
                issues.append(f"Trial {event.Trial}: non-monotonic event times")
            elif any(not ((pd.isna(active.get(c)) and pd.isna(x.get(c))) or active.get(c) == x.get(c)) for x in (hold, event) for c in ("SourceId", "TrackingEpoch", "ReferenceSpaceEpoch") if c in events):
                issues.append(f"Trial {event.Trial}: source or epoch changed within attempt")
            else:
                success.append((active, hold, event))
            active, hold = None, None
        elif kind in ("early_exit", "tracking_interrupted", "recenter_interrupted", "reference_space_reset"):
            if active is not None and kind != "reference_space_reset" and event.Trial != active.Trial:
                issues.append(f"Trial {event.Trial}: {kind} did not match active trial")
            active, hold = None, None
    if active is not None:
        issues.append(f"Trial {active.Trial}: unfinished attempt")
    return success, issues


def write_curves(d, ev, pairs, joints, valid_dt, out):
    """Write raw frame curves; differences are computed before window clipping."""
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    curve_dir = out / "joint_curves"
    curve_dir.mkdir(exist_ok=True)
    dt = d[TIME].diff()
    segments, coverage = [], []
    for entry, hold, success in pairs:
        start, leave = float(entry[TIME]), float(success[TIME])
        later = ev[(ev.Event == "enter") & ev[TIME].gt(leave)][TIME]
        next_entry = float(later.min()) if len(later) else float("inf")
        end = min(leave + 1, next_entry)
        mask = d[TIME].ge(start - 1) & d[TIME].le(end) & d.SourceId.eq(entry.SourceId) & d.TrackingEpoch.eq(entry.TrackingEpoch)
        if next_entry <= leave + 1: mask &= d[TIME].lt(next_entry)
        if "ReferenceSpaceEpoch" in d and "ReferenceSpaceEpoch" in ev:
            mask &= epoch_equal(d.ReferenceSpaceEpoch, entry.ReferenceSpaceEpoch)
        indices = d.index[mask]
        ts = d.loc[indices, TIME]
        post = ts[ts.ge(leave)]
        prev_success = ev[(ev.Event == "success") & ev[TIME].ge(start-1) & ev[TIME].lt(start)]
        coverage.append(dict(trial=int(entry.Trial), entry_realtime_s=start, exit_realtime_s=leave, exit_relative_s=leave-start, next_entry_realtime_s=next_entry if np.isfinite(next_entry) else None, post_exit_recorded_s=float(post.max()-leave) if len(post) else 0, next_entry_limited=next_entry <= leave+1, recording_end_limited=float(d[TIME].max()) < leave+1, pre_entry_contains_previous_success=not prev_success.empty, frames=len(indices)))
        segments.append((entry, success, indices))
    pd.DataFrame(coverage).to_csv(out / "trial_window_coverage.csv", index=False, encoding="utf-8-sig")
    frames = []
    colors = plt.get_cmap("tab20").colors
    for joint in joints:
        xyz = d[[joint + "_" + a for a in "XYZ"]].apply(pd.to_numeric, errors="coerce")
        pos_valid = d[joint + "_Valid"].eq(1) & xyz.notna().all(axis=1)
        v_valid = pos_valid & pos_valid.shift(fill_value=False) & valid_dt
        velocity = xyz.diff().div(dt, axis=0).where(v_valid, axis=0)
        a_valid = v_valid & v_valid.shift(fill_value=False) & valid_dt
        acceleration = velocity.diff().div(dt, axis=0).where(a_valid, axis=0)
        speed = np.sqrt(velocity.pow(2).sum(axis=1, min_count=3))
        acc_mag = np.sqrt(acceleration.pow(2).sum(axis=1, min_count=3))
        if v_valid.any(): fig, axes = plt.subplots(2, 1, figsize=(14, 8), sharex=True)
        for entry, success, ix in segments:
            trial, start, leave = int(entry.Trial), float(entry[TIME]), float(success[TIME])
            elapsed = d.loc[ix, TIME] - start
            exit_sample = d.loc[ix, "SampleSequence"].eq(success.SampleSequence)
            frames.append(pd.DataFrame(dict(trial=trial, joint=joint, time_from_entry_s=elapsed.to_numpy(), time_from_exit_s=(d.loc[ix, TIME]-leave).to_numpy(), received_realtime_s=d.loc[ix, TIME].to_numpy(), source_id=d.loc[ix, "SourceId"].to_numpy(), tracking_epoch=d.loc[ix, "TrackingEpoch"].to_numpy(), reference_space_epoch=d.loc[ix, "ReferenceSpaceEpoch"].to_numpy() if "ReferenceSpaceEpoch" in d else np.full(len(ix), np.nan), sample_sequence=d.loc[ix, "SampleSequence"].to_numpy(), inside=d.loc[ix, "Inside"].to_numpy(), position_valid=pos_valid.loc[ix].astype(int).to_numpy(), velocity_valid=v_valid.loc[ix].astype(int).to_numpy(), speed_m_s=speed.loc[ix].to_numpy(), acceleration_valid=a_valid.loc[ix].astype(int).to_numpy(), acceleration_magnitude_m_s2=acc_mag.loc[ix].to_numpy(), exit_frame=exit_sample.astype(int).to_numpy())))
            if v_valid.any():
                color = colors[(trial-1) % 20]
                for ax, values in zip(axes, (speed, acc_mag)):
                    ax.plot(elapsed, values.loc[ix], color=color, lw=.8, alpha=.8, label=str(trial))
                    ax.scatter(elapsed[exit_sample], values.loc[ix][exit_sample], s=18, color=[color], edgecolor="white", linewidth=.4)
        if v_valid.any():
            for ax in axes: ax.axvline(0, color="black", ls="--", lw=1); ax.grid(alpha=.2); ax.set_ylim(bottom=0)
            axes[0].set_title(f"{joint}: entry -1 s to exit +1 s, {len(segments)} trials")
            axes[0].set_ylabel("Speed (m/s)"); axes[1].set_ylabel("3D acceleration magnitude (m/s²)")
            axes[1].set_xlabel("Seconds from entry; dashed = entry, circles = exit")
            axes[0].legend(title="Trial", ncol=2, loc="upper left", bbox_to_anchor=(1.01,1), fontsize=7)
            fig.tight_layout(); fig.savefig(curve_dir / (joint + ".png"), dpi=150); plt.close(fig)
    if frames:
        all_frames = pd.concat(frames, ignore_index=True)
        all_frames.to_csv(out / "all_joint_frames_through_exit.csv", index=False, encoding="utf-8-sig", float_format="%.12g")
        all_frames[all_frames.joint == "IndexTip"].to_csv(out / "index_tip_frames_through_exit.csv", index=False, encoding="utf-8-sig", float_format="%.12g")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log_dir", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--prefix", help="filename before -continuous.csv when more than one pair is present")
    parser.add_argument("--no-curves", action="store_true", help="skip large per-frame and joint-plot outputs")
    args = parser.parse_args()
    cont_path, event_path = discover(args.log_dir, args.prefix)
    d = pd.read_csv(cont_path)
    ev = pd.read_csv(event_path)
    check_columns(d, [TIME, *KEYS, "Inside", "Velocity", "VelocityValid", "Acceleration", "AccelerationValid"], cont_path.name)
    check_columns(ev, ["Event", "Trial", TIME, "SourceId", "TrackingEpoch"], event_path.name)
    for col in [TIME, *KEYS, "Inside", "Velocity", "VelocityValid", "Acceleration", "AccelerationValid", "ReferenceSpaceEpoch"]:
        if col in d: d[col] = pd.to_numeric(d[col], errors="coerce")
    for col in ["Trial", TIME, "SourceId", "TrackingEpoch", "ReferenceSpaceEpoch"]:
        if col in ev: ev[col] = pd.to_numeric(ev[col], errors="coerce")
    if d[TIME].isna().any() or ev[TIME].isna().any():
        raise ValueError("Missing or invalid ReceivedRealtime values")
    pairs, issues = match_events(ev)
    dt = d[TIME].diff()
    context = same_context(d)
    valid_dt = dt.gt(0) & dt.le(.05) & context
    joints = sorted(c[:-6] for c in d.columns if c.endswith("_Valid") and all(c[:-6] + "_" + a in d for a in "XYZ"))
    speeds = {}
    for name in joints:
        coords = d[[name + "_" + a for a in "XYZ"]].apply(pd.to_numeric, errors="coerce")
        valid = d[name + "_Valid"].eq(1) & coords.notna().all(axis=1)
        speeds[name] = np.sqrt(coords.diff().pow(2).sum(axis=1, min_count=3)).div(dt).where(valid & valid.shift(fill_value=False) & valid_dt)
    trial_rows, joint_rows = [], []
    for entry, hold, success in pairs:
        start, h, end = (float(x[TIME]) for x in (entry, hold, success))
        context_mask = d.SourceId.eq(entry.SourceId) & d.TrackingEpoch.eq(entry.TrackingEpoch)
        if "ReferenceSpaceEpoch" in d and "ReferenceSpaceEpoch" in ev:
            context_mask &= epoch_equal(d.ReferenceSpaceEpoch, entry.ReferenceSpaceEpoch)
        inside = context_mask & d.Inside.eq(1) & d[TIME].ge(start) & d[TIME].lt(end)
        early = inside & d[TIME].lt(min(start + .2, h)) & d.AccelerationValid.eq(1) & context & context.shift(fill_value=False)
        still = inside & d[TIME].ge(start + .5) & d[TIME].lt(h) & d.VelocityValid.eq(1) & context
        after_hold = inside & d[TIME].ge(h) & d.VelocityValid.eq(1) & context
        next_entry = ev[(ev.Event == "enter") & ev[TIME].gt(end)][TIME]
        limit = min(end + .3, float(d[TIME].max()))
        if not next_entry.empty: limit = min(limit, float(next_entry.min()))
        after_exit = context_mask & d[TIME].ge(end) & d[TIME].lt(limit)
        exit_valid = after_exit & d.VelocityValid.eq(1) & context
        segment_dt = d.loc[inside, TIME].diff()
        trial_rows.append(dict(trial=int(entry.Trial), source_id=int(entry.SourceId), tracking_epoch=int(entry.TrackingEpoch), reference_space_epoch=entry.get("ReferenceSpaceEpoch", np.nan), entry_time=start, hold_complete_time=h, exit_time=end, hold_seconds=h-start, extra_wait_seconds=end-h, inside_seconds=end-start, inside_samples=int(inside.sum()), inside_max_gap_ms=segment_dt.max()*1000, entry_0p2s_samples=int(early.sum()), entry_0p2s_min_accel=d.loc[early,"Acceleration"].min(), hold_0p5s_to_signal_samples=int(still.sum()), hold_mean_speed=d.loc[still,"Velocity"].mean(), hold_median_speed=d.loc[still,"Velocity"].median(), hold_p95_speed=d.loc[still,"Velocity"].quantile(.95), post_signal_inside_mean_speed=d.loc[after_hold,"Velocity"].mean(), post_exit_0p3s_samples=int(exit_valid.sum()), post_exit_0p3s_mean_speed=d.loc[exit_valid,"Velocity"].mean(), post_exit_0p3s_max_speed=d.loc[exit_valid,"Velocity"].max(), post_exit_window_actual_seconds=max(0,limit-end)))
        hmask = context_mask & d.Inside.eq(1) & d[TIME].ge(start+.5) & d[TIME].lt(h)
        for joint in joints:
            hv, wv = speeds[joint][hmask], speeds[joint][after_exit]
            joint_rows.append(dict(trial=int(entry.Trial), joint=joint, hold_valid_samples=int(hv.count()), hold_mean_speed_m_s=hv.mean(), hold_p95_speed_m_s=hv.quantile(.95), withdrawal_valid_samples=int(wv.count()), withdrawal_mean_speed_m_s=wv.mean(), withdrawal_peak_speed_m_s=wv.max()))
    out = args.output
    out.mkdir(parents=True, exist_ok=True)
    trials, joint_df = pd.DataFrame(trial_rows), pd.DataFrame(joint_rows)
    trials.to_csv(out / "per_trial.csv", index=False, encoding="utf-8-sig")
    joint_df.to_csv(out / "joint_per_trial.csv", index=False, encoding="utf-8-sig")
    inner = cont_path.with_name(cont_path.name.removesuffix("-continuous.csv") + "-mrtk.csv")
    report = dict(continuous_file=str(cont_path.resolve()), events_file=str(event_path.resolve()), continuous_rows=len(d), inside_rows=int(d.Inside.eq(1).sum()), inner_rows=len(pd.read_csv(inner)) if inner.exists() else None, events=ev.Event.value_counts().to_dict(), matched_successes=len(pairs), issues=issues, duplicate_source_epoch_sequence=int(d.duplicated(KEYS).sum()), nonpositive_time_intervals=int(dt.le(0).sum()), event_nonpositive_time_intervals=int(ev[TIME].diff().lt(0).sum()), gap_over_50ms=int(dt.gt(.05).sum()), gap_over_250ms=int(dt.gt(.25).sum()), dt_median_seconds=dt.median(), dt_p95_seconds=dt.quantile(.95), dt_max_seconds=dt.max(), source_count=int(d.SourceId.nunique()), tracking_epoch_count=int(d.TrackingEpoch.nunique()), joint_validity={name:dict(valid=int(d[name+"_Valid"].eq(1).sum()), total=len(d)) for name in joints})
    for col in ("hold_seconds", "extra_wait_seconds", "entry_0p2s_min_accel", "hold_mean_speed", "post_exit_0p3s_mean_speed"):
        values = trials[col].dropna() if col in trials else pd.Series(dtype=float)
        report[col] = dict(n=len(values), mean=values.mean() if len(values) else None, median=values.median() if len(values) else None, min=values.min() if len(values) else None, max=values.max() if len(values) else None)
    def convert(o):
        if isinstance(o, (np.integer,)): return int(o)
        if isinstance(o, (np.floating,)): return float(o)
        raise TypeError(type(o).__name__)
    def clean_json(value):
        if isinstance(value, dict): return {str(k): clean_json(v) for k, v in value.items()}
        if isinstance(value, (list, tuple)): return [clean_json(v) for v in value]
        if isinstance(value, (float, np.floating)) and not np.isfinite(value): return None
        return value
    report = clean_json(report)
    (out / "quality_summary.json").write_text(json.dumps(report, ensure_ascii=False, indent=2, default=convert, allow_nan=False), encoding="utf-8")
    if not trials.empty:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        fig, axes = plt.subplots(3, 1, figsize=(10, 8), constrained_layout=True)
        for ax, col, title in zip(axes, ["entry_0p2s_min_accel", "hold_mean_speed", "post_exit_0p3s_mean_speed"], ["Entry: minimum signed speed change rate (m/s²)", "Hold: mean speed (m/s)", "Post-exit: mean speed (m/s)"]):
            ax.plot(trials.trial, trials[col], marker="o", markersize=3)
            ax.set_title(title); ax.grid(alpha=.25); ax.set_xlabel("Successful trial")
        fig.savefig(out / "participant_trial_metrics.png", dpi=160)
        plt.close(fig)
    if pairs and not args.no_curves:
        write_curves(d, ev, pairs, joints, valid_dt, out)
    (out / "README.md").write_text("""# Study 1 analysis\n\nEvents are matched in recorded order: enter → hold_complete → success; early exits and interruptions terminate attempts. Unmatched and incomplete attempts are recorded in quality_summary.json. Trial numbers are logical successful-trial numbers, not segment or case numbers.\n\nAll values are unfiltered descriptive summaries, with no outlier removal. Entry is the minimum signed rate of change of speed in the first 0.2 s after entry, restricted to inside samples with valid logged acceleration. Hold is mean/median/95th percentile logged speed from 0.5 s after entry until the hold-complete event, restricted to inside samples. Withdrawal is logged speed in the first 0.3 s after success, truncated at the next entry or end of recording. The signal-to-exit interval includes response and motion; it is not pure reaction time.\n\nJoint speeds are displacement divided by elapsed time between adjacent valid positions in the same source, tracking epoch and reference-space epoch, with 0 < interval ≤ 50 ms. Blank values indicate unavailable measurements, not zeros. These fixed windows are exploratory and do not reproduce a published segmentation method. Time is in seconds, speed in m/s, acceleration in m/s². Logged acceleration is the signed derivative of speed, not the magnitude of vector acceleration. Sample counts and window durations are provided for interpretation. Reference epochs absent from old files cannot be checked. Source IDs do not establish handedness or participant identity.\n\nJoint curves and frame data cover entry minus 1 s through exit plus 1 s, cut short at the next formal entry or end of recording. The preceding window can contain a previous trial's exit; see trial_window_coverage.csv. Curves use norm of 3D velocity and norm of 3D vector acceleration; both are nonnegative and vector acceleration differs from logged signed rate of speed change. Derivatives are computed before window clipping; no interpolation or filtering is applied. The timestamps are application receipt times, not verified hardware capture times.\n""", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2, default=convert))


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError) as exc:
        print(f"Error: {exc}", file=sys.stderr)
        sys.exit(2)

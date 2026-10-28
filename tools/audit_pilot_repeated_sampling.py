"""Audit repeated observations in PilotStudy logs without changing source data."""
from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np
import pandas as pd

from study3_trial_reader import read_trial_file

ROOT = Path('D:/2026.7.18PilotStudy')
OUT = Path('D:/HLStudy4_22/output/pilot_repeated_sampling_20261027')
MODE_ORDER = ['pinchReleaseTrigger', 'pinchDwell', 'pinchDirection', 'pinchThumbSwipe',
              'dwell', 'dwellRelease', 'dwellDirection', 'speed', 'speedDwell',
              'speedRelease', 'speedDirection', 'release010', 'time010', 'direction010', 'contact010']
LABELS = dict(zip(MODE_ORDER, ['1 Pinch-Release-Trigger', '2 Pinch-Dwell', '3 Pinch-Direction',
    '4 Pinch-Thumb-Swipe', '5 Dwell', '6 Dwell-Release', '7 Dwell-Direction', '8 Speed',
    '9 Speed-Dwell', '10 Speed-Release', '11 Speed-Direction', '12 010-Release',
    '13 010-Time', '14 010-Direction', '15 010-Contact']))


def read_rows(path):
    with path.open('r', encoding='utf-8-sig', errors='replace', newline='') as handle:
        return list(csv.DictReader(handle))


def discover():
    files = []
    for participant in sorted(ROOT.iterdir()):
        if participant.is_dir() and participant.name.isdigit():
            files.extend(('main', participant.name, p) for p in sorted(participant.glob('*/*/*-trajectory.csv')))
    auxiliary = ROOT / '30hz_50hz_analysis'
    files.extend(('framerate', 'self_test', p) for p in sorted(auxiliary.glob('*/*-trajectory.csv')))
    return files


def inventory():
    sessions = []
    schemas = Counter()
    event_schemas = Counter()
    event_types = Counter()
    for cohort, participant, path in discover():
        trajectory = read_rows(path)
        mode = Counter(r.get('modeSelected', '').strip() for r in trajectory)
        trial_path = path.with_name(path.name.replace('-trajectory.csv', '-trial-results-v2.csv'))
        if not trial_path.exists():
            trial_path = path.with_name(path.name.replace('-trajectory.csv', '-time.csv'))
        trials = read_rows(trial_path) if trial_path.exists() else []
        schemas[tuple(trajectory[0].keys()) if trajectory else ()] += 1
        entry = dict(cohort=cohort, participant=participant, condition=path.parent.parent.name,
                     session=path.parent.name, modes=dict(mode), trajectory_rows=len(trajectory),
                     trial_rows=len(trials), completed_modes=dict(Counter(r.get('modeSelected') for r in trials)))
        event_path = path.with_name(path.name.replace('-trajectory.csv', '-attribution-events.csv'))
        if event_path.exists():
            events = read_rows(event_path)
            event_schemas[tuple(events[0].keys()) if events else ()] += 1
            event_types.update(r.get('eventType', r.get('event', '?')) for r in events)
            entry['attribution_events'] = len(events)
            if events:
                entry['event_head'] = {k: v for k, v in events[0].items() if not (v and len(v) > 150)}
                for key, value in events[0].items():
                    if value and value.startswith('{'):
                        try:
                            state = json.loads(value)
                            entry['state_keys'] = list(state)
                        except ValueError:
                            pass
        sessions.append(entry)
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'inventory.json').write_text(json.dumps(dict(sessions=sessions,
        trajectory_schemas=[dict(count=v, columns=list(k)) for k, v in schemas.items()],
        event_schemas=[dict(count=v, columns=list(k)) for k, v in event_schemas.items()],
        event_types=dict(event_types)), ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(dict(sessions=len(sessions), by_cohort=dict(Counter(s['cohort'] for s in sessions)),
        completed_modes=dict(sum((Counter(s['completed_modes']) for s in sessions if s['cohort']=='main'), Counter())),
        trajectory_schemas=[dict(count=v, columns=list(k)) for k, v in schemas.items()],
        event_schemas=[dict(count=v, columns=list(k)) for k, v in event_schemas.items()],
        event_types=dict(event_types)), ensure_ascii=False, indent=2))
    print('SESSIONS', json.dumps([{k:v for k,v in s.items() if k not in ('state_keys','event_head')} for s in sessions], ensure_ascii=False))
    for s in sessions:
        if 'state_keys' in s:
            print('EVENT_EXAMPLE', json.dumps({k:s[k] for k in ('session','event_head','state_keys')}, ensure_ascii=False))
            break


def number(value, default=float('nan')):
    try:
        return float(value)
    except (ValueError, TypeError):
        return default


def yes(value):
    return str(value).lower() in ('1', 'true')


def write_csv(name, records):
    pd.DataFrame(records).to_csv(OUT / name, index=False, encoding='utf-8-sig')


def analyze():
    OUT.mkdir(parents=True, exist_ok=True)
    sessions, trials_out, repeats_out, speed_summary, speed_entries, sources = [], [], [], [], [], []
    boundary_evidence = []
    duplicate_sources, seen_hashes = [], {}
    excluded_trials, legacy_prefixes = [], []

    def source_record(path, role):
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        sources.append(dict(path=str(path), role=role, sha256=digest, bytes=path.stat().st_size))
        return digest

    for cohort, participant, path in discover():
        trial_path = path.with_name(path.name.replace('-trajectory.csv', '-trial-results-v2.csv'))
        if not trial_path.exists():
            trial_path = path.with_name(path.name.replace('-trajectory.csv', '-time.csv'))
        signature = (source_record(path, 'trajectory'), source_record(trial_path, 'trial_results'))
        if signature in seen_hashes:
            duplicate_sources.append(dict(path=str(path), duplicate_of=seen_hashes[signature]))
            continue
        seen_hashes[signature] = str(path)
        try:
            trial_rows = read_trial_file(trial_path)
        except ValueError as exc:
            if 'unsupported legacy trial prefix' not in str(exc):
                raise
            trial_rows = read_rows(trial_path)
            required = {'Case','ID','modeSelected','inTargetTimes','triggerCondition'}
            if not trial_rows or not required.issubset(trial_rows[0]):
                raise
            legacy_prefixes.append(dict(path=str(trial_path),
                note='Older named prefix; only Case, ID, mode, entry count and outcome are used. Duplicate target coordinate names are not used.'))
        accepted = []
        for trial in trial_rows:
            if not (1 <= number(trial.get('Case')) <= 48 and number(trial.get('ID')) in (1, 2, 3)):
                excluded_trials.append(dict(session=path.parent.name, case=trial.get('Case'), id=trial.get('ID'), reason='outside_case_1_48_or_id_1_3'))
                continue
            accepted.append(trial)
        keys = [(int(r['Case']), int(r['ID'])) for r in accepted]
        if len(set(keys)) != len(keys):
            raise ValueError(f'Duplicate formal trial keys: {trial_path}')
        valid_keys = set(keys)
        df = pd.DataFrame(read_rows(path))
        if df.empty:
            continue
        mode_values = set(df['modeSelected'].str.strip())
        if len(mode_values) != 1:
            raise ValueError(f'Mixed modes: {path}')
        mode = next(iter(mode_values))
        meta = dict(cohort=cohort, participant=participant, condition=path.parent.parent.name,
                    session=path.parent.name, mode=mode, technique=LABELS.get(mode, mode))
        df['_csv_line'] = np.arange(len(df)) + 2
        for col in ['Case','ID','indexLocX','indexLocY','indexLocZ','ori_v','area_type','index',
                    'thumbX','thumbY','thumbZ','wristX','wristY','wristZ','frame','fixedTime',
                    'realtime','motionSampleSequence','fixedDeltaTime']:
            if col in df:
                df[col] = pd.to_numeric(df[col], errors='coerce')
        df['_key'] = list(zip(df['Case'], df['ID']))
        formal = df['_key'].isin(valid_keys)
        lifecycle_path = path.with_name(path.name.replace('-trajectory.csv', '-trial-lifecycle.csv'))
        if lifecycle_path.exists():
            source_record(lifecycle_path, 'trial_lifecycle')
            life = read_rows(lifecycle_path)
            starts = {(number(r['case']), number(r['id'])): number(r['fixedTime']) for r in life if r['event']=='trial_start'}
            ends = {(number(r['case']), number(r['id'])): number(r['fixedTime']) for r in life if r['event']=='trial_end'}
            formal &= pd.Series([key in starts and starts[key]-1e-7 <= t <= ends.get(key, float('inf'))+1e-7
                                  for key,t in zip(df['_key'],df['fixedTime'])], index=df.index)
        position_cols = ['indexLocX','indexLocY','indexLocZ']
        valid = np.isfinite(df[position_cols]).all(axis=1) & df[position_cols].ne(0).any(axis=1)
        if 'tipPoseValid' in df:
            valid &= df['tipPoseValid'].map(yes)
        pair = formal & formal.shift(1, fill_value=False) & valid & valid.shift(1, fill_value=False) & df['_key'].eq(df['_key'].shift())
        same_position = df[position_cols].eq(df[position_cols].shift()).all(axis=1)
        zero = df['ori_v'].abs().le(1e-9)
        repeated = pair & same_position
        repeated_zero = repeated & zero
        inside_pair = pair & df['area_type'].eq(1) & df['area_type'].shift().eq(1) & df['index'].eq(df['index'].shift())
        timed_pair = pd.Series(False, index=df.index)
        if 'frame' in df:
            timed_pair = pair & df['frame'].notna() & df['frame'].shift().notna()
        for flag_name, flag in [('pairs',pair),('repeated_positions',repeated),('repeated_zero_speed',repeated_zero),
            ('inside_pairs',inside_pair),('inside_repeated_positions',repeated & inside_pair),
            ('timed_pairs',timed_pair)]:
            df['_' + flag_name] = flag.astype(int)
        trial_rows_here = []
        for trial in accepted:
            key = (int(trial['Case']),int(trial['ID']))
            matching_key = df['_key'].map(lambda value: value == key)
            part = df[matching_key]
            entry = dict(meta, case=key[0], id=key[1], logged_entries=number(trial['inTargetTimes']),
                         trigger_condition=trial['triggerCondition'], rows=int((formal & matching_key).sum()))
            for flag_name in ['pairs','repeated_positions','repeated_zero_speed','inside_pairs',
                              'inside_repeated_positions','timed_pairs']:
                entry[flag_name] = int(part['_' + flag_name].sum())
            trials_out.append(entry)
            trial_rows_here.append(entry)
        entry = dict(meta, formal_trials=len(accepted), rows=int(formal.sum()),
                     missing_trajectory_trials=sum(r['rows']==0 for r in trial_rows_here),
                     timestamped='frame' in df)
        for flag_name in ['pairs','repeated_positions','repeated_zero_speed','inside_pairs','inside_repeated_positions',
                          'timed_pairs']:
            entry[flag_name] = int(df['_' + flag_name].sum())
        entry['trials_with_repeated_positions'] = sum(r['repeated_positions']>0 for r in trial_rows_here)
        sessions.append(entry)
        for i in df.index[repeated]:
            r = df.loc[i]
            repeats_out.append(dict(meta, csv_line=int(r['_csv_line']), case=int(r['Case']), id=int(r['ID']),
                speed=r['ori_v'], inside_pair=bool(inside_pair[i]),
                logger_frame=r.get('frame'),
                motion_sequence=r.get('motionSampleSequence'), fixed_time=r.get('fixedTime'),
                fixed_dt_ms=(r.get('fixedTime', float('nan'))-df.loc[i-1].get('fixedTime', float('nan')))*1000,
                realtime_dt_ms=(r.get('realtime', float('nan'))-df.loc[i-1].get('realtime', float('nan')))*1000))

        boundary_path = path.with_name(path.name.replace('-trajectory.csv', '-speed-boundary-debug.csv'))
        if not boundary_path.exists():
            continue
        source_record(boundary_path, 'speed_boundary_debug')
        boundary = read_rows(boundary_path)
        formal_events = [dict(r, _csv_line=i+2) for i,r in enumerate(boundary)
            if r.get('source')=='target' and yes(r.get('entryExperimentStarted'))
            and (number(r.get('case')),number(r.get('id'))) in valid_keys]
        by_entry = defaultdict(list)
        for r in formal_events:
            by_entry[(r['sourceInstanceId'], r['entrySequence'])].append(r)
        for event_key, events in by_entry.items():
            samples, unique_checks, checks, arms, triggers = {}, set(), [], [], []
            for r in events:
                if r['event']=='check':
                    checks.append(r)
                    unique_checks.add(r['motionSampleSequence'])
                if r['event']=='armed': arms.append(r)
                if r['event']=='trigger': triggers.append(r)
                if r['event'] in ('enter','sample','check','armed','trigger') and yes(r['handInBall']) and yes(r['tipPoseValid']):
                    samples.setdefault(r['motionSampleSequence'], r)
            strong_sequences = set()
            observations = list(samples.values())
            for previous, current in zip(observations, observations[1:]):
                same_tip = all(number(current['index'+axis])==number(previous['index'+axis]) for axis in 'XYZ')
                same_distal = all(number(current['distal'+axis])==number(previous['distal'+axis]) for axis in 'XYZ')
                if (number(current['motionSampleSequence'])==number(previous['motionSampleSequence'])+1
                    and current['frame']==previous['frame'] and same_tip and same_distal
                    and number(current['fixedTime'])>number(previous['fixedTime'])
                    and abs(number(current['decisionSpeed']))<=1e-9
                    and yes(previous['distalPoseValid']) and yes(current['distalPoseValid'])):
                    strong_sequences.add(current['motionSampleSequence'])
                    boundary_evidence.append(dict(meta, entry_sequence=event_key[1], case=int(current['case']),
                        id=int(current['id']), source_file=str(boundary_path), csv_line=current['_csv_line'],
                        previous_csv_line=previous['_csv_line'], frame=int(current['frame']),
                        motion_sequence=int(current['motionSampleSequence']), decision_speed=number(current['decisionSpeed']),
                        previous_speed=number(previous['decisionSpeed']),
                        fixed_dt_ms=(number(current['motionFixedTime'])-number(previous['motionFixedTime']))*1000,
                        realtime_dt_ms=(number(current['realtime'])-number(previous['realtime']))*1000,
                        armed_here=any(r['motionSampleSequence']==current['motionSampleSequence'] for r in arms),
                        eventually_triggered=bool(triggers)))
            armed_strong = [r for r in arms if r['motionSampleSequence'] in strong_sequences]
            armed_zero = [r for r in arms if abs(number(r['decisionSpeed']))<=1e-9]
            first = events[0]
            detail = dict(meta, entry_sequence=event_key[1], source_instance=event_key[0], case=int(first['case']),
                id=int(first['id']), checks=len(checks), unique_checked_measurements=len(unique_checks),
                repeated_measurement_checks=len(checks)-len(unique_checks), inside_motion_samples=len(samples),
                same_frame_zero_tip_distal=len(strong_sequences), armed_events=len(arms),
                armed_at_zero=len(armed_zero), armed_at_same_frame_repeat=len(armed_strong),
                trigger_events=len(triggers), trigger_after_same_frame_repeat_arming=bool(triggers and armed_strong),
                arm_csv_line=arms[0]['_csv_line'] if arms else '',
                trigger_csv_line=triggers[0]['_csv_line'] if triggers else '')
            speed_entries.append(detail)
        these = [r for r in speed_entries if r['cohort']==cohort and r['session']==path.parent.name]
        if these:
            entry = dict(meta, entries=len(these))
            for k in ['checks','unique_checked_measurements','repeated_measurement_checks','inside_motion_samples',
                      'same_frame_zero_tip_distal','armed_events','armed_at_zero','armed_at_same_frame_repeat',
                      'trigger_events','trigger_after_same_frame_repeat_arming']:
                entry[k] = sum(r[k] for r in these)
            speed_summary.append(entry)

    session_df = pd.DataFrame(sessions)
    trial_df = pd.DataFrame(trials_out)
    measure_cols = ['formal_trials','rows','pairs','repeated_positions','repeated_zero_speed','inside_pairs',
                    'inside_repeated_positions','timed_pairs','trials_with_repeated_positions']
    mode_df = session_df.groupby(['cohort','condition','mode','technique'], sort=False)[measure_cols].sum().reset_index()
    mode_df['repeated_position_pct'] = mode_df['repeated_positions']/mode_df['pairs']*100
    mode_df['inside_repeated_position_pct'] = mode_df['inside_repeated_positions']/mode_df['inside_pairs']*100
    write_csv('session_summary.csv', sessions)
    write_csv('trial_summary.csv', trials_out)
    mode_df.to_csv(OUT/'mode_summary.csv', index=False, encoding='utf-8-sig')
    write_csv('repeated_position_rows.csv', repeats_out)
    write_csv('speed_check_summary.csv', speed_summary)
    write_csv('speed_entry_effects.csv', speed_entries)
    write_csv('same_frame_boundary_evidence.csv', boundary_evidence)
    write_csv('sources.csv', sources)
    changed = [r['path'] for r in sources if hashlib.sha256(Path(r['path']).read_bytes()).hexdigest()!=r['sha256']]
    if changed:
        raise ValueError(f'Source files changed during audit: {changed}')
    totals = []
    for (cohort,condition), group in session_df.groupby(['cohort','condition'], sort=False):
        totals.append(dict(cohort=cohort,condition=condition,sessions=len(group),
                           **{k:int(group[k].sum()) for k in measure_cols}))
    audit = dict(scope='Numeric participant directories only; old archives and project copies excluded. Framerate tests separate.',
        repeated_position_definition='Equal serialized numeric XYZ in consecutive valid trajectory rows within the same completed case/ID. Does not identify MRTK update identity.',
        same_frame_definition='Consecutive motion sequence, increasing fixedTime, identical frame, identical index and distal XYZ, valid joints, zero decision speed. Scheduling evidence, not a hardware frame ID.',
        speed_check_definition='Repeated check events reusing the same AcceStudy3 motionSampleSequence within a target entry; this is not the MRTK provider sequence.',
        formal_definition='Completed Case 1-48 and ID 1-3; modern trajectory further limited by trial lifecycle; boundary entries require entryExperimentStarted=1.',
        source_files_unchanged=True, sources=len(sources), totals=totals, excluded_trial_rows=excluded_trials,
        older_trial_prefixes=legacy_prefixes,
        duplicate_source_sessions=duplicate_sources,
        no_provider_sequence_available=True,
        trajectory_timestamp_limit='Trajectory frame is the logger frame and motionFixedTime may lag fixedTime. A zero same-frame count from trajectory is not absence of repeated reads; use speed-boundary-debug for decision-time evidence.',
        speed_summaries=speed_summary)
    (OUT/'audit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf-8')
    print('TOTALS', json.dumps(totals,ensure_ascii=False))
    print('MODES',mode_df[mode_df.cohort.eq('main') & mode_df.condition.eq('Trigger')][['mode','formal_trials','pairs','repeated_positions','repeated_position_pct','inside_repeated_position_pct']].to_json(orient='records'))
    print('SPEED',json.dumps(speed_summary,ensure_ascii=False))
    print('EXCLUDED',json.dumps(excluded_trials,ensure_ascii=False))
    print('DUPLICATES',json.dumps(duplicate_sources,ensure_ascii=False))
    evidence_df = pd.DataFrame(boundary_evidence)
    print('BOUNDARY_TIMING',evidence_df.groupby('cohort')['realtime_dt_ms'].agg(['count','median','min','max']).to_json())


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--inventory', action='store_true')
    args = parser.parse_args()
    if args.inventory:
        inventory()
    else:
        analyze()

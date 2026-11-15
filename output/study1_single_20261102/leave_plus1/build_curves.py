from pathlib import Path
import json
import zipfile
import numpy as np
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.font_manager import FontProperties
from matplotlib.lines import Line2D

BASE = Path(r'D:\HLStudy4_22\output\study1_single_20261102')
SOURCE = Path(r'C:\Users\tangx\AppData\LocalLow\Angelo Morty And Cindy Co\show4_0\Logs\20261102-17-39-12')
OUT = Path(__file__).parent
PNGS = OUT / 'joint_png'
PNGS.mkdir(parents=True, exist_ok=True)
prefix = '20261102-17-39-12-FastTapIn-'
raw = pd.read_csv(SOURCE / (prefix + 'continuous.csv'))
events = pd.read_csv(SOURCE / (prefix + 'events.csv'))
trials = pd.read_csv(BASE / 'per_trial.csv').sort_values('trial')
assert len(trials) == 20 and trials.trial.nunique() == 20
assert not raw.duplicated(['SourceId', 'TrackingEpoch', 'SampleSequence']).any()
t = raw.ReceivedRealtime
dt = t.diff()
same = raw.SourceId.eq(raw.SourceId.shift()) & raw.TrackingEpoch.eq(raw.TrackingEpoch.shift())
time_ok = same & dt.gt(0) & dt.le(0.05)
dtarr = dt.to_numpy()
joints = [name[:-6] for name in raw.columns if name.endswith('_Valid')]
font = FontProperties(fname=r'C:\Windows\Fonts\msyh.ttc')
colors = plt.get_cmap('tab20').colors
segments = []
meta = []
for tr in trials.itertuples(index=False):
    start, leave = float(tr.entry_time), float(tr.exit_time)
    success = events[(events.Event == 'success') & (events.Trial == tr.trial) & (np.isclose(events.ReceivedRealtime, leave, atol=1e-7))]
    assert len(success) == 1
    e = success.iloc[0]
    after = events[(events.Event == 'enter') & (events.ReceivedRealtime > leave)]
    next_entry = float(after.ReceivedRealtime.min()) if not after.empty else float('inf')
    end = min(leave + 1, next_entry)
    # Exclude a new entry sample, but keep all available frames through the desired endpoint.
    mask = (t >= start - 1) & (t <= end) & (raw.SourceId == e.SourceId) & (raw.TrackingEpoch == e.TrackingEpoch)
    if next_entry <= leave + 1:
        mask &= t < next_entry
    ix = np.flatnonzero(mask.to_numpy())
    assert len(ix) and (raw.SampleSequence.iloc[ix] == e.SampleSequence).sum() == 1
    segments.append((int(tr.trial), start, leave, ix, int(e.SampleSequence)))
    post = t.iloc[ix][t.iloc[ix] >= leave]
    meta.append({'trial': int(tr.trial), 'entry_realtime_s': start, 'exit_realtime_s': leave,
        'exit_relative_s': leave-start, 'next_entry_realtime_s': next_entry if np.isfinite(next_entry) else None,
        'post_exit_requested_s': 1.0, 'post_exit_recorded_s': float(post.max()-leave) if len(post) else 0,
        'next_entry_limited': bool(next_entry <= leave + 1), 'recording_end_limited': bool(t.max() < leave + 1),
        'frames': len(ix)})

tables = []
for joint in joints:
    xyz = raw[[joint + '_X', joint + '_Y', joint + '_Z']].to_numpy(dtype=float)
    ok = raw[joint + '_Valid'].eq(1).to_numpy() & np.isfinite(xyz).all(axis=1)
    vok = ok & np.r_[False, ok[:-1]] & time_ok.to_numpy()
    with np.errstate(divide='ignore', invalid='ignore'):
        vel = np.vstack([np.full(3, np.nan), np.diff(xyz, axis=0)]) / dtarr[:, None]
    vel[~vok] = np.nan
    aok = vok & np.r_[False, vok[:-1]] & time_ok.to_numpy()
    with np.errstate(divide='ignore', invalid='ignore'):
        acc = np.vstack([np.full(3, np.nan), np.diff(vel, axis=0)]) / dtarr[:, None]
    acc[~aok] = np.nan
    speed, amag = np.linalg.norm(vel, axis=1), np.linalg.norm(acc, axis=1)
    if vok.any():
        fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(15, 8.5), sharex=True)
    for trial, entry, leave, ix, exit_seq in segments:
        elapsed = t.iloc[ix].to_numpy() - entry
        frame = pd.DataFrame({'trial': trial, 'joint': joint, 'time_from_entry_s': elapsed,
            'time_from_exit_s': t.iloc[ix].to_numpy()-leave, 'received_realtime_s': t.iloc[ix].to_numpy(),
            'sample_sequence': raw.SampleSequence.iloc[ix].to_numpy(), 'inside': raw.Inside.iloc[ix].to_numpy(),
            'position_valid': ok[ix].astype(int), 'velocity_valid': vok[ix].astype(int),
            'speed_m_s': speed[ix], 'acceleration_valid': aok[ix].astype(int),
            'acceleration_magnitude_m_s2': amag[ix], 'exit_frame': (raw.SampleSequence.iloc[ix].to_numpy()==exit_seq).astype(int)})
        tables.append(frame)
        if vok.any():
            c = colors[(trial-1)%20]
            ax1.plot(elapsed, speed[ix], color=c, lw=.8, alpha=.8, label=f'{trial:02d}')
            ax2.plot(elapsed, amag[ix], color=c, lw=.8, alpha=.8)
            marker = np.flatnonzero(raw.SampleSequence.iloc[ix].to_numpy() == exit_seq)[0]
            ax1.scatter([elapsed[marker]], [speed[ix[marker]]], s=21, color=[c], edgecolor='white', linewidth=.4, zorder=4)
            ax2.scatter([elapsed[marker]], [amag[ix[marker]]], s=21, color=[c], edgecolor='white', linewidth=.4, zorder=4)
    if vok.any():
        for ax in (ax1, ax2):
            ax.axvline(0, color='black', lw=1, ls='--')
            ax.grid(True, alpha=.2)
            ax.set_xlim(-1, max(m['exit_relative_s'] + 1 for m in meta))
            ax.set_ylim(bottom=0)
        if joint == 'IndexTip':
            ax1.set_title('食指尖：入球前 1 秒至离球后 1 秒｜20 次成功试次叠加', fontproperties=font, fontsize=15)
            ax1.set_ylabel('速率 (m/s)', fontproperties=font)
            ax2.set_ylabel('三维加速度模长 (m/s²)', fontproperties=font)
            ax2.set_xlabel('相对于入球时间 (s)；虚线为入球，圆点为各次离球', fontproperties=font)
            ax1.legend(title='试次', title_fontproperties=font, ncol=2, loc='upper left', bbox_to_anchor=(1.005, 1), fontsize=8)
        else:
            ax1.set_title(joint + ': 20 trials, 1 s before entry to 1 s after exit')
            ax1.set_ylabel('Speed (m/s)')
            ax2.set_ylabel('3D acceleration magnitude (m/s²)')
            ax2.set_xlabel('Time from entry (s); dashed = entry, circles = exit')
            ax1.legend(title='Trial', ncol=2, loc='upper left', bbox_to_anchor=(1.005, 1), fontsize=8)
        fig.tight_layout(rect=(0, 0, .91, 1))
        fig.savefig(PNGS / (joint + '.png'), dpi=160)
        if joint == 'IndexTip': fig.savefig(OUT / 'index_tip_20trials_through_exit.svg')
        plt.close(fig)

long = pd.concat(tables, ignore_index=True)
long.to_csv(OUT / 'all_joint_frames_through_exit.csv', index=False, encoding='utf-8-sig', float_format='%.12g')
long[long.joint == 'IndexTip'].to_csv(OUT / 'index_tip_20trials_through_exit.csv', index=False, encoding='utf-8-sig', float_format='%.12g')
pd.DataFrame(meta).to_csv(OUT / 'trial_window_coverage.csv', index=False, encoding='utf-8-sig')
missing = [j for j in joints if not raw[j + '_Valid'].eq(1).any()]
assert len(joints) == 26 and len(missing) == 4 and len(list(PNGS.glob('*.png'))) == 22
assert long[long.joint.isin(missing)][['speed_m_s', 'acceleration_magnitude_m_s2']].isna().all().all()
assert long[long.joint == 'IndexTip'].groupby('trial').exit_frame.sum().eq(1).all()
text = '''Study1, 20 successful trials. Entry aligned to 0; exit marked with a circle for each trial.\nEach window begins at entry minus 1 s and ends at exit plus 1 s, earlier next formal entry, or end of recording, whichever comes first.\nAll original samples in each window are retained, including any unlabelled contact; physical re-entry or source changes need separate review.\nThe previous trial may overlap the beginning of the pre-entry window (notably trial 18).\nSpeed is the magnitude of the three-dimensional velocity. Acceleration magnitude is norm((v_i - v_(i-1))/dt_i).\nComputations use consecutive finite valid positions from the same source and epoch, with 0 < dt <= 0.05 s. They are computed before window clipping.\nThe timestamp is the MRTK application-received timestamp, not a verified sensor capture timestamp. No interpolation, filtering or outlier removal.\nEnd times and truncation are in trial_window_coverage.csv. Four metacarpal joints have no valid samples and are kept blank in the data.\nSource: ''' + str(SOURCE / (prefix + 'continuous.csv')) + '\n'
(OUT / 'README.txt').write_text(text, encoding='utf-8')
with zipfile.ZipFile(OUT / '所有关节_入球至离球后1秒.zip', 'w', zipfile.ZIP_DEFLATED, compresslevel=5) as z:
    for f in sorted(PNGS.glob('*.png')): z.write(f, f.name)
    for f in [OUT / 'all_joint_frames_through_exit.csv', OUT / 'trial_window_coverage.csv', OUT / 'README.txt', Path(__file__)]: z.write(f, f.name)
with zipfile.ZipFile(OUT / '所有关节_入球至离球后1秒.zip') as z:
    assert len(z.namelist()) == 26 and z.testzip() is None
print(json.dumps({'trials': len(meta), 'joints': len(joints), 'missing_joints': missing,
    'index_frames': int((long.joint == 'IndexTip').sum()),
    'post_exit_seconds_min': min(m['post_exit_recorded_s'] for m in meta),
    'post_exit_seconds_max': max(m['post_exit_recorded_s'] for m in meta),
    'next_entry_limited_trials': [m['trial'] for m in meta if m['next_entry_limited']],
    'recording_end_limited_trials': [m['trial'] for m in meta if m['recording_end_limited']]}, ensure_ascii=False, indent=2))

from pathlib import Path
import numpy as np
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

src = Path(r'C:\Users\tangx\AppData\LocalLow\Angelo Morty And Cindy Co\show4_0\Logs\20261102-17-39-12')
out = Path(__file__).parent
df = pd.read_csv(src / '20261102-17-39-12-FastTapIn-continuous.csv')
ev = pd.read_csv(src / '20261102-17-39-12-FastTapIn-events.csv')
tr = pd.read_csv(out.parent / 'per_trial.csv')
assert len(tr) == 20 and tr.trial.nunique() == 20
joint_names = [c[:-6] for c in df.columns if c.endswith('_Valid')]
time = df.ReceivedRealtime
dt = time.diff()
same = df.SourceId.eq(df.SourceId.shift()) & df.TrackingEpoch.eq(df.TrackingEpoch.shift())
dt_ok = dt.gt(0) & dt.le(.05) & same
colors = plt.get_cmap('tab20').colors
long_frames = []
invalid = []
for joint in joint_names:
    xyz = df[[joint+'_X', joint+'_Y', joint+'_Z']]
    current_valid = df[joint+'_Valid'].eq(1) & xyz.notna().all(axis=1)
    speed_valid = current_valid & current_valid.shift(fill_value=False) & dt_ok
    speed = np.sqrt(xyz.diff().pow(2).sum(axis=1, min_count=3)).div(dt).where(speed_valid)
    acc_valid = speed.notna() & speed.shift().notna() & dt_ok
    acceleration = speed.diff().div(dt).where(acc_valid)
    frames = []
    for _, row in tr.iterrows():
        event = ev[(ev.Event=='hold_complete') & (ev.Trial==row.trial)].iloc[-1]
        mask = (time >= row.entry_time) & (time < row.entry_time + 3.0) & df.Inside.eq(1) & df.SourceId.eq(event.SourceId) & df.TrackingEpoch.eq(event.TrackingEpoch)
        frame = pd.DataFrame({'trial':int(row.trial), 'joint':joint, 'time_since_entry_s':time[mask]-row.entry_time,
                              'received_realtime_s':time[mask], 'sample_sequence':df.loc[mask,'SampleSequence'],
                              'position_valid':current_valid[mask].astype(int), 'speed_m_s':speed[mask],
                              'acceleration_m_s2':acceleration[mask]})
        frames.append(frame)
    data = pd.concat(frames,ignore_index=True)
    long_frames.append(data)
    if not data.speed_m_s.notna().any():
        invalid.append(joint)
        continue
    fig, axs = plt.subplots(2,1,figsize=(14,8),sharex=True,layout='constrained')
    for k, part in data.groupby('trial',sort=True):
        for ax,col in zip(axs,['speed_m_s','acceleration_m_s2']):
            ax.plot(part.time_since_entry_s,part[col],lw=.75,alpha=.8,color=colors[(int(k)-1)%20],label=f'{int(k):02d}')
    fig.suptitle(f'{joint}: 20 successful trials, first 3 s after entry', fontsize=15)
    axs[0].set_ylabel('Speed (m/s)')
    axs[1].set_ylabel('Signed speed change (m/s²)')
    axs[1].set_xlabel('Time since target entry (s)')
    axs[1].set_xlim(0,3)
    for ax in axs: ax.grid(alpha=.25)
    axs[0].legend(title='Trial',ncol=2,fontsize=7,loc='upper left',bbox_to_anchor=(1.005,1.02))
    fig.text(.5,-.012,'Unfiltered; each curve uses application-received timestamps. Signed acceleration = change in scalar speed / time.',ha='center',fontsize=9)
    fig.savefig(out / (joint+'.png'), dpi=150, bbox_inches='tight')
    plt.close(fig)
all_data=pd.concat(long_frames,ignore_index=True)
all_data.to_csv(out/'joint_frame_curves_0_3s.csv',index=False,encoding='utf-8-sig',float_format='%.9g')
(out/'README.txt').write_text('20 successful trials, 0 <= time from entry < 3 s; all plots are unfiltered. Each trial selected by enter / hold_complete / success events, matching source and tracking epoch. Speed = norm(position[t]-position[t-1]) / application receipt interval. Signed acceleration = difference between successive scalar speeds / interval. Both adjacent positions and velocity estimates must be valid, same source and epoch and interval <= 0.05 s; invalid values left empty. Initial samples may use a previous sample before entry for differentiation. These are MRTK application callbacks, not verified independent hardware frames. No plot for joints without any valid speed: '+', '.join(invalid)+'. CSV has all joints including missing values.',encoding='utf-8')
print('trials:',tr.trial.nunique(),'joints:',len(joint_names),'missing:',invalid,'rows:',len(all_data),'plots:',len(joint_names)-len(invalid))

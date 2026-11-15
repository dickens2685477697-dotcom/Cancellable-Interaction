from pathlib import Path
import zipfile
import numpy as np
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

SRC = Path(r'C:\Users\tangx\AppData\LocalLow\Angelo Morty And Cindy Co\show4_0\Logs\20261102-17-39-12\20261102-17-39-12-FastTapIn-continuous.csv')
TRIALS = Path(r'D:\HLStudy4_22\output\study1_single_20261102\per_trial.csv')
OUT = Path(__file__).parent
d = pd.read_csv(SRC)
trials = pd.read_csv(TRIALS)
assert len(trials) == 20 and trials.trial.is_unique
assert not d.duplicated(['SourceId','TrackingEpoch','SampleSequence']).any()
t = d['ReceivedRealtime']
dt = t.diff()
same = d.SourceId.eq(d.SourceId.shift()) & d.TrackingEpoch.eq(d.TrackingEpoch.shift())
valid_time = (dt > 0) & (dt <= .05) & same
colours = plt.get_cmap('tab20').colors
names = [x[:-6] for x in d.columns if x.endswith('_Valid')]
parts = []
plt.rcParams.update({'font.family':'DejaVu Sans','font.size':10})
for joint in names:
    coords = d[[joint+'_X',joint+'_Y',joint+'_Z']].astype(float).to_numpy()
    ok = d[joint+'_Valid'].eq(1).to_numpy() & np.isfinite(coords).all(axis=1)
    prevok = np.r_[False,ok[:-1]]
    v_ok = ok & prevok & valid_time.to_numpy()
    delta = np.vstack([np.full(3,np.nan), np.diff(coords,axis=0)])
    with np.errstate(divide='ignore',invalid='ignore'):
        vel = delta / dt.to_numpy()[:,None]
    vel[~v_ok] = np.nan
    prev_v_ok = np.r_[False,v_ok[:-1]]
    a_ok = v_ok & prev_v_ok & valid_time.to_numpy()
    with np.errstate(divide='ignore',invalid='ignore'):
        acc = np.vstack([np.full(3,np.nan),np.diff(vel,axis=0)]) / dt.to_numpy()[:,None]
    acc[~a_ok] = np.nan
    speed = np.linalg.norm(vel,axis=1)
    accmag = np.linalg.norm(acc,axis=1)
    any_valid = bool(v_ok.any())
    if any_valid:
        fig,(ax1,ax2) = plt.subplots(2,1,figsize=(15,8),sharex=True)
    for _, trial in trials.iterrows():
        entry = float(trial.entry_time)
        mask = (t >= entry-1) & (t < entry+3)
        idx = np.flatnonzero(mask.to_numpy())
        tt = t.iloc[idx].to_numpy()-entry
        for j,i in enumerate(idx):
            parts.append((int(trial.trial),joint,tt[j],float(t.iloc[i]),int(d.SampleSequence.iloc[i]),int(ok[i]),int(v_ok[i]),float(speed[i]),int(a_ok[i]),float(accmag[i])))
        if any_valid:
            color = colours[(int(trial.trial)-1)%20]
            ax1.plot(tt,speed[idx],color=color,lw=.7,alpha=.8,label=f'{int(trial.trial):02d}')
            ax2.plot(tt,accmag[idx],color=color,lw=.7,alpha=.8)
    if any_valid:
        for ax in (ax1,ax2):
            ax.axvline(0,color='black',lw=1,ls='--',alpha=.8)
            ax.grid(True,alpha=.2)
            ax.set_xlim(-1,3)
            ax.set_ylim(bottom=0)
        ax1.set_title(f'{joint}: 20 successful trials, 1 s before to 3 s after entry')
        ax1.set_ylabel('Speed (m/s)')
        ax2.set_ylabel('3D acceleration magnitude (m/s²)')
        ax2.set_xlabel('Time relative to entry (s)')
        ax1.legend(title='Trial',ncol=2,loc='upper left',bbox_to_anchor=(1.005,1),fontsize=8)
        fig.text(.5,.01,'Unfiltered; acceleration is the magnitude of the change in 3D velocity per elapsed time.',ha='center',fontsize=9)
        fig.tight_layout(rect=(0,.035,.91,1))
        fig.savefig(OUT/(joint+'.png'),dpi=145)
        plt.close(fig)
cols=['trial','joint','time_relative_to_entry_s','received_realtime_s','sample_sequence','position_valid','velocity_valid','speed_m_s','acceleration_valid','acceleration_magnitude_m_s2']
long=pd.DataFrame(parts,columns=cols)
long.to_csv(OUT/'all_joint_frames_before1_after3.csv',index=False,encoding='utf-8-sig',float_format='%.12g')
missing=[x for x in names if not d[x+'_Valid'].eq(1).any()]
assert len(names)==26 and len(missing)==4 and len(list(OUT.glob('*.png')))==22
assert long.loc[long.joint.isin(missing),['speed_m_s','acceleration_magnitude_m_s2']].isna().all().all()
(OUT/'README.txt').write_text('Study1 20 successful trials, [-1,3) s relative to entry.\nEach PNG overlays 20 unfiltered traces for one tracked joint.\nSpeed = norm of position difference / elapsed application receive time.\nAcceleration = norm of 3D velocity difference / elapsed application receive time, NOT abs(change of speed).\nDifferences use consecutive valid same-source, same-epoch samples with 0 < dt <= 0.05 s; invalids are blank.\nDerived on full continuous recording before window selection, so entry uses pre-entry samples.\nNo interpolation or smoothing. Time is MRTK application receive time, not sensor capture time.\nA 1-second window can overlap other contact attempts; no outside-only filter is used.\nMissing joints (all coordinate validity zero): '+', '.join(missing)+'\nSource: '+str(SRC)+'\n',encoding='utf-8')
archive=OUT.parent/'各关节运动曲线_入球前1秒至后3秒.zip'
with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for path in sorted(OUT.glob('*.png')): z.write(path,path.name)
    for path in [OUT/'all_joint_frames_before1_after3.csv',OUT/'README.txt']:
        z.write(path,path.name)
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None and len(z.namelist())==24
print('trials',len(trials),'joints',len(names),'figures',len(list(OUT.glob('*.png'))),'missing',missing,'rows',len(long),'archive',archive)

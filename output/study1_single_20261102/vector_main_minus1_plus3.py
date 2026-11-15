from pathlib import Path
import json
import numpy as np
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.font_manager import FontProperties

ROOT=Path(r'D:\HLStudy4_22\output\study1_single_20261102')
DEST=ROOT/'vector_curves_minus1_plus3'
DEST.mkdir(exist_ok=True)
SRC=Path(r'C:\Users\tangx\AppData\LocalLow\Angelo Morty And Cindy Co\show4_0\Logs\20261102-17-39-12')
d=pd.read_csv(SRC/'20261102-17-39-12-FastTapIn-continuous.csv')
tr=pd.read_csv(ROOT/'per_trial.csv')
dt=d.ReceivedRealtime.diff()
same=d.SourceId.eq(d.SourceId.shift()) & d.TrackingEpoch.eq(d.TrackingEpoch.shift())
xyz=d[['IndexTip_X','IndexTip_Y','IndexTip_Z']].astype(float)
pos_ok=d.IndexTip_Valid.eq(1) & np.isfinite(xyz).all(axis=1)
v_ok=pos_ok & pos_ok.shift(fill_value=False) & same & dt.gt(0) & dt.le(.05)
v=xyz.diff().div(dt,axis=0).where(v_ok,axis=0)
a_ok=v_ok & v_ok.shift(fill_value=False) & same & dt.gt(0) & dt.le(.05)
a=v.diff().div(dt,axis=0).where(a_ok,axis=0)
speed=np.sqrt((v*v).sum(axis=1,min_count=3))
acc=np.sqrt((a*a).sum(axis=1,min_count=3))
for i,c in enumerate('xyz'):
 d['v_'+c]=v.iloc[:,i]; d['a_'+c]=a.iloc[:,i]
d['recomputed_speed_m_s']=speed
d['acceleration_magnitude_m_s2']=acc
d['vector_velocity_valid']=v_ok.astype(int)
d['vector_acceleration_valid']=a_ok.astype(int)

frames=[]
for row in tr.itertuples():
 s=d[(d.ReceivedRealtime>=row.entry_time-1)&(d.ReceivedRealtime<row.entry_time+3)].copy()
 s['trial']=int(row.trial)
 s['seconds_from_entry']=s.ReceivedRealtime-row.entry_time
 frames.append(s)
z=pd.concat(frames,ignore_index=True)
columns=['trial','seconds_from_entry','ReceivedRealtime','SourceId','TrackingEpoch','SampleSequence','SampleDeltaTime','Inside','IndexTip_Valid','IndexTip_X','IndexTip_Y','IndexTip_Z','vector_velocity_valid','v_x','v_y','v_z','recomputed_speed_m_s','vector_acceleration_valid','a_x','a_y','a_z','acceleration_magnitude_m_s2']
z[columns].to_csv(DEST/'index_tip_minus1_plus3_vector.csv',index=False,encoding='utf-8-sig')

font=FontProperties(fname=r'C:\Windows\Fonts\msyh.ttc')
fig,axes=plt.subplots(2,1,figsize=(16,9.5),sharex=True,layout='constrained')
colors=plt.get_cmap('tab20').colors
for trial,g in z.groupby('trial',sort=True):
 axes[0].plot(g.seconds_from_entry,g.recomputed_speed_m_s,lw=.9,alpha=.8,color=colors[(trial-1)%20],label=f'{trial:02d}')
 axes[1].plot(g.seconds_from_entry,g.acceleration_magnitude_m_s2,lw=.9,alpha=.8,color=colors[(trial-1)%20])
for ax in axes:
 ax.axvline(0,color='#20252b',linestyle='--',lw=1.2)
 ax.grid(alpha=.25)
 ax.set_xlim(-1,3)
 ax.set_ylim(bottom=0)
axes[0].set_title('食指尖：入球前 1 秒至入球后 3 秒｜20 次成功试次叠加',fontproperties=font,fontsize=15)
axes[0].set_ylabel('速率 (m/s)',fontproperties=font)
axes[1].set_ylabel('三维加速度模长 (m/s²)',fontproperties=font)
axes[1].set_xlabel('相对于入球时间 (s)，虚线 = 入球',fontproperties=font)
axes[0].legend(title='试次',title_fontproperties=font,bbox_to_anchor=(1.005,1),loc='upper left',ncol=2,fontsize=8)
fig.savefig(DEST/'index_tip_minus1_plus3_vector.png',dpi=180,bbox_inches='tight')
fig.savefig(DEST/'index_tip_minus1_plus3_vector.svg',bbox_inches='tight')
plt.close(fig)
summary=[]
for trial,g in z.groupby('trial'):
 summary.append({'trial':int(trial),'frames':len(g),'start':float(g.seconds_from_entry.min()),'end':float(g.seconds_from_entry.max()),'speed_missing':int(g.recomputed_speed_m_s.isna().sum()),'acceleration_missing':int(g.acceleration_magnitude_m_s2.isna().sum()),'max_acceleration':float(g.acceleration_magnitude_m_s2.max())})
logged=(d.recomputed_speed_m_s-d.Velocity).abs()
print(json.dumps({'trials':len(summary),'frames':len(z),'per_trial':summary,'speed_log_max_abs_error':float(logged.max()),'acceleration_min':float(z.acceleration_magnitude_m_s2.min()),'acceleration_max':float(z.acceleration_magnitude_m_s2.max())},ensure_ascii=False,indent=2))

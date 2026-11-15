from pathlib import Path
import csv, json, math
import numpy as np
import pandas as pd

src=Path(r'C:\Users\tangx\AppData\LocalLow\Angelo Morty And Cindy Co\show4_0\Logs\20261102-17-39-12')
out=Path(__file__).parent
prefix='20261102-17-39-12-FastTapIn-'
d=pd.read_csv(src/(prefix+'continuous.csv'))
ev=pd.read_csv(src/(prefix+'events.csv'))
inner=pd.read_csv(src/(prefix+'mrtk.csv'))
t='ReceivedRealtime'; seq='SampleSequence'
events=ev.groupby('Event').size().to_dict()
xyz=['IndexTip_X','IndexTip_Y','IndexTip_Z']
same=(d[xyz].eq(d[xyz].shift()).all(axis=1))
same &= d.SourceId.eq(d.SourceId.shift()) & d.TrackingEpoch.eq(d.TrackingEpoch.shift())
dt=d[t].diff()
keys=['SourceId','TrackingEpoch','SampleSequence']
validcols=[c for c in d if c.endswith('_Valid')]
result=[]
for trial in sorted(ev.Trial.unique()):
    e=ev[ev.Trial.eq(trial)]
    # Match each successful event to the latest preceding hold and preceding enter.
    for _,success in e[e.Event.eq('success')].iterrows():
        h=e[(e.Event=='hold_complete') & (e[t]<=success[t])].tail(1)
        if h.empty: continue
        h=h.iloc[0]
        en=e[(e.Event=='enter') & (e[t]<=h[t])].tail(1)
        if en.empty: continue
        en=en.iloc[0]
        sf=d[(d[t]>=en[t]) & (d[t]<success[t]) & (d.Inside==1) & (d.SourceId==en.SourceId) & (d.TrackingEpoch==en.TrackingEpoch)]
        first=sf[(sf[t]>=en[t]) & (sf[t]<min(en[t]+.2,h[t])) & (sf.AccelerationValid==1)]
        still=sf[(sf[t]>=en[t]+.5) & (sf[t]<h[t]) & (sf.VelocityValid==1)]
        late=sf[(sf[t]>=h[t]) & (sf[t]<success[t]) & (sf.VelocityValid==1)]
        # Fixed descriptive window after exit, no claim that this reconstructs the paper's departure phase.
        end=min(success[t]+.3, d[t].max())
        nxt=ev[(ev.Event=='enter') & (ev[t]>success[t])]
        if not nxt.empty: end=min(end,nxt[t].iloc[0])
        departure=d[(d[t]>=success[t]) & (d[t]<end) & (d.SourceId==en.SourceId) & (d.TrackingEpoch==en.TrackingEpoch) & (d.VelocityValid==1)]
        result.append(dict(trial=int(trial),entry_time=en[t],hold_complete_time=h[t],exit_time=success[t],hold_seconds=h[t]-en[t],extra_wait_seconds=success[t]-h[t],inside_seconds=success[t]-en[t],inside_samples=len(sf),inside_valid_speed_samples=int((sf.VelocityValid==1).sum()),inside_valid_acceleration_samples=int((sf.AccelerationValid==1).sum()),inside_max_gap_ms=sf[t].diff().max()*1000,entry_0p2s_samples=len(first),entry_0p2s_min_accel=first.Acceleration.min(),hold_0p5s_to_signal_samples=len(still),hold_mean_speed=still.Velocity.mean(),hold_median_speed=still.Velocity.median(),hold_p95_speed=still.Velocity.quantile(.95),post_signal_inside_mean_speed=late.Velocity.mean(),post_exit_0p3s_samples=len(departure),post_exit_0p3s_mean_speed=departure.Velocity.mean(),post_exit_0p3s_max_speed=departure.Velocity.max(),post_exit_window_actual_seconds=end-success[t]))
tr=pd.DataFrame(result)
tr.to_csv(out/'per_trial.csv',index=False,encoding='utf-8-sig')
joint={c.replace('_Valid',''):dict(valid=int(d[c].fillna(0).sum()),total=len(d),pct=round(100*d[c].fillna(0).mean(),3)) for c in validcols}
q={"continuous_rows":len(d),"inside_rows":int(d.Inside.sum()),"inner_rows":len(inner),"events":events,"event_trials":ev.Trial.nunique(),"success_trials":len(tr),"duplicate_source_epoch_sequence":int(d.duplicated(keys).sum()),"time_nonpositive":int((dt.dropna()<=0).sum()),"gap_over_50ms":int((dt>.05).sum()),"gap_over_250ms":int((dt>.25).sum()),"dt_median_seconds":float(dt.median()),"dt_p95_seconds":float(dt.quantile(.95)),"dt_max_seconds":float(dt.max()),"same_xyz_consecutive":int(same.sum()),"same_xyz_denominator":int(len(d)-1),"zero_speed_samples":int(((d.Velocity==0)&(d.VelocityValid==1)).sum()),"valid_speed_samples":int((d.VelocityValid==1).sum()),"source_count":int(d.SourceId.nunique()),"epoch_count":int(d.TrackingEpoch.nunique()),"joint_validity":joint,"hold_seconds_mean":tr.hold_seconds.mean(),"hold_seconds_range":[tr.hold_seconds.min(),tr.hold_seconds.max()],"additional_wait_mean":tr.extra_wait_seconds.mean(),"additional_wait_range":[tr.extra_wait_seconds.min(),tr.extra_wait_seconds.max()],"entry_min_accel_median":tr.entry_0p2s_min_accel.median(),"hold_speed_mean_of_trial_means":tr.hold_mean_speed.mean(),"post_exit_speed_mean_of_trial_means":tr.post_exit_0p3s_mean_speed.mean(),"post_exit_samples_range":[int(tr.post_exit_0p3s_samples.min()),int(tr.post_exit_0p3s_samples.max())]}
(out/'quality_summary.json').write_text(json.dumps(q,ensure_ascii=False,indent=2,default=lambda x:float(x)),encoding='utf-8')
print(json.dumps(q,ensure_ascii=False,indent=2,default=lambda x:float(x)))

joint_rows=[]
same_source=d.SourceId.eq(d.SourceId.shift()) & d.TrackingEpoch.eq(d.TrackingEpoch.shift())
valid_dt=(dt>0)&(dt<=.05)&same_source
for name in joint:
    valid=d[name+'_Valid'].eq(1) & d[name+'_Valid'].shift().eq(1) & valid_dt
    coords=d[[name+'_X',name+'_Y',name+'_Z']]
    speeds=np.sqrt((coords.diff()**2).sum(axis=1,min_count=3))/dt
    speeds=speeds.where(valid)
    for _,row in tr.iterrows():
        source_event=ev[(ev.Event=='hold_complete')&(ev.Trial==row.trial)].iloc[-1]
        allowed=(d.SourceId==source_event.SourceId)&(d.TrackingEpoch==source_event.TrackingEpoch)
        h=allowed & (d[t]>=row.entry_time+.5) & (d[t]<row.hold_complete_time) & (d.Inside==1)
        e=allowed & (d[t]>=row.exit_time) & (d[t]<row.exit_time+row.post_exit_window_actual_seconds)
        joint_rows.append(dict(trial=int(row.trial),joint=name,hold_valid_samples=int(speeds[h].count()),hold_mean_speed_m_s=speeds[h].mean(),hold_p95_speed_m_s=speeds[h].quantile(.95),withdrawal_valid_samples=int(speeds[e].count()),withdrawal_mean_speed_m_s=speeds[e].mean(),withdrawal_peak_speed_m_s=speeds[e].max()))
pd.DataFrame(joint_rows).to_csv(out/'joint_per_trial.csv',index=False,encoding='utf-8-sig')

from xml.sax.saxutils import escape
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="1100" height="780" viewBox="0 0 1100 780"><rect width="100%" height="100%" fill="white"/><text x="60" y="30" font-family="sans-serif" font-size="20">Study 1: single participant, descriptive event-aligned windows</text>']
for ix,(col,title,unit) in enumerate([('entry_0p2s_min_accel','Entry: minimum acceleration in first 0.2 s','m/s²'),('hold_mean_speed','Hold: mean speed from 0.5 s to hold signal','m/s'),('post_exit_0p3s_mean_speed','Withdrawal: mean speed in 0.3 s after exit','m/s')]):
    vals=tr[col].to_numpy(); low=float(np.nanmin(vals)); high=float(np.nanmax(vals)); pad=max((high-low)*.12,.0001); low-=pad; high+=pad; top=65+ix*225; bottom=top+155
    svg.append(f'<text x="85" y="{top-12}" font-family="sans-serif" font-size="16">{escape(title)} ({unit})</text>')
    for n in range(3):
        y=top+n*(bottom-top)/2; v=high-n*(high-low)/2
        svg.append(f'<line x1="85" y1="{y}" x2="1045" y2="{y}" stroke="#ddd"/><text x="78" y="{y+5}" text-anchor="end" font-family="sans-serif" font-size="12">{v:.3f}</text>')
    points=[]
    for x,v in zip(tr.trial,vals):
        px=85+(x-1)*960/19; py=bottom-(v-low)*(bottom-top)/(high-low); points.append(f'{px:.1f},{py:.1f}'); svg.append(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="3" fill="#2563a3"/>')
    svg.append(f'<polyline points="{" ".join(points)}" fill="none" stroke="#2563a3" stroke-width="2"/>')
    if ix==2:
        for x in tr.trial: svg.append(f'<text x="{85+(x-1)*960/19}" y="{bottom+19}" text-anchor="middle" font-family="sans-serif" font-size="11">{x}</text>')
svg.append('<text x="565" y="770" text-anchor="middle" font-family="sans-serif" font-size="14">Successful trial</text></svg>')
(out/'participant_trial_metrics.svg').write_text(''.join(svg),encoding='utf-8')

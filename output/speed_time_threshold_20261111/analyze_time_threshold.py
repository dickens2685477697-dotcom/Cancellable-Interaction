from pathlib import Path
import hashlib, json
import numpy as np
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

OUT=Path(r'D:/HLStudy4_22/output/speed_time_threshold_20261111')
BASE=Path(r'D:/2026.7.18PilotStudy/Collect_kinematic_data/MagicTap_15被试完整分析_20261108')
source=BASE/'run/data.csv'; mapfile=BASE/'试次与原始数据映射.csv'
x=pd.read_csv(source); mapping=pd.read_csv(mapfile)
assert len(x)==76324 and len(mapping)==300 and mapping.participant.nunique()==15
assert mapping.Case.is_unique and set(x.Case)==set(mapping.Case)
assert np.isfinite(x[['EnterTime','Velocity']].to_numpy()).all()
assert (x.Velocity>=0).all()
DURATIONS=[0.,.03,.05,.10]
BUDGETS=[.10,.15,.20]
THRESHOLDS=[.015,.020,.021,.025,.030,.040,.050,.060,.067,.080,.100,.114,.160]
records=[]; req=[]; trajectory=[]; traces={}; win={}; dt_all=[]
for m in mapping.itertuples(index=False):
    g=x[x.Case==m.Case]; t=g.EnterTime.to_numpy(); v=g.Velocity.to_numpy()
    assert len(g)==m.inside_valid_rows and np.all(np.diff(t)>0) and np.max(np.diff(t))<=.05+1e-9
    assert abs(t[0])<1e-8
    dt_all.extend(np.diff(t)); traces[m.Case]=(t,v)
    for budget in BUDGETS:
        j=np.searchsorted(t,budget)
        trajectory.append(dict(Case=m.Case,participant=m.participant,budget_ms=int(round(budget*1000)),sample_ms=float(t[j]*1000),speed=float(v[j])))
    for dur in DURATIONS:
        w=np.full(len(t),np.inf)
        for end in range(len(t)):
            if t[end]+1e-10<dur: continue
            start=int(np.searchsorted(t,t[end]-dur+1e-10,side='right')-1)
            if start>=0: w[end]=np.max(v[start:end+1])
        win[m.Case,dur]=w
        for budget in BUDGETS:
            val=float(np.min(w[t<=budget+1e-10]))
            req.append(dict(Case=m.Case,participant=m.participant,duration_ms=int(round(dur*1000)),budget_ms=int(round(budget*1000)),required_threshold=val))
        baseline=np.max(v[t<=.05])
        hold=v[(t>=.5)&(t<=2.5)]
        for threshold in THRESHOLDS:
            hit=np.flatnonzero(w<=threshold+1e-12)
            i=int(hit[0]) if len(hit) else None
            tau=t[i] if i is not None else np.nan
            future=(t>tau)&(t<=tau+.1) if i is not None else np.zeros(len(t),dtype=bool)
            row=dict(Case=m.Case,participant=m.participant,duration_ms=int(round(dur*1000)),threshold=threshold,ready_ms=float(tau*1000),initial_peak_50ms=baseline,ready_speed=float(v[i]) if i is not None else np.nan,relative_drop=1-float(v[i])/baseline if i is not None and baseline>0 else np.nan,above_threshold_next100ms=bool(np.any(v[future]>threshold)) if i is not None else True,above_010_next100ms=bool(np.any(v[future]>.1)) if i is not None else True,hold_sample_fraction_below=float(np.mean(hold<=threshold)))
            for budget in BUDGETS: row[f'by_{int(round(budget*1000))}ms']=bool(tau<=budget+1e-10) if i is not None else False
            records.append(row)
trials=pd.DataFrame(records); requirements=pd.DataFrame(req); sample_times=pd.DataFrame(trajectory)
summary=[]; people=[]
for (dur,threshold),g in trials.groupby(['duration_ms','threshold']):
    finite=g.ready_ms.dropna()
    row=dict(duration_ms=int(dur),threshold=threshold,n=300,ready_median_ms=float(finite.median()),ready_p90_ms=float(finite.quantile(.9)),ready_p95_ms=float(finite.quantile(.95)),missing=int(g.ready_ms.isna().sum()),rebound_above_threshold_next100ms=float(g.above_threshold_next100ms.mean()),rebound_above_010_next100ms=float(g.above_010_next100ms.mean()),relative_drop_median=float(g.relative_drop.median()),hold_sample_fraction_below_person_equal=float(g.groupby('participant').hold_sample_fraction_below.mean().mean()))
    for budget in [100,150,200]:
        rates=g.groupby('participant')[f'by_{budget}ms'].mean()
        row[f'by_{budget}ms']=float(rates.mean()); row[f'worst_person_by_{budget}ms']=float(rates.min())
    summary.append(row)
    for p,h in g.groupby('participant'):
        q=dict(duration_ms=int(dur),threshold=threshold,participant=p,n=len(h),ready_median_ms=float(h.ready_ms.median()),ready_p90_ms=float(h.ready_ms.quantile(.9)))
        for budget in [100,150,200]:q[f'by_{budget}ms']=float(h[f'by_{budget}ms'].mean())
        people.append(q)
summary=pd.DataFrame(summary); people=pd.DataFrame(people)
# All participants have twenty trials, so trial- and participant-weighted coverage agree.
assert mapping.groupby('participant').size().eq(20).all()
min_thresholds=[]; lopo=[]
for (dur,budget),g in requirements.groupby(['duration_ms','budget_ms']):
    for cover in [.9,.95]:
        values=np.sort(g.required_threshold.to_numpy()); index=int(np.ceil(cover*len(values)))-1
        threshold=float(values[index]); feasible=bool(np.isfinite(threshold))
        observed=float(np.mean(np.isfinite(values)&(values<=threshold+1e-12))) if feasible else np.nan
        assert not feasible or observed+1e-12>=cover
        min_thresholds.append(dict(duration_ms=int(dur),budget_ms=int(budget),target_coverage=cover,min_threshold=threshold if feasible else np.nan,observed_coverage=observed,feasible=feasible))
        for person,h in g.groupby('participant'):
            train=np.sort(g[g.participant!=person].required_threshold.to_numpy()); k=int(np.ceil(cover*len(train)))-1
            fitted=float(train[k]); test=float(np.mean(np.isfinite(h.required_threshold)&(h.required_threshold<=fitted+1e-12))) if np.isfinite(fitted) else np.nan
            lopo.append(dict(duration_ms=int(dur),budget_ms=int(budget),target_coverage=cover,heldout=person,fitted_threshold=fitted if np.isfinite(fitted) else np.nan,test_coverage=test))
# Cross-check deadline inversion against an independent streak-based detector on the full finite grid.
for r in trials.itertuples(index=False):
    t,v=traces[r.Case]; start=None; detected=np.nan; dur=r.duration_ms/1000
    for k in range(len(t)):
        if v[k]<=r.threshold+1e-12:
            if start is None:start=k
            if t[k]-t[start]+1e-10>=dur:
                detected=t[k]*1000; break
        else:start=None
    assert (np.isnan(detected) and np.isnan(r.ready_ms)) or abs(detected-r.ready_ms)<1e-6

for df,name in [(trials,'trial_results.csv'),(summary,'threshold_summary.csv'),(people,'participant_results.csv'),(requirements,'required_threshold_by_trial.csv'),(pd.DataFrame(min_thresholds),'minimum_threshold_by_deadline.csv'),(pd.DataFrame(lopo),'leave_one_participant_out.csv'),(sample_times,'speed_at_deadlines.csv')]:
    df.to_csv(OUT/name,index=False,encoding='utf-8-sig',float_format='%.10g')

fig,ax=plt.subplots(figsize=(8,4.8),layout='constrained')
grid=np.linspace(0,.8,161); mat=[]
for case,(t,v) in traces.items():mat.append(v[np.maximum(np.searchsorted(t,grid,side='right')-1,0)])
mat=np.asarray(mat)
ax.fill_between(grid*1000,np.quantile(mat,.1,axis=0),np.quantile(mat,.9,axis=0),alpha=.20,label='10th-90th percentile across trials')
ax.plot(grid*1000,np.median(mat,axis=0),label='Median speed',lw=2)
for th in [.03,.05,.067]:ax.axhline(th,ls='--',alpha=.7,label=f'{th:g} m/s')
for ms in [100,150,200]:ax.axvline(ms,color='grey',alpha=.3)
ax.set(xlabel='Time since target entry (ms)',ylabel='3D fingertip speed (m/s)',title='15 participants / 300 successful trials: entry deceleration',ylim=(0,.7))
ax.legend(fontsize=8); fig.savefig(OUT/'entry_deceleration.png',dpi=180); plt.close(fig)
fig,ax=plt.subplots(figsize=(8,4.8),layout='constrained')
for th in [.02,.03,.04,.05,.067,.1]:
    times=trials[(trials.duration_ms==50)&(trials.threshold==th)].ready_ms.to_numpy()
    ax.plot(np.arange(0,501),[np.mean(times<=a)*100 for a in range(501)],label=f'{th:g} m/s')
ax.set(xlabel='Time since target entry (ms)',ylabel='Trials entering Ready (%)',title='Causal Ready detection: speed stays below threshold for 50 ms',xlim=(0,500),ylim=(0,101))
ax.legend();ax.grid(alpha=.2);fig.savefig(OUT/'ready_coverage_50ms.png',dpi=180);plt.close(fig)
meta=dict(participants=15,trials=300,rows=len(x),source=str(source),mapping=str(mapfile),source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),mapping_sha256=hashlib.sha256(mapfile.read_bytes()).hexdigest(),sample_interval_median_ms=float(np.median(dt_all)*1000),max_interval_ms=float(np.max(dt_all)*1000),time_zero='target enter event; application reception time',velocity='3D norm; existing unfiltered pre-IQR reconstructed samples',ready='all observed samples in current continuous below-threshold streak; elapsed duration at least D; causal current/past samples only',coverage='per-trial; equivalent to participant-equal because 20 trials per participant',deadline_choice='100/150/200 ms engineering targets, not psychophysical stability thresholds',duration_choice='0/30/50/100 ms sensitivity; 50 ms engineering debounce, not established perceptual constant',validation='all 15600 candidate/trial combinations cross-checked with independent streak implementation',limits=['successful entry-hold-withdraw trials only; no subjective ready judgments','no display/capture end-to-end latency measured','P10-P15 session identities pending research records'])
(OUT/'analysis_manifest.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(meta,ensure_ascii=False,indent=2))
print(summary[(summary.duration_ms.isin([0,50]))&(summary.threshold.isin([.02,.03,.04,.05,.067,.1]))].to_string(index=False))
print(pd.DataFrame(min_thresholds).to_string(index=False))
print('LOPO:',pd.DataFrame(lopo).groupby(['duration_ms','budget_ms','target_coverage']).test_coverage.agg(['mean','min']).to_string())

"""Derive speed candidates from fixed elapsed times. No previous speed threshold is an input."""
from pathlib import Path
import hashlib, json
import numpy as np
import pandas as pd

BASE=Path(r'D:/2026.7.18PilotStudy/Collect_kinematic_data/MagicTap_15被试完整分析_20261108')
OUT=Path(__file__).resolve().parent
source_path=BASE/'run/data.csv'
x=pd.read_csv(source_path)
mapping=pd.read_csv(BASE/'试次与原始数据映射.csv').set_index('Case')
assert len(x)==76324 and len(mapping)==300 and mapping.participant.nunique()==15
assert mapping.groupby('participant').size().eq(20).all()
rows=[]
for case,g in x.groupby('Case'):
    t=g.EnterTime.to_numpy();v=g.Velocity.to_numpy()
    assert np.all(np.diff(t)>0)
    for ms in [100,150,200]:
        end=int(np.searchsorted(t,ms/1000,side='right')-1)
        start=int(np.searchsorted(t,t[end]-.05+1e-10,side='right')-1)
        assert start>=0 and t[end]-t[start]>=.05-1e-10
        rows.append(dict(Case=int(case),participant=mapping.loc[case,'participant'],time_ms=ms,
                         last_sample_ms=float(t[end]*1000),sample_age_ms=float(ms-t[end]*1000),
                         instantaneous_speed=float(v[end]),
                         prior_continuous50ms_max_speed=float(np.max(v[start:end+1])),
                         actual_window_span_ms=float((t[end]-t[start])*1000)))
trials=pd.DataFrame(rows)
summaries=[]
for ms,g in trials.groupby('time_ms'):
    for metric in ['instantaneous_speed','prior_continuous50ms_max_speed']:
        values=np.sort(g[metric].to_numpy())
        for coverage in [.5,.9,.95]:
            value=float(values[int(np.ceil(coverage*len(values)))-1])
            rounded=float(np.ceil(value*1000)/1000)
            observed=float(np.mean(g[metric]<=value))
            assert observed>=coverage
            summaries.append(dict(time_ms=int(ms),criterion=metric,target_coverage=coverage,
                                  minimum_threshold=value,rounded_up_3dp=rounded,observed_coverage=observed,
                                  coverage_after_rounding=float(np.mean(g[metric]<=rounded))))
summary=pd.DataFrame(summaries)
trials.to_csv(OUT/'fixed_time_independent_trials.csv',index=False,encoding='utf-8-sig',float_format='%.12g')
summary.to_csv(OUT/'fixed_time_independent_summary.csv',index=False,encoding='utf-8-sig',float_format='%.12g')
meta={'source':str(source_path),'sha256':hashlib.sha256(source_path.read_bytes()).hexdigest(),
      'participants':15,'trials':300,'previous_speed_threshold_used_as_input':False,
      'time_zero':'target enter event; actual application reception time',
      'instantaneous_speed':'last observed speed at or before deadline, no interpolation',
      'prior_continuous50ms_max_speed':'maximum of all consecutive samples in at least 50 ms immediately preceding the selected endpoint',
      'selection':'ceil(coverage*N)-th ordered observed speed or window maximum; no old numerical speed threshold',
      'coverage':'trial coverage, also participant-equally weighted because 20 trials each',
      'max_sample_age_ms':float(trials.sample_age_ms.max()),
      'limits':'timing and coverage rules are engineering choices; no subjective stability labels; sample-level persistence is not proof of subjective stability'}
(OUT/'fixed_time_independent_manifest.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf-8')
print(summary.to_string(index=False))
print('max sample age (ms):',trials.sample_age_ms.max())

from pathlib import Path
import hashlib, json, sys
import numpy as np
import pandas as pd
sys.path.insert(0,r'D:/HLStudy4_22/tools')
from analyze_study1 import match_events, epoch_equal

BASE=Path(r'D:/2026.7.18PilotStudy/Collect_kinematic_data/MagicTap_15被试完整分析_20261108')
OUT=Path(__file__).resolve().parent
mapping=pd.read_csv(BASE/'试次与原始数据映射.csv')
source=pd.read_csv(BASE/'run/data.csv')
manifest=pd.read_csv(BASE/'原始输入文件清单.csv')
checked_files=0; checked_rows=0
for rec in manifest[manifest.type.isin(['continuous','events'])].itertuples(index=False):
    path=Path(rec.path)
    assert path.stat().st_size==rec.bytes
    assert hashlib.sha256(path.read_bytes()).hexdigest()==rec.sha256, str(path)
    checked_files+=1
for (person,session),trials in mapping.groupby(['participant','session']):
    path=Path(manifest[(manifest.participant==person)&(manifest.type=='continuous')].iloc[0].path)
    cols=['ReceivedRealtime','SourceId','TrackingEpoch','ReferenceSpaceEpoch','Inside','IndexTip_Valid','IndexTip_X','IndexTip_Y','IndexTip_Z']
    raw=pd.read_csv(path,usecols=cols)
    event_path=Path(manifest[(manifest.participant==person)&(manifest.type=='events')].iloc[0].path)
    pairs,issues=match_events(pd.read_csv(event_path))
    assert len(pairs)==20 and not issues
    by_trial={int(en.Trial):(en,hold,ex) for en,hold,ex in pairs}
    dt=raw.ReceivedRealtime.diff()
    same=pd.Series(True,index=raw.index)
    for col in ['SourceId','TrackingEpoch','ReferenceSpaceEpoch']:
        same &= raw[col].eq(raw[col].shift()) | (raw[col].isna()&raw[col].shift().isna())
    xyz=raw[['IndexTip_X','IndexTip_Y','IndexTip_Z']]
    valid=raw.IndexTip_Valid.eq(1)&xyz.notna().all(axis=1)
    valid=valid&valid.shift(fill_value=False)&same&dt.gt(0)&dt.le(.05)
    v=np.sqrt(xyz.diff().div(dt,axis=0).pow(2).sum(axis=1,min_count=3)).where(valid)
    for m in trials.itertuples(index=False):
        en,hold,ex=by_trial[m.trial]
        assert abs(en.ReceivedRealtime-m.entry_s)<1e-8 and abs(ex.ReceivedRealtime-m.exit_s)<1e-8
        context=raw.SourceId.eq(en.SourceId)&raw.TrackingEpoch.eq(en.TrackingEpoch)&epoch_equal(raw.ReferenceSpaceEpoch,en.ReferenceSpaceEpoch)
        mask=context&raw.Inside.eq(1)&raw.ReceivedRealtime.ge(en.ReceivedRealtime)&raw.ReceivedRealtime.lt(ex.ReceivedRealtime)
        got=source[source.Case==m.Case]
        assert mask.sum()==len(got)==m.inside_valid_rows, (m.Case,person,int(mask.sum()),len(got),m.entry_s,m.exit_s,raw.loc[np.abs(raw.ReceivedRealtime-m.entry_s)<1e-6,'ReceivedRealtime'].to_list())
        assert np.allclose(raw.loc[mask,'ReceivedRealtime']-en.ReceivedRealtime,got.EnterTime,atol=2e-8,rtol=0)
        assert np.allclose(v.loc[mask],got.Velocity,atol=1e-8,rtol=1e-8)
        checked_rows+=len(got)
result={'raw_continuous_and_event_hashes_matched':checked_files,'reconstructed_rows_matched':checked_rows,'trials':len(mapping),'participants':mapping.participant.nunique(),'checks':'raw coordinates and application reception times independently reproduce all prepared speeds and trial-relative timestamps'}
(OUT/'source_verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))

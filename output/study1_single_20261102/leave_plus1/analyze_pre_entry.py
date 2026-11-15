from pathlib import Path
import json
import numpy as np
import pandas as pd

root = Path(__file__).parent
d = pd.read_csv(root / 'index_tip_20trials_through_exit.csv')
d = d[d.velocity_valid.eq(1) & d.speed_m_s.notna()].copy()
assert d.trial.nunique() == 20
rows = []
for trial, g in d.groupby('trial', sort=True):
    def window(lo, hi):
        s = g[(g.time_from_entry_s >= lo) & (g.time_from_entry_s < hi)].speed_m_s
        return float(s.mean()) if len(s) else float('nan')
    # Averages are computed within each trial, then compared across trials.
    a = window(-.3, -.2)
    b = window(-.2, -.1)
    c = window(-.1, 0)
    post = window(0, .1)
    at_entry = g[np.isclose(g.time_from_entry_s, 0.0, rtol=0, atol=1e-8)].speed_m_s
    pre = g[(g.time_from_entry_s >= -.2) & (g.time_from_entry_s < 0)]
    rows.append({'trial': int(trial), 'speed_pre_300_200ms':a, 'speed_pre_200_100ms':b,
                 'speed_pre_100_0ms':c, 'speed_post_0_100ms':post,
                 'speed_at_entry':float(at_entry.iloc[0]) if len(at_entry)==1 else np.nan,
                 'slope_last_200ms':float(np.polyfit(pre.time_from_entry_s,pre.speed_m_s,1)[0]) if len(pre)>1 else np.nan,
                 'last_100_vs_prior_100':c-b, 'post_100_vs_last_pre_100':post-c})
r = pd.DataFrame(rows)
r.to_csv(root/'入球前减速_逐试次对照.csv',index=False,encoding='utf-8-sig')
bins=[]
for i in range(-10,2):
    lo,hi=i/10,(i+1)/10
    vals=[]
    for _,g in d.groupby('trial'):
        s=g[(g.time_from_entry_s>=lo)&(g.time_from_entry_s<hi)].speed_m_s
        vals.append(float(s.mean()) if len(s) else np.nan)
    bins.append({'start_s':lo,'end_s':hi,'trials':int(np.isfinite(vals).sum()),'mean_of_trial_means':float(np.nanmean(vals)),'median_of_trial_means':float(np.nanmedian(vals))})
pd.DataFrame(bins).to_csv(root/'入球前后_百毫秒窗口.csv',index=False,encoding='utf-8-sig')
summary={'trials':len(r),'means':r.mean(numeric_only=True).to_dict(),'medians':r.median(numeric_only=True).to_dict(),
 'last_bin_reduced_count':int((r.last_100_vs_prior_100<0).sum()),
 'three_bins_strict_reduction':int(((r.speed_pre_300_200ms>r.speed_pre_200_100ms)&(r.speed_pre_200_100ms>r.speed_pre_100_0ms)).sum()),
 'last_200ms_slope_negative':int((r.slope_last_200ms<0).sum()),
 'post_less_than_pre_count':int((r.post_100_vs_last_pre_100<0).sum()),
 'entry_first_frame_speed':r.speed_at_entry.describe().to_dict(), 'bins':bins}
print(json.dumps(summary,ensure_ascii=False,indent=2))

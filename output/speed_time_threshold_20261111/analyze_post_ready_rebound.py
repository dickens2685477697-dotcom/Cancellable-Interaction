from pathlib import Path
import numpy as np
import pandas as pd

out=Path(__file__).resolve().parent
base=Path(r'D:/2026.7.18PilotStudy/Collect_kinematic_data/MagicTap_15被试完整分析_20261108')
source=pd.read_csv(base/'run/data.csv')
mapping=pd.read_csv(base/'试次与原始数据映射.csv').set_index('Case')
results=pd.read_csv(out/'trial_results.csv')
rows=[]
for row in results[(results.threshold==.067)&results.duration_ms.isin([0,50])].itertuples(index=False):
    g=source[source.Case==row.Case]
    t=g.EnterTime.to_numpy();v=g.Velocity.to_numpy();ready=row.ready_ms/1000
    after=(t>ready)&(t<float(mapping.loc[row.Case,'hold_complete_s']-mapping.loc[row.Case,'entry_s']))
    short=after&(t<=ready+.1)
    rows.append(dict(Case=row.Case,participant=row.participant,duration_ms=row.duration_ms,
                     above_0375_next100ms=bool(np.any(v[short]>=.375)),
                     above_0375_before_withdraw_prompt=bool(np.any(v[after]>=.375)),
                     max_speed_next100ms=float(np.max(v[short]))))
result=pd.DataFrame(rows)
result.to_csv(out/'post_ready_speed_rebound.csv',index=False,encoding='utf-8-sig')
print(result.groupby('duration_ms')[['above_0375_next100ms','above_0375_before_withdraw_prompt']].agg(['sum','mean']).to_string())

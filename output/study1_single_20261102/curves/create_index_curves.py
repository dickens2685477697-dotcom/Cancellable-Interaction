from pathlib import Path
import csv, html, math
import numpy as np
import pandas as pd

base=Path(__file__).parent
src=Path(r'C:\Users\tangx\AppData\LocalLow\Angelo Morty And Cindy Co\show4_0\Logs\20261102-17-39-12')
df=pd.read_csv(src/'20261102-17-39-12-FastTapIn-continuous.csv')
tr=pd.read_csv(base.parent/'per_trial.csv')
pieces=[]
for _,row in tr.iterrows():
    start=row.entry_time
    cut=df[(df.ReceivedRealtime>=start-1e-9)&(df.ReceivedRealtime<start+3)&(df.ReceivedRealtime<row.exit_time)&(df.Inside==1)].copy()
    cut.insert(0,'Trial',int(row.trial))
    cut.insert(1,'ElapsedSeconds',cut.ReceivedRealtime-start)
    pieces.append(cut)
z=pd.concat(pieces,ignore_index=True)
cols=['Trial','ElapsedSeconds','ReceivedRealtime','SourceId','TrackingEpoch','SampleSequence','SampleDeltaTime','VelocityValid','Velocity','AccelerationValid','Acceleration','IndexTip_Valid','IndexTip_X','IndexTip_Y','IndexTip_Z']
z[cols].to_csv(base/'index_tip_0to3s_long.csv',index=False,encoding='utf-8-sig')
assert z.Trial.nunique()==20 and z.groupby('Trial').size().min()>150
assert z.ElapsedSeconds.ge(-1e-9).all() and z.ElapsedSeconds.lt(3).all()
colors=['#0067a4','#d1495b','#00876c','#a46400','#6253a7','#da621e','#008ea0','#ab3980','#698523','#8b5b52','#0069ca','#c63828','#088e59','#a47900','#804ac3','#f08128','#257e84','#a257ab','#4c7426','#b56376']
W,H=2200,1420; X0,X1=145,1835; panels=[(160,635,'Velocity','速度 Speed (m/s)'),(770,1245,'Acceleration','加速度 Speed change rate (m/s²)')]
parts=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">', '<rect width="100%" height="100%" fill="#fff"/>','<g font-family="Microsoft YaHei,Arial,sans-serif" fill="#182a3c">','<text x="145" y="65" font-size="35" font-weight="bold">食指尖：入球后 3 秒的运动学曲线</text>','<text x="145" y="105" font-size="21">20 次正式成功试次叠加 · MRTK 应用帧 · 原始数据未平滑</text>']
for y0,y1,col,title in panels:
    vals=z.loc[z[col].notna(),col]; lo=float(vals.min()); hi=float(vals.max()); pad=(hi-lo)*.06 or 1; lo-=pad; hi+=pad
    parts.append(f'<text x="{X0}" y="{y0-25}" font-size="24" font-weight="bold">{title}</text>')
    for i in range(6):
        y=y0+(y1-y0)*i/5; v=hi-(hi-lo)*i/5
        parts.append(f'<line x1="{X0}" x2="{X1}" y1="{y:.2f}" y2="{y:.2f}" stroke="#dfe6eb"/><text x="{X0-15}" y="{y+7:.2f}" text-anchor="end" font-size="19">{v:.2f}</text>')
    for i in range(7):
        x=X0+(X1-X0)*i/6
        parts.append(f'<line x1="{x:.2f}" x2="{x:.2f}" y1="{y0}" y2="{y1}" stroke="#edf1f4"/><text x="{x:.2f}" y="{y1+31}" text-anchor="middle" font-size="19">{i/2:.1f}</text>')
    parts.append(f'<rect x="{X0}" y="{y0}" width="{X1-X0}" height="{y1-y0}" fill="none" stroke="#718096"/>')
    for i,(trial,g) in enumerate(z.groupby('Trial',sort=True)):
        g=g[g[col].notna()]; points=' '.join(f'{X0+x/3*(X1-X0):.1f},{y1-(v-lo)/(hi-lo)*(y1-y0):.1f}' for x,v in zip(g.ElapsedSeconds,g[col]))
        parts.append(f'<polyline points="{points}" fill="none" stroke="{colors[i]}" stroke-width="1.7" opacity="0.7"/>')
    parts.append(f'<text x="{(X0+X1)/2}" y="{y1+65}" text-anchor="middle" font-size="21">入球后时间 (s)</text>')
parts.append('<text x="1890" y="164" font-size="23" font-weight="bold">试次</text>')
for i in range(20):
    y=200+i*48; parts.append(f'<line x1="1890" y1="{y}" x2="1940" y2="{y}" stroke="{colors[i]}" stroke-width="5"/><text x="1955" y="{y+7}" font-size="20">{i+1:02}</text>')
parts.append('<text x="145" y="1390" font-size="18">仅成功试次；以该次成功试次的入球为 0 s，保留全部范围。加速度为带符号的速率变化率。</text></g></svg>')
(base/'index_tip_20trials_0to3s.svg').write_text(''.join(parts),encoding='utf8')
print('rows',len(z),'trials',z.Trial.nunique(),'counts',z.groupby('Trial').size().min(),z.groupby('Trial').size().max(),'speed range',z.Velocity.min(),z.Velocity.max(),'accel range',z.Acceleration.min(),z.Acceleration.max())

"""Saved geometry / reference audit, not a Unity physics test. Requires PyYAML, Shapely."""
from pathlib import Path
import math,re,yaml
from shapely.geometry import Polygon,LineString
from shapely.ops import unary_union
ROOT=Path(__file__).resolve().parents[1]
def docs(path):
    return {int(m[2]):(int(m[1]),next(iter(yaml.safe_load(m[3]).values()))) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',path.read_text(),re.M|re.S)}
d=docs(ROOT/'Assets/Project/Prefabs/Stage/Stage_01.prefab')
tfs={b['m_GameObject']['fileID']:i for i,(t,b) in d.items() if t in (4,224) and 'm_GameObject' in b}
def world(tf,point=(0,0)):
    t=d[tf][1];q=t['m_LocalRotation'];theta=2*math.atan2(q['z'],q['w'])
    x,y=point[0]*t['m_LocalScale']['x'],point[1]*t['m_LocalScale']['y']
    p=(x*math.cos(theta)-y*math.sin(theta)+t['m_LocalPosition']['x'],x*math.sin(theta)+y*math.cos(theta)+t['m_LocalPosition']['y'])
    return world(t['m_Father']['fileID'],p) if t['m_Father']['fileID'] else p
polys=[]
for typ,b in d.values():
    if typ==60:
        for path in b['m_Points']['m_Paths']:
            polys.append(Polygon([world(tfs[b['m_GameObject']['fileID']],(p['x'],p['y'])) for p in path]))
obstacles=unary_union(polys)
ids=[3602672865853165110,1676690022993873937,5637810211676505622,103296793874411822,3426528882663552579,4384857408303180194,8919927559305801656,9108794123129803236]
points=list(map(world,ids));clearances=[obstacles.distance(LineString([a,b])) for a,b in zip(points,points[1:])]
assert min(clearances)>.5,clearances
assert d[5118225735906958598][1]['clearOnTimeout']==0
rock=docs(ROOT/'Assets/Project/Prefabs/Hazards/FallingRock.prefab')
controller=rock[7427197253491799233][1]
receiver=rock[controller['powerReceiver']['fileID']][1]
owner=rock[receiver['m_GameObject']['fileID']][1]
assert owner['m_Layer']==6 and owner['m_IsActive']==1
colliders=[rock[c['component']['fileID']][1] for c in owner['m_Component'] if rock[c['component']['fileID']][0]==58]
assert len(colliders)==1 and colliders[0]['m_IsTrigger']==1
assert controller['windDisplacement']>0
assert rock[7765014546836242385][1]['m_RaycastTarget']==0
for n in (1,2,3):
    stage=docs(ROOT/f'Assets/Project/Prefabs/Stage/Stage_0{n}.prefab')
    assert stage[5118225735906958598][1]['clearOnTimeout']==0
print('PASS stage 1 route clearance:',[round(c,3) for c in clearances])
print('PASS rock ability receiver/trigger, non-blocking countdown UI, goal-required timeouts in stages 1–3')
print('Static data only; run GameplayRegressionTests in Unity PlayMode for physics/lifecycle checks.')

"""Static asset/geometry checks; does not simulate Unity physics or compile C#. Requires PyYAML, Shapely."""
from pathlib import Path
import re, math, yaml
from shapely.geometry import Polygon, LineString, Point
from shapely.ops import unary_union
ROOT=Path(__file__).resolve().parents[1]
def docs(path):
    out={}
    for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',path.read_text(),re.M|re.S):
        typ,fid,body=m.groups();fid=int(fid)
        assert fid not in out
        out[fid]=(int(typ),next(iter(yaml.safe_load(body).values())))
    return out
d=docs(ROOT/'Assets/Project/Prefabs/Stage/Stage_03.prefab')
for fid,(typ,b) in d.items():
    if typ==1:
        for c in b['m_Component']:
            ref=c['component']['fileID'];assert d[ref][1]['m_GameObject']['fileID']==fid
    if typ in (4,224) and 'm_Children' in b:
        for child in b['m_Children']:
            c=d[child['fileID']][1]
            if 'm_Father' in c:assert c['m_Father']['fileID']==fid
s=(ROOT/'Assets/Project/Prefabs/Stage/Stage_03.prefab').read_text()
assert all(int(x)==0 or int(x) in d for x in re.findall(r'\{fileID: (-?\d+)\}',s))
route=next(b for t,b in d.values() if b.get('m_EditorClassIdentifier','').endswith('BurningTownRoute2D'))
def positions(refs):
    return [(d[r['fileID']][1]['m_LocalPosition']['x'],d[r['fileID']][1]['m_LocalPosition']['y']) for r in refs]
main,back=positions(route['mainRoute']),positions(route['backRoute'])
polys=d[5751537408444854577][1]['m_Points']['m_Paths']
obstacles=unary_union([Polygon([(p['x'],p['y']) for p in poly]) for poly in polys])
for name,points in [('main',main),('back',[main[0],*back])]:
    clearance=min(obstacles.distance(LineString([a,b])) for a,b in zip(points,points[1:]))
    assert clearance>=.42,(name,clearance)
    length=sum(math.dist(a,b) for a,b in zip(points,points[1:]))
    assert length/.65 < 45
    print('PASS',name,'clearance',round(clearance,3),'route length',round(length,2),'walking time estimate',round(length/.65,1))
fence=d[route['fence']['fileID']][1]
assert fence['burnDuration']==3 and fence['disableColliderAfterBurn']==1 and fence['preventIgnitionWhileWet']==1
collider=d[fence['blockingCollider']['fileID']][1]
go=d[collider['m_GameObject']['fileID']][1];assert go['m_Layer']==7
# Fence spans the alley and is initially enabled; rain extinguishes through its independent layer-6 trigger.
assert collider['m_Enabled']==1 and collider['m_IsTrigger']==0
hazards=[b for t,b in d.values() if b.get('m_EditorClassIdentifier','').endswith('BurningArea2D')]
assert len(hazards)==3 and sum(h['initiallyBurning'] for h in hazards)==2
assert all(h['targetLayers']['m_Bits']==64 for h in hazards)
for h in hazards:
    g=d[h['m_GameObject']['fileID']][1];assert g['m_Layer']==6
    cs=[d[c['component']['fileID']][1] for c in g['m_Component']]
    f=next(c for c in cs if c.get('m_EditorClassIdentifier','').endswith('FlammableObject2D'))
    assert f['stageDirector']['fileID']==5118225735906958598
    if h['initiallyBurning']:assert f['burnDuration']==0
    assert d[f['fireVisual']['fileID']][1]['m_IsActive']==0 # ignited after reset handlers
power=d[6622081547540006974][1]
assert [power[k] for k in ('maxWindUses','maxThunderUses','maxRainUses','maxGhostUses')]==[2,2,2,0]
assert d[5118225735906958598][1]['clearOnTimeout']==0
# Earliest contact with west fire leaves time for the fence to burn; rain cooldown fits between fires.
assert (4.3-.55-.4)/.65 > fence['burnDuration']
assert 5.2/.65 > power['rainCooldown']
assert 5.2 > 2*(power['effectRadius']+.55) # the two fires require separate rain casts
font=(ROOT/'Assets/Project/Fonts/KaiseiTokumin-ExtraBold SDF.asset').read_text()
glyphs=set(map(int,re.findall(r'm_Unicode: (\d+)',font)))
labels=[d[fid][1]['m_text'] for fid in [9200000000000000007,9200000000000000011]]
labels+=re.findall(r'hint.text = "([^"]+)"',(ROOT/'Assets/Project/Scripts/Gameplay/Stage/BurningTownRoute2D.cs').read_text())
assert all(c.isspace() or ord(c) in glyphs for label in labels for c in label)
sg=yaml.safe_load((ROOT/'Assets/Project/Prefabs/Stage/Stage_03.prefab.meta').read_text())['guid']
loader=next(b for t,b in docs(ROOT/'Assets/Project/Scenes/GameScene.unity').values() if 'stages' in b)
assert [s['stageId'] for s in loader['stages']]==[1,2,3]
assert loader['stages'][2]['stagePrefab']['guid']==sg
assert not any(b.get('m_EditorClassIdentifier','').endswith('AssassinChaser2D') for t,b in d.values())
print('PASS prefab references, fire/fence wiring, reset configuration, action windows, glyphs and stage registration')
print('Unity import, compilation, input, physics, audio and actual clearability are NOT tested here.')

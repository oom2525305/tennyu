"""Static asset/geometry checks; does not substitute for Unity import or PlayMode."""
from pathlib import Path
import math, re, yaml
from shapely.geometry import Polygon, LineString, Point
from shapely.ops import unary_union
ROOT = Path(__file__).resolve().parents[1]
def docs(path):
    result = {}
    for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)', path.read_text(), re.M | re.S):
        fid = int(m[2]); assert fid not in result, (path, fid)
        result[fid] = (int(m[1]), next(iter(yaml.safe_load(m[3]).values())))
    return result
assets = list((ROOT/'Assets/Project').rglob('*.prefab')) + list((ROOT/'Assets/Project').rglob('*.unity'))
for path in assets:
    d = docs(path)
    for fid in re.findall(r'\{fileID: (-?\d+)\}', path.read_text()):
        assert int(fid) == 0 or int(fid) in d, (path, fid)
    for fid, (typ, v) in d.items():
        assert -(2**63) <= fid < 2**63
        if typ == 1:
            for component in v.get('m_Component', []):
                c = component['component']['fileID']
                assert d[c][1]['m_GameObject']['fileID'] == fid, (path, fid, c)
        if typ in (4,224) and 'm_Children' in v:
            for child in v['m_Children']:
                c = child['fileID']
                if 'm_Father' in d[c][1]: assert d[c][1]['m_Father']['fileID'] == fid, (path, fid, c)
print('PASS scene/prefab YAML, local references and parent/component ownership')
paths = list((ROOT/'Assets/Project/Prefabs/Stage').glob('Stage_*.prefab')) + [ROOT/'Assets/Project/Scenes/Title.unity', ROOT/'Assets/Project/Scenes/TimelineScene.unity']
for path in paths:
    d = docs(path)
    hosts = [v for t,v in d.values() if v.get('m_EditorClassIdentifier','').endswith('TenyuSettingsUI')]
    assert len(hosts) == 1, path
    host = hosts[0]; audio = d[host['sceneAudio']['fileID']][1]
    assert host['m_GameObject'] == audio['m_GameObject'] and host['stageDirector'] == audio['stageDirector']
    assert host['font']['fileID'] and host['panelSprite']['fileID'] and host['buttonSprite']['fileID']
print('PASS settings wiring in title, timeline and all 8 stage prefabs')
for hard in (False, True):
    path = ROOT/('Assets/Project/Prefabs/Stage/Stage_04' + ('_Hard' if hard else '') + '.prefab')
    d = docs(path)
    assert d[6041928993225237598][1]['m_LocalPosition'] == {'x':0,'y':0,'z':0}
    tfs = {v['m_GameObject']['fileID']:fid for fid,(t,v) in d.items() if t in (4,224) and 'm_GameObject' in v}
    def world(tf, point=(0,0)):
        v=d[tf][1]; q=v['m_LocalRotation']; angle=2*math.atan2(q['z'],q['w'])
        x,y=point[0]*v['m_LocalScale']['x'],point[1]*v['m_LocalScale']['y']
        p=(x*math.cos(angle)-y*math.sin(angle)+v['m_LocalPosition']['x'], x*math.sin(angle)+y*math.cos(angle)+v['m_LocalPosition']['y'])
        return world(v['m_Father']['fileID'],p) if v['m_Father']['fileID'] else p
    walls=unary_union([Polygon([world(tfs[v['m_GameObject']['fileID']],(p['x'],p['y'])) for p in polygon]) for t,v in d.values() if t==60 for polygon in v['m_Points']['m_Paths']])
    ids=[3602672865853165110,1676690022993873937,5637810211676505622,103296793874411822,3426528882663552579,4384857408303180194,8919927559305801656,9108794123129803236]
    route=[world(i) for i in ids]
    assert all(LineString([a,b]).distance(walls) > .42 for a,b in zip(route,route[1:]))
    for fid in (9170000000000000002,9170000000000000012):
        bridge=d[fid][1]; center=world(tfs[bridge['m_GameObject']['fileID']])
        assert LineString(route).distance(Point(center)) < .01
        assert walls.distance(Point(center)) > .42
        # Straight walking across 1.6 + actor diameter 0.8 at speed 1 takes ~2.4s.
        assert (bridge['collapseDelay'] < 2.4) == hard
    camera = next(v for t,v in d.values() if t == 20)
    assert camera['m_BackGroundColor'] == {'r':0,'g':0,'b':0,'a':1}
print('PASS stage 4 root, normal/hard route clearance, bridges across route and timing budgets')
d=docs(ROOT/'Assets/Project/Scenes/TimelineScene.unity')
node=d[9180000000000000000][1]
changes=node['m_Modification']['m_Modifications']
data_guid=yaml.safe_load((ROOT/'Assets/Project/Data/Stages/StageData_04.asset.meta').read_text())['guid']
assert any(c['propertyPath']=='stageData' and c['objectReference'].get('guid')==data_guid for c in changes)
assert any(c['propertyPath']=='m_AnchoredPosition.x' and c['value']==1750 for c in changes)
assert d[430215423][1]['m_SizeDelta']['x']==2000
assert {'fileID':9180000000000000001} in d[430215423][1]['m_Children']
font=(ROOT/'Assets/Project/Fonts/ShipporiMinchoB1-Bold SDF.asset').read_text()
glyphs=set(map(int,re.findall(r'm_Unicode: (\d+)',font)))
assert all(ord(c) in glyphs for c in '設定音の設定音を試す閉じる崩落崩れる橋BGMSE100%秒')
print('PASS fourth timeline node, scroll bounds, rail and Japanese UI glyphs')

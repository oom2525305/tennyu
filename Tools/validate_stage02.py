"""Validate saved Stage_02 geometry. Does not run Unity or simulate its physics.

Run from any directory: python Tools/validate_stage02.py
Requires PyYAML and Shapely (pip install PyYAML shapely).
"""
from pathlib import Path
import math
import heapq
import re
import yaml

ROOT = Path(__file__).resolve().parents[1]

def docs(path):
    result = {}
    pattern = r"^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)"
    for match in re.finditer(pattern, path.read_text(encoding="utf-8"), re.M | re.S):
        typ, fid, body = match.groups()
        fid = int(fid)
        assert fid not in result, ("duplicate fileID", path, fid)
        result[fid] = (int(typ), yaml.safe_load(body))
    return result
from shapely.geometry import Polygon,Point,LineString
from shapely.ops import unary_union
p=ROOT/'Assets/Project/Prefabs/Stage/Stage_02.prefab';ds=docs(p)
polys=[Polygon([(v['x'],v['y']) for v in path]) for t,b in ds.values() if t==60 for path in b['PolygonCollider2D']['m_Points']['m_Paths']]
assert len(polys) == 2 and all(p.is_valid for p in polys)
obstacles=unary_union(polys)
nav=ds[5631471843832230023][1]['MonoBehaviour'];w=nav['areaSize']['x'];h=nav['areaSize']['y'];step=nav['cellSize'];nx=math.ceil(w/step);ny=math.ceil(h/step)
assert nx*ny<=nav['maximumCells']
# Read the actual inherited body dimensions and stage overrides.
nobunaga = docs(ROOT / 'Assets/Project/Prefabs/Characters/Nobunaga.prefab')
modifications = ds[8184456472433754946][1]['PrefabInstance']['m_Modification']['m_Modifications']
def inherited(target, key, default):
    for mod in modifications:
        if mod['target']['fileID'] == target and mod['propertyPath'] == key:
            return float(mod['value'])
    return default
scale = nobunaga[4082709641747381250][1]['Transform']['m_LocalScale']
body_radius = inherited(6561023857285179873, 'm_Radius', nobunaga[6561023857285179873][1]['CircleCollider2D']['m_Radius'])
body_scale = max(inherited(4082709641747381250, 'm_LocalScale.' + axis, scale[axis]) for axis in ('x', 'y'))
skin = inherited(1085920508231766334, 'obstacleSkin', nobunaga[1085920508231766334][1]['MonoBehaviour']['obstacleSkin'])
radius = body_radius * body_scale + skin
coords=[(-w/2+(x+.5)*w/nx,-h/2+(y+.5)*h/ny) for y in range(ny) for x in range(nx)]
free={i for i,c in enumerate(coords) if obstacles.distance(Point(c))>=radius}
def clear(a,b):return obstacles.distance(LineString([a,b]))>=radius
pts=[ds[n][1]['Transform']['m_LocalPosition'] for n in [3602672865853165110,1676690022993873937,5637810211676505622,103296793874411822,3426528882663552579,4384857408303180194,8919927559305801656,9108794123129803236]]
route=[(p['x'],p['y']) for p in pts]
assert all(clear(a,b) for a,b in zip(route,route[1:])), 'Route intersects an obstacle with body clearance'
print('PASS 7 route segments; min clearance:',round(min(obstacles.distance(LineString([a,b])) for a,b in zip(route,route[1:])),3),'required',radius)
print('Navigation grid:',nx,ny,'cells',nx*ny,'walkable centers',len(free))
def cell(p):return int((p[1]+h/2)/(h/ny))*nx+int((p[0]+w/2)/(w/nx))
def neighbors(i):
 x,y=i%nx,i//nx
 for dy in (-1,0,1):
  for dx in (-1,0,1):
   if dx==0 and dy==0:continue
   if 0<=x+dx<nx and 0<=y+dy<ny:yield (y+dy)*nx+x+dx
edges={}
def reach(start,end):
 q=[];visited=set()
 for i in [cell(start),*neighbors(cell(start))]:
  if i in free and clear(start,coords[i]):heapq.heappush(q,(math.dist(start,coords[i]),i))
 while q:
  cost,i=heapq.heappop(q)
  if i in visited:continue
  visited.add(i)
  if clear(coords[i],end):return len(visited)
  if i not in edges:edges[i]=[j for j in neighbors(i) if j in free and clear(coords[i],coords[j])]
  for j in edges[i]:
   if j not in visited:heapq.heappush(q,(cost+math.dist(coords[i],coords[j]),j))
 raise AssertionError(('unreachable',start,end))
for label,px in [('left side road',(355,283)),('left side road end',(653,93)),('right side road',(1350,690))]:
 pos=((px[0]-836)/100,(470.5-px[1])/100)
 assert obstacles.distance(Point(pos))>=radius,label
 print('PASS ghost detour to',label,'visited',reach(route[3],pos),'and return',reach(pos,route[4]))
for px in [(150,430),(655,320),(1360,220),(1050,870)]:
 assert obstacles.contains(Point((px[0]-836)/100,(470.5-px[1])/100)),px
print('PASS river, central fence, fort and lower rocks are blocked')

# Check links and enabled states that the navigation calculation depends on.
for fid, (typ, body) in ds.items():
    v = next(iter(body.values()))
    if typ == 1 and 'm_Component' in v:
        for component in v['m_Component']:
            ref = component['component']['fileID']
            assert ref in ds, ('missing component', fid, ref)
            assert next(iter(ds[ref][1].values()))['m_GameObject']['fileID'] == fid
    if typ in (4, 224) and 'm_Children' in v:
        for child in v['m_Children']:
            ref = child['fileID']
            assert ref in ds, ('missing child', fid, ref)
            child_data = next(iter(ds[ref][1].values()))
            if 'm_Father' in child_data:
                assert child_data['m_Father']['fileID'] == fid
    if typ == 60:
        assert v['m_IsTrigger'] == 0 and v['m_Enabled'] == 1
        assert ds[v['m_GameObject']['fileID']][1]['GameObject']['m_Layer'] == 7

for fid in (6041928993225237598, 4292794898146203553, 8505350226217018248,
            7032986346192636983, 6965713250898310824, 7121971344750318318,
            5058540160395248311, 5898708710209683138):
    t = ds[fid][1]['Transform']
    assert t['m_LocalPosition'] == dict(x=0, y=0, z=0), ('non-origin transform', fid)
    assert t['m_LocalScale'] == dict(x=1, y=1, z=1), ('scaled transform', fid)
assert ds[6094450027046650917][1]['Camera']['orthographic size'] == h / 2
background = ds[4540788690312028448][1]['SpriteRenderer']['m_Sprite']
meta = yaml.safe_load((ROOT / 'Assets/Project/Resouce/Visual/stage02bg.png.meta').read_text(encoding='utf-8'))
assert background['guid'] == meta['guid']
sprite = next(s for s in meta['TextureImporter']['spriteSheet']['sprites'] if s['internalID'] == background['fileID'])
assert sprite['alignment'] == 0  # centered sprite
assert meta['TextureImporter']['spritePixelsToUnits'] == 100
assert (sprite['rect']['width'], sprite['rect']['height']) == (w * 100, h * 100)

def override(instance, target, key):
    matches = [m for m in ds[instance][1]['PrefabInstance']['m_Modification']['m_Modifications']
               if m['target']['fileID'] == target and m['propertyPath'] == key]
    assert len(matches) == 1, (instance, target, key)
    return matches[0]

assert override(8184456472433754946, 1085920508231766334, 'pathfinder')['objectReference']['fileID'] == 5631471843832230023
for index, fid in enumerate([3602672865853165110,1676690022993873937,5637810211676505622,103296793874411822,3426528882663552579,4384857408303180194,8919927559305801656,9108794123129803236]):
    assert override(8184456472433754946,1085920508231766334,f'waypoints.Array.data[{index}]')['objectReference']['fileID'] == fid
for axis, value in zip('xy', route[-1]):
    assert float(override(3060080178138198873,7880124868598227372,'m_LocalPosition.'+axis)['value']) == value
for axis, value in zip('xy', route[0]):
    assert float(override(8184456472433754946,4082709641747381250,'m_LocalPosition.'+axis)['value']) == value
assert ds[2407359218073375061][1]['GameObject']['m_IsActive'] == 0
assert int(override(3628234342753547939,890939652865411288,'m_IsActive')['value']) == 0
power = ds[6622081547540006974][1]['MonoBehaviour']
assert [power[k] for k in ('maxWindUses','maxThunderUses','maxRainUses','maxGhostUses')] == [3,0,0,2]
assert int(override(8256265302803100183,8658718073206414158,'m_IsActive')['value']) == 0
assert int(override(5314886830614317701,4768394836175988144,'m_IsActive')['value']) == 0
assert power['ghostPrefab']['fileID'] != 0
print('PASS root transforms, sprite sub-asset, obstacle layers, waypoint/goal references and ability configuration')

# Font coverage and fixed HUD rectangles; these checks are not a rendered UI test.
font = (ROOT / 'Assets/Project/Fonts/KaiseiTokumin-ExtraBold SDF.asset').read_text(encoding='utf-8')
glyphs = set(map(int, re.findall(r'm_Unicode: (\d+)', font)))
for fid in (9200000000000000007,9200000000000000011):
    label = ds[fid][1]['MonoBehaviour']
    assert label['m_RaycastTarget'] == 0
    assert all(c.isspace() or ord(c) in glyphs for c in label['m_text'])
scaler = ds[3023593714232660341][1]['MonoBehaviour']
assert scaler['m_UiScaleMode'] == 0
for width, height in [(1280,720),(1920,1080),(2560,1080),(1440,1080)]:
    scale = scaler['m_ScaleFactor']
    cw, ch = width / scale, height / scale
    for fid in (8551259362963079213,8993051861464211369,2632482069413861691,
                9200000000000000005,9200000000000000009):
        t = ds[fid][1]['RectTransform']
        assert t['m_AnchorMin'] == t['m_AnchorMax']
        x = t['m_AnchorMin']['x'] * cw + t['m_AnchoredPosition']['x']
        y = t['m_AnchorMin']['y'] * ch + t['m_AnchoredPosition']['y']
        sw, sh = t['m_SizeDelta']['x'], t['m_SizeDelta']['y']
        assert 0 <= x - sw / 2 <= x + sw / 2 <= cw, (width,height,fid,'horizontal bounds')
        assert 0 <= y - sh / 2 <= y + sh / 2 <= ch, (width,height,fid,'vertical bounds')
print('PASS Japanese font glyphs and HUD bounds at 16:9, 21:9 and 4:3')
print('Static validation only. Unity import, physics, input, rendering and audio still require Play Mode checks.')

# Assassin wiring: a separate ability target, never a ProtectedTarget / goal activator.
assassin = ds[9200000000000000022][1]['MonoBehaviour']
assert assassin['target']['fileID'] == 5326484789955831212
assert assassin['stageDirector']['fileID'] == 5118225735906958598
assert assassin['pathfinder']['fileID'] == 5631471843832230023
assert ds[9200000000000000016][1]['GameObject']['m_Layer'] == 6
assert ds[9200000000000000018][1]['Rigidbody2D']['m_BodyType'] == 1
assert ds[9200000000000000020][1]['MonoBehaviour']['routeWalker']['fileID'] == 0
assert ds[9200000000000000027][1]['AudioSource']['m_PlayOnAwake'] == 0
assert ds[5118225735906958598][1]['MonoBehaviour']['clearOnTimeout'] == 0
assert inherited(1085920508231766334, 'advanceWaypointsWhenPushed', 0) == 1
speed = inherited(1085920508231766334, 'moveSpeed', 2)
assert speed < assassin['moveSpeed']
# A stationary windup can hit a normally walking target, but leaves reaction time.
assert assassin['warningDuration'] >= 1
assert assassin['attackDistance'] + speed * assassin['warningDuration'] < assassin['strikeReach']
spawn = ds[9200000000000000017][1]['Transform']['m_LocalPosition']
spawn = (spawn['x'], spawn['y'])
assert clear(spawn, route[0])
assert math.dist(spawn, route[0]) > assassin['attackDistance'] * 0.8
for key in ('visual', 'attackIndicator', 'statusText', 'warningSource'):
    assert assassin[key]['fileID'] in ds
for ancestor in (3457423461418282553, 4292794898146203553):
    transform = ds[ancestor][1]['Transform']
    assert transform['m_LocalPosition'] == dict(x=0, y=0, z=0)
    assert transform['m_LocalScale'] == dict(x=1, y=1, z=1)
source = (ROOT/'Assets/Project/Scripts/Gameplay/Characters/AssassinChaser2D.cs').read_text()
for label in re.findall(r'SetStatus\("([^"\n]+)"\)', source):
    assert all(c.isspace() or ord(c) in glyphs for c in label), ('missing glyph', label)
print('PASS assassin spawn clearance, references, phase policy, warning tuning and dynamic text glyphs')

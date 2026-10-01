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
from shapely.geometry import Polygon,Point,LineString,box
from shapely.ops import unary_union
from shapely.affinity import rotate,translate
import math,heapq
d={k:b for k,(t,b) in d.items()}
def w(p):return ((p[0]-836)/100,(470.5-p[1])/100)
walls=unary_union([Polygon([(p['x'],p['y']) for p in path]) for path in d[5751537408444854577]['m_Points']['m_Paths']])
fence=translate(rotate(box(-.85,-.15,.85,.15),70,origin=(0,0)),*w((800,220)))
def block(p):return translate(box(-.15,-.525,.15,.525),*w(p))
park_world=d[9200000000000000201]['m_LocalPosition']
park=(park_world['x']*100+836,470.5-park_world['y']*100);closed=(1321,570)
# Swept shape must fit so wind can actually move it into the lane.
for i in range(101):
 p=tuple(a+(b-a)*i/100 for a,b in zip(park,closed))
 shape=unary_union([block(p),Point(w(p)).buffer(.4)])
 assert walls.distance(shape)>=.018,(p,walls.distance(shape))
assert walls.distance(LineString([w((570,470)),w((645,275))])) >= .42
print('PASS movable blocker sweep and wind access to upper junction')
W,H=16.72,9.41;nx,ny=112,63
coords=[(-W/2+(x+.5)*W/nx,-H/2+(y+.5)*H/ny) for y in range(ny) for x in range(nx)]
def find(start,goal,obs):
 start,goal=w(start),w(goal);r=.47
 free={i for i,p in enumerate(coords) if obs.distance(Point(p))>=r}
 def clear(a,b):return obs.distance(LineString([a,b]))>=r
 def near(p):
  x=int((p[0]+W/2)/(W/nx));y=int((p[1]+H/2)/(H/ny))
  return [yy*nx+xx for yy in range(max(0,y-1),min(ny,y+2)) for xx in range(max(0,x-1),min(nx,x+2))]
 q=[];cost={};parents={}
 for i in near(start):
  if i in free and clear(start,coords[i]):cost[i]=math.dist(start,coords[i]);heapq.heappush(q,(cost[i]+math.dist(coords[i],goal),i));parents[i]=None
 visited=set()
 while q:
  _,i=heapq.heappop(q)
  if i in visited:continue
  visited.add(i)
  if clear(coords[i],goal):
   points=[goal];k=i
   while k is not None:points.append(coords[k]);k=parents[k]
   return [start,*reversed(points)],cost[i]+math.dist(coords[i],goal)
  for j in near(coords[i]):
   if j==i or j not in free or j in visited or not clear(coords[i],coords[j]):continue
   c=cost[i]+math.dist(coords[i],coords[j])
   if c>=cost.get(j,math.inf):continue
   cost[j]=c;parents[j]=i;heapq.heappush(q,(c+math.dist(coords[j],goal),j))
 return None,None
for name,start,gate,barrier in [('initial',(210,350),True,park),('open gate',(210,350),False,park),('wind into upper',(645,275),False,park),('block main',(210,350),False,closed),('both closed',(210,350),True,closed)]:
 obs=unary_union([walls,block(barrier)]+([fence] if gate else []));path,length=find(start,(1416,100),obs)
 upper=path is not None and any(-1 < x < 3 and y > 2 for x,y in path)
 if name=='both closed':assert path is None
 else:
  assert path is not None, name
  assert upper == (name in ('open gate','wind into upper','block main')),name
 print('PASS',name,'length',round(length,2) if length else None,'upper',upper)

controller=next(b for b in d.values() if b.get('m_EditorClassIdentifier','').endswith('BurningTownRoute2D'))
assert 'mainRoute' not in controller and 'backRoute' not in controller and 'walker' not in controller
mods=d[8184456472433754946]['m_Modification']['m_Modifications']
assert any(m['propertyPath']=='navigateDirectlyToGoal' and int(m['value'])==1 for m in mods)
assert d[6622081547540006974]['maxWindUses']==3
assert d[9200000000000000209]['m_Points']['m_Paths'][0] == [dict(x=-.15,y=-.525),dict(x=.15,y=-.525),dict(x=.15,y=.525),dict(x=-.15,y=.525)]
assert d[9200000000000000202]['m_BodyType']==1
assert d[9200000000000000207]['m_Layer']==7
assert d[9200000000000000200]['m_Layer']==6
assert d[9200000000000000206]['maximumSpeed']==4
assert d[9200000000000000206]['deceleration']==5
# Replacing art changed the sprite subasset ID, not the texture GUID.
meta=yaml.safe_load((ROOT/'Assets/Project/Resouce/Visual/stage03bg.png.meta').read_text())
sprite=d[4540788690312028448]['m_Sprite']
assert sprite['guid']==meta['guid']
assert any(s['internalID']==sprite['fileID'] for s in meta['TextureImporter']['spriteSheet']['sprites'])
font=(ROOT/'Assets/Project/Fonts/KaiseiTokumin-ExtraBold SDF.asset').read_text()
glyphs=set(map(int,re.findall(r'm_Unicode: (\d+)',font)))
labels=[d[i]['m_text'] for i in [9200000000000000007,9200000000000000011]]
labels+=re.findall(r'hint.text = "([^"]+)"',(ROOT/'Assets/Project/Scripts/Gameplay/Stage/BurningTownRoute2D.cs').read_text())
assert all(c.isspace() or ord(c) in glyphs for label in labels for c in label)
assert d[5118225735906958598]['clearOnTimeout']==0
print('PASS goal navigation, independent roadblock layers, user sprite reference, dynamic HUD glyphs')
print('Static geometry only. Unity compilation, actual wind/physics/input/audio still require Play Mode testing.')

assert d[9108794123129803236]['m_LocalPosition'] == dict(x=5.8,y=3.705,z=0)
mods=d[3060080178138198873]['m_Modification']['m_Modifications']
for axis,value in [('x',5.8),('y',3.705)]:
    assert any(m['propertyPath']=='m_LocalPosition.'+axis and float(m['value'])==value for m in mods)
print('PASS destination and goal trigger both match the red-circle location')

# Geometric reproduction of the former early-corner skip (the Unity test executes the real follower).
corner_wall=box(-1.4,.5,-.4,1.5)
current,corner,following=(-.01,0),(.01,0),(.01,2)
assert math.dist(current,corner)<.025
assert corner_wall.distance(LineString([current,following]))<.4
assert corner_wall.distance(LineString([current,corner]))>.4
assert corner_wall.distance(LineString([corner,following]))>.4
print('PASS near-corner reproduction: direct skip collides, reaching the corner first clears')

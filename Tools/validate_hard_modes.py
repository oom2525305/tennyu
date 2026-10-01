"""Static wiring/geometry checks for normal/hard assets. Not Unity execution."""
from pathlib import Path
import re, yaml, math
from shapely.geometry import Polygon, Point
from shapely.ops import unary_union
ROOT=Path(__file__).resolve().parents[1]
def docs(path):
    return {int(m[2]):next(iter(yaml.safe_load(m[3]).values())) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',path.read_text(),re.M|re.S)}
loader=docs(ROOT/'Assets/Project/Scenes/GameScene.unity')[738214767]
glyphs=set(map(int,re.findall(r'm_Unicode: (\d+)',(ROOT/'Assets/Project/Fonts/KaiseiTokumin-ExtraBold SDF.asset').read_text())))
for entry in loader['stages']:
    i=entry['stageId'];base=ROOT/f'Assets/Project/Prefabs/Stage/Stage_0{i}_Hard.prefab'
    assert entry['hardStagePrefab']['guid']==yaml.safe_load(base.with_suffix('.prefab.meta').read_text())['guid']
    d=docs(base);ids=set(d)
    assert all(int(x)==0 or int(x) in ids for x in re.findall(r'\{fileID: (-?\d+)\}',base.read_text()))
    assert d[5118225735906958598]['stageDuration']==40
    power=d[6622081547540006974]
    assert power['maxWindUses']==2
    assert power['maxThunderUses']==(0 if i==2 else 1)
    assert power['maxRainUses']==(2 if i==3 else 0)
    assert power['maxGhostUses']==(2 if i==2 else 0)
    data=docs(ROOT/f'Assets/Project/Data/Stages/StageData_0{i}.asset')[11400000]
    for text in (data['hardDescription'],d[9200000000000000007]['m_text'],d[9200000000000000011]['m_text']):
        assert all(c.isspace() or ord(c) in glyphs for c in text),text
    assert math.ceil(len(data['hardDescription'])/30)*36<=160
    if i==3:
        assert not any(v.get('m_EditorClassIdentifier','').endswith('WindRoadblock2D') for v in d.values())
        assert d[9200000000000000128]['initiallyBurning']==0 # fixed fence needs lightning
        assert d[9200000000000000404]['initiallyBurning']==1
        assert d[9200000000000000401]['blockingCollider']['fileID']==0
        walls=unary_union([Polygon([(p['x'],p['y']) for p in path]) for path in d[5751537408444854577]['m_Points']['m_Paths']])
        assert walls.distance(Point(1.7,3.14))>.47 # additional fire sits on the walkable upper road
    print('PASS hard stage',i,'registration, references, inventory, duration, text')
s=docs(ROOT/'Assets/Project/Scenes/TimelineScene.unity');controller=s[1762420951]
assert controller['difficultyButton']['fileID'] in s and controller['difficultyButtonText']['fileID'] in s
for text in ('ノーマル・切替','ハード・切替'):
    assert all(c.isspace() or ord(c) in glyphs for c in text)
print('PASS difficulty selector references; use Unity PlayMode tests for mode selection/save isolation')

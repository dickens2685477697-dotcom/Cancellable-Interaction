"""Static Study1 scene checks. Does not replace a Unity/Quest runtime test."""
from pathlib import Path
import re

scene = (Path(__file__).resolve().parents[2] / 'Assets/Scenes/Study1.unity').read_text(encoding='utf-8')
blocks = {}
for match in re.finditer(r'^--- !u!(\d+) &(\d+)(?: stripped)?\s*$', scene, re.M):
    key = int(match.group(2))
    next_header = scene.find('\n--- !u!', match.end())
    blocks[key] = scene[match.start(): next_header if next_header >= 0 else len(scene)]

def field(block, name):
    match = re.search(r'^  ' + re.escape(name) + r':\s*(.*)$', block, re.M)
    assert match, f'{name} missing'
    return match.group(1).strip()

def parent(key):
    match = re.search(r'^  m_Father: \{fileID: (\d+)\}', blocks[key], re.M)
    assert match, f'no parent in transform {key}'
    return int(match.group(1))

def parents(key):
    result = []
    while key:
        assert key not in result, f'cyclic hierarchy at {key}'
        result.append(key)
        key = parent(key)
    return result

def vec(block, name):
    value = field(block, name)
    match = re.fullmatch(r'\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}', value)
    assert match, f'invalid {name}: {value}'
    return tuple(float(v) for v in match.groups())

def near(actual, expected, tolerance=1e-6):
    return all(abs(a-b) <= tolerance for a, b in zip(actual, expected))

root = 1885794242
target = 843864033
start = 570250457
hint = 706434806
assert field(blocks[1885794239], 'm_Name') == 'BackTrace'
assert field(blocks[1885794239], 'm_IsActive') == '1'
assert parent(root) == 0, 'BackTrace must be a world-space root'
assert root in parents(target), 'target not under BackTrace'
assert target in parents(start), 'start ball not under target'
assert target in parents(hint), 'task prompt not under target'
assert field(blocks[843864032], 'm_IsActive') == '1'
assert field(blocks[1387055148], 'm_IsActive') == '1'
assert field(blocks[570250456], 'm_IsActive') == '1'
assert field(blocks[1885794240], 'm_Enabled') == '0', 'Orbital follows target when enabled'
assert field(blocks[1885794241], 'm_Enabled') == '0', 'SolverHandler enabled'
assert field(blocks[1885794240], 'm_Script').find('14c3d8a4208d4b649529822e217623d4') >= 0
assert field(blocks[1885794241], 'm_Script').find('b55691ad5b034fe6966763a6e23818d2') >= 0
assert field(blocks[1885794243], 'm_Enabled') == '1', 'head anchor component must be enabled'
assert field(blocks[1885794243], 'm_GameObject') == '{fileID: 1885794239}', 'anchor must control the BackTrace root'
assert '94c7be1a22cc473388969ed166553c54' in field(blocks[1885794243], 'm_Script'), 'wrong anchor script'
assert abs(float(field(blocks[1885794243], 'ballCenterDistanceMeters'))-.36)<1e-6
assert field(blocks[1885794243], 'targetBall') == '{fileID: 843864038}'
assert field(blocks[1885794243], 'startBall') == '{fileID: 570250460}'
assert near(vec(blocks[root], 'm_LocalPosition'), (0, 1.1, .53)), 'fixed target should use Study3 height and requested 0.53 m forward coordinate'
study3 = (Path(__file__).resolve().parents[2] / 'Assets/Scenes/Study3/study3.unity').read_text(encoding='utf-8')
spawner = re.search(r'^--- !u!4 &1830579777\s*$(.*?)(?=^--- !u!)', study3, re.M | re.S)
assert spawner and near(vec(spawner.group(0), 'm_LocalPosition'), (0, 1.1, .537)), 'Study3 Spawner reference changed'
anchor_source = (Path(__file__).resolve().parents[2] / 'Assets/Scripts/Study1/Study1HeadAnchor.cs').read_text(encoding='utf-8')
assert 'transform.SetPositionAndRotation(' not in anchor_source, 'the anchor must not overwrite the fixed scene transform'
assert near(vec(blocks[root], 'm_LocalScale'), (1, 1, 1))
assert near(vec(blocks[1387055149], 'm_LocalPosition'), (0, 0, 0))
assert near(vec(blocks[1387055149], 'm_LocalScale'), (1, 1, 1))
assert near(vec(blocks[target], 'm_LocalPosition'), (0, 0, 0))
assert near(vec(blocks[target], 'm_LocalScale'), (.06, .06, .06))
assert abs(float(field(blocks[843864038], 'm_Radius')) * 2 * .06 - .06) < 1e-6, 'target diameter not 60 mm'
start_local = vec(blocks[start], 'm_LocalPosition')
target_scale = vec(blocks[target], 'm_LocalScale')
start_offset_world = tuple(a*b for a, b in zip(start_local, target_scale))
assert near(start_offset_world, (0, 0, -0.36)), 'start ball must be 360 mm in front of the target (centre to centre)'
assert field(blocks[843864034], 'TaskPrompt') == '{fileID: 706434807}'
assert field(blocks[843864034], 'GesturePrompt') == '{fileID: 239932347}'
assert field(blocks[843864034], 'MaxCount') == '20'
assert field(blocks[843864034], 'HoldDuration') == '3'
print('Study1 scene static checks passed: fixed world position (0, 1.1, 0.53), Study3 reference height, disabled solvers, 60mm target, 360mm centre spacing, prompt bindings, 20 trials and 3s hold.')
print('Static scene checks only; verify headset position and system recentering in Unity/Quest.')

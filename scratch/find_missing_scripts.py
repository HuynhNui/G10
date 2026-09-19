import os
import re

# Collect all project GUIDs
guid_to_path = {}
for root, dirs, files in os.walk('Assets'):
    for f in files:
        if f.endswith('.meta'):
            meta_path = os.path.join(root, f)
            with open(meta_path, 'r', encoding='utf-8', errors='ignore') as mf:
                first_few = mf.read(300)
            m = re.search(r'guid:\s*([a-f0-9]{32})', first_few)
            if m:
                guid_to_path[m.group(1)] = meta_path[:-5]

print(f"Total project GUIDs: {len(guid_to_path)}")

# Also check Packages/
for root, dirs, files in os.walk('Packages'):
    for f in files:
        if f.endswith('.meta'):
            meta_path = os.path.join(root, f)
            with open(meta_path, 'r', encoding='utf-8', errors='ignore') as mf:
                first_few = mf.read(300)
            m = re.search(r'guid:\s*([a-f0-9]{32})', first_few)
            if m:
                guid_to_path[m.group(1)] = meta_path[:-5]

# Scan scene files line by line
scenes = [
    r'Assets\_Project\Scenes\Gameplay\Zone01.unity',
    r'Assets\_Project\Scenes\Gameplay\GameplayCore.unity',
    r'Assets\_Project\Scenes\MainMenu\MainMenu.unity',
    r'Assets\_Project\Scenes\Bootstrap\Bootstrap.unity'
]

for s in scenes:
    if not os.path.exists(s): continue
    print(f"\n--- Checking {s} ---")
    current_mb = None
    with open(s, 'r', encoding='utf-8') as f:
        for i, line in enumerate(f, 1):
            if line.startswith('--- !u!114 &'):
                current_mb = line.strip()
            elif current_mb and 'm_Script:' in line:
                # e.g. m_Script: {fileID: 11500000, guid: ..., type: 3}
                m_guid = re.search(r'guid:\s*([a-f0-9]{32})', line)
                m_fileid = re.search(r'fileID:\s*(\d+)', line)
                fileid = m_fileid.group(1) if m_fileid else '0'
                if fileid == '0':
                    print(f"Line {i}: Missing script (fileID: 0) in {current_mb}")
                elif m_guid:
                    g = m_guid.group(1)
                    if g not in guid_to_path:
                        print(f"Line {i}: Unknown GUID {g} in {current_mb} -> {line.strip()}")
                else:
                    print(f"Line {i}: No GUID in script reference: {line.strip()} in {current_mb}")
                current_mb = None

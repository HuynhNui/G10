import re

for path in [r'Assets\_Project\Scenes\Gameplay\GameplayCore.unity', r'Assets\_Project\Scenes\Gameplay\Zone01.unity']:
    with open(path, 'r', encoding='utf-8') as f:
        text = f.read()
    print('=== ' + path + ' ===')
    blocks = text.split('--- !u!')
    for b in blocks:
        if not b.startswith('114 &'): continue
        lines = b.splitlines()
        header = lines[0]
        mb = header.split(' &')[1]
        script_line = [l for l in lines if 'm_Script:' in l]
        ecid_line = [l for l in lines if 'm_EditorClassIdentifier:' in l]
        name_line = [l for l in lines if 'm_Name:' in l]
        if script_line and ecid_line:
            s_val = script_line[0].strip()
            e_val = ecid_line[0].strip()
            if 'UnityEngine' not in e_val:
                print(f"  MB {mb}: {s_val} | {e_val}")

import os
import re

proj_meta_guids = {}
for root, dirs, files in os.walk('Assets'):
    for f in files:
        if f.endswith('.meta'):
            p = os.path.join(root, f)
            with open(p, 'r', encoding='utf-8', errors='ignore') as mf:
                m = re.search(r'guid:\s*([a-f0-9]{32})', mf.read(300))
                if m: proj_meta_guids[m.group(1)] = p

for scene_name in ['Zone01.unity', 'GameplayCore.unity', 'MainMenu.unity', 'Bootstrap.unity']:
    path = os.path.join(r'Assets\_Project\Scenes', 'Gameplay' if 'Zone' in scene_name or 'Core' in scene_name else ('MainMenu' if 'Main' in scene_name else 'Bootstrap'), scene_name)
    if not os.path.exists(path): continue
    with open(path, 'r', encoding='utf-8') as f:
        text = f.read()

    print(f"\n=== Checking {scene_name} ===")
    script_matches = re.findall(r'--- !u!114 &(\d+)\nMonoBehaviour:\n.*?m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([a-f0-9]{32})', text, re.DOTALL)
    for mb_id, g in script_matches:
        if g not in proj_meta_guids:
            # Check what GameObject it belongs to
            go_match = re.search(r'--- !u!114 &' + mb_id + r'\nMonoBehaviour:\n.*?m_GameObject:\s*\{fileID:\s*(\d+)\}', text, re.DOTALL)
            goid = go_match.group(1) if go_match else 'unknown'
            goname_match = re.search(r'--- !u!1 &' + goid + r'\nGameObject:\n.*?m_Name:\s*([^\n]+)', text, re.DOTALL)
            goname = goname_match.group(1) if goname_match else 'unknown'
            ecid_match = re.search(r'--- !u!114 &' + mb_id + r'\nMonoBehaviour:\n.*?m_EditorClassIdentifier:\s*([^\n]+)', text, re.DOTALL)
            ecid = ecid_match.group(1).strip() if ecid_match else 'none'
            print(f"  MB {mb_id} on GO {goid} ({goname}): GUID {g} -> ecid: {ecid}")

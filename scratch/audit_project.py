import os
import re
import sys

# Ensure UTF-8 output on Windows console
if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

PANEL_001_GUID = '2364b6cfb738544f84dae5c187e3055d'
PANEL_000_GUID = 'aae4a8af102d5f11bf66f0fe3656dd04'
FONT_BOLD_GUID = 'ef82219bc9fb5d49b82d64b7648713d6'

scenes_dir = r'Assets\_Project\Scenes'

def audit_scenes():
    print("=== AUDITING SCENES ===")
    scene_files = []
    for root, dirs, files in os.walk(scenes_dir):
        for f in files:
            if f.endswith('.unity') and not f.endswith('.bak'):
                scene_files.append(os.path.join(root, f))
                
    total_panel_000 = 0
    
    for s in scene_files:
        rel = os.path.relpath(s, scenes_dir)
        with open(s, 'r', encoding='utf-8') as f:
            text = f.read()
            
        p001_count = text.count(PANEL_001_GUID)
        p000_count = text.count(PANEL_000_GUID)
        total_panel_000 += p000_count
        
        print(f"Scene: {rel}")
        print(f"  panel-001 ('BAT' box style) occurrences: {p001_count}")
        print(f"  panel-000 occurrences: {p000_count}")
        
    print(f"\nTotal legacy panel-000 occurrences across all scenes: {total_panel_000}")

def audit_hotspots():
    print("\n=== AUDITING CABIN & NAVIGATION HOTSPOTS ===")
    z01_path = r'Assets\_Project\Scenes\Gameplay\Zone01.unity'
    with open(z01_path, 'r', encoding='utf-8') as f:
        text = f.read()
        
    # All 14 interactive hotspots in Zone01
    hotspots = [
        # Cabin Frame interaction hotspots:
        (648337001, 'MapHotspot'),
        (42255854, 'MonitorHotspot'),
        (749830910, 'RadarHotspot'),
        (1433112369, 'NavigationHotspot'),
        (1346279232, 'BackpackHotspot'),
        (588362063, 'CameraHotspot'),
        (1367672022, 'CaptureHotspot'),
        # Navigation panel control hotspots:
        (1883898878, 'ForwardHotspot'),
        (206251913, 'ReverseHotspot'),
        (534453456, 'TurnLeftHotspot'),
        (1957777460, 'TurnRightHotspot'),
        (1746887561, 'OpenMapHotspot'),
        (1268003556, 'AscendHotspot'),
        (1066034808, 'DiveHotspot'),
    ]
    
    all_clean = True
    for go_id, name in hotspots:
        go_block = re.search(r'--- !u!1 &' + str(go_id) + r'\n(.*?)(?=\n--- !u!|\Z)', text, re.DOTALL)
        if not go_block:
            print(f"  ERROR: Could not find GameObject {name} ({go_id})")
            all_clean = False
            continue
            
        comp_ids = re.findall(r'component: \{fileID: (\d+)\}', go_block.group(1))
        sprite_val = None
        type_val = None
        for cid in comp_ids:
            cb = re.search(r'--- !u!114 &' + cid + r'\n(.*?)(?=\n--- !u!|\Z)', text, re.DOTALL)
            if cb and 'm_Sprite:' in cb.group(1):
                sm = re.search(r'm_Sprite: \{fileID: (\d+)', cb.group(1))
                tm = re.search(r'm_Type: (\d+)', cb.group(1))
                if sm: sprite_val = sm.group(1)
                if tm: type_val = tm.group(1)
                
        if sprite_val == '0' and type_val == '0':
            print(f"  [PASS] {name:20} (GO {go_id}): strictly frameless (fileID: 0, type: 0)")
        else:
            print(f"  [FAIL] {name:20} (GO {go_id}): sprite={sprite_val}, type={type_val}")
            all_clean = False
            
    if all_clean:
        print("\n=> ALL interactive hotspots are verified 100% frameless (no border popups on hover)!")

def audit_audio():
    print("\n=== AUDITING AUDIO ASSETS & SCRIPT HOOKS ===")
    sfx_dir = r'Assets\_Project\Audio\SFX'
    ambient_dir = r'Assets\_Project\Audio\Ambient'
    res_sfx = r'Assets\_Project\Resources\Audio\SFX'
    res_amb = r'Assets\_Project\Resources\Audio\Ambient'
    
    expected_sfx = [
        'ui_button_click.wav', 'ui_button_back.wav', 'ui_item_click.wav',
        'radar_ping.wav', 'capture_grab.wav', 'capture_success.wav',
        'capture_fail.wav', 'ui_pause.wav'
    ]
    expected_ambient = ['ambient_submarine_loop.wav']
    
    all_ok = True
    for f in expected_sfx:
        p1 = os.path.join(sfx_dir, f)
        p2 = os.path.join(res_sfx, f)
        if os.path.exists(p1) and os.path.exists(p1 + '.meta') and os.path.exists(p2) and os.path.exists(p2 + '.meta'):
            sz = os.path.getsize(p1)
            print(f"  [PASS] SFX: {f:25} ({sz:6} bytes, .meta OK, Resources OK)")
        else:
            print(f"  [FAIL] MISSING SFX: {f}")
            all_ok = False
            
    for f in expected_ambient:
        p1 = os.path.join(ambient_dir, f)
        p2 = os.path.join(res_amb, f)
        if os.path.exists(p1) and os.path.exists(p1 + '.meta') and os.path.exists(p2) and os.path.exists(p2 + '.meta'):
            sz = os.path.getsize(p1)
            print(f"  [PASS] Ambient: {f:25} ({sz:6} bytes, .meta OK, Resources OK)")
        else:
            print(f"  [FAIL] MISSING Ambient: {f}")
            all_ok = False
            
    if all_ok:
        print("=> ALL audio files and Resources fallbacks are in place!")

if __name__ == '__main__':
    audit_scenes()
    audit_hotspots()
    audit_audio()

import os
import re

# Collect all valid GUIDs in Assets/ and Packages/
valid_guids = {}
for root, dirs, files in os.walk('Assets'):
    for f in files:
        if f.endswith('.meta'):
            p = os.path.join(root, f)
            with open(p, 'r', encoding='utf-8', errors='ignore') as mf:
                m = re.search(r'guid:\s*([a-f0-9]{32})', mf.read(300))
                if m:
                    valid_guids[m.group(1)] = p[:-5]

# Built-in Unity packages commonly used
builtin_prefixes = [
    '0000000000000000', # Unity built-in resources
]

# Scan all yaml files (.unity, .prefab, .asset)
yaml_files = []
for root, dirs, files in os.walk('Assets'):
    for f in files:
        if (f.endswith('.unity') or f.endswith('.prefab') or f.endswith('.asset')) and not f.endswith('.bak'):
            yaml_files.append(os.path.join(root, f))

print(f"Checking {len(yaml_files)} YAML files against {len(valid_guids)} valid GUIDs...")

for yf in yaml_files:
    with open(yf, 'r', encoding='utf-8', errors='ignore') as f:
        current_obj = ""
        current_type = ""
        for line_num, line in enumerate(f, 1):
            if line.startswith('--- !u!'):
                current_obj = line.strip()
                # e.g. --- !u!114 &12345
                parts = line.strip().split(' ')
                current_type = parts[1][4:] if len(parts) > 1 else ""
            elif current_type == '114' and 'm_Script:' in line:
                # check fileID
                fid_match = re.search(r'fileID:\s*(\d+)', line)
                guid_match = re.search(r'guid:\s*([a-f0-9]{32})', line)
                if not fid_match or fid_match.group(1) == '0':
                    print(f"[MISSING SCRIPT] {yf}:{line_num} in {current_obj} -> {line.strip()}")
                elif guid_match:
                    g = guid_match.group(1)
                    if g not in valid_guids and not any(g.startswith(bp) for bp in builtin_prefixes):
                        # check if it's a known uGUI guid
                        # We will print it
                        print(f"[UNKNOWN GUID] {yf}:{line_num} in {current_obj} -> guid={g}")

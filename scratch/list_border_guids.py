import os
import re

d = r'Assets\_Project\Art\UI\Borders'
for f in sorted(os.listdir(d)):
    if f.endswith('.meta'):
        p = os.path.join(d, f)
        with open(p, 'r', encoding='utf-8') as mf:
            m = re.search(r'guid:\s*([a-f0-9]{32})', mf.read())
            guid = m.group(1) if m else 'none'
            print(f"{f[:-5]:35}: {guid}")

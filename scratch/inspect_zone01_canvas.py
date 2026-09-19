with open(r'Assets\_Project\Scenes\Gameplay\Zone01.unity', 'r', encoding='utf-8') as f:
    text = f.read()

import re
canvases = re.findall(r'--- !u!223 &(\d+)\nCanvas:\n.*?(?=\n--- !u!|\Z)', text, re.DOTALL)
for c in canvases:
    print("Found Canvas:")
    for l in c.splitlines()[:15]:
        print(" ", l)

raycasters = re.findall(r'--- !u!114 &(\d+)\nMonoBehaviour:\n.*?m_Script: \{fileID: 11500000, guid: 0cd44c1031e13a943bb63640046fad76, type: 3\}.*?(?=\n--- !u!|\Z)', text, re.DOTALL)
for r in raycasters:
    print("Found GraphicRaycaster:")
    for l in r.splitlines()[:10]:
        print(" ", l)

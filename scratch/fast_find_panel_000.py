PANEL_000_GUID = 'aae4a8af102d5f11bf66f0fe3656dd04'

with open(r'Assets\_Project\Scenes\Gameplay\Zone01.unity', 'r', encoding='utf-8') as f:
    lines = f.readlines()

current_block = []
current_header = ''

for line in lines:
    if line.startswith('--- !u!'):
        if current_block:
            block_text = ''.join(current_block)
            if PANEL_000_GUID in block_text:
                print(f"Found panel-000 in {current_header}:")
                for l in current_block:
                    if 'm_Name:' in l or 'm_GameObject:' in l or 'm_Sprite:' in l:
                        print(" ", l.strip())
        current_block = [line]
        current_header = line.strip()
    else:
        current_block.append(line)

if current_block and PANEL_000_GUID in ''.join(current_block):
    print(f"Found panel-000 in {current_header}:")
    for l in current_block:
        if 'm_Name:' in l or 'm_GameObject:' in l or 'm_Sprite:' in l:
            print(" ", l.strip())

with open(r'Assets\_Project\Scenes\Gameplay\Zone01.unity', 'r', encoding='utf-8') as f:
    lines = f.readlines()

current_block = []
current_header = ''

for line in lines:
    if line.startswith('--- !u!'):
        if current_block:
            block_text = ''.join(current_block)
            if 'BackpackHotspot' in block_text or 'RadarHotspot' in block_text:
                print(f"=== {current_header} ===")
                print(block_text[:400])
        current_block = [line]
        current_header = line.strip()
    else:
        current_block.append(line)

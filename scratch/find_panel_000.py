import re

with open(r'Assets\_Project\Scenes\Gameplay\Zone01.unity', 'r', encoding='utf-8') as f:
    text = f.read()

# Map MonoBehaviours with panel-000 to their GameObject
panel000_matches = re.findall(r'--- !u!114 &(\d+)\nMonoBehaviour:\n.*?m_GameObject: \{fileID: (\d+)\}.*?aae4a8af102d5f11bf66f0fe3656dd04', text, re.DOTALL)
for mb_id, go_id in panel000_matches:
    # Find name of go_id
    go_match = re.search(r'--- !u!1 &' + go_id + r'\nGameObject:\n.*?m_Name: ([^\n]+)', text, re.DOTALL)
    name = go_match.group(1) if go_match else 'UNKNOWN'
    print(f"MonoBehaviour {mb_id} on GameObject {go_id} ({name}) uses panel-000")

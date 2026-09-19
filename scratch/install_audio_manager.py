import os

core_path = r'Assets\_Project\Scenes\Gameplay\GameplayCore.unity'

with open(core_path, 'r', encoding='utf-8') as f:
    text = f.read()

AUDIO_MGR_GUID = 'b45781a9c3d4e5f6a1b2c3d4e5f60002'

btn_click_guid = 'a1000000000000000000000000000001'
btn_back_guid = 'a1000000000000000000000000000002'
item_click_guid = 'a1000000000000000000000000000003'
radar_ping_guid = 'a1000000000000000000000000000004'
capture_grab_guid = 'a1000000000000000000000000000005'
capture_success_guid = 'a1000000000000000000000000000006'
capture_fail_guid = 'a1000000000000000000000000000007'
ui_pause_guid = 'a1000000000000000000000000000008'
ambient_sub_guid = 'a1000000000000000000000000000009'

audio_yaml = f'''--- !u!1 &1650000310
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1650000311}}
  - component: {{fileID: 1650000312}}
  m_Layer: 0
  m_Name: AudioManager
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &1650000311
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1650000310}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &1650000312
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1650000310}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {AUDIO_MGR_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: G10.Prototype.Audio::G10.Prototype.Audio.AudioManager
  masterVolume: 1
  sfxVolume: 1
  ambientVolume: 0.55
  buttonClickClip: {{fileID: 8300000, guid: {btn_click_guid}, type: 3}}
  buttonBackClip: {{fileID: 8300000, guid: {btn_back_guid}, type: 3}}
  itemClickClip: {{fileID: 8300000, guid: {item_click_guid}, type: 3}}
  radarPingClip: {{fileID: 8300000, guid: {radar_ping_guid}, type: 3}}
  captureGrabClip: {{fileID: 8300000, guid: {capture_grab_guid}, type: 3}}
  captureSuccessClip: {{fileID: 8300000, guid: {capture_success_guid}, type: 3}}
  captureFailClip: {{fileID: 8300000, guid: {capture_fail_guid}, type: 3}}
  pauseMenuClip: {{fileID: 8300000, guid: {ui_pause_guid}, type: 3}}
  ambientSubmarineClip: {{fileID: 8300000, guid: {ambient_sub_guid}, type: 3}}
'''

if '1650000310' not in text:
    text = text.rstrip() + '\n' + audio_yaml
    with open(core_path, 'w', encoding='utf-8') as f:
        f.write(text)
    print('Added AudioManager to GameplayCore.unity!')
else:
    print('AudioManager already present in GameplayCore.unity!')

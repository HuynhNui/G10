using G10.Prototype.Navigation;
using UnityEditor;
using UnityEngine;

namespace G10.Prototype.Editor
{
    [CustomEditor(typeof(ZoneNavigation))]
    public sealed class ZoneNavigationInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Cấu hình tàu trực tiếp trong scene. Các giá trị này là chỉ số ban đầu. Save có sẵn giữ nâng cấp và tài nguyên; bật Use Scene Ship Settings On Load khi muốn thử chỉ số scene thay cho save.", MessageType.Info);
            DrawDefaultInspector();
            var ship = (ZoneNavigation)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
                if (GUILayout.Button("Áp dụng chỉ số scene & hồi đầy (Play Mode)")) ship.ApplySceneShipSettings();
        }
    }
}

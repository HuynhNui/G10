using System;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>One-time, narrow opacity migration. Does not rebuild maps, change art, terrain or route data.</summary>
    public static class MapLightOpacitySetupEditor
    {
        private const string MapRoot = "Assets/_Project/Content/Maps";
        private const string UndoName = "Configure zone map light opacity";

        [MenuItem("G10/Maps/Apply Playtest Light Opacity Defaults")]
        public static void ApplyDefaults()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before configuring light opacity.");
            var maps = new ZoneMapConfig[4];
            for (int i = 0; i < maps.Length; i++)
            {
                string path = $"{MapRoot}/Zone{i + 1:00}.asset";
                maps[i] = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>(path)
                    ?? throw new InvalidOperationException("Missing map config: " + path);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            bool assetsChanged = false;
            foreach (ZoneMapConfig map in maps) assetsChanged |= InitializeDefaults(map);
            foreach (var presentation in UnityEngine.Object.FindObjectsByType<ZoneMapPresentation>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (presentation == null || presentation.config == null || presentation.lightImages == null ||
                    Array.IndexOf(maps, presentation.config) < 0) continue;
                bool sceneChanged = false;
                for (int i = 0; i < presentation.lightImages.Length; i++)
                {
                    UnityEngine.UI.RawImage light = presentation.lightImages[i];
                    if (light == null || Mathf.Approximately(light.color.a, presentation.config.LightOpacityFor(i))) continue;
                    Undo.RecordObject(light, UndoName);
                    Color color = light.color;
                    color.a = presentation.config.LightOpacityFor(i);
                    light.color = color;
                    EditorUtility.SetDirty(light);
                    sceneChanged = true;
                }
                if (sceneChanged && presentation.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(presentation.gameObject.scene);
            }
            Undo.CollapseUndoOperations(undoGroup);
            if (assetsChanged)
                foreach (ZoneMapConfig map in maps) AssetDatabase.SaveAssetIfDirty(map);
        }

        /// <summary>Installer-safe defaults; migrated configs retain subsequent Inspector edits and layer overrides.</summary>
        public static bool InitializeDefaults(ZoneMapConfig config)
        {
            if (config == null || config.LightOpacityConfigured) return false;
            float opacity = config.zoneId switch
            {
                "Zone02" => .35f,
                "Zone03" => .225f,
                "Zone04" => .30f,
                _ => 1f
            };
            Undo.RecordObject(config, UndoName);
            config.ConfigureLightOpacity(opacity);
            EditorUtility.SetDirty(config);
            return true;
        }
    }
}

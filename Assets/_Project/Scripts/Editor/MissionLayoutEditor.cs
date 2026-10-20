using System;
using System.Linq;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.Tutorial;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Editor
{
    /// <summary>Targeted asset/marker migration. Does not reinstall the cabin or regenerate terrain.</summary>
    public static class MissionLayoutEditor
    {
        [MenuItem("G10/Missions/Apply 11 Location Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            MissionConfigEditor.Build();
            for (int i = 1; i <= 4; i++)
            {
                var map = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"Assets/_Project/Content/Maps/Zone{i:00}.asset");
                var mission = AssetDatabase.LoadAssetAtPath<ZoneMissionConfig>($"Assets/_Project/Content/Missions/Zone{i:00}.asset");
                map.locations = mission.locations.Select(location =>
                    Array.Find(map.locations, poi => poi.id == location.poiId) ?? throw new InvalidOperationException("Missing POI " + location.poiId)).ToArray();
                map.hiddenLocationIds = mission.locations.Where(l => l.visibility == LocationVisibility.HiddenRadar).Select(l => l.id).ToArray();
                if (i == 2)
                {
                    var poi = Array.Find(map.locations, p => p.id == "zone02-l3");
                    if (!poi.overrideDepth || poi.targetDepth > 500) { poi.overrideDepth = true; poi.targetDepth = 500; }
                }
                EditorUtility.SetDirty(map);
            }
            var tutorial = AssetDatabase.LoadAssetAtPath<TutorialConfig>(TutorialEditor.ConfigPath);
            var defaults = ScriptableObject.CreateInstance<TutorialConfig>();
            foreach (var step in new[] { TutorialStepId.Capture, TutorialStepId.Upgrade, TutorialStepId.Complete })
            {
                var instruction = Array.Find(tutorial.instructions, item => item.step == step);
                instruction.text = Array.Find(defaults.instructions, item => item.step == step).text;
            }
            UnityEngine.Object.DestroyImmediate(defaults);
            EditorUtility.SetDirty(tutorial);
            AssetDatabase.SaveAssets();

            const string path = "Assets/_Project/Scenes/Gameplay/Zone01.unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save the authored Zone01 scene before migrating markers.");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var overlay in root.GetComponentsInChildren<PhotoSurveyMap>(true))
                {
                    if (overlay.mapConfig == null) continue;
                    ZoneMapEditor.BuildMarkers(overlay, overlay.mapConfig);
                    EditorUtility.SetDirty(overlay);
                }
                foreach (var story in root.GetComponentsInChildren<ZoneOneStory>(true))
                {
                    var map = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>("Assets/_Project/Content/Maps/Zone01.asset");
                    story.config = map.missionConfig;
                    story.poiIds = map.locations.Select(p => p.id).ToArray();
                    story.survey.locations = map.locations.Select(ZoneMapEditor.ClonePoi).ToArray();
                    EditorUtility.SetDirty(story); EditorUtility.SetDirty(story.survey);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}

using System;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>Corrects surface rendering without rebuilding UI or changing its callbacks.</summary>
    public static class WatercolorSurfaceEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Zone01.unity";
        private const string PaperPath = "Assets/_Project/Art/UI/Desktop/Update_UIKit/Sprites/Panels/Panel_Large.png";

        [MenuItem("G10/UI/Fix Watercolor Corners and Card Quality")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before editing UI surfaces.");
            foreach (var cabin in UnityEngine.Object.FindObjectsByType<CabinStationView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ApplyTo(cabin);
        }

        public static void ApplyTo(CabinStationView cabin)
        {
            var paper = AssetDatabase.LoadAssetAtPath<Sprite>(PaperPath);
            if (paper == null) throw new InvalidOperationException("Missing watercolor paper panel.");
            Canvas.ForceUpdateCanvases();
            foreach (var image in cabin.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.name == "WatercolorHUD" || image.name == "WatercolorHeader")
                {
                    Undo.RecordObject(image, "Reduce watercolor HUD corners");
                    image.sprite = paper;
                    image.type = UnityEngine.UI.Image.Type.Sliced;
                    // Preserve the paper border at 12px rather than stretching capsule artwork.
                    image.pixelsPerUnitMultiplier = 2;
                }
                else if (image.name == "PreviewFrame" && image.GetComponentInParent<G10.Prototype.Computer.PhotoCameraView>(true) != null)
                {
                    Undo.RecordObject(image, "Use a panel border for the camera frame");
                    image.sprite = paper;
                    image.type = UnityEngine.UI.Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 2;
                    image.fillCenter = false;
                }
                else if (image.name == "RadarInfo" || image.name == "NavigationInfo")
                {
                    Undo.RecordObject(image, "Use a rectangular radar card");
                    image.sprite = paper;
                    image.type = UnityEngine.UI.Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 2;
                }
                else if (image.sprite != null && AssetDatabase.GetAssetPath(image.sprite).StartsWith("Assets/_Project/Art/UI/Borders/", StringComparison.Ordinal) &&
                    image.GetComponent<UnityEngine.UI.Button>() == null &&
                    (image.name == "Letterbox" || image.name == "CameraPanel" ||
                     image.rectTransform.rect.width > 400 && image.rectTransform.rect.height > 180))
                {
                    Undo.RecordObject(image, "Replace legacy pixel card border");
                    // Full-screen backdrops need only a flat fill. Actual cards retain a smooth paper frame.
                    bool backdrop = image.name == "Letterbox" || image.name == "CameraPanel" || image.name == "WorldMapPanel" ||
                        image.name == "Zone02MapPanel" || image.name == "Zone03MapPanel" || image.name == "Zone04MapPanel";
                    image.sprite = backdrop ? null : paper;
                    image.type = backdrop ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 2;
                }
                else continue;
                EditorUtility.SetDirty(image);
            }
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
        }

        public static void ApplyBatch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            Apply();
            EditorSceneManager.SaveScene(scene);
        }
    }
}

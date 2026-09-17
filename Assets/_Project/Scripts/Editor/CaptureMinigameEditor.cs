using System;
using System.IO;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>Adds one modal to the existing cabin; never rebuilds the authored scene.</summary>
    public static class CaptureMinigameEditor
    {
        private const string Art = "Assets/_Project/Art/Sprites/CaptureMinigame_Assets/";
        private const string ProfilePath = "Assets/_Project/Data/Computer/Zone01CaptureMinigame.asset";

        [MenuItem("G10/Zone 1/Install Capture Minigame")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01")
                throw new InvalidOperationException("Open Zone01 before installing the capture minigame.");
            var catcher = cabin.GetComponent<CreatureCatcher>();
            if (catcher == null) throw new InvalidOperationException("Install creature catching first.");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install capture minigame");
            var profile = AssetDatabase.LoadAssetAtPath<CaptureMinigameProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CaptureMinigameProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var controller = cabin.GetComponent<CaptureMinigameController>();
            if (controller == null) controller = Undo.AddComponent<CaptureMinigameController>(cabin.gameObject);
            var parent = cabin.MapPanel.transform.parent;
            var existing = parent.Find("CaptureMinigamePanel");
            CaptureMinigameView view;
            if (existing != null)
            {
                view = existing.GetComponent<CaptureMinigameView>();
                if (view == null) throw new InvalidOperationException("CaptureMinigamePanel exists without its view; inspect it before installing.");
                ApplyArt(view.creature, "Fish");
                var controls = existing.Find("Controls")?.GetComponent<Text>();
                if (controls != null)
                {
                    controls.text = "W / S  OR  ↑ / ↓     TURN     •     STEER A CURVED INTERCEPT";
                    EditorUtility.SetDirty(controls);
                }
            }
            else
            {
                var root = Box(parent, "CaptureMinigamePanel", 0, 0, 1920, 1080);
                root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
                root.gameObject.AddComponent<Image>().color = Color.black;
                view = root.gameObject.AddComponent<CaptureMinigameView>();
                Label(root, "Title", "CAPTURE // SONAR LINK", 140, 65, 1000, 55, 30, TextAnchor.MiddleLeft);
                view.timer = Label(root, "Timer", "45s", 1340, 65, 160, 55, 34);
                var exit = ArtImage(root, "Exit", "Capture_Button_Frame", 1550, 65, 225, 55);
                exit.raycastTarget = true;
                var button = exit.gameObject.AddComponent<Button>(); button.targetGraphic = exit;
                button.transition = Selectable.Transition.None;
                button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                UnityEventTools.AddPersistentListener(button.onClick, view.Exit);
                Label(exit.transform, "Label", "ESC / EXIT", 0, 0, 225, 55, 22);
                ArtImage(root, "PlayfieldFrame", "Capture_Playfield_Frame", 130, 215, 1660, 500);
                view.playfield = Box(root, "Playfield", 230, 240, 1460, 450);
                view.playfield.gameObject.AddComponent<RectMask2D>();
                view.hook = Actor(view.playfield, "Hook", "Capture_Hook", profile.hookSize);
                view.creature = Actor(view.playfield, "Creature", "Fish", profile.creatureSize);
                view.impact = Actor(view.playfield, "Impact", "Capture_Hook_Impact", new(150,70));
                view.spark = Actor(view.playfield, "Spark", "Capture_Hit_Spark", new(85,100));
                view.success = Actor(view.playfield, "Success", "Capture_Result_Success", new(160,160));
                view.failure = Actor(view.playfield, "Failure", "Capture_Result_Failure", new(160,160));
                view.impact.enabled = view.spark.enabled = view.success.enabled = view.failure.enabled = false;
                view.stateLabel = Label(root, "State", "INTERCEPT TARGET", 460, 145, 1000, 40, 24);
                Label(root, "ProgressTitle", "CAPTURE PROGRESS", 570, 780, 780, 40, 24);
                ArtImage(root, "ProgressFrame", "Capture_Progress_Frame", 500, 842, 850, 55);
                view.progressClip = Box(root, "ProgressClip", 521, 855, 808, 29);
                view.progressClip.pivot = new(0,.5f); view.progressClip.anchoredPosition = new(521,-869.5f);
                view.progressClip.gameObject.AddComponent<RectMask2D>();
                ArtImage(view.progressClip, "Fill", "Capture_Progress_Fill", 0, 0, 808, 29);
                view.progressWidth = 808;
                view.hitCounter = Label(root, "HitCounter", "0 / 5", 1390, 835, 150, 65, 36);
                Label(root, "Controls", "W / S  OR  ↑ / ↓     TURN     •     STEER A CURVED INTERCEPT", 250, 970, 1420, 45, 24);
                root.SetAsLastSibling();
                root.gameObject.SetActive(false);
            }
            controller.cabin = cabin; controller.view = view;
            if (controller.profile == null) controller.profile = profile;
            view.controller = controller; catcher.minigame = controller;
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(view); EditorUtility.SetDirty(catcher);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Capture minigame installed on the existing Zone01 cabin.");
        }

        public static void InstallBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity");
            Install();
            EditorSceneManager.SaveOpenScenes();
        }

        private static RectTransform Box(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create capture minigame UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new(0,1);
            rect.sizeDelta = new(width,height); rect.anchoredPosition = new(x + width/2, -y - height/2);
            return rect;
        }

        private static Text Label(Transform parent, string name, string text, float x, float y, float width, float height, int size,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var label = Box(parent, name, x,y,width,height).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size; label.color = Color.white; label.text = text;
            label.alignment = alignment; label.raycastTarget = false;
            return label;
        }

        private static RawImage Actor(Transform parent, string name, string asset, Vector2 size)
        {
            var image = ArtImage(parent, name, asset, 0,0,size.x,size.y);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new(.5f,.5f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            return image;
        }

        private static RawImage ArtImage(Transform parent, string name, string asset, float x, float y, float width, float height)
        {
            var image = Box(parent, name, x,y,width,height).gameObject.AddComponent<RawImage>();
            ApplyArt(image, asset);
            return image;
        }

        private static void ApplyArt(RawImage image, string asset)
        {
            string path = Art + asset + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing minigame asset: " + path);
            // Preserve supplied PNGs and sprite slicing; use their alpha and crisp texture sampling.
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            image.texture = texture; image.color = Color.white; image.raycastTarget = false;
            image.uvRect = new Rect(0, 0, 1, 1);
            // The pack has large transparent margins. Crop only the UVs, never the source file.
            var copy = new Texture2D(2,2);
            try
            {
                copy.LoadImage(File.ReadAllBytes(path));
                var pixels = copy.GetPixels32(); int minX = copy.width, minY = copy.height, maxX = 0, maxY = 0;
                for (int py = 0; py < copy.height; py++) for (int px = 0; px < copy.width; px++)
                {
                    if (pixels[py*copy.width+px].a <= 16) continue;
                    minX = Mathf.Min(minX,px); minY = Mathf.Min(minY,py); maxX = Mathf.Max(maxX,px+1); maxY = Mathf.Max(maxY,py+1);
                }
                if (maxX > minX && maxY > minY)
                    image.uvRect = new((float)minX/copy.width, (float)minY/copy.height, (float)(maxX-minX)/copy.width, (float)(maxY-minY)/copy.height);
            }
            finally { Object.DestroyImmediate(copy); }
            EditorUtility.SetDirty(image);
        }
    }
}

using System;
using System.IO;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>Updates the existing cabin in place, preserving its gameplay wiring.</summary>
    public static class NavigationArtworkEditor
    {
        private const string Art = "Assets/_Project/Art/";
        private static readonly Color Ink = new(.11f, .23f, .28f, 1);

        [MenuItem("G10/Zone 1/Install Navigation Artwork")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01")
                throw new InvalidOperationException("Open Zone01 before installing navigation artwork.");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install navigation artwork");
            var config = new SerializedObject(cabin);
            Transform helm = cabin.NavigationPanel.transform;
            var idle = Texture("PrototypeCabin/Navigation/Idle_No_Compassneedle.png");
            var mapArt = Texture("Environment/Zone1/Mapingame.png");
            config.FindProperty("navigationArt").objectReferenceValue = idle;
            config.FindProperty("chartArt").objectReferenceValue = mapArt;
            helm.GetComponent<RawImage>().texture = idle;
            Place(helm.Find("X"), 405, 205, 270, 125);
            Place(helm.Find("Y"), 405, 400, 270, 125);
            Place(helm.Find("Depth"), 355, 730, 245, 120);
            Place(helm.Find("Heading"), 1330, 520, 295, 75);
            foreach (string name in new[] { "X", "Y", "Depth", "Heading" })
                helm.Find(name).GetComponent<Text>().color = Ink;
            helm.Find("CompassCenter").gameObject.SetActive(false);
            var pivot = helm.Find("HeadingPivot");
            Place(pivot, 1478, 319, 0, 0);
            pivot.Find("Needle").gameObject.SetActive(false);
            var needle = Image("CompassArtwork", pivot, Texture("PrototypeCabin/Navigation/Compassneedle.png"), 0, -26, 107, 53);
            // Align the triangle (14 degrees above screen-right) with the ship's north.
            config.FindProperty("compassArtOffset").floatValue = 76;
            config.FindProperty("compassNeedle").objectReferenceValue = pivot;
            needle.raycastTarget = false;

            string[] names = { "ForwardHotspot", "ReverseHotspot", "TurnLeftHotspot", "TurnRightHotspot", "AscendHotspot", "DiveHotspot" };
            string[] files = { "Move_Forward", "Move_Backward", "Turn_Left", "Turn_Right", "Depth_Up", "Depth_Down" };
            Rect[] areas = { new(888,205,247,165), new(888,370,247,158), new(1330,650,150,200),
                new(1490,650,170,200), new(642,695,211,106), new(642,803,211,99) };
            var controls = config.FindProperty("pressedControls"); controls.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                Rect a = areas[i]; Place(helm.Find(names[i]), a.x, a.y, a.width, a.height);
                var patch = Image("Pressed" + files[i], helm, Texture("PrototypeCabin/Navigation/" + files[i] + ".png"), a.x, a.y, a.width, a.height);
                patch.uvRect = new Rect(a.x / 1920, 1 - a.yMax / 1080, a.width / 1920, a.height / 1080);
                patch.enabled = false;
                controls.GetArrayElementAtIndex(i).objectReferenceValue = patch;
                var button = helm.Find(names[i]).GetComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            }
            var mini = helm.Find("MiniMap");
            Place(mini, 918, 708, 286, 167); mini.GetComponent<RawImage>().texture = mapArt;
            Place(helm.Find("OpenMapHotspot"), 918, 708, 286, 167);

            var map = cabin.MapPanel.transform;
            var content = map.Find("SquareChartContent");
            var fit = content.GetComponent<AspectRatioFitter>();
            if (fit != null) Object.DestroyImmediate(fit);
            // 24 by 14 square cells; both axes and their labels are outside the art.
            Place(content, 192, 95, 1536, 896);
            content.GetComponent<RawImage>().texture = mapArt;
            content.GetComponent<RawImage>().uvRect = new Rect(0, 0, 1, 1);
            Transform oldFrame = map.Find("ChartOuterFrame");
            if (oldFrame != null) Object.DestroyImmediate(oldFrame.gameObject);
            var frame = new GameObject("ChartOuterFrame", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            frame.transform.SetParent(map, false); Place(frame.transform, 130, 78, 1640, 962);
            frame.GetComponent<UnityEngine.UI.Image>().color = new(.12f, .22f, .25f, 1);
            frame.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            frame.transform.SetAsFirstSibling(); content.SetSiblingIndex(1);
            for (int x = 0; x <= 1200; x += 100)
                Label(frame.transform, "X" + x, x.ToString(), 62 + x * 1.28f - 30, 916, 60, 30);
            for (int y = 0; y <= 700; y += 50)
                Label(frame.transform, "Y" + y, y.ToString(), 2, 17 + (700 - y) * 1.28f - 15, 53, 30);
            // The existing footer and legend remain above the frame.
            Place(map.Find("ChartFooter"), 255, 1042, 1400, 36);
            Place(map.Find("ChartCoordinate"), 280, 1042, 1310, 36);
            var location = Texture("Environment/Zone1/location.png");
            foreach (var overlay in cabin.GetComponentsInChildren<PhotoSurveyMap>(true))
            {
                // Centers of the three brackets in the 1920 x 1080 reference Map.png.
                Vector2[] sourceCenters = { new(999, 330), new(1171, 809), new(400, 953) };
                overlay.locationIcons = new RawImage[3];
                overlay.locationCoordinates = new Vector2[3];
                if (overlay.locationTasks == null || overlay.locationTasks.Length != 3)
                    overlay.locationTasks = new[] { new PhotoSurveyMap.LocationTasks(), new PhotoSurveyMap.LocationTasks(), new PhotoSurveyMap.LocationTasks() };
                for (int i = 0; i < sourceCenters.Length; i++)
                {
                    Vector2 uv = new(sourceCenters[i].x / 1920f, 1 - sourceCenters[i].y / 1080f);
                    overlay.locationCoordinates[i] = ZoneNavigation.CellCenter(ZoneNavigation.UVToCoordinates(uv));
                    uv = ZoneNavigation.CoordinatesToUV(overlay.locationCoordinates[i]);
                    var icon = Image(i == 0 ? "SurveyLocation" : "MapLocation" + (i + 1), overlay.transform, location, 0, 0, 1, 1);
                    var rect = icon.rectTransform;
                    rect.anchorMin = rect.anchorMax = uv; rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new(overlay.rectTransform.rect.width / 24f, overlay.rectTransform.rect.height / 14f);
                    icon.enabled = true; overlay.locationIcons[i] = icon;
                }
                overlay.locationIcon = overlay.locationIcons[0];
                EditorUtility.SetDirty(overlay);
            }
            config.ApplyModifiedPropertiesWithoutUndo();
            var navigation = cabin.GetComponent<ZoneNavigation>();
            navigation.SetChart(BakeBoundaries(Texture("Environment/Zone1/mapline.png")), 960, 540);
            navigation.ResetVoyage();
            if (!navigation.CanOccupy(navigation.Position)) throw new InvalidOperationException("New chart obstructs the starting position.");
            EditorUtility.SetDirty(navigation); EditorUtility.SetDirty(cabin);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
        }

        public static void InstallBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity");
            Install();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Navigation artwork installed and saved.");
        }

        private static byte[] BakeBoundaries(Texture2D source)
        {
            // Preserve contour lines without classifying watercolor shading as obstacles.
            // The supplied line art describes boundaries, not filled terrain regions.
            var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                copy.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source)));
                var pixels = copy.GetPixels32(); var cells = new byte[960 * 540];
                for (int y = 0; y < 540; y++) for (int x = 0; x < 960; x++)
                {
                    bool blocked = false;
                    for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 2; dx++)
                    {
                        int sx = Mathf.Min(copy.width - 1, (x * 2 + dx) * copy.width / 1920);
                        int sy = Mathf.Min(copy.height - 1, (y * 2 + dy) * copy.height / 1080);
                        var p = pixels[sy * copy.width + sx];
                        blocked |= p.a > 100 && p.r + p.g + p.b < 500;
                    }
                    cells[y * 960 + x] = (byte)(blocked ? 0 : 1);
                }
                return cells;
            }
            finally { Object.DestroyImmediate(copy); }
        }
        private static Texture2D Texture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(Art + path)
            ?? throw new InvalidOperationException("Missing art: " + path);
        private static void Place(Transform target, float x, float y, float w, float h)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new(.5f, .5f);
            rect.sizeDelta = new(w, h); rect.anchoredPosition = new(x + w / 2, -y - h / 2);
            rect.localScale = Vector3.one;
        }
        private static RawImage Image(string name, Transform parent, Texture2D texture, float x, float y, float w, float h)
        {
            var child = parent.Find(name);
            if (child == null) { child = new GameObject(name, typeof(RectTransform), typeof(RawImage)).transform; child.SetParent(parent, false); }
            Place(child, x, y, w, h); var image = child.GetComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false; return image;
        }
        private static void Label(Transform parent, string name, string value, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Place(go.transform, x, y, w, h); var text = go.GetComponent<Text>();
            text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20; text.alignment = TextAnchor.MiddleCenter;
            text.color = new(.78f, .89f, .85f, 1); text.raycastTarget = false;
        }
    }
}

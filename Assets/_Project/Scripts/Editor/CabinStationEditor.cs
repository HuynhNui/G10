using System;
using System.Linq;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    [CustomEditor(typeof(CabinStationView))]
    public sealed class CabinStationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            foreach (string name in new[] { "cabinArt", "navigationArt", "chartArt", "radarArt" })
                EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
            serializedObject.ApplyModifiedProperties();
            var view = (CabinStationView)target;
            EditorGUILayout.HelpBox("Drag the four textures into these fields, then build. All panels and hotspots become editable scene objects.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying || view.transform.childCount > 0 ||
                !view.CabinArt || !view.NavigationArt || !view.ChartArt || !view.RadarArt))
                if (GUILayout.Button("Build Cabin In Zone 1", GUILayout.Height(32))) CabinStationBuilder.Build(view);
            if (view.transform.childCount > 0) EditorGUILayout.HelpBox("Cabin built. Edit panel artwork and hotspot RectTransforms in the Hierarchy. Save the scene with Ctrl+S.", MessageType.None);
            if (GUILayout.Button("Show all wiring")) DrawDefaultInspector();
        }
    }

    public static class CabinStationBuilder
    {
        private static readonly Color Ink = new(0.035f, 0.10f, 0.13f, 1f);
        private static readonly Color Glass = new(0.035f, 0.09f, 0.14f, 0.93f);
        private static readonly Color Mint = new(0.70f, 1f, 0.87f, 1f);

        [MenuItem("G10/Zone 1/Fit Cabin To Game View")]
        public static void FitExistingFrame()
        {
            CabinStationView view = Object.FindAnyObjectByType<CabinStationView>();
            if (view == null || EditorApplication.isPlaying) return;
            RectTransform frame = view.transform.Find("CabinCanvas/CabinFrame") as RectTransform;
            if (frame == null) return;
            Undo.RecordObject(frame, "Fit cabin frame");
            AspectRatioFitter oldFit = frame.GetComponent<AspectRatioFitter>();
            if (oldFit != null) Undo.DestroyObjectImmediate(oldFit);
            frame.anchorMin = frame.anchorMax = new(0.5f, 0.5f);
            frame.sizeDelta = new(1920, 1080); frame.anchoredPosition = Vector2.zero;
            if (frame.GetComponent<CabinCanvasFrame>() == null) Undo.AddComponent<CabinCanvasFrame>(frame.gameObject);
            RadarDisplay radar = frame.GetComponentInChildren<RadarDisplay>(true);
            if (radar != null && radar.GetComponent<CanvasRenderer>() == null) Undo.AddComponent<CanvasRenderer>(radar.gameObject);
            RectTransform hint = frame.Find("HoverHint") as RectTransform;
            if (hint != null) { Undo.RecordObject(hint, "Position hint"); hint.anchoredPosition = new(960, -112.5f); }
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }

        [MenuItem("G10/Zone 1/Create Cabin Setup")]
        public static void CreateSetup()
        {
            if (EditorApplication.isPlaying) return;
            Scene core = SceneManager.GetSceneByName("GameplayCore");
            if (!core.isLoaded) EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/GameplayCore.unity", OpenSceneMode.Additive);
            Scene scene = SceneManager.GetSceneByName("Zone01");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity", OpenSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);
            CabinStationView existing = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<CabinStationView>(true)).FirstOrDefault();
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
            GameObject root = new("Cabin Station");
            Undo.RegisterCreatedObjectUndo(root, "Create cabin setup");
            root.AddComponent<ZoneNavigation>();
            root.AddComponent<CabinStationView>();
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        public static void Build(CabinStationView view)
        {
            if (view.gameObject.scene.name != "Zone01" || view.transform.childCount != 0) throw new InvalidOperationException("Select an empty Zone01 cabin setup.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Zone 1 cabin");
            Undo.RegisterFullObjectHierarchyUndo(view.gameObject, "Build cabin");
            ZoneNavigation navigation = view.GetComponent<ZoneNavigation>();
            navigation.SetChart(BakeWater(view.ChartArt, 320, 180), 320, 180);
            Set(view, "navigation", navigation);
            Set(view, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
            GameObject canvasGO = new("CabinCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(view.transform, false);
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform backdrop = Box("Letterbox", canvasGO.transform, 0, 0, 1920, 1080);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color = Color.black;
            RectTransform frame = Box("CabinFrame", canvasGO.transform, 0, 0, 1920, 1080);
            frame.anchorMin = frame.anchorMax = new(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            frame.gameObject.AddComponent<CabinCanvasFrame>();
            Art("CabinArt", frame, view.CabinArt, 0, 0, 1920, 1080);
            Hotspot("Map", frame, view, "Bản đồ Miền Tảo Hát", 325, 205, 255, 280, view.OpenMap);
            Hotspot("Monitor", frame, view, "Màn hình quan sát", 650, 245, 545, 225, view.OpenCamera);
            Hotspot("Radar", frame, view, "Radar — xem địa hình quanh tàu", 1370, 245, 242, 225, view.OpenRadar);
            Hotspot("Navigation", frame, view, "Bàn điều khiển — lái tàu", 552, 495, 705, 137, view.OpenNavigation);
            Hotspot("Backpack", frame, view, "Balo", 195, 700, 325, 305, view.OpenCargo);
            Hotspot("Camera", frame, view, "Chụp hình", 1510, 505, 230, 115, view.OpenCamera);
            Hotspot("Capture", frame, view, "Thiết bị bắt sinh vật", 1250, 460, 215, 160, view.OpenCapture);

            RectTransform nav = Panel("NavigationPanel", frame, view.NavigationArt);
            Set(view, "navigationPanel", nav.gameObject);
            Set(view, "yReadout", Label("Y", nav, "100.0", 300, 155, 345, 125, 55, Ink));
            Set(view, "xReadout", Label("X", nav, "600.0", 300, 375, 345, 125, 55, Ink));
            Label("Depth", nav, "230 m", 265, 790, 255, 140, 45, Ink);
            Label("Zone", nav, "ALG / ZONE 1", 590, 795, 240, 95, 25, Ink);
            Set(view, "headingReadout", Label("Heading", nav, "000.0°", 1440, 525, 300, 80, 40, Ink));
            RectTransform compassCover = Box("CompassCenter", nav, 1460, 215, 220, 220);
            compassCover.gameObject.AddComponent<Image>().color = Color.white;
            RectTransform needlePivot = Box("HeadingPivot", nav, 1575, 325, 0, 0);
            RectTransform needle = Box("Needle", needlePivot, -3, -113, 6, 118);
            needle.gameObject.AddComponent<Image>().color = new(0.02f, 0.40f, 0.35f, 1f);
            Set(view, "compassNeedle", needlePivot);
            Hold("Forward", nav, view, "Giữ để tiến / W hoặc ↑", 860, 130, 295, 195, 1);
            Hold("Reverse", nav, view, "Giữ để lùi / S hoặc ↓", 860, 350, 295, 190, 2);
            Hold("TurnLeft", nav, view, "Giữ để xoay trái / A hoặc ←", 1380, 685, 155, 220, 3);
            Hold("TurnRight", nav, view, "Giữ để xoay phải / D hoặc →", 1560, 685, 190, 220, 4);
            RawImage mini = Art("MiniMap", nav, view.ChartArt, 875, 735, 360, 220);
            mini.raycastTarget = true;
            mini.gameObject.AddComponent<CabinPointerTarget>().Configure(view, "", 0, true);
            Hotspot("OpenMap", nav, view, "Mở bản đồ lớn", 875, 735, 360, 220, view.OpenMap);
            Bar("NavigationInfo", nav, 65, 990, 1380, 65);
            Set(view, "navigationStatus", Label("NavigationStatus", nav, "", 80, 995, 1350, 55, 27, Mint));
            Button("Brake", nav, "DỪNG", 1490, 990, 180, 65, view.Brake);
            Button("MapShortcut", nav, "BẢN ĐỒ", 40, 10, 210, 50, view.OpenMap);
            Button("RadarShortcut", nav, "RADAR", 270, 10, 180, 50, view.OpenRadar);
            Back(nav, view);

            RectTransform map = Panel("MapPanel", frame, view.ChartArt);
            Set(view, "mapPanel", map.gameObject);
            RawImage mapImage = map.GetComponent<RawImage>(); mapImage.raycastTarget = true;
            map.gameObject.AddComponent<CabinPointerTarget>().Configure(view, "", 0, true);
            Bar("ChartFooter", map, 270, 1000, 1330, 60);
            Set(view, "mapReadout", Label("ChartCoordinate", map, "Rê chuột trên bản đồ để đọc tọa độ", 280, 1003, 1310, 54, 27, Mint));
            Button("HelmShortcut", map, "BÀN LÁI", 40, 15, 210, 55, view.OpenNavigation);
            Back(map, view);

            RectTransform radar = Panel("RadarPanel", frame, view.RadarArt);
            Set(view, "radarPanel", radar.gameObject);
            RectTransform radarScope = Box("RadarScope", radar, 388, 324, 420, 404);
            RadarDisplay display = radarScope.gameObject.AddComponent<RadarDisplay>();
            display.Configure(navigation); Set(view, "radarDisplay", display);
            Hotspot("Scan", radar, view, "Quét địa hình quanh tàu", 1020, 324, 105, 125, view.Scan);
            Label("ScanLabel", radar, "QUÉT", 1025, 350, 95, 60, 28, Ink);
            Bar("RadarInfo", radar, 255, 925, 925, 125);
            Set(view, "radarStatus", Label("RadarStatus", radar, "", 280, 940, 875, 95, 27, Mint));
            Button("RadarHelm", radar, "BÀN LÁI", 35, 15, 210, 55, view.OpenNavigation);
            Back(radar, view);

            Set(view, "cameraPanel", Placeholder("CameraPanel", frame, "MÀN HÌNH QUAN SÁT", "Camera đã mở.\nChụp ảnh sẽ được bổ sung ở bước sau.", view).gameObject);
            Set(view, "cargoPanel", Placeholder("CargoPanel", frame, "BALO", "Kho đồ trống.\n4 ô chứa sẵn sàng cho bước tiếp theo.", view).gameObject);
            Set(view, "capturePanel", Placeholder("CapturePanel", frame, "THIẾT BỊ BẮT SINH VẬT", "Thiết bị đã mở.\nCơ chế bắt sẽ được bổ sung ở bước sau.", view).gameObject);

            RectTransform hint = Bar("HoverHint", frame, 430, 80, 1060, 65);
            Image hintImage = hint.GetComponent<Image>(); hintImage.raycastTarget = false;
            Set(view, "hoverLabel", Label("Hint", hint, "", 10, 0, 1040, 65, 27, Mint));
            hint.gameObject.SetActive(false);
            foreach (string field in new[] { "navigationPanel", "mapPanel", "radarPanel", "cameraPanel", "cargoPanel", "capturePanel" })
            {
                SerializedObject so = new(view);
                ((GameObject)so.FindProperty(field).objectReferenceValue).SetActive(false);
            }
            foreach (GameObject oldCanvas in view.gameObject.scene.GetRootGameObjects().Where(o => o.name == "ZoneCanvas"))
            { Undo.RecordObject(oldCanvas, "Hide placeholder zone UI"); oldCanvas.SetActive(false); }
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create cabin canvas");
            EditorUtility.SetDirty(view); EditorUtility.SetDirty(navigation);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Zone01 cabin built with assigned art, editable hotspots, chart, helm and radar. Save Zone01.");
        }

        private static byte[] BakeWater(Texture2D texture, int width, int height)
        {
            // Read a temporary GPU copy; source artwork/import settings remain untouched.
            RenderTexture previous = RenderTexture.active;
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D copy = new(width, height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(texture, rt); RenderTexture.active = rt;
                copy.ReadPixels(new Rect(0, 0, width, height), 0, 0); copy.Apply();
                Color32[] pixels = copy.GetPixels32();
                byte[] cells = new byte[pixels.Length];
                for (int i = 0; i < cells.Length; i++)
                    cells[i] = (byte)((pixels[i].r + pixels[i].g + pixels[i].b) > 360 ? 1 : 0);
                // Fill tiny ink marks, retaining larger rocks and cave walls.
                byte[] smooth = (byte[])cells.Clone();
                for (int y = 1; y < height - 1; y++)
                for (int x = 1; x < width - 1; x++)
                {
                    int count = 0;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) count += cells[(y + dy) * width + x + dx];
                    smooth[y * width + x] = (byte)(count >= 5 ? 1 : 0);
                }
                return smooth;
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(copy); }
        }

        private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject go = new(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            RectTransform r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new(0.5f, 0.5f);
            r.sizeDelta = new(width, height); r.anchoredPosition = new(x + width / 2, -y - height / 2);
            return r;
        }
        private static RawImage Art(string name, Transform parent, Texture2D texture, float x, float y, float w, float h)
        { RawImage image = Box(name, parent, x, y, w, h).gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false; return image; }
        private static RectTransform Panel(string name, Transform parent, Texture2D texture)
        { RawImage image = Art(name, parent, texture, 0, 0, 1920, 1080); image.raycastTarget = true; return image.rectTransform; }
        private static RectTransform Bar(string name, Transform parent, float x, float y, float w, float h)
        { RectTransform rect = Box(name, parent, x, y, w, h); rect.gameObject.AddComponent<Image>().color = Glass; return rect; }
        private static Font GetThemeFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Bold.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        private static Sprite GetThemeBorder(string name = "panel-001.png") =>
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/Borders/" + name);

        private static Text Label(string name, Transform parent, string text, float x, float y, float w, float h, int size, Color color)
        {
            Text label = Box(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = GetThemeFont(); label.fontSize = size;
            label.text = text; label.color = color; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        private static UnityEngine.UI.Button Hotspot(string name, Transform parent, CabinStationView view, string hint, float x, float y, float w, float h, UnityAction action)
        {
            RectTransform rect = Box(name + "Hotspot", parent, x, y, w, h);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = Color.white;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors; colors.normalColor = new(1, 1, 1, 0); colors.highlightedColor = new(0.65f, 1, 0.86f, 0.19f);
            colors.pressedColor = new(0.45f, 1, 0.8f, 0.35f); colors.selectedColor = colors.normalColor; colors.fadeDuration = 0.08f; button.colors = colors;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
            rect.gameObject.AddComponent<CabinPointerTarget>().Configure(view, hint);
            return button;
        }
        private static void Hold(string name, Transform parent, CabinStationView view, string hint, float x, float y, float w, float h, int command)
        { var b = Hotspot(name, parent, view, hint, x, y, w, h, null); b.GetComponent<CabinPointerTarget>().Configure(view, hint, command); }
        private static void Button(string name, Transform parent, string text, float x, float y, float w, float h, UnityAction action)
        {
            RectTransform r = Bar(name, parent, x, y, w, h);
            var image = r.GetComponent<Image>();
            Sprite border = GetThemeBorder("panel-001.png");
            if (border != null) { image.sprite = border; image.type = Image.Type.Sliced; }
            image.color = new Color(0.04f, 0.14f, 0.20f, 0.95f);
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = image;
            ColorBlock colors = b.colors;
            colors.normalColor = new Color(0.04f, 0.14f, 0.20f, 0.95f);
            colors.highlightedColor = new Color(0.15f, 0.45f, 0.55f, 1f);
            colors.pressedColor = new Color(0.02f, 0.10f, 0.14f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            b.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(b.onClick, action);
            var l = Label("Label", r, text, 0, 0, w, h, (int)Mathf.Clamp(h * 0.42f, 18, 28), Mint);
            l.color = new Color(0.92f, 0.96f, 0.98f, 1f);
        }
        private static void Back(Transform parent, CabinStationView view) => Button("BackToCabin", parent, "CABIN / ESC", 1660, 15, 235, 55, view.ClosePanel);
        private static RectTransform Placeholder(string name, Transform parent, string title, string message, CabinStationView view)
        {
            RectTransform p = Bar(name, parent, 0, 0, 1920, 1080);
            Label("Title", p, title, 360, 290, 1200, 100, 44, Mint);
            Label("Message", p, message, 360, 405, 1200, 250, 30, Color.white); Back(p, view); return p;
        }
        private static void Set(Object target, string field, Object value)
        { SerializedObject so = new(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}

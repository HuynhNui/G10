using System;
using System.IO;
using System.Linq;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    [InitializeOnLoad]
    public static class UIThemeStylerEditor
    {
        private const string FontFolder = "Assets/_Project/Art/UI/Fonts";
        private const string BorderFolder = "Assets/_Project/Art/UI/Borders";
        private const string BgFolder = "Assets/_Project/Art/UI/Backgrounds";
        private const string VersionKey = "G10_UI_VisualUpgrade_Applied_v1";

        private static readonly Color DeepNavy = new(0.04f, 0.14f, 0.20f, 0.95f);
        private static readonly Color SeafoamGlow = new(0.15f, 0.45f, 0.55f, 1f);
        private static readonly Color MintText = new(0.72f, 1f, 0.88f, 1f);
        private static readonly Color PearlText = new(0.92f, 0.96f, 0.98f, 1f);
        private static readonly Color BrassText = new(0.95f, 0.82f, 0.55f, 1f);

        static UIThemeStylerEditor()
        {
            EditorApplication.delayCall += AutoApplyOnce;
        }

        private static void AutoApplyOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!SessionState.GetBool(VersionKey, false))
            {
                SessionState.SetBool(VersionKey, true);
                ApplyVisualUpgrade();
            }
        }

        [MenuItem("G10/UI/Apply Full Visual Upgrade")]
        public static void ApplyVisualUpgrade()
        {
            Debug.Log("[UIThemeStyler] Starting Full Visual Upgrade...");

            // 1. Refresh to register all new files in AssetDatabase
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 2. Configure Texture Importers
            ConfigureAllTextureImporters();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 3. Upgrade Scenes
            UpgradeMainMenu();
            UpgradeEnding();
            UpgradeZones();
            UpgradeZone01Cabin();
            InstallPauseMenu();

            AssetDatabase.SaveAssets();
            Debug.Log("[UIThemeStyler] FULL VISUAL UPGRADE COMPLETED SUCCESSFULLY!");
        }

        [MenuItem("G10/UI/Configure Texture Importers")]
        public static void ConfigureAllTextureImporters()
        {
            // Configure borders
            if (Directory.Exists(BorderFolder))
            {
                foreach (string file in Directory.GetFiles(BorderFolder, "*.png"))
                {
                    string assetPath = file.Replace('\\', '/');
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null) continue;

                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }

                    if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                    if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                    if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }

                    bool isDefault = Path.GetFileName(file).StartsWith("default-");
                    Vector4 border = isDefault ? new Vector4(14, 14, 14, 14) : new Vector4(24, 24, 24, 24);
                    if (importer.spriteBorder != border)
                    {
                        importer.spriteBorder = border;
                        changed = true;
                    }

                    if (changed) importer.SaveAndReimport();
                }
            }

            // Configure backgrounds
            if (Directory.Exists(BgFolder))
            {
                foreach (string file in Directory.GetFiles(BgFolder, "*.png"))
                {
                    string assetPath = file.Replace('\\', '/');
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null) continue;

                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }

                    if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                    if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                    if (importer.maxTextureSize != 2048) { importer.maxTextureSize = 2048; changed = true; }

                    if (changed) importer.SaveAndReimport();
                }
            }

            Debug.Log("[UIThemeStyler] Texture importers configured.");
        }

        public static Font GetBoldFont() =>
            AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/AlegreyaSansSC-Bold.ttf")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Font GetRegularFont() =>
            AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/AlegreyaSansSC-Regular.ttf")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Font GetMediumFont() =>
            AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/AlegreyaSansSC-Medium.ttf")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Sprite GetBorder(string name = "panel-001.png") =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BorderFolder}/{name}");

        public static Sprite GetBackground(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BgFolder}/{name}");

        private static void UpgradeMainMenu()
        {
            string path = "Assets/_Project/Scenes/MainMenu/MainMenu.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            Canvas canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();
            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                // Background
                Transform bgTr = canvas.transform.Find("Background");
                if (bgTr != null)
                {
                    var img = bgTr.GetComponent<Image>();
                    Sprite bgSprite = GetBackground("MainMenu_Background.png");
                    if (bgSprite != null) img.sprite = bgSprite;
                    img.color = Color.white;
                    img.type = Image.Type.Simple;
                }

                // Veil
                Transform veilTr = canvas.transform.Find("Veil");
                if (veilTr == null)
                {
                    GameObject veil = new("Veil", typeof(RectTransform), typeof(Image));
                    veil.transform.SetParent(canvas.transform, false);
                    veil.transform.SetSiblingIndex(1);
                    var r = (RectTransform)veil.transform;
                    r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                    r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
                    var vImg = veil.GetComponent<Image>();
                    vImg.color = new Color(0.015f, 0.04f, 0.07f, 0.25f);
                    vImg.raycastTarget = false;
                }

                // Title
                Transform titleTr = canvas.transform.Find("Title");
                if (titleTr != null)
                {
                    var t = titleTr.GetComponent<Text>();
                    t.font = GetBoldFont();
                    t.fontSize = 84;
                    t.text = "PELAGIC : G10";
                    t.color = PearlText;
                    t.alignment = TextAnchor.MiddleCenter;
                    var rt = (RectTransform)titleTr;
                    rt.sizeDelta = new Vector2(900, 100);
                    rt.anchoredPosition = new Vector2(0, 160);

                    var outline = titleTr.GetComponent<Outline>() ?? titleTr.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.01f, 0.04f, 0.08f, 0.9f);
                    outline.effectDistance = new Vector2(2, -2);
                }

                // Subtitle
                Transform subTr = canvas.transform.Find("Subtitle");
                if (subTr != null)
                {
                    var t = subTr.GetComponent<Text>();
                    t.font = GetMediumFont();
                    t.fontSize = 26;
                    t.text = "POINT & CLICK SUBMARINE EXPLORATION";
                    t.color = MintText;
                    t.alignment = TextAnchor.MiddleCenter;
                    var rt = (RectTransform)subTr;
                    rt.sizeDelta = new Vector2(800, 45);
                    rt.anchoredPosition = new Vector2(0, 95);
                }

                // Buttons
                StyleButton(canvas.transform.Find("StartButton"), "START VOYAGE", 340, 64, new Vector2(0, -20), GetBorder("panel-001.png"));
                StyleButton(canvas.transform.Find("QuitButton"), "QUIT EXPEDITION", 340, 64, new Vector2(0, -100), GetBorder("panel-001.png"));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[UIThemeStyler] MainMenu upgraded.");
        }

        private static void UpgradeEnding()
        {
            string path = "Assets/_Project/Scenes/Ending/Ending.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            Canvas canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();
            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                Transform bgTr = canvas.transform.Find("Background");
                if (bgTr != null)
                {
                    var img = bgTr.GetComponent<Image>();
                    Sprite bgSprite = GetBackground("Ending_Background.png");
                    if (bgSprite != null) img.sprite = bgSprite;
                    img.color = Color.white;
                }

                Transform titleTr = canvas.transform.Find("Title");
                if (titleTr != null)
                {
                    var t = titleTr.GetComponent<Text>();
                    t.font = GetBoldFont();
                    t.fontSize = 68;
                    t.text = "EXPEDITION COMPLETE";
                    t.color = PearlText;
                    var rt = (RectTransform)titleTr;
                    rt.anchoredPosition = new Vector2(0, 90);
                    rt.sizeDelta = new Vector2(1000, 90);

                    var outline = titleTr.GetComponent<Outline>() ?? titleTr.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.01f, 0.04f, 0.08f, 0.9f);
                    outline.effectDistance = new Vector2(2, -2);
                }

                StyleButton(canvas.transform.Find("MainMenuButton"), "RETURN TO MENU", 360, 64, new Vector2(0, -40), GetBorder("panel-001.png"));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[UIThemeStyler] Ending upgraded.");
        }

        private static void UpgradeZones()
        {
            string[] zones = { "Zone02", "Zone03", "Zone04" };
            string[] bgs = { "Zone02_Background.png", "Zone03_Background.png", "Zone04_Background.png" };

            for (int i = 0; i < zones.Length; i++)
            {
                string path = $"Assets/_Project/Scenes/Gameplay/{zones[i]}.unity";
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasLoaded = scene.isLoaded;
                if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                Canvas canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();
                if (canvas != null)
                {
                    var scaler = canvas.GetComponent<CanvasScaler>();
                    if (scaler != null)
                    {
                        scaler.referenceResolution = new Vector2(1920, 1080);
                        scaler.matchWidthOrHeight = 0.5f;
                    }

                    Transform bgTr = canvas.transform.Find("Background");
                    if (bgTr == null)
                    {
                        GameObject bgGO = new("Background", typeof(RectTransform), typeof(Image));
                        bgGO.transform.SetParent(canvas.transform, false);
                        bgGO.transform.SetAsFirstSibling();
                        bgTr = bgGO.transform;
                        var r = (RectTransform)bgTr;
                        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
                    }

                    var img = bgTr.GetComponent<Image>();
                    Sprite s = GetBackground(bgs[i]);
                    if (s != null) img.sprite = s;
                    img.color = Color.white;
                    img.raycastTarget = false;

                    Transform labelTr = canvas.transform.Find("ZoneLabel");
                    if (labelTr != null)
                    {
                        var t = labelTr.GetComponent<Text>();
                        t.font = GetBoldFont();
                        t.fontSize = 38;
                        t.color = MintText;
                        var rt = (RectTransform)labelTr;
                        rt.anchoredPosition = new Vector2(0, 420);
                        var outline = labelTr.GetComponent<Outline>() ?? labelTr.gameObject.AddComponent<Outline>();
                        outline.effectColor = new Color(0, 0, 0, 0.8f);
                        outline.effectDistance = new Vector2(1, -1);
                    }

                    StyleButton(canvas.transform.Find("PreviousZoneButton"), "< PREVIOUS", 260, 56, new Vector2(-680, -420), GetBorder("panel-001.png"));
                    StyleButton(canvas.transform.Find("NextZoneButton"), "NEXT >", 260, 56, new Vector2(680, -420), GetBorder("panel-001.png"));
                    StyleButton(canvas.transform.Find("EndingButton"), "FINISH >", 260, 56, new Vector2(680, -420), GetBorder("panel-001.png"));
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
                Debug.Log($"[UIThemeStyler] {zones[i]} upgraded.");
            }
        }

        private static void UpgradeZone01Cabin()
        {
            string path = "Assets/_Project/Scenes/Gameplay/Zone01.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            var cabin = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CabinStationView>(true)).FirstOrDefault();
            if (cabin != null)
            {
                Transform frame = cabin.transform.Find("CabinCanvas/CabinFrame");
                if (frame != null)
                {
                    // 1. CARGO PANEL (BALO)
                    Transform cargo = frame.Find("CargoPanel");
                    if (cargo != null)
                    {
                        UpgradeCargoPanel(cargo);
                    }

                    // 2. NAVIGATION PANEL BUTTONS
                    Transform nav = frame.Find("NavigationPanel");
                    if (nav != null)
                    {
                        StyleButton(nav.Find("Brake"), "DỪNG", 180, 60, GetBorder("panel-001.png"), new Color(0.45f, 0.12f, 0.12f, 0.95f), new Color(0.7f, 0.2f, 0.2f, 1f));
                        StyleButton(nav.Find("MapShortcut"), "BẢN ĐỒ", 200, 50, GetBorder("panel-001.png"));
                        StyleButton(nav.Find("RadarShortcut"), "RADAR", 180, 50, GetBorder("panel-001.png"));
                        StyleButton(nav.Find("BackToCabin"), "CABIN / ESC", 220, 52, GetBorder("panel-001.png"));

                        foreach (var text in nav.GetComponentsInChildren<Text>(true))
                        {
                            if (text.name.Contains("Readout") || text.name == "X" || text.name == "Y" || text.name == "Heading" || text.name == "Depth" || text.name == "Zone")
                                text.font = GetBoldFont();
                        }
                    }

                    // 3. MAP PANEL BUTTONS
                    Transform map = frame.Find("MapPanel");
                    if (map != null)
                    {
                        StyleButton(map.Find("WorldMap"), "MAP TỔNG", 180, 52, GetBorder("panel-001.png"));
                        StyleButton(map.Find("HelmShortcut"), "BÀN LÁI", 200, 52, GetBorder("panel-001.png"));
                        StyleButton(map.Find("BackToCabin"), "MAP TỔNG / ESC", 240, 52, GetBorder("panel-001.png"));
                    }

                    // 4. RADAR PANEL BUTTONS
                    Transform radar = frame.Find("RadarPanel");
                    if (radar != null)
                    {
                        StyleButton(radar.Find("RadarHelm"), "BÀN LÁI", 200, 52, GetBorder("panel-001.png"));
                        StyleButton(radar.Find("BackToCabin"), "CABIN / ESC", 220, 52, GetBorder("panel-001.png"));
                    }

                    // 5. CAPTURE PANEL BUTTONS
                    Transform capture = frame.Find("CapturePanel");
                    if (capture != null)
                    {
                        StyleButton(capture.Find("CatchingControls/Catch"), "BẮT", 340, 75, GetBorder("panel-001.png"));
                        StyleButton(capture.Find("CatchingControls/Inventory"), "MỞ BALÔ", 340, 75, GetBorder("panel-001.png"));
                        StyleButton(capture.Find("BackToCabin"), "CABIN / ESC", 220, 52, GetBorder("panel-001.png"));
                    }

                    // 6. WORLD MAP BUTTONS
                    Transform world = frame.Find("WorldMapPanel");
                    if (world != null)
                    {
                        StyleButton(world.Find("Resume"), "MỞ LẠI KHU VỰC", 320, 55, GetBorder("panel-001.png"));
                        StyleButton(world.Find("Back"), "CABIN / ESC", 240, 55, GetBorder("panel-001.png"));
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[UIThemeStyler] Zone01 Cabin UI upgraded.");
        }

        private static void UpgradeCargoPanel(Transform cargo)
        {
            var baseImage = cargo.GetComponent<Image>();
            if (baseImage != null)
            {
                baseImage.color = new Color(0.015f, 0.035f, 0.06f, 0.85f);
            }

            // Specimen chest frame
            Transform chestTr = cargo.Find("SpecimenChest");
            if (chestTr == null)
            {
                GameObject chest = new("SpecimenChest", typeof(RectTransform), typeof(Image));
                chest.transform.SetParent(cargo, false);
                chest.transform.SetAsFirstSibling();
                chestTr = chest.transform;
                var r = (RectTransform)chestTr;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(1660, 840);
                r.anchoredPosition = new Vector2(0, -20);
            }

            var chestImg = chestTr.GetComponent<Image>();
            chestImg.sprite = GetBorder("panel-025.png") ?? GetBorder("panel-001.png");
            chestImg.type = Image.Type.Sliced;
            chestImg.color = new Color(0.03f, 0.09f, 0.13f, 0.96f);

            // Title
            Transform title = cargo.Find("Title");
            if (title != null)
            {
                var t = title.GetComponent<Text>();
                t.font = GetBoldFont();
                t.fontSize = 42;
                t.color = BrassText;
                t.text = "BALÔ — KHOANG MẪU VẬT THỦY SINH";
                var rt = (RectTransform)title;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 340);
                rt.sizeDelta = new Vector2(1200, 60);

                var outline = title.GetComponent<Outline>() ?? title.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.8f);
                outline.effectDistance = new Vector2(1, -1);
            }

            // Inventory Summary
            Transform summary = cargo.Find("InventorySummary");
            if (summary != null)
            {
                var t = summary.GetComponent<Text>();
                t.font = GetMediumFont();
                t.fontSize = 26;
                t.color = MintText;
                var rt = (RectTransform)summary;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 275);
                rt.sizeDelta = new Vector2(1200, 45);
            }

            // 4 Slots
            for (int i = 1; i <= 4; i++)
            {
                Transform slot = cargo.Find("ItemSlot" + i);
                if (slot != null)
                {
                    var slotImg = slot.GetComponent<Image>();
                    if (slotImg != null)
                    {
                        slotImg.sprite = GetBorder("panel-002.png") ?? GetBorder("panel-001.png");
                        slotImg.type = Image.Type.Sliced;
                        slotImg.color = new Color(0.05f, 0.16f, 0.22f, 0.98f);
                    }

                    var slotRect = (RectTransform)slot;
                    slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                    slotRect.sizeDelta = new Vector2(340, 460);
                    float xPos = -540 + (i - 1) * 360;
                    slotRect.anchoredPosition = new Vector2(xPos, -20);

                    Transform nameTr = slot.Find("Name");
                    if (nameTr != null)
                    {
                        var t = nameTr.GetComponent<Text>();
                        t.font = GetBoldFont();
                        t.fontSize = 24;
                        t.color = PearlText;
                        var nRect = (RectTransform)nameTr;
                        nRect.anchorMin = new Vector2(0, 0);
                        nRect.anchorMax = new Vector2(1, 0);
                        nRect.sizeDelta = new Vector2(-20, 50);
                        nRect.anchoredPosition = new Vector2(0, 35);
                    }

                    Transform iconFrame = slot.Find("IconFrame");
                    if (iconFrame != null)
                    {
                        var ifRect = (RectTransform)iconFrame;
                        ifRect.anchorMin = ifRect.anchorMax = new Vector2(0.5f, 0.5f);
                        ifRect.sizeDelta = new Vector2(280, 280);
                        ifRect.anchoredPosition = new Vector2(0, 45);

                        Transform itemIcon = iconFrame.Find("ItemIcon");
                        if (itemIcon != null)
                        {
                            var iiRect = (RectTransform)itemIcon;
                            iiRect.anchorMin = Vector2.zero;
                            iiRect.anchorMax = Vector2.one;
                            iiRect.offsetMin = iiRect.offsetMax = Vector2.zero;
                        }
                    }
                }
            }

            // Buttons
            StyleButton(cargo.Find("BackToRadar"), "VỀ RADAR", 320, 62, GetBorder("panel-001.png"));
            Transform backRadar = cargo.Find("BackToRadar");
            if (backRadar != null)
            {
                var rt = (RectTransform)backRadar;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -360);
            }

            StyleButton(cargo.Find("BackToCabin"), "CABIN / ESC", 220, 52, GetBorder("panel-001.png"));
        }

        private static void StyleButton(Transform btnTr, string label, float width, float height, Sprite borderSprite = null, Color? customNormal = null, Color? customHover = null)
        {
            if (btnTr == null) return;
            StyleButton(btnTr, label, width, height, (btnTr as RectTransform)?.anchoredPosition ?? Vector2.zero, borderSprite, customNormal, customHover);
        }

        private static void StyleButton(Transform btnTr, string label, float width, float height, Vector2 anchoredPos, Sprite borderSprite = null, Color? customNormal = null, Color? customHover = null)
        {
            if (btnTr == null) return;

            var rect = btnTr as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = anchoredPos;
            }

            var img = btnTr.GetComponent<Image>();
            if (img == null) img = btnTr.gameObject.AddComponent<Image>();

            Sprite border = borderSprite ?? GetBorder("panel-001.png");
            if (border != null)
            {
                img.sprite = border;
                img.type = Image.Type.Sliced;
            }

            Color normalCol = customNormal ?? DeepNavy;
            Color hoverCol = customHover ?? SeafoamGlow;
            Color pressedCol = new(normalCol.r * 0.7f, normalCol.g * 0.7f, normalCol.b * 0.7f, 1f);

            img.color = normalCol;

            var btn = btnTr.GetComponent<Button>();
            if (btn != null)
            {
                btn.targetGraphic = img;
                ColorBlock colors = btn.colors;
                colors.normalColor = normalCol;
                colors.highlightedColor = hoverCol;
                colors.pressedColor = pressedCol;
                colors.selectedColor = normalCol;
                colors.fadeDuration = 0.08f;
                btn.colors = colors;
            }

            Transform labelTr = btnTr.Find("Label");
            if (labelTr != null)
            {
                var text = labelTr.GetComponent<Text>();
                if (text != null)
                {
                    text.font = GetBoldFont();
                    text.fontSize = (int)Mathf.Clamp(height * 0.42f, 18, 30);
                    text.color = PearlText;
                    text.alignment = TextAnchor.MiddleCenter;
                    if (!string.IsNullOrEmpty(label)) text.text = label;

                    var outline = labelTr.GetComponent<Outline>() ?? labelTr.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0, 0, 0, 0.75f);
                    outline.effectDistance = new Vector2(1, -1);
                }
            }
        }

        [MenuItem("G10/UI/Install Pause Menu")]
        public static void InstallPauseMenu()
        {
            UpgradeGameplayCorePause();
            AddPauseButtonToCabin();
            AssetDatabase.SaveAssets();
            Debug.Log("[UIThemeStyler] Pause Menu installed successfully!");
        }

        private static void UpgradeGameplayCorePause()
        {
            string path = "Assets/_Project/Scenes/Gameplay/GameplayCore.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            Canvas canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault(c => c.name == "GameplayCanvas");
            if (canvas != null)
            {
                canvas.sortingOrder = 100;

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                Transform pauseMenuTr = canvas.transform.Find("PauseMenu");
                if (pauseMenuTr == null)
                {
                    GameObject pauseMenuGO = new("PauseMenu", typeof(RectTransform));
                    pauseMenuGO.transform.SetParent(canvas.transform, false);
                    pauseMenuTr = pauseMenuGO.transform;
                    var rt = (RectTransform)pauseMenuTr;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }

                Transform dimTr = pauseMenuTr.Find("DimOverlay");
                if (dimTr == null)
                {
                    GameObject dimGO = new("DimOverlay", typeof(RectTransform), typeof(Image));
                    dimGO.transform.SetParent(pauseMenuTr, false);
                    dimGO.transform.SetAsFirstSibling();
                    dimTr = dimGO.transform;
                    var r = (RectTransform)dimTr;
                    r.anchorMin = Vector2.zero;
                    r.anchorMax = Vector2.one;
                    r.offsetMin = Vector2.zero;
                    r.offsetMax = Vector2.zero;
                }
                var dimImg = dimTr.GetComponent<Image>();
                dimImg.color = new Color(0.01f, 0.03f, 0.06f, 0.85f);
                dimImg.raycastTarget = true;

                Transform dialogTr = pauseMenuTr.Find("DialogBox");
                if (dialogTr == null)
                {
                    GameObject dialogGO = new("DialogBox", typeof(RectTransform), typeof(Image));
                    dialogGO.transform.SetParent(pauseMenuTr, false);
                    dialogTr = dialogGO.transform;
                    var r = (RectTransform)dialogTr;
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.sizeDelta = new Vector2(580, 560);
                    r.anchoredPosition = Vector2.zero;
                }
                var dialogImg = dialogTr.GetComponent<Image>();
                dialogImg.sprite = GetBorder("panel-001.png");
                dialogImg.type = Image.Type.Sliced;
                dialogImg.color = new Color(0.03f, 0.09f, 0.14f, 0.98f);

                Transform titleTr = dialogTr.Find("Title");
                if (titleTr == null)
                {
                    GameObject titleGO = new("Title", typeof(RectTransform), typeof(Text));
                    titleGO.transform.SetParent(dialogTr, false);
                    titleTr = titleGO.transform;
                    var r = (RectTransform)titleTr;
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.sizeDelta = new Vector2(500, 60);
                    r.anchoredPosition = new Vector2(0, 205);
                }
                var titleText = titleTr.GetComponent<Text>();
                titleText.font = GetBoldFont();
                titleText.fontSize = 44;
                titleText.color = PearlText;
                titleText.text = "TẠM DỪNG";
                titleText.alignment = TextAnchor.MiddleCenter;
                var titleOutline = titleTr.GetComponent<Outline>() ?? titleTr.gameObject.AddComponent<Outline>();
                titleOutline.effectColor = new Color(0.01f, 0.04f, 0.08f, 0.9f);
                titleOutline.effectDistance = new Vector2(2, -2);

                Transform subTr = dialogTr.Find("Subtitle");
                if (subTr == null)
                {
                    GameObject subGO = new("Subtitle", typeof(RectTransform), typeof(Text));
                    subGO.transform.SetParent(dialogTr, false);
                    subTr = subGO.transform;
                    var r = (RectTransform)subTr;
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.sizeDelta = new Vector2(500, 35);
                    r.anchoredPosition = new Vector2(0, 155);
                }
                var subText = subTr.GetComponent<Text>();
                subText.font = GetBoldFont();
                subText.fontSize = 20;
                subText.color = MintText;
                subText.text = "CHUYẾN THÁM HIỂM ĐANG TẠM DỪNG";
                subText.alignment = TextAnchor.MiddleCenter;

                Transform resumeTr = CreateOrFindButton(dialogTr, "ResumeButton", "TIẾP TỤC", 380, 58, new Vector2(0, 65));
                Transform restartTr = CreateOrFindButton(dialogTr, "RestartButton", "CHƠI LẠI KHU VỰC", 380, 58, new Vector2(0, -10));
                Transform mainMenuTr = CreateOrFindButton(dialogTr, "MainMenuButton", "VỀ MENU CHÍNH", 380, 58, new Vector2(0, -85));
                Transform quitTr = CreateOrFindButton(dialogTr, "QuitButton", "THOÁT GAME", 380, 58, new Vector2(0, -160));

                var controller = pauseMenuTr.GetComponent<PauseMenuController>() ?? pauseMenuTr.gameObject.AddComponent<PauseMenuController>();
                SerializedObject so = new(controller);
                so.FindProperty("pausePanel").objectReferenceValue = pauseMenuTr.gameObject;
                so.FindProperty("resumeButton").objectReferenceValue = resumeTr.GetComponent<Button>();
                so.FindProperty("restartButton").objectReferenceValue = restartTr.GetComponent<Button>();
                so.FindProperty("mainMenuButton").objectReferenceValue = mainMenuTr.GetComponent<Button>();
                so.FindProperty("quitButton").objectReferenceValue = quitTr.GetComponent<Button>();
                so.ApplyModifiedProperties();

                pauseMenuTr.gameObject.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[UIThemeStyler] GameplayCore Pause Menu upgraded.");
        }

        private static Transform CreateOrFindButton(Transform parent, string name, string label, float width, float height, Vector2 pos)
        {
            Transform btnTr = parent.Find(name);
            if (btnTr == null)
            {
                GameObject btnGO = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
                btnGO.transform.SetParent(parent, false);
                btnTr = btnGO.transform;

                GameObject lblGO = new("Label", typeof(RectTransform), typeof(Text));
                lblGO.transform.SetParent(btnTr, false);
                var lrt = (RectTransform)lblGO.transform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            StyleButton(btnTr, label, width, height, pos, GetBorder("panel-001.png"));
            return btnTr;
        }

        private static void AddPauseButtonToCabin()
        {
            string path = "Assets/_Project/Scenes/Gameplay/Zone01.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            var cabin = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CabinStationView>(true)).FirstOrDefault();
            if (cabin != null)
            {
                Transform frame = cabin.transform.Find("CabinCanvas/CabinFrame");
                if (frame != null)
                {
                    Transform pauseBtn = frame.Find("PauseButton");
                    if (pauseBtn == null)
                    {
                        GameObject btnGO = new("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                        btnGO.transform.SetParent(frame, false);
                        pauseBtn = btnGO.transform;

                        GameObject lblGO = new("Label", typeof(RectTransform), typeof(Text));
                        lblGO.transform.SetParent(pauseBtn, false);
                        var lrt = (RectTransform)lblGO.transform;
                        lrt.anchorMin = Vector2.zero;
                        lrt.anchorMax = Vector2.one;
                        lrt.offsetMin = Vector2.zero;
                        lrt.offsetMax = Vector2.zero;

                        var rt = (RectTransform)pauseBtn;
                        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                        rt.pivot = new Vector2(1, 1);
                        rt.anchoredPosition = new Vector2(-30, -30);
                        rt.sizeDelta = new Vector2(150, 48);

                        var btn = btnGO.GetComponent<Button>();
                        UnityEventTools.AddPersistentListener(btn.onClick, cabin.OpenPause);
                    }

                    StyleButton(pauseBtn, "|| TẠM DỪNG", 150, 48, (pauseBtn as RectTransform).anchoredPosition, GetBorder("panel-001.png"));
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[UIThemeStyler] Cabin PauseButton added.");
        }
    }
}

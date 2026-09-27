using System;
using System.IO;
using G10.Prototype.Dialogue;
using G10.Prototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>Adds a dormant dialogue overlay to GameplayCore without rebuilding any existing UI.</summary>
    public static class DialogueUIEditor
    {
        private const string Art = "Assets/_Project/Art/UI/Dialouge/";
        public const string FontPath = "Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset";
        private static readonly Color Ink = new(.14f, .24f, .46f);

        [MenuItem("G10/Dialogue/Install Dialogue Framework")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var panels = Object.FindAnyObjectByType<UIManager>();
            if (panels == null || panels.gameObject.scene.name != "GameplayCore")
                throw new InvalidOperationException("Load GameplayCore before installing dialogue.");
            var controller = panels.GetComponent<DialogueController>();
            if (controller != null && controller.View != null)
            {
                if (!controller.View.IsConfigured) throw new InvalidOperationException("Existing dialogue references are incomplete; inspect the DialogueRoot before reinstalling.");
                Debug.Log("Dialogue framework is already installed.", controller);
                return;
            }
            if (panels.transform.Find("DialogueRoot") != null)
                throw new InvalidOperationException("DialogueRoot already exists without a configured controller; inspect it before installing.");
            TMP_FontAsset font = EnsureFont();
            Undo.RegisterFullObjectHierarchyUndo(panels.gameObject, "Install dialogue framework");
            if (controller == null) controller = Undo.AddComponent<DialogueController>(panels.gameObject);

            var root = Rect(panels.transform, "DialogueRoot", 0, 0, 1920, 1080);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            root.gameObject.AddComponent<GraphicRaycaster>();
            root.gameObject.AddComponent<Image>().color = Color.clear;
            var view = root.gameObject.AddComponent<DialogueView>();

            var panel = Rect(root, "PanelBackground", 0, 0, 1480, 445);
            panel.anchorMin = panel.anchorMax = panel.pivot = new(.5f, 0);
            panel.anchoredPosition = new(0, 38);
            AddArt(panel, "DialoguePanel_Base");
            var tab = Rect(panel, "SpeakerTab", 115, -42, 380, 85);
            var tabImage = AddArt(tab, "Tab_Story");
            var speaker = Label(tab, "SpeakerText", string.Empty, 124, 8, 234, 67, 29, font);
            speaker.alignment = TextAlignmentOptions.MidlineLeft;
            speaker.enableAutoSizing = true; speaker.fontSizeMin = 18; speaker.fontSizeMax = 29;
            ScrollRect bodyScroll = Scroll(panel, "DialogueText", 205, 95, 1070, 220, 33, font, out TMP_Text body);
            var controls = Rect(panel, "Controls", 565, 335, 715, 76);
            Button log = ArtButton(controls, "LogButton", "Button_Log", 0, 5, 175, 66);
            Button auto = ArtButton(controls, "AutoButton", "Button_Auto", 192, 5, 175, 66);
            Button skip = ArtButton(controls, "SkipButton", "Button_Skip", 384, 5, 175, 66);
            Button next = ArtButton(controls, "NextButton", "Button_Next", 606, 0, 76, 76);
            ConfigureNavigation(new[] { log, auto, skip, next });

            var logPanel = Rect(root, "DialogueLog", 200, 80, 1520, 880);
            var logBackground = logPanel.gameObject.AddComponent<Image>();
            logBackground.color = new(.90f, .96f, 1, .99f);
            var outline = logPanel.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink; outline.effectDistance = new(2, -2);
            Label(logPanel, "Title", "NHẬT KÝ HỘI THOẠI", 70, 35, 1140, 65, 35, font);
            var close = Rect(logPanel, "CloseLogButton", 1280, 38, 165, 64);
            AddArt(close, "Button_Log");
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close.GetComponent<RawImage>();
            closeButton.targetGraphic.raycastTarget = true;
            closeButton.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            close.gameObject.AddComponent<DialogueButtonFeedback>();
            ScrollRect logScroll = Scroll(logPanel, "History", 70, 130, 1380, 670, 31, font, out TMP_Text history);
            logPanel.gameObject.SetActive(false);

            var viewData = new SerializedObject(view);
            Reference(viewData, "controller", controller); Reference(viewData, "speakerTab", tabImage);
            Reference(viewData, "speakerText", speaker); Reference(viewData, "dialogueText", body);
            Reference(viewData, "dialogueScroll", bodyScroll); Reference(viewData, "nextButton", next);
            Reference(viewData, "logButton", log); Reference(viewData, "autoButton", auto); Reference(viewData, "skipButton", skip);
            Reference(viewData, "logPanel", logPanel.gameObject); Reference(viewData, "logText", history);
            Reference(viewData, "logScroll", logScroll); Reference(viewData, "closeLogButton", closeButton);
            string[] tabs = { "Tab_Story", "Tab_Character", "Tab_Guide" };
            var tabData = viewData.FindProperty("tabs"); tabData.arraySize = tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                var entry = tabData.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("texture").objectReferenceValue = Texture(tabs[i]);
                entry.FindPropertyRelative("uv").rectValue = ArtUv(tabs[i]);
            }
            viewData.ApplyModifiedPropertiesWithoutUndo();
            var controllerData = new SerializedObject(controller);
            Reference(controllerData, "panels", panels); Reference(controllerData, "view", view);
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(panels.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Dialogue framework installed. No story content or automatic trigger was added.", controller);
        }

        public static void InstallBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/GameplayCore.unity");
            Install();
            EditorSceneManager.SaveOpenScenes();
        }

        public static TMP_FontAsset EnsureFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) return font;
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular.ttf");
            if (source == null) throw new InvalidOperationException("Missing Vietnamese-capable Alegreya font.");
            font = TMP_FontAsset.CreateFontAsset(source);
            font.name = "AlegreyaSansSC-Regular SDF";
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font);
            return font;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create dialogue UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new(0, 1);
            rect.pivot = new(.5f, .5f);
            rect.sizeDelta = new(width, height);
            rect.anchoredPosition = new(x + width / 2, -y - height / 2);
            return rect;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, float x, float y, float w, float h, float size, TMP_FontAsset font)
        {
            var label = Rect(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = size; label.color = Ink;
            label.raycastTarget = false; label.richText = false;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static ScrollRect Scroll(Transform parent, string name, float x, float y, float w, float h, int fontSize, TMP_FontAsset font, out TMP_Text text)
        {
            var area = Rect(parent, name, x, y, w, h);
            var hit = area.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 35;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect(area, "Viewport", 0, 0, w, h);
            viewport.gameObject.AddComponent<RectMask2D>();
            text = Label(viewport, "Text", string.Empty, 0, 0, w - 12, h, fontSize, font);
            text.rectTransform.pivot = new(0, 1);
            text.rectTransform.anchoredPosition = Vector2.zero;
            var fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = text.rectTransform;
            return scroll;
        }

        private static Button ArtButton(Transform parent, string name, string art, float x, float y, float w, float h)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var image = AddArt(rect, art); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new(1, 1, 1, 1);
            colors.pressedColor = new(.82f, .88f, .97f); colors.selectedColor = new(.9f, .96f, 1);
            button.colors = colors;
            rect.gameObject.AddComponent<DialogueButtonFeedback>();
            return button;
        }

        private static void ConfigureNavigation(Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].navigation = new UnityEngine.UI.Navigation {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnLeft = buttons[(i + buttons.Length - 1) % buttons.Length],
                    selectOnRight = buttons[(i + 1) % buttons.Length],
                    selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length],
                    selectOnDown = buttons[(i + 1) % buttons.Length]
                };
        }

        private static RawImage AddArt(RectTransform rect, string asset)
        {
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = Texture(asset); image.uvRect = ArtUv(asset); image.raycastTarget = false;
            return image;
        }

        private static Texture2D Texture(string asset)
        {
            string path = Art + asset + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing dialogue artwork: " + path);
            // Keep the user's existing sprite slices and GUIDs. UV cropping removes only empty margins.
            if (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled || importer.maxTextureSize < 4096)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true; importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Rect ArtUv(string asset)
        {
            var image = new Texture2D(2, 2);
            try
            {
                image.LoadImage(File.ReadAllBytes(Art + asset + ".png"));
                var pixels = image.GetPixels32();
                int left = image.width, bottom = image.height, right = 0, top = 0;
                for (int y = 0; y < image.height; y++) for (int x = 0; x < image.width; x++)
                {
                    if (pixels[y * image.width + x].a <= 24) continue;
                    left = Mathf.Min(left, x); bottom = Mathf.Min(bottom, y);
                    right = Mathf.Max(right, x + 1); top = Mathf.Max(top, y + 1);
                }
                return right > left && top > bottom
                    ? new Rect((float)left / image.width, (float)bottom / image.height, (float)(right - left) / image.width, (float)(top - bottom) / image.height)
                    : new Rect(0, 0, 1, 1);
            }
            finally { Object.DestroyImmediate(image); }
        }

        private static void Reference(SerializedObject obj, string field, Object value)
            => obj.FindProperty(field).objectReferenceValue = value;
    }
}

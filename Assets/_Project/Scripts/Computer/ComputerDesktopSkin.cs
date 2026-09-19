using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>Watercolor presentation for the existing computer. Does not own application/gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed class ComputerDesktopSkin : MonoBehaviour
    {
        public Texture2D wallpaper, taskbar, windowFrame, buttonKit, warningDialog, journalSheet;
        public Texture2D[] shortcutIcons = new Texture2D[6];
        public TMP_FontAsset font;
        private readonly List<Sprite> sprites = new();
        private bool applied;
        private Sprite buttonSprite;
        private ComputerScreenController controller;
        private GameObject startMenu;
        private readonly Dictionary<ComputerAppId, UnityEngine.UI.Button> shortcuts = new();
        private readonly Dictionary<ComputerAppId, UnityEngine.UI.Button> taskButtons = new();
        private readonly Dictionary<ComputerAppId, UnityEngine.UI.Image> indicators = new();
        private static readonly Color Ink = new(.10f, .19f, .35f);

        public void Apply(ComputerScreenController screen)
        {
            if (applied || wallpaper == null || font == null) return;
            applied = true;
            controller = screen; screen.DesktopWindows = true;
            var scaler = GetComponentInParent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            }
            var baseImage = GetComponent<UnityEngine.UI.Image>();
            if (baseImage != null) baseImage.color = Color.clear;
            // Only the old terminal chrome is hidden; all application components remain in place.
            foreach (var label in GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (label.transform.parent == transform) label.gameObject.SetActive(false);
            var backdrop = Picture(transform, "DesktopBackground", Full(wallpaper), 0, 0, 1920, 1080);
            backdrop.transform.SetAsFirstSibling();
            backdrop.raycastTarget = true;
            var desktopClick = backdrop.gameObject.AddComponent<UnityEngine.UI.Button>();
            desktopClick.transition = UnityEngine.UI.Selectable.Transition.None;
            desktopClick.onClick.AddListener(() => { if (startMenu != null) startMenu.SetActive(false); });
            var desktop = (RectTransform)screen.Desktop.transform;
            desktop.name = "DesktopIcons"; Place(desktop, 0, 0, 1920, 980);
            var desktopImage = desktop.GetComponent<UnityEngine.UI.Image>();
            if (desktopImage != null) desktopImage.enabled = false;
            var exit = transform.Find("Exit");
            if (exit == null) exit = transform.Find("ExitButton");
            if (exit != null) exit.SetParent(desktop, false);
            string[] names = { "PhotoLabIcon", "ShipStatusIcon", "MissionLogIcon", "JournalIcon", "RestIcon", exit != null ? exit.name : "ExitIcon" };
            string[] titles = { "PHOTO LAB", "SHIP STATUS", "MISSION LOG", "JOURNAL", "REST", "EXIT" };
            ComputerAppId[] ids = { ComputerAppId.PhotoLab, ComputerAppId.ShipStatus, ComputerAppId.MissionLog, ComputerAppId.Journal, ComputerAppId.Rest };
            var desktopButtons = new UnityEngine.UI.Button[6];
            for (int i = 0; i < names.Length; i++)
            {
                var shortcut = desktop.Find(names[i]);
                if (shortcut == null && i == 5)
                {
                    shortcut = Box(desktop, "ExitIcon", 0, 0, 190, 210);
                    shortcut.gameObject.AddComponent<UnityEngine.UI.Image>();
                    shortcut.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(screen.Exit);
                }
                if (shortcut == null) continue;
                Place((RectTransform)shortcut, 58 + i % 2 * 210, 55 + i / 2 * 270, 190, 220);
                var button = shortcut.GetComponent<UnityEngine.UI.Button>();
                var background = shortcut.GetComponent<UnityEngine.UI.Image>();
                SetHover(button, background);
                var icon = Picture(shortcut, "Icon", Full(shortcutIcons[i]), 5, 0, 180, 180);
                icon.preserveAspect = true;
                desktopButtons[i] = button;
                if (i < ids.Length) shortcuts[ids[i]] = button;
                foreach (var old in shortcut.GetComponentsInChildren<UnityEngine.UI.Text>(true)) old.gameObject.SetActive(false);
                var caption = Label(shortcut, "ShortcutLabel", titles[i], 0, 183, 190, 35, 25);
                caption.alignment = TextAlignmentOptions.Center; caption.fontStyle = FontStyles.Bold;
            }
            var layer = Box(transform, "AppWindowLayer", 0, 0, 1920, 972);
            buttonSprite = Slice(buttonKit, new Rect(43, 389, 320, 148), new Vector4(55, 48, 55, 48));
            foreach (var app in screen.Apps) SkinWindow(app, layer);
            var bar = Picture(transform, "Taskbar", Slice(taskbar, new Rect(0, 26, 1672, 119)), 0, 972, 1920, 108);
            bar.transform.SetAsLastSibling();
            BuildTaskbar(bar.transform, desktopButtons, ids, titles);
            screen.WindowStateChanged += RefreshTaskbar;
            RefreshTaskbar();
            // Existing Text fields remain data-only so no application logic or saved references are replaced.
            foreach (var text in GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (text.gameObject.activeSelf && text.GetComponent<ComputerDesktopText>() == null)
                    text.gameObject.AddComponent<ComputerDesktopText>().Bind(text, font, Ink);
        }

        private void SkinWindow(ComputerAppPanel app, Transform layer)
        {
            var panel = (RectTransform)app.panel.transform;
            panel.SetParent(layer, false); Place(panel, 515, 70, 1340, 875);
            var frame = Picture(panel, "WindowFrame", Slice(windowFrame, new Rect(20, 80, 1410, 920), new Vector4(50, 60, 340, 140)), 0, 0, 1340, 875);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(frame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            frame.raycastTarget = true; frame.transform.SetAsFirstSibling();
            var content = Box(panel, "ContentRoot", 50, 133, 1240, 680);
            var window = panel.gameObject.AddComponent<ComputerWindow>();
            window.Initialize(controller, app.id, content);
            var children = new List<Transform>();
            foreach (Transform child in panel) if (child != frame.transform && child != content) children.Add(child);
            foreach (var child in children)
            {
                if (child.name == "AppTitle" || child.name == "Title") { child.gameObject.SetActive(false); continue; }
                if (child.name == "Back")
                {
                    var close = child.GetComponent<UnityEngine.UI.Button>();
                    SetHover(close, child.GetComponent<UnityEngine.UI.Image>());
                    Place((RectTransform)child, -134, 30, 80, 72);
                    ((RectTransform)child).anchorMin = ((RectTransform)child).anchorMax = new Vector2(1, 1);
                    close.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                    close.onClick.AddListener(() => {
                        if (app.id == ComputerAppId.Journal || app.id == ComputerAppId.Rest)
                            GetComponent<ExpeditionComputerView>()?.CancelConfirmation();
                        controller.CloseApp(app.id);
                    });
                    foreach (Transform label in child) label.gameObject.SetActive(false);
                    continue;
                }
                child.SetParent(content, false);
            }
            string title = app.id switch { ComputerAppId.PhotoLab => "PHOTO LAB", ComputerAppId.ShipStatus => "SHIP STATUS", ComputerAppId.MissionLog => "MISSION LOG", _ => app.id.ToString().ToUpperInvariant() };
            var titleText = Label(panel, "TitleText", title, 55, 25, 880, 55, 32);
            titleText.fontStyle = FontStyles.Bold;
            Stretch(titleText.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(55, -85), new Vector2(-350, -30));
            foreach (var button in content.GetComponentsInChildren<UnityEngine.UI.Button>(true)) StyleButton(button);
            var body = content.Find("Body");
            if (body != null)
            {
                Place((RectTransform)body, 30, 15, 1180, 585);
                var text = body.GetComponent<UnityEngine.UI.Text>(); text.fontSize = 29; text.alignment = TextAnchor.UpperLeft;
            }
            if (app.id == ComputerAppId.PhotoLab)
            {
                if (body != null) { Place((RectTransform)body, 30, 5, 1180, 110); body.GetComponent<UnityEngine.UI.Text>().fontSize = 25; }
                var photo = app.panel.GetComponent<PhotoLabView>();
                if (photo != null && photo.preview != null) Place(photo.preview.rectTransform, 225, 128, 790, 395);
                var buttons = content.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                for (int i = 0; i < buttons.Length; i++) Place((RectTransform)buttons[i].transform, 250 + i * 400, 560, 340, 76);
            }
            if (app.id == ComputerAppId.MissionLog) Move(content, "NextExpeditionZone", 800, 560, 400, 76);
            if (app.id == ComputerAppId.Journal)
            {
                var paper = Picture(content, "JournalPaper", Slice(journalSheet, new Rect(1336, 622, 307, 290), new Vector4(42, 42, 42, 42)), 15, 5, 1210, 525);
                paper.type = UnityEngine.UI.Image.Type.Sliced; paper.transform.SetAsFirstSibling();
                Move(content, "JournalDetails", 45, 26, 1150, 475);
                Move(content, "PreviousDay", 15, 560, 225, 76); Move(content, "NextDay", 255, 560, 225, 76);
                Move(content, "RestoreDay", 500, 560, 320, 76); Move(content, "ConfirmRestore", 500, 560, 435, 76);
                Move(content, "CancelRestore", 955, 560, 270, 76);
            }
            if (app.id == ComputerAppId.Rest)
            {
                // Use the warning dialog's hand-painted symbol and confirmation controls with live text.
                Picture(content, "RestWarning", Slice(warningDialog, new Rect(252, 461, 239, 230)), 25, 80, 195, 188);
                Move(content, "RestDetails", 245, 25, 950, 480);
                content.Find("RestDetails").GetComponent<UnityEngine.UI.Text>().fontSize = 27;
                Move(content, "RestRequest", 30, 560, 335, 76);
                Move(content, "ConfirmRest", 400, 560, 435, 76);
                Move(content, "CancelRest", 870, 560, 335, 76);
                var confirm = content.Find("ConfirmRest").GetComponent<UnityEngine.UI.Image>();
                confirm.sprite = Slice(warningDialog, new Rect(340, 280, 373, 135), new Vector4(58, 45, 58, 45));
                content.Find("CancelRest").GetComponent<UnityEngine.UI.Image>().sprite = Slice(warningDialog, new Rect(741, 280, 375, 135), new Vector4(58, 45, 58, 45));
            }
            WindowControls(window);
        }

        private void WindowControls(ComputerWindow window)
        {
            var panel = window.Rect;
            Handle(panel.gameObject, window, false, 0);
            foreach (var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true)) Handle(button.gameObject, window, false, 0);
            var title = Hit(panel, "DragTitleBar", 30, 16, 950, 95);
            Stretch((RectTransform)title.transform, new Vector2(0, 1), Vector2.one, new Vector2(30, -112), new Vector2(-350, -16));
            Handle(title.gameObject, window, true, 0);
            var minimize = Hit(panel, "Minimize", -326, 30, 80, 72);
            ((RectTransform)minimize.transform).anchorMin = ((RectTransform)minimize.transform).anchorMax = Vector2.one;
            minimize.onClick.AddListener(() => controller.MinimizeApp(window.app));
            var maximize = Hit(panel, "Maximize", -230, 30, 80, 72);
            ((RectTransform)maximize.transform).anchorMin = ((RectTransform)maximize.transform).anchorMax = Vector2.one;
            maximize.onClick.AddListener(window.ToggleMaximize);
            // Wider corner handles take precedence over the four edge handles.
            Edge(window, "ResizeLeft", 1, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-7, 23), new Vector2(13, -23));
            Edge(window, "ResizeRight", 2, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-13, 23), new Vector2(7, -23));
            Edge(window, "ResizeTop", 4, new Vector2(0, 1), new Vector2(1, 1), new Vector2(23, -13), new Vector2(-23, 7));
            Edge(window, "ResizeBottom", 8, Vector2.zero, new Vector2(1, 0), new Vector2(23, -7), new Vector2(-23, 13));
            Edge(window, "ResizeTopLeft", 5, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-7, -23), new Vector2(23, 7));
            Edge(window, "ResizeTopRight", 6, Vector2.one, Vector2.one, new Vector2(-23, -23), new Vector2(7, 7));
            Edge(window, "ResizeBottomLeft", 9, Vector2.zero, Vector2.zero, new Vector2(-7, -7), new Vector2(23, 23));
            Edge(window, "ResizeBottomRight", 10, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-23, -7), new Vector2(7, 23));
        }
        private void Edge(ComputerWindow window, string name, int edges, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var hit = Hit(window.Rect, name, 0, 0, 20, 20);
            Stretch((RectTransform)hit.transform, min, max, offsetMin, offsetMax);
            Handle(hit.gameObject, window, false, edges);
        }
        private static void Handle(GameObject target, ComputerWindow window, bool move, int edges)
        { var handle = target.AddComponent<ComputerWindowHandle>(); handle.window = window; handle.move = move; handle.edges = edges; }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        private UnityEngine.UI.Button Hit(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = Box(parent, name, x, y, w, h);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); SetHover(button, image); return button;
        }
        private static void SetHover(UnityEngine.UI.Button button, UnityEngine.UI.Image image)
        {
            image.color = Color.white; button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.clear; colors.highlightedColor = new Color(.85f, .95f, 1, .24f);
            colors.pressedColor = new Color(.8f, .9f, 1, .38f); colors.selectedColor = new Color(.8f, .9f, 1, .28f);
            colors.fadeDuration = .08f; button.colors = colors;
        }
        private void BuildTaskbar(Transform bar, UnityEngine.UI.Button[] buttons, ComputerAppId[] ids, string[] titles)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var button = Hit(bar, "Task_" + id, 200 + 124 * i, 8, 100, 90);
                button.onClick.AddListener(() => {
                    if (controller.IsRunning(id)) controller.ToggleTaskbarApp(id);
                    else shortcuts[id].onClick.Invoke();
                });
                taskButtons[id] = button;
                var indicator = Picture(button.transform, "RunningIndicator", null, 25, 82, 50, 4);
                indicator.color = Ink; indicators[id] = indicator;
            }
            var paper = Picture(transform, "StartMenu", Slice(journalSheet, new Rect(1336, 622, 307, 290), new Vector4(42, 42, 42, 42)), 20, 405, 400, 560);
            paper.type = UnityEngine.UI.Image.Type.Sliced; paper.raycastTarget = true;
            startMenu = paper.gameObject;
            Label(paper.transform, "Heading", "APPLICATIONS", 32, 25, 330, 40, 25).fontStyle = FontStyles.Bold;
            for (int i = 0; i < buttons.Length; i++)
            {
                var shortcut = buttons[i]; if (shortcut == null) continue;
                var row = Hit(paper.transform, "Start_" + titles[i], 24, 78 + i * 74, 352, 70);
                Picture(row.transform, "Icon", Full(shortcutIcons[i]), 8, 4, 60, 60).preserveAspect = true;
                Label(row.transform, "Label", titles[i], 82, 18, 260, 42, 24);
                row.onClick.AddListener(() => { startMenu.SetActive(false); shortcut.onClick.Invoke(); });
            }
            startMenu.SetActive(false);
            var start = Hit(bar, "Start", 32, 8, 102, 90);
            start.onClick.AddListener(() => { startMenu.SetActive(!startMenu.activeSelf); if (startMenu.activeSelf) startMenu.transform.SetAsLastSibling(); });
            Hit(bar, "ShowDesktop", 1884, 8, 30, 90).onClick.AddListener(controller.ShowDesktop);
            bar.SetAsLastSibling();
        }
        private void RefreshTaskbar()
        {
            if (startMenu != null) startMenu.SetActive(false);
            foreach (var pair in taskButtons)
            {
                bool active = controller.CurrentApp == pair.Key;
                var colors = pair.Value.colors; colors.normalColor = active ? new Color(.85f, .95f, 1, .24f) : Color.clear; pair.Value.colors = colors;
                indicators[pair.Key].gameObject.SetActive(controller.IsRunning(pair.Key));
                indicators[pair.Key].rectTransform.sizeDelta = new Vector2(active ? 50 : 26, 4);
            }
            foreach (var app in controller.Apps)
            {
                var frame = app.panel.transform.Find("WindowFrame");
                if (frame != null) frame.GetComponent<UnityEngine.UI.Image>().color = controller.CurrentApp == app.id ? Color.white : new Color(.85f, .89f, .94f);
            }
        }

        private void StyleButton(UnityEngine.UI.Button button)
        {
            var image = button.GetComponent<UnityEngine.UI.Image>();
            if (image == null) return;
            image.sprite = buttonSprite; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = Color.white;
            button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.8f, 1, 1);
            colors.pressedColor = new Color(.62f, .79f, 1); colors.disabledColor = new Color(.65f, .7f, .76f, .65f); button.colors = colors;
            foreach (var label in button.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                var rect = label.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(18, 8); rect.offsetMax = new Vector2(-18, -8);
                label.fontSize = 24; label.alignment = TextAnchor.MiddleCenter;
            }
        }
        private static void Move(Transform parent, string name, float x, float y, float w, float h)
        { var child = parent.Find(name); if (child != null) Place((RectTransform)child, x, y, w, h); }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); rect.localScale = Vector3.one;
        }
        private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform; Place(rect, x, y, w, h); return rect;
        }
        private UnityEngine.UI.Image Picture(Transform parent, string name, Sprite sprite, float x, float y, float w, float h)
        {
            var image = Box(parent, name, x, y, w, h).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite; image.raycastTarget = false; return image;
        }
        private TextMeshProUGUI Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            var text = Box(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.color = Ink; text.text = value; text.raycastTarget = false; return text;
        }
        private Sprite Full(Texture2D texture) => texture == null ? null : Slice(texture, new Rect(0, 0, texture.width, texture.height));
        private Sprite Slice(Texture2D texture, Rect rect, Vector4 border = default)
        {
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
            sprites.Add(sprite); return sprite;
        }
        private void OnDestroy()
        {
            if (controller != null) controller.WindowStateChanged -= RefreshTaskbar;
            foreach (var sprite in sprites) if (sprite != null) Destroy(sprite);
        }
    }
}

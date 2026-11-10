using System.Collections.Generic;
using TMPro;
using UnityEngine;
using G10.Prototype.Audio;
using UnityEngine.InputSystem;

namespace G10.Prototype.Computer
{
    /// <summary>Watercolor presentation for the existing computer. Does not own application/gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed class ComputerDesktopSkin : MonoBehaviour
    {
        public Texture2D wallpaper, taskbar, windowFrame, buttonKit, warningDialog, journalSheet, cursorSheet;
        public Texture2D[] shortcutIcons = new Texture2D[6];
        public Texture2D upgradeIcon;
        public Texture2D cargoIcon;
        public Sprite upgradeTitleIcon;
        public TMP_FontAsset font;
        private readonly List<Sprite> sprites = new();
        private bool applied;
        private Sprite buttonSprite;
        private ComputerScreenController controller;
        private GameObject startMenu;
        private TextMeshProUGUI taskbarClock, volumeReadout;
        private RectTransform volumePopup, volumeHit;
        private UnityEngine.UI.Slider volumeSlider;
        private string lastClockText;
        private readonly Dictionary<ComputerAppId, UnityEngine.UI.Button> shortcuts = new();
        private readonly Dictionary<ComputerAppId, UnityEngine.UI.Button> taskButtons = new();
        private readonly Dictionary<ComputerAppId, UnityEngine.UI.Image> indicators = new();
        private static readonly Color Ink = new(.10f, .19f, .35f);

        public void Apply(ComputerScreenController screen)
        {
            if (applied || wallpaper == null || font == null) return;
            applied = true;
            controller = screen; screen.DesktopWindows = true;
            if (cursorSheet != null)
            {
                var cursor = GetComponent<ComputerDesktopCursor>();
                if (cursor == null) cursor = gameObject.AddComponent<ComputerDesktopCursor>();
                cursor.Initialize(cursorSheet);
            }
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
            foreach (Transform child in desktop)
                if (child.GetComponent<UnityEngine.UI.Text>() != null) child.gameObject.SetActive(false);
            var desktopImage = desktop.GetComponent<UnityEngine.UI.Image>();
            if (desktopImage != null) desktopImage.enabled = false;
            var exit = transform.Find("Exit");
            if (exit == null) exit = transform.Find("ExitButton");
            if (exit != null) exit.SetParent(desktop, false);
            string[] names = { "PhotoLabIcon", "ShipStatusIcon", "MissionLogIcon", "UpgradeIcon", "JournalIcon", "RestIcon", "CargoIcon", exit != null ? exit.name : "ExitIcon" };
            string[] titles = { "PHOTO LAB", "SHIP STATUS", "MISSION LOG", "UPGRADE", "JOURNAL", "REST", "CARGO", "EXIT" };
            ComputerAppId[] ids = { ComputerAppId.PhotoLab, ComputerAppId.ShipStatus, ComputerAppId.MissionLog, ComputerAppId.Upgrade, ComputerAppId.Journal, ComputerAppId.Rest, ComputerAppId.Cargo };
            Texture2D[] icons = {
                ShortcutIcon(0), ShortcutIcon(1), ShortcutIcon(2), upgradeIcon,
                ShortcutIcon(3), ShortcutIcon(4), cargoIcon, ShortcutIcon(5)
            };
            var desktopButtons = new UnityEngine.UI.Button[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var shortcut = desktop.Find(names[i]);
                if (shortcut == null && i == names.Length - 1)
                {
                    shortcut = Box(desktop, "ExitIcon", 0, 0, 190, 210);
                    shortcut.gameObject.AddComponent<UnityEngine.UI.Image>();
                    shortcut.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(screen.Exit);
                }
                if (shortcut == null) continue;
                Place((RectTransform)shortcut, 58 + i % 4 * 210, 55 + i / 4 * 270, 190, 220);
                var button = shortcut.GetComponent<UnityEngine.UI.Button>();
                var background = shortcut.GetComponent<UnityEngine.UI.Image>();
                SetHover(button, background);
                var icon = Picture(shortcut, "Icon", Full(icons[i]), 5, 0, 180, 180);
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
            BuildTaskbar(bar.transform, desktopButtons, icons, ids, titles);
            BuildSystemTray(bar.transform);
            screen.WindowStateChanged += RefreshTaskbar;
            RefreshTaskbar();
            // Existing Text fields remain data-only so no application logic or saved references are replaced.
            foreach (var text in GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (text.gameObject.activeSelf && text.GetComponent<ComputerDesktopText>() == null)
                    text.gameObject.AddComponent<ComputerDesktopText>().Bind(text, font, Ink);
            foreach (var mission in GetComponentsInChildren<MissionLogView>(true)) mission.Refresh();
        }

        private void SkinWindow(ComputerAppPanel app, Transform layer)
        {
            var panel = (RectTransform)app.panel.transform;
            panel.SetParent(layer, false); Place(panel, app.id == ComputerAppId.Cargo ? 290 : 515, 70, 1340, 875);
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
            string title = app.id switch { ComputerAppId.PhotoLab => "PHOTO LAB", ComputerAppId.ShipStatus => "SHIP STATUS", ComputerAppId.MissionLog => "MISSION LOG", ComputerAppId.Upgrade => "SUBMARINE UPGRADE", _ => app.id.ToString().ToUpperInvariant() };
            if (app.id == ComputerAppId.Cargo && cargoIcon != null)
                Picture(panel, "TitleIcon", Full(cargoIcon), 35, 20, 65, 65).preserveAspect = true;
            if (app.id == ComputerAppId.Upgrade && upgradeTitleIcon != null)
            {
                var titleIcon = Picture(panel, "TitleIcon", upgradeTitleIcon, 34, 18, 68, 68);
                titleIcon.preserveAspect = true;
            }
            float titleLeft = app.id == ComputerAppId.Cargo || app.id == ComputerAppId.Upgrade && upgradeTitleIcon != null ? 112 : 55;
            var titleText = Label(panel, "TitleText", title, titleLeft, 25, 880, 55, app.id == ComputerAppId.Upgrade ? 36 : 32);
            titleText.fontStyle = FontStyles.Bold;
            Stretch(titleText.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(titleLeft, -85), new Vector2(-350, -30));
            if (app.id != ComputerAppId.Upgrade && app.id != ComputerAppId.Cargo)
                foreach (var button in content.GetComponentsInChildren<UnityEngine.UI.Button>(true)) StyleButton(button);
            var body = content.Find("Body");
            if (body != null && app.id != ComputerAppId.MissionLog)
            {
                Place((RectTransform)body, 30, 15, 1180, 585);
                var text = body.GetComponent<UnityEngine.UI.Text>(); text.fontSize = 29; text.alignment = TextAnchor.UpperLeft;
            }
            if (app.id == ComputerAppId.PhotoLab)
                BuildPhotoLab(app.panel.GetComponent<PhotoLabView>(), content, body);
            if (app.id == ComputerAppId.ShipStatus && body != null)
            {
                Place((RectTransform)body, 30, 15, 1180, 550);
                body.GetComponent<UnityEngine.UI.Text>().fontSize = 27;
            }
            if (app.id == ComputerAppId.MissionLog)
            {
                var mission = app.panel.GetComponent<MissionLogView>();
                if (mission != null)
                {
                    mission.EnsureScrollLayout(content);
                    mission.Body.fontSize = 29; mission.Body.alignment = TextAnchor.UpperLeft;
                }
            }
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
            // The hit surface is a flat highlight; artwork lives on separate children.
            image.sprite = null;
            image.overrideSprite = null;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.color = Color.white; button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.clear; colors.highlightedColor = new Color(.85f, .95f, 1, .24f);
            colors.pressedColor = new Color(.8f, .9f, 1, .38f); colors.selectedColor = new Color(.8f, .9f, 1, .28f);
            colors.fadeDuration = .08f; button.colors = colors;
        }
        private void BuildTaskbar(Transform bar, UnityEngine.UI.Button[] buttons, Texture2D[] icons, ComputerAppId[] ids, string[] titles)
        {
            Picture(bar, "AppStrip", Slice(taskbar, new Rect(1150, 26, 100, 119)), 150, 0, 1290, 108);
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (!shortcuts.ContainsKey(id)) continue;
                var button = Hit(bar, "Task_" + id, 200 + 124 * i, 8, 100, 90);
                Picture(button.transform, "Icon", Full(icons[i]), 14, 4, 72, 72).preserveAspect = true;
                button.onClick.AddListener(() => {
                    if (controller.IsRunning(id)) controller.ToggleTaskbarApp(id);
                    else shortcuts[id].onClick.Invoke();
                });
                taskButtons[id] = button;
                var indicator = Picture(button.transform, "RunningIndicator", null, 25, 82, 50, 4);
                indicator.color = Ink; indicators[id] = indicator;
            }
            var paper = Picture(transform, "StartMenu", Slice(journalSheet, new Rect(1336, 622, 307, 290), new Vector4(42, 42, 42, 42)), 20, 260, 400, 705);
            paper.type = UnityEngine.UI.Image.Type.Sliced; paper.raycastTarget = true;
            startMenu = paper.gameObject;
            Label(paper.transform, "Heading", "APPLICATIONS", 32, 25, 330, 40, 25).fontStyle = FontStyles.Bold;
            for (int i = 0; i < buttons.Length; i++)
            {
                var shortcut = buttons[i]; if (shortcut == null) continue;
                var row = Hit(paper.transform, "Start_" + titles[i], 24, 78 + i * 74, 352, 70);
                Picture(row.transform, "Icon", Full(icons[i]), 8, 4, 60, 60).preserveAspect = true;
                Label(row.transform, "Label", titles[i], 82, 18, 260, 42, 24);
                row.onClick.AddListener(() => { startMenu.SetActive(false); shortcut.onClick.Invoke(); });
            }
            startMenu.SetActive(false);
            var start = Hit(bar, "Start", 32, 8, 102, 90);
            start.onClick.AddListener(() => { startMenu.SetActive(!startMenu.activeSelf); if (startMenu.activeSelf) startMenu.transform.SetAsLastSibling(); });
            Hit(bar, "ShowDesktop", 1884, 8, 30, 90).onClick.AddListener(controller.ShowDesktop);
            bar.SetAsLastSibling();
        }

        private Texture2D ShortcutIcon(int index)
            => shortcutIcons != null && index >= 0 && index < shortcutIcons.Length ? shortcutIcons[index] : null;
        private void RefreshTaskbar()
        {
            if (startMenu != null) startMenu.SetActive(false);
            CloseVolume();
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

        private void BuildSystemTray(Transform bar)
        {
            // Reuse a clean strip of the painted taskbar to omit the baked-in chevron.
            Picture(bar, "TrayBackground", Slice(taskbar, new Rect(1150, 26, 100, 119)), 1450, 0, 105, 108);
            taskbarClock = Label(bar, "RealTimeClock", "", 1730, 20, 148, 66, 28);
            taskbarClock.alignment = TextAlignmentOptions.Center;
            taskbarClock.fontStyle = FontStyles.Bold;

            var speaker = Hit(bar, "Volume", 1625, 12, 84, 78);
            volumeHit = (RectTransform)speaker.transform;
            var popup = Picture(bar, "VolumePopup", Slice(journalSheet, new Rect(1336, 622, 307, 290), new Vector4(42, 42, 42, 42)), 1410, -276, 470, 263);
            popup.type = UnityEngine.UI.Image.Type.Sliced;
            popup.raycastTarget = true;
            volumePopup = popup.rectTransform;
            Label(volumePopup, "Title", "ÂM LƯỢNG", 30, 25, 290, 40, 27).fontStyle = FontStyles.Bold;
            volumeReadout = Label(volumePopup, "Value", "", 330, 25, 110, 40, 27);
            volumeReadout.alignment = TextAlignmentOptions.Right;
            var sliderRect = Box(volumePopup, "VolumeSlider", 36, 90, 398, 54);
            var hit = sliderRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            hit.color = Color.clear;
            Picture(sliderRect, "Track", null, 0, 23, 398, 8).color = new Color(.12f, .22f, .36f, .24f);
            var fillArea = Box(sliderRect, "FillArea", 0, 23, 398, 8);
            var fill = Picture(fillArea, "Fill", null, 0, 0, 398, 8);
            fill.color = new Color(.19f, .46f, .63f);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handleArea = Box(sliderRect, "HandleArea", 12, 10, 374, 34);
            var handle = Picture(handleArea, "Handle", null, 0, 0, 24, 34);
            handle.color = Color.white;
            handle.rectTransform.pivot = new Vector2(.5f, .5f);
            handle.rectTransform.anchorMin = Vector2.zero;
            handle.rectTransform.anchorMax = Vector2.up;
            handle.rectTransform.sizeDelta = new Vector2(24, 0);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            volumeSlider = sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            volumeSlider.fillRect = fill.rectTransform;
            volumeSlider.handleRect = handle.rectTransform;
            volumeSlider.targetGraphic = handle;
            volumeSlider.minValue = 0; volumeSlider.maxValue = 1;
            volumeSlider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            volumeSlider.onValueChanged.AddListener(value => {
                if (AudioManager.Instance != null) AudioManager.Instance.SetOutputVolume(value);
                volumeReadout.text = Mathf.RoundToInt(value * 100) + "%";
            });
            Label(volumePopup, "Hint", "Âm thanh tổng của game", 36, 156, 398, 32, 21);
            var shakeButton = Hit(volumePopup, "ScreenShakeToggle", 30, 198, 410, 44);
            var shakeLabel = Label(shakeButton.transform, "Label", "", 6, 4, 398, 36, 21);
            void RefreshShake() => shakeLabel.text = "SCREEN SHAKE: " + (G10.Prototype.Feedback.ImpactShake.ScreenShakeEnabled ? "ON" : "OFF");
            RefreshShake();
            shakeButton.onClick.AddListener(() => {
                G10.Prototype.Feedback.ImpactShake.SetScreenShakeEnabled(!G10.Prototype.Feedback.ImpactShake.ScreenShakeEnabled);
                RefreshShake();
            });
            speaker.onClick.AddListener(() => {
                bool show = !volumePopup.gameObject.activeSelf;
                if (startMenu != null) startMenu.SetActive(false);
                if (!show) { CloseVolume(); return; }
                float value = AudioManager.Instance != null ? AudioManager.Instance.OutputVolume : AudioListener.volume;
                volumeSlider.SetValueWithoutNotify(value);
                RefreshShake();
                volumeReadout.text = Mathf.RoundToInt(value * 100) + "%";
                volumePopup.gameObject.SetActive(true);
            });
            volumePopup.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (taskbarClock != null)
            {
                string clockText = System.DateTime.Now.ToString("HH:mm:ss");
                if (clockText != lastClockText) { taskbarClock.text = clockText; lastClockText = clockText; }
            }
            if (volumePopup == null || !volumePopup.gameObject.activeSelf || Mouse.current == null) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;
            var point = Mouse.current.position.ReadValue();
            var canvas = volumePopup.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.RectangleContainsScreenPoint(volumePopup, point, camera)
                && !RectTransformUtility.RectangleContainsScreenPoint(volumeHit, point, camera)) CloseVolume();
        }

        private void CloseVolume()
        {
            if (volumePopup == null || !volumePopup.gameObject.activeSelf) return;
            volumePopup.gameObject.SetActive(false);
            PlayerPrefs.Save();
        }

        private void OnDisable() => CloseVolume();


        private void BuildPhotoLab(PhotoLabView photo, RectTransform content, Transform oldBody)
        {
            if (photo == null) return;
            if (oldBody != null) oldBody.gameObject.SetActive(false);
            var paperSprite = Slice(journalSheet, new Rect(1336, 622, 307, 290), new Vector4(42, 42, 42, 42));
            var stage = Picture(content, "PhotoStage", paperSprite, 0, 0, 766, 680);
            stage.type = UnityEngine.UI.Image.Type.Sliced;
            stage.color = new Color(.37f, .36f, .65f); stage.transform.SetAsFirstSibling();
            var paper = Picture(content, "SurveyPaper", paperSprite, 756, 0, 484, 680);
            paper.type = UnityEngine.UI.Image.Type.Sliced;
            var area = Box(stage.transform, "PhotoPreviewArea", 25, 18, 706, 548);
            photo.ConfigurePreviewArea(area);
            photo.preview.raycastTarget = true;
            var imageButton = photo.preview.GetComponent<UnityEngine.UI.Button>();
            if (imageButton == null) imageButton = photo.preview.gameObject.AddComponent<UnityEngine.UI.Button>();
            imageButton.targetGraphic = photo.preview; imageButton.transition = UnityEngine.UI.Selectable.Transition.None;
            var empty = Label(stage.transform, "EmptyGallery", "", 65, 205, 626, 150, 27);
            empty.color = new Color(.85f, .91f, 1); empty.alignment = TextAlignmentOptions.Center;
            var previous = content.Find("PreviousPhoto")?.GetComponent<UnityEngine.UI.Button>();
            var next = content.Find("NextPhoto")?.GetComponent<UnityEngine.UI.Button>();
            var send = photo.sendButton;
            PlacePhotoButton(previous, stage.transform, "←", 208, 582, 88, 50, 32);
            PlacePhotoButton(next, stage.transform, "→", 460, 582, 88, 50, 32);
            var count = Label(stage.transform, "PhotoCounter", "00 / 00", 301, 584, 154, 46, 27);
            count.color = new Color(.88f, .92f, 1); count.alignment = TextAlignmentOptions.Center;
            var hint = Label(stage.transform, "EnlargeHint", "Bấm ảnh để xem lớn", 105, 637, 546, 30, 22);
            hint.alignment = TextAlignmentOptions.Center; hint.fontStyle = FontStyles.Italic;
            hint.color = new Color(.85f, .89f, 1);
            var title = Label(paper.transform, "SurveyHeading", "DỮ LIỆU KHẢO SÁT", 32, 30, 420, 48, 32);
            title.fontStyle = FontStyles.Bold;
            var nameLabel = Label(paper.transform, "SampleName", "Mẫu chưa định danh", 32, 85, 420, 60, 26);
            nameLabel.fontStyle = FontStyles.Italic;
            nameLabel.enableAutoSizing = true; nameLabel.fontSizeMin = 22; nameLabel.fontSizeMax = 26;
            var badge = Picture(paper.transform, "QualityBadge", buttonSprite, 30, 151, 358, 50);
            badge.type = UnityEngine.UI.Image.Type.Sliced; badge.pixelsPerUnitMultiplier = 2; badge.color = new Color(.77f, 1, .92f);
            var qualityLabel = Label(badge.transform, "Quality", "", 14, 4, 330, 40, 24);
            qualityLabel.alignment = TextAlignmentOptions.Center;
            PhotoRule(paper.transform, 222);
            var coordinate = PhotoRow(paper.transform, "Tọa độ", 240);
            var depthLabel = PhotoRow(paper.transform, "Độ sâu", 292);
            var headingLabel = PhotoRow(paper.transform, "Hướng chụp", 344);
            var time = PhotoRow(paper.transform, "Thời điểm", 396);
            PhotoRule(paper.transform, 449);
            var status = Label(paper.transform, "SurveyStatus", "", 32, 465, 420, 62, 24);
            status.alignment = TextAlignmentOptions.Center;
            PlacePhotoButton(send, paper.transform, "GỬI DỮ LIỆU", 32, 539, 420, 86, 34);
            send.GetComponent<UnityEngine.UI.Image>().sprite = Slice(buttonKit, new Rect(366, 389, 320, 148), new Vector4(55, 48, 55, 48));
            var delete = PhotoButton(paper.transform, "DeletePhoto", "XÓA ẢNH", 285, 632, 165);
            Place((RectTransform)delete.transform, 285, 632, 165, 36);
            delete.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            delete.transition = UnityEngine.UI.Selectable.Transition.None;
            var deleteCaption = delete.GetComponentInChildren<TMP_Text>();
            Place(deleteCaption.rectTransform, 0, 0, 165, 36);
            deleteCaption.color = new Color(.55f, .18f, .28f); deleteCaption.fontSize = 23;
            deleteCaption.fontStyle = FontStyles.Underline;
            var viewer = PhotoModal("PhotoViewer");
            var largeArea = Box(viewer, "ImageArea", 75, 75, 1770, 910);
            Stretch(largeArea, Vector2.zero, Vector2.one, new Vector2(75, 90), new Vector2(-75, -80));
            var large = Box(largeArea, "FullPhotograph", 0, 0, 1, 1).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            large.rectTransform.anchorMin = large.rectTransform.anchorMax = large.rectTransform.pivot = new Vector2(.5f, .5f);
            large.rectTransform.anchoredPosition = Vector2.zero; large.raycastTarget = false;
            var close = PhotoButton(viewer, "CloseViewer", "ĐÓNG · ESC", 1620, 20, 240);
            close.onClick.AddListener(() => { photo.CloseViewer(); photo.Refresh(); });
            var viewerHint = Label(viewer, "ViewerHint", "Bấm ĐÓNG hoặc Escape để quay lại", 510, 1015, 900, 38, 24);
            viewerHint.color = new Color(.87f, .93f, 1); viewerHint.alignment = TextAlignmentOptions.Center;
            viewer.gameObject.SetActive(false);
            var confirmation = PhotoModal("DeleteConfirmation");
            var dialog = Picture(confirmation, "ConfirmationPaper", paperSprite, 555, 365, 810, 350);
            dialog.type = UnityEngine.UI.Image.Type.Sliced;
            var message = Label(dialog.transform, "Message", "", 40, 30, 730, 150, 29);
            message.alignment = TextAlignmentOptions.Center;
            var cancel = PhotoButton(dialog.transform, "CancelDelete", "GIỮ ẢNH", 55, 230, 310);
            var confirm = PhotoButton(dialog.transform, "ConfirmDelete", "XÓA ẢNH", 445, 230, 310);
            confirm.GetComponentInChildren<TMP_Text>().color = new Color(.55f, .18f, .28f);
            confirmation.gameObject.SetActive(false);
            photo.ConfigureDeletionUI(delete, previous, next, confirmation.gameObject, message, confirm, cancel);
            photo.ConfigurePresentation(nameLabel, qualityLabel, coordinate, depthLabel, headingLabel, time, status,
                count, empty, imageButton, viewer.gameObject, large, largeArea);
        }
        private RectTransform PhotoModal(string name)
        {
            var modal = Box(transform, name, 0, 0, 1920, 1080);
            Stretch(modal, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var blocker = modal.gameObject.AddComponent<UnityEngine.UI.Image>();
            blocker.color = new Color(.035f, .055f, .14f, .96f); blocker.raycastTarget = true;
            return modal;
        }
        private TMP_Text PhotoRow(Transform parent, string caption, float y)
        {
            var label = Label(parent, caption + "Label", caption, 36, y, 150, 42, 24);
            label.fontStyle = FontStyles.Bold; label.alignment = TextAlignmentOptions.MidlineLeft;
            var value = Label(parent, caption + "Value", "—", 185, y, 263, 42, 24);
            value.alignment = TextAlignmentOptions.MidlineRight;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.enableAutoSizing = true; value.fontSizeMin = 21; value.fontSizeMax = 24;
            if (y < 396) PhotoRule(parent, y + 46, .38f);
            return value;
        }
        private void PhotoRule(Transform parent, float y, float alpha = .65f)
        {
            var line = Picture(parent, "PaperRule", null, 30, y, 424, 2);
            line.color = new Color(.27f, .35f, .62f, alpha);
        }
        private void PlacePhotoButton(UnityEngine.UI.Button button, Transform parent, string caption,
            float x, float y, float width, float height, int size)
        {
            if (button == null) return;
            button.transform.SetParent(parent, false); Place((RectTransform)button.transform, x, y, width, height);
            button.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = 2;
            foreach (var old in button.GetComponentsInChildren<UnityEngine.UI.Text>(true)) old.gameObject.SetActive(false);
            var label = Label(button.transform, "PhotoCaption", caption, 10, 0, width - 20, height, size);
            label.alignment = TextAlignmentOptions.Center; label.fontStyle = FontStyles.Bold;
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
        private UnityEngine.UI.Button PhotoButton(Transform parent, string name, string caption, float x, float y, float width)
        {
            var button = Hit(parent, name, x, y, width, 76); StyleButton(button);
            var label = Label(button.transform, "Caption", caption, 18, 8, width - 36, 60, 24);
            label.alignment = TextAlignmentOptions.Center; label.fontStyle = FontStyles.Bold;
            if (caption == "DELETE")
            {
                label.color = new Color(.55f, .18f, .26f);
                var colors = button.colors; colors.normalColor = new Color(1, .86f, .87f);
                colors.highlightedColor = new Color(1, .73f, .76f); button.colors = colors;
            }
            return button;
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

using System;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace G10.Prototype.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuDebugStart : MonoBehaviour
    {
        public UnityEngine.UI.Button template;
        public ZoneMapConfig[] maps = Array.Empty<ZoneMapConfig>();
        public SurveyContentDefinition[] contentCatalog = Array.Empty<SurveyContentDefinition>();
        public ShipState initialShip;
        public UnityEngine.UI.Button DebugButton { get; private set; }
        public UnityEngine.UI.Button StartButton { get; private set; }
        public UnityEngine.UI.Button ContinueButton { get; private set; }
        public UnityEngine.UI.Button CancelButton { get; private set; }
        public int SelectedZone { get; private set; } = 1;
        public bool RegularMax { get; private set; }
        public bool IsOpen => overlay != null && overlay.activeSelf;
        private GameObject overlay, oldSelection;
        private TMP_Text selection, error;
        private readonly System.Collections.Generic.Dictionary<UnityEngine.UI.Selectable, bool> blocked = new();
        public static bool VisibleForConfiguration(bool editor, bool development) => editor || development;
        public static bool IsEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }
        private void Start()
        {
            if (!IsEnabled || template == null) return;
            DebugButton = Button(transform, "DebugStartButton", "DEBUG", new Vector2(280, 70), new Vector2(460, -180), Open);
        }
        public void Open()
        {
            if (!IsEnabled || SceneFlowController.Instance?.IsTransitioning == true || IsOpen) return;
            if (overlay == null) Build();
            oldSelection = EventSystem.current?.currentSelectedGameObject;
            blocked.Clear();
            foreach (var control in GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
                if (!control.transform.IsChildOf(overlay.transform)) { blocked[control] = control.interactable; control.interactable = false; }
            overlay.SetActive(true); overlay.transform.SetAsLastSibling();
            ContinueButton.interactable = ExpeditionSaveStore.TryReadDebug(out var previous, out _) && previous != null;
            UpdateSelection();
            EventSystem.current?.SetSelectedGameObject(CancelButton.gameObject);
        }
        public void SelectZone(int value) { if (value is >= 1 and <= 4) SelectedZone = value; UpdateSelection(); }
        public void ToggleRegularMax() { RegularMax = !RegularMax; UpdateSelection(); }
        private void UpdateSelection()
        {
            if (selection != null) selection.text = $"ZONE {SelectedZone:00} · REGULAR UPGRADES: {(RegularMax ? "MAX" : "NONE")}\nSeparate DEBUG save — normal expedition remains untouched.";
        }
        public void StartRun()
        {
            if (!IsEnabled || !IsOpen) return;
            try
            {
                var preset = DebugExpeditionPreset.Create(SelectedZone, maps, contentCatalog, initialShip, RegularMax);
                if (SceneFlowController.Instance?.StartDebugRun(preset) == true) Close();
                else if (error != null) error.text = SceneFlowController.Instance?.LastError ?? "Start from Bootstrap.";
            }
            catch (Exception ex) { if (error != null) error.text = ex.Message; }
        }
        public void ContinueRun() { if (IsEnabled && IsOpen) { Close(); SceneFlowController.Instance?.ContinueDebugGame(); } }
        public void Close()
        {
            if (overlay != null) overlay.SetActive(false);
            foreach (var pair in blocked) if (pair.Key != null) pair.Key.interactable = pair.Value;
            blocked.Clear();
            if (oldSelection != null && oldSelection.activeInHierarchy) EventSystem.current?.SetSelectedGameObject(oldSelection);
            oldSelection = null;
        }
        private void Update() { if (IsOpen && (Keyboard.current?.escapeKey.wasPressedThisFrame == true || Gamepad.current?.buttonEast.wasPressedThisFrame == true)) Close(); }
        private void OnDisable() => Close();
        private void Build()
        {
            var background = Rect("DebugStartOverlay", transform, Vector2.zero, Vector2.zero);
            background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one; background.offsetMin = background.offsetMax = Vector2.zero;
            overlay = background.gameObject; overlay.SetActive(false);
            overlay.AddComponent<UnityEngine.UI.Image>().color = new Color(.04f, .08f, .14f, .85f);
            var panel = Rect("DebugStartCard", background, new Vector2(940, 540), Vector2.zero);
            var image = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            var skin = template.targetGraphic as UnityEngine.UI.Image;
            if (skin != null) { image.sprite = skin.sprite; image.type = UnityEngine.UI.Image.Type.Sliced; }
            image.color = new Color(.93f, .97f, 1);
            Label(panel, "Title", "DEBUG START", new Vector2(850, 60), new Vector2(0, 215), 34);
            for (int zone = 1; zone <= 4; zone++)
            { int selected = zone; Button(panel, $"Zone{zone:00}", $"ZONE {zone:00}", new Vector2(180, 70), new Vector2((zone - 2.5f) * 205, 115), () => SelectZone(selected)); }
            selection = Label(panel, "Preset", "", new Vector2(850, 95), new Vector2(0, 15), 24);
            Button(panel, "RegularPreset", "TOGGLE REGULAR MAX", new Vector2(350, 55), new Vector2(0, -65), ToggleRegularMax);
            StartButton = Button(panel, "StartDebugRun", "START DEBUG RUN", new Vector2(290, 65), new Vector2(-295, -150), StartRun);
            ContinueButton = Button(panel, "ContinueDebugRun", "CONTINUE DEBUG", new Vector2(290, 65), new Vector2(0, -150), ContinueRun);
            CancelButton = Button(panel, "CancelDebugStart", "CANCEL", new Vector2(240, 65), new Vector2(280, -150), Close);
            error = Label(panel, "Error", "", new Vector2(850, 55), new Vector2(0, -220), 20);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private TMP_Text Label(Transform parent, string name, string value, Vector2 size, Vector2 position, float fontSize)
        {
            var label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            var source = template.GetComponentInChildren<TMP_Text>(true); if (source != null) label.font = source.font;
            label.color = new Color(.16f, .22f, .38f); label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; label.text = value; return label;
        }
        private UnityEngine.UI.Button Button(Transform parent, string name, string text, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(name, parent, size, position); var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var original = template.targetGraphic as UnityEngine.UI.Image;
            if (original != null) { image.sprite = original.sprite; image.type = original.type; image.color = original.color; }
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.colors = template.colors;
            button.onClick.AddListener(action);
            var label = Label(rect, "Label", text, size, Vector2.zero, 23);
            var source = template.GetComponentInChildren<TMP_Text>(true);
            var legacy = template.GetComponentInChildren<UnityEngine.UI.Text>(true);
            label.color = source != null ? source.color : legacy != null ? legacy.color : Color.white;
            return button;
        }
    }
}

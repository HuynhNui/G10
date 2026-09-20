using G10.Prototype.Audio;
using G10.Prototype.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>Owns a standalone pause overlay. No pause graphics or raycaster are active while closed.</summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour, IPanelBackHandler
    {
        public static PauseMenuController Instance { get; private set; }
        public bool IsPaused { get; private set; }

        private GameObject overlay;
        private GameObject shortcutCanvas;
        private Button resumeButton;
        private GameObject previousSelection;
        private float previousTimeScale = 1f;
        private UIManager panelManager;
        private bool gameplayAvailable;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Instance == null)
                new GameObject("PauseService").AddComponent<PauseMenuController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            RefreshShortcut();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            ClosePause();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClosePause();
            RefreshShortcut();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            ClosePause();
            RefreshShortcut();
        }

        private static bool HasGameplay()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).name.StartsWith("Zone")) return true;
            return false;
        }

        private void Update()
        {
            // UIManager owns Escape when gameplay panels are present.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
                && FindAnyObjectByType<UIManager>() == null)
                TogglePause();
        }

        private void LateUpdate()
        {
            if (shortcutCanvas == null) return;
            bool show = gameplayAvailable && !IsPaused && (panelManager == null || !panelManager.IsPanelOpen);
            if (shortcutCanvas.activeSelf != show) shortcutCanvas.SetActive(show);
        }

        public void OpenPause()
        {
            if (IsPaused || !HasGameplay()) return;
            if (SceneFlowController.Instance != null && SceneFlowController.Instance.IsTransitioning) return;
            if (overlay == null) BuildOverlay();
            FindAnyObjectByType<CabinStationView>()?.Brake();
            previousTimeScale = Time.timeScale;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            IsPaused = true;
            overlay.SetActive(true);
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayPauseMenu();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            AudioManager.Instance?.PlayButtonClick();
            ClosePause();
        }

        private void ClosePause()
        {
            if (overlay != null) overlay.SetActive(false);
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = previousTimeScale;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
        }

        public void TogglePause() { if (IsPaused) Resume(); else OpenPause(); }
        public bool TryHandleBack() { if (!IsPaused) return false; Resume(); return true; }

        public void RestartZone()
        {
            ClosePause();
            AudioManager.Instance?.PlayButtonClick();
            string zone = SceneFlowController.Instance != null ? SceneFlowController.Instance.CurrentZoneScene : null;
            if (string.IsNullOrEmpty(zone))
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).name.StartsWith("Zone")) { zone = SceneManager.GetSceneAt(i).name; break; }
            if (string.IsNullOrEmpty(zone)) return;
            if (SceneFlowController.Instance != null) SceneFlowController.Instance.LoadZone(zone);
            else SceneManager.LoadScene(zone);
        }

        public void LoadMainMenu()
        {
            ClosePause();
            AudioManager.Instance?.PlayButtonClick();
            if (SceneFlowController.Instance != null) SceneFlowController.Instance.LoadMainMenu();
            else SceneManager.LoadScene(SceneFlowController.MainMenuScene);
        }

        public void QuitGame()
        {
            ClosePause();
            AudioManager.Instance?.PlayButtonBack();
            if (SceneFlowController.Instance != null) SceneFlowController.Instance.QuitGame();
            else Application.Quit();
        }

        private GameObject CreateCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            return go;
        }

        private void BuildOverlay()
        {
            overlay = CreateCanvas("PauseOverlay", 900);
            var backdrop = CreateRect("Backdrop", overlay.transform, Vector2.zero, Vector2.zero);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
            var dim = backdrop.gameObject.AddComponent<Image>();
            dim.color = new Color(.015f, .03f, .06f, .78f);
            dim.raycastTarget = true;
            var dialog = CreateRect("Dialog", overlay.transform, new Vector2(580, 570), Vector2.zero);
            dialog.gameObject.AddComponent<Image>().color = new Color(.055f, .095f, .15f, 1f);
            var accent = CreateRect("Accent", dialog, new Vector2(500, 3), new Vector2(0, 250));
            accent.gameObject.AddComponent<Image>().color = new Color(.48f, .82f, .9f);
            CreateLabel(dialog, "TẠM DỪNG", new Vector2(500, 65), new Vector2(0, 193), 40);
            CreateLabel(dialog, "Tiếp tục chuyến thám hiểm khi bạn sẵn sàng", new Vector2(520, 40), new Vector2(0, 137), 21);
            resumeButton = CreateButton(dialog, "Resume", "TIẾP TỤC", new Vector2(430, 62), new Vector2(0, 57), Resume);
            CreateButton(dialog, "Restart", "CHƠI LẠI KHU VỰC", new Vector2(430, 62), new Vector2(0, -18), RestartZone);
            CreateButton(dialog, "MainMenu", "VỀ MENU CHÍNH", new Vector2(430, 62), new Vector2(0, -93), LoadMainMenu);
            CreateButton(dialog, "Quit", "THOÁT GAME", new Vector2(430, 62), new Vector2(0, -168), QuitGame);
            CreateLabel(dialog, "ESC  ·  TIẾP TỤC", new Vector2(430, 30), new Vector2(0, -239), 18);
        }

        private void RefreshShortcut()
        {
            panelManager = FindAnyObjectByType<UIManager>();
            gameplayAvailable = HasGameplay();
            bool show = gameplayAvailable && (panelManager == null || !panelManager.IsPanelOpen);
            if (show && shortcutCanvas == null)
            {
                // This canvas has no full-screen Graphic: only the small button can receive a raycast.
                shortcutCanvas = CreateCanvas("PauseShortcut", 50);
                var button = CreateButton(shortcutCanvas.transform, "Pause", "Ⅱ  TẠM DỪNG", new Vector2(170, 48), new Vector2(-115, -54), OpenPause);
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = Vector2.one;
            }
            if (shortcutCanvas != null) shortcutCanvas.SetActive(show);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }

        private static void CreateLabel(Transform parent, string text, Vector2 size, Vector2 position, int fontSize)
        {
            var label = CreateRect("Label", parent, size, position).gameObject.AddComponent<Text>();
            label.font = Resources.Load<Font>("UI/AlegreyaSansSC-Bold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.9f, .95f, 1f); label.text = text;
            label.raycastTarget = false;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var rect = CreateRect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(.1f, .19f, .28f);
            colors.highlightedColor = new Color(.17f, .34f, .44f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.08f, .15f, .22f);
            colors.fadeDuration = .1f; button.colors = colors;
            button.onClick.AddListener(action);
            CreateLabel(rect, text, size, Vector2.zero, 25);
            return button;
        }
    }
}

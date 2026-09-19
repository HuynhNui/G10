using System;
using G10.Prototype.Audio;
using G10.Prototype.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour, IPanelBackHandler
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        private GameObject proceduralCanvasGo;
        private GameObject inGamePauseButtonGo;

        public bool IsPaused => pausePanel != null && pausePanel.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = FindAnyObjectByType<PauseMenuController>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    var go = new GameObject("PauseMenuController");
                    DontDestroyOnLoad(go);
                    go.AddComponent<PauseMenuController>();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            FindOrBuildUI();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            // Ensure timeScale is restored if scene changes while paused
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPaused)
            {
                Time.timeScale = 1f;
                if (pausePanel != null) pausePanel.SetActive(false);
            }

            FindOrBuildUI();
            EnsureInGamePauseButton(scene.name);
        }

        private void Update()
        {
            // Fallback ESC handling if UIManager is not in the scene
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                var uiManager = FindAnyObjectByType<UIManager>();
                if (uiManager == null)
                {
                    TogglePause();
                }
            }
        }

        public void FindOrBuildUI()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
                BindButtons();
                return;
            }

            var existingMenu = GameObject.Find("PauseMenu");
            if (existingMenu != null)
            {
                pausePanel = existingMenu;
                resumeButton = existingMenu.transform.Find("DialogBox/ResumeButton")?.GetComponent<Button>();
                restartButton = existingMenu.transform.Find("DialogBox/RestartButton")?.GetComponent<Button>();
                mainMenuButton = existingMenu.transform.Find("DialogBox/MainMenuButton")?.GetComponent<Button>();
                quitButton = existingMenu.transform.Find("DialogBox/QuitButton")?.GetComponent<Button>();
                pausePanel.SetActive(false);
                BindButtons();
                return;
            }

            BuildProceduralUI();
        }

        private void BuildProceduralUI()
        {
            if (proceduralCanvasGo != null)
            {
                pausePanel = proceduralCanvasGo.transform.Find("PausePanel")?.gameObject;
                if (pausePanel != null)
                {
                    pausePanel.SetActive(false);
                    return;
                }
            }

            var font = Resources.Load<Font>("UI/AlegreyaSansSC-Bold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var borderSprite = Resources.Load<Sprite>("UI/panel-001");

            proceduralCanvasGo = new GameObject("ProceduralPauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(proceduralCanvasGo);

            var canvas = proceduralCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            var scaler = proceduralCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // PausePanel root
            var panelGo = new GameObject("PausePanel", typeof(RectTransform));
            panelGo.transform.SetParent(proceduralCanvasGo.transform, false);
            var panelRt = (RectTransform)panelGo.transform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            pausePanel = panelGo;

            // Dim overlay
            var dimGo = new GameObject("DimOverlay", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(panelRt, false);
            var dimRt = (RectTransform)dimGo.transform;
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dimGo.GetComponent<Image>();
            dimImg.color = new Color(0.015f, 0.04f, 0.07f, 0.88f);
            dimImg.raycastTarget = true;

            // Dialog Box
            var dialogGo = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
            dialogGo.transform.SetParent(panelRt, false);
            var dialogRt = (RectTransform)dialogGo.transform;
            dialogRt.anchorMin = dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRt.sizeDelta = new Vector2(560, 520);
            dialogRt.anchoredPosition = Vector2.zero;

            var dialogImg = dialogGo.GetComponent<Image>();
            if (borderSprite != null)
            {
                dialogImg.sprite = borderSprite;
                dialogImg.type = Image.Type.Sliced;
            }
            dialogImg.color = new Color(0.03f, 0.09f, 0.14f, 0.98f);

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(Outline));
            titleGo.transform.SetParent(dialogRt, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(500, 60);
            titleRt.anchoredPosition = new Vector2(0, 195);
            var titleText = titleGo.GetComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 44;
            titleText.text = "TẠM DỪNG";
            titleText.color = new Color(0.92f, 0.96f, 0.98f, 1f);
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleOutline = titleGo.GetComponent<Outline>();
            titleOutline.effectColor = new Color(0.01f, 0.04f, 0.08f, 0.9f);
            titleOutline.effectDistance = new Vector2(2, -2);

            // Subtitle
            var subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(dialogRt, false);
            var subRt = (RectTransform)subGo.transform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.sizeDelta = new Vector2(500, 32);
            subRt.anchoredPosition = new Vector2(0, 148);
            var subText = subGo.GetComponent<Text>();
            subText.font = font;
            subText.fontSize = 18;
            subText.text = "CHUYẾN THÁM HIỂM ĐANG TẠM DỪNG";
            subText.color = new Color(0.72f, 1f, 0.88f, 1f);
            subText.alignment = TextAnchor.MiddleCenter;

            // Buttons
            resumeButton = CreateButton(dialogRt, "ResumeButton", "TIẾP TỤC", 360, 56, new Vector2(0, 65), font, borderSprite);
            restartButton = CreateButton(dialogRt, "RestartButton", "CHƠI LẠI KHU VỰC", 360, 56, new Vector2(0, -5), font, borderSprite);
            mainMenuButton = CreateButton(dialogRt, "MainMenuButton", "VỀ MENU CHÍNH", 360, 56, new Vector2(0, -75), font, borderSprite);
            quitButton = CreateButton(dialogRt, "QuitButton", "THOÁT GAME", 360, 56, new Vector2(0, -145), font, borderSprite);

            BindButtons();
            pausePanel.SetActive(false);
        }

        private Button CreateButton(Transform parent, string name, string label, float width, float height, Vector2 pos, Font font, Sprite borderSprite)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = pos;

            var img = btnGo.GetComponent<Image>();
            if (borderSprite != null)
            {
                img.sprite = borderSprite;
                img.type = Image.Type.Sliced;
            }
            Color normalCol = new Color(0.04f, 0.14f, 0.20f, 0.95f);
            Color hoverCol = new Color(0.15f, 0.45f, 0.55f, 1f);
            Color pressedCol = new Color(0.028f, 0.098f, 0.14f, 1f);
            img.color = normalCol;

            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = normalCol;
            cb.highlightedColor = hoverCol;
            cb.pressedColor = pressedCol;
            cb.selectedColor = normalCol;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
            lblGo.transform.SetParent(btnGo.transform, false);
            var lrt = (RectTransform)lblGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var txt = lblGo.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = (int)Mathf.Clamp(height * 0.44f, 18, 26);
            txt.color = new Color(0.92f, 0.96f, 0.98f, 1f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = label;

            var outline = lblGo.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(1, -1);

            return btn;
        }

        private void EnsureInGamePauseButton(string sceneName)
        {
            bool isGameplay = sceneName.StartsWith("Zone") || sceneName == "GameplayCore";
            if (!isGameplay)
            {
                if (inGamePauseButtonGo != null) inGamePauseButtonGo.SetActive(false);
                return;
            }

            if (inGamePauseButtonGo != null)
            {
                inGamePauseButtonGo.SetActive(true);
                return;
            }

            if (proceduralCanvasGo == null) BuildProceduralUI();
            if (proceduralCanvasGo == null) return;

            var font = Resources.Load<Font>("UI/AlegreyaSansSC-Bold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var borderSprite = Resources.Load<Sprite>("UI/panel-001");

            var btnGo = new GameObject("InGamePauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(proceduralCanvasGo.transform, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(140, 44);
            rt.anchoredPosition = new Vector2(25, 25);

            var img = btnGo.GetComponent<Image>();
            if (borderSprite != null)
            {
                img.sprite = borderSprite;
                img.type = Image.Type.Sliced;
            }
            Color normalCol = new Color(0.04f, 0.14f, 0.20f, 0.95f);
            Color hoverCol = new Color(0.15f, 0.45f, 0.55f, 1f);
            Color pressedCol = new Color(0.028f, 0.098f, 0.14f, 1f);
            img.color = normalCol;

            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = normalCol;
            cb.highlightedColor = hoverCol;
            cb.pressedColor = pressedCol;
            cb.selectedColor = normalCol;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            btn.onClick.AddListener(OpenPause);

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
            lblGo.transform.SetParent(btnGo.transform, false);
            var lrt = (RectTransform)lblGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var txt = lblGo.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = 20;
            txt.color = new Color(0.92f, 0.96f, 0.98f, 1f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = "|| TẠM DỪNG";

            var outline = lblGo.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(1, -1);

            inGamePauseButtonGo = btnGo;
        }

        private void BindButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartZone);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(LoadMainMenu);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        public void OpenPause()
        {
            if (pausePanel == null) FindOrBuildUI();
            if (pausePanel == null || IsPaused) return;

            // Brake cabin if station is active
            var cabin = FindAnyObjectByType<CabinStationView>();
            if (cabin != null)
            {
                cabin.Brake();
            }

            AudioManager.Instance?.PlayPauseMenu();
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (pausePanel == null || !IsPaused) return;

            AudioManager.Instance?.PlayButtonClick();
            Time.timeScale = 1f;
            pausePanel.SetActive(false);
        }

        public void TogglePause()
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                OpenPause();
            }
        }

        public void RestartZone()
        {
            AudioManager.Instance?.PlayButtonClick();
            Time.timeScale = 1f;
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            string currentZone = "Zone01";
            if (SceneFlowController.Instance != null && !string.IsNullOrEmpty(SceneFlowController.Instance.CurrentZoneScene))
            {
                currentZone = SceneFlowController.Instance.CurrentZoneScene;
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    string sceneName = SceneManager.GetSceneAt(i).name;
                    if (sceneName.StartsWith("Zone"))
                    {
                        currentZone = sceneName;
                        break;
                    }
                }
            }

            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.LoadZone(currentZone);
            }
            else
            {
                SceneManager.LoadScene(currentZone);
            }
        }

        public void LoadMainMenu()
        {
            AudioManager.Instance?.PlayButtonClick();
            Time.timeScale = 1f;
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene(SceneFlowController.MainMenuScene);
            }
        }

        public void QuitGame()
        {
            AudioManager.Instance?.PlayButtonBack();
            Time.timeScale = 1f;

            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.QuitGame();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        public bool TryHandleBack()
        {
            if (IsPaused)
            {
                Resume();
                return true;
            }

            return false;
        }
    }
}

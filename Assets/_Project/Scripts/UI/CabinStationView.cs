using System.Collections;
using G10.Prototype.Audio;
using G10.Prototype.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using G10.Prototype.Computer;
using G10.Prototype.Tutorial;

namespace G10.Prototype.UI
{
    public sealed class CabinStationView : MonoBehaviour
    {
        [Header("Drag the four supplied images here")]
        [SerializeField] private Texture2D cabinArt;
        [SerializeField] private Texture2D captureCabinArt;
        [SerializeField] private Texture2D photoCabinArt;
        [SerializeField] private UnityEngine.UI.RawImage cabinBackground;
        [SerializeField] private Texture2D navigationArt;
        [SerializeField] private Texture2D chartArt;
        [SerializeField] private Texture2D radarArt;
        [Header("Scene references")]
        [SerializeField] private UIManager panelManager;
        [SerializeField] private ZoneNavigation navigation;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private GameObject navigationPanel;
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject radarPanel;
        [SerializeField] private GameObject cameraPanel;
        [SerializeField] private GameObject cargoPanel;
        [SerializeField] private GameObject capturePanel;
        [SerializeField] private Text hoverLabel;
        [SerializeField] private Text xReadout;
        [SerializeField] private Text yReadout;
        [SerializeField] private Text depthReadout;
        [SerializeField] private PhotoSurveyZone photoSurvey;
        [SerializeField] private Text headingReadout;
        [SerializeField] private Text navigationStatus;
        [SerializeField] private Text mapReadout;
        [SerializeField] private Text radarStatus;
        [SerializeField] private RectTransform compassNeedle;
        [SerializeField] private float compassArtOffset;
        [SerializeField] private RawImage[] pressedControls;
        [SerializeField] private RadarDisplay radarDisplay;
        [SerializeField] private ComputerScreenController computerScreen;

        private InputActionAsset ownedActions;
        private InputAction moveAction;
        private WorldMapController worldMap;
        private float readoutTimer;
        private int heldControl;
        private bool hasFocus = true;
        private bool applicationPaused;
        private bool waitingForNeutralInput;
        private CreatureCatcher directCatcher;
        private PhotoCaptureService directCamera;
        private TutorialManager tutorial;
        public bool AllowsStation(TutorialStation station)
        { bool allowed = tutorial == null || tutorial.Allows(station); if (!allowed) tutorial.NotifyLocked(); return allowed; }
        public bool AllowsComputerApp(ComputerAppId app)
        { bool allowed = tutorial == null || tutorial.AllowsComputerApp(app); if (!allowed) tutorial.NotifyLocked(); return allowed; }
        private Coroutine directRoutine;
        private bool directInteraction;
        public bool IsDirectInteractionActive => directInteraction;
        public UIManager Panels => panelManager;
        public ZoneNavigation Navigation => navigation;
        public GameObject NavigationPanel => navigationPanel;
        public GameObject MapPanel => mapPanel;
        public GameObject RadarPanel => radarPanel;
        public GameObject CargoPanel => cargoPanel;
        public Texture2D CabinArt => cabinArt;
        public Texture2D NavigationArt => navigationArt;
        public Texture2D ChartArt => chartArt;
        public Texture2D RadarArt => radarArt;
        public RadarDisplay Radar => radarDisplay;

        public void SetActiveMap(GameObject panel) => mapPanel = panel;

        private void Awake()
        {
            tutorial = GetComponent<TutorialManager>();
            directCatcher = GetComponent<CreatureCatcher>();
            directCamera = GetComponent<PhotoCaptureService>();
            worldMap = GetComponent<WorldMapController>();
            var mapPresentation = mapPanel != null ? mapPanel.GetComponent<ZoneMapPresentation>() : null;
            if (navigation != null && mapPresentation != null && mapPresentation.config != null)
                mapPresentation.config.ApplyTerrain(navigation, false);
            if (inputActions != null)
            {
                ownedActions = Instantiate(inputActions);
                moveAction = ownedActions.FindAction("Player/NavigateShip", true);
            }

        }

        private void OnEnable()
        {
            moveAction?.Enable();
            if (directCatcher != null) directCatcher.CaptureResolved += OnCaptureResolved;
        }
        private void Start()
        {
            ConfigureHelmStatus();
            // GameplayCore is loaded additively; Unity cannot serialize a cross-scene reference.
            if (panelManager == null) panelManager = FindAnyObjectByType<UIManager>();
            if (panelManager == null)
            {
                SceneManager.sceneLoaded += OnSceneLoadedForCore;
            }
        }

        private void ConfigureHelmStatus()
        {
            if (navigationPanel == null) return;
            // Older scenes may still contain the removed control until their UI migration is applied.
            var stop = navigationPanel.transform.Find("Brake");
            if (stop != null)
            {
                stop.gameObject.SetActive(false);
                Destroy(stop.gameObject);
            }
            if (navigation == null || navigationStatus == null) return;
            var card = navigationPanel.transform.Find("NavigationInfo");
            if (card == null) return;

            navigationStatus.transform.SetParent(card, false);
            var speedRect = navigationStatus.rectTransform;
            speedRect.anchorMin = Vector2.zero;
            speedRect.anchorMax = new Vector2(.5f, 1);
            speedRect.offsetMin = new Vector2(32, 6);
            speedRect.offsetMax = new Vector2(-24, -6);
            navigationStatus.alignment = TextAnchor.MiddleCenter;
            navigationStatus.resizeTextForBestFit = true;
            navigationStatus.resizeTextMinSize = 20;
            navigationStatus.resizeTextMaxSize = 26;
            navigationStatus.text = $"TỐC ĐỘ {navigation.Speed:0.0}";

            var divider = (RectTransform)new GameObject("StatusDivider", typeof(RectTransform), typeof(Image)).transform;
            divider.SetParent(card, false);
            divider.anchorMin = new Vector2(.5f, .24f);
            divider.anchorMax = new Vector2(.5f, .76f);
            divider.sizeDelta = new Vector2(2, 0);
            divider.anchoredPosition = Vector2.zero;
            var dividerImage = divider.GetComponent<Image>();
            dividerImage.color = new Color(.16f, .23f, .53f, .45f);
            dividerImage.raycastTarget = false;

            ShipEnergyBar.CreateInline(card, navigation, navigationStatus);
        }

        private void OnSceneLoadedForCore(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "GameplayCore")
            {
                SceneManager.sceneLoaded -= OnSceneLoadedForCore;
                if (panelManager == null) panelManager = FindAnyObjectByType<UIManager>();
                if (panelManager != null) enabled = true;
            }
        }

        private void OnDisable()
        {
            CancelDirectInteraction();
            if (directCatcher != null) directCatcher.CaptureResolved -= OnCaptureResolved;
            moveAction?.Disable();
            heldControl = 0;
            if (navigation != null) navigation.Brake();
            if (panelManager != null) panelManager.CloseCurrentPanel();
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedForCore;
            if (ownedActions != null) Destroy(ownedActions);
        }
        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused) Brake();
        }
        private void OnApplicationPause(bool paused)
        { applicationPaused = paused; if (paused) Brake(); }

        private void Update()
        {
            if (panelManager == null || navigation == null) return;
            Vector2 helmInput = Vector2.zero;
            if (hasFocus && !applicationPaused && panelManager.CurrentPanel == navigationPanel && navigationPanel.activeInHierarchy)
            {
                Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
                // Re-entering the helm must not resume a key held across the tab change.
                if (waitingForNeutralInput)
                {
                    if (input.sqrMagnitude < .0001f) waitingForNeutralInput = false;
                    input = Vector2.zero;
                }
                if (heldControl >= 1 && heldControl <= 4)
                    input = heldControl switch { 1 => Vector2.up, 2 => Vector2.down, 3 => Vector2.left, _ => Vector2.right };
                navigation.Navigate(input.y, input.x, heldControl == 5 ? -1 : heldControl == 6 ? 1 : 0, Time.deltaTime);
                helmInput = input;
            }
            else
            {
                // Includes Escape/direct UIManager transitions, not just our shortcut buttons.
                Brake();
            }

            UpdateControlArt(helmInput);
            if (compassNeedle != null)
                compassNeedle.localEulerAngles = new Vector3(0, 0, compassArtOffset - navigation.Heading);
            readoutTimer -= Time.unscaledDeltaTime;
            if (readoutTimer > 0f) return;
            readoutTimer = 0.08f;
            xReadout.text = navigation.Position.x.ToString("000.0");
            yReadout.text = navigation.Position.y.ToString("000.0");
            if (depthReadout != null) depthReadout.text = $"{navigation.Depth:0.0} m";
            headingReadout.text = navigation.Heading.ToString("000.0") + "°";
            navigationStatus.text = $"TỐC ĐỘ {navigation.Speed:0.0}" + (navigation.Obstructed ? "  ·  VẬT CẢN" : "");
            if (!navigation.Ship.CanMove) navigationStatus.text += navigation.Ship.Hull <= 0 ? "  ·  TÀU HỎNG" : "  ·  HẾT ENERGY";
            var progression = GetComponent<ExpeditionProgression>();
            if (progression != null && progression.ExitNearby) navigationStatus.text += "  ·  " + progression.ExitPrompt;
            else if (progression != null && progression.FinalNearby) navigationStatus.text += "  ·  " + progression.FinalPrompt;
            bool near = navigation.HasNearbyObstacle(12f);
            string scanInfo = radarDisplay != null && radarDisplay.IsScanning 
                ? "ĐANG QUÉT RADAR 360°..." 
                : radarDisplay != null && radarDisplay.VisibleContactCount > 0 
                ? "TÍN HIỆU VÀNG: SINH VẬT • XANH: ĐỊA HÌNH" 
                : "Bấm nút QUÉT để dò sóng radar 360°";
            radarStatus.text = (near ? "CẢNH BÁO: VẬT CẢN Ở GẦN" : "KHÔNG CÓ VẬT CẢN Ở SÁT TÀU") + "\n" +
                (radarDisplay != null && radarDisplay.LastError != null ? radarDisplay.LastError : scanInfo) + $" • LƯỢT QUÉT CÒN: {navigation.Ship.Radar}/{navigation.Ship.RadarCapacity}";
        }

        public void OpenNavigation() { if (AllowsStation(TutorialStation.Helm)) Open(navigationPanel); }
        public void OpenMap()
        {
            if (!AllowsStation(TutorialStation.Map)) return;
            if (directInteraction) return;
            if (panelManager != null && panelManager.IsModalOpen) return;
            if (worldMap != null && worldMap.worldPanel != null) worldMap.OpenRememberedMap();
            else Open(mapPanel);
        }
        public void OpenRadar() { if (AllowsStation(TutorialStation.Radar)) Open(radarPanel); }
        public void OpenCamera()
        {
            if (!AllowsStation(TutorialStation.Camera)) return;
            if (directCamera == null || !BeginDirectInteraction(photoCabinArt)) return;
            directRoutine = StartCoroutine(TakeCabinPhoto());
        }
        public void OpenCargo()
        {
            if (!AllowsStation(TutorialStation.Cargo)) return;
            if (directInteraction) return;
            if (panelManager != null && panelManager.IsModalOpen || computerScreen == null) return;
            OpenComputer();
            computerScreen.OpenCargo();
        }
        public void OpenCapture()
        {
            if (!AllowsStation(TutorialStation.Capture)) return;
            if (directCatcher == null || !BeginDirectInteraction(captureCabinArt)) return;
            directRoutine = StartCoroutine(StartCabinCapture());
        }
        private bool BeginDirectInteraction(Texture2D art)
        {
            if (!isActiveAndEnabled || directInteraction || art == null || cabinBackground == null ||
                navigation == null || navigation.ExpeditionBlocked || navigation.Ship.Hull <= 0 ||
                panelManager == null || panelManager.IsModalOpen || panelManager.LockedPanel != null ||
                PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) return false;
            Brake(); SetHover("");
            radarDisplay?.StopContinuousScan();
            panelManager.CloseCurrentPanel();
            directInteraction = true;
            cabinBackground.texture = art;
            return true;
        }
        private IEnumerator StartCabinCapture()
        {
            yield return new WaitForSecondsRealtime(1f);
            directRoutine = null;
            if (navigation.ExpeditionBlocked || directCatcher.TryCapture() != CreatureCatcher.Result.Started)
                FinishDirectInteraction();
        }
        private IEnumerator TakeCabinPhoto()
        {
            // Let the swapped art render for a frame; the existing service still composes/stores the photograph.
            yield return null;
            try
            {
                if (!navigation.ExpeditionBlocked && directCamera.Capture() != null)
                    AudioManager.Instance?.PlayCameraShutter();
            }
            finally { directRoutine = null; FinishDirectInteraction(); }
        }
        private void OnCaptureResolved(CreatureCatcher.Result result)
        {
            if (directInteraction && result != CreatureCatcher.Result.Started && result != CreatureCatcher.Result.Busy)
                FinishDirectInteraction();
        }
        private void FinishDirectInteraction()
        {
            directInteraction = false;
            if (cabinBackground != null) cabinBackground.texture = cabinArt;
        }
        public void CancelDirectInteraction()
        {
            if (directRoutine != null) { StopCoroutine(directRoutine); directRoutine = null; }
            if (directInteraction) directCatcher?.minigame?.Cancel();
            FinishDirectInteraction();
        }
        public void OpenComputer()
        {
            if (!AllowsStation(TutorialStation.Computer)) return;
            if (directInteraction) return;
            if (panelManager != null && panelManager.IsModalOpen) return;
            if (computerScreen == null) return;
            Open(computerScreen.gameObject);
            computerScreen.ShowDesktop();
        }
        private void Open(GameObject panel)
        {
            if (directInteraction) return;
            if (panelManager == null) panelManager = FindAnyObjectByType<UIManager>();
            if (panelManager != null && panelManager.IsModalOpen) return;
            Brake();
            SetHover("");
            if (mapReadout != null) mapReadout.text = "Rê chuột trên bản đồ để đọc tọa độ";
            AudioManager.Instance?.PlayButtonClick();
            if (panelManager != null) panelManager.OpenPanel(panel);
        }
        public void ClosePanel()
        {
            if (directInteraction) return;
            if (panelManager == null) panelManager = FindAnyObjectByType<UIManager>();
            if (panelManager != null && panelManager.IsModalOpen) return;
            if (radarDisplay != null) radarDisplay.StopContinuousScan();
            Brake();
            SetHover("");
            AudioManager.Instance?.PlayButtonBack();
            if (panelManager != null) panelManager.CloseCurrentPanel();
        }
        public void OpenPause()
        {
            Brake();
            SetHover("");
            PauseMenuController.Instance?.OpenPause();
        }
        public void Scan() { if (!directInteraction && (panelManager == null || !panelManager.IsModalOpen)) radarDisplay?.Scan(); }
        public void Brake()
        { heldControl = 0; waitingForNeutralInput = true; UpdateControlArt(Vector2.zero); if (navigation != null) navigation.Brake(); }
        private void UpdateControlArt(Vector2 input)
        {
            if (pressedControls == null) return;
            for (int i = 0; i < pressedControls.Length; i++)
            {
                bool pressed = i switch { 0 => input.y > .01f, 1 => input.y < -.01f,
                    2 => input.x < -.01f, 3 => input.x > .01f, 4 => heldControl == 5,
                    5 => heldControl == 6, _ => false };
                if (pressedControls[i] != null) pressedControls[i].enabled = pressed;
            }
        }
        public void Hold(int command)
        {
            if (hasFocus && !applicationPaused && panelManager != null && panelManager.CurrentPanel == navigationPanel)
                heldControl = command;
        }
        public void Release(int command) { if (heldControl == command) heldControl = 0; }
        public void SetHover(string value) { hoverLabel.text = value; hoverLabel.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(value)); }
        public void ShowChartCoordinate(Vector2 uv)
        {
            if (mapReadout == null) return;
            ZoneMapConfig config = mapPanel != null ? mapPanel.GetComponentInChildren<PhotoSurveyMap>(true)?.mapConfig : null;
            Vector2 worldSize = navigation != null ? navigation.MapWorldSize :
                config != null ? config.WorldSize : Vector2.one * ZoneNavigation.DefaultGridSize;
            Vector2 coordinate = navigation != null ? navigation.NormalizedToMapCoordinates(uv) : ZoneNavigation.UVToCoordinates(uv, worldSize);
            if (coordinate.x < 0 || coordinate.x >= worldSize.x || coordinate.y < 0 || coordinate.y >= worldSize.y)
            { ClearChartCoordinate(); return; }
            var poi = photoSurvey != null ? photoSurvey.FindPoiContaining(coordinate) : null;
            string area = poi != null ? $" • VÙNG CHỤP P01 • SÂU {photoSurvey.DepthFor(poi):0} m" : "";
            mapReadout.text = $"X {coordinate.x:000.0}   Y {coordinate.y:000.0}" + area;
        }
        public void ClearChartCoordinate()
        { if (mapReadout != null) mapReadout.text = "Rê chuột trên bản đồ để đọc tọa độ"; }
    }
}

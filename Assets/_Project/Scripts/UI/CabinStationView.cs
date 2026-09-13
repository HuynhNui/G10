using G10.Prototype.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using G10.Prototype.Computer;

namespace G10.Prototype.UI
{
    public sealed class CabinStationView : MonoBehaviour
    {
        [Header("Drag the four supplied images here")]
        [SerializeField] private Texture2D cabinArt;
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
        [SerializeField] private RadarDisplay radarDisplay;
        [SerializeField] private ComputerScreenController computerScreen;

        private InputActionAsset ownedActions;
        private InputAction moveAction;
        private float readoutTimer;
        private int heldControl;
        private bool hasFocus = true;
        private bool applicationPaused;
        private bool waitingForNeutralInput;
        public UIManager Panels => panelManager;
        public ZoneNavigation Navigation => navigation;
        public GameObject NavigationPanel => navigationPanel;
        public GameObject MapPanel => mapPanel;
        public GameObject RadarPanel => radarPanel;
        public Texture2D CabinArt => cabinArt;
        public Texture2D NavigationArt => navigationArt;
        public Texture2D ChartArt => chartArt;
        public Texture2D RadarArt => radarArt;
        public RadarDisplay Radar => radarDisplay;

        private void Awake()
        {
            if (inputActions != null)
            {
                ownedActions = Instantiate(inputActions);
                moveAction = ownedActions.FindAction("Player/NavigateShip", true);
            }
        }

        private void OnEnable() => moveAction?.Enable();
        private void Start()
        {
            // GameplayCore is loaded additively; Unity cannot serialize a cross-scene reference.
            if (panelManager == null) panelManager = FindAnyObjectByType<UIManager>();
            if (panelManager == null)
            {
                Debug.LogError("Cabin needs GameplayCore loaded alongside Zone01.", this);
                enabled = false;
            }
        }
        private void OnDisable()
        {
            moveAction?.Disable();
            heldControl = 0;
            if (navigation != null) navigation.Brake();
            if (panelManager != null) panelManager.CloseCurrentPanel();
        }
        private void OnDestroy()
        {
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
                navigation.Step(input.y, input.x, Mathf.Min(Time.deltaTime, 0.1f));
                navigation.StepDepth(heldControl == 5 ? -1 : heldControl == 6 ? 1 : 0, Mathf.Min(Time.deltaTime, 0.1f));
            }
            else
            {
                // Includes Escape/direct UIManager transitions, not just our shortcut buttons.
                Brake();
            }

            readoutTimer -= Time.unscaledDeltaTime;
            if (readoutTimer > 0f) return;
            readoutTimer = 0.08f;
            xReadout.text = navigation.Position.x.ToString("000.0");
            yReadout.text = navigation.Position.y.ToString("000.0");
            if (depthReadout != null) depthReadout.text = $"{navigation.Depth:0.0} m";
            headingReadout.text = navigation.Heading.ToString("000.0") + "°";
            compassNeedle.localEulerAngles = new Vector3(0, 0, -navigation.Heading);
            navigationStatus.text = navigation.Obstructed ? "VẬT CẢN — HÃY ĐỔI HƯỚNG" : $"Tốc độ {navigation.Speed:0.0}  •  0° Bắc / 90° Đông";
            bool near = navigation.HasNearbyObstacle(12f);
            radarStatus.text = (near ? "CẢNH BÁO: VẬT CẢN Ở GẦN" : "KHÔNG CÓ VẬT CẢN Ở SÁT TÀU") + "\n" +
                (radarDisplay.IsScanning ? "Đang quét…" : radarDisplay.VisibleContactCount > 0 ? "TÍN HIỆU VÀNG: SINH VẬT • XANH: ĐỊA HÌNH" : "QUÉT: tìm sinh vật (vàng) và địa hình (xanh)");
        }

        public void OpenNavigation() => Open(navigationPanel);
        public void OpenMap() => Open(mapPanel);
        public void OpenRadar() => Open(radarPanel);
        public void OpenCamera() => Open(cameraPanel);
        public void OpenCargo() => Open(cargoPanel);
        public void OpenCapture() => Open(capturePanel);
        public void OpenComputer()
        {
            if (computerScreen == null) return;
            Open(computerScreen.gameObject);
            computerScreen.ShowDesktop();
        }
        private void Open(GameObject panel)
        {
            Brake();
            SetHover("");
            mapReadout.text = "Rê chuột trên bản đồ để đọc tọa độ";
            panelManager.OpenPanel(panel);
        }
        public void ClosePanel()
        {
            Brake();
            SetHover("");
            panelManager.CloseCurrentPanel();
        }
        public void Scan() => radarDisplay.Scan();
        public void Brake()
        { heldControl = 0; waitingForNeutralInput = true; if (navigation != null) navigation.Brake(); }
        public void Hold(int command)
        {
            if (hasFocus && !applicationPaused && panelManager != null && panelManager.CurrentPanel == navigationPanel)
                heldControl = command;
        }
        public void Release(int command) { if (heldControl == command) heldControl = 0; }
        public void SetHover(string value) { hoverLabel.text = value; hoverLabel.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(value)); }
        public void ShowChartCoordinate(Vector2 uv)
        {
            Vector2 coordinate = ZoneNavigation.UVToCoordinates(uv);
            if (coordinate.x < 0 || coordinate.x >= 1200 || coordinate.y < 0 || coordinate.y >= 700)
            { ClearChartCoordinate(); return; }
            string area = photoSurvey != null && photoSurvey.Contains(coordinate) ? $" • VÙNG CHỤP P01 • SÂU {photoSurvey.targetDepth:0} m" : "";
            mapReadout.text = $"X {coordinate.x:000.0}   Y {coordinate.y:000.0}" + area;
        }
        public void ClearChartCoordinate() => mapReadout.text = "Rê chuột trên bản đồ để đọc tọa độ";
    }
}

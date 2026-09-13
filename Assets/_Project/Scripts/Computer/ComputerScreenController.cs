using System;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    public enum ComputerAppId { Desktop, PhotoLab, ShipStatus, MissionLog }

    [Serializable]
    public sealed class ComputerAppPanel
    {
        public ComputerAppId id;
        public GameObject panel;
    }

    /// <summary>One modal panel in the existing UIManager; apps never load scenes or pause time.</summary>
    public sealed class ComputerScreenController : MonoBehaviour, IPanelBackHandler
    {
        [SerializeField] private CabinStationView cabin;
        [SerializeField] private GameObject desktop;
        [SerializeField] private ComputerAppPanel[] apps;
        private Font screenFont;
        public ComputerAppId CurrentApp { get; private set; }
        public GameObject Desktop => desktop;
        public ComputerAppPanel[] Apps => apps;

        private void Awake()
        {
            screenFont = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Liberation Mono", "Courier New" }, 28);
            if (screenFont != null)
                foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = screenFont;
        }
        private void OnDestroy() { if (screenFont != null) Destroy(screenFont); }
        public void OpenPhotoLab() => OpenApp(ComputerAppId.PhotoLab);
        public void OpenShipStatus() => OpenApp(ComputerAppId.ShipStatus);
        public void OpenMissionLog() => OpenApp(ComputerAppId.MissionLog);
        public void ShowDesktop() => OpenApp(ComputerAppId.Desktop);
        public void OpenApp(ComputerAppId id)
        {
            if (id != ComputerAppId.Desktop && Array.Find(apps, app => app.id == id && app.panel != null) == null) return;
            CurrentApp = id;
            desktop.SetActive(id == ComputerAppId.Desktop);
            foreach (ComputerAppPanel app in apps) app.panel.SetActive(app.id == id);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        public bool TryHandleBack()
        {
            if (CurrentApp == ComputerAppId.Desktop) return false;
            ShowDesktop();
            return true;
        }
        public void Exit() => cabin.ClosePanel();
    }
}

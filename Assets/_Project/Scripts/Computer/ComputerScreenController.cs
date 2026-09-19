using System;
using System.Collections.Generic;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    public enum ComputerAppId { Desktop, PhotoLab, ShipStatus, MissionLog, Journal, Rest }

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
        public ExpeditionLoop Expedition { get; set; }
        public bool DesktopWindows { get; set; }
        public event Action WindowStateChanged;
        private readonly List<ComputerAppId> running = new();
        private readonly HashSet<ComputerAppId> minimized = new();
        public bool IsRunning(ComputerAppId id) => running.Contains(id);
        public bool IsMinimized(ComputerAppId id) => minimized.Contains(id);
        public void RegisterApp(ComputerAppId id, GameObject panel)
        {
            if (Array.Exists(apps, a=>a.id==id)) return;
            Array.Resize(ref apps,apps.Length+1); apps[apps.Length-1]=new ComputerAppPanel { id=id,panel=panel };
        }

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
            if (cabin != null && cabin.Panels != null && cabin.Panels.IsModalOpen) return;
            if (id != ComputerAppId.Desktop && Array.Find(apps, app => app.id == id && app.panel != null) == null) return;
            if (DesktopWindows)
            {
                desktop.SetActive(true);
                if (id == ComputerAppId.Desktop)
                {
                    if (Expedition != null && Expedition.Blocked) { OpenApp(ComputerAppId.Journal); return; }
                    foreach (var app in apps) if (IsRunning(app.id)) { minimized.Add(app.id); app.panel.SetActive(false); }
                    CurrentApp = ComputerAppId.Desktop;
                }
                else
                {
                    running.Remove(id); running.Add(id); minimized.Remove(id);
                    var app = Array.Find(apps, a => a.id == id);
                    app.panel.SetActive(true); app.panel.transform.SetAsLastSibling(); CurrentApp = id;
                }
                WindowStateChanged?.Invoke();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                return;
            }
            CurrentApp = id;
            desktop.SetActive(id == ComputerAppId.Desktop);
            foreach (ComputerAppPanel app in apps) app.panel.SetActive(app.id == id);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        public void CloseApp(ComputerAppId id)
        {
            if (Expedition != null && Expedition.Blocked && id == ComputerAppId.Journal) return;
            running.Remove(id); minimized.Remove(id);
            var app = Array.Find(apps, a => a.id == id); if (app != null) app.panel.SetActive(false);
            FocusLastWindow();
        }
        public void MinimizeApp(ComputerAppId id)
        {
            if (!IsRunning(id) || Expedition != null && Expedition.Blocked && id == ComputerAppId.Journal) return;
            minimized.Add(id);
            var app = Array.Find(apps, a => a.id == id); if (app != null) app.panel.SetActive(false);
            FocusLastWindow();
        }
        public void ToggleTaskbarApp(ComputerAppId id)
        { if (CurrentApp == id && !IsMinimized(id)) MinimizeApp(id); else OpenApp(id); }
        private void FocusLastWindow()
        {
            CurrentApp = ComputerAppId.Desktop;
            for (int i = running.Count - 1; i >= 0; i--)
                if (!minimized.Contains(running[i])) { OpenApp(running[i]); return; }
            WindowStateChanged?.Invoke();
        }
        public bool TryHandleBack()
        {
            if (Expedition != null && Expedition.Blocked) { OpenApp(ComputerAppId.Journal); return true; }
            if (CurrentApp == ComputerAppId.Desktop) return false;
            if (DesktopWindows) { CloseApp(CurrentApp); return true; }
            ShowDesktop();
            return true;
        }
        public void Exit() => cabin.ClosePanel();
    }
}

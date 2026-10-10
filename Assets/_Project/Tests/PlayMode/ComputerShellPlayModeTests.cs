using System.Collections;
using System.Collections.Generic;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace G10.Prototype.Tests
{
    public sealed class ComputerShellPlayModeTests
    {
        private LegacyCabinTestSession session;
        [UnitySetUp] public IEnumerator Setup()
        { session = new LegacyCabinTestSession(); yield return session.Begin(); }

        [UnityTest]
        public IEnumerator ShellBlocksCabinKeepsVoyageAndRoutesEscape()
        {
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            Assert.That(screen, Is.Not.Null);
            int sceneCount = SceneManager.sceneCount;
            float timeScale = Time.timeScale;
            Transform frame = cabin.transform.Find("CabinCanvas/CabinFrame");
            // Exercise the scene's persistent monitor binding.
            var monitorButton = frame.GetComponentsInChildren<Button>(true).Single(button => button.name == "MonitorHotspot");
            monitorButton.onClick.Invoke();
            Assert.That(cabin.Panels.CurrentPanel, Is.EqualTo(screen.gameObject));
            Button[] desktopButtons = screen.Desktop.GetComponentsInChildren<Button>();
            Assert.That(desktopButtons.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(desktopButtons.Any(button => button.name == "UpgradeIcon"), Is.True);
            Assert.That(screen.DesktopWindows, Is.True, "Exercise the currently shipped multiple-window desktop.");
            var photoApp = screen.Apps.Single(app => app.id == ComputerAppId.PhotoLab);
            var statusApp = screen.Apps.Single(app => app.id == ComputerAppId.ShipStatus);
            screen.OpenPhotoLab(); screen.OpenShipStatus();
            Assert.That(photoApp.panel.activeInHierarchy, Is.True); Assert.That(statusApp.panel.activeInHierarchy, Is.True);
            Assert.That(screen.IsRunning(ComputerAppId.PhotoLab), Is.True); Assert.That(screen.IsRunning(ComputerAppId.ShipStatus), Is.True);
            Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.ShipStatus));
            Assert.That(statusApp.panel.transform.GetSiblingIndex(), Is.GreaterThan(photoApp.panel.transform.GetSiblingIndex()));
            screen.MinimizeApp(ComputerAppId.ShipStatus);
            Assert.That(screen.IsRunning(ComputerAppId.ShipStatus), Is.True); Assert.That(screen.IsMinimized(ComputerAppId.ShipStatus), Is.True);
            Assert.That(statusApp.panel.activeInHierarchy, Is.False); Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
            screen.OpenShipStatus(); Assert.That(screen.IsMinimized(ComputerAppId.ShipStatus), Is.False);
            statusApp.panel.transform.Find("Back").GetComponent<Button>().onClick.Invoke();
            Assert.That(screen.IsRunning(ComputerAppId.ShipStatus), Is.False); Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
            photoApp.panel.transform.Find("Back").GetComponent<Button>().onClick.Invoke();
            Assert.That(screen.IsRunning(ComputerAppId.PhotoLab), Is.False); Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.Desktop));
            foreach (ComputerAppPanel app in screen.Apps)
            {
                screen.OpenApp(app.id);
                Assert.That(app.panel.activeInHierarchy, Is.True);
                Assert.That(screen.CurrentApp, Is.EqualTo(app.id)); Assert.That(screen.IsRunning(app.id), Is.True);
                Assert.That(screen.IsMinimized(app.id), Is.False); Assert.That(screen.Desktop.activeInHierarchy, Is.True);
                foreach (ComputerAppPanel other in screen.Apps)
                    Assert.That(other.panel.activeInHierarchy, Is.EqualTo(screen.IsRunning(other.id) && !screen.IsMinimized(other.id)));
                app.panel.transform.Find("Back").GetComponent<Button>().onClick.Invoke();
                Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.Desktop));
                Assert.That(screen.IsRunning(app.id), Is.False); Assert.That(app.panel.activeInHierarchy, Is.False);
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            // The foreground raycast at a cabin hotspot must belong to the computer.
            var monitor = (RectTransform)monitorButton.transform;
            var pointer = new PointerEventData(EventSystem.current)
            { position = RectTransformUtility.WorldToScreenPoint(null, monitor.TransformPoint(monitor.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.transform.IsChildOf(screen.transform), Is.True);

            cabin.OpenNavigation(); cabin.Navigation.Step(1, 0, 0.5f);
            Vector2 position = cabin.Navigation.Position;
            float voyageHeading = cabin.Navigation.Heading, depth = cabin.Navigation.Depth;
            float distance = cabin.Navigation.DistanceTravelled, energy = cabin.Navigation.Ship.Energy;
            cabin.ClosePanel(); cabin.OpenComputer();
            yield return new WaitForSeconds(0.25f);
            Assert.That(cabin.Navigation.Speed, Is.Zero, "Leaving Helm brakes instead of coasting behind a foreground UI.");
            Assert.That(cabin.Navigation.Position, Is.EqualTo(position));
            Assert.That(cabin.Navigation.Heading, Is.EqualTo(voyageHeading)); Assert.That(cabin.Navigation.Depth, Is.EqualTo(depth));
            Assert.That(cabin.Navigation.DistanceTravelled, Is.EqualTo(distance)); Assert.That(cabin.Navigation.Ship.Energy, Is.EqualTo(energy));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));

            // Synthetic input must not be discarded when the automated Editor runs unfocused.
            var originalSettings = InputSystem.settings;
            var testSettings = Object.Instantiate(originalSettings);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = testSettings;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                float heading = cabin.Navigation.Heading;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                yield return new WaitForSeconds(0.15f);
                Assert.That(cabin.Navigation.Heading, Is.EqualTo(heading));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                screen.OpenPhotoLab();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return null; yield return null;
                Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.Desktop));
                Assert.That(cabin.Panels.IsPanelOpen, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return null; yield return null;
                Assert.That(cabin.Panels.IsPanelOpen, Is.False);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings = originalSettings;
                Object.Destroy(testSettings);
            }
            cabin.OpenComputer(); screen.Exit();
            Assert.That(cabin.Panels.IsPanelOpen, Is.False);
            cabin.OpenRadar(); cabin.Scan();
            Assert.That(cabin.Radar.IsScanning, Is.True);
            cabin.Brake();
        }

        [UnityTearDown]
        public IEnumerator Cleanup() { if (session != null) yield return session.End(); }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Core;
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
        [UnityTest]
        public IEnumerator ShellBlocksCabinKeepsVoyageAndRoutesEscape()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
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
            Assert.That(screen.Desktop.GetComponentsInChildren<Button>().Length, Is.EqualTo(3));
            foreach (ComputerAppPanel app in screen.Apps)
            {
                screen.OpenApp(app.id);
                Assert.That(app.panel.activeInHierarchy, Is.True);
                foreach (ComputerAppPanel other in screen.Apps)
                    Assert.That(other.panel.activeInHierarchy, Is.EqualTo(other.id == app.id));
                app.panel.transform.Find("Back").GetComponent<Button>().onClick.Invoke();
                Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.Desktop));
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
            float speed = cabin.Navigation.Speed;
            Vector2 position = cabin.Navigation.Position;
            cabin.ClosePanel(); cabin.OpenComputer();
            yield return new WaitForSeconds(0.25f);
            Assert.That(cabin.Navigation.Speed, Is.EqualTo(speed).Within(0.01f));
            Assert.That(cabin.Navigation.Position.y, Is.GreaterThan(position.y));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));

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
            finally { InputSystem.RemoveDevice(keyboard); }
            cabin.OpenComputer(); screen.Exit();
            Assert.That(cabin.Panels.IsPanelOpen, Is.False);
            cabin.OpenRadar(); cabin.Scan();
            Assert.That(cabin.Radar.IsScanning, Is.True);
            cabin.Brake();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (SceneFlowController.Instance != null)
            { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
        }
    }
}

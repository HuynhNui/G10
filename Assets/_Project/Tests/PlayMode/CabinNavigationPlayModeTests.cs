using System.Collections;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace G10.Prototype.Tests
{
    public sealed class CabinNavigationPlayModeTests
    {
        [Test]
        public void ChartCoordinatesAgreeWithPrintedTicks()
        {
            Vector2 origin = ZoneNavigation.UVToCoordinates(new Vector2(48f / 1672f, 76f / 941f));
            Vector2 tick = ZoneNavigation.UVToCoordinates(new Vector2(1620f / 1672f, 838f / 941f));
            Assert.That(origin.magnitude, Is.LessThan(0.001f));
            Assert.That(Vector2.Distance(tick, new Vector2(1200, 700)), Is.LessThan(0.01f));
        }

        [Test]
        public void HelmTurnsMovesReversesAndStopsAtBoundary()
        {
            GameObject go = new("Navigation test");
            try
            {
                ZoneNavigation nav = go.AddComponent<ZoneNavigation>();
                byte[] water = new byte[320 * 180];
                for (int i = 0; i < water.Length; i++) water[i] = 1;
                nav.SetChart(water, 320, 180);
                nav.ResetVoyage();
                nav.Step(1, 0, 1);
                Assert.That(nav.Position.y, Is.GreaterThan(100));
                Assert.That(nav.Position.x, Is.EqualTo(600).Within(0.01));
                nav.Brake();
                nav.Step(0, 1, 2.25f);
                Assert.That(nav.Heading, Is.EqualTo(90).Within(0.01));
                nav.Step(1, 0, 1);
                Assert.That(nav.Position.x, Is.GreaterThan(600));
                float x = nav.Position.x;
                nav.Brake(); nav.Step(-1, 0, 1);
                Assert.That(nav.Position.x, Is.LessThan(x));
                nav.Brake();
                Vector2 stopped = nav.Position;
                nav.Step(0, 0, 1);
                Assert.That(nav.Position, Is.EqualTo(stopped));
                nav.Step(1, 0, 100);
                Assert.That(nav.Position.x, Is.LessThanOrEqualTo(1198));
                Assert.That(nav.Obstructed, Is.True);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [UnityTest]
        public IEnumerator Zone01CabinOpensPanelsAndKeepsNavigationState()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            CabinStationView view = Object.FindAnyObjectByType<CabinStationView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Panels, Is.Not.Null);
            Assert.That(view.Navigation.HasChart, Is.True);
            Assert.That(view.Navigation.CanOccupy(view.Navigation.Position), Is.True, "Starting location must be inside open water.");
            view.OpenRadar();
            Assert.That(view.Panels.CurrentPanel, Is.EqualTo(view.RadarPanel));
            view.Scan();
            view.OpenMap();
            Assert.That(view.RadarPanel.activeSelf, Is.False);
            Assert.That(view.MapPanel.activeSelf, Is.True);
            view.OpenNavigation();
            Vector2 start = view.Navigation.Position;
            var forward = view.NavigationPanel.transform.Find("ForwardHotspot").gameObject;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(forward, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(0.5f);
            ExecuteEvents.Execute(forward, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(view.Navigation.Position.y, Is.GreaterThan(start.y));
            view.Brake();
            Vector2 moved = view.Navigation.Position;
            view.ClosePanel();
            yield return null;
            Assert.That(view.Panels.IsPanelOpen, Is.False);
            Assert.That(view.Navigation.Speed, Is.Zero);
            view.OpenNavigation();
            Assert.That(view.Navigation.Position, Is.EqualTo(moved));
            view.OpenCargo(); view.ClosePanel();
            view.OpenCamera(); view.ClosePanel();
            view.OpenCapture(); view.ClosePanel();

            // Exercise the real action bindings through a temporary Input System device.
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                view.OpenNavigation();
                Vector2 beforeKeys = view.Navigation.Position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return new WaitForSeconds(0.5f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(view.Navigation.Position.y, Is.GreaterThan(beforeKeys.y));
                view.Brake();
                float beforeReverse = view.Navigation.Position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                yield return new WaitForSeconds(0.5f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(view.Navigation.Position.y, Is.LessThan(beforeReverse));
                view.Brake();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                yield return new WaitForSeconds(0.5f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(view.Navigation.Heading, Is.GreaterThan(5));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(view.Panels.IsPanelOpen, Is.False, "Escape must close the helm.");
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (SceneFlowController.Instance != null)
            { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
        }
    }
}

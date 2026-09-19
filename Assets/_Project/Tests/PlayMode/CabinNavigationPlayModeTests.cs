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
using UnityEngine.UI;

namespace G10.Prototype.Tests
{
    public sealed class CabinNavigationPlayModeTests
    {
        [Test]
        public void ChartCoordinatesCoverEntireGameplayImage()
        {
            Vector2 origin = ZoneNavigation.UVToCoordinates(Vector2.zero);
            Vector2 tick = ZoneNavigation.UVToCoordinates(Vector2.one);
            Assert.That(origin.magnitude, Is.LessThan(0.001f));
            Assert.That(Vector2.Distance(tick, new Vector2(1200, 700)), Is.LessThan(0.01f));
            Vector2 point = new(275, 425);
            Assert.That(Vector2.Distance(ZoneNavigation.UVToCoordinates(ZoneNavigation.CoordinatesToUV(point)), point), Is.LessThan(.001f));
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
            view.GetComponent<WorldMapController>().OpenZone(0);
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
                // The helm requires a neutral frame after a panel transition.
                yield return new WaitForSeconds(.1f);
                Vector2 beforeKeys = view.Navigation.Position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return new WaitForSeconds(0.5f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(view.Navigation.Position.y, Is.GreaterThan(beforeKeys.y));
                view.Brake();
                yield return new WaitForSeconds(.1f);
                float beforeReverse = view.Navigation.Position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                yield return new WaitForSeconds(0.5f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(view.Navigation.Position.y, Is.LessThan(beforeReverse));
                view.Brake();
                yield return new WaitForSeconds(.1f);
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

        [UnityTest]
        public IEnumerator AuthoredHelmAndFullChartStayAligned()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            var view = Object.FindAnyObjectByType<CabinStationView>();
            view.OpenNavigation();
            yield return null;
            Assert.That(view.NavigationArt.name, Is.EqualTo("Idle_No_Compassneedle"));
            var helm = view.NavigationPanel.transform;
            Assert.That(helm.Find("CompassCenter").gameObject.activeSelf, Is.False);
            float before = helm.Find("HeadingPivot").localEulerAngles.z;
            view.Hold(4);
            yield return new WaitForSeconds(.25f);
            Assert.That(helm.Find("PressedTurn_Right").GetComponent<RawImage>().enabled, Is.True);
            Assert.That(Mathf.DeltaAngle(before, helm.Find("HeadingPivot").localEulerAngles.z), Is.LessThan(-1));
            view.Release(4);
            yield return null;
            Assert.That(helm.Find("PressedTurn_Right").GetComponent<RawImage>().enabled, Is.False);
            view.Hold(5);
            float depth = view.Navigation.Depth;
            yield return new WaitForSeconds(.2f);
            Assert.That(view.Navigation.Depth, Is.LessThan(depth));
            Assert.That(helm.Find("PressedDepth_Up").GetComponent<RawImage>().enabled, Is.True);
            view.Brake();
            yield return CaptureArt(view, "helm-art.png");
            view.OpenMap();
            view.GetComponent<WorldMapController>().OpenZone(0);
            yield return null;
            var chart = view.MapPanel.transform.Find("SquareChartContent").GetComponent<RawImage>();
            Assert.That(chart.texture.name, Is.EqualTo("Mapingame"));
            Assert.That(chart.uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
            Assert.That(chart.rectTransform.rect.width / 24, Is.EqualTo(chart.rectTransform.rect.height / 14).Within(.01f));
            Assert.That(chart.GetComponentInChildren<PhotoSurveyMap>().locationIcon.texture.name, Is.EqualTo("location"));
            foreach (var overlay in view.GetComponentsInChildren<PhotoSurveyMap>(true))
            {
                Assert.That(overlay.locationIcons.Length, Is.EqualTo(3));
                foreach (var icon in overlay.locationIcons)
                {
                    Assert.That(icon.enabled, Is.True);
                    Assert.That(icon.rectTransform.rect.width, Is.EqualTo(overlay.rectTransform.rect.width / 24f).Within(.01f));
                    Assert.That(icon.rectTransform.rect.height, Is.EqualTo(overlay.rectTransform.rect.height / 14f).Within(.01f));
                }
                Vector2[] expected = { new(625,475), new(725,175), new(275,75) };
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(overlay.Locations[i].mapPosition, Is.EqualTo(expected[i]));
                    Assert.That(overlay.locationIcons[i].rectTransform.anchorMin, Is.EqualTo(ZoneNavigation.CoordinatesToUV(expected[i])));
                    Assert.That(overlay.locationTasks[i].tasks, Is.Empty);
                }
            }
            Assert.That(view.MapPanel.transform.Find("ChartOuterFrame"), Is.Not.Null);
            yield return CaptureArt(view, "map-art.png");
            var mapOverlay = chart.GetComponentInChildren<PhotoSurveyMap>();
            for (int i = 0; i < 3; i++)
            {
                Vector2 center = mapOverlay.Locations[i].mapPosition;
                foreach (Vector2 offset in new[] { Vector2.zero, new Vector2(-10,-10), new Vector2(10,10) })
                {
                    Vector2 uv = ZoneNavigation.CoordinatesToUV(center + offset);
                    Vector2 local = chart.rectTransform.rect.min + Vector2.Scale(uv, chart.rectTransform.rect.size);
                    chart.GetComponent<CabinPointerTarget>().OnPointerMove(new PointerEventData(EventSystem.current)
                        { position = RectTransformUtility.WorldToScreenPoint(null, chart.rectTransform.TransformPoint(local)) });
                    Assert.That(mapOverlay.taskReadout.transform.parent.gameObject.activeSelf, Is.True);
                    Assert.That(mapOverlay.taskReadout.text, Is.EqualTo(i == 2 ? mapOverlay.survey.TaskDescription() : $"ĐỊA ĐIỂM {i + 1:00} • NHIỆM VỤ (0)"));
                }
            }
            yield return CaptureArt(view, "map-location-hover.png");
            chart.GetComponent<CabinPointerTarget>().OnPointerExit(new PointerEventData(EventSystem.current));
            Assert.That(mapOverlay.taskReadout.transform.parent.gameObject.activeSelf, Is.False);
            mapOverlay.SetPointer(new Vector2(.99f,.99f));
            Assert.That(mapOverlay.taskReadout.transform.parent.gameObject.activeSelf, Is.False);
            view.OpenRadar(); view.Scan();
            yield return new WaitForSeconds(1.5f);
            yield return CaptureArt(view, "radar-art.png");
        }

        internal static IEnumerator CaptureArt(CabinStationView view, string filename)
        {
            // ScreenCapture does not render an overlay canvas in batchmode.
            var canvas = view.GetComponentInChildren<Canvas>();
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
            var go = new GameObject("Artwork validation camera", typeof(Camera));
            var camera = go.GetComponent<Camera>(); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var target = new RenderTexture(1920, 1080, 24);
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
                yield return null;
                // Rebuild static UI as well as moving actors after changing Canvas render mode.
                // Otherwise batch captures can retain stale overlay-space culling/vertices.
                foreach (var label in canvas.GetComponentsInChildren<Text>())
                    if (label.font != null && label.font.dynamic)
                        label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
                foreach (var graphic in canvas.GetComponentsInChildren<Graphic>())
                { graphic.SetAllDirty(); graphic.canvasRenderer.cull = false; }
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                ((RectTransform)canvas.transform.Find("CabinFrame")).GetWorldCorners(corners);
                camera.transform.position = (corners[0] + corners[2]) * .5f - Vector3.forward * 100;
                camera.orthographicSize = Vector3.Distance(corners[0], corners[1]) * .5f;
                camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../" + filename), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
                camera.targetTexture = null; target.Release();
                Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(go);
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (SceneFlowController.Instance != null)
            { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
        }
    }
}

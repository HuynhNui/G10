using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Dialogue;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ScopeUIPresentationPlayModeTests
    {
        private string folder;
        [UnityTest]
        public IEnumerator SharedHudWithoutCameraAndDialogueUseLiveControls()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ScopeUI-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null; yield return null;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            cabin.OpenNavigation();
            yield return null;
            Assert.That(cabin.NavigationPanel.transform.Find("Brake"), Is.Null, "The helm no longer has a stop button.");
            var navigationInfo = cabin.NavigationPanel.transform.Find("NavigationInfo");
            Assert.That(navigationInfo, Is.Not.Null);
            var energyMeters = cabin.NavigationPanel.GetComponentsInChildren<ShipEnergyBar>(true);
            Assert.That(energyMeters, Has.Length.EqualTo(1));
            Assert.That(energyMeters[0].transform.IsChildOf(navigationInfo), Is.True, "Speed and energy share the bottom status card.");
            Assert.That(energyMeters[0].GetComponentInChildren<Text>().text, Does.Contain("ENERGY"));
            Assert.That(energyMeters[0].GetComponentInChildren<Text>().text, Does.Contain(cabin.Navigation.Ship.EnergyCapacity.ToString("0")));
            foreach (var button in cabin.NavigationPanel.transform.Find("WatercolorHUD").GetComponentsInChildren<Button>())
            {
                var background = button.GetComponent<Image>();
                Assert.That(background.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(background.pixelsPerUnitMultiplier, Is.GreaterThan(1), "Small corner slices keep wide buttons rectangular.");
            }
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "scope-helm.png");
            cabin.NavigationPanel.transform.Find("WatercolorHUD/Radar").GetComponent<Button>().onClick.Invoke();
            Assert.That(cabin.Panels.CurrentPanel, Is.SameAs(cabin.RadarPanel));
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "scope-radar.png");
            foreach(var hud in cabin.GetComponentsInChildren<Transform>(true))
                if(hud.name=="WatercolorHUD")
                {
                    Assert.That(hud.Find("Camera"),Is.Null);
                    Assert.That(hud.Find("Cabin"),Is.Null,"Escape is the cabin return action; no top-HUD cabin button.");
                    // The overview keeps its separate Resume header, not the shared station navigation.
                    foreach(string name in hud.parent.name=="WorldMapPanel" ? new[]{"Resume"} : new[]{"Map","Helm","Radar"})
                        Assert.That(hud.Find(name)?.GetComponent<Button>(),Is.Not.Null,name);
                }
            cabin.RadarPanel.transform.Find("WatercolorHUD/Helm").GetComponent<Button>().onClick.Invoke();
            Assert.That(cabin.Panels.CurrentPanel,Is.SameAs(cabin.NavigationPanel));
            cabin.ClosePanel();
            Assert.That(cabin.Panels.CurrentPanel,Is.Null);
            cabin.OpenRadar();
            cabin.RadarPanel.transform.Find("WatercolorHUD/Map").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var world = cabin.GetComponent<WorldMapController>();
            Assert.That(cabin.Panels.CurrentPanel, Is.SameAs(world.zoneMaps[world.LastZoneIndex]), "Map resumes the active zone.");
            world.OpenWorld();
            yield return null;
            Assert.That(cabin.Panels.CurrentPanel, Is.SameAs(world.worldPanel));
            foreach (var child in world.worldPanel.GetComponentsInChildren<Transform>(true))
            {
                Assert.That(child.name.StartsWith("WatercolorRegion"), Is.False, "World-map region cards were removed.");
                Assert.That(child.name, Is.Not.EqualTo("WatercolorMarker"), "World regions use the original polygon hover interaction.");
            }
            var region = world.worldPanel.GetComponentsInChildren<WorldMapZoneHotspot>()[0];
            var pointer = RegionPointer(region);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Exists(hit => hit.gameObject == region.gameObject), Is.True, "A region must open directly through its painted polygon.");
            region.OnPointerEnter(pointer);
            yield return new WaitForSecondsRealtime(region.fadeDuration);
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "scope-world-map.png");
            region.OnPointerClick(pointer);
            Assert.That(cabin.Panels.CurrentPanel, Is.SameAs(cabin.MapPanel));
            yield return null;
            var chartBackground = cabin.MapPanel.transform.Find("WatercolorChartBackground");
            var chart = cabin.MapPanel.transform.Find("SquareChartContent");
            Assert.That(chartBackground, Is.Not.Null, "The frame belongs to the full chart-panel background.");
            Assert.That(chartBackground.GetSiblingIndex(), Is.LessThan(chart.GetSiblingIndex()));
            var backgroundRect = ((RectTransform)chartBackground).rect;
            var chartRect = ((RectTransform)chart).rect;
            Assert.That(backgroundRect.width, Is.GreaterThan(chartRect.width));
            Assert.That(backgroundRect.height, Is.GreaterThan(chartRect.height));
            Assert.That(cabin.MapPanel.transform.Find("ChartOuterFrame").GetComponent<Image>().color.a, Is.Zero, "Axis labels must not add another frame around the map artwork.");
            var overlay = world.zone01Overlay;
            Assert.That(cabin.MapPanel.transform.Find("ChartCoordinate"), Is.Null, "The clipped bottom coordinate label was removed.");
            Assert.That(overlay.coordinateReadout, Is.Not.Null);
            Assert.That(overlay.coordinateReadout, Is.TypeOf<TextMeshProUGUI>());
            var pointerTarget = chart.GetComponent<CabinPointerTarget>();
            Assert.That(pointerTarget, Is.Not.Null);
            var chartArea = (RectTransform)chart;
            var chartPointer = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(null,
                    chartArea.TransformPoint(chartArea.rect.min + Vector2.Scale(new Vector2(.25f,.5f),chartArea.rect.size)))
            };
            pointerTarget.OnPointerMove(chartPointer);
            Assert.That(overlay.coordinateReadout.text, Does.Contain($"X {overlay.mapConfig.WorldSize.x * .25f:0.0}"));
            Assert.That(overlay.coordinateReadout.text, Does.Contain($"Y {overlay.mapConfig.WorldSize.y * .5f:0.0}"));
            Assert.That(overlay.coordinateReadout.text, Does.Not.Contain("Z "));
            string lastCoordinate=overlay.coordinateReadout.text;
            pointerTarget.OnPointerExit(chartPointer);
            Assert.That(overlay.coordinateReadout.text, Is.EqualTo(lastCoordinate), "Leaving the chart keeps the last coordinate without flicker.");
            Assert.That(overlay.locationIcons, Has.Length.EqualTo(overlay.Locations.Length));
            foreach (var icon in overlay.locationIcons)
            {
                Assert.That(icon, Is.Not.Null);
                Assert.That(icon.enabled, Is.True);
                Assert.That(icon.texture, Is.Not.Null, "Restored map locations use their original artwork.");
            }
            overlay.SetPointer(overlay.mapConfig.CoordinatesToUV(overlay.Locations[0].mapPosition));
            Assert.That(overlay.taskReadout.transform.parent.gameObject.activeSelf, Is.True);
            Assert.That(overlay.taskReadout.enabled, Is.False);
            Assert.That(overlay.styledTaskReadout, Is.Not.Null);
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "scope-chart.png");
            cabin.ClosePanel();
            var dialogue = Object.FindAnyObjectByType<DialogueController>();
            Assert.That(dialogue.TryBegin(new[] { new DialogueLine(DialogueSpeakerKind.Guide, "HƯỚNG DẪN", "Vật phẩm thu thập được lưu trong CARGO trên máy tính.") }), Is.True);
            dialogue.Next();
            yield return CaptureDialogue(dialogue.View.GetComponent<Canvas>());
            dialogue.Next();
            Assert.That(dialogue.IsActive, Is.False);
        }

        private static PointerEventData RegionPointer(WorldMapZoneHotspot region)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 center = Vector2.zero;
            foreach (var uv in region.polygon) center += uv;
            center /= region.polygon.Length;
            Vector2 local = region.rectTransform.rect.min + Vector2.Scale(center, region.rectTransform.rect.size);
            return new PointerEventData(EventSystem.current) {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, region.rectTransform.TransformPoint(local))
            };
        }

        private static IEnumerator CaptureDialogue(Canvas canvas)
        {
            var mode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            var go = new GameObject("Dialogue validation camera", typeof(Camera));
            var camera = go.GetComponent<Camera>(); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new(.19f, .35f, .5f);
            var target = new RenderTexture(1920, 1080, 24); var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active;
            try
            {
                canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera; camera.targetTexture = target;
                yield return null;
                foreach (var graphic in canvas.GetComponentsInChildren<Graphic>()) { graphic.SetAllDirty(); graphic.canvasRenderer.cull = false; }
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4]; ((RectTransform)canvas.transform).GetWorldCorners(corners);
                camera.transform.position = (corners[0] + corners[2]) * .5f - Vector3.forward * 100;
                camera.orthographicSize = Vector3.Distance(corners[0], corners[1]) * .5f;
                camera.Render(); RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
                File.WriteAllBytes(CabinNavigationPlayModeTests.CapturePath("scope-dialogue.png"), pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = mode; canvas.worldCamera = previousCamera; RenderTexture.active = previousTarget;
                camera.targetTexture = null; target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(go);
            }
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            var dialogue = Object.FindAnyObjectByType<DialogueController>(); if (dialogue != null) dialogue.Cancel();
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            ExpeditionSaveStore.PathOverride = null; PhotoCaptureService.ArchivePathOverride = null;
            if (folder != null && Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}

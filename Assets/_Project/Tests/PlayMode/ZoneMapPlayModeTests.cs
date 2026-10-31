using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests.PlayMode
{
    public sealed class ZoneMapPlayModeTests
    {
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone04ExperienceLoop/native-validation/ZoneMapPlayModeTests"));

        [UnityTest] public IEnumerator StaticTerrainPixelsMatchCanonicalCellsAndUvWithoutChangingSourceAssets()
        {
            var config = ScriptableObject.CreateInstance<ZoneMapConfig>(); config.zoneId = "Zone04";
            config.map = new Texture2D(12, 8); config.terrainLine = new Texture2D(12, 8);
            byte[] cells = { 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 0, 0, 1, 0 };
            config.StoreTerrainMask(cells, 6, 4, false);
            config.StoreTerrainMask(Enumerable.Repeat((byte)1, 24).ToArray(), 6, 4, true);
            byte[] art = config.map.EncodeToPNG(), line = config.terrainLine.EncodeToPNG();
            string serialized = JsonUtility.ToJson(config);
            var owner = new GameObject("Controlled static chart fixture", typeof(RectTransform), typeof(Canvas)); owner.SetActive(false);
            var source = new GameObject("Source chart", typeof(RectTransform), typeof(RawImage)); source.transform.SetParent(owner.transform, false);
            var image = source.GetComponent<RawImage>(); image.uvRect = new Rect(.1f, .9f, .7f, -.8f);
            var presentation = owner.AddComponent<ZoneMapPresentation>(); presentation.config = config; presentation.mapImage = image;
            owner.SetActive(true); presentation.RefreshMap();
            Texture2D texture = presentation.TerrainImage.texture as Texture2D;
            try
            {
                Assert.That(texture, Is.Not.Null); Assert.That(texture.width, Is.EqualTo(6)); Assert.That(texture.height, Is.EqualTo(4));
                Color32[] pixels = texture.GetPixels32();
                for (int index = 0; index < cells.Length; index++)
                    Assert.That(pixels[index].a > 0, Is.EqualTo(cells[index] == 0), "Canonical bottom-left cell " + index + ": no water dilation or vertical flip.");
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point)); Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(texture.mipmapCount, Is.EqualTo(1)); Assert.That(presentation.TerrainImage.uvRect, Is.EqualTo(image.uvRect));
                Assert.That(presentation.TerrainImage.rectTransform.parent, Is.SameAs(image.transform));
                Assert.That(presentation.TerrainImage.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(presentation.TerrainImage.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(presentation.TerrainImage.rectTransform.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(presentation.TerrainImage.rectTransform.offsetMax, Is.EqualTo(Vector2.zero));
                Assert.That(presentation.TerrainImage.raycastTarget, Is.False);
                Assert.That(config.TryCopyTerrainMask(out var copy, out int columns, out int rows), Is.True);
                Assert.That(columns, Is.EqualTo(6)); Assert.That(rows, Is.EqualTo(4)); CollectionAssert.AreEqual(cells, copy);
                copy[0] = 0;
                Assert.That(config.TryCopyTerrainMask(out var independent, out _, out _), Is.True); CollectionAssert.AreEqual(cells, independent);
                for (int i = 0; i < 12; i++) { presentation.RefreshMap(); presentation.ToggleTerrainVisibility(); }
                Assert.That(presentation.TerrainTextureBuildCount, Is.EqualTo(1)); Assert.That(presentation.TerrainCachedTextureCount, Is.EqualTo(1));
                Assert.That(presentation.TerrainImage.texture, Is.SameAs(texture)); Assert.That(JsonUtility.ToJson(config), Is.EqualTo(serialized));
                CollectionAssert.AreEqual(art, config.map.EncodeToPNG()); CollectionAssert.AreEqual(line, config.terrainLine.EncodeToPNG());
            }
            finally { UnityEngine.Object.Destroy(owner); }
            yield return null; yield return null;
            Assert.That(texture == null, Is.True, "Destroying the owner releases the actual runtime contour texture.");
            UnityEngine.Object.Destroy(config.map); UnityEngine.Object.Destroy(config.terrainLine); UnityEngine.Object.Destroy(config);
        }

        [UnityTest] public IEnumerator InstalledZoneFourTerrainTogglePreservesPointerSecretsOtherZonesAndCachedLifetime()
        {
            string oldSave = ExpeditionSaveStore.PathOverride, oldPhotos = PhotoCaptureService.ArchivePathOverride;
            string folder = Path.Combine(Application.temporaryCachePath, "ZoneFourChart-" + Guid.NewGuid().ToString("N"));
            SceneFlowController flow = null; Texture2D terrain = null;
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            try
            {
                if (SceneFlowController.Instance != null) { UnityEngine.Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
                yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
                flow = SceneFlowController.Instance; yield return WaitChartTransition(flow);
                var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(4); menu.StartRun(); yield return WaitChartTransition(flow);
                var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
                var loop = UnityEngine.Object.FindAnyObjectByType<ExpeditionLoop>();
                var world = UnityEngine.Object.FindAnyObjectByType<WorldMapController>();
                Assert.That(loop.Zone, Is.EqualTo("Zone04")); cabin.ClosePanel(); cabin.OpenCurrentMap(); yield return null; yield return null;
                var presentation = world.zoneMaps[3].GetComponent<ZoneMapPresentation>(); var overlay = presentation.overlay;
                Assert.That(presentation.TerrainVisible, Is.True); Assert.That(presentation.TerrainMapConfig, Is.SameAs(loop.ActiveMap));
                terrain = presentation.TerrainImage.texture as Texture2D; Assert.That(terrain, Is.Not.Null);
                Assert.That(presentation.TerrainToggleLabel.text, Does.Contain("ĐỊA HÌNH").And.Contain("BẬT"));
                Assert.That(presentation.TerrainLegend.text, Does.Contain("Vàng nhạt").And.Contain("vật cản"));
                Assert.That(presentation.TerrainImage.raycastTarget, Is.False); Assert.That(presentation.TerrainLegend.raycastTarget, Is.False);
                Assert.That(presentation.TerrainImage.transform.GetSiblingIndex(), Is.LessThan(overlay.transform.GetSiblingIndex()));
                Assert.That(presentation.config.TryCopyTerrainMask(out byte[] cells, out int columns, out int rows), Is.True);
                Assert.That(terrain.width, Is.EqualTo(columns)); Assert.That(terrain.height, Is.EqualTo(rows));
                var pixels = terrain.GetPixels32();
                for (int index = 0; index < cells.Length; index++)
                    Assert.That(pixels[index].a > 0, Is.EqualTo(cells[index] == 0), "Installed mask cell " + index);
                string mapBefore = JsonUtility.ToJson(presentation.config), progress = JsonUtility.ToJson(cabin.GetComponent<ZoneOneStory>().ExportProgress());
                var originalMarkers = overlay.locationIcons.Select(icon => icon.rectTransform.anchoredPosition).ToArray();
                var graphics = presentation.GetComponentsInChildren<Graphic>(true).Select(graphic => graphic.GetInstanceID()).OrderBy(id => id).ToArray();
                Assert.That(overlay.locationIcons[2].enabled, Is.False); Assert.That(overlay.locationIcons[3].enabled, Is.False);
                Assert.That(overlay.FinalApproachVisible, Is.False);
                // Real event raycast and chart pointer handlers, with the new nonblocking layer present.
                var target = presentation.mapImage.GetComponent<CabinPointerTarget>(); Assert.That(target, Is.Not.Null);
                Vector2 uv = presentation.config.CoordinatesToUV(presentation.config.GridCellCenter(overlay.Locations[0].mapPosition));
                RectTransform rect = presentation.mapImage.rectTransform;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.min + Vector2.Scale(uv, rect.rect.size)));
                var pointer = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Any(hit => hit.gameObject == target.gameObject), Is.True, "Pointer raycasts must still reach the authored chart.");
                Assert.That(hits.Any(hit => hit.gameObject == presentation.TerrainImage.gameObject), Is.False);
                target.OnPointerMove(pointer); target.OnPointerClick(pointer);
                string coordinate = overlay.coordinateReadout.text, task = overlay.styledTaskReadout.text;
                Assert.That(overlay.SelectedLocation, Is.EqualTo(0)); Assert.That(task, Is.Not.Empty);
                yield return ChartScreenshot("chart-terrain-on");
                ExecuteEvents.Execute(presentation.TerrainToggleButton.gameObject, pointer, ExecuteEvents.pointerClickHandler); yield return null;
                Assert.That(presentation.TerrainVisible, Is.False); Assert.That(presentation.TerrainEnabled, Is.False);
                Assert.That(presentation.TerrainToggleLabel.text, Does.Contain("TẮT"));
                target.OnPointerMove(pointer); target.OnPointerClick(pointer);
                Assert.That(overlay.coordinateReadout.text, Is.EqualTo(coordinate)); Assert.That(overlay.styledTaskReadout.text, Is.EqualTo(task));
                yield return ChartScreenshot("chart-terrain-off");
                cabin.ClosePanel(); cabin.OpenCurrentMap(); yield return null;
                Assert.That(presentation.TerrainVisible, Is.False, "Reopen retains the player's explicit layer choice.");
                ExecuteEvents.Execute(presentation.TerrainToggleButton.gameObject, pointer, ExecuteEvents.pointerClickHandler); yield return null;
                Assert.That(presentation.TerrainVisible, Is.True); Assert.That(presentation.TerrainImage.texture, Is.SameAs(terrain));
                Assert.That(presentation.TerrainTextureBuildCount, Is.EqualTo(1)); Assert.That(presentation.TerrainCachedTextureCount, Is.EqualTo(1));
                // Move the controlled ship while the chart stays open. Its graphics and static mesh must not follow the vessel.
                Canvas.ForceUpdateCanvases(); var meshBefore = overlay.canvasRenderer.GetMesh();
                var verticesBefore = meshBefore.vertices; var colorsBefore = meshBefore.colors32;
                Vector2 ship = cabin.Navigation.Position; Vector2 shifted = ship + Vector2.right;
                Assert.That(cabin.Navigation.CanOccupy(shifted), Is.True);
                cabin.Navigation.RestoreVoyage(shifted, cabin.Navigation.Heading + 90, cabin.Navigation.Depth, cabin.Navigation.DistanceTravelled);
                yield return null; Canvas.ForceUpdateCanvases(); var meshAfter = overlay.canvasRenderer.GetMesh();
                CollectionAssert.AreEqual(verticesBefore, meshAfter.vertices, "There must be no live ship-position marker in chart geometry.");
                CollectionAssert.AreEqual(colorsBefore, meshAfter.colors32);
                CollectionAssert.AreEqual(graphics, presentation.GetComponentsInChildren<Graphic>(true).Select(graphic => graphic.GetInstanceID()).OrderBy(id => id).ToArray());
                for (int i = 0; i < originalMarkers.Length; i++) Assert.That(overlay.locationIcons[i].rectTransform.anchoredPosition, Is.EqualTo(originalMarkers[i]));
                Assert.That(overlay.locationIcons[2].enabled, Is.False); Assert.That(overlay.locationIcons[3].enabled, Is.False); Assert.That(overlay.FinalApproachVisible, Is.False);
                Assert.That(JsonUtility.ToJson(presentation.config), Is.EqualTo(mapBefore));
                Assert.That(JsonUtility.ToJson(cabin.GetComponent<ZoneOneStory>().ExportProgress()), Is.EqualTo(progress));
                for (int i = 0; i < 3; i++)
                {
                    world.OpenZone(i); yield return null; var other = world.zoneMaps[i].GetComponent<ZoneMapPresentation>();
                    Texture original = other.mapImage.texture; string configBefore = JsonUtility.ToJson(other.config);
                    other.ToggleTerrainVisibility(); other.RefreshMap();
                    Assert.That(other.TerrainImage, Is.Null, "Zone01–03 must not gain the Zone04 overlay.");
                    Assert.That(other.TerrainToggleButton, Is.Null); Assert.That(other.TerrainTextureBuildCount, Is.Zero);
                    Assert.That(other.mapImage.texture, Is.SameAs(original)); Assert.That(JsonUtility.ToJson(other.config), Is.EqualTo(configBefore));
                }
                flow.LoadMainMenu(); yield return WaitChartTransition(flow); yield return null;
                Assert.That(terrain == null, Is.True, "Leaving the real chart scene destroys its cached contour texture.");
            }
            finally
            {
                ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
                if (flow != null) UnityEngine.Object.Destroy(flow.gameObject);
                string absolute = Path.GetFullPath(folder), temporary = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (absolute.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(absolute).StartsWith("ZoneFourChart-", StringComparison.Ordinal) && Directory.Exists(absolute))
                    Directory.Delete(absolute, true);
            }
        }

        private static IEnumerator WaitChartTransition(SceneFlowController flow)
        {
            yield return null; float end = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
        private static IEnumerator ChartScreenshot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture(); Assert.That(image, Is.Not.Null);
            try { Directory.CreateDirectory(Qa); File.WriteAllBytes(Path.Combine(Qa, name + ".png"), image.EncodeToPNG()); }
            finally { UnityEngine.Object.Destroy(image); }
        }

        [Test]
        public void SourcePixelsDetermineGridCountWhileEachCellStaysFiftyUnits()
        {
            var config = ScriptableObject.CreateInstance<ZoneMapConfig>();
            config.map = new Texture2D(1920, 1080);
            config.locationMarker = new Texture2D(78, 72);
            Assert.That(config.GridSize, Is.EqualTo(50));
            Assert.That(config.GridCoordinateStep, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(config.GridColumns, Is.EqualTo(39));
            Assert.That(config.GridRows, Is.EqualTo(22));
            Assert.That(config.WorldSize, Is.EqualTo(new Vector2(1920, 1080)));
            Object.DestroyImmediate(config.map);
            Object.DestroyImmediate(config.locationMarker);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void MapCoordinatesConvertThroughNormalizedWorldSize()
        {
            var config = ScriptableObject.CreateInstance<ZoneMapConfig>();
            config.map = new Texture2D(1000, 600);
            config.locationMarker = new Texture2D(100, 100);
            config.ConfigureGrid(50);
            config.ConfigureDisplaySize(new Vector2(1000, 600));
            Vector2 coordinate = new(250, 450);
            Assert.That(config.CoordinatesToUV(coordinate), Is.EqualTo(new Vector2(.25f, .75f)));
            Assert.That(config.UVToCoordinates(new Vector2(.25f, .75f)), Is.EqualTo(coordinate));
            Object.DestroyImmediate(config.map);
            Object.DestroyImmediate(config.locationMarker);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PoiDisplayPositionSnapsToGridCellCenter()
        {
            var config = ScriptableObject.CreateInstance<ZoneMapConfig>();
            config.map = new Texture2D(1000, 600);
            config.locationMarker = new Texture2D(100, 100);
            Vector2 step = config.GridCoordinateStep;
            Vector2 center = config.GridCellCenter(new Vector2(281, 198));
            Assert.That(Mathf.Repeat(center.x, step.x), Is.EqualTo(step.x * .5f).Within(.001f));
            Assert.That(Mathf.Repeat(center.y, step.y), Is.EqualTo(step.y * .5f).Within(.001f));
            Object.DestroyImmediate(config.map);
            Object.DestroyImmediate(config.locationMarker);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void LightExpandsForFiveSecondsThenReturns()
        {
            Assert.That(ZoneMapPresentation.EvaluateLightScale(0, 5, 1.08f), Is.EqualTo(1).Within(.0001f));
            Assert.That(ZoneMapPresentation.EvaluateLightScale(5, 5, 1.08f), Is.EqualTo(1.08f).Within(.0001f));
            Assert.That(ZoneMapPresentation.EvaluateLightScale(10, 5, 1.08f), Is.EqualTo(1).Within(.0001f));
        }

        [Test]
        public void ZoneThreeUsesPostRockMapAfterDestroyObjective()
        {
            var mission = ScriptableObject.CreateInstance<ZoneMissionConfig>();
            mission.zoneGate = new ZoneGateConfig {
                objectives = new[] { new MissionObjectiveConfig { id = "DESTROY", type = MissionObjectiveType.DestroyObstacle, targetId = "ROCK" } }
            };
            var map = ScriptableObject.CreateInstance<ZoneMapConfig>();
            map.missionConfig = mission;
            map.alternateObjectiveId = "DESTROY";
            map.map = new Texture2D(2, 2);
            map.alternateMap = new Texture2D(2, 2);
            var go = new GameObject("ZoneMapTest", typeof(RectTransform), typeof(RawImage), typeof(ZoneMissionRuntime), typeof(ZoneMapPresentation));
            var runtime = go.GetComponent<ZoneMissionRuntime>();
            runtime.config = mission;
            var presentation = go.GetComponent<ZoneMapPresentation>();
            presentation.config = map;
            presentation.mapImage = go.GetComponent<RawImage>();
            presentation.MissionRuntime = runtime;
            Assert.That(presentation.mapImage.texture, Is.SameAs(map.map));
            Assert.That(runtime.RecordGlobalObjective(MissionObjectiveType.DestroyObstacle, "ROCK"), Is.True);
            presentation.RefreshMap();
            Assert.That(presentation.mapImage.texture, Is.SameAs(map.alternateMap));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(map.map);
            Object.DestroyImmediate(map.alternateMap);
            Object.DestroyImmediate(map);
            Object.DestroyImmediate(mission);
        }

        [Test]
        public void TerrainMaskRoundTripsThroughCompressedMapConfig()
        {
            var config = ScriptableObject.CreateInstance<ZoneMapConfig>();
            config.map = new Texture2D(100, 100);
            config.locationMarker = new Texture2D(50, 50);
            config.StoreTerrainMask(new byte[] { 1, 1, 0, 1 }, 2, 2, false);
            var go = new GameObject("Navigation", typeof(ZoneNavigation));
            var navigation = go.GetComponent<ZoneNavigation>();
            Assert.That(config.ApplyTerrain(navigation, false), Is.True);
            Assert.That(navigation.IsWater(new Vector2(25, 25)), Is.True);
            Assert.That(navigation.IsWater(new Vector2(25, 75)), Is.False);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(config.map);
            Object.DestroyImmediate(config.locationMarker);
            Object.DestroyImmediate(config);
        }

        [UnityTest]
        public IEnumerator InstalledSceneContainsAllZoneMapsAndHidesZoneFourSecrets()
        {
            SceneManager.LoadScene("Zone01");
            yield return null;
            var world = Object.FindAnyObjectByType<WorldMapController>();
            Assert.That(world, Is.Not.Null);
            Assert.That(world.zoneMaps, Has.Length.EqualTo(4));
            for (int i = 0; i < world.zoneMaps.Length; i++)
            {
                var presentation = world.zoneMaps[i].GetComponent<ZoneMapPresentation>();
                Assert.That(presentation, Is.Not.Null, $"Zone {i + 1} presentation");
                Assert.That(presentation.config, Is.Not.Null, $"Zone {i + 1} config");
                Assert.That(presentation.config.terrainLine, Is.Not.Null, $"Zone {i + 1} terrain line");
                Assert.That(presentation.lightImages, Is.Not.Empty, $"Zone {i + 1} light layers");
            }
            var zoneFour = world.zoneMaps[3].GetComponentInChildren<PhotoSurveyMap>(true);
            Assert.That(zoneFour.locationIcons, Has.Length.EqualTo(4));
            Assert.That(zoneFour.locationIcons[0].enabled, Is.True);
            Assert.That(zoneFour.locationIcons[1].enabled, Is.True);
            Assert.That(zoneFour.locationIcons[2].enabled, Is.False);
            Assert.That(zoneFour.locationIcons[3].enabled, Is.False);
            var zoneThree = world.zoneMaps[2].GetComponent<ZoneMapPresentation>();
            Assert.That(zoneThree.config.alternateMap, Is.Not.Null);
            Assert.That(zoneThree.config.alternateTerrainLine, Is.Not.Null);

            var zoneOneContent = world.zoneMaps[0].transform.Find("SquareChartContent") as RectTransform;
            for (int i = 1; i < world.zoneMaps.Length; i++)
            {
                Transform panel = world.zoneMaps[i].transform;
                var content = panel.Find("MapChartContent") as RectTransform;
                var overlay = content.GetComponentInChildren<PhotoSurveyMap>(true);
                Assert.That(content.sizeDelta, Is.EqualTo(zoneOneContent.sizeDelta), $"Zone {i + 1} chart size");
                Assert.That(panel.Find("ChartOuterFrame"), Is.Not.Null, $"Zone {i + 1} axes");
                foreach (Transform label in panel.Find("ChartOuterFrame"))
                    if (label.gameObject.activeSelf && label.name.Length > 1 &&
                        (label.name[0] == 'X' || label.name[0] == 'Y') && int.TryParse(label.name.Substring(1), out int coordinate))
                        Assert.That(coordinate % 100, Is.Zero, $"Zone {i + 1} major axis label {label.name}");
                Assert.That(panel.Find("SurveyLegend/Coordinate"), Is.Not.Null, $"Zone {i + 1} coordinate readout");
                Assert.That(panel.Find("WatercolorWorld")?.gameObject.activeSelf, Is.True, $"Zone {i + 1} world map button");
                Assert.That(overlay.coordinateReadout, Is.Not.Null, $"Zone {i + 1} coordinate binding");
                Assert.That(overlay.styledTaskReadout, Is.Not.Null, $"Zone {i + 1} task binding");
                Assert.That(panel.Find("WatercolorWorld").GetComponent<Button>().onClick.GetPersistentEventCount(), Is.EqualTo(1),
                    $"Zone {i + 1} world map action");
                overlay.SetPointer(new Vector2(.5f, .5f));
                Assert.That(overlay.coordinateReadout.text,
                    Does.Contain($"X {overlay.mapConfig.WorldSize.x * .5f:0.0}")
                        .And.Contain($"Y {overlay.mapConfig.WorldSize.y * .5f:0.0}"),
                    $"Zone {i + 1} hover coordinate");
                for (int p = 0; p < overlay.locationIcons.Length; p++)
                {
                    var marker = overlay.locationIcons[p].rectTransform;
                    Vector2 displayCoordinate = overlay.mapConfig.UVToCoordinates(marker.anchorMin);
                    Assert.That(Vector2.Distance(displayCoordinate, overlay.mapConfig.GridCellCenter(overlay.Locations[p].mapPosition)),
                        Is.LessThan(.001f), $"Zone {i + 1} marker {p + 1} center");
                    Assert.That(marker.sizeDelta.x, Is.EqualTo(content.rect.width * overlay.mapConfig.GridSize / overlay.mapConfig.WorldSize.x).Within(.01f));
                    Assert.That(marker.sizeDelta.y, Is.EqualTo(content.rect.height * overlay.mapConfig.GridSize / overlay.mapConfig.WorldSize.y).Within(.01f));
                }
            }
        }
    }
}

using System.Collections;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace G10.Prototype.Tests.PlayMode
{
    public sealed class ZoneMapPlayModeTests
    {
        [Test]
        public void GridStepUsesLocationFileDimensions()
        {
            var config = ScriptableObject.CreateInstance<ZoneMapConfig>();
            config.map = new Texture2D(1920, 1080);
            config.locationMarker = new Texture2D(96, 54);
            Assert.That(config.GridCoordinateStep, Is.EqualTo(new Vector2(60, 35)));
            Object.DestroyImmediate(config.map);
            Object.DestroyImmediate(config.locationMarker);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PoiDisplayPositionSnapsToGridCellCenter()
        {
            var config = ScriptableObject.CreateInstance<ZoneMapConfig>();
            config.map = new Texture2D(1920, 1080);
            config.locationMarker = new Texture2D(80, 72);
            Vector2 step = config.GridCoordinateStep;
            Vector2 center = config.GridCellCenter(new Vector2(281, 398));
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
            config.StoreTerrainMask(new byte[] { 1, 1, 0, 1 }, 2, 2, false);
            var go = new GameObject("Navigation", typeof(ZoneNavigation));
            var navigation = go.GetComponent<ZoneNavigation>();
            Assert.That(config.ApplyTerrain(navigation, false), Is.True);
            Assert.That(navigation.IsWater(new Vector2(100, 100)), Is.True);
            Assert.That(navigation.IsWater(new Vector2(100, 600)), Is.False);
            Object.DestroyImmediate(go);
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
            Assert.That(zoneFour.locationIcons, Has.Length.EqualTo(5));
            Assert.That(zoneFour.locationIcons[0].enabled, Is.True);
            Assert.That(zoneFour.locationIcons[1].enabled, Is.True);
            Assert.That(zoneFour.locationIcons[2].enabled, Is.False);
            Assert.That(zoneFour.locationIcons[3].enabled, Is.False);
            Assert.That(zoneFour.locationIcons[4].enabled, Is.False);
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
                Assert.That(panel.Find("SurveyLegend/Coordinate"), Is.Not.Null, $"Zone {i + 1} coordinate readout");
                Assert.That(panel.Find("WatercolorWorld")?.gameObject.activeSelf, Is.True, $"Zone {i + 1} world map button");
                Assert.That(overlay.coordinateReadout, Is.Not.Null, $"Zone {i + 1} coordinate binding");
                Assert.That(overlay.styledTaskReadout, Is.Not.Null, $"Zone {i + 1} task binding");
                Assert.That(panel.Find("WatercolorWorld").GetComponent<Button>().onClick.GetPersistentEventCount(), Is.EqualTo(1),
                    $"Zone {i + 1} world map action");
                overlay.SetPointer(new Vector2(.5f, .5f));
                Assert.That(overlay.coordinateReadout.text, Does.Contain("X 600.0").And.Contain("Y 350.0"),
                    $"Zone {i + 1} hover coordinate");
                Vector2 step = overlay.mapConfig.GridCoordinateStep;
                for (int p = 0; p < overlay.locationIcons.Length; p++)
                {
                    var marker = overlay.locationIcons[p].rectTransform;
                    Vector2 displayCoordinate = ZoneNavigation.UVToCoordinates(marker.anchorMin);
                    Assert.That(Vector2.Distance(displayCoordinate, overlay.mapConfig.GridCellCenter(overlay.Locations[p].mapPosition)),
                        Is.LessThan(.001f), $"Zone {i + 1} marker {p + 1} center");
                    Assert.That(marker.sizeDelta.x, Is.EqualTo(content.rect.width * step.x / 1200f).Within(.01f));
                    Assert.That(marker.sizeDelta.y, Is.EqualTo(content.rect.height * step.y / 700f).Within(.01f));
                }
            }
        }
    }
}

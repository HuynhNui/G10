using System.Collections;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests.PlayMode
{
    public sealed class MapLightOpacityPlayModeTests
    {
        private GameObject panel;
        private ZoneMapConfig config, replacement;
        private ZoneMapPresentation presentation;
        private ZoneMissionConfig mission;
        private Texture2D map, alternate, lightTexture;
        private UnityEngine.UI.RawImage grid, marker;

        [SetUp]
        public void Setup()
        {
            config = ScriptableObject.CreateInstance<ZoneMapConfig>();
            map = new Texture2D(200, 100);
            alternate = new Texture2D(200, 100);
            lightTexture = new Texture2D(2, 2);
            config.map = map;
            config.lightLayers = new[] { lightTexture, lightTexture };
            config.ConfigureLightOpacity(.35f);
            panel = new GameObject("Light Opacity Test", typeof(RectTransform));
            panel.SetActive(false);
            presentation = panel.AddComponent<ZoneMapPresentation>();
            presentation.config = config;
            presentation.mapImage = Image("Map", new Color(.7f, .8f, .9f, 1f));
            presentation.lightImages = new[] {
                Image("Light 1", new Color(.5f, .6f, .7f, .9f)),
                Image("Light 2", new Color(.8f, .7f, .6f, .8f))
            };
            foreach (var light in presentation.lightImages) light.texture = lightTexture;
            grid = Image("Grid", new Color(.6f, .7f, .8f, .4f));
            marker = Image("Location", new Color(.1f, .2f, .3f, .75f));
        }

        [TearDown]
        public void Cleanup()
        {
            if (panel != null) Object.DestroyImmediate(panel);
            Object.DestroyImmediate(config);
            if (replacement != null) Object.DestroyImmediate(replacement);
            if (mission != null) Object.DestroyImmediate(mission);
            Object.DestroyImmediate(map);
            Object.DestroyImmediate(alternate);
            Object.DestroyImmediate(lightTexture);
        }

        private UnityEngine.UI.RawImage Image(string name, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            item.transform.SetParent(panel.transform, false);
            var image = item.GetComponent<UnityEngine.UI.RawImage>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        [Test]
        public void OpacityOnlyChangesLightAlphaNotArtGridMarkersOrCoordinates()
        {
            config.ConfigureLightOpacity(.225f, -1f, .2f);
            Color gridBefore = grid.color, markerBefore = marker.color, mapBefore = presentation.mapImage.color;
            Vector2 worldBefore = config.WorldSize, coordinate = new(75f, 25f);
            Vector2 uvBefore = config.CoordinatesToUV(coordinate);
            int columnsBefore = config.GridColumns, rowsBefore = config.GridRows;
            var light = presentation.lightImages[0];
            Vector3 scale = new(1.04f, 1.04f, 1f);
            light.rectTransform.localScale = scale;
            presentation.RefreshMap();
            Assert.That(light.color, Is.EqualTo(new Color(.5f, .6f, .7f, .225f)));
            Assert.That(presentation.lightImages[1].color.a, Is.EqualTo(.2f));
            Assert.That(light.texture, Is.SameAs(lightTexture));
            Assert.That(light.rectTransform.localScale, Is.EqualTo(scale));
            Assert.That(presentation.mapImage.texture, Is.SameAs(map));
            Assert.That(presentation.mapImage.color, Is.EqualTo(mapBefore));
            Assert.That(grid.color, Is.EqualTo(gridBefore));
            Assert.That(marker.color, Is.EqualTo(markerBefore));
            Assert.That(config.WorldSize, Is.EqualTo(worldBefore));
            Assert.That(config.CoordinatesToUV(coordinate), Is.EqualTo(uvBefore));
            Assert.That(config.UVToCoordinates(uvBefore), Is.EqualTo(coordinate));
            Assert.That(config.GridColumns, Is.EqualTo(columnsBefore));
            Assert.That(config.GridRows, Is.EqualTo(rowsBefore));
            Assert.That(config.GridSize, Is.EqualTo(50f));
        }

        [UnityTest]
        public IEnumerator ReopeningAndLiveOpacityEditsRefreshEveryLayerWithoutStoppingScaleAnimation()
        {
            panel.SetActive(true);
            yield return null;
            Assert.That(presentation.lightImages[0].color.a, Is.EqualTo(.35f));
            panel.SetActive(false);
            presentation.lightImages[0].color = new Color(.5f, .6f, .7f, 1f);
            config.ConfigureLightOpacity(.225f);
            panel.SetActive(true);
            Assert.That(presentation.lightImages[0].color.a, Is.EqualTo(.225f));
            Assert.That(presentation.lightImages[1].color.a, Is.EqualTo(.225f));
            config.ConfigureLightOpacity(.3f, 0f, .25f);
            yield return null;
            yield return null;
            Assert.That(presentation.lightImages[0].color.a, Is.Zero);
            Assert.That(presentation.lightImages[1].color.a, Is.EqualTo(.25f));
            Assert.That(presentation.lightImages[0].rectTransform.localScale.x, Is.GreaterThanOrEqualTo(1f));
            Assert.That(presentation.lightImages[0].rectTransform.localScale.x, Is.LessThanOrEqualTo(1.08f));
            Assert.That(ZoneMapPresentation.EvaluateLightScale(5f, 5f, 1.08f), Is.EqualTo(1.08f).Within(.0001f));
            Assert.That(ZoneMapPresentation.EvaluateLightScale(10f, 5f, 1.08f), Is.EqualTo(1f).Within(.0001f));
        }

        [Test]
        public void AlternateArtAndReboundZoneUseOpacityConfigEvenWhenAlternateStateIsCached()
        {
            mission = ScriptableObject.CreateInstance<ZoneMissionConfig>();
            mission.zoneGate = new ZoneGateConfig {
                objectives = new[] { new MissionObjectiveConfig { id = "DESTROY", type = MissionObjectiveType.DestroyObstacle, targetId = "ROCK" } }
            };
            config.missionConfig = mission;
            config.alternateObjectiveId = "DESTROY";
            config.alternateMap = alternate;
            var runtime = panel.AddComponent<ZoneMissionRuntime>();
            runtime.config = mission;
            presentation.MissionRuntime = runtime;
            Assert.That(runtime.RecordGlobalObjective(MissionObjectiveType.DestroyObstacle, "ROCK"), Is.True);
            presentation.RefreshMap();
            Assert.That(presentation.mapImage.texture, Is.SameAs(alternate));
            config.ConfigureLightOpacity(.225f);
            presentation.RefreshMap();
            Assert.That(presentation.lightImages[0].color.a, Is.EqualTo(.225f));
            Assert.That(presentation.lightImages[1].color.a, Is.EqualTo(.225f));
            replacement = ScriptableObject.CreateInstance<ZoneMapConfig>();
            replacement.map = map;
            replacement.ConfigureLightOpacity(.3f);
            presentation.config = replacement;
            presentation.RefreshMap();
            Assert.That(presentation.mapImage.texture, Is.SameAs(map));
            Assert.That(presentation.lightImages[0].color.a, Is.EqualTo(.3f));
        }

        [UnityTest]
        public IEnumerator InstalledZoneMapsHaveTunedOpacityAndZoneOneRemainsUnchanged()
        {
            yield return SceneManager.LoadSceneAsync("Zone01");
            yield return null;
            var world = Object.FindAnyObjectByType<WorldMapController>();
            Assert.That(world, Is.Not.Null);
            var expected = new[] { 1f, .35f, .225f, .3f };
            for (int zone = 0; zone < world.zoneMaps.Length; zone++)
            {
                var installed = world.zoneMaps[zone].GetComponent<ZoneMapPresentation>();
                Assert.That(installed.config.LightOpacityConfigured, Is.True);
                installed.RefreshMap();
                for (int i = 0; i < installed.lightImages.Length; i++)
                {
                    Assert.That(installed.config.LightOpacityFor(i), Is.EqualTo(expected[zone]).Within(.0001f));
                    Assert.That(installed.lightImages[i].color.a, Is.EqualTo(expected[zone]).Within(.0001f));
                    Assert.That(installed.lightImages[i].texture, Is.SameAs(installed.config.lightLayers[i]));
                    Assert.That(installed.lightImages[i].raycastTarget, Is.False);
                }
                Assert.That(installed.config.GridSize, Is.EqualTo(50f));
            }
            Assert.That(world.zoneMaps[2].GetComponent<ZoneMapPresentation>().lightImages, Has.Length.EqualTo(2));
        }
    }
}

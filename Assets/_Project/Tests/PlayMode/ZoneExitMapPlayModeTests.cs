using System.Reflection;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ZoneExitMapPlayModeTests
    {
        private GameObject owner, card;
        private ZoneMapConfig map;
        private ZoneMissionConfig missions;
        private ZoneMissionRuntime runtime;
        private PhotoSurveyMap overlay;

        [SetUp] public void Setup()
        {
            owner = new GameObject("Exit map fixture", typeof(RectTransform));
            var nav = owner.AddComponent<ZoneNavigation>();
            runtime = owner.AddComponent<ZoneMissionRuntime>();
            overlay = owner.AddComponent<PhotoSurveyMap>();
            map = ScriptableObject.CreateInstance<ZoneMapConfig>();
            map.ConfigureDisplaySize(new Vector2(2000, 1000));
            map.destinationZone = "Zone03";
            map.exitArea = new MapPoi { mapPosition = new Vector2(1830, 75), arrivalRadius = 75 };
            missions = ScriptableObject.CreateInstance<ZoneMissionConfig>();
            missions.locations = new[] { new MissionLocationConfig { id = "fieldwork", objectives = new[] {
                new MissionObjectiveConfig { id = "fieldwork-photo", type = MissionObjectiveType.Photograph } } } };
            missions.zoneGate.objectives = new[] { new MissionObjectiveConfig { id = "gate-install", type = MissionObjectiveType.InstallUpgrade } };
            runtime.config = missions;
            overlay.mapConfig = map; overlay.missionRuntime = runtime; overlay.navigation = nav;
            overlay.showGrid = false;
            overlay.rectTransform.sizeDelta = new Vector2(1000, 1000);
            nav.RestoreVoyage(new Vector2(100, 400), 0, 230, 0);
            card = new GameObject("Exit hint card", typeof(RectTransform));
            card.transform.SetParent(owner.transform, false);
            var label = new GameObject("Hint", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            label.transform.SetParent(card.transform, false);
            overlay.taskReadout = label.GetComponent<UnityEngine.UI.Text>();
            overlay.taskReadout.raycastTarget = false;
        }

        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(map);
            Object.DestroyImmediate(missions);
        }

        [Test] public void DisplayedExitBoundaryUsesSameCircleAsContainsEvenWhenChartStretches()
        {
            CompleteAll();
            Assert.That(overlay.ExitApproachVisible, Is.True);
            Assert.That(overlay.ExitApproachRadii, Is.EqualTo(new Vector2(37.5f, 75)));
            using var vertices = Mesh();
            Assert.That(vertices.currentVertCount, Is.EqualTo(64 * 4 + 16), "The diamond remains alongside the radius outline.");
            // Line's paired vertices straddle each endpoint. Recover the center-line endpoints,
            // then map them back to the same gameplay coordinates used by MapPoi.Contains.
            for (int i = 0; i < 64 * 4; i += 4)
            {
                var first = new UIVertex(); var second = new UIVertex();
                vertices.PopulateUIVertex(ref first, i); vertices.PopulateUIVertex(ref second, i + 1);
                Vector2 point = (first.position + second.position) * .5f;
                Vector2 uv = new((point.x - overlay.rectTransform.rect.xMin) / overlay.rectTransform.rect.width,
                    (point.y - overlay.rectTransform.rect.yMin) / overlay.rectTransform.rect.height);
                Vector2 world = map.UVToCoordinates(uv);
                Assert.That(Vector2.Distance(world, map.exitArea.mapPosition), Is.EqualTo(map.exitArea.arrivalRadius).Within(.001f));
                Vector2 inward = Vector2.MoveTowards(world, map.exitArea.mapPosition, .01f);
                Vector2 outward = world + (world - map.exitArea.mapPosition).normalized * .01f;
                Assert.That(map.exitArea.Contains(inward), Is.True);
                Assert.That(map.exitArea.Contains(outward), Is.False);
            }
            Assert.That(overlay.raycastTarget, Is.False);
        }

        [Test] public void InspectorRadiusChangeUpdatesBothRenderedBoundsAndArrivalRule()
        {
            CompleteAll();
            var probe = map.exitArea.mapPosition + Vector2.right * 90;
            Assert.That(map.exitArea.Contains(probe), Is.False);
            map.exitArea.arrivalRadius = 100;
            Assert.That(map.exitArea.Contains(probe), Is.True);
            Assert.That(overlay.ExitApproachRadii, Is.EqualTo(new Vector2(50, 100)));
            overlay.rectTransform.sizeDelta = new Vector2(800, 400);
            Assert.That(overlay.ExitApproachRadii, Is.EqualTo(new Vector2(40, 40)), "Only rendering changes with UI scale.");
            Assert.That(map.exitArea.Contains(probe), Is.True);
        }

        [Test] public void LockedExitKeepsDiamondAndReportsFieldworkThenUpgradeReason()
        {
            Assert.That(overlay.ExitApproachVisible, Is.False);
            using (var mesh = Mesh()) Assert.That(mesh.currentVertCount, Is.EqualTo(16));
            overlay.SetPointer(map.CoordinatesToUV(map.exitArea.mapPosition));
            Assert.That(card.activeSelf, Is.True);
            Assert.That(overlay.taskReadout.text, Does.Contain("EXIT LOCKED").And.Contain("khảo sát"));
            runtime.RestoreProgress(new MissionProgressState { completedObjectives = new() { "fieldwork-photo" } });
            overlay.SetPointer(map.CoordinatesToUV(map.exitArea.mapPosition));
            Assert.That(overlay.taskReadout.text, Does.Contain("EXIT LOCKED").And.Contain("nâng cấp"));
        }

        [Test] public void ReadyAndApproachHintsReadConfiguredExitWithoutNewButton()
        {
            CompleteAll();
            overlay.SetPointer(map.CoordinatesToUV(map.exitArea.mapPosition));
            Assert.That(overlay.taskReadout.text, Does.Contain("READY").And.Contain("1830, 75"));
            overlay.navigation.RestoreVoyage(map.exitArea.mapPosition + Vector2.left * 90, 0, 230, 0);
            overlay.SetPointer(null);
            Assert.That(card.activeSelf, Is.True);
            Assert.That(overlay.taskReadout.text, Does.Contain("APPROACH").And.Contain("vùng viền"));
            overlay.navigation.RestoreVoyage(map.exitArea.mapPosition, 0, 230, 0);
            overlay.SetPointer(null);
            Assert.That(overlay.taskReadout.text, Does.Contain("APPROACH").And.Contain("Di chuyển tàu"));
            Assert.That(card.GetComponentsInChildren<UnityEngine.UI.Button>().Length, Is.Zero);
        }

        [TestCase("Zone01", 1875, 680, "Zone02")]
        [TestCase("Zone02", 1830, 75, "Zone03")]
        [TestCase("Zone03", 1750, 25, "Zone04")]
        public void EveryZoneExitUsesConfiguredCenterAndSeventyFiveUnitBoundary(string zone, float x, float y, string destination)
        {
            map.zoneId = zone; map.destinationZone = destination;
            map.ConfigureDisplaySize(new Vector2(1920, 1080));
            map.exitArea.mapPosition = new Vector2(x, y);
            map.exitArea.arrivalRadius = 75;
            CompleteAll();
            using var vertices = Mesh();
            AssertBoundary(vertices, map.exitArea);
            Assert.That(overlay.ExitPrompt, Does.Contain($"({x:0}, {y:0})"));
            Assert.That(map.exitArea.Contains(map.exitArea.mapPosition + Vector2.left * 74.99f), Is.True);
            Assert.That(map.exitArea.Contains(map.exitArea.mapPosition + Vector2.left * 75.01f), Is.False);
        }

        [Test] public void HiddenFinalHasNoNormalExitAndStaysInvisibleUntilEveryDiscovery()
        {
            ConfigureHiddenRoute();
            runtime.HiddenRouteAvailable = true;
            var progress = runtime.ExportProgress(); progress.revealedLocations.AddRange(new[] { "hidden-1", "hidden-2" });
            runtime.RestoreProgress(progress);
            Assert.That(overlay.FinalApproachVisible, Is.False);
            using (var vertices = Mesh()) Assert.That(vertices.currentVertCount, Is.Zero, "No final destination spoiler while discovery is incomplete.");
            overlay.SetPointer(map.CoordinatesToUV(map.finalHiddenPoint.mapPosition));
            Assert.That(card.activeSelf, Is.False);
            Assert.That(overlay.FinalPrompt, Does.Contain("LOCKED"));
            progress.revealedLocations.Add("hidden-3"); runtime.RestoreProgress(progress);
            Assert.That(overlay.ExitApproachVisible, Is.False);
            Assert.That(overlay.FinalApproachVisible, Is.True);
            using (var vertices = Mesh()) AssertBoundary(vertices, map.finalHiddenPoint);
            overlay.SetPointer(map.CoordinatesToUV(map.finalHiddenPoint.mapPosition));
            Assert.That(card.activeSelf, Is.True);
            Assert.That(overlay.taskReadout.text, Does.Contain("FINAL SIGNAL").And.Contain("1500, 150"));
            runtime.HiddenRouteAvailable = false;
            Assert.That(overlay.FinalApproachVisible, Is.False, "Discovery data alone cannot bypass the ending choice.");
        }

        [Test] public void InspectorFinalRadiusUpdatesRenderedRegionAndContainsWithoutChangingNormalRoute()
        {
            ConfigureHiddenRoute(); runtime.HiddenRouteAvailable = true;
            var progress = runtime.ExportProgress(); progress.revealedLocations.AddRange(map.hiddenLocationIds);
            runtime.RestoreProgress(progress);
            var probe = map.finalHiddenPoint.mapPosition + Vector2.left * 90;
            Assert.That(map.finalHiddenPoint.Contains(probe), Is.False);
            map.finalHiddenPoint.arrivalRadius = 100;
            Assert.That(map.finalHiddenPoint.Contains(probe), Is.True);
            Assert.That(overlay.FinalApproachRadii, Is.EqualTo(new Vector2(50, 100)));
            using (var vertices = Mesh()) AssertBoundary(vertices, map.finalHiddenPoint);
            overlay.navigation.RestoreVoyage(map.finalHiddenPoint.mapPosition, 0, 500, 0);
            overlay.SetPointer(null);
            Assert.That(overlay.taskReadout.text, Does.Contain("APPROACH").And.Contain("Di chuyển tàu"));
            Assert.That(map.destinationZone, Is.Null.Or.Empty);
        }

        [Test] public void UnknownHiddenLocationCannotUnlockFinalPresentation()
        {
            ConfigureHiddenRoute(); runtime.HiddenRouteAvailable = true;
            map.hiddenLocationIds = new[] { "missing-location" };
            Assert.That(map.HiddenDestinationAvailable(runtime), Is.False);
            using var vertices = Mesh(); Assert.That(vertices.currentVertCount, Is.Zero);
        }

        private void ConfigureHiddenRoute()
        {
            map.destinationZone = "";
            map.finalHiddenPoint = new MapPoi { id = "final", mapPosition = new Vector2(1500, 150), arrivalRadius = 75 };
            map.hiddenLocationIds = new[] { "hidden-1", "hidden-2", "hidden-3" };
            missions.locations = System.Array.ConvertAll(map.hiddenLocationIds, id => new MissionLocationConfig {
                id = id, poiId = id, visibility = LocationVisibility.HiddenRadar,
                objectives = new[] { new MissionObjectiveConfig { id = id + "-photo", type = MissionObjectiveType.Photograph } } });
        }

        private void AssertBoundary(VertexHelper vertices, MapPoi point)
        {
            Assert.That(vertices.currentVertCount, Is.EqualTo(64 * 4 + 16));
            for (int i = 0; i < 64 * 4; i += 4)
            {
                var a = new UIVertex(); var b = new UIVertex();
                vertices.PopulateUIVertex(ref a, i); vertices.PopulateUIVertex(ref b, i + 1);
                Vector2 local = (a.position + b.position) * .5f;
                Vector2 offset = local - overlay.rectTransform.rect.min;
                Vector2 uv = new(offset.x / overlay.rectTransform.rect.width, offset.y / overlay.rectTransform.rect.height);
                Assert.That(Vector2.Distance(map.UVToCoordinates(uv), point.mapPosition), Is.EqualTo(point.arrivalRadius).Within(.002f));
            }
            var first = new UIVertex(); var second = new UIVertex();
            vertices.PopulateUIVertex(ref first, 64 * 4); vertices.PopulateUIVertex(ref second, 64 * 4 + 1);
            Vector2 diamondTop = (first.position + second.position) * .5f;
            Vector2 expectedCenter = overlay.rectTransform.rect.min + Vector2.Scale(map.CoordinatesToUV(point.mapPosition), overlay.rectTransform.rect.size);
            Assert.That(Vector2.Distance(diamondTop, expectedCenter + Vector2.up * 12), Is.LessThan(.001f));
        }

        private void CompleteAll() => runtime.RestoreProgress(new MissionProgressState {
            completedObjectives = new() { "fieldwork-photo", "gate-install" } });
        private VertexHelper Mesh()
        {
            var result = new VertexHelper();
            typeof(PhotoSurveyMap).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null).Invoke(overlay, new object[] { result });
            return result;
        }
    }
}

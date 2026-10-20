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
        [Test] public void ReturnOutlineKeepsAuthoredCircleAndHistoricalHintIgnoresOtherZoneVessel()
        {
            map.destinationZone = ""; map.returnDestinationZone = "Zone01";
            map.returnArea = new MapPoi { id = "return", mapPosition = new Vector2(100, 900), arrivalRadius = 75 };
            Assert.That(overlay.ReturnApproachVisible, Is.True);
            Assert.That(overlay.ReturnApproachRadii, Is.EqualTo(new Vector2(37.5f, 75)));
            using (var vertices = Mesh())
            {
                Assert.That(vertices.currentVertCount, Is.EqualTo(2 * 64 * 4 + 2 * 16 + 2 * 8));
                for (int ring = 0; ring < 2; ring++)
                    for (int i = ring * 64 * 4; i < (ring + 1) * 64 * 4; i += 4)
                    {
                        var a = new UIVertex(); var b = new UIVertex();
                        vertices.PopulateUIVertex(ref a, i); vertices.PopulateUIVertex(ref b, i + 1);
                        Vector2 local = (a.position + b.position) * .5f;
                        var uv = (local - overlay.rectTransform.rect.min) / overlay.rectTransform.rect.size;
                        Assert.That(Vector2.Distance(map.UVToCoordinates(uv), map.returnArea.mapPosition), Is.EqualTo(75).Within(.002f));
                        Assert.That(a.color.a, Is.GreaterThan(200), "Both strokes stay visible over blue terrain.");
                        if (ring == 0) Assert.That(a.color.b, Is.LessThan(100), "Dark outline separates the cyan marker from the map.");
                    }
            }
            map.entryPosition = map.returnArea.mapPosition;
            overlay.navigation.RestoreVoyage(map.returnArea.mapPosition, 90, 230, 0);
            Assert.That(overlay.ReturnPrompt, Does.Contain("Ra khỏi vùng viền"));
            overlay.navigation = null;
            Assert.That(overlay.ReturnPrompt, Does.Contain("READY").And.Contain("100, 900"));
        }
        [Test] public void CornerReturnRegionCannotDrawAcrossMapAxes()
        {
            map.ConfigureDisplaySize(new Vector2(1920, 1080));
            overlay.rectTransform.sizeDelta = new Vector2(1440, 840);
            map.destinationZone = ""; map.returnDestinationZone = "Zone02";
            map.returnArea = new MapPoi { mapPosition = new Vector2(25, 25), arrivalRadius = 75 };
            using var vertices = Mesh();
            Assert.That(vertices.currentVertCount, Is.GreaterThan(0));
            Assert.That(vertices.currentVertCount, Is.LessThan(2 * 64 * 4 + 2 * 16 + 2 * 8));
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                var vertex = new UIVertex(); vertices.PopulateUIVertex(ref vertex, i);
                Assert.That(vertex.position.x, Is.InRange(overlay.rectTransform.rect.xMin, overlay.rectTransform.rect.xMax));
                Assert.That(vertex.position.y, Is.InRange(overlay.rectTransform.rect.yMin, overlay.rectTransform.rect.yMax));
            }
            Assert.That(map.returnArea.Contains(new Vector2(90, 25)), Is.True, "Clipping cannot shrink gameplay radius.");
        }

        [Test] public void ReturnHintRequiresHoverEvenWhenVesselIsAtEntryOrInsideGate()
        {
            ConfigureReturnGate();
            overlay.navigation.RestoreVoyage(new Vector2(100, 900), 0, 230, 0);
            overlay.SetPointer(null);
            Assert.That(card.activeSelf, Is.False, "The Zone02 entry is near the gate, but proximity is not a hover.");
            overlay.SetPointer(map.CoordinatesToUV(map.returnArea.mapPosition));
            Assert.That(card.activeSelf, Is.True);
            Assert.That(overlay.taskReadout.text, Does.Contain("RETURN GATE"));
            overlay.SetPointer(map.CoordinatesToUV(map.returnArea.mapPosition + Vector2.right * 76));
            Assert.That(card.activeSelf, Is.False, "Leaving the return marker must hide its card.");
            overlay.navigation.RestoreVoyage(map.returnArea.mapPosition, 0, 230, 0);
            overlay.SetPointer(null);
            Assert.That(card.activeSelf, Is.False, "A stationary vessel inside the gate cannot keep its card visible.");
            Assert.That(overlay.ReturnApproachVisible, Is.True, "Hiding information must preserve the gate marker.");
            Assert.That(overlay.raycastTarget, Is.False);
        }

        [Test] public void ReturnHintClearsHoverOnMapReopenAndHistoricalProgressRefresh()
        {
            ConfigureReturnGate();
            overlay.navigation.RestoreVoyage(new Vector2(100, 900), 0, 230, 0);
            overlay.SetPointer(map.CoordinatesToUV(map.returnArea.mapPosition));
            Assert.That(card.activeSelf, Is.True);
            owner.SetActive(false);
            owner.SetActive(true);
            Assert.That(card.activeSelf, Is.False, "Reopening a map starts without a stale hover.");

            overlay.navigation = null;
            overlay.missionRuntime = null;
            overlay.SavedProgress = new SavedZoneMapProgress(map, null, true);
            overlay.RestoreSelection();
            Assert.That(card.activeSelf, Is.False, "Loaded historical return availability is not a hover.");
            overlay.SetPointer(map.CoordinatesToUV(map.returnArea.mapPosition));
            Assert.That(card.activeSelf, Is.True);
            Assert.That(overlay.taskReadout.text, Does.Contain("RETURN GATE").And.Contain("READY"));
            overlay.SetPointer(null);
            Assert.That(card.activeSelf, Is.False);
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
            var progress = runtime.ExportProgress(); progress.revealedLocations.Add("hidden-1");
            runtime.RestoreProgress(progress);
            Assert.That(overlay.FinalApproachVisible, Is.False);
            using (var vertices = Mesh()) Assert.That(vertices.currentVertCount, Is.Zero, "No final destination spoiler while discovery is incomplete.");
            overlay.SetPointer(map.CoordinatesToUV(map.finalHiddenPoint.mapPosition));
            Assert.That(card.activeSelf, Is.False);
            Assert.That(overlay.FinalPrompt, Does.Contain("LOCKED"));
            progress.revealedLocations.Add("hidden-2");
            foreach (string id in map.hiddenLocationIds) progress.completedObjectives.Add(id + "-photo");
            runtime.RestoreProgress(progress);
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
            foreach (string id in map.hiddenLocationIds) progress.completedObjectives.Add(id + "-photo");
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
            map.hiddenLocationIds = new[] { "hidden-1", "hidden-2" };
            missions.locations = System.Array.ConvertAll(map.hiddenLocationIds, id => new MissionLocationConfig {
                id = id, poiId = id, visibility = LocationVisibility.HiddenRadar,
                objectives = new[] { new MissionObjectiveConfig { id = id + "-photo", type = MissionObjectiveType.Photograph } } });
            var locations = new System.Collections.Generic.List<MissionLocationConfig>(missions.locations);
            locations.Add(new MissionLocationConfig { id = "main", poiId = "main", objectives = new[] {
                new MissionObjectiveConfig { id = "main-photo", type = MissionObjectiveType.Photograph } } });
            missions.locations = locations.ToArray(); missions.zoneGate = new ZoneGateConfig();
            runtime.RestoreProgress(new MissionProgressState { completedObjectives = new() { "main-photo" } });
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
        private void ConfigureReturnGate()
        {
            map.ConfigureDisplaySize(new Vector2(1920, 1080));
            map.destinationZone = "";
            map.returnDestinationZone = "Zone01";
            map.returnArea = new MapPoi { id = "zone02-return", mapPosition = new Vector2(100, 1000), arrivalRadius = 75 };
        }
        private VertexHelper Mesh()
        {
            var result = new VertexHelper();
            typeof(PhotoSurveyMap).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null).Invoke(overlay, new object[] { result });
            return result;
        }
    }
}

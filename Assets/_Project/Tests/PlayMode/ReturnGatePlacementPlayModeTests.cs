using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ReturnGatePlacementPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private bool oldDebug;
        private SceneFlowController flow;
        private ZoneMapConfig temporaryMap;
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private ExpeditionProgression Route => Cabin.GetComponent<ExpeditionProgression>();

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ReturnPlacement-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            oldDebug = ExpeditionSaveStore.IsDebugTimeline;
            ExpeditionSaveStore.SelectNormalTimeline();
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return Wait();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            if (temporaryMap != null) Object.Destroy(temporaryMap);
            yield return null;
            if (oldDebug) ExpeditionSaveStore.SelectDebugTimeline(); else ExpeditionSaveStore.SelectNormalTimeline();
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        private IEnumerator StartZone(int zone)
        {
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>();
            menu.Open(); menu.SelectZone(zone); menu.StartButton.onClick.Invoke(); yield return Wait();
            Assert.That(Loop.Zone, Is.EqualTo($"Zone0{zone}"));
        }

        [UnityTest] public IEnumerator AuthoredPlacementAndReturnMarkersFollowCurrentZoneAfterContinue()
        {
            for (int zone = 2; zone <= 4; zone++)
            {
                yield return StartZone(zone);
                var map = Loop.ActiveMap;
                Cabin.OpenCurrentMap(); yield return null;
                var overlay = Cabin.MapPanel.GetComponent<ZoneMapPresentation>().overlay;
                Assert.That(overlay.ReturnApproachVisible, Is.True, map.zoneId);
                Assert.That(overlay.mapConfig, Is.SameAs(map));
                overlay.SetPointer(null);
                Assert.That(overlay.styledTaskReadout.transform.parent.gameObject.activeSelf, Is.False,
                    "A newly opened map must not show return information just because the vessel is near the gate.");
                overlay.SetPointer(map.CoordinatesToUV(map.returnArea.mapPosition));
                yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, $"return-gate-zone{zone}.png");
                if (zone == 2)
                    yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "return-gate-zone2-1366.png", 1366, 768);
                var expected = zone == 3 ? new Vector2(25, 25) : new Vector2(100, 1000);
                Assert.That(map.returnArea.mapPosition, Is.EqualTo(expected));
                if (zone == 2) Assert.That(map.entryPosition, Is.EqualTo(new Vector2(100, 900)));
                Assert.That(Cabin.Navigation.CanOccupy(Cabin.Navigation.Position), Is.True);
                Assert.That(Cabin.Navigation.CanOccupy(expected), Is.True);
                foreach (var poi in map.locations)
                    Assert.That(Vector2.Distance(poi.mapPosition, expected),
                        Is.GreaterThan(poi.arrivalRadius + map.returnArea.arrivalRadius), poi.id);
                overlay.SetPointer(map.CoordinatesToUV(map.returnArea.mapPosition));
                Assert.That(overlay.styledTaskReadout.transform.parent.gameObject.activeSelf, Is.True);
                Assert.That(overlay.styledTaskReadout.text, Does.Contain("RETURN GATE"));
                var card = (RectTransform)overlay.styledTaskReadout.transform.parent;
                foreach (var graphic in card.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    Assert.That(graphic.raycastTarget, Is.False, $"Return tooltip {graphic.name} must not block chart input.");
                Vector2 markerLocal = overlay.rectTransform.rect.min + Vector2.Scale(map.CoordinatesToUV(expected), overlay.rectTransform.rect.size);
                var canvas = overlay.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector2 markerScreen = RectTransformUtility.WorldToScreenPoint(camera, overlay.transform.TransformPoint(markerLocal));
                Assert.That(RectTransformUtility.RectangleContainsScreenPoint(card, markerScreen, camera), Is.False,
                    "The return tooltip must never hide the marker.");
                Assert.That(flow.IsTransitioning, Is.False, "Viewing a return marker cannot travel.");
                overlay.SetPointer(null);
                Assert.That(card.gameObject.activeSelf, Is.False, "Leaving the marker must hide return information.");
                overlay.SetPointer(map.CoordinatesToUV(map.returnArea.mapPosition));
                var position = Cabin.Navigation.Position;
                Cabin.ClosePanel();
                Assert.That(card.gameObject.activeSelf, Is.False, "Closing the map must clear its hover.");
                Assert.That(Loop.SaveCurrent(), Is.True);
                flow.LoadMainMenu(); yield return Wait(); flow.ContinueDebugGame(); yield return Wait();
                Assert.That(Cabin.Navigation.Position, Is.EqualTo(position));
                Assert.That(Loop.ActiveMap.returnArea.mapPosition, Is.EqualTo(expected));
                Cabin.OpenCurrentMap(); yield return null;
                var loadedOverlay = Cabin.MapPanel.GetComponent<ZoneMapPresentation>().overlay;
                Assert.That(loadedOverlay.ReturnApproachVisible, Is.True);
                Assert.That(loadedOverlay.styledTaskReadout.transform.parent.gameObject.activeSelf, Is.False,
                    "Continue restores the gate marker without restoring transient pointer information.");
                loadedOverlay.SetPointer(map.CoordinatesToUV(map.returnArea.mapPosition));
                Assert.That(loadedOverlay.styledTaskReadout.transform.parent.gameObject.activeSelf, Is.True);
                loadedOverlay.SetPointer(null);
                Assert.That(loadedOverlay.styledTaskReadout.transform.parent.gameObject.activeSelf, Is.False);
                flow.LoadMainMenu(); yield return Wait();
            }
        }

        [UnityTest] public IEnumerator MovementAtOverlappingEntryCannotBounceBackToPreviousZone()
        {
            yield return StartZone(3);
            var presentation = Cabin.MapPanel.GetComponent<ZoneMapPresentation>();
            var authored = presentation.config;
            // Reproduce the requested overlap before editing assets; never mutate the authored config.
            temporaryMap = Object.Instantiate(authored);
            temporaryMap.returnArea.mapPosition = new Vector2(25, 25);
            presentation.config = temporaryMap;
            try
            {
                Assert.That(Cabin.GetComponent<CabinZoneSession>().Configure("Zone03"), Is.True);
                Cabin.Navigation.RestoreVoyage(temporaryMap.entryPosition, 90, temporaryMap.entryDepth, 0);
                Assert.That(temporaryMap.returnArea.Contains(Cabin.Navigation.Position), Is.True);
                Route.Evaluate(); Assert.That(Route.ReturnArmed, Is.False);
                Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
                Assert.That(flow.IsTransitioning, Is.False, "Fresh movement at an overlapping spawn is not intentional reentry.");
                Assert.That(Loop.Zone, Is.EqualTo("Zone03"));
                Assert.That(Route.ReturnArmed, Is.False);
            }
            finally { if (presentation != null) presentation.config = authored; }
        }

        [UnityTest] public IEnumerator ContinueAndRestInsideOverlappingReturnRequireExitThenPhysicalReentry()
        {
            yield return StartZone(3);
            var map = Loop.ActiveMap; var nav = Cabin.Navigation;
            Assert.That(map.returnArea.mapPosition, Is.EqualTo(new Vector2(25, 25)));
            nav.RestoreVoyage(map.returnArea.mapPosition, 90, map.entryDepth, 0);
            Assert.That(Loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return Wait(); flow.ContinueDebugGame(); yield return Wait();
            Route.Evaluate(); nav = Cabin.Navigation; map = Loop.ActiveMap;
            nav.Step(1, 0, .2f); nav.Brake(); Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(Route.ReturnArmed, Is.False);
            Assert.That(Loop.Rest(), Is.True); Route.Evaluate();
            nav.Step(1, 0, .2f); nav.Brake(); Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(Route.ReturnArmed, Is.False);

            // Pick a footprint-clear ray rather than assuming the eastward terrain is open.
            float heading = ClearOutwardHeading(nav, map.returnArea);
            nav.RestoreVoyage(nav.Position, heading, nav.Depth, nav.DistanceTravelled); Route.Evaluate();
            // Drive real navigation across the boundary, then turn and reenter. No mission injection.
            for (int i = 0; i < 100 && map.returnArea.Contains(nav.Position); i++) nav.Step(1, 0, .1f);
            nav.Brake(); Assert.That(map.returnArea.Contains(nav.Position), Is.False);
            Route.Evaluate(); Assert.That(Route.ReturnArmed, Is.True);
            nav.Step(0, 1, 4.5f); nav.Brake(); // Reverse heading at the authored 40 degrees/s.
            for (int i = 0; i < 30 && !map.returnArea.Contains(nav.Position); i++) nav.Step(1, 0, .1f);
            nav.Brake(); Assert.That(map.returnArea.Contains(nav.Position), Is.True);
            Route.Evaluate(); yield return Wait();
            Assert.That(Loop.Zone, Is.EqualTo("Zone02"));
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.backwardArrivalPosition));
            Assert.That(Loop.Day, Is.EqualTo(2));
        }
        private static float ClearOutwardHeading(ZoneNavigation nav, MapPoi area)
        {
            for (int i = 0; i < 64; i++)
            {
                float heading = i * 360f / 64;
                Vector2 direction = new(Mathf.Sin(heading * Mathf.Deg2Rad), Mathf.Cos(heading * Mathf.Deg2Rad));
                for (int distance = 1; distance <= 160; distance++)
                {
                    Vector2 point = nav.Position + direction * distance;
                    if (!nav.CanOccupy(point)) break;
                    if (!area.Contains(point)) return heading;
                }
            }
            Assert.Fail("No clear route out of return area."); return 0;
        }

        private IEnumerator Wait()
        {
            yield return null; float deadline = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
    }
}

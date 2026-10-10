using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ZoneExitFlowPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionProgression Route => Cabin.GetComponent<ExpeditionProgression>();

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ZoneExit-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return null;
            flow = SceneFlowController.Instance;
            yield return Transition();
            flow.StartNewGame(); yield return Transition();
            Assert.That(Loop.IsInitialized, Is.True, Loop.LastError);
            CompleteRequiredObjectives();
            Route.Evaluate();
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.exitArea.mapPosition, 0, Loop.ActiveMap.entryDepth, Cabin.Navigation.DistanceTravelled);
            flow.LoadZone("Zone02"); yield return Transition();
            Assert.That(Loop.Zone, Is.EqualTo("Zone02"));
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [UnityTest] public IEnumerator LockedInsideNeverTransfersAndUnlockNeedsActualTravel()
        {
            var nav = Cabin.Navigation;
            var exit = Loop.ActiveMap.exitArea;
            Assert.That(nav.CanOccupy(exit.mapPosition), Is.True, "Authored exit center must admit the vessel footprint.");
            nav.RestoreVoyage(exit.mapPosition, 0, Loop.ActiveMap.entryDepth, nav.DistanceTravelled);
            Route.Evaluate();
            nav.Step(1, 0, .1f); nav.Brake(); Route.Evaluate();
            Assert.That(Loop.Zone, Is.EqualTo("Zone02")); Assert.That(Route.CanExit(), Is.False);
            CompleteRequiredObjectives(); Route.Evaluate();
            Assert.That(Loop.CurrentProgress.exitUnlocked, Is.True);
            Assert.That(Route.ExitArmed, Is.False, "Unlocking inside is not an arrival.");
            Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Route.ExitNearby, Is.True); Assert.That(Route.ExitPrompt, Does.Contain("APPROACH"));
            nav.Navigate(0, 1, 1, .2f); nav.Brake(); Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False, "Turn/depth changes cannot masquerade as approaching the exit.");
            nav.Step(1, 0, .2f); nav.Brake(); Route.Evaluate();
            float departureDepth = nav.Depth;
            yield return Transition();
            AssertZoneThreeEntry(departureDepth);
        }

        [UnityTest] public IEnumerator ContinueInsideStaysPutUntilIntentionalMovementAndTriggersOnlyOnce()
        {
            var exit = Loop.ActiveMap.exitArea;
            Cabin.Navigation.RestoreVoyage(exit.mapPosition, 180, Loop.ActiveMap.entryDepth, 125);
            CompleteRequiredObjectives(); Route.Evaluate();
            Assert.That(Loop.SaveCurrent(), Is.True);
            yield return Continue();
            Route.Evaluate(); Route.Evaluate();
            Assert.That(Loop.Zone, Is.EqualTo("Zone02")); Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(exit.mapPosition)); Assert.That(Route.ExitArmed, Is.False);
            var nav = Cabin.Navigation;
            nav.RestoreVoyage(nav.Position, nav.Heading, nav.Depth, nav.DistanceTravelled);
            Route.Evaluate(); Assert.That(flow.IsTransitioning, Is.False, "Restoring the saved position is not movement.");
            nav.Step(1, 0, .2f); nav.Brake();
            Route.Evaluate(); Route.Evaluate(); Route.Evaluate();
            yield return Transition(); AssertZoneThreeEntry();
            for (int i = 0; i < 3; i++) Route.Evaluate();
            Assert.That(Loop.Zone, Is.EqualTo("Zone03")); Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
            Assert.That(saved.current.zones.FindAll(z => z.zone == "Zone03").Count, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator ContinueNearOutsideDoesNotTransferUntilEnteringDisplayedRadius()
        {
            CompleteRequiredObjectives(); Route.Evaluate();
            var config = Loop.ActiveMap;
            Vector2 outside = ReachableApproach();
            Vector2 delta = config.exitArea.mapPosition - outside;
            float heading = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            Cabin.Navigation.RestoreVoyage(outside, heading, config.entryDepth, 75);
            Route.Evaluate();
            Assert.That(Route.ExitArmed, Is.True); Assert.That(Route.CanExit(), Is.False);
            Assert.That(Loop.SaveCurrent(), Is.True);
            yield return Continue();
            Route.Evaluate();
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(outside));
            Assert.That(Loop.Zone, Is.EqualTo("Zone02")); Assert.That(flow.IsTransitioning, Is.False);
            Cabin.OpenMap(); yield return null;
            var overlay = Cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>(true);
            Assert.That(overlay.mapConfig, Is.SameAs(Loop.ActiveMap));
            Assert.That(overlay.ExitApproachVisible, Is.True);
            Vector2 expected = Vector2.Scale(overlay.rectTransform.rect.size,
                new Vector2(config.exitArea.arrivalRadius / config.WorldSize.x, config.exitArea.arrivalRadius / config.WorldSize.y));
            Assert.That(overlay.ExitApproachRadii, Is.EqualTo(expected));
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "zone02-exit-preserved.png", 1366, 768);
            Cabin.ClosePanel();
            for (int i = 0; i < 20 && !config.exitArea.Contains(Cabin.Navigation.Position); i++)
                Cabin.Navigation.Step(1, 0, .1f);
            Cabin.Navigation.Brake();
            Assert.That(config.exitArea.Contains(Cabin.Navigation.Position), Is.True, "The verified approach should enter the authored radius.");
            Route.Evaluate(); yield return Transition(); AssertZoneThreeEntry();
        }

        [UnityTest] public IEnumerator CompletedMissionsOutsideDoNotAutoTransfer()
        {
            CompleteRequiredObjectives(); Route.Evaluate();
            Assert.That(Loop.CurrentProgress.exitUnlocked, Is.True);
            Assert.That(Route.CanExit(), Is.False);
            Route.Evaluate(); yield return new WaitForSecondsRealtime(.4f);
            Assert.That(Loop.Zone, Is.EqualTo("Zone02")); Assert.That(flow.IsTransitioning, Is.False);
        }

        [UnityTest] public IEnumerator SameZoneCheckpointWithHigherSavedDistanceDoesNotBecomeNewArrival()
        {
            var nav = Cabin.Navigation;
            var exit = Loop.ActiveMap.exitArea;
            nav.RestoreVoyage(exit.mapPosition, 0, Loop.ActiveMap.entryDepth, 100);
            CompleteRequiredObjectives(); Route.Evaluate();
            Assert.That(Route.ExitArmed, Is.False);
            Assert.That(Loop.Rest(), Is.True);
            Route.Evaluate();
            // Establish a newer session whose observed distance is below the restored checkpoint.
            nav.RestoreVoyage(exit.mapPosition, 0, nav.Depth, 0);
            Route.Evaluate();
            Assert.That(Route.ExitArmed, Is.False);
            Assert.That(Loop.RestoreDay(1), Is.True, Loop.LastError);
            Assert.That(nav.DistanceTravelled, Is.EqualTo(100));
            Route.Evaluate(); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Loop.Zone, Is.EqualTo("Zone02")); Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Route.ExitArmed, Is.False, "Saved travel history must not auto-arm a restored inside position.");
            nav.Step(1, 0, .2f); nav.Brake(); Route.Evaluate();
            yield return Transition(); AssertZoneThreeEntry();
        }

        [UnityTest] public IEnumerator ZoneOneKeepsUpgradeGateAndAutomaticallyTransfersThroughExpandedRadius()
        {
            flow.StartNewGame(); yield return Transition();
            var config = Loop.ActiveMap; var nav = Cabin.Navigation;
            Assert.That(config.exitArea.mapPosition, Is.EqualTo(new Vector2(1875, 680)));
            Assert.That(config.exitArea.arrivalRadius, Is.EqualTo(75));
            RestoreOutside(config.exitArea); Route.Evaluate();
            MoveInto(config.exitArea); Route.Evaluate();
            Assert.That(Loop.Zone, Is.EqualTo("Zone01")); Assert.That(Route.CanExit(), Is.False);
            CompleteRequiredObjectives(false); Route.Evaluate();
            Assert.That(Loop.MissionRuntime.MainLocationsComplete, Is.True);
            Assert.That(Loop.MissionRuntime.MainObjectivesComplete, Is.False, "Pressure Hull still gates the route.");
            var story = Cabin.GetComponent<ZoneOneStory>();
            Assert.That(story.InstallHull(), Is.True); Route.Evaluate();
            Assert.That(Route.ExitArmed, Is.False, "Unlocking inside is not an arrival.");
            Assert.That(flow.IsTransitioning, Is.False);
            Cabin.OpenMap(); yield return null;
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "zone01-exit-expanded.png");
            Cabin.ClosePanel();
            nav.Step(1, 0, .2f); nav.Brake(); Route.Evaluate(); Route.Evaluate();
            yield return Transition(); AssertEntry("Zone02");
        }

        [UnityTest] public IEnumerator ZoneOneContinueInsideRequiresFreshMovementNotRestoredTravelHistory()
        {
            flow.StartNewGame(); yield return Transition();
            CompleteRequiredObjectives(); Route.Evaluate();
            var point = Loop.ActiveMap.exitArea.mapPosition;
            Cabin.Navigation.RestoreVoyage(point, 0, Loop.ActiveMap.entryDepth, 1000); Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Continue();
            Route.Evaluate(); Assert.That(Route.ExitArmed, Is.False); Assert.That(Loop.Zone, Is.EqualTo("Zone01"));
            Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
            yield return Transition(); AssertEntry("Zone02");
        }

        [UnityTest] public IEnumerator PositionRestoreCannotTransferEvenAfterOutsideMovementArmedZoneTwo()
        {
            CompleteRequiredObjectives(); Route.Evaluate();
            var nav = Cabin.Navigation; var exit = Loop.ActiveMap.exitArea;
            RestoreOutside(exit); Route.Evaluate();
            nav.Step(1, 0, .1f); nav.Brake(); Route.Evaluate();
            Assert.That(Route.ExitArmed, Is.True);
            nav.RestoreVoyage(exit.mapPosition, 0, Loop.ActiveMap.entryDepth, nav.DistanceTravelled + 1000);
            Route.Evaluate(); Route.Evaluate();
            Assert.That(Loop.Zone, Is.EqualTo("Zone02")); Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Route.ExitArmed, Is.False);
            nav.Step(1, 0, .2f); nav.Brake(); Route.Evaluate();
            yield return Transition(); AssertEntry("Zone03");
        }

        [UnityTest] public IEnumerator ZoneThreeCannotBypassRockAndAutomaticallyTransfersOnlyAfterActualDestruction()
        {
            yield return AdvanceFixtureZone(); Assert.That(Loop.Zone, Is.EqualTo("Zone03"));
            var config = Loop.ActiveMap; var nav = Cabin.Navigation;
            Assert.That(config.exitArea.mapPosition, Is.EqualTo(new Vector2(1750, 25)));
            Assert.That(config.exitArea.arrivalRadius, Is.EqualTo(75));
            CompleteRequiredObjectives(true, true); Route.Evaluate();
            Assert.That(Loop.MissionRuntime.MainObjectivesComplete, Is.False);
            Assert.That(config.UsesAlternate(Loop.MissionRuntime), Is.False);
            Assert.That(CountClearRegion(nav, config.exitArea), Is.Zero, "The whole expanded area is behind the intact rock mask.");
            nav.RestoreVoyage(config.exitArea.mapPosition, 0, config.entryDepth, 0); Route.Evaluate();
            Assert.That(Route.CanExit(), Is.False); Assert.That(flow.IsTransitioning, Is.False);
            nav.RestoreVoyage(config.rockInteractionArea.mapPosition, 0, config.rockInteractionArea.targetDepth, 0);
            var story = Cabin.GetComponent<ZoneOneStory>();
            Assert.That(story.ApplyProgressionAction(), Is.True, story.ProgressionActionDescription);
            Route.Evaluate();
            Assert.That(Loop.CurrentProgress.rockDestroyed, Is.True);
            Assert.That(Loop.CurrentProgress.exitUnlocked, Is.True);
            Assert.That(config.UsesAlternate(Loop.MissionRuntime), Is.True);
            RestoreOutside(config.exitArea); Route.Evaluate();
            Assert.That(Route.CanExit(), Is.False);
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Continue(); Route.Evaluate();
            Assert.That(Loop.Zone, Is.EqualTo("Zone03")); Assert.That(flow.IsTransitioning, Is.False);
            Cabin.OpenMap(); yield return null;
            var overlay = Cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>(true);
            Assert.That(overlay.ExitApproachVisible, Is.True);
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "zone03-exit-expanded.png");
            Cabin.ClosePanel(); MoveInto(config.exitArea); Route.Evaluate(); Route.Evaluate();
            yield return Transition(); AssertEntry("Zone04");
            Assert.That(Loop.ActiveMap.destinationZone, Is.Null.Or.Empty);
        }

        [UnityTest] public IEnumerator ZoneThreeContinueInsideExpandedAreaRequiresMovementAndTransfersOnce()
        {
            yield return AdvanceFixtureZone(); CompleteRequiredObjectives(); Route.Evaluate();
            var exit = Loop.ActiveMap.exitArea;
            Cabin.Navigation.RestoreVoyage(exit.mapPosition, 90, Loop.ActiveMap.entryDepth, 100);
            Route.Evaluate(); Assert.That(Loop.SaveCurrent(), Is.True); yield return Continue();
            Route.Evaluate(); Assert.That(Route.ExitArmed, Is.False); Assert.That(flow.IsTransitioning, Is.False);
            Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake();
            Route.Evaluate(); Route.Evaluate(); Route.Evaluate();
            yield return Transition(); AssertEntry("Zone04");
            Route.Evaluate(); Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
            Assert.That(saved.current.zones.FindAll(z => z.zone == "Zone04").Count, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator FinalSignalNeedsAllDiscoveriesAndUnlockInsideDoesNotImmediatelyEnd()
        {
            yield return PrepareHiddenRoute(false);
            var config = Loop.ActiveMap; var nav = Cabin.Navigation; var runtime = Loop.MissionRuntime;
            nav.RestoreVoyage(config.finalHiddenPoint.mapPosition, 90, config.entryDepth, 0); Route.Evaluate();
            Assert.That(runtime.RevealPoi("zone04-l3"), Is.True); Assert.That(runtime.RevealPoi("zone04-l4"), Is.True);
            // A stale persisted completion flag cannot replace actual discoveries.
            Loop.CurrentProgress.hiddenRouteComplete = true; Route.Evaluate();
            nav.Step(1, 0, .2f); nav.Brake(); Route.Evaluate();
            Assert.That(Loop.EndingReached, Is.Null.Or.Empty); Assert.That(Route.FinalArmed, Is.False);
            Assert.That(config.HiddenDestinationAvailable(runtime), Is.False);
            Assert.That(runtime.RevealPoi("zone04-l5"), Is.True); Route.Evaluate();
            Assert.That(config.HiddenDestinationAvailable(runtime), Is.True);
            Assert.That(Route.FinalArmed, Is.False); Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Route.FinalNearby, Is.True); Assert.That(Route.FinalPrompt, Does.Contain("APPROACH"));
            nav.Navigate(0, 1, 1, .2f); nav.Brake(); Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False, "Rotation/depth is not final arrival.");
            nav.Step(1, 0, .2f); nav.Brake(); Route.Evaluate(); Route.Evaluate();
            yield return Transition(); AssertHiddenEnding();
        }

        [UnityTest] public IEnumerator FinalContinueInsideAndCheckpointRestoreNeedFreshPhysicalArrival()
        {
            yield return PrepareHiddenRoute(true);
            var point = Loop.ActiveMap.finalHiddenPoint.mapPosition;
            var nav = Cabin.Navigation;
            nav.RestoreVoyage(point, 90, Loop.ActiveMap.entryDepth, 100); Route.Evaluate();
            Assert.That(Loop.Rest(), Is.True); Route.Evaluate();
            nav.RestoreVoyage(point, 90, nav.Depth, 0); Route.Evaluate();
            Assert.That(Loop.RestoreDay(1), Is.True, Loop.LastError); Route.Evaluate();
            Assert.That(nav.DistanceTravelled, Is.EqualTo(100)); Assert.That(Route.FinalArmed, Is.False);
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Continue(); Route.Evaluate();
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(point)); Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Route.FinalArmed, Is.False);
            Cabin.OpenMap(); yield return null;
            var overlay = Cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>(true);
            Assert.That(overlay.FinalApproachVisible, Is.True); Assert.That(overlay.ExitApproachVisible, Is.False);
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "zone04-final-expanded.png", 1366, 768);
            Cabin.ClosePanel(); Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake();
            Route.Evaluate(); Route.Evaluate(); Route.Evaluate();
            yield return Transition(); AssertHiddenEnding();
        }

        [UnityTest] public IEnumerator FinalContinueNearAreaDoesNotEndBeforeCrossingActualBoundary()
        {
            yield return PrepareHiddenRoute(true);
            var final = Loop.ActiveMap.finalHiddenPoint;
            Vector2 outside = RestoreOutside(final); Route.Evaluate();
            Assert.That(Route.FinalArmed, Is.True); Assert.That(Loop.SaveCurrent(), Is.True);
            yield return Continue(); Route.Evaluate();
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(outside)); Assert.That(flow.IsTransitioning, Is.False);
            MoveInto(final); Route.Evaluate(); Route.Evaluate();
            yield return Transition(); AssertHiddenEnding();
        }

        [UnityTest] public IEnumerator RealTerrainApproachesMatchAuthoredBoundsAndZoneTwoRemainsUnchanged()
        {
            var world = Cabin.GetComponent<WorldMapController>();
            var owner = new GameObject("Activation terrain probe");
            try
            {
                var nav = owner.AddComponent<ZoneNavigation>();
                for (int zone = 0; zone < 4; zone++)
                {
                    var panel = world.zoneMaps[zone]; var config = panel.GetComponent<ZoneMapPresentation>().config;
                    var point = zone == 3 ? config.finalHiddenPoint : config.exitArea;
                    Assert.That(point.arrivalRadius, Is.EqualTo(75));
                    Assert.That(panel.GetComponentInChildren<PhotoSurveyMap>(true).GetComponentInParent<UnityEngine.UI.RectMask2D>(true), Is.Not.Null,
                        "Edge circles must be clipped to the actual map bounds.");
                    Assert.That(config.ApplyTerrain(nav, zone == 2), Is.True);
                    Assert.That(nav.CanOccupy(point.mapPosition), Is.True, config.zoneId);
                    FindApproach(nav, point);
                    if (zone == 2) { config.ApplyTerrain(nav, false); Assert.That(CountClearRegion(nav, point), Is.Zero); }
                    if (zone == 1) Assert.That(point.mapPosition, Is.EqualTo(new Vector2(1830, 75)));
                }
            }
            finally { Object.Destroy(owner); }
            yield return null;
        }

        private IEnumerator AdvanceFixtureZone()
        {
            CompleteRequiredObjectives(); Route.Evaluate();
            string next = Loop.ActiveMap.destinationZone;
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.exitArea.mapPosition, 0, Loop.ActiveMap.entryDepth, 0);
            flow.LoadZone(next); yield return Transition(); AssertEntry(next);
        }
        private IEnumerator PrepareHiddenRoute(bool discoverAll)
        {
            while (Loop.Zone != "Zone04") yield return AdvanceFixtureZone();
            CompleteRequiredObjectives(); Route.Evaluate();
            var dialogue = Object.FindAnyObjectByType<DialogueController>();
            Assert.That(dialogue.IsChoiceActive, Is.True); dialogue.SelectChoice(1);
            Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.True);
            Assert.That(Loop.ActiveMap.destinationZone, Is.Null.Or.Empty);
            Assert.That(Loop.ActiveMap.finalHiddenPoint.mapPosition, Is.EqualTo(new Vector2(1500, 150)));
            Assert.That(Loop.ActiveMap.finalHiddenPoint.arrivalRadius, Is.EqualTo(75));
            if (discoverAll)
                foreach (string id in Loop.ActiveMap.hiddenLocationIds)
                    Assert.That(Loop.MissionRuntime.RevealPoi(Loop.MissionRuntime.config.FindLocation(id).poiId), Is.True);
            Route.Evaluate();
        }
        private void AssertEntry(string zone)
        {
            Assert.That(Loop.Zone, Is.EqualTo(zone), flow.LastError);
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.entryPosition));
            Assert.That(Cabin.Navigation.Heading, Is.EqualTo(Mathf.Repeat(Loop.ActiveMap.entryHeading, 360)));
            Assert.That(Cabin.Navigation.Depth, Is.EqualTo(Loop.ActiveMap.entryDepth));
        }
        private static void AssertHiddenEnding()
        {
            Assert.That(Object.FindAnyObjectByType<EndingPresentation>().EndingId, Is.EqualTo("hidden"));
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
            Assert.That(saved.current.endingReached, Is.EqualTo("hidden"));
        }
        private Vector2 RestoreOutside(MapPoi point)
        {
            var nav = Cabin.Navigation; Vector2 outside = FindApproach(nav, point);
            Vector2 delta = point.mapPosition - outside;
            nav.RestoreVoyage(outside, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg, Loop.ActiveMap.entryDepth, nav.DistanceTravelled);
            return outside;
        }
        private void MoveInto(MapPoi point)
        {
            for (int i = 0; i < 20 && !point.Contains(Cabin.Navigation.Position); i++) Cabin.Navigation.Step(1, 0, .1f);
            Cabin.Navigation.Brake(); Assert.That(point.Contains(Cabin.Navigation.Position), Is.True);
        }
        private static Vector2 FindApproach(ZoneNavigation nav, MapPoi point)
        {
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64; Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outside = point.mapPosition + direction * (point.arrivalRadius + 6);
                bool clear = true;
                for (int distance = 0; distance <= 24 && clear; distance++) clear &= nav.CanOccupy(outside - direction * distance);
                if (clear) return outside;
            }
            Assert.Fail("No clear inward approach: " + point.id); return Vector2.zero;
        }
        private static int CountClearRegion(ZoneNavigation nav, MapPoi point)
        {
            int count = 0;
            for (float y = -point.arrivalRadius; y <= point.arrivalRadius; y += 2)
                for (float x = -point.arrivalRadius; x <= point.arrivalRadius; x += 2)
                    if (x * x + y * y <= point.arrivalRadius * point.arrivalRadius && nav.CanOccupy(point.mapPosition + new Vector2(x, y))) count++;
            return count;
        }

        private Vector2 ReachableApproach()
        {
            var nav = Cabin.Navigation;
            var exit = Loop.ActiveMap.exitArea;
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2 / 32;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outside = exit.mapPosition + direction * (exit.arrivalRadius + 6);
                bool clear = true;
                for (int distance = 0; distance <= 20 && clear; distance++) clear &= nav.CanOccupy(outside - direction * distance);
                if (clear) return outside;
            }
            Assert.Fail("No 20-unit clear approach into the configured exit."); return Vector2.zero;
        }
        private void CompleteRequiredObjectives(bool includeGate = true, bool omitDestroy = false)
        {
            // Authoritative fixture provisioning only; runtime unlock conditions stay unchanged.
            var runtime = Loop.MissionRuntime;
            var progress = runtime.ExportProgress();
            foreach (var location in runtime.config.locations)
                if (location.visibility == LocationVisibility.Visible)
                    foreach (var objective in location.objectives)
                        if (objective.required && !progress.completedObjectives.Contains(objective.id)) progress.completedObjectives.Add(objective.id);
            if (includeGate) foreach (var objective in runtime.config.zoneGate.objectives)
                if (objective.required && !(omitDestroy && objective.type == MissionObjectiveType.DestroyObstacle) &&
                    !progress.completedObjectives.Contains(objective.id)) progress.completedObjectives.Add(objective.id);
            runtime.RestoreProgress(progress);
            var ship = Cabin.Navigation.Ship.Export(); ship.maximumDepth = 2000;
            Cabin.Navigation.Ship.Restore(ship);
            if (includeGate && !omitDestroy) Assert.That(runtime.MainObjectivesComplete, Is.True);
        }
        private void AssertZoneThreeEntry(float departureDepth = 230)
        {
            Assert.That(Loop.Zone, Is.EqualTo("Zone03"), flow.LastError);
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.entryPosition));
            Assert.That(Cabin.Navigation.Heading, Is.EqualTo(Mathf.Repeat(Loop.ActiveMap.entryHeading, 360)));
            Assert.That(Cabin.Navigation.Depth, Is.EqualTo(Mathf.Clamp(departureDepth, Loop.ActiveMap.minimumDepth, Cabin.Navigation.Ship.MaximumDepth)));
        }
        private IEnumerator Continue()
        {
            flow.LoadMainMenu(); yield return Transition();
            flow.ContinueGame(); yield return Transition();
            Assert.That(Loop.IsInitialized, Is.True, Loop.LastError);
        }
        private IEnumerator Transition()
        {
            yield return null;
            float timeout = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(flow.IsTransitioning, Is.False, "Transition timed out");
            Assert.That(flow.LastError, Is.Null.Or.Empty);
            yield return null;
        }
    }
}

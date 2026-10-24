using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
using G10.Prototype.Missions;
using G10.Prototype.Tutorial;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class DebugStartPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "DebugStart-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
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
            yield return null;
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = oldSave;
            PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        [UnityTest] public IEnumerator FourZonePresetsUseRealProgressAndIsolatedContinue()
        {
            byte[] normal = File.ReadAllBytes(ExpeditionSaveStore.NormalSavePath);
            for (int zone = 1; zone <= 4; zone++)
            {
                var menu = Object.FindAnyObjectByType<MainMenuDebugStart>();
                Assert.That(menu.DebugButton, Is.Not.Null); menu.DebugButton.onClick.Invoke();
                Assert.That(menu.IsOpen, Is.True); menu.SelectZone(zone);
                Assert.That(menu.ContinueButton.interactable, Is.EqualTo(zone > 1));
                Assert.That(menu.StartButton.GetComponentInChildren<TMPro.TMP_Text>().color.r, Is.GreaterThan(.7f));
                if (zone == 4)
                {
                    Canvas.ForceUpdateCanvases();
                    ScreenCapture.CaptureScreenshot(CabinNavigationPlayModeTests.CapturePath("round3-debug-start.png"));
                    yield return null;
                }
                menu.StartButton.onClick.Invoke(); yield return Wait();
                Assert.That(Loop.Zone, Is.EqualTo($"Zone0{zone}")); Assert.That(Loop.Day, Is.EqualTo(1));
                Assert.That(Loop.TotalDays, Is.EqualTo(25)); Assert.That(ExpeditionSaveStore.IsDebugTimeline, Is.True);
                Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.entryPosition));
                Assert.That(Cabin.Navigation.Heading, Is.EqualTo(Loop.ActiveMap.entryHeading));
                Assert.That(Cabin.Navigation.Depth, Is.EqualTo(Loop.ActiveMap.entryDepth));
                Assert.That(Cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(zone > 2 ? 750 : 500));
                Assert.That(Loop.UpgradeLevel(G10.Prototype.Navigation.ShipUpgrade.Speed), Is.Zero);
                Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.False);
                Assert.That(Loop.MissionRuntime.CompletedCount, Is.Zero);
                for (int prior = 1; prior < zone; prior++)
                {
                    var saved = Loop.ReadZoneState($"Zone0{prior}");
                    Assert.That(saved.progress.mainObjectivesComplete, Is.True);
                    var world = Cabin.GetComponent<WorldMapController>(); world.OpenZone(prior - 1); yield return null;
                    var presentation = world.zoneMaps[prior - 1].GetComponent<ZoneMapPresentation>();
                    foreach (var icon in presentation.overlay.completionIcons) Assert.That(icon.enabled, Is.True);
                    Assert.That(saved.missionProgress.grantedRewards.Count,
                        Is.EqualTo(new System.Collections.Generic.HashSet<string>(saved.missionProgress.grantedRewards).Count));
                    if (prior == 3) Assert.That(presentation.mapImage.texture, Is.SameAs(presentation.config.alternateMap));
                }
                if (zone >= 3) Assert.That(Loop.MissionRuntime.HasRecipe("RECIPE_BIO_LAMP"), Is.True);
                if (zone == 4)
                {
                    Assert.That(Loop.MissionRuntime.HasRecipe("RECIPE_ROCK_BREAKER"), Is.True);
                    Assert.That(Loop.MissionRuntime.HasWorldFlag("ROCK_BARRIER_DESTROYED"), Is.True);
                    Assert.That(Loop.ReadZoneState("Zone03").missionProgress.completedObjectives, Does.Contain("Z3_GATE_INSTALL_ROCK_BREAKER"));
                }
                // Zone01 intentionally starts fresh onboarding; earn map access through real helm actions.
                if (zone == 1) yield return LearnZoneOneHelm();
                yield return AssertCurrentHelmMap();
                yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, $"helm-preview-zone{zone}.png");
                Cabin.ClosePanel(); Assert.That(Loop.SaveCurrent(), Is.True);
                flow.LoadMainMenu(); yield return Wait(); flow.ContinueDebugGame(); yield return Wait();
                Assert.That(Loop.Zone, Is.EqualTo($"Zone0{zone}"));
                yield return AssertCurrentHelmMap();
                CollectionAssert.AreEqual(normal, File.ReadAllBytes(ExpeditionSaveStore.NormalSavePath));
                flow.LoadMainMenu(); yield return Wait();
            }
            flow.ContinueGame(); yield return Wait();
            Assert.That(ExpeditionSaveStore.IsDebugTimeline, Is.False); Assert.That(Loop.Zone, Is.EqualTo("Zone01"));
        }
        [UnityTest] public IEnumerator ReturningToLegacyInactiveZoneDoesNotScaleNewArrivalTwice()
        {
            foreach (int version in new[] { 0, 1, 2 })
            {
                var menu = Object.FindAnyObjectByType<MainMenuDebugStart>();
                var map = menu.maps[0]; var poi = map.locations[0];
                Vector2 oldSize = version == 0 ? new Vector2(1200, 700) : version == 1 ? map.LegacyLocationSizedWorldSize : map.LegacyDisplayWorldSize;
                menu.Open(); menu.SelectZone(2); menu.StartRun(); yield return Wait();
                flow.LoadMainMenu(); yield return Wait();
                Assert.That(ExpeditionSaveStore.TryReadDebug(out var saved, out var error), Is.True, error);
                var prior = saved.current.zones.Find(z => z.zone == "Zone01");
                prior.mapCoordinateVersion = version; prior.position = new Vector2(900, 160);
                prior.creatureSpawns.Add(new SavedCreatureSpawn { day = saved.current.day, poiId = poi.id,
                    coordinate = Vector2.Scale(poi.mapPosition, new Vector2(oldSize.x / map.WorldSize.x, oldSize.y / map.WorldSize.y)) });
                saved.dayStart = ExpeditionSaveStore.Copy(saved.current); ExpeditionSaveStore.Write(saved, true);
                flow.ContinueDebugGame(); yield return Wait();
                Assert.That(Loop.ReadZoneState("Zone01").mapCoordinateVersion, Is.EqualTo(version));
                var nav = Cabin.Navigation; var route = Cabin.GetComponent<ExpeditionProgression>();
                nav.RestoreVoyage(Loop.ActiveMap.returnArea.mapPosition, 90, Loop.ActiveMap.entryDepth, 0);
                route.Evaluate();
                Assert.That(flow.IsTransitioning, Is.False, "A restored position inside the gate is not return intent.");
                RestoreReturnApproach();
                route.Evaluate();
                Assert.That(route.ReturnArmed, Is.True);
                for (int i = 0; i < 30 && !Loop.ActiveMap.returnArea.Contains(nav.Position); i++) nav.Step(1, 0, .1f);
                nav.Brake();
                Assert.That(Loop.ActiveMap.returnArea.Contains(nav.Position), Is.True);
                route.Evaluate(); yield return Wait();
                Assert.That(Loop.Zone, Is.EqualTo("Zone01"));
                Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.backwardArrivalPosition));
                Assert.That(Cabin.Navigation.CanOccupy(Cabin.Navigation.Position), Is.True);
                var migrated = Loop.ReadZoneState("Zone01");
                Assert.That(migrated.mapCoordinateVersion, Is.EqualTo(G10.Prototype.Navigation.ZoneMapConfig.CurrentCoordinateVersion));
                Assert.That(Vector2.Distance(migrated.creatureSpawns.Find(s => s.poiId == poi.id).coordinate, poi.mapPosition), Is.LessThan(.001f));
                flow.LoadMainMenu(); yield return Wait();
            }
        }
        [UnityTest] public IEnumerator OptionalRegularPresetDoesNotReplaceMandatoryStoryModules()
        {
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(4); menu.ToggleRegularMax();
            menu.StartRun(); yield return Wait();
            Assert.That(Loop.UpgradeLevel(G10.Prototype.Navigation.ShipUpgrade.Hull), Is.EqualTo(2));
            Assert.That(Loop.UpgradeLevel(G10.Prototype.Navigation.ShipUpgrade.Speed), Is.EqualTo(2));
            Assert.That(Loop.UpgradeLevel(G10.Prototype.Navigation.ShipUpgrade.Energy), Is.EqualTo(2));
            Assert.That(Cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(750));
            Assert.That(MainMenuDebugStart.VisibleForConfiguration(false, false), Is.False);
            Assert.That(MainMenuDebugStart.VisibleForConfiguration(true, false), Is.True);
            Assert.That(MainMenuDebugStart.VisibleForConfiguration(false, true), Is.True);
        }
        private IEnumerator Wait()
        {
            yield return null; float deadline = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
        private void RestoreReturnApproach()
        {
            var nav = Cabin.Navigation;
            var map = Loop.ActiveMap;
            var gate = map.returnArea;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outside = gate.mapPosition + direction * (gate.arrivalRadius + 6);
                bool clear = true;
                for (int distance = 0; distance <= 24 && clear; distance++) clear &= nav.CanOccupy(outside - direction * distance);
                if (!clear) continue;
                Vector2 delta = gate.mapPosition - outside;
                nav.RestoreVoyage(outside, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg, nav.Depth, nav.DistanceTravelled);
                return;
            }
            Assert.Fail("No clear return-gate approach: " + gate.id);
        }
        private IEnumerator LearnZoneOneHelm()
        {
            var tutorial = Cabin.GetComponent<TutorialManager>();
            var dialogue = Object.FindAnyObjectByType<DialogueController>();
            float deadline = Time.realtimeSinceStartup + 8;
            while (!dialogue.IsActive && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStepId.Intro));
            Assert.That(dialogue.IsActive, Is.True); dialogue.Skip();
            yield return new WaitForSecondsRealtime(.4f);
            Cabin.OpenNavigation(); yield return new WaitForSecondsRealtime(.4f);
            Cabin.Navigation.Step(1, 0, 1.5f); yield return new WaitForSecondsRealtime(.4f);
            Cabin.Navigation.Step(0, 1, .5f); yield return new WaitForSecondsRealtime(.4f);
            Cabin.Navigation.StepDepth(1, 1.1f); yield return new WaitForSecondsRealtime(.4f);
            Assert.That(tutorial.HasLearned(TutorialStepId.Helm), Is.True);
        }
        private IEnumerator AssertCurrentHelmMap()
        {
            Cabin.OpenNavigation(); yield return null;
            var preview = Cabin.NavigationPanel.GetComponentInChildren<HelmMapPreview>(true);
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.MapImage.texture, Is.SameAs(Loop.ActiveMap.MapFor(Loop.MissionRuntime)));
            preview.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(Cabin.Panels.CurrentPanel, Is.SameAs(Cabin.MapPanel));
            yield return new WaitForSecondsRealtime(.4f);
            Cabin.OpenNavigation();
        }
    }
}

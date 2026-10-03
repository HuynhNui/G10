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
    /// <summary>Flow integration: mission events are simulated; terrain and scene/save/UI contracts are real.</summary>
    public sealed class ExpeditionEndToEndPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionProgression Route => Cabin.GetComponent<ExpeditionProgression>();

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ExpeditionFlow-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride;
            oldPhotos = PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return null;
            flow = SceneFlowController.Instance;
            Assert.That(flow, Is.Not.Null);
            yield return WaitTransition();
            flow.StartNewGame();
            yield return WaitTransition();
            Assert.That(Loop.IsInitialized, Is.True, Loop.LastError);
            Assert.That(Loop.Zone, Is.EqualTo("Zone01"));
            Assert.That(Loop.MissionRuntime.CompletedCount, Is.Zero);
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.entryPosition));
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave;
            PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [UnityTest] public IEnumerator NewGameThroughFourZonesToNormalEndingAndContinue()
        {
            yield return ReachZoneFour();
            CompleteFieldwork();
            Route.Evaluate();
            var dialogue = Object.FindAnyObjectByType<DialogueController>();
            Assert.That(dialogue.IsChoiceActive, Is.True);
            Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.False);
            Canvas.ForceUpdateCanvases();
            foreach (var label in dialogue.View.GetComponentsInChildren<TMPro.TMP_Text>())
                if (label.transform.parent.name.Contains("ChoiceButton"))
                {
                    label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing, Is.False, label.text);
                    Assert.That(label.rectTransform.rect.width, Is.GreaterThan(200));
                }
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "../Temp/expedition-ending-choice.png"));
            yield return new WaitForSecondsRealtime(.3f);
            dialogue.SelectChoice(0);
            yield return WaitTransition();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Ending"));
            Assert.That(Object.FindAnyObjectByType<EndingPresentation>().EndingId, Is.EqualTo("normal"));
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
            Assert.That(saved.current.endingReached, Is.EqualTo("normal"));
            flow.LoadMainMenu(); yield return WaitTransition();
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Object.FindAnyObjectByType<EndingPresentation>().EndingId, Is.EqualTo("normal"));
        }

        [UnityTest] public IEnumerator HiddenRouteRequiresChoiceThreeDiscoveriesAndFinalArrivalSurvivesReload()
        {
            yield return ReachZoneFour();
            var runtime = Loop.MissionRuntime;
            Assert.That(runtime.RevealPoi("zone04-l3"), Is.False);
            Assert.That(runtime.IsContentPresent("zone04-l3"), Is.False);
            CompleteFieldwork(); Route.Evaluate();
            Object.FindAnyObjectByType<DialogueController>().SelectChoice(1);
            Assert.That(Loop.Zone, Is.EqualTo("Zone04"));
            Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.True);
            Assert.That(runtime.RevealPoi("zone04-l3"), Is.True);
            Assert.That(Loop.SaveCurrent(), Is.True);
            yield return Reload();
            Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.True);
            Assert.That(Loop.MissionRuntime.IsLocationRevealed("Z4_L3"), Is.True);
            Assert.That(Loop.MissionRuntime.RevealPoi("zone04-l4"), Is.True);
            Assert.That(Loop.MissionRuntime.RevealPoi("zone04-l5"), Is.True);
            Route.Evaluate();
            Assert.That(Loop.CurrentProgress.hiddenRouteComplete, Is.True);
            Assert.That(Loop.EndingReached, Is.Null.Or.Empty);
            Assert.That(flow.IsTransitioning, Is.False, "The third discovery must not end the game.");
            Assert.That(Loop.SaveCurrent(), Is.True);
            yield return Reload();
            Assert.That(Loop.CurrentProgress.hiddenRouteComplete, Is.True);
            Route.Evaluate(); // Arms arrival while outside the final location.
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.finalHiddenPoint.mapPosition, 0, 230, 0);
            Route.Evaluate();
            yield return WaitTransition();
            Assert.That(Object.FindAnyObjectByType<EndingPresentation>().EndingId, Is.EqualTo("hidden"));
        }

        [UnityTest] public IEnumerator RealCameraCaptureUpgradeAndRadarServicesWorkAcrossAllFourZones()
        {
            yield return ReachZoneFour(true);
            yield return CompleteFieldworkViaDevices();
            Route.Evaluate();
            Object.FindAnyObjectByType<DialogueController>().SelectChoice(1);
            var survey = Cabin.GetComponent<PhotoSurveyZone>();
            foreach (string id in Loop.ActiveMap.hiddenLocationIds)
            {
                var location = Loop.MissionRuntime.config.FindLocation(id);
                var poi = survey.FindPoi(location.poiId);
                Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi), 0, survey.targetDepth, 0);
                Cabin.OpenRadar();
                Cabin.Scan();
                yield return new WaitForSecondsRealtime(2.2f);
                Assert.That(Loop.MissionRuntime.IsLocationRevealed(id), Is.True, "Radar must discover " + id);
            }
            Cabin.ClosePanel();
            Route.Evaluate();
            Assert.That(Loop.CurrentProgress.hiddenRouteComplete, Is.True);
            Assert.That(flow.IsTransitioning, Is.False);
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.finalHiddenPoint.mapPosition, 0, 230, 0);
            Route.Evaluate();
            yield return WaitTransition();
            Assert.That(Object.FindAnyObjectByType<EndingPresentation>().EndingId, Is.EqualTo("hidden"));
        }

        private IEnumerator CompleteFieldworkViaDevices()
        {
            var survey = Cabin.GetComponent<PhotoSurveyZone>();
            var camera = Cabin.GetComponent<PhotoCaptureService>();
            var catcher = Cabin.GetComponent<CreatureCatcher>();
            var runtime = Loop.MissionRuntime;
            foreach (var location in runtime.config.locations)
            {
                if (location.visibility == LocationVisibility.HiddenRadar) continue;
                var poi = survey.FindPoi(location.poiId);
                Vector2 subject = survey.ContactPosition(poi);
                foreach (var objective in location.objectives)
                {
                    if (!objective.required) continue;
                    if (objective.type == MissionObjectiveType.Photograph)
                    {
                        Cabin.OpenCamera();
                        bool aimed = false;
                        for (int i = 0; i < 16 && !aimed; i++)
                        {
                            float angle = i * Mathf.PI / 8;
                            Vector2 offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 20;
                            Vector2 position = subject - offset;
                            if (!Cabin.Navigation.CanOccupy(position)) continue;
                            Cabin.Navigation.RestoreVoyage(position, i * 22.5f, survey.targetDepth, 0);
                            aimed = survey.TryGetPhotoContact(Cabin.Navigation, camera.profile.visibleDistance,
                                camera.profile.fieldOfView, out var found, out _) && found == poi;
                        }
                        Assert.That(aimed, Is.True, "No reachable camera angle for " + poi.id);
                        yield return new WaitForSecondsRealtime(.65f);
                        var photo = camera.Capture();
                        Assert.That(photo, Is.Not.Null, poi.id + ": " + camera.LastError);
                        Assert.That(runtime.HasObjective(objective.id), Is.True, poi.id + ": " + photo.Result);
                    }
                    else if (objective.type == MissionObjectiveType.Capture || objective.type == MissionObjectiveType.Collect)
                    {
                        Cabin.Navigation.RestoreVoyage(subject, 0, survey.targetDepth, 0);
                        Cabin.OpenCapture();
                        Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started), poi.id);
                        CaptureMinigamePlayModeTests.Win(catcher.minigame);
                        Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught), poi.id);
                        Assert.That(runtime.HasObjective(objective.id), Is.True, objective.id);
                    }
                }
            }
            Cabin.ClosePanel();
        }

        private IEnumerator ReachZoneFour(bool useDevices = false)
        {
            for (int zone = 1; zone <= 3; zone++)
            {
                var loop = Loop;
                var config = loop.ActiveMap;
                string target = config.destinationZone;
                Assert.That(loop.CurrentProgress.exitUnlocked, Is.False);
                flow.LoadZone(target);
                Assert.That(flow.IsTransitioning, Is.False, "Locked route cannot be bypassed.");
                if (zone == 3) Assert.That(Cabin.Navigation.CanOccupy(config.exitArea.mapPosition), Is.False);
                if (useDevices) yield return CompleteFieldworkViaDevices();
                else CompleteFieldwork();
                var story = Cabin.GetComponent<ZoneOneStory>();
                while (story.PendingGateObjective != null)
                {
                    if (story.PendingGateObjective.type == MissionObjectiveType.DestroyObstacle)
                        Cabin.Navigation.RestoreVoyage(config.rockInteractionArea.mapPosition, 0, 230, 0);
                    var upgrade = Array.Find(Cabin.GetComponentsInChildren<UpgradeEntryConfig>(true), item => item.UpgradeId == "Hull");
                    Assert.That(upgrade, Is.Not.Null);
                    Assert.That(upgrade.TryApply(), Is.True, story.ProgressionActionDescription);
                }
                Route.Evaluate();
                Assert.That(loop.CurrentProgress.exitUnlocked, Is.True);
                Assert.That(loop.Zone, Is.EqualTo("Zone0" + zone));
                Assert.That(flow.IsTransitioning, Is.False, "Completing objectives must not teleport.");
                if (zone == 3)
                {
                    Assert.That(loop.CurrentProgress.rockDestroyed, Is.True);
                    Assert.That(Cabin.Navigation.CanOccupy(config.exitArea.mapPosition), Is.True);
                    Assert.That(loop.SaveCurrent(), Is.True);
                    yield return Reload();
                    Assert.That(Loop.CurrentProgress.rockDestroyed, Is.True);
                    Assert.That(Loop.ActiveMap.UsesAlternate(Loop.MissionRuntime), Is.True);
                    Assert.That(Cabin.Navigation.CanOccupy(config.exitArea.mapPosition), Is.True);
                    Route.Evaluate();
                }
                Cabin.Navigation.RestoreVoyage(config.exitArea.mapPosition, 0, 230, 0);
                Route.Evaluate();
                yield return WaitTransition();
                Assert.That(Loop.Zone, Is.EqualTo(target), flow.LastError);
                Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.entryPosition));
                Assert.That(SceneManager.GetSceneByName("Zone01").isLoaded, Is.True);
                Assert.That(SceneManager.GetSceneByName(target).isLoaded, Is.False, "Reuse the cabin, not placeholder scenes.");
            }
        }

        private void CompleteFieldwork()
        {
            var runtime = Loop.MissionRuntime;
            foreach (var location in runtime.config.locations)
            {
                if (location.visibility == LocationVisibility.HiddenRadar) continue;
                Assert.That(runtime.ContentForPoi(location.poiId), Is.Not.Null, location.poiId + " content binding");
                foreach (var objective in location.objectives)
                    if (objective.required) Assert.That(runtime.RecordObjective(location.poiId, objective.type, objective.targetId), Is.True);
            }
        }

        private IEnumerator Reload()
        {
            flow.LoadMainMenu(); yield return WaitTransition();
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Loop.IsInitialized, Is.True, Loop.LastError);
        }
        private IEnumerator WaitTransition()
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

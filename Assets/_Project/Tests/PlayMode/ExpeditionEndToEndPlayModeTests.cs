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
    /// <summary>Flow integration through real camera, Photo Lab submission and capture services.</summary>
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
            TutorialTestSave.SeedReturningPlayer();
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
            yield return CompleteFieldworkViaDevices();
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
            ScreenCapture.CaptureScreenshot(CabinNavigationPlayModeTests.CapturePath("expedition-ending-choice.png"));
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

        [UnityTest] public IEnumerator HiddenRouteRequiresChoiceTwoPhotosAndFinalArrivalSurvivesReload()
        {
            yield return ReachZoneFour();
            var runtime = Loop.MissionRuntime;
            Assert.That(runtime.RevealPoi("zone04-l3"), Is.False);
            Assert.That(runtime.IsContentPresent("zone04-l3"), Is.False);
            yield return CompleteFieldworkViaDevices(); Route.Evaluate();
            Object.FindAnyObjectByType<DialogueController>().SelectChoice(1);
            Assert.That(Loop.Zone, Is.EqualTo("Zone04"));
            Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.True);
            Assert.That(runtime.RevealPoi("zone04-l3"), Is.True);
            Assert.That(Loop.SaveCurrent(), Is.True);
            yield return Reload();
            Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.True);
            Assert.That(Loop.MissionRuntime.IsLocationRevealed("Z4_L3"), Is.True);
            Assert.That(Loop.MissionRuntime.RevealPoi("zone04-l4"), Is.False, "Deleted POI must stay absent");
            Assert.That(Loop.MissionRuntime.RevealPoi("zone04-l5"), Is.True);
            Assert.That(Loop.ActiveMap.HiddenDestinationAvailable(Loop.MissionRuntime), Is.False, "Discoveries alone do not complete hidden PHOTO work");
            yield return CompleteFieldworkViaDevices("zone04-l3");
            yield return CompleteFieldworkViaDevices("zone04-l5");
            Route.Evaluate();
            Assert.That(Loop.CurrentProgress.hiddenRouteComplete, Is.True);
            Assert.That(Loop.EndingReached, Is.Null.Or.Empty);
            Assert.That(flow.IsTransitioning, Is.False, "Completing hidden photos must not end the game before final arrival.");
            Assert.That(Loop.SaveCurrent(), Is.True);
            yield return Reload();
            Assert.That(Loop.CurrentProgress.hiddenRouteComplete, Is.True);
            Route.Evaluate(); // Arms arrival while outside the final location.
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.finalHiddenPoint.mapPosition, 0, Loop.ActiveMap.entryDepth, 0);
            Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False, "Position restoration is not final arrival.");
            Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
            yield return WaitTransition();
            Assert.That(Object.FindAnyObjectByType<EndingPresentation>().EndingId, Is.EqualTo("hidden"));
        }

        [UnityTest] public IEnumerator RealCameraCaptureUpgradeAndRadarServicesWorkAcrossAllFourZones()
        {
            File.WriteAllText(StatPath, "stage\thull/cap\tspeed\tdive\tascent\tmaxDepth\tenergy/cap\tradar/cap\tphotos/cap\tcaptures/cap\n");
            RecordShipStats("Zone01 start");
            yield return ReachZoneFour(true);
            yield return CompleteFieldworkViaDevices();
            Route.Evaluate();
            Object.FindAnyObjectByType<DialogueController>().SelectChoice(1);
            var survey = Cabin.GetComponent<PhotoSurveyZone>();
            foreach (string id in Loop.ActiveMap.hiddenLocationIds)
            {
                var location = Loop.MissionRuntime.config.FindLocation(id);
                var poi = survey.FindPoi(location.poiId);
                Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), 0);
                Cabin.OpenRadar();
                Cabin.Scan();
                yield return new WaitForSecondsRealtime(2.2f);
                Assert.That(Loop.MissionRuntime.IsLocationRevealed(id), Is.True, "Radar must discover " + id);
                Cabin.ClosePanel();
                yield return CompleteFieldworkViaDevices(location.poiId);
            }
            Cabin.ClosePanel();
            Route.Evaluate();
            Assert.That(Loop.CurrentProgress.hiddenRouteComplete, Is.True);
            Assert.That(flow.IsTransitioning, Is.False);
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.finalHiddenPoint.mapPosition, 0, Loop.ActiveMap.entryDepth, 0);
            Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False, "Position restoration is not final arrival.");
            Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
            yield return WaitTransition();
            Assert.That(Object.FindAnyObjectByType<EndingPresentation>().EndingId, Is.EqualTo("hidden"));
        }

        private IEnumerator CompleteFieldworkViaDevices(string onlyPoi = null, MissionObjectiveType? onlyType = null)
        {
            var survey = Cabin.GetComponent<PhotoSurveyZone>();
            var camera = Cabin.GetComponent<PhotoCaptureService>();
            var catcher = Cabin.GetComponent<CreatureCatcher>();
            var runtime = Loop.MissionRuntime;
            foreach (var location in runtime.config.locations)
            {
                if (location.visibility == LocationVisibility.HiddenRadar && onlyPoi == null) continue;
                if (onlyPoi != null && location.poiId != onlyPoi) continue;
                var poi = survey.FindPoi(location.poiId);
                Vector2 subject = survey.ContactPosition(poi);
                foreach (var objective in location.objectives)
                {
                    if (!objective.required) continue;
                    if (onlyType.HasValue && objective.type != onlyType.Value) continue;
                    if (runtime.HasObjective(objective.id)) continue;
                    if (objective.type == MissionObjectiveType.Photograph)
                    {
                        bool aimed = false;
                        for (int i = 0; i < 16 && !aimed; i++)
                        {
                            float angle = i * Mathf.PI / 8;
                            Vector2 offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 20;
                            Vector2 position = subject - offset;
                            if (!Cabin.Navigation.CanOccupy(position)) continue;
                            Cabin.Navigation.RestoreVoyage(position, i * 22.5f, survey.DepthFor(poi), 0);
                            aimed = survey.TryGetPhotoContact(Cabin.Navigation, camera.profile.visibleDistance,
                                camera.profile.fieldOfView, out var found, out _) && found == poi;
                        }
                        Assert.That(aimed, Is.True, "No reachable camera angle for " + poi.id);
                        yield return new WaitForSecondsRealtime(.65f);
                        int before = camera.TotalPhotosTaken;
                        Cabin.OpenCamera();
                        yield return null; yield return null;
                        Assert.That(camera.TotalPhotosTaken, Is.EqualTo(before + 1));
                        var photo = camera.Photos[camera.Photos.Count - 1];
                        Assert.That(photo, Is.Not.Null, poi.id + ": " + camera.LastError);
                        Assert.That(photo.MissionObjectiveId, Is.EqualTo(objective.id));
                        Assert.That(runtime.HasObjective(objective.id), Is.False);
                        Cabin.OpenComputer();
                        var desktop=Cabin.GetComponentInChildren<ComputerScreenController>(true);
                        desktop.OpenPhotoLab();
                        var lab=desktop.GetComponentInChildren<PhotoLabView>(true);
                        Assert.That(lab.sendButton.interactable,Is.True);
                        lab.sendButton.onClick.Invoke();
                        Assert.That(photo.IsMissionPhoto,Is.True);
                        Assert.That(runtime.HasObjective(objective.id), Is.True, poi.id + ": " + photo.Result);
                        Cabin.ClosePanel();
                    }
                    else if (objective.type == MissionObjectiveType.Capture || objective.type == MissionObjectiveType.Collect)
                    {
                        Cabin.Navigation.RestoreVoyage(subject, 0, survey.DepthFor(poi), 0);
                        Cabin.OpenCapture();
                        yield return new WaitForSecondsRealtime(1.1f);
                        Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Started), poi.id);
                        CaptureMinigamePlayModeTests.Win(catcher.minigame);
                        Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught), poi.id);
                        Assert.That(runtime.HasObjective(objective.id), Is.True, objective.id);
                    }
                }
            }
            Cabin.ClosePanel();
            if (Loop.Zone == "Zone04" && runtime.HasObjective("Z4_L1_CAPTURE"))
            {
                var log = Cabin.GetComponentInChildren<MissionLogView>(true); log.Refresh();
                Assert.That(log.DisplayedText, Does.Contain("1006.56, 450.51").And.Contain("358.56, 119.57"));
                if (!runtime.HiddenRouteAvailable) Assert.That(runtime.ProgressState.revealedLocations, Is.Empty);
            }
        }

        private IEnumerator ReachZoneFour(bool useDevices = false) => ReachZone(4, useDevices);

        [UnityTest] public IEnumerator FailedCaptureAndLampWritesRollbackThenReloadRestAndRestoreDoNotDuplicateMaterials()
        {
            yield return CompleteFieldworkViaDevices(onlyType: MissionObjectiveType.Photograph);
            var story = Cabin.GetComponent<ZoneOneStory>();
            var survey = Cabin.GetComponent<PhotoSurveyZone>();
            var inventory = Cabin.GetComponent<CreatureInventory>();
            var catcher = Cabin.GetComponent<CreatureCatcher>();
            var poi = survey.FindPoi("zone01-left");
            Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), 0);
            string path = ExpeditionSaveStore.PathOverride, disk = File.ReadAllText(path);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-capture.json");
            try { CaptureMinigamePlayModeTests.Win(catcher.minigame); }
            finally { ExpeditionSaveStore.PathOverride = path; }
            Assert.That(catcher.LastResult, Is.Not.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(story.HasObjective(ZoneOneStory.CaptureOneObjective), Is.False);
            Assert.That(story.HasRecipe(ZoneOneStory.LampRecipe), Is.False);
            Assert.That(inventory.GetCount("Z1_Creature_01"), Is.Zero);
            Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            CaptureMinigamePlayModeTests.Win(catcher.minigame);
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(inventory.GetCount("Z1_Creature_01"), Is.EqualTo(1));
            Assert.That(Loop.Rest(), Is.True);
            disk = File.ReadAllText(path);
            ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-upgrade.json");
            try { Assert.That(story.ApplyProgressionAction(), Is.False); }
            finally { ExpeditionSaveStore.PathOverride = path; }
            Assert.That(inventory.GetCount("Z1_Creature_01"), Is.EqualTo(1));
            Assert.That(story.HasObjective(ZoneOneStory.LampInstallObjective), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
            Assert.That(story.ApplyProgressionAction(), Is.True);
            Assert.That(inventory.GetCount("Z1_Creature_01"), Is.Zero);
            yield return Reload();
            Assert.That(Cabin.GetComponent<CreatureInventory>().GetCount("Z1_Creature_01"), Is.Zero);
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.LampInstallObjective), Is.True);
            Assert.That(Loop.RestoreDay(1), Is.True);
            story = Cabin.GetComponent<ZoneOneStory>(); inventory = Cabin.GetComponent<CreatureInventory>();
            Assert.That(inventory.GetCount("Z1_Creature_01"), Is.EqualTo(1));
            Assert.That(story.HasObjective(ZoneOneStory.CaptureOneObjective), Is.True);
            Assert.That(story.HasObjective(ZoneOneStory.LampInstallObjective), Is.False);
            Assert.That(story.ApplyProgressionAction(), Is.True);
            Assert.That(inventory.GetCount("Z1_Creature_01"), Is.Zero);
            Assert.That(story.ApplyProgressionAction(), Is.False);
            Assert.That(Cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(500));
        }

        [UnityTest] public IEnumerator LegacyInstalledPressureHullWithoutShipSnapshotKeepsDepthAndDoesNotCreditNewCapture()
        {
            flow.LoadMainMenu(); yield return WaitTransition();
            Assert.That(ExpeditionSaveStore.TryRead(out var save, out _), Is.True);
            save.version = 5;
            save.current.hasShipState = false; save.current.ship = null;
            save.hasDayStart = false; save.dayStart = null;
            save.current.zones[0].missionProgress.completedObjectives.Add("Z1_GATE_INSTALL_PRESSURE_HULL");
            save.current.zones[0].progress.exitUnlocked = true;
            ExpeditionSaveStore.Write(save, true);
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(750));
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.CaptureOneObjective), Is.False);
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.LampInstallObjective), Is.False);
            Assert.That(Loop.MissionRuntime.CanExitZone, Is.True);
            Assert.That(Cabin.GetComponent<CreatureInventory>().Items, Is.Empty);
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
            Assert.That(Cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(750));
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.CaptureOneObjective), Is.False);
        }

        [UnityTest] public IEnumerator RoundThreeJournalDayFourReplaysPhotoSendCaptureThenContinue()
            => JournalReplaysLocation(0);

        [UnityTest] public IEnumerator RoundThreeJournalKeepsEarlierPhotoButDiscardsFutureCapture()
            => JournalReplaysLocation(1);

        [UnityTest] public IEnumerator RoundThreeJournalKeepsEarlierCompletionAndRollsBackRepeatStacks()
            => JournalReplaysLocation(2);

        private IEnumerator JournalReplaysLocation(int checkpointCompletion)
        {
            yield return ReachZone(2);
            while (Loop.Day < 4) Assert.That(Loop.Rest(), Is.True);
            const string poiId = "zone02-l3", photoId = "Z2_L2_PHOTO", captureId = "Z2_L2_CAPTURE";
            if (checkpointCompletion > 0)
                yield return CompleteFieldworkViaDevices(poiId, checkpointCompletion == 1 ? MissionObjectiveType.Photograph : null);
            Assert.That(Loop.Rest(), Is.True); // The actual end-of-Day-4 Journal checkpoint.
            Assert.That(Loop.Day, Is.EqualTo(5));
            var checkpoint = ExpeditionSaveStore.Copy(Loop.Journal[Loop.Journal.Count - 1].checkpoint);
            yield return CompleteFieldworkViaDevices(poiId);
            var survey = Cabin.GetComponent<PhotoSurveyZone>();
            var catcher = Cabin.GetComponent<CreatureCatcher>();
            var poi = survey.FindPoi(poiId);
            Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), 0);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
                CaptureMinigamePlayModeTests.Win(catcher.minigame);
                Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            }
            Assert.That(Loop.MissionRuntime.HasObjective(photoId), Is.True);
            Assert.That(Loop.MissionRuntime.HasObjective(captureId), Is.True);
            Cabin.OpenComputer();
            var journal = Cabin.GetComponentInChildren<ExpeditionComputerView>(true);
            journal.OpenJournal(); journal.RequestRestore(); journal.ConfirmRestore();
            yield return null;
            Assert.That(Loop.Day, Is.EqualTo(4));
            Assert.That(Loop.MissionRuntime.HasObjective(photoId), Is.EqualTo(checkpointCompletion > 0));
            Assert.That(Loop.MissionRuntime.HasObjective(captureId), Is.EqualTo(checkpointCompletion == 2));
            Assert.That(Loop.MissionRuntime.IsContentPresent(poiId), Is.True);
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()),
                Is.EqualTo(JsonUtility.ToJson(checkpoint.zones.Find(z => z.zone == "Zone02").missionProgress)));
            Assert.That(Loop.SaveCurrent(), Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var restored, out _), Is.True);
            Assert.That(restored.current.inventory.Count, Is.EqualTo(checkpoint.inventory.Count));
            foreach (var item in checkpoint.inventory)
                Assert.That(restored.current.inventory.Find(value => value.id == item.id)?.quantity, Is.EqualTo(item.quantity));
            Assert.That(restored.current.photos.Count, Is.EqualTo(checkpoint.photos.Count));
            Cabin.ClosePanel();
            yield return CompleteFieldworkViaDevices(poiId);
            Assert.That(Loop.MissionRuntime.HasObjective(photoId), Is.True);
            Assert.That(Loop.MissionRuntime.HasObjective(captureId), Is.True);
            int rewards = Loop.MissionRuntime.ExportProgress().grantedRewards.Count;
            int cargo = Cabin.GetComponent<CreatureInventory>().GetCount("Z2_Creature_02");
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
            Assert.That(Loop.MissionRuntime.HasObjective(photoId), Is.True);
            Assert.That(Loop.MissionRuntime.HasObjective(captureId), Is.True);
            Assert.That(Loop.MissionRuntime.ExportProgress().grantedRewards.Count, Is.EqualTo(rewards));
            Assert.That(Cabin.GetComponent<CreatureInventory>().GetCount("Z2_Creature_02"), Is.EqualTo(cargo));
        }

        [UnityTest] public IEnumerator QaRepeatedGameplayRollbackUpdatesHistoricalMapsAndRejectsFailedJournal()
        {
            yield return ReachZone(2, true);
            while (Loop.Day < 4) Assert.That(Loop.Rest(), Is.True);
            Assert.That(Loop.Rest(), Is.True);
            var checkpoint = ExpeditionSaveStore.Copy(Loop.Journal[Loop.Journal.Count - 1].checkpoint);
            string originalProgress = JsonUtility.ToJson(checkpoint.zones.Find(z => z.zone == "Zone02").missionProgress);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                yield return CompleteFieldworkViaDevices("zone02-l3");
                Assert.That(Loop.Rest(), Is.True); yield return Reload();
                Assert.That(Loop.MissionRuntime.IsPoiComplete("zone02-l3"), Is.True);
                var start = ExpeditionSaveStore.Copy(Loop.ReadZoneState("Zone02").missionProgress);
                Cabin.ClosePanel(); Cabin.Navigation.Ship.HitTerrain(10000); yield return null;
                float timeout = Time.realtimeSinceStartup + 25;
                while ((flow.IsTransitioning || Loop == null || Loop.IsDeathInProgress) && Time.realtimeSinceStartup < timeout) yield return null;
                Assert.That(Loop, Is.Not.Null);
                Assert.That(Loop.IsDeathInProgress, Is.False);
                Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(JsonUtility.ToJson(start)));
                yield return QaReturnToZoneOne();
                yield return QaHistoricalL3(true);

                string path = ExpeditionSaveStore.PathOverride;
                string disk = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
                string runtime = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
                Cabin.OpenComputer(); var journal = Cabin.GetComponentInChildren<ExpeditionComputerView>(true);
                journal.OpenJournal();
                int page = Loop.Journal.Count - 1;
                while (Loop.Journal[page].day > 4)
                { Array.Find(journal.GetComponentsInChildren<UnityEngine.UI.Button>(true), button => button.name == "PreviousDay").onClick.Invoke(); page--; }
                Assert.That(journal.JournalText, Does.Contain("DAY 04"));
                ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-journal.json");
                try { journal.RequestRestore(); journal.ConfirmRestore(); }
                finally { ExpeditionSaveStore.PathOverride = path; }
                Assert.That(flow.IsTransitioning, Is.False);
                Assert.That(Loop.Zone, Is.EqualTo("Zone01")); Assert.That(Loop.Day, Is.EqualTo(6));
                Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(runtime));
                Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
                Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(backup));
                Assert.That(journal.JournalText, Does.Contain(Loop.LastError));
                journal.RequestRestore(); journal.ConfirmRestore(); yield return WaitTransition();
                Assert.That(Loop.Zone, Is.EqualTo("Zone02")); Assert.That(Loop.Day, Is.EqualTo(4));
                Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(originalProgress));
                Assert.That(ExpeditionSaveStore.TryRead(out var rolledBack, out _), Is.True);
                Assert.That(rolledBack.current.photos.Count, Is.EqualTo(checkpoint.photos.Count));
                Assert.That(rolledBack.current.capturesTaken, Is.EqualTo(checkpoint.capturesTaken));
                foreach (var item in checkpoint.inventory)
                    Assert.That(Cabin.GetComponent<CreatureInventory>().GetCount(item.id), Is.EqualTo(item.quantity));
                Assert.That(Cabin.GetComponent<CreatureInventory>().Items.Count, Is.EqualTo(checkpoint.inventory.Count));
                yield return QaReturnToZoneOne(); yield return QaHistoricalL3(false);
                yield return ReachZoneFromCurrent(2);
                Assert.That(Loop.Rest(), Is.True); // Re-establish a genuine day-start, never manufacture one after Journal.
            }
        }

        private IEnumerator QaReturnToZoneOne()
        {
            Cabin.ClosePanel(); Route.Evaluate();
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.returnArea.mapPosition, 90, 500, 0);
            Route.Evaluate(); Assert.That(flow.IsTransitioning, Is.False);
            Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
            yield return WaitTransition(); Assert.That(Loop.Zone, Is.EqualTo("Zone01"));
        }

        private IEnumerator QaHistoricalL3(bool completed)
        {
            Assert.That(Loop.SaveCurrent(), Is.True);
            string disk = File.ReadAllText(ExpeditionSaveStore.SavePath);
            var world = Cabin.GetComponent<WorldMapController>(); world.OpenZone(1); yield return null;
            var presentation = world.zoneMaps[1].GetComponent<ZoneMapPresentation>();
            int index = Array.FindIndex(presentation.config.locations, poi => poi.id == "zone02-l3");
            Assert.That(presentation.overlay.completionIcons[index].enabled, Is.EqualTo(completed));
            CollectionAssert.AllItemsAreUnique(Loop.ReadZoneState("Zone02").missionProgress.grantedRewards);
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath), Is.EqualTo(disk), "Historical views are read-only.");
            Cabin.ClosePanel();
        }

        [UnityTest] public IEnumerator RoundThreeHistoricalZoneOneMapKeepsChecksAfterTravel()
        {
            yield return ReachZone(2);
            var world = Cabin.GetComponent<WorldMapController>();
            world.OpenZone(0);
            yield return null;
            var overlay = world.zoneMaps[0].GetComponent<ZoneMapPresentation>().overlay;
            foreach (var icon in overlay.completionIcons) Assert.That(icon.enabled, Is.True);
            Assert.That(Loop.Zone, Is.EqualTo("Zone02"));
        }

        [UnityTest] public IEnumerator RoundThreeRestoreProgressDoesNotGrantMissingRewardReceipts()
        {
            yield return ReachZone(2);
            var runtime = Loop.MissionRuntime;
            int before = Cabin.GetComponent<CreatureInventory>().GetCount("Z2_Creature_02");
            var restored = runtime.ExportProgress();
            restored.completedObjectives.Add("Z2_L2_PHOTO");
            restored.completedObjectives.Add("Z2_L2_CAPTURE");
            int receipts = restored.grantedRewards.Count;
            runtime.RestoreProgress(restored);
            Assert.That(runtime.ProgressState.grantedRewards.Count, Is.EqualTo(receipts), "Restore is not a reward transaction.");
            Assert.That(Cabin.GetComponent<CreatureInventory>().GetCount("Z2_Creature_02"), Is.EqualTo(before));
        }

        [UnityTest] public IEnumerator RoundThreeJournalCrossZoneDiscardsFutureTimelineAndContinue()
        {
            while (Loop.Day < 4) Assert.That(Loop.Rest(), Is.True);
            Assert.That(Loop.Rest(), Is.True);
            yield return ReachZone(2);
            yield return CompleteFieldworkViaDevices("zone02-l3");
            Assert.That(Loop.RestoreDay(4), Is.True);
            yield return WaitTransition();
            Assert.That(Loop.Zone, Is.EqualTo("Zone01")); Assert.That(Loop.Day, Is.EqualTo(4));
            Assert.That(Loop.MissionRuntime.CompletedCount, Is.Zero);
            Assert.That(Cabin.GetComponent<CreatureInventory>().Items, Is.Empty);
            Assert.That(Cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(500));
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
            Assert.That(ExpeditionSaveStore.TryRead(out var restored, out _), Is.True);
            Assert.That(restored.current.zones.Exists(z => z.zone == "Zone02"), Is.False);
            Assert.That(Loop.MissionRuntime.CompletedCount, Is.Zero);
            yield return ReachZone(2);
            Assert.That(Loop.MissionRuntime.HasObjective("Z2_L2_CAPTURE"), Is.False);
        }

        [UnityTest] public IEnumerator RoundThreeReturnTripsPreserveTimelineResourcesAndDoNotLoop()
        {
            for (int zone = 2; zone <= 4; zone++)
            {
                yield return ReachZoneFromCurrent(zone);
                string source = Loop.Zone, destination = Loop.ActiveMap.returnDestinationZone;
                int day = Loop.Day;
                Assert.That(Loop.Rest(), Is.True);
                Assert.That(Loop.RestoreDay(day), Is.True, "Backward travel must use the restored Journal timeline.");
                var state = Cabin.Navigation.Ship.Export(); state.energy = 43; state.radar = 3;
                Cabin.Navigation.Ship.Restore(state);
                string sourceProgress = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
                var items = Loop.ReadZoneState(destination).missionProgress.completedObjectives.ToArray();
                var cargo = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var item in Cabin.GetComponent<CreatureInventory>().Items) cargo.Add(item.Id, item.Quantity);
                Route.Evaluate();
                Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.returnArea.mapPosition, 90, Mathf.Max(500, Loop.ActiveMap.minimumDepth), 0);
                Route.Evaluate();
                Assert.That(flow.IsTransitioning, Is.False, "Restoration inside a return gate is not arrival.");
                Cabin.Navigation.Navigate(0, 1, 0, .1f); Route.Evaluate();
                Assert.That(flow.IsTransitioning, Is.False, "Turning alone cannot activate return.");
                Cabin.Navigation.StepDepth(1, .1f); Route.Evaluate();
                Assert.That(flow.IsTransitioning, Is.False, "Depth alone cannot activate return.");
                float depth = Cabin.Navigation.Depth;
                if (Loop.ActiveMap.ReturnOverlapsEntry)
                {
                    Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
                    Assert.That(flow.IsTransitioning, Is.False, "An overlapping entry requires actual boundary reentry.");
                    RestoreReturnApproach(Loop.ActiveMap.returnArea);
                    Route.Evaluate();
                    for (int i = 0; i < 30 && !Loop.ActiveMap.returnArea.Contains(Cabin.Navigation.Position); i++)
                        Cabin.Navigation.Step(1, 0, .1f);
                    Cabin.Navigation.Brake();
                    Assert.That(Loop.ActiveMap.returnArea.Contains(Cabin.Navigation.Position), Is.True);
                }
                else { Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); }
                var departing = Cabin.Navigation.Ship.Export();
                Route.Evaluate(); yield return WaitTransition();
                Assert.That(Loop.Zone, Is.EqualTo(destination)); Assert.That(Loop.Day, Is.EqualTo(day));
                Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.backwardArrivalPosition));
                Assert.That(Loop.ActiveMap.exitArea.Contains(Cabin.Navigation.Position), Is.False);
                Assert.That(Cabin.Navigation.Depth, Is.EqualTo(Mathf.Clamp(depth, Loop.ActiveMap.minimumDepth, Cabin.Navigation.Ship.MaximumDepth)));
                Assert.That(Cabin.Navigation.Ship.Energy, Is.EqualTo(departing.energy));
                Assert.That(Cabin.Navigation.Ship.Radar, Is.EqualTo(departing.radar));
                CollectionAssert.AreEquivalent(items, Loop.MissionRuntime.ExportProgress().completedObjectives);
                AssertCargo(cargo);
                Route.Evaluate(); yield return null; Assert.That(flow.IsTransitioning, Is.False);
                Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
                Assert.That(Loop.Zone, Is.EqualTo(destination)); Route.Evaluate();
                Assert.That(flow.IsTransitioning, Is.False);
                Assert.That(Loop.Rest(), Is.True); day++;
                Route.Evaluate(); Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.exitArea.mapPosition, 0, depth, 0);
                Route.Evaluate(); Assert.That(flow.IsTransitioning, Is.False);
                Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
                yield return WaitTransition();
                Assert.That(Loop.Zone, Is.EqualTo(source)); Assert.That(Loop.Day, Is.EqualTo(day));
                Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(sourceProgress));
                AssertCargo(cargo);
                if (source == "Zone04") Assert.That(Loop.ReadZoneState("Zone03").progress.rockDestroyed, Is.True);
            }
        }

        private void AssertCargo(System.Collections.Generic.Dictionary<string, int> expected)
        {
            var inventory = Cabin.GetComponent<CreatureInventory>();
            Assert.That(inventory.Items.Count, Is.EqualTo(expected.Count));
            foreach (var item in expected) Assert.That(inventory.GetCount(item.Key), Is.EqualTo(item.Value), item.Key);
        }
        private void RestoreReturnApproach(G10.Prototype.Navigation.MapPoi point)
        {
            var nav = Cabin.Navigation;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outside = point.mapPosition + direction * (point.arrivalRadius + 6);
                bool clear = true;
                for (int distance = 0; distance <= 24 && clear; distance++) clear &= nav.CanOccupy(outside - direction * distance);
                if (!clear) continue;
                Vector2 delta = point.mapPosition - outside;
                nav.RestoreVoyage(outside, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg, nav.Depth, nav.DistanceTravelled);
                return;
            }
            Assert.Fail("No clear return-gate approach: " + point.id);
        }

        private IEnumerator ReachZoneFromCurrent(int target)
        {
            // Existing device helper owns every mission action; avoid replacing the timeline to seed travel.
            while (int.Parse(Loop.Zone.Substring(4)) < target)
            {
                var map = Loop.ActiveMap;
                yield return CompleteFieldworkViaDevices();
                var story = Cabin.GetComponent<ZoneOneStory>();
                while (story.PendingGateObjective != null)
                {
                    if (story.PendingGateObjective.type == MissionObjectiveType.DestroyObstacle)
                        Cabin.Navigation.RestoreVoyage(map.rockInteractionArea.mapPosition, 0, map.rockInteractionArea.targetDepth, 0);
                    Assert.That(story.ApplyProgressionAction(), Is.True);
                }
                yield return WaitForRockSequence();
                Route.Evaluate();
                Cabin.Navigation.RestoreVoyage(map.exitArea.mapPosition, 0, Mathf.Max(500, map.entryDepth), 0); Route.Evaluate();
                Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate(); yield return WaitTransition();
            }
        }

        private IEnumerator ReachZone(int destination, bool useDevices = false)
        {
            for (int zone = 1; zone < destination; zone++)
            {
                var loop = Loop;
                int travelDay = loop.Day, travelDaysLeft = loop.DaysLeft;
                var config = loop.ActiveMap;
                string target = config.destinationZone;
                Assert.That(loop.CurrentProgress.exitUnlocked, Is.False);
                flow.LoadZone(target);
                Assert.That(flow.IsTransitioning, Is.False, "Locked route cannot be bypassed.");
                if (zone == 3) Assert.That(Cabin.Navigation.CanOccupy(config.exitArea.mapPosition), Is.False);
                yield return CompleteFieldworkViaDevices();
                var story = Cabin.GetComponent<ZoneOneStory>();
                while (story.PendingGateObjective != null)
                {
                    if (story.PendingGateObjective.type == MissionObjectiveType.DestroyObstacle)
                        Cabin.Navigation.RestoreVoyage(config.rockInteractionArea.mapPosition, 0, config.rockInteractionArea.targetDepth, 0);
                    var upgrade = Array.Find(Cabin.GetComponentsInChildren<UpgradeEntryConfig>(true), item => item.UpgradeId == "ExpeditionModule");
                    Assert.That(upgrade, Is.Not.Null);
                    Assert.That(upgrade.TryApply(), Is.True, story.ProgressionActionDescription);
                }
                Assert.That(Cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(zone == 1 ? 500 : 750));
                foreach (var cost in story.config.zoneGate.objectives[0].materials)
                    Assert.That(Cabin.GetComponent<CreatureInventory>().GetCount(cost.itemId), Is.Zero, "Story material consumed once");
                yield return WaitForRockSequence();
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
                Assert.That(Loop.SaveCurrent(), Is.True);
                Assert.That(ExpeditionSaveStore.TryRead(out var beforeTravel, out _), Is.True);
                var knownDestination = beforeTravel.current.zones.Find(z => z.zone == target && z.hasVoyage);
                Cabin.Navigation.RestoreVoyage(config.exitArea.mapPosition, 0, config.entryDepth, 0);
                if (useDevices) RecordShipStats("before " + target);
                Route.Evaluate();
                Assert.That(flow.IsTransitioning, Is.False, "Position restoration is not automatic arrival.");
                Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
                yield return WaitTransition();
                if (useDevices) RecordShipStats("entered " + target);
                Assert.That(Loop.Zone, Is.EqualTo(target), flow.LastError);
                Assert.That(Loop.Day, Is.EqualTo(travelDay)); Assert.That(Loop.DaysLeft, Is.EqualTo(travelDaysLeft));
                Assert.That(Loop.TotalDays, Is.EqualTo(25));
                Assert.That(Cabin.Navigation.Position, Is.EqualTo(knownDestination != null ? knownDestination.position : Loop.ActiveMap.entryPosition));
                Assert.That(Cabin.Navigation.Depth, Is.EqualTo(Mathf.Clamp(knownDestination != null ? knownDestination.depth : config.entryDepth,
                    Loop.ActiveMap.minimumDepth, Cabin.Navigation.Ship.MaximumDepth)));
                Assert.That(Cabin.Navigation.MinimumDepth, Is.EqualTo(Loop.ActiveMap.minimumDepth));
                Assert.That(Cabin.GetComponent<CaptureMinigameController>().profile, Is.SameAs(Loop.ActiveMap.captureMinigameProfile));
                if (target == "Zone04")
                    Assert.That(Cabin.GetComponent<G10.Prototype.Feedback.CabinFeedbackController>().NotificationText,
                        Does.Contain("DEPTH ADJUSTED").And.Contain("500"));
                Assert.That(SceneManager.GetSceneByName("Zone01").isLoaded, Is.True);
                Assert.That(SceneManager.GetSceneByName(target).isLoaded, Is.False, "Reuse the cabin, not placeholder scenes.");
            }
        }

        [UnityTest] public IEnumerator RoundTwoZoneTwoFarmThenZoneThreeCaptureContinueAndRest()
        {
            yield return ReachZone(2);
            yield return CompleteFieldworkViaDevices();
            var survey = Cabin.GetComponent<PhotoSurveyZone>();
            var catcher = Cabin.GetComponent<CreatureCatcher>();
            var poi = survey.FindPoi("zone02-l3");
            Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), 0);
            int charges = Cabin.Navigation.Ship.Captures;
            for (int i = 0; i < 8; i++)
            {
                var result = catcher.TryCapture();
                Assert.That(result, Is.EqualTo(CreatureCatcher.Result.Started));
                CaptureMinigamePlayModeTests.Win(catcher.minigame);
                Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
                Assert.That(Cabin.Navigation.Ship.Captures, Is.EqualTo(charges));
            }
            var story = Cabin.GetComponent<ZoneOneStory>();
            while (story.PendingGateObjective != null)
            {
                var upgrade = Array.Find(Cabin.GetComponentsInChildren<UpgradeEntryConfig>(true), item => item.UpgradeId == "ExpeditionModule");
                Assert.That(upgrade.TryApply(), Is.True, story.ProgressionActionDescription);
            }
            Route.Evaluate();
            Assert.That(Loop.CurrentProgress.exitUnlocked, Is.True);
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.exitArea.mapPosition, 0, 470, 0);
            Route.Evaluate();
            Cabin.Navigation.Step(1, 0, .2f); Cabin.Navigation.Brake(); Route.Evaluate();
            yield return WaitTransition();
            Assert.That(Loop.Zone, Is.EqualTo("Zone03"));
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(new Vector2(11, 97)));
            Assert.That(Cabin.Navigation.Depth, Is.EqualTo(470));
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
            Assert.That(Cabin.Navigation.Depth, Is.EqualTo(470));
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(new Vector2(11, 97)));
            survey = Cabin.GetComponent<PhotoSurveyZone>(); catcher = Cabin.GetComponent<CreatureCatcher>();
            poi = survey.FindPoi("zone03-l1");
            Assert.That(Vector2.Distance(poi.mapPosition, survey.ContactPosition(poi)), Is.GreaterThan(poi.arrivalRadius));
            Cabin.Navigation.RestoreVoyage(poi.mapPosition, 0, survey.DepthFor(poi), 0);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.TooFar));
            Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), 0);
            Assert.That(survey.TryGetRadarContact(Cabin.Navigation, 85, out _, out _), Is.True);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.PhotoRequired));
            yield return CompleteFieldworkViaDevices();
            Vector2 contact = survey.ContactPosition(poi);
            Assert.That(Loop.SaveCurrent(), Is.True);
            yield return Reload();
            survey = Cabin.GetComponent<PhotoSurveyZone>(); catcher = Cabin.GetComponent<CreatureCatcher>();
            poi = survey.FindPoi("zone03-l1");
            Assert.That(survey.ContactPosition(poi), Is.EqualTo(contact));
            int objectives = Loop.MissionRuntime.CompletedCount;
            for (int day = 0; day < 2; day++)
            {
                var state = Cabin.Navigation.Ship.Export(); state.captures = 0; Cabin.Navigation.Ship.Restore(state);
                Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi) + 20, 0);
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
                    CaptureMinigamePlayModeTests.Win(catcher.minigame);
                    Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
                    Assert.That(Cabin.Navigation.Ship.Captures, Is.Zero);
                }
                Assert.That(Loop.MissionRuntime.CompletedCount, Is.EqualTo(objectives), "Repeats must not duplicate objectives/rewards.");
                Assert.That(Loop.Rest(), Is.True);
            }
        }

        [UnityTest] public IEnumerator ExistingDestinationVoyageKeepsDepthAndHeadingButNewGameDoesNotLeakIt()
        {
            Assert.That(Loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return WaitTransition();
            Assert.That(ExpeditionSaveStore.TryRead(out var save, out _), Is.True);
            // Existing destination snapshot, as carried in a returning voyage; do not overwrite it with departure data.
            save.current.zones.Add(new ExpeditionZoneState { zone = "Zone02", deadline = 15,
                mapCoordinateVersion = ZoneMapConfig.CurrentCoordinateVersion, hasVoyage = true,
                position = new Vector2(45, 595), heading = 135, depth = 470, distance = 60 });
            ExpeditionSaveStore.Write(save, true);
            flow.ContinueGame(); yield return WaitTransition();
            yield return ReachZone(2);
            Assert.That(Cabin.Navigation.Depth, Is.EqualTo(470)); Assert.That(Cabin.Navigation.Heading, Is.EqualTo(135));
            Assert.That(Cabin.Navigation.DistanceTravelled, Is.EqualTo(60));
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
            Assert.That(Cabin.Navigation.Depth, Is.EqualTo(470)); Assert.That(Cabin.Navigation.Heading, Is.EqualTo(135));
            flow.StartNewGame(); yield return WaitTransition();
            Assert.That(Loop.Zone, Is.EqualTo("Zone01")); Assert.That(Loop.Day, Is.EqualTo(1));
            Assert.That(Cabin.Navigation.Depth, Is.EqualTo(230)); Assert.That(Cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(500));
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(Loop.ActiveMap.entryPosition));
            Assert.That(Cabin.GetComponent<CreatureInventory>().Items, Is.Empty);
            Assert.That(ExpeditionSaveStore.TryRead(out var fresh, out _), Is.True); Assert.That(fresh.current.zones.Count, Is.EqualTo(1));
        }

        private static string StatPath => Path.Combine(Application.dataPath, "../Temp/expedition-device-transition-stats.tsv");
        private void RecordShipStats(string stage)
        {
            Assert.That(Loop.SaveCurrent(), Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
            var s = Cabin.Navigation.Ship.Export();
            Assert.That(JsonUtility.ToJson(saved.current.ship), Is.EqualTo(JsonUtility.ToJson(s)));
            File.AppendAllText(StatPath, $"{stage}\t{s.hull}/{s.hullCapacity}\t{s.speed}\t{s.diveSpeed}\t{s.ascentSpeed}\t{s.maximumDepth}\t{s.energy}/{s.energyCapacity}\t{s.radar}/{s.radarCapacity}\t{s.photos}/{s.photoCapacity}\t{s.captures}/{s.captureCapacity}\n");
        }

        private IEnumerator WaitForRockSequence()
        {
            // The dedicated presentation fixture checks real wall time; flow tests advance its clock.
            var effect = Cabin.GetComponent<G10.Prototype.Feedback.RockBreakerSequence>();
            if (effect != null && effect.IsActive) effect.Tick(effect.Duration);
            yield return null;
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
            // NEW GAME includes the real 35-second Zone01 opening.
            float timeout = Time.realtimeSinceStartup + 45;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(flow.IsTransitioning, Is.False, "Transition timed out");
            Assert.That(flow.LastError, Is.Null.Or.Empty);
            yield return null;
        }
    }
}

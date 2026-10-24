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
    public sealed class PhotoSubmissionPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private PhotoCaptureService Capture => Cabin.GetComponent<PhotoCaptureService>();

        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"PhotoSubmission-"+Guid.NewGuid().ToString("N"));
            oldSave=ExpeditionSaveStore.PathOverride;oldPhotos=PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"timeline.json");
            PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            TutorialTestSave.SeedReturningPlayer();
            if(SceneFlowController.Instance!=null){Object.Destroy(SceneFlowController.Instance.gameObject);yield return null;}
            yield return SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single);
            yield return null;flow=SceneFlowController.Instance;
            yield return WaitTransition();flow.ContinueGame();yield return WaitTransition();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(flow!=null)Object.Destroy(flow.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride=oldSave;PhotoCaptureService.ArchivePathOverride=oldPhotos;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        private IEnumerator WaitTransition()
        {
            yield return null;
            float end=Time.realtimeSinceStartup+25;
            while(flow.IsTransitioning&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(flow.IsTransitioning,Is.False);Assert.That(flow.LastError,Is.Null.Or.Empty);
        }
        private IEnumerator Reload()
        {flow.LoadMainMenu();yield return WaitTransition();flow.ContinueGame();yield return WaitTransition();}
        private void Aim(string poiId="zone01-left")
        {
            var survey=Capture.survey;var poi=survey.FindPoi(poiId);
            Assert.That(poi,Is.Not.Null);
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI/8;
                var position=survey.ContactPosition(poi)-new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*20;
                if(!Cabin.Navigation.CanOccupy(position))continue;
                Cabin.Navigation.RestoreVoyage(position,i*22.5f,survey.DepthFor(poi),0);
                if(survey.TryGetPhotoContact(Cabin.Navigation,Capture.profile.visibleDistance,Capture.profile.fieldOfView,out var found,out _)&&found==poi)return;
            }
            Assert.Fail("No reachable photo angle: "+poiId);
        }
        private PhotoRecord Candidate(string poiId="zone01-left")
        {
            Aim(poiId);var photo=Capture.Capture();
            Assert.That(photo,Is.Not.Null,Capture.LastError);
            Assert.That(photo.MissionObjectiveId,Is.Not.Null.And.Not.Empty,photo.Result);
            Assert.That(photo.IsMissionPhoto,Is.False);
            Assert.That(Loop.MissionRuntime.HasObjective(photo.MissionObjectiveId),Is.False);
            return photo;
        }
        private PhotoLabView OpenLab()
        {
            Cabin.OpenComputer();var desktop=Cabin.GetComponentInChildren<ComputerScreenController>(true);
            desktop.OpenPhotoLab();return desktop.GetComponentInChildren<PhotoLabView>(true);
        }
        private IEnumerator Die()
        {
            Cabin.ClosePanel();Cabin.Navigation.Ship.HitTerrain(10000);yield return null;
            float end=Time.realtimeSinceStartup+20;
            while(Loop.IsDeathInProgress&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Loop.IsDeathInProgress,Is.False,Loop.LastError);
        }

        // Provision prior zones only; current-zone photographs still use the authored camera,
        // pending evidence, timeline persistence and the real Photo Lab SEND callback.
        private IEnumerator StartPhotoAuditZone(int zone)
        {
            flow.LoadMainMenu(); yield return WaitTransition();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>();
            var preset = DebugExpeditionPreset.Create(zone, menu.maps, menu.contentCatalog, menu.initialShip);
            preset.tutorial.completed = true;
            ExpeditionSaveStore.Write(preset, discardFuture: true);
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Loop.Zone, Is.EqualTo($"Zone0{zone}"));
            Assert.That(Loop.MissionRuntime.CompletedCount, Is.Zero);
        }

        [UnityTest] public IEnumerator EveryAuthoredPhotographAcrossFourZonesRequiresManualSendAfterContinue()
        {
            int photographs = 0, combined = 0, hidden = 0;
            for (int zone = 1; zone <= 4; zone++)
            {
                yield return StartPhotoAuditZone(zone);
                var locations = Loop.MissionRuntime.config.locations;
                foreach (var location in locations)
                {
                    var objective = Array.Find(location.objectives, item => item.type == MissionObjectiveType.Photograph);
                    if (objective == null) continue;
                    if (location.visibility == LocationVisibility.HiddenRadar)
                    {
                        var route = Cabin.GetComponent<ExpeditionProgression>();
                        route.Evaluate();
                        var dialogue = Object.FindAnyObjectByType<DialogueController>();
                        if (dialogue.IsChoiceActive) dialogue.SelectChoice(1);
                        Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.True);
                        Assert.That(Loop.MissionRuntime.RevealPoi(location.poiId), Is.True);
                        hidden++;
                    }
                    Cabin.ClosePanel();
                    Aim(location.poiId);
                    string progressBeforeCapture = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
                    int charges = Cabin.Navigation.Ship.Photos, taken = Capture.TotalPhotosTaken;
                    Cabin.OpenCamera(); yield return null; yield return null;
                    Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(taken + 1), objective.id);
                    var photo = Capture.Photos[Capture.Photos.Count - 1];
                    string photoId = photo.Id;
                    Assert.That(photo.MissionObjectiveId, Is.EqualTo(objective.id), photo.Result);
                    Assert.That(photo.IsMissionPhoto, Is.False, objective.id);
                    Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(progressBeforeCapture),
                        "Taking a photo cannot complete any objective or grant its rewards: " + objective.id);
                    Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges - 1));
                    Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
                    photo = Capture.Photos[Capture.Photos.Count - 1];
                    Assert.That(photo.Id, Is.EqualTo(photoId));
                    Assert.That(photo.IsMissionPhoto, Is.False);
                    Assert.That(Capture.CanSubmitPhoto(photo), Is.True, objective.id);
                    Assert.That(Loop.MissionRuntime.HasObjective(objective.id), Is.False);

                    var captureObjective = Loop.MissionRuntime.FindObjective(location.poiId, MissionObjectiveType.Capture, objective.targetId);
                    if (captureObjective != null)
                    {
                        var poi = Capture.survey.FindPoi(location.poiId);
                        Cabin.Navigation.RestoreVoyage(Capture.survey.ContactPosition(poi), 0, Capture.survey.DepthFor(poi), 0);
                        Assert.That(Cabin.GetComponent<CreatureCatcher>().TryCapture(), Is.EqualTo(CreatureCatcher.Result.PhotoRequired));
                    }
                    var lab = OpenLab();
                    Assert.That(lab.SelectedPhotoId, Is.EqualTo(photoId));
                    Assert.That(lab.sendButton.interactable, Is.True);
                    lab.sendButton.onClick.Invoke();
                    Assert.That(photo.IsMissionPhoto, Is.True, objective.id);
                    Assert.That(Loop.MissionRuntime.HasObjective(objective.id), Is.True);
                    Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges - 1));
                    string submitted = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
                    lab.sendButton.onClick.Invoke();
                    Assert.That(Capture.SubmitPhoto(photo), Is.False);
                    Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(submitted));
                    Cabin.ClosePanel();
                    if (captureObjective != null)
                    {
                        var catcher = Cabin.GetComponent<CreatureCatcher>();
                        Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
                        catcher.minigame.Cancel();
                        Assert.That(Loop.MissionRuntime.HasObjective(captureObjective.id), Is.False,
                            "SEND unlocks capture; it does not complete the separate capture objective.");
                        Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
                        CaptureMinigamePlayModeTests.Win(catcher.minigame);
                        Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
                        combined++;
                    }
                    photographs++;
                }
                Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
                foreach (var photo in Capture.Photos)
                {
                    Assert.That(photo.IsMissionPhoto, Is.True, photo.MissionObjectiveId);
                    Assert.That(Loop.MissionRuntime.HasObjective(photo.MissionObjectiveId), Is.True);
                    Assert.That(Capture.SubmitPhoto(photo), Is.False);
                }
            }
            Assert.That(photographs, Is.EqualTo(11), "Audit every authored Photograph objective, including the hidden route.");
            Assert.That(combined, Is.EqualTo(6));
            Assert.That(hidden, Is.EqualTo(2));
        }

        [UnityTest] public IEnumerator CompletedLegacyPhotoWithoutEvidenceSurvivesContinueAndPreviousDayRestore()
        {
            // Existing completed work has authority even when its old save contains no archive record.
            Assert.That(Loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return WaitTransition();
            Assert.That(ExpeditionSaveStore.TryRead(out var legacy, out var error), Is.True, error);
            legacy.version = 3;
            legacy.current.zones[0].zoneOneStoryProgress = (int)(ZoneOneStory.Progress.PhotoOne | ZoneOneStory.Progress.Analysis);
            legacy.current.zones[0].missionProgress = new MissionProgressState();
            legacy.current.photos.Clear(); legacy.current.photosTaken = 0;
            legacy.hasDayStart = false; legacy.dayStart = null;
            ExpeditionSaveStore.Write(legacy, discardFuture: true);
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.PhotoOneObjective), Is.True);
            Assert.That(Cabin.GetComponent<CreatureInventory>().GetCount("Z1_Creature_01"), Is.Zero, "Legacy PHOTO cannot manufacture capture material");
            Assert.That(Capture.Photos, Is.Empty);
            string completed = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            int previousDay = Loop.Day;
            Assert.That(Loop.Rest(), Is.True);
            var future = Candidate("zone01-north");
            Assert.That(Capture.SubmitPhoto(future), Is.True);
            Assert.That(Loop.RestoreDay(previousDay), Is.True);
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(completed));
            Assert.That(Capture.Photos, Is.Empty);
            Assert.That(Loop.SaveCurrent(), Is.True); yield return Reload();
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(completed));
            Assert.That(Capture.Photos, Is.Empty);
        }

        [UnityTest] public IEnumerator CaptureCandidateRequiresSendAndSubmissionCostsNothing()
        {
            var empty=OpenLab();Assert.That(empty.sendButton.interactable,Is.False);Cabin.ClosePanel();
            int charges=Cabin.Navigation.Ship.Photos;
            var photo=Candidate();
            Assert.That(Capture.Photos.Count,Is.EqualTo(1));Assert.That(Capture.TotalPhotosTaken,Is.EqualTo(1));
            Assert.That(Cabin.Navigation.Ship.Photos,Is.EqualTo(charges-1));
            var lab=OpenLab();Assert.That(lab.sendButton.interactable,Is.True);
            Assert.That(lab.DisplayedText,Does.Contain("MISSION DATA DETECTED"));
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,"photo-lab-candidate.png");
            foreach(var text in lab.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing,Is.False,text.name+": "+text.text);
            }
            lab.sendButton.onClick.Invoke();
            Assert.That(photo.IsMissionPhoto,Is.True);Assert.That(Loop.MissionRuntime.HasObjective(photo.MissionObjectiveId),Is.True);
            Assert.That(lab.sendButton.interactable,Is.False);Assert.That(lab.DisplayedText,Does.Contain("MISSION DATA SUBMITTED"));
            Assert.That(Cabin.Navigation.Ship.Photos,Is.EqualTo(charges-1));
            string state=JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            Assert.That(Capture.SubmitPhoto(photo),Is.False);lab.sendButton.onClick.Invoke();
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()),Is.EqualTo(state));
            yield return null;
        }
        [UnityTest] public IEnumerator FailedSendKeepsRuntimeJsonArchiveAndUiRetryable()
        {
            Aim(); Cabin.OpenCamera(); yield return null; yield return null;
            var photo = Capture.Photos[Capture.Photos.Count - 1];
            Assert.That(photo.MissionObjectiveId, Is.Not.Empty);
            Assert.That(Loop.SaveCurrent(), Is.True);
            string path = ExpeditionSaveStore.SavePath;
            string disk = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
            string archivePath = Path.Combine(Capture.ArchivePath, photo.Id + ".json");
            string archive = File.ReadAllText(archivePath);
            string progress = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            int charges = Cabin.Navigation.Ship.Photos, notifications = 0;
            Action changed = () => { if (Loop.MissionRuntime.HasObjective(photo.MissionObjectiveId)) notifications++; };
            Loop.MissionRuntime.Changed += changed;
            var lab = OpenLab();
            try
            {
                // Failure before staging and failure at atomic primary replacement are distinct boundaries.
                for (int fault = 0; fault < 2; fault++)
                {
                    string original = ExpeditionSaveStore.PathOverride;
                    FileStream locked = null;
                    try
                    {
                        if (fault == 0) ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-send.json");
                        else locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        lab.sendButton.onClick.Invoke();
                    }
                    finally { locked?.Dispose(); ExpeditionSaveStore.PathOverride = original; }
                    {
                        Assert.That(photo.IsMissionPhoto, Is.False, "Failed SEND must remain a candidate.");
                        Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(progress));
                        Assert.That(notifications, Is.Zero, "No success event before durability.");
                        Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
                        Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(backup));
                        Assert.That(File.ReadAllText(archivePath), Is.EqualTo(archive));
                        Assert.That(Capture.LastError, Is.Not.Null.And.Not.Empty);
                        Assert.That(lab.DisplayedText, Does.Contain(Capture.LastError));
                        Assert.That(lab.sendButton.interactable, Is.True);
                        Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges));
                    }
                    if (fault == 0)
                    {
                        yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "qa-send-rejected.png");
                        foreach (var text in lab.GetComponentsInChildren<TMPro.TMP_Text>())
                        { text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text); }
                    }
                }
                lab.sendButton.onClick.Invoke();
                Assert.That(photo.IsMissionPhoto, Is.True); Assert.That(notifications, Is.EqualTo(1));
                Assert.That(lab.DisplayedText, Does.Contain("MISSION DATA SUBMITTED"));
                Assert.That(Capture.LastError, Is.Null.Or.Empty);
                string completed = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
                Assert.That(Capture.SubmitPhoto(photo), Is.False);
                yield return Reload();
                Assert.That(Capture.Photos[0].IsMissionPhoto, Is.True);
                Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(completed));
                Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges));
                AssertArchive(true, photo.MissionObjectiveId);
            }
            finally { if (Loop != null) Loop.MissionRuntime.Changed -= changed; }
        }

        [UnityTest] public IEnumerator FailedSendRollsBackEveryRewardListCargoAndCounter()
        {
            // Engineered authority fan-out, NOT proof of authored gameplay progression.
            var runtime = Loop.MissionRuntime; var original = runtime.config;
            var clone = Object.Instantiate(original); runtime.config = clone;
            var location = clone.FindLocationByPoi("zone01-left");
            location.objectives = new[] { Array.Find(location.objectives, o => o.type == MissionObjectiveType.Photograph) };
            location.rewards = new[] {
                new MissionRewardConfig { type=MissionRewardType.Item, targetId="Z1_Creature_01" },
                new MissionRewardConfig { type=MissionRewardType.ResearchData, targetId="qa-research" },
                new MissionRewardConfig { type=MissionRewardType.UnlockRecipe, targetId="qa-recipe" },
                new MissionRewardConfig { type=MissionRewardType.SetWorldFlag, targetId="qa-flag" },
                new MissionRewardConfig { type=MissionRewardType.RevealLocation, targetId="qa-reveal" },
                new MissionRewardConfig { type=MissionRewardType.UnlockZone, targetId="Zone02" },
                new MissionRewardConfig { type=MissionRewardType.Ending, targetId="qa-ending" } };
            var inventory = Cabin.GetComponent<CreatureInventory>();
            try
            {
                var photo = Candidate(); Assert.That(Loop.SaveCurrent(), Is.True);
                string progress = JsonUtility.ToJson(runtime.ExportProgress());
                int captures = Loop.CapturesToday, cargoEvents = 0, addedEvents = 0;
                inventory.Changed += () => cargoEvents++;
                inventory.ItemAdded += () => addedEvents++;
                string path = ExpeditionSaveStore.PathOverride, disk = File.ReadAllText(path);
                ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-fanout.json");
                bool result;
                try { result = Capture.SubmitPhoto(photo); }
                finally { ExpeditionSaveStore.PathOverride = path; }
                {
                    Assert.That(result, Is.False);
                    Assert.That(JsonUtility.ToJson(runtime.ExportProgress()), Is.EqualTo(progress));
                    Assert.That(inventory.Items, Is.Empty);
                    Assert.That(Loop.CapturesToday, Is.EqualTo(captures));
                    Assert.That(cargoEvents, Is.Zero); Assert.That(addedEvents, Is.Zero);
                    Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
                    Assert.That(photo.IsMissionPhoto, Is.False);
                }
                Assert.That(Loop.SaveCurrent(), Is.True, "An ordinary later save cannot leak a rejected reward counter.");
                Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
                Assert.That(saved.current.capturesTaken, Is.EqualTo(captures));
                Assert.That(Capture.SubmitPhoto(photo), Is.True);
                Assert.That(runtime.ProgressState.grantedRewards.Count, Is.EqualTo(location.rewards.Length));
                CollectionAssert.AllItemsAreUnique(runtime.ProgressState.grantedRewards);
                Assert.That(inventory.GetCount("Z1_Creature_01"), Is.EqualTo(1));
                Assert.That(Loop.CapturesToday, Is.EqualTo(captures + 1));
                Assert.That(cargoEvents, Is.EqualTo(1)); Assert.That(addedEvents, Is.EqualTo(1));
                Assert.That(Capture.SubmitPhoto(photo), Is.False);
                Assert.That(ExpeditionSaveStore.TryRead(out saved, out _), Is.True);
                Assert.That(saved.current.capturesTaken, Is.EqualTo(captures + 1));
                runtime.config = original; Object.Destroy(clone); clone = null;
                yield return Reload();
                Assert.That(Cabin.GetComponent<CreatureInventory>().GetCount("Z1_Creature_01"), Is.EqualTo(1));
                Assert.That(Loop.MissionRuntime.ProgressState.grantedRewards.Count, Is.EqualTo(7));
            }
            finally { if (runtime != null) runtime.config = original; if (clone != null) Object.Destroy(clone); }
        }

        [UnityTest] public IEnumerator ArchiveWriteFailureCannotUndoDurableSendOrDuplicateRewards()
        {
            var photo = Candidate(); string metadata = Path.Combine(Capture.ArchivePath, photo.Id + ".json");
            var lab = OpenLab();
            using (new FileStream(metadata, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                LogAssert.Expect(LogType.Warning, "Không lưu được ảnh ra ổ đĩa; ảnh vẫn còn trong phiên này.");
                lab.sendButton.onClick.Invoke();
            }
            Assert.That(photo.IsMissionPhoto, Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
            Assert.That(saved.current.photos.Find(p => p.id == photo.Id).mission, Is.True);
            Assert.That(Capture.LastError, Is.Not.Null.And.Not.Empty);
            Assert.That(lab.DisplayedText, Does.Contain(Capture.LastError));
            string progress = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            yield return Reload();
            Assert.That(Capture.Photos[0].IsMissionPhoto, Is.True);
            Assert.That(Capture.SubmitPhoto(Capture.Photos[0]), Is.False);
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(progress));
        }

        [UnityTest] public IEnumerator PausedSendRejectsStaleUiCallbackAndResumesWithoutCharge()
        {
            var photo = Candidate(); Assert.That(Loop.SaveCurrent(), Is.True);
            string path = ExpeditionSaveStore.SavePath, disk = File.ReadAllText(path);
            var lab = OpenLab(); int charges = Cabin.Navigation.Ship.Photos;
            var pause = PauseMenuController.Instance; pause.OpenPause();
            try
            {
                Assert.That(pause.IsPaused, Is.True);
                lab.sendButton.onClick.Invoke();
                {
                    Assert.That(photo.IsMissionPhoto, Is.False);
                    Assert.That(Loop.MissionRuntime.HasObjective(photo.MissionObjectiveId), Is.False);
                    Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
                    Assert.That(lab.sendButton.interactable, Is.False);
                }
            }
            finally { pause.Resume(); }
            lab.Refresh(); Assert.That(lab.sendButton.interactable, Is.True);
            lab.sendButton.onClick.Invoke(); Assert.That(photo.IsMissionPhoto, Is.True);
            Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges));
            yield return null;
        }

        [UnityTest] public IEnumerator PausedDeviceServicesCannotConsumeChargesOrMoveVessel()
        {
            var poi = Capture.survey.FindPoi("zone01-left");
            Cabin.Navigation.RestoreVoyage(Capture.survey.ContactPosition(poi), 0, Capture.survey.DepthFor(poi), 0);
            Cabin.ClosePanel();
            var ship = Cabin.Navigation.Ship; string resources = JsonUtility.ToJson(ship.Export());
            Vector2 position = Cabin.Navigation.Position;
            var pause = PauseMenuController.Instance; pause.OpenPause();
            try
            {
                Assert.That(pause.IsPaused, Is.True);
                Assert.That(Capture.Capture(), Is.Null, "Paused camera service must not consume a charge.");
                Cabin.Radar.Scan(); Assert.That(Cabin.Radar.IsScanning, Is.False);
                Assert.That(Cabin.GetComponent<CreatureCatcher>().TryCapture(), Is.EqualTo(CreatureCatcher.Result.Unavailable));
                Cabin.Navigation.Navigate(1, 1, 1, 1);
                Assert.That(Cabin.Navigation.Position, Is.EqualTo(position));
                Assert.That(JsonUtility.ToJson(ship.Export()), Is.EqualTo(resources));
                Assert.That(Capture.Photos, Is.Empty);
            }
            finally { pause.Resume(); }
            Assert.That(Cabin.Navigation.ExpeditionBlocked, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator TransitionAndDeathRejectStaleSendAndKeepUnsentDayStart()
        {
            var photo = Candidate(); Assert.That(Loop.Rest(), Is.True);
            var lab = OpenLab();
            Assert.That(flow.PresentRest(Loop), Is.True); Assert.That(flow.IsTransitioning, Is.True);
            lab.sendButton.onClick.Invoke(); Assert.That(photo.IsMissionPhoto, Is.False);
            Assert.That(Capture.CanSubmitPhoto(photo), Is.False);
            yield return WaitTransition();
            lab = OpenLab(); Cabin.Navigation.Ship.HitTerrain(10000);
            lab.sendButton.onClick.Invoke(); Assert.That(photo.IsMissionPhoto, Is.False);
            Assert.That(Capture.CanSubmitPhoto(photo), Is.False);
            yield return null;
            float end = Time.realtimeSinceStartup + 25;
            while ((flow.IsTransitioning || Loop.IsDeathInProgress) && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(Loop.IsDeathInProgress, Is.False);
            Assert.That(Loop.MissionRuntime.HasObjective(photo.MissionObjectiveId), Is.False);
            Assert.That(Capture.Photos[0].IsMissionPhoto, Is.False);
            Assert.That(Capture.CanSubmitPhoto(Capture.Photos[0]), Is.True);
        }

        [UnityTest] public IEnumerator BadPhotoIsStoredButCannotBeSubmitted()
        {
            Aim();Cabin.Navigation.RestoreVoyage(Cabin.Navigation.Position,Cabin.Navigation.Heading,0,0);
            var photo=Capture.Capture();Assert.That(photo,Is.Not.Null);
            Assert.That(photo.Result,Is.EqualTo(PhotoResultType.NoSubject.ToString()));
            Assert.That(photo.MissionObjectiveId,Is.Null.Or.Empty);Assert.That(Capture.SubmitPhoto(photo),Is.False);
            Assert.That(OpenLab().sendButton.interactable,Is.False);Assert.That(Loop.MissionRuntime.CompletedCount,Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator DuplicateCandidatesNeverGrantDuplicateRewards()
        {
            var first=Candidate();yield return new WaitForSecondsRealtime(.65f);var second=Candidate();
            Assert.That(Capture.SubmitPhoto(first),Is.True);
            string state=JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            Assert.That(Capture.SubmitPhoto(second),Is.False);Assert.That(second.IsMissionPhoto,Is.False);
            var lab=OpenLab();Assert.That(lab.sendButton.interactable,Is.False);
            Assert.That(lab.DisplayedText,Does.Contain("OBJECTIVE ALREADY COMPLETED"));
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()),Is.EqualTo(state));
        }
        [UnityTest] public IEnumerator SubmissionRevalidatesRepositoryZoneTargetObjectiveAndRuntime()
        {
            var photo=Candidate();var runtime=Loop.MissionRuntime;var original=runtime.config;
            var clone=Object.Instantiate(original);runtime.config=clone;
            try
            {
                var external=new PhotoRecord(photo.Id,photo.Image,photo.Image,photo.CapturedAt,photo.MapCoordinate,false,
                    photo.Depth,photo.Heading,photo.Result,photo.MissionZoneId,photo.MissionPoiId,photo.MissionObjectiveId,photo.MissionTargetId);
                Assert.That(Capture.SubmitPhoto(external),Is.False);
                clone.zoneId="Zone04";Assert.That(Capture.SubmitPhoto(photo),Is.False);
                var lab=OpenLab();Assert.That(lab.sendButton.interactable,Is.False);
                Assert.That(lab.DisplayedText,Does.Contain("UNAVAILABLE"));
                clone.zoneId=photo.MissionZoneId;
                var objective=clone.FindLocationByPoi(photo.MissionPoiId).objectives[0];
                string target=objective.targetId,id=objective.id;
                objective.targetId="wrong-target";Assert.That(Capture.SubmitPhoto(photo),Is.False);objective.targetId=target;
                objective.id="changed-id";Assert.That(Capture.SubmitPhoto(photo),Is.False);objective.id=id;
                runtime.config=null;Assert.That(Capture.SubmitPhoto(photo),Is.False);runtime.config=clone;
                Assert.That(Capture.CanSubmitPhoto(photo),Is.True);Assert.That(runtime.CompletedCount,Is.Zero);
            }
            finally{runtime.config=original;Object.Destroy(clone);}
            yield return null;
        }
        [UnityTest] public IEnumerator CandidateAndSubmittedStateSurviveContinueAndArchiveReload()
        {
            var photo=Candidate();string id=photo.Id,objective=photo.MissionObjectiveId;
            Assert.That(Loop.SaveCurrent(),Is.True);yield return Reload();
            photo=Capture.Photos[0];Assert.That(photo.Id,Is.EqualTo(id));Assert.That(photo.IsMissionPhoto,Is.False);
            Assert.That(Capture.CanSubmitPhoto(photo),Is.True);Assert.That(Loop.MissionRuntime.HasObjective(objective),Is.False);
            AssertArchive(false,objective);
            Assert.That(Capture.SubmitPhoto(photo),Is.True);yield return Reload();
            Assert.That(Capture.Photos[0].IsMissionPhoto,Is.True);Assert.That(Loop.MissionRuntime.HasObjective(objective),Is.True);
            Assert.That(Capture.CanSubmitPhoto(Capture.Photos[0]),Is.False);AssertArchive(true,objective);
        }
        private void AssertArchive(bool submitted,string objective)
        {
            var go=new GameObject("Archive metadata reload probe");
            try
            {
                var archive=go.AddComponent<PhotoCaptureService>();var photo=archive.Photos[0];
                Assert.That(photo.IsMissionPhoto,Is.EqualTo(submitted));Assert.That(photo.MissionObjectiveId,Is.EqualTo(objective));
                Assert.That(photo.MissionZoneId,Is.EqualTo("Zone01"));Assert.That(photo.MissionPoiId,Is.EqualTo("zone01-left"));
                Assert.That(photo.MissionTargetId,Is.EqualTo("Z1_Creature_01"));
            }
            finally{Object.Destroy(go);}
        }
        [UnityTest] public IEnumerator LegacyPhotoSaveAndArchiveWithoutOptionalFieldsRemainReadable()
        {
            var photo=Candidate();var saved=Capture.ExportPhotos();
            saved[0].missionZoneId=null;saved[0].missionPoiId=null;saved[0].missionObjectiveId=null;saved[0].missionTargetId=null;
            Capture.RestorePhotos(saved,1);Assert.That(Capture.Photos[0].IsMissionPhoto,Is.False);
            Assert.That(Capture.CanSubmitPhoto(Capture.Photos[0]),Is.False);
            File.WriteAllText(Path.Combine(Capture.ArchivePath,photo.Id+".json"),"{\"time\":\""+photo.CapturedAt.ToString("O")+"\",\"result\":\"GoodPhoto\"}");
            var go=new GameObject("Legacy archive probe");var archive=go.AddComponent<PhotoCaptureService>();
            Assert.That(archive.Photos.Count,Is.EqualTo(1));Assert.That(archive.Photos[0].MissionObjectiveId,Is.Null.Or.Empty);
            Assert.That(archive.Photos[0].IsMissionPhoto,Is.False);Object.Destroy(go);yield return null;
        }
        [UnityTest] public IEnumerator DeathRestoresUnsentThenSubmittedDayStartWithoutSpecialCases()
        {
            var photo=Candidate();string id=photo.Id,objective=photo.MissionObjectiveId;
            Assert.That(Loop.Rest(),Is.True);Assert.That(Capture.SubmitPhoto(photo),Is.True);
            yield return new WaitForSecondsRealtime(.65f);Candidate("zone01-north");
            yield return Die();
            Assert.That(Capture.Photos.Count,Is.EqualTo(1));photo=Capture.Photos[0];
            Assert.That(photo.Id,Is.EqualTo(id));Assert.That(photo.IsMissionPhoto,Is.False);
            Assert.That(Loop.MissionRuntime.HasObjective(objective),Is.False);Assert.That(Capture.CanSubmitPhoto(photo),Is.True);
            Assert.That(Capture.SubmitPhoto(photo),Is.True);Assert.That(Loop.Rest(),Is.True);
            Candidate("zone01-north");yield return Die();
            Assert.That(Capture.Photos.Count,Is.EqualTo(1));Assert.That(Capture.Photos[0].IsMissionPhoto,Is.True);
            Assert.That(Loop.MissionRuntime.HasObjective(objective),Is.True);
        }
        [UnityTest] public IEnumerator DepthCapableZoneTwoCaptureRemainsPhotoRequiredUntilSend()
        {
            var story=Cabin.GetComponent<ZoneOneStory>();
            var one=Candidate();Assert.That(Capture.SubmitPhoto(one),Is.True);
            yield return new WaitForSecondsRealtime(.65f);Assert.That(Capture.SubmitPhoto(Candidate("zone01-north")),Is.True);
            Assert.That(story.ResolveCapture("zone01-left",story.FindObjective("zone01-left",MissionObjectiveType.Capture)),Is.True);
            Assert.That(story.InstallBioLamp(),Is.True);Cabin.GetComponent<ExpeditionProgression>().Evaluate();
            // Zone01 deliberately retains the 500m cap. This Zone02 repository test needs a depth-capable vessel.
            var capableShip=Cabin.Navigation.Ship.Export();capableShip.maximumDepth=750;Cabin.Navigation.Ship.Restore(capableShip);
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.exitArea.mapPosition,0,230,0);
            flow.LoadZone("Zone02");yield return WaitTransition();Assert.That(Loop.Zone,Is.EqualTo("Zone02"));
            var photo=Candidate("zone02-l3");var survey=Capture.survey;var poi=survey.FindPoi(photo.MissionPoiId);
            Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi),0,survey.DepthFor(poi),0);
            var catcher=Cabin.GetComponent<CreatureCatcher>();
            Assert.That(catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.PhotoRequired));
            var lab=OpenLab();Assert.That(lab.sendButton.interactable,Is.True);lab.sendButton.onClick.Invoke();Cabin.ClosePanel();
            Assert.That(catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.Started));
            CaptureMinigamePlayModeTests.Win(catcher.minigame);
            Assert.That(catcher.LastResult,Is.EqualTo(CreatureCatcher.Result.Caught));
        }
    }
}

using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
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
            yield return WaitTransition();flow.StartNewGame();yield return WaitTransition();
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
        [UnityTest] public IEnumerator RealZoneTwoCaptureRemainsPhotoRequiredUntilSend()
        {
            var story=Cabin.GetComponent<ZoneOneStory>();
            var one=Candidate();Assert.That(Capture.SubmitPhoto(one),Is.True);
            yield return new WaitForSecondsRealtime(.65f);Assert.That(Capture.SubmitPhoto(Candidate("zone01-north")),Is.True);
            Assert.That(story.RecordObjective("zone01-east",MissionObjectiveType.Collect,ZoneOneStory.EmmaBlueprint),Is.True);
            Assert.That(story.InstallHull(),Is.True);Cabin.GetComponent<ExpeditionProgression>().Evaluate();
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

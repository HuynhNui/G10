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
using Object=UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ExpeditionLoopPlayModeTests
    {
        private string folder;
        private ExpeditionLoop loop;
        private CabinStationView cabin;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"ExpeditionTests-"+Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"timeline.json");
            PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            yield return Load(5);
        }
        private IEnumerator Load(int days)
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore",LoadSceneMode.Single);
            loop=Object.FindAnyObjectByType<ExpeditionLoop>();
            loop.zones[0].baseDays=days;
            yield return SceneManager.LoadSceneAsync("Zone01",LoadSceneMode.Additive);
            yield return null;yield return null;
            cabin=Object.FindAnyObjectByType<CabinStationView>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(SceneFlowController.Instance!=null)Object.Destroy(SceneFlowController.Instance.gameObject);
            ExpeditionSaveStore.PathOverride=null;PhotoCaptureService.ArchivePathOverride=null;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        [UnityTest] public IEnumerator RestConfirmationRollbackAndReloadPreserveActualGameplay()
        {
            Assert.That(loop.Day,Is.EqualTo(1));Assert.That(loop.CanRest,Is.True);
            cabin.OpenComputer();
            var screen=cabin.GetComponentInChildren<ComputerScreenController>(true);
            var ui=screen.GetComponent<ExpeditionComputerView>();
            Assert.That(screen.CurrentApp,Is.EqualTo(ComputerAppId.Desktop));
            screen.OpenShipStatus();
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"expedition-status.png");
            var status=screen.GetComponentInChildren<ShipStatusView>();
            Assert.That(status.DisplayedText,Does.Contain("DAY 01"));
            Assert.That(status.DisplayedText,Does.Contain("CAPTURE ARRAY"));
            AssertTextFits(status.transform.Find("Body").GetComponent<UnityEngine.UI.Text>());
            Vector2 dock=cabin.Navigation.Position;
            cabin.Navigation.RestoreVoyage(dock+Vector2.right*100,90,230,100);
            ui.OpenRest();ui.RequestRest();ui.ConfirmRest();
            Assert.That(loop.Day,Is.EqualTo(1));Assert.That(loop.CanRest,Is.False);
            cabin.Navigation.RestoreVoyage(dock,90,230,200);
            ui.OpenRest();yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"expedition-rest.png");
            var catcher=cabin.GetComponent<CreatureCatcher>();var survey=catcher.survey;
            Assert.That(catcher.inventory.TryAdd(survey.creatureId,catcher.itemName,catcher.itemIcon),Is.True);
            survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);survey.creaturePresent=false;
            var photos=cabin.GetComponent<PhotoCaptureService>();
            Assert.That(photos.Capture(),Is.Not.Null);
            string originalPhoto=photos.Photos[0].Id;
            ui.RequestRest();Assert.That(loop.Day,Is.EqualTo(1));ui.ConfirmRest();
            Assert.That(loop.Day,Is.EqualTo(2));Assert.That(loop.Journal.Count,Is.EqualTo(1));
            Assert.That(loop.Journal[0].photos,Is.EqualTo(1));Assert.That(loop.Journal[0].captures,Is.EqualTo(1));
            Assert.That(loop.Journal[0].distance,Is.EqualTo(200));
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"expedition-journal.png");
            Assert.That(catcher.inventory.Items.Count,Is.EqualTo(1));Assert.That(survey.IsTaskComplete(PhotoSurveyZone.TaskKind.Photograph),Is.True);
            Assert.That(loop.Rest(),Is.True);Assert.That(loop.Day,Is.EqualTo(3));
            yield return new WaitForSecondsRealtime(.65f);
            photos.Capture();survey.CompleteTask(PhotoSurveyZone.TaskKind.Capture);
            cabin.Navigation.RestoreVoyage(dock+Vector2.up*20,180,100,300);
            ui.OpenJournal(); // Latest day must confirm before changing state.
            ui.RequestRestore();Assert.That(ui.JournalText,Does.Contain("Restoring this day will erase all progress made afterward."));
            ui.CancelConfirmation();Assert.That(loop.Day,Is.EqualTo(3));
            Assert.That(loop.RestoreDay(1),Is.True);
            Assert.That(loop.Day,Is.EqualTo(1));Assert.That(loop.Journal.Count,Is.EqualTo(1));
            Assert.That(cabin.Navigation.Position,Is.EqualTo(dock));Assert.That(cabin.Navigation.Heading,Is.EqualTo(90));
            Assert.That(cabin.Navigation.Depth,Is.EqualTo(230));Assert.That(survey.IsTaskComplete(PhotoSurveyZone.TaskKind.Capture),Is.False);
            Assert.That(photos.Photos.Count,Is.EqualTo(1));Assert.That(photos.Photos[0].Id,Is.EqualTo(originalPhoto));
            yield return Load(5);
            Assert.That(loop.Day,Is.EqualTo(1));Assert.That(loop.Journal.Count,Is.EqualTo(1));
            Assert.That(cabin.GetComponent<CreatureInventory>().Items.Count,Is.EqualTo(1));
            Assert.That(cabin.GetComponent<PhotoCaptureService>().Photos[0].Id,Is.EqualTo(originalPhoto));
            Assert.That(cabin.GetComponent<PhotoCaptureService>().Photos.Count,Is.EqualTo(1),"Future archive files must not reappear.");
            Assert.That(loop.Rest(),Is.True);Assert.That(loop.Journal.Count,Is.EqualTo(1),"Resting a restored day replaces its checkpoint.");
        }
        [UnityTest] public IEnumerator DeadlineFailureLocksGameplayAndJournalRestorationReleasesIt()
        {
            yield return Load(2);
            Assert.That(loop.Deadline,Is.EqualTo(2));
            Assert.That(loop.Rest(),Is.True);Assert.That(loop.Failed,Is.False);
            Assert.That(loop.Rest(),Is.True);Assert.That(loop.Failed,Is.True);
            var screen=cabin.GetComponentInChildren<ComputerScreenController>(true);
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(screen.gameObject));
            Assert.That(screen.CurrentApp,Is.EqualTo(ComputerAppId.Journal));
            Vector2 before=cabin.Navigation.Position;float depth=cabin.Navigation.Depth;
            cabin.Navigation.Step(1,1,2);cabin.Navigation.StepDepth(1,2);
            Assert.That(cabin.Navigation.Position,Is.EqualTo(before));Assert.That(cabin.Navigation.Depth,Is.EqualTo(depth));
            Assert.That(cabin.GetComponent<PhotoCaptureService>().Capture(),Is.Null);
            Assert.That(cabin.GetComponent<CreatureCatcher>().TryCapture(),Is.EqualTo(CreatureCatcher.Result.Unavailable));
            cabin.OpenNavigation();cabin.ClosePanel();
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(screen.gameObject));Assert.That(loop.Rest(),Is.False);
            yield return Load(2);
            Assert.That(loop.Failed,Is.True,"Failure must survive restarting the game.");
            screen=cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.GetComponent<ExpeditionComputerView>().RequestRestore();
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"expedition-failure.png");
            AssertTextFits(screen.transform.Find("JournalApp/JournalDetails").GetComponent<UnityEngine.UI.Text>());
            Assert.That(loop.RestoreDay(1),Is.True);Assert.That(loop.Failed,Is.False);
            Assert.That(cabin.Navigation.ExpeditionBlocked,Is.False);Assert.That(loop.Journal.Count,Is.EqualTo(1));
            cabin.ClosePanel();Assert.That(cabin.Panels.IsPanelOpen,Is.False);
        }
        [UnityTest] public IEnumerator CompletedObjectivesCarryDaysOnceWithConfiguredCap()
        {
            Assert.That(loop.PrepareZone("Zone02"),Is.False);
            var survey=cabin.GetComponent<PhotoSurveyZone>();
            foreach(var task in survey.tasks)survey.CompleteTask(task);
            loop.EvaluateDeadline();
            Assert.That(loop.RequiredObjectivesComplete,Is.True);
            Assert.That(loop.Rest(),Is.True); // Completion was on day 1; waiting does not erase earned days.
            loop.maxCarryOverDays=2;
            Assert.That(loop.PrepareZone("Zone02"),Is.True);
            Assert.That(loop.Zone,Is.EqualTo("Zone02"));Assert.That(loop.Deadline,Is.EqualTo(8));
            Assert.That(loop.PrepareZone("Zone02"),Is.True);Assert.That(loop.Deadline,Is.EqualTo(8));
            Assert.That(ExpeditionSaveStore.TryRead(out var saved,out _),Is.True);
            Assert.That(saved.current.zone,Is.EqualTo("Zone02"));Assert.That(saved.current.zones[0].tasks.Length,Is.EqualTo(2));
            yield return null;
        }
        [Test] public void AtomicSaveRecoversBackupAndRejectsCorruptionWithoutOverwrite()
        {
            ExpeditionSaveStore.Write(new ExpeditionSave());
            var second=new ExpeditionSave();second.current.day=2;ExpeditionSaveStore.Write(second);
            File.WriteAllText(ExpeditionSaveStore.SavePath,"broken json");
            Assert.That(ExpeditionSaveStore.TryRead(out var recovered,out _),Is.True);Assert.That(recovered.current.day,Is.EqualTo(1));
            File.WriteAllText(ExpeditionSaveStore.SavePath+".bak","broken backup");
            Assert.That(ExpeditionSaveStore.TryRead(out _,out _),Is.False);
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath),Is.EqualTo("broken json"));
        }
        private static void AssertTextFits(UnityEngine.UI.Text label)
        { Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height),label.name+" must not clip information."); }
        [UnityTest] public IEnumerator ZoneTravelAndCrossZoneRollbackKeepTheSingleTimeline()
        {
            if(SceneFlowController.Instance==null)new GameObject("TestSceneFlow").AddComponent<SceneFlowController>();
            var catcher=cabin.GetComponent<CreatureCatcher>();
            catcher.inventory.TryAdd(catcher.survey.creatureId,catcher.itemName,catcher.itemIcon);
            foreach(var task in catcher.survey.tasks)catcher.survey.CompleteTask(task);
            Assert.That(loop.Rest(),Is.True);
            SceneFlowController.Instance.LoadZone("Zone02");
            while(SceneFlowController.Instance.IsTransitioning)yield return null;
            Assert.That(SceneManager.GetSceneByName("Zone02").isLoaded,Is.True);
            Assert.That(loop.Zone,Is.EqualTo("Zone02"));Assert.That(loop.Deadline,Is.EqualTo(9));
            Assert.That(loop.RestoreDay(1),Is.True);
            while(SceneFlowController.Instance.IsTransitioning)yield return null;
            yield return null;yield return null;
            cabin=Object.FindAnyObjectByType<CabinStationView>();
            Assert.That(loop.Zone,Is.EqualTo("Zone01"));Assert.That(loop.Day,Is.EqualTo(1));
            Assert.That(loop.Deadline,Is.EqualTo(5));Assert.That(loop.RequiredObjectivesComplete,Is.True);
            Assert.That(cabin.GetComponent<CreatureInventory>().Items.Count,Is.EqualTo(1));
            Assert.That(loop.Journal.Count,Is.EqualTo(1));Assert.That(loop.CanRest,Is.True);
        }
    }
}

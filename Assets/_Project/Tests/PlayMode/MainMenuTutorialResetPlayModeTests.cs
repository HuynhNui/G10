using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
using G10.Prototype.Navigation;
using G10.Prototype.Tutorial;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class MainMenuTutorialResetPlayModeTests
    {
        private string folder,oldSave,oldPhotos,prefs;
        private SceneFlowController flow;
        private MainMenuTutorialReset View=>Object.FindAnyObjectByType<MainMenuTutorialReset>();
        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"MenuTutorialQA-"+Guid.NewGuid().ToString("N"));
            prefs="MenuTutorialQA."+Guid.NewGuid().ToString("N");
            oldSave=ExpeditionSaveStore.PathOverride;oldPhotos=PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"timeline.json");
            PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            TutorialTestSave.SeedReturningPlayer();
            if(SceneFlowController.Instance!=null){Object.Destroy(SceneFlowController.Instance.gameObject);yield return null;}
            yield return SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single);yield return null;
            flow=SceneFlowController.Instance;yield return Transition();yield return null;
            Assert.That(View,Is.Not.Null);Assert.That(View.ResetButton.gameObject.activeInHierarchy,Is.True);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(flow!=null)Object.Destroy(flow.gameObject);yield return null;
            ExpeditionSaveStore.PathOverride=oldSave;PhotoCaptureService.ArchivePathOverride=oldPhotos;
            foreach(string suffix in new[]{"audio","display","custom"})PlayerPrefs.DeleteKey(prefs+suffix);
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        private IEnumerator Transition()
        {
            yield return null;float end=Time.realtimeSinceStartup+25;
            while(flow.IsTransitioning&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(flow.IsTransitioning,Is.False);Assert.That(flow.LastError,Is.Null.Or.Empty);
        }
        private static ExpeditionSave Read()
        {Assert.That(ExpeditionSaveStore.TryRead(out var save,out var error),Is.True,error);return save;}
        private static string WithoutTutorial(ExpeditionSave save)
        {var copy=ExpeditionSaveStore.Copy(save);copy.tutorial=null;return JsonUtility.ToJson(copy);}
        private void ResetViaButtons()
        {
            View.ResetButton.onClick.Invoke();Assert.That(View.IsConfirmationOpen,Is.True);
            View.ConfirmButton.onClick.Invoke();Assert.That(View.IsConfirmationOpen,Is.False);
            Assert.That(View.Feedback,Is.EqualTo(MainMenuTutorialReset.SuccessText));
        }
        [TestCase(false,false,false)][TestCase(false,true,true)]
        [TestCase(true,false,true)][TestCase(true,true,true)]
        public void VisibilityPolicyExcludesRelease(bool editor,bool development,bool expected)
            => Assert.That(MainMenuTutorialReset.VisibleForConfiguration(editor,development),Is.EqualTo(expected));

        [UnityTest] public IEnumerator ConfirmClearsOnlyKnowledgeAndPreservesFullTimelineAndPreferences()
        {
            var saved=Read();saved.current.day=2;saved.current.endingReached="HiddenEnding";
            var probe=new GameObject("Ship defaults");
            saved.current.ship=probe.AddComponent<ZoneNavigation>().CreateInitialShipState();Object.Destroy(probe);
            saved.current.hasShipState=true;
            saved.current.zones.Add(new ExpeditionZoneState{zone="Zone01",deadline=5,hasVoyage=true,
                mapCoordinateVersion=3,position=new Vector2(120,400),heading=110,depth=240,distance=100});
            saved.current.zones[0].missionProgress.completedObjectives.Add("Z1_L1_PHOTO");
            saved.current.zones[0].progress.hiddenRouteUnlocked=true;
            saved.current.inventory.Add(new SavedCreature{id="Z1_ITEM_EMMA_BLUEPRINT",name="Emma"});
            var texture=new Texture2D(2,2);
            saved.current.photos.Add(new SavedPhoto{id="preserve-photo",time=DateTimeOffset.Now.ToString("O"),
                png=Convert.ToBase64String(texture.EncodeToPNG())});Object.Destroy(texture);
            saved.current.photosTaken=1;saved.dayStart=ExpeditionSaveStore.Copy(saved.current);saved.hasDayStart=true;
            saved.journal.Add(new ExpeditionJournalEntry{day=2,zone="Zone01",checkpoint=ExpeditionSaveStore.Copy(saved.current)});
            saved.tutorial.completedSteps.Add("Radar");saved.tutorial.presentedSteps.Add("Camera");
            ExpeditionSaveStore.Write(saved);string before=WithoutTutorial(Read());
            PlayerPrefs.SetFloat(prefs+"audio",.37f);PlayerPrefs.SetInt(prefs+"display",2);PlayerPrefs.SetString(prefs+"custom","keep");
            ResetViaButtons();var reset=Read();
            Assert.That(reset.tutorial.completed,Is.False);Assert.That(reset.tutorial.completedSteps,Is.Empty);
            Assert.That(reset.tutorial.presentedSteps,Is.Empty);Assert.That(reset.tutorial.waitForNewGame,Is.True);
            Assert.That(WithoutTutorial(reset),Is.EqualTo(before));
            Assert.That(PlayerPrefs.GetFloat(prefs+"audio"),Is.EqualTo(.37f));
            Assert.That(PlayerPrefs.GetInt(prefs+"display"),Is.EqualTo(2));Assert.That(PlayerPrefs.GetString(prefs+"custom"),Is.EqualTo("keep"));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));Assert.That(flow.IsTransitioning,Is.False);
            Assert.That(Object.FindAnyObjectByType<ExpeditionLoop>(),Is.Null);
            // Duplicate confirmation without reopening cannot reset/write again.
            string persisted=File.ReadAllText(ExpeditionSaveStore.SavePath);View.ConfirmReset();
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath),Is.EqualTo(persisted));
        }
        [UnityTest] public IEnumerator CancelAndRepeatedSetupDoNotMutateSaveOrDuplicateUi()
        {
            var start=GameObject.Find("StartButton").GetComponent<Button>();var original=View;
            Assert.That(MainMenuTutorialReset.Ensure(start),Is.SameAs(original));
            Assert.That(MainMenuTutorialReset.Ensure(start),Is.SameAs(original));
            Assert.That(Object.FindObjectsByType<MainMenuTutorialReset>(FindObjectsInactive.Include).Length,Is.EqualTo(1));
            string before=File.ReadAllText(ExpeditionSaveStore.SavePath);
            View.ResetButton.onClick.Invoke();Assert.That(start.interactable,Is.False);
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath),Is.EqualTo(before));
            View.CancelButton.onClick.Invoke();Assert.That(View.IsConfirmationOpen,Is.False);
            Assert.That(start.interactable,Is.True);Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath),Is.EqualTo(before));
            View.ResetButton.onClick.Invoke();View.enabled=false;
            Assert.That(View.IsConfirmationOpen,Is.False);Assert.That(start.interactable,Is.True);
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath),Is.EqualTo(before));
            yield return null;
        }
        [UnityTest] public IEnumerator ContinueAllProgressedZonesStaysNormalThenNewGameStartsIntro()
        {
            foreach(string zone in new[]{"Zone01","Zone02","Zone03","Zone04"})
            {
                var saved=new ExpeditionSave();saved.current.zone=zone;saved.current.day=2;saved.tutorial.completed=true;
                ExpeditionSaveStore.Write(saved,discardFuture:true);ResetViaButtons();
                GameObject.Find("ContinueButton").GetComponent<Button>().onClick.Invoke();yield return Transition();
                yield return new WaitForSecondsRealtime(.5f);
                var loop=Object.FindAnyObjectByType<ExpeditionLoop>();var manager=Object.FindAnyObjectByType<TutorialManager>();
                Assert.That(loop.Zone,Is.EqualTo(zone));Assert.That(loop.Day,Is.EqualTo(2));
                Assert.That(manager.IsRunning,Is.False);Assert.That(Object.FindAnyObjectByType<DialogueController>().IsActive,Is.False);
                foreach(TutorialStation station in Enum.GetValues(typeof(TutorialStation)))Assert.That(manager.Allows(station),Is.True);
                foreach(ComputerAppId app in Enum.GetValues(typeof(ComputerAppId)))Assert.That(manager.AllowsComputerApp(app),Is.True);
                flow.LoadMainMenu();yield return Transition();
            }
            GameObject.Find("StartButton").GetComponent<Button>().onClick.Invoke();yield return Transition();
            var cabin=Object.FindAnyObjectByType<CabinStationView>();var tutorial=cabin.GetComponent<TutorialManager>();
            float end=Time.realtimeSinceStartup+5;
            while(!cabin.GetComponent<ZoneOneTutorialDirector>().IsPresenting&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(tutorial.CurrentStep,Is.EqualTo(TutorialStepId.Intro));Assert.That(tutorial.IsRunning,Is.True);
            Assert.That(cabin.GetComponent<ZoneOneTutorialDirector>().IsPresenting,Is.True);
            Assert.That(cabin.Navigation.Position,Is.EqualTo(new Vector2(100,400)));
            Assert.That(cabin.Navigation.Heading,Is.EqualTo(90));Assert.That(cabin.Navigation.Depth,Is.EqualTo(230));
        }
    }
}

using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.Tutorial;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class TutorialPlayModeTests
    {
        private string folder,oldSave,oldPhotos;
        private SceneFlowController flow;
        private CabinStationView Cabin=>Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionLoop Loop=>Object.FindAnyObjectByType<ExpeditionLoop>();
        private TutorialManager Manager=>Cabin.GetComponent<TutorialManager>();
        private ZoneOneTutorialDirector Director=>Cabin.GetComponent<ZoneOneTutorialDirector>();
        private DialogueController Dialogue=>Object.FindAnyObjectByType<DialogueController>();
        private PhotoCaptureService Photos=>Cabin.GetComponent<PhotoCaptureService>();
        private ComputerScreenController Computer=>Cabin.GetComponentInChildren<ComputerScreenController>(true);
        private static WaitForSecondsRealtime Poll=>new(.4f);
        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"TutorialPlay-"+Guid.NewGuid().ToString("N"));
            oldSave=ExpeditionSaveStore.PathOverride;oldPhotos=PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"timeline.json");
            PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            if(SceneFlowController.Instance!=null){Object.Destroy(SceneFlowController.Instance.gameObject);yield return null;}
            yield return SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single);
            yield return null;flow=SceneFlowController.Instance;yield return Transition();
            flow.StartNewGame();yield return Transition();
            Assert.That(Loop.IsInitialized,Is.True);Assert.That(Manager.IsRunning,Is.True);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(flow!=null)Object.Destroy(flow.gameObject);yield return null;
            ExpeditionSaveStore.PathOverride=oldSave;PhotoCaptureService.ArchivePathOverride=oldPhotos;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        private IEnumerator Transition()
        {
            yield return null;float end=Time.realtimeSinceStartup+25;
            while(flow.IsTransitioning&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(flow.IsTransitioning,Is.False);Assert.That(flow.LastError,Is.Null.Or.Empty);
        }
        private IEnumerator Presentation(TutorialStepId step)
        {
            float end=Time.realtimeSinceStartup+8;
            while((!Director.IsPresenting||Manager.CurrentStep!=step)&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Manager.CurrentStep,Is.EqualTo(step));Assert.That(Director.IsPresenting,Is.True,step.ToString());
        }
        private IEnumerator Skip(TutorialStepId step)
        {
            yield return Presentation(step);Dialogue.Skip();yield return Poll;
            if(step!=TutorialStepId.Intro)Assert.That(Manager.HasLearned(step),Is.False,"Text skip cannot teach "+step);
        }
        private IEnumerator LearnRadar()
        {
            yield return Skip(TutorialStepId.Intro);yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation();yield return Poll;Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Helm));
            Cabin.Navigation.Step(1,0,1.5f);yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.False);
            Cabin.Navigation.Step(0,1,.5f);yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.False);
            Cabin.Navigation.StepDepth(1,1.1f);yield return Poll;
            yield return Skip(TutorialStepId.Map);
            Assert.That(Cabin.NavigationPanel.transform.Find("MiniMap"), Is.Null);
            Assert.That(Cabin.NavigationPanel.transform.Find("OpenMapHotspot"), Is.Null);
            Cabin.OpenNavigation(); yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Map), Is.False, "The helm must not satisfy map practice.");
            Cabin.NavigationPanel.transform.Find("WatercolorHUD/Map").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Poll;yield return Skip(TutorialStepId.Radar);
            Cabin.OpenRadar();yield return Poll;Assert.That(Manager.HasLearned(TutorialStepId.Radar),Is.False);
            int charges=Cabin.Navigation.Ship.Radar;Cabin.Scan();yield return Poll;
            Assert.That(Cabin.Navigation.Ship.Radar,Is.EqualTo(charges-1));
            Assert.That(Manager.HasLearned(TutorialStepId.Radar),Is.True);
            Assert.That(Manager.Allows(TutorialStation.Helm),Is.True);Assert.That(Manager.Allows(TutorialStation.Map),Is.True);
        }
        private void Aim(string id)
        {
            var survey=Photos.survey;var poi=survey.FindPoi(id);Assert.That(poi,Is.Not.Null);
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI/8;
                var position=survey.ContactPosition(poi)-new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*20;
                if(!Cabin.Navigation.CanOccupy(position))continue;
                Cabin.Navigation.RestoreVoyage(position,i*22.5f,survey.DepthFor(poi),0);
                if(survey.TryGetPhotoContact(Cabin.Navigation,Photos.profile.visibleDistance,Photos.profile.fieldOfView,out var found,out _)&&found==poi)return;
            }
            Assert.Fail("No reachable photo angle: "+id);
        }
        private IEnumerator PhysicalPhoto()
        {
            // Respect the production camera cooldown between independent shots.
            yield return new WaitForSecondsRealtime(.65f);
            int before=Photos.Photos.Count;Cabin.OpenCamera();
            float end=Time.realtimeSinceStartup+5;
            while((Cabin.IsDirectInteractionActive||Photos.Photos.Count==before)&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Photos.Photos.Count,Is.EqualTo(before+1));yield return Poll;
        }
        private PhotoLabView OpenLab()
        {Cabin.OpenComputer();Computer.OpenPhotoLab();return Computer.GetComponentInChildren<PhotoLabView>(true);}

        [UnityTest] public IEnumerator FreshSpawnGatesSkipCancelAndDisableAreSafe()
        {
            Assert.That(Cabin.Navigation.Position,Is.EqualTo(new Vector2(100,400)));
            Assert.That(Cabin.Navigation.Heading,Is.EqualTo(90));Assert.That(Cabin.Navigation.Depth,Is.EqualTo(230));
            Assert.That(Cabin.Navigation.CanOccupy(new Vector2(100,400)),Is.True);
            yield return Presentation(TutorialStepId.Intro);Dialogue.Cancel();
            Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Intro));Assert.That(Manager.WasPresented(TutorialStepId.Intro),Is.False);
            var previous=Cabin.Panels.CurrentPanel;
            UnityAction[] calls={Cabin.OpenNavigation,Cabin.OpenMap,Cabin.OpenRadar,Cabin.OpenComputer,Cabin.OpenCargo,Cabin.OpenCamera,Cabin.OpenCapture};
            foreach(var action in calls){var evt=new UnityEvent();evt.AddListener(action);evt.Invoke();Assert.That(Cabin.Panels.CurrentPanel,Is.SameAs(previous));}
            Assert.That(Cabin.IsDirectInteractionActive,Is.False);
            yield return Skip(TutorialStepId.Intro);yield return Presentation(TutorialStepId.Helm);
            Dialogue.Cancel();Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.False);
            Assert.That(Manager.WasPresented(TutorialStepId.Helm),Is.False);
            yield return Skip(TutorialStepId.Helm);
            Assert.That(Manager.Allows(TutorialStation.Helm),Is.True);Assert.That(Manager.Allows(TutorialStation.Radar),Is.False);
            Director.enabled=false;Assert.That(Manager.Allows(TutorialStation.Camera),Is.True);Assert.That(Dialogue.IsActive,Is.False);
            Director.enabled=true;yield return Poll;Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Helm));
            Manager.enabled=false;Assert.That(Manager.Allows(TutorialStation.Computer),Is.True);
            Manager.enabled=true;yield return Poll;Assert.That(Director.IsPractical,Is.True);
        }
        [UnityTest] public IEnumerator FullTutorialUsesPhotosSendCollectionAndRealHullUpgrade()
        {
            yield return LearnRadar();yield return Skip(TutorialStepId.Camera);
            Aim("zone01-left");Cabin.Navigation.RestoreVoyage(Cabin.Navigation.Position,Cabin.Navigation.Heading,0,0);
            yield return PhysicalPhoto();Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Camera));
            Aim("zone01-left");yield return PhysicalPhoto();
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.PhotoOneObjective),Is.False);
            yield return Skip(TutorialStepId.PhotoLab);
            var lab=OpenLab();yield return Poll;Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.PhotoLab));
            Computer.OpenApp(ComputerAppId.Upgrade);Assert.That(Computer.CurrentApp,Is.EqualTo(ComputerAppId.PhotoLab));
            Assert.That(lab.sendButton.interactable,Is.True);lab.sendButton.onClick.Invoke();yield return Poll;
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.PhotoOneObjective),Is.True);
            yield return Skip(TutorialStepId.Capture);Cabin.ClosePanel();
            Cabin.Navigation.RestoreVoyage(new Vector2(100,400),90,230,0);Cabin.OpenCapture();
            yield return new WaitForSecondsRealtime(1.5f);Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Capture));
            var poi=Photos.survey.FindPoi("zone01-east");
            Cabin.Navigation.RestoreVoyage(Photos.survey.ContactPosition(poi),0,Photos.survey.DepthFor(poi),0);
            Cabin.OpenCapture();yield return new WaitForSecondsRealtime(1.5f);
            var catcher=Cabin.GetComponent<CreatureCatcher>();CaptureMinigamePlayModeTests.Win(catcher.minigame);
            yield return Poll;Assert.That(Manager.HasLearned(TutorialStepId.Capture),Is.True);
            Assert.That(Manager.UpgradeReady,Is.False);Assert.That(Director.IsPresenting,Is.False);
            Aim("zone01-north");yield return PhysicalPhoto();
            Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Upgrade));Assert.That(Director.IsPresenting,Is.False);
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.PhotoTwoObjective),Is.False);
            lab=OpenLab();lab.sendButton.onClick.Invoke();yield return Poll;yield return Skip(TutorialStepId.Upgrade);
            Computer.OpenApp(ComputerAppId.Upgrade);yield return Poll;Assert.That(Manager.Completed,Is.False);
            var action=Computer.GetComponentInChildren<StoryHullUpgradeAction>(true);
            Assert.That(action.TryApply(),Is.True);Assert.That(Manager.Completed,Is.True,"Completion must persist synchronously with the real gate.");
            Assert.That(ExpeditionSaveStore.TryRead(out var saved,out _),Is.True);Assert.That(saved.tutorial.completed,Is.True);
            string before=File.ReadAllText(ExpeditionSaveStore.SavePath);
            Assert.That(Manager.MarkLearned(TutorialStepId.Upgrade),Is.False);
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath),Is.EqualTo(before));
            foreach(TutorialStation station in Enum.GetValues(typeof(TutorialStation)))Assert.That(Manager.Allows(station),Is.True);
            yield return Presentation(TutorialStepId.Complete);Dialogue.Skip();
            Cabin.GetComponent<ExpeditionProgression>().Evaluate();Cabin.ClosePanel();
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.exitArea.mapPosition,0,230,0);
            flow.LoadZone("Zone02");yield return Transition();Assert.That(Loop.Zone,Is.EqualTo("Zone02"));
            Assert.That(Manager.IsRunning,Is.False);Assert.That(Director.IsPresenting,Is.False);
            foreach(ComputerAppId app in Enum.GetValues(typeof(ComputerAppId)))Assert.That(Manager.AllowsComputerApp(app),Is.True);
        }
        [UnityTest] public IEnumerator LearnedRadarSurvivesDeathReloadAndSavedVoyage()
        {
            yield return LearnRadar();yield return Skip(TutorialStepId.Camera);
            Cabin.ClosePanel();Cabin.Navigation.Ship.HitTerrain(10000);yield return null;
            float end=Time.realtimeSinceStartup+20;
            while(Loop.IsDeathInProgress&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Loop.IsDeathInProgress,Is.False);Assert.That(Manager.HasLearned(TutorialStepId.Radar),Is.True);
            Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Camera));
            var position=new Vector2(120,400);Cabin.Navigation.RestoreVoyage(position,110,235,25);
            Assert.That(Loop.SaveCurrent(),Is.True);flow.LoadMainMenu();yield return Transition();
            flow.ContinueGame();yield return Transition();yield return Poll;
            Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Camera));Assert.That(Director.IsPresenting,Is.False);
            Assert.That(Cabin.Navigation.Position,Is.EqualTo(position));Assert.That(Cabin.Navigation.Heading,Is.EqualTo(110));
        }
        [UnityTest] public IEnumerator CompletedNewGameStaysUnrestrictedAndDebugResetReenablesIntro()
        {
            Director.enabled=false;Assert.That(Loop.SaveTutorialProgress(new TutorialProgressState{completed=true}),Is.True);
            flow.StartNewGame();yield return Transition();yield return Poll;
            Assert.That(Manager.Completed,Is.True);Assert.That(Dialogue.IsActive,Is.False);
            Assert.That(Cabin.Navigation.Position,Is.EqualTo(new Vector2(100,400)));
            Director.enabled=true;Assert.That(Loop.ResetTutorialProgress(),Is.True);
            yield return Presentation(TutorialStepId.Intro);Assert.That(Manager.Completed,Is.False);
        }
        [UnityTest] public IEnumerator EmptyResourcesAllowRestWithoutBypassingUnlearnedMechanics()
        {
            yield return Skip(TutorialStepId.Intro);yield return Skip(TutorialStepId.Helm);
            while(Cabin.Navigation.Ship.TryUse(ShipCharge.Photo)){}
            Assert.That(Manager.Allows(TutorialStation.Computer),Is.True);
            Assert.That(Manager.AllowsComputerApp(ComputerAppId.Rest),Is.True);
            Assert.That(Manager.AllowsComputerApp(ComputerAppId.PhotoLab),Is.False);
            Cabin.OpenComputer();Computer.OpenApp(ComputerAppId.Rest);
            Assert.That(Computer.CurrentApp,Is.EqualTo(ComputerAppId.Rest));
            Assert.That(Loop.Rest(),Is.True);Assert.That(Cabin.Navigation.Ship.Photos,Is.GreaterThan(0));
            Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.False);
        }
        [UnityTest] public IEnumerator LaterLogicalZonesNeverRestrictEvenWithIncompleteTutorialKnowledge()
        {
            Director.enabled=false;
            foreach(string zone in new[]{"Zone02","Zone03","Zone04"})
            {
                flow.LoadMainMenu();yield return Transition();
                var saved=new ExpeditionSave();saved.current.zone=zone;
                ExpeditionSaveStore.Write(saved,discardFuture:true);
                flow.ContinueGame();yield return Transition();yield return Poll;
                Assert.That(Loop.Zone,Is.EqualTo(zone));Assert.That(Manager.Completed,Is.False);
                Assert.That(Manager.IsRunning,Is.False);Assert.That(Dialogue.IsActive,Is.False);
                foreach(TutorialStation station in Enum.GetValues(typeof(TutorialStation)))Assert.That(Manager.Allows(station),Is.True);
                foreach(ComputerAppId app in Enum.GetValues(typeof(ComputerAppId)))Assert.That(Manager.AllowsComputerApp(app),Is.True);
            }
        }
    }
}

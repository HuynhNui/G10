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
        [UnityTest] public IEnumerator UnlimitedCapturesDoNotTriggerSupplyRecoveryShortcut()
        {
            Assert.That(Manager.CurrentStep, Is.EqualTo(TutorialStepId.Intro));
            Assert.That(Manager.Allows(TutorialStation.Computer), Is.False);
            var ship = Cabin.Navigation.Ship; var state = ship.Export(); state.captures = 0; ship.Restore(state);
            Assert.That(ship.UnlimitedCaptureAttempts, Is.True); Assert.That(ship.LowResources, Is.False);
            Assert.That(Manager.Allows(TutorialStation.Computer), Is.False);
            ship.UnlimitedCaptureAttempts = false;
            Assert.That(Manager.Allows(TutorialStation.Computer), Is.True, "Limited-mode supply recovery remains supported.");
            ship.UnlimitedCaptureAttempts = true;
            yield return null;
        }
        private IEnumerator Presentation(TutorialStepId step)
        {
            float end=Time.realtimeSinceStartup+8;
            while((Manager.CurrentStep!=step || (step==TutorialStepId.Intro ? !Director.IsPresenting : !Director.IsPractical))&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(Manager.CurrentStep,Is.EqualTo(step));
            if(step==TutorialStepId.Intro)Assert.That(Director.IsPresenting,Is.True);
            else { Assert.That(Director.IsPractical,Is.True); Assert.That(Dialogue.IsActive,Is.False,"Contextual steps must not open a modal."); }
        }
        private IEnumerator Skip(TutorialStepId step)
        {
            yield return Presentation(step);if(step==TutorialStepId.Intro)Dialogue.Skip();yield return Poll;
            if(step!=TutorialStepId.Intro)Assert.That(Manager.HasLearned(step),Is.False,"Text skip cannot teach "+step);
            if(Director.Guidance.IsVisible)
                foreach(var label in Director.Guidance.GetComponentsInChildren<TMPro.TMP_Text>())
                {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.name+" at "+step);}
        }
        private IEnumerator LearnRadar(bool useHelmPreview = false)
        {
            yield return Skip(TutorialStepId.Intro);yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation();yield return Poll;Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Helm));
            Cabin.Navigation.Step(1,0,1.5f);yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.False);
            Cabin.Navigation.Step(0,1,.5f);yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.False);
            Cabin.Navigation.StepDepth(1,1.1f);yield return Poll;
            yield return Skip(TutorialStepId.Map);
            var preview = Cabin.NavigationPanel.transform.Find("MiniMap");
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.GetComponentInChildren<PhotoSurveyMap>(true), Is.Null);
            Assert.That(Cabin.NavigationPanel.transform.Find("OpenMapHotspot"), Is.Null);
            Cabin.OpenNavigation(); yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Map), Is.False, "The helm must not satisfy map practice.");
            var mapButton = useHelmPreview ? preview : Cabin.NavigationPanel.transform.Find("WatercolorHUD/Map");
            mapButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Poll;yield return Skip(TutorialStepId.Radar);
            Cabin.OpenRadar();yield return Poll;Assert.That(Manager.HasLearned(TutorialStepId.Radar),Is.False);
            int charges=Cabin.Navigation.Ship.Radar;Cabin.Scan();yield return Poll;
            Assert.That(Cabin.Navigation.Ship.Radar,Is.EqualTo(charges-1));
            Assert.That(Manager.HasLearned(TutorialStepId.Radar),Is.True);
            Assert.That(Manager.Allows(TutorialStation.Helm),Is.True);Assert.That(Manager.Allows(TutorialStation.Map),Is.True);
        }
        [UnityTest] public IEnumerator HelmPreviewRequiresOpeningFullChartToTeachMap()
        {
            yield return LearnRadar(useHelmPreview: true);
            Assert.That(Manager.HasLearned(TutorialStepId.Map), Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.tutorial.Has(TutorialStepId.Map), Is.True);
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
            UnityAction[] calls={Cabin.OpenNavigation,Cabin.OpenMap,Cabin.OpenCurrentMap,Cabin.OpenRadar,Cabin.OpenComputer,Cabin.OpenCargo,Cabin.OpenCamera,Cabin.OpenCapture};
            foreach(var action in calls){var evt=new UnityEvent();evt.AddListener(action);evt.Invoke();Assert.That(Cabin.Panels.CurrentPanel,Is.SameAs(previous));}
            Assert.That(Cabin.IsDirectInteractionActive,Is.False);
            yield return Skip(TutorialStepId.Intro);yield return Presentation(TutorialStepId.Helm);
            Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.False);
            Assert.That(Manager.WasPresented(TutorialStepId.Helm),Is.True);
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
            Assert.That(Director.Guidance.DisplayedHint,Does.Contain("Còn thiếu"));
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
            yield return Poll;Assert.That(Director.IsPresenting,Is.False);Assert.That(Director.Guidance.IsVisible,Is.False);
            var route=Loop.ActiveMap.exitArea.mapPosition;
            Assert.That(Cabin.GetComponent<ZoneOneStory>().ProgressionActionDescription,
                Does.Contain($"({route.x:0}, {route.y:0})").And.Contain("vùng viền"));
            Cabin.GetComponent<ExpeditionProgression>().Evaluate();Cabin.ClosePanel();
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.exitArea.mapPosition,0,230,0);
            flow.LoadZone("Zone02");yield return Transition();Assert.That(Loop.Zone,Is.EqualTo("Zone02"));
            Assert.That(Manager.IsRunning,Is.False);Assert.That(Director.IsPresenting,Is.False);
            foreach(ComputerAppId app in Enum.GetValues(typeof(ComputerAppId)))Assert.That(Manager.AllowsComputerApp(app),Is.True);
        }
        [UnityTest] public IEnumerator FailedSendCannotTeachPhotoLabBeforeDurableCompletion()
        {
            yield return LearnRadar(); yield return Skip(TutorialStepId.Camera);
            Aim("zone01-left"); yield return PhysicalPhoto(); yield return Presentation(TutorialStepId.PhotoLab);
            var photo = Photos.Photos[Photos.Photos.Count - 1]; var lab = OpenLab(); yield return Poll;
            Assert.That(Loop.SaveCurrent(), Is.True);
            string path = ExpeditionSaveStore.PathOverride, disk = File.ReadAllText(path);
            ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-tutorial-send.json");
            try { lab.sendButton.onClick.Invoke(); yield return Poll; }
            finally { ExpeditionSaveStore.PathOverride = path; }
            Assert.That(photo.IsMissionPhoto, Is.False);
            Assert.That(Manager.HasLearned(TutorialStepId.PhotoLab), Is.False);
            Assert.That(Loop.MissionRuntime.HasObjective(photo.MissionObjectiveId), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
            lab.sendButton.onClick.Invoke(); yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.PhotoLab), Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
            Assert.That(saved.tutorial.Has(TutorialStepId.PhotoLab), Is.True);
            Assert.That(saved.current.photos.Find(p => p.id == photo.Id).mission, Is.True);
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
        [UnityTest] public IEnumerator HelmCountsReturnedTravelTurnsAndDepthAndKeepsMilestones()
        {
            yield return Skip(TutorialStepId.Intro); yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation(); yield return Poll;
            var nav=Cabin.Navigation; var initial=nav.Position;
            nav.Step(1,0,.8f); nav.Brake();
            nav.RestoreVoyage(nav.Position,270,nav.Depth,nav.DistanceTravelled);
            nav.Step(1,0,.8f); nav.Brake();
            Assert.That(Vector2.Distance(nav.Position,initial),Is.LessThan(1));
            Assert.That(Manager.Progress.helmDistance,Is.EqualTo(Director.config.MovementThreshold));
            nav.Step(0,1,.2f);nav.Step(0,-1,.2f);nav.Brake();
            Assert.That(nav.Heading,Is.EqualTo(270).Within(.01f));
            Assert.That(Manager.Progress.helmTurn,Is.EqualTo(Director.config.HeadingThreshold));
            nav.StepDepth(1,.6f);nav.StepDepth(-1,.6f);nav.Brake();yield return Poll;
            Assert.That(nav.Depth,Is.EqualTo(230).Within(.01f));
            Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.True);
            Assert.That(Manager.Progress.helmDepth,Is.EqualTo(Director.config.DepthThreshold));
        }
        [UnityTest] public IEnumerator PartialHelmSurvivesRebindPanelsDisableAndContinue()
        {
            yield return Skip(TutorialStepId.Intro);yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation();yield return Poll;
            Cabin.Navigation.Step(1,0,.4f);Cabin.Navigation.Brake();
            Cabin.Navigation.Step(0,1,.1f);Cabin.Navigation.StepDepth(1,.3f);Cabin.Navigation.Brake();
            var saved=ExpeditionSaveStore.Copy(Manager.Progress);
            Assert.That(saved.helmDistance,Is.GreaterThan(0).And.LessThan(20));
            Director.enabled=false;Director.enabled=true;Manager.Bind(Loop);yield return Poll;
            Cabin.ClosePanel();Cabin.OpenNavigation();yield return Poll;
            Assert.That(Manager.Progress.helmTurn,Is.EqualTo(saved.helmTurn));
            Assert.That(Loop.SaveCurrent(),Is.True);flow.LoadMainMenu();yield return Transition();
            flow.ContinueGame();yield return Transition();yield return Poll;
            Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Helm));Assert.That(Dialogue.IsActive,Is.False);
            Assert.That(Manager.Progress.helmDistance,Is.EqualTo(saved.helmDistance));
            Assert.That(Manager.Progress.helmTurn,Is.EqualTo(saved.helmTurn));
            Assert.That(Manager.Progress.helmDepth,Is.EqualTo(saved.helmDepth));
        }
        [UnityTest] public IEnumerator InvalidHelmInputCannotGrantProgressOrDisplayEnergyDrain()
        {
            yield return Skip(TutorialStepId.Intro);yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation();yield return Poll;var nav=Cabin.Navigation;
            var ship=nav.Ship.Export();ship.energy=0;nav.Ship.Restore(ship);nav.Navigate(1,1,1,1);
            Assert.That(nav.EnergyDrainRate,Is.Zero);
            ship.energy=ship.energyCapacity;nav.Ship.Restore(ship);nav.ExpeditionBlocked=true;
            nav.Navigate(1,1,1,1);nav.StepDepth(1,1);nav.ExpeditionBlocked=false;
            nav.SetChart(new byte[]{0},1,1);nav.Step(1,0,1);nav.Brake();
            nav.RestoreVoyage(nav.Position,nav.Heading,nav.Ship.MaximumDepth,nav.DistanceTravelled);nav.StepDepth(1,1);
            Assert.That(Manager.Progress.helmDistance,Is.Zero);Assert.That(Manager.Progress.helmTurn,Is.Zero);
            Assert.That(Manager.Progress.helmDepth,Is.Zero);Assert.That(nav.EnergyDrainRate,Is.Zero);
            Assert.That(Manager.HasLearned(TutorialStepId.Helm),Is.False);
        }
        [UnityTest] public IEnumerator ContextualChecklistIsPassiveFitsBothResolutionsAndLockedActionsExplainWhy()
        {
            yield return Skip(TutorialStepId.Intro);yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation();yield return Poll;
            Assert.That(Director.Guidance.ChecklistVisible,Is.True);
            foreach(var graphic in Director.Guidance.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                Assert.That(graphic.raycastTarget,Is.False,graphic.name);
            var config=Director.config;
            Assert.That(config.compactPosition.y+config.compactSize.y,Is.LessThan(70),"Keep hint above desktop window chrome.");
            Assert.That(config.helmPosition.x+config.helmSize.x,Is.LessThan(1330),"Keep checklist left of turn controls.");
            Assert.That(config.helmPosition.y+config.helmSize.y,Is.LessThan(990),"Keep checklist above status card.");
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768)})
            {
                Canvas.ForceUpdateCanvases();
                foreach(var label in Director.Guidance.GetComponentsInChildren<TMPro.TMP_Text>())
                {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.name+" at "+resolution);}
                AssertCardLayout("HelmChecklist");
                yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,$"tutorial-helm-{resolution.x}.png",resolution.x,resolution.y);
            }
            Cabin.Navigation.Step(1,0,1.5f);yield return Poll;
            var checklist=Director.Guidance.transform.Find("HelmChecklist");
            Assert.That(checklist.Find("Icon0/Number").gameObject.activeSelf,Is.False);
            Assert.That(checklist.Find("Icon0/Check0").gameObject.activeSelf,Is.True);
            Assert.That(checklist.Find("CompletedCount").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("1/3"));
            AssertCardLayout("HelmChecklist");
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,"tutorial-helm-progress-1920.png");
            Cabin.OpenRadar();Assert.That(Director.Guidance.DisplayedHint,Does.Contain("3 thao tác bàn lái"));
            AssertCardLayout("CompactHint");
            Assert.That(Manager.Allows(TutorialStation.Radar),Is.False);Assert.That(Dialogue.IsActive,Is.False);
        }
        [UnityTest] public IEnumerator CompactCameraHintKeepsRectangularRadarBorderAndReadableText()
        {
            yield return LearnRadar();yield return Skip(TutorialStepId.Camera);
            Cabin.OpenRadar();yield return Poll;
            Assert.That(Director.Guidance.DisplayedHint,Does.Contain("CAMERA"));
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768)})
            {
                AssertCardLayout("CompactHint");
                yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,$"tutorial-radar-hint-{resolution.x}.png",resolution.x,resolution.y);
            }
            var world=Cabin.GetComponent<WorldMapController>();
            var overviewTitle=world.worldPanel.transform.Find("WatercolorHUD/Title").GetComponent<TMPro.TMP_Text>();
            bool titleWasEnabled=overviewTitle.enabled;
            world.OpenWorld();yield return Poll;
            Assert.That(overviewTitle.enabled,Is.False,"The tutorial hint must not overlap the overview heading.");
            AssertCardLayout("CompactHint");
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,"tutorial-world-hint-1920.png");
            Cabin.OpenRadar();yield return Poll;
            Assert.That(overviewTitle.enabled,Is.EqualTo(titleWasEnabled));
            Assert.That(Dialogue.IsActive,Is.False);Assert.That(Manager.HasLearned(TutorialStepId.Camera),Is.False);
            world.OpenWorld();yield return Poll;Director.enabled=false;
            Assert.That(overviewTitle.enabled,Is.EqualTo(titleWasEnabled),"Hiding tutorial restores the authored overview heading.");
        }
        [UnityTest] public IEnumerator HelmCompletionPreservesConsumedEnergyAcrossSaveAndContinue()
        {
            yield return Skip(TutorialStepId.Intro); yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation(); yield return Poll;
            var ship = Cabin.Navigation.Ship;
            var state = ship.Export(); state.energyCapacity = 145; state.energy = 83;
            state.hull = 73; state.radar = 3; state.photos = 4; ship.Restore(state);
            Cabin.Navigation.Step(1, 0, 1.5f); Cabin.Navigation.Step(0, 1, .5f);
            Assert.That(ship.Energy, Is.LessThan(83), "Practice still consumes Energy.");
            float beforeCompletion = ship.Energy;
            Cabin.Navigation.StepDepth(1, 1.1f); Cabin.Navigation.Brake();
            float afterPractice = ship.Energy;
            Assert.That(afterPractice, Is.LessThan(beforeCompletion), "Completing practice consumes Energy without a tutorial refill.");
            yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Helm), Is.True);
            Assert.That(ship.Energy, Is.EqualTo(afterPractice));
            Assert.That(ship.EnergyCapacity, Is.EqualTo(145));
            Assert.That(ship.Hull, Is.EqualTo(73)); Assert.That(ship.Radar, Is.EqualTo(3)); Assert.That(ship.Photos, Is.EqualTo(4));
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.tutorial.Has(TutorialStepId.Helm), Is.True);
            Assert.That(saved.current.ship.energy, Is.EqualTo(afterPractice));
            ship.ConsumeMovement(7); Assert.That(Loop.SaveCurrent(), Is.True);
            float consumed = ship.Energy;
            Cabin.ClosePanel(); Cabin.OpenNavigation(); Manager.Bind(Loop); yield return Poll;
            Assert.That(ship.Energy, Is.EqualTo(consumed));
            Assert.That(Manager.MarkLearned(TutorialStepId.Helm), Is.False);
            flow.LoadMainMenu(); yield return Transition(); flow.ContinueGame(); yield return Transition(); yield return Poll;
            Assert.That(Cabin.Navigation.Ship.Energy, Is.EqualTo(consumed));
            Assert.That(Manager.HasLearned(TutorialStepId.Helm), Is.True);
        }
        [UnityTest] public IEnumerator FailedHelmCompletionSaveRetryPreservesConsumedEnergy()
        {
            yield return Skip(TutorialStepId.Intro); yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation(); yield return Poll;
            Cabin.Navigation.Step(1, 0, 1.5f); Cabin.Navigation.Step(0, 1, .5f);
            Assert.That(Loop.SaveCurrent(), Is.True);
            string path = ExpeditionSaveStore.PathOverride, before = File.ReadAllText(path);
            float afterPractice = 0;
            ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-helm.json");
            try
            {
                Cabin.Navigation.StepDepth(1, 1.1f); Cabin.Navigation.Brake(); afterPractice = Cabin.Navigation.Ship.Energy;
                yield return Poll;
                Assert.That(Manager.HasLearned(TutorialStepId.Helm), Is.False);
                Assert.That(Cabin.Navigation.Ship.Energy, Is.LessThan(Cabin.Navigation.Ship.EnergyCapacity));
                Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            }
            finally { ExpeditionSaveStore.PathOverride = path; }
            yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Helm), Is.True);
            Assert.That(Cabin.Navigation.Ship.Energy, Is.EqualTo(afterPractice));
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.current.ship.energy, Is.EqualTo(afterPractice));
        }
        [UnityTest] public IEnumerator HelmCompletionAndPreviousDayRollbackKeepTheirNormalEnergyBudgets()
        {
            yield return Skip(TutorialStepId.Intro); yield return Skip(TutorialStepId.Helm);
            Cabin.OpenNavigation(); yield return Poll;
            var ship = Cabin.Navigation.Ship;
            ship.ConsumeMovement(23);
            float checkpointEnergy = ship.Energy;
            Assert.That(checkpointEnergy, Is.LessThan(ship.EnergyCapacity));
            Assert.That(Manager.HasLearned(TutorialStepId.Helm), Is.False);
            Assert.That(Loop.Rest(), Is.True);
            Assert.That(Loop.Day, Is.EqualTo(2));
            Assert.That(Loop.Journal[0].checkpoint.ship.energy, Is.EqualTo(checkpointEnergy));

            Cabin.Navigation.Step(1, 0, 1.5f); Cabin.Navigation.Step(0, 1, .5f);
            Cabin.Navigation.StepDepth(1, 1.1f); Cabin.Navigation.Brake(); yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Helm), Is.True);
            Assert.That(ship.Energy, Is.LessThan(ship.EnergyCapacity), "Tutorial completion keeps the Energy spent during practice.");
            ship.ConsumeMovement(7);
            Assert.That(Loop.RestoreDay(1), Is.True);
            Assert.That(Loop.Day, Is.EqualTo(1));
            Assert.That(ship.Energy, Is.EqualTo(checkpointEnergy), "Rollback restores the pre-tutorial ship budget.");
            Assert.That(Manager.HasLearned(TutorialStepId.Helm), Is.True, "Tutorial knowledge is outside the rolled-back snapshot.");

            Manager.Bind(Loop); Cabin.ClosePanel(); Cabin.OpenNavigation(); yield return Poll;
            Assert.That(Manager.MarkLearned(TutorialStepId.Helm), Is.False);
            Assert.That(ship.Energy, Is.EqualTo(checkpointEnergy), "Rebinding and reopening preserve the restored Energy budget.");
            Cabin.Navigation.StepDepth(1, .2f); Cabin.Navigation.Brake(); yield return Poll;
            Assert.That(ship.Energy, Is.LessThan(checkpointEnergy), "Normal movement still consumes Energy after rollback.");
            Assert.That(Loop.SaveCurrent(), Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.tutorial.Has(TutorialStepId.Helm), Is.True);
            Assert.That(saved.current.ship.energy, Is.EqualTo(ship.Energy));
        }
        [UnityTest] public IEnumerator ComputerGuidanceFollowsRealControlsWithoutExtraRequiredStagesAndSurvivesContinue()
        {
            yield return LearnRadar(); yield return Skip(TutorialStepId.Camera);
            Aim("zone01-left"); yield return PhysicalPhoto(); yield return Skip(TutorialStepId.PhotoLab);
            Cabin.Navigation.RestoreVoyage(Cabin.Navigation.Position, Cabin.Navigation.Heading, 0, Cabin.Navigation.DistanceTravelled);
            yield return PhysicalPhoto();
            Cabin.ClosePanel(); yield return Poll;
            Assert.That(Director.Guidance.DisplayedHint, Does.Contain("COMPUTER"));
            Assert.That(Director.Guidance.Spotlight.Target.name, Is.EqualTo("MonitorHotspot"));
            Cabin.OpenComputer(); yield return Poll;
            Assert.That(Director.Guidance.DisplayedHint, Does.Contain("MISSION LOG").And.Contain("EXIT"));
            Assert.That(Manager.AllowsComputerApp(ComputerAppId.MissionLog), Is.True);
            Computer.OpenMissionLog(); yield return Poll;
            Assert.That(Computer.CurrentApp, Is.EqualTo(ComputerAppId.MissionLog));
            Assert.That(Director.Guidance.DisplayedHint, Does.Contain("mục tiêu"));
            Assert.That(Director.Guidance.Spotlight.Target, Is.SameAs(Computer.GetComponentInChildren<MissionLogView>().Viewport));
            Computer.CloseApp(ComputerAppId.MissionLog); yield return Poll;
            Assert.That(Manager.KnowsComputerInteraction("MissionLog"), Is.True);
            Computer.OpenMissionLog();
            Computer.GetComponentInChildren<MissionLogView>().GetComponent<ComputerWindow>().ToggleMaximize();
            yield return Poll;
            Assert.That(Director.Guidance.Spotlight.Target.name, Is.EqualTo("Task_PhotoLab"), "Highlight the visible app switcher when the shortcut is covered.");
            var lab = OpenLab(); yield return Poll;
            Assert.That(lab.sendButton.IsInteractable(), Is.False, "The newest unusable photo must remain unsendable.");
            Assert.That(Director.Guidance.Spotlight.Target, Is.SameAs(lab.previousButton.transform));
            lab.previousButton.onClick.Invoke(); yield return Poll;
            Assert.That(lab.sendButton.IsInteractable(), Is.True);
            Assert.That(Manager.CurrentStep, Is.EqualTo(TutorialStepId.PhotoLab));
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.PhotoOneObjective), Is.False);
            Assert.That(Director.Guidance.Spotlight.Target, Is.SameAs(lab.sendButton.transform));
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, lab.sendButton.transform.TransformPoint(((RectTransform)lab.sendButton.transform).rect.center));
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = point }, hits);
            Assert.That(hits.Exists(hit => hit.gameObject == lab.sendButton.gameObject || hit.gameObject.transform.IsChildOf(lab.sendButton.transform)), Is.True);
            Assert.That(hits.Exists(hit => hit.gameObject.GetComponent<TutorialSpotlight>() != null), Is.False);
            Assert.That(Director.Guidance.Spotlight.IsVisible, Is.True);
            var spotlightMesh = Director.Guidance.Spotlight.canvasRenderer.GetMesh();
            Assert.That(spotlightMesh.vertexCount, Is.GreaterThan(0), "The renderer must contain the dimmer, beyond logical target bounds.");
            Assert.That(Director.Guidance.Spotlight.canvasRenderer.materialCount, Is.GreaterThan(0));
            ScreenCapture.CaptureScreenshot(CabinNavigationPlayModeTests.CapturePath("tutorial-spotlight-native.png"));
            yield return null; yield return null;
            foreach (var resolution in new[] { new Vector2Int(1920,1080), new Vector2Int(1366,768) })
                yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, $"tutorial-spotlight-send-{resolution.x}.png", resolution.x, resolution.y);
            lab.sendButton.onClick.Invoke(); yield return Poll;
            Assert.That(Manager.CurrentStep, Is.EqualTo(TutorialStepId.Capture), "No extra required computer stages.");
            Assert.That(Director.Guidance.DisplayedHint, Does.Contain("ESC"));
            Assert.That(Computer.TryHandleBack(), Is.True); yield return Poll;
            Assert.That(Director.Guidance.Spotlight.Target, Is.Not.Null);
            Assert.That(Director.Guidance.Spotlight.Target.GetComponent<UnityEngine.UI.Button>(), Is.Not.Null);
            Computer.Exit(); yield return Poll;
            Assert.That(Manager.KnowsComputerInteraction("Exit"), Is.True);
            Assert.That(Loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return Transition(); flow.ContinueGame(); yield return Transition(); yield return Poll;
            Assert.That(Manager.KnowsComputerInteraction("Navigate"), Is.True);
            Assert.That(Manager.KnowsComputerInteraction("MissionLog"), Is.True);
            Assert.That(Manager.KnowsComputerInteraction("Exit"), Is.True);
            Cabin.OpenComputer(); Computer.OpenMissionLog(); yield return Poll;
            Assert.That(Director.Guidance.DisplayedHint, Does.Not.Contain("cuộn xem"));
            Director.enabled = false;
            Assert.That(Director.Guidance.Spotlight.IsVisible, Is.False);
            Director.enabled = true; yield return Poll;
            Manager.enabled = false;
            Assert.That(Director.Guidance.Spotlight.IsVisible, Is.False);
        }
        [UnityTest] public IEnumerator ComputerInstructionCompletionRetriesTransientSaveFailure()
        {
            // Resume at the existing PhotoLab milestone without adding mandatory app visits.
            var progress = ExpeditionSaveStore.Copy(Manager.Progress);
            progress.completedSteps.AddRange(new[] { "Intro", "Helm", "Map", "Radar", "Camera" });
            Assert.That(Loop.SaveTutorialProgress(progress), Is.True);
            Manager.Bind(Loop); yield return Poll;
            Cabin.OpenComputer(); Computer.OpenMissionLog(); yield return Poll;
            Assert.That(Manager.KnowsComputerInteraction("Navigate"), Is.True);
            string path = ExpeditionSaveStore.PathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-computer-lesson.json");
            try
            {
                Computer.CloseApp(ComputerAppId.MissionLog); Computer.Exit(); yield return Poll;
                Assert.That(Manager.KnowsComputerInteraction("MissionLog"), Is.False);
                Assert.That(Manager.KnowsComputerInteraction("Exit"), Is.False);
            }
            finally { ExpeditionSaveStore.PathOverride = path; }
            yield return Poll;
            Assert.That(Manager.KnowsComputerInteraction("MissionLog"), Is.True);
            Assert.That(Manager.KnowsComputerInteraction("Exit"), Is.True);
            flow.LoadMainMenu(); yield return Transition(); flow.ContinueGame(); yield return Transition(); yield return Poll;
            Assert.That(Manager.KnowsComputerInteraction("MissionLog"), Is.True);
            Assert.That(Manager.KnowsComputerInteraction("Exit"), Is.True);
        }
        private IEnumerator ResumeAtSurveyStep(TutorialStepId step)
        {
            var progress = ExpeditionSaveStore.Copy(Manager.Progress);
            foreach (var prior in new[] { TutorialStepId.Intro, TutorialStepId.Helm, TutorialStepId.Map,
                TutorialStepId.Radar, TutorialStepId.Camera, TutorialStepId.PhotoLab })
            {
                if (prior == step) break;
                if (!progress.completedSteps.Contains(prior.ToString())) progress.completedSteps.Add(prior.ToString());
            }
            Assert.That(Loop.SaveTutorialProgress(progress), Is.True);
            Manager.Bind(Loop); yield return Poll;
            Assert.That(Manager.CurrentStep, Is.EqualTo(step));
            Assert.That(Director.IsPractical, Is.True);
        }
        private IEnumerator ScanSurveyContact(string poiId)
        {
            Aim(poiId); Cabin.OpenRadar(); Cabin.Scan();
            yield return new WaitForSecondsRealtime(RadarDisplay.SweepDuration + .15f);
            Assert.That(Cabin.Radar.VisibleContactCount, Is.EqualTo(1), poiId);
            Cabin.ClosePanel(); yield return Poll;
            Assert.That(Cabin.Radar.VisibleContactCount, Is.Zero, "Closing radar clears transient echoes.");
        }
        private void AssertSurveySpotlight(string targetName)
        {
            Assert.That(Director.Guidance.Spotlight.IsVisible, Is.EqualTo(targetName != null));
            if (targetName != null) Assert.That(Director.Guidance.Spotlight.Target.name, Is.EqualTo(targetName));
        }
        [UnityTest] public IEnumerator CameraSpotlightRequiresDetectedActionableContactAndKeepsReceiptAfterContinue()
        {
            const string poiId = "zone01-left";
            yield return ResumeAtSurveyStep(TutorialStepId.Camera);
            Cabin.ClosePanel(); Aim(poiId); yield return Poll;
            Assert.That(Photos.CanPhotographPoi(poiId), Is.True);
            Assert.That(Manager.HasDetectedContact(poiId), Is.False);
            AssertSurveySpotlight(null);
            var approach = Cabin.Navigation;
            Vector2 nearPosition = approach.Position; float nearHeading = approach.Heading, nearDepth = approach.Depth;
            var contact = Photos.survey.FindPoi(poiId);
            bool foundApproach = false;
            float scanDistance = Photos.profile.visibleDistance + 10;
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8;
                var position = Photos.survey.ContactPosition(contact) - new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * scanDistance;
                if (!approach.CanOccupy(position)) continue;
                approach.RestoreVoyage(position, i * 22.5f, nearDepth, approach.DistanceTravelled);
                if (!Photos.survey.Detectable(approach, contact, scanDistance + .1f)) continue;
                foundApproach = true; break;
            }
            Assert.That(foundApproach, Is.True);
            Cabin.OpenRadar(); Cabin.Scan(); yield return new WaitForSecondsRealtime(RadarDisplay.SweepDuration + .15f);
            Assert.That(Cabin.Radar.VisibleContactCount, Is.EqualTo(1));
            Cabin.ClosePanel(); yield return Poll;
            Assert.That(Manager.HasDetectedContact(poiId), Is.True);
            Assert.That(Photos.CanPhotographPoi(poiId), Is.False, "Detection alone is not arrival in the photo region.");
            AssertSurveySpotlight(null);
            approach.RestoreVoyage(nearPosition, nearHeading, nearDepth, approach.DistanceTravelled); yield return Poll;
            AssertSurveySpotlight("CameraHotspot");
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "tutorial-camera-ready-1920.png");
            Cabin.OpenNavigation(); yield return Poll;
            AssertSurveySpotlight(null);
            Assert.That(Director.Guidance.DisplayedHint, Does.Contain("ESC về cabin"));
            Cabin.ClosePanel(); yield return Poll; AssertSurveySpotlight("CameraHotspot");
            Assert.That(Loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return Transition(); flow.ContinueGame(); yield return Transition(); yield return Poll;
            Assert.That(Manager.HasDetectedContact(poiId), Is.True);
            Assert.That(Cabin.Radar.VisibleContactCount, Is.Zero);
            AssertSurveySpotlight("CameraHotspot");
            var nav = Cabin.Navigation; var poi = Photos.survey.FindPoi(poiId);
            Vector2 readyPosition = nav.Position; float readyHeading = nav.Heading, readyDepth = nav.Depth;
            string resources = JsonUtility.ToJson(nav.Ship.Export());
            int photoCount = Photos.Photos.Count;
            for (int i = 0; i < 3; i++) Assert.That(Photos.CanPhotographPoi(poiId), Is.True);
            Assert.That(JsonUtility.ToJson(nav.Ship.Export()), Is.EqualTo(resources));
            Assert.That(Photos.Photos.Count, Is.EqualTo(photoCount), "Readiness checks cannot take a photograph.");
            nav.RestoreVoyage(Photos.survey.ContactPosition(poi) + Vector2.right * (Photos.profile.visibleDistance + 10), 270, readyDepth, nav.DistanceTravelled);
            yield return Poll; Assert.That(Photos.CanPhotographPoi(poiId), Is.False); AssertSurveySpotlight(null);
            nav.RestoreVoyage(readyPosition, readyHeading + 180, readyDepth, nav.DistanceTravelled);
            yield return Poll; Assert.That(Photos.CanPhotographPoi(poiId), Is.False); AssertSurveySpotlight(null);
            nav.RestoreVoyage(readyPosition, readyHeading, Photos.survey.DepthFor(poi) - 1, nav.DistanceTravelled);
            yield return Poll; Assert.That(Photos.CanPhotographPoi(poiId), Is.False); AssertSurveySpotlight(null);
            nav.RestoreVoyage(readyPosition, readyHeading, readyDepth, nav.DistanceTravelled);
            // A temporary runtime chart isolates a genuine line-of-sight obstruction without changing assets.
            int columns = Mathf.CeilToInt(nav.MapWorldSize.x / 2), rows = Mathf.CeilToInt(nav.MapWorldSize.y / 2);
            var water = new byte[columns * rows]; Array.Fill(water, (byte)1);
            Vector2 middle = nav.MapCoordinatesToNormalized((readyPosition + Photos.survey.ContactPosition(poi)) * .5f);
            water[Mathf.Clamp(Mathf.FloorToInt(middle.y * rows), 0, rows - 1) * columns +
                Mathf.Clamp(Mathf.FloorToInt(middle.x * columns), 0, columns - 1)] = 0;
            nav.SetChart(water, columns, rows);
            yield return Poll; Assert.That(Photos.CanPhotographPoi(poiId), Is.False); AssertSurveySpotlight(null);
            Loop.ActiveMap.ApplyTerrain(nav, false);
            var originalShip = nav.Ship.Export(); var emptyCamera = originalShip.Copy(); emptyCamera.photos = 0;
            nav.Ship.Restore(emptyCamera);
            yield return Poll; Assert.That(Photos.CanPhotographPoi(poiId), Is.False); AssertSurveySpotlight(null);
            nav.Ship.Restore(originalShip); yield return Poll;
            Assert.That(Photos.CanPhotographPoi(poiId), Is.True); AssertSurveySpotlight("CameraHotspot");
            yield return PhysicalPhoto();
            Assert.That(Manager.HasLearned(TutorialStepId.Camera), Is.True);
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.PhotoOneObjective), Is.False, "The photo still needs manual SEND.");
            Assert.That(Director.Guidance.Spotlight.Target == null || Director.Guidance.Spotlight.Target.name != "CameraHotspot", Is.True);
        }
        [UnityTest] public IEnumerator CaptureSpotlightRequiresItsOwnDetectedReachableContactAndAvailableAttempt()
        {
            const string poiId = "zone01-east";
            yield return ResumeAtSurveyStep(TutorialStepId.Capture);
            var nav = Cabin.Navigation; var survey = Photos.survey; var poi = survey.FindPoi(poiId);
            var catcher = Cabin.GetComponent<CreatureCatcher>();
            Cabin.ClosePanel(); nav.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), nav.DistanceTravelled);
            yield return Poll;
            Assert.That(catcher.CanStartCaptureAt(poiId), Is.True);
            Assert.That(Manager.HasDetectedContact(poiId), Is.False); AssertSurveySpotlight(null);
            yield return ScanSurveyContact("zone01-left");
            Assert.That(Manager.HasDetectedContact("zone01-left"), Is.True);
            nav.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), nav.DistanceTravelled);
            yield return Poll;
            Assert.That(catcher.CanStartCaptureAt(poiId), Is.True);
            Assert.That(Manager.HasDetectedContact(poiId), Is.False); AssertSurveySpotlight(null);
            yield return ScanSurveyContact(poiId);
            nav.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), nav.DistanceTravelled);
            yield return Poll; Assert.That(Manager.HasDetectedContact(poiId), Is.True); AssertSurveySpotlight("CaptureHotspot");
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "tutorial-capture-ready-1920.png");
            Cabin.OpenRadar(); yield return Poll;
            AssertSurveySpotlight(null);
            Assert.That(Director.Guidance.DisplayedHint, Does.Contain("ESC về cabin"));
            Cabin.ClosePanel(); yield return Poll; AssertSurveySpotlight("CaptureHotspot");
            string resources = JsonUtility.ToJson(nav.Ship.Export());
            for (int i = 0; i < 3; i++) Assert.That(catcher.CanStartCaptureAt(poiId), Is.True);
            Assert.That(JsonUtility.ToJson(nav.Ship.Export()), Is.EqualTo(resources));
            Assert.That(catcher.minigame.IsActive, Is.False, "Readiness checks cannot start a capture attempt.");
            nav.RestoreVoyage(survey.ContactPosition(poi) + Vector2.right * (poi.arrivalRadius + 10), 0, survey.DepthFor(poi), nav.DistanceTravelled);
            yield return Poll; Assert.That(catcher.CanStartCaptureAt(poiId), Is.False); AssertSurveySpotlight(null);
            nav.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi) + survey.DeeperInteractionRange + 1, nav.DistanceTravelled);
            yield return Poll; Assert.That(catcher.CanStartCaptureAt(poiId), Is.False); AssertSurveySpotlight(null);
            nav.RestoreVoyage(survey.ContactPosition(poi), 180, survey.DepthFor(poi), nav.DistanceTravelled);
            var originalShip = nav.Ship.Export(); bool unlimited = nav.Ship.UnlimitedCaptureAttempts;
            var noAttempts = originalShip.Copy(); noAttempts.captures = 0;
            nav.Ship.UnlimitedCaptureAttempts = false; nav.Ship.Restore(noAttempts);
            yield return Poll; Assert.That(catcher.CanStartCaptureAt(poiId), Is.False); AssertSurveySpotlight(null);
            nav.Ship.UnlimitedCaptureAttempts = unlimited; nav.Ship.Restore(originalShip); yield return Poll;
            Assert.That(catcher.CanStartCaptureAt(poiId), Is.True, "Capture readiness must not impose a camera-heading restriction.");
            AssertSurveySpotlight("CaptureHotspot");
            Cabin.OpenCapture(); yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(catcher.minigame.IsActive, Is.True); AssertSurveySpotlight(null);
            CaptureMinigamePlayModeTests.Win(catcher.minigame); yield return Poll;
            Assert.That(Manager.HasLearned(TutorialStepId.Capture), Is.True);
            Assert.That(Loop.MissionRuntime.HasObjective(ZoneOneStory.BlueprintObjective), Is.True);
            Assert.That(Director.Guidance.Spotlight.Target == null || Director.Guidance.Spotlight.Target.name != "CaptureHotspot", Is.True);
        }
        [UnityTest] public IEnumerator DetectedContactReceiptRetriesFailedSaveAfterRadarClosesWithoutAnotherScan()
        {
            const string poiId = "zone01-left";
            yield return ResumeAtSurveyStep(TutorialStepId.Camera);
            Cabin.ClosePanel(); Aim(poiId); yield return Poll;
            string path = ExpeditionSaveStore.PathOverride, disk = File.ReadAllText(path);
            int charges = Cabin.Navigation.Ship.Radar;
            ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-radar-receipt.json");
            try
            {
                yield return ScanSurveyContact(poiId);
                Assert.That(Manager.HasDetectedContact(poiId), Is.False, "Discovery knowledge is published only after its save succeeds.");
                Assert.That(Photos.CanPhotographPoi(poiId), Is.True);
                AssertSurveySpotlight(null);
                Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
            }
            finally { ExpeditionSaveStore.PathOverride = path; }
            yield return Poll;
            Assert.That(Manager.HasDetectedContact(poiId), Is.True);
            Assert.That(Cabin.Navigation.Ship.Radar, Is.EqualTo(charges - 1), "Retrying the receipt must not scan or consume charges again.");
            Assert.That(Cabin.Radar.VisibleContactCount, Is.Zero); AssertSurveySpotlight("CameraHotspot");
            Assert.That(Manager.Progress.completedSteps.FindAll(id => id == "RadarContact:" + poiId).Count, Is.EqualTo(1));
            Assert.That(Loop.MissionRuntime.CompletedCount, Is.Zero);
            Assert.That(Photos.Photos, Is.Empty);
            flow.LoadMainMenu(); yield return Transition(); flow.ContinueGame(); yield return Transition(); yield return Poll;
            Assert.That(Manager.HasDetectedContact(poiId), Is.True); AssertSurveySpotlight("CameraHotspot");
        }
        [UnityTest] public IEnumerator DetectionDuringPauseIsRememberedWithoutRescanOrHiddenDeviceHighlight()
        {
            const string poiId = "zone01-left";
            yield return ResumeAtSurveyStep(TutorialStepId.Camera);
            Cabin.ClosePanel(); Aim(poiId);
            int charges = Cabin.Navigation.Ship.Radar;
            Cabin.OpenRadar(); Cabin.Scan(); Cabin.OpenPause();
            Assert.That(PauseMenuController.Instance.IsPaused, Is.True);
            yield return new WaitForSecondsRealtime(RadarDisplay.SweepDuration + .15f);
            Assert.That(Cabin.Radar.VisibleContactCount, Is.EqualTo(1));
            Assert.That(Manager.HasDetectedContact(poiId), Is.False);
            AssertSurveySpotlight(null);
            PauseMenuController.Instance.Resume(); yield return Poll;
            Assert.That(Manager.HasDetectedContact(poiId), Is.True);
            Assert.That(Cabin.Navigation.Ship.Radar, Is.EqualTo(charges - 1));
            AssertSurveySpotlight(null);
            Assert.That(Director.Guidance.DisplayedHint, Does.Contain("ESC về cabin"));
            Cabin.ClosePanel(); yield return Poll; AssertSurveySpotlight("CameraHotspot");
        }
        private void AssertCardLayout(string name)
        {
            Canvas.ForceUpdateCanvases();
            var card=(RectTransform)Director.Guidance.transform.Find(name);
            var image=card.GetComponent<UnityEngine.UI.Image>();
            var radar=Cabin.RadarPanel.transform.Find("RadarInfo").GetComponent<UnityEngine.UI.Image>();
            Assert.That(image.sprite,Is.SameAs(radar.sprite));
            Assert.That(image.type,Is.EqualTo(UnityEngine.UI.Image.Type.Sliced));
            Assert.That(image.pixelsPerUnitMultiplier,Is.EqualTo(radar.pixelsPerUnitMultiplier));
            foreach(var label in card.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.name);
                // Check actual glyph bounds as well as the text rect: midline alignment previously put text on the border.
                for(int i=0;i<label.textInfo.characterCount;i++)
                {
                    var character=label.textInfo.characterInfo[i];if(!character.isVisible)continue;
                    foreach(var corner in new[]{character.bottomLeft,character.topRight})
                    {
                        var point=card.InverseTransformPoint(label.transform.TransformPoint(corner));
                        Assert.That(point.x,Is.InRange(card.rect.xMin+14,card.rect.xMax-14),label.name+" horizontal padding");
                        Assert.That(point.y,Is.InRange(card.rect.yMin+12,card.rect.yMax-12),label.name+" vertical padding");
                    }
                }
            }
            if(name!="HelmChecklist")return;
            for(int i=0;i<3;i++)
            {
                var row=(RectTransform)card.Find(new[]{"Di chuyển","Xoay hướng","Đổi độ sâu"}[i]);
                var value=(RectTransform)card.Find("Value"+i);
                Assert.That(-row.anchoredPosition.y,Is.EqualTo(-value.anchoredPosition.y));
                Assert.That(row.anchoredPosition.x+row.rect.width,Is.LessThan(value.anchoredPosition.x));
                var track=(RectTransform)card.Find("Track"+i);
                Assert.That(-track.anchoredPosition.y,Is.GreaterThan(-row.anchoredPosition.y+row.rect.height));
                if(i<2)Assert.That(-track.anchoredPosition.y+track.rect.height,Is.LessThan(-((RectTransform)card.Find("Value"+(i+1))).anchoredPosition.y));
            }
        }
        [UnityTest] public IEnumerator SendThenImmediateDeleteAndContinueCannotStrandPhotoLabTutorial()
        {
            yield return LearnRadar();yield return Skip(TutorialStepId.Camera);Aim("zone01-left");yield return PhysicalPhoto();
            yield return Skip(TutorialStepId.PhotoLab);var lab=OpenLab();yield return Poll;
            string id=lab.SelectedPhotoId;lab.SubmitCurrent();
            Assert.That(Manager.HasLearned(TutorialStepId.PhotoLab),Is.True,"Valid SEND is learned synchronously.");
            Assert.That(Photos.DeletePhoto(id),Is.True,Photos.LastError);Assert.That(Photos.Photos.Count,Is.Zero);
            Assert.That(Loop.SaveCurrent(),Is.True);flow.LoadMainMenu();yield return Transition();
            flow.ContinueGame();yield return Transition();yield return Poll;
            Assert.That(Manager.CurrentStep,Is.EqualTo(TutorialStepId.Capture));Assert.That(Manager.Allows(TutorialStation.Capture),Is.True);
            Assert.That(Photos.Photos.Count,Is.Zero);Assert.That(Dialogue.IsActive,Is.False);
        }
    }
}

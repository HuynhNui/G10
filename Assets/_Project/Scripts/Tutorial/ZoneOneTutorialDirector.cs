using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
using G10.Prototype.Missions;
using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Tutorial
{
    /// <summary>Observes production gameplay; never grants mission objectives or upgrades.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneOneTutorialDirector : MonoBehaviour
    {
        public TutorialConfig config;
        private TutorialManager manager;
        private ExpeditionLoop loop;
        private CabinStationView cabin;
        private DialogueController dialogue;
        private ComputerScreenController computer;
        private PhotoCaptureService photos;
        private ZoneMissionRuntime runtime;
        private WorldMapController worldMap;
        private TutorialStepId step;
        private bool ownsDialogue, practical, labOpened;
        private bool stepAssigned;
        private float nextPoll,retryAt;
        private TutorialGuidanceView guidance;
        private G10.Prototype.Navigation.ZoneNavigation observedNavigation;
        private int radarCharges;
        private bool configured;
        public bool IsConfigured => config!=null && configured && dialogue!=null;
        public bool IsPresenting => ownsDialogue;
        public bool IsPractical => practical;
        public TutorialGuidanceView Guidance => guidance;
        public void ShowLockReason() { if (manager != null && manager.IsRunning) guidance?.Notice(manager.LockReason); }
        public void Initialize(TutorialManager owner,ExpeditionLoop expedition,CabinStationView view)
        {
            Suspend();manager=owner;loop=expedition;cabin=view;
            dialogue=FindAnyObjectByType<DialogueController>();
            computer=cabin.GetComponentInChildren<ComputerScreenController>(true);
            photos=cabin.GetComponent<PhotoCaptureService>();runtime=loop.MissionRuntime;
            worldMap=cabin.GetComponent<WorldMapController>();
            if (guidance == null) guidance = TutorialGuidanceView.Create(cabin, manager, config);
            configured=config!=null && config.IsConfigured && photos!=null && runtime!=null && computer!=null;
            stepAssigned=false;retryAt=Time.unscaledTime+.3f;nextPoll=0;
            if(!IsConfigured){Debug.LogWarning("Zone01 tutorial is not configured; tutorial access restrictions are disabled.",this);return;}
            if(manager.IsRunning)Subscribe();
        }
        private void Subscribe()
        {
            if(runtime!=null){runtime.Changed-=OnMissionChanged;runtime.Changed+=OnMissionChanged;}
            if(computer!=null){computer.WindowStateChanged-=ObserveApp;computer.WindowStateChanged+=ObserveApp;}
            observedNavigation = cabin.Navigation;
            observedNavigation.ControlApplied -= OnControlApplied;
            observedNavigation.ControlApplied += OnControlApplied;
        }
        private void OnEnable(){if(manager!=null){if(manager.IsRunning)Subscribe();stepAssigned=false;}}
        private void OnDisable()=>Suspend();
        public void Suspend()
        {
            if(runtime!=null)runtime.Changed-=OnMissionChanged;
            if(computer!=null)computer.WindowStateChanged-=ObserveApp;
            if(observedNavigation!=null)observedNavigation.ControlApplied-=OnControlApplied;
            bool cancel=ownsDialogue;ownsDialogue=false;
            if(cancel&&dialogue!=null&&dialogue.IsActive)dialogue.Cancel();
            practical=false;stepAssigned=false;guidance?.Hide();
            StopAllCoroutines();
        }
        private bool Safe => loop!=null && loop.IsInitialized && !loop.Blocked && loop.Zone=="Zone01" &&
            (SceneFlowController.Instance==null||!SceneFlowController.Instance.IsTransitioning) && cabin!=null &&
            cabin.Panels!=null && !cabin.IsDirectInteractionActive &&
            (PauseMenuController.Instance==null||!PauseMenuController.Instance.IsPaused);
        private void Update()
        {
            if(manager==null||!IsConfigured||Time.unscaledTime<nextPoll)return;
            nextPoll=Time.unscaledTime+config.PollInterval;
            if(!Safe)
            {
                if(ownsDialogue){ownsDialogue=false;dialogue.Cancel();}
                guidance?.Hide();
                return;
            }
            if(!manager.IsRunning){guidance?.Hide();return;}
            if(!stepAssigned||step!=manager.CurrentStep)
            {step=manager.CurrentStep;stepAssigned=true;practical=false;labOpened=false;}
            if(ownsDialogue||cabin.Panels.IsModalOpen){guidance?.Hide();return;}
            guidance?.Refresh();
            if(!practical)
            {
                if(manager.WasPresented(step))BeginPractical();
                else if(step!=TutorialStepId.Intro)
                { if(manager.MarkPresented(step))BeginPractical(); }
                else if(Time.unscaledTime>=retryAt)Present(step);
                return;
            }
            ObserveApp();EvaluatePractical();
        }
        private void Present(TutorialStepId requested)
        {
            ownsDialogue=true;
            if(!dialogue.TryBegin(config.Lines(requested),reason=>
            {
                if(!ownsDialogue)return;
                ownsDialogue=false;retryAt=Time.unscaledTime+config.RetryDelay;
                if(reason==DialogueEndReason.Cancelled)return;
                if(!manager.IsRunning||manager.CurrentStep!=requested)return;
                if(!manager.WasPresented(requested)&&!manager.MarkPresented(requested))return;
                BeginPractical();
            }))ownsDialogue=false;
        }
        private void BeginPractical()
        {
            practical=true;radarCharges=cabin.Navigation.Ship.Radar;
            if(step==TutorialStepId.Intro)LearnCurrent();
        }
        private void OnControlApplied(float distance, float angle, float depth)
        {
            if (!Safe || !practical || !manager.IsRunning || step != TutorialStepId.Helm ||
                cabin.Panels.IsModalOpen || cabin.Panels.CurrentPanel != cabin.NavigationPanel) return;
            bool firstDepth = manager.Progress.helmDepth <= 0 && depth > 0;
            manager.RecordHelmProgress(distance, angle, depth);
            if (firstDepth) guidance?.Notice("Điều khiển tàu tiêu hao ENERGY");
            guidance?.Refresh(); EvaluatePractical();
        }
        private void ObserveApp()
        {
            if(practical&&step==TutorialStepId.PhotoLab&&computer!=null&&computer.gameObject.activeInHierarchy&&
                computer.CurrentApp==ComputerAppId.PhotoLab)labOpened=true;
        }
        private void OnMissionChanged()
        {
            // A sent photo may be deleted immediately: learn from the irreversible mission event.
            if(practical&&(step==TutorialStepId.Upgrade||step==TutorialStepId.PhotoLab)&&manager.IsRunning&&Safe)EvaluatePractical();
        }
        private void EvaluatePractical()
        {
            bool complete=false;
            switch(step)
            {
                case TutorialStepId.Helm:
                    complete=manager.Progress.helmDistance>=config.MovementThreshold &&
                        manager.Progress.helmTurn>=config.HeadingThreshold && manager.Progress.helmDepth>=config.DepthThreshold;break;
                case TutorialStepId.Map:
                    var panel=cabin.Panels.CurrentPanel;
                    // Only the full map/world-map panels count, never the helm.
                    complete=panel!=null&&panel.activeInHierarchy&&(panel==cabin.MapPanel||
                        worldMap!=null&&(panel==worldMap.worldPanel||
                        System.Array.IndexOf(worldMap.zoneMaps??System.Array.Empty<GameObject>(),panel)>=0));break;
                case TutorialStepId.Radar:
                    complete=cabin.Navigation.Ship.Radar<radarCharges;
                    radarCharges=cabin.Navigation.Ship.Radar;break;
                case TutorialStepId.Camera:
                    foreach(var photo in photos.Photos)
                        if(photo.MissionZoneId=="Zone01"&&!string.IsNullOrEmpty(photo.MissionObjectiveId)&&!photo.IsMissionPhoto)
                        {complete=true;break;}
                    break;
                case TutorialStepId.PhotoLab:
                    complete=labOpened&&(runtime.HasObjective(ZoneOneStory.PhotoOneObjective)||runtime.HasObjective(ZoneOneStory.PhotoTwoObjective));
                    if(labOpened)foreach(var photo in photos.Photos)
                        if(photo.MissionZoneId=="Zone01"&&!string.IsNullOrEmpty(photo.MissionObjectiveId)&&
                            (photo.IsMissionPhoto||runtime.HasObjective(photo.MissionObjectiveId))) {complete=true;break;}
                    break;
                case TutorialStepId.Capture: complete=runtime.HasObjective(ZoneOneStory.BlueprintObjective);break;
                case TutorialStepId.Upgrade: complete=runtime.HasObjective("Z1_GATE_INSTALL_PRESSURE_HULL");break;
            }
            if(complete)LearnCurrent();
        }
        private void LearnCurrent()
        {
            if(!manager.MarkLearned(step))return;
            practical=false;stepAssigned=false;retryAt=Time.unscaledTime+.3f;
            if(manager.Completed)
            {
                Suspend();
            }
        }
    }
}

using System;
using G10.Prototype.Computer;
using G10.Prototype.Missions;
using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialManager : MonoBehaviour
    {
        private ExpeditionLoop loop;
        private ZoneOneTutorialDirector director;
        public TutorialProgressState Progress => loop?.TutorialProgress;
        public bool Completed => Progress?.completed == true;
        public bool IsRunning => isActiveAndEnabled && director!=null && director.isActiveAndEnabled && director.IsConfigured &&
            loop!=null && loop.IsInitialized && loop.Zone=="Zone01" && !Completed && Progress?.waitForNewGame!=true;
        public TutorialStepId CurrentStep
        {
            get {foreach(TutorialStepId step in Enum.GetValues(typeof(TutorialStepId)))if(!HasLearned(step))return step;return TutorialStepId.Complete;}
        }
        public bool HasLearned(TutorialStepId step) => Progress?.Has(step)==true;
        public bool WasPresented(TutorialStepId step) => Progress?.Presented(step)==true;
        public bool UpgradeReady => loop?.MissionRuntime!=null && loop.MissionRuntime.MainLocationsComplete &&
            loop.MissionRuntime.HasRecipe(ZoneOneStory.PressureHullRecipe);
        public void Bind(ExpeditionLoop owner)
        {
            loop=owner;director=GetComponent<ZoneOneTutorialDirector>();
            director?.Initialize(this,owner,GetComponent<CabinStationView>());
        }
        public bool MarkPresented(TutorialStepId step)
        {
            if(Progress==null||WasPresented(step))return false;
            var next=ExpeditionSaveStore.Copy(Progress);next.presentedSteps.Add(step.ToString());
            return loop.SaveTutorialProgress(next);
        }
        public bool MarkLearned(TutorialStepId step)
        {
            if(!IsRunning||step!=CurrentStep||HasLearned(step))return false;
            var next=ExpeditionSaveStore.Copy(Progress);next.completedSteps.Add(step.ToString());
            if(step==TutorialStepId.Upgrade||step==TutorialStepId.Complete)
            {next.completed=true;if(!next.completedSteps.Contains(nameof(TutorialStepId.Complete)))next.completedSteps.Add(nameof(TutorialStepId.Complete));}
            return loop.SaveTutorialProgress(next);
        }
        private bool NeedsSupplies => loop?.Navigation!=null && (loop.NeedsRecovery || loop.Navigation.Ship.Photos==0 ||
            loop.Navigation.Ship.Radar==0 || loop.Navigation.Ship.Captures==0);
        public bool Allows(TutorialStation station)
        {
            if(!IsRunning||loop.Blocked)return true; // Never obstruct the existing failure/recovery UI.
            return station switch {
                TutorialStation.Helm => HasLearned(TutorialStepId.Intro),
                TutorialStation.Map => HasLearned(TutorialStepId.Helm),
                TutorialStation.Radar => HasLearned(TutorialStepId.Map),
                TutorialStation.Camera => HasLearned(TutorialStepId.Radar),
                TutorialStation.Computer => HasLearned(TutorialStepId.Camera)||NeedsSupplies,
                TutorialStation.Capture => HasLearned(TutorialStepId.PhotoLab),
                TutorialStation.Cargo => HasLearned(TutorialStepId.Capture), _=>false };
        }
        public bool AllowsComputerApp(ComputerAppId app)
        {
            if(!IsRunning||loop.Blocked)return true;
            if(!Allows(TutorialStation.Computer))return false;
            return app switch {
                ComputerAppId.Desktop=>true,
                ComputerAppId.Rest or ComputerAppId.Journal=>NeedsSupplies||HasLearned(TutorialStepId.PhotoLab),
                ComputerAppId.PhotoLab=>HasLearned(TutorialStepId.Camera),
                ComputerAppId.Upgrade=>HasLearned(TutorialStepId.Capture)&&UpgradeReady,
                ComputerAppId.Cargo=>HasLearned(TutorialStepId.Capture), _=>false };
        }
        private void OnEnable(){if(loop!=null&&loop.IsInitialized)Bind(loop);}
        private void OnDisable()=>director?.Suspend();
    }
}

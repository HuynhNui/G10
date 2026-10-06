using System;
using System.Collections;
using System.IO;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ExpeditionEconomyPlayModeTests
    {
        private string folder,oldSave,oldPhotos;
        private SceneFlowController flow;
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private CreatureInventory Inventory => Cabin.GetComponent<CreatureInventory>();
        private ShipResources Ship => Cabin.Navigation.Ship;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"EconomyPlay-"+Guid.NewGuid().ToString("N"));
            oldSave=ExpeditionSaveStore.PathOverride; oldPhotos=PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"timeline.json"); PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            TutorialTestSave.SeedReturningPlayer();
            if(SceneFlowController.Instance!=null) { Object.Destroy(SceneFlowController.Instance.gameObject);yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single);yield return null;
            flow=SceneFlowController.Instance;yield return WaitTransition();flow.StartNewGame();yield return WaitTransition();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(flow!=null)Object.Destroy(flow.gameObject);yield return null;
            ExpeditionSaveStore.PathOverride=oldSave;PhotoCaptureService.ArchivePathOverride=oldPhotos;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        private IEnumerator WaitTransition()
        {
            yield return null;float timeout=Time.realtimeSinceStartup+20;
            while(flow.IsTransitioning && Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(flow.IsTransitioning,Is.False);Assert.That(flow.LastError,Is.Null.Or.Empty);
        }
        private ExpeditionSave Read()
        { Assert.That(ExpeditionSaveStore.TryRead(out var save,out var error),Is.True,error);return save; }
        private IEnumerator Reload()
        { flow.LoadMainMenu();yield return WaitTransition();flow.ContinueGame();yield return WaitTransition(); }
        private void Add(string id,int count)
        { for(int i=0;i<count;i++)Assert.That(Inventory.TryAdd(id,id,null),Is.True); }
        [UnityTest] public IEnumerator GlobalBoundarySaveAndJournalIgnoreCompletedZoneDeadline()
        {
            Assert.That(Loop.Day,Is.EqualTo(1));Assert.That(Loop.DaysLeft,Is.EqualTo(15));Assert.That(Loop.TotalDays,Is.EqualTo(15));
            for(int day=2;day<=15;day++)Assert.That(Loop.Rest(),Is.True);
            Assert.That(Loop.Day,Is.EqualTo(15));Assert.That(Loop.DaysLeft,Is.EqualTo(1));Assert.That(Loop.Failed,Is.False);
            var story=Cabin.GetComponent<ZoneOneStory>();
            story.RecordObjective("zone01-left",MissionObjectiveType.Photograph,"Z1_Creature_01");
            story.RecordObjective("zone01-north",MissionObjectiveType.Photograph,"Z1_Creature_02");
            story.RecordObjective("zone01-east",MissionObjectiveType.Collect,ZoneOneStory.EmmaBlueprint);story.InstallHull();
            Assert.That(Loop.RequiredObjectivesComplete,Is.True);
            Assert.That(Loop.Rest(),Is.True);Assert.That(Loop.Day,Is.EqualTo(16));Assert.That(Loop.DaysLeft,Is.Zero);Assert.That(Loop.Failed,Is.True);
            yield return Reload();Assert.That(Loop.Day,Is.EqualTo(16));Assert.That(Loop.Failed,Is.True);
            Assert.That(Loop.RestoreDay(14),Is.True);Assert.That(Loop.Day,Is.EqualTo(14));Assert.That(Loop.DaysLeft,Is.EqualTo(2));Assert.That(Loop.Failed,Is.False);
        }
        [UnityTest] public IEnumerator RestFadeBlocksDoubleFireAndUsesUnscaledTime()
        {
            Cabin.OpenComputer();var screen=Cabin.GetComponentInChildren<ComputerScreenController>(true);
            var ui=screen.GetComponent<ExpeditionComputerView>();ui.OpenRest();ui.RequestRest();
            Vector2 position=Cabin.Navigation.Position;Time.timeScale=0;ui.ConfirmRest();ui.ConfirmRest();
            Assert.That(flow.IsTransitioning,Is.True);Assert.That(Loop.Day,Is.EqualTo(1));Assert.That(Loop.CanRest,Is.False);
            Assert.That(Loop.Rest(),Is.False);Assert.That(flow.PresentRest(Loop),Is.False);
            Assert.That(Loop.RestoreDay(1),Is.False);
            Cabin.Navigation.Step(1,1,2);Assert.That(Cabin.Navigation.Position,Is.EqualTo(position));
            float timeout=Time.realtimeSinceStartup+5;
            while(Loop.Day==1 && Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(Loop.Day,Is.EqualTo(2));Assert.That(flow.DayLeftPresentationText,Is.EqualTo("DAY LEFT: 14"));
            var label=flow.GetComponentsInChildren<TMPro.TMP_Text>().Single(x=>x.name=="RestDayLeft");
            Assert.That(label.text,Is.EqualTo("DAY LEFT: 14"));Assert.That(label.GetComponentInParent<CanvasGroup>().alpha,Is.EqualTo(1));
            yield return WaitTransition();Time.timeScale=1;
            Assert.That(Loop.Day,Is.EqualTo(2));Assert.That(Loop.Journal.Count,Is.EqualTo(1));Assert.That(Loop.DaysLeft,Is.EqualTo(14));
            Assert.That(Cabin.Panels.IsPanelOpen,Is.False);Assert.That(Cabin.Navigation.TransitionBlocked,Is.False);Assert.That(Loop.CanRest,Is.True);
            Ship.ConsumeMovement(10000);Assert.That(flow.PresentRest(Loop,true),Is.True);yield return WaitTransition();
            Assert.That(Loop.Day,Is.EqualTo(3));Assert.That(flow.DayLeftPresentationText,Is.EqualTo("DAY LEFT: 13"));Assert.That(Ship.Energy,Is.EqualTo(100));
        }
        [UnityTest] public IEnumerator AllThreeBranchesConsumeExactCostsPersistAndStopAtMax()
        {
            Add(RegularShipUpgradeRules.TierOneMaterial,5);Add(RegularShipUpgradeRules.TierTwoMaterial,6);
            float speed=Loop.BaseMovementSpeed;float dive=Ship.DiveSpeed,ascent=Ship.AscentSpeed;
            Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.DiveSpeed),Is.False);
            foreach(var branch in new[]{ShipUpgrade.Hull,ShipUpgrade.Speed,ShipUpgrade.Energy})
            {
                int before=Inventory.GetCount(RegularShipUpgradeRules.TierOneMaterial);
                Assert.That(Loop.TryPurchaseUpgrade(branch),Is.True);
                Assert.That(Inventory.GetCount(RegularShipUpgradeRules.TierOneMaterial),Is.EqualTo(before-RegularShipUpgradeRules.Cost(branch,0)));
                Assert.That(Loop.UpgradeLevel(branch),Is.EqualTo(1));
            }
            Assert.That(Ship.HullCapacity,Is.EqualTo(120));Assert.That(Ship.Speed,Is.EqualTo(speed*1.1f).Within(.001));
            Assert.That(Ship.EnergyPerSecond,Is.EqualTo(.9f).Within(.001));Assert.That(Ship.EnergyCapacity,Is.EqualTo(100));
            foreach(var branch in new[]{ShipUpgrade.Hull,ShipUpgrade.Speed,ShipUpgrade.Energy})
            { Assert.That(Loop.TryPurchaseUpgrade(branch),Is.True);Assert.That(Loop.UpgradeLevel(branch),Is.EqualTo(2));Assert.That(Loop.TryPurchaseUpgrade(branch),Is.False); }
            Assert.That(Inventory.Items,Is.Empty);Assert.That(Ship.HullCapacity,Is.EqualTo(140));Assert.That(Ship.Speed,Is.EqualTo(speed*1.2f).Within(.001));
            Assert.That(Ship.EnergyPerSecond,Is.EqualTo(.8f).Within(.001));Assert.That(Ship.EnergyCapacity,Is.EqualTo(100));
            Assert.That(Ship.DiveSpeed,Is.EqualTo(dive));Assert.That(Ship.AscentSpeed,Is.EqualTo(ascent));
            string stats=JsonUtility.ToJson(Ship.Export());yield return Reload();
            Assert.That(JsonUtility.ToJson(Ship.Export()),Is.EqualTo(stats));
            Assert.That(Loop.UpgradeLevel(ShipUpgrade.Hull),Is.EqualTo(2));Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Hull),Is.False);
        }
        [UnityTest] public IEnumerator PurchaseFailureRollsBackStatsLevelsMaterialsAndDisk()
        {
            Add(RegularShipUpgradeRules.TierOneMaterial,3);Assert.That(Loop.SaveCurrent(),Is.True);
            string path=ExpeditionSaveStore.PathOverride, disk=File.ReadAllText(path),stats=JsonUtility.ToJson(Ship.Export());
            ExpeditionSaveStore.PathOverride=Path.Combine(path,"blocked.json");
            try { Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Hull),Is.False); }
            finally { ExpeditionSaveStore.PathOverride=path; }
            Assert.That(Loop.UpgradeLevel(ShipUpgrade.Hull),Is.Zero);Assert.That(Inventory.GetCount(RegularShipUpgradeRules.TierOneMaterial),Is.EqualTo(3));
            Assert.That(JsonUtility.ToJson(Ship.Export()),Is.EqualTo(stats));Assert.That(File.ReadAllText(path),Is.EqualTo(disk));
            Inventory.TryConsume(new System.Collections.Generic.Dictionary<string,int>{{RegularShipUpgradeRules.TierOneMaterial,2}});
            Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Hull),Is.False);Assert.That(Inventory.GetCount(RegularShipUpgradeRules.TierOneMaterial),Is.EqualTo(1));
            yield return null;
        }
        [UnityTest] public IEnumerator JournalAndDeathRestoreUpgradeLevelsStatsAndQuantities()
        {
            Add(RegularShipUpgradeRules.TierOneMaterial,5);Add(RegularShipUpgradeRules.TierTwoMaterial,4);
            Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Hull),Is.True);Assert.That(Loop.Rest(),Is.True);
            Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Hull),Is.True);Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Speed),Is.True);
            Ship.HitTerrain(10000);yield return null;
            float timeout=Time.realtimeSinceStartup+10;
            while(Loop.IsDeathInProgress && Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(Loop.IsDeathInProgress,Is.False);Assert.That(Loop.Day,Is.EqualTo(2));
            Assert.That(Loop.UpgradeLevel(ShipUpgrade.Hull),Is.EqualTo(1));Assert.That(Loop.UpgradeLevel(ShipUpgrade.Speed),Is.Zero);
            Assert.That(Ship.HullCapacity,Is.EqualTo(120));Assert.That(Inventory.GetCount(RegularShipUpgradeRules.TierTwoMaterial),Is.EqualTo(4));
            Assert.That(Inventory.GetCount(RegularShipUpgradeRules.TierOneMaterial),Is.EqualTo(3));
            Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Hull),Is.True);
            Assert.That(Loop.RestoreDay(1),Is.True);Assert.That(Loop.Day,Is.EqualTo(1));Assert.That(Loop.UpgradeLevel(ShipUpgrade.Hull),Is.EqualTo(1));
            Assert.That(Ship.HullCapacity,Is.EqualTo(120));Assert.That(Inventory.GetCount(RegularShipUpgradeRules.TierTwoMaterial),Is.EqualTo(4));
            yield return Reload();Assert.That(Loop.UpgradeLevel(ShipUpgrade.Hull),Is.EqualTo(1));Assert.That(Ship.HullCapacity,Is.EqualTo(120));
        }
        [UnityTest] public IEnumerator UpgradeCardsAndDetailsRefreshFromRealRuntimeInventory()
        {
            Cabin.OpenComputer();var screen=Cabin.GetComponentInChildren<ComputerScreenController>(true);screen.OpenUpgrade();yield return null;
            var ui=screen.GetComponentInChildren<SubmarineUpgradeUIController>(true);
            var cards=ui.GetComponentsInChildren<UpgradeEntryConfig>(true);var regular=cards.Where(x=>x.Category==UpgradeCategory.ShipSystem).ToArray();
            Assert.That(regular.Select(x=>x.UpgradeId).ToArray(),Is.EqualTo(new[]{"Hull","MaxSpeed","Energy"}));
            Assert.That(ui.Selected,Is.SameAs(regular[0]));Assert.That(cards.Single(x=>x.UpgradeId=="ExpeditionModule").GetComponent<StoryHullUpgradeAction>(),Is.Not.Null);
            Assert.That(regular[0].GetComponent<StoryHullUpgradeAction>(),Is.Null);
            var button=ui.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(x=>x.name=="ActionButton");
            Func<string> quantity=()=>ui.GetComponentsInChildren<TMPro.TMP_Text>().Single(x=>x.name=="QuantityText").text;
            Assert.That(button.interactable,Is.False);Assert.That(quantity(),Is.EqualTo("0 / 2"));
            Add(RegularShipUpgradeRules.TierOneMaterial,1);Assert.That(quantity(),Is.EqualTo("1 / 2"));Assert.That(button.interactable,Is.False);
            Add(RegularShipUpgradeRules.TierOneMaterial,1);Assert.That(quantity(),Is.EqualTo("2 / 2"));Assert.That(button.interactable,Is.True);
            Assert.That(regular[0].Level,Is.Zero);Assert.That(regular[0].ComparisonRows[0].CurrentValue,Is.EqualTo("100"));
            button.onClick.Invoke();Assert.That(regular[0].Level,Is.EqualTo(1));Assert.That(Inventory.GetCount(RegularShipUpgradeRules.TierOneMaterial),Is.Zero);
            Assert.That(regular[0].ComparisonRows[0].CurrentValue,Is.EqualTo("120"));Assert.That(regular[0].ComparisonRows[0].NextValue,Is.EqualTo("140"));
            Assert.That(quantity(),Is.EqualTo("0 / 2"));
            Add(RegularShipUpgradeRules.TierTwoMaterial,2);button.onClick.Invoke();Assert.That(regular[0].IsMax,Is.True);Assert.That(button.interactable,Is.False);
            Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>().text,Is.EqualTo("MAX"));
            yield return null;
        }
        [UnityTest] public IEnumerator RestSaveFailureDoesNotAdvanceAndReleasesTransitionLock()
        {
            string path=ExpeditionSaveStore.PathOverride,disk=File.ReadAllText(path);
            ExpeditionSaveStore.PathOverride=Path.Combine(path,"blocked-rest.json");
            try
            {
                Assert.That(flow.PresentRest(Loop),Is.True);
                yield return null;float timeout=Time.realtimeSinceStartup+5;
                while(flow.IsTransitioning && Time.realtimeSinceStartup<timeout)yield return null;
                Assert.That(flow.IsTransitioning,Is.False);Assert.That(flow.LastError,Is.Not.Null.And.Not.Empty);
                Assert.That(Loop.Day,Is.EqualTo(1));Assert.That(Loop.Journal,Is.Empty);Assert.That(Loop.DaysLeft,Is.EqualTo(15));
                Assert.That(Loop.CanRest,Is.True);Assert.That(Cabin.Navigation.TransitionBlocked,Is.False);
                Assert.That(File.ReadAllText(path),Is.EqualTo(disk));
            }
            finally { ExpeditionSaveStore.PathOverride=path; }
        }
        [UnityTest] public IEnumerator BothConfiguredMaterialCapturesRepeatButCollectionStaysOneTime()
        {
            foreach(string zone in new[]{"Zone02","Zone03"})
            {
                flow.LoadMainMenu();yield return WaitTransition();var save=Read();save.current.zone=zone;save.current.zones.Clear();
                save.current.zones.Add(new ExpeditionZoneState{zone=zone,deadline=15});save.dayStart=null;save.hasDayStart=false;
                ExpeditionSaveStore.Write(save,true);flow.ContinueGame();yield return WaitTransition();
                var runtime=Loop.MissionRuntime;var survey=Cabin.GetComponent<PhotoSurveyZone>();var catcher=Cabin.GetComponent<CreatureCatcher>();
                string poiId=zone=="Zone02"?"zone02-l3":"zone03-l1";
                string material=zone=="Zone02"?RegularShipUpgradeRules.TierOneMaterial:RegularShipUpgradeRules.TierTwoMaterial;
                var poi=survey.FindPoi(poiId);var objective=runtime.FindObjective(poiId,MissionObjectiveType.Capture);
                runtime.RecordObjective(poiId,MissionObjectiveType.Photograph,material);int completed=runtime.CompletedCount;
                int initialCharges=Ship.Captures;
                for(int attempt=1;attempt<=3;attempt++)
                {
                    Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi),0,survey.DepthFor(poi),0);
                    Assert.That(catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.Started),poiId);
                    CaptureMinigamePlayModeTests.Win(catcher.minigame);
                    Assert.That(catcher.LastResult,Is.EqualTo(CreatureCatcher.Result.Caught));Assert.That(Inventory.GetCount(material),Is.EqualTo(attempt));
                    Assert.That(runtime.CompletedCount,Is.EqualTo(completed+1));Assert.That(Inventory.Items.Count(x=>x.Id==material),Is.EqualTo(1));
                }
                Assert.That(Ship.Captures,Is.EqualTo(initialCharges-3));Assert.That(Loop.Rest(),Is.True);Assert.That(Ship.Captures,Is.EqualTo(Ship.CaptureCapacity));
                Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi),0,survey.DepthFor(poi),0);
                Assert.That(catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.Started));CaptureMinigamePlayModeTests.Win(catcher.minigame);
                Assert.That(Inventory.GetCount(material),Is.EqualTo(4));Assert.That(Loop.SaveCurrent(),Is.True);yield return Reload();
                Assert.That(Inventory.GetCount(material),Is.EqualTo(4));Assert.That(Loop.MissionRuntime.HasObjective(objective.id),Is.True);
                runtime=Loop.MissionRuntime;survey=Cabin.GetComponent<PhotoSurveyZone>();catcher=Cabin.GetComponent<CreatureCatcher>();
                poi=survey.FindPoi(zone=="Zone02"?"zone02-l2":"zone03-l2");
                var collect=runtime.FindObjective(poi.id,MissionObjectiveType.Collect);
                Cabin.Navigation.RestoreVoyage(survey.ContactPosition(poi),0,survey.DepthFor(poi),0);
                Assert.That(catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.Started));CaptureMinigamePlayModeTests.Win(catcher.minigame);
                Assert.That(Inventory.GetCount(collect.targetId),Is.EqualTo(1));Assert.That(catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.Empty));
            }
        }
    }
}

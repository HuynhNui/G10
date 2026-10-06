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
            TutorialTestSave.SeedReturningPlayer();
            yield return Load(5);
        }
        private IEnumerator Load(int days)
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore",LoadSceneMode.Single);
            loop=Object.FindAnyObjectByType<ExpeditionLoop>();
            typeof(ExpeditionLoop).GetField("totalExpeditionDays", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(loop, days == 2 ? 2 : 15);
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
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"Temp/expedition-status-" + Guid.NewGuid().ToString("N") + ".png");
            var status=screen.GetComponentInChildren<ShipStatusView>(true);
            Assert.That(status.DisplayedText,Does.Contain("DAY 01"));
            Assert.That(status.DisplayedText,Does.Contain("CAPTURE ATTEMPTS"));
            Vector2 dock=cabin.Navigation.Position;
            Vector2 restPosition=dock+Vector2.right*100;
            cabin.Navigation.RestoreVoyage(restPosition,90,230,100);
            ui.OpenRest();yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"Temp/expedition-rest-" + Guid.NewGuid().ToString("N") + ".png");
            Assert.That(loop.CanRest,Is.True,"Rest must be available away from authored rest areas.");
            Assert.That(ui.RestText,Does.Contain("bất kỳ vị trí nào"));
            Assert.That(ui.RestText,Does.Not.Contain("RETURN TO REST AREA"));
            var catcher=cabin.GetComponent<CreatureCatcher>();var survey=catcher.survey;
            Assert.That(catcher.inventory.TryAdd(survey.creatureId,catcher.itemName,catcher.itemIcon),Is.True);
            survey.creaturePresent=false;
            var photos=cabin.GetComponent<PhotoCaptureService>();
            Assert.That(photos.Capture(),Is.Not.Null);
            string originalPhoto=photos.Photos[0].Id;
            ui.RequestRest();Assert.That(loop.Day,Is.EqualTo(1));ui.ConfirmRest();
            Assert.That(loop.Day, Is.EqualTo(1), "Day changes only after fade to black.");
            ui.ConfirmRest();
            while(SceneFlowController.Instance.IsTransitioning) yield return null;
            Assert.That(loop.Day,Is.EqualTo(2));Assert.That(loop.Journal.Count,Is.EqualTo(1));
            Assert.That(loop.Journal[0].photos,Is.EqualTo(1));Assert.That(loop.Journal[0].captures,Is.EqualTo(1));
            Assert.That(loop.Journal[0].distance,Is.EqualTo(100));
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"Temp/expedition-journal-" + Guid.NewGuid().ToString("N") + ".png");
            Assert.That(catcher.inventory.Items.Count,Is.EqualTo(1));
            Assert.That(loop.Rest(),Is.True);Assert.That(loop.Day,Is.EqualTo(3));
            yield return new WaitForSecondsRealtime(.65f);
            photos.Capture();
            cabin.Navigation.RestoreVoyage(dock+Vector2.up*20,180,100,300);
            ui.OpenJournal(); // Latest day must confirm before changing state.
            ui.RequestRestore();Assert.That(ui.JournalText,Does.Contain("Restoring this day will erase all progress made afterward."));
            ui.CancelConfirmation();Assert.That(loop.Day,Is.EqualTo(3));
            Assert.That(loop.RestoreDay(1),Is.True);
            Assert.That(loop.Day,Is.EqualTo(1));Assert.That(loop.Journal.Count,Is.EqualTo(1));
            Assert.That(cabin.Navigation.Position,Is.EqualTo(restPosition));Assert.That(cabin.Navigation.Heading,Is.EqualTo(90));
            Assert.That(cabin.Navigation.Depth,Is.EqualTo(230));
            Assert.That(photos.Photos.Count,Is.EqualTo(1));Assert.That(photos.Photos[0].Id,Is.EqualTo(originalPhoto));
            yield return Load(5);
            Assert.That(loop.Day,Is.EqualTo(1));Assert.That(loop.Journal.Count,Is.EqualTo(1));
            Assert.That(cabin.GetComponent<CreatureInventory>().Items.Count,Is.EqualTo(1));
            Assert.That(cabin.GetComponent<PhotoCaptureService>().Photos[0].Id,Is.EqualTo(originalPhoto));
            Assert.That(cabin.GetComponent<PhotoCaptureService>().Photos.Count,Is.EqualTo(1),"Future archive files must not reappear.");
            Assert.That(loop.Rest(),Is.True);Assert.That(loop.Journal.Count,Is.EqualTo(1),"Resting a restored day replaces its checkpoint.");
        }
        [UnityTest] public IEnumerator DailyCreatureSpawnsStayInsideMissionCellsAndPersistPrivately()
        {
            var survey = cabin.GetComponent<PhotoSurveyZone>();
            Assert.That(loop.SaveCurrent(), Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var dayOne, out _), Is.True);
            var zoneOne = dayOne.current.zones.Find(zone => zone.zone == "Zone01");
            Assert.That(zoneOne.creatureSpawns, Has.Count.EqualTo(survey.locations.Length));
            foreach (var spawn in zoneOne.creatureSpawns)
            {
                var poi = Array.Find(survey.locations, value => value.id == spawn.poiId);
                Assert.That(poi, Is.Not.Null);
                Vector2 delta = spawn.coordinate - poi.mapPosition;
                Assert.That(Mathf.Abs(delta.x), Is.LessThanOrEqualTo(ZoneNavigation.ChartCellSize * .5f));
                Assert.That(Mathf.Abs(delta.y), Is.LessThanOrEqualTo(ZoneNavigation.ChartCellSize * .5f));
                Assert.That(spawn.day, Is.EqualTo(1));
            }
            Vector2 firstDay = survey.CreatureMapPosition;
            survey.Story.Restore((int)(G10.Prototype.Missions.ZoneOneStory.Progress.RadarOne |
                G10.Prototype.Missions.ZoneOneStory.Progress.PhotoOne));
            var secondSite = zoneOne.creatureSpawns.Find(spawn => spawn.poiId == survey.TargetPoi.id);
            Assert.That(survey.CreatureMapPosition, Is.EqualTo(secondSite.coordinate),
                "Changing mission location immediately selects that location's saved daily spawn.");
            survey.Story.Restore(0);
            Assert.That(survey.CreatureMapPosition, Is.EqualTo(firstDay));
            Assert.That(loop.Rest(), Is.True);
            Assert.That(loop.Day, Is.EqualTo(2));
            Vector2 secondDay = survey.CreatureMapPosition;
            Assert.That(secondDay, Is.Not.EqualTo(firstDay));
            Assert.That(loop.Journal[0].checkpoint.zones.Find(zone => zone.zone == "Zone01").creatureSpawns[0].day, Is.EqualTo(1));
            Assert.That(ExpeditionSaveStore.TryRead(out var dayTwo, out _), Is.True);
            Assert.That(dayTwo.current.zones.Find(zone => zone.zone == "Zone01").creatureSpawns[0].day, Is.EqualTo(2));

            cabin.OpenComputer();
            var journal = cabin.GetComponentInChildren<ExpeditionComputerView>(true);
            journal.OpenJournal();
            Assert.That(journal.JournalText, Does.Not.Contain("CREATURE X"));
            yield return Load(5);
            Assert.That(cabin.GetComponent<PhotoSurveyZone>().CreatureMapPosition, Is.EqualTo(secondDay));
        }
        [UnityTest] public IEnumerator DeadlineFailureLocksGameplayAndJournalRestorationReleasesIt()
        {
            // Initial binding now persists day 1 immediately; explicitly start a fresh test voyage with a shorter deadline.
            ExpeditionSaveStore.ResetGameProgress();
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
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"Temp/expedition-failure-" + Guid.NewGuid().ToString("N") + ".png");
            Assert.That(screen.GetComponent<ExpeditionComputerView>().JournalText, Does.Contain("MISSION FAILED"));
            Assert.That(loop.RestoreDay(1),Is.True);Assert.That(loop.Failed,Is.False);
            Assert.That(cabin.Navigation.ExpeditionBlocked,Is.False);Assert.That(loop.Journal.Count,Is.EqualTo(1));
            cabin.ClosePanel();Assert.That(cabin.Panels.IsPanelOpen,Is.False);
        }
        [UnityTest] public IEnumerator CompletedObjectivesAndZoneTransitionDoNotGrantExtraDays()
        {
            Assert.That(loop.PrepareZone("Zone02"),Is.False);
            CompleteZoneOneMission();
            loop.EvaluateDeadline();
            Assert.That(loop.RequiredObjectivesComplete,Is.True);
            Assert.That(loop.Rest(),Is.True); // Completion was on day 1; waiting does not erase earned days.
            loop.maxCarryOverDays=2;
            cabin.GetComponent<ExpeditionProgression>().Evaluate();
            Assert.That(loop.PrepareZone("Zone02"),Is.True);
            Assert.That(loop.Zone,Is.EqualTo("Zone02"));Assert.That(loop.Deadline,Is.EqualTo(15));
            Assert.That(loop.Day, Is.EqualTo(2)); Assert.That(loop.DaysLeft, Is.EqualTo(14));
            Assert.That(loop.PrepareZone("Zone02"),Is.True);Assert.That(loop.Deadline,Is.EqualTo(15));
            Assert.That(ExpeditionSaveStore.TryRead(out var saved,out _),Is.True);
            Assert.That(saved.current.zone,Is.EqualTo("Zone02"));
            Assert.That(saved.current.zones[0].missionProgress.completedObjectives, Does.Contain("Z1_GATE_INSTALL_PRESSURE_HULL"));
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
        [Test] public void LegacyVersionOneSaveWithoutCreatureSpawnsLoadsWithSafeDefaults()
        {
            var legacy = new ExpeditionSave();
            legacy.current.zones.Add(new ExpeditionZoneState { zone="Zone01", deadline=5 });
            string json = JsonUtility.ToJson(legacy).Replace(",\"creatureSpawns\":[]", string.Empty);
            Directory.CreateDirectory(Path.GetDirectoryName(ExpeditionSaveStore.SavePath));
            File.WriteAllText(ExpeditionSaveStore.SavePath, json);
            Assert.That(ExpeditionSaveStore.TryRead(out var restored, out _), Is.True);
            Assert.That(restored.current.zones[0].creatureSpawns, Is.Not.Null.And.Empty);
        }
        private static void AssertTextFits(UnityEngine.UI.Text label)
        { Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height),label.name+" must not clip information."); }
        [UnityTest] public IEnumerator ZoneTravelAndCrossZoneRollbackKeepTheSingleTimeline()
        {
            if(SceneFlowController.Instance==null)new GameObject("TestSceneFlow").AddComponent<SceneFlowController>();
            var catcher=cabin.GetComponent<CreatureCatcher>();
            catcher.inventory.TryAdd(catcher.survey.creatureId,catcher.itemName,catcher.itemIcon);
            CompleteZoneOneMission();
            Assert.That(loop.Rest(),Is.True);
            cabin.GetComponent<ExpeditionProgression>().Evaluate();
            cabin.Navigation.RestoreVoyage(loop.ActiveMap.exitArea.mapPosition, 0, 230, 0);
            SceneFlowController.Instance.LoadZone("Zone02");
            while(SceneFlowController.Instance.IsTransitioning)yield return null;
            Assert.That(SceneManager.GetSceneByName("Zone01").isLoaded,Is.True);
            Assert.That(SceneManager.GetSceneByName("Zone02").isLoaded,Is.False);
            Assert.That(loop.Zone,Is.EqualTo("Zone02"));Assert.That(loop.Deadline,Is.EqualTo(15));
            Assert.That(loop.RestoreDay(1),Is.True);
            while(SceneFlowController.Instance.IsTransitioning)yield return null;
            yield return null;yield return null;
            cabin=Object.FindAnyObjectByType<CabinStationView>();
            Assert.That(loop.Zone,Is.EqualTo("Zone01"));Assert.That(loop.Day,Is.EqualTo(1));
            Assert.That(loop.Deadline,Is.EqualTo(15));Assert.That(loop.RequiredObjectivesComplete,Is.True);
            Assert.That(cabin.GetComponent<CreatureInventory>().Items.Count,Is.EqualTo(2));
            Assert.That(cabin.GetComponent<CreatureInventory>().Contains(ZoneOneStory.EmmaBlueprint), Is.True);
            Assert.That(loop.Journal.Count,Is.EqualTo(1));Assert.That(loop.CanRest,Is.True);
        }

        private void CompleteZoneOneMission()
        {
            var story = cabin.GetComponent<ZoneOneStory>();
            story.RecordObjective("zone01-north", MissionObjectiveType.Photograph, "Z1_Creature_02");
            story.RecordObjective("zone01-left", MissionObjectiveType.Photograph, "Z1_Creature_01");
            story.RecordObjective("zone01-east", MissionObjectiveType.Collect, ZoneOneStory.EmmaBlueprint);
            Assert.That(story.InstallHull(), Is.True);
        }
    }
}

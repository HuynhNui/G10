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
    public sealed class VesselDeathPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "VesselDeath-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return null; flow = SceneFlowController.Instance;
            yield return WaitTransition(); flow.StartNewGame(); yield return WaitTransition();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (flow != null) Object.Destroy(flow.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        private ExpeditionSave Read()
        { Assert.That(ExpeditionSaveStore.TryRead(out var save, out var error), Is.True, error); return save; }
        private IEnumerator WaitTransition()
        {
            yield return null;
            float timeout = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty);
        }
        private IEnumerator Die()
        {
            float started = Time.realtimeSinceStartup;
            Cabin.Navigation.Ship.HitTerrain(10000);
            Assert.That(Loop.CanRecover, Is.False);
            Assert.That(Loop.Rest(), Is.False);
            yield return null;
            Assert.That(Loop.IsDeathInProgress, Is.True);
            Assert.That(Cabin.Navigation.ExpeditionBlocked, Is.True);
            float timeout = Time.realtimeSinceStartup + 15;
            while (Loop.IsDeathInProgress && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(Loop.IsDeathInProgress, Is.False, Loop.LastError);
            Assert.That(Time.realtimeSinceStartup - started, Is.GreaterThanOrEqualTo(1f));
            Assert.That(Cabin.Navigation.ExpeditionBlocked, Is.False);
            Assert.That(Cabin.Panels.IsPanelOpen, Is.False, "Hull rollback must not open deadline-failure UI.");
        }
        [UnityTest] public IEnumerator HullDeathRevertsEverySameDayChangeWithoutAdvancingDay()
        {
            var start = Read().dayStart;
            Assert.That(start, Is.Not.Null);
            var nav = Cabin.Navigation;
            nav.RestoreVoyage(nav.Position + Vector2.right * 15, 180, 150, 88);
            var runtime = Loop.MissionRuntime;
            Assert.That(runtime.RecordObjective("zone01-east", MissionObjectiveType.Collect, ZoneOneStory.EmmaBlueprint), Is.True);
            Assert.That(Cabin.GetComponent<PhotoCaptureService>().Capture(), Is.Not.Null);
            Loop.CurrentProgress.hiddenRouteUnlocked = true;
            nav.Ship.ApplyUpgrade(ShipUpgrade.Speed, 2);
            nav.Ship.ConsumeMovement(8);
            Assert.That(Loop.SaveCurrent(), Is.True);
            Assert.That(JsonUtility.ToJson(Read().dayStart), Is.EqualTo(JsonUtility.ToJson(start)));
            yield return Die();
            Assert.That(Loop.Day, Is.EqualTo(start.day));
            Assert.That(Loop.Zone, Is.EqualTo(start.zone));
            Assert.That(JsonUtility.ToJson(nav.Ship.Export()), Is.EqualTo(JsonUtility.ToJson(start.ship)));
            var zone = start.zones.Find(z => z.zone == start.zone);
            Assert.That(nav.Position, Is.EqualTo(zone.position)); Assert.That(nav.Heading, Is.EqualTo(zone.heading));
            Assert.That(nav.Depth, Is.EqualTo(zone.depth)); Assert.That(nav.DistanceTravelled, Is.EqualTo(zone.distance));
            Assert.That(Cabin.GetComponent<CreatureInventory>().Items, Is.Empty);
            Assert.That(Cabin.GetComponent<PhotoCaptureService>().Photos, Is.Empty);
            Assert.That(Loop.MissionRuntime.CompletedCount, Is.Zero);
            Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.False);
            Assert.That(JsonUtility.ToJson(Read().current), Is.EqualTo(JsonUtility.ToJson(start)));
            Assert.That(Loop.SaveCurrent(), Is.True);
            var spawns = Read().current.zones[0].creatureSpawns;
            Assert.That(spawns.Count, Is.EqualTo(zone.creatureSpawns.Count));
            for (int i = 0; i < spawns.Count; i++)
                Assert.That(JsonUtility.ToJson(spawns[i]), Is.EqualTo(JsonUtility.ToJson(zone.creatureSpawns[i])));
        }
        [UnityTest] public IEnumerator EmptyEnergyUsesRecoveryAndRestCreatesNewDayStart()
        {
            Cabin.Navigation.Ship.ConsumeMovement(10000);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Loop.IsDeathInProgress, Is.False);
            Assert.That(Loop.Day, Is.EqualTo(1)); Assert.That(Loop.CanRecover, Is.True);
            Assert.That(Loop.RecoverShip(), Is.True);
            var start = Read().dayStart;
            Assert.That(start.day, Is.EqualTo(2)); Assert.That(start.ship.energy, Is.EqualTo(start.ship.energyCapacity));
            Assert.That(start.zones[0].creatureSpawns[0].day, Is.EqualTo(2));
            yield return Die();
            Assert.That(Loop.Day, Is.EqualTo(2));
            Assert.That(JsonUtility.ToJson(Cabin.Navigation.Ship.Export()), Is.EqualTo(JsonUtility.ToJson(start.ship)));
        }
        [UnityTest] public IEnumerator ContinueAndCrossZoneDeathKeepOriginalDayStart()
        {
            var start = Read().dayStart;
            var story = Cabin.GetComponent<ZoneOneStory>();
            story.RecordObjective("zone01-north", MissionObjectiveType.Photograph, "Z1_Creature_02");
            story.RecordObjective("zone01-left", MissionObjectiveType.Photograph, "Z1_Creature_01");
            story.RecordObjective("zone01-east", MissionObjectiveType.Collect, ZoneOneStory.EmmaBlueprint);
            Assert.That(story.InstallHull(), Is.True);
            Cabin.GetComponent<ExpeditionProgression>().Evaluate();
            Cabin.Navigation.RestoreVoyage(Loop.ActiveMap.exitArea.mapPosition, 0, 230, 0);
            flow.LoadZone("Zone02"); yield return WaitTransition();
            Assert.That(Loop.Zone, Is.EqualTo("Zone02"));
            flow.LoadMainMenu(); yield return WaitTransition();
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(JsonUtility.ToJson(Read().dayStart), Is.EqualTo(JsonUtility.ToJson(start)));
            yield return Die();
            Assert.That(Loop.Zone, Is.EqualTo("Zone01")); Assert.That(Loop.Day, Is.EqualTo(1));
            Assert.That(Cabin.Navigation.Ship.HullCapacity, Is.EqualTo(100));
            Assert.That(Loop.MissionRuntime.CompletedCount, Is.Zero);
            Assert.That(Read().current.zones.Count, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RestSnapshotIsDeepCopiedAndPersistsThroughContinue()
        {
            Assert.That(Loop.Rest(), Is.True);
            var start = Read().dayStart;
            Cabin.Navigation.RestoreVoyage(Cabin.Navigation.Position, 240, 140, 50);
            Assert.That(Loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return WaitTransition(); flow.ContinueGame(); yield return WaitTransition();
            Assert.That(JsonUtility.ToJson(Read().dayStart), Is.EqualTo(JsonUtility.ToJson(start)));
            yield return Die();
            Assert.That(Cabin.Navigation.Heading, Is.EqualTo(start.zones[0].heading));
            Assert.That(Loop.Journal.Count, Is.EqualTo(1));
        }
        [TestCase(1)] [TestCase(2)] public void LegacySavesRemainReadableWithoutInventingDayStart(int version)
        {
            var legacy = Read(); legacy.version = version; legacy.dayStart = null;
            ExpeditionSaveStore.Write(legacy);
            var restored = Read();
            Assert.That(restored.version, Is.EqualTo(3)); Assert.That(restored.dayStart, Is.Null);
        }
        [UnityTest] public IEnumerator DeathCancelsActiveAndPendingCabinCapture()
        {
            var survey = Cabin.GetComponent<PhotoSurveyZone>();
            Cabin.Navigation.RestoreVoyage(survey.ContactPosition(survey.FindPoi("zone01-east")), 0, survey.targetDepth, 0);
            var catcher = Cabin.GetComponent<CreatureCatcher>();
            Cabin.OpenCapture(); yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(catcher.minigame.IsActive, Is.True);
            yield return Die();
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Cancelled));
            Assert.That(catcher.minigame.IsActive, Is.False);
            Assert.That(Cabin.IsDirectInteractionActive, Is.False);
            Cabin.OpenCapture();
            yield return Die();
            Assert.That(catcher.minigame.IsActive, Is.False);
            Assert.That(Cabin.IsDirectInteractionActive, Is.False);
            Assert.That(Cabin.Navigation.Ship.Captures, Is.EqualTo(5));
        }
        [UnityTest] public IEnumerator LegacyDeathReturnsToMenuWithoutInventingCheckpointOrOverwritingSave()
        {
            flow.LoadMainMenu(); yield return WaitTransition();
            var legacy = Read(); legacy.version = 2; legacy.hasDayStart = false; legacy.dayStart = null;
            ExpeditionSaveStore.Write(legacy);
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Read().hasDayStart, Is.False);
            string previous = File.ReadAllText(ExpeditionSaveStore.SavePath);
            Cabin.Navigation.Ship.HitTerrain(1000);
            yield return null;
            float timeout = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < timeout) yield return null;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(flow.LastError, Does.Contain("No day-start checkpoint"));
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath), Is.EqualTo(previous));
            Assert.That(Read().current.day, Is.EqualTo(1));
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Loop.Rest(), Is.True);
            Assert.That(Read().hasDayStart, Is.True);
            yield return Die();
            Assert.That(Loop.Day, Is.EqualTo(2));
        }
    }
}

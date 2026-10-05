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
    public sealed class ExpeditionNewGamePlayModeTests
    {
        private string folder;
        private string previousSavePath;
        private string previousPhotoPath;
        private ExpeditionLoop loop;
        private CabinStationView cabin;

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ExpeditionNewGame-" + Guid.NewGuid().ToString("N"));
            previousSavePath = ExpeditionSaveStore.PathOverride;
            previousPhotoPath = PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            yield return LoadCabin();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            ExpeditionSaveStore.PathOverride = previousSavePath;
            PhotoCaptureService.ArchivePathOverride = previousPhotoPath;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [UnityTest] public IEnumerator RuntimeResetClearsProgressionAndCannotSaveThePreviousVoyageOverFreshState()
        {
            var inventory = cabin.GetComponent<CreatureInventory>();
            var photos = cabin.GetComponent<PhotoCaptureService>();
            var story = cabin.GetComponent<ZoneOneStory>();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError);
            story.RecordObjective("zone01-north", MissionObjectiveType.Photograph, "Z1_Creature_02");
            inventory.TryAdd(ZoneOneStory.EmmaBlueprint, "Test cargo", null);
            Assert.That(photos.Capture(), Is.Not.Null);
            cabin.Navigation.Ship.ApplyUpgrade(ShipUpgrade.Speed, 3);
            Assert.That(loop.Rest(), Is.True);
            cabin.Navigation.RestoreVoyage(loop.ActiveMap.entryPosition + new Vector2(100, 50), 90, 100, 500);
            loop.CurrentProgress.exitUnlocked = true;
            loop.CurrentProgress.hiddenRouteUnlocked = true;
            Assert.That(loop.RecordEnding("NormalEnding"), Is.True);
            Assert.That(loop.ResetGameProgress(), Is.True, loop.LastError);
            Assert.That(loop.SaveCurrent(), Is.False, "The old runtime must not overwrite the new timeline before rebinding.");
            Assert.That(ExpeditionSaveStore.TryRead(out var clean, out _), Is.True);
            Assert.That(clean.current.zones, Is.Empty);
            Assert.That(clean.current.inventory, Is.Empty);

            loop.RebindCurrentZone();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError);
            Assert.That(loop.Zone, Is.EqualTo("Zone01"));
            Assert.That(loop.Day, Is.EqualTo(1));
            Assert.That(loop.Journal, Is.Empty);
            Assert.That(loop.MissionRuntime.CompletedCount, Is.Zero);
            Assert.That(loop.MissionRuntime.ProgressState.IsEmpty, Is.True);
            Assert.That(loop.RequiredObjectivesComplete, Is.False);
            Assert.That(loop.CurrentProgress.exitUnlocked, Is.False);
            Assert.That(loop.CurrentProgress.hiddenRouteUnlocked, Is.False);
            Assert.That(loop.EndingReached, Is.Null.Or.Empty);
            Assert.That(inventory.Items, Is.Empty);
            Assert.That(photos.Photos, Is.Empty);
            Assert.That(photos.TotalPhotosTaken, Is.Zero);
            Assert.That(cabin.Navigation.Position, Is.EqualTo(loop.ActiveMap.entryPosition));
            Assert.That(cabin.Navigation.Ship.Speed, Is.EqualTo(cabin.Navigation.CreateInitialShipState().speed));
            Assert.That(cabin.Navigation.Ship.Radar, Is.EqualTo(cabin.Navigation.Ship.RadarCapacity));
            Assert.That(cabin.Navigation.Ship.Photos, Is.EqualTo(cabin.Navigation.Ship.PhotoCapacity));
            Assert.That(cabin.Navigation.Ship.Captures, Is.EqualTo(cabin.Navigation.Ship.CaptureCapacity));
            Assert.That(loop.SaveCurrent(), Is.True);

            yield return LoadCabin();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError);
            Assert.That(cabin.GetComponent<PhotoCaptureService>().Photos, Is.Empty,
                "Loose photo files left by the previous playthrough must not become new-game progression.");
            Assert.That(loop.MissionRuntime.CompletedCount, Is.Zero);
            Assert.That(cabin.GetComponent<CreatureInventory>().Items, Is.Empty);
        }

        private IEnumerator LoadCabin()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            cabin = Object.FindAnyObjectByType<CabinStationView>();
        }
    }
}

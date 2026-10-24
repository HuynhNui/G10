using System;
using System.Collections;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    /// <summary>Real quick shutter, Photo Lab SEND, capture minigame and durable assembly authority.</summary>
    public sealed class ZoneThreeMissionTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ZoneOneStory story;
        private ExpeditionLoop loop;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private CreatureCatcher Catcher => cabin.GetComponent<CreatureCatcher>();

        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "ZoneThreeMission-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return Wait();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(3); menu.StartRun(); yield return Wait();
            Bind(); cabin.ClosePanel();
        }

        private void Bind()
        {
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone03"));
        }

        private void Place(string poiId)
        {
            var poi = story.survey.FindPoi(poiId); Assert.That(poi, Is.Not.Null);
            cabin.Navigation.Ship.Refill();
            cabin.Navigation.RestoreVoyage(story.survey.ContactPosition(poi) + Vector2.down * 14, 0,
                story.survey.DepthFor(poi), cabin.Navigation.DistanceTravelled);
        }

        private IEnumerator Survey(string poiId, bool capture)
        {
            // Respect the real camera's .6-second cooldown between successive sites.
            yield return new WaitForSeconds(.65f);
            Place(poiId); cabin.ClosePanel(); int before = photos.Photos.Count;
            string cargo = string.Join(";", story.inventory.Items.Select(item => item.Id + ":" + item.Quantity));
            cabin.OpenCamera();
            float deadline = Time.realtimeSinceStartup + 4;
            while ((photos.Photos.Count == before || cabin.IsDirectInteractionActive) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(cabin.IsDirectInteractionActive, Is.False, "Quick shutter must finish before opening Photo Lab.");
            Assert.That(photos.Photos.Count, Is.EqualTo(before + 1)); var photo = photos.Photos.Last();
            Assert.That(photo.MissionZoneId, Is.EqualTo("Zone03"), photo.Result);
            Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.False);
            Assert.That(string.Join(";", story.inventory.Items.Select(item => item.Id + ":" + item.Quantity)), Is.EqualTo(cargo), "PHOTO never grants capture cargo.");
            Assert.That(Catcher.TryCapture(), Is.EqualTo(capture ? CreatureCatcher.Result.PhotoRequired : CreatureCatcher.Result.NoTarget));
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            var lab = computer.GetComponentInChildren<PhotoLabView>(true);
            Assert.That(lab.sendButton.interactable, Is.True); lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(photo.IsMissionPhoto, Is.True); Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.True);
            Assert.That(photos.SubmitPhoto(photo), Is.False, "SEND is idempotent.");
            cabin.ClosePanel(); Place(poiId);
            if (capture)
            {
                Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
                CaptureMinigamePlayModeTests.Win(Catcher.minigame);
                Assert.That(Catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            }
            else Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.NoTarget));
        }

        private IEnumerator Fieldwork()
        {
            yield return Survey("zone03-l1", true); yield return Survey("zone03-l2", false); yield return Survey("zone03-l4", true);
            Assert.That(story.MainLocationsComplete, Is.True); Assert.That(story.MainObjectivesComplete, Is.False);
        }

        [UnityTest] public IEnumerator ThreeSitesUseManualSendAndBothCapturedMaterialsAreConsumedOnlyByCraft()
        {
            CollectionAssert.AreEqual(new[] { "Z3_L1", "Z3_L2", "Z3_L3" }, story.config.locations.Select(location => location.id));
            CollectionAssert.AreEqual(new[] { "zone03-l1", "zone03-l2", "zone03-l4" }, loop.ActiveMap.locations.Select(poi => poi.id));
            Assert.That(story.survey.FindPoi("zone03-l3"), Is.Null);
            Assert.That(cabin.Navigation.Ship.MaximumDepth, Is.EqualTo(750));
            yield return Fieldwork();
            Assert.That(story.inventory.GetCount("Z3_Creature_01"), Is.EqualTo(1));
            Assert.That(story.inventory.GetCount("Z3_Creature_03"), Is.EqualTo(1));
            Assert.That(story.HasItem("Z3_ITEM_MECHANICAL_MODULE"), Is.False);
            cabin.OpenComputer(); computer.OpenUpgrade();
            var upgrades = computer.GetComponentInChildren<SubmarineUpgradeUIController>(true);
            var module = upgrades.GetComponentsInChildren<UpgradeEntryConfig>(true).Single(entry => entry.UpgradeId == "ExpeditionModule");
            upgrades.Select(module); yield return null;
            var rows = upgrades.GetComponentsInChildren<UpgradeMaterialRequirementView>(true).Where(row => row.gameObject.activeInHierarchy).ToArray();
            Assert.That(rows, Has.Length.EqualTo(2), "Both canonical crafting costs must be visible in the real Upgrade app.");
            Assert.That(rows.SelectMany(row => row.GetComponentsInChildren<TMPro.TMP_Text>(true)).Any(text => text.text.Contains("Explosive material")), Is.True);
            Assert.That(rows.SelectMany(row => row.GetComponentsInChildren<TMPro.TMP_Text>(true)).Any(text => text.text.Contains("Detonator material")), Is.True);
            var actions = new List<MissionObjectiveType>(); story.ZoneThreeActionCommitted += actions.Add;
            Assert.That(story.ApplyProgressionAction(), Is.True);
            Assert.That(story.HasObjective("Z3_GATE_CRAFT_ROCK_BREAKER"), Is.True);
            Assert.That(story.inventory.GetCount("Z3_Creature_01"), Is.Zero);
            Assert.That(story.inventory.GetCount("Z3_Creature_03"), Is.Zero);
            Assert.That(story.ApplyProgressionAction(), Is.True, "INSTALL consumes no material a second time.");
            Assert.That(story.RockBreakerInstalled, Is.True); Assert.That(story.ApplyProgressionAction(), Is.False, "BREAK requires the real rock approach/depth.");
            CollectionAssert.AreEqual(new[] { MissionObjectiveType.Craft, MissionObjectiveType.InstallUpgrade }, actions);
            Assert.That(loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return Wait(); flow.ContinueDebugGame(); yield return Wait(); Bind();
            Assert.That(story.RockBreakerInstalled, Is.True); Assert.That(story.HasObjective("Z3_L3_CAPTURE"), Is.True);
            Assert.That(story.inventory.GetCount("Z3_Creature_03"), Is.Zero);
            Assert.That(cabin.GetComponentInChildren<PhotoCameraView>(true).gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest] public IEnumerator FailedCraftPublicationRestoresCargoReceiptsAndPresentationNotification()
        {
            yield return Fieldwork(); string progress = JsonUtility.ToJson(story.ExportProgress());
            string valid = ExpeditionSaveStore.PathOverride, publishedPath = ExpeditionSaveStore.SavePath;
            Assert.That(File.Exists(publishedPath), Is.True);
            byte[] published = File.ReadAllBytes(publishedPath);
            int actions = 0; story.ZoneThreeActionCommitted += _ => actions++;
            // Debug Start appends .debug to PathOverride. Block the parent with the
            // existing timeline file so either selected timeline must fail to publish.
            ExpeditionSaveStore.PathOverride = Path.Combine(publishedPath, "blocked-craft.json");
            try
            {
                Assert.That(story.ApplyProgressionAction(), Is.False);
                Assert.That(JsonUtility.ToJson(story.ExportProgress()), Is.EqualTo(progress));
                Assert.That(story.inventory.GetCount("Z3_Creature_01"), Is.EqualTo(1));
                Assert.That(story.inventory.GetCount("Z3_Creature_03"), Is.EqualTo(1)); Assert.That(actions, Is.Zero);
            }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            CollectionAssert.AreEqual(published, File.ReadAllBytes(publishedPath), "Failed publication leaves the prior durable timeline unchanged.");
            Assert.That(story.ApplyProgressionAction(), Is.True); Assert.That(actions, Is.EqualTo(1));
            Assert.That(ExpeditionSaveStore.TryRead(out var save, out var error), Is.True, error);
            Assert.That(save.current.zones.Single(zone => zone.zone == "Zone03").missionProgress.completedObjectives,
                Does.Contain("Z3_GATE_CRAFT_ROCK_BREAKER"));
            Assert.That(save.current.inventory.Any(item => item.id == "Z3_Creature_01" || item.id == "Z3_Creature_03"), Is.False);
        }

        private IEnumerator Wait()
        {
            yield return null; float until = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; PauseMenuController.Instance?.Resume();
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject); yield return null;
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}

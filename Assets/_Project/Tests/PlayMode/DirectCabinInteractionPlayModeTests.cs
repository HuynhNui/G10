using System;
using System.Collections;
using System.IO;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class DirectCabinInteractionPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private CabinStationView cabin;
        private CreatureCatcher catcher;
        private PhotoCaptureService camera;
        private UnityEngine.UI.RawImage art;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "DirectCabin-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null; yield return null;
            cabin = Object.FindAnyObjectByType<CabinStationView>();
            catcher = cabin.GetComponent<CreatureCatcher>(); camera = cabin.GetComponent<PhotoCaptureService>();
            art = cabin.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Single(image => image.name == "CabinArt");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        private void ReadyCapture()
        {
            var survey = catcher.survey;
            survey.Story = null; survey.MissionRuntime = null;
            survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);
            cabin.Navigation.RestoreVoyage(survey.ContactPosition(survey.TargetPoi), 0, survey.targetDepth, 0);
        }
        private void AssertOldPanelsClosed()
        {
            Assert.That(cabin.GetComponentInChildren<PhotoCameraView>(true).gameObject.activeSelf, Is.False);
            Assert.That(cabin.GetComponentInChildren<CreatureCaptureView>(true).gameObject.activeSelf, Is.False);
        }
        [UnityTest] public IEnumerator CaptureWaitsOneRealtimeSecondRejectsDuplicatesAndRestoresAfterCancel()
        {
            ReadyCapture(); int calls = 0;
            catcher.CaptureResolved += _ => calls++;
            int charges = cabin.Navigation.Ship.Captures;
            Time.timeScale = 0;
            cabin.OpenCapture(); cabin.OpenCapture(); cabin.OpenCamera(); cabin.OpenNavigation();
            Assert.That(art.texture.name, Is.EqualTo("Cabin_Capture"));
            Assert.That(cabin.Panels.IsPanelOpen, Is.False); AssertOldPanelsClosed();
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(calls, Is.Zero); Assert.That(catcher.minigame.IsActive, Is.False);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(calls, Is.EqualTo(1)); Assert.That(catcher.minigame.IsActive, Is.True);
            Assert.That(cabin.Navigation.Ship.Captures, Is.EqualTo(charges - 1));
            Assert.That(art.texture.name, Is.EqualTo("Cabin_Capture"));
            catcher.minigame.Cancel();
            Assert.That(art.texture, Is.SameAs(cabin.CabinArt));
            Assert.That(cabin.Panels.IsPanelOpen, Is.False); Assert.That(cabin.IsDirectInteractionActive, Is.False);
        }
        [UnityTest] public IEnumerator CaptureInvalidRestoresImmediatelyWithoutModalOrCharge()
        {
            cabin.Navigation.RestoreVoyage(Vector2.zero, 0, 230, 0);
            int charges = cabin.Navigation.Ship.Captures;
            cabin.OpenCapture(); yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Empty));
            Assert.That(cabin.Navigation.Ship.Captures, Is.EqualTo(charges));
            Assert.That(cabin.Panels.IsModalOpen, Is.False); AssertOldPanelsClosed();
            Assert.That(art.texture, Is.SameAs(cabin.CabinArt));
        }
        [UnityTest] public IEnumerator CaptureSuccessKeepsExistingResolution()
        {
            ReadyCapture(); cabin.OpenCapture(); yield return new WaitForSecondsRealtime(1.1f);
            CaptureMinigamePlayModeTests.Win(catcher.minigame);
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(catcher.inventory.Items.Count, Is.EqualTo(1));
            Assert.That(art.texture, Is.SameAs(cabin.CabinArt));
            Assert.That(cabin.Panels.IsPanelOpen, Is.False);
        }
        [UnityTest] public IEnumerator PhotoRendersArtOnceKeepsArchiveCooldownAndPhotoLab()
        {
            int charges = cabin.Navigation.Ship.Photos;
            cabin.OpenCamera(); cabin.OpenCamera(); cabin.OpenCapture();
            Assert.That(art.texture.name, Is.EqualTo("Canbin_Photo"));
            Assert.That(camera.TotalPhotosTaken, Is.Zero); AssertOldPanelsClosed();
            yield return null; yield return null;
            Assert.That(camera.TotalPhotosTaken, Is.EqualTo(1));
            Assert.That(cabin.Navigation.Ship.Photos, Is.EqualTo(charges - 1));
            Assert.That(File.Exists(Path.Combine(camera.ArchivePath, camera.Photos[0].Id + ".png")), Is.True);
            Assert.That(art.texture, Is.SameAs(cabin.CabinArt)); AssertOldPanelsClosed();
            cabin.OpenCamera(); yield return null; yield return null;
            Assert.That(camera.TotalPhotosTaken, Is.EqualTo(1));
            Assert.That(cabin.Panels.IsPanelOpen, Is.False);
            cabin.OpenComputer();
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenPhotoLab();
            Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
            Assert.That(camera.Photos.Count, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator DisablingOrRebindingCancelsDelayedCapture()
        {
            ReadyCapture(); int calls = 0; catcher.CaptureResolved += _ => calls++;
            cabin.OpenCapture(); cabin.enabled = false;
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(calls, Is.Zero); Assert.That(art.texture, Is.SameAs(cabin.CabinArt));
            cabin.enabled = true;
            cabin.OpenCapture();
            cabin.GetComponent<CabinZoneSession>().Configure("Zone01");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(calls, Is.Zero); Assert.That(cabin.IsDirectInteractionActive, Is.False);
        }
    }
}

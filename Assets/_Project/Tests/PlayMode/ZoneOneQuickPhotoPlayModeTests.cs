using System;
using System.Collections;
using System.IO;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Feedback;
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
    public sealed class ZoneOneQuickPhotoPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private CabinStationView cabin;
        private ZoneOneStory story;
        private ExpeditionLoop loop;
        private ZoneOneAudioController audio;
        private PhotoCaptureService photos;
        private ZoneNavigation Nav => cabin.Navigation;
        private EffectDirector Effects => cabin.GetComponent<EffectDirector>();
        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "ZoneOneQuickPhoto-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer(); yield return Load();
        }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null; yield return null;
            cabin = Object.FindAnyObjectByType<CabinStationView>(); story = cabin.GetComponent<ZoneOneStory>();
            loop = Object.FindAnyObjectByType<ExpeditionLoop>(); audio = cabin.GetComponent<ZoneOneAudioController>();
            photos = cabin.GetComponent<PhotoCaptureService>(); Assert.That(loop.IsInitialized, Is.True, loop.LastError);
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
        private void Aim(string poiId)
        {
            var poi = story.survey.FindPoi(poiId); Nav.Ship.Refill();
            Nav.RestoreVoyage(story.survey.ContactPosition(poi) + Vector2.down * 14, 0, story.survey.DepthFor(poi), 600);
        }
        private IEnumerator QuickPhoto()
        { cabin.OpenCamera(); yield return null; yield return null; yield return null; }
        private bool Playing(string id) => Effects.IsPlaying("z1.audio." + id);
        [UnityTest] public IEnumerator QuickPhotoGoesToPhotoLabAndOnlySendCompletesTheObjective()
        {
            Assert.That(cabin.NavigationPanel.transform.Find("WatercolorHUD/Observation"), Is.Null);
            var legacy = cabin.GetComponentInChildren<PhotoCameraView>(true);
            Assert.That(legacy.gameObject.activeInHierarchy, Is.False);
            Assert.That(legacy.transform.Find("PhotoPreview/LiveSurveyLayers"), Is.Null);
            var computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            var upgrade = Array.Find(computer.Apps, a => a.id == ComputerAppId.Upgrade).panel;
            Assert.That(upgrade.transform.Find("LeftPanel/CheckExternalLight"), Is.Null);
            Aim("zone01-left"); int count = photos.Photos.Count, charges = Nav.Ship.Photos;
            yield return QuickPhoto();
            Assert.That(photos.Photos.Count, Is.EqualTo(count + 1)); Assert.That(Nav.Ship.Photos, Is.EqualTo(charges - 1));
            Assert.That(cabin.IsDirectInteractionActive, Is.False); Assert.That(cabin.Panels.IsPanelOpen, Is.False);
            Assert.That(legacy.gameObject.activeInHierarchy, Is.False, "Quick shutter must not reopen the retired Camera panel.");
            var photo = photos.Photos.Last(); Assert.That(photo.MissionTargetId, Is.EqualTo("Z1_Creature_01"));
            Assert.That(story.HasObjective(ZoneOneStory.PhotoOneObjective), Is.False);
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            var lab = computer.GetComponentInChildren<PhotoLabView>(true);
            Assert.That(lab.preview.texture, Is.SameAs(photo.Image)); Assert.That(lab.sendButton.interactable, Is.True);
            yield return Screenshot("01-photo-lab-before-send");
            lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(photo.IsMissionPhoto, Is.True); Assert.That(story.HasObjective(ZoneOneStory.PhotoOneObjective), Is.True);
            Assert.That(lab.sendButton.interactable, Is.False); yield return Screenshot("02-photo-lab-after-send");
        }
        [UnityTest] public IEnumerator LampChangesNewQuickPhotosWithoutRepaintingArchivedPhotos()
        {
            Aim("zone01-left"); yield return QuickPhoto();
            var before = photos.Photos.Last(); byte[] archived = before.Image.EncodeToPNG(); float dark = Brightness(before.Image);
            story.RecordObjective("zone01-left", MissionObjectiveType.Photograph, "Z1_Creature_01");
            story.ResolveCapture("zone01-left", story.FindObjective("zone01-left", MissionObjectiveType.Capture));
            story.RecordObjective("zone01-north", MissionObjectiveType.Photograph, "Z1_Creature_02");
            Assert.That(story.InstallBioLamp(), Is.True); audio.Tick(2.3f); yield return new WaitForSeconds(.7f);
            yield return QuickPhoto(); var after = photos.Photos.Last();
            Assert.That(after, Is.Not.SameAs(before)); Assert.That(Brightness(after.Image), Is.GreaterThan(dark * 1.1f));
            Assert.That(before.Image.EncodeToPNG(), Is.EqualTo(archived)); Assert.That(cabin.Panels.IsPanelOpen, Is.False);
            Assert.That(story.config.locations, Has.Length.EqualTo(2));
        }
        [UnityTest] public IEnumerator SubjectPoseAffectsTheStillFrameWithoutMovingTheSavedContact()
        {
            Aim("zone01-north"); var poi = story.survey.FindPoi("zone01-north");
            Vector2 contact = story.survey.ContactPosition(poi);
            var frame = new ZoneOneSurveyFrame(); Assert.That(photos.TryBuildSurveyFrame(frame), Is.True);
            Rect first = frame.SubjectRect; yield return new WaitForSeconds(.7f);
            photos.TryBuildSurveyFrame(frame); Assert.That(frame.SubjectRect, Is.Not.EqualTo(first));
            Assert.That(story.survey.ContactPosition(poi), Is.EqualTo(contact));
            Time.timeScale = 0; photos.TryBuildSurveyFrame(frame); Rect paused = frame.SubjectRect;
            yield return new WaitForSecondsRealtime(.15f); photos.TryBuildSurveyFrame(frame);
            Assert.That(frame.SubjectRect, Is.EqualTo(paused)); Time.timeScale = 1;
            yield return QuickPhoto(); Assert.That(photos.Photos.Last().MissionTargetId, Is.EqualTo("Z1_Creature_02"));
            Assert.That(story.HasObjective(ZoneOneStory.PhotoTwoObjective), Is.False);
        }
        [UnityTest] public IEnumerator FirstPressureCreakFollowsActualDescentAndItsReceiptSurvivesReload()
        {
            cabin.OpenNavigation(); Nav.Ship.Refill(); Nav.RestoreVoyage(new Vector2(745, 295), 90, 379, 600);
            audio.Tick(.01f); float hull = Nav.Ship.Hull;
            Nav.Navigate(0, 0, 1, .4f); audio.Tick(.1f);
            Assert.That(Playing("sfx_hull_pressure_creak"), Is.True);
            Assert.That(loop.ZoneOneAudio.Has("pressure-descent"), Is.True); Assert.That(Nav.Ship.Hull, Is.EqualTo(hull));
            Assert.That(loop.SaveCurrent(), Is.True); yield return Load();
            cabin.OpenNavigation(); Nav.Ship.Refill(); Nav.RestoreVoyage(new Vector2(745, 295), 90, 379, 600);
            audio.Tick(.01f); Nav.Navigate(0, 0, 1, .4f); audio.Tick(.1f);
            Assert.That(Playing("sfx_hull_pressure_creak"), Is.False);
            cabin.OpenComputer(); audio.Tick(40); Assert.That(Playing("sfx_hull_pressure_creak"), Is.False);
        }
        [UnityTest] public IEnumerator PressureCreakLeavesAQuietGapAfterThreeKnocks()
        {
            story.RecordObjective("zone01-left", MissionObjectiveType.Photograph, "Z1_Creature_01");
            story.ResolveCapture("zone01-left", story.FindObjective("zone01-left", MissionObjectiveType.Capture));
            cabin.OpenNavigation(); Nav.Ship.Refill(); Nav.RestoreVoyage(new Vector2(745, 295), 90, 379, 600);
            Nav.Navigate(0, 1, 0, .02f); audio.Tick(8.1f); Assert.That(Playing("sfx_hull_knocks_three"), Is.True);
            Nav.Navigate(0, 0, 1, .4f); audio.Tick(.1f); Assert.That(Playing("sfx_hull_pressure_creak"), Is.False);
            yield return new WaitForSeconds(Effects.Definition("z1.audio.sfx_hull_knocks_three").clip.length + .2f);
            Nav.Navigate(0, 0, 1, .02f); audio.Tick(7); Assert.That(Playing("sfx_hull_pressure_creak"), Is.False);
            Nav.Navigate(0, 0, 1, .02f); audio.Tick(1.1f); Assert.That(Playing("sfx_hull_pressure_creak"), Is.True);
        }
        private static float Brightness(Texture2D texture)
        {
            var pixels = texture.GetPixels32(); float sum = 0; int count = 0;
            for (int i = 0; i < pixels.Length; i += 127) { var c = pixels[i]; sum += c.r + c.g + c.b; count++; }
            return sum / count;
        }
        private static IEnumerator Screenshot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone01QuickPhoto"));
                Directory.CreateDirectory(path); File.WriteAllBytes(Path.Combine(path, name + ".png"), frame.EncodeToPNG());
            }
            finally { Object.Destroy(frame); }
        }
    }
}

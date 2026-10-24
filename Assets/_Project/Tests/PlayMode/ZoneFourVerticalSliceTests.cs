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
    /// <summary>The first Zone4 photograph and recovered fragment through real cabin devices.</summary>
    public sealed class ZoneFourVerticalSliceTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ExpeditionLoop loop;
        private ZoneOneStory story;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private ZoneFourPresentationController presentation;
        private EffectDirector effects;
        private CreatureCatcher Catcher => cabin.GetComponent<CreatureCatcher>();
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone04VerticalSlice"));

        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "ZoneFourSlice-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return WaitTransition();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(4); menu.StartRun(); yield return WaitTransition();
            Bind(); cabin.ClosePanel();
        }

        private void Bind()
        {
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            presentation = cabin.GetComponent<ZoneFourPresentationController>(); effects = cabin.GetComponent<EffectDirector>();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone04"));
            Assert.That(presentation, Is.Not.Null, "Install the Zone4 vertical slice in the shared cabin.");
            Assert.That(presentation.fragmentView, Is.Not.Null); Assert.That(presentation.fragmentSequence, Is.Not.Null);
            presentation.Tick(0);
        }

        private void PlaceL1()
        {
            var poi = story.survey.FindPoi("zone04-l1"); Assert.That(poi, Is.Not.Null);
            cabin.Navigation.Ship.Refill();
            cabin.Navigation.RestoreVoyage(story.survey.ContactPosition(poi) + Vector2.down * 14, 0,
                story.survey.DepthFor(poi), cabin.Navigation.DistanceTravelled);
        }

        private IEnumerator PhotographL1()
        {
            yield return new WaitForSeconds(.65f); PlaceL1(); cabin.ClosePanel(); int before = photos.Photos.Count;
            cabin.OpenCamera();
            yield return Until(() => photos.Photos.Count == before + 1 && !cabin.IsDirectInteractionActive, 4, "The real quick shutter must finish.");
            var photo = photos.Photos.Last();
            Assert.That(photo.MissionZoneId, Is.EqualTo("Zone04"), photo.Result);
            Assert.That(photo.MissionPoiId, Is.EqualTo("zone04-l1")); Assert.That(photo.MissionObjectiveId, Is.EqualTo("Z4_L1_PHOTO"));
            Assert.That(photo.IsMissionPhoto, Is.False); Assert.That(story.HasObjective("Z4_L1_PHOTO"), Is.False);
        }

        private IEnumerator OpenPhotoLab()
        {
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            Assert.That(computer.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
        }

        private IEnumerator SendL1()
        {
            yield return OpenPhotoLab(); var lab = computer.GetComponentInChildren<PhotoLabView>(true);
            Assert.That(lab.sendButton.interactable, Is.True); lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(photos.Photos.Last().IsMissionPhoto, Is.True); Assert.That(story.HasObjective("Z4_L1_PHOTO"), Is.True);
            Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.Zero, "SEND does not grant capture cargo.");
        }

        private IEnumerator BeginL1Capture()
        {
            cabin.ClosePanel(); PlaceL1(); cabin.OpenCapture();
            yield return Until(() => Catcher.minigame.IsActive, 4, "The real cabin capture device must open its minigame.");
        }

        private IEnumerator CaptureL1()
        {
            yield return BeginL1Capture(); CaptureMinigamePlayModeTests.Win(Catcher.minigame); yield return null;
            Assert.That(Catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(story.HasObjective("Z4_L1_CAPTURE"), Is.True);
            Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.EqualTo(1));
            Assert.That(cabin.IsDirectInteractionActive, Is.False);
        }

        private void AssertHiddenLocationsUndiscovered()
        {
            Assert.That(story.HiddenRouteAvailable, Is.False); Assert.That(loop.CurrentProgress.hiddenRouteUnlocked, Is.False);
            Assert.That(story.ProgressState.revealedLocations, Is.Empty);
            Assert.That(story.IsLocationRevealed("Z4_L3"), Is.False); Assert.That(story.IsLocationRevealed("Z4_L4"), Is.False);
            Assert.That(loop.ActiveMap.HiddenDestinationAvailable(story), Is.False);
        }

        private IEnumerator OpenFragment()
        {
            cabin.OpenComputer(); computer.OpenMissionLog(); presentation.Tick(0);
            yield return Until(() => presentation.fragmentView.Visible && presentation.FragmentActive, 2, "Only the foreground Mission Log may present the recovered fragment.");
            Assert.That(computer.CurrentApp, Is.EqualTo(ComputerAppId.MissionLog));
            Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.True);
        }

        private bool FragmentAudioPlaying()
        {
            var clips = presentation.fragmentSequence.cues.Where(cue => cue?.effect?.clip != null).Select(cue => cue.effect.clip).ToArray();
            Assert.That(clips, Is.Not.Empty, "The production fragment sequence must bind actual audio.");
            return cabin.GetComponent<EffectAudioExecutor>().GetComponentsInChildren<AudioSource>(true)
                .Any(source => source.isPlaying && source.clip != null && clips.Contains(source.clip));
        }

        private static IEnumerator Screenshot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture(); Assert.That(image, Is.Not.Null);
            try { Directory.CreateDirectory(Qa); File.WriteAllBytes(Path.Combine(Qa, name + ".png"), image.EncodeToPNG()); }
            finally { Object.Destroy(image); }
            yield return null;
        }

        [UnityTest] public IEnumerator ActualWreckPhotoAndCapturedFragmentUseRealLabAndMissionLogWithoutDiscoveringHiddenSites()
        {
            photos.ZoneFourComposition.Enabled = false; yield return PhotographL1(); var plain = photos.Photos.Last();
            Assert.That(photos.ZoneFourComposition.LastWreckComposited, Is.False);
            photos.ZoneFourComposition.Enabled = true; yield return PhotographL1(); var composed = photos.Photos.Last();
            Assert.That(photos.ZoneFourComposition.LastWreckComposited, Is.True);
            Rect bounds = photos.ZoneFourComposition.LastWreckRect;
            Assert.That(bounds.width, Is.GreaterThan(0).And.LessThan(.4f)); Assert.That(bounds.height, Is.GreaterThan(0).And.LessThan(.3f));
            Assert.That(composed.Result, Is.EqualTo(plain.Result), "The background wreck cannot alter subject coverage or mission validation.");
            var before = plain.Image.GetPixels32(); var after = composed.Image.GetPixels32(); Assert.That(after.Length, Is.EqualTo(before.Length));
            int changed = 0, outside = 0;
            for (int y = 0; y < composed.Image.height; y++) for (int x = 0; x < composed.Image.width; x++)
            {
                int index = y * composed.Image.width + x;
                if (before[index].Equals(after[index])) continue;
                changed++;
                if (x < Mathf.FloorToInt(bounds.xMin * composed.Image.width) - 1 || x >= Mathf.CeilToInt(bounds.xMax * composed.Image.width) + 1 ||
                    y < Mathf.FloorToInt(bounds.yMin * composed.Image.height) - 1 || y >= Mathf.CeilToInt(bounds.yMax * composed.Image.height) + 1) outside++;
            }
            Assert.That(changed, Is.GreaterThan(20)); Assert.That(changed / (float)after.Length, Is.LessThan(.08f));
            Assert.That(outside, Is.Zero, "The background layer must not repaint the rest of the evidence image.");
            CollectionAssert.AreEqual(composed.Image.EncodeToPNG(), File.ReadAllBytes(Path.Combine(photos.ArchivePath, composed.Id + ".png")),
                "The real loose archive must contain the composited photograph.");
            Directory.CreateDirectory(Qa); File.WriteAllBytes(Path.Combine(Qa, "A-wreck-photograph.png"), composed.Image.EncodeToPNG());
            yield return OpenPhotoLab(); yield return Screenshot("A-01-wreck-photo-in-real-lab");
            yield return SendL1(); Assert.That(photos.SubmitPhoto(composed), Is.False);
            yield return CaptureL1(); AssertHiddenLocationsUndiscovered();
            Assert.That(ExpeditionSaveStore.TryRead(out var captured, out var readError), Is.True, readError);
            CollectionAssert.AreEqual(composed.Image.EncodeToPNG(), Convert.FromBase64String(captured.current.photos.Single(photo => photo.id == composed.Id).png),
                "The durable timeline must preserve the photograph's actual pixels.");
            Assert.That(story.MissionText(), Does.Contain("1006.56, 450.51").And.Contain("358.56, 119.57"));
            Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.False, "Recovering data must not force open its presentation.");
            var missionLog = computer.GetComponentInChildren<MissionLogView>(true);
            var bodyGraphic = missionLog.Body.GetComponent<ComputerDesktopText>()?.RenderedText;
            Assert.That(bodyGraphic, Is.Not.Null, "Inspect the actual TMP mission renderer.");
            bool bodyWasEnabled = bodyGraphic.enabled;
            Assert.That(bodyWasEnabled, Is.True, "The normal Mission Log text must be readable before processing.");
            var audioEvidence = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 10);
            yield return OpenFragment();
            yield return Until(() => presentation.fragmentView.Progress > .1f && FragmentAudioPlaying(), 3, "The real processing card must be accompanied by its assigned audio.");
            Assert.That(bodyGraphic.enabled, Is.False, "Mission body graphics must not bleed through the processing card.");
            yield return Screenshot("B-01-fragment-processing-in-real-mission-log");
            yield return Until(() => presentation.FragmentActive && presentation.fragmentView.Visible &&
                presentation.fragmentView.FirstCoordinateText.Contains("1006.56") &&
                presentation.fragmentView.FirstCoordinateText.Contains("450.51") && presentation.fragmentView.SecondCoordinateText.Contains("358.56") &&
                presentation.fragmentView.SecondCoordinateText.Contains("119.57"), 6, "Both existing hint coordinates must become readable.");
            yield return Screenshot("B-02a-readable-fragment-card-in-real-mission-log");
            // The director expires the sequence in Update; its controller clears the card in LateUpdate.
            yield return Until(() => !presentation.FragmentActive && !presentation.fragmentView.Visible,
                presentation.fragmentSequence.Duration + 1, "The short processing card and its sequence must finish.");
            Assert.That(presentation.fragmentView.Visible, Is.False);
            Assert.That(bodyGraphic.enabled, Is.EqualTo(bodyWasEnabled), "Finishing processing must restore the actual Mission Log renderer.");
            var mix = audioEvidence.Finish(Path.Combine(Qa, "B-fragment-listener.wav"));
            File.WriteAllText(Path.Combine(Qa, "B-fragment-listener.json"), JsonUtility.ToJson(mix, true));
            Assert.That(mix.nonzero, Is.True, "Record the real listener mix, without replacing it with the source clip.");
            Assert.That(mix.durationSeconds, Is.GreaterThanOrEqualTo(.8f)); Assert.That(mix.clippedSamples, Is.Zero);
            Assert.That(missionLog.DisplayedText,
                Does.Contain("1006.56, 450.51").And.Contain("358.56, 119.57"), "The real hint remains readable after the card closes.");
            yield return Screenshot("B-02-recovered-coordinates-in-real-mission-log");
            Assert.That(presentation.FragmentCount, Is.EqualTo(1)); AssertHiddenLocationsUndiscovered();
        }

        [UnityTest] public IEnumerator FragmentFirstDisplayPersistsAcrossPauseAndContinueAndJournalRestoreCancelsItsChildren()
        {
            yield return PhotographL1(); yield return SendL1(); yield return CaptureL1();
            cabin.ClosePanel(); int checkpointDay = loop.Day; Assert.That(loop.Rest(), Is.True);
            int cargo = story.inventory.GetCount("Z4_Creature_01");
            yield return OpenFragment(); Assert.That(presentation.FragmentCount, Is.EqualTo(1));
            cabin.OpenPause(); yield return null; presentation.Tick(0); yield return null;
            Assert.That(presentation.FragmentActive, Is.False); Assert.That(presentation.fragmentView.Visible, Is.False);
            Assert.That(FragmentAudioPlaying(), Is.False); Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.True);
            PauseMenuController.Instance.Resume(); yield return null; presentation.Tick(8);
            Assert.That(presentation.FragmentCount, Is.EqualTo(1)); Assert.That(presentation.FragmentActive, Is.False);
            Assert.That(loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return WaitTransition(); flow.ContinueDebugGame(); yield return WaitTransition(); Bind();
            Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.True);
            cabin.OpenComputer(); computer.OpenMissionLog(); presentation.Tick(8); yield return null;
            Assert.That(presentation.FragmentCount, Is.Zero); Assert.That(presentation.FragmentActive, Is.False);
            Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.EqualTo(cargo));
            cabin.ClosePanel(); Assert.That(loop.RestoreDay(checkpointDay), Is.True, loop.LastError); Bind();
            Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.False);
            Assert.That(story.HasObjective("Z4_L1_CAPTURE"), Is.True); Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.EqualTo(cargo));
            yield return OpenFragment(); Assert.That(presentation.FragmentCount, Is.EqualTo(1));
            computer.CloseApp(ComputerAppId.MissionLog); presentation.Tick(0); yield return null;
            Assert.That(presentation.FragmentActive, Is.False); Assert.That(presentation.fragmentView.Visible, Is.False);
            Assert.That(FragmentAudioPlaying(), Is.False);
            computer.OpenMissionLog(); presentation.Tick(8); yield return null;
            Assert.That(presentation.FragmentCount, Is.EqualTo(1)); Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.EqualTo(cargo));
            AssertHiddenLocationsUndiscovered();
        }

        [UnityTest] public IEnumerator FailedCapturePublicationGrantsNoCargoHintOrFragmentAndPreservesDurableTimeline()
        {
            yield return PhotographL1(); yield return SendL1(); yield return BeginL1Capture();
            string progress = JsonUtility.ToJson(story.ExportProgress()); string valid = ExpeditionSaveStore.PathOverride;
            string publishedPath = ExpeditionSaveStore.SavePath; byte[] published = File.ReadAllBytes(publishedPath);
            // Both normal and debug publication fail because their parent is an existing timeline file.
            ExpeditionSaveStore.PathOverride = Path.Combine(publishedPath, "blocked-capture.json");
            try { CaptureMinigamePlayModeTests.Win(Catcher.minigame); }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            yield return null;
            Assert.That(Catcher.LastResult, Is.Not.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(JsonUtility.ToJson(story.ExportProgress()), Is.EqualTo(progress));
            Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.Zero); Assert.That(story.HasObjective("Z4_L1_CAPTURE"), Is.False);
            CollectionAssert.AreEqual(published, File.ReadAllBytes(publishedPath));
            cabin.OpenComputer(); computer.OpenMissionLog(); presentation.Tick(8); yield return null;
            Assert.That(computer.GetComponentInChildren<MissionLogView>(true).DisplayedText, Does.Not.Contain("1006.56"));
            Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.False); Assert.That(presentation.FragmentCount, Is.Zero);
            Assert.That(presentation.fragmentView.Visible, Is.False); Assert.That(FragmentAudioPlaying(), Is.False);
            AssertHiddenLocationsUndiscovered();
        }

        [UnityTest] public IEnumerator FailedManualSendKeepsWreckEvidenceRetryableAndCannotGrantCaptureOrFragment()
        {
            yield return PhotographL1(); var photo = photos.Photos.Last(); byte[] evidence = photo.Image.EncodeToPNG();
            yield return OpenPhotoLab(); var lab = computer.GetComponentInChildren<PhotoLabView>(true);
            string progress = JsonUtility.ToJson(story.ExportProgress()); string valid = ExpeditionSaveStore.PathOverride;
            string publishedPath = ExpeditionSaveStore.SavePath; byte[] published = File.ReadAllBytes(publishedPath);
            ExpeditionSaveStore.PathOverride = Path.Combine(publishedPath, "blocked-send.json");
            try { lab.sendButton.onClick.Invoke(); }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            yield return null;
            Assert.That(photo.IsMissionPhoto, Is.False); Assert.That(photos.CanSubmitPhoto(photo), Is.True);
            Assert.That(JsonUtility.ToJson(story.ExportProgress()), Is.EqualTo(progress));
            Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.Zero); Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.False);
            CollectionAssert.AreEqual(published, File.ReadAllBytes(publishedPath)); CollectionAssert.AreEqual(evidence, photo.Image.EncodeToPNG());
            Assert.That(lab.sendButton.interactable, Is.True); Assert.That(photos.LastError, Is.Not.Null.And.Not.Empty);
            lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(photo.IsMissionPhoto, Is.True); Assert.That(story.HasObjective("Z4_L1_PHOTO"), Is.True);
            Assert.That(photos.SubmitPhoto(photo), Is.False); Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.Zero);
            Assert.That(story.MissionText(), Does.Not.Contain("1006.56")); Assert.That(presentation.FragmentCount, Is.Zero);
            AssertHiddenLocationsUndiscovered();
        }

        private IEnumerator WaitTransition()
        {
            yield return null; float deadline = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }

        private static IEnumerator Until(Func<bool> predicate, float seconds, string reason)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, reason);
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

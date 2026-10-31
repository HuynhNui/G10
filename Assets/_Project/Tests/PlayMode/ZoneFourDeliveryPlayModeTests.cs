using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using G10.Prototype.Audio;
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
    /// <summary>
    /// Controlled native delivery fixtures: mission state and water approaches are seeded directly.
    /// These tests observe real rendered frames and imported voices, not an actual-input playthrough or human perception.
    /// Gameplay PHOTO/SEND/capture/Radar/choice/arrival acceptance remains in the existing ZoneFour fixtures.
    /// </summary>
    public sealed class ZoneFourDeliveryPlayModeTests
    {
        private sealed class Preference { public string key; public bool existed; public float value; }
        private readonly List<Preference> preferences = new();
        private readonly float[] categories = new float[5];
        private string folder, oldSave, oldPhotos;
        private float oldOutput, oldListener;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ExpeditionLoop loop;
        private ZoneOneStory story;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private ZoneFourPresentationController presentation;
        private EffectDirector effects;
        private AudioManager audio;
        private ZoneNavigation Nav => cabin.Navigation;
        private PhotoLabView Lab => computer.GetComponentInChildren<PhotoLabView>(true);

        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            preferences.Clear(); oldListener = AudioListener.volume;
            foreach (string key in new[] { "G10.Audio.OutputVolume" }.Concat(Enum.GetValues(typeof(AudioCategory)).Cast<AudioCategory>()
                .Select(category => "G10.Audio.Category." + category)))
                preferences.Add(new Preference { key = key, existed = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetFloat(key, 1) });
            oldOutput = AudioManager.Instance != null ? AudioManager.Instance.OutputVolume : PlayerPrefs.GetFloat("G10.Audio.OutputVolume", 1);
            for (int i = 0; i < categories.Length; i++) categories[i] = AudioManager.Instance != null
                ? AudioManager.Instance.CategoryVolume((AudioCategory)i) : PlayerPrefs.GetFloat("G10.Audio.Category." + (AudioCategory)i, 1);
            folder = Path.Combine(Application.temporaryCachePath, "ZoneFourDelivery-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return WaitTransition();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(4); menu.StartRun(); yield return WaitTransition();
            Bind(); cabin.ClosePanel(); Nav.Brake();
            audio = AudioManager.Instance; Assert.That(audio, Is.Not.Null); audio.SetOutputVolume(1);
            for (int i = 0; i < categories.Length; i++) audio.SetCategoryVolume((AudioCategory)i, 1);
        }

        private void Bind()
        {
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            presentation = cabin.GetComponent<ZoneFourPresentationController>(); effects = cabin.GetComponent<EffectDirector>();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone04"));
        }

        private void Seed(bool finalRoute = false)
        {
            // Deliberately direct controlled state, never reported as gameplay progression.
            loop.CurrentProgress.hiddenRouteUnlocked = true; loop.CurrentProgress.endingChoice = "explore";
            story.HiddenRouteAvailable = true; var state = story.ExportProgress();
            state.completedObjectives.Add("Z4_L1_CAPTURE");
            state.revealedLocations.AddRange(loop.ActiveMap.hiddenLocationIds);
            if (finalRoute)
            {
                foreach (var location in story.config.locations)
                    foreach (var objective in location.objectives) state.completedObjectives.Add(objective.id);
                foreach (var objective in story.config.zoneGate.objectives) state.completedObjectives.Add(objective.id);
            }
            story.RestoreProgress(state); Assert.That(loop.SaveCurrent(), Is.True, loop.LastError);
            if (finalRoute) Assert.That(loop.ActiveMap.HiddenDestinationAvailable(story), Is.True);
        }

        private IEnumerator Photograph(string poiId)
        {
            cabin.ClosePanel(); yield return new WaitForSecondsRealtime(.65f);
            var poi = story.survey.FindPoi(poiId); Nav.Ship.Refill();
            Vector2 position = story.survey.ContactPosition(poi) + Vector2.down * 14;
            Assert.That(Nav.CanOccupy(position), Is.True, "Controlled photo placement still uses the production water chart.");
            Nav.RestoreVoyage(position, 0, story.survey.DepthFor(poi), Nav.DistanceTravelled);
            int before = photos.Photos.Count; cabin.OpenCamera();
            yield return Until(() => photos.Photos.Count == before + 1 && !cabin.IsDirectInteractionActive, 4, "Real quick shutter must finish.");
            Assert.That(photos.Photos.Last().MissionPoiId, Is.EqualTo(poiId));
            Assert.That(photos.Photos.Last().IsMissionPhoto, Is.False);
        }

        private IEnumerator OpenLab()
        {
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            Assert.That(computer.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
            Assert.That(Lab.SelectedPhotoId, Is.EqualTo(photos.Photos.Last().Id));
        }

        private AudioSource[] Voices(EffectSequence sequence)
        {
            var clips = sequence.cues.Where(cue => cue?.effect?.clip != null).Select(cue => cue.effect.clip).ToArray();
            Assert.That(clips, Is.Not.Empty);
            return effects.GetComponent<EffectAudioExecutor>().GetComponentsInChildren<AudioSource>(true)
                .Where(source => source.isPlaying && source.clip != null && clips.Contains(source.clip)).ToArray();
        }

        [UnityTest] public IEnumerator FragmentRequiresCompletedReadableRenderedCardAndManualTickCannotSupplyItsQuarterSecond()
        {
            Seed(); string authority = JsonUtility.ToJson(story.ExportProgress());
            cabin.OpenComputer(); computer.OpenMissionLog(); presentation.Tick(600);
            Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.False);
            float firstCompleteRender = -1; int completeFrames = 0;
            Canvas.WillRenderCanvases rendered = () =>
            {
                if (!presentation.fragmentView.ReadableCompleted) return;
                if (firstCompleteRender < 0) firstCompleteRender = Time.realtimeSinceStartup;
                completeFrames++;
            };
            Canvas.willRenderCanvases += rendered;
            try
            {
                yield return Until(() => presentation.fragmentView.ReadableCompleted, 6, "The real card must complete both coordinate rows and progress.");
                Assert.That(presentation.fragmentView.Progress, Is.EqualTo(1));
                Assert.That(presentation.fragmentView.FirstCoordinateText, Does.Contain("1006.56").And.Contain("450.51"));
                Assert.That(presentation.fragmentView.SecondCoordinateText, Does.Contain("358.56").And.Contain("119.57"));
                Assert.That(Voices(presentation.fragmentSequence), Is.Empty, "The 1.48-second voice is allowed to finish before the completed card.");
                presentation.Tick(600);
                Assert.That(loop.ZoneFourPresentation.Has("fragment"), Is.False, "Manual dt is not readable exposure.");
                yield return Until(() => loop.ZoneFourPresentation.Has("fragment"), 2, "Actual completed-card exposure must publish the receipt.");
                Assert.That(firstCompleteRender, Is.GreaterThanOrEqualTo(0)); Assert.That(completeFrames, Is.GreaterThan(1));
                Assert.That(Time.realtimeSinceStartup - firstCompleteRender, Is.GreaterThanOrEqualTo(.22f), "Allow at most one small observation-frame tolerance around the authored .25 seconds.");
                Assert.That(presentation.FragmentCount, Is.EqualTo(1));
                Assert.That(JsonUtility.ToJson(story.ExportProgress()), Is.EqualTo(authority));
            }
            finally { Canvas.willRenderCanvases -= rendered; }
        }

        [UnityTest] public IEnumerator SwitchingBetweenTwoPhotosOfTheSameSiteRestartsObservationWithoutCombiningExposure()
        {
            Seed(); yield return Photograph("zone04-l3"); string first = photos.Photos.Last().Id;
            yield return Photograph("zone04-l3"); string second = photos.Photos.Last().Id;
            Assert.That(first, Is.Not.EqualTo(second)); yield return OpenLab();
            yield return Until(() => Voices(presentation.hiddenA).Length > 0, 2, "The selected second photo must start its real voice.");
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(loop.ZoneFourPresentation.Has("hidden.a"), Is.False);
            Lab.Previous(); Assert.That(Lab.SelectedPhotoId, Is.EqualTo(first)); presentation.Tick(120);
            Assert.That(loop.ZoneFourPresentation.Has("hidden.a"), Is.False);
            yield return Until(() => Voices(presentation.hiddenA).Any(source => source.time < .2f), 2,
                "The selected original photo must start a fresh voice after cancelling the second photo's audience.");
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(loop.ZoneFourPresentation.Has("hidden.a"), Is.False, "Two different photo audiences cannot pool subthreshold voice progress.");
            yield return Until(() => loop.ZoneFourPresentation.Has("hidden.a"), 4, "The selected original photo must retry in place and reach its own checkpoint.");
            Assert.That(presentation.HiddenACount, Is.EqualTo(1)); Assert.That(photos.Photos.All(photo => !photo.IsMissionPhoto), Is.True);
            Assert.That(story.HasObjective("Z4_L3_PHOTO"), Is.False); Assert.That(loop.EndingReached, Is.Null.Or.Empty);
        }

        [UnityTest] public IEnumerator MutedOutputMutedAnomaliesAndControlledFocusLossCannotSpendTheSelectedPhotoOpportunity()
        {
            Seed(); yield return Photograph("zone04-l5"); string selected = photos.Photos.Last().Id;
            string authority = JsonUtility.ToJson(story.ExportProgress()); int radar = Nav.Ship.Radar;
            audio.SetOutputVolume(0); yield return OpenLab();
            yield return new WaitForSecondsRealtime(1); presentation.Tick(120);
            Assert.That(loop.ZoneFourPresentation.Has("hidden.b"), Is.False);
            audio.SetOutputVolume(1); audio.SetCategoryVolume(AudioCategory.Anomalies, 0);
            yield return new WaitForSecondsRealtime(1); presentation.Tick(120);
            Assert.That(loop.ZoneFourPresentation.Has("hidden.b"), Is.False);
            // Controlled lifecycle callback injection tests the focus path; it is not a claim of operating-system focus measurement.
            presentation.SendMessage("OnApplicationFocus", false); audio.SetCategoryVolume(AudioCategory.Anomalies, 1);
            yield return new WaitForSecondsRealtime(1); presentation.Tick(120);
            Assert.That(loop.ZoneFourPresentation.Has("hidden.b"), Is.False);
            presentation.SendMessage("OnApplicationFocus", true);
            yield return Until(() => loop.ZoneFourPresentation.Has("hidden.b"), 4, "The unchanged selected photo must retry after focus and output eligibility return.");
            Assert.That(Lab.SelectedPhotoId, Is.EqualTo(selected)); Assert.That(presentation.HiddenBCount, Is.EqualTo(1));
            Assert.That(Nav.Ship.Radar, Is.EqualTo(radar)); Assert.That(JsonUtility.ToJson(story.ExportProgress()), Is.EqualTo(authority));
            Assert.That(photos.Photos.Last().IsMissionPhoto, Is.False);
        }

        [UnityTest] public IEnumerator StorageFailureBeforeAndAfterHiddenDeliveryRetriesPublicationWithoutReplayingTheAttackInTheSameLiveSession()
        {
            Seed(); yield return Photograph("zone04-l3"); Assert.That(loop.SaveCurrent(), Is.True);
            string valid = ExpeditionSaveStore.PathOverride, publishedPath = ExpeditionSaveStore.SavePath;
            byte[] durable = File.ReadAllBytes(publishedPath);
            ExpeditionSaveStore.PathOverride = Path.Combine(publishedPath, "blocked-delivery.json");
            try
            {
                yield return OpenLab(); presentation.Tick(120);
                Assert.That(loop.ZoneFourPresentation.Has("hidden.a"), Is.False, "Storage failure before exposure cannot fabricate delivery.");
                yield return Until(() => presentation.PendingCue == "hidden.a.delivered-awaiting-publication", 5,
                    "Real voice exposure must remain locally delivered when receipt publication fails.");
                Assert.That(Voices(presentation.hiddenA), Is.Not.Empty); Assert.That(presentation.HiddenACount, Is.Zero);
                CollectionAssert.AreEqual(durable, File.ReadAllBytes(publishedPath));
                computer.CloseApp(ComputerAppId.PhotoLab); yield return null;
                presentation.SendMessage("OnApplicationFocus", false); yield return null;
                presentation.SendMessage("OnApplicationFocus", true); yield return OpenLab();
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.That(Voices(presentation.hiddenA), Is.Empty, "Audience and focus recovery must not replay an already delivered attack while storage still fails.");
                Assert.That(presentation.PendingCue, Is.EqualTo("hidden.a.delivered-awaiting-publication"));
            }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            yield return Until(() => loop.ZoneFourPresentation.Has("hidden.a"), 3, "Publication must retry in place after storage recovers.");
            Assert.That(Voices(presentation.hiddenA), Is.Empty); Assert.That(presentation.HiddenACount, Is.EqualTo(1));
            Assert.That(photos.Photos.Last().IsMissionPhoto, Is.False); Assert.That(story.HasObjective("Z4_L3_PHOTO"), Is.False);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.current.zones.Single(zone => zone.zone == "Zone04").zoneFourPresentation.Has("hidden.a"), Is.True);
        }

        private void PlaceOutsideFinal()
        {
            cabin.ClosePanel(); Nav.Ship.Refill(); var final = loop.ActiveMap.finalHiddenPoint; bool found = false;
            for (int i = 0; i < 64 && !found; i++)
            {
                float angle = i * Mathf.PI * 2 / 64; Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 position = final.mapPosition + direction * (final.arrivalRadius + 12);
                if (!Nav.CanOccupy(position) || !Nav.CanOccupy(position - direction * 5)) continue;
                Nav.RestoreVoyage(position, Mathf.Atan2(-direction.x, -direction.y) * Mathf.Rad2Deg, loop.ActiveMap.entryDepth, Nav.DistanceTravelled);
                found = true;
            }
            Assert.That(found, Is.True); Assert.That(final.Contains(Nav.Position), Is.False); cabin.OpenNavigation();
        }

        private IEnumerator EarnFinal()
        {
            float before = Nav.DistanceTravelled;
            for (int i = 0; i < 4; i++) { Nav.Step(1, 0, .02f); presentation.Tick(0); yield return null; }
            Nav.Brake(); Assert.That(Nav.DistanceTravelled, Is.GreaterThan(before));
            Assert.That(loop.ActiveMap.finalHiddenPoint.Contains(Nav.Position), Is.False, "Controlled approach must not trigger the physical ending.");
        }

        [UnityTest] public IEnumerator EarnedFinalOpportunitySurvivesInterruptionRestAndContinueStationaryButJournalRemovesFutureOpportunity()
        {
            Seed(true); int checkpoint = loop.Day; cabin.ClosePanel(); Assert.That(loop.Rest(), Is.True);
            PlaceOutsideFinal(); yield return EarnFinal();
            yield return Until(() => loop.ZoneFourPresentation.Has(ZoneFourPresentationState.FinalSignalOpportunity), 2, "Real multi-frame motion must publish the opportunity.");
            Assert.That(loop.ZoneFourPresentation.Has("final.signal"), Is.False);
            cabin.OpenRadar(); yield return null; presentation.Tick(0);
            Assert.That(Voices(presentation.finalSignal), Is.Empty); Assert.That(presentation.FinalSignalCount, Is.Zero);
            cabin.ClosePanel(); Assert.That(loop.Rest(), Is.True); Assert.That(loop.SaveCurrent(), Is.True);
            yield return Reload(); Assert.That(loop.ZoneFourPresentation.Has(ZoneFourPresentationState.FinalSignalOpportunity), Is.True);
            Assert.That(loop.ZoneFourPresentation.Has("final.signal"), Is.False); Assert.That(Mathf.Abs(Nav.Speed), Is.LessThan(.1f));
            cabin.OpenNavigation(); presentation.Tick(120);
            Assert.That(loop.ZoneFourPresentation.Has("final.signal"), Is.False, "Manual dt cannot skip the 1.2-second onset and .65 seconds of actual voice.");
            yield return Until(() => loop.ZoneFourPresentation.Has("final.signal"), 5, "Stationary Helm must deliver the saved earned opportunity.");
            Assert.That(presentation.FinalSignalCount, Is.EqualTo(1)); Assert.That(loop.EndingReached, Is.Null.Or.Empty);
            cabin.ClosePanel(); Assert.That(loop.RestoreDay(checkpoint), Is.True, loop.LastError); Bind();
            Assert.That(loop.ZoneFourPresentation.Has(ZoneFourPresentationState.FinalSignalOpportunity), Is.False);
            Assert.That(loop.ZoneFourPresentation.Has("final.signal"), Is.False); cabin.OpenNavigation();
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.That(Voices(presentation.finalSignal), Is.Empty, "Journal rollback cannot retain a future local final opportunity.");
            Assert.That(loop.EndingReached, Is.Null.Or.Empty);
        }

        [UnityTest] public IEnumerator FailedFinalOpportunityPublicationRetriesStationaryBeforeAnyPrimaryDelivery()
        {
            Seed(true); PlaceOutsideFinal(); string valid = ExpeditionSaveStore.PathOverride, path = ExpeditionSaveStore.SavePath;
            byte[] durable = File.ReadAllBytes(path); ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked-opportunity.json");
            try
            {
                yield return EarnFinal(); yield return new WaitForSecondsRealtime(.6f);
                Assert.That(loop.ZoneFourPresentation.Has(ZoneFourPresentationState.FinalSignalOpportunity), Is.False);
                Assert.That(loop.ZoneFourPresentation.Has("final.signal"), Is.False); Assert.That(presentation.FinalSignalCount, Is.Zero);
                Assert.That(Voices(presentation.finalSignal), Is.Empty, "The unsaved opportunity must not start a primary delivery.");
                CollectionAssert.AreEqual(durable, File.ReadAllBytes(path));
            }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            yield return Until(() => loop.ZoneFourPresentation.Has(ZoneFourPresentationState.FinalSignalOpportunity), 3, "Publication must retry after motion has stopped.");
            yield return Until(() => loop.ZoneFourPresentation.Has("final.signal"), 5, "The now durable opportunity must deliver through real time.");
            Assert.That(presentation.FinalSignalCount, Is.EqualTo(1)); Assert.That(loop.EndingReached, Is.Null.Or.Empty);
        }

        [UnityTest] public IEnumerator LegacyPrimaryReceiptsPreventFragmentHiddenAndFinalReplaysAfterContinue()
        {
            Seed(true); yield return Photograph("zone04-l3"); yield return Photograph("zone04-l5");
            foreach (string key in new[] { "fragment", "hidden.a", "hidden.b", "final.signal" })
                Assert.That(loop.CommitZoneFourCue(key), Is.True, "Seed legacy primary receipts through the existing save authority.");
            Assert.That(loop.ZoneFourPresentation.Has(ZoneFourPresentationState.FinalSignalOpportunity), Is.False);
            yield return Reload(); cabin.OpenComputer(); computer.OpenMissionLog();
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(presentation.FragmentActive, Is.False); Assert.That(presentation.FragmentCount, Is.Zero);
            yield return OpenLab(); yield return new WaitForSecondsRealtime(.8f); Lab.Previous();
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(Voices(presentation.hiddenA), Is.Empty); Assert.That(Voices(presentation.hiddenB), Is.Empty);
            Assert.That(presentation.HiddenACount, Is.Zero); Assert.That(presentation.HiddenBCount, Is.Zero);
            PlaceOutsideFinal(); yield return EarnFinal(); yield return new WaitForSecondsRealtime(2.2f);
            Assert.That(Voices(presentation.finalSignal), Is.Empty); Assert.That(presentation.FinalSignalCount, Is.Zero);
            Assert.That(loop.EndingReached, Is.Null.Or.Empty);
        }

        private IEnumerator Reload()
        { flow.LoadMainMenu(); yield return WaitTransition(); flow.ContinueDebugGame(); yield return WaitTransition(); Bind(); }
        private IEnumerator WaitTransition()
        {
            yield return null; float end = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
        private static IEnumerator Until(Func<bool> predicate, float seconds, string reason)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(predicate(), Is.True, reason);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; PauseMenuController.Instance?.Resume();
            if (presentation != null) presentation.SendMessage("OnApplicationFocus", true);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetOutputVolume(oldOutput);
                for (int i = 0; i < categories.Length; i++) AudioManager.Instance.SetCategoryVolume((AudioCategory)i, categories[i]);
            }
            AudioListener.volume = oldListener;
            foreach (var preference in preferences)
                if (preference.existed) PlayerPrefs.SetFloat(preference.key, preference.value); else PlayerPrefs.DeleteKey(preference.key);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject); yield return null;
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            string absolute = Path.GetFullPath(folder), temporary = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (absolute.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(absolute).StartsWith("ZoneFourDelivery-", StringComparison.Ordinal) && Directory.Exists(absolute))
                Directory.Delete(absolute, true);
        }
    }
}

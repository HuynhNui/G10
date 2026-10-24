using System;
using System.Collections;
using System.IO;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
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
    /// <summary>Real Zone4 fieldwork before exercising stale choice and failed publication boundaries.</summary>
    public sealed class ZoneFourProductionTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ExpeditionLoop loop;
        private ZoneOneStory story;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private ExpeditionProgression route;
        private DialogueController dialogue;
        private int cabinDirectorInstance;
        private CreatureCatcher Catcher => cabin.GetComponent<CreatureCatcher>();
        private ZoneNavigation Nav => cabin.Navigation;
        private ZoneFourPresentationController Presentation => cabin.GetComponent<ZoneFourPresentationController>();
        private EffectDirector Effects => cabin.GetComponent<EffectDirector>();
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone04Production"));

        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "ZoneFourProduction-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return WaitTransition();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(4); menu.StartRun(); yield return WaitTransition();
            Bind(); cabinDirectorInstance = Effects.GetInstanceID(); cabin.ClosePanel();
        }

        private void Bind()
        {
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true); route = cabin.GetComponent<ExpeditionProgression>();
            dialogue = Object.FindAnyObjectByType<DialogueController>();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone04"));
        }

        private void Place(string poiId)
        {
            var poi = story.survey.FindPoi(poiId); Assert.That(poi, Is.Not.Null); cabin.Navigation.Ship.Refill();
            cabin.Navigation.RestoreVoyage(story.survey.ContactPosition(poi) + Vector2.down * 14, 0,
                story.survey.DepthFor(poi), cabin.Navigation.DistanceTravelled);
        }

        private IEnumerator Photograph(string poiId)
        {
            yield return new WaitForSeconds(.65f); Place(poiId); cabin.ClosePanel(); int before = photos.Photos.Count; cabin.OpenCamera();
            yield return Until(() => photos.Photos.Count == before + 1 && !cabin.IsDirectInteractionActive, 4, "The real quick shutter must finish.");
            var photo = photos.Photos.Last(); Assert.That(photo.MissionPoiId, Is.EqualTo(poiId), photo.Result);
            Assert.That(photo.IsMissionPhoto, Is.False); Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.False);
        }

        private IEnumerator OpenLab()
        {
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            Assert.That(computer.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
        }

        private IEnumerator SurveyAndSend(string poiId, bool capture)
        {
            yield return Photograph(poiId); var photo = photos.Photos.Last(); yield return OpenLab();
            var lab = computer.GetComponentInChildren<PhotoLabView>(true); Assert.That(lab.sendButton.interactable, Is.True);
            lab.sendButton.onClick.Invoke(); yield return null; Assert.That(photo.IsMissionPhoto, Is.True);
            Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.True); cabin.ClosePanel();
            if (!capture) yield break;
            Place(poiId); cabin.OpenCapture(); yield return Until(() => Catcher.minigame.IsActive, 4, "The actual capture minigame must start.");
            CaptureMinigamePlayModeTests.Win(Catcher.minigame); yield return null;
            Assert.That(Catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught)); Assert.That(story.HasObjective("Z4_L1_CAPTURE"), Is.True);
        }

        private IEnumerator CompleteMainFieldwork()
        {
            yield return SurveyAndSend("zone04-l1", true); yield return SurveyAndSend("zone04-l2", false);
            route.Evaluate(); Assert.That(story.MainObjectivesComplete, Is.True); Assert.That(dialogue.IsChoiceActive, Is.True);
            var counts = ExpeditionPhotoSurvey.Read(loop, cabin.GetComponent<CabinZoneSession>());
            Assert.That(counts.MandatoryCompleted, Is.EqualTo(9)); Assert.That(counts.MandatoryTotal, Is.EqualTo(9));
            Assert.That(counts.HiddenCompleted, Is.Zero); Assert.That(photos.Photos.Count, Is.EqualTo(2), "Prior-zone receipts remain valid without archived images.");
            Assert.That(Presentation.LastSurveyMessage, Is.EqualTo("BIOLOGICAL SURVEY — 09 / 09"));
        }

        private void AssertNoChoiceOrEnding()
        {
            Assert.That(loop.CurrentProgress.endingChoice, Is.Null.Or.Empty); Assert.That(loop.CurrentProgress.hiddenRouteUnlocked, Is.False);
            Assert.That(story.HiddenRouteAvailable, Is.False); Assert.That(loop.EndingReached, Is.Null.Or.Empty);
        }

        [UnityTest] public IEnumerator StaleEndingChoiceCallbacksCannotMutatePausedOrTransitioningVoyages()
        {
            yield return CompleteMainFieldwork(); string publishedPath = ExpeditionSaveStore.SavePath;
            byte[] published = File.ReadAllBytes(publishedPath); cabin.OpenPause(); Assert.That(PauseMenuController.Instance.IsPaused, Is.True);
            route.SelectEndingChoice(0); route.SelectEndingChoice(1); route.Evaluate(); AssertNoChoiceOrEnding();
            CollectionAssert.AreEqual(published, File.ReadAllBytes(publishedPath));
            Assert.That(loop.SaveCurrent(), Is.True, "Pausing must preserve the existing autosave path.");
            PauseMenuController.Instance.Resume(); Time.timeScale = 0;
            route.SelectEndingChoice(0); route.SelectEndingChoice(1); route.Evaluate(); AssertNoChoiceOrEnding();
            Time.timeScale = 1; dialogue.Cancel(); flow.LoadMainMenu(); Assert.That(flow.IsTransitioning, Is.True);
            route.SelectEndingChoice(0); route.SelectEndingChoice(1); AssertNoChoiceOrEnding();
            yield return WaitTransition();
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.current.endingReached, Is.Null.Or.Empty);
            Assert.That(saved.current.zones.Single(zone => zone.zone == "Zone04").progress.endingChoice, Is.Null.Or.Empty);
        }

        [UnityTest] public IEnumerator FailedExploreChoicePublicationRollsBackAndTheRealChoiceCanRetry()
        {
            yield return CompleteMainFieldwork(); string valid = ExpeditionSaveStore.PathOverride;
            string publishedPath = ExpeditionSaveStore.SavePath; byte[] published = File.ReadAllBytes(publishedPath);
            ExpeditionSaveStore.PathOverride = Path.Combine(publishedPath, "blocked-explore.json");
            try { dialogue.SelectChoice(1); }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            AssertNoChoiceOrEnding(); CollectionAssert.AreEqual(published, File.ReadAllBytes(publishedPath));
            Assert.That(flow.IsTransitioning, Is.False); route.Evaluate(); Assert.That(dialogue.IsChoiceActive, Is.True);
            dialogue.SelectChoice(1); yield return null;
            Assert.That(loop.CurrentProgress.endingChoice, Is.EqualTo("explore")); Assert.That(story.HiddenRouteAvailable, Is.True);
            Assert.That(loop.CurrentProgress.hiddenRouteUnlocked, Is.True); Assert.That(story.ProgressState.revealedLocations, Is.Empty);
            Assert.That(loop.ActiveMap.HiddenDestinationAvailable(story), Is.False); Assert.That(loop.EndingReached, Is.Null.Or.Empty);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.current.zones.Single(zone => zone.zone == "Zone04").progress.endingChoice, Is.EqualTo("explore"));
        }

        [UnityTest] public IEnumerator FailedEndingPublicationNeverLoadsEndingAndTheNormalChoiceCanRetry()
        {
            yield return CompleteMainFieldwork(); string valid = ExpeditionSaveStore.PathOverride;
            string publishedPath = ExpeditionSaveStore.SavePath; byte[] published = File.ReadAllBytes(publishedPath);
            ExpeditionSaveStore.PathOverride = Path.Combine(publishedPath, "blocked-ending.json");
            try { Assert.That(loop.RecordEnding("normal"), Is.False); }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            AssertNoChoiceOrEnding(); CollectionAssert.AreEqual(published, File.ReadAllBytes(publishedPath));
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(SceneManager.GetSceneByName("Ending").isLoaded, Is.False);
            Assert.That(dialogue.IsChoiceActive, Is.True); dialogue.SelectChoice(0); yield return WaitTransition();
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.current.endingReached, Is.EqualTo("normal"));
            Assert.That(saved.current.zones.Single(zone => zone.zone == "Zone04").progress.endingChoice, Is.EqualTo("end"));
            Assert.That(Object.FindAnyObjectByType<EndingPresentation>().EndingId, Is.EqualTo("normal"));
            yield return RecordEndingEvidence("normal");
            yield return ContinueEnding("normal");
        }

        [UnityTest] public IEnumerator LastRequiredSendAnnouncesNineOnlyAfterPublicationAndStillRequiresTheActualCapture()
        {
            yield return SurveyAndSend("zone04-l1", false);
            Assert.That(Presentation.LastSurveyMessage, Is.Null.Or.Empty);
            yield return Photograph("zone04-l2"); var photo = photos.Photos.Last(); yield return OpenLab();
            var lab = computer.GetComponentInChildren<PhotoLabView>(true);
            string valid = ExpeditionSaveStore.PathOverride, publishedPath = ExpeditionSaveStore.SavePath;
            byte[] published = File.ReadAllBytes(publishedPath);
            ExpeditionSaveStore.PathOverride = Path.Combine(publishedPath, "blocked-ninth-photo.json");
            try { lab.sendButton.onClick.Invoke(); }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            Assert.That(photo.IsMissionPhoto, Is.False); Assert.That(Presentation.LastSurveyMessage, Is.Null.Or.Empty);
            Assert.That(ExpeditionPhotoSurvey.Read(loop, cabin.GetComponent<CabinZoneSession>()).MandatoryCompleted, Is.EqualTo(8));
            CollectionAssert.AreEqual(published, File.ReadAllBytes(publishedPath));
            Assert.That(lab.sendButton.interactable, Is.True); lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(photo.IsMissionPhoto, Is.True); Assert.That(Presentation.LastSurveyMessage, Is.EqualTo("BIOLOGICAL SURVEY — 09 / 09"));
            Assert.That(ExpeditionPhotoSurvey.Read(loop, cabin.GetComponent<CabinZoneSession>()).AllMandatoryPhotosComplete, Is.True);
            cabin.ClosePanel(); route.Evaluate(); Assert.That(story.HasObjective("Z4_L1_CAPTURE"), Is.False);
            Assert.That(story.MainObjectivesComplete, Is.False); Assert.That(dialogue.IsChoiceActive, Is.False); AssertNoChoiceOrEnding();
        }

        [UnityTest] public IEnumerator RealHiddenRadarPhotosAndManualSendsRequirePhysicalFinalTravelAndProduceTheHiddenEnding()
        {
            yield return CompleteMainFieldwork(); dialogue.SelectChoice(1); yield return null;
            Assert.That(story.HiddenRouteAvailable, Is.True); Assert.That(story.ProgressState.revealedLocations, Is.Empty);
            Assert.That(Presentation.hiddenA, Is.Not.Null); Assert.That(Presentation.hiddenB, Is.Not.Null);
            Assert.That(Presentation.hiddenA.cues.First(cue => cue.effect.clip != null).effect.clip,
                Is.Not.SameAs(Presentation.hiddenB.cues.First(cue => cue.effect.clip != null).effect.clip));
            yield return DiscoverAndSendHidden("zone04-l3", "Z4_L3", Presentation.hiddenA, "C-A");
            Assert.That(loop.ActiveMap.HiddenDestinationAvailable(story), Is.False);
            yield return DiscoverAndSendHidden("zone04-l5", "Z4_L4", Presentation.hiddenB, "C-B");
            route.Evaluate(); Assert.That(loop.ActiveMap.HiddenDestinationAvailable(story), Is.True);
            Assert.That(loop.CurrentProgress.hiddenRouteComplete, Is.True); Assert.That(loop.EndingReached, Is.Null.Or.Empty);
            var counts = ExpeditionPhotoSurvey.Read(loop, cabin.GetComponent<CabinZoneSession>());
            Assert.That(counts.Completed, Is.EqualTo(11)); Assert.That(counts.AllPhotosComplete, Is.True);
            Assert.That(flow.IsTransitioning, Is.False, "Completing both hidden SENDs cannot replace final navigation.");
            Assert.That(loop.SaveCurrent(), Is.True); yield return Reload();
            Assert.That(story.HasObjective("Z4_L3_PHOTO"), Is.True); Assert.That(story.HasObjective("Z4_L4_PHOTO"), Is.True);
            Assert.That(loop.ZoneFourPresentation.Has("hidden.a"), Is.True); Assert.That(loop.ZoneFourPresentation.Has("hidden.b"), Is.True);
            Assert.That(Presentation.HiddenACount, Is.Zero); Assert.That(Presentation.HiddenBCount, Is.Zero);
            var final = loop.ActiveMap.finalHiddenPoint; RestoreOutside(final); cabin.OpenNavigation();
            Assert.That(final.Contains(Nav.Position), Is.False); Assert.That(Presentation.FinalSignalCount, Is.Zero);
            var mix = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 10);
            float beforeTravel = Nav.DistanceTravelled;
            Nav.Step(1, 0, .1f); Assert.That(Nav.Speed, Is.GreaterThan(.1f)); Presentation.Tick(.1f); Nav.Brake(); route.Evaluate();
            Assert.That(Nav.DistanceTravelled, Is.GreaterThan(beforeTravel)); Assert.That(final.Contains(Nav.Position), Is.False);
            Assert.That(Presentation.FinalSignalCount, Is.EqualTo(1)); Assert.That(loop.ZoneFourPresentation.Has("final.signal"), Is.True);
            yield return Until(() => SequenceAudioPlaying(Effects, Presentation.finalSignal), 3, "Real approach motion must start the authored final signal.");
            Presentation.Tick(4); Assert.That(Presentation.EnvironmentLevel, Is.LessThan(.2f));
            Assert.That(Playing(Effects, "ambient_submarine_engine"), Is.True, "Withdrawal must preserve essential ship machinery.");
            float heading = Nav.Heading; Nav.Navigate(0, 1, 0, .1f); Assert.That(Nav.Heading, Is.Not.EqualTo(heading));
            Nav.Navigate(0, -1, 0, .1f); Nav.Brake(); Assert.That(Nav.ExpeditionBlocked, Is.False);
            yield return Screenshot("D-01-actual-final-approach"); yield return new WaitForSeconds(3);
            Assert.That(Presentation.FinalSignalCount, Is.EqualTo(1)); Assert.That(loop.EndingReached, Is.Null.Or.Empty);
            SaveMix(mix, "D-final-approach-listener", 2);
            float approachDistance = Nav.DistanceTravelled; MoveInto(final); route.Evaluate(); yield return WaitTransition();
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.current.endingReached, Is.EqualTo("hidden"));
            Assert.That(saved.current.zones.Single(zone => zone.zone == "Zone04").distance, Is.GreaterThan(approachDistance));
            yield return RecordEndingEvidence("hidden"); yield return ContinueEnding("hidden");
        }

        [UnityTest] public IEnumerator HiddenPhotoAudioCancelsOnPauseAndSceneTravelAndJournalRestoresItsRealDiscoveryTimeline()
        {
            yield return CompleteMainFieldwork(); dialogue.SelectChoice(1); yield return null;
            int checkpointDay = loop.Day; Assert.That(loop.Rest(), Is.True); int cargo = story.inventory.GetCount("Z4_Creature_01");
            yield return DiscoverHidden("zone04-l3", "Z4_L3"); yield return Photograph("zone04-l3"); yield return OpenLab();
            yield return Until(() => Presentation.HiddenACount == 1 && SequenceAudioPlaying(Effects, Presentation.hiddenA), 3, "Real Hidden A evidence must play its own observation sound.");
            cabin.OpenPause(); yield return null; Presentation.Tick(0); yield return null;
            Assert.That(Effects.ActiveSequenceCount, Is.Zero); Assert.That(SequenceAudioPlaying(Effects, Presentation.hiddenA), Is.False);
            PauseMenuController.Instance.Resume(); yield return null; Presentation.Tick(4);
            Assert.That(Presentation.HiddenACount, Is.EqualTo(1)); Assert.That(loop.ZoneFourPresentation.Has("hidden.a"), Is.True);
            var lab = computer.GetComponentInChildren<PhotoLabView>(true); Assert.That(lab.sendButton.interactable, Is.True);
            lab.sendButton.onClick.Invoke(); yield return null; Assert.That(story.HasObjective("Z4_L3_PHOTO"), Is.True);
            cabin.ClosePanel(); Assert.That(loop.Rest(), Is.True);
            RestoreOutside(loop.ActiveMap.returnArea); MoveInto(loop.ActiveMap.returnArea); route.Evaluate(); yield return WaitTransition();
            Assert.That(loop.Zone, Is.EqualTo("Zone03")); Presentation.Tick(0);
            Assert.That(Presentation.InZone, Is.False); Assert.That(Playing(Effects, "hidden_signal_a"), Is.False);
            Assert.That(Playing(Effects, "abyss_sparse_water"), Is.False);
            RestoreOutside(loop.ActiveMap.exitArea); MoveInto(loop.ActiveMap.exitArea); route.Evaluate(); yield return WaitTransition(); Bind();
            Assert.That(story.HasObjective("Z4_L3_PHOTO"), Is.True); Assert.That(loop.ZoneFourPresentation.Has("hidden.a"), Is.True);
            int firstShows = Presentation.HiddenACount; yield return OpenLab(); Presentation.Tick(4); yield return null;
            Assert.That(Presentation.HiddenACount, Is.EqualTo(firstShows));
            cabin.ClosePanel(); Assert.That(loop.RestoreDay(checkpointDay), Is.True, loop.LastError); Bind();
            Assert.That(story.HasObjective("Z4_L3_PHOTO"), Is.False); Assert.That(story.IsLocationRevealed("Z4_L3"), Is.False);
            Assert.That(loop.ZoneFourPresentation.Has("hidden.a"), Is.False); Assert.That(story.HiddenRouteAvailable, Is.True);
            Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.EqualTo(cargo));
            Assert.That(ExpeditionPhotoSurvey.Read(loop, cabin.GetComponent<CabinZoneSession>()).HiddenCompleted, Is.Zero);
            firstShows = Presentation.HiddenACount;
            yield return DiscoverHidden("zone04-l3", "Z4_L3"); yield return Photograph("zone04-l3"); yield return OpenLab();
            yield return Until(() => Presentation.HiddenACount == firstShows + 1 && SequenceAudioPlaying(Effects, Presentation.hiddenA), 3, "Journal rollback must permit the actual observation again.");
            computer.CloseApp(ComputerAppId.PhotoLab);
            yield return Until(() => Effects.ActiveSequenceCount == 0 && !SequenceAudioPlaying(Effects, Presentation.hiddenA), 2, "Closing the actual photo window cancels its owned sound.");
            Assert.That(story.inventory.GetCount("Z4_Creature_01"), Is.EqualTo(cargo)); Assert.That(loop.EndingReached, Is.Null.Or.Empty);
        }

        private IEnumerator DiscoverHidden(string poiId, string locationId)
        {
            cabin.ClosePanel(); Place(poiId); Assert.That(story.IsLocationRevealed(locationId), Is.False);
            cabin.OpenRadar(); int charges = Nav.Ship.Radar; cabin.Scan(); Assert.That(cabin.Radar.IsScanning, Is.True);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1));
            yield return Until(() => story.IsLocationRevealed(locationId), 3, "The actual radar sweep must discover " + locationId);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1)); Assert.That(story.HasObjective(locationId + "_PHOTO"), Is.False);
            cabin.ClosePanel();
        }

        private IEnumerator DiscoverAndSendHidden(string poiId, string locationId, EffectSequence sequence, string label)
        {
            yield return DiscoverHidden(poiId, locationId); yield return Photograph(poiId); var photo = photos.Photos.Last();
            var recorder = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 8);
            yield return OpenLab(); yield return Until(() => SequenceAudioPlaying(Effects, sequence), 3, "Actual Photo Lab evidence must start " + sequence.id);
            Assert.That(story.HasObjective(locationId + "_PHOTO"), Is.False);
            yield return Screenshot(label + "-actual-radar-photo-lab"); yield return new WaitForSeconds(3);
            SaveMix(recorder, label + "-actual-observation-listener", 2);
            var lab = computer.GetComponentInChildren<PhotoLabView>(true); Assert.That(lab.sendButton.interactable, Is.True);
            lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(photo.IsMissionPhoto, Is.True); Assert.That(story.HasObjective(locationId + "_PHOTO"), Is.True);
            Assert.That(photos.SubmitPhoto(photo), Is.False); Assert.That(story.inventory.GetCount(photo.MissionTargetId), Is.Zero);
            cabin.ClosePanel(); yield return Until(() => Effects.ActiveSequenceCount == 0, 2, "Leaving Photo Lab cancels its observation sequence.");
        }

        private Vector2 RestoreOutside(MapPoi point)
        {
            cabin.ClosePanel(); Nav.Ship.Refill(); Vector2 outside = Vector2.zero; bool found = false;
            for (int i = 0; i < 64 && !found; i++)
            {
                float angle = i * Mathf.PI * 2 / 64; Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 candidate = point.mapPosition + direction * (point.arrivalRadius + 6); bool clear = true;
                for (int distance = 0; distance <= 24 && clear; distance++) clear &= Nav.CanOccupy(candidate - direction * distance);
                if (clear) { outside = candidate; found = true; }
            }
            Assert.That(found, Is.True, "Find a real clear-water approach to " + point.id);
            Vector2 delta = point.mapPosition - outside;
            Nav.RestoreVoyage(outside, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg, loop.ActiveMap.entryDepth, Nav.DistanceTravelled);
            route.Evaluate(); Assert.That(point.Contains(Nav.Position), Is.False); return outside;
        }

        private void MoveInto(MapPoi point)
        {
            float distance = Nav.DistanceTravelled, hull = Nav.Ship.Hull;
            for (int i = 0; i < 200 && !point.Contains(Nav.Position); i++) Nav.Step(1, 0, .1f);
            Nav.Brake(); Assert.That(point.Contains(Nav.Position), Is.True); Assert.That(Nav.DistanceTravelled, Is.GreaterThan(distance + .01f));
            Assert.That(Nav.Ship.Hull, Is.EqualTo(hull), "The verified approach must not collide with the authored terrain.");
        }

        private IEnumerator Reload()
        {
            flow.LoadMainMenu(); yield return WaitTransition(); flow.ContinueDebugGame(); yield return WaitTransition(); Bind();
        }

        private static bool Playing(EffectDirector director, string clipName) => director.GetComponent<EffectAudioExecutor>()
            .GetComponentsInChildren<AudioSource>(true).Any(source => source.isPlaying && source.clip != null && source.clip.name == clipName);

        private static bool SequenceAudioPlaying(EffectDirector director, EffectSequence sequence)
        {
            Assert.That(sequence, Is.Not.Null); var clips = sequence.cues.Where(cue => cue?.effect?.clip != null).Select(cue => cue.effect.clip).ToArray();
            Assert.That(clips, Is.Not.Empty);
            return director.GetComponent<EffectAudioExecutor>().GetComponentsInChildren<AudioSource>(true)
                .Any(source => source.isPlaying && source.clip != null && clips.Contains(source.clip));
        }

        private static void SaveMix(ZoneThreeAudioEvidence recorder, string name, float minimumSeconds)
        {
            Directory.CreateDirectory(Qa); var mix = recorder.Finish(Path.Combine(Qa, name + ".wav"));
            File.WriteAllText(Path.Combine(Qa, name + ".json"), JsonUtility.ToJson(mix, true));
            Assert.That(mix.nonzero, Is.True, "Capture the actual listener, without substituting asset audio.");
            Assert.That(mix.durationSeconds, Is.GreaterThanOrEqualTo(minimumSeconds)); Assert.That(mix.clippedSamples, Is.Zero);
        }

        private IEnumerator RecordEndingEvidence(string id)
        {
            var ending = Object.FindAnyObjectByType<EndingPresentation>(); Assert.That(ending, Is.Not.Null);
            Assert.That(ending.EndingId, Is.EqualTo(id)); Assert.That(ending.effects.GetInstanceID(), Is.Not.EqualTo(cabinDirectorInstance));
            var sequence = id == "hidden" ? ending.hiddenSequence : ending.normalSequence;
            Assert.That(sequence.Duration, Is.EqualTo(id == "hidden" ? 16 : 12)); Assert.That(ending.IsPlaying, Is.True);
            Assert.That(ending.TitleText, Is.EqualTo(id == "hidden" ? "HIDDEN ENDING" : "EXPEDITION COMPLETE"));
            Assert.That(ending.SurveyText, Does.Contain(id == "hidden" ? "11 / 11" : "09 / 09")); Assert.That(ending.SummaryText, Is.Not.Empty);
            var menu = ending.transform.Find("MainMenuButton").GetComponent<UnityEngine.UI.Button>();
            Assert.That(menu.interactable, Is.True); Vector3 menuPosition = menu.transform.position;
            var recorder = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 20);
            yield return Until(() => SequenceAudioPlaying(ending.effects, sequence), sequence.Duration, "The actual ending canvas must play its authored audio.");
            yield return Until(() => ending.Elapsed >= 2, 4, "The ending composition must advance through the production clock.");
            yield return Screenshot("E-" + id + "-01-authored-ending-canvas");
            yield return Until(() => !ending.IsPlaying, sequence.Duration + 2, "The ending presentation must have a bounded duration.");
            Assert.That(menu.interactable, Is.True); Assert.That(menu.transform.position, Is.EqualTo(menuPosition));
            SaveMix(recorder, "E-" + id + "-ending-listener", id == "hidden" ? 10 : 8);
            yield return Screenshot("E-" + id + "-02-final-ending-canvas");
        }

        private IEnumerator ContinueEnding(string id)
        {
            var ending = Object.FindAnyObjectByType<EndingPresentation>();
            ending.transform.Find("MainMenuButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return WaitTransition();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            flow.ContinueDebugGame(); yield return WaitTransition();
            ending = Object.FindAnyObjectByType<EndingPresentation>(); Assert.That(ending.EndingId, Is.EqualTo(id));
            Assert.That(ending.SurveyText, Does.Contain(id == "hidden" ? "11 / 11" : "09 / 09"));
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error); Assert.That(saved.current.endingReached, Is.EqualTo(id));
        }

        private static IEnumerator Screenshot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture(); Assert.That(image, Is.Not.Null);
            try { Directory.CreateDirectory(Qa); File.WriteAllBytes(Path.Combine(Qa, name + ".png"), image.EncodeToPNG()); }
            finally { Object.Destroy(image); }
            yield return null;
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

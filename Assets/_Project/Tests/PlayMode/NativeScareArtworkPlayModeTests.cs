#if UNITY_EDITOR
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
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class NativeScareArtworkPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ExpeditionLoop loop;
        private ZoneOneStory story;
        private EffectDirector effects;
        private AudioManager audio;
        private readonly Dictionary<string, (bool exists, float value)> preferences = new();
        private float oldOutput;
        private readonly float[] oldCategories = new float[5];
        private ZoneNavigation Nav => cabin.Navigation;
        private static readonly string Qa = Path.GetFullPath("Docs/QA/ArtworkCompletion/scares");
        [Serializable] private sealed class Evidence
        {
            public string zone, scope, artwork, audioId, authorityBefore, authorityAfter;
            public float sequenceOnset, imageAt, audioAtImage, imageOffAt;
            public bool sourceVoicePlaying, cleanup, nativeKeyboardMotion, noPhotoCreated;
            public ZoneThreeAudioEvidence.Metrics listener;
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(SessionState.GetBool("G10.Zone02Experience.isolated", false), Is.True);
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            Assert.That(oldSave, Is.Not.Null.And.Not.Empty); Assert.That(oldPhotos, Is.Not.Null.And.Not.Empty);
            folder = Path.Combine(Application.temporaryCachePath, "NativeScareArtwork-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline();
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            preferences.Clear();
            foreach (string key in new[] { "G10.Audio.OutputVolume" }.Concat(Enum.GetValues(typeof(AudioCategory)).Cast<AudioCategory>().Select(c => "G10.Audio.Category." + c)))
                preferences[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetFloat(key, 1));
            oldOutput = AudioManager.Instance != null ? AudioManager.Instance.OutputVolume : PlayerPrefs.GetFloat("G10.Audio.OutputVolume", 1);
            for (int i = 0; i < oldCategories.Length; i++) oldCategories[i] = AudioManager.Instance != null ? AudioManager.Instance.CategoryVolume((AudioCategory)i) : 1;
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return Transition();
            Directory.CreateDirectory(Qa);
        }
        private IEnumerator StartZone(int zone)
        {
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>();
            menu.Open(); menu.SelectZone(zone); menu.StartRun(); yield return Transition();
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); effects = cabin.GetComponent<EffectDirector>();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone0" + zone));
            audio = AudioManager.Instance; audio.SetOutputVolume(1);
            for (int i = 0; i < oldCategories.Length; i++) audio.SetCategoryVolume((AudioCategory)i, 1);
            cabin.OpenNavigation(); yield return null; yield return null;
        }
        private void SeedCompletedCurrentZone(bool gate)
        {
            var state = story.ExportProgress();
            foreach (var location in story.config.locations)
                foreach (var objective in location.objectives) if (!state.completedObjectives.Contains(objective.id)) state.completedObjectives.Add(objective.id);
            if (gate) foreach (var objective in story.config.zoneGate.objectives)
                if (!state.completedObjectives.Contains(objective.id)) state.completedObjectives.Add(objective.id);
            story.RestoreProgress(state);
        }
        private static void Key(bool down)
        {
            Assert.That(Keyboard.current, Is.Not.Null);
            InputSystem.QueueStateEvent(Keyboard.current, down ? new KeyboardState(UnityEngine.InputSystem.Key.W) : new KeyboardState());
        }
        private bool Lane(Vector2 start, Vector2 direction, float length)
        {
            for (float distance = 0; distance <= length; distance += 2)
                if (!Nav.CanOccupy(start + direction * distance)) return false;
            return true;
        }
        private void PlaceOutwardFrom(MapPoi point, float radius, float laneLength)
        {
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI / 32; var direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                var position = point.mapPosition + direction * radius;
                if (!Lane(position, direction, laneLength)) continue;
                Nav.Ship.Refill(); Nav.RestoreVoyage(position, i * 5.625f, loop.ActiveMap.entryDepth, Nav.DistanceTravelled);
                return;
            }
            Assert.Fail("No straight native water-chart lane outside " + point.id);
        }
        private void PlaceOpenLane(float length, float depth)
        {
            var size = Nav.MapWorldSize;
            for (float y = 50; y < size.y - 50; y += 25)
            for (float x = 50; x < size.x - 50; x += 25)
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4; var direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                if (!Lane(new Vector2(x, y), direction, length)) continue;
                Nav.Ship.Refill(); Nav.RestoreVoyage(new Vector2(x, y), i * 45, depth, Nav.DistanceTravelled); return;
            }
            Assert.Fail("No native chart lane for actual keyboard motion.");
        }
        private bool Voice(string id)
        {
            if (!effects.TryGetAudioTime(id, out _)) return false;
            var clip = effects.Definition(id).clip;
            return cabin.GetComponent<EffectAudioExecutor>().GetComponentsInChildren<AudioSource>(true)
                .Any(s => s.clip == clip && s.isPlaying && !s.mute && s.volume > .001f);
        }
        private Evidence Row(string zone, string path, string audioId) => new() {
            zone = zone, artwork = path, audioId = audioId, authorityBefore = JsonUtility.ToJson(story.ExportProgress()),
            scope = "Natural production trigger/sequence clock and native W input on the actual water chart. Controlled setup: Debug Start, position/refill and prior mission/cue eligibility seeded; not a full voyage or proof of obtaining those prerequisites." };
        private void Finish(Evidence row, string name)
        {
            row.authorityAfter = JsonUtility.ToJson(story.ExportProgress());
            Assert.That(row.authorityAfter, Is.EqualTo(row.authorityBefore), "A scare cannot alter mission progress.");
            row.noPhotoCreated = cabin.GetComponent<PhotoCaptureService>().Photos.Count == 0;
            Assert.That(row.noPhotoCreated, Is.True);
            File.WriteAllText(Path.Combine(Qa, name + ".json"), JsonUtility.ToJson(row, true));
        }
        private static IEnumerator Shot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Qa, name + ".png"), image.EncodeToPNG()); Object.Destroy(image);
            yield return null;
        }

        [UnityTest, Timeout(90000)] public IEnumerator ZoneTwoNativeApproachShowsPsdGhostWithFizzThenEnds()
        {
            yield return StartZone(2);
            SeedCompletedCurrentZone(true); Assert.That(story.ZoneTwoHullInstalled, Is.True);
            loop.ZoneTwoPresentation.played.AddRange(new[] { "adhesive", "radar", "knocks", "scrape", "following" });
            PlaceOutwardFrom(loop.ActiveMap.exitArea, 120, 205);
            Assert.That(loop.SaveCurrent(), Is.True);
            var controller = cabin.GetComponent<ZoneTwoPresentationController>(); var surface = effects.Surface(EffectTarget.Reflection);
            var row = Row("Zone02", "Assets/_Project/Art/Creatures/Finished/Reserve/Zone02_03_Ghost.png", "z2.audio.electric_fizz");
            float distance = Nav.DistanceTravelled; Key(true);
            yield return Until(() => controller.PendingCue == "reflection", 16, "Actual Helm motion and natural quiet interval must trigger reflection.");
            row.nativeKeyboardMotion = Nav.DistanceTravelled > distance + 1; Assert.That(row.nativeKeyboardMotion, Is.True);
            row.sequenceOnset = Time.realtimeSinceStartup;
            var recording = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 5);
            yield return Until(() => surface.visual.AnomalyVisible && surface.visual.AnomalyGraphic.color.a > .15f, 3, "Native ghost must appear.");
            row.imageAt = Time.realtimeSinceStartup - row.sequenceOnset;
            Assert.That(AssetDatabase.GetAssetPath(surface.visual.AnomalyGraphic.sprite), Is.EqualTo(row.artwork));
            Assert.That(surface.visual.AnomalyGraphic.color.r, Is.EqualTo(1));
            row.sourceVoicePlaying = Voice(row.audioId); Assert.That(row.sourceVoicePlaying, Is.True);
            effects.TryGetAudioTime(row.audioId, out row.audioAtImage);
            yield return Shot("Zone02-native-ghost");
            Key(false);
            yield return Until(() => !surface.visual.AnomalyVisible && effects.ActiveSequenceCount == 0, 6, "Ghost and fizz must end.");
            row.imageOffAt = Time.realtimeSinceStartup - row.sequenceOnset;
            row.cleanup = true;
            Assert.That(loop.ZoneTwoPresentation.Has("reflection"), Is.True);
            row.listener = recording.Finish(Path.Combine(Qa, "Zone02-native-listener.wav"));
            Assert.That(row.listener.nonzero && row.listener.clippedSamples == 0, Is.True);
            yield return Shot("Zone02-recovered");
            Finish(row, "Zone02-native");
        }
        [UnityTest, Timeout(90000)] public IEnumerator ZoneThreeNativeDeepPassageShowsPsdReflectionAndPauseClearsIt()
        {
            yield return StartZone(3);
            var state = story.ExportProgress(); state.completedObjectives.Add("Z3_L2_PHOTO"); story.RestoreProgress(state);
            loop.ZoneThreePresentation.played.AddRange(new[] { "engine", "radar" });
            PlaceOpenLane(165, 640);
            float before = Nav.DistanceTravelled; loop.ZoneThreePresentation.radarDistance = before;
            Assert.That(loop.SaveCurrent(), Is.True);
            var row = Row("Zone03", "Assets/_Project/Art/Creatures/Finished/Reserve/Zone02_03_Ghost.png", "z3.audio.soft_movement");
            Key(true);
            yield return Until(() => Nav.DistanceTravelled - before >= 105, 15, "Native keyboard propulsion must earn passage distance.");
            Key(false); row.nativeKeyboardMotion = true;
            yield return Until(() => loop.ZoneThreePresentation.Has(ZoneThreePresentationState.ReflectionOpportunity), 3, "Deep passage must publish its opportunity.");
            yield return Until(() => effects.IsPlaying("z3.mix.void"), 15, "Natural quiet interval must start the authored sequence.");
            row.sequenceOnset = Time.realtimeSinceStartup;
            var surface = effects.Surface(EffectTarget.Reflection).visual.procedural;
            var recording = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 8);
            yield return Until(() => surface.Visible && surface.ActivePreset == ScreenInterference.ProceduralReflection, 5, "Native PSD reflection must render.");
            row.imageAt = Time.realtimeSinceStartup - row.sequenceOnset;
            Assert.That(AssetDatabase.GetAssetPath(surface.Graphic.texture), Is.EqualTo(row.artwork));
            Assert.That(surface.Graphic.material.GetFloat("_UseArtwork"), Is.EqualTo(1));
            yield return Until(() => Voice(row.audioId), 1, "Movement audio must accompany the ghost.");
            row.sourceVoicePlaying = true; effects.TryGetAudioTime(row.audioId, out row.audioAtImage);
            yield return Until(() => surface.Graphic.material.GetFloat("_Strength") > .7f, 1, "Capture the authored reflection at full fade, not its invisible first frame.");
            yield return Shot("Zone03-native-ghost");
            yield return Until(() => loop.ZoneThreePresentation.Has("reflection"), 2, "Actual visible and audible exposure must commit.");
            cabin.OpenPause(); yield return null; yield return null;
            Assert.That(surface.Visible, Is.False); Assert.That(Voice(row.audioId), Is.False);
            row.cleanup = true; row.imageOffAt = Time.realtimeSinceStartup - row.sequenceOnset;
            row.listener = recording.Finish(Path.Combine(Qa, "Zone03-native-listener.wav"));
            Assert.That(row.listener.nonzero && row.listener.clippedSamples == 0, Is.True);
            PauseMenuController.Instance.Resume(); yield return null; yield return null;
            Assert.That(surface.Visible, Is.False); yield return Shot("Zone03-recovered");
            Finish(row, "Zone03-native");
        }
        [UnityTest, Timeout(90000)] public IEnumerator ZoneFourNativeFinalApproachShowsHelmetAtSignalOnsetThenRecovers()
        {
            yield return StartZone(4);
            SeedCompletedCurrentZone(true);
            loop.CurrentProgress.hiddenRouteUnlocked = true; loop.CurrentProgress.endingChoice = "explore";
            story.HiddenRouteAvailable = true;
            var state = story.ExportProgress(); state.revealedLocations.AddRange(loop.ActiveMap.hiddenLocationIds); story.RestoreProgress(state);
            Assert.That(loop.ActiveMap.HiddenDestinationAvailable(story), Is.True);
            var final = loop.ActiveMap.finalHiddenPoint; bool placed = false;
            for (int i = 0; i < 64 && !placed; i++)
            {
                float angle = i * Mathf.PI / 32; var direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                var position = final.mapPosition + direction * (final.arrivalRadius + 12);
                if (!Lane(position, -direction, 5)) continue;
                Nav.Ship.Refill(); Nav.RestoreVoyage(position, i * 5.625f + 180, loop.ActiveMap.entryDepth, Nav.DistanceTravelled); placed = true;
            }
            Assert.That(placed, Is.True); Assert.That(loop.SaveCurrent(), Is.True);
            var row = Row("Zone04", "Assets/_Project/Art/Creatures/Finished/Reserve/Zone04_HelmetGhost.png", "z4.audio.final_signal");
            float before = Nav.DistanceTravelled; Key(true);
            yield return Until(() => loop.ZoneFourPresentation.Has(ZoneFourPresentationState.FinalSignalOpportunity), 2, "Native multi-frame final-approach motion must earn the signal.");
            Key(false); row.nativeKeyboardMotion = Nav.DistanceTravelled > before;
            Assert.That(row.nativeKeyboardMotion, Is.True);
            Assert.That(final.Contains(Nav.Position), Is.False);
            var controller = cabin.GetComponent<ZoneFourPresentationController>();
            yield return Until(() => controller.PendingCue.StartsWith("final.signal"), 2, "Production controller must start the existing signal.");
            row.sequenceOnset = Time.realtimeSinceStartup;
            var surface = effects.Surface(EffectTarget.Helm).visual.procedural;
            var recording = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 12);
            Assert.That(surface.Visible, Is.False, "No helmet before the delayed onset.");
            yield return Until(() => surface.Visible && surface.ActivePreset == ScreenInterference.ProceduralReflection, 3, "Helmet must appear with delayed signature.");
            row.imageAt = Time.realtimeSinceStartup - row.sequenceOnset;
            Assert.That(row.imageAt, Is.GreaterThan(.9f));
            Assert.That(AssetDatabase.GetAssetPath(surface.Graphic.texture), Is.EqualTo(row.artwork));
            row.sourceVoicePlaying = Voice(row.audioId); Assert.That(row.sourceVoicePlaying, Is.True);
            effects.TryGetAudioTime(row.audioId, out row.audioAtImage);
            Assert.That(row.audioAtImage, Is.LessThan(.3f));
            yield return Until(() => surface.Graphic.material.GetFloat("_Strength") > .75f, 1, "Capture the helmet at peak fade.");
            yield return Shot("Zone04-native-helmet");
            yield return Until(() => !surface.Visible, 3, "Helmet fades out independently of the remaining signal.");
            row.imageOffAt = Time.realtimeSinceStartup - row.sequenceOnset;
            yield return Until(() => effects.ActiveSequenceCount == 0, 11, "The final signal must fully recover.");
            Assert.That(Voice(row.audioId), Is.False);
            Assert.That(loop.EndingReached, Is.Null.Or.Empty); Assert.That(loop.ZoneFourPresentation.Has("final.signal"), Is.True);
            row.cleanup = true; row.listener = recording.Finish(Path.Combine(Qa, "Zone04-native-listener.wav"));
            Assert.That(row.listener.nonzero && row.listener.clippedSamples == 0, Is.True);
            yield return Shot("Zone04-recovered"); Finish(row, "Zone04-native");
        }
        [UnityTest, Timeout(60000)] public IEnumerator FixtureAudienceCloseAndSceneChangeCancelImportedArtWithoutGameplayWrites()
        {
            yield return StartZone(4);
            string authority = JsonUtility.ToJson(story.ExportProgress());
            var reflection = effects.Surface(EffectTarget.Reflection);
            var sequence = cabin.GetComponent<ZoneThreePresentationController>().reflection;
            var handle = effects.PlaySequence(sequence, condition: () => cabin.NavigationPanel.activeInHierarchy);
            Assert.That(handle.IsValid, Is.True);
            yield return Until(() => reflection.visual.procedural.Visible, 5, "Controlled sequence fixture must render authored art.");
            cabin.OpenComputer(); yield return null; yield return null;
            Assert.That(reflection.visual.procedural.Visible, Is.False);
            Assert.That(effects.IsActive(handle), Is.False);
            Assert.That(JsonUtility.ToJson(story.ExportProgress()), Is.EqualTo(authority));
            cabin.OpenNavigation(); yield return null;
            handle = effects.PlaySequence(cabin.GetComponent<ZoneFourPresentationController>().finalSignal,
                condition: () => cabin.NavigationPanel.activeInHierarchy);
            yield return Until(() => effects.Surface(EffectTarget.Helm).visual.procedural.Visible, 3, "Controlled helmet fixture must render.");
            var graphic = effects.Surface(EffectTarget.Helm).visual.procedural.Graphic;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single); yield return null;
            Assert.That(graphic == null, Is.True, "Scene destruction must remove the owned apparition graphic.");
            File.WriteAllText(Path.Combine(Qa, "fixture-cancellation.json"),
                "{\"scope\":\"Direct sequence fixture only; not a natural story trigger\",\"audienceClose\":true,\"sceneDestroy\":true,\"missionUnchanged\":true}");
        }
        private IEnumerator Transition()
        {
            yield return null;
            yield return Until(() => flow != null && !flow.IsTransitioning, 30, "Scene transition must finish.");
            Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
        private static IEnumerator Until(Func<bool> predicate, float seconds, string reason)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(predicate(), Is.True, reason);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Key(false); Time.timeScale = 1; PauseMenuController.Instance?.Resume();
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (flow != null) Object.Destroy(flow.gameObject); yield return null;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetOutputVolume(oldOutput);
                for (int i = 0; i < oldCategories.Length; i++) AudioManager.Instance.SetCategoryVolume((AudioCategory)i, oldCategories[i]);
            }
            foreach (var entry in preferences)
                if (entry.Value.exists) PlayerPrefs.SetFloat(entry.Key, entry.Value.value); else PlayerPrefs.DeleteKey(entry.Key);
            PlayerPrefs.Save();
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
        }
    }
}
#endif

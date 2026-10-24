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
    /// <summary>Actual imported Zone4 catalog through the real debug-start cabin and listener.</summary>
    public sealed class ZoneFourAudioProductionTests
    {
        private sealed class Preference
        {
            public string Key;
            public bool Existed;
            public float Value;
        }

        [Serializable] private sealed class Evidence
        {
            public string setup = "Bootstrap -> Debug Start Zone4; original production catalog, real scene listener; no normalized or substituted audio";
            public string listeningLimit = "Technical runtime evidence only; human headphone aesthetic review remains required.";
            public string gains = "Temporary test output/category gains=1; exact prior preference values and key presence restored by teardown.";
            public string abyss = "The existing production-owned persistent bed is recorded once; no duplicate preview loop is added.";
            public string withdrawal = "Presentation-only seeded Hidden B proximity; no claim of completing hidden-route gameplay.";
            public float engineBeforeWithdrawal, engineAfterWithdrawal, ventBeforeWithdrawal, ventAfterWithdrawal;
            public float environmentAfterWithdrawal, anomalyVolumeMuted, engineDuringAnomalyMute, outputVolumeMuted;
            public bool pausedCueCancelled, sceneSourcesDestroyed;
            public List<string> recordings = new();
        }

        private readonly List<Preference> preferences = new();
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ExpeditionLoop loop;
        private ZoneOneStory story;
        private ZoneFourPresentationController presentation;
        private EffectDirector effects;
        private EffectAudioExecutor executor;
        private AudioManager manager;
        private AudioListener listener;
        private Evidence evidence;
        private float oldOutput;
        private readonly float[] oldCategories = new float[5];
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone04FinalChapter"));
        private const string Engine = "z1.audio.ambient_submarine_engine", Vent = "z1.audio.ambient_cabin_ventilation";

        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            preferences.Clear();
            foreach (string key in new[] { "G10.Audio.OutputVolume" }.Concat(Enum.GetValues(typeof(AudioCategory)).Cast<AudioCategory>()
                .Select(category => "G10.Audio.Category." + category)))
                preferences.Add(new Preference { Key = key, Existed = PlayerPrefs.HasKey(key), Value = PlayerPrefs.GetFloat(key, 1) });
            oldOutput = AudioListener.volume;
            for (int i = 0; i < oldCategories.Length; i++)
                oldCategories[i] = AudioManager.Instance != null ? AudioManager.Instance.CategoryVolume((AudioCategory)i) :
                    PlayerPrefs.GetFloat("G10.Audio.Category." + (AudioCategory)i, 1);
            folder = Path.Combine(Application.temporaryCachePath, "ZoneFourAudio-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return WaitTransition();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>();
            Assert.That(menu, Is.Not.Null); menu.Open(); menu.SelectZone(4); menu.StartRun(); yield return WaitTransition();
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone04"));
            story = cabin.GetComponent<ZoneOneStory>(); presentation = cabin.GetComponent<ZoneFourPresentationController>();
            effects = cabin.GetComponent<EffectDirector>(); executor = cabin.GetComponent<EffectAudioExecutor>();
            manager = AudioManager.Instance; listener = Object.FindAnyObjectByType<AudioListener>();
            Assert.That(presentation, Is.Not.Null); Assert.That(listener, Is.Not.Null); Assert.That(manager, Is.Not.Null);
            manager.SetOutputVolume(1);
            for (int i = 0; i < oldCategories.Length; i++) manager.SetCategoryVolume((AudioCategory)i, 1);
            cabin.ClosePanel(); cabin.Navigation.Brake(); presentation.Tick(0);
            evidence = new Evidence(); Directory.CreateDirectory(Qa);
        }

        private IEnumerator WaitTransition()
        {
            yield return null; float end = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }

        private AudioSource[] Sources(string id)
        {
            var clip = effects.Definition(id)?.clip;
            return executor.GetComponentsInChildren<AudioSource>(true).Where(s => s.isPlaying && s.clip != null && s.clip == clip).ToArray();
        }

        private AudioSource SingleVoice(string id)
        {
            var sources = Sources(id); Assert.That(sources, Has.Length.EqualTo(1), id + " must have one real playing voice.");
            return sources[0];
        }

        private IEnumerator Record(string name, float seconds)
        {
            var recorder = ZoneThreeAudioEvidence.Begin(listener, Mathf.Min(20, seconds + 1));
            yield return new WaitForSecondsRealtime(seconds);
            var metrics = recorder.Finish(Path.Combine(Qa, "M3-" + name + "-listener.wav"));
            string json = Path.Combine(Qa, "M3-" + name + "-listener.json");
            File.WriteAllText(json, JsonUtility.ToJson(metrics, true)); evidence.recordings.Add(json);
            Assert.That(metrics.nonzero, Is.True, name + " must contain the actual mixed scene output.");
            Assert.That(metrics.durationSeconds, Is.GreaterThan(seconds * .75f), name + " listener callback must run.");
            Assert.That(metrics.clippedSamples, Is.Zero, name + " raw listener clipping must not be hidden by normalization.");
            yield return null; // Finish destroys its test-only recorder at frame end.
        }

        private IEnumerator PlayAndRecord(string name)
        {
            string id = "z4.audio." + name;
            var definition = effects.Definition(id);
            var handle = effects.Play(id);
            Assert.That(handle.IsValid, Is.True, "The real catalog definition must be playable: " + id);
            yield return null; SingleVoice(id);
            yield return Record(name.Replace('_', '-'), definition.clip.length + .15f);
            Assert.That(effects.IsActive(handle), Is.False, "A one-shot must finish without leaving a director request.");
            Assert.That(Sources(id), Is.Empty, "A one-shot must release its actual voice after its clip.");
            Assert.That(SingleVoice(Engine).volume, Is.GreaterThan(0)); Assert.That(SingleVoice(Vent).volume, Is.GreaterThan(0));
        }

        [UnityTest, Timeout(180000)] public IEnumerator ImportedFamiliesMixWithMachineryAndRespectWithdrawalCategoryPauseAndSceneLifetime()
        {
            foreach (string name in new[] { "fragment_recovery", "abyss_sparse_water", "distant_structure", "hidden_signal_a",
                "hidden_signal_b", "final_signal", "normal_ending_settle", "hidden_ending_tail" })
            {
                var definition = effects.Definition("z4.audio." + name);
                Assert.That(definition, Is.Not.Null, "Install the authored Zone4 production catalog: " + name);
                Assert.That(definition.clip, Is.Not.Null); Assert.That(definition.clip.name, Is.EqualTo(name));
                Assert.That(definition.clip.channels, Is.EqualTo(2)); Assert.That(definition.clip.frequency, Is.EqualTo(44100));
                Assert.That(definition.clip.samples, Is.GreaterThan(0));
                Assert.That(definition.clip.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
                Assert.That(definition.clip.loadState, Is.EqualTo(AudioDataLoadState.Loaded));
                Assert.That(definition.loop, Is.EqualTo(name == "abyss_sparse_water"));
            }
            yield return new WaitForSecondsRealtime(2.7f);
            // Advance the real sparse scheduler once and cancel that probe, leaving its normal
            // 47-second quiet interval around the individual production demonstrations below.
            presentation.Tick(60); effects.StopChannel("z4.audio.distant_structure");
            Assert.That(SingleVoice(Engine).volume, Is.GreaterThan(0)); Assert.That(SingleVoice(Vent).volume, Is.GreaterThan(0));
            Assert.That(SingleVoice("z4.audio.abyss_sparse_water").loop, Is.True);
            string progress = JsonUtility.ToJson(story.ExportProgress());
            yield return Record("baseline", 1.5f);
            yield return Record("abyss-sparse-water", 8.2f);
            yield return PlayAndRecord("distant_structure");
            yield return PlayAndRecord("hidden_signal_a");
            yield return PlayAndRecord("hidden_signal_b");
            Assert.That(JsonUtility.ToJson(story.ExportProgress()), Is.EqualTo(progress), "Audio previews cannot grant discoveries or mission progress.");

            // Isolate the existing presentation rule with a seeded proximity fixture. It does
            // not stand in for an actual hidden-route discovery or its gameplay acceptance.
            evidence.engineBeforeWithdrawal = SingleVoice(Engine).volume;
            evidence.ventBeforeWithdrawal = SingleVoice(Vent).volume;
            // Progression re-applies this canonical route state each frame. Seed both
            // owners for this presentation-only proximity probe, not a transient flag.
            loop.CurrentProgress.hiddenRouteUnlocked = true;
            loop.CurrentProgress.endingChoice = "explore";
            story.HiddenRouteAvailable = true;
            var seeded = story.ExportProgress(); seeded.revealedLocations.Add("Z4_L4"); story.RestoreProgress(seeded);
            var hiddenB = story.survey.FindPoi("zone04-l5"); Assert.That(hiddenB, Is.Not.Null);
            cabin.Navigation.RestoreVoyage(story.survey.ContactPosition(hiddenB), 0, story.survey.DepthFor(hiddenB), cabin.Navigation.DistanceTravelled);
            presentation.Tick(4); yield return new WaitForSecondsRealtime(1.1f);
            evidence.environmentAfterWithdrawal = presentation.EnvironmentLevel;
            evidence.engineAfterWithdrawal = SingleVoice(Engine).volume; evidence.ventAfterWithdrawal = SingleVoice(Vent).volume;
            Assert.That(evidence.environmentAfterWithdrawal, Is.LessThan(.04f));
            Assert.That(evidence.engineAfterWithdrawal, Is.EqualTo(evidence.engineBeforeWithdrawal).Within(.0001f));
            Assert.That(evidence.ventAfterWithdrawal, Is.EqualTo(evidence.ventBeforeWithdrawal).Within(.0001f));
            Assert.That(SingleVoice("z4.audio.abyss_sparse_water").volume, Is.LessThan(.01f));
            yield return PlayAndRecord("final_signal");

            var categoryProbe = effects.Play("z4.audio.final_signal"); Assert.That(categoryProbe.IsValid, Is.True);
            yield return null; Assert.That(SingleVoice("z4.audio.final_signal").volume, Is.GreaterThan(0));
            manager.SetCategoryVolume(AudioCategory.Anomalies, 0); yield return null; yield return null;
            evidence.anomalyVolumeMuted = SingleVoice("z4.audio.final_signal").volume;
            evidence.engineDuringAnomalyMute = SingleVoice(Engine).volume;
            Assert.That(evidence.anomalyVolumeMuted, Is.Zero); Assert.That(evidence.engineDuringAnomalyMute, Is.GreaterThan(0));
            manager.SetOutputVolume(0); evidence.outputVolumeMuted = manager.OutputVolume;
            Assert.That(evidence.outputVolumeMuted, Is.Zero); Assert.That(SingleVoice(Engine).volume, Is.GreaterThan(0), "Output gain must not rewrite machinery gain.");
            manager.SetOutputVolume(1); manager.SetCategoryVolume(AudioCategory.Anomalies, 1);
            cabin.OpenPause(); yield return null; presentation.Tick(0); yield return null;
            evidence.pausedCueCancelled = !effects.IsActive(categoryProbe);
            Assert.That(evidence.pausedCueCancelled, Is.True); Assert.That(Sources("z4.audio.final_signal"), Is.Empty);
            Assert.That(Sources("z1.audio.sfx_steering_servo"), Is.Empty); Assert.That(Sources("z1.audio.sfx_depth_pressure_adjustment"), Is.Empty);
            PauseMenuController.Instance.Resume(); yield return null; yield return null;
            Assert.That(effects.IsActive(categoryProbe), Is.False, "Resuming must not replay the cancelled anomaly.");
            Assert.That(Sources("z1.audio.sfx_steering_servo"), Is.Empty); Assert.That(Sources("z1.audio.sfx_depth_pressure_adjustment"), Is.Empty);
            var sceneVoices = executor.GetComponentsInChildren<AudioSource>(true);
            flow.LoadMainMenu(); yield return WaitTransition();
            evidence.sceneSourcesDestroyed = sceneVoices.All(source => source == null);
            Assert.That(evidence.sceneSourcesDestroyed, Is.True, "Leaving Zone4 must destroy every scene-local presentation voice.");
            File.WriteAllText(Path.Combine(Qa, "audio-m3-runtime-evidence.json"), JsonUtility.ToJson(evidence, true));
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; PauseMenuController.Instance?.Resume();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetOutputVolume(oldOutput);
                for (int i = 0; i < oldCategories.Length; i++) AudioManager.Instance.SetCategoryVolume((AudioCategory)i, oldCategories[i]);
            }
            else AudioListener.volume = oldOutput;
            foreach (var preference in preferences)
            {
                if (preference.Existed) PlayerPrefs.SetFloat(preference.Key, preference.Value);
                else PlayerPrefs.DeleteKey(preference.Key);
            }
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject); yield return null;
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            string absolute = Path.GetFullPath(folder), temporary = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (absolute.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(absolute).StartsWith("ZoneFourAudio-", StringComparison.Ordinal) && Directory.Exists(absolute))
                Directory.Delete(absolute, true);
        }
    }
}

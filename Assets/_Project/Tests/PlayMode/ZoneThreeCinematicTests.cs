using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <summary>Actual shared-cabin production demos. Evidence is written only when these tests run.</summary>
    public sealed class ZoneThreeCinematicTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ZoneOneStory story;
        private ExpeditionLoop loop;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private ZoneThreePresentationController presentation;
        private EffectDirector effects;
        private RockBreakerSequence rock;
        private ZoneNavigation Nav => cabin.Navigation;
        private CreatureCatcher Catcher => cabin.GetComponent<CreatureCatcher>();
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone03Cinematic"));
        [Serializable] private sealed class AuthorityState
        {
            public ShipState ship;
            public Vector2 position;
            public float heading, depth, distance;
            public string mission, receipts;
            public int photos, contacts;
        }
        [Serializable] private sealed class Evidence
        {
            public string demo, status = "passed automated checks; human audio/visual review remains required", utc;
            public int width, height;
            public float changedFraction, meanChannelDifference;
            public string[] playingClipBindings;
        }
        private sealed class Frame { public Color32[] pixels; public int width, height; }
        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "ZoneThreeCinematic-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return Wait();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(3); menu.StartRun(); yield return Wait();
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            presentation = cabin.GetComponent<ZoneThreePresentationController>(); effects = cabin.GetComponent<EffectDirector>();
            rock = cabin.GetComponent<RockBreakerSequence>();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone03"));
            Assert.That(presentation, Is.Not.Null, "Install the production Zone 3 controller and assets first."); presentation.Tick(0);
            foreach (var sequence in new[] { presentation.delayedEngine, presentation.falseRadar, presentation.reflection, presentation.assembly, presentation.demolition, presentation.specimen07 })
            {
                Assert.That(sequence, Is.Not.Null); Assert.That(sequence.cues, Is.Not.Empty);
                foreach (var cue in sequence.cues) Assert.That(cue.effect, Is.Not.Null);
            }
            Assert.That(rock.productionSequence, Is.SameAs(presentation.demolition));
            foreach (var audience in new[] { EffectTarget.Radar, EffectTarget.Reflection, EffectTarget.PhotoLab, EffectTarget.Global })
            { Assert.That(effects.Surface(audience)?.visual?.procedural?.compositionTemplate, Is.Not.Null, audience.ToString()); }
            cabin.ClosePanel(); Nav.Brake(); yield return null;
        }
        private IEnumerator Wait()
        {
            yield return null; float until = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
        private void Place(string poiId)
        {
            var poi = story.survey.FindPoi(poiId); Assert.That(poi, Is.Not.Null); Nav.Ship.Refill();
            Nav.RestoreVoyage(story.survey.ContactPosition(poi) + Vector2.down * 14, 0, story.survey.DepthFor(poi), Nav.DistanceTravelled);
        }
        private IEnumerator Survey(string poiId, bool capture)
        {
            yield return new WaitForSeconds(.65f); Place(poiId); cabin.ClosePanel(); int before = photos.Photos.Count; cabin.OpenCamera();
            float deadline = Time.realtimeSinceStartup + 4;
            while ((photos.Photos.Count == before || cabin.IsDirectInteractionActive) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(photos.Photos.Count, Is.EqualTo(before + 1)); var photo = photos.Photos.Last();
            Assert.That(photo.MissionZoneId, Is.EqualTo("Zone03"), photo.Result); Assert.That(photo.MissionObjectiveId, Is.Not.Null.And.Not.Empty);
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            var lab = computer.GetComponentInChildren<PhotoLabView>(true); Assert.That(lab.sendButton.interactable, Is.True);
            lab.sendButton.onClick.Invoke(); yield return null; Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.True);
            cabin.ClosePanel(); Place(poiId);
            if (capture)
            {
                Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started)); CaptureMinigamePlayModeTests.Win(Catcher.minigame);
                Assert.That(Catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            }
            yield return null;
        }
        private string Authority() => JsonUtility.ToJson(new AuthorityState
        {
            ship = Nav.Ship.Export(), position = Nav.Position, heading = Nav.Heading, depth = Nav.Depth, distance = Nav.DistanceTravelled,
            mission = JsonUtility.ToJson(story.ExportProgress()), receipts = JsonUtility.ToJson(loop.ZoneThreePresentation),
            photos = photos.Photos.Count, contacts = cabin.Radar.VisibleContactCount
        });
        private AudioSource[] Voices() => cabin.GetComponent<EffectAudioExecutor>().GetComponentsInChildren<AudioSource>(true)
            .Where(source => source.isPlaying && source.clip != null).ToArray();
        private bool Playing(string clip) => Voices().Any(source => source.clip.name == clip);
        private static IEnumerator Shot(string name, Action<Frame> result = null)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture(); Assert.That(texture, Is.Not.Null);
            try
            {
                Assert.That(texture.width, Is.GreaterThan(100)); Assert.That(texture.height, Is.GreaterThan(100));
                Directory.CreateDirectory(Qa); File.WriteAllBytes(Path.Combine(Qa, name + ".png"), texture.EncodeToPNG());
                result?.Invoke(new Frame { pixels = texture.GetPixels32(), width = texture.width, height = texture.height });
            }
            finally { Object.Destroy(texture); }
        }
        private static RectInt ScreenRegion(EffectSurface surface, Frame frame)
        {
            var rect = (surface.visibilityRoot != null ? surface.visibilityRoot.transform : surface.transform) as RectTransform;
            Assert.That(rect, Is.Not.Null); var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]), max = min;
            foreach (var corner in corners)
            { var point = RectTransformUtility.WorldToScreenPoint(camera, corner); min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
            int x = Mathf.Clamp(Mathf.FloorToInt(min.x), 0, frame.width - 1), y = Mathf.Clamp(Mathf.FloorToInt(min.y), 0, frame.height - 1);
            return new RectInt(x, y, Mathf.Clamp(Mathf.CeilToInt(max.x) - x, 1, frame.width - x), Mathf.Clamp(Mathf.CeilToInt(max.y) - y, 1, frame.height - y));
        }
        private static Evidence Difference(string demo, Frame before, Frame after, RectInt region, IEnumerable<string> heard)
        {
            Assert.That(after.width, Is.EqualTo(before.width)); Assert.That(after.height, Is.EqualTo(before.height));
            int changed = 0, placeholder = 0; long difference = 0;
            for (int y = region.yMin; y < region.yMax; y++) for (int x = region.xMin; x < region.xMax; x++)
            {
                var a = before.pixels[y * before.width + x]; var b = after.pixels[y * after.width + x];
                int delta = Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b); difference += delta;
                if (delta > 18) changed++;
                if (b.r > 190 && b.b > 190 && b.g < 60 || b.r < 40 && b.g > 220 && b.b > 220) placeholder++;
            }
            int count = region.width * region.height; Assert.That(count, Is.GreaterThan(100));
            Assert.That(placeholder / (float)count, Is.LessThan(.35f), "Reject a monitor filled by an uncompiled shader placeholder.");
            return new Evidence { demo = demo, utc = DateTime.UtcNow.ToString("o"), width = after.width, height = after.height,
                changedFraction = changed / (float)count, meanChannelDifference = difference / (count * 3f * 255), playingClipBindings = heard.Distinct().ToArray() };
        }
        private static void SaveEvidence(Evidence evidence)
        { Directory.CreateDirectory(Qa); File.WriteAllText(Path.Combine(Qa, evidence.demo + ".json"), JsonUtility.ToJson(evidence, true)); }
        private IEnumerator Until(Func<bool> predicate, float seconds, string reason)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(predicate(), Is.True, reason);
        }

        [UnityTest] public IEnumerator DemoARealCaptureAndPropulsionStopTriggerDistinctReflectedAudioOnce()
        {
            yield return Survey("zone03-l1", true); Assert.That(story.HasObjective("Z3_L1_CAPTURE"), Is.True);
            effects.ClearTransients(); cabin.OpenNavigation();
            var audioEvidence = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 15);
            // Navigation driver uses an open chart to isolate the audio stop edge from terrain collisions.
            Nav.ConfigureMapCoordinates(new Vector2(2000, 1000), 50); var water = new byte[200 * 100]; Array.Fill(water, (byte)1); Nav.SetChart(water, 200, 100);
            Nav.RestoreVoyage(new Vector2(800, 500), 90, 400, Nav.DistanceTravelled); cabin.Hold(1); Nav.Navigate(1, 0, 0, 2.2f);
            Assert.That(Nav.Speed, Is.GreaterThan(.1f)); presentation.Tick(4.2f);
            yield return Shot("A-01-real-engine-moving"); Assert.That(Nav.Speed, Is.GreaterThan(.1f));
            // Keep real held propulsion audible in the listener recording before the stop edge.
            yield return new WaitForSeconds(2.2f); Assert.That(Playing("ambient_propulsion_thrust"), Is.True);
            cabin.Release(1); cabin.Brake(); presentation.Tick(.01f);
            Assert.That(presentation.EngineEventCount, Is.EqualTo(1)); Assert.That(Playing("engine_reflection"), Is.True);
            Assert.That(Playing("ambient_propulsion_thrust"), Is.False, "True propulsion ends at the stop edge.");
            Assert.That(Playing("ambient_submarine_engine"), Is.True, "Normal cabin machinery stays present.");
            var echo = Voices().Single(source => source.clip.name == "engine_reflection");
            Assert.That(echo.clip, Is.Not.SameAs(effects.Definition("z1.audio.ambient_submarine_engine").clip));
            Assert.That(echo.clip.samples, Is.GreaterThan(1000)); string authority = Authority();
            yield return new WaitForSeconds(.35f); Assert.That(Playing("engine_reflection"), Is.True); Assert.That(Authority(), Is.EqualTo(authority));
            yield return Shot("A-02-stopped-engine-reflection");
            SaveEvidence(new Evidence { demo = "A-delayed-engine", utc = DateTime.UtcNow.ToString("o"), width = Screen.width, height = Screen.height,
                playingClipBindings = Voices().Select(source => source.clip.name).ToArray() });
            yield return Until(() => !Playing("engine_reflection"), presentation.delayedEngine.Duration + 1, "Echo should finish.");
            Nav.Navigate(1, 0, 0, .2f); presentation.Tick(4.2f); Nav.Brake(); presentation.Tick(.1f);
            Assert.That(presentation.EngineEventCount, Is.EqualTo(1)); Assert.That(loop.ZoneThreePresentation.Has("engine"), Is.True);
            var mix = audioEvidence.Finish(Path.Combine(Qa, "A-gameplay-listener.wav"));
            File.WriteAllText(Path.Combine(Qa, "A-gameplay-listener.json"), JsonUtility.ToJson(mix, true));
            Assert.That(mix.nonzero, Is.True, "Record the real listener mix, without substituting source WAVs.");
            Assert.That(mix.durationSeconds, Is.GreaterThan(1)); Assert.That(mix.clippedSamples, Is.Zero);
        }
        [UnityTest] public IEnumerator RealSurveyScanAndTravelCommitRadarAndReflectionOnceAcrossContinue()
        {
            yield return Survey("zone03-l2", false);
            Assert.That(story.HasObjective("Z3_L2_PHOTO"), Is.True);
            Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
            Assert.That(loop.ZoneThreePresentation.Has("reflection"), Is.False);
            cabin.OpenRadar(); presentation.Tick(4.1f);
            int radarCharges = Nav.Ship.Radar;
            cabin.Scan(); Assert.That(cabin.Radar.IsScanning, Is.True);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(radarCharges - 1));
            presentation.Tick(0);
            Assert.That(presentation.RadarEventCount, Is.EqualTo(1));
            Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.True);
            float scanDistance = loop.ZoneThreePresentation.radarDistance;
            Assert.That(scanDistance, Is.EqualTo(Nav.DistanceTravelled));
            var radarSurface = effects.Surface(EffectTarget.Radar);
            yield return Until(() => Playing("false_radar_echo") &&
                radarSurface.visual.procedural.ActivePreset == ScreenInterference.FalseRadarSweep,
                2, "A real charged scan must start the authored false sweep after the sent L2 photo.");
            cabin.Scan(); presentation.Tick(4.1f);
            Assert.That(presentation.RadarEventCount, Is.EqualTo(1));
            Assert.That(Nav.Ship.Radar, Is.EqualTo(radarCharges - 1), "The false sweep and an already-running scan never spend a second charge.");
            yield return Until(() => effects.ActiveSequenceCount == 0, presentation.falseRadar.Duration + 1, "The false sweep must finish before the passage event.");

            cabin.OpenNavigation();
            // Use the actual navigation driver on open water; moving the saved position cannot earn passage distance.
            Nav.ConfigureMapCoordinates(new Vector2(2000, 1000), 50);
            var water = new byte[200 * 100]; Array.Fill(water, (byte)1); Nav.SetChart(water, 200, 100);
            Nav.RestoreVoyage(new Vector2(800, 500), 90, 400, Nav.DistanceTravelled);
            for (int step = 0; step < 80 && Nav.DistanceTravelled - scanDistance < 110; step++) Nav.Navigate(1, 0, 0, .5f);
            Nav.Brake();
            Assert.That(Nav.DistanceTravelled - scanDistance, Is.GreaterThanOrEqualTo(100));
            // Advance only the observation clock, as in the real stop-edge test above.
            presentation.Tick(8.1f);
            Assert.That(presentation.ReflectionCount, Is.EqualTo(1));
            Assert.That(loop.ZoneThreePresentation.Has("reflection"), Is.True);
            var reflectionSurface = effects.Surface(EffectTarget.Reflection);
            yield return Until(() => Playing("soft_movement") &&
                reflectionSurface.visual.procedural.ActivePreset == ScreenInterference.ProceduralReflection,
                5, "Actual travel and a quiet helm must start the authored reflection sequence.");
            presentation.Tick(8.1f); Assert.That(presentation.ReflectionCount, Is.EqualTo(1));
            yield return Until(() => effects.ActiveSequenceCount == 0, presentation.reflection.Duration + 1, "The reflection must recover before Continue.");
            Place("zone03-l2");
            string receipts = JsonUtility.ToJson(loop.ZoneThreePresentation);
            Assert.That(loop.SaveCurrent(), Is.True);
            flow.LoadMainMenu(); yield return Wait(); flow.ContinueDebugGame(); yield return Wait();
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); presentation = cabin.GetComponent<ZoneThreePresentationController>();
            effects = cabin.GetComponent<EffectDirector>(); presentation.Tick(0);
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone03"));
            Assert.That(story.HasObjective("Z3_L2_PHOTO"), Is.True);
            Assert.That(JsonUtility.ToJson(loop.ZoneThreePresentation), Is.EqualTo(receipts));
            Assert.That(Nav.DistanceTravelled - loop.ZoneThreePresentation.radarDistance, Is.GreaterThanOrEqualTo(100));
            cabin.OpenRadar(); radarCharges = Nav.Ship.Radar; cabin.Scan();
            Assert.That(cabin.Radar.IsScanning, Is.True); presentation.Tick(4.1f);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(radarCharges - 1));
            Assert.That(presentation.RadarEventCount, Is.Zero, "Continue retains the radar receipt even when another real scan qualifies.");
            Assert.That(Playing("false_radar_echo"), Is.False);
            cabin.OpenNavigation(); presentation.Tick(8.1f); yield return null;
            Assert.That(presentation.ReflectionCount, Is.Zero, "The earned passage remains eligible, but its saved reflection receipt prevents replay.");
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Playing("soft_movement"), Is.False);
            Assert.That(JsonUtility.ToJson(loop.ZoneThreePresentation), Is.EqualTo(receipts));
        }
        [UnityTest] public IEnumerator DemoBProductionFalseSweepChangesRadarPixelsWithoutAnyGameplayAuthority()
        {
            cabin.OpenRadar(); cabin.Radar.ResetScanState(); yield return null;
            Frame before = null, after = null; yield return Shot("B-01-radar-before", frame => before = frame);
            string authority = Authority(); int count = presentation.RadarEventCount;
            Assert.That(presentation.Preview(ZoneThreeDemo.FalseRadar).IsValid, Is.True);
            var surface = effects.Surface(EffectTarget.Radar);
            yield return Until(() => surface.visual.procedural.ActivePreset == ScreenInterference.FalseRadarSweep, 2, "The production secondary sweep must be visible.");
            yield return new WaitForSeconds(1.2f); Assert.That(Playing("false_radar_echo"), Is.True);
            yield return Shot("B-02-false-secondary-sweep", frame => after = frame);
            var evidence = Difference("B-false-radar", before, after, ScreenRegion(surface, after), Voices().Select(source => source.clip.name));
            Assert.That(evidence.changedFraction, Is.GreaterThan(.003f), "A real radar display must visibly change.");
            Assert.That(cabin.Radar.IsScanning, Is.False); Assert.That(Authority(), Is.EqualTo(authority));
            Assert.That(presentation.RadarEventCount, Is.EqualTo(count), "Preview neither writes a story receipt nor charges a real scan.");
            yield return Until(() => !surface.visual.procedural.Visible, presentation.falseRadar.Duration + 1, "Sweep must recover.");
            yield return Shot("B-03-radar-recovered"); SaveEvidence(evidence);
        }
        [UnityTest] public IEnumerator DemoCRealMonitorReflectionHasMeasuredPixelsAndPauseCancelsEveryOwnedTransient()
        {
            cabin.OpenNavigation(); yield return null; Frame before = null, after = null;
            yield return Shot("C-01-monitor-before", frame => before = frame); string authority = Authority();
            Assert.That(presentation.Preview(ZoneThreeDemo.Reflection).IsValid, Is.True); var surface = effects.Surface(EffectTarget.Reflection);
            yield return Until(() => surface.visual.procedural.ActivePreset == ScreenInterference.ProceduralReflection, 5, "The actual monitor must show its procedural reflection.");
            yield return new WaitForSeconds(.3f); Assert.That(Playing("soft_movement"), Is.True);
            Assert.That(presentation.EnvironmentLevel, Is.LessThan(.4f));
            yield return Shot("C-02-procedural-glass-reflection", frame => after = frame);
            var evidence = Difference("C-reflection", before, after, ScreenRegion(surface, after), Voices().Select(source => source.clip.name));
            Assert.That(evidence.changedFraction, Is.GreaterThan(.08f)); Assert.That(Authority(), Is.EqualTo(authority));
            Assert.That(surface.visual.AnomalyVisible, Is.False, "The old crew sprite must not be substituted for the procedural composition.");
            cabin.OpenPause(); yield return null; yield return null;
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(surface.visual.procedural.Visible, Is.False);
            Assert.That(Playing("soft_movement"), Is.False); Assert.That(Playing("engine_reflection"), Is.False);
            PauseMenuController.Instance.Resume(); yield return null; yield return null;
            Assert.That(surface.visual.procedural.Visible, Is.False); Assert.That(Playing("ambient_submarine_engine"), Is.True);
            Assert.That(cabin.GetComponent<EffectAudioExecutor>().GetComponentsInChildren<AudioSource>(true).Length, Is.LessThanOrEqualTo(16));
            yield return Shot("C-03-monitor-recovered-after-pause"); SaveEvidence(evidence);
        }
        [UnityTest] public IEnumerator DemoDRealDemolitionLayersAudioResumesAtElapsedTimeAndRevealsTerrainAtOnePointFiveSeconds()
        {
            yield return Survey("zone03-l1", true); yield return Survey("zone03-l2", false); yield return Survey("zone03-l4", true);
            Assert.That(story.MainLocationsComplete, Is.True); Assert.That(story.ApplyProgressionAction(), Is.True);
            Assert.That(Playing("rock_breaker_assembly"), Is.True); Assert.That(story.ApplyProgressionAction(), Is.True);
            effects.ClearTransients(); cabin.ClosePanel(); var area = loop.ActiveMap.rockInteractionArea;
            Nav.RestoreVoyage(area.mapPosition, 90, story.survey.DepthFor(area), Nav.DistanceTravelled);
            Frame before = null, after = null; yield return Shot("D-01-cabin-before-break", frame => before = frame);
            // PNG encoding is synchronous; let that capture frame drain before starting
            // a .28-second real-time cue, so the QA capture cannot skip its whole attack.
            yield return null; yield return null;
            var audioEvidence = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 20);
            Assert.That(story.ApplyProgressionAction(), Is.True); Assert.That(rock.IsActive, Is.True); Assert.That(rock.Duration, Is.EqualTo(10));
            Assert.That(rock.DeferTerrainReveal, Is.True); Assert.That(Nav.CanOccupy(loop.ActiveMap.exitArea.mapPosition), Is.False);
            Assert.That(Playing("underwater_detonation"), Is.True); var heard = new HashSet<string> { "underwater_detonation" };
            // Capture the actual rendered pressure peak, after its first update, rather than
            // the scheduling frame at age zero before the overlay has been composited.
            yield return Until(() => rock.Elapsed >= .1f, 2, "The pressure pulse must reach its visible attack.");
            Assert.That(effects.Surface(EffectTarget.Global).overlay.tint.color.a, Is.GreaterThan(.05f),
                "Pressure elapsed=" + rock.Elapsed + ", frame=" + Time.unscaledDeltaTime + ", active=" +
                effects.IsPlaying("z3.visual.pressure") + ", suppressed=" + effects.Suppressed +
                ", audience=" + effects.Surface(EffectTarget.Global).Available);
            Frame pressureFrame = null;
            yield return Shot("D-02-pressure-impact", frame => pressureFrame = frame);
            var pressureEvidence = Difference("D-pressure", before, pressureFrame,
                new RectInt(0, pressureFrame.height / 2, pressureFrame.width, pressureFrame.height / 2), heard);
            Assert.That(pressureEvidence.changedFraction, Is.GreaterThan(.25f));
            Assert.That(pressureEvidence.meanChannelDifference, Is.GreaterThan(.03f), "A pressure still must show more than a changed status label or subpixel shake.");
            SaveEvidence(pressureEvidence);
            yield return Until(() => rock.Elapsed >= .25f, 3, "Initial impact should run.");
            foreach (var voice in Voices()) heard.Add(voice.clip.name);
            Time.timeScale = 0; float pausedAt = rock.Elapsed; Assert.That(pausedAt, Is.LessThan(1.5f)); yield return new WaitForSecondsRealtime(.25f);
            Assert.That(rock.Elapsed, Is.EqualTo(pausedAt)); Assert.That(Playing("underwater_detonation"), Is.False);
            Time.timeScale = 1; yield return null; yield return null;
            var resumed = Voices().SingleOrDefault(source => source.clip.name == "underwater_detonation");
            Assert.That(resumed, Is.Not.Null); Assert.That(resumed.time, Is.GreaterThanOrEqualTo(pausedAt - .12f), "Resume seeks the production cue instead of replaying the explosion.");
            Assert.That(rock.DeferTerrainReveal, Is.True);
            yield return Until(() => rock.Elapsed >= 1.55f, 3, "Authoritative reveal timing should advance.");
            Assert.That(rock.DeferTerrainReveal, Is.False); Assert.That(Nav.CanOccupy(loop.ActiveMap.exitArea.mapPosition), Is.True);
            yield return Shot("D-03-layered-collapse-debris", frame => after = frame);
            var global = effects.Surface(EffectTarget.Global);
            Assert.That(global.visual.procedural.ActivePreset, Is.EqualTo(ScreenInterference.RockDebris));
            var evidence = Difference("D-demolition", before, after, new RectInt(0, 0, after.width, after.height), heard);
            Assert.That(evidence.changedFraction, Is.GreaterThan(.003f));
            float end = Time.realtimeSinceStartup + 12;
            while (rock.IsActive && Time.realtimeSinceStartup < end)
            { foreach (var voice in Voices()) heard.Add(voice.clip.name); yield return null; }
            Assert.That(rock.IsActive, Is.False); Assert.That(rock.SequenceCount, Is.EqualTo(1));
            Assert.That(Nav.ExpeditionBlocked, Is.False, "The ten-second gameplay lock must end independently of the audio tail.");
            Assert.That(Playing("post_explosion_resonance"), Is.True, "The authored receding resonance remains after stabilization.");
            yield return Until(() => !Playing("post_explosion_resonance"), presentation.demolition.Duration + 1, "The bounded receding tail must finish.");
            foreach (string clip in new[] { "underwater_detonation", "rock_collapse_01", "rock_collapse_02", "rock_collapse_03", "post_explosion_resonance" })
            { Assert.That(heard, Does.Contain(clip)); Assert.That(Playing(clip), Is.False); }
            Assert.That(story.HasWorldFlag("ROCK_BARRIER_DESTROYED"), Is.True); Assert.That(loop.CurrentProgress.exitUnlocked, Is.True);
            Assert.That(story.ApplyProgressionAction(), Is.False); Assert.That(global.visual.procedural.Visible, Is.False);
            yield return Shot("D-04-cabin-stabilized"); evidence.playingClipBindings = heard.ToArray(); SaveEvidence(evidence);
            var mix = audioEvidence.Finish(Path.Combine(Qa, "D-gameplay-listener.wav"));
            File.WriteAllText(Path.Combine(Qa, "D-gameplay-listener.json"), JsonUtility.ToJson(mix, true));
            Assert.That(mix.nonzero, Is.True, "Record the actual layered demolition mix.");
            Assert.That(mix.durationSeconds, Is.GreaterThan(10)); Assert.That(mix.clippedSamples, Is.Zero);
        }
        [UnityTest] public IEnumerator ScopedPhotoEffectStaysBehindForegroundWindowsAndReflectionFollowsTiltedNativeGlass()
        {
            yield return Survey("zone03-l2", false); cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            effects.ClearTransients(); var lab = computer.GetComponentInChildren<PhotoLabView>(true);
            var photoSurface = effects.Surface(EffectTarget.PhotoLab); Frame baseline = null, composed = null;
            yield return Shot("E-01-photo-before-scoped-effect", frame => baseline = frame);
            // Extend only the lifetime so window movement/captures can inspect the same production shader/strength.
            var handle = effects.Play("z3.visual.photo-reflection", new EffectContext { Duration = 20 }); Assert.That(handle.IsValid, Is.True);
            yield return new WaitForSeconds(.5f);
            var graphic = photoSurface.visual.procedural.Graphic;
            Assert.That(graphic, Is.Not.Null); Assert.That(graphic.raycastTarget, Is.False);
            Assert.That(graphic.transform.parent, Is.SameAs(lab.preview.transform));
            Assert.That(graphic.GetComponentInParent<ComputerWindow>(), Is.SameAs(lab.GetComponent<ComputerWindow>()));
            Assert.That(graphic.rectTransform.rect.size, Is.EqualTo(lab.preview.rectTransform.rect.size));
            yield return Shot("E-02-photo-with-scoped-effect", frame => composed = frame);
            var visible = Difference("E-scoped-photo", baseline, composed, ScreenRegion(photoSurface, composed), Voices().Select(source => source.clip.name));
            Assert.That(visible.changedFraction, Is.GreaterThan(.01f), "The production photo effect must render before testing occlusion.");
            computer.OpenMissionLog(); var front = computer.GetComponentInChildren<MissionLogView>().GetComponent<ComputerWindow>();
            Assert.That(front, Is.Not.Null); if (!front.Maximized) front.ToggleMaximize(); yield return null; yield return null;
            Assert.That(lab.gameObject.activeInHierarchy, Is.True, "A background desktop window stays open; this must be real sibling occlusion.");
            Assert.That(effects.IsActive(handle), Is.True); Assert.That(graphic.enabled, Is.True);
            Frame covered = null, uncoveredEffect = null;
            yield return Shot("E-03-foreground-window-over-effect", frame => covered = frame);
            effects.Stop(handle); yield return Shot("E-04-foreground-window-baseline", frame => uncoveredEffect = frame);
            var hidden = Difference("E-scoped-photo-occlusion", covered, uncoveredEffect, ScreenRegion(photoSurface, covered), Array.Empty<string>());
            Assert.That(hidden.changedFraction, Is.LessThan(.005f), "A background photo effect must not paint over the foreground mission window.");

            cabin.ClosePanel(); cabin.OpenNavigation(); yield return null;
            var miniMap = (RectTransform)cabin.NavigationPanel.transform.Find("MiniMap");
            Assert.That(Quaternion.Angle(miniMap.rotation, Quaternion.identity), Is.GreaterThan(.1f), "Check the authored tilted glass, not an axis-aligned test rectangle.");
            var reflection = effects.Play("z3.visual.reflection", new EffectContext { Duration = 5 }); Assert.That(reflection.IsValid, Is.True);
            // The authored .18 s attack begins at zero opacity; the lazy graphic is created
            // by a later production render rather than synchronously by Play().
            yield return Until(() => effects.Surface(EffectTarget.Reflection).visual.procedural.ActivePreset == ScreenInterference.ProceduralReflection,
                1, "Wait for the production reflection attack before inspecting its native glass geometry.");
            var reflectionGraphic = effects.Surface(EffectTarget.Reflection).visual.procedural.Graphic;
            Assert.That(reflectionGraphic, Is.Not.Null); Assert.That(reflectionGraphic.transform.parent, Is.SameAs(miniMap));
            Assert.That(Quaternion.Angle(reflectionGraphic.rectTransform.rotation, miniMap.rotation), Is.LessThan(.01f));
            var expected = new Vector3[4]; var actual = new Vector3[4]; miniMap.GetWorldCorners(expected); reflectionGraphic.rectTransform.GetWorldCorners(actual);
            for (int i = 0; i < 4; i++) Assert.That(Vector3.Distance(expected[i], actual[i]), Is.LessThan(.1f));
            yield return Shot("E-05-reflection-follows-tilted-glass"); effects.Stop(reflection);
            SaveEvidence(visible); SaveEvidence(hidden);
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

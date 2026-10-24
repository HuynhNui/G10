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
    public sealed class ZoneTwoEnvironmentalHorrorTests
    {
        private string folder, oldSave, oldPhotos;
        private CabinStationView cabin;
        private ZoneOneStory story;
        private ZoneTwoPresentationController presentation;
        private ExpeditionLoop loop;
        private EffectDirector effects;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private ZoneNavigation Nav => cabin.Navigation;
        private CreatureCatcher Catcher => cabin.GetComponent<CreatureCatcher>();
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone02EnvironmentalHorror"));
        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "ZoneTwo-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json"); PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer(); Assert.That(ExpeditionSaveStore.TryRead(out var save, out _), Is.True);
            save.current.zone = "Zone02"; save.current.zones.Clear(); save.current.zones.Add(new ExpeditionZoneState { zone = "Zone02", deadline = 15 });
            save.hasDayStart = false; save.dayStart = null; ExpeditionSaveStore.Write(save, true);
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return Load();
        }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive); yield return null; yield return null;
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); presentation = cabin.GetComponent<ZoneTwoPresentationController>();
            effects = cabin.GetComponent<EffectDirector>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone02"));
            Assert.That(story.UsesZoneTwoHullStory, Is.True); presentation.Tick(0);
        }
        private void BindLive()
        {
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); presentation = cabin.GetComponent<ZoneTwoPresentationController>();
            effects = cabin.GetComponent<EffectDirector>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
        }
        private IEnumerator ContinueThroughBootstrap()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            float until = Time.realtimeSinceStartup + 12;
            while ((SceneFlowController.Instance == null || SceneManager.GetActiveScene().name != "MainMenu" || SceneFlowController.Instance.IsTransitioning) && Time.realtimeSinceStartup < until) yield return null;
            var flow = SceneFlowController.Instance; Assert.That(flow, Is.Not.Null); flow.ContinueGame(); yield return null;
            until = Time.realtimeSinceStartup + 12;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); BindLive();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; PauseMenuController.Instance?.Resume();
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject); yield return null;
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        private void Place(string id)
        {
            var poi = story.survey.FindPoi(id); Nav.Ship.Refill();
            Nav.RestoreVoyage(story.survey.ContactPosition(poi) + Vector2.down * 14, 0, story.survey.DepthFor(poi), Nav.DistanceTravelled);
        }
        private IEnumerator SendAndCapture(string id, bool inspectLab = false)
        {
            Place(id); cabin.ClosePanel(); int count = photos.Photos.Count; cabin.OpenCamera();
            yield return null; yield return null; yield return null;
            Assert.That(photos.Photos.Count, Is.EqualTo(count + 1)); var photo = photos.Photos.Last();
            Assert.That(photo.MissionZoneId, Is.EqualTo("Zone02")); Assert.That(photo.MissionObjectiveId, Is.Not.Null.And.Not.Empty, photo.Result);
            Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.False);
            Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.PhotoRequired));
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            var lab = computer.GetComponentInChildren<PhotoLabView>(true); Assert.That(lab.sendButton.interactable, Is.True);
            if (inspectLab) yield return Screenshot("01-specimen-photo-lab");
            lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.True);
            cabin.ClosePanel(); Place(id);
            Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            CaptureMinigamePlayModeTests.Win(Catcher.minigame);
            Assert.That(Catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(cabin.GetComponent<CabinFeedbackController>().HasPendingScrape, Is.False, "No random post-capture jumpscare in Zone 2.");
            yield return new WaitForSeconds(.7f);
        }
        private IEnumerator Fieldwork()
        { yield return SendAndCapture("zone02-l1"); yield return SendAndCapture("zone02-l3"); }
        private void Sail(float distance, Vector2? point = null)
        {
            // Open-water driver isolates presentation gating; the authored chart is checked separately.
            Nav.ConfigureMapCoordinates(new Vector2(2000, 1000), 50); var cells = new byte[200 * 100]; Array.Fill(cells, (byte)1); Nav.SetChart(cells, 200, 100);
            cabin.OpenNavigation(); Nav.Ship.Refill(); Nav.RestoreVoyage(point ?? new Vector2(800, 500), 90, 300,
                (loop.ZoneTwoPresentation?.firstCaptureDistance ?? 0) + distance);
            Nav.Navigate(1, 0, 0, .1f); Assert.That(Nav.IsMoving, Is.True);
        }
        private bool Playing(string id) => effects.IsPlaying("z2.audio." + id);
        private void CrossExit()
        {
            Nav.ConfigureMapCoordinates(new Vector2(2000, 1000), 50); var cells = new byte[200 * 100]; Array.Fill(cells, (byte)1); Nav.SetChart(cells, 200, 100);
            var exit = loop.ActiveMap.exitArea; Nav.Ship.Refill();
            Nav.RestoreVoyage(exit.mapPosition + Vector2.up * (exit.arrivalRadius + 2), 180, 300, 1900);
            var route = cabin.GetComponent<ExpeditionProgression>(); route.Evaluate(); Assert.That(route.CanExit(), Is.False);
            Nav.Navigate(1, 0, 0, 1.2f); route.Evaluate();
            Assert.That(SceneFlowController.Instance.IsTransitioning, Is.True, "Only crossing into an armed physical gate starts travel.");
        }
        private IEnumerator ToScrape()
        {
            yield return SendAndCapture("zone02-l1");
            Sail(200); presentation.Tick(9); cabin.OpenRadar(); cabin.Scan(); presentation.Tick(0);
            Assert.That(presentation.RadarEventCount, Is.EqualTo(1)); yield return new WaitForSeconds(3);
            Sail(400); presentation.Tick(11); Assert.That(presentation.KnockCount, Is.EqualTo(1));
            yield return new WaitForSeconds(3.9f);
            Sail(650); cabin.OpenComputer(); presentation.Tick(13); Assert.That(presentation.ScrapeEventCount, Is.EqualTo(1));
        }
        private static IEnumerator Screenshot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            try { Directory.CreateDirectory(Qa); File.WriteAllBytes(Path.Combine(Qa, name + ".png"), image.EncodeToPNG()); }
            finally { Object.Destroy(image); }
        }
        [UnityTest] public IEnumerator TwoRealQuickPhotosSendCaptureAndHullTransactionPersist750m()
        {
            Assert.That(story.config.locations, Has.Length.EqualTo(2)); Assert.That(Nav.Ship.MaximumDepth, Is.EqualTo(500));
            Assert.That(story.survey.FindPoi("zone02-l3").targetDepth, Is.LessThanOrEqualTo(500));
            yield return SendAndCapture("zone02-l1", true); yield return SendAndCapture("zone02-l3");
            Assert.That(story.CanInstallZoneTwoHull, Is.True); Assert.That(story.HasZone("Zone03"), Is.False);
            cabin.OpenComputer(); computer.OpenUpgrade(); float hull = Nav.Ship.Hull, energy = Nav.Ship.Energy;
            var ui = computer.GetComponentInChildren<SubmarineUpgradeUIController>(true);
            var module = ui.GetComponentsInChildren<UpgradeEntryConfig>(true).Single(e => e.UpgradeId == "ExpeditionModule");
            ui.Select(module); yield return null;
            Assert.That(story.PendingGateObjective.materials, Has.Length.EqualTo(2));
            var install = ui.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b =>
                b.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => t.text == "INSTALL PRESSURE HULL"));
            Assert.That(install.interactable, Is.True); yield return Screenshot("02a-pressure-hull-materials");
            install.onClick.Invoke(); presentation.Tick(0);
            Assert.That(Nav.Ship.MaximumDepth, Is.EqualTo(750)); Assert.That(story.HasZone("Zone03"), Is.True);
            Assert.That(story.inventory.GetCount("Z2_Creature_01"), Is.Zero); Assert.That(story.inventory.GetCount("Z2_Creature_02"), Is.Zero);
            Assert.That(story.ApplyProgressionAction(), Is.False); Assert.That(Nav.Ship.Hull, Is.EqualTo(hull)); Assert.That(Nav.Ship.Energy, Is.EqualTo(energy));
            Assert.That(Playing("hull_install"), Is.True); yield return new WaitForSeconds(3.2f);
            var view = computer.GetComponentInChildren<ZoneTwoHullView>(true); Assert.That(view.IsVisible, Is.True);
            Assert.That(view.message.text, Does.Contain("750 M")); Assert.That(view.card.blocksRaycasts, Is.False);
            view.message.ForceMeshUpdate(); Assert.That(view.message.isTextOverflowing, Is.False);
            yield return Screenshot("02-pressure-hull-installed"); Assert.That(loop.SaveCurrent(), Is.True);
            yield return Load(); Assert.That(Nav.Ship.MaximumDepth, Is.EqualTo(750)); Assert.That(story.ZoneTwoHullInstalled, Is.True);
            Assert.That(story.inventory.GetCount("Z2_Creature_01"), Is.Zero); Assert.That(Playing("hull_install"), Is.False);
            Assert.That(cabin.GetComponentInChildren<PhotoCameraView>(true).gameObject.activeInHierarchy, Is.False);
        }
        [UnityTest] public IEnumerator FailedInstallSaveRestoresMaterialsDepthGateAndAudio()
        {
            yield return Fieldwork(); string valid = ExpeditionSaveStore.PathOverride;
            string blocked = Path.Combine(folder, "blocked"); Directory.CreateDirectory(blocked); ExpeditionSaveStore.PathOverride = blocked;
            try
            {
                Assert.That(story.ApplyProgressionAction(), Is.False); Assert.That(Nav.Ship.MaximumDepth, Is.EqualTo(500));
                Assert.That(story.inventory.GetCount("Z2_Creature_01"), Is.EqualTo(1)); Assert.That(story.inventory.GetCount("Z2_Creature_02"), Is.EqualTo(1));
                Assert.That(story.ZoneTwoHullInstalled, Is.False); Assert.That(story.HasZone("Zone03"), Is.False); Assert.That(Playing("hull_install"), Is.False);
            }
            finally { ExpeditionSaveStore.PathOverride = valid; }
            Assert.That(story.ApplyProgressionAction(), Is.True);
        }
        [UnityTest] public IEnumerator RadarGateUsesActualScanAndKnocksHaveNoVisualsOrDamage()
        {
            Sail(600); cabin.OpenRadar(); cabin.Scan(); presentation.Tick(20); Assert.That(presentation.RadarEventCount, Is.Zero);
            yield return SendAndCapture("zone02-l1"); Sail(200); presentation.Tick(9); Assert.That(presentation.RadarEventCount, Is.Zero);
            cabin.OpenRadar(); cabin.Scan(); presentation.Tick(0); Assert.That(presentation.RadarEventCount, Is.EqualTo(1));
            int pois = story.survey.locations.Length; float hull = Nav.Ship.Hull;
            yield return new WaitForSeconds(3); Sail(400); presentation.Tick(11);
            Assert.That(presentation.KnockCount, Is.EqualTo(1)); Assert.That(Playing("knocks_rear"), Is.True);
            Assert.That(effects.ActiveSequenceCount, Is.Zero, "Zone 2 knocks intentionally have no screen sequence.");
            var surface = effects.Surface(EffectTarget.Global);
            Assert.That(surface.shake.shake.IsShaking, Is.False); Assert.That(surface.overlay.tint.color.a, Is.EqualTo(.10f).Within(.001f));
            Assert.That(Nav.Ship.Hull, Is.EqualTo(hull)); Assert.That(story.survey.locations.Length, Is.EqualTo(pois));
            yield return new WaitForSeconds(4); Assert.That(Playing("knocks_rear"), Is.False);
            Sail(500); presentation.Tick(20); Assert.That(presentation.KnockCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator ScrapeGlitchesComputerAndDimsTheWholeScreenTwiceWithoutDamage()
        {
            yield return ToScrape();
            var global = effects.Surface(EffectTarget.Global); float hull = Nav.Ship.Hull;
            bool signal = false, first = false, second = false; int flashes = 0; bool dark = false;
            float until = Time.realtimeSinceStartup + 10;
            while (effects.ActiveSequenceCount > 0 && Time.realtimeSinceStartup < until)
            {
                signal |= effects.Surface(EffectTarget.Computer).visual.GlitchEnabled;
                float alpha = global.overlay.tint.color.a;
                bool now = alpha > .16f;
                if (now && !dark) flashes++;
                dark = now;
                if (!first && alpha > .20f) { first = true; yield return Screenshot("03-first-screen-dip"); }
                if (!second && alpha > .34f) { second = true; yield return Screenshot("04-second-screen-dip"); }
                Assert.That(effects.Surface(EffectTarget.Reflection).visual.AnomalyVisible, Is.False);
                yield return null;
            }
            Assert.That(signal && first && second, Is.True); Assert.That(flashes, Is.EqualTo(2));
            Assert.That(global.overlay.tint.rectTransform.rect.width, Is.GreaterThanOrEqualTo(1900));
            Assert.That(global.overlay.tint.rectTransform.rect.height, Is.GreaterThanOrEqualTo(1000));
            Assert.That(Nav.Ship.Hull, Is.EqualTo(hull)); Assert.That(global.shake.shake.IsShaking, Is.False);
            Assert.That(effects.Surface(EffectTarget.Computer).visual.SignalVisible, Is.False);
            Assert.That(global.overlay.tint.color.a, Is.EqualTo(.1f).Within(.01f));
        }
        [UnityTest] public IEnumerator ReflectionIsBriefClippedToGlassAndNeverBecomesAPhotoOrContact()
        {
            yield return Fieldwork(); Assert.That(story.ApplyProgressionAction(), Is.True); presentation.Tick(0);
            yield return new WaitForSeconds(4.3f);
            int photosBefore = photos.Photos.Count; int contacts = story.survey.locations.Length;
            Sail(1900, new Vector2(1800, 350)); presentation.Tick(9);
            Assert.That(presentation.ReflectionCount, Is.EqualTo(1));
            var surface = effects.Surface(EffectTarget.Reflection); float start = -1, end = -1;
            float until = Time.realtimeSinceStartup + 4; bool captured = false;
            while (effects.ActiveSequenceCount > 0 && Time.realtimeSinceStartup < until)
            {
                if (surface.visual.AnomalyVisible)
                {
                    if (start < 0) start = Time.realtimeSinceStartup;
                    Assert.That(surface.visual.GetComponentInChildren<Coffee.UIEffects.UIEffect>(), Is.Not.Null);
                    Assert.That(surface.GetComponent<UnityEngine.UI.RectMask2D>(), Is.Not.Null);
                    if (!captured) { captured = true; yield return Screenshot("05-glass-reflection"); }
                }
                else if (start >= 0 && end < 0) end = Time.realtimeSinceStartup;
                yield return null;
            }
            if (end < 0) end = Time.realtimeSinceStartup;
            Assert.That(captured, Is.True); Assert.That(end - start, Is.InRange(.08f, .40f));
            Assert.That(photos.Photos.Count, Is.EqualTo(photosBefore)); Assert.That(story.survey.locations.Length, Is.EqualTo(contacts));
            Assert.That(surface.visual.AnomalyVisible, Is.False); Assert.That(loop.SaveCurrent(), Is.True);
            yield return Load(); Sail(1900, new Vector2(1800, 350)); presentation.Tick(20); Assert.That(presentation.ReflectionCount, Is.Zero);
        }
        [UnityTest] public IEnumerator PauseRollbackAndZoneChangesCancelTransientChildrenAndOwnedLoops()
        {
            yield return SendAndCapture("zone02-l1"); Nav.Brake(); Assert.That(loop.Rest(), Is.True);
            Sail(200); presentation.Tick(9); cabin.OpenRadar(); cabin.Scan(); presentation.Tick(0);
            Assert.That(presentation.RadarEventCount, Is.EqualTo(1)); cabin.OpenPause(); yield return null; yield return null;
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Playing("ambient_roots"), Is.False);
            PauseMenuController.Instance.Resume(); yield return null; presentation.Tick(1); Assert.That(Playing("ambient_roots"), Is.True);
            cabin.OpenRadar(); cabin.Scan(); presentation.Tick(20); Assert.That(presentation.RadarEventCount, Is.EqualTo(1));
            Assert.That(loop.RestoreDay(1), Is.True); Sail(200); presentation.Tick(9); cabin.OpenRadar(); cabin.Scan(); presentation.Tick(0);
            Assert.That(presentation.RadarEventCount, Is.EqualTo(2)); Assert.That(loop.RestoreDay(1), Is.True); yield return null;
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(effects.Surface(EffectTarget.Radar).visual.SignalVisible, Is.False);
            presentation.enabled = false; yield return null; Assert.That(Playing("ambient_roots"), Is.False);
            Assert.That(effects.IsPlaying("z2.screen.cold"), Is.False);
        }
        [Test] public void LegacyCompletedZoneTwoKeepsItsRouteAndDoesNotGrantCargo()
        {
            int cargo = story.inventory.Items.Count;
            var old = new MissionProgressState(); old.completedObjectives.AddRange(new[] { "Z2_L1_PHOTO", "Z2_L2_COLLECT", "Z2_L3_PHOTO", "Z2_L3_CAPTURE", "Z2_GATE_INSTALL_BIO_LAMP" });
            old.unlockedZones.Add("Zone03");
            var snapshot = new ExpeditionSnapshot(); var zone = new ExpeditionZoneState { zone = "Zone02", missionProgress = old.Copy() };
            snapshot.zones.Add(zone); MissionLayoutMigration.Apply(snapshot); story.RestoreProgress(zone.missionProgress);
            Assert.That(story.ZoneTwoHullInstalled, Is.False, "Legacy route entitlement never fabricates an installation or capture receipt.");
            Assert.That(story.CanExitZone, Is.True);
            Assert.That(story.HasZone("Zone03"), Is.True); Assert.That(story.inventory.Items.Count, Is.EqualTo(cargo));
            Assert.That(old.completedObjectives, Does.Not.Contain(ZoneOneStory.ZoneTwoHullObjective), "Do not mutate a caller's checkpoint.");
        }
        [UnityTest] public IEnumerator DepartureFadesRootsAndStopsEveryOwnedVoice()
        {
            presentation.BeginDeparture(); Assert.That(Playing("far_root_groan"), Is.True);
            yield return new WaitForSeconds(4.5f);
            Assert.That(presentation.IsDeparting, Is.False); Assert.That(Playing("far_root_groan"), Is.False);
        }
        [UnityTest] public IEnumerator PhysicalExitPresentsZoneThreeAndCarriesHullWithoutRootVoices()
        {
            yield return Fieldwork(); Assert.That(story.ApplyProgressionAction(), Is.True); Assert.That(loop.SaveCurrent(), Is.True);
            yield return ContinueThroughBootstrap(); Assert.That(Nav.Ship.MaximumDepth, Is.EqualTo(750));
            var flow = SceneFlowController.Instance; CrossExit();
            float until = Time.realtimeSinceStartup + 16; bool departure = false, title = false;
            while (Time.realtimeSinceStartup < until)
            {
                departure |= presentation != null && presentation.IsDeparting && Playing("far_root_groan");
                title |= Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("TÀN TÍCH VÔ DANH"));
                if (loop.Zone == "Zone03" && !flow.IsTransitioning) break;
                yield return null;
            }
            Assert.That(loop.Zone, Is.EqualTo("Zone03"), flow.LastError); Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(departure && title, Is.True); yield return null;
            Assert.That(Nav.Ship.MaximumDepth, Is.EqualTo(750)); Assert.That(Playing("ambient_roots"), Is.False); Assert.That(Playing("far_root_groan"), Is.False);
            Assert.That(effects.IsPlaying("z2.screen.cold"), Is.False); Assert.That(effects.Surface(EffectTarget.Reflection).visual.AnomalyVisible, Is.False);
        }
        [UnityTest] public IEnumerator PhysicalArrivalShowsZoneTwoTitleStartsRootsAndStopsTheSong()
        {
            Assert.That(ExpeditionSaveStore.TryRead(out var save, out _), Is.True);
            save.current.zone = "Zone01"; save.current.zoneOneIntroPending = false; save.current.zones.Clear();
            var zone = new ExpeditionZoneState { zone = "Zone01", deadline = 15 };
            zone.missionProgress.completedObjectives.AddRange(new[] { ZoneOneStory.PhotoOneObjective, ZoneOneStory.AlgaeCaptureObjective, ZoneOneStory.PhotoTwoObjective, ZoneOneStory.BioLampObjective });
            zone.missionProgress.unlockedZones.Add("Zone02"); zone.missionProgress.worldFlags.Add(ZoneOneStory.BioLampOnline);
            save.current.zones.Add(zone); save.hasDayStart = false; save.dayStart = null; ExpeditionSaveStore.Write(save, true);
            yield return ContinueThroughBootstrap(); Assert.That(loop.Zone, Is.EqualTo("Zone01"));
            CrossExit();
            var flow = SceneFlowController.Instance; float until = Time.realtimeSinceStartup + 15; bool title = false;
            while (Time.realtimeSinceStartup < until)
            {
                title |= Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("CỔ THỤ LINH HỒN"));
                if (loop.Zone == "Zone02" && !flow.IsTransitioning) break;
                yield return null;
            }
            Assert.That(loop.Zone, Is.EqualTo("Zone02"), flow.LastError); Assert.That(flow.IsTransitioning, Is.False); Assert.That(title, Is.True);
            yield return null; yield return null; Assert.That(Playing("ambient_roots"), Is.True);
            Assert.That(effects.IsPlaying("z1.audio.ambient_singing_algae"), Is.False); Assert.That(Nav.Ship.MaximumDepth, Is.EqualTo(500));
        }
        [UnityTest] public IEnumerator ReflectionRendersWithoutAnUncompiledShaderPlaceholder()
        {
            cabin.OpenNavigation();
            var surface = effects.Surface(EffectTarget.Reflection);
            Assert.That(surface.visual.reflectionVariants, Is.Not.Null);
            Assert.That(surface.visual.reflectionVariants.isWarmedUp, Is.True);
            var d = presentation.reflection.cues.Single(c => c.effect.target == EffectTarget.Reflection).effect;
            var handle = effects.Play(d, new EffectContext { Duration = 3 });
            yield return null; yield return null; yield return new WaitForEndOfFrame();
            var image = surface.visual.GetComponentInChildren<UnityEngine.UI.Image>();
            Directory.CreateDirectory(Qa);
            File.WriteAllText(Path.Combine(Qa, "reflection-material.json"), JsonUtility.ToJson(new ReflectionProbe {
                shader = image.materialForRendering.shader.name, texture = image.mainTexture.name, color = image.color.ToString(),
                canvasChannels = image.canvas.additionalShaderChannels.ToString(), keywords = string.Join(",", image.materialForRendering.shaderKeywords) }, true));
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                var colors = frame.GetPixels32();
                Assert.That(colors.Count(c => c.r < 26 && c.g > 240 && c.b > 240), Is.LessThan(100), "A cold shader must not replace the reflection with a cyan rectangle.");
                File.WriteAllBytes(Path.Combine(Qa, "06-reflection-library-ready.png"), frame.EncodeToPNG());
            }
            finally { Object.Destroy(frame); effects.Stop(handle); }
        }
        [Serializable] private sealed class ReflectionProbe { public string shader, texture, color, canvasChannels, keywords; }
        [UnityTest] public IEnumerator GlobalDimmingChangesPixelsAcrossMapRadarComputerAndPhotoLab()
        {
            var results = new ScreenProbeResults();
            foreach (string panel in new[] { "Map", "Radar", "Computer", "PhotoLab" })
            {
                cabin.ClosePanel();
                if (panel == "Map") cabin.OpenMap();
                else if (panel == "Radar") cabin.OpenRadar();
                else { cabin.OpenComputer(); if (panel == "PhotoLab") computer.OpenPhotoLab(); }
                yield return null; yield return new WaitForEndOfFrame();
                var baseline = ScreenCapture.CaptureScreenshotAsTexture(); float before;
                try { before = Brightness(baseline); } finally { Object.Destroy(baseline); }
                var handle = effects.Play("z2.screen.reflection-dip", new EffectContext { Duration = 1 });
                yield return null; yield return new WaitForEndOfFrame();
                var dim = ScreenCapture.CaptureScreenshotAsTexture();
                try
                {
                    float after = Brightness(dim); results.panels.Add(new ScreenProbe { panel = panel, baseline = before, dim = after });
                    Assert.That(after, Is.LessThan(before * .85f), panel + " must visibly dim across the viewport, not just an in-cabin light.");
                    if (panel == "Computer") { Directory.CreateDirectory(Qa); File.WriteAllBytes(Path.Combine(Qa, "07-global-screen-dim.png"), dim.EncodeToPNG()); }
                }
                finally { Object.Destroy(dim); effects.Stop(handle); }
            }
            Directory.CreateDirectory(Qa); File.WriteAllText(Path.Combine(Qa, "global-screen-pixels.json"), JsonUtility.ToJson(results, true));
        }
        private static float Brightness(Texture2D texture)
        {
            var colors = texture.GetPixels32(); double total = 0;
            foreach (var c in colors) total += c.r * .2126 + c.g * .7152 + c.b * .0722;
            return (float)(total / (colors.Length * 255));
        }
        [Serializable] private sealed class ScreenProbe { public string panel; public float baseline, dim; }
        [Serializable] private sealed class ScreenProbeResults { public System.Collections.Generic.List<ScreenProbe> panels = new(); }
    }
}

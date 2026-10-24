using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// <summary>Real optional-site photographs. The route is seeded; full choice/radar progression is tested separately.</summary>
    public sealed class ZoneFourHiddenVisualTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ExpeditionLoop loop;
        private ZoneOneStory story;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone04HiddenScenes"));
        [Serializable] private sealed class PhotoEvidence
        {
            public string poi, result, beforeHash, afterHash, archiveHash, timelineHash;
            public string artLimitation = "Null Zone4 creature binding uses existing generic algae; no replacement art was assigned.";
            public float coverageBefore, coverageAfter, occlusionBefore, occlusionAfter;
            public int changedPixels, changedOutsideDetail;
            public Rect detailRect;
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "ZoneFourHiddenVisual-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return WaitTransition();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(4); menu.StartRun(); yield return WaitTransition();
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone04"));
            cabin.ClosePanel(); Directory.CreateDirectory(Qa);
        }

        private void SeedOptionalRoute()
        {
            // Presentation-only fixture: seed both canonical owners so Evaluate cannot replace a transient flag.
            // These steps are not evidence for the real Continue choice or radar discovery acceptance.
            loop.CurrentProgress.hiddenRouteUnlocked = true; loop.CurrentProgress.endingChoice = "explore";
            story.HiddenRouteAvailable = true;
            Assert.That(story.RevealPoi("zone04-l3"), Is.True); Assert.That(story.RevealPoi("zone04-l5"), Is.True);
            Assert.That(loop.SaveCurrent(), Is.True, loop.LastError);
        }

        private void Place(string poiId, bool faceAway = false)
        {
            var poi = story.survey.FindPoi(poiId); Assert.That(poi, Is.Not.Null);
            Vector2 contact = story.survey.ContactPosition(poi), position = contact + Vector2.down * 14;
            cabin.Navigation.Ship.Refill();
            Assert.That(cabin.Navigation.IsWater(position), Is.True, "Use the authored water chart, not a substitute test map.");
            cabin.Navigation.RestoreVoyage(position, faceAway ? 180 : 0, story.survey.DepthFor(poi), cabin.Navigation.DistanceTravelled);
            Assert.That(story.survey.Detectable(cabin.Navigation, poi, photos.profile.visibleDistance), Is.True, "The real site needs a clear authored sightline.");
        }

        private IEnumerator Photograph(string poiId, bool expectSubject = true, bool faceAway = false)
        {
            yield return new WaitForSeconds(.65f); cabin.ClosePanel(); Place(poiId, faceAway);
            int count = photos.Photos.Count; cabin.OpenCamera();
            yield return Until(() => photos.Photos.Count == count + 1 && !cabin.IsDirectInteractionActive, 4, "The real quick shutter must finish.");
            var photo = photos.Photos.Last(); Assert.That(photo.IsMissionPhoto, Is.False);
            if (expectSubject)
            {
                Assert.That(photo.MissionZoneId, Is.EqualTo("Zone04"), photo.Result);
                Assert.That(photo.MissionPoiId, Is.EqualTo(poiId), photo.Result);
                Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.False, "Shutter cannot replace manual SEND.");
            }
            else Assert.That(photo.MissionObjectiveId, Is.Null.Or.Empty);
        }

        private IEnumerator Lab()
        {
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            Assert.That(computer.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
            var lab = computer.GetComponentInChildren<PhotoLabView>(true);
            Assert.That(lab.SelectedPhotoId, Is.EqualTo(photos.Photos.Last().Id));
            Assert.That(lab.preview.texture, Is.SameAs(photos.Photos.Last().Image));
        }

        [UnityTest] public IEnumerator ActualHiddenAAndBKeepDistinctBackgroundEvidenceThroughLabAndManualSend()
        {
            SeedOptionalRoute();
            string[] sites = { "zone04-l3", "zone04-l5" }, objectives = { "Z4_L3_PHOTO", "Z4_L4_PHOTO" };
            ZoneFourPhotoDetail[] kinds = { ZoneFourPhotoDetail.SedimentFins, ZoneFourPhotoDetail.AsymmetricShadow };
            var bindings = sites.Select(site => story.ContentForPoi(site).Image).ToArray();
            for (int site = 0; site < sites.Length; site++)
            {
                string label = site == 0 ? "C-A" : "C-B";
                photos.ZoneFourComposition.Enabled = false;
                yield return Photograph(sites[site]); var plain = photos.Photos.Last();
                float coverage = photos.LastSubjectCoverage, occlusion = photos.LastSubjectOcclusion;
                Assert.That(photos.ZoneFourComposition.LastDetailKind, Is.EqualTo(ZoneFourPhotoDetail.None));
                yield return Lab(); yield return Screenshot(label + "-01-before-detail-in-real-lab");
                var lab = computer.GetComponentInChildren<PhotoLabView>(true);
                Vector3 buttonPosition = lab.sendButton.transform.position; Rect previewRect = lab.preview.rectTransform.rect;

                photos.ZoneFourComposition.Enabled = true;
                yield return Photograph(sites[site]); var composed = photos.Photos.Last();
                var evidence = new PhotoEvidence { poi = sites[site], result = composed.Result,
                    coverageBefore = coverage, coverageAfter = photos.LastSubjectCoverage,
                    occlusionBefore = occlusion, occlusionAfter = photos.LastSubjectOcclusion,
                    detailRect = photos.ZoneFourComposition.LastDetailRect };
                Assert.That(photos.ZoneFourComposition.LastDetailKind, Is.EqualTo(kinds[site]));
                Assert.That(photos.ZoneFourComposition.LastWreckComposited, Is.False);
                Assert.That(composed.Result, Is.EqualTo(plain.Result)); Assert.That(composed.MissionObjectiveId, Is.EqualTo(objectives[site]));
                Assert.That(evidence.coverageAfter, Is.EqualTo(coverage)); Assert.That(evidence.occlusionAfter, Is.EqualTo(occlusion));
                ComparePixels(plain.Image, composed.Image, evidence);
                Assert.That(evidence.changedPixels, Is.GreaterThan(30), "The real detail must survive fog and foreground composition.");
                Assert.That(evidence.changedOutsideDetail, Is.Zero, "A site detail must not repaint pixels beyond its authored region.");
                Assert.That(evidence.changedPixels / (float)(PhotoLayerComposer.Width * PhotoLayerComposer.Height), Is.LessThan(site == 0 ? .08f : .62f));
                byte[] beforeBytes = plain.Image.EncodeToPNG(), bytes = composed.Image.EncodeToPNG();
                evidence.beforeHash = Hash(beforeBytes); evidence.afterHash = Hash(bytes);
                Assert.That(evidence.afterHash, Is.Not.EqualTo(evidence.beforeHash));
                byte[] archived = File.ReadAllBytes(Path.Combine(photos.ArchivePath, composed.Id + ".png"));
                CollectionAssert.AreEqual(bytes, archived); evidence.archiveHash = Hash(archived);
                File.WriteAllBytes(Path.Combine(Qa, label + "-before-photograph.png"), beforeBytes);
                File.WriteAllBytes(Path.Combine(Qa, label + "-authored-photograph.png"), bytes);

                yield return Lab();
                Assert.That(lab.sendButton.transform.position, Is.EqualTo(buttonPosition)); Assert.That(lab.preview.rectTransform.rect, Is.EqualTo(previewRect));
                Assert.That(lab.sendButton.interactable, Is.True); Assert.That(story.HasObjective(objectives[site]), Is.False);
                yield return Screenshot(label + "-02-authored-detail-in-real-lab");
                lab.sendButton.onClick.Invoke(); yield return null;
                Assert.That(composed.IsMissionPhoto, Is.True); Assert.That(plain.IsMissionPhoto, Is.False);
                Assert.That(story.HasObjective(objectives[site]), Is.True); Assert.That(lab.sendButton.interactable, Is.False);
                Assert.That(photos.SubmitPhoto(composed), Is.False, "Repeat SEND cannot duplicate an objective.");
                Assert.That(story.inventory.GetCount(site == 0 ? "Z4_Creature_03" : "Z4_Creature_04"), Is.Zero);
                Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
                byte[] durable = Convert.FromBase64String(saved.current.photos.Single(photo => photo.id == composed.Id).png);
                CollectionAssert.AreEqual(bytes, durable); evidence.timelineHash = Hash(durable);
                File.WriteAllText(Path.Combine(Qa, label + "-photograph-evidence.json"), JsonUtility.ToJson(evidence, true));
            }
            for (int i = 0; i < sites.Length; i++) Assert.That(story.ContentForPoi(sites[i]).Image, Is.SameAs(bindings[i]), "Never overwrite the artist's binding.");
            Assert.That(loop.EndingReached, Is.Null.Or.Empty); Assert.That(flow.IsTransitioning, Is.False);
        }

        [UnityTest] public IEnumerator AbyssGradeDarkensOnlyPhotoEnvironmentWithoutChangingSubjectValidationOrOtherZones()
        {
            photos.ZoneFourComposition.Enabled = false; photos.ZoneFourComposition.EnvironmentEnabled = false;
            yield return Photograph("zone04-l1"); var bright = photos.Photos.Last();
            float coverage = photos.LastSubjectCoverage, occlusion = photos.LastSubjectOcclusion;
            photos.ZoneFourComposition.EnvironmentEnabled = true;
            yield return Photograph("zone04-l1"); var dark = photos.Photos.Last();
            Assert.That(dark.Result, Is.EqualTo(bright.Result)); Assert.That(dark.MissionObjectiveId, Is.EqualTo(bright.MissionObjectiveId));
            Assert.That(photos.LastSubjectCoverage, Is.EqualTo(coverage)); Assert.That(photos.LastSubjectOcclusion, Is.EqualTo(occlusion));
            // Open water above/right of the specimen: no foreground plant or authored site detail.
            var water = new Rect(.79f, .68f, .16f, .20f);
            float before = Luminance(bright.Image, water), after = Luminance(dark.Image, water);
            Assert.That(before, Is.GreaterThan(.05f)); Assert.That(after, Is.LessThan(before * .60f).And.GreaterThan(.025f));
            foreach (ZoneFourPhotoLayer layer in Enum.GetValues(typeof(ZoneFourPhotoLayer)))
            {
                Assert.That(photos.ZoneFourComposition.EnvironmentTint("Zone04", layer).a, Is.EqualTo(1));
                foreach (string zone in new[] { "Zone01", "Zone02", "Zone03", null })
                    Assert.That(photos.ZoneFourComposition.EnvironmentTint(zone, layer), Is.EqualTo(Color.white));
            }
            Assert.That(photos.ZoneFourComposition.LastDetailKind, Is.EqualTo(ZoneFourPhotoDetail.None));
            Assert.That(story.HasObjective("Z4_L1_PHOTO"), Is.False); Assert.That(photos.CanSubmitPhoto(dark), Is.True);
            File.WriteAllBytes(Path.Combine(Qa, "C-grade-before-photograph.png"), bright.Image.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(Qa, "C-grade-after-photograph.png"), dark.Image.EncodeToPNG());
            File.WriteAllText(Path.Combine(Qa, "C-grade-measurement.txt"), "Water luminance before=" + before.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                " after=" + after.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "\nSubject coverage=" + coverage.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                " occlusion=" + occlusion.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            yield return Lab(); yield return Screenshot("C-grade-after-in-real-lab");
        }

        [UnityTest] public IEnumerator DetailCacheIsReusedSiteGatedAndDisposedByRealSceneCleanup()
        {
            SeedOptionalRoute(); var composition = photos.ZoneFourComposition;
            Assert.That(composition.CachedTextureCount, Is.Zero);
            yield return Photograph("zone04-l3"); byte[] first = photos.Photos.Last().Image.EncodeToPNG();
            Assert.That(composition.TextureBuildCount, Is.EqualTo(1)); Assert.That(composition.CachedTextureCount, Is.EqualTo(1));
            yield return Photograph("zone04-l3"); CollectionAssert.AreEqual(first, photos.Photos.Last().Image.EncodeToPNG());
            Assert.That(composition.TextureBuildCount, Is.EqualTo(1));
            yield return Photograph("zone04-l5"); Assert.That(composition.TextureBuildCount, Is.EqualTo(2));
            yield return Photograph("zone04-l5"); Assert.That(composition.TextureBuildCount, Is.EqualTo(2));
            yield return Photograph("zone04-l1"); Assert.That(composition.TextureBuildCount, Is.EqualTo(3));
            Assert.That(composition.LastDetailKind, Is.EqualTo(ZoneFourPhotoDetail.DistantWreck));
            yield return Photograph("zone04-l3", expectSubject: false, faceAway: true);
            Assert.That(composition.LastDetailKind, Is.EqualTo(ZoneFourPhotoDetail.None)); Assert.That(composition.TextureBuildCount, Is.EqualTo(3));
            var probe = new PhotoLayerComposer(); probe.Begin();
            Assert.That(composition.DrawSiteBackground(probe, "Zone03", "zone04-l3", new Rect(.3f, .2f, .3f, .6f), .5f), Is.False);
            Assert.That(composition.DrawSiteBackground(probe, "Zone04", "zone04-l2", new Rect(.3f, .2f, .3f, .6f), .5f), Is.False);
            Assert.That(composition.DrawSiteBackground(probe, "Zone04", "zone04-l4", new Rect(.3f, .2f, .3f, .6f), .5f), Is.False);
            Assert.That(composition.CachedTextureCount, Is.EqualTo(3));
            var textures = Resources.FindObjectsOfTypeAll<Texture2D>().Where(texture => texture.name.StartsWith("Zone 4 ", StringComparison.Ordinal)).ToArray();
            Assert.That(textures.Length, Is.GreaterThanOrEqualTo(3));
            flow.LoadMainMenu(); yield return WaitTransition(); yield return null;
            Assert.That(composition.CachedTextureCount, Is.Zero); Assert.That(composition.HasCachedTexture, Is.False);
            Assert.That(composition.LastDetailKind, Is.EqualTo(ZoneFourPhotoDetail.None));
            Assert.That(textures.All(texture => texture == null), Is.True, "Scene cleanup must destroy actual cached Unity textures.");
        }

        [UnityTest, Timeout(90000)] public IEnumerator FirstRealLabObservationPlaysEachAuthoredHiddenSignalOnceAndRecordsItsActualMix()
        {
            SeedOptionalRoute();
            var presentation = cabin.GetComponent<ZoneFourPresentationController>();
            var executor = cabin.GetComponent<EffectAudioExecutor>(); var director = cabin.GetComponent<EffectDirector>();
            var listener = Object.FindAnyObjectByType<AudioListener>();
            Assert.That(presentation, Is.Not.Null); Assert.That(listener, Is.Not.Null);
            Assert.That(presentation.hiddenA, Is.Not.Null); Assert.That(presentation.hiddenB, Is.Not.Null);
            Assert.That(presentation.HiddenACount, Is.Zero); Assert.That(presentation.HiddenBCount, Is.Zero);
            string[] sites = { "zone04-l3", "zone04-l5" }, keys = { "hidden.a", "hidden.b" };
            var sequences = new[] { presentation.hiddenA, presentation.hiddenB };
            for (int site = 0; site < sites.Length; site++)
            {
                yield return Photograph(sites[site]);
                Assert.That(photos.ZoneFourComposition.LastDetailKind,
                    Is.EqualTo(site == 0 ? ZoneFourPhotoDetail.SedimentFins : ZoneFourPhotoDetail.AsymmetricShadow));
                Assert.That(loop.ZoneFourPresentation.Has(keys[site]), Is.False, "Shutter alone cannot spend a first-observation receipt.");
                Assert.That(site == 0 ? presentation.HiddenACount : presentation.HiddenBCount, Is.Zero);
                string authority = JsonUtility.ToJson(story.ExportProgress());
                var definitions = sequences[site].cues.Where(cue => cue?.effect?.clip != null).Select(cue => cue.effect).Distinct().ToArray();
                Assert.That(definitions, Is.Not.Empty, "The actual production sequence must carry imported audio.");
                float[] gains = definitions.Select(definition => definition.gain).ToArray();
                var clips = definitions.Select(definition => definition.clip).ToArray();
                Func<AudioSource[]> voices = () => executor.GetComponentsInChildren<AudioSource>(true)
                    .Where(source => source.isPlaying && source.clip != null && clips.Contains(source.clip)).ToArray();
                var recorder = ZoneThreeAudioEvidence.Begin(listener, 10.5f);
                float began = Time.realtimeSinceStartup;
                yield return Lab();
                yield return Until(() => (site == 0 ? presentation.HiddenACount : presentation.HiddenBCount) == 1 && voices().Length > 0,
                    2, "Opening the actual selected site's Lab photo must start its authored signal.");
                Assert.That(loop.ZoneFourPresentation.Has(keys[site]), Is.True);
                Assert.That(voices().All(source => source.volume > 0 && !source.loop), Is.True);
                Assert.That(director.ActiveSequenceCount, Is.GreaterThan(0));
                Assert.That(JsonUtility.ToJson(story.ExportProgress()), Is.EqualTo(authority), "Observation cannot grant SEND or discovery progress.");
                yield return new WaitForSecondsRealtime(Mathf.Max(0, 9.3f - (Time.realtimeSinceStartup - began)));
                string name = site == 0 ? "C-A-first-observation-listener" : "C-B-first-observation-listener";
                var metrics = recorder.Finish(Path.Combine(Qa, name + ".wav"));
                File.WriteAllText(Path.Combine(Qa, name + ".json"), JsonUtility.ToJson(metrics, true));
                Assert.That(metrics.nonzero, Is.True); Assert.That(metrics.durationSeconds, Is.GreaterThan(8.5f));
                Assert.That(metrics.clippedSamples, Is.Zero, "Retain and test the actual mix without normalizing its recorded output.");
                for (int i = 0; i < gains.Length; i++) Assert.That(definitions[i].gain, Is.EqualTo(gains[i]), "Do not alter source gains to manufacture evidence.");
                yield return Until(() => voices().Length == 0, sequences[site].Duration + 1, "An authored one-shot must release its real voice.");
                cabin.ClosePanel(); yield return null; yield return Lab();
                Assert.That(site == 0 ? presentation.HiddenACount : presentation.HiddenBCount, Is.EqualTo(1));
                Assert.That(voices(), Is.Empty, "Reopening the same evidence cannot replay its spent first-observation signal.");
                Assert.That(photos.Photos.Last().IsMissionPhoto, Is.False, "This is observation proof; manual SEND remains necessary.");
                cabin.ClosePanel(); yield return null;
            }
            Assert.That(presentation.HiddenACount, Is.EqualTo(1)); Assert.That(presentation.HiddenBCount, Is.EqualTo(1));
            Assert.That(loop.EndingReached, Is.Null.Or.Empty); Assert.That(flow.IsTransitioning, Is.False);
        }

        private static void ComparePixels(Texture2D before, Texture2D after, PhotoEvidence evidence)
        {
            var a = before.GetPixels32(); var b = after.GetPixels32();
            for (int y = 0; y < after.height; y++) for (int x = 0; x < after.width; x++)
            {
                int index = y * after.width + x; if (a[index].Equals(b[index])) continue;
                evidence.changedPixels++;
                if (x < Mathf.FloorToInt(evidence.detailRect.xMin * after.width) - 1 || x >= Mathf.CeilToInt(evidence.detailRect.xMax * after.width) + 1 ||
                    y < Mathf.FloorToInt(evidence.detailRect.yMin * after.height) - 1 || y >= Mathf.CeilToInt(evidence.detailRect.yMax * after.height) + 1) evidence.changedOutsideDetail++;
            }
        }
        private static float Luminance(Texture2D image, Rect rect)
        {
            Color32[] pixels = image.GetPixels32(); float total = 0; int count = 0;
            for (int y = Mathf.FloorToInt(rect.yMin * image.height); y < Mathf.CeilToInt(rect.yMax * image.height); y++)
                for (int x = Mathf.FloorToInt(rect.xMin * image.width); x < Mathf.CeilToInt(rect.xMax * image.width); x++)
                { Color c = pixels[y * image.width + x]; total += c.r * .2126f + c.g * .7152f + c.b * .0722f; count++; }
            return total / count;
        }
        private static string Hash(byte[] data)
        { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
        private static IEnumerator Screenshot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture(); Assert.That(image, Is.Not.Null);
            try { File.WriteAllBytes(Path.Combine(Qa, name + ".png"), image.EncodeToPNG()); }
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

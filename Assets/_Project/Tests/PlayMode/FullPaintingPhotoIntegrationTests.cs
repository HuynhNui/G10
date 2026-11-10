#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    /// <summary>
    /// Controlled full-PSD-painting inspection, not an ordinary-input voyage. Uses authored
    /// contacts, fixture refills/reveals and capture Win only for prerequisite progression.
    /// Run the Zone02 prototype and inspect its images before binding the remaining paintings.
    /// </summary>
    public sealed class FullPaintingPhotoIntegrationTests
    {
        private string folder, oldSave, oldPhotos, output;
        private bool oldDebug;
        private bool artworkAlwaysEnabled;
        private SceneFlowController flow;
        private RunEvidence evidence;
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private PhotoCaptureService Photos => Cabin.GetComponent<PhotoCaptureService>();
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/CreatureArtworkIntegration"));

        [Serializable] private sealed class RunEvidence
        {
            public string scope = "Controlled full-painting fixture: authored contact positioning/refills, actual camera/Lab/gallery/SEND/Continue and source-pixel equality; captures use controlled Win only for prerequisite progression, Z3/Z4 use a deleted baseline shutter with photoArtwork temporarily disabled. The existing Zone04 continue-exploring choice is selected before inspecting hidden sites. Not an ordinary-input voyage.";
            public string startedUtc, finishedUtc;
            public bool completed;
            public List<PhotoEvidence> photographs = new List<PhotoEvidence>();
            public int prerequisiteCaptures;
            public HistoricalEvidence historical;
        }
        [Serializable] private sealed class PhotoEvidence
        {
            public string creatureId, zone, poiId, objectiveId, photoId, result, artworkPath, cutoutPath, maskPath;
            public string sourceImportedPixelHash, sourcePngHash, sourcePngPixelHash;
            public bool sourcePngMatchesImportedPixels, capturedPixelsMatchCompleteArtwork, cutoutBindingUnchanged;
            public bool bakedFullPainting, pendingContinueFullPainting, submittedContinueFullPainting;
            public bool shutterViaCabinButton;
            public string bakedPng, labScreenshot, galleryScreenshot, continuedLabScreenshot;
            public string bakedHash, archiveHash, pendingContinueHash, submittedContinueHash, durableHash;
            public string bakedPixelHash, pendingContinuePixelHash, submittedContinuePixelHash, durablePixelHash, archiveAfterContinueHash;
            public int artworkWidth, artworkHeight;
            public float coverage, occlusion, fixedClockBaselineCoverage, fixedClockCandidateCoverage;
            public float fixedClockBaselineOcclusion, fixedClockCandidateOcclusion;
            public bool fixedClockCompared, fixedClockMeasurementsEqual;
            public bool pngEncodingChangedAfterContinue;
            public bool actualShutterCalibrationCompared, calibrationPhotoDeleted, actualShutterMeasurementsEqual;
            public string calibrationResult, calibrationBakedHash, calibrationBakedPng;
            public float calibrationCoverage, calibrationOcclusion;
        }
        [Serializable] private sealed class HistoricalEvidence
        {
            public string sourcePng, sourceMetadata, photoId, targetId, sourceHash, looseReloadHash, continueHash, screenshot;
            public string sourcePixelHash, looseReloadPixelHash, continuePixelHash, copiedArchiveHash;
            public string looseMetadataHash, continuedMetadataHash;
            public bool available, metadataIdentityPreserved;
            public bool legacyMarkerStayedFalse;
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            artworkAlwaysEnabled = false;
            Assert.That(SessionState.GetBool("G10.Zone02Experience.isolated", false), Is.True,
                "Root must call ZoneTwoExperiencePlaytestTools.SetupIsolation in Edit Mode before this native job.");
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            oldDebug = ExpeditionSaveStore.IsDebugTimeline;
            Assert.That(oldSave, Is.Not.Null.And.Not.Empty, "Outer isolation must protect quit-save.");
            Assert.That(oldPhotos, Is.Not.Null.And.Not.Empty, "Outer isolation must protect photos.");
            folder = Path.Combine(Application.temporaryCachePath, "FullPaintingPhotoIntegration-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline();
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance;
            yield return WaitTransition();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ZoneTwoPrototypeFullPaintingReachesRealLabSendAndContinueWithoutRepainting()
        {
            BeginEvidence("Prototype");
            yield return StartZone(2);
            var location = Loop.MissionRuntime.config.locations.Single(site => site.objectives.Any(item =>
                item.type == MissionObjectiveType.Photograph && item.targetId == "Z2_Creature_01"));
            var objective = location.objectives.Single(item => item.type == MissionObjectiveType.Photograph);
            yield return AuditPhotograph(location, objective, false);
            evidence.completed = true; WriteEvidence();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator AllElevenUseCanonicalShutterButtonsAndRadarWithoutArtworkSwaps()
        {
            artworkAlwaysEnabled = true;
            yield return AllElevenFullPaintingsKeepSourcePixelsThroughPhotoLabGallerySendAndContinue();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator AllElevenFullPaintingsKeepSourcePixelsThroughPhotoLabGallerySendAndContinue()
        {
            BeginEvidence("AllPaintings");
            int hidden = 0;
            var distinctArt = new HashSet<string>();
            for (int zone = 1; zone <= 4; zone++)
            {
                yield return StartZone(zone);
                // Keep stable IDs across Continue; component and config references are rebound after reload.
                var siteIds = Loop.MissionRuntime.config.locations.Where(site => site.objectives.Any(item =>
                    item.type == MissionObjectiveType.Photograph)).Select(site => site.id).ToArray();
                foreach (string siteId in siteIds)
                {
                    var location = Loop.MissionRuntime.config.FindLocation(siteId);
                    var objective = location.objectives.Single(item => item.type == MissionObjectiveType.Photograph);
                    if (location.visibility == LocationVisibility.HiddenRadar)
                    {
                        var route = Cabin.GetComponent<ExpeditionProgression>(); route.Evaluate();
                        var dialogue = Object.FindAnyObjectByType<DialogueController>();
                        if (dialogue != null && dialogue.IsChoiceActive) dialogue.SelectChoice(1);
                        Assert.That(Loop.CurrentProgress.hiddenRouteUnlocked, Is.True);
                        if (artworkAlwaysEnabled) yield return DiscoverWithRealRadar(location.poiId);
                        else Assert.That(Loop.MissionRuntime.RevealPoi(location.poiId), Is.True);
                        hidden++;
                    }
                    var content = Loop.MissionRuntime.FindContent(objective.targetId);
                    Assert.That(distinctArt.Add(AssetPath(content.photoArtwork)), Is.True,
                        "Distinct creature objectives must not silently share an artwork/fallback: " + objective.targetId);
                    yield return AuditPhotograph(location, objective, true);
                }
            }
            Assert.That(evidence.photographs.Count, Is.EqualTo(11));
            Assert.That(evidence.prerequisiteCaptures, Is.EqualTo(6));
            Assert.That(hidden, Is.EqualTo(2));
            if (!artworkAlwaysEnabled) yield return AuditHistoricalPhoto();
            evidence.completed = true; WriteEvidence();
        }

        private void BeginEvidence(string name)
        {
            output = artworkAlwaysEnabled ? Path.GetFullPath("Docs/QA/ArtworkCompletion/gameplay-shutter")
                : Path.Combine(Qa, "FullPaintingsCorrection", "native-validation", name);
            Directory.CreateDirectory(output);
            evidence = new RunEvidence { startedUtc = DateTimeOffset.UtcNow.ToString("O") };
            if (artworkAlwaysEnabled) evidence.scope = "Production quick-shutter UI via raycast/EventSystem click on CameraHotspot, real gallery/SEND/Continue and actual radar discovery for both hidden POIs. All catalog artwork remains enabled and source-pixel identical throughout. Controlled setup: zone-start preset, contact placement/refills and prerequisite minigame Win. Not an ordinary-input full voyage; no synthetic photo records or calibration artwork swaps.";
            WriteEvidence();
        }

        private IEnumerator StartZone(int zone)
        {
            flow.LoadMainMenu(); yield return WaitTransition();
            ExpeditionSaveStore.SelectNormalTimeline();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>();
            var preset = DebugExpeditionPreset.Create(zone, menu.maps, menu.contentCatalog, menu.initialShip);
            preset.tutorial.completed = true;
            ExpeditionSaveStore.Write(preset, discardFuture: true);
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Loop.IsInitialized, Is.True, Loop.LastError);
            Assert.That(Loop.Zone, Is.EqualTo("Zone0" + zone));
        }

        private IEnumerator AuditPhotograph(MissionLocationConfig location, MissionObjectiveConfig objective, bool captureCargo)
        {
            string poiId = location.poiId, targetId = objective.targetId, objectiveId = objective.id;
            Cabin.ClosePanel(); Cabin.Navigation.Ship.Refill(); Aim(poiId);
            var content = Loop.MissionRuntime.FindContent(targetId);
            Assert.That(content, Is.Not.Null, targetId);
            Assert.That(content.Image, Is.Not.Null, targetId);
            Assert.That(content.photoArtwork, Is.Not.Null, "The complete PSD painting must be bound separately: " + targetId);
            Assert.That(content.photoArtwork, Is.Not.SameAs(content.Image), "Capture cutout and complete photograph artwork have different roles.");
            var cutoutAtEntry = content.Image;
            Assert.That(content.preservePhotoCoverage, Is.True, "New artwork must retain authored measurement authority: " + targetId);
            Assert.That(content.placeholderArt, Is.False, targetId);
            var row = new PhotoEvidence { creatureId = targetId, zone = Loop.Zone, poiId = poiId,
                objectiveId = objectiveId, artworkPath = AssetPath(content.photoArtwork), cutoutPath = AssetPath(content.Image), maskPath = AssetPath(content.photoCoverageMask),
                artworkWidth = content.photoArtwork.width, artworkHeight = content.photoArtwork.height };
            evidence.photographs.Add(row); WriteEvidence();
            Assert.That(row.artworkPath, Is.Not.Null.And.Not.Empty, "Imported artwork must be a durable asset.");
            row.sourceImportedPixelHash = PixelHash(content.photoArtwork);
            byte[] sourcePng = File.ReadAllBytes(Path.GetFullPath(row.artworkPath));
            row.sourcePngHash = Hash(sourcePng); row.sourcePngPixelHash = PngPixelHash(sourcePng);
            row.sourcePngMatchesImportedPixels = row.sourcePngPixelHash == row.sourceImportedPixelHash;
            Assert.That(row.sourcePngMatchesImportedPixels, Is.True, "Import must preserve full exported PNG dimensions and RGBA pixels.");
            if (!artworkAlwaysEnabled) CompareFixedClockMeasurements(content, row);
            if (!artworkAlwaysEnabled && (Loop.Zone == "Zone03" || Loop.Zone == "Zone04"))
                yield return CalibrateActualShutter(content, row);
            yield return new WaitForSecondsRealtime(.7f);
            int count = Photos.Photos.Count, charges = Cabin.Navigation.Ship.Photos;
            string beforeProgress = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            if (artworkAlwaysEnabled)
            {
                var button = Cabin.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "CameraHotspot");
                yield return ClickProductionButton(button);
                row.shutterViaCabinButton = true;
            }
            else Cabin.OpenCamera();
            yield return Until(() => Photos.Photos.Count == count + 1 && !Cabin.IsDirectInteractionActive, 5, "Quick PHOTO must finish.");
            var photo = Photos.Photos.Last(); row.photoId = photo.Id; row.result = photo.Result;
            row.bakedFullPainting = photo.IsFullPainting;
            Assert.That(row.bakedFullPainting, Is.True, "New full-painting presentation must be explicitly identified for UI effects.");
            row.coverage = Photos.LastSubjectCoverage; row.occlusion = Photos.LastSubjectOcclusion;
            if (row.actualShutterCalibrationCompared)
            {
                row.actualShutterMeasurementsEqual = row.coverage == row.calibrationCoverage && row.occlusion == row.calibrationOcclusion && row.result == row.calibrationResult;
                Assert.That(row.actualShutterMeasurementsEqual, Is.True,
                    "The actual Z3/Z4 shutter must retain the original mask's coverage, occlusion and result at the same authored pose.");
            }
            Assert.That(photo.MissionObjectiveId, Is.EqualTo(objectiveId), targetId + ": " + photo.Result);
            Assert.That(photo.MissionTargetId, Is.EqualTo(targetId)); Assert.That(photo.IsMissionPhoto, Is.False);
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(beforeProgress), "PHOTO cannot grant mission progress.");
            Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges - 1));
            var bytes = photo.Image.EncodeToPNG(); row.bakedHash = Hash(bytes); row.bakedPixelHash = PixelHash(photo.Image);
            row.capturedPixelsMatchCompleteArtwork = row.bakedPixelHash == row.sourceImportedPixelHash;
            Assert.That(photo.Image, Is.Not.SameAs(content.photoArtwork), "A photograph must own baked pixels; it must not retain a live catalog reference.");
            Assert.That(row.capturedPixelsMatchCompleteArtwork, Is.True,
                "New PHOTO must contain the exact whole painting, including its background, film frame and effects, with no procedural overlays.");
            row.cutoutBindingUnchanged = content.Image == cutoutAtEntry;
            Assert.That(row.cutoutBindingUnchanged, Is.True);
            row.bakedPng = targetId + "-baked.png"; File.WriteAllBytes(Path.Combine(output, row.bakedPng), bytes);
            row.archiveHash = Hash(File.ReadAllBytes(Path.Combine(Photos.ArchivePath, photo.Id + ".png")));
            Assert.That(row.archiveHash, Is.EqualTo(row.bakedHash));
            var lab = OpenLab(); yield return null;
            AssertLab(lab, photo);
            row.labScreenshot = targetId + "-01-lab.png"; yield return Screenshot(row.labScreenshot);
            if (Photos.Photos.Count > 1)
            {
                string previousId = Photos.Photos[Photos.Photos.Count - 2].Id;
                lab.Previous(); yield return null;
                Assert.That(lab.SelectedPhotoId, Is.EqualTo(previousId));
                Assert.That(lab.preview.texture, Is.SameAs(Photos.Photos[Photos.Photos.Count - 2].Image));
                row.galleryScreenshot = targetId + "-02-gallery-previous.png"; yield return Screenshot(row.galleryScreenshot);
                lab.Next(); yield return null; AssertLab(lab, photo);
            }
            Assert.That(Loop.SaveCurrent(), Is.True, Loop.LastError); yield return Reload();
            photo = FindPhoto(row.photoId); row.pendingContinueHash = Hash(photo.Image.EncodeToPNG());
            row.pendingContinueFullPainting = photo.IsFullPainting;
            Assert.That(row.pendingContinueFullPainting, Is.True);
            row.pendingContinuePixelHash = PixelHash(photo.Image);
            row.pngEncodingChangedAfterContinue = row.pendingContinueHash != row.bakedHash;
            Assert.That(row.pendingContinuePixelHash, Is.EqualTo(row.bakedPixelHash), "Continue preserves exact RGBA pixels and dimensions, independent of RGB/RGBA PNG encoding.");
            Assert.That(Hash(File.ReadAllBytes(Path.Combine(Photos.ArchivePath, row.photoId + ".png"))), Is.EqualTo(row.archiveHash),
                "Loading the snapshot must not repaint or rewrite the loose PNG archive.");
            Assert.That(photo.MissionTargetId, Is.EqualTo(targetId)); Assert.That(photo.IsMissionPhoto, Is.False);
            Assert.That(Photos.CanSubmitPhoto(photo), Is.True);
            lab = OpenLab(); yield return null; AssertLab(lab, photo);
            Assert.That(lab.sendButton.interactable, Is.True);
            lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(photo.IsMissionPhoto, Is.True); Assert.That(Loop.MissionRuntime.HasObjective(objectiveId), Is.True);
            Assert.That(Photos.SubmitPhoto(photo), Is.False, "Repeated SEND cannot duplicate rewards.");
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            var durable = Convert.FromBase64String(saved.current.photos.Single(item => item.id == row.photoId).png);
            row.durableHash = Hash(durable); row.durablePixelHash = PngPixelHash(durable);
            Assert.That(row.durablePixelHash, Is.EqualTo(row.bakedPixelHash));
            Assert.That(Hash(File.ReadAllBytes(Path.Combine(Photos.ArchivePath, row.photoId + ".png"))), Is.EqualTo(row.archiveHash),
                "SEND updates metadata only; the original loose PNG remains byte-identical.");
            yield return Reload(); photo = FindPhoto(row.photoId);
            row.submittedContinueFullPainting = photo.IsFullPainting;
            Assert.That(row.submittedContinueFullPainting, Is.True);
            row.submittedContinueHash = Hash(photo.Image.EncodeToPNG());
            row.submittedContinuePixelHash = PixelHash(photo.Image);
            Assert.That(row.submittedContinuePixelHash, Is.EqualTo(row.bakedPixelHash)); Assert.That(photo.IsMissionPhoto, Is.True);
            row.archiveAfterContinueHash = Hash(File.ReadAllBytes(Path.Combine(Photos.ArchivePath, row.photoId + ".png")));
            Assert.That(row.archiveAfterContinueHash, Is.EqualTo(row.archiveHash));
            Assert.That(photo.MissionTargetId, Is.EqualTo(targetId));
            lab = OpenLab(); yield return null; AssertLab(lab, photo);
            row.continuedLabScreenshot = targetId + "-03-continued-submitted-lab.png";
            yield return Screenshot(row.continuedLabScreenshot); WriteEvidence();
            Cabin.ClosePanel();
            var capture = Loop.MissionRuntime.FindObjective(poiId, MissionObjectiveType.Capture, targetId);
            if (captureCargo && capture != null) yield return CompletePrerequisiteCapture(poiId, capture);
        }


        private IEnumerator DiscoverWithRealRadar(string poiId)
        {
            Cabin.ClosePanel(); Cabin.Navigation.Ship.Refill();
            var poi = Photos.survey.FindPoi(poiId);
            Cabin.Navigation.RestoreVoyage(Photos.survey.ContactPosition(poi) + Vector2.down * 14, 0, Photos.survey.DepthFor(poi), 0);
            Assert.That(Loop.MissionRuntime.IsPoiVisible(poiId), Is.False, "A hidden contact must begin undiscovered.");
            Cabin.OpenRadar(); yield return null;
            var scan = Cabin.RadarPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "ScanHotspot");
            int charges = Cabin.Navigation.Ship.Radar;
            yield return ClickProductionButton(scan);
            yield return Until(() => Loop.MissionRuntime.IsPoiVisible(poiId), 8, "The production radar sweep must discover " + poiId);
            Assert.That(Cabin.Navigation.Ship.Radar, Is.EqualTo(charges - 1));
            yield return Screenshot(poiId + "-real-radar-discovery.png");
            Cabin.ClosePanel();
        }
        private static IEnumerator ClickProductionButton(UnityEngine.UI.Button button)
        {
            Canvas.ForceUpdateCanvases(); yield return null;
            Assert.That(button.IsInteractable() && button.gameObject.activeInHierarchy, Is.True, button.name);
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), button.name);
            Assert.That(hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>(), Is.SameAs(button),
                "The actual production button must receive the top raycast: " + button.name + " hit " + hits[0].gameObject.name);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
        }

        private IEnumerator CalibrateActualShutter(SurveyContentDefinition content, PhotoEvidence row)
        {
            Assert.That(Photos.survey.TryGetPhotoContact(Cabin.Navigation, Photos.profile.visibleDistance,
                Photos.profile.fieldOfView, out var found, out var world), Is.True);
            Assert.That(found.id, Is.EqualTo(row.poiId));
            float distance = Vector3.Distance(Cabin.Navigation.WorldPosition, world);
            var fallback = distance < 12 ? Photos.profile.closeCreature : distance > 40 ? Photos.profile.silhouette : Photos.profile.creature;
            var originalMask = content.PhotoCoverageImage(fallback);
            Assert.That(originalMask, Is.Not.Null, "Calibration needs the unchanged distance-specific authored mask.");
            var artwork = content.image; var sprite = content.sprite; bool preserve = content.preservePhotoCoverage;
            var fullPainting = content.photoArtwork;
            string beforeProgress = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            string calibrationId = null;
            yield return new WaitForSecondsRealtime(.7f);
            try
            {
                // Fixture-local comparison through the real quick shutter. No asset writes or mission grants.
                content.image = originalMask; content.sprite = null; content.preservePhotoCoverage = false;
                content.photoArtwork = null;
                int before = Photos.Photos.Count;
                Cabin.OpenCamera();
                yield return Until(() => Photos.Photos.Count == before + 1 && !Cabin.IsDirectInteractionActive, 5,
                    "Old-mask calibration shutter must finish.");
                var calibration = Photos.Photos.Last(); calibrationId = calibration.Id;
                row.calibrationResult = calibration.Result;
                row.calibrationCoverage = Photos.LastSubjectCoverage; row.calibrationOcclusion = Photos.LastSubjectOcclusion;
                Assert.That(calibration.MissionTargetId, Is.EqualTo(row.creatureId), calibration.Result);
                Assert.That(calibration.IsMissionPhoto, Is.False);
                var bytes = calibration.Image.EncodeToPNG(); row.calibrationBakedHash = Hash(bytes);
                row.calibrationBakedPng = row.creatureId + "-calibration-old-mask.png";
                File.WriteAllBytes(Path.Combine(output, row.calibrationBakedPng), bytes);
            }
            finally { content.image = artwork; content.sprite = sprite; content.preservePhotoCoverage = preserve; content.photoArtwork = fullPainting; }
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(beforeProgress));
            Assert.That(Loop.SaveCurrent(), Is.True, Loop.LastError);
            Assert.That(Photos.DeletePhoto(calibrationId), Is.True, Photos.LastError);
            row.calibrationPhotoDeleted = !Photos.Photos.Any(photo => photo.Id == calibrationId) &&
                !File.Exists(Path.Combine(Photos.ArchivePath, calibrationId + ".png")) &&
                !File.Exists(Path.Combine(Photos.ArchivePath, calibrationId + ".json"));
            Assert.That(row.calibrationPhotoDeleted, Is.True, "No calibration fallback may remain in the gallery or loose archive.");
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(beforeProgress));
            Cabin.Navigation.Ship.Refill(); row.actualShutterCalibrationCompared = true; WriteEvidence();
        }

        private IEnumerator CompletePrerequisiteCapture(string poiId, MissionObjectiveConfig objective)
        {
            Cabin.ClosePanel(); Cabin.Navigation.Ship.Refill();
            var poi = Photos.survey.FindPoi(poiId);
            Cabin.Navigation.RestoreVoyage(Photos.survey.ContactPosition(poi), 0, Photos.survey.DepthFor(poi), 0);
            var catcher = Cabin.GetComponent<CreatureCatcher>();
            var content = Loop.MissionRuntime.FindContent(objective.targetId);
            var game = catcher.minigame;
            var cutout = content.Image;
            Vector2 collisionBefore = game.profile.CreatureCollisionSize;
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started)); yield return null;
            Assert.That(game.EncounterArtwork, Is.SameAs(cutout), "The full painting cannot replace the separate capture cutout.");
            Assert.That(game.CreatureCollider.size, Is.EqualTo(collisionBefore));
            // This is only an existing prerequisite for the Zone04 hidden route, not a new capture visual audit.
            CaptureMinigamePlayModeTests.Win(game); yield return null;
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            var cargoItem = catcher.inventory.Items.Single(item => item.Id == objective.targetId);
            Assert.That(cargoItem.Icon, Is.SameAs(content.InventoryImage), "Collected material artwork may differ from the capture creature; stable item ID is unchanged.");
            evidence.prerequisiteCaptures++; WriteEvidence();
        }

        private void CompareFixedClockMeasurements(SurveyContentDefinition content, PhotoEvidence row)
        {
            if (Loop.Zone != "Zone01" && Loop.Zone != "Zone02") return;
            var frame = new ZoneOneSurveyFrame();
            frame.Build(Photos, 17.25f, 1, 0, 0);
            ComposeMeasurements(frame, out row.fixedClockCandidateCoverage, out row.fixedClockCandidateOcclusion);
            var artwork = content.image; var sprite = content.sprite; bool preserve = content.preservePhotoCoverage;
            try
            {
                // Temporary in-memory comparison only. No SetDirty/SaveAssets and restoration even on assertion failure.
                content.image = content.PhotoCoverageImage(Photos.profile.creature); content.sprite = null; content.preservePhotoCoverage = false;
                frame.Build(Photos, 17.25f, 1, 0, 0);
                ComposeMeasurements(frame, out row.fixedClockBaselineCoverage, out row.fixedClockBaselineOcclusion);
            }
            finally { content.image = artwork; content.sprite = sprite; content.preservePhotoCoverage = preserve; }
            row.fixedClockCompared = true;
            row.fixedClockMeasurementsEqual = row.fixedClockBaselineCoverage == row.fixedClockCandidateCoverage &&
                row.fixedClockBaselineOcclusion == row.fixedClockCandidateOcclusion;
            Assert.That(row.fixedClockMeasurementsEqual, Is.True, "Artwork must not alter fixed-clock authored coverage/occlusion.");
        }

        private static void ComposeMeasurements(ZoneOneSurveyFrame frame, out float coverage, out float occlusion)
        {
            var composer = new PhotoLayerComposer(); composer.Begin();
            for (int i = 0; i < frame.Count; i++)
            {
                var layer = frame.Layers[i];
                composer.Draw(layer.Texture, layer.Rect, 1, layer.Subject, layer.Occludes, layer.Color, layer.MeasurementMask, layer.PreserveAspect);
            }
            var image = composer.Finish(out coverage, out occlusion); Object.Destroy(image);
        }

        private IEnumerator AuditHistoricalPhoto()
        {
            var row = new HistoricalEvidence(); evidence.historical = row;
            string history = Path.Combine(Qa, "preserved-local", "entry", "persistent", "Zone01Photos");
            string source = Directory.Exists(history) ? Directory.GetFiles(history, "*.png").OrderBy(path => path)
                .FirstOrDefault(path => File.Exists(Path.ChangeExtension(path, ".json"))) : null;
            if (source == null) { WriteEvidence(); yield break; }
            row.available = true; row.sourcePng = source; row.sourceMetadata = Path.ChangeExtension(source, ".json");
            var originalBytes = File.ReadAllBytes(source);
            row.sourceHash = Hash(originalBytes); row.sourcePixelHash = PngPixelHash(originalBytes);
            string probeFolder = Path.Combine(folder, "historical-probe"); Directory.CreateDirectory(probeFolder);
            File.Copy(source, Path.Combine(probeFolder, Path.GetFileName(source)));
            File.Copy(row.sourceMetadata, Path.Combine(probeFolder, Path.GetFileName(row.sourceMetadata)));
            string currentArchive = PhotoCaptureService.ArchivePathOverride;
            GameObject probeObject = null; List<SavedPhoto> historic;
            try
            {
                PhotoCaptureService.ArchivePathOverride = probeFolder;
                probeObject = new GameObject("HistoricalArtworkArchiveProbe");
                var probe = probeObject.AddComponent<PhotoCaptureService>();
                Assert.That(probe.Photos.Count, Is.EqualTo(1));
                var old = probe.Photos[0]; row.photoId = old.Id; row.targetId = old.MissionTargetId;
                Assert.That(old.IsFullPainting, Is.False, "Legacy metadata without the optional presentation field must retain its old treatment.");
                row.looseMetadataHash = PhotoIdentityHash(old);
                row.looseReloadHash = Hash(old.Image.EncodeToPNG()); row.looseReloadPixelHash = PixelHash(old.Image);
                Assert.That(row.looseReloadPixelHash, Is.EqualTo(row.sourcePixelHash));
                row.copiedArchiveHash = Hash(File.ReadAllBytes(Path.Combine(probeFolder, Path.GetFileName(source))));
                Assert.That(row.copiedArchiveHash, Is.EqualTo(row.sourceHash));
                historic = probe.ExportPhotos();
            }
            finally
            {
                PhotoCaptureService.ArchivePathOverride = currentArchive;
                if (probeObject != null) Object.Destroy(probeObject);
            }
            yield return null;
            var merged = Photos.ExportPhotos(); merged.AddRange(historic);
            Photos.RestorePhotos(merged, Photos.TotalPhotosTaken);
            Assert.That(Loop.SaveCurrent(), Is.True, Loop.LastError); yield return Reload();
            var loaded = FindPhoto(row.photoId); row.continueHash = Hash(loaded.Image.EncodeToPNG());
            row.continuePixelHash = PixelHash(loaded.Image);
            row.legacyMarkerStayedFalse = !loaded.IsFullPainting;
            Assert.That(row.legacyMarkerStayedFalse, Is.True);
            row.continuedMetadataHash = PhotoIdentityHash(loaded);
            row.metadataIdentityPreserved = row.continuedMetadataHash == row.looseMetadataHash;
            Assert.That(row.continuePixelHash, Is.EqualTo(row.sourcePixelHash)); Assert.That(row.metadataIdentityPreserved, Is.True);
            var lab = OpenLab(); yield return null; AssertLab(lab, loaded);
            row.screenshot = "historical-original-continues-unchanged.png"; yield return Screenshot(row.screenshot); WriteEvidence();
        }

        private void Aim(string poiId)
        {
            var poi = Photos.survey.FindPoi(poiId); Assert.That(poi, Is.Not.Null, poiId);
            foreach (float radius in new[] { 20f, 14f, 26f }) for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8;
                var position = Photos.survey.ContactPosition(poi) - new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
                if (!Cabin.Navigation.CanOccupy(position)) continue;
                Cabin.Navigation.RestoreVoyage(position, i * 22.5f, Photos.survey.DepthFor(poi), 0);
                if (Photos.survey.TryGetPhotoContact(Cabin.Navigation, Photos.profile.visibleDistance, Photos.profile.fieldOfView,
                    out var found, out _) && found == poi) return;
            }
            Assert.Fail("No authored, reachable photo angle for " + poiId);
        }
        private PhotoLabView OpenLab()
        {
            if (Loop.Zone == "Zone04")
            {
                // Mandatory completion can present its choice immediately after Continue.
                // Resolve it through the authored callback before opening a station behind the modal.
                Cabin.GetComponent<ExpeditionProgression>().Evaluate();
                var dialogue = Object.FindAnyObjectByType<DialogueController>();
                if (dialogue != null && dialogue.IsChoiceActive) dialogue.SelectChoice(1);
            }
            Assert.That(Cabin.Panels.IsModalOpen, Is.False, "The visual fixture must finish the authored modal before inspecting Lab.");
            Cabin.OpenComputer(); var computer = Cabin.GetComponentInChildren<ComputerScreenController>(true);
            computer.OpenPhotoLab(); return computer.GetComponentInChildren<PhotoLabView>(true);
        }
        private static void AssertLab(PhotoLabView lab, PhotoRecord photo)
        {
            Assert.That(lab.SelectedPhotoId, Is.EqualTo(photo.Id)); Assert.That(lab.preview.texture, Is.SameAs(photo.Image));
            Assert.That(lab.SelectedUsesFullPainting, Is.EqualTo(photo.IsFullPainting));
            if (photo.IsFullPainting)
            {
                Assert.That(lab.preview.color, Is.EqualTo(Color.white), "The artist's complete composite cannot receive a second UI color treatment.");
                var presentation = Object.FindAnyObjectByType<ZoneTwoPresentationController>();
                Assert.That(presentation == null || !presentation.PhotoMotionVisible, Is.True, "Existing edge distortion cannot warp the complete artist frame.");
                Assert.That(lab.preview.material == null || lab.preview.material.name != "Zone02 bounded photo-edge motion", Is.True);
            }
            Assert.That(lab.preview.uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)), "The full painting cannot inherit a thumbnail/atlas crop.");
            Canvas.ForceUpdateCanvases();
            var size = lab.preview.rectTransform.rect.size;
            Assert.That(size.y, Is.GreaterThan(0));
            Assert.That(size.x / size.y, Is.EqualTo((float)photo.Image.width / photo.Image.height).Within(.001f),
                "The actual Photo Lab rectangle must preserve the baked photograph's aspect ratio.");
            var area = lab.preview.rectTransform.parent as RectTransform;
            Assert.That(area, Is.Not.Null);
            Assert.That(area.name, Is.EqualTo("PhotoPreviewArea"));
            Assert.That(size.x, Is.LessThanOrEqualTo(area.rect.width + .001f));
            Assert.That(size.y, Is.LessThanOrEqualTo(area.rect.height + .001f));
            Assert.That(lab.preview.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero), "Contain the whole image in the center of the existing preview area.");
        }
        private PhotoRecord FindPhoto(string id) => Photos.Photos.Single(item => item.Id == id);
        private IEnumerator Reload()
        { flow.LoadMainMenu(); yield return WaitTransition(); flow.ContinueGame(); yield return WaitTransition(); }
        private IEnumerator WaitTransition()
        {
            yield return null;
            yield return Until(() => flow != null && !flow.IsTransitioning, 25, "Scene transition must finish.");
            Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
        private static IEnumerator Until(Func<bool> condition, float seconds, string reason)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(condition(), Is.True, reason);
        }
        private IEnumerator Screenshot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture(); Assert.That(image, Is.Not.Null);
            try { File.WriteAllBytes(Path.Combine(output, name), image.EncodeToPNG()); }
            finally { Object.Destroy(image); }
            yield return null;
        }
        private static string Hash(byte[] bytes)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        private static string PixelHash(Texture2D image)
        {
            var pixels = image.GetPixels32();
            var bytes = new byte[8 + pixels.Length * 4];
            Buffer.BlockCopy(BitConverter.GetBytes(image.width), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(image.height), 0, bytes, 4, 4);
            for (int i = 0; i < pixels.Length; i++)
            {
                int j = 8 + i * 4; var color = pixels[i];
                bytes[j] = color.r; bytes[j + 1] = color.g; bytes[j + 2] = color.b; bytes[j + 3] = color.a;
            }
            return Hash(bytes);
        }
        private static string PngPixelHash(byte[] bytes)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try { Assert.That(texture.LoadImage(bytes), Is.True); return PixelHash(texture); }
            finally { Object.Destroy(texture); }
        }
        private static string PhotoIdentityHash(PhotoRecord photo)
        {
            var metadata = new SavedPhoto { id = photo.Id, time = photo.CapturedAt.ToString("O"), result = photo.Result,
                coordinate = photo.MapCoordinate, depth = photo.Depth, heading = photo.Heading, mission = photo.IsMissionPhoto,
                missionZoneId = photo.MissionZoneId, missionPoiId = photo.MissionPoiId, missionObjectiveId = photo.MissionObjectiveId,
                missionTargetId = photo.MissionTargetId, legacyMissionObjectiveId = photo.LegacyMissionObjectiveId };
            return Hash(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(metadata)));
        }
        private static string AssetPath(Object value) => value != null ? AssetDatabase.GetAssetPath(value) : null;
        private void WriteEvidence()
        {
            if (evidence == null || string.IsNullOrEmpty(output)) return;
            evidence.finishedUtc = DateTimeOffset.UtcNow.ToString("O");
            File.WriteAllText(Path.Combine(output, "evidence.json"), JsonUtility.ToJson(evidence, true));
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            // A failed outer-isolation precondition must leave any preexisting scene/save owner untouched.
            if (string.IsNullOrEmpty(folder)) yield break;
            WriteEvidence(); Time.timeScale = 1; PauseMenuController.Instance?.Resume();
            if (SceneFlowController.Instance != null)
            {
                yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
                Object.Destroy(SceneFlowController.Instance.gameObject); yield return null;
            }
            // Restore the outer isolation only after the scene owners finished their saves.
            ExpeditionSaveStore.SelectNormalTimeline();
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (oldDebug) ExpeditionSaveStore.SelectDebugTimeline();
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
#endif

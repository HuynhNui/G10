using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    // The real cabin, gallery and persistent repositories run under an outer EditMode save guard.
    public sealed class PhotoLabLayoutPlayModeTests
    {
        private string oldSave, oldPhotos, folder;
        private CabinStationView cabin;
        private PhotoCaptureService photos;
        private ComputerScreenController screen;
        private PhotoLabView lab;
#if UNITY_EDITOR
        private int oldSize;
        private EditorWindow gameView;
#endif
        private static readonly string Evidence = Path.GetFullPath("Docs/QA/PhotoLabRedesign/screens");

        [UnitySetUp] public IEnumerator Setup()
        {
#if UNITY_EDITOR
            Assert.That(SessionState.GetBool("G10.Zone02Experience.isolated", false), Is.True, "Set outer isolation before Play Mode.");
#endif
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            Assert.That(oldSave, Is.Not.Null.And.Not.Empty); Assert.That(oldPhotos, Is.Not.Null.And.Not.Empty);
            folder = Path.Combine(Application.temporaryCachePath, "PhotoLabLayout-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null; yield return null;
            cabin = Object.FindAnyObjectByType<CabinStationView>();
            photos = cabin.GetComponent<PhotoCaptureService>();
            screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            cabin.OpenComputer(); screen.OpenPhotoLab(); yield return null;
            lab = screen.GetComponentInChildren<PhotoLabView>(true);
            Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
#if UNITY_EDITOR
            gameView = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
            oldSize = (int)gameView.GetType().GetProperty("selectedSizeIndex").GetValue(gameView);
#endif
        }

        [UnityTest] public IEnumerator EmptyPortraitLandscapeLegacyAndModalsAtBothResolutions()
        {
            var artwork = photos.survey.Story.creatureOne.photoArtwork;
            Assert.That(artwork, Is.Not.Null);
            // A real old procedural camera raster exercises landscape/legacy records.
            var nav = cabin.Navigation;
            nav.Ship.Refill();
            nav.RestoreVoyage(new Vector2(1500, 1500), 180, 100, 0);
            var landscape = photos.Capture();
            Assert.That(landscape, Is.Not.Null);
            var legacy = photos.ExportPhotos().Last();
            legacy.id = "lab-legacy-landscape"; legacy.mission = false; legacy.fullPainting = false;
            legacy.missionZoneId = legacy.missionPoiId = legacy.missionTargetId = legacy.missionObjectiveId = null;
            legacy.result = "Obstructed";
            var portrait = new SavedPhoto { id = "lab-portrait", time = DateTimeOffset.UtcNow.ToString("O"),
                coordinate = new Vector2(15.1f, 583.7f), depth = 280, heading = 37.5f, result = "GoodPhoto",
                png = Convert.ToBase64String(artwork.EncodeToPNG()), fullPainting = true };
            foreach (int width in new[] { 1920, 1366 })
            {
                int height = width == 1920 ? 1080 : 768;
                SetResolution(width, height); yield return null; yield return null;
                photos.RestorePhotos(new List<SavedPhoto>(), 0); lab.Refresh(); yield return null;
                Assert.That(lab.preview.texture, Is.Null);
                foreach (var button in new[] { lab.sendButton, lab.deleteButton, lab.previousButton, lab.nextButton })
                    Assert.That(button.interactable, Is.False);
                yield return Screenshot(width + "-empty", width, height);
                photos.RestorePhotos(new List<SavedPhoto> { portrait }, 1); lab.Refresh(); yield return null;
                Assert.That(lab.SampleName, Is.EqualTo("Mẫu chưa định danh"));
                Assert.That(lab.QualityLabel, Is.EqualTo("Ảnh đạt yêu cầu"));
                Assert.That(lab.previousButton.interactable || lab.nextButton.interactable, Is.False);
                AssertContained(lab.preview);
                AssertTextFits();
                AssertRaycast(lab.preview.rectTransform, lab.preview.gameObject);
                yield return Screenshot(width + "-portrait", width, height);
                string selected = lab.SelectedPhotoId;
                var beforeProgress = JsonUtility.ToJson(Object.FindAnyObjectByType<ExpeditionLoop>().MissionRuntime.ExportProgress());
                lab.preview.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return null;
                Assert.That(lab.IsViewerOpen, Is.True);
                var large = screen.transform.Find("PhotoViewer").GetComponentInChildren<UnityEngine.UI.RawImage>();
                Assert.That(large.texture, Is.SameAs(lab.preview.texture));
                AssertContained(large);
                yield return Screenshot(width + "-viewer", width, height);
                AssertRaycast(lab.sendButton.transform as RectTransform, screen.transform.Find("PhotoViewer").gameObject);
                AssertRaycast(screen.transform.Find("Taskbar") as RectTransform, screen.transform.Find("PhotoViewer").gameObject);
                lab.SubmitCurrent(); lab.RequestDelete(); lab.Next();
                Assert.That(lab.SelectedPhotoId, Is.EqualTo(selected));
                Assert.That(lab.IsDeleteConfirmationOpen, Is.False);
                Assert.That(JsonUtility.ToJson(Object.FindAnyObjectByType<ExpeditionLoop>().MissionRuntime.ExportProgress()), Is.EqualTo(beforeProgress));
                yield return Screenshot(width + "-viewer", width, height);
                yield return Escape(); // Exercise the real UIManager keyboard dispatch.
                Assert.That(lab.IsViewerOpen, Is.False);
                Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.PhotoLab));
                Assert.That(lab.SelectedPhotoId, Is.EqualTo(selected));
                photos.RestorePhotos(new List<SavedPhoto> { portrait, legacy }, 2); lab.Refresh();
                lab.Next(); yield return null;
                Assert.That(lab.SelectedPhotoId, Is.EqualTo(legacy.id));
                Assert.That(lab.QualityLabel, Is.EqualTo("Ảnh bị che khuất"));
                AssertContained(lab.preview); AssertTextFits();
                yield return Screenshot(width + "-landscape-legacy", width, height);
                lab.RequestDelete(); yield return null;
                Assert.That(lab.IsDeleteConfirmationOpen, Is.True);
                yield return Screenshot(width + "-delete-confirmation", width, height);
                AssertRaycast(lab.sendButton.transform as RectTransform, lab.deleteConfirmation);
                AssertRaycast(screen.transform.Find("Taskbar") as RectTransform, lab.deleteConfirmation);
                Assert.That(screen.TryHandleBack(), Is.True);
                Assert.That(lab.IsDeleteConfirmationOpen, Is.False); Assert.That(photos.Photos.Count, Is.EqualTo(2));
                lab.RequestDelete(); lab.confirmDeleteButton.onClick.Invoke(); yield return null;
                Assert.That(photos.Photos.Count, Is.EqualTo(1));
                Assert.That(lab.SelectedPhotoId, Is.EqualTo(portrait.id));
                AssertTextFits();
                screen.CloseApp(ComputerAppId.PhotoLab); screen.OpenPhotoLab(); yield return null;
                Assert.That(lab.SelectedPhotoId, Is.EqualTo(portrait.id));
                Assert.That(lab.IsViewerOpen || lab.IsDeleteConfirmationOpen, Is.False);
            }
            foreach (var result in Enum.GetNames(typeof(PhotoResultType)))
                if (result != "GoodPhoto") Assert.That(PhotoLabView.DescribeQuality(result), Is.Not.EqualTo("Ảnh đạt yêu cầu"));
            Assert.That(PhotoLabView.DescribeQuality("old-unknown"), Is.EqualTo("Chưa đánh giá chất lượng"));
        }

        [UnityTest] public IEnumerator RealSendKeepsIdentityAndReopenShowsDurableStatusWithoutDuplicateReward()
        {
            SetResolution(1920, 1080); yield return null;
            cabin.ClosePanel();
            var poi = photos.survey.FindPoi("zone01-left"); var nav = cabin.Navigation;
            nav.Ship.Refill();
            nav.RestoreVoyage(photos.survey.ContactPosition(poi) + Vector2.down * 14, 0, photos.survey.DepthFor(poi), 600);
            var photo = photos.Capture(); Assert.That(photo, Is.Not.Null);
            cabin.OpenComputer(); screen.OpenPhotoLab(); yield return null;
            lab.Refresh();
            Assert.That(lab.SelectedPhotoId, Is.EqualTo(photo.Id));
            Assert.That(lab.SampleName, Is.EqualTo("Mẫu chưa định danh"));
            Assert.That(lab.SubmissionLabel, Is.EqualTo("Phát hiện dữ liệu khảo sát"));
            Assert.That(lab.sendButton.interactable, Is.True);
            yield return Screenshot("1920-real-capture-before-send", 1920, 1080);
            int calls = 0; bool blockedDuringSend = false;
            photos.PhotoSubmitted += value => { calls++; blockedDuringSend = lab.IsSubmitting && !lab.sendButton.interactable; lab.SubmitCurrent(); };
            lab.sendButton.onClick.Invoke(); yield return null;
            Assert.That(calls, Is.EqualTo(1)); Assert.That(blockedDuringSend, Is.True);
            Assert.That(photo.IsMissionPhoto, Is.True);
            Assert.That(lab.SelectedPhotoId, Is.EqualTo(photo.Id));
            Assert.That(lab.SubmissionLabel, Is.EqualTo("Đã gửi dữ liệu khảo sát"));
            Assert.That(lab.SampleName, Is.Not.EqualTo("Mẫu chưa định danh"));
            Assert.That(lab.sendButton.interactable, Is.False);
            AssertTextFits(); yield return Screenshot("1920-real-capture-after-send", 1920, 1080);
            cabin.ClosePanel(); cabin.OpenComputer(); screen.OpenPhotoLab(); yield return null;
            Assert.That(lab.SubmissionLabel, Is.EqualTo("Đã gửi dữ liệu khảo sát"));
            lab.SubmitCurrent(); Assert.That(calls, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<ExpeditionLoop>().MissionRuntime.HasObjective(photo.MissionObjectiveId), Is.True);
            Assert.That(File.Exists(ExpeditionSaveStore.SavePath), Is.True);
        }

        internal static IEnumerator Escape()
        {
            // Match the existing ComputerShell fixture: synthetic keys must reach the Game View
            // even when Codex/Unity Test Runner runs in the background. Restore all input settings.
            var original = InputSystem.settings;
            var settings = Object.Instantiate(original);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.settings = original; Object.Destroy(settings);
            }
        }
        private void AssertTextFits()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var label in lab.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, label.name + ": " + label.text);
                foreach (char letter in label.text.Where(c => !char.IsWhiteSpace(c)))
                    Assert.That(label.font.HasCharacter(letter, true, true), Is.True, "Missing glyph: " + letter);
            }
        }
        private static void AssertContained(UnityEngine.UI.RawImage image)
        {
            Canvas.ForceUpdateCanvases();
            var rect = image.rectTransform.rect; var area = ((RectTransform)image.transform.parent).rect;
            Assert.That(rect.width / rect.height, Is.EqualTo((float)image.texture.width / image.texture.height).Within(.001f));
            Assert.That(rect.width, Is.LessThanOrEqualTo(area.width + .01f));
            Assert.That(rect.height, Is.LessThanOrEqualTo(area.height + .01f));
            Assert.That(image.uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
        }
        private static void AssertRaycast(RectTransform at, GameObject expected)
        {
            var eventData = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(null, at.TransformPoint(at.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(eventData, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.EqualTo(expected), "Top raycast: " + string.Join("; ", hits.Select(h => h.gameObject.name + "/" + h.sortingLayer + "/" + h.sortingOrder + "/" + h.depth)) + " expected canvas " + expected.GetComponentInParent<Canvas>().sortingOrder);
        }
        private static IEnumerator Screenshot(string name, int width, int height)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.That(image.width, Is.EqualTo(width)); Assert.That(image.height, Is.EqualTo(height));
            Directory.CreateDirectory(Evidence);
            File.WriteAllBytes(Path.Combine(Evidence, name + ".png"), image.EncodeToPNG()); Object.Destroy(image);
        }
        private void SetResolution(int width, int height)
        {
#if UNITY_EDITOR
            var assembly = typeof(Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = sizesType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var getSize = group.GetType().GetMethod("GetGameViewSize");
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            int selected = -1;
            for (int i = 0; i < count; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                if ((int)size.GetType().GetProperty("width").GetValue(size) == width &&
                    (int)size.GetType().GetProperty("height").GetValue(size) == height) { selected = i; break; }
            }
            if (selected < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var kindType = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(kindType, "FixedResolution"), width, height, "Photo Lab QA" });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); selected = count;
            }
            gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView, selected); gameView.Repaint();
#endif
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
#if UNITY_EDITOR
            if (gameView != null) gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView, oldSize);
#endif
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
        }
    }
}

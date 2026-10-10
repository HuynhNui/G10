using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class PhotoDeletionPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private PhotoCaptureService Capture => Cabin.GetComponent<PhotoCaptureService>();

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "PhotoDeletion-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return null; flow = SceneFlowController.Instance;
            yield return WaitTransition(); flow.StartNewGame(); yield return WaitTransition();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (flow != null) Object.Destroy(flow.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        private IEnumerator WaitTransition()
        {
            yield return null;
            float end = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty);
        }
        private IEnumerator Reload()
        { flow.LoadMainMenu(); yield return WaitTransition(); flow.ContinueGame(); yield return WaitTransition(); }
        private void Aim()
        {
            var survey = Capture.survey; var poi = survey.FindPoi("zone01-left");
            Assert.That(poi, Is.Not.Null);
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8;
                var position = survey.ContactPosition(poi) - new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 20;
                if (!Cabin.Navigation.CanOccupy(position)) continue;
                Cabin.Navigation.RestoreVoyage(position, i * 22.5f, survey.DepthFor(poi), 0);
                if (survey.TryGetPhotoContact(Cabin.Navigation, Capture.profile.visibleDistance,
                    Capture.profile.fieldOfView, out var found, out _) && found == poi) return;
            }
            Assert.Fail("No reachable angle for the real Zone01 mission photo.");
        }
        private PhotoRecord Candidate()
        {
            Aim(); var photo = Capture.Capture();
            Assert.That(photo, Is.Not.Null, Capture.LastError);
            Assert.That(photo.MissionObjectiveId, Is.Not.Null.And.Not.Empty, photo.Result);
            return photo;
        }
        private PhotoLabView OpenLab()
        {
            Cabin.OpenComputer(); var screen = Cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenPhotoLab(); return screen.GetComponentInChildren<PhotoLabView>(true);
        }
        private bool Contains(string id)
        { foreach (var photo in Capture.Photos) if (photo.Id == id) return true; return false; }
        private string ArchiveFile(string id, string extension) => Path.Combine(Capture.ArchivePath, id + extension);

        [UnityTest] public IEnumerator TenRealCapturesDeleteSevenRemainThreeAfterContinue()
        {
            int charges = Cabin.Navigation.Ship.Photos;
            var ids = new List<string>(); var deletedTextures = new List<Texture2D>();
            for (int i = 0; i < 10; i++)
            {
                var photo = Candidate(); ids.Add(photo.Id);
                if (i < 7) deletedTextures.Add(photo.Image);
                if (i < 9) yield return new WaitForSecondsRealtime(.65f);
            }
            Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(10));
            string objectives = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            for (int i = 0; i < 7; i++)
            {
                Assert.That(Capture.DeletePhoto(ids[i]), Is.True, Capture.LastError);
                Assert.That(File.Exists(ArchiveFile(ids[i], ".png")), Is.False);
                Assert.That(File.Exists(ArchiveFile(ids[i], ".json")), Is.False);
            }
            yield return null;
            foreach (var texture in deletedTextures) Assert.That(texture == null, Is.True, "Deleted textures must be released.");
            Assert.That(Capture.Photos.Count, Is.EqualTo(3)); Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(10));
            Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges - 10), "Deleting never refunds a camera charge.");
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(objectives));
            yield return Reload();
            Assert.That(Capture.Photos.Count, Is.EqualTo(3)); Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(10));
            for (int i = 0; i < 10; i++) Assert.That(Contains(ids[i]), Is.EqualTo(i >= 7));
            Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges - 10));
        }

        [UnityTest] public IEnumerator ConfirmationTargetsIdAfterIndexShiftThenUsesNextPreviousAndEmptyState()
        {
            string a = Candidate().Id; yield return new WaitForSecondsRealtime(.65f);
            string b = Candidate().Id; yield return new WaitForSecondsRealtime(.65f);
            string c = Candidate().Id;
            var lab = OpenLab();
            Assert.That(lab.deleteButton, Is.Not.Null); Assert.That(lab.confirmDeleteButton, Is.Not.Null);
            Assert.That(lab.cancelDeleteButton, Is.Not.Null); Assert.That(lab.previousButton, Is.Not.Null); Assert.That(lab.nextButton, Is.Not.Null);
            Assert.That(lab.SelectedPhotoId, Is.EqualTo(c));
            lab.previousButton.onClick.Invoke(); Assert.That(lab.SelectedPhotoId, Is.EqualTo(b));
            lab.deleteButton.onClick.Invoke();
            Assert.That(lab.IsDeleteConfirmationOpen, Is.True); Assert.That(lab.deleteConfirmation.activeSelf, Is.True);
            Assert.That(lab.DeleteConfirmationMessage, Does.Contain("XÓA ẢNH NÀY?").And.Contain("nhiệm vụ"));
            Assert.That(lab.sendButton.interactable, Is.False); Assert.That(lab.previousButton.interactable, Is.False);
            lab.cancelDeleteButton.onClick.Invoke(); Assert.That(lab.IsDeleteConfirmationOpen, Is.False);
            Assert.That(Capture.Photos.Count, Is.EqualTo(3)); Assert.That(lab.SelectedPhotoId, Is.EqualTo(b));
            lab.deleteButton.onClick.Invoke();
            Assert.That(Capture.DeletePhoto(a), Is.True, Capture.LastError); // The UI index for B now points at C.
            lab.confirmDeleteButton.onClick.Invoke();
            Assert.That(Contains(b), Is.False); Assert.That(Contains(c), Is.True); Assert.That(lab.SelectedPhotoId, Is.EqualTo(c));
            Assert.That(lab.sendButton.interactable, Is.True);
            lab.deleteButton.onClick.Invoke(); lab.confirmDeleteButton.onClick.Invoke();
            Assert.That(Capture.Photos.Count, Is.Zero); Assert.That(lab.DisplayedText, Does.Contain("NO IMAGES RECORDED"));
            Assert.That(lab.preview.enabled, Is.False); Assert.That(lab.preview.texture, Is.Null);
            foreach (var button in new[] { lab.deleteButton, lab.sendButton, lab.previousButton, lab.nextButton }) Assert.That(button.interactable, Is.False);
            Assert.That(lab.IsDeleteConfirmationOpen, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator DeletingFinalImageChoosesPreviousAndReopeningCancelsPendingConfirmation()
        {
            string first = Candidate().Id; yield return new WaitForSecondsRealtime(.65f); string last = Candidate().Id;
            var lab = OpenLab(); Assert.That(lab.SelectedPhotoId, Is.EqualTo(last));
            lab.Previous(); Assert.That(lab.SelectedPhotoId, Is.EqualTo(first));
            lab.deleteButton.onClick.Invoke(); Cabin.ClosePanel(); Assert.That(lab.IsDeleteConfirmationOpen, Is.False);
            lab = OpenLab(); Assert.That(lab.IsDeleteConfirmationOpen, Is.False); Assert.That(lab.SelectedPhotoId, Is.EqualTo(last));
            lab.deleteButton.onClick.Invoke(); lab.confirmDeleteButton.onClick.Invoke();
            Assert.That(Capture.Photos.Count, Is.EqualTo(1)); Assert.That(lab.SelectedPhotoId, Is.EqualTo(first));
            Assert.That(lab.preview.texture, Is.SameAs(Capture.Photos[0].Image));
            Assert.That(lab.deleteButton.interactable, Is.True); yield return null;
        }

        [UnityTest] public IEnumerator SubmittedDeletionPreservesRewardsCountersAndPrunesAllCheckpointCopies()
        {
            var photo = Candidate(); string id = photo.Id, objective = photo.MissionObjectiveId;
            Assert.That(Capture.SubmitPhoto(photo), Is.True); Assert.That(Loop.Rest(), Is.True, Loop.LastError);
            string mission = JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress());
            int charges = Cabin.Navigation.Ship.Photos;
            var lab = OpenLab(); lab.RequestDelete();
            Assert.That(lab.DeleteConfirmationMessage, Is.EqualTo("XÓA ẢNH NÀY?")); lab.CancelDelete(); Cabin.ClosePanel();
            Assert.That(Capture.DeletePhoto(id), Is.True, Capture.LastError);
            Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(1)); Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges));
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(mission));
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.current.photos.Count, Is.Zero); Assert.That(saved.dayStart.photos.Count, Is.Zero);
            foreach (var entry in saved.journal) Assert.That(entry.checkpoint.photos.Count, Is.Zero);
            Assert.That(saved.current.photosTaken, Is.EqualTo(1)); Assert.That(saved.current.dayStartPhotos, Is.EqualTo(1));
            Assert.That(saved.journal[0].photos, Is.EqualTo(1), "The historical daily camera-use summary is not rewritten.");
            yield return Reload(); Assert.That(Capture.Photos.Count, Is.Zero); Assert.That(Loop.MissionRuntime.HasObjective(objective), Is.True);
            Assert.That(Loop.RestoreDay(1), Is.True, Loop.LastError); Assert.That(Capture.Photos.Count, Is.Zero);
            Assert.That(Loop.MissionRuntime.HasObjective(objective), Is.True); Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator FailedTimelineWriteKeepsExactPhotoFilesTextureAndSelection()
        {
            var photo = Candidate(); Assert.That(Loop.SaveCurrent(), Is.True);
            byte[] png = File.ReadAllBytes(ArchiveFile(photo.Id, ".png"));
            string json = File.ReadAllText(ArchiveFile(photo.Id, ".json")), timeline = File.ReadAllText(ExpeditionSaveStore.SavePath);
            var lab = OpenLab(); lab.RequestDelete();
            using (var locked = new FileStream(ExpeditionSaveStore.SavePath + ".tmp", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                lab.ConfirmDelete();
            Assert.That(Capture.Photos.Count, Is.EqualTo(1)); Assert.That(Capture.Photos[0].Image, Is.SameAs(photo.Image));
            Assert.That(lab.SelectedPhotoId, Is.EqualTo(photo.Id)); Assert.That(lab.LastDeleteError, Is.Not.Null.And.Not.Empty);
            Assert.That(lab.DisplayedText, Does.Contain("KHÔNG LƯU ĐƯỢC"));
            Assert.That(File.ReadAllBytes(ArchiveFile(photo.Id, ".png")), Is.EqualTo(png));
            Assert.That(File.ReadAllText(ArchiveFile(photo.Id, ".json")), Is.EqualTo(json));
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath), Is.EqualTo(timeline));
            Assert.That(Directory.GetDirectories(Capture.ArchivePath, ".delete-*").Length, Is.Zero);
            Assert.That(Capture.DeletePhoto(photo.Id), Is.True, Capture.LastError); yield return Reload(); Assert.That(Capture.Photos.Count, Is.Zero);
        }

        [UnityTest] public IEnumerator FailedSecondArchiveMoveRestoresFirstAndDoesNotCommitTimeline()
        {
            var photo = Candidate(); Assert.That(Loop.SaveCurrent(), Is.True);
            string pngPath = ArchiveFile(photo.Id, ".png"), jsonPath = ArchiveFile(photo.Id, ".json");
            byte[] png = File.ReadAllBytes(pngPath); string timeline = File.ReadAllText(ExpeditionSaveStore.SavePath);
            using (var locked = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.That(Capture.DeletePhoto(photo.Id), Is.False);
            Assert.That(Capture.LastError, Is.Not.Null.And.Not.Empty); Assert.That(Capture.Photos.Count, Is.EqualTo(1));
            Assert.That(Capture.Photos[0].Image, Is.SameAs(photo.Image)); Assert.That(File.ReadAllBytes(pngPath), Is.EqualTo(png));
            Assert.That(File.Exists(jsonPath), Is.True); Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath), Is.EqualTo(timeline));
            Assert.That(Directory.GetDirectories(Capture.ArchivePath, ".delete-*").Length, Is.Zero);
            yield return Reload(); Assert.That(Capture.Photos[0].Id, Is.EqualTo(photo.Id));
        }

        [UnityTest] public IEnumerator FailedPrimaryPublishRetainsPreviousRecoveryCopyAndCanRetry()
        {
            var photo = Candidate(); Assert.That(Loop.SaveCurrent(), Is.True);
            string primary = File.ReadAllText(ExpeditionSaveStore.SavePath);
            string backup = File.ReadAllText(ExpeditionSaveStore.SavePath + ".bak");
            // Reads remain legal, but Windows cannot replace a file without FileShare.Delete.
            using (var locked = new FileStream(ExpeditionSaveStore.SavePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                Assert.That(Capture.DeletePhoto(photo.Id), Is.False);
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath), Is.EqualTo(primary));
            Assert.That(File.ReadAllText(ExpeditionSaveStore.SavePath + ".bak"), Is.EqualTo(backup),
                "A rejected deletion must not silently sanitize the previously complete recovery timeline.");
            Assert.That(Capture.Photos.Count, Is.EqualTo(1)); Assert.That(Capture.Photos[0].Image, Is.SameAs(photo.Image));
            Assert.That(File.Exists(ArchiveFile(photo.Id, ".png")), Is.True); Assert.That(File.Exists(ArchiveFile(photo.Id, ".json")), Is.True);
            Assert.That(Capture.DeletePhoto(photo.Id), Is.True, Capture.LastError);
            yield return Reload(); Assert.That(Capture.Photos.Count, Is.Zero);
        }

        [UnityTest] public IEnumerator CorruptPrimaryRecoversDeletionWithoutResurrectingFromBackup()
        {
            var photo = Candidate(); Assert.That(Loop.Rest(), Is.True); Assert.That(Capture.DeletePhoto(photo.Id), Is.True, Capture.LastError);
            var backup = JsonUtility.FromJson<ExpeditionSave>(File.ReadAllText(ExpeditionSaveStore.SavePath + ".bak"));
            Assert.That(backup.current.photos.Count, Is.Zero); Assert.That(backup.dayStart.photos.Count, Is.Zero);
            foreach (var entry in backup.journal) Assert.That(entry.checkpoint.photos.Count, Is.Zero);
            flow.LoadMainMenu(); yield return WaitTransition();
            File.WriteAllText(ExpeditionSaveStore.SavePath, "{corrupt-primary");
            Assert.That(ExpeditionSaveStore.TryRead(out var recovered, out _), Is.True); Assert.That(recovered.current.photos.Count, Is.Zero);
            flow.ContinueGame(); yield return WaitTransition();
            Assert.That(Capture.Photos.Count, Is.Zero); Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(1));
            Assert.That(OpenLab().DisplayedText, Does.Contain("NO IMAGES RECORDED"));
        }

        [UnityTest] public IEnumerator InterruptedArchiveTransactionRecoversOrCleansUsingAuthoritativeTimeline()
        {
            var photo = Candidate(); Assert.That(Loop.SaveCurrent(), Is.True);
            string id = photo.Id, png = ArchiveFile(id, ".png"), json = ArchiveFile(id, ".json");
            byte[] pngBytes = File.ReadAllBytes(png); string metadata = File.ReadAllText(json);
            string staging = Path.Combine(Capture.ArchivePath, ".delete-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(staging);
            File.Move(png, Path.Combine(staging, id + ".png")); File.Move(json, Path.Combine(staging, id + ".json"));
            var probe = new GameObject("Uncommitted photo deletion recovery probe");
            var archive = probe.AddComponent<PhotoCaptureService>();
            Assert.That(archive.Photos.Count, Is.EqualTo(1)); Assert.That(archive.Photos[0].Id, Is.EqualTo(id));
            Assert.That(File.Exists(png), Is.True); Assert.That(File.Exists(json), Is.True); Assert.That(Directory.Exists(staging), Is.False);
            Object.Destroy(probe); yield return null;
            Assert.That(Capture.DeletePhoto(id), Is.True, Capture.LastError);
            Directory.CreateDirectory(staging); File.WriteAllBytes(Path.Combine(staging, id + ".png"), pngBytes); File.WriteAllText(Path.Combine(staging, id + ".json"), metadata);
            probe = new GameObject("Committed photo deletion cleanup probe"); archive = probe.AddComponent<PhotoCaptureService>();
            Assert.That(archive.Photos.Count, Is.Zero); Assert.That(File.Exists(png), Is.False); Assert.That(File.Exists(json), Is.False);
            Assert.That(Directory.Exists(staging), Is.False); Object.Destroy(probe); yield return null;
        }

        [UnityTest] public IEnumerator InvalidAndTraversalIdsNeverDeleteOutsideTheSelectedArchive()
        {
            Candidate(); var original = Capture.ExportPhotos();
            string outside = Path.Combine(folder, "do-not-delete.png"); File.WriteAllText(outside, "retained");
            foreach (string id in new[] { "../do-not-delete", "..\\do-not-delete", "..", ".", Path.Combine(folder, "do-not-delete"), "photo.", "photo " })
            {
                var fixture = ExpeditionSaveStore.Copy(new ExpeditionSnapshot { photos = original }).photos;
                fixture[0].id = id; Capture.RestorePhotos(fixture, 1);
                Assert.That(Capture.DeletePhoto(id), Is.False, id); Assert.That(Capture.Photos.Count, Is.EqualTo(1));
                Assert.That(File.ReadAllText(outside), Is.EqualTo("retained"));
            }
            Capture.RestorePhotos(original, 1);
            Assert.That(Capture.DeletePhoto("missing-id"), Is.False); Assert.That(Capture.Photos.Count, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest] public IEnumerator TwentyFourPhotoBoundCleansEvictedArchiveAndDeleteFreesStorageNotCharges()
        {
            // Lightweight saved-image fixture seeds the existing cap; the next image is an actual
            // capture so charge accounting, trimming, PNG/JSON cleanup and Continue all execute.
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false); texture.Apply();
            string png = Convert.ToBase64String(texture.EncodeToPNG()); Object.Destroy(texture);
            var fixture = new List<SavedPhoto>(); Directory.CreateDirectory(Capture.ArchivePath);
            for (int i = 0; i < PhotoCaptureService.MaximumStoredPhotos; i++)
            {
                string id = "fixture-" + i.ToString("000"); var time = DateTimeOffset.UnixEpoch.AddSeconds(i);
                fixture.Add(new SavedPhoto { id = id, time = time.ToString("O"), result = "NoSubject", png = png });
                File.WriteAllBytes(ArchiveFile(id, ".png"), Convert.FromBase64String(png));
                File.WriteAllText(ArchiveFile(id, ".json"), "{\"id\":\"" + id + "\",\"time\":\"" + time.ToString("O") + "\",\"result\":\"NoSubject\"}");
                File.SetLastWriteTimeUtc(ArchiveFile(id, ".json"), time.UtcDateTime.AddYears(30));
            }
            Capture.RestorePhotos(fixture, 24); int charges = Cabin.Navigation.Ship.Photos;
            string newest = Candidate().Id;
            Assert.That(Capture.Photos.Count, Is.EqualTo(24)); Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(25));
            Assert.That(Contains("fixture-000"), Is.False); Assert.That(File.Exists(ArchiveFile("fixture-000", ".png")), Is.False);
            Assert.That(File.Exists(ArchiveFile("fixture-000", ".json")), Is.False);
            Assert.That(Directory.GetFiles(Capture.ArchivePath, "*.json").Length, Is.EqualTo(24));
            File.WriteAllBytes(ArchiveFile("broken-latest", ".png"), Convert.FromBase64String(png));
            File.WriteAllText(ArchiveFile("broken-latest", ".json"), "{\"time\":\"invalid-date\"}");
            File.SetLastWriteTimeUtc(ArchiveFile("broken-latest", ".json"), DateTime.UtcNow.AddMinutes(1));
            LogAssert.Expect(LogType.Warning, "Skipped unreadable photo archive entry.");
            var probe = new GameObject("Bounded archive with unreadable newest entry"); var archive = probe.AddComponent<PhotoCaptureService>();
            Assert.That(archive.Photos.Count, Is.EqualTo(24)); Assert.That(File.Exists(ArchiveFile("fixture-001", ".json")), Is.True);
            Object.Destroy(probe); File.Delete(ArchiveFile("broken-latest", ".png")); File.Delete(ArchiveFile("broken-latest", ".json"));
            Assert.That(Loop.SaveCurrent(), Is.True, Loop.LastError); yield return Reload();
            Assert.That(Capture.Photos.Count, Is.EqualTo(24)); Assert.That(Contains(newest), Is.True); Assert.That(Contains("fixture-000"), Is.False);
            Assert.That(Capture.DeletePhoto("fixture-001"), Is.True, Capture.LastError);
            Assert.That(Capture.Photos.Count, Is.EqualTo(23)); Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(25));
            Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges - 1));
            Candidate(); Assert.That(Capture.Photos.Count, Is.EqualTo(24)); Assert.That(Capture.TotalPhotosTaken, Is.EqualTo(26));
            Assert.That(Cabin.Navigation.Ship.Photos, Is.EqualTo(charges - 2));
            Assert.That(Directory.GetFiles(Capture.ArchivePath, "*.png").Length, Is.EqualTo(24));
        }
    }
}

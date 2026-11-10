using UnityEngine;

namespace G10.Prototype.Computer
{
    public sealed class PhotoLabView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour repositorySource;
        [SerializeField] private UnityEngine.UI.Text body;
        private IPhotoRepository repository;
        public UnityEngine.UI.RawImage preview;
        public UnityEngine.UI.Button sendButton;
        public UnityEngine.UI.Button deleteButton, previousButton, nextButton;
        public GameObject deleteConfirmation;
        public TMPro.TMP_Text deleteConfirmationText;
        public UnityEngine.UI.Button confirmDeleteButton, cancelDeleteButton;
        private int index = -1;
        private string currentPhotoId, pendingDeleteId;
        private string lastSubmissionError;
        private RectTransform previewArea;
        private TMPro.TMP_Text sampleName, quality, coordinates, depth, heading, capturedAt, submissionStatus, counter, emptyState;
        private GameObject imageViewer;
        private UnityEngine.UI.RawImage enlargedPreview;
        private RectTransform enlargedArea;
        private UnityEngine.UI.Button enlargeButton;
        private ExpeditionLoop expedition;
        private bool submitting;
        private Vector2 fittedArea, fittedEnlargedArea;
        public bool IsViewerOpen => imageViewer != null && imageViewer.activeSelf;
        public bool IsSubmitting => submitting;
        public string SampleName => sampleName != null ? sampleName.text : "Mẫu chưa định danh";
        public string QualityLabel => quality != null ? quality.text : "";
        public string SubmissionLabel => submissionStatus != null ? submissionStatus.text : "";

        public void ConfigurePresentation(TMPro.TMP_Text nameLabel, TMPro.TMP_Text qualityLabel,
            TMPro.TMP_Text coordinateLabel, TMPro.TMP_Text depthLabel, TMPro.TMP_Text headingLabel,
            TMPro.TMP_Text timeLabel, TMPro.TMP_Text statusLabel, TMPro.TMP_Text countLabel, TMPro.TMP_Text emptyLabel,
            UnityEngine.UI.Button imageButton, GameObject viewer, UnityEngine.UI.RawImage largeImage, RectTransform largeArea)
        {
            if (enlargeButton != null) enlargeButton.onClick.RemoveListener(OpenViewer);
            sampleName = nameLabel; quality = qualityLabel; coordinates = coordinateLabel; depth = depthLabel;
            heading = headingLabel; capturedAt = timeLabel; submissionStatus = statusLabel; counter = countLabel; emptyState = emptyLabel;
            enlargeButton = imageButton; imageViewer = viewer; enlargedPreview = largeImage; enlargedArea = largeArea;
            if (enlargeButton != null) enlargeButton.onClick.AddListener(OpenViewer);
            CloseViewer(); Refresh();
        }
        public void OpenViewer()
        {
            if (IsDeleteConfirmationOpen || submitting || SelectedPhoto()?.Image == null || imageViewer == null) return;
            enlargedPreview.texture = SelectedPhoto().Image;
            enlargedPreview.color = Color.white; enlargedPreview.material = null;
            imageViewer.SetActive(true); imageViewer.transform.SetAsLastSibling();
            FitImage(enlargedPreview, enlargedArea);
            Canvas.ForceUpdateCanvases(); Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(imageViewer.GetComponentInChildren<UnityEngine.UI.Button>().gameObject);
        }
        public void CloseViewer()
        {
            if (imageViewer != null) imageViewer.SetActive(false);
            if (enlargedPreview != null) enlargedPreview.texture = null;
        }
        public bool TryHandleBack()
        {
            if (IsViewerOpen) { CloseViewer(); Refresh(); return true; }
            if (IsDeleteConfirmationOpen) { CancelDelete(); return true; }
            return submitting;
        }
        private void LateUpdate()
        {
            if (previewArea != null && fittedArea != previewArea.rect.size)
            { fittedArea = previewArea.rect.size; FitPreview(preview != null ? preview.texture : null); }
            if (IsViewerOpen && enlargedArea != null && fittedEnlargedArea != enlargedArea.rect.size)
            { fittedEnlargedArea = enlargedArea.rect.size; FitImage(enlargedPreview, enlargedArea); }
        }
        private static void FitImage(UnityEngine.UI.RawImage image, RectTransform area)
        {
            if (image == null || image.texture == null || area == null) return;
            var available = area.rect.size;
            float scale = Mathf.Min(available.x / image.texture.width, available.y / image.texture.height);
            image.uvRect = new Rect(0, 0, 1, 1);
            image.rectTransform.sizeDelta = new Vector2(image.texture.width, image.texture.height) * Mathf.Max(0, scale);
        }
        public static string DescribeQuality(string result) => result switch
        {
            nameof(PhotoResultType.GoodPhoto) => "Ảnh đạt yêu cầu",
            nameof(PhotoResultType.LifeDetected) => "Phát hiện sinh vật",
            nameof(PhotoResultType.NoSubject) => "Chưa thấy đối tượng",
            nameof(PhotoResultType.TooFar) => "Đối tượng quá xa",
            nameof(PhotoResultType.Obstructed) => "Ảnh bị che khuất",
            nameof(PhotoResultType.LowVisibility) => "Tầm nhìn hạn chế",
            _ => "Chưa đánh giá chất lượng"
        };
        private string KnownSampleName(PhotoRecord photo)
        {
            // A catalog binding alone is not knowledge. Require a durable PHOTO receipt.
            if (photo == null || string.IsNullOrEmpty(photo.MissionTargetId)) return "Mẫu chưa định danh";
            expedition ??= GetComponentInParent<ComputerScreenController>()?.Expedition;
            var runtime = (repositorySource as PhotoCaptureService)?.survey?.MissionRuntime;
            bool known = photo.IsMissionPhoto ||
                runtime != null && runtime.config != null && runtime.config.zoneId == photo.MissionZoneId && runtime.HasObjective(photo.MissionObjectiveId);
            if (!known && expedition != null && !string.IsNullOrEmpty(photo.MissionZoneId))
                known = expedition.ReadZoneState(photo.MissionZoneId)?.missionProgress?.completedObjectives?.Contains(photo.MissionObjectiveId) == true;
            if (!known) return "Mẫu chưa định danh";
            var content = runtime?.FindContent(photo.MissionTargetId);
            if (content == null && expedition?.contentCatalog != null)
                foreach (var item in expedition.contentCatalog)
                    if (item != null && item.id == photo.MissionTargetId) { content = item; break; }
            return !string.IsNullOrWhiteSpace(content?.displayName) ? content.displayName : "Mẫu chưa định danh";
        }
        private void RefreshPresentation(PhotoRecord photo)
        {
            bool hasImage = photo?.Image != null;
            bool blocked = submitting || IsDeleteConfirmationOpen || IsViewerOpen;
            if (sampleName != null) sampleName.text = KnownSampleName(photo);
            if (quality != null)
            {
                quality.text = photo != null ? DescribeQuality(photo.Result) : "Chưa có ảnh";
                quality.color = photo?.Result == nameof(PhotoResultType.GoodPhoto) ? new Color(.08f, .35f, .34f) : new Color(.46f, .29f, .35f);
            }
            if (coordinates != null) coordinates.text = photo != null ? $"X {photo.MapCoordinate.x:0.0} · Y {photo.MapCoordinate.y:0.0}" : "—";
            if (depth != null) depth.text = photo != null ? $"{photo.Depth:0.#} m" : "—";
            if (heading != null) heading.text = photo != null ? $"{photo.Heading:0.#}°" : "—";
            if (capturedAt != null) capturedAt.text = photo != null ? photo.CapturedAt.ToLocalTime().ToString("dd.MM.yyyy · HH:mm") : "—";
            if (counter != null) counter.text = photo != null ? $"{index + 1:00} / {repository.Photos.Count:00}" : "00 / 00";
            if (emptyState != null)
            {
                emptyState.gameObject.SetActive(!hasImage);
                emptyState.text = photo != null ? "Không đọc được ảnh đã lưu" : "Chưa có ảnh\nChụp ảnh từ cabin để bắt đầu khảo sát";
            }
            if (enlargeButton != null) enlargeButton.interactable = hasImage && !blocked;
            var state = photo != null && repository is IPhotoSubmissionRepository submissions
                ? submissions.GetSubmissionState(photo) : PhotoSubmissionState.NotCandidate;
            if (submissionStatus != null) submissionStatus.text = submitting ? "Đang gửi dữ liệu…"
                : !string.IsNullOrEmpty(LastDeleteError) ? LastDeleteError
                : !string.IsNullOrEmpty(lastSubmissionError) ? lastSubmissionError
                : photo == null ? "Chưa có dữ liệu khảo sát"
                : state switch
                {
                    PhotoSubmissionState.Available => "Phát hiện dữ liệu khảo sát",
                    PhotoSubmissionState.Submitted => "Đã gửi dữ liệu khảo sát",
                    PhotoSubmissionState.ObjectiveCompleted => "Nhiệm vụ khảo sát đã hoàn thành",
                    PhotoSubmissionState.Unavailable => "Chưa thể gửi ở khu vực hiện tại",
                    _ => "Ảnh không có dữ liệu nhiệm vụ"
                };
            if (sendButton != null) sendButton.interactable = state == PhotoSubmissionState.Available && !blocked;
            if (deleteButton != null) deleteButton.interactable = photo != null && repository is IPhotoDeletionRepository && !blocked;
            if (previousButton != null) previousButton.interactable = photo != null && index > 0 && !blocked;
            if (nextButton != null) nextButton.interactable = photo != null && index < repository.Photos.Count - 1 && !blocked;
            if (IsViewerOpen && !hasImage) CloseViewer();
        }
        public string SelectedPhotoId => currentPhotoId;
        public bool SelectedUsesFullPainting { get; private set; }
        public bool IsDeleteConfirmationOpen => !string.IsNullOrEmpty(pendingDeleteId);
        public string DeleteConfirmationMessage { get; private set; }
        public string LastDeleteError { get; private set; }
        public void Previous()
        {
            if (IsDeleteConfirmationOpen || IsViewerOpen || submitting) return;
            index = Mathf.Max(0, index - 1); currentPhotoId = null; LastDeleteError = null; lastSubmissionError = null; Refresh();
        }
        public void Next()
        {
            if (IsDeleteConfirmationOpen || IsViewerOpen || submitting) return;
            index = Mathf.Min((repository?.Photos.Count ?? 0) - 1, index + 1); currentPhotoId = null; LastDeleteError = null; lastSubmissionError = null; Refresh();
        }
        public string DisplayedText => body != null ? body.text : "";
        private void Awake() => repository = repositorySource as IPhotoRepository;
        private void OnEnable()
        {
            // Preserve the existing lab entry behavior: a newly captured image is shown first.
            index = -1; currentPhotoId = null; LastDeleteError = null; lastSubmissionError = null;
            CloseViewer(); CancelDeleteState(); BindDeletionButtons(); Refresh();
        }
        private void OnDisable() { UnbindDeletionButtons(); CancelDeleteState(); CloseViewer(); }
        public void ConfigurePreviewArea(RectTransform area)
        {
            previewArea = area;
            if (preview != null && area != null)
            {
                var rect = preview.rectTransform;
                rect.SetParent(area, false);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }
            Refresh();
        }
        private void FitPreview(Texture image)
        {
            if (preview == null || image == null) return;
            preview.uvRect = new Rect(0, 0, 1, 1);
            preview.color = Color.white;
            if (SelectedUsesFullPainting)
            {
                // The PSD composite already includes its lighting, texture and artist's frame.
                preview.color = Color.white;
                preview.material = null;
            }
            if (previewArea == null) return;
            Vector2 available = previewArea.rect.size;
            if (available.x <= 0 || available.y <= 0) return;
            float scale = Mathf.Min(available.x / image.width, available.y / image.height);
            preview.rectTransform.sizeDelta = new Vector2(image.width * scale, image.height * scale);
        }
        public void ConfigureDeletionUI(UnityEngine.UI.Button delete, UnityEngine.UI.Button previous, UnityEngine.UI.Button next,
            GameObject confirmation, TMPro.TMP_Text confirmationText, UnityEngine.UI.Button confirm, UnityEngine.UI.Button cancel)
        {
            UnbindDeletionButtons();
            deleteButton = delete; previousButton = previous; nextButton = next;
            deleteConfirmation = confirmation; deleteConfirmationText = confirmationText;
            confirmDeleteButton = confirm; cancelDeleteButton = cancel;
            CancelDeleteState();
            if (isActiveAndEnabled) BindDeletionButtons();
            Refresh();
        }
        private void BindDeletionButtons()
        {
            UnbindDeletionButtons();
            deleteButton?.onClick.AddListener(RequestDelete);
            confirmDeleteButton?.onClick.AddListener(ConfirmDelete);
            cancelDeleteButton?.onClick.AddListener(CancelDelete);
        }
        private void UnbindDeletionButtons()
        {
            deleteButton?.onClick.RemoveListener(RequestDelete);
            confirmDeleteButton?.onClick.RemoveListener(ConfirmDelete);
            cancelDeleteButton?.onClick.RemoveListener(CancelDelete);
        }
        public void RequestDelete()
        {
            if (IsDeleteConfirmationOpen || IsViewerOpen || submitting || repository is not IPhotoDeletionRepository) return;
            Refresh();
            var photo = SelectedPhoto();
            if (photo == null) return;
            pendingDeleteId = photo.Id;
            lastSubmissionError = null;
            LastDeleteError = null;
            DeleteConfirmationMessage = "XÓA ẢNH NÀY?";
            if (!photo.IsMissionPhoto && !string.IsNullOrEmpty(photo.MissionObjectiveId))
                DeleteConfirmationMessage += "\nẢnh chưa gửi có thể cần cho nhiệm vụ.";
            if (deleteConfirmation != null) deleteConfirmation.transform.SetAsLastSibling();
            Refresh(); Canvas.ForceUpdateCanvases();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(cancelDeleteButton != null ? cancelDeleteButton.gameObject : null);
        }
        public void CancelDelete() { CancelDeleteState(); Refresh(); }
        private void CancelDeleteState()
        {
            pendingDeleteId = null; DeleteConfirmationMessage = null;
            if (deleteConfirmation != null) deleteConfirmation.SetActive(false);
        }
        public void ConfirmDelete()
        {
            if (!IsDeleteConfirmationOpen || repository is not IPhotoDeletionRepository deletion) return;
            string id = pendingDeleteId;
            int removedIndex = FindPhotoIndex(id);
            CancelDeleteState();
            if (removedIndex < 0)
            { LastDeleteError = "ẢNH KHÔNG CÒN KHẢ DỤNG."; Refresh(); return; }
            if (deletion.DeletePhoto(id))
            {
                // Removing the selected ID shifts the next image into this index. Clamp to
                // the preceding image only when the deleted record was the final one.
                index = removedIndex; currentPhotoId = null; LastDeleteError = deletion.LastError;
            }
            else { currentPhotoId = id; LastDeleteError = deletion.LastError ?? "KHÔNG XÓA ĐƯỢC ẢNH. THỬ LẠI."; }
            Refresh();
        }
        private int FindPhotoIndex(string id)
        {
            if (repository == null || string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < repository.Photos.Count; i++) if (repository.Photos[i].Id == id) return i;
            return -1;
        }
        private PhotoRecord SelectedPhoto()
            => repository != null && index >= 0 && index < repository.Photos.Count ? repository.Photos[index] : null;
        public void SubmitCurrent()
        {
            if (IsDeleteConfirmationOpen || IsViewerOpen || submitting) return;
            if (repository is IPhotoSubmissionRepository submissions && index >= 0 && index < repository.Photos.Count)
            {
                var selected = repository.Photos[index];
                if (!submissions.CanSubmitPhoto(selected)) { Refresh(); return; }
                submitting = true; RefreshPresentation(selected);
                try { submissions.SubmitPhoto(selected); lastSubmissionError = submissions.LastError; }
                finally { submitting = false; }
            }
            Refresh();
        }
        public void Refresh()
        {
            repository ??= repositorySource as IPhotoRepository;
            SelectedUsesFullPainting = false;
            if (sendButton != null) sendButton.interactable = false;
            bool hasPhotos = repository?.Photos.Count > 0;
            if (IsDeleteConfirmationOpen && FindPhotoIndex(pendingDeleteId) < 0) CancelDeleteState();
            if (deleteButton != null) deleteButton.interactable = hasPhotos && repository is IPhotoDeletionRepository && !IsDeleteConfirmationOpen;
            if (previousButton != null) previousButton.interactable = hasPhotos && repository.Photos.Count > 1 && !IsDeleteConfirmationOpen;
            if (nextButton != null) nextButton.interactable = hasPhotos && repository.Photos.Count > 1 && !IsDeleteConfirmationOpen;
            if (deleteConfirmation != null) deleteConfirmation.SetActive(IsDeleteConfirmationOpen);
            if (deleteConfirmationText != null) deleteConfirmationText.text = DeleteConfirmationMessage ?? "";
            if (preview != null) { preview.enabled = hasPhotos; if (!hasPhotos) preview.texture = null; }
            if (repository == null) { if (body != null) body.text = "PHOTO STORAGE OFFLINE"; RefreshPresentation(null); return; }
            string text = hasPhotos ? $"{repository.Photos.Count} IMAGES RECORDED" : "NO IMAGES RECORDED";
            text += repository.CameraOnline ? "\n\nCAMERA MODULE ONLINE" : "\n\nCAMERA MODULE OFFLINE";
            if (!hasPhotos)
            {
                index = -1; currentPhotoId = null;
                CloseViewer(); RefreshPresentation(null);
                if (body != null) body.text = text + (string.IsNullOrEmpty(LastDeleteError) ? "" : "\n" + LastDeleteError);
                return;
            }
            int retainedIndex = FindPhotoIndex(currentPhotoId);
            if (retainedIndex >= 0) index = retainedIndex;
            index=index<0?repository.Photos.Count-1:Mathf.Clamp(index,0,repository.Photos.Count-1);
            var photo=repository.Photos[index]; currentPhotoId = photo.Id;
            SelectedUsesFullPainting = photo.IsFullPainting;
            if (preview != null) { preview.texture=photo.Image; FitPreview(photo.Image); }
            text=$"ẢNH {index+1}/{repository.Photos.Count} • {photo.Result}\nX {photo.MapCoordinate.x:0.0} Y {photo.MapCoordinate.y:0.0} • {photo.Depth:0.0} m • {photo.Heading:0.0}°\n{photo.CapturedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
            if (repository is IPhotoSubmissionRepository submissions)
            {
                var state = submissions.GetSubmissionState(photo);
                if (sendButton != null) sendButton.interactable = state == PhotoSubmissionState.Available && !IsDeleteConfirmationOpen;
                // Retain the legacy data-only status binding for existing integrations.
                text += !string.IsNullOrEmpty(lastSubmissionError) ? "\n" + lastSubmissionError : state switch
                {
                    PhotoSubmissionState.Available => "\nMISSION DATA DETECTED",
                    PhotoSubmissionState.Submitted => "\nMISSION DATA SUBMITTED",
                    PhotoSubmissionState.ObjectiveCompleted => "\nOBJECTIVE ALREADY COMPLETED",
                    PhotoSubmissionState.Unavailable when !string.IsNullOrEmpty(photo.MissionObjectiveId) => "\nMISSION DATA UNAVAILABLE IN CURRENT ZONE",
                    _ => ""
                };
            }
            if (!string.IsNullOrEmpty(LastDeleteError)) text += "\n" + LastDeleteError;
            if (body != null) body.text = text;
            RefreshPresentation(photo);
        }
    }
}

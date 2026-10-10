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
        public string SelectedPhotoId => currentPhotoId;
        public bool IsDeleteConfirmationOpen => !string.IsNullOrEmpty(pendingDeleteId);
        public string DeleteConfirmationMessage { get; private set; }
        public string LastDeleteError { get; private set; }
        public void Previous()
        {
            if (IsDeleteConfirmationOpen) return;
            index = Mathf.Max(0, index - 1); currentPhotoId = null; LastDeleteError = null; Refresh();
        }
        public void Next()
        {
            if (IsDeleteConfirmationOpen) return;
            index = Mathf.Min((repository?.Photos.Count ?? 0) - 1, index + 1); currentPhotoId = null; LastDeleteError = null; Refresh();
        }
        public string DisplayedText => body != null ? body.text : "";
        private void Awake() => repository = repositorySource as IPhotoRepository;
        private void OnEnable()
        {
            // Preserve the existing lab entry behavior: a newly captured image is shown first.
            index = -1; currentPhotoId = null; LastDeleteError = null;
            CancelDeleteState(); BindDeletionButtons(); Refresh();
        }
        private void OnDisable() { UnbindDeletionButtons(); CancelDeleteState(); }
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
            if (IsDeleteConfirmationOpen || repository is not IPhotoDeletionRepository) return;
            Refresh();
            var photo = SelectedPhoto();
            if (photo == null) return;
            pendingDeleteId = photo.Id;
            LastDeleteError = null;
            DeleteConfirmationMessage = "XÓA ẢNH NÀY?";
            if (!photo.IsMissionPhoto && !string.IsNullOrEmpty(photo.MissionObjectiveId))
                DeleteConfirmationMessage += "\nẢnh chưa SEND có thể cần cho nhiệm vụ.";
            Refresh();
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
            if (IsDeleteConfirmationOpen) return;
            if (repository is IPhotoSubmissionRepository submissions && index >= 0 && index < repository.Photos.Count)
                submissions.SubmitPhoto(repository.Photos[index]);
            Refresh();
        }
        public void Refresh()
        {
            repository ??= repositorySource as IPhotoRepository;
            if (sendButton != null) sendButton.interactable = false;
            bool hasPhotos = repository?.Photos.Count > 0;
            if (IsDeleteConfirmationOpen && FindPhotoIndex(pendingDeleteId) < 0) CancelDeleteState();
            if (deleteButton != null) deleteButton.interactable = hasPhotos && repository is IPhotoDeletionRepository && !IsDeleteConfirmationOpen;
            if (previousButton != null) previousButton.interactable = hasPhotos && repository.Photos.Count > 1 && !IsDeleteConfirmationOpen;
            if (nextButton != null) nextButton.interactable = hasPhotos && repository.Photos.Count > 1 && !IsDeleteConfirmationOpen;
            if (deleteConfirmation != null) deleteConfirmation.SetActive(IsDeleteConfirmationOpen);
            if (deleteConfirmationText != null) deleteConfirmationText.text = DeleteConfirmationMessage ?? "";
            if (preview != null) { preview.enabled = hasPhotos; if (!hasPhotos) preview.texture = null; }
            if (repository == null) { if (body != null) body.text = "PHOTO STORAGE OFFLINE"; return; }
            string text = hasPhotos ? $"{repository.Photos.Count} IMAGES RECORDED" : "NO IMAGES RECORDED";
            text += repository.CameraOnline ? "\n\nCAMERA MODULE ONLINE" : "\n\nCAMERA MODULE OFFLINE";
            if (!hasPhotos)
            {
                index = -1; currentPhotoId = null;
                if (body != null) body.text = text + (string.IsNullOrEmpty(LastDeleteError) ? "" : "\n" + LastDeleteError);
                return;
            }
            int retainedIndex = FindPhotoIndex(currentPhotoId);
            if (retainedIndex >= 0) index = retainedIndex;
            index=index<0?repository.Photos.Count-1:Mathf.Clamp(index,0,repository.Photos.Count-1);
            var photo=repository.Photos[index]; currentPhotoId = photo.Id;
            if (preview != null) preview.texture=photo.Image;
            text=$"ẢNH {index+1}/{repository.Photos.Count} • {photo.Result}\nX {photo.MapCoordinate.x:0.0} Y {photo.MapCoordinate.y:0.0} • {photo.Depth:0.0} m • {photo.Heading:0.0}°\n{photo.CapturedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
            if (repository is IPhotoSubmissionRepository submissions)
            {
                var state = submissions.GetSubmissionState(photo);
                if (sendButton != null) sendButton.interactable = state == PhotoSubmissionState.Available && !IsDeleteConfirmationOpen;
                text += state switch
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
        }
    }
}

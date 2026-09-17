using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Capture checks the live encounter, never a stale radar echo.</summary>
    public sealed class CreatureCatcher : MonoBehaviour
    {
        public ZoneNavigation navigation;
        public PhotoSurveyZone survey;
        public CreatureInventory inventory;
        public Texture2D itemIcon;
        public string itemName = "Sinh vật Zone 1";
        public float captureRadius => survey != null && survey.TargetPoi != null ? survey.TargetPoi.arrivalRadius : 0f;
        [Min(0f)] public float depthTolerance = 10f;
        public CaptureMinigameController minigame;
        public enum Result { Caught, Empty, Full, Unavailable, PhotoRequired, Started, Failed, Cancelled, Busy }
        public Result? LastResult { get; private set; }
        public event System.Action<Result> CaptureResolved;
        private bool pending;
        private PhotoSurveyZone pendingSurvey;
        private string pendingCreatureId;
        private MapPoi pendingPoi;

        public Result TryCapture()
        {
            if (pending) return Result.Busy;
            var invalid = ValidateConditions();
            if (invalid.HasValue) return SetResult(invalid.Value);
            if (minigame == null) return SetResult(Result.Unavailable);
            pending = true;
            pendingSurvey = survey;
            pendingCreatureId = survey.creatureId;
            pendingPoi = survey.TargetPoi;
            if (!minigame.Begin(ResolveCapture))
            {
                pending = false;
                return SetResult(Result.Unavailable);
            }
            return SetResult(Result.Started);
        }

        private Result? ValidateConditions()
        {
            if (navigation == null || survey == null || survey.TargetPoi == null || inventory == null || itemIcon == null)
                return Result.Unavailable;
            if (!survey.creaturePresent || !survey.Contains(navigation.Position) ||
                Mathf.Abs(navigation.Depth - survey.targetDepth) > depthTolerance ||
                !survey.Detectable(navigation, Mathf.Sqrt(captureRadius * captureRadius + depthTolerance * depthTolerance)))
                return Result.Empty;
            if (inventory.IsFull) return Result.Full;
            if (!survey.CanCapture) return Result.PhotoRequired;
            return null;
        }

        private void ResolveCapture(CaptureMinigameResult result)
        {
            if (!pending) return;
            pending = false;
            if (result != CaptureMinigameResult.Success)
            {
                SetResult(result == CaptureMinigameResult.Cancelled ? Result.Cancelled : Result.Failed);
                return;
            }
            // Revalidate after the modal closes. Never award a replaced encounter or overfill the bag.
            if (survey != pendingSurvey || survey == null || survey.creatureId != pendingCreatureId || survey.TargetPoi != pendingPoi)
            { SetResult(Result.Unavailable); return; }
            var invalid = ValidateConditions();
            if (invalid.HasValue) { SetResult(invalid.Value); return; }
            if (!inventory.TryAdd(survey.creatureId, itemName, itemIcon)) { SetResult(Result.Empty); return; }
            survey.creaturePresent = false;
            survey.CompleteTask(PhotoSurveyZone.TaskKind.Capture);
            SetResult(Result.Caught);
        }

        private Result SetResult(Result result)
        {
            LastResult = result;
            CaptureResolved?.Invoke(result);
            return result;
        }
        private void OnDisable() { if (pending && minigame != null) minigame.Cancel(); }
    }
}

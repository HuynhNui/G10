using G10.Prototype.Audio;
using G10.Prototype.Missions;
using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Capture/collection resolves the POI under the submarine, never a global active POI.</summary>
    public sealed class CreatureCatcher : MonoBehaviour
    {
        public ZoneNavigation navigation;
        public PhotoSurveyZone survey;
        public CreatureInventory inventory;
        public Texture2D itemIcon;
        public string itemName = "Sinh vật";
        [SerializeField, Min(.1f)] private float defaultCaptureRadius = 20f;
        public float DefaultCaptureRadius => Mathf.Max(.1f, defaultCaptureRadius);
        public float CaptureRadiusFor(MapPoi poi)
            => poi != null && poi.arrivalRadius > 0f ? poi.arrivalRadius : DefaultCaptureRadius;
        public float captureRadius => DefaultCaptureRadius;
        [Min(0f)] public float depthTolerance = 10f;
        public CaptureMinigameController minigame;
        public enum Result { Caught, Empty, Full, Unavailable, PhotoRequired, Started, Failed, Cancelled, Busy, NoCharges }
        public Result? LastResult { get; private set; }
        public event System.Action<Result> CaptureResolved;

        private bool pending;
        private PhotoSurveyZone pendingSurvey;
        private string pendingCreatureId;
        private MapPoi pendingPoi;
        private ZoneMissionRuntime pendingRuntime;
        private string pendingTargetId;

        public Result TryCapture()
        {
            if (pending) return Result.Busy;
            var invalid = ValidateConditions(out var poi, out var objective);
            if (invalid.HasValue) return SetResult(invalid.Value);
            if (minigame == null) return SetResult(Result.Unavailable);
            if (navigation.Ship.Captures <= 0) return SetResult(Result.NoCharges);
            pending = true;
            pendingSurvey = survey;
            pendingCreatureId = survey.creatureId;
            pendingPoi = poi;
            pendingRuntime = survey.MissionRuntime;
            pendingTargetId = objective?.targetId;
            if (!minigame.Begin(ResolveCapture))
            {
                ClearPending();
                return SetResult(Result.Unavailable);
            }
            navigation.Ship.TryUse(ShipCharge.Capture);
            return SetResult(Result.Started);
        }

        private Result? ValidateConditions(out MapPoi poi, out MissionObjectiveConfig objective, bool requireCharge = true)
        {
            poi = null; objective = null;
            if (navigation != null && navigation.ExpeditionBlocked) return Result.Unavailable;
            if (navigation == null || survey == null || inventory == null) return Result.Unavailable;
            if (navigation.Ship.Hull <= 0) return Result.Unavailable;
            poi = survey.FindContactContaining(navigation.Position);
            if (poi == null || Mathf.Abs(navigation.Depth - survey.DepthFor(poi)) > depthTolerance ||
                !survey.Detectable(navigation, poi, Mathf.Sqrt(poi.arrivalRadius * poi.arrivalRadius + depthTolerance * depthTolerance)))
                return Result.Empty;

            var runtime = survey.MissionRuntime;
            if (runtime != null)
            {
                objective = runtime.FindObjective(poi.id, MissionObjectiveType.Collect) ?? runtime.FindObjective(poi.id, MissionObjectiveType.Capture);
                if (objective == null || runtime.HasObjective(objective.id) && !runtime.IsRepeatableCapture(objective) || !runtime.IsContentPresent(poi.id)) return Result.Empty;
                var photo = runtime.FindObjective(poi.id, MissionObjectiveType.Photograph, objective.targetId);
                if (objective.type == MissionObjectiveType.Capture && photo != null && photo.required && !runtime.HasObjective(photo.id))
                    return Result.PhotoRequired;
            }
            else
            {
                if (!survey.creaturePresent) return Result.Empty;
                if (!survey.CanCapture) return Result.PhotoRequired;
            }
            if (!inventory.CanAdd(objective?.targetId ?? survey.creatureId)) return Result.Full;
            if (requireCharge && navigation.Ship.Captures <= 0) return Result.NoCharges;
            return null;
        }

        private void ResolveCapture(CaptureMinigameResult result)
        {
            if (!pending) return;
            pending = false;
            if (result != CaptureMinigameResult.Success)
            {
                ClearPendingReferences();
                SetResult(result == CaptureMinigameResult.Cancelled ? Result.Cancelled : Result.Failed);
                return;
            }
            if (survey != pendingSurvey || survey == null || survey.creatureId != pendingCreatureId ||
                survey.FindContactContaining(navigation.Position) != pendingPoi || survey.MissionRuntime != pendingRuntime)
            { ClearPendingReferences(); SetResult(Result.Unavailable); return; }

            // The charge was paid when this attempt began, including the final available charge.
            var invalid = ValidateConditions(out var poi, out var objective, requireCharge: false);
            if (invalid.HasValue || poi != pendingPoi || objective?.targetId != pendingTargetId)
            { ClearPendingReferences(); SetResult(invalid ?? Result.Unavailable); return; }

            if (pendingRuntime != null)
            {
                if (!pendingRuntime.ResolveCapture(pendingPoi.id, objective))
                { ClearPendingReferences(); SetResult(Result.Empty); return; }
            }
            else
            {
                if (!inventory.TryAdd(survey.creatureId, itemName, itemIcon))
                { ClearPendingReferences(); SetResult(Result.Empty); return; }
                survey.creaturePresent = false;
                survey.CompleteTask(PhotoSurveyZone.TaskKind.Capture);
            }
            AudioManager.Instance?.PlayCaptureSuccess();
            ClearPendingReferences();
            SetResult(Result.Caught);
        }

        private void ClearPending() { pending = false; ClearPendingReferences(); }
        private void ClearPendingReferences()
        {
            pendingSurvey = null; pendingCreatureId = null; pendingPoi = null; pendingRuntime = null; pendingTargetId = null;
        }
        private Result SetResult(Result result) { LastResult = result; CaptureResolved?.Invoke(result); return result; }
        private void OnDisable() { if (pending && minigame != null) minigame.Cancel(); }
    }
}

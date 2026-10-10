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
        // Serialized legacy tolerance is retained for old scenes; gameplay uses the survey's one depth band.
        [HideInInspector] public float depthTolerance = 10f;
        public CaptureMinigameController minigame;
        public enum Result { Caught, Empty, Full, Unavailable, PhotoRequired, Started, Failed, Cancelled, Busy, NoCharges,
            NoTarget, TooFar, TooShallow, TooDeep, Occluded }
        public static string FeedbackText(Result result) => result switch
        {
            Result.Caught => "CAPTURE SUCCESS", Result.Full => "CARGO FULL",
            Result.PhotoRequired => "PHOTO DATA REQUIRED", Result.TooFar => "TOO FAR FROM TARGET",
            Result.TooShallow => "TARGET TOO SHALLOW", Result.TooDeep => "TARGET TOO DEEP",
            Result.Occluded => "TARGET BLOCKED BY TERRAIN", Result.Empty or Result.NoTarget => "NO CAPTURE TARGET",
            Result.NoCharges => "NO CAPTURE ATTEMPTS", Result.Failed => "CAPTURE FAILED — TRY AGAIN",
            Result.Cancelled => "CAPTURE CANCELLED", Result.Started or Result.Busy => "CAPTURE IN PROGRESS",
            _ => "CAPTURE UNAVAILABLE"
        };
        public Result? LastResult { get; private set; }
        public event System.Action<Result> CaptureResolved;
        public string LastCapturedId { get; private set; }
        public bool LastCaptureWasRepeat { get; private set; }
        public string LastSuccessText { get; private set; }

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
            if (!navigation.Ship.CanAttemptCapture) return SetResult(Result.NoCharges);
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
            var spatial = ValidateSpatial(survey, navigation, out poi);
            if (spatial.HasValue) return spatial;

            var runtime = survey.MissionRuntime;
            if (runtime != null)
            {
                objective = runtime.FindObjective(poi.id, MissionObjectiveType.Collect) ?? runtime.FindObjective(poi.id, MissionObjectiveType.Capture);
                if (objective == null || runtime.HasObjective(objective.id) && !runtime.IsRepeatableCapture(objective) || !runtime.IsContentPresent(poi.id)) return Result.NoTarget;
                var photo = runtime.FindObjective(poi.id, MissionObjectiveType.Photograph, objective.targetId);
                if (objective.type == MissionObjectiveType.Capture && photo != null && photo.required && !runtime.HasObjective(photo.id))
                    return Result.PhotoRequired;
            }
            else
            {
                if (!survey.creaturePresent) return Result.NoTarget;
                if (!survey.CanCapture) return Result.PhotoRequired;
            }
            if (!inventory.CanAdd(objective?.targetId ?? survey.creatureId)) return Result.Full;
            if (requireCharge && !navigation.Ship.CanAttemptCapture) return Result.NoCharges;
            return null;
        }

        public static Result? ValidateSpatial(PhotoSurveyZone survey, ZoneNavigation navigation, out MapPoi poi)
        {
            poi = survey.FindNearestContact(navigation.Position);
            if (poi == null) return Result.NoTarget;
            if (Vector2.Distance(navigation.Position, survey.ContactPosition(poi)) > poi.arrivalRadius)
            {
                var closestSpawn = survey.FindNearestContact(navigation.Position, includeAbsent: true);
                if (closestSpawn != null && !survey.IsRadarContactPresent(closestSpawn) &&
                    Vector2.Distance(navigation.Position, survey.ContactPosition(closestSpawn)) <= closestSpawn.arrivalRadius)
                    return Result.NoTarget; // Consumed one-time location, not a distant remaining creature.
                return Result.TooFar;
            }
            if (navigation.Depth < survey.DepthFor(poi)) return Result.TooShallow;
            if (!survey.IsInteractionDepth(navigation.Depth, poi)) return Result.TooDeep;
            if (!survey.Detectable(navigation, poi, poi.arrivalRadius)) return Result.Occluded;
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

            bool repeat = pendingRuntime != null && pendingRuntime.HasObjective(objective.id);
            string capturedId = pendingTargetId ?? survey.creatureId;
            if (pendingRuntime != null)
            {
                if (!pendingRuntime.ResolveCapture(pendingPoi.id, objective))
                { ClearPendingReferences(); SetResult(Result.NoTarget); return; }
            }
            else
            {
                if (!inventory.TryAdd(survey.creatureId, itemName, itemIcon))
                { ClearPendingReferences(); SetResult(Result.NoTarget); return; }
                survey.creaturePresent = false;
                survey.CompleteTask(PhotoSurveyZone.TaskKind.Capture);
            }
            AudioManager.Instance?.PlayCaptureSuccess();
            LastCapturedId = capturedId;
            LastCaptureWasRepeat = repeat;
            var content = pendingRuntime?.FindContent(capturedId);
            string name = !string.IsNullOrEmpty(content?.displayName) ? content.displayName : itemName;
            LastSuccessText = (repeat ? "+1 MATERIAL" : "+1 ITEM") + $" • {name}\nCARGO: {inventory.GetCount(capturedId)}";
            ClearPendingReferences();
            SetResult(Result.Caught);
        }

        private void ClearPending() { pending = false; ClearPendingReferences(); }
        private void ClearPendingReferences()
        {
            pendingSurvey = null; pendingCreatureId = null; pendingPoi = null; pendingRuntime = null; pendingTargetId = null;
        }
        private Result SetResult(Result result)
        {
            LastResult = result;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (result is not (Result.Started or Result.Caught or Result.Busy or Result.Cancelled or Result.Failed))
            {
                var poi = navigation != null ? survey?.FindNearestContact(navigation.Position, includeAbsent: result == Result.NoTarget) : null;
                var runtime = survey?.MissionRuntime;
                var objective = poi != null ? runtime?.FindObjective(poi.id, MissionObjectiveType.Collect) ?? runtime?.FindObjective(poi.id, MissionObjectiveType.Capture) : null;
                var photo = poi != null ? runtime?.FindObjective(poi.id, MissionObjectiveType.Photograph, objective?.targetId) : null;
                Debug.Log($"[Capture rejected] reason={result}; zone={runtime?.config?.zoneId}; poi={poi?.id}; objective={objective?.id}; marker={poi?.mapPosition}; contact={(poi != null ? survey.ContactPosition(poi) : default)}; ship={navigation?.Position}; xy={(poi != null ? Vector2.Distance(navigation.Position, survey.ContactPosition(poi)) : -1):0.00}; radius={poi?.arrivalRadius}; depth={navigation?.Depth}; target={(poi != null ? survey.DepthFor(poi) : -1)}; deeperBand={survey?.DeeperInteractionRange}; present={(poi != null && survey.IsRadarContactPresent(poi))}; photo={photo?.id}; submitted={(photo != null && runtime.HasObjective(photo.id))}; charges={navigation?.Ship.Captures}; unlimited={navigation?.Ship.UnlimitedCaptureAttempts}; cargo={inventory?.Items.Count}; canStack={(inventory != null && inventory.CanAdd(objective?.targetId ?? survey?.creatureId))}", this);
            }
#endif
            CaptureResolved?.Invoke(result); return result;
        }
        private void OnDisable() { if (pending && minigame != null) minigame.Cancel(); }
    }
}

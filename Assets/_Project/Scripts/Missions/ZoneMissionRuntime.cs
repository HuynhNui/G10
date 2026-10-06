using System;
using System.Collections.Generic;
using System.Text;
using G10.Prototype.Navigation;
using UnityEngine;

namespace G10.Prototype.Missions
{
    /// <summary>Generic, ID-based mission runtime shared by every zone.</summary>
    public class ZoneMissionRuntime : MonoBehaviour
    {
        public ZoneMissionConfig config;
        public PhotoSurveyZone survey;
        public ZoneNavigation navigation;
        public CreatureInventory inventory;
        public SurveyContentDefinition[] contentCatalog = Array.Empty<SurveyContentDefinition>();
        [SerializeField] private MissionProgressState missionProgress = new();

        public MissionProgressState ProgressState => missionProgress;
        public string LastMessage { get; protected set; }
        public int CompletedCount => missionProgress?.completedObjectives?.Count ?? 0;
        public bool Complete => MainObjectivesComplete;
        public bool IsGateComplete => config?.zoneGate == null || RequiredObjectivesComplete(config.zoneGate.objectives);
        // Availability is owned by the expedition's saved ending choice, not by mission rewards.
        public bool HiddenRouteAvailable { get; set; }
        public bool MainObjectivesComplete => MainLocationsComplete &&
            (!HasRequiredObjectives(config?.zoneGate?.objectives) || IsGateComplete);
        public bool MainLocationsComplete
        {
            get
            {
                if (config?.locations == null) return false;
                bool hasRequired = false;
                foreach (var location in config.locations)
                {
                    if (location == null || location.visibility != LocationVisibility.Visible || !HasRequiredObjectives(location.objectives)) continue;
                    hasRequired = true;
                    if (!RequiredObjectivesComplete(location.objectives)) return false;
                }
                return hasRequired;
            }
        }
        public event Action Changed;

        protected virtual void Awake()
        {
            missionProgress ??= new MissionProgressState();
            missionProgress.Normalize();
            if (survey != null) survey.MissionRuntime = this;
        }

        public MissionProgressState ExportProgress() => missionProgress?.Copy() ?? new MissionProgressState();

        public virtual void RestoreProgress(MissionProgressState restored)
        {
            missionProgress = restored?.Copy() ?? new MissionProgressState();
            missionProgress.Normalize();
            EvaluateRules();
            LastMessage = null;
            NotifyChanged();
        }

        public bool HasObjective(string id) => Has(missionProgress.completedObjectives, id);
        public bool HasResearch(string id) => Has(missionProgress.researchData, id);
        public bool HasItem(string id) => Has(missionProgress.collectedItems, id) || inventory != null && inventory.Contains(id);
        public bool HasRecipe(string id) => Has(missionProgress.unlockedRecipes, id);
        public bool HasWorldFlag(string id) => Has(missionProgress.worldFlags, id);
        public bool HasZone(string id) => Has(missionProgress.unlockedZones, id);
        public bool HasEnding(string id) => Has(missionProgress.endings, id);

        public bool IsLocationRevealed(string locationId)
        {
            var location = config != null ? config.FindLocation(locationId) : null;
            return location == null || location.visibility == LocationVisibility.Visible ||
                HiddenRouteAvailable && Has(missionProgress.revealedLocations, locationId);
        }

        public bool IsPoiAvailable(string poiId)
        {
            var location = LocationForPoi(poiId);
            return location == null || location.visibility == LocationVisibility.Visible || HiddenRouteAvailable;
        }

        public bool IsPoiVisible(string poiId)
        {
            var location = config != null ? config.FindLocationByPoi(poiId) : null;
            return location == null || IsLocationRevealed(location.id);
        }

        public bool RevealPoi(string poiId)
        {
            var location = config != null ? config.FindLocationByPoi(poiId) : null;
            if (location == null || !HiddenRouteAvailable || location.visibility != LocationVisibility.HiddenRadar || !Add(missionProgress.revealedLocations, location.id)) return false;
            LastMessage = $"Đã phát hiện {LocationName(location)}.";
            NotifyChanged();
            return true;
        }

        public MissionLocationConfig LocationForPoi(string poiId) => config != null ? config.FindLocationByPoi(poiId) : null;

        public MissionObjectiveConfig FindObjective(string poiId, MissionObjectiveType type, string targetId = null)
        {
            var location = LocationForPoi(poiId);
            if (location?.objectives == null) return null;
            foreach (var objective in location.objectives)
                if (objective != null && objective.type == type &&
                    (string.IsNullOrEmpty(targetId) || objective.targetId == targetId)) return objective;
            return null;
        }

        public string TargetIdForPoi(string poiId)
        {
            var location = LocationForPoi(poiId);
            if (location?.objectives == null) return null;
            foreach (var objective in location.objectives)
                if (objective != null && !string.IsNullOrEmpty(objective.targetId)) return objective.targetId;
            return null;
        }

        public SurveyContentDefinition ContentForPoi(string poiId) => FindContent(TargetIdForPoi(poiId));

        public SurveyContentDefinition FindContent(string id)
        {
            if (contentCatalog == null || string.IsNullOrEmpty(id)) return null;
            return Array.Find(contentCatalog, entry => entry != null && entry.id == id);
        }

        public virtual bool IsContentPresent(string poiId)
        {
            if (!IsPoiAvailable(poiId)) return false;
            var location = LocationForPoi(poiId);
            if (location?.objectives == null) return true;
            foreach (var objective in location.objectives)
                if (objective != null && (objective.type == MissionObjectiveType.Capture || objective.type == MissionObjectiveType.Collect) &&
                    !IsRepeatableCapture(objective) && HasObjective(objective.id))
                    return false;
            return true;
        }

        public bool IsRepeatableCapture(MissionObjectiveConfig objective)
            => objective != null && objective.type == MissionObjectiveType.Capture && objective.repeatableCapture;

        private bool IsRepeatableMaterial(string id)
        {
            foreach (var location in config?.locations ?? Array.Empty<MissionLocationConfig>())
                foreach (var objective in location?.objectives ?? Array.Empty<MissionObjectiveConfig>())
                    if (IsRepeatableCapture(objective) && objective.targetId == id) return true;
            return false;
        }

        public bool ResolveCapture(string poiId, MissionObjectiveConfig objective)
        {
            if (objective == null || inventory == null || !inventory.CanAdd(objective.targetId) || !IsContentPresent(poiId)) return false;
            if (!HasObjective(objective.id)) return RecordObjective(poiId, objective.type, objective.targetId);
            if (!IsRepeatableCapture(objective)) return false;
            var content = FindContent(objective.targetId);
            if (!inventory.TryAdd(objective.targetId, content?.displayName ?? objective.targetId, content?.Image)) return false;
            LastMessage = $"Captured: {objective.targetId} ×{inventory.GetCount(objective.targetId)}";
            NotifyChanged();
            return true;
        }

        public bool RecordObjective(string poiId, MissionObjectiveType type, string targetId = null)
        {
            if (!IsPoiAvailable(poiId)) return false;
            var objective = FindObjective(poiId, type, targetId);
            if (objective == null || string.IsNullOrEmpty(objective.id) || !Add(missionProgress.completedObjectives, objective.id)) return false;
            var location = LocationForPoi(poiId);
            if (location != null && location.visibility == LocationVisibility.HiddenRadar)
                Add(missionProgress.revealedLocations, location.id);
            LastMessage = $"Đã hoàn thành: {ObjectiveName(objective)}.";
            EvaluateRules();
            NotifyChanged();
            return true;
        }

        protected bool CompleteObjectiveById(string objectiveId)
        {
            if (!Add(missionProgress.completedObjectives, objectiveId)) return false;
            EvaluateRules();
            return true;
        }

        public bool RecordGlobalObjective(MissionObjectiveType type, string targetId)
        {
            if (config?.zoneGate?.objectives != null)
                foreach (var objective in config.zoneGate.objectives)
                    if (objective != null && objective.type == type && objective.targetId == targetId)
                    {
                        if (!Add(missionProgress.completedObjectives, objective.id)) return false;
                        LastMessage = $"Đã hoàn thành: {ObjectiveName(objective)}.";
                        EvaluateRules(); NotifyChanged(); return true;
                    }
            return false;
        }

        public bool IsLocationComplete(string locationId)
        {
            var location = config != null ? config.FindLocation(locationId) : null;
            return location != null && RequiredObjectivesComplete(location.objectives);
        }

        public bool IsPoiComplete(string poiId)
        {
            var location = LocationForPoi(poiId);
            return location != null && RequiredObjectivesComplete(location.objectives);
        }

        public bool AreRequiredLocationsComplete()
        {
            if (config?.locations == null) return false;
            foreach (var location in config.locations)
                if (location != null && !RequiredObjectivesComplete(location.objectives)) return false;
            return config.locations.Length > 0;
        }

        public string LocationText(string poiId)
        {
            var location = LocationForPoi(poiId);
            if (location == null) return string.IsNullOrEmpty(poiId) ? "LOCATION NOT CONFIGURED" : poiId;
            var text = new StringBuilder(LocationName(location));
            if (location.visibility == LocationVisibility.HiddenRadar && !IsLocationRevealed(location.id)) text.Append(" • HIDDEN");
            var poi = survey != null ? survey.FindPoi(poiId) : null;
            if (poi != null && IsLocationRevealed(location.id))
                text.Append("\nTARGET DEPTH: ").Append(survey.DepthFor(poi).ToString("0")).Append(" M");
            if (location.objectives != null)
                foreach (var objective in location.objectives)
                    if (objective != null) text.Append('\n').Append(HasObjective(objective.id) ? "[x] " : "[ ] ").Append(ObjectiveName(objective));
            return text.ToString();
        }

        public virtual string MissionText()
        {
            var text = new StringBuilder(config != null && !string.IsNullOrEmpty(config.displayName) ? config.displayName : "MISSION");
            if (config?.locations != null)
                foreach (var location in config.locations)
                {
                    if (location == null || !IsLocationRevealed(location.id)) continue;
                    text.Append("\n\n").Append(LocationText(location.poiId));
                }
            if (config?.zoneGate?.objectives != null && config.zoneGate.objectives.Length > 0)
            {
                text.Append("\n\nZONE GATE");
                foreach (var objective in config.zoneGate.objectives)
                    if (objective != null) text.Append('\n').Append(HasObjective(objective.id) ? "[x] " : "[ ] ").Append(ObjectiveName(objective));
            }
            return text.ToString();
        }

        protected void Grant(MissionRewardType type, string targetId, string sourceId = "manual")
        {
            if (string.IsNullOrEmpty(targetId)) return;
            ApplyReward(new MissionRewardConfig { type = type, targetId = targetId }, sourceId);
        }

        protected void NotifyChanged() => Changed?.Invoke();

        private void EvaluateRules()
        {
            if (config?.locations != null)
                foreach (var location in config.locations)
                    if (location != null && RequiredObjectivesComplete(location.objectives)) ApplyRewards(location.rewards, "location:" + location.id);
            if (config?.zoneGate != null && RequiredObjectivesComplete(config.zoneGate.objectives))
                ApplyRewards(config.zoneGate.rewards, "gate:" + config.zoneGate.id);
            if (config?.completions != null)
                foreach (var completion in config.completions)
                    if (completion != null && RequiredIdsComplete(completion.requiredObjectiveIds)) ApplyRewards(completion.rewards, "completion:" + completion.id);
        }

        private bool RequiredObjectivesComplete(MissionObjectiveConfig[] objectives)
        {
            if (objectives == null || objectives.Length == 0) return false;
            bool hasRequired = false;
            foreach (var objective in objectives)
            {
                if (objective == null || !objective.required) continue;
                hasRequired = true;
                if (!HasObjective(objective.id)) return false;
            }
            return hasRequired;
        }

        private static bool HasRequiredObjectives(MissionObjectiveConfig[] objectives)
        {
            if (objectives != null)
                foreach (var objective in objectives)
                    if (objective != null && objective.required) return true;
            return false;
        }

        private bool RequiredIdsComplete(string[] ids)
        {
            if (ids == null || ids.Length == 0) return false;
            foreach (string id in ids) if (!HasObjective(id)) return false;
            return true;
        }

        private void ApplyRewards(MissionRewardConfig[] rewards, string sourceId)
        {
            if (rewards == null) return;
            for (int i = 0; i < rewards.Length; i++) ApplyReward(rewards[i], sourceId + ":" + i);
        }

        private void ApplyReward(MissionRewardConfig reward, string rewardId)
        {
            if (reward == null || string.IsNullOrEmpty(reward.targetId) || !Add(missionProgress.grantedRewards, rewardId)) return;
            switch (reward.type)
            {
                case MissionRewardType.ResearchData: Add(missionProgress.researchData, reward.targetId); break;
                case MissionRewardType.Item:
                    Add(missionProgress.collectedItems, reward.targetId);
                    var content = FindContent(reward.targetId);
                    // Old story saves can grant the same one-time item through both a location and legacy reward ID.
                    // Stacking must not duplicate those items; explicit farm captures still receive their first +1.
                    if (inventory != null && (!inventory.Contains(reward.targetId) || IsRepeatableMaterial(reward.targetId)))
                        inventory.TryAdd(reward.targetId, content != null && !string.IsNullOrEmpty(content.displayName) ? content.displayName : reward.targetId, content?.Image);
                    break;
                case MissionRewardType.UnlockRecipe: Add(missionProgress.unlockedRecipes, reward.targetId); break;
                case MissionRewardType.SetWorldFlag: Add(missionProgress.worldFlags, reward.targetId); break;
                case MissionRewardType.RevealLocation: Add(missionProgress.revealedLocations, reward.targetId); break;
                case MissionRewardType.UnlockZone: Add(missionProgress.unlockedZones, reward.targetId); break;
                case MissionRewardType.Ending: Add(missionProgress.endings, reward.targetId); break;
            }
        }

        private static bool Has(List<string> values, string id) => values != null && !string.IsNullOrEmpty(id) && values.Contains(id);
        private static bool Add(List<string> values, string id)
        {
            if (values == null || string.IsNullOrEmpty(id) || values.Contains(id)) return false;
            values.Add(id); return true;
        }
        private static string LocationName(MissionLocationConfig location)
            => !string.IsNullOrEmpty(location.displayName) ? location.displayName : location.id;
        private static string ObjectiveName(MissionObjectiveConfig objective)
            => $"{objective.type}: {(string.IsNullOrEmpty(objective.targetId) ? objective.id : objective.targetId)}";
    }
}

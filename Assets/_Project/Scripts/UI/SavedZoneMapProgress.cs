using System;
using System.Text;
using G10.Prototype.Computer;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;

namespace G10.Prototype.UI
{
    /// <summary>Detached, read-only projection. Viewing a historical map never evaluates rewards.</summary>
    public sealed class SavedZoneMapProgress
    {
        private readonly ZoneMapConfig map;
        private readonly ExpeditionZoneState state;
        public bool ReturnAvailable { get; }
        public SavedZoneMapProgress(ZoneMapConfig config, ExpeditionZoneState saved, bool returnAvailable = false)
        { map = config; state = saved; ReturnAvailable = returnAvailable; }
        private ZoneMissionConfig Mission => map?.missionConfig;
        public int CompletedCount => state?.missionProgress?.completedObjectives.Count ?? 0;
        public bool HasObjective(string id) => !string.IsNullOrEmpty(id) && state?.missionProgress?.completedObjectives.Contains(id) == true;
        public bool HasWorldFlag(string id) => !string.IsNullOrEmpty(id) && state?.missionProgress?.worldFlags.Contains(id) == true;
        public bool HasRecipe(string id) => !string.IsNullOrEmpty(id) && state?.missionProgress?.unlockedRecipes.Contains(id) == true;
        public bool IsLocationRevealed(string id)
        {
            var location = Mission?.FindLocation(id);
            return location == null || location.visibility == LocationVisibility.Visible ||
                state?.progress?.hiddenRouteUnlocked == true && state.missionProgress.revealedLocations.Contains(id);
        }
        public bool IsPoiVisible(string id)
        { var location = Mission?.FindLocationByPoi(id); return location == null || IsLocationRevealed(location.id); }
        public bool IsPoiComplete(string id) => Complete(Mission?.FindLocationByPoi(id)?.objectives);
        private bool Complete(MissionObjectiveConfig[] objectives)
        {
            bool required = false;
            foreach (var objective in objectives ?? Array.Empty<MissionObjectiveConfig>())
                if (objective != null && objective.required)
                { required = true; if (!HasObjective(objective.id)) return false; }
            return required;
        }
        public bool MainLocationsComplete
        {
            get
            {
                bool required = false;
                foreach (var location in Mission?.locations ?? Array.Empty<MissionLocationConfig>())
                {
                    if (location == null || location.visibility != LocationVisibility.Visible ||
                        !Array.Exists(location.objectives ?? Array.Empty<MissionObjectiveConfig>(), o => o != null && o.required)) continue;
                    required = true; if (!Complete(location.objectives)) return false;
                }
                return required;
            }
        }
        public bool MainObjectivesComplete => HasWorldFlag("LEGACY_ROUTE_UNLOCKED_" + map?.zoneId) || MainLocationsComplete &&
            (!Array.Exists(Mission?.zoneGate?.objectives ?? Array.Empty<MissionObjectiveConfig>(), o => o != null && o.required) ||
                Complete(Mission.zoneGate.objectives));
        public bool UsesAlternate => HasObjective(map?.alternateObjectiveId) || HasWorldFlag(map?.alternateWorldFlag);
        public bool HiddenDestinationAvailable
        {
            get
            {
                if (!MainObjectivesComplete || state?.progress?.hiddenRouteUnlocked != true || map?.finalHiddenPoint == null || map.hiddenLocationIds.Length == 0) return false;
                foreach (string id in map.hiddenLocationIds)
                    if (Mission?.FindLocation(id) == null || (!IsLocationRevealed(id) || !Complete(Mission.FindLocation(id).objectives))) return false;
                return true;
            }
        }
        public string LocationText(string poiId)
        {
            var location = Mission?.FindLocationByPoi(poiId);
            if (location == null) return poiId ?? "LOCATION NOT CONFIGURED";
            var text = new StringBuilder(string.IsNullOrEmpty(location.displayName) ? location.id : location.displayName);
            var poi = Array.Find(map.locations, p => p != null && p.id == poiId);
            if (poi != null && IsPoiVisible(poiId))
                text.Append("\nTARGET DEPTH: ").Append((poi.overrideDepth ? poi.targetDepth : map.entryDepth).ToString("0")).Append(" M");
            foreach (var objective in location.objectives ?? Array.Empty<MissionObjectiveConfig>())
                if (objective != null) text.Append('\n').Append(HasObjective(objective.id) ? "[x] " : "[ ] ")
                    .Append(objective.type).Append(": ").Append(string.IsNullOrEmpty(objective.targetId) ? objective.id : objective.targetId);
            return text.ToString();
        }
    }
}

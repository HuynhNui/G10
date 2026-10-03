using System;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Missions
{
    /// <summary>Cabin interaction adapter; location/objective state lives in the ID-based mission runtime.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneOneStory : ZoneMissionRuntime
    {
        // Legacy input only. Values stay stable so v1 checkpoints can be migrated.
        [Flags]
        public enum Progress
        {
            None = 0, RadarOne = 1, PhotoOne = 2, Analysis = 4, RadarTwo = 8,
            Tube = 16, Recipe = 32, PhotoTwo = 64, Installed = 256
        }

        public const string LocationOne = "Z1_L1";
        public const string LocationTwo = "Z1_L2";
        public const string LocationThree = "Z1_L3";
        public const string PhotoOneObjective = "Z1_L1_PHOTO";
        public const string BlueprintObjective = "Z1_L2_COLLECT";
        public const string PhotoTwoObjective = "Z1_L3_PHOTO";
        public const string PressureData = "Z1_PRESSURE_DATA";
        public const string EmmaBlueprint = "Z1_ITEM_EMMA_BLUEPRINT";
        public const string PressureHullRecipe = "RECIPE_PRESSURE_HULL";
        public const string PressureHullUpgrade = "UPGRADE_PRESSURE_HULL";

        public CabinStationView cabin;
        public SurveyContentDefinition creatureOne, creatureTwo, emmaTube;
        public MapPoi RockInteractionArea { get; set; }
        [Tooltip("Legacy POI order retained only for authored scene compatibility and display numbering.")]
        public string[] poiIds = Array.Empty<string>();
        [Min(1)] public float upgradedMaximumDepth = 750;
        [Min(1)] public float hullBonus = 25;
        [SerializeField, HideInInspector] private int migratedLegacyBits;

        public int SavedProgress => LegacyProjection();
        public bool CanInstall => config != null && config.zoneId == "Zone01" && !Blocked && MainLocationsComplete &&
            HasRecipe(PressureHullRecipe) && !HasObjective("Z1_GATE_INSTALL_PRESSURE_HULL");
        private bool Blocked => navigation == null || navigation.ExpeditionBlocked || cabin != null && cabin.Panels != null && cabin.Panels.IsModalOpen;

        public MissionObjectiveConfig PendingGateObjective
        {
            get
            {
                if (config?.zoneGate?.objectives != null)
                    foreach (var objective in config.zoneGate.objectives)
                        if (objective != null && objective.required && !HasObjective(objective.id)) return objective;
                return null;
            }
        }

        public bool CanApplyProgressionAction
        {
            get
            {
                var objective = PendingGateObjective;
                if (Blocked || !GateFieldworkComplete || objective == null) return false;
                if (objective.type == MissionObjectiveType.DestroyObstacle)
                    return RockInteractionArea != null && RockInteractionArea.Contains(navigation.Position);
                return objective.type == MissionObjectiveType.Craft || objective.type == MissionObjectiveType.InstallUpgrade;
            }
        }

        private bool GateFieldworkComplete
        {
            get
            {
                if (config?.zoneId != "Zone03") return MainLocationsComplete;
                // Only research/material/recipe locations feed the Rock Breaker.
                // The remaining survey still gates the exit, but not the breaker action.
                bool hasPrerequisites = false;
                if (config.locations != null)
                    foreach (var location in config.locations)
                    {
                        if (location == null || location.visibility != LocationVisibility.Visible || location.rewards == null) continue;
                        bool suppliesUpgrade = Array.Exists(location.rewards, reward => reward != null &&
                            (reward.type == MissionRewardType.ResearchData || reward.type == MissionRewardType.Item || reward.type == MissionRewardType.UnlockRecipe));
                        if (!suppliesUpgrade) continue;
                        hasPrerequisites = true;
                        if (!IsLocationComplete(location.id)) return false;
                    }
                return hasPrerequisites || MainLocationsComplete;
            }
        }

        public string ProgressionActionTitle
        {
            get
            {
                var objective = PendingGateObjective;
                if (objective == null) return "UPGRADE COMPLETE";
                string target = (objective.targetId ?? "").Replace("UPGRADE_", "").Replace('_', ' ');
                return (objective.type == MissionObjectiveType.Craft ? "CRAFT " :
                    objective.type == MissionObjectiveType.DestroyObstacle ? "BREAK " : "INSTALL ") + target;
            }
        }

        public string ProgressionActionDescription
        {
            get
            {
                if (!GateFieldworkComplete) return "Complete the research, recovery and recipe objectives to unlock this expedition upgrade.";
                var objective = PendingGateObjective;
                if (objective == null) return "Main progression complete. Travel to the marked exit on the map.";
                if (objective.type == MissionObjectiveType.DestroyObstacle)
                    return RockInteractionArea == null ? "Rock interaction area is not configured." :
                        $"Navigate to the rock at ({RockInteractionArea.mapPosition.x:0}, {RockInteractionArea.mapPosition.y:0}), then use BREAK ROCK BARRIER here. Opening the route does not move the submarine.";
                return objective.type == MissionObjectiveType.Craft ?
                    "Use the recovered research, module and unlocked recipe to craft the Rock Breaker, then install it." :
                    "Install the upgrade earned from this zone's research and recovered items.";
            }
        }

        protected override void Awake()
        {
            if (config == null) config = CreateFallbackConfig();
            if (contentCatalog == null || contentCatalog.Length == 0) contentCatalog = new[] { creatureOne, creatureTwo, emmaTube };
            base.Awake();
            if (survey != null) survey.Story = this;
        }

        public bool Has(Progress flag)
        {
            if (flag == Progress.None) return true;
            bool result = true;
            if ((flag & Progress.RadarOne) != 0) result &= (migratedLegacyBits & (int)Progress.RadarOne) != 0;
            if ((flag & Progress.RadarTwo) != 0) result &= (migratedLegacyBits & (int)Progress.RadarTwo) != 0;
            if ((flag & Progress.PhotoOne) != 0) result &= HasObjective(PhotoOneObjective);
            if ((flag & Progress.Analysis) != 0) result &= HasResearch(PressureData);
            if ((flag & Progress.Tube) != 0) result &= HasObjective(BlueprintObjective) || HasItem(EmmaBlueprint);
            if ((flag & Progress.Recipe) != 0) result &= HasRecipe(PressureHullRecipe);
            if ((flag & Progress.PhotoTwo) != 0) result &= HasObjective(PhotoTwoObjective);
            if ((flag & Progress.Installed) != 0) result &= HasObjective("Z1_GATE_INSTALL_PRESSURE_HULL");
            return result;
        }

        public static int CountProgress(int legacyValue)
        {
            int value = legacyValue & ((int)Progress.RadarOne | (int)Progress.PhotoOne | (int)Progress.RadarTwo |
                (int)Progress.Tube | (int)Progress.PhotoTwo | (int)Progress.Installed);
            int count = 0;
            while (value != 0) { count += value & 1; value >>= 1; }
            return count;
        }

        public void Restore(int legacyValue)
        {
            RestoreProgress(new MissionProgressState());
            migratedLegacyBits = legacyValue & 511;
            if ((legacyValue & (int)Progress.PhotoOne) != 0) CompleteObjectiveById(PhotoOneObjective);
            if ((legacyValue & (int)Progress.Analysis) != 0 || (legacyValue & (int)Progress.PhotoOne) != 0)
                Grant(MissionRewardType.ResearchData, PressureData, "legacy:pressure-data");
            if ((legacyValue & (int)Progress.Tube) != 0)
            {
                CompleteObjectiveById(BlueprintObjective);
                Grant(MissionRewardType.Item, EmmaBlueprint, "legacy:emma-blueprint");
            }
            if ((legacyValue & (int)Progress.PhotoTwo) != 0) CompleteObjectiveById(PhotoTwoObjective);
            if ((legacyValue & (int)Progress.Installed) != 0)
            {
                CompleteObjectiveById("Z1_GATE_INSTALL_PRESSURE_HULL");
                Grant(MissionRewardType.UnlockZone, "Zone02", "legacy:zone02");
            }
            NotifyChanged();
        }

        public override void RestoreProgress(MissionProgressState restored)
        {
            base.RestoreProgress(restored);
            migratedLegacyBits = LegacyProjection();
        }

        public void RecordRadar(string poiId)
        {
            if (Blocked || string.IsNullOrEmpty(poiId)) return;
            if (poiIds.Length > 0 && poiId == poiIds[0]) migratedLegacyBits |= (int)Progress.RadarOne;
            if (poiIds.Length > 1 && poiId == poiIds[1]) migratedLegacyBits |= (int)Progress.RadarTwo;
            RevealPoi(poiId);
            NotifyChanged();
        }

        public void RecordPhoto(string poiId, string targetId = null)
        {
            if (Blocked) return;
            RecordObjective(poiId, MissionObjectiveType.Photograph, targetId);
        }

        public CreatureCatcher.Result? ValidateCollection(MapPoi poi, float depthTolerance, bool requireCharge = true)
        {
            if (Blocked || inventory == null || survey == null || navigation == null || navigation.Ship.Hull <= 0)
                return CreatureCatcher.Result.Unavailable;
            if (poi == null || survey.FindContactContaining(navigation.Position) != poi ||
                Mathf.Abs(navigation.Depth - survey.targetDepth) > depthTolerance ||
                !survey.Detectable(navigation, poi, Mathf.Sqrt(poi.arrivalRadius * poi.arrivalRadius + depthTolerance * depthTolerance)))
                return CreatureCatcher.Result.Empty;
            var objective = FindObjective(poi.id, MissionObjectiveType.Collect) ?? FindObjective(poi.id, MissionObjectiveType.Capture);
            if (objective == null || HasObjective(objective.id) || !IsContentPresent(poi.id)) return CreatureCatcher.Result.Empty;
            var photo = FindObjective(poi.id, MissionObjectiveType.Photograph, objective.targetId);
            if (objective.type == MissionObjectiveType.Capture && photo != null && photo.required && !HasObjective(photo.id))
                return CreatureCatcher.Result.PhotoRequired;
            if (inventory.IsFull) return CreatureCatcher.Result.Full;
            if (requireCharge && navigation.Ship.Captures <= 0) return CreatureCatcher.Result.NoCharges;
            return null;
        }

        public CreatureCatcher.Result CompleteCollection(MapPoi poi, float depthTolerance)
        {
            var invalid = ValidateCollection(poi, depthTolerance, false);
            if (invalid.HasValue) return invalid.Value;
            var objective = FindObjective(poi.id, MissionObjectiveType.Collect) ?? FindObjective(poi.id, MissionObjectiveType.Capture);
            if (!RecordObjective(poi.id, objective.type, objective.targetId)) return CreatureCatcher.Result.Empty;
            return CreatureCatcher.Result.Caught;
        }

        public bool InstallHull()
        {
            if (!CanInstall) return false;
            var next = navigation.Ship.Export();
            next.maximumDepth = Mathf.Max(next.maximumDepth, upgradedMaximumDepth);
            next.hullCapacity += hullBonus;
            if (!next.IsValid) return false;
            navigation.Ship.Restore(next);
            if (!RecordGlobalObjective(MissionObjectiveType.InstallUpgrade, PressureHullUpgrade)) return false;
            LastMessage = "Đã lắp Pressure Hull. Zone02 đã được mở.";
            return true;
        }

        public bool ApplyProgressionAction()
        {
            if (!CanApplyProgressionAction) return false;
            var objective = PendingGateObjective;
            if (objective.type == MissionObjectiveType.InstallUpgrade && objective.targetId == PressureHullUpgrade)
                return InstallHull();
            if (!RecordGlobalObjective(objective.type, objective.targetId)) return false;
            if (objective.type == MissionObjectiveType.DestroyObstacle)
                Grant(MissionRewardType.SetWorldFlag, "ROCK_BARRIER_DESTROYED", "rock-destroyed");
            LastMessage = objective.type == MissionObjectiveType.DestroyObstacle ?
                "Rock barrier removed. Travel through the opened route to the exit." :
                $"Completed: {objective.type} {objective.targetId}.";
            NotifyChanged();
            return true;
        }

        public string MapLocationText(int index)
        {
            if (index < 0 || index >= poiIds.Length) return string.Empty;
            return LocationText(poiIds[index]);
        }

        public bool IsLocationComplete(int index)
            => index >= 0 && index < poiIds.Length && IsPoiComplete(poiIds[index]);

        private int LegacyProjection()
        {
            int value = migratedLegacyBits & ((int)Progress.RadarOne | (int)Progress.RadarTwo);
            if (HasObjective(PhotoOneObjective)) value |= (int)Progress.PhotoOne;
            if (HasResearch(PressureData)) value |= (int)Progress.Analysis;
            if (HasObjective(BlueprintObjective) || HasItem(EmmaBlueprint)) value |= (int)Progress.Tube;
            if (HasRecipe(PressureHullRecipe)) value |= (int)Progress.Recipe;
            if (HasObjective(PhotoTwoObjective)) value |= (int)Progress.PhotoTwo;
            if (HasObjective("Z1_GATE_INSTALL_PRESSURE_HULL")) value |= (int)Progress.Installed;
            return value;
        }

        private static ZoneMissionConfig CreateFallbackConfig()
        {
            var result = ScriptableObject.CreateInstance<ZoneMissionConfig>();
            result.hideFlags = HideFlags.DontSave;
            result.zoneId = "Zone01"; result.displayName = "ZONE 1";
            result.locations = new[]
            {
                Location(LocationOne, "01 • RẠN TẢO ĐỎ", "zone01-left", MissionObjectiveType.Photograph, PhotoOneObjective, "Z1_Creature_01",
                    Reward(MissionRewardType.ResearchData, PressureData)),
                Location(LocationTwo, "02 • RÃNH SAN HÔ CỔ", "zone01-east", MissionObjectiveType.Collect, BlueprintObjective, EmmaBlueprint,
                    Reward(MissionRewardType.Item, EmmaBlueprint)),
                Location(LocationThree, "03 • THỀM BIỂN SÂU", "zone01-north", MissionObjectiveType.Photograph, PhotoTwoObjective, "Z1_Creature_02",
                    Reward(MissionRewardType.UnlockRecipe, PressureHullRecipe))
            };
            result.zoneGate = new ZoneGateConfig
            {
                id = "Z1_GATE",
                objectives = new[] { new MissionObjectiveConfig { id = "Z1_GATE_INSTALL_PRESSURE_HULL", type = MissionObjectiveType.InstallUpgrade, targetId = PressureHullUpgrade } },
                rewards = new[] { Reward(MissionRewardType.UnlockZone, "Zone02") }
            };
            return result;
        }

        private static MissionLocationConfig Location(string id, string name, string poiId, MissionObjectiveType type, string objectiveId, string targetId, MissionRewardConfig reward)
            => new() { id = id, displayName = name, poiId = poiId, visibility = LocationVisibility.Visible,
                objectives = new[] { new MissionObjectiveConfig { id = objectiveId, type = type, targetId = targetId } }, rewards = new[] { reward } };
        private static MissionRewardConfig Reward(MissionRewardType type, string targetId) => new() { type = type, targetId = targetId };
    }
}

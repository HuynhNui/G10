using System;
using System.Collections.Generic;
using System.Text;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Missions
{
    /// <summary>Cabin interaction adapter; all mission and material requirements come from config.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneOneStory : ZoneMissionRuntime
    {
        // Stable numeric values and removed IDs are retained only for old checkpoints/callers.
        [Flags] public enum Progress
        {
            None = 0, RadarOne = 1, PhotoOne = 2, Analysis = 4, RadarTwo = 8,
            Tube = 16, Recipe = 32, PhotoTwo = 64, Installed = 256
        }
        public const string LocationOne = "Z1_L1", LocationTwo = "Z1_L2", LocationThree = "Z1_L3";
        public const string PhotoOneObjective = "Z1_L1_PHOTO", PhotoTwoObjective = "Z1_L2_PHOTO";
        public const string CaptureOneObjective = "Z1_L1_CAPTURE", LampInstallObjective = "Z1_GATE_INSTALL_BIO_LAMP";
        public const string BlueprintObjective = "Z1_L2_COLLECT", PressureData = "Z1_PRESSURE_DATA";
        public const string EmmaBlueprint = "Z1_ITEM_EMMA_BLUEPRINT", PressureHullRecipe = "RECIPE_PRESSURE_HULL";
        public const string PressureHullUpgrade = "UPGRADE_PRESSURE_HULL", LampRecipe = "RECIPE_BIO_LAMP";
        // Presentation adapters retain their existing API; mission/config data owns every cost.
        public const string AlgaeCaptureObjective = CaptureOneObjective, LuminescentMaterial = "Z1_Creature_01";
        public const string BioLampRecipe = LampRecipe, BioLampUpgrade = "UPGRADE_BIO_LAMP", BioLampObjective = LampInstallObjective;
        public const string BioLampOnline = "BIO_LAMP_ONLINE", ZoneTwoHullObjective = "Z2_GATE_INSTALL_PRESSURE_HULL";
        public bool UsesBioLampStory => config?.zoneId == "Zone01" && config.zoneGate?.objectives != null &&
            Array.Exists(config.zoneGate.objectives, objective => objective?.id == BioLampObjective);
        public bool BioLampInstalled => HasObjective(BioLampObjective) || HasWorldFlag(BioLampOnline);
        public bool CanInstallBioLamp => UsesBioLampStory && PendingGateObjective?.id == BioLampObjective && CanApplyProgressionAction;
        public bool UsesZoneTwoHullStory => config?.zoneId == "Zone02" && config.zoneGate?.objectives != null &&
            Array.Exists(config.zoneGate.objectives, objective => objective?.id == ZoneTwoHullObjective);
        public bool ZoneTwoHullInstalled => UsesZoneTwoHullStory && HasObjective(ZoneTwoHullObjective);
        public bool CanInstallZoneTwoHull => UsesZoneTwoHullStory && PendingGateObjective?.id == ZoneTwoHullObjective && CanApplyProgressionAction;
        public bool UsesZoneThreeRockStory => config?.zoneId == "Zone03" && FindObjective("zone03-l4", MissionObjectiveType.Capture)?.id == "Z3_L3_CAPTURE";
        public bool RockBreakerInstalled => UsesZoneThreeRockStory && HasObjective("Z3_GATE_INSTALL_ROCK_BREAKER");
        public event Action<MissionObjectiveType> ZoneThreeActionCommitted;

        public CabinStationView cabin;
        public SurveyContentDefinition creatureOne, creatureTwo, emmaTube;
        public MapPoi RockInteractionArea { get; set; }
        [SerializeField, Min(0)] private float rockDepthTolerance = 10f;
        public float RockDepthTolerance => Mathf.Max(0, rockDepthTolerance);
        public string[] poiIds = Array.Empty<string>();
        [Min(1)] public float upgradedMaximumDepth = 750;
        [HideInInspector] public float hullBonus = 25;
        [SerializeField, HideInInspector] private int migratedLegacyBits;
        public int SavedProgress => 0; // Version 6 saves use only ID-based state.
        public bool CanInstall => PendingGateObjective?.targetId == PressureHullUpgrade && CanApplyProgressionAction;
        private bool Blocked => navigation == null || navigation.ExpeditionBlocked ||
            cabin != null && cabin.Panels != null && cabin.Panels.IsModalOpen;

        public MissionObjectiveConfig PendingGateObjective
        {
            get
            {
                foreach (var objective in config?.zoneGate?.objectives ?? Array.Empty<MissionObjectiveConfig>())
                    if (objective != null && objective.required && !HasObjective(objective.id)) return objective;
                return null;
            }
        }
        private bool GateFieldworkComplete => MainLocationsComplete;
        public bool CanApplyProgressionAction
        {
            get
            {
                var objective = PendingGateObjective;
                if (Blocked || !GateFieldworkComplete || !PrerequisitesMet(objective) || !HasMaterialsAndRecipe(objective)) return false;
                if (objective.type == MissionObjectiveType.DestroyObstacle)
                    return RockInteractionArea != null && survey != null && RockInteractionArea.Contains(navigation.Position) &&
                        Mathf.Abs(navigation.Depth - survey.DepthFor(RockInteractionArea)) <= RockDepthTolerance;
                return objective.type == MissionObjectiveType.Craft || objective.type == MissionObjectiveType.InstallUpgrade;
            }
        }
        private bool HasMaterialsAndRecipe(MissionObjectiveConfig objective)
        {
            if (!string.IsNullOrEmpty(objective.requiredRecipeId) && !HasRecipe(objective.requiredRecipeId)) return false;
            if (!TryCosts(objective, out var costs)) return false;
            foreach (var cost in costs)
                if (inventory == null || inventory.GetCount(cost.Key) < cost.Value) return false;
            return true;
        }
        private static bool TryCosts(MissionObjectiveConfig objective, out Dictionary<string, int> costs)
        {
            costs = new Dictionary<string, int>();
            foreach (var cost in objective.materials ?? Array.Empty<MissionMaterialCost>())
            {
                if (cost == null || string.IsNullOrEmpty(cost.itemId) || cost.amount < 1) return false;
                long total = (long)(costs.TryGetValue(cost.itemId, out int amount) ? amount : 0) + cost.amount;
                if (total > int.MaxValue) return false;
                costs[cost.itemId] = (int)total;
            }
            return true;
        }
        public string ProgressionActionTitle
        {
            get
            {
                var objective = PendingGateObjective;
                if (objective == null) return "UPGRADE COMPLETE";
                string name = string.IsNullOrEmpty(objective.displayName) ?
                    (objective.targetId ?? "").Replace("UPGRADE_", "").Replace('_', ' ') : objective.displayName;
                return (objective.type == MissionObjectiveType.Craft ? "CRAFT " :
                    objective.type == MissionObjectiveType.DestroyObstacle ? "BREAK " : "INSTALL ") + name;
            }
        }
        public string ProgressionActionDescription
        {
            get
            {
                var objective = PendingGateObjective;
                if (objective == null)
                {
                    var map = cabin != null ? cabin.GetComponent<CabinZoneSession>()?.ActiveConfig : null;
                    return map?.exitArea != null && !string.IsNullOrEmpty(map.destinationZone) ?
                        $"ROUTE OPEN · {map.destinationZone}\nTravel to ({map.exitArea.mapPosition.x:0}, {map.exitArea.mapPosition.y:0})." : "Main progression complete.";
                }
                var text = new StringBuilder();
                if (!GateFieldworkComplete) text.Append("Complete all required PHOTO (SEND) and CAPTURE fieldwork.\n");
                if (!string.IsNullOrEmpty(objective.requiredRecipeId))
                    text.Append(HasRecipe(objective.requiredRecipeId) ? "[x] " : "[ ] ").Append(objective.requiredRecipeId).Append('\n');
                foreach (var cost in objective.materials ?? Array.Empty<MissionMaterialCost>())
                    if (cost != null) text.Append(cost.displayName).Append(" · ").Append(cost.itemId).Append(": ")
                        .Append(inventory?.GetCount(cost.itemId) ?? 0).Append(" / ").Append(cost.amount).Append('\n');
                if (objective.minimumMaximumDepth > 0) text.Append($"Maximum depth → {objective.minimumMaximumDepth:0} m. Hull HP uses regular upgrades.\n");
                if (objective.type == MissionObjectiveType.DestroyObstacle)
                    text.Append(RockInteractionArea == null ? "Rock interaction area missing." :
                        $"Approach rock ({RockInteractionArea.mapPosition.x:0}, {RockInteractionArea.mapPosition.y:0}), DEPTH {survey?.DepthFor(RockInteractionArea):0} m (±{RockDepthTolerance:0} m), then BREAK ROCK BARRIER.");
                else text.Append(objective.type == MissionObjectiveType.Craft ?
                    "Assemble explosives and detonator from both captured materials; then install the assembly." :
                    "Install this expedition module. Materials are consumed once when installation succeeds.");
                return text.ToString();
            }
        }
        protected override void Awake()
        {
            if (contentCatalog == null || contentCatalog.Length == 0) contentCatalog = new[] { creatureOne, creatureTwo, emmaTube };
            base.Awake();
            if (survey != null) survey.Story = this;
        }
        public bool Has(Progress flag)
        {
            if (flag == Progress.None) return true;
            int value = migratedLegacyBits;
            if (HasObjective(PhotoOneObjective)) value |= (int)Progress.PhotoOne;
            if (HasObjective(PhotoTwoObjective)) value |= (int)Progress.PhotoTwo;
            if (HasObjective(LampInstallObjective)) value |= (int)Progress.Installed;
            if (HasRecipe(LampRecipe)) value |= (int)Progress.Recipe;
            return (value & (int)flag) == (int)flag;
        }
        public static int CountProgress(int legacyValue)
        {
            int value = legacyValue & 347, count = 0;
            while (value != 0) { count += value & 1; value >>= 1; }
            return count;
        }
        public void Restore(int legacyValue)
        {
            migratedLegacyBits = legacyValue & 511;
            var state = new MissionProgressState();
            if (config?.zoneId == "Zone01")
            {
                if ((legacyValue & (int)Progress.PhotoOne) != 0) state.completedObjectives.Add(PhotoOneObjective);
                if ((legacyValue & (int)Progress.PhotoTwo) != 0) state.completedObjectives.Add(PhotoTwoObjective);
                if ((legacyValue & 6) != 0) state.researchData.Add(PressureData);
                if ((legacyValue & 96) != 0) state.unlockedRecipes.Add(PressureHullRecipe);
                if ((legacyValue & (int)Progress.Installed) != 0)
                {
                    state.worldFlags.Add("LEGACY_ROUTE_UNLOCKED_Zone01");
                    state.worldFlags.Add(MissionLayoutMigration.LegacyPressureHullFlag);
                }
            }
            // Restore is never a reward transaction: no cargo or new capture credit is manufactured.
            RestoreProgress(state);
        }
        public void RecordRadar(string poiId)
        {
            if (Blocked || string.IsNullOrEmpty(poiId)) return;
            if (poiIds.Length > 0 && poiId == poiIds[0]) migratedLegacyBits |= (int)Progress.RadarOne;
            if (poiIds.Length > 1 && poiId == poiIds[1]) migratedLegacyBits |= (int)Progress.RadarTwo;
            RevealPoi(poiId); NotifyChanged();
        }
        public void RecordPhoto(string poiId, string targetId = null)
        {
            if (!Blocked) RecordObjective(poiId, MissionObjectiveType.Photograph, targetId);
        }
        public CreatureCatcher.Result? ValidateCollection(MapPoi poi, float depthTolerance, bool requireCharge = true)
        {
            if (Blocked || inventory == null || survey == null || navigation.Ship.Hull <= 0) return CreatureCatcher.Result.Unavailable;
            var spatial = CreatureCatcher.ValidateSpatial(survey, navigation, out var contact);
            if (spatial.HasValue) return spatial;
            if (poi == null || contact != poi) return CreatureCatcher.Result.NoTarget;
            var objective = FindObjective(poi.id, MissionObjectiveType.Collect) ?? FindObjective(poi.id, MissionObjectiveType.Capture);
            if (objective == null || HasObjective(objective.id) && !IsRepeatableCapture(objective) || !IsContentPresent(poi.id)) return CreatureCatcher.Result.NoTarget;
            if (!PrerequisitesMet(objective)) return CreatureCatcher.Result.PhotoRequired;
            var photo = FindObjective(poi.id, MissionObjectiveType.Photograph, objective.targetId);
            if (objective.type == MissionObjectiveType.Capture && photo != null && photo.required && !HasObjective(photo.id)) return CreatureCatcher.Result.PhotoRequired;
            if (!inventory.CanAdd(objective.targetId)) return CreatureCatcher.Result.Full;
            if (requireCharge && !navigation.Ship.CanAttemptCapture) return CreatureCatcher.Result.NoCharges;
            return null;
        }
        public CreatureCatcher.Result CompleteCollection(MapPoi poi, float depthTolerance)
        {
            var invalid = ValidateCollection(poi, depthTolerance, false);
            if (invalid.HasValue) return invalid.Value;
            var objective = FindObjective(poi.id, MissionObjectiveType.Collect) ?? FindObjective(poi.id, MissionObjectiveType.Capture);
            return ResolveCapture(poi.id, objective) ? CreatureCatcher.Result.Caught : CreatureCatcher.Result.Empty;
        }
        public bool InstallHull() => CanInstall && ApplyProgressionAction();
        public bool InstallBioLamp() => CanInstallBioLamp && ApplyProgressionAction();
        public bool ApplyProgressionAction()
        {
            if (!CanApplyProgressionAction) return false;
            var objective = PendingGateObjective;
            var loop = cabin != null ? cabin.Panels?.Expedition : null;
            bool success = loop != null && loop.IsInitialized ? loop.CommitMissionAction(this, ApplyProgressionCore) : ApplyProgressionCore();
            if (success && objective.type == MissionObjectiveType.DestroyObstacle)
                cabin?.GetComponent<G10.Prototype.Feedback.RockBreakerSequence>()?.BeginCommitted(this);
            if (success && objective.type == MissionObjectiveType.InstallUpgrade)
            {
                if (objective.targetId == BioLampUpgrade) cabin?.GetComponent<G10.Prototype.Feedback.ZoneOneAudioController>()?.OnBioLampCommitted();
                if (UsesZoneTwoHullStory && objective.targetId == PressureHullUpgrade) cabin?.GetComponent<G10.Prototype.Feedback.ZoneTwoPresentationController>()?.OnHullInstalled();
            }
            if (success && config?.zoneId == "Zone03") ZoneThreeActionCommitted?.Invoke(objective.type);
            return success;
        }
        private bool ApplyProgressionCore()
        {
            if (!CanApplyProgressionAction) return false;
            var objective = PendingGateObjective;
            string completedAction = ProgressionActionTitle;
            if (!TryCosts(objective, out var costs) || costs.Count > 0 && (inventory == null || !inventory.TryConsume(costs))) return false;
            if (objective.minimumMaximumDepth > 0)
            {
                var ship = navigation.Ship.Export();
                ship.maximumDepth = Mathf.Max(ship.maximumDepth, objective.minimumMaximumDepth);
                navigation.Ship.Restore(ship);
            }
            if (!RecordGlobalObjective(objective.type, objective.targetId)) return false;
            if (objective.type == MissionObjectiveType.DestroyObstacle)
            {
                Grant(MissionRewardType.SetWorldFlag, "ROCK_BARRIER_DESTROYED", "rock-destroyed");
                var loop = cabin != null ? cabin.Panels?.Expedition : null;
                if (loop?.CurrentProgress != null) loop.CurrentProgress.rockDestroyed = true;
            }
            LastMessage = objective.type == MissionObjectiveType.DestroyObstacle ?
                "Rock barrier removed. Travel through the opened route to the exit." : $"Completed: {completedAction}.";
            NotifyChanged(); return true;
        }
        public string MapLocationText(int index) => index >= 0 && index < poiIds.Length ? LocationText(poiIds[index]) : string.Empty;
        public bool IsLocationComplete(int index) => index >= 0 && index < poiIds.Length && IsPoiComplete(poiIds[index]);

        public override bool IsContentPresent(string poiId)
        {
            // A captured algae sample does not remove the surrounding singing colony.
            if (UsesBioLampStory && FindObjective(poiId, MissionObjectiveType.Capture)?.id == AlgaeCaptureObjective)
                return IsPoiAvailable(poiId);
            return base.IsContentPresent(poiId);
        }
        public override string MissionText()
        {
            if (!UsesBioLampStory) return base.MissionText();
            int species = (HasObjective(PhotoOneObjective) ? 1 : 0) + (HasObjective(PhotoTwoObjective) ? 1 : 0);
            return $"BIOLOGICAL SURVEY\nSPECIES {species:00} / 09\n" +
                (BioLampInstalled ? "EXTERNAL ILLUMINATION: ONLINE\n" : "REPAIR REQUIRED: EXTERNAL ILLUMINATION\n") + base.MissionText();
        }
    }
}

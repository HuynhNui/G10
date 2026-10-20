using System;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.Tutorial;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>Developer-only provisioning through the same objective/reward authority as real play.</summary>
    public static class DebugExpeditionPreset
    {
        public static ExpeditionSave Create(int startingZone, ZoneMapConfig[] maps, SurveyContentDefinition[] catalog,
            ShipState initialShip, bool regularMax = false)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (startingZone is < 1 or > 4 || maps == null || maps.Length != 4 || initialShip?.IsValid != true)
                throw new ArgumentException("Invalid Debug Start configuration.");
            var save = new ExpeditionSave();
            save.current.ship = initialShip.Copy(); save.current.hasShipState = true;
            save.current.zone = $"Zone0{startingZone}";
            save.current.upgrades.baseSpeed = initialShip.speed;
            if (startingZone > 1) save.tutorial = new TutorialProgressState { completed = true };
            var owner = new GameObject("DebugPresetProvisioning") { hideFlags = HideFlags.HideAndDontSave };
            owner.SetActive(false);
            try
            {
                var inventory = owner.AddComponent<CreatureInventory>();
                var runtime = owner.AddComponent<ZoneMissionRuntime>();
                runtime.inventory = inventory; runtime.contentCatalog = catalog;
                MissionProgressState shared = new();
                for (int index = 0; index < startingZone; index++)
                {
                    var map = maps[index];
                    if (map == null || map.missionConfig == null || map.zoneId != $"Zone0{index + 1}")
                        throw new ArgumentException("Debug map order must be Zone01–04.");
                    runtime.config = map.missionConfig;
                    // Only ship-wide unlocks cross zone boundaries; completion/reward IDs stay local.
                    var progress = new MissionProgressState {
                        researchData = new(shared.researchData), collectedItems = new(shared.collectedItems),
                        unlockedRecipes = new(shared.unlockedRecipes), worldFlags = new(shared.worldFlags),
                        unlockedZones = new(shared.unlockedZones) };
                    runtime.RestoreProgress(progress);
                    bool prior = index + 1 < startingZone;
                    if (prior)
                    {
                        foreach (var location in runtime.config.locations)
                        {
                            if (location.visibility != LocationVisibility.Visible) continue;
                            foreach (var objective in location.objectives)
                                if (objective.required)
                                {
                                    if (objective.type == MissionObjectiveType.Capture) runtime.ResolveCapture(location.poiId, objective);
                                    else runtime.RecordObjective(location.poiId, objective.type, objective.targetId);
                                }
                        }
                        foreach (var objective in runtime.config.zoneGate?.objectives ?? Array.Empty<MissionObjectiveConfig>())
                        {
                            if (!objective.required) continue;
                            var costs = new System.Collections.Generic.Dictionary<string, int>();
                            foreach (var cost in objective.materials)
                                costs[cost.itemId] = (costs.TryGetValue(cost.itemId, out int count) ? count : 0) + cost.amount;
                            if (!inventory.TryConsume(costs)) throw new InvalidOperationException("Debug module material provisioning failed.");
                            runtime.RecordGlobalObjective(objective.type, objective.targetId);
                            if (objective.type == MissionObjectiveType.InstallUpgrade && objective.targetId == ZoneOneStory.PressureHullUpgrade)
                                save.current.ship.maximumDepth = Mathf.Max(save.current.ship.maximumDepth, objective.minimumMaximumDepth);
                            if (objective.type == MissionObjectiveType.DestroyObstacle && !runtime.ProgressState.worldFlags.Contains("ROCK_BARRIER_DESTROYED"))
                            { runtime.ProgressState.worldFlags.Add("ROCK_BARRIER_DESTROYED"); runtime.ProgressState.grantedRewards.Add("rock-destroyed"); }
                        }
                    }
                    shared = runtime.ExportProgress();
                    save.current.zones.Add(new ExpeditionZoneState { zone = map.zoneId, deadline = 25,
                        mapCoordinateVersion = ZoneMapConfig.CurrentCoordinateVersion, hasVoyage = true, creaturePresent = true,
                        position = map.entryPosition, heading = map.entryHeading,
                        depth = Mathf.Clamp(map.entryDepth, map.minimumDepth, save.current.ship.maximumDepth),
                        missionProgress = shared.Copy(), completedDay = prior ? 1 : 0,
                        progress = new ZoneProgressState { mainObjectivesComplete = prior && runtime.MainObjectivesComplete,
                            exitUnlocked = prior && !string.IsNullOrEmpty(map.destinationZone),
                            rockDestroyed = map.UsesAlternate(runtime) } });
                }
                foreach (var item in inventory.Items)
                    save.current.inventory.Add(new SavedCreature { id = item.Id, name = item.Name, quantity = item.Quantity });
                if (regularMax)
                    foreach (var branch in new[] { ShipUpgrade.Hull, ShipUpgrade.Speed, ShipUpgrade.Energy })
                    {
                        save.current.upgrades.SetLevel(branch, 2);
                        RegularShipUpgradeRules.TryApply(save.current.ship, branch, 2, initialShip.speed);
                    }
                foreach (var zone in save.current.zones) save.current.dayStartTasks += zone.missionProgress.completedObjectives.Count;
                save.dayStart = ExpeditionSaveStore.Copy(save.current); save.hasDayStart = true;
                return save;
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
#else
            throw new InvalidOperationException("Debug Start is unavailable in Release builds.");
#endif
        }
    }
}

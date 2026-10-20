using System;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>v5 -> v6 maps by the original POI, never by the reused display number.</summary>
    public static class MissionLayoutMigration
    {
        public const string LegacyPressureHullFlag = "LEGACY_PRESSURE_HULL_INSTALLED";
        public static void Apply(ExpeditionSnapshot snapshot)
        {
            if (snapshot?.zones == null) return;
            foreach (var zone in snapshot.zones)
            {
                if (zone == null) continue;
                if (zone.zoneOneStoryProgress < 0 || zone.zoneOneStoryProgress > 511)
                    throw new InvalidDataException("Invalid legacy mission bits.");
                var progress = zone.missionProgress;
                zone.legacyMissionProgressJson = JsonUtility.ToJson(zone);
                if (zone.zone == "Zone01" && progress.IsEmpty)
                {
                    int bits = zone.zoneOneStoryProgress;
                    if ((bits & 2) != 0 || Array.IndexOf(zone.tasks, PhotoSurveyZone.TaskKind.Photograph) >= 0)
                        progress.completedObjectives.Add("Z1_L1_PHOTO");
                    if ((bits & 64) != 0) progress.completedObjectives.Add("Z1_L3_PHOTO");
                    if ((bits & 256) != 0) progress.completedObjectives.Add("Z1_GATE_INSTALL_PRESSURE_HULL");
                    if ((bits & 6) != 0) progress.researchData.Add(ZoneOneStory.PressureData);
                    if ((bits & 96) != 0) progress.unlockedRecipes.Add(ZoneOneStory.PressureHullRecipe);
                }
                if (zone.zone == "Zone01" && progress.completedObjectives.Contains("Z1_GATE_INSTALL_PRESSURE_HULL"))
                    progress.worldFlags.Add(LegacyPressureHullFlag);
                bool routeEarned = zone.progress.exitUnlocked || zone.zone == "Zone01" && progress.completedObjectives.Contains("Z1_GATE_INSTALL_PRESSURE_HULL") ||
                    zone.zone == "Zone02" && progress.completedObjectives.Contains("Z2_GATE_INSTALL_BIO_LAMP");
                if (routeEarned && zone.zone != "Zone04")
                    progress.worldFlags.Add("LEGACY_ROUTE_UNLOCKED_" + zone.zone);
                Remap(progress.completedObjectives, zone.zone, false);
                Remap(progress.revealedLocations, zone.zone, true);
                // Old location receipts refer to a different reward layout; retain them in the archive.
                progress.grantedRewards.RemoveAll(id => id.StartsWith("location:", StringComparison.Ordinal));
                zone.creatureSpawns.RemoveAll(spawn => spawn != null && RemovedPoi(zone.zone, spawn.poiId));
                zone.tasks = Array.Empty<PhotoSurveyZone.TaskKind>();
                zone.zoneOneStoryProgress = 0;
                if (zone.zone == "Zone04" && string.IsNullOrEmpty(snapshot.endingReached))
                {
                    zone.progress.mainObjectivesComplete = false;
                    zone.progress.hiddenRouteComplete = false;
                    progress.endings.Clear();
                    progress.grantedRewards.RemoveAll(id => id.StartsWith("completion:Z4_", StringComparison.Ordinal));
                }
                // Existing valid capture receipts can unlock their new recipe, but never manufacture cargo.
                if (zone.zone == "Zone02" && progress.completedObjectives.Contains("Z2_L2_CAPTURE") && !progress.unlockedRecipes.Contains("RECIPE_PRESSURE_HULL"))
                    progress.unlockedRecipes.Add("RECIPE_PRESSURE_HULL");
                if (zone.zone == "Zone03" && progress.completedObjectives.Contains("Z3_L1_CAPTURE") && !progress.unlockedRecipes.Contains("RECIPE_ROCK_BREAKER"))
                    progress.unlockedRecipes.Add("RECIPE_ROCK_BREAKER");
                progress.Normalize();
            }
            foreach (var photo in snapshot.photos ?? new List<SavedPhoto>())
            {
                if (photo == null || string.IsNullOrEmpty(photo.missionObjectiveId)) continue;
                string mapped = ObjectiveId(photo.missionZoneId, photo.missionObjectiveId);
                if (RemovedPoi(photo.missionZoneId, photo.missionPoiId)) mapped = null;
                if (mapped == photo.missionObjectiveId) continue;
                photo.legacyMissionObjectiveId = photo.missionObjectiveId;
                photo.missionObjectiveId = mapped;
                // Keep submitted historical photos and image bytes; removed POIs cannot be sent again.
            }
        }

        private static void Remap(List<string> values, string zone, bool location)
        {
            for (int i = values.Count - 1; i >= 0; i--)
            {
                string id = location ? LocationId(zone, values[i]) : ObjectiveId(zone, values[i]);
                if (id == null) values.RemoveAt(i); else values[i] = id;
            }
        }
        private static string LocationId(string zone, string id) => (zone, id) switch
        {
            ("Zone01", "Z1_L2") or ("Zone02", "Z2_L2") or ("Zone03", "Z3_L3") or ("Zone04", "Z4_L4") => null,
            ("Zone01", "Z1_L3") => "Z1_L2",
            ("Zone02", "Z2_L3") => "Z2_L2",
            ("Zone03", "Z3_L4") => "Z3_L3",
            ("Zone04", "Z4_L5") => "Z4_L4",
            _ => id
        };
        private static string ObjectiveId(string zone, string id) => (zone, id) switch
        {
            ("Zone01", "Z1_L2_COLLECT") or ("Zone01", "Z1_GATE_INSTALL_PRESSURE_HULL") or
            ("Zone02", "Z2_L2_COLLECT") or ("Zone02", "Z2_GATE_INSTALL_BIO_LAMP") or
            ("Zone03", "Z3_L2_COLLECT") or ("Zone03", "Z3_L3_PHOTO") or
            ("Zone04", "Z4_L4_PHOTO") or ("Zone04", "Z4_L5_COLLECT") => null,
            ("Zone01", "Z1_L3_PHOTO") => "Z1_L2_PHOTO",
            ("Zone02", "Z2_L3_PHOTO") => "Z2_L2_PHOTO",
            ("Zone02", "Z2_L3_CAPTURE") => "Z2_L2_CAPTURE",
            ("Zone03", "Z3_L4_PHOTO") => "Z3_L3_PHOTO",
            _ => id
        };
        private static bool RemovedPoi(string zone, string id) => (zone, id) is
            ("Zone01", "zone01-east") or ("Zone02", "zone02-l2") or ("Zone03", "zone03-l3") or ("Zone04", "zone04-l4");
    }
}

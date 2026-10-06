using System;
using System.Collections.Generic;
using G10.Prototype.Missions;
using UnityEditor;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>Creates stable-ID mission/content assets. Safe to rerun; existing art and descriptions are preserved.</summary>
    public static class MissionConfigEditor
    {
        private const string MissionRoot = "Assets/_Project/Content/Missions";

        [MenuItem("G10/Missions/Build Configs and Placeholders")]
        public static void Build()
        {
            GetOrCreateZoneConfigs();
            CreateContentPlaceholders();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built Zone01–Zone04 mission configs and stable-ID placeholder content.");
        }

        public static ZoneMissionConfig[] GetOrCreateZoneConfigs()
        {
            EnsureFolder(MissionRoot);
            var configs = new[]
            {
                Save("Zone01", Zone01()), Save("Zone02", Zone02()), Save("Zone03", Zone03()), Save("Zone04", Zone04())
            };
            AssetDatabase.SaveAssets();
            return configs;
        }

        private static ZoneMissionConfig Save(string name, ZoneMissionConfig source)
        {
            string path = $"{MissionRoot}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ZoneMissionConfig>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ZoneMissionConfig>();
                AssetDatabase.CreateAsset(asset, path);
            }
            EditorUtility.CopySerialized(source, asset);
            UnityEngine.Object.DestroyImmediate(source);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ZoneMissionConfig Zone01()
        {
            var config = Config("Zone01", "ZONE 1");
            config.locations = new[]
            {
                Location("Z1_L1", "01 • RẠN TẢO ĐỎ", "zone01-left", LocationVisibility.Visible,
                    Objectives(Objective("Z1_L1_PHOTO", MissionObjectiveType.Photograph, "Z1_Creature_01")),
                    Rewards(Reward(MissionRewardType.ResearchData, "Z1_PRESSURE_DATA"))),
                Location("Z1_L2", "02 • RÃNH SAN HÔ CỔ", "zone01-east", LocationVisibility.Visible,
                    Objectives(Objective("Z1_L2_COLLECT", MissionObjectiveType.Collect, "Z1_ITEM_EMMA_BLUEPRINT")),
                    Rewards(Reward(MissionRewardType.Item, "Z1_ITEM_EMMA_BLUEPRINT"))),
                Location("Z1_L3", "03 • THỀM BIỂN SÂU", "zone01-north", LocationVisibility.Visible,
                    Objectives(Objective("Z1_L3_PHOTO", MissionObjectiveType.Photograph, "Z1_Creature_02")),
                    Rewards(Reward(MissionRewardType.UnlockRecipe, "RECIPE_PRESSURE_HULL")))
            };
            config.zoneGate = Gate("Z1_GATE", Objectives(Objective("Z1_GATE_INSTALL_PRESSURE_HULL", MissionObjectiveType.InstallUpgrade, "UPGRADE_PRESSURE_HULL")),
                Rewards(Reward(MissionRewardType.UnlockZone, "Zone02")));
            return config;
        }

        private static ZoneMissionConfig Zone02()
        {
            var config = Config("Zone02", "ZONE 2");
            config.locations = new[]
            {
                Location("Z2_L1", "Location 01", "zone02-l1", LocationVisibility.Visible,
                    Objectives(Objective("Z2_L1_PHOTO", MissionObjectiveType.Photograph, "Z2_Creature_01")),
                    Rewards(Reward(MissionRewardType.ResearchData, "Z2_BIOLUMINESCENCE_DATA"))),
                Location("Z2_L2", "Location 02", "zone02-l2", LocationVisibility.Visible,
                    Objectives(Objective("Z2_L2_COLLECT", MissionObjectiveType.Collect, "Z2_ITEM_BIO_ENERGY_CORE")),
                    Rewards(Reward(MissionRewardType.Item, "Z2_ITEM_BIO_ENERGY_CORE"))),
                Location("Z2_L3", "Location 03", "zone02-l3", LocationVisibility.Visible,
                    Objectives(Objective("Z2_L3_PHOTO", MissionObjectiveType.Photograph, "Z2_Creature_02"), Objective("Z2_L3_CAPTURE", MissionObjectiveType.Capture, "Z2_Creature_02")),
                    Rewards(Reward(MissionRewardType.Item, "Z2_Creature_02"), Reward(MissionRewardType.ResearchData, "Z2_ADVANCED_LIGHT_DATA"), Reward(MissionRewardType.UnlockRecipe, "RECIPE_BIO_LAMP")))
            };
            config.zoneGate = Gate("Z2_GATE", Objectives(Objective("Z2_GATE_INSTALL_BIO_LAMP", MissionObjectiveType.InstallUpgrade, "UPGRADE_BIO_LAMP")),
                Rewards(Reward(MissionRewardType.UnlockZone, "Zone03")));
            return config;
        }

        private static ZoneMissionConfig Zone03()
        {
            var config = Config("Zone03", "ZONE 3");
            config.locations = new[]
            {
                Location("Z3_L1", "Location 01", "zone03-l1", LocationVisibility.Visible,
                    Objectives(Objective("Z3_L1_PHOTO", MissionObjectiveType.Photograph, "Z3_Creature_01"), Objective("Z3_L1_CAPTURE", MissionObjectiveType.Capture, "Z3_Creature_01")),
                    Rewards(Reward(MissionRewardType.Item, "Z3_Creature_01"), Reward(MissionRewardType.ResearchData, "Z3_IMPACT_RESISTANCE_DATA"))),
                Location("Z3_L2", "Location 02", "zone03-l2", LocationVisibility.Visible,
                    Objectives(Objective("Z3_L2_COLLECT", MissionObjectiveType.Collect, "Z3_ITEM_MECHANICAL_MODULE")),
                    Rewards(Reward(MissionRewardType.Item, "Z3_ITEM_MECHANICAL_MODULE"), Reward(MissionRewardType.SetWorldFlag, "ROCK_BARRIER_DISCOVERED"))),
                Location("Z3_L3", "Location 03", "zone03-l3", LocationVisibility.Visible,
                    Objectives(Objective("Z3_L3_PHOTO", MissionObjectiveType.Photograph, "Z3_Creature_02")),
                    Rewards(Reward(MissionRewardType.ResearchData, "Z3_SHOCKWAVE_DATA"), Reward(MissionRewardType.UnlockRecipe, "RECIPE_ROCK_BREAKER"))),
                Location("Z3_L4", "Location 04", "zone03-l4", LocationVisibility.Visible,
                    Objectives(Objective("Z3_L4_PHOTO", MissionObjectiveType.Photograph, "Z3_Creature_03")), Array.Empty<MissionRewardConfig>())
            };
            config.zoneGate = Gate("Z3_GATE", Objectives(
                    Objective("Z3_GATE_CRAFT_ROCK_BREAKER", MissionObjectiveType.Craft, "UPGRADE_ROCK_BREAKER"),
                    Objective("Z3_GATE_INSTALL_ROCK_BREAKER", MissionObjectiveType.InstallUpgrade, "UPGRADE_ROCK_BREAKER"),
                    Objective("Z3_GATE_DESTROY_ROCK_BARRIER", MissionObjectiveType.DestroyObstacle, "ROCK_BARRIER")),
                Rewards(Reward(MissionRewardType.UnlockZone, "Zone04")));
            return config;
        }

        private static ZoneMissionConfig Zone04()
        {
            var config = Config("Zone04", "ZONE 4");
            config.locations = new[]
            {
                PhotoLocation("Z4_L1", "Location 01", "zone04-l1", LocationVisibility.Visible, "Z4_L1_PHOTO", "Z4_Creature_01"),
                PhotoLocation("Z4_L2", "Location 02", "zone04-l2", LocationVisibility.Visible, "Z4_L2_PHOTO", "Z4_Creature_02"),
                PhotoLocation("Z4_L3", "Location 03", "zone04-l3", LocationVisibility.HiddenRadar, "Z4_L3_PHOTO", "Z4_Creature_03"),
                PhotoLocation("Z4_L4", "Location 04", "zone04-l4", LocationVisibility.HiddenRadar, "Z4_L4_PHOTO", "Z4_Creature_04"),
                Location("Z4_L5", "Location 05", "zone04-l5", LocationVisibility.HiddenRadar,
                    Objectives(Objective("Z4_L5_COLLECT", MissionObjectiveType.Collect, "Z4_ITEM_HIDDEN_01")),
                    Rewards(Reward(MissionRewardType.Item, "Z4_ITEM_HIDDEN_01")))
            };
            config.zoneGate = new ZoneGateConfig { id = "Z4_ENDINGS" };
            config.completions = new[]
            {
                Completion("Z4_NORMAL_ENDING", new[] { "Z4_L1_PHOTO", "Z4_L2_PHOTO" }, Reward(MissionRewardType.Ending, "NORMAL_ENDING")),
                Completion("Z4_HIDDEN_ENDING", new[] { "Z4_L3_PHOTO", "Z4_L4_PHOTO", "Z4_L5_COLLECT" }, Reward(MissionRewardType.Ending, "HIDDEN_ENDING"))
            };
            return config;
        }

        private static void CreateContentPlaceholders()
        {
            var entries = new Dictionary<string, string>
            {
                ["Z1_Creature_01"]="Sinh vật Zone 1 - 01", ["Z1_Creature_02"]="Sinh vật Zone 1 - 02", ["Z1_ITEM_EMMA_BLUEPRINT"]="Bản vẽ của Emma",
                ["Z2_Creature_01"]="Sinh vật Zone 2 - 01", ["Z2_Creature_02"]="Sinh vật Zone 2 - 02", ["Z2_ITEM_BIO_ENERGY_CORE"]="Bio Energy Core",
                ["Z3_Creature_01"]="Sinh vật Zone 3 - 01", ["Z3_Creature_02"]="Sinh vật Zone 3 - 02", ["Z3_Creature_03"]="Sinh vật Zone 3 - 03", ["Z3_ITEM_MECHANICAL_MODULE"]="Mechanical Module",
                ["Z4_Creature_01"]="Sinh vật Zone 4 - 01", ["Z4_Creature_02"]="Sinh vật Zone 4 - 02", ["Z4_Creature_03"]="Sinh vật Zone 4 - 03", ["Z4_Creature_04"]="Sinh vật Zone 4 - 04", ["Z4_ITEM_HIDDEN_01"]="Hidden Item"
            };
            foreach (var pair in entries)
            {
                string zone = pair.Key.Substring(0, 2).Replace("Z", "Zone0");
                string existingZoneOnePath = pair.Key == "Z1_Creature_01" ? "Assets/_Project/Content/Zone01/Creatures/001/Definition.asset" :
                    pair.Key == "Z1_Creature_02" ? "Assets/_Project/Content/Zone01/Creatures/002/Definition.asset" :
                    pair.Key == "Z1_ITEM_EMMA_BLUEPRINT" ? "Assets/_Project/Content/Zone01/Items/001_EmmaTube/Definition.asset" : null;
                if (existingZoneOnePath != null)
                {
                    var existing = AssetDatabase.LoadAssetAtPath<SurveyContentDefinition>(existingZoneOnePath);
                    if (existing != null) { existing.id = pair.Key; EditorUtility.SetDirty(existing); }
                    continue;
                }
                string folder = $"Assets/_Project/Content/{zone}/Definitions";
                EnsureFolder(folder);
                string path = $"{folder}/{pair.Key}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<SurveyContentDefinition>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<SurveyContentDefinition>();
                    asset.id = pair.Key; asset.displayName = pair.Value; asset.placeholderArt = true;
                    AssetDatabase.CreateAsset(asset, path);
                }
            }
        }

        private static ZoneMissionConfig Config(string id, string name)
        { var value = ScriptableObject.CreateInstance<ZoneMissionConfig>(); value.zoneId = id; value.displayName = name; return value; }
        private static MissionLocationConfig PhotoLocation(string id, string name, string poi, LocationVisibility visibility, string objectiveId, string target)
            => Location(id, name, poi, visibility, Objectives(Objective(objectiveId, MissionObjectiveType.Photograph, target)), Array.Empty<MissionRewardConfig>());
        private static MissionLocationConfig Location(string id, string name, string poi, LocationVisibility visibility, MissionObjectiveConfig[] objectives, MissionRewardConfig[] rewards)
            => new() { id=id, displayName=name, poiId=poi, visibility=visibility, objectives=objectives, rewards=rewards };
        private static MissionObjectiveConfig Objective(string id, MissionObjectiveType type, string target)
            => new() { id=id, type=type, targetId=target, required=true,
                repeatableCapture = type == MissionObjectiveType.Capture && (id == "Z2_L3_CAPTURE" || id == "Z3_L1_CAPTURE") };
        private static MissionRewardConfig Reward(MissionRewardType type, string target) => new() { type=type, targetId=target };
        private static MissionObjectiveConfig[] Objectives(params MissionObjectiveConfig[] values) => values;
        private static MissionRewardConfig[] Rewards(params MissionRewardConfig[] values) => values;
        private static ZoneGateConfig Gate(string id, MissionObjectiveConfig[] objectives, MissionRewardConfig[] rewards)
            => new() { id=id, objectives=objectives, rewards=rewards };
        private static MissionCompletionConfig Completion(string id, string[] requirements, params MissionRewardConfig[] rewards)
            => new() { id=id, requiredObjectiveIds=requirements, rewards=rewards };

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}

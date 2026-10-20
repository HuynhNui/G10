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
            config.locations = new[] {
                SurveyLocation("Z1_L1", "01 • RẠN TẢO ĐỎ", "zone01-left", "Z1_Creature_01", true, "RECIPE_BIO_LAMP"),
                SurveyLocation("Z1_L2", "02 • THỀM BIỂN SÂU", "zone01-north", "Z1_Creature_02")
            };
            config.zoneGate = Gate("Z1_GATE", Objectives(Upgrade("Z1_GATE_INSTALL_BIO_LAMP", "UPGRADE_BIO_LAMP",
                "BIO LAMP", "RECIPE_BIO_LAMP", Cost("Z1_Creature_01", "Lamp material"))),
                Rewards(Reward(MissionRewardType.UnlockZone, "Zone02")));
            return config;
        }

        private static ZoneMissionConfig Zone02()
        {
            var config = Config("Zone02", "ZONE 2");
            config.locations = new[] {
                SurveyLocation("Z2_L1", "Location 01", "zone02-l1", "Z2_Creature_01", true),
                SurveyLocation("Z2_L2", "Location 02", "zone02-l3", "Z2_Creature_02", true, "RECIPE_PRESSURE_HULL", true)
            };
            var hull = Upgrade("Z2_GATE_INSTALL_PRESSURE_HULL", "UPGRADE_PRESSURE_HULL", "PRESSURE HULL",
                "RECIPE_PRESSURE_HULL", Cost("Z2_Creature_01", "Hull material A"), Cost("Z2_Creature_02", "Hull material B"));
            hull.minimumMaximumDepth = 750;
            config.zoneGate = Gate("Z2_GATE", Objectives(hull), Rewards(Reward(MissionRewardType.UnlockZone, "Zone03")));
            return config;
        }

        private static ZoneMissionConfig Zone03()
        {
            var config = Config("Zone03", "ZONE 3");
            config.locations = new[] {
                SurveyLocation("Z3_L1", "Location 01", "zone03-l1", "Z3_Creature_01", true, "RECIPE_ROCK_BREAKER", true),
                SurveyLocation("Z3_L2", "Location 02", "zone03-l2", "Z3_Creature_02"),
                SurveyLocation("Z3_L3", "Location 03", "zone03-l4", "Z3_Creature_03", true)
            };
            var craft = Upgrade("Z3_GATE_CRAFT_ROCK_BREAKER", "UPGRADE_ROCK_BREAKER", "EXPLOSIVES + DETONATOR",
                "RECIPE_ROCK_BREAKER", Cost("Z3_Creature_01", "Explosive material"), Cost("Z3_Creature_03", "Detonator material"));
            craft.type = MissionObjectiveType.Craft;
            var install = Objective("Z3_GATE_INSTALL_ROCK_BREAKER", MissionObjectiveType.InstallUpgrade, "UPGRADE_ROCK_BREAKER");
            install.displayName = "EXPLOSIVES + DETONATOR";
            config.zoneGate = Gate("Z3_GATE", Objectives(craft, install,
                Objective("Z3_GATE_DESTROY_ROCK_BARRIER", MissionObjectiveType.DestroyObstacle, "ROCK_BARRIER")),
                Rewards(Reward(MissionRewardType.UnlockZone, "Zone04")));
            return config;
        }

        private static ZoneMissionConfig Zone04()
        {
            var config = Config("Zone04", "ZONE 4");
            config.locations = new[] {
                SurveyLocation("Z4_L1", "Location 01", "zone04-l1", "Z4_Creature_01", true),
                SurveyLocation("Z4_L2", "Location 02", "zone04-l2", "Z4_Creature_02"),
                SurveyLocation("Z4_L3", "Location 03", "zone04-l3", "Z4_Creature_03"),
                SurveyLocation("Z4_L4", "Location 04", "zone04-l5", "Z4_Creature_04")
            };
            config.locations[2].visibility = config.locations[3].visibility = LocationVisibility.HiddenRadar;
            config.hints = new[] { new MissionHintConfig {
                requiredObjectiveId = "Z4_L1_CAPTURE",
                text = "CAPTURE DATA — Hidden signals: Z4L3 (1006.56, 450.51); Z4L4 (358.56, 119.57). Use RADAR to discover each location after choosing to continue exploring." } };
            config.zoneGate = new ZoneGateConfig { id = "Z4_ENDINGS" };
            config.completions = new[] {
                Completion("Z4_NORMAL_ENDING", new[] { "Z4_L1_PHOTO", "Z4_L1_CAPTURE", "Z4_L2_PHOTO" }, Reward(MissionRewardType.Ending, "NORMAL_ENDING")),
                Completion("Z4_HIDDEN_ENDING", new[] { "Z4_L3_PHOTO", "Z4_L4_PHOTO" }, Reward(MissionRewardType.Ending, "HIDDEN_ENDING"))
            };
            return config;
        }

        private static MissionLocationConfig SurveyLocation(string id, string name, string poi, string creature,
            bool capture = false, string recipe = null, bool repeatable = false)
        {
            var photo = Objective(id + "_PHOTO", MissionObjectiveType.Photograph, creature);
            var caught = Objective(id + "_CAPTURE", MissionObjectiveType.Capture, creature);
            caught.repeatableCapture = repeatable;
            if (!string.IsNullOrEmpty(recipe)) caught.rewards = Rewards(Reward(MissionRewardType.UnlockRecipe, recipe));
            return Location(id, name, poi, LocationVisibility.Visible, capture ? Objectives(photo, caught) : Objectives(photo), Array.Empty<MissionRewardConfig>());
        }
        private static MissionMaterialCost Cost(string item, string name) => new() { itemId = item, displayName = name, amount = 1 };
        private static MissionObjectiveConfig Upgrade(string id, string target, string name, string recipe, params MissionMaterialCost[] costs)
            => new() { id = id, targetId = target, type = MissionObjectiveType.InstallUpgrade, displayName = name,
                requiredRecipeId = recipe, materials = costs };

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
            => new() { id=id, type=type, targetId=target, required=true };
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

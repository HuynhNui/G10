using System;
using UnityEngine;

namespace G10.Prototype.Missions
{
    public enum LocationVisibility { Visible, HiddenRadar }

    public enum MissionObjectiveType
    {
        Photograph,
        Capture,
        Collect,
        Research,
        Craft,
        InstallUpgrade,
        DestroyObstacle
    }

    public enum MissionRewardType
    {
        ResearchData,
        Item,
        UnlockRecipe,
        SetWorldFlag,
        RevealLocation,
        UnlockZone,
        Ending
    }

    [Serializable]
    public sealed class MissionObjectiveConfig
    {
        public string id;
        public MissionObjectiveType type;
        public string targetId;
        public bool required = true;
        [Tooltip("Only capture objectives explicitly configured as crafting sources can be repeated.")]
        public bool repeatableCapture;
        [Tooltip("Stable objective IDs which must be complete before this action can succeed.")]
        public string[] prerequisites = Array.Empty<string>();
        public MissionRewardConfig[] rewards = Array.Empty<MissionRewardConfig>();
        public string displayName;
        public string requiredRecipeId;
        public MissionMaterialCost[] materials = Array.Empty<MissionMaterialCost>();
        [Min(0)] public float minimumMaximumDepth;
    }

    [Serializable]
    public sealed class MissionMaterialCost
    {
        public string itemId;
        public string displayName;
        [Min(1)] public int amount = 1;
    }

    [Serializable]
    public sealed class MissionHintConfig
    {
        public string requiredObjectiveId;
        [TextArea] public string text;
    }

    [Serializable]
    public sealed class MissionRewardConfig
    {
        public MissionRewardType type;
        public string targetId;
    }

    [Serializable]
    public sealed class MissionLocationConfig
    {
        public string id;
        public string displayName;
        public LocationVisibility visibility;
        public string poiId;
        public MissionObjectiveConfig[] objectives = Array.Empty<MissionObjectiveConfig>();
        public MissionRewardConfig[] rewards = Array.Empty<MissionRewardConfig>();
    }

    [Serializable]
    public sealed class ZoneGateConfig
    {
        public string id;
        public MissionObjectiveConfig[] objectives = Array.Empty<MissionObjectiveConfig>();
        public MissionRewardConfig[] rewards = Array.Empty<MissionRewardConfig>();
    }

    /// <summary>Optional cross-location completion rule, used by independent normal/hidden endings.</summary>
    [Serializable]
    public sealed class MissionCompletionConfig
    {
        public string id;
        public string[] requiredObjectiveIds = Array.Empty<string>();
        public MissionRewardConfig[] rewards = Array.Empty<MissionRewardConfig>();
    }

    [CreateAssetMenu(menuName = "G10/Missions/Zone Mission Config")]
    public sealed class ZoneMissionConfig : ScriptableObject
    {
        public string zoneId;
        public string displayName;
        public MissionLocationConfig[] locations = Array.Empty<MissionLocationConfig>();
        public ZoneGateConfig zoneGate = new();
        public MissionCompletionConfig[] completions = Array.Empty<MissionCompletionConfig>();
        public MissionHintConfig[] hints = Array.Empty<MissionHintConfig>();

        public MissionLocationConfig FindLocation(string id)
            => Array.Find(locations, location => location != null && location.id == id);

        public MissionLocationConfig FindLocationByPoi(string poiId)
            => Array.Find(locations, location => location != null && !string.IsNullOrEmpty(poiId) && location.poiId == poiId);
    }
}

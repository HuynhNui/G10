using System;
using G10.Prototype.Navigation;
using UnityEngine;

namespace G10.Prototype.Computer
{
    [Serializable]
    public sealed class ShipUpgradeProgress
    {
        public int hullLevel, propulsionLevel, efficiencyLevel;
        public float baseSpeed;
        public bool IsValid => hullLevel is >= 0 and <= 2 && propulsionLevel is >= 0 and <= 2 &&
            efficiencyLevel is >= 0 and <= 2 && float.IsFinite(baseSpeed) && baseSpeed >= 0;
        public int Level(ShipUpgrade branch) => branch switch
        { ShipUpgrade.Hull => hullLevel, ShipUpgrade.Speed => propulsionLevel, ShipUpgrade.Energy => efficiencyLevel, _ => -1 };
        public void SetLevel(ShipUpgrade branch, int value)
        {
            switch (branch)
            {
                case ShipUpgrade.Hull: hullLevel = value; break;
                case ShipUpgrade.Speed: propulsionLevel = value; break;
                case ShipUpgrade.Energy: efficiencyLevel = value; break;
            }
        }
    }

    /// <summary>One source for the three regular recipes and their non-additive target stats.</summary>
    public static class RegularShipUpgradeRules
    {
        public const int MaxLevel = 2;
        public const string TierOneMaterial = "Z2_Creature_02", TierTwoMaterial = "Z3_Creature_01";
        public static bool IsRegular(ShipUpgrade branch) => branch is ShipUpgrade.Hull or ShipUpgrade.Speed or ShipUpgrade.Energy;
        public static string MaterialId(int currentLevel) => currentLevel == 0 ? TierOneMaterial : TierTwoMaterial;
        public static int Cost(ShipUpgrade branch, int currentLevel)
            => !IsRegular(branch) || currentLevel is < 0 or >= MaxLevel ? 0 : currentLevel == 0 && branch == ShipUpgrade.Speed ? 1 : 2;
        public static float Value(ShipUpgrade branch, int level, float baseSpeed) => branch switch
        {
            ShipUpgrade.Hull => 100 + 20 * level,
            ShipUpgrade.Speed => level >= MaxLevel ? 25f : baseSpeed * (1 + .1f * level),
            ShipUpgrade.Energy => 1 - .1f * level,
            _ => 0
        };
        public static bool TryApply(ShipState state, ShipUpgrade branch, int level, float baseSpeed)
        {
            if (state == null || !state.IsValid || !IsRegular(branch) || level is < 1 or > MaxLevel) return false;
            float value = Value(branch, level, baseSpeed);
            switch (branch)
            {
                case ShipUpgrade.Hull: state.hullCapacity = value; state.hull = Mathf.Min(state.hull, value); break;
                case ShipUpgrade.Speed: state.speed = value; break;
                case ShipUpgrade.Energy: state.energyPerSecond = value; break;
            }
            return state.IsValid;
        }
    }
}

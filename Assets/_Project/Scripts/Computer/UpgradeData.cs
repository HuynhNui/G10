using System;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public enum UpgradeCategory
    {
        ShipSystem,
        Module
    }

    [Serializable]
    public sealed class UpgradeComparisonData
    {
        [SerializeField] private string statName;
        [SerializeField] private string currentValue;
        [SerializeField] private string nextValue;

        public string StatName => statName;
        public string CurrentValue => currentValue;
        public string NextValue => nextValue;
    }

    [Serializable]
    public sealed class UpgradeMaterialRequirement
    {
        [SerializeField] private UpgradeMaterialDefinition material;
        [Min(1), SerializeField] private int requiredAmount = 1;

        public UpgradeMaterialDefinition Material => material;
        public int RequiredAmount => Mathf.Max(1, requiredAmount);
    }

    public interface IUpgradeAction
    {
        bool CanApply { get; }
        bool TryApply();
    }

    public interface IUpgradeMaterialInventory
    {
        int GetCount(string materialId);
        bool TryConsume(UpgradeMaterialRequirement[] requirements);
    }
}

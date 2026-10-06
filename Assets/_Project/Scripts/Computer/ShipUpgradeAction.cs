using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>Small bridge to the existing ship authority; contains no presentation logic.</summary>
    [DisallowMultipleComponent]
    public sealed class ShipUpgradeAction : MonoBehaviour, IUpgradeAction
    {
        [SerializeField] private CabinStationView cabin;
        [SerializeField] private ShipUpgrade upgrade;
        // Old serialized amount is intentionally ignored: levels have exact target values.
        [HideInInspector, SerializeField] private float amount = 1;
        private ExpeditionLoop owner;
        private ExpeditionLoop Loop => owner != null ? owner : owner = FindAnyObjectByType<ExpeditionLoop>();
        public ShipUpgrade Branch => upgrade;
        public int Level => Loop?.UpgradeLevel(upgrade) ?? 0;
        public bool IsMax => Level >= RegularShipUpgradeRules.MaxLevel;
        public UpgradeComparisonData[] Comparisons
        {
            get
            {
                var owner = Loop;
                var ship = ResolveCabin()?.Navigation?.Ship;
                if (owner == null || ship == null) return System.Array.Empty<UpgradeComparisonData>();
                float current = upgrade == ShipUpgrade.Hull ? ship.HullCapacity : upgrade == ShipUpgrade.Speed ? ship.Speed : ship.EnergyPerSecond;
                string format = upgrade == ShipUpgrade.Energy ? "0.00" : "0.##";
                string next = IsMax ? "MAX" : RegularShipUpgradeRules.Value(upgrade, Level + 1, owner.BaseMovementSpeed).ToString(format, System.Globalization.CultureInfo.InvariantCulture);
                return new[] { new UpgradeComparisonData(upgrade == ShipUpgrade.Hull ? "Hull" : upgrade == ShipUpgrade.Speed ? "Speed" : "Energy / sec",
                    current.ToString(format, System.Globalization.CultureInfo.InvariantCulture), next) };
            }
        }

        public bool CanApply => ResolveCabin() != null && Loop != null && Loop.CanPurchaseUpgrade(upgrade);

        public bool TryApply()
            => CanApply && Loop.TryPurchaseUpgrade(upgrade);

        private CabinStationView ResolveCabin()
        {
            if (cabin == null) cabin = GetComponentInParent<CabinStationView>(true);
            return cabin;
        }
    }
}

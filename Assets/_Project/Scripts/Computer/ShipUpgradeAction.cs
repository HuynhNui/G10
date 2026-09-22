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
        [Min(0.01f), SerializeField] private float amount = 1;

        public bool CanApply => ResolveCabin() != null && cabin.Navigation != null && amount > 0 && float.IsFinite(amount);

        public bool TryApply()
            => CanApply && cabin.Navigation.Ship.ApplyUpgrade(upgrade, amount);

        private CabinStationView ResolveCabin()
        {
            if (cabin == null) cabin = GetComponentInParent<CabinStationView>(true);
            return cabin;
        }
    }
}

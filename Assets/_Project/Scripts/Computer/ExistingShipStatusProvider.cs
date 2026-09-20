using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public sealed class ExistingShipStatusProvider : MonoBehaviour, IShipStatusProvider
    {
        [SerializeField] private RadarDisplay radar;
        public PhotoCaptureService photoCapture;
        public RadarDisplay Radar => radar;
        public G10.Prototype.Navigation.ZoneNavigation Navigation => photoCapture != null ? photoCapture.navigation : null;
        public ShipStatusSnapshot ReadStatus()
        {
            var ship = Navigation != null ? Navigation.Ship : null;
            return new ShipStatusSnapshot(radar != null, radar != null && radar.IsScanning,
                ship?.Radar, ship?.Energy, ship?.EnergyCapacity, ship == null ? null : Navigation.IsMoving ? ship.EnergyPerSecond : 0,
                photoCapture != null && photoCapture.CameraOnline ? "ONLINE" : "OFFLINE",
                photoCapture != null && photoCapture.GetComponent<G10.Prototype.Navigation.CreatureCatcher>() != null ? "ONLINE" : "NOT INSTALLED",
                ship == null ? "NOT INSTALLED" : ship.CanMove ? "ONLINE" : ship.Hull <= 0 ? "HULL CRITICAL" : "EMPTY",
                ship?.LowResources, ship?.Export());
        }
    }
}

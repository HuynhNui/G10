using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public sealed class ExistingShipStatusProvider : MonoBehaviour, IShipStatusProvider
    {
        [SerializeField] private RadarDisplay radar;
        public PhotoCaptureService photoCapture;
        public RadarDisplay Radar => radar;
        public ShipStatusSnapshot ReadStatus() => new(
            radar != null, radar != null && radar.IsScanning,
            // Existing RadarDisplay has no charge counter. Unknown is not zero.
            null, null, null, null, photoCapture != null && photoCapture.CameraOnline ? "ONLINE" : "OFFLINE",
            photoCapture != null && photoCapture.GetComponent<G10.Prototype.Navigation.CreatureCatcher>() != null ? "ONLINE" : "NOT INSTALLED", "NOT INSTALLED", null);
    }
}

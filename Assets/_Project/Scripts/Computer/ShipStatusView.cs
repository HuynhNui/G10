using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    public sealed class ShipStatusView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerSource;
        [SerializeField] private Text body;
        private IShipStatusProvider provider;
        private float refreshAt;
        public string DisplayedText => body.text;
        public ExpeditionLoop Expedition { get; set; }
        private void Awake() => provider = providerSource as IShipStatusProvider;
        private void OnEnable() => Refresh();
        private void Update() { if (Time.unscaledTime >= refreshAt) Refresh(); }
        public void Refresh()
        {
            refreshAt = Time.unscaledTime + 0.25f;
            if (provider == null) { body.text = "TELEMETRY OFFLINE"; return; }
            ShipStatusSnapshot status = provider.ReadStatus();
            string radarState = !status.RadarInstalled ? "NOT INSTALLED" : status.RadarScanning ? "SCANNING" : "ONLINE";
            string warning = !status.LowResourceWarning.HasValue ? "--" : status.LowResourceWarning.Value ? "LOW RESOURCES" : "CLEAR";
            body.text = $"RADAR MODULE       {radarState}\n" +
                $"RADAR USES LEFT    {status.RadarUsesRemaining?.ToString() ?? "--"}\n\n" +
                $"ENERGY             {Number(status.EnergyCurrent)} / {Number(status.EnergyMaximum)}\n" +
                $"DRAIN RATE         {Number(status.EnergyDrain)}\n" +
                $"POWER MODULE       {status.PowerState}\n" +
                $"RESOURCE WARNING   {warning}\n\n" +
                $"CAMERA MODULE      {status.CameraState}\n" +
                $"CAPTURE ARRAY      {status.CaptureState}";
            if (Expedition != null) body.text = Expedition.StatusText() + "\n" + body.text;
        }
        private static string Number(float? value) => value?.ToString("0.0") ?? "--";
    }
}

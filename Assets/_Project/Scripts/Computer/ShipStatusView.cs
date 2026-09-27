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
        private G10.Prototype.UI.ShipEnergyBar energyBar;
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
            if (energyBar == null && providerSource is ExistingShipStatusProvider existing && existing.Navigation != null)
                energyBar = G10.Prototype.UI.ShipEnergyBar.Create(body.transform.parent, existing.Navigation, body.font,
                    new Vector2(30, 20), new Vector2(1180, 68));
            if (status.Ship != null)
            {
                var ship = status.Ship;
                body.text = (Expedition != null ? Expedition.StatusText() + "\n\n" : "") +
                    $"HULL                  {ship.hull:0.0} / {ship.hullCapacity:0.#}\n" +
                    $"MAX SPEED             {ship.speed:0.#} m/s\n" +
                    $"DIVE / ASCEND         {ship.diveSpeed:0.#} / {ship.ascentSpeed:0.#} m/s\n" +
                    $"DEPTH LIMIT           {ship.maximumDepth:0.#} m\n\n" +
                    $"RADAR                 {ship.radar} / {ship.radarCapacity}   {(status.RadarScanning ? "SCANNING" : "")}\n" +
                    $"PHOTO SHOTS           {ship.photos} / {ship.photoCapacity}\n" +
                    $"CAPTURE ATTEMPTS      {ship.captures} / {ship.captureCapacity}\n\n" +
                    (ship.hull <= 0 ? "HULL CRITICAL — REST / RECOVERY REQUIRED" : ship.energy <= 0 ? "ENERGY EMPTY — REST / RECOVERY REQUIRED" :
                        status.LowResourceWarning == true ? "LOW RESOURCES — REST TO REFILL" : "SYSTEMS READY");
                return;
            }
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

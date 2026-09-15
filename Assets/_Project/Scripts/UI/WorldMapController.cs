using UnityEngine;

namespace G10.Prototype.UI
{
    /// <summary>Session-local UI navigation; never loads or unloads gameplay scenes.</summary>
    public sealed class WorldMapController : MonoBehaviour
    {
        public CabinStationView cabin;
        public GameObject worldPanel;
        public GameObject[] zoneMaps;
        public PhotoSurveyMap zone01Overlay;
        public int LastZoneIndex { get; private set; } = 0;
        private WorldMapZoneHotspot focusedRegion;
        public void FocusRegion(WorldMapZoneHotspot region)
        {
            if (focusedRegion != null && focusedRegion != region) focusedRegion.ClearFocusImmediately();
            focusedRegion = region;
        }
        public void OpenWorld()
        {
            cabin.Brake(); cabin.SetHover("");
            // A zone panel may be active in the authored scene before UIManager owns it.
            if (zoneMaps != null)
                foreach (var zone in zoneMaps)
                    if (zone != null && zone != worldPanel) zone.SetActive(false);
            cabin.Panels.OpenPanel(worldPanel);
        }
        public void OpenZone(int index)
        {
            if (zoneMaps == null || index < 0 || index >= zoneMaps.Length || zoneMaps[index] == null) return;
            LastZoneIndex = index;
            cabin.Brake(); cabin.SetHover("");
            cabin.Panels.OpenPanel(zoneMaps[index]);
            if (index == 0 && zone01Overlay != null) zone01Overlay.RestoreSelection();
        }
        public void ResumeZone() => OpenZone(LastZoneIndex);
        public void CloseWorld() => cabin.ClosePanel();
    }
}

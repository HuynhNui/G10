using UnityEngine;

namespace G10.Prototype.UI
{
    public sealed class ZoneMapBackHandler : MonoBehaviour, IPanelBackHandler
    {
        public WorldMapController worldMap;
        // Escape leaves the map completely. Returning to the world map is an explicit UI action.
        public bool TryHandleBack() { worldMap.CloseWorld(); return true; }
    }
}

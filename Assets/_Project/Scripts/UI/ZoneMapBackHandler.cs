using UnityEngine;

namespace G10.Prototype.UI
{
    public sealed class ZoneMapBackHandler : MonoBehaviour, IPanelBackHandler
    {
        public WorldMapController worldMap;
        public bool TryHandleBack() { worldMap.OpenWorld(); return true; }
    }
}

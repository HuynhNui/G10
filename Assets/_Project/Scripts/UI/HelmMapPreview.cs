using UnityEngine;

namespace G10.Prototype.UI
{
    /// <summary>Art-only view of the active zone; never owns navigation, objectives or map overlays.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class HelmMapPreview : MonoBehaviour
    {
        [SerializeField] private CabinStationView cabin;
        [SerializeField] private UnityEngine.UI.RawImage mapImage;
        [SerializeField] private UnityEngine.UI.AspectRatioFitter mapAspect;
        [SerializeField] private UnityEngine.UI.Button openMapButton;

        private GameObject sourcePanel;
        private ZoneMapPresentation source;
        public UnityEngine.UI.RawImage MapImage => mapImage;

        public void Configure(CabinStationView owner, UnityEngine.UI.RawImage image,
            UnityEngine.UI.AspectRatioFitter aspect, UnityEngine.UI.Button button)
        {
            cabin = owner; mapImage = image; mapAspect = aspect; openMapButton = button;
            RefreshMap();
        }

        private void Awake()
        {
            if (cabin == null) cabin = GetComponentInParent<CabinStationView>();
            if (openMapButton == null) openMapButton = GetComponent<UnityEngine.UI.Button>();
        }

        private void OnEnable()
        {
            RefreshMap();
            if (openMapButton != null) openMapButton.onClick.AddListener(OpenMap);
        }

        private void OnDisable()
        {
            if (openMapButton != null) openMapButton.onClick.RemoveListener(OpenMap);
        }

        private void Update() => RefreshMap();
        private void OpenMap() => cabin?.OpenCurrentMap();

        public void RefreshMap()
        {
            if (cabin == null || mapImage == null) return;
            if (sourcePanel != cabin.MapPanel)
            {
                sourcePanel = cabin.MapPanel;
                source = sourcePanel != null ? sourcePanel.GetComponent<ZoneMapPresentation>() : null;
            }
            Texture2D texture = cabin.ChartArt;
            if (source != null && source.config != null)
            {
                // The full chart can be inactive. Read authoritative state, not its stale RawImage.
                texture = source.navigation != null && source.RockEffect?.DeferTerrainReveal == true
                    ? source.config.map : source.config.MapFor(source.MissionRuntime);
            }
            if (mapImage.texture == texture) return;
            mapImage.texture = texture;
            if (mapAspect != null && texture != null)
                mapAspect.aspectRatio = (float)texture.width / texture.height;
        }
    }
}

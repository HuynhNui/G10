using System.Collections.Generic;
using G10.Prototype.Missions;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>Applies one zone's map art and animates its transparent light layers.</summary>
    public sealed class ZoneMapPresentation : MonoBehaviour
    {
        public ZoneMapConfig config;
        public RawImage mapImage;
        public RawImage[] lightImages;
        public PhotoSurveyMap overlay;
        public ZoneNavigation navigation;
        [Min(.01f)] public float lightExpandSeconds = 5f;
        [Min(1f)] public float maximumLightScale = 1.08f;

        private ZoneMissionRuntime runtime;
        private float enabledAt;
        private bool? alternateApplied;
        private bool axesConfigured;
        private bool terrainEnabled = true;
        private ZoneMapConfig terrainMapConfig;
        private UnityEngine.UI.RawImage terrainSourceImage;
        private UnityEngine.UI.RawImage terrainImage;
        private RectTransform terrainControls;
        private UnityEngine.UI.Button terrainToggleButton;
        private TMP_Text terrainToggleLabel, terrainLegend;
        private readonly Dictionary<ZoneMapConfig, Texture2D> terrainTextures = new();
        private int terrainTextureBuildCount, terrainCachedTextureCount;
        public bool TerrainEnabled => terrainEnabled;
        public bool TerrainVisible => terrainImage != null && terrainImage.isActiveAndEnabled;
        public ZoneMapConfig TerrainMapConfig => terrainMapConfig;
        public UnityEngine.UI.RawImage TerrainImage => terrainImage;
        public UnityEngine.UI.Button TerrainToggleButton => terrainToggleButton;
        public TMP_Text TerrainToggleLabel => terrainToggleLabel;
        public TMP_Text TerrainLegend => terrainLegend;
        public int TerrainTextureBuildCount => terrainTextureBuildCount;
        public int TerrainCachedTextureCount => terrainCachedTextureCount;
        public ExpeditionLoop Timeline { get; set; }
        public G10.Prototype.Feedback.RockBreakerSequence RockEffect { get; set; }
        private SavedZoneMapProgress savedProgress;
        public void RefreshSavedProgress()
        {
            savedProgress = Timeline != null && config != null && runtime == null ? new SavedZoneMapProgress(config,
                Timeline.ReadZoneState(config.zoneId), Timeline.HasVisitedZone(config.returnDestinationZone)) : null;
            if (overlay != null) overlay.SavedProgress = savedProgress;
            RefreshMap();
        }

        public ZoneMissionRuntime MissionRuntime
        {
            get => runtime;
            set
            {
                runtime = value;
                if (overlay != null) overlay.missionRuntime = value;
                RefreshMap();
            }
        }

        private void OnEnable()
        {
            enabledAt = Time.unscaledTime;
            ConfigureAxisLabels();
            ResolveRuntime();
            RefreshSavedProgress();
            RefreshMap();
            AnimateLights(0f);
        }

        private void Update()
        {
            if (runtime == null) ResolveRuntime();
            RefreshMap();
            AnimateLights(Time.unscaledTime - enabledAt);
        }

        public void RefreshMap()
        {
            RefreshTerrainLayer();
            if (config == null) return;
            // Refresh before the alternate-map cache: reopening and live Inspector edits must not reset alpha.
            ApplyLightOpacity();
            bool alternate = runtime != null ? config.UsesAlternate(runtime) : savedProgress?.UsesAlternate == true;
            if (navigation != null && RockEffect?.DeferTerrainReveal == true) alternate = false;
            if (mapImage != null) mapImage.texture = alternate && config.alternateMap != null ? config.alternateMap : config.map;
            if (alternateApplied == alternate) return;
            alternateApplied = alternate;
            if (navigation != null) config.ApplyTerrain(navigation, alternate);
        }

        public void SetTerrainVisible(bool visible)
        {
            terrainEnabled = visible;
            RefreshTerrainLayer();
        }

        public void ToggleTerrainVisibility() => SetTerrainVisible(!terrainEnabled);

        private void RefreshTerrainLayer()
        {
            if (config == null || config.zoneId != "Zone04" || mapImage == null)
            {
                terrainMapConfig = null;
                terrainSourceImage = null;
                if (terrainImage != null) terrainImage.gameObject.SetActive(false);
                if (terrainControls != null) terrainControls.gameObject.SetActive(false);
                return;
            }
            if (terrainImage == null || terrainMapConfig != config || terrainSourceImage != mapImage)
            {
                terrainMapConfig = config;
                terrainSourceImage = mapImage;
                if (terrainImage == null)
                {
                    var layer = new GameObject("Zone04TerrainContours", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
                    terrainImage = layer.GetComponent<UnityEngine.UI.RawImage>();
                    terrainImage.raycastTarget = false;
                    terrainImage.color = Color.white;
                }
                RectTransform rect = terrainImage.rectTransform;
                rect.SetParent(mapImage.transform, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
                // Lights remain beneath the static contour; the existing grid, POIs and pointer remain above it.
                rect.SetAsLastSibling();
                if (overlay != null && overlay.transform.parent == mapImage.transform)
                    rect.SetSiblingIndex(overlay.transform.GetSiblingIndex());
                terrainImage.texture = TerrainTextureFor(config);
                EnsureTerrainControls();
            }
            // A child stretches over the exact source rectangle, including its non-default crop or UV flip.
            if (terrainImage.uvRect != mapImage.uvRect) terrainImage.uvRect = mapImage.uvRect;
            bool available = terrainImage.texture != null;
            bool show = available && terrainEnabled;
            if (terrainImage.gameObject.activeSelf != show) terrainImage.gameObject.SetActive(show);
            if (terrainControls != null && terrainControls.gameObject.activeSelf != available)
                terrainControls.gameObject.SetActive(available);
            if (terrainToggleLabel != null)
            {
                string label = terrainEnabled ? "ĐỊA HÌNH: BẬT" : "ĐỊA HÌNH: TẮT";
                if (terrainToggleLabel.text != label) terrainToggleLabel.text = label;
            }
        }

        private Texture2D TerrainTextureFor(ZoneMapConfig source)
        {
            if (terrainTextures.TryGetValue(source, out Texture2D cached)) return cached;
            Texture2D texture = null;
            if (source.TryCopyTerrainMask(out byte[] cells, out int columns, out int rows))
            {
                var pixels = new Color32[cells.Length];
                Color32 edge = new(242, 222, 153, 238);
                Color32 fill = new(242, 222, 153, 50);
                for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int index = y * columns + x;
                    if (cells[index] != 0) continue;
                    // Only blocked cells receive colour. No dilation, region fill, or water-side stroke.
                    bool boundary = x == 0 || y == 0 || x == columns - 1 || y == rows - 1 ||
                        cells[index - 1] != 0 || cells[index + 1] != 0 ||
                        cells[index - columns] != 0 || cells[index + columns] != 0;
                    pixels[index] = boundary ? edge : fill;
                }
                texture = new Texture2D(columns, rows, TextureFormat.RGBA32, false)
                {
                    name = "Zone04 Static Terrain Contours",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                terrainTextureBuildCount++;
                terrainCachedTextureCount++;
            }
            // Also cache an invalid source so a damaged mask cannot cause a decode attempt every frame.
            terrainTextures.Add(source, texture);
            return texture;
        }

        private void EnsureTerrainControls()
        {
            if (terrainControls != null) return;
            terrainControls = NewTerrainRect("Zone04TerrainLegend", transform);
            terrainControls.anchorMin = terrainControls.anchorMax = new Vector2(1, 1);
            terrainControls.pivot = new Vector2(1, 1);
            // The authored header has spare space right of SurveyLegend and above the chart.
            terrainControls.anchoredPosition = new Vector2(-30, -118);
            terrainControls.sizeDelta = new Vector2(510, 58);
            var background = terrainControls.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(.03f, .08f, .11f, .96f);
            background.raycastTarget = false;

            RectTransform buttonRect = NewTerrainRect("TerrainToggle", terrainControls);
            buttonRect.anchorMin = Vector2.zero; buttonRect.anchorMax = new Vector2(.52f, 1);
            buttonRect.offsetMin = new Vector2(4, 4); buttonRect.offsetMax = new Vector2(-4, -4);
            var buttonImage = buttonRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(.97f, .96f, .86f, 1);
            buttonImage.raycastTarget = true;
            terrainToggleButton = buttonRect.gameObject.AddComponent<UnityEngine.UI.Button>();
            terrainToggleButton.targetGraphic = buttonImage;
            terrainToggleButton.onClick.AddListener(ToggleTerrainVisibility);
            terrainToggleLabel = NewTerrainLabel("Label", buttonRect, new Color(.16f, .23f, .53f, 1), 24);
            terrainLegend = NewTerrainLabel("Meaning", terrainControls, new Color(242 / 255f, 222 / 255f, 153 / 255f), 22);
            terrainLegend.rectTransform.anchorMin = new Vector2(.52f, 0);
            terrainLegend.rectTransform.offsetMin = new Vector2(4, 4);
            terrainLegend.rectTransform.offsetMax = new Vector2(-8, -4);
            terrainLegend.text = "Vàng nhạt: vật cản";
        }

        private TMP_Text NewTerrainLabel(string objectName, Transform parent, Color color, float size)
        {
            TMP_Text existing = overlay != null ? overlay.coordinateReadout : null;
            if (existing == null) existing = GetComponentInChildren<TMP_Text>(true);
            RectTransform rect = NewTerrainRect(objectName, parent);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6, 2); rect.offsetMax = new Vector2(-6, -2);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = existing != null && existing.font != null ? existing.font : TMP_Settings.defaultFontAsset;
            label.fontSize = size; label.color = color; label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false; label.richText = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true; label.fontSizeMin = size * .85f; label.fontSizeMax = size;
            return label;
        }

        private static RectTransform NewTerrainRect(string objectName, Transform parent)
        {
            var rect = (RectTransform)new GameObject(objectName, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private void OnDestroy()
        {
            if (terrainToggleButton != null) terrainToggleButton.onClick.RemoveListener(ToggleTerrainVisibility);
            if (terrainImage != null) { terrainImage.texture = null; DisposeTerrainObject(terrainImage.gameObject); }
            if (terrainControls != null) DisposeTerrainObject(terrainControls.gameObject);
            foreach (Texture2D texture in terrainTextures.Values)
                if (texture != null) DisposeTerrainObject(texture);
            terrainTextures.Clear();
            terrainCachedTextureCount = 0;
        }

        private static void DisposeTerrainObject(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        public void ApplyLightOpacity()
        {
            if (config == null || lightImages == null) return;
            for (int i = 0; i < lightImages.Length; i++)
            {
                UnityEngine.UI.RawImage light = lightImages[i];
                if (light == null) continue;
                float opacity = config.LightOpacityFor(i);
                Color color = light.color;
                if (Mathf.Approximately(color.a, opacity)) continue;
                color.a = opacity;
                light.color = color;
            }
        }

        public static float EvaluateLightScale(float elapsed, float expandSeconds, float maximumScale)
        {
            float duration = Mathf.Max(.01f, expandSeconds);
            float amount = Mathf.PingPong(Mathf.Max(0f, elapsed) / duration, 1f);
            return Mathf.Lerp(1f, Mathf.Max(1f, maximumScale), Mathf.SmoothStep(0f, 1f, amount));
        }

        private void AnimateLights(float elapsed)
        {
            if (lightImages == null) return;
            float scale = EvaluateLightScale(elapsed, lightExpandSeconds, maximumLightScale);
            foreach (var light in lightImages)
                if (light != null) light.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private void ResolveRuntime()
        {
            if (Timeline != null || config == null || config.missionConfig == null) return;
            foreach (var candidate in FindObjectsByType<ZoneMissionRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate != null && candidate.config == config.missionConfig)
                {
                    MissionRuntime = candidate;
                    return;
                }
        }

        private void ConfigureAxisLabels()
        {
            if (axesConfigured || config == null) return;
            Transform frame = transform.Find("ChartOuterFrame");
            RectTransform content = transform.Find("SquareChartContent") as RectTransform ??
                transform.Find("MapChartContent") as RectTransform;
            if (frame == null || content == null) return;
            ConfigureAxis(frame, 'X', config.WorldSize.x, content.rect.width);
            ConfigureAxis(frame, 'Y', config.WorldSize.y, content.rect.height);
            axesConfigured = true;
        }

        private static void ConfigureAxis(Transform frame, char axis, float extent, float displayLength)
        {
            var labels = new List<RectTransform>();
            foreach (Transform child in frame)
                if (child is RectTransform rect && TryCoordinate(child.name, axis, out _)) labels.Add(rect);
            if (labels.Count < 2 || extent <= 0f) return;
            labels.Sort((a, b) => Coordinate(a.name, axis).CompareTo(Coordinate(b.name, axis)));
            Vector2 start = labels[0].anchoredPosition;
            Vector2 second = labels[1].anchoredPosition;
            Vector2 end = start;
            if (axis == 'X') end.x += Mathf.Sign(second.x - start.x) * displayLength;
            else end.y += Mathf.Sign(second.y - start.y) * displayLength;
            int maximum = Mathf.FloorToInt(extent / 100f) * 100;
            RectTransform template = labels[0];
            for (int coordinate = 0; coordinate <= maximum; coordinate += 100)
            {
                RectTransform label = labels.Find(item => Coordinate(item.name, axis) == coordinate);
                if (label == null)
                {
                    label = Instantiate(template, frame);
                    label.name = axis + coordinate.ToString();
                    labels.Add(label);
                }
                label.gameObject.SetActive(true);
                label.anchoredPosition = Vector2.Lerp(start, end, coordinate / extent);
                var tmp = label.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null) tmp.text = coordinate.ToString();
                var legacy = label.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (legacy != null) legacy.text = coordinate.ToString();
            }
            foreach (RectTransform label in labels)
                if (Coordinate(label.name, axis) > maximum) label.gameObject.SetActive(false);
        }

        private static int Coordinate(string name, char axis) =>
            TryCoordinate(name, axis, out int coordinate) ? coordinate : int.MaxValue;

        private static bool TryCoordinate(string name, char axis, out int coordinate)
        {
            coordinate = 0;
            return !string.IsNullOrEmpty(name) && name.Length > 1 && name[0] == axis &&
                int.TryParse(name.Substring(1), out coordinate);
        }
    }
}

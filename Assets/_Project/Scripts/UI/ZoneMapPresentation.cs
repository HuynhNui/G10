using System.Collections.Generic;
using G10.Prototype.Missions;
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
            if (config == null) return;
            bool alternate = config.UsesAlternate(runtime);
            if (mapImage != null) mapImage.texture = alternate && config.alternateMap != null ? config.alternateMap : config.map;
            if (alternateApplied == alternate) return;
            alternateApplied = alternate;
            if (navigation != null) config.ApplyTerrain(navigation, alternate);
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
            if (config == null || config.missionConfig == null) return;
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

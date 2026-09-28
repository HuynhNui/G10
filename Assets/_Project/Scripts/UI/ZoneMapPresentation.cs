using G10.Prototype.Missions;
using G10.Prototype.Navigation;
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
    }
}

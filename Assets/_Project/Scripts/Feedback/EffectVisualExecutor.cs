using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    /// <summary>Corrupts a captured display frame; live UI and detection state stay authoritative.</summary>
    public sealed class EffectVisualExecutor : MonoBehaviour
    {
        public RectTransform displayRegion;
        public bool circularDisplay;
        public Material signalTemplate;
        public bool useLibraryReflection;
        public ShaderVariantCollection reflectionVariants;
        public ZoneThreeVisualSurface procedural;
        private RawImage signal;
        private Image anomaly;
        private Texture2D capture;
        private Material material;
        private Coroutine captureRoutine;
        private bool requested;
        private readonly Vector3[] corners = new Vector3[4];
        public bool SignalVisible => signal != null && signal.enabled;
        public bool AnomalyVisible => anomaly != null && anomaly.enabled;
        public bool GlitchEnabled => SignalVisible && material != null;
        public int CaptureCount { get; private set; }
        private void Awake()
        {
            // The first glimpse lasts only .2 s: compile its library variant during scene loading.
            if (useLibraryReflection && reflectionVariants != null) reflectionVariants.WarmUp();
        }
        public void Render(ScreenInterference preset, float strength, float time, int seed,
            Sprite sprite, float opacity, Vector2 position, Vector2 size, bool visible)
        {
            if (!isActiveAndEnabled || !visible) { Restore(); return; }
            bool composition = ZoneThreeVisualSurface.Supports(preset);
            procedural?.Render(preset, strength, time, seed, visible);
            if (!composition && preset != ScreenInterference.None && strength > 0)
            {
                if (!requested)
                {
                    requested = true;
                    if (signalTemplate != null) captureRoutine = StartCoroutine(CaptureDisplay());
                }
                if (material != null)
                {
                    material.SetFloat("_Strength", Mathf.Clamp01(strength)); material.SetFloat("_EffectTime", time);
                    material.SetFloat("_Seed", seed); material.SetFloat("_Circle", circularDisplay ? 1 : 0);
                    material.SetFloat("_Analog", preset == ScreenInterference.AnalogSignalFailure ? 1 : 0);
                }
            }
            else StopSignal();
            if (sprite == null || opacity <= 0) { if (anomaly != null) anomaly.enabled = false; }
            else
            {
                if (anomaly == null)
                {
                    anomaly = Layer<Image>("TransientSilhouette", transform);
                    if (useLibraryReflection)
                    {
                        var effect = anomaly.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();
                        effect.toneFilter = Coffee.UIEffects.ToneFilter.Grayscale; effect.toneIntensity = .65f;
                        effect.samplingFilter = Coffee.UIEffects.SamplingFilter.RgbShift; effect.samplingIntensity = .012f;
                    }
                }
                anomaly.enabled = true; anomaly.sprite = sprite; anomaly.preserveAspect = true;
                anomaly.color = new Color(.12f, .17f, .25f, Mathf.Clamp01(opacity));
                var rect = anomaly.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = position; rect.sizeDelta = size;
            }
        }
        private IEnumerator CaptureDisplay()
        {
            // Overlay canvases are absent from camera post processing. Capture after UI renders,
            // once per activation, before enabling our own graphic (no recursive feedback).
            yield return new WaitForEndOfFrame(); captureRoutine = null;
            if (!requested || !isActiveAndEnabled) yield break;
            var region = displayRegion != null ? displayRegion : transform as RectTransform;
            if (region == null) yield break;
            var canvas = region.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            region.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            int x = Mathf.Clamp(Mathf.FloorToInt(min.x), 0, Screen.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(min.y), 0, Screen.height - 1);
            int w = Mathf.Clamp(Mathf.CeilToInt(max.x) - x, 1, Screen.width - x);
            int h = Mathf.Clamp(Mathf.CeilToInt(max.y) - y, 1, Screen.height - y);
            var frame = ScreenCapture.CaptureScreenshotAsTexture(); if (frame == null) yield break;
            try
            {
                if (capture == null || capture.width != w || capture.height != h)
                {
                    Release(capture); capture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                    { name = "Transient display frame", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                }
                capture.SetPixels(frame.GetPixels(x, y, w, h)); capture.Apply(false, false);
                if (signal == null) signal = Layer<RawImage>("ScopedSignalInterference", region);
                if (material == null) material = new Material(signalTemplate) { name = "Scoped signal instance" };
                material.SetFloat("_Circle", circularDisplay ? 1 : 0);
                signal.texture = capture; signal.material = material; signal.color = Color.white;
                signal.enabled = true; CaptureCount++;
            }
            finally { Release(frame); }
        }
        private static T Layer<T>(string label, Transform parent) where T : Graphic
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(T)); obj.transform.SetParent(parent, false);
            var graphic = obj.GetComponent<T>(); graphic.raycastTarget = false;
            var rect = graphic.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return graphic;
        }
        private void StopSignal()
        {
            requested = false;
            if (captureRoutine != null) { StopCoroutine(captureRoutine); captureRoutine = null; }
            if (signal != null) signal.enabled = false;
        }
        public void Restore() { StopSignal(); procedural?.Restore(); if (anomaly != null) { anomaly.enabled = false; anomaly.sprite = null; } }
        private static void Release(Object value)
        { if (value != null) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); } }
        private void OnDisable() => Restore();
        private void OnDestroy() { Release(capture); Release(material); if (signal != null) Release(signal.gameObject); }
    }
}

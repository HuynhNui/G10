using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    /// <summary>Owns only dedicated, noninteractive surface graphics. Never edits the audience UI.</summary>
    public sealed class EffectVisualExecutor : MonoBehaviour
    {
        private RawImage signal;
        private Image anomaly;
        private UIEffect uiEffect;
        private Texture2D noise;
        public bool SignalVisible => signal != null && signal.enabled;
        public bool AnomalyVisible => anomaly != null && anomaly.enabled;
        public bool GlitchEnabled => uiEffect != null && uiEffect.enabled;
        public void Render(ScreenInterference preset, float strength, float time, int seed,
            Sprite sprite, float opacity, Vector2 position, Vector2 size, bool visible)
        {
            if (!isActiveAndEnabled || !visible) { Restore(); return; }
            if (preset == ScreenInterference.None || strength <= 0)
            { if (signal != null) signal.enabled = false; if (uiEffect != null) uiEffect.enabled = false; }
            else
            {
                EnsureSignal(); signal.enabled = true; uiEffect.enabled = true;
                float irregular = EffectMotion.Irregular(time * 2, seed);
                float amount = Mathf.Clamp01(strength);
                float scale = preset == ScreenInterference.SubtleDisplayInstability ? .08f : preset == ScreenInterference.AnalogSignalFailure ? .28f : .16f;
                signal.color = new Color(.65f, .77f, .74f, amount * scale * (.4f + .6f * irregular));
                signal.uvRect = new Rect(0, Mathf.Floor(time * 5) * .071f, 1, 1);
                uiEffect.samplingIntensity = amount * (preset == ScreenInterference.AnalogSignalFailure ? .15f : .04f);
            }
            if (sprite == null || opacity <= 0) { if (anomaly != null) anomaly.enabled = false; }
            else
            {
                if (anomaly == null) anomaly = Layer<Image>("TransientSilhouette");
                anomaly.enabled = true; anomaly.sprite = sprite; anomaly.preserveAspect = true;
                anomaly.color = new Color(0, 0, 0, Mathf.Clamp01(opacity));
                var rect = anomaly.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = position; rect.sizeDelta = size;
            }
        }
        private T Layer<T>(string label) where T : Graphic
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
            obj.transform.SetParent(transform, false);
            var graphic = obj.GetComponent<T>(); graphic.raycastTarget = false;
            var rect = graphic.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return graphic;
        }
        private void EnsureSignal()
        {
            if (signal != null) return;
            signal = Layer<RawImage>("ScopedSignalInterference");
            noise = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Procedural instrument static", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var pixels = new Color32[4096]; var random = new System.Random(147);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            { byte v = (byte)random.Next(30, 225); pixels[y * 64 + x] = new Color32(v, v, v, (byte)(y % 4 == 0 ? 180 : 40)); }
            noise.SetPixels32(pixels); noise.Apply(false, true); signal.texture = noise;
            uiEffect = signal.gameObject.AddComponent<UIEffect>();
            uiEffect.samplingFilter = SamplingFilter.RgbShift; uiEffect.toneFilter = ToneFilter.Grayscale;
            uiEffect.toneIntensity = .6f;
        }
        public void Restore()
        {
            if (uiEffect != null) uiEffect.enabled = false;
            if (signal != null) { signal.enabled = false; signal.color = Color.clear; signal.uvRect = new Rect(0, 0, 1, 1); }
            if (anomaly != null)
            {
                anomaly.enabled = false; anomaly.sprite = null;
                var rect = anomaly.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
        }
        private void OnDisable() => Restore();
        private void OnDestroy() { if (noise != null) { if (Application.isPlaying) Destroy(noise); else DestroyImmediate(noise); } }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    /// <summary>A bounded, masked drawing module driven exclusively by EffectDirector's clock.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneThreeVisualSurface : MonoBehaviour
    {
        public Material compositionTemplate;
        // Scoped graphics live inside their actual window; the director still owns their lifetime.
        public RectTransform compositionRegion;
        public bool circularDisplay;
        private RawImage layer;
        private Material material;
        private ScreenInterference activePreset;
        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly int TimeId = Shader.PropertyToID("_EffectTime");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int CircleId = Shader.PropertyToID("_Circle");
        public bool Visible => layer != null && layer.enabled && layer.gameObject.activeInHierarchy;
        public ScreenInterference ActivePreset => Visible ? activePreset : ScreenInterference.None;
        public int ActivationCount { get; private set; }
        public bool HasRuntimeMaterial => material != null;
        public RawImage Graphic => layer;

        public static bool Supports(ScreenInterference preset) => preset == ScreenInterference.StoneDepth ||
            preset == ScreenInterference.FalseRadarSweep || preset == ScreenInterference.ProceduralReflection ||
            preset == ScreenInterference.BioelectricPulse || preset == ScreenInterference.RockDebris;

        public void Render(ScreenInterference preset, float strength, float time, int seed, bool visible)
        {
            if (!isActiveAndEnabled || !visible || !Supports(preset) || strength <= 0 || compositionTemplate == null)
            { Restore(); return; }
            if (layer == null)
            {
                var obj = new GameObject("ZoneThreeProceduralComposition", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                var parent = compositionRegion != null ? compositionRegion : transform;
                obj.layer = parent.gameObject.layer;
                obj.transform.SetParent(parent, false);
                layer = obj.GetComponent<RawImage>(); layer.raycastTarget = false; layer.color = Color.white;
                var rect = layer.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            // A window can rebuild its graphic independently of this owner. Reuse the material.
            if (material == null) material = new Material(compositionTemplate) { name = "Zone 3 scoped procedural instance" };
            if (layer.material != material) layer.material = material;
            if (!layer.enabled || activePreset != preset) ActivationCount++;
            activePreset = preset; layer.enabled = true;
            material.SetFloat(ModeId, (int)preset - (int)ScreenInterference.StoneDepth + 1);
            material.SetFloat(StrengthId, Mathf.Clamp01(strength));
            material.SetFloat(TimeId, Mathf.Max(0, time)); material.SetFloat(SeedId, seed);
            var bounds = layer.rectTransform.rect;
            material.SetFloat(AspectId, bounds.height > 0 ? bounds.width / bounds.height : 1);
            material.SetFloat(CircleId, circularDisplay ? 1 : 0);
        }
        public void Restore()
        {
            if (layer != null) layer.enabled = false;
            activePreset = ScreenInterference.None;
        }
        private void OnDisable() => Restore();
        private void OnDestroy()
        {
            if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            if (layer != null) { if (Application.isPlaying) Destroy(layer.gameObject); else DestroyImmediate(layer.gameObject); }
        }
    }
}

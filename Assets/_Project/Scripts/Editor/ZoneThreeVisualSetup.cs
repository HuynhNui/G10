using System;
using G10.Prototype.Feedback;
using UnityEditor;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>Authoring helper for the existing EffectDirector surfaces; creates no runtime coordinator.</summary>
    public static class ZoneThreeVisualSetup
    {
        public const string MaterialPath = "Assets/_Project/Shaders/ZoneThreeComposition.mat";
        public static Material CompositionMaterial()
        {
            var shader = Shader.Find("G10/UI/ZoneThreeComposition");
            if (shader == null) throw new InvalidOperationException("Import ZoneThreeComposition.shader before configuring Zone 3.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "ZoneThreeComposition" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            { Undo.RecordObject(material, "Set Zone 3 UI shader"); material.shader = shader; EditorUtility.SetDirty(material); }
            return material;
        }

        public static ZoneThreeVisualSurface Bind(EffectSurface surface, GameObject audience, bool circular = false)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Configure production surfaces outside Play Mode.");
            Undo.RecordObject(surface, "Bind Zone 3 effect audience");
            if (audience != null) surface.BindVisibility(audience);
            var visual = surface.visual != null ? surface.visual : surface.GetComponent<EffectVisualExecutor>();
            if (visual == null) visual = Undo.AddComponent<EffectVisualExecutor>(surface.gameObject);
            var composition = surface.GetComponent<ZoneThreeVisualSurface>();
            if (composition == null) composition = Undo.AddComponent<ZoneThreeVisualSurface>(surface.gameObject);
            Undo.RecordObject(visual, "Configure Zone 3 scoped visuals");
            Undo.RecordObject(composition, "Configure Zone 3 procedural composition");
            composition.compositionTemplate = CompositionMaterial(); composition.circularDisplay = circular;
            composition.compositionRegion = audience != null ? audience.transform as RectTransform : null;
            visual.procedural = composition; visual.circularDisplay = circular;
            // Existing analog presets keep using their proven captured-frame shader.
            // Scoped compositions inherit real window order, masks and rotation. Global effects
            // retain the feedback canvas so gameplay-wide lighting/debris still cover the viewport.
            if (visual.signalTemplate == null) visual.signalTemplate = HorrorContentSetup.SignalMaterial();
            if (audience != null) visual.displayRegion = audience.transform as RectTransform;
            surface.visual = visual; surface.FitToAudience();
            EditorUtility.SetDirty(surface); EditorUtility.SetDirty(visual); EditorUtility.SetDirty(composition);
            return composition;
        }

        public static void WarmUp()
        {
            var template = CompositionMaterial();
            foreach (bool clipped in new[] { false, true })
            {
                var material = new Material(template);
                if (clipped) material.EnableKeyword("UNITY_UI_CLIP_RECT");
                ShaderUtil.CompilePass(material, 0, true);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}

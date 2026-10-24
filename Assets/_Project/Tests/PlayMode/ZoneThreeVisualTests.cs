#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using G10.Prototype.Feedback;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    /// <summary>
    /// GPU and executor acceptance for the production shader, independent of a player save.
    /// Full shared-cabin story demos are separate: this fixture does not invent navigation or contacts.
    /// </summary>
    public sealed class ZoneThreeVisualTests
    {
        private const int Size = 256;
        private GameObject root, cameraObject;
        private Camera camera;
        private RenderTexture target;
        private Texture2D readback;
        private EffectDirector director;
        private EffectSurface surface;
        private Material template;
        private readonly List<EffectDefinition> definitions = new();
        private static readonly ScreenInterference[] Presets = { ScreenInterference.StoneDepth, ScreenInterference.FalseRadarSweep,
            ScreenInterference.ProceduralReflection, ScreenInterference.BioelectricPulse, ScreenInterference.RockDebris };

        [SetUp] public void Setup()
        {
            template = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Shaders/ZoneThreeComposition.mat");
            Assert.That(template, Is.Not.Null, "Run the Zone 3 production installer before this asset acceptance suite.");
            Assert.That(template.shader.isSupported, Is.True);
            foreach (bool clipped in new[] { false, true })
            {
                var warm = new Material(template);
                if (clipped) warm.EnableKeyword("UNITY_UI_CLIP_RECT");
                ShaderUtil.CompilePass(warm, 0, true); Object.DestroyImmediate(warm);
            }
            root = new GameObject("Zone 3 production visual acceptance", typeof(RectTransform), typeof(Canvas)); root.layer = 31;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)root.transform; canvasRect.sizeDelta = new Vector2(Size, Size);
            cameraObject = new GameObject("Zone 3 acceptance render camera", typeof(Camera)); camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.orthographicSize = Size * .5f;
            camera.transform.position = new Vector3(0, 0, -100); camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.27f, .30f, .32f, 1);
            canvas.worldCamera = camera;
            target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            readback = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            var audience = Child("Actual audience", typeof(RawImage)); var audienceRect = (RectTransform)audience.transform;
            audienceRect.sizeDelta = new Vector2(224, 224);
            var background = audience.GetComponent<RawImage>(); background.texture = Texture2D.whiteTexture;
            background.color = new Color(.22f, .30f, .32f, 1); background.raycastTarget = false;
            var node = Child("Production surface", typeof(EffectSurface), typeof(EffectVisualExecutor), typeof(ZoneThreeVisualSurface), typeof(RectMask2D));
            surface = node.GetComponent<EffectSurface>(); surface.target = EffectTarget.Reflection; surface.BindVisibility(audience);
            surface.visual = node.GetComponent<EffectVisualExecutor>(); surface.visual.procedural = node.GetComponent<ZoneThreeVisualSurface>();
            surface.visual.procedural.compositionTemplate = template;
            foreach (var preset in Presets)
            {
                var definition = ScriptableObject.CreateInstance<EffectDefinition>(); definition.id = preset.ToString(); definition.channel = "z3.acceptance";
                definition.target = surface.target; definition.interference = preset; definition.interferenceIntensity = 1;
                definition.duration = 12; definition.fadeIn = definition.fadeOut = 0; definitions.Add(definition);
            }
            director = root.AddComponent<EffectDirector>(); director.Configure(definitions.ToArray(), new[] { surface }, null);
            Canvas.ForceUpdateCanvases();
        }
        private GameObject Child(string label, params Type[] components)
        {
            var types = new List<Type> { typeof(RectTransform) }; types.AddRange(components);
            var obj = new GameObject(label, types.ToArray()); obj.layer = 31; obj.transform.SetParent(root.transform, false); return obj;
        }
        private Color32[] Capture()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var graphic in root.GetComponentsInChildren<Graphic>()) { graphic.SetAllDirty(); graphic.canvasRenderer.cull = false; }
            Canvas.ForceUpdateCanvases(); camera.Render(); var previous = RenderTexture.active;
            try { RenderTexture.active = target; readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); readback.Apply(); return readback.GetPixels32(); }
            finally { RenderTexture.active = previous; }
        }
        private EffectHandle Play(ScreenInterference preset, float age)
        {
            director.ClearTransients(); var handle = director.Play(preset.ToString()); Assert.That(handle.IsValid, Is.True);
            director.Tick(age); Assert.That(surface.visual.procedural.Visible, Is.True); return handle;
        }
        private static int Distance(Color32 a, Color32 b) => Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b);
        private static int Changed(Color32[] a, Color32[] b, int threshold = 12)
        { int count = 0; for (int i = 0; i < a.Length; i++) if (Distance(a[i], b[i]) > threshold) count++; return count; }
        private static void NoPlaceholder(Color32[] pixels)
        {
            int cyan = 0, magenta = 0;
            foreach (var c in pixels) { if (c.r < 60 && c.g > 190 && c.b > 190) cyan++; if (c.r > 190 && c.b > 190 && c.g < 60) magenta++; }
            Assert.That(cyan + magenta, Is.LessThan(16), "A valid handle is insufficient if the GPU displays a shader compilation placeholder.");
        }
        private static float Brightness(Color32[] image, float u, float v)
        {
            int cx = Mathf.RoundToInt(16 + u * 224), cy = Mathf.RoundToInt(16 + v * 224); float sum = 0;
            for (int y = cy - 3; y <= cy + 3; y++) for (int x = cx - 3; x <= cx + 3; x++)
            { var p = image[y * Size + x]; sum += (p.r + p.g + p.b) / (255f * 3); }
            return sum / 49;
        }

        [UnityTest] public IEnumerator EveryProductionCompositionChangesRealPixelsAndStaysInsideItsAudience()
        {
            yield return null; var baseline = Capture(); Color32[] previous = null;
            float[] times = { 1.1f, 1.2f, .8f, .05f, 1.7f };
            for (int i = 0; i < Presets.Length; i++)
            {
                Play(Presets[i], times[i]); var pixels = Capture(); NoPlaceholder(pixels);
                Assert.That(Changed(baseline, pixels), Is.GreaterThan(32), Presets[i] + " must affect rendered pixels.");
                if (previous != null) Assert.That(Changed(previous, pixels), Is.GreaterThan(32), "Distinct concepts cannot all render one generic tint.");
                for (int y = 0; y < Size; y += 11)
                    Assert.That(Distance(baseline[y * Size + 4], pixels[y * Size + 4]), Is.LessThanOrEqualTo(2), "Scoped effects must not escape to the HUD.");
                Assert.That(surface.visual.CaptureCount, Is.Zero, "Procedural effects should not allocate/copy full-screen captures.");
                Assert.That(surface.visual.AnomalyVisible, Is.False, "Zone 3 composition has no old crew sprite."); previous = pixels;
            }
            foreach (var graphic in surface.GetComponentsInChildren<Graphic>()) Assert.That(graphic.raycastTarget, Is.False);
        }
        [UnityTest] public IEnumerator FalseSweepMovesAndCircularMaskRejectsCorners()
        {
            yield return null; var baseline = Capture(); surface.visual.procedural.circularDisplay = true;
            Play(ScreenInterference.FalseRadarSweep, .35f); var early = Capture(); director.Tick(.85f); var later = Capture();
            Assert.That(Changed(early, later), Is.GreaterThan(100), "A secondary sweep needs visible movement, not a static tinted radar.");
            foreach (var point in new[] { new Vector2Int(38, 38), new Vector2Int(218, 38), new Vector2Int(38, 218), new Vector2Int(218, 218) })
                Assert.That(Distance(baseline[point.y * Size + point.x], later[point.y * Size + point.x]), Is.LessThanOrEqualTo(2));
        }
        [UnityTest] public IEnumerator ReflectionHasSoftHeadAndShoulderVolumesRatherThanAFlatDarkCard()
        {
            yield return null; Play(ScreenInterference.ProceduralReflection, .8f); var pixels = Capture(); NoPlaceholder(pixels);
            Assert.That(Brightness(pixels, .555f, .67f) - Brightness(pixels, .22f, .67f), Is.GreaterThan(.01f), "Head volume must survive compositing.");
            Assert.That(Brightness(pixels, .555f, .49f) - Brightness(pixels, .22f, .49f), Is.GreaterThan(.01f), "Shoulder volume must survive compositing.");
        }
        [UnityTest] public IEnumerator SuspensionAndDisableRemoveActualPixelsWithoutAllocatingMoreLayers()
        {
            yield return null; var baseline = Capture();
            for (int i = 0; i < 60; i++) { var handle = Play(ScreenInterference.RockDebris, 1.7f); director.Stop(handle); }
            Assert.That(surface.GetComponentsInChildren<RawImage>(true), Has.Length.EqualTo(1));
            Play(ScreenInterference.RockDebris, 1.7f); Assert.That(Changed(baseline, Capture()), Is.GreaterThan(32));
            director.SetSuspended(true, true); Assert.That(surface.visual.procedural.Visible, Is.False);
            Assert.That(Changed(baseline, Capture(), 2), Is.Zero, "Suspension must restore the actual rendered baseline.");
            director.SetSuspended(false); Play(ScreenInterference.ProceduralReflection, .8f);
            var material = surface.GetComponentInChildren<RawImage>().material;
            surface.gameObject.SetActive(false); Assert.That(Changed(baseline, Capture(), 2), Is.Zero);
            Object.Destroy(surface.gameObject); yield return null; yield return null;
            Assert.That(material == null, Is.True, "Owned runtime material must be released.");
        }
        [TearDown] public void Cleanup()
        {
            if (camera != null) camera.targetTexture = null;
            Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            Object.DestroyImmediate(readback);
            foreach (var definition in definitions) Object.DestroyImmediate(definition); definitions.Clear();
        }
    }
}
#endif

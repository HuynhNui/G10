using System;
using System.IO;
using System.Linq;
using G10.Prototype.Atmosphere;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>Additive installation. Existing artwork is not cut up or modified; only presentation objects move.</summary>
    public static class CabinAtmosphereEditor
    {
        private const string TextureFolder = "Assets/_Project/Art/CabinAtmosphere";
        [MenuItem("G10/Atmosphere/Sync Authored FX Opacity")]
        public static void SyncOpacity()
        {
            if (EditorApplication.isPlaying) return;
            CabinAtmosphere mood = Object.FindAnyObjectByType<CabinAtmosphere>(); if (mood == null) return;
            foreach (WatercolorBreathing fx in mood.GetComponentsInChildren<WatercolorBreathing>(true)) SetAlpha(fx.GetComponent<Graphic>(), fx.opacity);
            foreach (CausticScroller fx in mood.GetComponentsInChildren<CausticScroller>(true)) SetAlpha(fx.GetComponent<Graphic>(), fx.opacity);
            foreach (RandomBlink fx in mood.GetComponentsInChildren<RandomBlink>(true)) SetAlpha(fx.GetComponent<Graphic>(), fx.minimumAlpha);
            EditorSceneManager.MarkSceneDirty(mood.gameObject.scene);
        }
        private static void SetAlpha(Graphic graphic, float alpha)
        { Undo.RecordObject(graphic, "Set authored FX opacity"); Color tint = graphic.color; tint.a = alpha; graphic.color = tint; }
        [MenuItem("G10/Atmosphere/Install Living Watercolor Cabin")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            CabinStationView cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01") throw new InvalidOperationException("Load GameplayCore and Zone01 first.");
            Transform frame = cabin.transform.Find("CabinCanvas/CabinFrame");
            if (frame.Find("CabinViewport") != null) { Selection.activeGameObject = frame.Find("CabinViewport").gameObject; return; }
            Transform art = frame.Find("CabinArt");
            if (art == null) throw new InvalidOperationException("CabinArt not found. No changes made.");
            if (!AssetDatabase.IsValidFolder(TextureFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "CabinAtmosphere");
            Texture2D caustic = Texture("SoftCaustics", 256, 0);
            Texture2D paper = Texture("PaperGrain", 512, 1);
            Texture2D wash = Texture("PigmentWash", 256, 2);
            Texture2D soft = Texture("SoftDot", 64, 3);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Living watercolor cabin");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Layer cabin presentation");
            RectTransform viewport = Frame("CabinViewport", frame); viewport.SetAsFirstSibling();
            viewport.gameObject.AddComponent<RectMask2D>();
            CanvasGroup visibility = viewport.gameObject.AddComponent<CanvasGroup>();
            CabinAtmosphere mood = viewport.gameObject.AddComponent<CabinAtmosphere>(); mood.cabin = cabin; mood.presentation = visibility;
            RectTransform cameraRig = Frame("CameraBreathingRig", viewport); cameraRig.localScale = Vector3.one * 1.02f;
            CameraBreathing breathing = cameraRig.gameObject.AddComponent<CameraBreathing>(); breathing.mood = mood;
            RectTransform motion = Frame("CabinMotion", cameraRig); CabinBob bob = motion.gameObject.AddComponent<CabinBob>(); bob.mood = mood;
            RectTransform background = Frame("Background", motion);
            Undo.SetTransformParent(art, background, "Move cabin artwork");
            art.localScale = Vector3.one; art.localRotation = Quaternion.identity;
            // Parent rectangles share the same authoring size and pivot, retaining exact hotspot coordinates.
            ((RectTransform)art).anchoredPosition = new Vector2(960, -540);
            RectTransform midground = Frame("Midground_Instruments", motion);
            Glow("MonitorWash", midground, soft, 660, 225, 545, 245, new Color(0.76f, 0.91f, 0.7f), 0.06f, mood);
            Glow("RadarWash", midground, soft, 1370, 225, 265, 265, new Color(0.67f, 0.83f, 0.76f), 0.065f, mood);
            Glow("MapWash", midground, soft, 325, 210, 255, 280, new Color(0.75f, 0.89f, 0.81f), 0.035f, mood);
            Indicator(midground, soft, 1364, 77, mood, 28);
            Indicator(midground, soft, 1610, 117, mood, 79);
            // Ambient sweep is restricted to the small physical display, not the gameplay radar scope.
            RectTransform sweepPivot = Rect("AmbientRadarPivot", midground, 1490, 348, 0, 0);
            RawImage sweep = Overlay("SoftSweep", sweepPivot, soft, -10, -83, 20, 88, new Color(0.78f, 0.88f, 0.71f, 0.065f));
            RadarSweep radarSweep = sweepPivot.gameObject.AddComponent<RadarSweep>(); radarSweep.sweepGraphic = sweep; radarSweep.mood = mood;
            RectTransform needlePivot = Rect("DecorativeConsoleNeedle", midground, 842, 554, 0, 0);
            RawImage needle = Overlay("NeedlePigment", needlePivot, soft, -1.5f, -17, 3, 21, new Color(0.19f, 0.31f, 0.37f, 0.28f));
            GaugeJitter gauge = needlePivot.gameObject.AddComponent<GaugeJitter>(); gauge.mood = mood;

            RectTransform fx = Frame("FX_WaterAndPigment", motion);
            RawImage causticImage = Overlay("Caustics", fx, caustic, -30, -30, 1980, 1140, new Color(0.81f, 0.9f, 0.8f));
            causticImage.uvRect = new Rect(0, 0, 2, 1.2f);
            CausticScroller scroller = causticImage.gameObject.AddComponent<CausticScroller>(); scroller.mood = mood;
            RawImage pigment = Overlay("WatercolorWash", fx, wash, -25, -25, 1970, 1130, new Color(0.34f, 0.46f, 0.55f));
            WatercolorBreathing pigmentBreath = pigment.gameObject.AddComponent<WatercolorBreathing>(); pigmentBreath.mood = mood;
            pigmentBreath.opacity = 0.038f; pigmentBreath.cyclesPerSecond = 0.027f;
            RawImage paperImage = Overlay("PaperTexture", fx, paper, 0, 0, 1920, 1080, new Color(0.8f, 0.72f, 0.59f, 0.045f));
            paperImage.uvRect = new Rect(0, 0, 3, 2);
            RectTransform foreground = Frame("Foreground_Particles", motion);
            Particles("CabinDust", foreground, soft, mood, new Vector2(940, 500), new Vector2(1400, 800), 0.55f, 30, new Color(0.96f, 0.91f, 0.78f), 3.5f);
            Particles("WindowMotes", foreground, soft, mood, new Vector2(470, 310), new Vector2(340, 300), 0.25f, 14, new Color(0.79f, 0.92f, 0.87f), 2.2f);
            RectTransform interaction = Frame("Interaction", motion);
            foreach (Transform target in frame.Cast<Transform>().Where(t => t.name.EndsWith("Hotspot", StringComparison.Ordinal)).ToArray())
            {
                RectTransform rect = (RectTransform)target; Vector2 anchored = rect.anchoredPosition;
                Vector3 scale = rect.localScale; Quaternion rotation = rect.localRotation;
                Undo.SetTransformParent(target, interaction, "Keep hotspot with floating cabin"); rect.anchoredPosition = anchored;
                rect.localScale = scale; rect.localRotation = rotation;
            }
            Undo.RegisterCreatedObjectUndo(viewport.gameObject, "Create atmosphere layers");
            SyncOpacity();
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene); AssetDatabase.SaveAssets();
            Selection.activeGameObject = viewport.gameObject; Undo.CollapseUndoOperations(group);
            Debug.Log("Living watercolor cabin installed. Save Zone01. Tune CabinViewport mood, CameraBreathingRig, CabinMotion and FX children in Inspector.");
        }

        private static RectTransform Frame(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(1920, 1080); return rect;
        }
        private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x + w / 2, -y - h / 2); return rect;
        }
        private static RawImage Overlay(string name, Transform parent, Texture2D texture, float x, float y, float w, float h, Color tint)
        {
            RawImage image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.color = tint; image.raycastTarget = false; return image;
        }
        private static void Glow(string name, Transform parent, Texture2D texture, float x, float y, float w, float h, Color tint, float opacity, CabinAtmosphere mood)
        {
            RawImage image = Overlay(name, parent, texture, x, y, w, h, tint);
            WatercolorBreathing glow = image.gameObject.AddComponent<WatercolorBreathing>(); glow.mood = mood; glow.screenGlow = true; glow.opacity = opacity;
            glow.phase = x * 0.01f; glow.driftPixels = Vector2.zero;
        }
        private static void Indicator(Transform parent, Texture2D texture, float x, float y, CabinAtmosphere mood, int seed)
        {
            RawImage image = Overlay("Indicator" + seed, parent, texture, x - 9, y - 9, 18, 18, new Color(0.97f, 0.78f, 0.55f));
            RandomBlink blink = image.gameObject.AddComponent<RandomBlink>(); blink.mood = mood; blink.seed = seed;
        }
        private static void Particles(string name, Transform parent, Texture2D soft, CabinAtmosphere mood, Vector2 center, Vector2 area, float rate, int maximum, Color tint, float size)
        {
            RectTransform rect = Rect(name, parent, center.x - area.x / 2, center.y - area.y / 2, area.x, area.y);
            ParticleSystem ps = rect.gameObject.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.duration = 35; main.loop = true; main.prewarm = true; main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local; main.maxParticles = maximum;
            main.startLifetime = new ParticleSystem.MinMaxCurve(24, 40); main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(size, size * 1.7f); main.startColor = tint;
            var emission = ps.emission; emission.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(area.x, area.y, 0.01f);
            var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f); velocity.y = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(0, 0);
            var color = ps.colorOverLifetime; color.enabled = true;
            Gradient fade = new(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.22f, 0.2f), new GradientAlphaKey(0.22f, 0.75f), new GradientAlphaKey(0, 1) });
            color.color = fade;
            ps.useAutoRandomSeed = false; ps.randomSeed = (uint)(maximum + 103);
            rect.GetComponent<ParticleSystemRenderer>().enabled = false;
            UIParticleAtmosphere ui = rect.gameObject.AddComponent<UIParticleAtmosphere>(); ui.softParticle = soft; ui.capacity = maximum; ui.mood = mood;
        }
        private static Texture2D Texture(string name, int size, int type)
        {
            string path = TextureFolder + "/" + name + ".png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (existing != null) return existing;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false); Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size, alpha;
                if (type == 0)
                {
                    float a = u * Mathf.PI * 2, b = v * Mathf.PI * 2;
                    float field = Mathf.Sin(2 * a + 0.65f * Mathf.Sin(3 * b)) + Mathf.Sin(3 * b + 0.55f * Mathf.Sin(2 * a)) + 0.35f * Mathf.Sin(5 * a + 4 * b);
                    alpha = Mathf.Exp(-Mathf.Abs(field) * 5);
                }
                else if (type == 1) alpha = 0.15f + Mathf.PerlinNoise(x * 0.65f, y * 0.65f) * 0.75f;
                else if (type == 2) alpha = Mathf.SmoothStep(0, 1, Mathf.PerlinNoise(u * 4 + 3.1f, v * 4 + 8.7f));
                else alpha = Mathf.Pow(Mathf.Clamp01(1 - new Vector2(u * 2 - 1, v * 2 - 1).magnitude), 2);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.wrapMode = type == 3 ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}

using System;
using System.Linq;
using G10.Prototype.Atmosphere;
using G10.Prototype.Computer;
using G10.Prototype.Feedback;
using G10.Prototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>One-time additive migration; repeated installation preserves authored definitions/tuning.</summary>
    public static class EffectDirectorSetupEditor
    {
        private const string Folder = "Assets/_Project/FeedbackEffects";
        [MenuItem("G10/Feedback/Install Unified Effect Director")]
        public static void Install()
        {
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null) throw new InvalidOperationException("Open the shared Zone01 cabin first.");
            Apply(cabin);
        }
        public static void Apply(CabinStationView cabin)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var feedback = cabin.GetComponent<CabinFeedbackController>();
            var old = feedback != null ? new SerializedObject(feedback) : null;
            T Ref<T>(string name) where T : UnityEngine.Object => old?.FindProperty(name)?.objectReferenceValue as T;
            var hull = Ref<UnityEngine.UI.RawImage>("lowHullOverlay");
            var energy = Ref<UnityEngine.UI.RawImage>("lowEnergyOverlay");
            var label = Ref<TMP_Text>("notification");
            var shake = Ref<ImpactShake>("impactShake");
            var atmosphere = Ref<CabinAtmosphere>("atmosphere");
            var rock = cabin.GetComponent<RockBreakerSequence>();
            var computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            var lab = computer != null ? computer.Apps.FirstOrDefault(a => a.id == ComputerAppId.PhotoLab)?.panel : null;
            var canvas = cabin.transform.Find("FeedbackOverlayCanvas");
            if (hull == null || energy == null || label == null || shake == null || atmosphere == null || canvas == null || lab == null)
                throw new InvalidOperationException("Existing feedback, atmosphere, Photo Lab and outer shake bindings are required. No changes made.");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install unified effects");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project", "FeedbackEffects");
            var director = Component<EffectDirector>(cabin.gameObject);
            var audio = Component<EffectAudioExecutor>(cabin.gameObject);
            var global = Surface(canvas.gameObject, EffectTarget.Global, null);
            global.overlay = Component<EffectOverlayExecutor>(canvas.gameObject);
            global.overlay.hull = hull; global.overlay.energy = energy; global.overlay.notification = label;
            global.overlay.fadeSeconds = old.FindProperty("overlayFadeSeconds").floatValue;
            global.lighting = Component<EffectLightingExecutor>(canvas.gameObject); global.lighting.atmosphere = atmosphere;
            global.shake = Component<EffectShakeExecutor>(canvas.gameObject); global.shake.shake = shake;
            var cabinSurface = Target(canvas, EffectTarget.Cabin, atmosphere.gameObject);
            cabinSurface.BindVisibility(atmosphere.gameObject, atmosphere.presentation);
            var helm = Target(canvas, EffectTarget.Helm, cabin.NavigationPanel);
            var radar = Target(canvas, EffectTarget.Radar, cabin.RadarPanel);
            var photoLab = Target(canvas, EffectTarget.PhotoLab, lab);
            var computerSurface = Target(canvas, EffectTarget.Computer, computer.gameObject);
            // Scoped dimming/blackout uses an ordinary black UI overlay, never a new camera or RT.
            foreach (var surface in new[] { global, cabinSurface, helm, radar, photoLab, computerSurface })
            {
                if (surface.overlay.tint == null) surface.overlay.tint = Tint(surface.transform);
                EditorUtility.SetDirty(surface); EditorUtility.SetDirty(surface.overlay);
            }
            var definitions = new[]
            {
                Definition(CabinEffects.Hull, "resource.hull", d => { d.overlay = EffectOverlay.Hull; d.alpha = .15f; d.overrideMood = true; d.mood = CabinMood.Danger; d.priority = 100; }),
                Definition(CabinEffects.Energy, "resource.energy", d => { d.overlay = EffectOverlay.Energy; d.alpha = .15f; d.overrideMood = true; d.mood = CabinMood.LowPower; d.priority = 50; d.clip = Ref<AudioClip>("lowPowerHum"); d.loop = true; d.gain = .35f; }),
                Definition(CabinEffects.Impact, "impact", d => { d.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Zone01/SFX/sfx_terrain_impact.wav") ?? Ref<AudioClip>("hullImpact"); d.audioCategory = G10.Prototype.Audio.AudioCategory.Impacts; d.gain = .8f; d.pitch = .94f; d.shakePixels = 7; d.shakeSeconds = .28f; d.overlay = EffectOverlay.Hull; d.alpha = .3f; d.duration = .3f / 1.3f; d.priority = 50; }),
                Definition(CabinEffects.Creak, "creak", d => { d.clip = Ref<AudioClip>("hullCreak"); d.gain = .28f; }),
                Definition(CabinEffects.Warning, "warning", d => { d.clip = Ref<AudioClip>("criticalWarning"); d.gain = .38f; }),
                Definition(CabinEffects.PowerDown, "powerdown", d => { d.clip = Ref<AudioClip>("powerDown"); d.gain = .6f; }),
                Definition(CabinEffects.Scrape, "scrape", d => { var clips = old.FindProperty("exteriorScrapes"); d.clip = clips.arraySize > 0 ? clips.GetArrayElementAtIndex(0).objectReferenceValue as AudioClip : null; d.gain = .26f; }),
                Definition(CabinEffects.Flicker, "flicker", d => { d.lightDip = .35f; d.flicker = true; d.duration = .5f; }),
                Definition(CabinEffects.Jitter, "capture.shake", d => { d.shakePixels = 1.5f; d.shakeSeconds = d.duration = .16f; d.priority = 10; }),
                Definition(CabinEffects.Notice, "notice", d => { d.duration = 2.5f; d.priority = 10; }),
                Definition(CabinEffects.RockPulse, "rock.shake", d => { d.shakePixels = 8; d.shakeSeconds = d.duration = .24f; d.priority = 100; }),
                Definition(CabinEffects.RockAudio, "rock.audio", d => { d.clip = rock != null ? rock.impactAudio : null; d.gain = .85f; d.pitch = .9f; d.priority = 100; }),
                Definition(CabinEffects.RockNotice, "rock.notice", d => { d.duration = 1; d.priority = 100; })
            };
            // Existing custom catalog entries survive re-running setup.
            var data = new SerializedObject(director);
            var prior = data.FindProperty("definitions");
            var catalog = definitions.ToList();
            for (int i = 0; i < prior.arraySize; i++)
            {
                var definition = prior.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition;
                if (definition != null && catalog.All(d => d.id != definition.id)) catalog.Add(definition);
            }
            director.Configure(catalog.ToArray(), new[] { global, cabinSurface, helm, radar, photoLab, computerSurface }, audio);
            old.FindProperty("effects").objectReferenceValue = director; old.ApplyModifiedProperties();
            if (rock != null)
            {
                var dataRock = new SerializedObject(rock); dataRock.FindProperty("effects").objectReferenceValue = director; dataRock.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(director); EditorUtility.SetDirty(global.lighting); EditorUtility.SetDirty(global.shake);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
        private static EffectDefinition Definition(string id, string channel, Action<EffectDefinition> setup)
        {
            string path = $"{Folder}/{id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<EffectDefinition>(path);
            if (definition != null) return definition;
            definition = ScriptableObject.CreateInstance<EffectDefinition>(); definition.id = id; definition.channel = channel;
            setup(definition); AssetDatabase.CreateAsset(definition, path); return definition;
        }
        private static T Component<T>(GameObject gameObject) where T : UnityEngine.Component
            => gameObject.GetComponent<T>() ?? Undo.AddComponent<T>(gameObject);
        private static EffectSurface Surface(GameObject obj, EffectTarget target, GameObject root)
        { var surface = Component<EffectSurface>(obj); surface.target = target; surface.BindVisibility(root); return surface; }
        private static EffectSurface Target(Transform canvas, EffectTarget target, GameObject root)
        {
            var child = canvas.Find(target + "Effects");
            if (child == null)
            {
                var obj = new GameObject(target + "Effects", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(obj, "Create scoped effect surface"); obj.transform.SetParent(canvas, false); child = obj.transform;
                var rect = (RectTransform)child; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            var surface = Surface(child.gameObject, target, root); surface.overlay = Component<EffectOverlayExecutor>(child.gameObject);
            return surface;
        }
        private static UnityEngine.UI.RawImage Tint(Transform parent)
        {
            var existing = parent.Find("EffectTint");
            if (existing != null) return existing.GetComponent<UnityEngine.UI.RawImage>();
            var obj = new GameObject("EffectTint", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            Undo.RegisterCreatedObjectUndo(obj, "Create effect tint"); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<UnityEngine.UI.RawImage>(); image.color = new Color(0, 0, 0, 0); image.raycastTarget = false; return image;
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
using G10.Prototype.Audio;
using G10.Prototype.Feedback;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace G10.Prototype.Editor
{
    public static class HorrorFoundationSetup
    {
        public const string Folder = "Assets/_Project/FeedbackEffects/Horror";
        private const string MixerPath = "Assets/_Project/Audio/Horror/G10Horror.mixer";
        private const string MixPath = "Assets/_Project/Resources/Audio/HorrorAudioMix.asset";
        [MenuItem("G10/Horror/Install Foundation In Open Cabin")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authoring assets.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null) throw new InvalidOperationException("Open Zone01 first.");
            EffectDirectorSetupEditor.Apply(cabin);
            CreateMixer(); CreatePresets(); RegisterSignalShader();
            var director = cabin.GetComponent<EffectDirector>();
            foreach (var surface in cabin.GetComponentsInChildren<EffectSurface>(true)) BindVisual(surface);
            var so = new SerializedObject(director); var catalog = so.FindProperty("definitions");
            var definitions = Enumerable.Range(0, catalog.arraySize).Select(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition).Where(d => d != null).ToList();
            foreach (var guid in AssetDatabase.FindAssets("t:EffectDefinition", new[] { Folder }))
            {
                var d = AssetDatabase.LoadAssetAtPath<EffectDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (!definitions.Contains(d)) definitions.Add(d);
            }
            catalog.arraySize = definitions.Count;
            for (int i = 0; i < definitions.Count; i++) catalog.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene); AssetDatabase.SaveAssets();
        }
        public static void BindVisual(EffectSurface surface)
        {
            // New screen graphics belong inside their actual window so sibling windows,
            // masks and existing sorting remain authoritative. The legacy tint adapter
            // still uses the projected feedback surface for backwards compatibility.
            var parent = surface.visibilityRoot != null ? surface.visibilityRoot.transform : surface.transform;
            var motion = surface.visual != null ? surface.visual.transform : surface.transform.Find("HorrorPresentation");
            if (motion == null) motion = parent.Find("HorrorPresentation");
            if (motion == null)
            {
                var obj = new GameObject("HorrorPresentation", typeof(RectTransform)); motion = obj.transform;
            }
            if (motion.parent != parent) motion.SetParent(parent, false);
            var rect = (RectTransform)motion; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            surface.visual = motion.GetComponent<EffectVisualExecutor>() ?? motion.gameObject.AddComponent<EffectVisualExecutor>();
            if (surface.shake == null)
            {
                surface.shake = motion.gameObject.AddComponent<EffectShakeExecutor>();
                surface.shake.shake = motion.gameObject.AddComponent<ImpactShake>();
            }
            EditorUtility.SetDirty(surface); EditorUtility.SetDirty(surface.shake);
        }
        public static void RegisterSignalShader()
        {
            var obj = new GameObject("Signal variant registration", typeof(RectTransform), typeof(Canvas));
            obj.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var visual = obj.AddComponent<EffectVisualExecutor>();
                visual.Render(ScreenInterference.ScreenSignalInterference, .5f, .2f, 147, null, 0, Vector2.zero, Vector2.zero, true);
                Canvas.ForceUpdateCanvases();
                var material = obj.GetComponentInChildren<UnityEngine.UI.RawImage>().materialForRendering;
                var settings = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Coffee.UIEffects.UIEffectProjectSettings")).First(t => t != null);
                var registry = settings.GetProperty("shaderRegistry").GetValue(null);
                registry.GetType().GetMethod("RegisterVariant", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(registry, new object[] { material, "G10/Horror" });
                var collection = (ShaderVariantCollection)settings.GetProperty("shaderVariantCollection").GetValue(null);
                EditorUtility.SetDirty(collection); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }
        public static void CreateMixer()
        {
            if (AssetDatabase.LoadAssetAtPath<HorrorAudioMix>(MixPath) != null) return;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null) mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", flags).Invoke(null, new object[] { MixerPath });
            var master = type.GetProperty("masterGroup", flags).GetValue(mixer);
            var groupType = master.GetType();
            var names = new[] { "Machinery", "UnderwaterAmbient", "Anomalies", "Interface", "Impacts" };
            var groups = Array.CreateInstance(groupType, names.Length);
            for (int i = 0; i < names.Length; i++) groups.SetValue(type.GetMethod("CreateNewGroup", flags).Invoke(mixer, new object[] { names[i], true }), i);
            groupType.GetProperty("children", flags).SetValue(master, groups);
            var snapshotsProperty = type.GetProperty("snapshots", flags);
            var initial = (Array)snapshotsProperty.GetValue(mixer);
            var normal = (AudioMixerSnapshot)initial.GetValue(0); normal.name = "Normal";
            type.GetProperty("TargetSnapshot", flags).SetValue(mixer, normal);
            type.GetMethod("CloneNewSnapshotFromTarget", flags).Invoke(mixer, new object[] { false });
            var suspense = (AudioMixerSnapshot)type.GetProperty("TargetSnapshot", flags).GetValue(mixer); suspense.name = "Suspense";
            type.GetProperty("TargetSnapshot", flags).SetValue(mixer, normal);
            type.GetMethod("CloneNewSnapshotFromTarget", flags).Invoke(mixer, new object[] { false });
            var silence = (AudioMixerSnapshot)type.GetProperty("TargetSnapshot", flags).GetValue(mixer); silence.name = "NearSilence";
            for (int i = 0; i < names.Length; i++)
            {
                var group = groups.GetValue(i); var volume = groupType.GetMethod("SetValueForVolume", flags);
                volume.Invoke(group, new object[] { mixer, normal, 0f });
                volume.Invoke(group, new object[] { mixer, suspense, i == 1 ? -4f : i == 2 ? -2f : 0f });
                volume.Invoke(group, new object[] { mixer, silence, i == 1 ? -32f : i == 2 ? -48f : 0f });
                EditorUtility.SetDirty((UnityEngine.Object)group);
            }
            type.GetProperty("TargetSnapshot", flags).SetValue(mixer, normal);
            type.GetProperty("startSnapshot", flags).SetValue(mixer, normal);
            var mix = ScriptableObject.CreateInstance<HorrorAudioMix>(); mix.mixer = mixer;
            mix.machinery = (AudioMixerGroup)groups.GetValue(0); mix.underwaterAmbient = (AudioMixerGroup)groups.GetValue(1);
            mix.anomalies = (AudioMixerGroup)groups.GetValue(2); mix.userInterface = (AudioMixerGroup)groups.GetValue(3); mix.impacts = (AudioMixerGroup)groups.GetValue(4);
            mix.normal = normal; mix.suspense = suspense; mix.nearSilence = silence;
            AssetDatabase.CreateAsset(mix, MixPath); EditorUtility.SetDirty(mixer);
            EditorUtility.SetDirty(normal); EditorUtility.SetDirty(suspense); EditorUtility.SetDirty(silence); AssetDatabase.SaveAssets();
        }
        private static EffectDefinition Definition(string id, Action<EffectDefinition> setup)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EffectDefinition>($"{Folder}/{id}.asset"); if (existing != null) return existing;
            var d = ScriptableObject.CreateInstance<EffectDefinition>(); d.id = id; d.channel = id; d.duration = 2;
            setup(d); AssetDatabase.CreateAsset(d, $"{Folder}/{id}.asset"); return d;
        }
        public static void CreatePresets()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/FeedbackEffects", "Horror");
            var scrape = AssetDatabase.LoadAssetAtPath<EffectDefinition>("Assets/_Project/FeedbackEffects/capture.scrape.asset")?.clip;
            var creak = AssetDatabase.LoadAssetAtPath<EffectDefinition>("Assets/_Project/FeedbackEffects/hull.creak.asset")?.clip;
            var knock = Definition("horror.knock", d => { d.audioCategory = AudioCategory.Anomalies; d.duration = .45f; d.gain = .24f; d.stereoPan = -.45f; });
            Definition("horror.scrape", d => { d.clip = scrape; d.audioCategory = AudioCategory.Anomalies; d.gain = .26f; d.stereoPan = -.6f; d.fadeAudio = true; d.lowPassHz = 6500; d.fadeOut = .3f; });
            Definition("light.SubtleFlicker", d => { d.lightBehavior = LightBehavior.SubtleFlicker; d.lightDip = .08f; d.duration = 3; });
            Definition("light.PowerInstability", d => { d.lightBehavior = LightBehavior.PowerInstability; d.lightDip = .35f; d.duration = 2; });
            Definition("light.ElectricalFailure", d => { d.lightBehavior = LightBehavior.ElectricalFailure; d.lightDip = .85f; d.duration = 3; });
            Definition("light.PressureReaction", d => { d.lightBehavior = LightBehavior.PressureReaction; d.lightDip = .12f; d.duration = 3; });
            Definition("ScreenSignalInterference", d => { d.target = EffectTarget.Radar; d.interference = ScreenInterference.ScreenSignalInterference; d.duration = .7f; d.interferenceIntensity = .4f; d.fadeOut = .25f; });
            Definition("AnalogSignalFailure", d => { d.target = EffectTarget.Computer; d.interference = ScreenInterference.AnalogSignalFailure; d.duration = 1.2f; d.interferenceIntensity = .6f; d.fadeOut = .5f; });
            Definition("SubtleDisplayInstability", d => { d.target = EffectTarget.Radar; d.interference = ScreenInterference.SubtleDisplayInstability; d.duration = 3; d.interferenceIntensity = .3f; });
            Definition("horror.pressure", d => { d.clip = creak; d.audioCategory = AudioCategory.Machinery; d.gain = .22f; d.shakeProfile = ShakeProfile.PressureRumble; d.shakePixels = 2; d.duration = 3; d.lightBehavior = LightBehavior.PressureReaction; d.lightDip = .1f; });
            Definition("horror.vibration", d => { d.shakeProfile = ShakeProfile.HeavyHullVibration; d.shakePixels = 3; d.duration = 1.3f; });
            Definition("horror.short-impact", d => { d.shakeProfile = ShakeProfile.ShortImpact; d.shakePixels = 3; d.shakeSeconds = d.duration = .2f; });
            Definition("horror.directional-scrape", d => { d.target = EffectTarget.Cabin; d.shakeProfile = ShakeProfile.DirectionalScrape; d.shakePixels = 1.5f; d.duration = 2; });
            Definition("horror.interface-jitter", d => { d.target = EffectTarget.Radar; d.shakeProfile = ShakeProfile.InterfaceJitter; d.shakePixels = 1; d.duration = .5f; });
            Definition("horror.silhouette", d => { d.target = EffectTarget.Cabin; d.duration = .8f; d.silhouetteOpacity = .22f; d.silhouettePosition = new Vector2(360, -20); d.fadeIn = .08f; d.fadeOut = .35f; });
            Definition("mix.suspense", d => { d.mixState = HorrorMixState.Suspense; d.duration = 3.5f; d.audioFade = .45f; });
            Definition("mix.near-silence", d => { d.mixState = HorrorMixState.NearSilence; d.duration = 3; d.audioFade = .6f; });
            Sequence("ThreeMetallicKnocks", new[] { Cue(knock, 0, .45f), Cue(knock, .7f, .45f), Cue(knock, 1.55f, .45f) });
            Sequence("CombinedHorror", new[] {
                Cue(Load("mix.suspense"), 0, 3.5f, EffectPhase.Preparation),
                Cue(Load("horror.scrape"), .45f, 1.55f, EffectPhase.Disturbance),
                Cue(Load("light.PowerInstability"), 1.2f, 1.6f, EffectPhase.Disturbance),
                Cue(Load("ScreenSignalInterference"), 1.35f, .5f, EffectPhase.Peak),
                Cue(Load("horror.interface-jitter"), 1.55f, .45f, EffectPhase.Peak),
                Cue(Load("SubtleDisplayInstability"), 2, 1.5f, EffectPhase.Recovery) });
            AssetDatabase.SaveAssets();
        }
        public static EffectDefinition Load(string id) => AssetDatabase.LoadAssetAtPath<EffectDefinition>($"{Folder}/{id}.asset");
        private static EffectCue Cue(EffectDefinition effect, float offset, float duration, EffectPhase phase = EffectPhase.Disturbance)
            => new EffectCue { effect = effect, offset = offset, duration = duration, phase = phase };
        private static void Sequence(string id, EffectCue[] cues)
        {
            var path = $"{Folder}/{id}.asset"; if (AssetDatabase.LoadAssetAtPath<EffectSequence>(path) != null) return;
            var sequence = ScriptableObject.CreateInstance<EffectSequence>(); sequence.id = id; sequence.cues = cues;
            AssetDatabase.CreateAsset(sequence, path);
        }
    }
}

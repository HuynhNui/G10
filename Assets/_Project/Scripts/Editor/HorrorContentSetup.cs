using System;
using System.Linq;
using G10.Prototype.Audio;
using G10.Prototype.Feedback;
using UnityEditor;
using UnityEngine;

namespace G10.Prototype.Editor
{
    public static class HorrorContentSetup
    {
        public const string SpritePath = "Assets/_Project/Art/Horror/TemporaryCrewSilhouette.png";
        public static Material SignalMaterial()
        {
            const string path = "Assets/_Project/Shaders/HorrorDisplay.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("G10/UI/HorrorDisplay");
                if (shader == null) throw new InvalidOperationException("HorrorDisplay shader is unavailable.");
                material = new Material(shader); AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
        private static AudioClip Clip(string id) => AssetDatabase.LoadAssetAtPath<AudioClip>(
            $"Assets/_Project/Audio/Horror/{(id.StartsWith("ambient_") ? "Ambience" : "Anomalies")}/{id}.wav");
        private static EffectDefinition Set(string id, Action<EffectDefinition> configure)
        {
            var d = HorrorFoundationSetup.Load(id);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<EffectDefinition>(); d.id = d.channel = id;
                AssetDatabase.CreateAsset(d, $"{HorrorFoundationSetup.Folder}/{id}.asset");
            }
            Undo.RecordObject(d, "Bind horror content"); configure(d); EditorUtility.SetDirty(d); return d;
        }
        [MenuItem("G10/Horror/Bind Generated Content And Polished Presets")]
        public static void BindContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            HorrorFoundationSetup.CreatePresets();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[]{"Assets/_Project/Audio/Horror"}))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".wav")) continue;
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true; importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            var knock = Set("horror.knock", d => { d.clip = Clip("horror_knock_three"); d.duration = 4; d.gain = .7f; d.stereoPan = -.25f; });
            Set("horror.scrape", d => { d.clip = Clip("horror_hull_scraping"); d.duration = 6; d.gain = .85f; d.fadeIn = .1f; d.fadeOut = .5f; d.lowPassHz = 16000; });
            foreach (string id in new[]{"light.SubtleFlicker","light.PowerInstability","light.ElectricalFailure","light.PressureReaction","ScreenSignalInterference","AnalogSignalFailure","SubtleDisplayInstability"})
                Set(id, d => { d.clip = Clip("horror_electrical_interference"); d.gain = id.StartsWith("light.") ? .16f : .4f; d.audioCategory = AudioCategory.Interface; d.fadeAudio = true; d.fadeIn = .06f; d.fadeOut = .25f; d.duration = 3; });
            Set("light.SubtleFlicker", d => { d.lightDip = .48f; d.lightBehavior = LightBehavior.SubtleFlicker; });
            Set("light.PowerInstability", d => d.lightDip = .52f);
            Set("ScreenSignalInterference", d => { d.interferenceIntensity = .9f; d.duration = 2.2f; });
            Set("AnalogSignalFailure", d => { d.interferenceIntensity = 1; d.duration = 2.4f; });
            Set("SubtleDisplayInstability", d => { d.interferenceIntensity = .35f; d.gain = .14f; });
            Set("horror.pressure", d => { d.clip = Clip("horror_pressure_groan"); d.duration = 7; d.gain = .72f; d.shakePixels = 2.8f; d.shakeProfile = ShakeProfile.PressureRumble; d.lightDip = .24f; d.instability = 1.8f; d.fadeAudio = true; d.fadeIn = .12f; d.fadeOut = .6f; });
            Set("horror.silhouette", d => { d.clip = Clip("horror_unidentified_breathing"); d.audioCategory = AudioCategory.Anomalies; d.gain = .3f; d.silhouette = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); d.duration = .7f; d.silhouetteOpacity = .72f; d.silhouetteSize = new Vector2(400, 650); d.silhouettePosition = new Vector2(730, -170); d.fadeIn = .07f; d.fadeOut = .25f; });
            Set("horror.distant-impact", d => { d.clip = Clip("horror_distant_impact"); d.audioCategory = AudioCategory.Impacts; d.duration = 5; d.gain = .75f; });
            Set("horror.movement", d => { d.audioCategory = AudioCategory.Anomalies; d.clip = Clip("horror_pressure_groan"); d.gain = .16f; d.duration = 1; d.fadeAudio = true; d.fadeIn = .1f; d.fadeOut = .4f; });
            foreach (var id in new[]{"ambient_singing_algae","ambient_ancient_roots","ambient_ruins","ambient_deep_abyss"})
                Set(id, d => { d.clip = Clip(id); d.loop = true; d.audioCategory = AudioCategory.UnderwaterAmbient; d.gain = .65f; d.audioFade = 1.4f; });
            Sequence("ThreeMetallicKnocks", new[]{Cue(knock.id, 0, 4)});
            Sequence("CombinedHorror", new[]{
                Cue("mix.suspense", .8f, 5.7f), Cue("horror.movement", 1.5f, 1),
                Cue("horror.scrape", 2.2f, 6), Cue("light.PowerInstability", 2.7f, 2.3f),
                Cue("ScreenSignalInterference", 3.5f, 1.15f), Cue("AnalogSignalFailure", 3.65f, 1.2f),
                Cue("horror.distant-impact", 4.2f, 5), Cue("horror.vibration", 4.25f, .8f),
                Cue("horror.silhouette", 4.5f, .7f)});
            AssetDatabase.SaveAssets();
        }
        private static EffectCue Cue(string id, float offset, float duration) => new()
        { effect = HorrorFoundationSetup.Load(id), offset = offset, duration = duration, phase = offset >= 4.2f ? EffectPhase.Peak : EffectPhase.Disturbance };
        private static void Sequence(string id, EffectCue[] cues)
        {
            var path = $"{HorrorFoundationSetup.Folder}/{id}.asset";
            var sequence = AssetDatabase.LoadAssetAtPath<EffectSequence>(path);
            if (sequence == null) { sequence = ScriptableObject.CreateInstance<EffectSequence>(); AssetDatabase.CreateAsset(sequence, path); }
            Undo.RecordObject(sequence, "Polish horror sequence"); sequence.id = id; sequence.cues = cues; EditorUtility.SetDirty(sequence);
        }
    }
}

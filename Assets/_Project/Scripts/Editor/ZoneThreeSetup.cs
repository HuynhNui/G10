using System;
using System.Linq;
using G10.Prototype.Audio;
using G10.Prototype.Computer;
using G10.Prototype.Feedback;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>Idempotent authoring of production assets on the existing shared cabin.</summary>
    public static class ZoneThreeSetup
    {
        public const string Folder = "Assets/_Project/FeedbackEffects/Zone03";
        public const string AudioFolder = "Assets/_Project/Audio/Horror/Zone03";
        private static EffectDefinition Def(string id, Action<EffectDefinition> setup)
        {
            string path = Folder + "/" + id + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<EffectDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<EffectDefinition>(); AssetDatabase.CreateAsset(d, path); }
            Undo.RecordObject(d, "Author Zone 3 effect"); d.id = "z3." + id; d.channel = d.id;
            d.priority = 35; d.target = EffectTarget.Global; setup(d); EditorUtility.SetDirty(d); return d;
        }
        private static EffectCue Cue(EffectDefinition d, float at, float duration, EffectPhase phase = EffectPhase.Disturbance) =>
            new() { effect = d, offset = at, duration = duration, phase = phase };
        private static EffectSequence Sequence(string id, params EffectCue[] cues)
        {
            string path = Folder + "/" + id + ".asset";
            var s = AssetDatabase.LoadAssetAtPath<EffectSequence>(path);
            if (s == null) { s = ScriptableObject.CreateInstance<EffectSequence>(); AssetDatabase.CreateAsset(s, path); }
            s.id = "z3." + id; s.channel = "z3.sequence." + id; s.priority = 45; s.cues = cues;
            EditorUtility.SetDirty(s); return s;
        }
        [MenuItem("G10/Zone 3/Install Cinematic Production")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.path != "Assets/_Project/Scenes/Gameplay/Zone01.unity")
                throw new InvalidOperationException("Open the shared Zone01 cabin scene first.");
            if (cabin.gameObject.scene.isDirty) throw new InvalidOperationException("Save authored cabin changes before installing Zone 3.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/FeedbackEffects", "Zone03");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install Zone 3 cinematic production");
            // Mission synchronization is explicit and preserves retained geometry/art/presentation.
            MissionLayoutEditor.ConfigureAssets(); MissionLayoutEditor.ConfigureMarkers(cabin.gameObject.scene);
            var presentation = cabin.GetComponent<ZoneThreePresentationController>() ?? Undo.AddComponent<ZoneThreePresentationController>(cabin.gameObject);
            presentation.cabin = cabin; presentation.effects = cabin.GetComponent<EffectDirector>();
            var one = cabin.GetComponent<ZoneOneAudioController>(); presentation.radarClip = one.radarClip; presentation.shutterClip = one.shutterClip;
            var names = new[] { "ambient_stone", "stone_resonance_01", "stone_resonance_02", "stone_resonance_03", "engine_reflection",
                "creature05_pressure", "creature06_crystal_motion", "creature07_bioelectric", "false_radar_echo", "soft_movement", "close_click",
                "rock_breaker_assembly", "underwater_detonation", "rock_collapse_01", "rock_collapse_02", "rock_collapse_03", "post_explosion_resonance", "sparse_water" };
            var audio = names.ToDictionary(n => n, n =>
            {
                string path = AudioFolder + "/" + n + ".wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) throw new InvalidOperationException("Missing production audio: " + path);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path); var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true; importer.defaultSampleSettings = settings; importer.SaveAndReimport();
                return Def("audio." + n, d =>
                {
                    d.clip = clip; d.duration = clip.length; d.loop = n == "ambient_stone" || n == "sparse_water";
                    d.audioFade = d.loop ? 1.5f : .08f; d.gain = .70f;
                    d.audioCategory = n == "rock_breaker_assembly" || n == "close_click" ? AudioCategory.Machinery :
                        n == "false_radar_echo" ? AudioCategory.Interface : n.StartsWith("rock_collapse") || n == "underwater_detonation" ? AudioCategory.Impacts :
                        n == "soft_movement" || n == "engine_reflection" ? AudioCategory.Anomalies : AudioCategory.UnderwaterAmbient;
                    if (n.StartsWith("stone_resonance")) { d.stereoPan = n.EndsWith("01") ? -.5f : n.EndsWith("02") ? .5f : -.15f; d.lowPassHz = 5800; d.reverb = .12f; }
                    if (n == "soft_movement") d.stereoPan = -.42f;
                    if (n == "engine_reflection") d.gain = .85f;
                    if (d.loop) d.gain = .70f;
                });
            });
            EffectDefinition Visual(string id, EffectTarget target, ScreenInterference mode, float strength, float fadeIn = .05f, float fadeOut = .2f) =>
                Def("visual." + id, d => { d.target = target; d.interference = mode; d.interferenceIntensity = strength; d.fadeIn = fadeIn; d.fadeOut = fadeOut; });
            Visual("stone", EffectTarget.Helm, ScreenInterference.StoneDepth, .8f);
            var radar = Visual("radar", EffectTarget.Radar, ScreenInterference.FalseRadarSweep, .95f, .04f, .4f);
            var analog = Visual("analog", EffectTarget.Reflection, ScreenInterference.AnalogSignalFailure, .75f, .02f, .15f);
            var shadow = Visual("reflection", EffectTarget.Reflection, ScreenInterference.ProceduralReflection, 1, .18f, .45f);
            Visual("photo-reflection", EffectTarget.PhotoLab, ScreenInterference.ProceduralReflection, .22f, .4f, .6f).duration = 1.8f;
            var electric = Visual("bioelectric", EffectTarget.PhotoLab, ScreenInterference.BioelectricPulse, .85f, .02f, .2f);
            var debris = Visual("debris", EffectTarget.Global, ScreenInterference.RockDebris, .9f, .1f, 1.8f);
            var darkness = Def("mix.void", d => { d.mixState = HorrorMixState.Suspense; d.audioFade = 2.5f; d.priority = 50; });
            var pressure = Def("visual.pressure", d =>
            {
                d.overlay = EffectOverlay.Tint; d.tintColor = new(.11f, .17f, .19f); d.alpha = .32f;
                d.decayOverlay = false;
                d.fadeIn = .01f; d.fadeOut = .06f;
                d.shakePixels = 5; d.shakeSeconds = .28f; d.shakeProfile = ShakeProfile.ShortImpact;
                d.shakeDirection = new(1, .18f); d.seed = 301;
            });
            var fracture = Def("visual.fracture", d => { d.shakePixels = 3; d.shakeSeconds = .18f; d.shakeProfile = ShakeProfile.ShortImpact; d.seed = 310; d.shakeDirection = new(-.35f, 1); });
            var settling = Def("visual.settling", d => { d.shakePixels = 1.5f; d.shakeSeconds = .15f; d.shakeProfile = ShakeProfile.ShortImpact; d.seed = 330; });
            var installLight = Def("visual.install", d => { d.target = EffectTarget.Computer; d.overlay = EffectOverlay.Tint; d.alpha = .10f; d.tintColor = new(.30f, .19f, .055f); });
            presentation.delayedEngine = Sequence("DelayedEngine", Cue(audio["engine_reflection"], 0, audio["engine_reflection"].clip.length));
            presentation.falseRadar = Sequence("FalseRadar", Cue(audio["false_radar_echo"], .22f, audio["false_radar_echo"].clip.length), Cue(radar, .18f, 2.8f));
            presentation.reflection = Sequence("Reflection",
                Cue(darkness, 0, 6.8f, EffectPhase.Preparation), Cue(analog, 2, .65f), Cue(shadow, 2.9f, 2.1f, EffectPhase.Peak),
                Cue(audio["soft_movement"], 3, audio["soft_movement"].clip.length), Cue(audio["close_click"], 4.9f, audio["close_click"].clip.length, EffectPhase.Recovery));
            presentation.assembly = Sequence("Assembly", Cue(audio["rock_breaker_assembly"], 0, audio["rock_breaker_assembly"].clip.length),
                Cue(installLight, .8f, .25f), Cue(installLight, 2.1f, .20f));
            presentation.specimen07 = Sequence("Bioelectric", Cue(audio["creature07_bioelectric"], 0, audio["creature07_bioelectric"].clip.length), Cue(electric, 0, 2.1f));
            presentation.demolition = Sequence("Demolition", Cue(audio["underwater_detonation"], 0, Math.Min(8, audio["underwater_detonation"].clip.length), EffectPhase.Peak),
                Cue(pressure, 0, .28f, EffectPhase.Peak), Cue(fracture, .65f, .18f), Cue(audio["rock_collapse_01"], .7f, 3),
                Cue(fracture, 1.5f, .18f), Cue(audio["rock_collapse_02"], 1.5f, 3.4f), Cue(debris, .7f, 8),
                Cue(audio["rock_collapse_03"], 2.9f, 3), Cue(settling, 3.15f, .15f), Cue(audio["rock_collapse_01"], 4.6f, 2.5f),
                Cue(audio["post_explosion_resonance"], 6, audio["post_explosion_resonance"].clip.length, EffectPhase.Recovery));

            var director = presentation.effects; var so = new SerializedObject(director); var definitions = so.FindProperty("definitions");
            var catalog = Enumerable.Range(0, definitions.arraySize).Select(i => definitions.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition)
                .Where(d => d != null && !d.id.StartsWith("z3.")).Concat(AssetDatabase.FindAssets("t:EffectDefinition", new[] { Folder }).Select(g => AssetDatabase.LoadAssetAtPath<EffectDefinition>(AssetDatabase.GUIDToAssetPath(g)))).ToArray();
            var surfaceProperty = so.FindProperty("surfaces");
            var surfaces = Enumerable.Range(0, surfaceProperty.arraySize).Select(i => surfaceProperty.GetArrayElementAtIndex(i).objectReferenceValue as EffectSurface).Where(s => s != null).ToArray();
            var minimap = cabin.NavigationPanel.transform.Find("MiniMap").gameObject;
            var scope = cabin.RadarPanel.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "RadarScope").gameObject;
            var lab = cabin.GetComponentInChildren<PhotoLabView>(true);
            ZoneThreeVisualSetup.Bind(director.Surface(EffectTarget.Helm), minimap);
            ZoneThreeVisualSetup.Bind(director.Surface(EffectTarget.Reflection), minimap);
            ZoneThreeVisualSetup.Bind(director.Surface(EffectTarget.Radar), scope, true);
            ZoneThreeVisualSetup.Bind(director.Surface(EffectTarget.PhotoLab), lab.preview.gameObject);
            ZoneThreeVisualSetup.Bind(director.Surface(EffectTarget.Global), null);
            director.Configure(catalog, surfaces, cabin.GetComponent<EffectAudioExecutor>());
            var rock = cabin.GetComponent<RockBreakerSequence>(); rock.productionSequence = presentation.demolition;
            foreach (var asset in new UnityEngine.Object[] { presentation, director, rock }) EditorUtility.SetDirty(asset);
            ZoneThreeVisualSetup.WarmUp(); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene); EditorSceneManager.SaveScene(cabin.gameObject.scene);
        }
        private static void Preview(ZoneThreeDemo demo)
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<ZoneThreePresentationController>();
            if (!EditorApplication.isPlaying || controller == null || !controller.InZone) throw new InvalidOperationException("Enter real Zone 3 gameplay first.");
            if (demo == ZoneThreeDemo.FalseRadar) controller.cabin.OpenRadar(); else controller.cabin.OpenNavigation();
            controller.Preview(demo);
        }
        [MenuItem("G10/Zone 3/Preview A - Engine Reflection")] private static void A() => Preview(ZoneThreeDemo.DelayedEngine);
        [MenuItem("G10/Zone 3/Preview B - False Radar")] private static void B() => Preview(ZoneThreeDemo.FalseRadar);
        [MenuItem("G10/Zone 3/Preview C - Reflection")] private static void C() => Preview(ZoneThreeDemo.Reflection);
        [MenuItem("G10/Zone 3/Preview D - Demolition")] private static void D() => Preview(ZoneThreeDemo.Demolition);
    }
}

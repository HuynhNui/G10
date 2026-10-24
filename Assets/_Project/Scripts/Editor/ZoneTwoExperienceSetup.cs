using System;
using G10.Prototype.Feedback;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>Narrow presentation authoring. Does not call mission/map/content installers.</summary>
    public static class ZoneTwoExperienceSetup
    {
        private const string Folder = ZoneTwoSetup.Folder;
        private static EffectDefinition Definition(string name) => AssetDatabase.LoadAssetAtPath<EffectDefinition>(Folder + "/" + name + ".asset");
        private static EffectDefinition Create(string name, Action<EffectDefinition> configure)
        {
            var value = Definition(name);
            if (value == null) { value = ScriptableObject.CreateInstance<EffectDefinition>(); AssetDatabase.CreateAsset(value, Folder + "/" + name + ".asset"); }
            Undo.RecordObject(value, "Zone02 experience presentation");
            value.id = "z2." + name; value.channel = value.id; value.target = EffectTarget.Global;
            configure(value); EditorUtility.SetDirty(value); AssetDatabase.SaveAssetIfDirty(value); return value;
        }
        private static EffectCue Cue(EffectDefinition effect, float offset, float duration) => new() { effect = effect, offset = offset, duration = duration };
        private static EffectSequence Sequence(string name, params EffectCue[] cues)
        {
            string path = Folder + "/" + name + ".asset";
            var value = AssetDatabase.LoadAssetAtPath<EffectSequence>(path);
            if (value == null) { value = ScriptableObject.CreateInstance<EffectSequence>(); AssetDatabase.CreateAsset(value, path); }
            Undo.RecordObject(value, "Zone02 experience sequence"); value.id = "z2." + name; value.channel = "z2.sequence";
            value.priority = 40; value.cues = cues; EditorUtility.SetDirty(value); AssetDatabase.SaveAssetIfDirty(value); return value;
        }
        [MenuItem("G10/Zone 2/Install Experience Improvement (Presentation Only)")]
        public static void InstallPresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.isDirty) throw new InvalidOperationException("Open the clean shared Zone01 cabin first.");
            var controller = cabin.GetComponent<ZoneTwoPresentationController>();
            if (controller == null) throw new InvalidOperationException("Existing Zone02 presentation is required.");
            var prep = Create("audio.preparation", d =>
            {
                d.clip = Definition("audio.root_friction").clip; d.duration = .7f; d.gain = .20f;
                d.lowPassHz = 1500; d.stereoPan = -.28f; d.audioCategory = Audio.AudioCategory.UnderwaterAmbient;
                d.fadeAudio = true; d.fadeIn = .25f; d.fadeOut = .15f;
            });
            const string tailPath = "Assets/_Project/Audio/Zone02/following_tail.wav";
            var importer = AssetImporter.GetAtPath(tailPath) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Import the licensed offline following-tail derivative first.");
            var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM; importer.defaultSampleSettings = settings; importer.SaveAndReimport();
            var tail = Create("audio.following_tail", d =>
            {
                d.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(tailPath); d.duration = d.clip.length;
                d.gain = .65f; d.audioCategory = Audio.AudioCategory.Anomalies; d.stereoPan = .25f; d.lowPassHz = 2400;
            });
            controller.following = Sequence("Following", Cue(tail, .2f, 3.4f));
            controller.radarDisturbance = Sequence("RadarDisturbance", Cue(prep, 0, .7f),
                Cue(Definition("audio.radar_static"), .7f, .5f), Cue(Definition("screen.radar"), .7f, .5f),
                Cue(Definition("screen.dip"), .75f, .18f), Cue(Definition("audio.outside_movement"), 1.2f, 2));
            var signal = Definition("screen.computer"); Undo.RecordObject(signal, "Readable Zone02 escalation"); signal.interferenceIntensity = .38f;
            EditorUtility.SetDirty(signal); AssetDatabase.SaveAssetIfDirty(signal);
            foreach (var pair in new[] { ("screen.flash-one", .23f), ("screen.flash-two", .35f), ("screen.reflection-dip", .38f) })
            { var d = Definition(pair.Item1); Undo.RecordObject(d, "Zone02 restrained dip"); d.alpha = pair.Item2; EditorUtility.SetDirty(d); AssetDatabase.SaveAssetIfDirty(d); }
            var shadow = Definition("screen.reflection");
            EffectDefinition Step(string name, Vector2 offset) => Create(name, d =>
            { d.target = EffectTarget.Reflection; d.silhouette = shadow.silhouette; d.silhouetteSize = shadow.silhouetteSize;
                d.silhouettePosition = shadow.silhouettePosition + offset; d.silhouetteOpacity = .30f; d.fadeIn = .01f; d.fadeOut = .035f; });
            var middle = Step("screen.reflection-middle", new Vector2(2, 1));
            var last = Step("screen.reflection-last", new Vector2(4, 2));
            controller.reflection = Sequence("Reflection", Cue(Definition("screen.reflection-dip"), .75f, .8f),
                Cue(Definition("audio.electric_fizz"), .75f, .65f), Cue(shadow, .9f, .22f), Cue(middle, 1.12f, .24f), Cue(last, 1.36f, .27f));
            string materialPath = Folder + "/PhotoEdgeMotion.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("G10/UI/ZoneTwoWaterMemory");
                if (shader == null) throw new InvalidOperationException("Import the native ZoneTwoWaterMemory shader first.");
                material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath);
            }
            controller.photoMotionTemplate = material;
            // Append only the new definitions while retaining the exact existing catalog/surfaces.
            var serialized = new SerializedObject(controller.effects); var catalog = serialized.FindProperty("definitions");
            foreach (var value in new[] { prep, tail, middle, last })
            {
                bool found = false;
                for (int i = 0; i < catalog.arraySize; i++) if (catalog.GetArrayElementAtIndex(i).objectReferenceValue == value) { found = true; break; }
                if (!found) { int index = catalog.arraySize; catalog.InsertArrayElementAtIndex(index); catalog.GetArrayElementAtIndex(index).objectReferenceValue = value; }
            }
            serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene); EditorSceneManager.SaveScene(cabin.gameObject.scene);
        }
    }
}

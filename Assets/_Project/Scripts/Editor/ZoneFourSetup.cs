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
    /// <summary>Targeted Zone4 authoring. Never rebuilds mission assets, the cabin, or creature art.</summary>
    public static class ZoneFourSetup
    {
        public const string Folder = "Assets/_Project/FeedbackEffects/Zone04";
        public const string AudioFolder = "Assets/_Project/Audio/Horror/Zone04";
        private static EffectSequence Sequence(string name, EffectTarget target, params EffectCue[] cues)
        {
            string path = Folder + "/" + name + ".asset";
            var sequence = AssetDatabase.LoadAssetAtPath<EffectSequence>(path);
            if (sequence == null) { sequence = ScriptableObject.CreateInstance<EffectSequence>(); AssetDatabase.CreateAsset(sequence, path); }
            Undo.RecordObject(sequence, "Author Zone4 sequence"); sequence.id = "z4." + name; sequence.channel = "z4.sequence." + name;
            sequence.target = target; sequence.priority = 45; sequence.cues = cues; EditorUtility.SetDirty(sequence); return sequence;
        }
        private static EffectCue Cue(EffectDefinition d, float offset = 0, float? duration = null) =>
            new() { effect = d, offset = offset, duration = duration ?? d.duration };
        private static EffectDefinition Definition(string name, Action<EffectDefinition> configure)
        {
            string path = Folder + "/" + name + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<EffectDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<EffectDefinition>(); AssetDatabase.CreateAsset(d, path); }
            Undo.RecordObject(d, "Author Zone4 presentation"); d.id = "z4." + name; d.channel = d.id;
            d.priority = 45; configure(d); EditorUtility.SetDirty(d); return d;
        }
        [MenuItem("G10/Zone 4/Install Final Chapter")]
        public static void InstallFinalChapter()
        {
            const string endingPath = "Assets/_Project/Scenes/Ending/Ending.unity";
            var ending = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(endingPath);
            if (ending.IsValid() && ending.isDirty) throw new InvalidOperationException("Save the existing Ending scene changes before installation.");
            InstallHiddenScenes();
            var controller = UnityEngine.Object.FindAnyObjectByType<ZoneFourPresentationController>();
            var clock = Definition("clock.ending", d => { d.target = EffectTarget.Global; d.duration = 16; });
            var haze = Definition("visual.final-water", d =>
            {
                d.target = EffectTarget.Helm; d.overlay = EffectOverlay.Tint; d.tintColor = new Color(.035f, .10f, .14f);
                d.alpha = .075f; d.decayOverlay = false; d.duration = 9;
            });
            Undo.RecordObject(controller, "Bind final approach");
            controller.finalSignal = Sequence("FinalSignal", EffectTarget.Helm,
                Cue(controller.effects.Definition("z4.audio.final_signal"), 1.2f), Cue(haze, 0, 9));
            // The ending owns a separate director: cabin feedback intentionally suspends on ending publication.
            bool opened = !ending.IsValid() || !ending.isLoaded;
            if (opened) ending = EditorSceneManager.OpenScene(endingPath, OpenSceneMode.Additive);
            var canvas = ending.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).First();
            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Author both endings");
            var presentation = canvas.GetComponent<EndingPresentation>() ?? Undo.AddComponent<EndingPresentation>(canvas.gameObject);
            var audio = canvas.GetComponent<EffectAudioExecutor>() ?? Undo.AddComponent<EffectAudioExecutor>(canvas.gameObject);
            var director = canvas.GetComponent<EffectDirector>() ?? Undo.AddComponent<EffectDirector>(canvas.gameObject);
            var surface = canvas.GetComponent<EffectSurface>() ?? Undo.AddComponent<EffectSurface>(canvas.gameObject);
            surface.target = EffectTarget.Global; surface.BindVisibility(canvas.gameObject);
            presentation.effects = director;
            presentation.presentationFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset");
            if (presentation.presentationFont == null) throw new InvalidOperationException("Use the existing Vietnamese-capable presentation font.");
            presentation.surveyConfigs = Enumerable.Range(1, 4).Select(i => AssetDatabase.LoadAssetAtPath<G10.Prototype.Missions.ZoneMissionConfig>($"Assets/_Project/Content/Missions/Zone{i:00}.asset")).ToArray();
            var normal = controller.effects.Definition("z4.audio.normal_ending_settle");
            var signal = controller.effects.Definition("z4.audio.final_signal");
            var tail = controller.effects.Definition("z4.audio.hidden_ending_tail");
            presentation.normalSequence = Sequence("NormalEnding", EffectTarget.Global, Cue(normal), Cue(clock, 0, 12));
            presentation.hiddenSequence = Sequence("HiddenEnding", EffectTarget.Global, Cue(signal, 2), Cue(tail, 6), Cue(clock, 0, 16));
            director.Configure(new[] { normal, signal, tail, clock }, new[] { surface }, audio);
            var cabinDirector = controller.effects; var serialized = new SerializedObject(cabinDirector);
            var catalog = serialized.FindProperty("definitions"); var surfaces = serialized.FindProperty("surfaces");
            cabinDirector.Configure(Enumerable.Range(0, catalog.arraySize).Select(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition)
                .Where(d => d != null && d.id != haze.id).Append(haze).ToArray(),
                Enumerable.Range(0, surfaces.arraySize).Select(i => surfaces.GetArrayElementAtIndex(i).objectReferenceValue as EffectSurface).ToArray(), controller.GetComponent<EffectAudioExecutor>());
            foreach (var item in new UnityEngine.Object[] { presentation, audio, director, surface, controller, cabinDirector }) EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(ending); EditorSceneManager.SaveScene(ending);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene); EditorSceneManager.SaveScene(controller.gameObject.scene);
            if (opened) EditorSceneManager.CloseScene(ending, true);
        }
        [MenuItem("G10/Zone 4/Preview D - Final Signal on Helm")]
        private static void FinalSignal()
        {
            var controller = ActiveController(); controller.cabin.OpenNavigation();
            if (!controller.Preview(ZoneFourDemo.FinalSignal).IsValid) throw new InvalidOperationException("The real helm must be visible.");
        }
        private static void PreviewEnding(string id)
        {
            var view = UnityEngine.Object.FindAnyObjectByType<EndingPresentation>();
            if (!EditorApplication.isPlaying || view == null || view.gameObject.scene.name != "Ending" || Time.timeScale <= 0)
                throw new InvalidOperationException("Open the actual Ending scene in Play Mode; preview never changes the saved ending.");
            view.Show(id);
        }
        [MenuItem("G10/Zone 4/Preview E1 - Normal on Ending Canvas")] private static void NormalEnding() => PreviewEnding("normal");
        [MenuItem("G10/Zone 4/Preview E2 - Hidden on Ending Canvas")] private static void HiddenEnding() => PreviewEnding("hidden");
        [MenuItem("G10/Zone 4/Install Hidden Location Scenes")]
        public static void InstallHiddenScenes()
        {
            InstallAudio();
            var controller = UnityEngine.Object.FindAnyObjectByType<ZoneFourPresentationController>();
            var a = controller.effects.Definition("z4.audio.hidden_signal_a"); var b = controller.effects.Definition("z4.audio.hidden_signal_b");
            Undo.RecordObject(controller, "Bind Zone4 hidden observation scenes");
            controller.hiddenA = Sequence("HiddenA", EffectTarget.PhotoLab, Cue(a));
            controller.hiddenB = Sequence("HiddenB", EffectTarget.PhotoLab, Cue(b));
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene); EditorSceneManager.SaveScene(controller.gameObject.scene);
        }
        private static void PreviewHidden(ZoneFourDemo demo)
        {
            var controller = ReviewPhoto(demo == ZoneFourDemo.HiddenA ? "zone04-l3" : "zone04-l5");
            if (!controller.Preview(demo).IsValid) throw new InvalidOperationException("Open the actual Photo Lab with a selected photograph; close pause/modal windows.");
        }
        private static ZoneFourPresentationController ActiveController()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<ZoneFourPresentationController>();
            if (!EditorApplication.isPlaying || controller == null || !controller.InZone)
                throw new InvalidOperationException("Enter real Zone4 gameplay first.");
            if (Time.timeScale <= 0 || PauseMenuController.Instance?.IsPaused == true ||
                G10.Prototype.Core.SceneFlowController.Instance?.IsTransitioning == true || controller.cabin.Panels?.IsModalOpen == true)
                throw new InvalidOperationException("Resume gameplay and close modal windows before review.");
            return controller;
        }
        private static ZoneFourPresentationController ReviewPhoto(string poi)
        {
            var controller = ActiveController(); var repository = controller.GetComponent<PhotoCaptureService>();
            var photo = repository.Photos.LastOrDefault(p => p.MissionZoneId == "Zone04" && p.MissionPoiId == poi);
            if (photo == null) throw new InvalidOperationException("First take a real valid photograph at " + poi + "; review does not fabricate or charge a photograph.");
            controller.cabin.OpenComputer(); var computer = controller.GetComponentInChildren<ComputerScreenController>(true); computer.OpenPhotoLab();
            var lab = controller.GetComponentInChildren<PhotoLabView>(true);
            for (int i = 0; i < repository.Photos.Count; i++) lab.Previous();
            for (int i = 0; i < repository.Photos.Count && lab.SelectedPhotoId != photo.Id; i++) lab.Next();
            return controller;
        }
        [MenuItem("G10/Zone 4/Review A - Saved Wreck Photograph")] private static void Wreck() => ReviewPhoto("zone04-l1");
        [MenuItem("G10/Zone 4/Preview B - Recovered Fragment")]
        private static void Fragment()
        {
            var controller = ActiveController(); controller.cabin.OpenComputer();
            controller.GetComponentInChildren<ComputerScreenController>(true).OpenMissionLog();
            if (!controller.PreviewFragment().IsValid) throw new InvalidOperationException("Complete actual L1 capture first; preview grants no hint or discovery.");
        }
        [MenuItem("G10/Zone 4/Preview C1 - Hidden A on Photo Lab")] private static void HiddenA() => PreviewHidden(ZoneFourDemo.HiddenA);
        [MenuItem("G10/Zone 4/Preview C2 - Hidden B on Photo Lab")] private static void HiddenB() => PreviewHidden(ZoneFourDemo.HiddenB);
        [MenuItem("G10/Zone 4/Install Audio Production")]
        public static void InstallAudio()
        {
            InstallVerticalSlice();
            var controller = UnityEngine.Object.FindAnyObjectByType<ZoneFourPresentationController>();
            var one = controller.GetComponent<ZoneOneAudioController>();
            Undo.RecordObject(controller, "Bind Zone4 audio ownership");
            controller.radarClip = one.radarClip; controller.shutterClip = one.shutterClip;
            var names = new[] { "fragment_recovery", "abyss_sparse_water", "distant_structure", "hidden_signal_a", "hidden_signal_b", "final_signal", "normal_ending_settle", "hidden_ending_tail" };
            foreach (var name in names)
            {
                string path = AudioFolder + "/" + name + ".wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) throw new InvalidOperationException("Missing original production clip: " + path);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path); var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true; importer.defaultSampleSettings = settings; importer.SaveAndReimport();
                string assetPath = Folder + "/audio." + name + ".asset";
                var d = AssetDatabase.LoadAssetAtPath<EffectDefinition>(assetPath);
                if (d == null) { d = ScriptableObject.CreateInstance<EffectDefinition>(); AssetDatabase.CreateAsset(d, assetPath); }
                Undo.RecordObject(d, "Author Zone4 audio");
                d.id = "z4.audio." + name; d.channel = d.id; d.priority = 45;
                d.clip = clip; d.duration = clip.length; d.loop = name == "abyss_sparse_water";
                d.target = name == "fragment_recovery" ? EffectTarget.Computer : EffectTarget.Global;
                d.audioCategory = name == "fragment_recovery" ? AudioCategory.Interface : name == "normal_ending_settle" ? AudioCategory.Machinery :
                    name == "abyss_sparse_water" || name == "hidden_ending_tail" ? AudioCategory.UnderwaterAmbient : AudioCategory.Anomalies;
                d.gain = name == "fragment_recovery" ? .85f : name == "distant_structure" ? .45f : .72f;
                d.audioFade = d.loop ? 2.5f : .1f; d.overlay = EffectOverlay.None;
                EditorUtility.SetDirty(d);
            }
            var director = controller.effects; var so = new SerializedObject(director);
            var catalog = so.FindProperty("definitions"); var surfaceList = so.FindProperty("surfaces");
            director.Configure(Enumerable.Range(0, catalog.arraySize).Select(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition)
                .Where(d => d != null && !d.id.StartsWith("z4.")).Concat(AssetDatabase.FindAssets("t:EffectDefinition", new[] { Folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<EffectDefinition>(AssetDatabase.GUIDToAssetPath(g)))).ToArray(),
                Enumerable.Range(0, surfaceList.arraySize).Select(i => surfaceList.GetArrayElementAtIndex(i).objectReferenceValue as EffectSurface).ToArray(), controller.GetComponent<EffectAudioExecutor>());
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(director); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene); EditorSceneManager.SaveScene(controller.gameObject.scene);
        }
        [MenuItem("G10/Zone 4/Install Wreck and Fragment Slice")]
        public static void InstallVerticalSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.path != "Assets/_Project/Scenes/Gameplay/Zone01.unity")
                throw new InvalidOperationException("Open the existing shared Zone01 cabin.");
            if (cabin.gameObject.scene.isDirty) throw new InvalidOperationException("Preserve and save cabin edits before installation.");
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/fragment_recovery.wav");
            if (clip == null) throw new InvalidOperationException("Import the approved original fragment_recovery.wav first.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/FeedbackEffects", "Zone04");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install Zone4 vertical slice");
            var definition = AssetDatabase.LoadAssetAtPath<EffectDefinition>(Folder + "/audio.fragment_recovery.asset");
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EffectDefinition>();
                AssetDatabase.CreateAsset(definition, Folder + "/audio.fragment_recovery.asset");
            }
            Undo.RecordObject(definition, "Author fragment data sound");
            definition.id = "z4.audio.fragment_recovery"; definition.channel = definition.id;
            definition.target = EffectTarget.Computer; definition.priority = 45;
            definition.clip = clip; definition.audioCategory = AudioCategory.Interface;
            definition.gain = .85f; definition.duration = clip.length; definition.loop = false;
            definition.overlay = EffectOverlay.None;
            EditorUtility.SetDirty(definition);
            var sequence = AssetDatabase.LoadAssetAtPath<EffectSequence>(Folder + "/FragmentRecovery.asset");
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<EffectSequence>();
                AssetDatabase.CreateAsset(sequence, Folder + "/FragmentRecovery.asset");
            }
            Undo.RecordObject(sequence, "Author fragment processing clock");
            sequence.id = "z4.fragment"; sequence.channel = "z4.sequence.fragment";
            sequence.target = EffectTarget.Computer; sequence.priority = 45;
            sequence.cues = new[] { new EffectCue { effect = definition, duration = 3.6f, phase = EffectPhase.Recovery } };
            EditorUtility.SetDirty(sequence);
            var controller = cabin.GetComponent<ZoneFourPresentationController>() ?? Undo.AddComponent<ZoneFourPresentationController>(cabin.gameObject);
            var log = cabin.GetComponentInChildren<MissionLogView>(true);
            if (log == null || log.Viewport == null) throw new InvalidOperationException("The actual Mission Log viewport is required.");
            var view = log.GetComponent<ZoneFourFragmentView>() ?? Undo.AddComponent<ZoneFourFragmentView>(log.gameObject);
            Undo.RecordObject(view, "Bind fragment to actual Mission Log");
            view.Bind(log);
            Undo.RecordObject(controller, "Bind Zone4 slice");
            controller.cabin = cabin; controller.effects = cabin.GetComponent<EffectDirector>();
            controller.fragmentSequence = sequence; controller.fragmentView = view;
            var director = controller.effects; var so = new SerializedObject(director);
            var catalog = so.FindProperty("definitions");
            var definitions = Enumerable.Range(0, catalog.arraySize)
                .Select(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition)
                .Where(d => d != null && d.id != definition.id).Append(definition).ToArray();
            var surfaces = so.FindProperty("surfaces");
            director.Configure(definitions, Enumerable.Range(0, surfaces.arraySize)
                .Select(i => surfaces.GetArrayElementAtIndex(i).objectReferenceValue as EffectSurface).ToArray(),
                cabin.GetComponent<EffectAudioExecutor>());
            foreach (var item in new UnityEngine.Object[] { view, controller, director }) EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            EditorSceneManager.SaveScene(cabin.gameObject.scene);
        }
    }
}

using System;
using System.Linq;
using G10.Prototype.Audio;
using G10.Prototype.Computer;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Editor
{
    public static class ZoneTwoSetup
    {
        public const string Folder = "Assets/_Project/FeedbackEffects/Zone02";
        private static EffectDefinition Def(string id, Action<EffectDefinition> configure)
        {
            string path = Folder + "/" + id + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<EffectDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<EffectDefinition>(); AssetDatabase.CreateAsset(d, path); }
            Undo.RecordObject(d, "Configure Zone 2 effect");
            d.id = "z2." + id; d.channel = d.id; d.target = EffectTarget.Global; configure(d); EditorUtility.SetDirty(d); return d;
        }
        private static EffectCue Cue(EffectDefinition effect, float time, float duration) => new() { effect = effect, offset = time, duration = duration };
        private static EffectSequence Sequence(string id, params EffectCue[] cues)
        {
            string path = Folder + "/" + id + ".asset";
            var s = AssetDatabase.LoadAssetAtPath<EffectSequence>(path);
            if (s == null) { s = ScriptableObject.CreateInstance<EffectSequence>(); AssetDatabase.CreateAsset(s, path); }
            Undo.RecordObject(s, "Configure Zone 2 sequence"); s.id = "z2." + id; s.channel = "z2.sequence"; s.priority = 40; s.cues = cues;
            EditorUtility.SetDirty(s); return s;
        }
        [MenuItem("G10/Zone 2/Install Environmental Horror V2")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null) throw new InvalidOperationException("Open the shared Zone01 cabin first.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/FeedbackEffects", "Zone02");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install Zone 2 environmental horror");
            var presentation = cabin.GetComponent<ZoneTwoPresentationController>() ?? Undo.AddComponent<ZoneTwoPresentationController>(cabin.gameObject);
            presentation.cabin = cabin; presentation.effects = cabin.GetComponent<EffectDirector>();
            var one = cabin.GetComponent<ZoneOneAudioController>(); presentation.radarClip = one.radarClip; presentation.shutterClip = one.shutterClip;
            var audioDefs = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio/Zone02" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".wav")).Select(path =>
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path); string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    var importer = (AudioImporter)AssetImporter.GetAtPath(path); var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
                    settings.preloadAudioData = true; importer.defaultSampleSettings = settings; importer.SaveAndReimport();
                    return Def("audio." + name, d =>
                    {
                        d.clip = clip; d.duration = clip.length; d.gain = name == "ambient_roots" ? .65f : name.StartsWith("specimen") ? .45f : .70f;
                        d.loop = name == "ambient_roots" || name.StartsWith("specimen"); d.audioFade = d.loop ? .8f : .1f;
                        d.audioCategory = name.Contains("radar") || name.Contains("electric") ? AudioCategory.Interface : name == "hull_install" ? AudioCategory.Machinery : AudioCategory.UnderwaterAmbient;
                        if (name == "knocks_rear") { d.lowPassHz = 3600; d.reverb = .20f; d.audioCategory = AudioCategory.Anomalies; }
                        if (name == "hull_scrape" || name == "outside_movement") d.audioCategory = AudioCategory.Anomalies;
                    });
                }).ToArray();
            EffectDefinition Audio(string id) => audioDefs.Single(d => d.id == "z2.audio." + id);
            Def("screen.cold", d => { d.overlay = EffectOverlay.Tint; d.alpha = .10f; d.decayOverlay = false; d.tintColor = new(.025f, .08f, .14f); });
            var dip = Def("screen.dip", d => { d.overlay = EffectOverlay.Tint; d.alpha = .20f; d.duration = .18f; d.priority = 40; });
            var first = Def("screen.flash-one", d => { d.overlay = EffectOverlay.Tint; d.alpha = .24f; d.duration = .14f; d.priority = 40; });
            var second = Def("screen.flash-two", d => { d.overlay = EffectOverlay.Tint; d.alpha = .43f; d.duration = .18f; d.priority = 40; });
            var radar = Def("screen.radar", d => { d.target = EffectTarget.Radar; d.interference = ScreenInterference.ScreenSignalInterference; d.interferenceIntensity = .6f; d.fadeIn = .02f; d.fadeOut = .12f; });
            var computer = Def("screen.computer", d => { d.target = EffectTarget.Computer; d.interference = ScreenInterference.AnalogSignalFailure; d.interferenceIntensity = .55f; d.fadeIn = .02f; d.fadeOut = .12f; });
            var suspense = Def("mix.suspense", d => { d.mixState = HorrorMixState.Suspense; d.priority = 30; d.audioFade = .35f; });
            var line = Def("screen.lily-subtitle", d => { d.message = "LILY · Không có va chạm..."; });
            var shadow = Def("screen.reflection", d =>
            {
                d.target = EffectTarget.Reflection; d.silhouette = AssetDatabase.LoadAssetAtPath<Sprite>(HorrorContentSetup.SpritePath);
                d.silhouettePosition = new(36, -8); d.silhouetteSize = new(65, 145); d.silhouetteOpacity = .34f;
                d.fadeIn = .01f; d.fadeOut = .035f;
            });
            var reflectionDip = Def("screen.reflection-dip", d => { d.overlay = EffectOverlay.Tint; d.alpha = .38f; d.duration = .4f; d.decayOverlay = false; d.priority = 40; });
            presentation.radarDisturbance = Sequence("RadarDisturbance", Cue(Audio("radar_static"), 0, .65f), Cue(radar, 0, .65f), Cue(dip, .05f, .18f), Cue(Audio("outside_movement"), .65f, 2));
            presentation.scrapeDisturbance = Sequence("ScrapeDisturbance", Cue(suspense, 0, 7.5f), Cue(Audio("hull_scrape"), .35f, 6), Cue(computer, 1.5f, .75f),
                Cue(first, 3.25f, .14f), Cue(second, 3.62f, .18f), Cue(Audio("electric_fizz"), 3.25f, .65f), Cue(Audio("cabin_tick"), 6.4f, .55f), Cue(line, 6.7f, 2.4f));
            presentation.reflection = Sequence("Reflection", Cue(reflectionDip, 1, .4f), Cue(Audio("electric_fizz"), 1, .65f), Cue(shadow, 1.2f, .2f));

            var director = presentation.effects; var serialized = new SerializedObject(director);
            var entries = serialized.FindProperty("definitions");
            var catalog = Enumerable.Range(0, entries.arraySize).Select(i => entries.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition)
                .Where(d => d != null && !d.id.StartsWith("z2.")).Concat(AssetDatabase.FindAssets("t:EffectDefinition", new[] { Folder }).Select(g => AssetDatabase.LoadAssetAtPath<EffectDefinition>(AssetDatabase.GUIDToAssetPath(g)))).ToArray();
            var surfacesProperty = serialized.FindProperty("surfaces");
            var surfaces = Enumerable.Range(0, surfacesProperty.arraySize).Select(i => surfacesProperty.GetArrayElementAtIndex(i).objectReferenceValue as EffectSurface).Where(s => s != null && s.target != EffectTarget.Reflection).ToList();
            var parent = director.Surface(EffectTarget.Global).transform;
            var node = parent.Find("GlassReflectionEffects");
            if (node == null) { var obj = new GameObject("GlassReflectionEffects", typeof(RectTransform), typeof(EffectSurface), typeof(EffectVisualExecutor), typeof(RectMask2D)); obj.transform.SetParent(parent, false); node = obj.transform; }
            var surface = node.GetComponent<EffectSurface>(); surface.target = EffectTarget.Reflection;
            surface.BindVisibility(cabin.NavigationPanel.transform.Find("MiniMap").gameObject);
            surface.visual = node.GetComponent<EffectVisualExecutor>(); surface.visual.useLibraryReflection = true;
            surface.visual.signalTemplate = HorrorContentSetup.SignalMaterial(); surfaces.Add(surface);
            const string variantsPath = Folder + "/ReflectionVariants.shadervariants";
            var variants = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(variantsPath);
            if (variants == null) { variants = new ShaderVariantCollection(); AssetDatabase.CreateAsset(variants, variantsPath); }
            var reflectionShader = Shader.Find("Hidden/UI/Default (UIEffect)");
            foreach (bool clipped in new[] { false, true })
            {
                string[] keywords = clipped ? new[] { "TONE_GRAYSCALE", "SAMPLING_RGB_SHIFT", "UNITY_UI_CLIP_RECT" } : new[] { "TONE_GRAYSCALE", "SAMPLING_RGB_SHIFT" };
                variants.Add(new ShaderVariantCollection.ShaderVariant(reflectionShader, UnityEngine.Rendering.PassType.Normal, keywords));
                var warmup = new Material(reflectionShader) { shaderKeywords = keywords };
                // Editor asynchronous compilation otherwise shows a cyan placeholder during the entire glimpse.
                ShaderUtil.CompilePass(warmup, 0, true); UnityEngine.Object.DestroyImmediate(warmup);
            }
            surface.visual.reflectionVariants = variants; variants.WarmUp(); EditorUtility.SetDirty(variants);
            director.Configure(catalog, surfaces.ToArray(), cabin.GetComponent<EffectAudioExecutor>());

            var map = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>("Assets/_Project/Content/Maps/Zone02.asset"); ConfigureMission(map.missionConfig);
            map.locations = map.locations.Where(p => p.id != "zone02-l2").ToArray();
            foreach (var p in map.locations) if (p.id == "zone02-l3" && (!p.overrideDepth || p.targetDepth > 500))
            { p.overrideDepth = true; p.targetDepth = 500; }
            foreach (string id in new[] { "Z2_Creature_01", "Z2_Creature_02" })
            {
                var content = AssetDatabase.LoadAssetAtPath<SurveyContentDefinition>($"Assets/_Project/Content/Zone02/Definitions/{id}.asset");
                bool adhesive = id.EndsWith("01");
                content.displayName = adhesive ? "SPECIMEN 03 · Sinh vật tiết chất kết dính" : "SPECIMEN 04 · Sinh vật có giáp";
                content.description = adhesive ? "BIOLOGICAL STRUCTURE DETECTED\nADHESIVE SECRETION OBSERVED\nThu hồi mẫu kết dính để gia cố tàu." : "HARDENED EXTERNAL STRUCTURE\nHIGH PRESSURE RESISTANCE\nThu hồi vật liệu cứng để lắp Pressure Hull.";
                EditorUtility.SetDirty(content);
            }
            ConfigureReadout(cabin, presentation);
            var module = cabin.GetComponentsInChildren<UpgradeEntryConfig>(true).Single(e => e.UpgradeId == "ExpeditionModule");
            var moduleData = new SerializedObject(module);
            foreach (bool adhesive in new[] { true, false })
            {
                string path = "Assets/_Project/Data/Upgrade/Zone02" + (adhesive ? "Adhesive" : "Hardened") + ".asset";
                var material = AssetDatabase.LoadAssetAtPath<UpgradeMaterialDefinition>(path);
                if (material == null) { material = ScriptableObject.CreateInstance<UpgradeMaterialDefinition>(); AssetDatabase.CreateAsset(material, path); }
                var data = new SerializedObject(material); data.FindProperty("materialId").stringValue = adhesive ? "Z2_Creature_01" : "Z2_Creature_02";
                data.FindProperty("displayName").stringValue = adhesive ? "Vật liệu kết dính" : "Vật liệu giáp cứng";
                data.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UpgradeMaterialDefinition>("Assets/_Project/Data/Upgrade/Z2_Creature_02.asset").Icon;
                data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(material);
                moduleData.FindProperty(adhesive ? "zoneTwoAdhesive" : "zoneTwoHardened").objectReferenceValue = material;
            }
            moduleData.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(module);
            foreach (var obj in new UnityEngine.Object[] { presentation, director, surface, surface.visual, map, map.missionConfig }) EditorUtility.SetDirty(obj);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(cabin.gameObject.scene);
        }
        public static void ConfigureMission(ZoneMissionConfig config)
        {
            MissionConfigEditor.ConfigureMission(config);
        }
        private static void ConfigureReadout(CabinStationView cabin, ZoneTwoPresentationController presentation)
        {
            var computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            var panel = Array.Find(computer.Apps, app => app.id == ComputerAppId.Upgrade).panel;
            var view = panel.GetComponent<ZoneTwoHullView>() ?? Undo.AddComponent<ZoneTwoHullView>(panel);
            var parent = panel.transform.Find("LeftPanel") ?? panel.transform;
            var node = parent.Find("PressureHullInstallReadout");
            if (node == null)
            {
                var obj = new GameObject("PressureHullInstallReadout", typeof(RectTransform), typeof(Image), typeof(CanvasGroup)); obj.transform.SetParent(parent, false); node = obj.transform;
                var text = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(node, false);
                var tr = (RectTransform)text.transform; tr.anchorMin = new(.04f, .40f); tr.anchorMax = new(.96f, .96f); tr.offsetMin = tr.offsetMax = Vector2.zero;
                var label = text.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.fontSize = 19; label.color = new(.10f, .23f, .35f); label.raycastTarget = false;
                for (int i = 0; i < 3; i++)
                {
                    var plate = new GameObject("Plate" + i, typeof(RectTransform), typeof(Image)); plate.transform.SetParent(node, false);
                    var r = (RectTransform)plate.transform; r.anchorMin = new(.12f + i * .26f, .10f); r.anchorMax = new(.36f + i * .26f, .32f); r.offsetMin = r.offsetMax = Vector2.zero;
                    plate.GetComponent<Image>().raycastTarget = false;
                }
            }
            var rect = (RectTransform)node; rect.anchorMin = new(.40f, .02f); rect.anchorMax = new(.96f, .24f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            node.GetComponent<Image>().color = new(.90f, 1, .97f, .96f); node.GetComponent<Image>().raycastTarget = false;
            view.card = node.GetComponent<CanvasGroup>(); view.card.alpha = 0; view.card.interactable = view.card.blocksRaycasts = false;
            view.message = node.GetComponentInChildren<TMP_Text>(); view.plates = Enumerable.Range(0, 3).Select(i => node.Find("Plate" + i).GetComponent<Image>()).ToArray();
            view.story = cabin.GetComponent<ZoneOneStory>(); view.presentation = presentation; view.computer = computer; EditorUtility.SetDirty(view);
        }
    }
}

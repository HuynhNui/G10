using System;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.Tutorial;
using G10.Prototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Editor
{
    public static class ZoneOneAudioSetup
    {
        private const string Effects = "Assets/_Project/FeedbackEffects/Zone01/";
        [MenuItem("G10/Zone 1/Install Singing Forest Gameplay and Audio")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01") throw new InvalidOperationException("Open the shared Zone01 cabin.");
            var core = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameplayCore");
            if (!core.isLoaded) core = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/GameplayCore.unity", OpenSceneMode.Additive);
            var loop = UnityEngine.Object.FindAnyObjectByType<ExpeditionLoop>();
            var story = cabin.GetComponent<ZoneOneStory>();
            var map = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>("Assets/_Project/Content/Maps/Zone01.asset");
            if (loop == null || story == null || map == null) throw new InvalidOperationException("Missing existing expedition/mission/map.");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Wire Zone01 story audio");
            Undo.RecordObject(loop, "Add Zone01 material catalog");
            ConfigureMission(map.missionConfig);
            ZoneMapEditor.RebuildLocations(map, new[] {
                new MapPoi { id="zone01-left", mapPosition=new(440,115.71f), arrivalRadius=35, overrideDepth=true, targetDepth=230 },
                new MapPoi { id="zone01-north", mapPosition=new(1000,732.86f), arrivalRadius=35, overrideDepth=true, targetDepth=460 }
            });
            story.config = map.missionConfig; story.poiIds = map.locations.Select(p => p.id).ToArray();
            story.survey.locations = map.locations;
            var material = Material(story.creatureOne);
            if (story.creatureOne != null)
            {
                story.creatureOne.displayName = "Tảo Hát — Singing Algae";
                story.creatureOne.description = "Quần thể sinh vật dạng rỗng cộng hưởng trong dòng nước. Một mẫu phát sáng có thể dùng để sửa hệ thống chiếu sáng ngoài tàu.";
                EditorUtility.SetDirty(story.creatureOne);
            }
            story.contentCatalog = story.contentCatalog.Where(x => x != null && x.id != material.id).Append(material).ToArray();
            loop.contentCatalog = loop.contentCatalog.Where(x => x != null && x.id != material.id).Append(material).ToArray();
            if (!loop.creatureCatalog.Any(x => x?.id == material.id))
                loop.creatureCatalog = loop.creatureCatalog.Append(new ExpeditionCreatureAsset { id=material.id, icon=material.Image }).ToArray();
            var audio = cabin.GetComponent<ZoneOneAudioController>() ?? Undo.AddComponent<ZoneOneAudioController>(cabin.gameObject);
            audio.cabin = cabin; audio.effects = cabin.GetComponent<EffectDirector>();
            audio.radarClip = Clip("sfx_radar_scan_ping"); audio.shutterClip = Clip("sfx_camera_shutter");
            AddVisual("landing_shake", d => { d.duration=.25f; d.shakePixels=6; d.shakeSeconds=.25f; d.shakeProfile=ShakeProfile.ShortImpact; });
            AddVisual("reboot_flicker", d => { d.duration=1.6f; d.lightDip=.09f; d.flicker=true; d.lightBehavior=LightBehavior.SubtleFlicker; });
            ConfigureHullFeedback(audio);
            var catalog = AssetDatabase.FindAssets("t:EffectDefinition", new[] { Effects.TrimEnd('/') })
                .Select(g => AssetDatabase.LoadAssetAtPath<EffectDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(d => d != null).ToArray();
            var serialized = new SerializedObject(audio.effects);
            var entries = serialized.FindProperty("definitions");
            var all = Enumerable.Range(0, entries.arraySize).Select(i => entries.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition)
                .Where(d => d != null && !d.id.StartsWith("z1.audio.")).Concat(catalog).ToArray();
            entries.arraySize = all.Length;
            for (int i=0;i<all.Length;i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Machine movement follows the controls promptly; large environment layers dissolve slowly.
            foreach (var d in catalog)
            {
                bool helmControl = d.id == "z1.audio.sfx_steering_servo" || d.id == "z1.audio.sfx_depth_pressure_adjustment";
                if (helmControl) { d.loop = true; d.audioFade = .08f; }
                else if (d.loop) d.audioFade = d.id.Contains("propulsion") || d.id.Contains("capture") ? .3f : .8f;
                EditorUtility.SetDirty(d);
            }
            ConfigureTutorial(cabin.GetComponent<ZoneOneTutorialDirector>()?.config);
            foreach (var camera in cabin.GetComponentsInChildren<PhotoCameraView>(true)) ConfigureLamp(camera, story, audio);
            ConfigureBioLampReadout(cabin, story, audio);
            foreach (var obj in new UnityEngine.Object[] { story, story.survey, loop, audio, audio.effects, map }) EditorUtility.SetDirty(obj);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene); EditorSceneManager.MarkSceneDirty(core);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(cabin.gameObject.scene); EditorSceneManager.SaveScene(core);
            Debug.Log("Zone01: two survey sites, SEND → algae sample → SEND second species → Bio Lamp, 32 audio clips and timeline-aware cues installed.");
        }
        public static void ConfigureMission(ZoneMissionConfig config)
        {
            MissionConfigEditor.ConfigureMission(config);
        }
        // A narrow installer lets audio/feedback revisions avoid changing missions or the photo flow.
        public static void ConfigureHullFeedback(ZoneOneAudioController audio)
        {
            var impact = AssetDatabase.LoadAssetAtPath<EffectDefinition>("Assets/_Project/FeedbackEffects/hull.impact.asset");
            var clip = Clip("sfx_terrain_impact");
            if (impact == null || clip == null) throw new InvalidOperationException("Terrain impact definition/audio is missing.");
            impact.clip = clip; impact.audioCategory = G10.Prototype.Audio.AudioCategory.Impacts;
            impact.loop = impact.fadeAudio = false;
            EditorUtility.SetDirty(impact);

            string pulsePath = Effects + "knock_pulse.asset";
            var pulse = AssetDatabase.LoadAssetAtPath<EffectDefinition>(pulsePath);
            if (pulse == null)
            {
                pulse = ScriptableObject.CreateInstance<EffectDefinition>();
                AssetDatabase.CreateAsset(pulse, pulsePath);
            }
            pulse.id = "z1.audio.knock_pulse"; pulse.channel = "z1.knock.screen";
            pulse.target = EffectTarget.Global; pulse.priority = 35;
            pulse.duration = pulse.shakeSeconds = .24f; pulse.shakePixels = 7;
            pulse.shakeProfile = ShakeProfile.ShortImpact;
            pulse.overlay = EffectOverlay.Tint; pulse.alpha = .10f; pulse.decayOverlay = true;
            pulse.clip = null; pulse.lightDip = 0; pulse.interference = ScreenInterference.None;
            EditorUtility.SetDirty(pulse);
            var sound = AssetDatabase.LoadAssetAtPath<EffectDefinition>(Effects + "sfx_hull_knocks_three.asset");
            if (sound == null || sound.clip == null) throw new InvalidOperationException("Three-knock audio is missing.");
            string path = Effects + "HullKnocks.asset";
            var sequence = AssetDatabase.LoadAssetAtPath<EffectSequence>(path);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<EffectSequence>();
                AssetDatabase.CreateAsset(sequence, path);
            }
            sequence.id = "z1.hull-knocks"; sequence.channel = "z1.sequence.knocks";
            sequence.target = EffectTarget.Global; sequence.priority = 35;
            sequence.synchronizeToAudio = true;
            // Exact attacks from sfx_hull_knocks_three.provenance.json; the pan is baked into the WAV.
            sequence.cues = new[] {
                new EffectCue { effect = sound, offset = 0, duration = sound.clip.length },
                new EffectCue { effect = pulse, offset = .12f, duration = .24f, intensity = .85f },
                new EffectCue { effect = pulse, offset = .72f, duration = .24f },
                new EffectCue { effect = pulse, offset = 1.32f, duration = .24f, intensity = .9f }
            };
            audio.hullKnocks = sequence;
            EditorUtility.SetDirty(sequence); EditorUtility.SetDirty(audio);
            EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);
        }
        private static MissionRewardConfig Reward(MissionRewardType type,string id) => new() {type=type,targetId=id};
        private static SurveyContentDefinition Material(SurveyContentDefinition reference)
        {
            // The canonical captured-organism definition owns both art and cargo identity.
            // Do not replace it with the old separate material asset on an installer rerun.
            if (reference != null) return reference;
            const string path="Assets/_Project/Content/Zone01/Items/LuminescentAlgae.asset";
            var entry=AssetDatabase.LoadAssetAtPath<SurveyContentDefinition>(path);
            if(entry!=null)return entry;
            entry=ScriptableObject.CreateInstance<SurveyContentDefinition>();entry.id="Z1_MATERIAL_LUMINESCENT_ALGAE";
            entry.displayName="Vật liệu tảo phát sáng";
            entry.description="Mẫu sinh học thu hồi từ Rạn Tảo Đỏ. Có thể dùng để khôi phục hệ thống chiếu sáng ngoài tàu bằng Bio Lamp.";
            entry.image=reference?.Image;entry.sprite=reference?.sprite;entry.placeholderArt=true;
            AssetDatabase.CreateAsset(entry,path);return entry;
        }
        private static AudioClip Clip(string name) => AssetDatabase.FindAssets(name+" t:AudioClip",new[] {"Assets/_Project/Audio/Zone01"})
            .Select(AssetDatabase.GUIDToAssetPath).Where(p=>System.IO.Path.GetFileNameWithoutExtension(p)==name)
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>).FirstOrDefault();
        private static void AddVisual(string id, Action<EffectDefinition> configure)
        {
            string path=Effects+id+".asset";
            var definition=AssetDatabase.LoadAssetAtPath<EffectDefinition>(path);
            if(definition!=null)return;
            definition=ScriptableObject.CreateInstance<EffectDefinition>();definition.id="z1.audio."+id;definition.channel=definition.id;
            definition.silhouetteOpacity=0;definition.interferenceIntensity=0;configure(definition);AssetDatabase.CreateAsset(definition,path);
        }
        private static void ConfigureTutorial(TutorialConfig config)
        {
            if(config==null)return;
            config.guideDisplayName="SYSTEM";
            foreach(var line in config.instructions)
                if(line!=null)line.text=line.step switch {
                    TutorialStepId.Intro=>"NAVIGATION: ONLINE\nLIFE SUPPORT: ONLINE\nEXTERNAL ILLUMINATION: OFFLINE\nREPAIR REQUIRED: BIO LAMP",
                    TutorialStepId.Capture=>"Sau khi SEND ảnh tảo, quay lại Rạn Tảo Đỏ và dùng CAPTURE để thu vật liệu phát sáng. Tiếp tục tới Thềm Biển Sâu, chụp và SEND ảnh sinh vật thứ hai.",
                    TutorialStepId.Upgrade=>"Hai ảnh đã SEND và mẫu phát sáng đã có. COMPUTER → UPGRADE → MODULES → INSTALL BIO LAMP.",
                    TutorialStepId.Complete=>"BIO LAMP INSTALLED\nEXTERNAL ILLUMINATION: ONLINE\nZONE 01 COMPLETE — NEXT SURVEY AREA AVAILABLE",_=>line.text};
            EditorUtility.SetDirty(config);
        }
        private static void ConfigureLamp(PhotoCameraView camera,ZoneOneStory story,ZoneOneAudioController audio)
        {
            if(camera.preview==null)return;
            var view=camera.GetComponent<ZoneOneLampView>()??Undo.AddComponent<ZoneOneLampView>(camera.gameObject);
            var overlay=camera.preview.transform.Find("ExternalLightDarkness");
            if(overlay==null)
            {
                var node=new GameObject("ExternalLightDarkness",typeof(RectTransform),typeof(Image));node.transform.SetParent(camera.preview.transform,false);
                var rect=(RectTransform)node.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                node.GetComponent<Image>().raycastTarget=false;overlay=node.transform;
            }
            view.story=story;view.audio=audio;view.darkness=overlay.GetComponent<Graphic>();view.darkness.color=new(0,0,0,.28f);
            EditorUtility.SetDirty(view);
        }
        public static void ConfigureBioLampReadout(CabinStationView cabin, ZoneOneStory story, ZoneOneAudioController audio)
        {
            var computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            var panel = Array.Find(computer.Apps, app => app.id == ComputerAppId.Upgrade)?.panel;
            if (panel == null) return;
            var view = panel.GetComponent<ZoneOneBioLampView>() ?? Undo.AddComponent<ZoneOneBioLampView>(panel);
            var node = view.card != null ? view.card.transform : panel.transform.Find("BioLampInstallReadout");
            if (node == null)
            {
                var card = new GameObject("BioLampInstallReadout", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                card.transform.SetParent(panel.transform, false); node = card.transform;
                var rect = (RectTransform)node; rect.anchorMin = new(.33f, .02f); rect.anchorMax = new(.97f, .13f); rect.offsetMin = rect.offsetMax = Vector2.zero;
                card.GetComponent<Image>().color = new(.04f, .12f, .17f, .96f); card.GetComponent<Image>().raycastTarget = false;
                var label = new GameObject("InstallStatus", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(node, false);
                var lr = (RectTransform)label.transform; lr.anchorMin = new(0, .3f); lr.anchorMax = new(1, 1); lr.offsetMin = new(18, 0); lr.offsetMax = new(-18, -6);
                var text = label.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.fontSize = 21;
                text.color = new(.10f, .23f, .35f); text.raycastTarget = false; text.alignment = TextAlignmentOptions.MidlineLeft;
                var track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image)); track.transform.SetParent(node, false);
                var tr = (RectTransform)track.transform; tr.anchorMin = new(0, 0); tr.anchorMax = new(1, 0); tr.pivot = new(.5f, 0);
                tr.offsetMin = new(18, 10); tr.offsetMax = new(-18, 14); track.GetComponent<Image>().color = new(.12f, .27f, .29f); track.GetComponent<Image>().raycastTarget = false;
                var bar = new GameObject("Progress", typeof(RectTransform), typeof(Image)); bar.transform.SetParent(track.transform, false);
                var br = (RectTransform)bar.transform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
                var image = bar.GetComponent<Image>(); image.color = new(.28f, .75f, .62f); image.raycastTarget = false;
                image.type = Image.Type.Simple;
            }
            var left = panel.transform.Find("LeftPanel");
            node.SetParent(left != null ? left : panel.transform, false);
            var cardRect = (RectTransform)node;
            cardRect.anchorMin = new(.40f, .02f); cardRect.anchorMax = new(.96f, .24f); cardRect.offsetMin = cardRect.offsetMax = Vector2.zero;
            var background = node.GetComponent<Image>();
            background.sprite = left != null ? left.GetComponent<Image>()?.sprite : null;
            background.type = Image.Type.Sliced; background.color = new(.90f, 1, .97f, .96f);
            view.audio = audio; view.story = story; view.computer = computer; view.card = node.GetComponent<CanvasGroup>();
            view.card.alpha = 0; view.card.interactable = false; view.card.blocksRaycasts = false;
            view.message = node.GetComponentInChildren<TMP_Text>(true);
            view.message.color = new(.10f, .23f, .35f);
            view.fill = node.Find("ProgressTrack/Progress").GetComponent<Image>();
            EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
        }
    }
}

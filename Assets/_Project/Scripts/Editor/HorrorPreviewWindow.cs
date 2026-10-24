using System.Linq;
using G10.Prototype.Atmosphere;
using G10.Prototype.Feedback;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Editor
{
    public sealed class HorrorPreviewWindow : EditorWindow
    {
        public const string ScenePath = "Assets/_Project/Scenes/Testing/HorrorPresentationPreview.unity";
        private AudioClip sampleAudio;
        private Sprite sampleSilhouette;
        private float intensity = .75f, volume = 1;
        private int ambience;
        [MenuItem("G10/Horror/Presentation Preview")]
        public static void Open() => GetWindow<HorrorPreviewWindow>("Horror Preview");
        private void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Real ElevenLabs clips and actual illustrated displays. Isolated presentation scene; no mission or save components. Silhouette is temporary test artwork. Free-plan audio is non-commercial only.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Create / open illustrated preview scene")) CreateScene();
            sampleAudio = (AudioClip)EditorGUILayout.ObjectField("Individual preset audio override", sampleAudio, typeof(AudioClip), false);
            sampleSilhouette = (Sprite)EditorGUILayout.ObjectField("Preset 7 silhouette override", sampleSilhouette, typeof(Sprite), false);
            EditorGUI.BeginChangeCheck();
            volume = EditorGUILayout.Slider("Audio volume", volume, 0, 1);
            bool volumeChanged = EditorGUI.EndChangeCheck();
            intensity = EditorGUILayout.Slider("Visual intensity", intensity, 0, 1);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Subtle")) intensity = .45f;
            if (GUILayout.Button("Medium")) intensity = .75f;
            if (GUILayout.Button("Strong")) intensity = 1;
            EditorGUILayout.EndHorizontal();
            var preview = Object.FindAnyObjectByType<HorrorPreviewAudioBinding>();
            bool ready = EditorApplication.isPlaying && EditorSceneManager.GetActiveScene().path == ScenePath && preview != null;
            if (ready && volumeChanged) preview.SetAmbience(ambience, volume);
            EditorGUILayout.LabelField("Playing", ready ? preview.CurrentPreset : "Enter Play Mode in preview scene");
            using (new EditorGUI.DisabledScope(!ready))
            {
                int chosen = EditorGUILayout.Popup("Ambient loop", ambience, new[]{"Deep abyss", "Singing algae", "Ancient roots", "Submerged ruins"});
                if (chosen != ambience) { ambience = chosen; preview.SetAmbience(ambience, volume); }
                for (int i = 1; i <= 9; i++)
                    if (GUILayout.Button(i + " · " + HorrorPreviewAudioBinding.PresetNames[i - 1])) preview.PlayPreset(i, intensity, volume, sampleAudio, sampleSilhouette);
                if (GUILayout.Button("Stop / Reset all preview audio and visuals")) preview.StopPreview();
            }
            foreach (var id in new[]{"horror.knock","horror.scrape","light.SubtleFlicker","ScreenSignalInterference","AnalogSignalFailure","horror.pressure","horror.silhouette"})
            {
                var d = HorrorFoundationSetup.Load(id);
                if (d == null || d.clip == null) EditorGUILayout.HelpBox("Missing default AudioClip: " + id, MessageType.Warning);
                if (id == "horror.silhouette" && d != null && d.silhouette == null) EditorGUILayout.HelpBox("Missing silhouette sprite", MessageType.Warning);
            }
            if (ready)
                foreach (var target in new[]{EffectTarget.Radar, EffectTarget.Computer})
                {
                    var visual = preview.director.Surface(target)?.visual;
                    if (visual?.displayRegion == null || visual.signalTemplate == null) EditorGUILayout.HelpBox("Missing display/material binding: " + target, MessageType.Warning);
                }
        }
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            HorrorContentSetup.BindContent();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("PreviewCamera", typeof(Camera), typeof(AudioListener));
            camera.GetComponent<Camera>().backgroundColor = new Color(.015f, .025f, .03f);
            var canvasObject = new GameObject("Presentation Preview", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var frame = new GameObject("IllustrationFrame", typeof(RectTransform)); frame.transform.SetParent(canvasObject.transform, false); Stretch((RectTransform)frame.transform);
            var background = Region(frame.transform, "CabinArt", Vector2.zero, new Vector2(1920, 1080));
            background.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/PrototypeCabin/Cabin.png");
            var atmosphere = frame.AddComponent<CabinAtmosphere>(); atmosphere.followRealRadarScan = false;
            // Reuse the real screen artwork, fitted INSIDE the painted bezel, with no debug plates.
            var computer = Region(frame.transform, "ComputerDisplay", new Vector2(-36, 183), new Vector2(526, 216));
            computer.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Desktop/Pelagic_Desktop_UI_Assets_No_Text/01_Desktop_Background.png");
            foreach (var item in new[]{("Photo lab", "02_Icon_Photo_Lab_No_Text"), ("Ship status", "03_Icon_Ship_Status_No_Text"), ("Mission log", "04_Icon_Mission_Log_No_Text")}.Select((v,i)=>(v,i)))
            {
                var icon = Region(computer.transform, item.v.Item1, new Vector2(-195+item.i*75, 48), new Vector2(42,42));
                icon.texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Art/UI/Desktop/Pelagic_Desktop_UI_Assets_No_Text/{item.v.Item2}.png");
            }
            var radar = Region(frame.transform, "RadarScope", new Vector2(533, 183), new Vector2(225, 216));
            radar.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/PrototypeCabin/Radar.png");
            radar.uvRect = new Rect(388f/1920, 352f/1080, 420f/1920, 404f/1080);
            var needle = Region(frame.transform, "Decorative pressure needle", new Vector2(-118, -25), new Vector2(4, 35));
            needle.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/CabinAtmosphere/SoftDot.png");
            needle.color = new Color(.19f, .31f, .37f, .6f); needle.rectTransform.pivot = new Vector2(.5f, 0);
            var gauge = needle.gameObject.AddComponent<GaugeJitter>(); gauge.mood = atmosphere; gauge.amplitudeDegrees = 0;
            var director = canvasObject.AddComponent<EffectDirector>(); var audio = canvasObject.AddComponent<EffectAudioExecutor>();
            var surfaces = new EffectSurface[6];
            for (int i = 0; i < surfaces.Length; i++)
            {
                var target = (EffectTarget)i; var go = new GameObject(target + "Effects", typeof(RectTransform)); go.transform.SetParent(canvasObject.transform, false); Stretch((RectTransform)go.transform);
                var surface = go.AddComponent<EffectSurface>(); surface.target = target; surfaces[i] = surface;
                if (target == EffectTarget.Radar) surface.BindVisibility(radar.gameObject);
                else if (target == EffectTarget.Computer) surface.BindVisibility(computer.gameObject);
                else if (target == EffectTarget.Cabin) surface.BindVisibility(frame);
                HorrorFoundationSetup.BindVisual(surface);
                if (target == EffectTarget.Global)
                {
                    surface.lighting = go.AddComponent<EffectLightingExecutor>(); surface.lighting.atmosphere = atmosphere;
                    surface.shake.shake = frame.AddComponent<ImpactShake>();
                }
            }
            var definitions = AssetDatabase.FindAssets("t:EffectDefinition", new[]{HorrorFoundationSetup.Folder}).Select(g=>AssetDatabase.LoadAssetAtPath<EffectDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            director.Configure(definitions, surfaces, audio);
            var binder = canvasObject.AddComponent<HorrorPreviewAudioBinding>(); binder.director = director;
            binder.knocks = AssetDatabase.LoadAssetAtPath<EffectSequence>($"{HorrorFoundationSetup.Folder}/ThreeMetallicKnocks.asset");
            binder.combined = AssetDatabase.LoadAssetAtPath<EffectSequence>($"{HorrorFoundationSetup.Folder}/CombinedHorror.asset");
            binder.ambience = new[]{"ambient_deep_abyss","ambient_singing_algae","ambient_ancient_roots","ambient_ruins"}.Select(HorrorFoundationSetup.Load).ToArray();
            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes/Testing");
            AssetDatabase.Refresh(); EditorSceneManager.SaveScene(scene, ScenePath); Selection.activeObject = canvasObject;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static RawImage Region(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<RawImage>(); image.raycastTarget = false; return image;
        }
    }
}

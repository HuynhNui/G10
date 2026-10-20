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
        private float intensity = 1, duration = 2;
        [MenuItem("G10/Horror/Presentation Preview")]
        public static void Open() => GetWindow<HorrorPreviewWindow>("Horror Preview");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Preview uses a separate scene with no ship, mission, Day or save components. Audio/silhouette samples are optional; no new ElevenLabs audio or ghost artwork has been produced.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Create / open isolated preview scene")) CreateScene();
            sampleAudio = (AudioClip)EditorGUILayout.ObjectField("Optional sample audio", sampleAudio, typeof(AudioClip), false);
            sampleSilhouette = (Sprite)EditorGUILayout.ObjectField("Artist silhouette sprite", sampleSilhouette, typeof(Sprite), false);
            intensity = EditorGUILayout.Slider("Intensity", intensity, 0, 1);
            duration = EditorGUILayout.Slider("Preset duration", duration, .1f, 8);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path != ScenePath))
            {
                if (GUILayout.Button("1 · Three knocks (sample rhythm)")) Sequence("ThreeMetallicKnocks", true);
                if (GUILayout.Button("2 · Outside hull scraping")) Play("horror.scrape");
                if (GUILayout.Button("3 · Subtle fluorescent flicker")) Play("light.SubtleFlicker");
                if (GUILayout.Button("4 · Radar signal interference")) Play("ScreenSignalInterference");
                if (GUILayout.Button("5 · Analog screen disturbance")) Play("AnalogSignalFailure");
                if (GUILayout.Button("6 · Heavy hull pressure")) Play("horror.pressure");
                if (GUILayout.Button("7 · Brief silhouette (assign sprite above)")) Play("horror.silhouette");
                if (GUILayout.Button("8 · Combined horror sequence")) Sequence("CombinedHorror", false);
                if (GUILayout.Button("9 · Recover to normal")) Director()?.ClearTransients();
            }
            EditorGUILayout.HelpBox("Enter Play Mode in the preview scene, then use these controls. Three knocks uses the existing creak as an explicitly labeled sample unless you assign audio. Screen effects affect only the marked Radar/computer regions. Shake follows the saved accessibility preference.", MessageType.None);
        }
        private static EffectDirector Director() => UnityEngine.Object.FindAnyObjectByType<EffectDirector>();
        private EffectContext Context() => new EffectContext { Intensity = intensity, Clip = sampleAudio, Sprite = sampleSilhouette };
        private void Play(string id)
        { var context = Context(); context.Duration = duration; Director()?.Play(id, context); }
        private void Sequence(string id, bool sample)
        {
            var context = Context();
            if (sample && context.Clip == null) context.Clip = HorrorFoundationSetup.Load("horror.pressure")?.clip;
            Director()?.PlaySequence(AssetDatabase.LoadAssetAtPath<EffectSequence>($"{HorrorFoundationSetup.Folder}/{id}.asset"), context);
        }
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Reuse the authored cabin illustration only. No gameplay components enter this scene.
            var art = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/PrototypeCabin/Cabin.png");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("PreviewCamera", typeof(Camera), typeof(AudioListener));
            camera.GetComponent<Camera>().backgroundColor = new Color(.015f, .025f, .03f);
            var canvasObject = new GameObject("Presentation Preview", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var frame = new GameObject("IllustrationFrame", typeof(RectTransform)); frame.transform.SetParent(canvasObject.transform, false);
            Stretch((RectTransform)frame.transform);
            var background = Region(frame.transform, "Cabin illustration", new Vector2(0, 0), new Vector2(1920, 1080));
            background.texture = art; background.color = art != null ? Color.white : new Color(.12f, .17f, .18f);
            var atmosphere = frame.AddComponent<CabinAtmosphere>(); atmosphere.followRealRadarScan = false;
            var director = canvasObject.AddComponent<EffectDirector>(); var audio = canvasObject.AddComponent<EffectAudioExecutor>();
            var surfaces = new EffectSurface[6];
            for (int i = 0; i < surfaces.Length; i++)
            {
                var target = (EffectTarget)i; var go = new GameObject(target + "Effects", typeof(RectTransform)); go.transform.SetParent(canvasObject.transform, false); Stretch((RectTransform)go.transform);
                var surface = go.AddComponent<EffectSurface>(); surface.target = target; surfaces[i] = surface;
                if (target == EffectTarget.Radar || target == EffectTarget.Computer || target == EffectTarget.PhotoLab || target == EffectTarget.Helm)
                {
                    var window = Region(frame.transform, target + " preview region", new Vector2(target == EffectTarget.Radar || target == EffectTarget.Helm ? -450 : 440, target == EffectTarget.PhotoLab || target == EffectTarget.Helm ? -320 : 170), new Vector2(580, 300));
                    window.color = new Color(.025f, .085f, .075f, .65f); surface.BindVisibility(window.gameObject);
                    var labelObject = new GameObject("Preview region label", typeof(RectTransform), typeof(Text)); labelObject.transform.SetParent(window.transform, false);
                    Stretch((RectTransform)labelObject.transform); var label = labelObject.GetComponent<Text>();
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = target + " · isolated preview region";
                    label.fontSize = 22; label.color = new Color(.7f, .8f, .7f); label.alignment = TextAnchor.UpperLeft; label.raycastTarget = false;
                }
                HorrorFoundationSetup.BindVisual(surface);
                if (target == EffectTarget.Global)
                {
                    surface.lighting = go.AddComponent<EffectLightingExecutor>(); surface.lighting.atmosphere = atmosphere;
                    surface.shake.shake = frame.AddComponent<ImpactShake>();
                }
            }
            // The illustrated preview responds to atmosphere.Light through the existing adapter.
            var watercolor = background.gameObject.AddComponent<PreviewIllustrationLight>(); watercolor.atmosphere = atmosphere; watercolor.image = background;
            var definitions = AssetDatabase.FindAssets("t:EffectDefinition", new[] { HorrorFoundationSetup.Folder }).Select(g => AssetDatabase.LoadAssetAtPath<EffectDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            director.Configure(definitions, surfaces, audio);
            var binder = canvasObject.AddComponent<HorrorPreviewAudioBinding>(); binder.director = director;
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

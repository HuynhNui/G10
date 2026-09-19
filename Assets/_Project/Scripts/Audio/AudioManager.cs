using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace G10.Prototype.Audio
{
    public enum SoundEffect
    {
        ButtonClick,
        ButtonBack,
        ItemClick,
        RadarPing,
        CaptureGrab,
        CaptureSuccess,
        CaptureFail,
        PauseMenu,
        CameraShutter,
        ZipOpen,
        ZipClose
    }

    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Volume Controls")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.5f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip buttonBackClip;
        [SerializeField] private AudioClip itemClickClip;
        [SerializeField] private AudioClip radarPingClip;
        [SerializeField] private AudioClip radarPingSingleClip;
        [SerializeField] private AudioClip captureGrabClip;
        [SerializeField] private AudioClip captureSuccessClip;
        [SerializeField] private AudioClip captureFailClip;
        [SerializeField] private AudioClip pauseMenuClip;
        [SerializeField] private AudioClip ambientSubmarineClip;
        [SerializeField] private AudioClip cameraShutterClip;
        [SerializeField] private AudioClip zipOpenClip;
        [SerializeField] private AudioClip zipCloseClip;

        private AudioSource sfxSource;
        private AudioSource radarSource;
        private AudioSource captureSource;
        private AudioSource ambientSource;

        private readonly HashSet<Button> boundButtons = new();
        private Coroutine ambientFadeCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("AudioManager");
                DontDestroyOnLoad(go);
                go.AddComponent<AudioManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitAudioSources();
            LoadClipsIfMissing();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureEventSystemExists()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var existing = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
                if (existing == null)
                {
                    var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                    DontDestroyOnLoad(esGo);
                }
            }
        }

        private void Start()
        {
            EnsureEventSystemExists();
            AutoBindAllButtonsInActiveScene();
            UpdateAmbientForScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void InitAudioSources()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f; // 2D UI

            radarSource = gameObject.AddComponent<AudioSource>();
            radarSource.playOnAwake = false;
            radarSource.loop = false;
            radarSource.spatialBlend = 0f;

            captureSource = gameObject.AddComponent<AudioSource>();
            captureSource.playOnAwake = false;
            captureSource.loop = false;
            captureSource.spatialBlend = 0f;

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = ambientVolume * masterVolume;
        }

        private void LoadClipsIfMissing()
        {
#if UNITY_EDITOR
            LoadEditorClipsIfMissing();
#endif
            if (buttonClickClip == null) buttonClickClip = Resources.Load<AudioClip>("Audio/SFX/ui_button_click");
            if (buttonBackClip == null) buttonBackClip = Resources.Load<AudioClip>("Audio/SFX/ui_button_back");
            if (itemClickClip == null) itemClickClip = Resources.Load<AudioClip>("Audio/SFX/ui_item_click");
            if (radarPingClip == null) radarPingClip = Resources.Load<AudioClip>("Audio/SFX/radar_ping");
            if (radarPingSingleClip == null) radarPingSingleClip = Resources.Load<AudioClip>("Audio/SFX/radar_ping_single");
            if (captureGrabClip == null) captureGrabClip = Resources.Load<AudioClip>("Audio/SFX/capture_grab");
            if (captureSuccessClip == null) captureSuccessClip = Resources.Load<AudioClip>("Audio/SFX/capture_success");
            if (captureFailClip == null) captureFailClip = Resources.Load<AudioClip>("Audio/SFX/capture_fail");
            if (pauseMenuClip == null) pauseMenuClip = Resources.Load<AudioClip>("Audio/SFX/ui_pause");
            if (ambientSubmarineClip == null) ambientSubmarineClip = Resources.Load<AudioClip>("Audio/Ambient/ambient_submarine_loop");
            if (cameraShutterClip == null) cameraShutterClip = Resources.Load<AudioClip>("Audio/SFX/camera_shutter");
            if (zipOpenClip == null) zipOpenClip = Resources.Load<AudioClip>("Audio/SFX/zip_open");
            if (zipCloseClip == null) zipCloseClip = Resources.Load<AudioClip>("Audio/SFX/zip_close");

            // Procedural synthesis fallback if any clip could not be loaded from disk/resources
            EnsureProceduralFallbacks();
        }

#if UNITY_EDITOR
        private void LoadEditorClipsIfMissing()
        {
            if (buttonClickClip == null) buttonClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/ui_button_click.wav");
            if (buttonBackClip == null) buttonBackClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/ui_button_back.wav");
            if (itemClickClip == null) itemClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/ui_item_click.wav");
            if (radarPingClip == null) radarPingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/radar_ping.wav")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Radar_Sound_Effect.mp3");
            if (radarPingSingleClip == null) radarPingSingleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/radar_ping_single.wav");
            if (captureGrabClip == null) captureGrabClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/capture_grab.wav");
            if (captureSuccessClip == null) captureSuccessClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/capture_success.wav");
            if (captureFailClip == null) captureFailClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/capture_fail.wav");
            if (pauseMenuClip == null) pauseMenuClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/ui_pause.wav");
            if (ambientSubmarineClip == null) ambientSubmarineClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Ambient/ambient_submarine_loop.wav")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Diving_Sea_Ambience.mp3");
            if (cameraShutterClip == null) cameraShutterClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/camera_shutter.wav")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Camera Shutter Sound Effects.mp3");
            if (zipOpenClip == null) zipOpenClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/zip_open.wav")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Sound zip open bag.mp3");
            if (zipCloseClip == null) zipCloseClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/zip_close.wav")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Sound zip close bag.mp3");
        }
#endif

        private void EnsureProceduralFallbacks()
        {
            if (buttonClickClip == null) buttonClickClip = CreateProceduralClick();
            if (buttonBackClip == null) buttonBackClip = CreateProceduralBack();
            if (itemClickClip == null) itemClickClip = CreateProceduralItem();
            if (radarPingClip == null) radarPingClip = CreateProceduralRadar();
            if (captureGrabClip == null) captureGrabClip = CreateProceduralGrab();
            if (captureSuccessClip == null) captureSuccessClip = CreateProceduralSuccess();
            if (captureFailClip == null) captureFailClip = CreateProceduralFail();
            if (pauseMenuClip == null) pauseMenuClip = CreateProceduralPause();
            if (ambientSubmarineClip == null) ambientSubmarineClip = CreateProceduralAmbient();
            if (cameraShutterClip == null) cameraShutterClip = CreateProceduralShutter();
            if (zipOpenClip == null) zipOpenClip = CreateProceduralZipOpen();
            if (zipCloseClip == null) zipCloseClip = CreateProceduralZipClose();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureEventSystemExists();
            boundButtons.RemoveWhere(b => b == null);
            AutoBindAllButtonsInActiveScene();
            UpdateAmbientForScene(scene.name);
        }

        private void UpdateAmbientForScene(string sceneName)
        {
            bool isGameplay = sceneName.StartsWith("Zone") || sceneName == "GameplayCore";
            if (isGameplay)
            {
                PlayAmbientSubmarine(true);
            }
            else
            {
                StopAmbient(true);
            }
        }

        public void Play(SoundEffect effect, float volumeMultiplier = 1f)
        {
            switch (effect)
            {
                case SoundEffect.ButtonClick:
                    PlayButtonClick(volumeMultiplier);
                    break;
                case SoundEffect.ButtonBack:
                    PlayButtonBack(volumeMultiplier);
                    break;
                case SoundEffect.ItemClick:
                    PlayItemClick(volumeMultiplier);
                    break;
                case SoundEffect.RadarPing:
                    PlayRadarPing(volumeMultiplier);
                    break;
                case SoundEffect.CaptureGrab:
                    PlayCaptureGrab(volumeMultiplier);
                    break;
                case SoundEffect.CaptureSuccess:
                    PlayCaptureSuccess(volumeMultiplier);
                    break;
                case SoundEffect.CaptureFail:
                    PlayCaptureFail(volumeMultiplier);
                    break;
                case SoundEffect.PauseMenu:
                    PlayPauseMenu(volumeMultiplier);
                    break;
                case SoundEffect.CameraShutter:
                    PlayCameraShutter(volumeMultiplier);
                    break;
                case SoundEffect.ZipOpen:
                    PlayZipOpen(volumeMultiplier);
                    break;
                case SoundEffect.ZipClose:
                    PlayZipClose(volumeMultiplier);
                    break;
            }
        }

        public void PlayButtonClick(float volumeMultiplier = 1f)
        {
            PlayClip(sfxSource, buttonClickClip, volumeMultiplier * 0.9f);
        }

        public void PlayButtonBack(float volumeMultiplier = 1f)
        {
            PlayClip(sfxSource, buttonBackClip, volumeMultiplier * 0.85f);
        }

        public void PlayItemClick(float volumeMultiplier = 1f)
        {
            PlayClip(sfxSource, itemClickClip, volumeMultiplier * 1.0f);
        }

        public void StartRadarLoop(float volumeMultiplier = 1f)
        {
            if (radarSource == null || radarPingClip == null) return;
            radarSource.clip = radarPingClip;
            radarSource.loop = true;
            radarSource.volume = Mathf.Clamp01(sfxVolume * masterVolume * volumeMultiplier);
            radarSource.Play();
        }

        public void StopRadarPing()
        {
            if (radarSource != null)
            {
                radarSource.Stop();
                radarSource.loop = false;
            }
        }

        public void PlayRadarPing(float volumeMultiplier = 1f)
        {
            var clip = radarPingSingleClip != null ? radarPingSingleClip : radarPingClip;
            PlayClip(radarSource, clip, volumeMultiplier * 1.0f);
        }

        public void PlayCaptureGrab(float volumeMultiplier = 1f)
        {
            PlayClip(captureSource, captureGrabClip, volumeMultiplier * 1.0f);
        }

        public void PlayCaptureSuccess(float volumeMultiplier = 1f)
        {
            PlayClip(captureSource, captureSuccessClip, volumeMultiplier * 1.0f);
        }

        public void PlayCaptureFail(float volumeMultiplier = 1f)
        {
            PlayClip(captureSource, captureFailClip, volumeMultiplier * 0.9f);
        }

        public void PlayPauseMenu(float volumeMultiplier = 1f)
        {
            PlayClip(sfxSource, pauseMenuClip, volumeMultiplier * 0.9f);
        }

        public void PlayCameraShutter(float volumeMultiplier = 1f)
        {
            PlayClip(sfxSource, cameraShutterClip, volumeMultiplier * 1.0f);
        }

        public void PlayZipOpen(float volumeMultiplier = 1f)
        {
            PlayClip(sfxSource, zipOpenClip, volumeMultiplier * 1.0f);
        }

        public void PlayZipClose(float volumeMultiplier = 1f)
        {
            PlayClip(sfxSource, zipCloseClip, volumeMultiplier * 1.0f);
        }

        public void PlayAmbientSubmarine(bool fadeIn = true)
        {
            if (ambientSource == null || ambientSubmarineClip == null) return;
            if (ambientSource.isPlaying && ambientSource.clip == ambientSubmarineClip) return;

            ambientSource.clip = ambientSubmarineClip;
            ambientSource.loop = true;

            if (ambientFadeCoroutine != null) StopCoroutine(ambientFadeCoroutine);

            if (fadeIn)
            {
                ambientSource.volume = 0f;
                ambientSource.Play();
                ambientFadeCoroutine = StartCoroutine(FadeAudioSource(ambientSource, ambientVolume * masterVolume, 1.5f));
            }
            else
            {
                ambientSource.volume = ambientVolume * masterVolume;
                ambientSource.Play();
            }
        }

        public void StopAmbient(bool fadeOut = true)
        {
            if (ambientSource == null || !ambientSource.isPlaying) return;

            if (ambientFadeCoroutine != null) StopCoroutine(ambientFadeCoroutine);

            if (fadeOut)
            {
                ambientFadeCoroutine = StartCoroutine(FadeAudioSource(ambientSource, 0f, 1f, () => ambientSource.Stop()));
            }
            else
            {
                ambientSource.Stop();
            }
        }

        private void PlayClip(AudioSource source, AudioClip clip, float volumeFactor)
        {
            if (source == null || clip == null) return;
            float finalVolume = Mathf.Clamp01(sfxVolume * masterVolume * volumeFactor);
            source.PlayOneShot(clip, finalVolume);
        }

        private IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration, Action onComplete = null)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
            onComplete?.Invoke();
        }

        public void AutoBindAllButtonsInActiveScene()
        {
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var btn in allButtons)
            {
                if (btn == null || boundButtons.Contains(btn)) continue;

                string n = btn.gameObject.name.ToLowerInvariant();
                bool isBack = n.Contains("back") || n.Contains("close") || n.Contains("thoát") || n.Contains("quay lại");

                if (isBack)
                {
                    btn.onClick.AddListener(() => PlayButtonBack());
                }
                else
                {
                    btn.onClick.AddListener(() => PlayButtonClick());
                }

                boundButtons.Add(btn);
            }
        }

        #region Procedural Audio Synthesizers (100% Zero-Dependency Fallback)
        private static AudioClip CreateProceduralClick()
        {
            const int sampleRate = 44100;
            const float duration = 0.055f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];
            var rng = new System.Random(42);

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t / 0.003f);
                float snap = Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t / 0.012f);
                float thud = Mathf.Sin(2f * Mathf.PI * 850f * t) * Mathf.Exp(-t / 0.025f);
                data[i] = (0.5f * noise + 0.6f * snap + 0.5f * thud) * 0.7f;
            }

            var clip = AudioClip.Create("ui_button_click_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralBack()
        {
            const int sampleRate = 44100;
            const float duration = 0.075f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 680f - 260f * (t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t / 0.028f);
                float sub = 0.4f * Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * Mathf.Exp(-t / 0.035f);
                data[i] = (tone + sub) * 0.7f;
            }

            var clip = AudioClip.Create("ui_button_back_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralItem()
        {
            const int sampleRate = 44100;
            const float duration = 0.26f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float fInit = 1200f + 400f * Mathf.Min(1f, t / 0.008f);
                float chirp = Mathf.Sin(2f * Mathf.PI * fInit * t) * Mathf.Exp(-t / 0.015f);
                float bell1 = Mathf.Sin(2f * Mathf.PI * 1568f * t) * Mathf.Exp(-t / 0.07f);
                float bell2 = 0.6f * Mathf.Sin(2f * Mathf.PI * 2349f * t) * Mathf.Exp(-t / 0.05f);
                float sparkle = 0.3f * Mathf.Sin(2f * Mathf.PI * 3136f * t) * Mathf.Exp(-t / 0.03f);
                data[i] = (0.35f * chirp + 0.7f * bell1 + bell2 + sparkle) * 0.6f;
            }

            var clip = AudioClip.Create("ui_item_click_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralRadar()
        {
            const int sampleRate = 44100;
            const float duration = 0.65f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];

            (float startT, float fBase, float decayTime, float amp)[] pings =
            {
                (0.00f, 1760.0f, 0.35f, 1.0f),
                (0.14f, 2217.4f, 0.45f, 0.88f)
            };

            foreach (var (startT, fBase, decayTime, amp) in pings)
            {
                int startIdx = (int)(startT * sampleRate);
                for (int i = startIdx; i < n; i++)
                {
                    float t = (i - startIdx) / (float)sampleRate;
                    float env = Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t / decayTime);
                    float p = fBase - 20f * Mathf.Exp(-t / 0.015f);
                    float tone = Mathf.Sin(2f * Mathf.PI * p * t) * env;
                    float h2 = 0.35f * Mathf.Sin(2f * Mathf.PI * (p * 2f) * t) * (Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t / (decayTime * 0.6f)));
                    float h3 = 0.15f * Mathf.Sin(2f * Mathf.PI * (p * 3f) * t) * (Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t / (decayTime * 0.4f)));
                    float hull = 0.25f * Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t / 0.08f);

                    data[i] += (tone + h2 + h3 + hull) * amp;
                }
            }

            int reverbDelay = (int)(0.08f * sampleRate);
            for (int i = reverbDelay; i < n; i++)
            {
                data[i] += data[i - reverbDelay] * 0.18f;
            }

            for (int i = 0; i < n; i++) data[i] *= 0.7f;

            var clip = AudioClip.Create("radar_ping_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralGrab()
        {
            const int sampleRate = 44100;
            const float duration = 0.45f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];
            var rng = new System.Random(123);

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float surge = 0f;
                if (t < 0.15f)
                {
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    surge = 0.5f * noise * Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-t / 0.04f);
                }

                float clamp = 0f;
                if (t >= 0.04f)
                {
                    float dt = t - 0.04f;
                    clamp += 0.8f * Mathf.Sin(2f * Mathf.PI * 320f * dt) * Mathf.Exp(-dt / 0.025f);
                    clamp += 0.5f * Mathf.Sin(2f * Mathf.PI * 720f * dt) * Mathf.Exp(-dt / 0.015f);
                }
                if (t >= 0.08f)
                {
                    float dt = t - 0.08f;
                    clamp += 0.6f * Mathf.Sin(2f * Mathf.PI * 240f * dt) * Mathf.Exp(-dt / 0.035f);
                }

                float ratchet = 0f;
                float[] clicks = { 0.14f, 0.20f, 0.26f, 0.32f };
                foreach (float clickT in clicks)
                {
                    float dt = t - clickT;
                    if (dt >= 0f && dt < 0.03f)
                    {
                        ratchet += 0.45f * Mathf.Sin(2f * Mathf.PI * 1850f * dt) * Mathf.Exp(-dt / 0.005f);
                    }
                }

                data[i] = (surge + clamp + ratchet) * 0.7f;
            }

            var clip = AudioClip.Create("capture_grab_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralSuccess()
        {
            const int sampleRate = 44100;
            const float duration = 1.2f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];

            (float start, float freq, float sustain)[] notes =
            {
                (0.00f, 523.25f, 0.4f),
                (0.12f, 659.25f, 0.45f),
                (0.24f, 783.99f, 0.5f),
                (0.36f, 1046.50f, 0.8f),
            };

            foreach (var note in notes)
            {
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / sampleRate;
                    float dt = t - note.start;
                    if (dt >= 0f)
                    {
                        float env = Mathf.Min(1f, dt / 0.008f) * Mathf.Exp(-dt / note.sustain);
                        float tone = Mathf.Sin(2f * Mathf.PI * note.freq * dt) * env;
                        float overtone = 0.35f * Mathf.Sin(2f * Mathf.PI * (note.freq * 2f) * dt) *
                                         (Mathf.Min(1f, dt / 0.006f) * Mathf.Exp(-dt / (note.sustain * 0.6f)));
                        float shimmer = note.freq > 1000f
                            ? 0.2f * Mathf.Sin(2f * Mathf.PI * (note.freq * 3f) * dt) *
                              (Mathf.Min(1f, dt / 0.005f) * Mathf.Exp(-dt / 0.4f))
                            : 0f;

                        data[i] += tone + overtone + shimmer;
                    }
                }
            }

            for (int i = 0; i < n; i++) data[i] *= 0.5f;

            var clip = AudioClip.Create("capture_success_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralFail()
        {
            const int sampleRate = 44100;
            const float duration = 0.45f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 340f - 200f * (t / duration);
                float mod = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 16f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t / 0.22f) * mod;
                float clang = 0.4f * Mathf.Sin(2f * Mathf.PI * 420f * t) * Mathf.Exp(-t / 0.06f);
                data[i] = (tone + clang) * 0.7f;
            }

            var clip = AudioClip.Create("capture_fail_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralPause()
        {
            const int sampleRate = 44100;
            const float duration = 0.5f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Min(1f, t / 0.035f) * Mathf.Exp(-t / 0.22f);
                float tone1 = Mathf.Sin(2f * Mathf.PI * 440f * t) * env;
                float tone2 = 0.7f * Mathf.Sin(2f * Mathf.PI * 659.25f * t) * env;
                data[i] = (0.6f * tone1 + 0.5f * tone2) * 0.7f;
            }

            var clip = AudioClip.Create("ui_pause_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralAmbient()
        {
            const int sampleRate = 44100;
            const float duration = 4f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float hum1 = 0.45f * Mathf.Sin(2f * Mathf.PI * 58f * t);
                float hum2 = 0.25f * Mathf.Sin(2f * Mathf.PI * 116f * t);
                float hum3 = 0.10f * Mathf.Sin(2f * Mathf.PI * 174f * t);
                data[i] = (hum1 + hum2 + hum3) * 0.35f;
            }

            int fadeLen = (int)(sampleRate * 0.2f);
            for (int i = 0; i < fadeLen; i++)
            {
                float w = (float)i / fadeLen;
                data[i] = data[i] * w + data[n - fadeLen + i] * (1f - w);
                data[n - fadeLen + i] = data[i];
            }

            var clip = AudioClip.Create("ambient_submarine_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralShutter()
        {
            const int sampleRate = 44100;
            const float duration = 0.28f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];
            var rng = new System.Random(101);

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float sig = 0f;
                if (t < 0.025f)
                {
                    float noise1 = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t / 0.004f);
                    float click1 = Mathf.Sin(2f * Mathf.PI * 2800f * t) * Mathf.Exp(-t / 0.006f);
                    sig += 0.4f * noise1 + 0.5f * click1;
                }
                if (t >= 0.035f)
                {
                    float dt = t - 0.035f;
                    float noise2 = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-dt / 0.008f);
                    float snap1 = Mathf.Sin(2f * Mathf.PI * 1450f * dt) * Mathf.Exp(-dt / 0.018f);
                    float thud1 = Mathf.Sin(2f * Mathf.PI * 520f * dt) * Mathf.Exp(-dt / 0.025f);
                    sig += 0.8f * snap1 + 0.6f * thud1 + 0.5f * noise2;
                }
                if (t >= 0.095f)
                {
                    float dt2 = t - 0.095f;
                    float noise3 = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-dt2 / 0.005f);
                    float snap2 = Mathf.Sin(2f * Mathf.PI * 1950f * dt2) * Mathf.Exp(-dt2 / 0.015f);
                    float metallic = 0.4f * Mathf.Sin(2f * Mathf.PI * 3400f * dt2) * Mathf.Exp(-dt2 / 0.012f);
                    sig += 0.7f * snap2 + metallic + 0.4f * noise3;
                }
                data[i] = sig * 0.7f;
            }

            var clip = AudioClip.Create("camera_shutter_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralZipOpen()
        {
            const int sampleRate = 44100;
            const float duration = 0.32f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];
            var rng = new System.Random(202);

            const int nTeeth = 34;
            for (int tooth = 0; tooth < nTeeth; tooth++)
            {
                float frac = (float)tooth / nTeeth;
                float tClick = 0.01f + frac * 0.23f;
                int idx = (int)(tClick * sampleRate);
                float freq = 900f + 1400f * frac;
                int clickLen = (int)(0.012f * sampleRate);

                for (int j = 0; j < clickLen; j++)
                {
                    int cur = idx + j;
                    if (cur >= n) break;
                    float tj = (float)j / sampleRate;
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-tj / 0.002f);
                    float tone = Mathf.Sin(2f * Mathf.PI * freq * tj) * Mathf.Exp(-tj / 0.004f);
                    data[cur] += (0.45f * noise + 0.55f * tone) * 0.7f;
                }
            }

            int tEnd = (int)(0.25f * sampleRate);
            for (int j = 0; j < (int)(0.06f * sampleRate); j++)
            {
                int cur = tEnd + j;
                if (cur >= n) break;
                float tj = (float)j / sampleRate;
                float clink = Mathf.Sin(2f * Mathf.PI * 3200f * tj) * Mathf.Exp(-tj / 0.015f);
                data[cur] += clink * 0.35f;
            }

            var clip = AudioClip.Create("zip_open_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralZipClose()
        {
            const int sampleRate = 44100;
            const float duration = 0.32f;
            int n = (int)(sampleRate * duration);
            float[] data = new float[n];
            var rng = new System.Random(303);

            const int nTeeth = 34;
            for (int tooth = 0; tooth < nTeeth; tooth++)
            {
                float frac = (float)tooth / nTeeth;
                float tClick = 0.01f + frac * 0.23f;
                int idx = (int)(tClick * sampleRate);
                float freq = 2300f - 1450f * frac;
                int clickLen = (int)(0.012f * sampleRate);

                for (int j = 0; j < clickLen; j++)
                {
                    int cur = idx + j;
                    if (cur >= n) break;
                    float tj = (float)j / sampleRate;
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-tj / 0.002f);
                    float tone = Mathf.Sin(2f * Mathf.PI * freq * tj) * Mathf.Exp(-tj / 0.004f);
                    data[cur] += (0.45f * noise + 0.55f * tone) * 0.7f;
                }
            }

            int tEnd = (int)(0.25f * sampleRate);
            for (int j = 0; j < (int)(0.06f * sampleRate); j++)
            {
                int cur = tEnd + j;
                if (cur >= n) break;
                float tj = (float)j / sampleRate;
                float thud = Mathf.Sin(2f * Mathf.PI * 620f * tj) * Mathf.Exp(-tj / 0.02f);
                data[cur] += thud * 0.6f;
            }

            var clip = AudioClip.Create("zip_close_proc", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion
    }
}

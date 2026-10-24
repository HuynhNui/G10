using G10.Prototype.Audio;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Only the isolated preview: serialized content, same director/executors as the cabin.</summary>
    public sealed class HorrorPreviewAudioBinding : MonoBehaviour
    {
        public EffectDirector director;
        public EffectSequence knocks, combined;
        public EffectDefinition[] ambience;
        public static readonly string[] PresetNames = {"Three metallic knocks", "Outside hull scraping", "Fluorescent flicker", "Radar signal interference", "Analog screen disturbance", "Heavy hull pressure", "Brief silhouette · temporary art", "Combined horror sequence", "Recover to normal"};
        private EffectHandle playing;
        private string ambientId;
        private int ambientIndex;
        public string CurrentPreset { get; private set; } = "Ready";
        public float StartedAt { get; private set; }
        private void Start() { director.BindAudio(AudioManager.Instance); SetAmbience(0, 1); }
        public void SetAmbience(int index, float volume)
        {
            if (ambience == null || ambience.Length == 0) return;
            ambientIndex = Mathf.Clamp(index, 0, ambience.Length - 1);
            var definition = ambience[ambientIndex];
            if (definition == null) return;
            if (!string.IsNullOrEmpty(ambientId) && ambientId != definition.id) director.SetPersistentState(ambientId, false);
            ambientId = definition.id;
            director.SetPersistentState(ambientId, true, new EffectContext { AudioMultiplier = volume });
        }
        public EffectHandle PlayPreset(int number, float intensity = .75f, float volume = 1, AudioClip sample = null, Sprite sprite = null)
        {
            if (number < 1 || number > 9) return default;
            director.ClearTransients();
            // The combined demonstration always uses its synchronized authored clips. A sprite
            // override belongs only to the silhouette preset, never to mix/light/screen cues.
            var context = new EffectContext { Intensity = intensity, AudioMultiplier = volume,
                Clip = number == 8 ? null : sample, Sprite = number == 7 ? sprite : null };
            CurrentPreset = PresetNames[number - 1]; StartedAt = Time.time;
            if (number == 9) { SetAmbience(ambientIndex, volume); playing = default; return default; }
            if (number == 1 || number == 8) playing = director.PlaySequence(number == 1 ? knocks : combined, context);
            else
            {
                string id = number switch { 2 => "horror.scrape", 3 => "light.SubtleFlicker", 4 => "ScreenSignalInterference", 5 => "AnalogSignalFailure", 6 => "horror.pressure", _ => "horror.silhouette" };
                playing = director.Play(id, context);
            }
            return playing;
        }
        public void StopPreview() { director.ClearAll(); playing = default; CurrentPreset = "Stopped / reset"; }
        private void Update()
        { if (playing.IsValid && !director.IsActive(playing)) { playing = default; CurrentPreset = "Ready · restored"; } }
    }
}

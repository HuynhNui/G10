using System.Collections.Generic;
using G10.Prototype.Audio;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Bounded, scene-local reusable voices. Handles never stop somebody else's sound.</summary>
    public class EffectAudioExecutor : MonoBehaviour
    {
        private sealed class Voice
        {
            public AudioSource Source;
            public long Id;
            public float Gain, Current, Fade;
            public bool Loop, Stopping;
        }
        private readonly List<Voice> voices = new();
        private AudioManager mixer;
        public int PlayingCount { get { int count = 0; foreach (var v in voices) if (v.Id != 0) count++; return count; } }
        public void Bind(AudioManager value)
        {
            if (mixer == value) return;
            foreach (var voice in voices) mixer?.UnregisterFeedbackSource(voice.Source);
            mixer = value;
            foreach (var voice in voices) mixer?.RegisterFeedbackSource(voice.Source);
        }
        public virtual void Play(long id, AudioClip clip, bool loop, float gain, float pitch, float fade)
        {
            if (!isActiveAndEnabled || clip == null) return;
            Voice voice = null;
            foreach (var item in voices) if (item.Id == id) { voice = item; break; }
            if (voice == null) foreach (var item in voices) if (item.Id == 0) { voice = item; break; }
            if (voice == null)
            {
                if (voices.Count >= 16) return; // Bounded presentation capacity; never steal an unrelated voice.
                voice = new Voice(); voices.Add(voice);
            }
            if (voice.Source == null)
            {
                mixer?.UnregisterFeedbackSource(voice.Source);
                voice.Source = gameObject.AddComponent<AudioSource>(); voice.Id = 0;
                voice.Source.playOnAwake = false; voice.Source.spatialBlend = 0;
                mixer?.RegisterFeedbackSource(voice.Source);
            }
            bool restart = voice.Id != id || voice.Source.clip != clip;
            voice.Id = id; voice.Loop = loop; voice.Gain = Mathf.Clamp01(gain); voice.Fade = Mathf.Max(.01f, fade); voice.Stopping = false;
            voice.Source.loop = loop; voice.Source.pitch = Mathf.Clamp(pitch, .9f, 1.1f);
            if (restart)
            {
                voice.Source.Stop(); voice.Source.clip = clip; voice.Current = loop ? 0 : voice.Gain;
                voice.Source.volume = voice.Current * Mix(loop); voice.Source.Play();
            }
        }
        private float Mix(bool loop) => mixer == null ? 1 : loop ? mixer.FeedbackAmbientGain : mixer.FeedbackSfxGain;
        public virtual void Stop(long id, bool fade = false)
        {
            foreach (var voice in voices)
            {
                if (voice.Id != id) continue;
                if (fade && voice.Loop) { voice.Stopping = true; voice.Gain = 0; }
                else Release(voice);
            }
        }
        private static void Release(Voice voice)
        {
            voice.Id = 0;
            if (voice.Source == null) return;
            voice.Source.Stop(); voice.Source.clip = null; voice.Source.volume = 0;
        }
        public virtual void Tick(float dt, bool paused)
        {
            foreach (var voice in voices)
            {
                if (voice.Id == 0) continue;
                if (voice.Source == null) { voice.Id = 0; continue; }
                if (paused) { voice.Source.Pause(); continue; }
                voice.Source.UnPause();
                if (voice.Loop) voice.Current = Mathf.MoveTowards(voice.Current, voice.Gain, dt / voice.Fade);
                voice.Source.volume = voice.Current * Mix(voice.Loop);
                if (voice.Stopping && voice.Current == 0 || !voice.Loop && !voice.Source.isPlaying) Release(voice);
            }
        }
        public virtual void Restore() { foreach (var voice in voices) Release(voice); }
        private void OnDisable() => Restore();
        private void OnDestroy() { foreach (var voice in voices) mixer?.UnregisterFeedbackSource(voice.Source); }
    }
}

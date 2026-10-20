using System.Collections.Generic;
using UnityEngine;

namespace G10.Prototype.Audio
{
    public sealed partial class AudioManager
    {
        private struct MixRequest { public HorrorMixState State; public int Priority; public long Order; public float Fade; }
        private readonly Dictionary<int, MixRequest> mixRequests = new();
        private readonly float[] categoryVolumes = new float[5];
        private HorrorAudioMix horrorMix;
        private long mixOrder;
        public HorrorAudioMix HorrorMix => horrorMix;
        public HorrorMixState CurrentMix { get; private set; } = HorrorMixState.Normal;

        private void InitializeHorrorMix()
        {
            horrorMix = Resources.Load<HorrorAudioMix>("Audio/HorrorAudioMix");
            for (int i = 0; i < categoryVolumes.Length; i++)
                categoryVolumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat("G10.Audio.Category." + (AudioCategory)i, 1));
            Route(sfxSource, AudioCategory.Interface); Route(radarSource, AudioCategory.Interface);
            Route(captureSource, AudioCategory.Interface); Route(ambientSource, AudioCategory.Machinery);
            Route(feedbackOneShotSource, AudioCategory.Impacts); Route(feedbackLoopSource, AudioCategory.Machinery);
            horrorMix?.normal?.TransitionTo(0);
        }
        public void Route(AudioSource source, AudioCategory category)
        { if (source != null && horrorMix != null) source.outputAudioMixerGroup = horrorMix.Group(category); }
        public float CategoryVolume(AudioCategory category) => categoryVolumes[(int)category];
        public void SetCategoryVolume(AudioCategory category, float value)
        {
            categoryVolumes[(int)category] = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("G10.Audio.Category." + category, categoryVolumes[(int)category]);
            ApplyCategoryVolumes();
            if (category == AudioCategory.Machinery && ambientSource != null && ambientSource.isPlaying)
            {
                if (ambientFadeCoroutine != null) { StopCoroutine(ambientFadeCoroutine); ambientFadeCoroutine = null; }
                ambientSource.volume = ambientVolume * masterVolume * CategoryVolume(category);
            }
            if (category == AudioCategory.Interface && radarSource != null && radarSource.loop)
                radarSource.volume = sfxVolume * masterVolume * CategoryVolume(category);
        }
        private void ApplyCategoryVolumes()
        {
            if (sfxSource != null) sfxSource.volume = CategoryVolume(AudioCategory.Interface);
            if (captureSource != null) captureSource.volume = CategoryVolume(AudioCategory.Interface);
            if (radarSource != null && !radarSource.loop) radarSource.volume = CategoryVolume(AudioCategory.Interface);
            if (feedbackOneShotSource != null) feedbackOneShotSource.volume = CategoryVolume(AudioCategory.Impacts);
        }
        // Each scene director owns only its request; releasing one cannot reset another scene's mix.
        public void SetPresentationMix(int owner, HorrorMixState state, int priority = 0, float fade = .4f)
        {
            if (state == HorrorMixState.None) mixRequests.Remove(owner);
            else if (!mixRequests.TryGetValue(owner, out var old) || old.State != state || old.Priority != priority)
                mixRequests[owner] = new MixRequest { State = state, Priority = priority, Order = ++mixOrder, Fade = fade };
            var winner = new MixRequest { State = HorrorMixState.Normal, Priority = int.MinValue, Fade = fade };
            foreach (var request in mixRequests.Values)
                if (request.Priority > winner.Priority || request.Priority == winner.Priority && request.Order > winner.Order) winner = request;
            if (CurrentMix == winner.State) return;
            CurrentMix = winner.State;
            horrorMix?.Snapshot(CurrentMix)?.TransitionTo(Mathf.Max(0, winner.Fade));
        }
    }
}

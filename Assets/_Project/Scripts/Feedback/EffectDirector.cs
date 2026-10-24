using System;
using System.Collections.Generic;
using G10.Prototype.Atmosphere;
using G10.Prototype.Audio;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Scene-scoped presentation only. Knows no expedition, save, ship, mission or navigation state.</summary>
    [DisallowMultipleComponent]
    public sealed class EffectDirector : MonoBehaviour
    {
        [SerializeField] private EffectDefinition[] definitions = Array.Empty<EffectDefinition>();
        [SerializeField] private EffectSurface[] surfaces = Array.Empty<EffectSurface>();
        [SerializeField] private EffectAudioExecutor audioExecutor;
        private sealed class Request
        {
            public long Id;
            public EffectDefinition Definition;
            public EffectContext Context;
            public EffectTarget Target;
            public float Age, Duration;
            public bool Persistent, SequenceOwned;
            public int Priority => Context.Priority ?? Definition.priority;
            public int Seed => Context.Seed ?? Definition.seed;
            public float ShakeDuration => Definition.shakeProfile == ShakeProfile.LegacyImpact || Definition.shakeProfile == ShakeProfile.ShortImpact
                ? Mathf.Clamp(Context.ShakeSeconds ?? Definition.shakeSeconds, .01f, .28f) : VisualDuration;
            public float Strength => Mathf.Max(0, Context.Intensity ?? Definition.intensity);
            public float VisualDuration => Mathf.Max(.01f, Context.Duration ?? Definition.duration);
        }
        private readonly List<Request> requests = new();
        private sealed class SequenceRequest
        {
            public long Id;
            public EffectSequence Definition;
            public EffectContext Context;
            public EffectTarget Target;
            public Func<bool> Condition;
            public float Age;
            public long ClockId;
            public bool[] Fired;
            public readonly List<(EffectHandle handle, float start, float end)> Children = new();
        }
        private readonly List<SequenceRequest> sequences = new();
        private AudioManager mixer;
        public int ActiveSequenceCount => sequences.Count;
        private long nextId;
        private bool suspended, hidePersistent, applicationPaused, clockPaused;
        public int ActiveTransientCount { get { int count = 0; foreach (var r in requests) if (!r.Persistent) count++; return count; } }
        public int PersistentCount => requests.Count - ActiveTransientCount;
        public bool Suppressed => suspended || applicationPaused || Time.timeScale <= 0;
        public void Configure(EffectDefinition[] catalog, EffectSurface[] targets, EffectAudioExecutor audio)
        { ClearAll(); definitions = catalog ?? Array.Empty<EffectDefinition>(); surfaces = targets ?? Array.Empty<EffectSurface>(); audioExecutor = audio; }
        public void BindAudio(AudioManager value)
        { if (mixer != value && mixer != null) mixer.SetPresentationMix(GetInstanceID(), HorrorMixState.None); mixer = value; audioExecutor?.Bind(value); }
        public EffectSurface Surface(EffectTarget target)
        { foreach (var surface in surfaces) if (surface != null && surface.target == target) return surface; return null; }
        public EffectDefinition Definition(string id)
        { if (string.IsNullOrWhiteSpace(id)) return null; foreach (var definition in definitions) if (definition != null && definition.id == id) return definition; return null; }
        public bool IsActive(EffectHandle handle)
        { if (handle.Owner != GetInstanceID()) return false; foreach (var r in requests) if (r.Id == handle.Id) return true; foreach (var r in sequences) if (r.Id == handle.Id) return true; return false; }
        public bool TryGetSequenceTime(EffectHandle handle, out float seconds)
        {
            if (handle.Owner == GetInstanceID())
                foreach (var sequence in sequences)
                    if (sequence.Id == handle.Id) { seconds = sequence.Age; return true; }
            seconds = 0; return false;
        }
        public bool IsPlaying(string id)
        { foreach (var r in requests) if (r.Definition.id == id) return true; return false; }
        /// <summary>Read the real voice clock, including a sequence child; a request alone is not delivery.</summary>
        public bool TryGetAudioTime(string definitionId, out float seconds)
        {
            foreach (var request in requests)
                if (request.Definition.id == definitionId && audioExecutor != null && audioExecutor.TryPlaybackTime(request.Id, out seconds)) return true;
            seconds = 0; return false;
        }
        public EffectHandle Play(string id, EffectContext context = default) => Play(Definition(id), context);
        public EffectHandle Play(EffectDefinition definition, EffectContext context = default)
        {
            // Check the gate before registering. No reset-on-resume may erase a new first-frame request.
            if (!isActiveAndEnabled || Suppressed) return default;
            if (definition == null) return default;
            var target = context.Target ?? definition.target;
            var surface = Surface(target);
            if (surface == null || !surface.Available) return default;
            if (definition.conflict != EffectConflict.Stack)
            {
                foreach (var other in requests)
                    if (!other.Persistent && other.Target == target && other.Definition.channel == definition.channel &&
                        (definition.conflict == EffectConflict.IgnoreWhileActive || other.Priority > (context.Priority ?? definition.priority))) return default;
                StopChannel(definition.channel, target);
            }
            if (requests.Count >= 128) return default;
            var request = Create(definition, context, target, false);
            requests.Add(request); StartAudio(request); Render(0);
            return new EffectHandle(GetInstanceID(), request.Id);
        }
        public EffectHandle PlaySequence(EffectSequence definition, EffectContext context = default, Func<bool> condition = null, float startAtSeconds = 0)
        {
            if (definition == null || !isActiveAndEnabled || Suppressed || sequences.Count >= 16 || !Permitted(condition)) return default;
            if (!float.IsFinite(startAtSeconds) || startAtSeconds < 0 || startAtSeconds >= definition.Duration) return default;
            var target = context.Target ?? definition.target;
            if (Surface(target) == null || !Surface(target).Available || definition.cues == null || definition.cues.Length == 0) return default;
            foreach (var cue in definition.cues) if (cue?.effect != null && cue.effect.channel == definition.channel) return default;
            if (definition.conflict != EffectConflict.Stack)
            {
                foreach (var other in sequences)
                    if (other.Target == target && other.Definition.channel == definition.channel &&
                        (definition.conflict == EffectConflict.IgnoreWhileActive || (other.Context.Priority ?? other.Definition.priority) > (context.Priority ?? definition.priority))) return default;
                StopChannel(definition.channel, target);
            }
            var sequence = new SequenceRequest { Id = ++nextId, Definition = definition, Context = context, Target = target,
                Condition = condition, Age = startAtSeconds, Fired = new bool[definition.cues.Length] };
            sequences.Add(sequence); AdvanceSequence(sequence, 0); Render(0);
            return new EffectHandle(GetInstanceID(), sequence.Id);
        }
        private static bool Permitted(Func<bool> condition)
        {
            if (condition == null) return true;
            try { return condition(); }
            catch (Exception) { Debug.LogWarning("Effect sequence condition failed; presentation cancelled."); return false; }
        }
        private void AdvanceSequence(SequenceRequest sequence, float dt)
        {
            sequence.Age += dt;
            for (int i = sequence.Children.Count - 1; i >= 0; i--)
                if (sequence.Age >= sequence.Children[i].end)
                { Stop(sequence.Children[i].handle); sequence.Children.RemoveAt(i); }
            for (int i = 0; i < sequence.Definition.cues.Length; i++)
            {
                var cue = sequence.Definition.cues[i];
                if (sequence.Fired[i] || cue == null || cue.effect == null || sequence.Age < cue.offset) continue;
                sequence.Fired[i] = true;
                // A hitch never replays an already expired one-shot/cue.
                if (sequence.Age >= cue.offset + cue.duration) continue;
                var context = sequence.Context;
                context.Duration = cue.duration;
                context.Intensity = (context.Intensity ?? 1) * cue.intensity * cue.effect.intensity;
                context.Priority = Mathf.Max(context.Priority ?? sequence.Definition.priority, cue.effect.priority);
                var handle = Play(cue.effect, context);
                foreach (var request in requests) if (request.Id == handle.Id)
                {
                    request.SequenceOwned = true;
                    float elapsed = Mathf.Max(0, sequence.Age - cue.offset);
                    if (elapsed > .02f) audioExecutor?.Seek(handle.Id, elapsed);
                    if (sequence.Definition.synchronizeToAudio && sequence.ClockId == 0 && cue.offset == 0 &&
                        !cue.effect.loop && (context.Clip != null || cue.effect.clip != null)) sequence.ClockId = handle.Id;
                }
                if (handle.IsValid) sequence.Children.Add((handle, cue.offset, cue.offset + cue.duration));
            }
            foreach (var child in sequence.Children)
                foreach (var request in requests)
                    if (request.Id == child.handle.Id) request.Age = Mathf.Max(0, sequence.Age - child.start);
        }
        private void RemoveSequence(int index)
        {
            var sequence = sequences[index]; sequences.RemoveAt(index);
            foreach (var child in sequence.Children) Stop(child.handle);
        }
        public EffectHandle SetPersistentState(string id, bool enabled, EffectContext context = default)
        {
            var definition = Definition(id);
            if (definition == null || !isActiveAndEnabled) return default;
            var target = context.Target ?? definition.target;
            if (Surface(target) == null) return default;
            Request existing = null;
            foreach (var r in requests) if (r.Persistent && r.Definition == definition && r.Target == target) { existing = r; break; }
            if (!enabled)
            {
                if (existing != null) { requests.Remove(existing); audioExecutor?.Stop(existing.Id, true); }
                Render(0, updateOverlays: false);
                return default;
            }
            if (existing == null)
            {
                if (requests.Count >= 128) return default;
                existing = Create(definition, context, target, true); requests.Add(existing);
            }
            existing.Context = context;
            if (!Suppressed && Surface(target).Available) StartAudio(existing);
            // Resource consumers must see this frame's baseline even if our Update ran earlier.
            // Leave overlay easing to Tick; do not snap resource graphics every coordinator update.
            Render(0, updateOverlays: false);
            return new EffectHandle(GetInstanceID(), existing.Id);
        }
        private Request Create(EffectDefinition definition, EffectContext context, EffectTarget target, bool persistent)
        {
            var clip = context.Clip != null ? context.Clip : definition.clip;
            float duration = Mathf.Max(.01f, context.Duration ?? definition.duration);
            if ((context.ShakePixels ?? definition.shakePixels) > 0) duration = Mathf.Max(duration, context.ShakeSeconds ?? definition.shakeSeconds);
            if (clip != null && !definition.loop) duration = Mathf.Max(duration, clip.length / Mathf.Clamp(context.Pitch ?? definition.pitch, .9f, 1.1f));
            return new Request { Id = ++nextId, Definition = definition, Context = context, Target = target, Persistent = persistent, Duration = duration };
        }
        private void StartAudio(Request r)
        {
            audioExecutor?.Play(r.Id, r.Context.Clip != null ? r.Context.Clip : r.Definition.clip, r.Definition.loop,
                (r.Context.Gain ?? r.Definition.gain) * (r.Context.AudioMultiplier ?? r.Strength), r.Context.Pitch ?? r.Definition.pitch, r.Definition.audioFade);
            ConfigureVoice(r);
        }
        private void ConfigureVoice(Request r)
        {
            var d = r.Definition;
            float envelope = d.fadeAudio && !r.Persistent ? EffectMotion.Envelope(r.Age, r.VisualDuration, d.fadeIn, d.fadeOut) : 1;
            audioExecutor?.ConfigureVoice(r.Id, d.audioCategory, r.Context.StereoPan ?? d.stereoPan, r.Context.LowPassHz ?? d.lowPassHz, d.reverb, envelope);
        }
        public void Stop(EffectHandle handle)
        {
            if (handle.Owner != GetInstanceID()) return;
            for (int i = sequences.Count - 1; i >= 0; i--) if (sequences[i].Id == handle.Id) RemoveSequence(i);
            for (int i = requests.Count - 1; i >= 0; i--) if (requests[i].Id == handle.Id) Remove(i);
            Render(0);
        }
        public void StopChannel(string channel, EffectTarget? target = null)
        {
            for (int i = sequences.Count - 1; i >= 0; i--)
                if (sequences[i].Definition.channel == channel && (!target.HasValue || sequences[i].Target == target)) RemoveSequence(i);
            for (int i = requests.Count - 1; i >= 0; i--)
                if (!requests[i].Persistent && requests[i].Definition.channel == channel && (!target.HasValue || requests[i].Target == target)) Remove(i);
            Render(0);
        }
        public void ClearTransients()
        {
            for (int i = sequences.Count - 1; i >= 0; i--) RemoveSequence(i);
            for (int i = requests.Count - 1; i >= 0; i--) if (!requests[i].Persistent) Remove(i);
            foreach (var surface in surfaces) if (surface != null) surface.shake?.Restore();
            Render(0);
        }
        public void ClearAll()
        {
            sequences.Clear(); requests.Clear(); audioExecutor?.Restore();
            if (mixer != null) mixer.SetPresentationMix(GetInstanceID(), HorrorMixState.None);
            foreach (var surface in surfaces) if (surface != null) surface.Restore();
        }
        private void Remove(int index) { audioExecutor?.Stop(requests[index].Id); requests.RemoveAt(index); }
        public void SetSuspended(bool value, bool hidePersistentOverlays = false)
        {
            if (suspended == value && hidePersistent == hidePersistentOverlays) return;
            if (value && !suspended) ClearTransients();
            suspended = value; hidePersistent = hidePersistentOverlays;
            Render(0); audioExecutor?.Tick(0, Suppressed);
        }
        private void OnApplicationPause(bool value)
        { applicationPaused = value; if (value) ClearTransients(); audioExecutor?.Tick(0, Suppressed); }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            bool paused = Suppressed;
            if (paused && !clockPaused) ClearTransients();
            clockPaused = paused;
            float dt = paused ? 0 : Mathf.Max(0, seconds);
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                var r = requests[i];
                var target = Surface(r.Target);
                if (r.Persistent)
                {
                    if (!paused) r.Age += dt;
                    if (target == null || !target.Available) audioExecutor?.Stop(r.Id);
                    else if (!paused) StartAudio(r);
                    continue;
                }
                if (!r.SequenceOwned) r.Age += dt;
                if (target == null || !target.Available || !r.SequenceOwned && r.Age >= r.Duration) Remove(i);
            }
            for (int i = sequences.Count - 1; i >= 0; i--)
            {
                var sequence = sequences[i]; var surface = Surface(sequence.Target);
                float advance = dt;
                bool lostClock = false;
                if (!paused && sequence.Definition.synchronizeToAudio && sequence.ClockId != 0)
                {
                    if (audioExecutor != null && audioExecutor.TryPlaybackTime(sequence.ClockId, out float position))
                        advance = Mathf.Max(0, position - sequence.Age);
                    else lostClock = true;
                }
                if (surface == null || !surface.Available || lostClock || !Permitted(sequence.Condition) || sequence.Age + advance >= sequence.Definition.Duration)
                { RemoveSequence(i); continue; }
                AdvanceSequence(sequence, advance);
            }
            Render(dt); audioExecutor?.Tick(dt, paused);
        }
        private void Render(float dt, bool updateOverlays = true)
        {
            foreach (var surface in surfaces)
            {
                if (surface == null) continue;
                surface.FitToAudience();
                bool visible = surface.Available && !hidePersistent;
                float hull = 0, energy = 0, tint = 0, dip = 0, instability = 1, energyFlicker = 0;
                Color tintColor = Color.black;
                CabinMood? mood = null; int moodPriority = int.MinValue;
                Request shake = null, notice = null, signal = null, silhouette = null;
                foreach (var r in requests)
                {
                    if (r.Target != surface.target) continue;
                    var d = r.Definition;
                    float envelope = r.Persistent || !d.decayOverlay ? 1 : Mathf.Clamp01(1 - r.Age / r.VisualDuration);
                    if (r.Persistent || !Suppressed)
                    {
                        float alpha = Mathf.Clamp01((r.Context.Alpha ?? d.alpha) * r.Strength * envelope);
                        if (d.overlay == EffectOverlay.Hull) hull = Mathf.Max(hull, alpha);
                        if (d.overlay == EffectOverlay.Energy) energy = Mathf.Max(energy, alpha);
                        if (d.overlay == EffectOverlay.Tint && alpha >= tint) { tint = alpha; tintColor = d.tintColor; }
                        if (d.overrideMood && r.Priority >= moodPriority) { mood = d.mood; moodPriority = r.Priority; }
                        instability = Mathf.Max(instability, r.Context.Instability ?? d.instability);
                        if (r.Persistent || r.Age < r.VisualDuration)
                        {
                            float flicker = d.lightDip * r.Strength * (d.lightBehavior != LightBehavior.Legacy
                                ? EffectMotion.Light(d.lightBehavior, r.Age, r.VisualDuration, r.Seed)
                                : d.flicker ? .35f + .65f * EffectMotion.Irregular(r.Age, r.Seed) : 1);
                            dip = Mathf.Max(dip, flicker);
                            if (d.flicker) energyFlicker = Mathf.Max(energyFlicker, flicker * (.04f / .35f));
                        }
                    }
                    if (!Suppressed && r.Persistent && d.interference != ScreenInterference.None && Better(r, signal)) signal = r;
                    if (r.Persistent || Suppressed) continue;
                    if ((r.Context.ShakePixels ?? d.shakePixels) > 0 && r.Age < r.ShakeDuration && Better(r, shake)) shake = r;
                    if (r.Age < r.VisualDuration && d.interference != ScreenInterference.None && Better(r, signal)) signal = r;
                    if (r.Age < r.VisualDuration && (r.Context.Sprite != null || d.silhouette != null) && Better(r, silhouette)) silhouette = r;
                    ConfigureVoice(r);
                    if (r.Age < r.VisualDuration && !string.IsNullOrEmpty(r.Context.Text ?? d.message) && Better(r, notice)) notice = r;
                }
                if (updateOverlays && surface.overlay != null && surface.overlay.isActiveAndEnabled)
                {
                    surface.overlay.SetTintColor(tintColor);
                    surface.overlay.Render(hull, Mathf.Min(.30f, energy + energyFlicker), tint, notice == null ? "" : notice.Context.Text ?? notice.Definition.message, dt, visible);
                }
                if (surface.lighting != null) surface.lighting.Render(visible ? mood : null, visible ? instability : 1, visible ? Mathf.Clamp01(dip) : 0);
                if (surface.shake != null) surface.shake.RenderProfile(visible && shake != null ? shake.Id : 0,
                    shake == null ? 0 : (shake.Context.ShakePixels ?? shake.Definition.shakePixels) * shake.Strength,
                    shake == null ? 0 : shake.ShakeDuration - shake.Age,
                    shake == null ? ShakeProfile.LegacyImpact : shake.Definition.shakeProfile,
                    shake == null ? Vector2.right : shake.Definition.shakeDirection, shake == null ? 0 : shake.Seed);
                if (surface.visual != null)
                {
                    float SignalStrength(Request r) => r == null ? 0 : r.Strength * (r.Persistent ? 1 : EffectMotion.Envelope(r.Age, r.VisualDuration, r.Definition.fadeIn, r.Definition.fadeOut));
                    var sd = silhouette?.Definition;
                    surface.visual.Render(signal == null ? ScreenInterference.None : signal.Definition.interference,
                        signal == null ? 0 : signal.Definition.interferenceIntensity * SignalStrength(signal), signal?.Age ?? 0, signal?.Seed ?? 0,
                        silhouette == null ? null : silhouette.Context.Sprite != null ? silhouette.Context.Sprite : sd.silhouette,
                        silhouette == null ? 0 : sd.silhouetteOpacity * SignalStrength(silhouette),
                        sd == null ? Vector2.zero : sd.silhouettePosition, sd == null ? Vector2.zero : sd.silhouetteSize, visible && !Suppressed);
                }
            }
            Request mix = null;
            if (!Suppressed) foreach (var r in requests)
                if (r.Definition.mixState != HorrorMixState.None && Surface(r.Target)?.Available == true && Better(r, mix)) mix = r;
            if (mixer != null) mixer.SetPresentationMix(GetInstanceID(), mix == null ? HorrorMixState.None : mix.Definition.mixState, mix?.Priority ?? 0, mix?.Definition.audioFade ?? .4f);
        }
        private static bool Better(Request candidate, Request current) => current == null || candidate.Priority > current.Priority ||
            candidate.Priority == current.Priority && candidate.Id > current.Id;
        private void OnDisable() => ClearAll();
        private void OnDestroy() => ClearAll();
    }
}

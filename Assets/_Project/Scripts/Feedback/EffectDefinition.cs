using G10.Prototype.Atmosphere;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    public enum EffectTarget { Global, Cabin, Helm, Radar, PhotoLab }
    public enum EffectConflict { Stack, ReplaceLowerPriority, IgnoreWhileActive }
    public enum EffectOverlay { None, Hull, Energy, Tint }

    [CreateAssetMenu(menuName = "G10/Feedback/Effect Definition")]
    public sealed class EffectDefinition : ScriptableObject
    {
        public string id, channel;
        public EffectTarget target;
        public EffectConflict conflict = EffectConflict.ReplaceLowerPriority;
        public int priority;
        [Min(.01f)] public float duration = .3f;
        [Min(0)] public float intensity = 1;
        [Header("Transform (shake preference affects only this module)")]
        [Range(0, 8)] public float shakePixels;
        [Range(.01f, .28f)] public float shakeSeconds = .2f;
        [Header("Screen")]
        public EffectOverlay overlay;
        [Range(0, 1)] public float alpha;
        public bool decayOverlay = true;
        [Header("Lighting")]
        public bool overrideMood;
        public CabinMood mood;
        [Range(0, 1)] public float lightDip;
        public bool flicker;
        [Range(.1f, 2)] public float instability = 1;
        [Header("Audio")]
        public AudioClip clip;
        public bool loop;
        [Range(0, 1)] public float gain = 1;
        [Range(.9f, 1.1f)] public float pitch = 1;
        [Min(.01f)] public float audioFade = .4f;
        [Header("UI")]
        [TextArea] public string message;
    }

    /// <summary>Per-request presentation overrides. Null means use the authored definition.</summary>
    public struct EffectContext
    {
        public EffectTarget? Target;
        public float? Duration, Intensity, ShakePixels, ShakeSeconds, Alpha, Gain, Pitch, Instability;
        public AudioClip Clip;
        public string Text;
    }

    public readonly struct EffectHandle
    {
        internal readonly int Owner;
        internal readonly long Id;
        internal EffectHandle(int owner, long id) { Owner = owner; Id = id; }
        public bool IsValid => Id != 0;
    }

    public static class CabinEffects
    {
        public const string Hull = "resource.hull", Energy = "resource.energy", Impact = "hull.impact";
        public const string Creak = "hull.creak", Warning = "hull.critical", PowerDown = "energy.powerdown";
        public const string Scrape = "capture.scrape", Flicker = "capture.flicker", Jitter = "capture.jitter";
        public const string Notice = "ui.notice", RockPulse = "rock.pulse", RockAudio = "rock.audio", RockNotice = "rock.notice";
    }
}

using G10.Prototype.Atmosphere;
using G10.Prototype.Audio;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    public enum EffectTarget { Global, Cabin, Helm, Radar, PhotoLab, Computer, Reflection }
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
        public ShakeProfile shakeProfile;
        public Vector2 shakeDirection = Vector2.right;
        [Header("Screen")]
        public EffectOverlay overlay;
        public Color tintColor = Color.black;
        [Range(0, 1)] public float alpha;
        public bool decayOverlay = true;
        [Header("Lighting")]
        public bool overrideMood;
        public CabinMood mood;
        [Range(0, 1)] public float lightDip;
        public bool flicker;
        [Range(.1f, 2)] public float instability = 1;
        public LightBehavior lightBehavior;
        public int seed = 147;
        [Header("Audio")]
        public AudioClip clip;
        public bool loop;
        public AudioCategory audioCategory;
        public bool fadeAudio;
        [Range(-1, 1)] public float stereoPan;
        [Range(10, 22000)] public float lowPassHz = 22000;
        [Range(0, 1)] public float reverb;
        public HorrorMixState mixState;
        [Header("Scoped visual presentation")]
        public ScreenInterference interference;
        [Range(0, 1)] public float interferenceIntensity = .25f;
        public Sprite silhouette;
        public Vector2 silhouettePosition;
        public Vector2 silhouetteSize = new Vector2(180, 240);
        [Range(0, 1)] public float silhouetteOpacity = .25f;
        [Min(0)] public float fadeIn = .12f, fadeOut = .5f;
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
        public float? AudioMultiplier;
        public float? LowPassHz;
        public AudioClip Clip;
        public Sprite Sprite;
        public int? Priority;
        public float? StereoPan;
        public int? Seed;
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

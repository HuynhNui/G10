using UnityEngine;

namespace G10.Prototype.Feedback
{
    public enum LightBehavior { Legacy, SubtleFlicker, PowerInstability, ElectricalFailure, PressureReaction }
    public enum ShakeProfile { LegacyImpact, ShortImpact, HeavyHullVibration, DirectionalScrape, PressureRumble, InterfaceJitter }
    // Append new presentation presets: existing serialized values must retain their meaning.
    public enum ScreenInterference { None, ScreenSignalInterference, AnalogSignalFailure, SubtleDisplayInstability,
        StoneDepth, FalseRadarSweep, ProceduralReflection, BioelectricPulse, RockDebris }

    public static class EffectMotion
    {
        private static float Hash(int value)
        { unchecked { uint x = (uint)value; x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; x ^= x >> 16; return (x & 65535) / 65535f; } }
        public static float Irregular(float time, int seed)
        {
            float sample = Mathf.Max(0, time) * 2.7f;
            int index = Mathf.FloorToInt(sample);
            return Mathf.SmoothStep(Hash(seed + index), Hash(seed + index + 1), sample - index);
        }
        public static float Envelope(float time, float duration, float fadeIn, float fadeOut)
        {
            float attack = fadeIn <= 0 ? 1 : Mathf.SmoothStep(0, 1, time / fadeIn);
            float release = fadeOut <= 0 ? 1 : Mathf.SmoothStep(0, 1, (duration - time) / fadeOut);
            return Mathf.Clamp01(attack * release);
        }
        public static float Light(LightBehavior behavior, float time, float duration, int seed)
        {
            float n = Irregular(time, seed);
            float envelope = Envelope(time, duration, .12f, Mathf.Min(.8f, duration * .35f));
            return behavior switch
            {
                LightBehavior.SubtleFlicker => envelope * (.2f + .8f * n),
                LightBehavior.PowerInstability => envelope * Mathf.Lerp(.8f, .2f + .3f * n, Mathf.Clamp01(time / duration)),
                LightBehavior.ElectricalFailure => envelope * (time < duration * .45f ? 1 : .3f + .5f * n),
                LightBehavior.PressureReaction => envelope * (.25f + .55f * n),
                _ => envelope
            };
        }
        public static Vector2 Shake(ShakeProfile profile, float time, float duration, Vector2 direction, int seed)
        {
            float rate = profile switch { ShakeProfile.HeavyHullVibration => 2.2f, ShakeProfile.DirectionalScrape => 2, ShakeProfile.PressureRumble => .9f, ShakeProfile.InterfaceJitter => 12, _ => 7 };
            float verticalRate = profile == ShakeProfile.PressureRumble || profile == ShakeProfile.HeavyHullVibration ? rate * 1.27f : rate + 2;
            float x = Irregular(time * rate, seed) * 2 - 1, y = Irregular(time * verticalRate, seed + 673) * 2 - 1;
            float attack = profile == ShakeProfile.PressureRumble ? duration * .4f : profile == ShakeProfile.DirectionalScrape ? .2f : .02f;
            float envelope = Envelope(time, duration, attack, duration * .4f);
            if (profile == ShakeProfile.ShortImpact) envelope *= Mathf.Pow(Mathf.Clamp01(1 - time / duration), 2);
            if (profile == ShakeProfile.DirectionalScrape)
                return (direction.sqrMagnitude < .01f ? Vector2.right : direction.normalized) * (x * envelope);
            return new Vector2(x, y) * envelope * (profile == ShakeProfile.InterfaceJitter ? .35f : 1);
        }
    }
}

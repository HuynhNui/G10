using System;
using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Atmosphere
{
    public enum CabinMood { Normal, LowPower, Danger, ScanActive }
    [Serializable]
    public sealed class CabinMoodSettings
    {
        [Range(0, 1.5f)] public float light = 1, motion = 1, caustics = 1, glow = 1, particles = 1;
        [Range(0.1f, 2)] public float blinkRate = 1;
    }

    /// <summary>Presentation-only mood. LowPower/Danger are manual until real resource state exists.</summary>
    public sealed class CabinAtmosphere : MonoBehaviour
    {
        public CabinStationView cabin;
        public CanvasGroup presentation;
        public CabinMood mood;
        public bool followRealRadarScan = true;
        [Min(0.1f)] public float transitionSeconds = 2;
        public CabinMoodSettings normal = new();
        public CabinMoodSettings lowPower = new() { light = 0.55f, motion = 0.65f, caustics = 0.5f, glow = 0.3f, blinkRate = 0.5f, particles = 0.65f };
        public CabinMoodSettings danger = new() { light = 0.8f, motion = 1.15f, caustics = 0.7f, glow = 0.9f, blinkRate = 1.5f, particles = 1 };
        public CabinMoodSettings scanActive = new() { light = 1, motion = 0.8f, caustics = 0.85f, glow = 1.3f, blinkRate = 1.15f, particles = 1 };
        public float Light { get; private set; } = 1;
        public float Motion { get; private set; } = 1;
        public float Caustics { get; private set; } = 1;
        public float Glow { get; private set; } = 1;
        public float Particles { get; private set; } = 1;
        public float BlinkRate { get; private set; } = 1;
        public CabinMood EffectiveMood { get; private set; }
        public void SetMood(CabinMood value) => mood = value;
        private void Update()
        {
            EffectiveMood = followRealRadarScan && mood == CabinMood.Normal && cabin != null && cabin.Radar != null && cabin.Radar.IsScanning ? CabinMood.ScanActive : mood;
            CabinMoodSettings settings = EffectiveMood switch { CabinMood.LowPower => lowPower, CabinMood.Danger => danger, CabinMood.ScanActive => scanActive, _ => normal };
            float t = 1 - Mathf.Exp(-Time.deltaTime * 3 / Mathf.Max(0.1f, transitionSeconds));
            Light = Mathf.Lerp(Light, settings.light, t); Motion = Mathf.Lerp(Motion, settings.motion, t);
            Caustics = Mathf.Lerp(Caustics, settings.caustics, t); Glow = Mathf.Lerp(Glow, settings.glow, t);
            Particles = Mathf.Lerp(Particles, settings.particles, t); BlinkRate = Mathf.Lerp(BlinkRate, settings.blinkRate, t);
            bool visible = cabin == null || cabin.Panels == null || !cabin.Panels.IsPanelOpen;
            if (presentation != null) { presentation.alpha = visible ? 1 : 0; presentation.blocksRaycasts = visible; }
        }
    }
}

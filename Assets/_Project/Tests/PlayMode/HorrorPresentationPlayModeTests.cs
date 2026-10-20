using System.Collections;
using System.Collections.Generic;
using G10.Prototype.Audio;
using G10.Prototype.Feedback;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace G10.Prototype.Tests
{
    public sealed class HorrorPresentationPlayModeTests
    {
        private GameObject root;
        private EffectDirector director;
        private EffectAudioExecutor audio;
        private EffectSurface surface;
        private readonly List<Object> assets = new();
        [SetUp] public void Setup()
        {
            root = new GameObject("Horror test", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            director = root.AddComponent<EffectDirector>(); audio = root.AddComponent<EffectAudioExecutor>();
            var obj = new GameObject("Radar surface", typeof(RectTransform)); obj.transform.SetParent(root.transform, false);
            surface = obj.AddComponent<EffectSurface>(); surface.target = EffectTarget.Radar; surface.visual = obj.AddComponent<EffectVisualExecutor>();
            director.Configure(new EffectDefinition[0], new[] { surface }, audio); director.BindAudio(AudioManager.Instance);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); foreach (var asset in assets) Object.Destroy(asset); assets.Clear(); Time.timeScale = 1; yield return null; }
        private T Asset<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); assets.Add(asset); return asset; }
        private EffectDefinition Definition(string id)
        { var d = Asset<EffectDefinition>(); d.id = d.channel = id; d.target = EffectTarget.Radar; d.duration = 2; return d; }
        private AudioClip Clip()
        { var clip = AudioClip.Create("Silence sample", 44100, 1, 44100, false); assets.Add(clip); return clip; }
        [UnityTest] public IEnumerator UIEffectRunsOnlyInsideItsSurfaceAndDisablesAtZero()
        {
            var unrelated = new GameObject("Unrelated text region", typeof(RectTransform), typeof(RawImage)); unrelated.transform.SetParent(root.transform, false);
            var image = unrelated.GetComponent<RawImage>(); image.color = Color.green; var material = image.material;
            foreach (var preset in new[] { ScreenInterference.ScreenSignalInterference, ScreenInterference.AnalogSignalFailure, ScreenInterference.SubtleDisplayInstability })
            {
                var d = Definition(preset.ToString()); d.interference = preset;
                var handle = director.Play(d); director.Tick(.2f); yield return null;
                Assert.That(surface.visual.SignalVisible, Is.True); Assert.That(surface.visual.GlitchEnabled, Is.True);
                Assert.That(image.color, Is.EqualTo(Color.green)); Assert.That(image.material, Is.SameAs(material));
                foreach (var graphic in surface.GetComponentsInChildren<Graphic>()) Assert.That(graphic.raycastTarget, Is.False);
                director.Stop(handle); Assert.That(surface.visual.SignalVisible, Is.False); Assert.That(surface.visual.GlitchEnabled, Is.False);
                var zero = director.Play(d, new EffectContext { Intensity = 0 }); director.Tick(.1f);
                Assert.That(surface.visual.GlitchEnabled, Is.False); director.Stop(zero);
            }
        }
        [UnityTest] public IEnumerator DirectionalAudioStays2DAndCancellationIsScoped()
        {
            var a = Definition("a"); a.clip = Clip(); a.loop = true; a.audioCategory = AudioCategory.Anomalies; a.stereoPan = -.6f;
            var b = Definition("b"); b.clip = Clip(); b.loop = true; b.audioCategory = AudioCategory.Impacts;
            var ah = director.Play(a); director.Play(b); director.Tick(.2f); yield return null;
            var sources = root.GetComponentsInChildren<AudioSource>(); Assert.That(sources.Length, Is.EqualTo(2));
            var directional = System.Array.Find(sources, s => s.clip == a.clip);
            Assert.That(directional.spatialBlend, Is.Zero); Assert.That(directional.panStereo, Is.EqualTo(-.6f).Within(.001));
            Assert.That(directional.outputAudioMixerGroup, Is.EqualTo(AudioManager.Instance.HorrorMix.anomalies));
            director.Stop(ah); Assert.That(audio.PlayingCount, Is.EqualTo(1)); Assert.That(directional.clip, Is.Null);
        }
        [UnityTest] public IEnumerator SequencePauseCancelsPendingOneShotsAndRestoresMix()
        {
            var mix = Definition("mix"); mix.mixState = HorrorMixState.NearSilence;
            var sound = Definition("later"); sound.clip = Clip();
            var sequence = Asset<EffectSequence>(); sequence.target = EffectTarget.Radar;
            sequence.cues = new[] { new EffectCue { effect = mix, duration = 2 }, new EffectCue { effect = sound, offset = .8f, duration = .5f } };
            var handle = director.PlaySequence(sequence); Assert.That(AudioManager.Instance.CurrentMix, Is.EqualTo(HorrorMixState.NearSilence));
            Time.timeScale = 0; director.Tick(1); Assert.That(director.IsActive(handle), Is.False);
            Assert.That(AudioManager.Instance.CurrentMix, Is.EqualTo(HorrorMixState.Normal));
            Time.timeScale = 1; director.Tick(2); Assert.That(audio.PlayingCount, Is.Zero); yield return null;
        }
        [UnityTest] public IEnumerator LoopReuseAndSceneOwnerDisableDoNotLeakVoices()
        {
            var d = Definition("loop"); d.clip = Clip(); d.loop = true;
            for (int i = 0; i < 25; i++) { var handle = director.Play(d); director.Tick(.01f); director.Stop(handle); }
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1));
            director.Play(d); root.SetActive(false); Assert.That(audio.PlayingCount, Is.Zero); yield return null;
        }
        [UnityTest] public IEnumerator SilhouetteVisibilityAndCancellationRestoreAllGraphics()
        {
            var texture = new Texture2D(8, 8); assets.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * .5f); assets.Add(sprite);
            var d = Definition("silhouette"); d.silhouette = sprite;
            var handle = director.Play(d); director.Tick(.3f); Assert.That(surface.visual.AnomalyVisible, Is.True);
            var audience = new GameObject("Audience"); audience.transform.SetParent(root.transform); surface.BindVisibility(audience);
            audience.SetActive(false); director.Tick(.1f);
            Assert.That(director.IsActive(handle), Is.False); Assert.That(surface.visual.AnomalyVisible, Is.False); yield return null;
        }
        [UnityTest] public IEnumerator CategoryVolumeRespectsOutputSettingAndUnrelatedCategories()
        {
            var manager = AudioManager.Instance; float original = manager.CategoryVolume(AudioCategory.Anomalies), output = manager.OutputVolume;
            string key = "G10.Audio.Category.Anomalies"; bool had = PlayerPrefs.HasKey(key);
            try
            {
                manager.SetCategoryVolume(AudioCategory.Anomalies, 0);
                var d = Definition("muted"); d.clip = Clip(); d.loop = true; d.audioCategory = AudioCategory.Anomalies;
                director.Play(d); director.Tick(.5f); yield return null;
                Assert.That(root.GetComponentInChildren<AudioSource>().volume, Is.Zero);
                Assert.That(manager.OutputVolume, Is.EqualTo(output));
            }
            finally { manager.SetCategoryVolume(AudioCategory.Anomalies, original); if (!had) PlayerPrefs.DeleteKey(key); }
        }
        [UnityTest] public IEnumerator CancellingHigherMixOwnerRestoresOtherOwnersRequest()
        {
            var otherObject = new GameObject("Other presentation owner"); otherObject.transform.SetParent(root.transform);
            var other = otherObject.AddComponent<EffectDirector>();
            var otherSurface = otherObject.AddComponent<EffectSurface>(); otherSurface.target = EffectTarget.Radar;
            other.Configure(new EffectDefinition[0], new[] { otherSurface }, null); other.BindAudio(AudioManager.Instance);
            var high = Definition("high"); high.mixState = HorrorMixState.NearSilence; high.priority = 50;
            var low = Definition("low"); low.mixState = HorrorMixState.Suspense; low.priority = 20;
            var highHandle = director.Play(high); var lowHandle = other.Play(low);
            Assert.That(AudioManager.Instance.CurrentMix, Is.EqualTo(HorrorMixState.NearSilence));
            director.Stop(highHandle); Assert.That(AudioManager.Instance.CurrentMix, Is.EqualTo(HorrorMixState.Suspense));
            other.Stop(lowHandle); Assert.That(AudioManager.Instance.CurrentMix, Is.EqualTo(HorrorMixState.Normal));
            yield return null;
        }
        [UnityTest] public IEnumerator LocalDirectionalProfileHonorsAccessibilityAndRestoresOrigin()
        {
            bool had = PlayerPrefs.HasKey(ImpactShake.PreferenceKey); int previous = PlayerPrefs.GetInt(ImpactShake.PreferenceKey, 1);
            try
            {
                Canvas.ForceUpdateCanvases(); var rootOrigin = root.transform.localPosition;
                var rig = new GameObject("Dedicated signal rig", typeof(RectTransform)); rig.transform.SetParent(surface.transform, false); rig.transform.localPosition = new Vector3(17, 11, 0);
                var origin = rig.transform.localPosition;
                surface.shake = rig.AddComponent<EffectShakeExecutor>(); surface.shake.shake = rig.AddComponent<ImpactShake>();
                var definition = Definition("local"); definition.shakeProfile = ShakeProfile.DirectionalScrape; definition.shakePixels = 3;
                ImpactShake.SetScreenShakeEnabled(false); var disabled = director.Play(definition); director.Tick(.1f); yield return null;
                Assert.That(surface.shake.shake.IsShaking, Is.False); Assert.That(rig.transform.localPosition, Is.EqualTo(origin)); director.Stop(disabled);
                ImpactShake.SetScreenShakeEnabled(true); var handle = director.Play(definition); director.Tick(.1f); yield return null;
                Assert.That(surface.shake.shake.IsShaking, Is.True); Assert.That(rig.transform.localPosition.y, Is.EqualTo(origin.y).Within(.001));
                Assert.That(root.transform.localPosition, Is.EqualTo(rootOrigin));
                director.Stop(handle); Assert.That(rig.transform.localPosition, Is.EqualTo(origin));
            }
            finally { if (had) PlayerPrefs.SetInt(ImpactShake.PreferenceKey, previous); else PlayerPrefs.DeleteKey(ImpactShake.PreferenceKey); PlayerPrefs.Save(); }
        }
        [UnityTest] public IEnumerator ShortImpactEndsBeforeLongerCombinedEffect()
        {
            var rig = new GameObject("Short impact rig"); rig.transform.SetParent(root.transform, false);
            surface.shake = rig.AddComponent<EffectShakeExecutor>(); surface.shake.shake = rig.AddComponent<ImpactShake>();
            var d = Definition("short"); d.shakeProfile = ShakeProfile.ShortImpact; d.shakePixels = 2; d.shakeSeconds = .2f; d.duration = 2;
            var handle = director.Play(d); director.Tick(.21f);
            Assert.That(director.IsActive(handle), Is.True); Assert.That(surface.shake.shake.IsShaking, Is.False);
            Assert.That(rig.transform.localPosition, Is.EqualTo(Vector3.zero)); yield return null;
        }
    }
}

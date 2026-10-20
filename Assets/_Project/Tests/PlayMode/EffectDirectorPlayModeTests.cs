using System.Collections;
using System.Collections.Generic;
using G10.Prototype.Atmosphere;
using G10.Prototype.Feedback;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    /// <summary>Self-contained executor lifecycle, with no expedition scene or player save.</summary>
    public sealed class EffectDirectorPlayModeTests
    {
        private GameObject root, frame;
        private EffectDirector director;
        private EffectAudioExecutor audio;
        private EffectSurface global, radar;
        private CabinAtmosphere atmosphere;
        private int previousShake;
        private readonly List<Object> assets = new();
        private readonly List<EffectDefinition> definitions = new();
        [SetUp] public void Setup()
        {
            previousShake = PlayerPrefs.GetInt(ImpactShake.PreferenceKey, -1); ImpactShake.SetScreenShakeEnabled(true);
            root = new GameObject("Effect executor test", typeof(RectTransform)); director = root.AddComponent<EffectDirector>(); audio = root.AddComponent<EffectAudioExecutor>();
            frame = new GameObject("Outer frame"); frame.transform.SetParent(root.transform); frame.transform.localPosition = new Vector3(12, -9, 0);
            global = Target(EffectTarget.Global); radar = Target(EffectTarget.Radar);
            var shake = global.gameObject.AddComponent<EffectShakeExecutor>(); shake.shake = frame.AddComponent<ImpactShake>(); global.shake = shake;
            atmosphere = root.AddComponent<CabinAtmosphere>(); atmosphere.followRealRadarScan = false;
            global.lighting = global.gameObject.AddComponent<EffectLightingExecutor>(); global.lighting.atmosphere = atmosphere;
            Define("hull", "hull").overlay = EffectOverlay.Hull;
            var energy = Define("energy", "energy"); energy.overlay = EffectOverlay.Energy; energy.overrideMood = true; energy.mood = CabinMood.LowPower; energy.loop = true; energy.clip = Clip(); energy.gain = .35f;
            var impact = Define("impact", "impact"); impact.overlay = EffectOverlay.Hull; impact.alpha = .3f; impact.shakePixels = 7; impact.shakeSeconds = .28f; impact.clip = Clip(); impact.priority = 50;
            var rock = Define("rock", "rock"); rock.shakePixels = 8; rock.shakeSeconds = .24f; rock.priority = 100;
            var flicker = Define("flicker", "lighting"); flicker.lightDip = .35f; flicker.flicker = true;
            var tint = Define("radar", "screen"); tint.target = EffectTarget.Radar; tint.overlay = EffectOverlay.Tint; tint.alpha = .4f;
            var notice = Define("notice", "notice"); notice.message = "test";
            director.Configure(definitions.ToArray(), new[] { global, radar }, audio);
        }
        private EffectSurface Target(EffectTarget target)
        {
            var obj = new GameObject(target.ToString(), typeof(RectTransform)); obj.transform.SetParent(root.transform);
            var surface = obj.AddComponent<EffectSurface>(); surface.target = target;
            surface.overlay = obj.AddComponent<EffectOverlayExecutor>();
            surface.overlay.hull = Image(obj.transform, "Hull"); surface.overlay.energy = Image(obj.transform, "Energy"); surface.overlay.tint = Image(obj.transform, "Tint");
            var label = new GameObject("Notice", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(obj.transform);
            surface.overlay.notification = label.GetComponent<TextMeshProUGUI>(); return surface;
        }
        private static UnityEngine.UI.RawImage Image(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.RawImage)); obj.transform.SetParent(parent);
            var image = obj.GetComponent<UnityEngine.UI.RawImage>(); image.color = new Color(1, 1, 1, 0); return image;
        }
        private AudioClip Clip()
        { var clip = AudioClip.Create("Test silence", 44100, 1, 44100, false); assets.Add(clip); return clip; }
        private EffectDefinition Define(string id, string channel)
        {
            var d = ScriptableObject.CreateInstance<EffectDefinition>(); d.id = id; d.channel = channel; d.duration = .28f;
            assets.Add(d); definitions.Add(d); return d;
        }
        [UnityTest] public IEnumerator DispatchCompositionAndScopedAudioCancellationRestoreResourceBaseline()
        {
            director.SetPersistentState("hull", true, new EffectContext { Alpha = .25f });
            var hum = director.SetPersistentState("energy", true, new EffectContext { Alpha = .22f, Instability = 1.3f });
            director.Tick(.5f);
            var impact = director.Play("impact");
            Assert.That(global.overlay.HullAlpha, Is.EqualTo(.3f).Within(.001f));
            Assert.That(global.overlay.EnergyAlpha, Is.GreaterThan(.20f)); Assert.That(audio.PlayingCount, Is.EqualTo(2));
            director.Stop(impact);
            Assert.That(director.IsActive(hum), Is.True); Assert.That(audio.PlayingCount, Is.EqualTo(1));
            Assert.That(global.overlay.HullAlpha, Is.EqualTo(.25f).Within(.001f));
            Assert.That(global.overlay.EnergyAlpha, Is.EqualTo(.22f).Within(.001f));
            yield return null; Assert.That(atmosphere.EffectiveMood, Is.EqualTo(CabinMood.LowPower));
        }
        [UnityTest] public IEnumerator ResourceMoodIsAvailableToConsumersBeforeTheNextDirectorUpdate()
        {
            director.SetPersistentState("energy", true, new EffectContext { Alpha = .22f });
            // Deterministically emulate the consumer updating before the director's next frame.
            atmosphere.SendMessage("Update");
            Assert.That(atmosphere.EffectiveMood, Is.EqualTo(CabinMood.LowPower));
            director.SetPersistentState("energy", false); atmosphere.SendMessage("Update");
            Assert.That(atmosphere.EffectiveMood, Is.EqualTo(CabinMood.Normal));
            yield return null;
        }
        [UnityTest] public IEnumerator TargetIsolationAndVisibilityCancellationDoNotLeakToOtherScreens()
        {
            var request = director.Play("radar");
            Assert.That(radar.overlay.tint.color.a, Is.EqualTo(.4f).Within(.001f)); Assert.That(global.overlay.tint.color.a, Is.Zero);
            var gate = new GameObject("Radar panel"); gate.transform.SetParent(root.transform); radar.visibilityRoot = gate; gate.SetActive(false);
            director.Tick(.01f); Assert.That(director.IsActive(request), Is.False); Assert.That(radar.overlay.tint.color.a, Is.Zero);
            gate.SetActive(true); director.Tick(.01f); Assert.That(radar.overlay.tint.color.a, Is.Zero);
            Assert.That(director.Play("radar").IsValid, Is.True); yield return null;
        }
        [UnityTest] public IEnumerator RockPriorityPreventsAccumulationAndStopDoesNotCancelImpact()
        {
            Vector3 origin = frame.transform.localPosition;
            var rock = director.Play("rock"); var impact = director.Play("impact");
            long rockWinner = global.shake.Winner; Assert.That(rockWinner, Is.Not.Zero);
            yield return null; Assert.That(frame.transform.localPosition.x, Is.InRange(origin.x - 8, origin.x + 8));
            Assert.That(global.shake.Winner, Is.EqualTo(rockWinner));
            director.Stop(rock); Assert.That(director.IsActive(impact), Is.True); Assert.That(global.shake.Winner, Is.Not.EqualTo(rockWinner));
            director.ClearTransients(); Assert.That(frame.transform.localPosition, Is.EqualTo(origin));
        }
        [UnityTest] public IEnumerator AccessibilitySuppressesOnlyShakeAndRestoresExactPose()
        {
            Vector3 origin = frame.transform.localPosition; ImpactShake.SetScreenShakeEnabled(false);
            director.Play("impact"); director.Play("flicker");
            Assert.That(frame.GetComponent<ImpactShake>().IsShaking, Is.False); Assert.That(audio.PlayingCount, Is.EqualTo(1));
            Assert.That(global.overlay.HullAlpha, Is.EqualTo(.3f).Within(.001f));
            yield return null; Assert.That(atmosphere.Light, Is.LessThan(1)); Assert.That(frame.transform.localPosition, Is.EqualTo(origin));
        }
        [UnityTest] public IEnumerator PauseAndApplicationPauseClearOldRequestsButPreserveFreshResumeFrame()
        {
            director.SetPersistentState("energy", true, new EffectContext { Alpha = .22f }); director.Play("impact");
            Time.timeScale = 0; director.Tick(.5f);
            Assert.That(director.ActiveTransientCount, Is.Zero); Assert.That(director.PersistentCount, Is.EqualTo(1));
            Assert.That(director.Play("impact").IsValid, Is.False);
            Time.timeScale = 1; var fresh = director.Play("impact"); director.Tick(.001f);
            Assert.That(director.IsActive(fresh), Is.True);
            director.SendMessage("OnApplicationPause", true); Assert.That(director.ActiveTransientCount, Is.Zero);
            director.SendMessage("OnApplicationPause", false); fresh = director.Play("impact"); director.Tick(.001f);
            Assert.That(director.IsActive(fresh), Is.True); yield return null;
        }
        [UnityTest] public IEnumerator SuspensionHideAndFlickerRestorePersistentMoodWithoutStaleText()
        {
            director.SetPersistentState("energy", true, new EffectContext { Alpha = .22f });
            var flicker = director.Play("flicker"); director.Play("notice"); yield return null;
            director.Stop(flicker); director.Tick(1); yield return null;
            Assert.That(atmosphere.EffectiveMood, Is.EqualTo(CabinMood.LowPower));
            Assert.That(global.overlay.EnergyAlpha, Is.EqualTo(.22f).Within(.001f));
            director.SetSuspended(true, true); Assert.That(global.overlay.EnergyAlpha, Is.Zero); Assert.That(global.overlay.notification.text, Is.Empty);
            director.SetSuspended(false); var fresh = director.Play("notice"); director.Tick(.01f);
            Assert.That(director.IsActive(fresh), Is.True); Assert.That(global.overlay.notification.text, Is.EqualTo("test"));
        }
        [UnityTest] public IEnumerator RepeatedPlaybackDisableAndDestroyDoNotAccumulateVoicesOrOffsets()
        {
            Vector3 origin = frame.transform.localPosition;
            for (int i = 0; i < 200; i++)
            { var h = director.Play("impact"); director.Stop(h); director.Play("notice"); director.ClearTransients(); }
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1)); Assert.That(audio.PlayingCount, Is.Zero);
            Assert.That(director.ActiveTransientCount, Is.Zero); Assert.That(frame.transform.localPosition, Is.EqualTo(origin));
            director.SetPersistentState("energy", true); director.Play("impact"); director.enabled = false;
            Assert.That(audio.PlayingCount, Is.Zero); Assert.That(global.overlay.HullAlpha, Is.Zero); Assert.That(global.overlay.EnergyAlpha, Is.Zero);
            Assert.That(frame.transform.localPosition, Is.EqualTo(origin));
            director.enabled = true; director.Play("impact"); Object.Destroy(director); yield return null;
            Assert.That(audio.PlayingCount, Is.Zero); Assert.That(frame.transform.localPosition, Is.EqualTo(origin));
        }
        [UnityTest] public IEnumerator MissingOrDisabledExecutorIsSafeAndNeverTouchesOtherTargets()
        {
            radar.overlay.enabled = false; director.Play("radar"); director.Tick(.1f);
            Assert.That(global.overlay.tint.color.a, Is.Zero); Assert.That(radar.overlay.tint.color.a, Is.Zero);
            Object.Destroy(radar.gameObject); yield return null;
            Assert.DoesNotThrow(() => director.Tick(.2f));
            Assert.That(director.Play("radar").IsValid, Is.False);
        }
        [UnityTest] public IEnumerator HiddenPersistentTargetStopsOnlyItsOwnAudioAndCanResume()
        {
            var gate = new GameObject("Radar visibility"); gate.transform.SetParent(root.transform); radar.visibilityRoot = gate;
            var a = director.SetPersistentState("energy", true);
            var b = director.SetPersistentState("energy", true, new EffectContext { Target = EffectTarget.Radar });
            Assert.That(audio.PlayingCount, Is.EqualTo(2));
            gate.SetActive(false); director.Tick(.1f);
            Assert.That(audio.PlayingCount, Is.EqualTo(1)); Assert.That(director.IsActive(a), Is.True); Assert.That(director.IsActive(b), Is.True);
            gate.SetActive(true); director.Tick(.1f); Assert.That(audio.PlayingCount, Is.EqualTo(2));
            director.SetPersistentState("energy", false, new EffectContext { Target = EffectTarget.Radar });
            director.Tick(.5f); Assert.That(audio.PlayingCount, Is.EqualTo(1));
            yield return null;
        }
        [UnityTest] public IEnumerator ScopedOverlayFitsTheActualWindowRatherThanTheWholeCanvas()
        {
            var window = new GameObject("Small Photo Lab audience", typeof(RectTransform)); window.transform.SetParent(root.transform, false);
            var audience = (RectTransform)window.transform; audience.sizeDelta = new Vector2(240, 120); audience.anchoredPosition = new Vector2(143, 75);
            radar.visibilityRoot = window; director.Play("radar");
            var surface = (RectTransform)radar.transform;
            Assert.That(surface.rect.size, Is.EqualTo(audience.rect.size));
            Assert.That(surface.anchoredPosition, Is.EqualTo(audience.anchoredPosition));
            audience.sizeDelta = new Vector2(330, 180); audience.anchoredPosition = new Vector2(-61, 24);
            director.Tick(.01f);
            Assert.That(surface.rect.size, Is.EqualTo(audience.rect.size));
            Assert.That(surface.anchoredPosition, Is.EqualTo(audience.anchoredPosition));
            yield return null;
        }
        [UnityTest] public IEnumerator DestroyedAudienceDoesNotBecomeAnUnrestrictedGlobalSurface()
        {
            var gate = new GameObject("Removed audience"); gate.transform.SetParent(root.transform); radar.visibilityRoot = gate;
            Assert.That(director.Play("radar").IsValid, Is.True);
            Object.DestroyImmediate(gate); director.Tick(.01f);
            Assert.That(director.Play("radar").IsValid, Is.False);
            Assert.That(radar.overlay.tint.color.a, Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator DestroyedNativeShakeTargetIsSafeToRenderAndRestore()
        {
            director.enabled = false; Object.Destroy(frame); yield return null;
            try { Assert.DoesNotThrow(() => { global.shake.Render(999, 3, .2f); global.shake.Restore(); }); }
            finally { global.shake.shake = null; } // Also makes teardown independent of a failed native-target assertion.
        }
        [UnityTest] public IEnumerator RemovedPooledAudioSourceIsRecreatedWithoutLeakingOrThrowing()
        {
            director.Play("impact"); director.enabled = false;
            Object.DestroyImmediate(root.GetComponentInChildren<AudioSource>());
            director.enabled = true;
            Assert.DoesNotThrow(() => director.Play("impact"));
            Assert.That(audio.PlayingCount, Is.EqualTo(1)); Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1));
            director.ClearAll(); Assert.That(audio.PlayingCount, Is.Zero);
            yield return null;
        }
        [TearDown] public void Cleanup()
        {
            Time.timeScale = 1; Object.DestroyImmediate(root);
            foreach (var asset in assets) Object.DestroyImmediate(asset); assets.Clear(); definitions.Clear();
            if (previousShake < 0) PlayerPrefs.DeleteKey(ImpactShake.PreferenceKey); else PlayerPrefs.SetInt(ImpactShake.PreferenceKey, previousShake);
            PlayerPrefs.Save();
        }
    }
}

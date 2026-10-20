using System.Collections.Generic;
using G10.Prototype.Feedback;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class HorrorSequenceTests
    {
        private GameObject root;
        private EffectDirector director;
        private readonly List<Object> assets = new();
        [SetUp] public void Setup()
        {
            root = new GameObject("Sequence test"); director = root.AddComponent<EffectDirector>();
            var surface = root.AddComponent<EffectSurface>(); surface.target = EffectTarget.Global;
            director.Configure(new EffectDefinition[0], new[] { surface }, null);
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(root); foreach (var asset in assets) Object.DestroyImmediate(asset); assets.Clear(); }
        private T Asset<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); assets.Add(asset); return asset; }
        private EffectSequence Sequence(float offset = .5f)
        {
            var effect = Asset<EffectDefinition>(); effect.id = "cue"; effect.channel = "cue";
            var sequence = Asset<EffectSequence>(); sequence.id = "sequence";
            sequence.cues = new[] { new EffectCue { effect = effect, offset = offset, duration = .5f } }; return sequence;
        }
        [Test] public void RelativeCueTimingAndCompletionAreBounded()
        {
            var sequence = Sequence(); var handle = director.PlaySequence(sequence);
            Assert.That(director.IsPlaying("cue"), Is.False); director.Tick(.49f);
            Assert.That(director.IsPlaying("cue"), Is.False); director.Tick(.02f);
            Assert.That(director.IsPlaying("cue"), Is.True); director.Tick(.5f);
            Assert.That(director.IsActive(handle), Is.False); Assert.That(director.ActiveTransientCount, Is.Zero);
        }
        [Test] public void ParentCancellationKeepsUnrelatedPresentation()
        {
            var sequence = Sequence(0); var unrelated = Asset<EffectDefinition>(); unrelated.id = "other"; unrelated.channel = "other"; unrelated.duration = 10;
            var other = director.Play(unrelated); var handle = director.PlaySequence(sequence);
            director.Stop(handle); Assert.That(director.IsActive(other), Is.True); Assert.That(director.IsPlaying("cue"), Is.False);
        }
        [Test] public void CallerConditionCancelsAllChildrenWithoutFutureCues()
        {
            bool permitted = true; var sequence = Sequence(0); director.PlaySequence(sequence, condition: () => permitted);
            Assert.That(director.IsPlaying("cue"), Is.True); permitted = false; director.Tick(.1f);
            Assert.That(director.ActiveSequenceCount, Is.Zero); Assert.That(director.ActiveTransientCount, Is.Zero);
            director.Tick(10); Assert.That(director.IsPlaying("cue"), Is.False);
        }
        [Test] public void LargeFrameDoesNotReplayExpiredCues()
        {
            var sequence = Sequence(); sequence.cues = new[] { sequence.cues[0], new EffectCue { effect = sequence.cues[0].effect, offset = 10, duration = 1 } };
            director.PlaySequence(sequence); director.Tick(2); Assert.That(director.ActiveTransientCount, Is.Zero);
        }
        [Test] public void PriorityAndScopedChannelCancellationApplyToSequences()
        {
            var high = Sequence(0); high.priority = 40; var low = Sequence(0); low.priority = 20;
            var handle = director.PlaySequence(high); Assert.That(director.PlaySequence(low).IsValid, Is.False);
            director.StopChannel(high.channel, EffectTarget.Radar); Assert.That(director.IsActive(handle), Is.True);
            director.StopChannel(high.channel, EffectTarget.Global); Assert.That(director.IsActive(handle), Is.False);
            Assert.That(director.ActiveTransientCount, Is.Zero);
        }
        [Test] public void ClearingTransientsAlsoClearsPendingSequences()
        {
            director.PlaySequence(Sequence()); director.ClearTransients(); director.Tick(1);
            Assert.That(director.ActiveSequenceCount, Is.Zero); Assert.That(director.ActiveTransientCount, Is.Zero);
        }
        [Test] public void DefinitionCueAndCallerIntensityComposeWithoutMutatingTheAsset()
        {
            var overlayObject = new GameObject("Tint", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)); overlayObject.transform.SetParent(root.transform);
            var surface = root.GetComponent<EffectSurface>(); surface.overlay = root.AddComponent<EffectOverlayExecutor>();
            surface.overlay.tint = overlayObject.GetComponent<UnityEngine.UI.RawImage>(); surface.overlay.tint.color = Color.clear;
            var sequence = Sequence(0); var definition = sequence.cues[0].effect;
            definition.overlay = EffectOverlay.Tint; definition.alpha = .8f; definition.intensity = .5f; sequence.cues[0].intensity = .5f;
            director.PlaySequence(sequence, new EffectContext { Intensity = .5f });
            Assert.That(surface.overlay.tint.color.a, Is.EqualTo(.1f).Within(.001));
            Assert.That(definition.intensity, Is.EqualTo(.5f)); Assert.That(sequence.cues[0].intensity, Is.EqualTo(.5f));
        }
        [TestCase(LightBehavior.SubtleFlicker)] [TestCase(LightBehavior.PowerInstability)]
        [TestCase(LightBehavior.ElectricalFailure)] [TestCase(LightBehavior.PressureReaction)]
        public void LightingProfilesAreReproducibleBoundedAndRecover(LightBehavior profile)
        {
            float previous = -1; bool varied = false;
            for (int i = 0; i < 100; i++)
            {
                float time = i * .03f, value = EffectMotion.Light(profile, time, 3, 147);
                Assert.That(value, Is.InRange(0, 1)); Assert.That(value, Is.EqualTo(EffectMotion.Light(profile, time, 3, 147)));
                if (previous >= 0 && Mathf.Abs(previous - value) > .001f) varied = true; previous = value;
            }
            Assert.That(varied, Is.True); Assert.That(EffectMotion.Light(profile, 3, 3, 147), Is.Zero);
        }
    }
}

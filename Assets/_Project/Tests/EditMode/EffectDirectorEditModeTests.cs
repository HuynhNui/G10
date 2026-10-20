using System.Collections.Generic;
using G10.Prototype.Feedback;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    /// <summary>Dispatch/arbitration contracts only; no scene load or save access.</summary>
    public sealed class EffectDirectorEditModeTests
    {
        private GameObject root;
        private EffectDirector director;
        private EffectSurface global, radar;
        private readonly List<EffectDefinition> definitions = new();
        [SetUp] public void Setup()
        {
            root = new GameObject("Effect contract test"); director = root.AddComponent<EffectDirector>();
            global = Target(EffectTarget.Global); radar = Target(EffectTarget.Radar);
        }
        private EffectSurface Target(EffectTarget target)
        {
            var obj = new GameObject(target.ToString()); obj.transform.SetParent(root.transform);
            var surface = obj.AddComponent<EffectSurface>(); surface.target = target; return surface;
        }
        private EffectDefinition Define(string id, string channel, int priority = 0, EffectConflict conflict = EffectConflict.ReplaceLowerPriority)
        {
            var d = ScriptableObject.CreateInstance<EffectDefinition>(); d.id = id; d.channel = channel; d.priority = priority; d.conflict = conflict;
            definitions.Add(d); return d;
        }
        private void Bind() => director.Configure(definitions.ToArray(), new[] { global, radar }, null);
        [Test] public void StableIdDispatchRejectsUnknownAndMissingTargets()
        {
            Define("valid", "a"); Bind();
            Assert.That(director.Play("unknown").IsValid, Is.False);
            Assert.That(director.Play("valid", new EffectContext { Target = EffectTarget.PhotoLab }).IsValid, Is.False);
            Assert.That(director.IsActive(director.Play("valid")), Is.True);
        }
        [Test] public void PriorityRejectsLowerAndEqualPriorityLatestReplaces()
        {
            Define("low", "a", 1); Define("high", "a", 10); Bind();
            var low = director.Play("low"); var high = director.Play("high");
            Assert.That(director.IsActive(low), Is.False); Assert.That(director.IsActive(high), Is.True);
            Assert.That(director.Play("low").IsValid, Is.False);
            var replacement = director.Play("high"); Assert.That(director.IsActive(high), Is.False);
            Assert.That(director.IsActive(replacement), Is.True); Assert.That(director.ActiveTransientCount, Is.EqualTo(1));
        }
        [Test] public void IgnoreAndStackPoliciesHaveDistinctLifetimes()
        {
            Define("ignore", "a", 1, EffectConflict.IgnoreWhileActive); Define("stack", "b", 1, EffectConflict.Stack); Bind();
            Assert.That(director.Play("ignore").IsValid, Is.True); Assert.That(director.Play("ignore").IsValid, Is.False);
            var a = director.Play("stack"); var b = director.Play("stack"); director.Stop(a);
            Assert.That(director.IsActive(b), Is.True); Assert.That(director.ActiveTransientCount, Is.EqualTo(2));
        }
        [Test] public void StopChannelIsTargetScopedAndHandlesCannotCancelOtherDirectors()
        {
            Define("a", "shared"); Bind();
            var globalHandle = director.Play("a"); var radarHandle = director.Play("a", new EffectContext { Target = EffectTarget.Radar });
            var other = radar.gameObject.AddComponent<EffectDirector>(); other.Configure(definitions.ToArray(), new[] { global }, null);
            var otherHandle = other.Play("a"); director.Stop(otherHandle);
            Assert.That(director.IsActive(globalHandle), Is.True); Assert.That(other.IsActive(otherHandle), Is.True);
            director.StopChannel("shared", EffectTarget.Radar);
            Assert.That(director.IsActive(globalHandle), Is.True); Assert.That(director.IsActive(radarHandle), Is.False);
        }
        [Test] public void PersistentUpdatesAreIdempotentAndNotClearedWithTransients()
        {
            Define("a", "a"); Bind();
            var state = director.SetPersistentState("a", true);
            for (int i = 0; i < 200; i++) director.SetPersistentState("a", true, new EffectContext { Alpha = .22f });
            director.Play("a"); director.ClearTransients();
            Assert.That(director.IsActive(state), Is.True); Assert.That(director.PersistentCount, Is.EqualTo(1));
            Assert.That(director.ActiveTransientCount, Is.Zero);
            director.SetPersistentState("a", false); Assert.That(director.PersistentCount, Is.Zero);
        }
        [Test] public void DisabledAndDestroyedSurfacesRejectSafely()
        {
            Define("a", "a"); Bind(); radar.enabled = false;
            Assert.That(director.Play("a", new EffectContext { Target = EffectTarget.Radar }).IsValid, Is.False);
            Object.DestroyImmediate(radar.gameObject);
            Assert.DoesNotThrow(() => director.Tick(.1f));
            Assert.That(director.Play("a", new EffectContext { Target = EffectTarget.Radar }).IsValid, Is.False);
        }
        [Test] public void VisibilityBindingDistinguishesExplicitNoneFromDestroyedAudience()
        {
            global.BindVisibility(null); Assert.That(global.Available, Is.True);
            var audience = new GameObject("Bound audience"); audience.transform.SetParent(root.transform);
            global.BindVisibility(audience); Assert.That(global.Available, Is.True);
            Object.DestroyImmediate(audience); Assert.That(global.Available, Is.False);
            global.BindVisibility(null); Assert.That(global.Available, Is.True, "Explicit rebind can intentionally remove an audience restriction.");
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(root); foreach (var definition in definitions) Object.DestroyImmediate(definition); definitions.Clear(); }
    }
}

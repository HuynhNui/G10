using G10.Prototype.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests.EditMode
{
    public sealed class MapLightOpacityRulesTests
    {
        private ZoneMapConfig config;

        [SetUp] public void Setup() => config = ScriptableObject.CreateInstance<ZoneMapConfig>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(config);

        [Test]
        public void LegacyConfigWithoutOpacityFieldsKeepsFullyOpaqueLights()
        {
            JsonUtility.FromJsonOverwrite("{\"zoneId\":\"Zone01\"}", config);
            Assert.That(config.LightOpacity, Is.EqualTo(1f));
            Assert.That(config.LightOpacityFor(0), Is.EqualTo(1f));
            Assert.That(config.LightOpacityFor(1), Is.EqualTo(1f));
            Assert.That(config.LightOpacityConfigured, Is.False);
        }

        [Test]
        public void MissingOverridesUseConfiguredZoneOpacity()
        {
            config.ConfigureLightOpacity(.225f, null);
            Assert.That(config.LightOpacityConfigured, Is.True);
            Assert.That(config.LightOpacityFor(0), Is.EqualTo(.225f));
            Assert.That(config.LightOpacityFor(1), Is.EqualTo(.225f));
            Assert.That(config.LightOpacityFor(-1), Is.EqualTo(.225f));
            Assert.That(config.LightOpacityFor(200), Is.EqualTo(.225f));
        }

        [TestCase(-1f, .35f)]
        [TestCase(float.NaN, .35f)]
        [TestCase(float.NegativeInfinity, .35f)]
        [TestCase(0f, 0f)]
        [TestCase(.5f, .5f)]
        [TestCase(1f, 1f)]
        [TestCase(1.5f, 1f)]
        public void LayerOverrideIsSafeAndZeroIntentionallyHides(float value, float expected)
        {
            config.ConfigureLightOpacity(.35f, value);
            Assert.That(config.LightOpacityFor(0), Is.EqualTo(expected));
            Assert.That(config.LightOpacityFor(1), Is.EqualTo(.35f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-2f)]
        public void InvalidZoneOpacityFallsBackToOne(float value)
        {
            config.ConfigureLightOpacity(value);
            Assert.That(config.LightOpacity, Is.EqualTo(1f));
        }

        [Test]
        public void ConfigureDoesNotKeepCallerMutableOverrideArray()
        {
            var overrides = new[] { .2f, .25f };
            config.ConfigureLightOpacity(.35f, overrides);
            overrides[0] = .9f;
            Assert.That(config.LightOpacityFor(0), Is.EqualTo(.2f));
        }

        [Test]
        public void OpacityAndOverridesRoundTripThroughConfigSerialization()
        {
            config.ConfigureLightOpacity(.225f, -1f, .2f);
            var restored = ScriptableObject.CreateInstance<ZoneMapConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(config), restored);
                Assert.That(restored.LightOpacityConfigured, Is.True);
                Assert.That(restored.LightOpacityFor(0), Is.EqualTo(.225f));
                Assert.That(restored.LightOpacityFor(1), Is.EqualTo(.2f));
                Assert.That(restored.LightOpacityFor(2), Is.EqualTo(.225f));
            }
            finally { Object.DestroyImmediate(restored); }
        }
    }
}

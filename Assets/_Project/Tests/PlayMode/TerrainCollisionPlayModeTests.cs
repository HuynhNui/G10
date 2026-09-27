using G10.Prototype.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    /// <summary>Self-contained navigation tests; never load or modify the player's save.</summary>
    public sealed class TerrainCollisionPlayModeTests
    {
        private GameObject vessel;
        private ZoneNavigation navigation;

        [SetUp]
        public void Setup()
        {
            vessel = new GameObject("Terrain collision test vessel");
            navigation = vessel.AddComponent<ZoneNavigation>();
            var cells = new byte[120 * 70];
            for (int y = 0; y < 70; y++)
                for (int x = 0; x < 60; x++) cells[y * 120 + x] = 1;
            navigation.SetChart(cells, 120, 70); // Vertical shoreline at chart X = 600.
            navigation.RestoreVoyage(new Vector2(580, 350), 90, 100, 0);
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(vessel);

        [TestCase(1f, 90f)]
        [TestCase(-1f, 270f)]
        public void ForwardAndReverseImpactsLoseExactlyAbsoluteSpeed(float throttle, float heading)
        {
            navigation.RestoreVoyage(new Vector2(580, 350), heading, 100, 0);
            navigation.Step(throttle, 0, 1);

            Assert.That(navigation.Obstructed, Is.True);
            Assert.That(navigation.Speed, Is.Zero, "Collision stops the vessel after measuring impact speed.");
            Assert.That(navigation.Ship.Hull, Is.EqualTo(82).Within(.001f), "An 18-speed impact removes 18 Hull in either direction.");
            Assert.That(navigation.CanOccupy(navigation.Position), Is.True);
        }

        [Test]
        public void SlowImpactUsesCurrentSpeedInsteadOfMaximumSpeed()
        {
            navigation.RestoreVoyage(new Vector2(597.5f, 350), 90, 100, 0);
            navigation.Step(1, 0, .25f); // Acceleration 18: actual speed is 4.5, not max speed 18.

            Assert.That(navigation.Obstructed, Is.True);
            Assert.That(navigation.Ship.Hull, Is.EqualTo(95.5f).Within(.001f));
        }

        [Test]
        public void CoastingImpactUsesRetainedVelocity()
        {
            navigation.RestoreVoyage(new Vector2(570, 350), 90, 100, 0);
            navigation.Step(1, 0, .5f);
            Assert.That(navigation.Speed, Is.EqualTo(9).Within(.001f));
            navigation.Coast(3);

            Assert.That(navigation.Obstructed, Is.True);
            Assert.That(navigation.Ship.Hull, Is.EqualTo(91).Within(.001f));
        }

        [Test]
        public void HoldingAgainstTerrainDamagesOnceUntilTheVesselMovesClear()
        {
            navigation.Step(1, 0, 1);
            Vector2 impactPosition = navigation.Position;
            for (int frame = 0; frame < 120; frame++) navigation.Step(1, 0, 1f / 60f);
            navigation.Brake();
            navigation.Step(1, 0, 1);

            // Smaller integration ticks may settle into the final <=1 m before the blocked sample.
            Assert.That(Vector2.Distance(navigation.Position, impactPosition), Is.LessThanOrEqualTo(1.01f));
            Assert.That(navigation.CanOccupy(navigation.Position), Is.True);
            Assert.That(navigation.Obstructed, Is.True);
            Assert.That(navigation.Ship.Hull, Is.EqualTo(82).Within(.001f), "Holding throttle or reopening the helm must not repeat contact damage.");

            navigation.Step(-1, 0, .5f);
            Assert.That(Vector2.Distance(impactPosition, navigation.Position), Is.GreaterThan(2));
            navigation.Brake();
            navigation.Step(1, 0, 1);

            Assert.That(navigation.Obstructed, Is.True);
            Assert.That(navigation.Ship.Hull, Is.EqualTo(64).Within(.001f), "Moving clear and hitting again is a new impact.");
        }

        [TestCase(0f)]
        [TestCase(.25f)]
        [TestCase(4f)]
        public void SavedLegacyDamageMultipliersCannotChangeTheOneToOneRule(float legacyMultiplier)
        {
            var saved = navigation.Ship.Export();
            saved.collisionDamagePerSpeed = legacyMultiplier;
            navigation.Ship.Restore(saved);
            navigation.Step(1, 0, 1);

            Assert.That(navigation.Ship.Hull, Is.EqualTo(82).Within(.001f));
            Assert.That(navigation.Ship.Export().collisionDamagePerSpeed, Is.EqualTo(1));
            Assert.That(saved.collisionDamagePerSpeed, Is.EqualTo(legacyMultiplier), "Restoring must not mutate the source checkpoint.");
        }

        [Test]
        public void FatalImpactClampsHullAndStopsFurtherMovement()
        {
            var saved = navigation.Ship.Export();
            saved.hull = 10;
            navigation.Ship.Restore(saved);
            navigation.Step(1, 0, 1);
            Vector2 wreckPosition = navigation.Position;

            Assert.That(navigation.Ship.Hull, Is.Zero);
            Assert.That(navigation.Ship.CanMove, Is.False);
            navigation.Step(-1, 0, 1);
            Assert.That(navigation.Position, Is.EqualTo(wreckPosition));
            Assert.That(navigation.Speed, Is.Zero);
            Assert.That(navigation.Ship.Export().IsValid, Is.True);
        }

        [Test]
        public void StationaryVesselAndOpenWaterDoNotLoseHull()
        {
            navigation.Step(0, 0, 1);
            navigation.Step(-1, 0, 1);
            Assert.That(navigation.Obstructed, Is.False);
            Assert.That(navigation.Ship.Hull, Is.EqualTo(100));
        }
    }
}

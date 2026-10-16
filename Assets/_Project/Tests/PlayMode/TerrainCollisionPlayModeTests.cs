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
            navigation.ConfigureMapCoordinates(new Vector2(1200, 700), 50);
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
            navigation.Step(throttle, 0, throttle < 0 ? 3 : 1);

            Assert.That(navigation.Obstructed, Is.True);
            Assert.That(navigation.Speed, Is.Zero, "Collision stops the vessel after measuring impact speed.");
            Assert.That(navigation.Ship.Hull, Is.EqualTo(throttle < 0 ? 94 : 82).Within(.001f), "Damage still equals actual impact speed: forward 18, reverse 6.");
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

            navigation.Step(-1, 0, 1f);
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

        [TestCase(1f, 270f, 0f, 30f)]
        [TestCase(-1f, 90f, 230f, 60f)]
        [TestCase(1f, 270f, 500f, 144f)]
        public void ShallowRestoredOverlapCanMoveOutAtGameplayTickRates(float throttle, float heading, float depth, float rate)
        {
            // A saved position can have its centre in water while its 2 m clearance overlaps terrain.
            navigation.RestoreVoyage(new Vector2(598.5f, 350), heading, depth, 0);
            Assert.That(navigation.IsWater(navigation.Position), Is.True);
            Assert.That(navigation.CanOccupy(navigation.Position), Is.False);
            for (int frame = 0; frame < rate; frame++) navigation.Step(throttle, 0, 1f / rate);

            Assert.That(navigation.Position.x, Is.LessThan(598));
            Assert.That(navigation.CanOccupy(navigation.Position), Is.True);
            Assert.That(navigation.Obstructed, Is.False);
            Assert.That(navigation.Depth, Is.EqualTo(depth));
            Assert.That(navigation.Ship.Hull, Is.EqualTo(100), "Backing out of an existing clearance overlap is not a new impact.");
            Assert.That(navigation.Ship.Energy, Is.LessThan(100));
            Assert.That(Mathf.Abs(navigation.Speed), Is.LessThanOrEqualTo(throttle < 0 ? 6 : 18));
        }

        [TestCase(0f)] [TestCase(230f)] [TestCase(500f)]
        public void CollisionAllowsTurningAndForwardTravelAlongTheNewClearHeading(float depth)
        {
            navigation.RestoreVoyage(navigation.Position, 90, depth, 0);
            navigation.Step(1, 0, 1);
            Vector2 impact = navigation.Position;
            navigation.Step(0, 1, 4.5f);
            navigation.Step(1, 0, 1);

            Assert.That(navigation.Heading, Is.EqualTo(270).Within(.001f));
            Assert.That(navigation.Position.x, Is.LessThan(impact.x - 10));
            Assert.That(navigation.CanOccupy(navigation.Position), Is.True);
            Assert.That(navigation.Obstructed, Is.False);
            Assert.That(navigation.Ship.Hull, Is.EqualTo(82));
        }

        [TestCase(90f)] [TestCase(0f)]
        public void ExistingOverlapCannotMoveDeeperOrSlideAlongTerrain(float heading)
        {
            navigation.RestoreVoyage(new Vector2(598.5f, 350), heading, 100, 0);
            Vector2 before = navigation.Position;
            navigation.Step(1, 0, .25f);

            Assert.That(navigation.Position, Is.EqualTo(before));
            Assert.That(navigation.Obstructed, Is.True);
        }

        [Test]
        public void ExistingOverlapCannotEscapeThroughSolidTerrain()
        {
            navigation.RestoreVoyage(new Vector2(601, 350), 270, 100, 0);
            Vector2 before = navigation.Position;
            navigation.Step(1, 0, .25f);

            Assert.That(navigation.Position, Is.EqualTo(before));
            Assert.That(navigation.Obstructed, Is.True);
        }

        [Test]
        public void EscapingOverlapCannotCreateANewCollisionAtACorner()
        {
            var cells = new byte[120 * 70];
            for (int y = 34; y < 70; y++)
                for (int x = 0; x < 60; x++) cells[y * 120 + x] = 1;
            navigation.SetChart(cells, 120, 70);
            navigation.RestoreVoyage(new Vector2(598.5f, 342.1f), 225, 100, 0);
            Vector2 before = navigation.Position;
            navigation.Step(1, 0, .25f);

            Assert.That(navigation.Position, Is.EqualTo(before), "Leaving the right wall cannot enter the bottom wall.");
            Assert.That(navigation.Obstructed, Is.True);
        }

        [TestCase(1.5f, 350f, 90f)]
        [TestCase(1198.5f, 350f, 270f)]
        [TestCase(350f, 1.5f, 0f)]
        [TestCase(350f, 698.5f, 180f)]
        public void ShallowMapEdgeOverlapCanRetreatIntoWater(float x, float y, float heading)
        {
            var cells = new byte[120 * 70];
            System.Array.Fill(cells, (byte)1);
            navigation.SetChart(cells, 120, 70);
            navigation.RestoreVoyage(new Vector2(x, y), heading, 100, 0);
            Assert.That(navigation.CanOccupy(navigation.Position), Is.False);
            for (int frame = 0; frame < 60; frame++) navigation.Step(1, 0, 1f / 60);

            Assert.That(navigation.CanOccupy(navigation.Position), Is.True);
            Assert.That(navigation.Ship.Hull, Is.EqualTo(100));
            Assert.That(navigation.Obstructed, Is.False);
        }
    }
}

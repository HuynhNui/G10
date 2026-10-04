using G10.Prototype.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class NavigationBudgetPlayModeTests
    {
        private GameObject owner;
        private ZoneNavigation nav;
        [SetUp] public void Setup()
        {
            owner = new GameObject("Navigation budget test");
            nav = owner.AddComponent<ZoneNavigation>();
            nav.ConfigureMapCoordinates(new Vector2(1200, 700), 50);
            var cells = new byte[120 * 70];
            System.Array.Fill(cells, (byte)1);
            nav.SetChart(cells, 120, 70);
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(owner);

        [Test] public void ReverseMaximumIsOneThirdAndTakesTwiceForwardTime()
        {
            nav.Step(1, 0, .5f); Assert.That(nav.Speed, Is.EqualTo(9).Within(.001));
            nav.Step(1, 0, .5f); Assert.That(nav.Speed, Is.EqualTo(nav.Ship.Speed));
            nav.Brake();
            nav.Step(-1, 0, 1); Assert.That(nav.Speed, Is.EqualTo(-3).Within(.001));
            nav.Step(-1, 0, 1); Assert.That(nav.Speed, Is.EqualTo(-nav.Ship.Speed / 3).Within(.001));
            nav.Step(-1, 0, 1); Assert.That(nav.Speed, Is.EqualTo(-6).Within(.001));
        }
        [Test] public void ForwardToReverseBrakesNormallyThenUsesRemainingTick()
        {
            nav.Step(1, 0, 1);
            nav.Step(-1, 0, .5f); Assert.That(nav.Speed, Is.EqualTo(9).Within(.001));
            nav.Step(-1, 0, 1); Assert.That(nav.Speed, Is.EqualTo(-1.5f).Within(.001));
        }
        [TestCase(0, 1, 0, 1)]
        [TestCase(1, 1, 0, 1)]
        [TestCase(1, 1, 1, 1)]
        [TestCase(0, .5f, 0, .5f)]
        [TestCase(0, 0, 0, 0)]
        public void AllAxesShareOneEnergyBudget(float move, float turn, float depth, float cost)
        {
            float before = nav.Ship.Energy;
            nav.Navigate(move, turn, depth, 1);
            Assert.That(before - nav.Ship.Energy, Is.EqualTo(cost * nav.Ship.EnergyPerSecond).Within(.001));
        }
    }
}

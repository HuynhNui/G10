using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class ZoneDepthPlayModeTests
    {
        private GameObject owner;
        private ZoneNavigation nav;
        private PhotoSurveyZone survey;
        private MissionDefinition mission;
        private MapPoi poi;

        [SetUp] public void Setup()
        {
            owner = new GameObject("Depth plumbing fixture");
            nav = owner.AddComponent<ZoneNavigation>();
            nav.ConfigureMapCoordinates(new Vector2(1200, 700), 50);
            var water = new byte[120 * 70]; System.Array.Fill(water, (byte)1);
            nav.SetChart(water, 120, 70);
            survey = owner.AddComponent<PhotoSurveyZone>();
            mission = ScriptableObject.CreateInstance<MissionDefinition>();
            poi = new MapPoi { id = "depth-test", mapPosition = new Vector2(600, 120) };
            mission.targetPoiId = poi.id; survey.mission = mission;
            survey.locations = new[] { poi };
            survey.RestoreCreatureSpawn(1, poi.id, poi.mapPosition);
            nav.RestoreVoyage(new Vector2(600, 100), 0, 230, 0);
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(owner); Object.DestroyImmediate(mission); }

        [TestCase(230)] [TestCase(500)]
        public void AscentStopsAtFloorThroughBothMovementApis(float floor)
        {
            nav.ConfigureDepthRange(floor);
            nav.RestoreVoyage(nav.Position, 0, 500, 0);
            nav.StepDepth(-1, 1000);
            Assert.That(nav.Depth, Is.EqualTo(floor));
            nav.Ship.Refill(); nav.RestoreVoyage(nav.Position, 0, 500, 0);
            nav.Navigate(0, 0, -1, 1000);
            Assert.That(nav.Depth, Is.EqualTo(floor));
            float energy = nav.Ship.Energy;
            nav.StepDepth(-1, 1);
            Assert.That(nav.Ship.Energy, Is.EqualTo(energy), "No movement at the floor costs no energy.");
        }
        [Test] public void DescentStillReachesShipCeiling()
        {
            nav.ConfigureDepthRange(230); nav.StepDepth(1, 1000);
            Assert.That(nav.Depth, Is.EqualTo(nav.Ship.MaximumDepth));
        }
        [TestCase(500, 230, 500)] [TestCase(230, 400, 400)]
        [TestCase(230, 0, 230)] [TestCase(0, 100, 100)]
        public void RestoreUsesActiveRange(float floor, float saved, float expected)
        {
            nav.ConfigureDepthRange(floor); nav.RestoreVoyage(nav.Position, 90, saved, 25);
            Assert.That(nav.Depth, Is.EqualTo(expected));
            Assert.That(nav.Heading, Is.EqualTo(90)); Assert.That(nav.DistanceTravelled, Is.EqualTo(25));
        }
        [Test] public void ResetAndSceneSettingsCannotBypassFloor()
        {
            nav.ConfigureDepthRange(500); nav.ResetVoyage();
            Assert.That(nav.Depth, Is.EqualTo(500));
            nav.ApplySceneShipSettings(); Assert.That(nav.Depth, Is.EqualTo(500));
        }
        [Test] public void ZoneOneCanAscendBelowEntryDepth()
        {
            nav.ConfigureDepthRange(0); nav.StepDepth(-1, 1);
            Assert.That(nav.Depth, Is.LessThan(230)); Assert.That(nav.Depth, Is.GreaterThanOrEqualTo(0));
        }
        [TestCase(-10)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidFloorAndRestoreRemainFinite(float floor)
        {
            nav.ConfigureDepthRange(floor); nav.RestoreVoyage(nav.Position, 0, float.NaN, 0);
            Assert.That(nav.MinimumDepth, Is.Zero); Assert.That(nav.Depth, Is.Zero);
        }
        [Test] public void ImpossibleRangeRetainsAuthoredFloorForFutureCapability()
        {
            nav.ConfigureDepthRange(800);
            Assert.That(nav.MinimumDepth, Is.EqualTo(500)); Assert.That(nav.Depth, Is.EqualTo(500));
            var ship = nav.Ship.Export(); ship.maximumDepth = 1000; nav.Ship.Restore(ship);
            nav.RestoreVoyage(nav.Position, 0, 230, 0);
            Assert.That(nav.MinimumDepth, Is.EqualTo(800)); Assert.That(nav.Depth, Is.EqualTo(800));
        }
        [Test] public void FallbackAndOverrideUseOneResolverWithoutCappingMissionContent()
        {
            poi.targetDepth = 420;
            Assert.That(poi.overrideDepth, Is.False);
            Assert.That(survey.DepthFor(poi), Is.EqualTo(230)); Assert.That(survey.DepthFor(null), Is.EqualTo(230));
            poi.overrideDepth = true; Assert.That(survey.DepthFor(poi), Is.EqualTo(420));
            poi.targetDepth = 1500; Assert.That(survey.DepthFor(poi), Is.EqualTo(1500));
            Assert.That(poi.targetDepth, Is.EqualTo(1500));
        }
        [TestCase(-10)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidPoiDepthIsSafeWithoutRewritingContent(float depth)
        {
            poi.overrideDepth = true; poi.targetDepth = depth;
            Assert.That(survey.DepthFor(poi), Is.Zero);
            Assert.That(poi.targetDepth, Is.EqualTo(depth));
        }
        [Test] public void RadarAndPhotoUseResolvedThreeDimensionalDistance()
        {
            Assert.That(survey.TryGetRadarContact(nav, 85, out _, out _), Is.True);
            Assert.That(survey.TryGetPhotoContact(nav, 85, 60, out _, out _), Is.True);
            poi.overrideDepth = true; poi.targetDepth = 420;
            Assert.That(survey.ContactWorldPosition(poi).z, Is.EqualTo(-420));
            Assert.That(survey.CreaturePosition.z, Is.EqualTo(-420));
            Assert.That(survey.TryGetRadarContact(nav, 85, out _, out _), Is.False);
            Assert.That(survey.TryGetPhotoContact(nav, 85, 60, out _, out _), Is.False);
            Assert.That(survey.Detectable(nav, poi, 85), Is.False);
            nav.RestoreVoyage(nav.Position, 0, 420, 0);
            Assert.That(survey.TryGetRadarContact(nav, 85, out var radar, out _), Is.True);
            Assert.That(radar, Is.SameAs(poi));
            Assert.That(survey.TryGetPhotoContact(nav, 85, 60, out var photo, out var position), Is.True);
            Assert.That(photo, Is.SameAs(poi)); Assert.That(position.z, Is.EqualTo(-420));
        }
        [TestCase(250, false)] [TestCase(260, true)] [TestCase(270, true)]
        [TestCase(290, true)] [TestCase(300, false)]
        public void RoundTwoAuthoritativeDepthBand(float depth, bool valid)
        {
            poi.overrideDepth = true; poi.targetDepth = 260;
            nav.RestoreVoyage(nav.Position, 0, depth, 0);
            Assert.That(survey.TryGetRadarContact(nav, 85, out _, out _), Is.EqualTo(valid));
            Assert.That(survey.TryGetPhotoContact(nav, 85, 60, out _, out _), Is.EqualTo(valid));
        }
        [Test] public void RadarKeepsHorizontalRangeWhileCaptureRequiresArrivalRadius()
        {
            poi.overrideDepth = true; poi.targetDepth = 260;
            nav.RestoreVoyage(poi.mapPosition - Vector2.up * 84, 0, 290, 0);
            Assert.That(survey.TryGetRadarContact(nav, 85, out _, out _), Is.True);
            var catcher = owner.AddComponent<CreatureCatcher>(); catcher.navigation = nav;
            catcher.survey = survey; catcher.inventory = owner.AddComponent<CreatureInventory>();
            survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.TooFar));
            nav.RestoreVoyage(poi.mapPosition - Vector2.up * 86, 0, 290, 0);
            Assert.That(survey.TryGetRadarContact(nav, 85, out _, out _), Is.False);
            nav.RestoreVoyage(poi.mapPosition - Vector2.up * 20, 0, 290, 0);
            nav.SetChart(new byte[4], 2, 2);
            Assert.That(survey.TryGetRadarContact(nav, 85, out _, out _), Is.False);
            Assert.That(survey.TryGetPhotoContact(nav, 85, 60, out _, out _), Is.False);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Occluded));
        }
        [TestCase(0)] [TestCase(230)] [TestCase(500)]
        public void ResourceRejectionHasPriorityAndDoesNotLatchDepthLimit(float depth)
        {
            int limits = 0, rejects = 0; ZoneNavigation.MovementRejection reason = default;
            nav.DepthLimitReached += () => limits++;
            nav.MovementRejected += value => { rejects++; reason = value; };
            nav.RestoreVoyage(nav.Position, 0, depth, 0);
            var state = nav.Ship.Export(); state.energy = 0; nav.Ship.Restore(state);
            nav.StepDepth(-1, 1); nav.Navigate(0, 0, 1, 1);
            Assert.That(limits, Is.Zero); Assert.That(rejects, Is.EqualTo(1));
            Assert.That(reason, Is.EqualTo(ZoneNavigation.MovementRejection.NoEnergy));
            state.hull = 0; nav.Ship.Restore(state); nav.StepDepth(1, 1);
            Assert.That(reason, Is.EqualTo(ZoneNavigation.MovementRejection.Destroyed));
            Assert.That(nav.Depth, Is.EqualTo(depth)); Assert.That(limits, Is.Zero);
        }
        [Test] public void UnlimitedModePreservesLegacyChargeStateAndLimitedModeStillWorks()
        {
            var state = nav.Ship.Export(); state.captures = 0;
            nav.Ship.Restore(state);
            Assert.That(nav.Ship.UnlimitedCaptureAttempts, Is.True);
            Assert.That(nav.Ship.CanAttemptCapture, Is.True); Assert.That(nav.Ship.LowResources, Is.False);
            for (int i = 0; i < 20; i++) Assert.That(nav.Ship.TryUse(ShipCharge.Capture), Is.True);
            Assert.That(nav.Ship.Captures, Is.Zero);
            nav.Ship.UnlimitedCaptureAttempts = false;
            Assert.That(nav.Ship.CanAttemptCapture, Is.False); Assert.That(nav.Ship.TryUse(ShipCharge.Capture), Is.False);
            state.captures = 1; nav.Ship.Restore(state);
            Assert.That(nav.Ship.TryUse(ShipCharge.Capture), Is.True); Assert.That(nav.Ship.Captures, Is.Zero);
        }
        [Test] public void CaptureChecksResolvedDepthToleranceBeforeStartingMinigame()
        {
            var catcher = owner.AddComponent<CreatureCatcher>();
            catcher.navigation = nav; catcher.survey = survey;
            catcher.inventory = owner.AddComponent<CreatureInventory>();
            survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);
            poi.overrideDepth = true; poi.targetDepth = 420;
            nav.RestoreVoyage(poi.mapPosition, 0, 230, 0);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.TooShallow));
            nav.RestoreVoyage(poi.mapPosition, 0, 420 + survey.DeeperInteractionRange + 1, 0);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.TooDeep));
            nav.RestoreVoyage(poi.mapPosition, 0, 420 + survey.DeeperInteractionRange, 0);
            // Full proves depth validation passed; no fake minigame required.
            for (int i = 0; i < CreatureInventory.Capacity; i++) catcher.inventory.TryAdd("item" + i, "item", null);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Full));
            poi.overrideDepth = false; nav.RestoreVoyage(poi.mapPosition, 0, 230, 0);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Full));
        }
    }
}

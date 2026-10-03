using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class RadarTerrainMaskTests
    {
        private GameObject vessel;
        private ZoneNavigation navigation;
        private const int Width = 1200, Height = 700;

        [SetUp] public void Setup()
        {
            vessel = new GameObject("Radar terrain test");
            navigation = vessel.AddComponent<ZoneNavigation>();
            navigation.ConfigureMapCoordinates(new Vector2(Width, Height), ZoneNavigation.DefaultGridSize);
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(vessel);
        private static byte[] OpenWater()
        {
            var water = new byte[Width * Height];
            Array.Fill(water, (byte)1);
            return water;
        }

        [Test] public void EdgeToEdgeContourFillsFarSideAndKeepsShipSideClear()
        {
            var water = OpenWater();
            for (int y = 0; y < Height; y++) water[y * Width + 650] = 0;
            navigation.SetChart(water, Width, Height);
            Assert.That(navigation.IsRadarTerrain(new Vector2(620, 100)), Is.False);
            Assert.That(navigation.IsRadarTerrain(new Vector2(650, 100)), Is.True);
            Assert.That(navigation.IsRadarTerrain(new Vector2(680, 100)), Is.True, "The terrain beyond the line must be filled, not only its contour.");
            Assert.That(navigation.IsWater(new Vector2(680, 100)), Is.True, "Radar fill must not rewrite the authored navigation mask.");
            Assert.That(navigation.IsRadarTerrain(new Vector2(-1, 100)), Is.True);
        }

        [Test] public void ClosedIslandIsFilledWithoutFillingSurroundingWater()
        {
            var water = OpenWater();
            for (int y = 90; y <= 210; y++) { water[y * Width + 640] = 0; water[y * Width + 690] = 0; }
            for (int x = 640; x <= 690; x++) { water[90 * Width + x] = 0; water[210 * Width + x] = 0; }
            navigation.SetChart(water, Width, Height);
            Assert.That(navigation.IsRadarTerrain(new Vector2(665, 150)), Is.True);
            Assert.That(navigation.IsRadarTerrain(new Vector2(710, 150)), Is.False);
            Assert.That(navigation.IsRadarTerrain(new Vector2(620, 150)), Is.False);
        }

        [Test] public void SubShipWidthLineGapDoesNotLeakAndReplacingChartInvalidatesCache()
        {
            var water = OpenWater();
            for (int y = 0; y < Height; y++) if (y != 101) water[y * Width + 650] = 0;
            navigation.SetChart(water, Width, Height);
            Assert.That(navigation.IsRadarTerrain(new Vector2(680, 100)), Is.True);
            navigation.SetChart(OpenWater(), Width, Height);
            Assert.That(navigation.IsRadarTerrain(new Vector2(680, 100)), Is.False);
        }

        [Test] public void RestoredVoyageRebuildsWaterRegionFromItsNewPosition()
        {
            var water = OpenWater();
            for (int y = 0; y < Height; y++) water[y * Width + 650] = 0;
            navigation.SetChart(water, Width, Height);
            Assert.That(navigation.IsRadarTerrain(new Vector2(680, 100)), Is.True);
            navigation.RestoreVoyage(new Vector2(680, 100), 0, 230, 0);
            Assert.That(navigation.IsRadarTerrain(new Vector2(680, 100)), Is.False);
            Assert.That(navigation.IsRadarTerrain(new Vector2(600, 100)), Is.True);
        }

        [Test] public void InvalidOriginDoesNotCacheAnAllSolidMapAndEmptyChartIsSafe()
        {
            var water = OpenWater();
            for (int y = 0; y < Height; y++) water[y * Width + 650] = 0;
            navigation.SetChart(water, Width, Height);
            navigation.RestoreVoyage(new Vector2(648.5f, 100), 270, 230, 0);
            Assert.That(navigation.CanOccupy(navigation.Position), Is.False);
            Assert.That(navigation.IsRadarTerrain(new Vector2(600, 100)), Is.False);
            navigation.Step(1, 0, 1);
            Assert.That(navigation.CanOccupy(navigation.Position), Is.True);
            Assert.That(navigation.IsRadarTerrain(new Vector2(680, 100)), Is.True);
            navigation.SetChart(Array.Empty<byte>(), 1, 0);
            Assert.That(navigation.HasChart, Is.False);
            Assert.That(navigation.IsRadarTerrain(new Vector2(600, 100)), Is.True);
        }
    }

    public sealed class RadarFilledTerrainPlayModeTests
    {
        private string folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "FilledRadar-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            ExpeditionSaveStore.PathOverride = null;
            PhotoCaptureService.ArchivePathOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [UnityTest] public IEnumerator AuthoredLineMapFillsTerrainAndRadarStillRevealsWithSweep()
        {
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            var navigation = cabin.Navigation;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Assert.That(navigation.IsRadarTerrain(navigation.Position), Is.False);
            timer.Stop();
            Debug.Log($"Radar connected-water mask initial build: {timer.Elapsed.TotalMilliseconds:0.0} ms");
            foreach (var poi in cabin.GetComponent<PhotoSurveyZone>().locations)
                Assert.That(navigation.IsRadarTerrain(poi.mapPosition), Is.False, $"Mission site {poi.id} must stay in the navigable water region.");
            Assert.That(navigation.IsRadarTerrain(new Vector2(200, 500)), Is.True, "Northwest area beyond the contour must be filled.");
            Assert.That(navigation.IsRadarTerrain(new Vector2(1100, 100)), Is.True, "Southeast area beyond the contour must be filled.");

            // Place inside the water side near the hand-drawn shoreline to capture both open water and filled terrain.
            Vector2 position = new(270, 330);
            Assert.That(navigation.CanOccupy(position), Is.True);
            navigation.RestoreVoyage(position, 0, 230, 0);
            cabin.OpenRadar();
            int charges = navigation.Ship.Radar;
            cabin.Radar.StartSweep();
            Assert.That(cabin.Radar.IsScanning, Is.True);
            Assert.That(navigation.Ship.Radar, Is.EqualTo(charges - 1));
            yield return new WaitForSecondsRealtime(.6f);
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "radar-filled-sweeping.png");
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.That(cabin.Radar.IsScanning, Is.False);
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "radar-filled-complete.png");
        }
    }
}

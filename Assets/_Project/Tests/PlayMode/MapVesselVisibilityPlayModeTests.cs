using System.Reflection;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Tests
{
    public sealed class MapVesselVisibilityPlayModeTests
    {
        [Test]
        public void ChartNeverDrawsVesselButKeepsGridAndExitMarker()
        {
            var owner = new GameObject("Map vessel visibility fixture", typeof(RectTransform));
            var map = ScriptableObject.CreateInstance<ZoneMapConfig>();
            try
            {
                var nav = owner.AddComponent<ZoneNavigation>();
                var runtime = owner.AddComponent<ZoneMissionRuntime>();
                var overlay = owner.AddComponent<PhotoSurveyMap>();
                overlay.navigation = nav; overlay.missionRuntime = runtime; overlay.mapConfig = map;
                overlay.rectTransform.sizeDelta = new Vector2(1200,700);
                overlay.showGrid = false;
                Assert.That(VertexCount(overlay), Is.Zero, "No vessel strokes when grid and routes are absent.");
                map.destinationZone = "Zone02";
                map.exitArea.mapPosition = new Vector2(500,300);
                Assert.That(VertexCount(overlay), Is.EqualTo(16), "The four exit marker strokes remain.");
                nav.RestoreVoyage(new Vector2(800,200), 90, 230, 0);
                Assert.That(nav.Position, Is.EqualTo(new Vector2(800,200)));
                Assert.That(VertexCount(overlay), Is.EqualTo(16), "Moving/turning never adds vessel geometry.");
                overlay.showGrid = true;
                Assert.That(VertexCount(overlay), Is.GreaterThan(16), "Chart grid is preserved.");
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(map); }
        }
        private static int VertexCount(PhotoSurveyMap overlay)
        {
            using var vertices = new VertexHelper();
            typeof(PhotoSurveyMap).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null)
                .Invoke(overlay, new object[] {vertices});
            return vertices.currentVertCount;
        }
    }
}

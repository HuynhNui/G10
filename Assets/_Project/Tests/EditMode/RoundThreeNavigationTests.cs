using System.Collections.Generic;
using G10.Prototype.Navigation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class RoundThreeNavigationTests
    {
        [Test] public void ReturnGatesAndBackwardArrivalsHaveFootprintClearanceAndReachableWater()
        {
            var owner = new GameObject("Round 3 travel terrain fixture");
            try
            {
                var nav = owner.AddComponent<ZoneNavigation>();
                for (int zone = 1; zone <= 4; zone++)
                {
                    var map = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"Assets/_Project/Content/Maps/Zone0{zone}.asset");
                    Assert.That(map.ApplyTerrain(nav, false), Is.True);
                    if (zone > 1)
                    {
                        Assert.That(map.returnDestinationZone, Is.EqualTo($"Zone0{zone - 1}"));
                        Assert.That(map.returnArea.arrivalRadius, Is.EqualTo(75));
                        Assert.That(map.returnArea.Contains(map.entryPosition), Is.False);
                        Assert.That(nav.CanOccupy(map.returnArea.mapPosition), Is.True);
                        AssertReachable(nav, map.entryPosition, map.returnArea.mapPosition, map.WorldSize);
                        foreach (var poi in map.locations)
                            Assert.That(Vector2.Distance(poi.mapPosition, map.returnArea.mapPosition), Is.GreaterThan(poi.arrivalRadius + map.returnArea.arrivalRadius));
                    }
                    else Assert.That(map.returnDestinationZone, Is.Null.Or.Empty);
                    if (zone == 4) continue;
                    Assert.That(map.ApplyTerrain(nav, zone == 3), Is.True);
                    Assert.That(nav.CanOccupy(map.backwardArrivalPosition), Is.True);
                    Assert.That(map.exitArea.Contains(map.backwardArrivalPosition), Is.False);
                    AssertReachable(nav, map.exitArea.mapPosition, map.backwardArrivalPosition, map.WorldSize);
                    Vector2 ahead = new(Mathf.Sin(map.backwardArrivalHeading * Mathf.Deg2Rad), Mathf.Cos(map.backwardArrivalHeading * Mathf.Deg2Rad));
                    for (int i = 0; i <= 25; i++) Assert.That(nav.CanOccupy(map.backwardArrivalPosition + ahead * i), Is.True, map.zoneId);
                }
            }
            finally { Object.DestroyImmediate(owner); }
        }
        private static void AssertReachable(ZoneNavigation nav, Vector2 start, Vector2 target, Vector2 size)
        {
            Vector2Int Node(Vector2 p) => new(Mathf.RoundToInt(p.x / 5), Mathf.RoundToInt(p.y / 5));
            var queue = new Queue<Vector2Int>(); var seen = new HashSet<Vector2Int>();
            var origin = Node(start); var end = Node(target); queue.Enqueue(origin); seen.Add(origin);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue(); if (node == end) return;
                foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var next = node + direction; var point = new Vector2(next.x * 5, next.y * 5);
                    if (point.x < 0 || point.y < 0 || point.x >= size.x || point.y >= size.y ||
                        seen.Contains(next) || !nav.CanOccupy(point)) continue;
                    seen.Add(next); queue.Enqueue(next);
                }
            }
            Assert.Fail($"No footprint-safe route {start} → {target}");
        }
    }
}

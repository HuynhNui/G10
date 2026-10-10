using System;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class RoundTwoConfigurationTests
    {
        [Test] public void AllZoneProfilesMatchTuningAndKeepOriginalHitboxes()
        {
            for (int i = 0; i < 4; i++)
            {
                string zone = "Zone0" + (i + 1);
                var map = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"Assets/_Project/Content/Maps/{zone}.asset");
                var profile = map.captureMinigameProfile;
                Assert.That(profile, Is.Not.Null, zone);
                Assert.That(profile.fishMoveSpeed, Is.EqualTo(new[] { 100, 125, 155, 175 }[i]));
                Assert.That(profile.fishTurnSpeed, Is.EqualTo(new[] { 100, 145, 190, 220 }[i]));
                Assert.That(profile.fishMinDecisionInterval, Is.EqualTo(new[] { 1f, .7f, .45f, .35f }[i]));
                Assert.That(profile.fishMaxDecisionInterval, Is.EqualTo(new[] { 1.8f, 1.3f, .9f, .75f }[i]));
                Assert.That(profile.fishFleeSpeedMultiplier, Is.EqualTo(new[] { 1.6f, 1.8f, 2f, 2.2f }[i]));
                Assert.That(profile.requiredHits, Is.EqualTo(i < 2 ? 5 : 6));
                Assert.That(profile.attemptDuration, Is.EqualTo(45));
                Assert.That(profile.hookMoveSpeed, Is.EqualTo(300)); Assert.That(profile.hookTurnSpeed, Is.EqualTo(220));
                Assert.That(profile.hookHitbox, Is.EqualTo(new Vector2(45, 34)));
                Assert.That(profile.creatureHitbox, Is.EqualTo(new Vector2(76, 44)));
            }
        }

        [Test] public void ZoneThreeEntryHasShipClearanceForwardWaterAndRoutesToAllPois()
        {
            var map = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>("Assets/_Project/Content/Maps/Zone03.asset");
            Assert.That(map.entryPosition, Is.EqualTo(new Vector2(11, 97))); Assert.That(map.entryHeading, Is.EqualTo(90));
            Assert.That(map.exitArea.mapPosition, Is.EqualTo(new Vector2(1750, 25)));
            var owner = new GameObject("Round 2 entry fixture");
            try
            {
                var nav = owner.AddComponent<ZoneNavigation>(); Assert.That(map.ApplyTerrain(nav, false), Is.True);
                Assert.That(nav.CanOccupy(map.entryPosition), Is.True);
                for (int x = 0; x <= 25; x++) Assert.That(nav.CanOccupy(map.entryPosition + Vector2.right * x), Is.True);
                var queue = new System.Collections.Generic.Queue<Vector2Int>();
                var seen = new System.Collections.Generic.HashSet<Vector2Int>();
                var start = new Vector2Int(2, 19); queue.Enqueue(start); seen.Add(start);
                while (queue.Count > 0)
                {
                    var point = queue.Dequeue();
                    foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        var next = point + direction;
                        if (next.x < 0 || next.y < 0 || next.x >= 384 || next.y >= 216 || seen.Contains(next) ||
                            !nav.CanOccupy(new Vector2(next.x * 5, next.y * 5))) continue;
                        seen.Add(next); queue.Enqueue(next);
                    }
                }
                foreach (var poi in map.locations)
                    Assert.That(seen.Contains(new Vector2Int(Mathf.RoundToInt(poi.mapPosition.x / 5), Mathf.RoundToInt(poi.mapPosition.y / 5))), Is.True, poi.id);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test] public void SerializedCoreDeadlineIsTwentyFiveAndSceneCaptureIsUnlimited()
        {
            string path = "Assets/_Project/Scenes/Gameplay/GameplayCore.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                ExpeditionLoop loop = null;
                foreach (var root in scene.GetRootGameObjects()) loop ??= root.GetComponentInChildren<ExpeditionLoop>(true);
                Assert.That(loop, Is.Not.Null); Assert.That(loop.TotalDays, Is.EqualTo(25));
                Assert.That(Array.Find(loop.zones, z => z.zone == "Zone03").restAreas[0].mapPosition, Is.EqualTo(new Vector2(11, 97)));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
            scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity", OpenSceneMode.Additive);
            try
            {
                ZoneNavigation nav = null;
                foreach (var root in scene.GetRootGameObjects()) nav ??= root.GetComponentInChildren<ZoneNavigation>(true);
                Assert.That(nav, Is.Not.Null);
                Assert.That(new SerializedObject(nav).FindProperty("resourceSettings.unlimitedCaptureAttempts").boolValue, Is.True);
                var survey = nav.GetComponent<PhotoSurveyZone>();
                Assert.That(survey.DeeperInteractionRange, Is.EqualTo(30));
                Assert.That(nav.GetComponent<G10.Prototype.UI.CabinStationView>().Radar.PostSweepDuration, Is.EqualTo(5));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
            var settings = new ShipResourceSettings(); Assert.That(settings.unlimitedCaptureAttempts, Is.True);
        }
    }
}

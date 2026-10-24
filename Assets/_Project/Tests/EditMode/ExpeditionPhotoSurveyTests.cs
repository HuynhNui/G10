using System;
using System.Collections.Generic;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ExpeditionPhotoSurveyTests
    {
        private static ZoneMissionConfig[] Configs() => Enumerable.Range(1, 4)
            .Select(zone => AssetDatabase.LoadAssetAtPath<ZoneMissionConfig>($"Assets/_Project/Content/Missions/Zone{zone:00}.asset")).ToArray();

        [Test] public void AuthoredPhotoTotalsAreNineMandatoryAndTwoHiddenWithoutStoredImages()
        {
            var configs = Configs(); Assert.That(configs.All(config => config != null), Is.True);
            var counts = ExpeditionPhotoSurvey.Count(configs, (_, _) => false);
            Assert.That(counts.MandatoryTotal, Is.EqualTo(9)); Assert.That(counts.HiddenTotal, Is.EqualTo(2));
            Assert.That(counts.Total, Is.EqualTo(11)); Assert.That(counts.Completed, Is.Zero);
            Assert.That(counts.AllMandatoryPhotosComplete, Is.False); Assert.That(counts.AllPhotosComplete, Is.False);
        }

        [Test] public void DuplicateDefinitionsAndReceiptsCannotInflatePhotoCountsAndCaptureGatesNeverCount()
        {
            var configs = Configs();
            var nonPhotos = configs.ToDictionary(config => config.zoneId, config => new HashSet<string>(config.locations
                .SelectMany(location => location.objectives).Concat(config.zoneGate.objectives)
                .Where(objective => objective.type != MissionObjectiveType.Photograph).Select(objective => objective.id)));
            nonPhotos["Zone04"].Add("Z4_L5_PHOTO"); nonPhotos["Zone04"].Add("NORMAL_ENDING");
            var noPhotos = ExpeditionPhotoSurvey.Count(configs, (zone, id) => nonPhotos[zone].Contains(id));
            Assert.That(noPhotos.Completed, Is.Zero); Assert.That(noPhotos.Total, Is.EqualTo(11));
            var all = ExpeditionPhotoSurvey.Count(configs.Concat(configs), (_, _) => true);
            Assert.That(all.MandatoryCompleted, Is.EqualTo(9)); Assert.That(all.HiddenCompleted, Is.EqualTo(2));
            Assert.That(all.Total, Is.EqualTo(11)); Assert.That(all.AllPhotosComplete, Is.True);
        }

        [Test] public void MandatoryPhotoCompletionDoesNotGrantCaptureDiscoveryOrEndingProgress()
        {
            var configs = Configs(); var snapshot = new ExpeditionSnapshot();
            foreach (var config in configs)
            {
                var zone = new ExpeditionZoneState { zone = config.zoneId };
                foreach (var location in config.locations.Where(location => location.visibility == LocationVisibility.Visible))
                    foreach (var objective in location.objectives.Where(objective => objective.type == MissionObjectiveType.Photograph))
                        zone.missionProgress.completedObjectives.Add(objective.id);
                snapshot.zones.Add(zone);
            }
            string before = JsonUtility.ToJson(snapshot);
            var counts = ExpeditionPhotoSurvey.Count(configs, (zone, id) => snapshot.zones.Single(state => state.zone == zone).missionProgress.completedObjectives.Contains(id));
            Assert.That(counts.MandatoryCompleted, Is.EqualTo(9)); Assert.That(counts.HiddenCompleted, Is.Zero);
            Assert.That(counts.AllMandatoryPhotosComplete, Is.True); Assert.That(counts.AllPhotosComplete, Is.False);
            var owner = new GameObject("photo-only gate check");
            try
            {
                var runtime = owner.AddComponent<ZoneMissionRuntime>(); runtime.config = configs[3];
                runtime.RestoreProgress(snapshot.zones[3].missionProgress);
                Assert.That(runtime.MainObjectivesComplete, Is.False, "Nine photos do not replace the real Zone4 capture requirement.");
                Assert.That(runtime.HiddenRouteAvailable, Is.False); Assert.That(runtime.ProgressState.revealedLocations, Is.Empty);
            }
            finally { Object.DestroyImmediate(owner); }
            Assert.That(JsonUtility.ToJson(snapshot), Is.EqualTo(before), "Counting is read-only.");
        }

        [Test] public void MigratedLegacyPhotoReceiptsRemainCountedWithoutPngEvidenceOrNewCaptures()
        {
            var configs = Configs(); var snapshot = new ExpeditionSnapshot();
            string[][] legacyPhotos = {
                new[] { "Z1_L1_PHOTO", "Z1_L3_PHOTO" }, new[] { "Z2_L1_PHOTO", "Z2_L3_PHOTO" },
                new[] { "Z3_L1_PHOTO", "Z3_L3_PHOTO", "Z3_L4_PHOTO" },
                new[] { "Z4_L1_PHOTO", "Z4_L2_PHOTO", "Z4_L3_PHOTO", "Z4_L4_PHOTO" }
            };
            for (int zone = 0; zone < configs.Length; zone++)
            {
                var state = new ExpeditionZoneState { zone = configs[zone].zoneId };
                state.missionProgress.completedObjectives.AddRange(legacyPhotos[zone]); snapshot.zones.Add(state);
            }
            MissionLayoutMigration.Apply(snapshot);
            string migrated = JsonUtility.ToJson(snapshot);
            var counts = ExpeditionPhotoSurvey.Count(configs, (zone, id) => snapshot.zones.Single(state => state.zone == zone).missionProgress.completedObjectives.Contains(id));
            Assert.That(snapshot.photos, Is.Empty); Assert.That(counts.MandatoryCompleted, Is.EqualTo(8));
            Assert.That(counts.HiddenCompleted, Is.EqualTo(1)); Assert.That(counts.AllPhotosComplete, Is.False);
            Assert.That(snapshot.zones[2].missionProgress.completedObjectives, Does.Not.Contain("Z3_L2_PHOTO"));
            Assert.That(snapshot.zones[3].missionProgress.completedObjectives, Does.Not.Contain("Z4_L4_PHOTO"));
            Assert.That(snapshot.zones.SelectMany(zone => zone.missionProgress.completedObjectives).Any(id => id.EndsWith("_CAPTURE", StringComparison.Ordinal)), Is.False);
            Assert.That(JsonUtility.ToJson(snapshot), Is.EqualTo(migrated));
        }

        [Test] public void MissingOrPartialDefinitionsNeverReportAnEmptySurveyComplete()
        {
            var empty = ExpeditionPhotoSurvey.Count(null, (_, _) => true);
            Assert.That(empty.Total, Is.Zero); Assert.That(empty.AllMandatoryPhotosComplete, Is.False); Assert.That(empty.AllPhotosComplete, Is.False);
            var partial = ExpeditionPhotoSurvey.Count(new[] { Configs()[3] }, (_, _) => true);
            Assert.That(partial.MandatoryTotal, Is.EqualTo(2)); Assert.That(partial.HiddenTotal, Is.EqualTo(2));
        }
    }
}

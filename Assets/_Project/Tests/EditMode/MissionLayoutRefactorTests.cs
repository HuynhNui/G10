using System;
using System.Collections.Generic;
using System.IO;
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
    public sealed class MissionLayoutRefactorTests
    {
        private static ZoneMissionConfig Mission(int zone) => AssetDatabase.LoadAssetAtPath<ZoneMissionConfig>($"Assets/_Project/Content/Missions/Zone{zone:00}.asset");
        private static ZoneMapConfig Map(int zone) => AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"Assets/_Project/Content/Maps/Zone{zone:00}.asset");

        [Test] public void ElevenLocationsHaveExactTargetsObjectivesAndNoDanglingReferences()
        {
            string[][] pois = { new[] { "zone01-left", "zone01-north" }, new[] { "zone02-l1", "zone02-l3" },
                new[] { "zone03-l1", "zone03-l2", "zone03-l4" }, new[] { "zone04-l1", "zone04-l2", "zone04-l3", "zone04-l5" } };
            bool[][] capture = { new[] { true, false }, new[] { true, true }, new[] { true, false, true }, new[] { true, false, false, false } };
            for (int zone = 1; zone <= 4; zone++)
            {
                var config = Mission(zone); var map = Map(zone);
                Assert.That(config.locations.Length, Is.EqualTo(pois[zone - 1].Length));
                CollectionAssert.AreEqual(pois[zone - 1], map.locations.Select(p => p.id));
                var ids = new HashSet<string>();
                for (int index = 0; index < config.locations.Length; index++)
                {
                    var location = config.locations[index];
                    Assert.That(location.id, Is.EqualTo($"Z{zone}_L{index + 1}"));
                    Assert.That(location.poiId, Is.EqualTo(pois[zone - 1][index]));
                    Assert.That(location.objectives.Length, Is.EqualTo(capture[zone - 1][index] ? 2 : 1));
                    Assert.That(location.objectives[0].type, Is.EqualTo(MissionObjectiveType.Photograph));
                    if (capture[zone - 1][index]) Assert.That(location.objectives[1].type, Is.EqualTo(MissionObjectiveType.Capture));
                    foreach (var objective in location.objectives)
                    {
                        Assert.That(ids.Add(objective.id), Is.True);
                        Assert.That(objective.targetId, Is.EqualTo($"Z{zone}_Creature_{index + 1:00}"));
                        Assert.That(objective.required, Is.True);
                    }
                    Assert.That(location.rewards.Any(r => r.type == MissionRewardType.Item), Is.False);
                }
                foreach (var objective in config.zoneGate.objectives) Assert.That(ids.Add(objective.id), Is.True);
                foreach (var completion in config.completions)
                    foreach (string id in completion.requiredObjectiveIds) Assert.That(ids.Contains(id), Is.True, id);
                foreach (var hint in config.hints) Assert.That(ids.Contains(hint.requiredObjectiveId), Is.True);
            }
            CollectionAssert.AreEqual(new[] { "Z4_L3", "Z4_L4" }, Map(4).hiddenLocationIds);
            Assert.That(Mission(4).locations.Count(l => l.visibility == LocationVisibility.HiddenRadar), Is.EqualTo(2));
        }

        [Test] public void RetainedPoisKeepCurrentLocalGeometryRadiusAndDepth()
        {
            Vector2[][] positions = {
                new[] { new Vector2(440, 115.71f), new Vector2(1000, 732.86f) },
                new[] { new Vector2(35.52f, 593.0743f), new Vector2(1715.52f, 486.46286f) },
                new[] { new Vector2(446.08f, 214.45714f), new Vector2(48, 630.56573f), new Vector2(1114.08f, 249.48f) },
                new[] { new Vector2(289.6f, 956.5714f), new Vector2(1772.48f, 693.5143f), new Vector2(1006.56f, 450.51428f), new Vector2(358.56f, 119.57143f) }
            };
            float[][] depths = { new[] { 230f, 460 }, new[] { 280f, 460 }, new[] { 360f, 470, 640 }, new[] { 525f, 590, 615, 735 } };
            for (int zone = 1; zone <= 4; zone++)
                for (int index = 0; index < Map(zone).locations.Length; index++)
                {
                    var poi = Map(zone).locations[index];
                    Assert.That(Vector2.Distance(poi.mapPosition, positions[zone - 1][index]), Is.LessThan(.001f));
                    Assert.That(poi.arrivalRadius, Is.EqualTo(zone == 1 ? 35 : 20));
                    Assert.That(poi.overrideDepth, Is.True);
                    Assert.That(poi.targetDepth, Is.EqualTo(depths[zone - 1][index]));
                }
        }

        [Test] public void CapturesSupplyConsumedModuleMaterialsAndPressureHullChangesOnlyDepth()
        {
            for (int zone = 1; zone <= 3; zone++)
            {
                var owner = new GameObject("mission materials");
                try
                {
                    var nav = owner.AddComponent<ZoneNavigation>();
                    var inventory = owner.AddComponent<CreatureInventory>();
                    var story = owner.AddComponent<ZoneOneStory>();
                    story.config = Mission(zone); story.navigation = nav; story.inventory = inventory;
                    float hp = nav.Ship.HullCapacity;
                    Assert.That(story.ApplyProgressionAction(), Is.False);
                    foreach (var location in story.config.locations)
                    {
                        var photo = location.objectives[0];
                        Assert.That(story.RecordObjective(location.poiId, photo.type, photo.targetId), Is.True);
                        Assert.That(inventory.GetCount(photo.targetId), Is.Zero, "PHOTO must never grant capture materials");
                        if (location.objectives.Length < 2) continue;
                        var capture = location.objectives[1];
                        Assert.That(story.ResolveCapture(location.poiId, capture), Is.True);
                        Assert.That(inventory.GetCount(capture.targetId), Is.EqualTo(1));
                        if (!capture.repeatableCapture) Assert.That(story.ResolveCapture(location.poiId, capture), Is.False);
                    }
                    Assert.That(story.CanApplyProgressionAction, Is.True, story.ProgressionActionDescription);
                    var costs = story.PendingGateObjective.materials;
                    Assert.That(costs.Length, Is.EqualTo(zone == 1 ? 1 : 2));
                    Assert.That(costs.Select(c => c.itemId).Distinct().Count(), Is.EqualTo(costs.Length));
                    string action = story.ProgressionActionTitle;
                    Assert.That(story.ApplyProgressionAction(), Is.True);
                    Assert.That(story.LastMessage, Does.Contain(action));
                    foreach (var cost in costs) Assert.That(inventory.GetCount(cost.itemId), Is.Zero);
                    Assert.That(nav.Ship.HullCapacity, Is.EqualTo(hp));
                    Assert.That(nav.Ship.MaximumDepth, Is.EqualTo(zone == 2 ? 750 : 500));
                    if (zone < 3) Assert.That(story.ApplyProgressionAction(), Is.False);
                    else
                    {
                        Assert.That(story.ApplyProgressionAction(), Is.True, "Install the crafted assembly");
                        Assert.That(story.ApplyProgressionAction(), Is.False, "Breaking still requires a real rock approach");
                    }
                }
                finally { Object.DestroyImmediate(owner); }
            }
        }

        [Test] public void CaptureHintDoesNotRevealOrUnlockHiddenLocations()
        {
            var owner = new GameObject("hint fixture");
            try
            {
                var runtime = owner.AddComponent<ZoneMissionRuntime>(); runtime.config = Mission(4);
                runtime.inventory = owner.AddComponent<CreatureInventory>();
                runtime.RecordObjective("zone04-l1", MissionObjectiveType.Photograph, "Z4_Creature_01");
                Assert.That(runtime.MissionText(), Does.Not.Contain("1006.56"));
                Assert.That(runtime.ResolveCapture("zone04-l1", runtime.FindObjective("zone04-l1", MissionObjectiveType.Capture)), Is.True);
                Assert.That(runtime.MissionText(), Does.Contain("1006.56, 450.51").And.Contain("358.56, 119.57"));
                Assert.That(runtime.ProgressState.revealedLocations, Is.Empty);
                Assert.That(runtime.HiddenRouteAvailable, Is.False);
                var saved = runtime.ExportProgress(); runtime.RestoreProgress(saved);
                Assert.That(runtime.inventory.GetCount("Z4_Creature_01"), Is.EqualTo(1));
                Assert.That(runtime.MissionText(), Does.Contain("1006.56"));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test] public void LegacyExplorationChoiceStillRequiresNewMainCaptureBeforeFinalSignal()
        {
            var owner = new GameObject("legacy hidden choice");
            try
            {
                var runtime = owner.AddComponent<ZoneMissionRuntime>(); runtime.config = Mission(4);
                runtime.inventory = owner.AddComponent<CreatureInventory>(); runtime.HiddenRouteAvailable = true;
                var state = new MissionProgressState();
                foreach (var location in runtime.config.locations)
                {
                    state.completedObjectives.Add(location.id + "_PHOTO");
                    if (location.visibility == LocationVisibility.HiddenRadar) state.revealedLocations.Add(location.id);
                }
                runtime.RestoreProgress(state);
                Assert.That(Map(4).HiddenDestinationAvailable(runtime), Is.False);
                var saved = new ExpeditionZoneState { missionProgress = state, progress = new ZoneProgressState { hiddenRouteUnlocked = true } };
                Assert.That(new G10.Prototype.UI.SavedZoneMapProgress(Map(4), saved).HiddenDestinationAvailable, Is.False);
                Assert.That(runtime.ResolveCapture("zone04-l1", runtime.FindObjective("zone04-l1", MissionObjectiveType.Capture)), Is.True);
                Assert.That(Map(4).HiddenDestinationAvailable(runtime), Is.True);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test] public void SerializedMapOverlaysContainOnlyTheirConfiguredMarkersAndChecks()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var overlay in root.GetComponentsInChildren<G10.Prototype.UI.PhotoSurveyMap>(true))
                    {
                        if (overlay.mapConfig == null) continue;
                        Assert.That(overlay.locationIcons.Length, Is.EqualTo(overlay.mapConfig.locations.Length));
                        Assert.That(overlay.completionIcons.Length, Is.EqualTo(overlay.mapConfig.locations.Length));
                        foreach (var image in overlay.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))
                            Assert.That(Array.Exists(overlay.locationIcons, value => value == image) || Array.Exists(overlay.completionIcons, value => value == image),
                                Is.True, "Untracked legacy marker: " + image.name);
                    }
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
        }

        [Test] public void MigrationKeepsRetainedPoiEvidenceAndRejectsPhotoFromDeletedPoiWithReusedId()
        {
            var snapshot = new ExpeditionSnapshot();
            var zone = new ExpeditionZoneState { zone = "Zone03" };
            zone.missionProgress.completedObjectives.AddRange(new[] { "Z3_L3_PHOTO", "Z3_L4_PHOTO" });
            snapshot.zones.Add(zone);
            var kept = new SavedPhoto { missionZoneId = "Zone03", missionPoiId = "zone03-l4", missionObjectiveId = "Z3_L4_PHOTO" };
            var removed = new SavedPhoto { missionZoneId = "Zone04", missionPoiId = "zone04-l4", missionObjectiveId = "Z4_L4_PHOTO" };
            snapshot.photos.AddRange(new[] { kept, removed });
            MissionLayoutMigration.Apply(snapshot);
            CollectionAssert.AreEqual(new[] { "Z3_L3_PHOTO" }, zone.missionProgress.completedObjectives);
            Assert.That(kept.missionObjectiveId, Is.EqualTo("Z3_L3_PHOTO"));
            Assert.That(kept.missionPoiId, Is.EqualTo("zone03-l4"));
            Assert.That(removed.missionObjectiveId, Is.Null);
            Assert.That(removed.legacyMissionObjectiveId, Is.EqualTo("Z4_L4_PHOTO"));
        }

        [Test] public void LegacyMigrationPreservesEarnedHullCapabilityAndRejectsInvalidBits()
        {
            var snapshot = new ExpeditionSnapshot();
            var zone = new ExpeditionZoneState { zone = "Zone01", zoneOneStoryProgress = 2 | 4 | 32 | 64 | 256 };
            snapshot.zones.Add(zone);
            MissionLayoutMigration.Apply(snapshot);
            Assert.That(zone.missionProgress.worldFlags, Does.Contain(MissionLayoutMigration.LegacyPressureHullFlag));
            Assert.That(zone.missionProgress.researchData, Does.Contain(ZoneOneStory.PressureData));
            Assert.That(zone.missionProgress.unlockedRecipes, Does.Contain(ZoneOneStory.PressureHullRecipe));
            Assert.That(zone.missionProgress.completedObjectives, Does.Not.Contain(ZoneOneStory.CaptureOneObjective));
            Assert.That(snapshot.inventory, Is.Empty);
            zone.zoneOneStoryProgress = 999;
            Assert.Throws<InvalidDataException>(() => MissionLayoutMigration.Apply(snapshot));
        }

        [Test] public void VersionFiveMigrationMapsOriginalPoisAcrossCurrentDayStartAndJournal()
        {
            string oldPath = ExpeditionSaveStore.PathOverride;
            string folder = Path.Combine(Application.temporaryCachePath, "MissionMigration-" + Guid.NewGuid().ToString("N"));
            try
            {
                ExpeditionSaveStore.PathOverride = Path.Combine(folder, "save.json");
                var save = new ExpeditionSave { version = 5 };
                save.current.ship = new ShipState { speed = 20, diveSpeed = 5, ascentSpeed = 5, maximumDepth = 750, energyCapacity = 100, energyPerSecond = 1, hullCapacity = 100, energy = 100, hull = 100 }; save.current.hasShipState = true;
                for (int i = 1; i <= 4; i++) save.current.zones.Add(new ExpeditionZoneState { zone = $"Zone{i:00}", deadline = 25, hasVoyage = true });
                save.current.zones[0].missionProgress.completedObjectives.AddRange(new[] { "Z1_L2_COLLECT", "Z1_L3_PHOTO", "Z1_GATE_INSTALL_PRESSURE_HULL" });
                save.current.zones[1].missionProgress.completedObjectives.AddRange(new[] { "Z2_L2_COLLECT", "Z2_L3_PHOTO", "Z2_L3_CAPTURE" });
                save.current.zones[2].missionProgress.completedObjectives.Add("Z3_L3_PHOTO");
                save.current.zones[3].missionProgress.completedObjectives.AddRange(new[] { "Z4_L4_PHOTO", "Z4_L5_COLLECT" });
                save.current.zones[3].missionProgress.revealedLocations.AddRange(new[] { "Z4_L4", "Z4_L5" });
                save.current.inventory.Add(new SavedCreature { id = "Z1_ITEM_EMMA_BLUEPRINT", name = "legacy" });
                save.dayStart = ExpeditionSaveStore.Copy(save.current); save.hasDayStart = true;
                save.journal.Add(new ExpeditionJournalEntry { day = 1, zone = "Zone01", checkpoint = ExpeditionSaveStore.Copy(save.current) });
                ExpeditionSaveStore.Write(save, true);
                Assert.That(ExpeditionSaveStore.TryRead(out var migrated, out string error), Is.True, error);
                foreach (var snapshot in new[] { migrated.current, migrated.dayStart, migrated.journal[0].checkpoint })
                {
                    var z1 = snapshot.zones[0].missionProgress;
                    CollectionAssert.AreEqual(new[] { "Z1_L2_PHOTO" }, z1.completedObjectives);
                    Assert.That(z1.worldFlags, Does.Contain("LEGACY_ROUTE_UNLOCKED_Zone01"));
                    Assert.That(z1.completedObjectives, Does.Not.Contain("Z1_L1_CAPTURE"));
                    CollectionAssert.AreEquivalent(new[] { "Z2_L2_PHOTO", "Z2_L2_CAPTURE" }, snapshot.zones[1].missionProgress.completedObjectives);
                    Assert.That(snapshot.zones[2].missionProgress.completedObjectives, Is.Empty);
                    Assert.That(snapshot.zones[3].missionProgress.completedObjectives, Is.Empty, "Deleted L4 photo cannot credit the new L4");
                    CollectionAssert.AreEqual(new[] { "Z4_L4" }, snapshot.zones[3].missionProgress.revealedLocations);
                    Assert.That(snapshot.inventory.Count, Is.EqualTo(1));
                    Assert.That(snapshot.zones[3].legacyMissionProgressJson, Does.Contain("Z4_L4_PHOTO"));
                }
                string json = JsonUtility.ToJson(migrated);
                ExpeditionSaveStore.Write(migrated, true);
                Assert.That(ExpeditionSaveStore.TryRead(out var reread, out error), Is.True, error);
                Assert.That(JsonUtility.ToJson(reread), Is.EqualTo(json), "v6 reload cannot remap reused IDs a second time");
            }
            finally { ExpeditionSaveStore.PathOverride = oldPath; if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }

        [TestCase("Zone01", "Z1_GATE_INSTALL_BIO_LAMP")]
        [TestCase("Zone02", "Z2_GATE_INSTALL_PRESSURE_HULL")]
        [TestCase("Zone03", "Z3_GATE_DESTROY_ROCK_BARRIER")]
        public void DurableLocalGateKeepsRouteBeforeExitAdapterPublishesWithoutInventingCapture(string zoneId, string gate)
        {
            var snapshot = new ExpeditionSnapshot(); var zone = new ExpeditionZoneState { zone = zoneId };
            zone.missionProgress.completedObjectives.Add(gate); snapshot.zones.Add(zone);
            Assert.That(zone.progress.exitUnlocked, Is.False);
            MissionLayoutMigration.Apply(snapshot);
            CollectionAssert.AreEqual(new[] { gate }, zone.missionProgress.completedObjectives);
            Assert.That(zone.missionProgress.worldFlags, Does.Contain("LEGACY_ROUTE_UNLOCKED_" + zoneId));
            Assert.That(snapshot.inventory, Is.Empty);
            var owner = new GameObject("earned route migration"); var config = ScriptableObject.CreateInstance<ZoneMissionConfig>();
            try
            {
                config.zoneId = zoneId; var runtime = owner.AddComponent<ZoneMissionRuntime>(); runtime.config = config;
                runtime.RestoreProgress(zone.missionProgress);
                Assert.That(runtime.MainObjectivesComplete, Is.False);
                Assert.That(runtime.CanExitZone, Is.True, "Travel entitlement is separate from new fieldwork credit.");
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(config); }
        }

        [Test] public void LocalLampAliasPreservesEntireQuantityNameAndUnrelatedLegacyCargo()
        {
            var snapshot = new ExpeditionSnapshot();
            snapshot.inventory.Add(new SavedCreature { id = "Z1_MATERIAL_LUMINESCENT_ALGAE", name = "Local algae sample", quantity = 7 });
            snapshot.inventory.Add(new SavedCreature { id = "Z3_ITEM_MECHANICAL_MODULE", name = "Legacy module", quantity = 3 });
            MissionLayoutMigration.Apply(snapshot);
            Assert.That(snapshot.inventory.Single(item => item.id == "Z1_Creature_01").quantity, Is.EqualTo(7));
            Assert.That(snapshot.inventory.Single(item => item.id == "Z1_Creature_01").name, Is.EqualTo("Local algae sample"));
            Assert.That(snapshot.inventory.Single(item => item.id == "Z3_ITEM_MECHANICAL_MODULE").quantity, Is.EqualTo(3));
            Assert.That(snapshot.inventory, Has.Count.EqualTo(2));
        }

        [TestCase(0)] [TestCase(-1)]
        public void LocalLampMergeCannotHideInvalidLegacyStackQuantity(int quantity)
        {
            var snapshot = new ExpeditionSnapshot();
            snapshot.inventory.Add(new SavedCreature { id = "Z1_Creature_01", quantity = 2 });
            snapshot.inventory.Add(new SavedCreature { id = "Z1_MATERIAL_LUMINESCENT_ALGAE", quantity = quantity });
            Assert.Throws<InvalidDataException>(() => MissionLayoutMigration.Apply(snapshot));
        }

        [Test] public void LocalLampMergeRejectsQuantityOverflowAndDuplicateSourceStacks()
        {
            var snapshot = new ExpeditionSnapshot();
            snapshot.inventory.Add(new SavedCreature { id = "Z1_Creature_01", quantity = int.MaxValue });
            snapshot.inventory.Add(new SavedCreature { id = "Z1_MATERIAL_LUMINESCENT_ALGAE", quantity = 1 });
            Assert.Throws<InvalidDataException>(() => MissionLayoutMigration.Apply(snapshot));
            snapshot.inventory.Clear();
            snapshot.inventory.Add(new SavedCreature { id = "Z1_MATERIAL_LUMINESCENT_ALGAE", quantity = 1 });
            snapshot.inventory.Add(new SavedCreature { id = "Z1_MATERIAL_LUMINESCENT_ALGAE", quantity = 2 });
            Assert.Throws<InvalidDataException>(() => MissionLayoutMigration.Apply(snapshot));
        }

        [Test] public void LocalVersionFiveReceiptsCargoAndPendingPhotosMigrateAcrossEveryCheckpointOnlyOnce()
        {
            string oldPath = ExpeditionSaveStore.PathOverride;
            string folder = Path.Combine(Application.temporaryCachePath, "LocalMissionMigration-" + Guid.NewGuid().ToString("N"));
            var image = new Texture2D(2, 2);
            try
            {
                ExpeditionSaveStore.PathOverride = Path.Combine(folder, "save.json");
                var save = new ExpeditionSave { version = 5 }; save.current.zone = "Zone02";
                save.current.ship = new ShipState { speed = 20, diveSpeed = 5, ascentSpeed = 5, maximumDepth = 750,
                    energyCapacity = 100, energyPerSecond = 1, hullCapacity = 100, energy = 100, hull = 100 };
                save.current.hasShipState = true;
                foreach (string id in new[] { "Zone01", "Zone02", "Zone03" })
                    save.current.zones.Add(new ExpeditionZoneState { zone = id, deadline = 25, hasVoyage = true });
                var one = save.current.zones[0]; var two = save.current.zones[1]; var three = save.current.zones[2];
                one.missionProgress.completedObjectives.AddRange(new[] { "Z1_L1_PHOTO", "Z1_L1_CAPTURE", "Z1_L3_PHOTO", "Z1_GATE_INSTALL_BIO_LAMP" });
                two.missionProgress.completedObjectives.AddRange(new[] { "Z2_L1_PHOTO", "Z2_L1_CAPTURE", "Z2_L3_PHOTO", "Z2_L3_CAPTURE", "Z2_GATE_INSTALL_PRESSURE_HULL" });
                three.missionProgress.completedObjectives.AddRange(new[] { "Z3_L1_PHOTO", "Z3_L1_CAPTURE", "Z3_L4_PHOTO", "Z3_GATE_CRAFT_ROCK_BREAKER", "Z3_GATE_INSTALL_ROCK_BREAKER", "Z3_GATE_DESTROY_ROCK_BARRIER" });
                one.zoneOneAudio.played.Add("knocks"); two.zoneTwoPresentation.played.Add("radar"); two.zoneTwoPresentation.firstCaptureDistance = 123;
                save.current.inventory.Add(new SavedCreature { id = "Z1_Creature_01", name = "Canonical sample", quantity = 2 });
                save.current.inventory.Add(new SavedCreature { id = "Z1_MATERIAL_LUMINESCENT_ALGAE", name = "Local sample", quantity = 3 });
                save.current.inventory.Add(new SavedCreature { id = "Z3_ITEM_MECHANICAL_MODULE", name = "Old module", quantity = 4 });
                string png = Convert.ToBase64String(image.EncodeToPNG()), time = DateTimeOffset.UtcNow.ToString("O");
                save.current.photos.Add(new SavedPhoto { id = "local-retained", png = png, time = time, missionZoneId = "Zone03",
                    missionPoiId = "zone03-l4", missionObjectiveId = "Z3_L4_PHOTO", missionTargetId = "Z3_Creature_03" });
                save.current.photos.Add(new SavedPhoto { id = "local-removed", png = png, time = time, missionZoneId = "Zone03",
                    missionPoiId = "zone03-l3", missionObjectiveId = "Z3_L3_PHOTO", missionTargetId = "Z3_Creature_02" });
                save.dayStart = ExpeditionSaveStore.Copy(save.current); save.hasDayStart = true;
                save.journal.Add(new ExpeditionJournalEntry { day = 1, zone = "Zone02", checkpoint = ExpeditionSaveStore.Copy(save.current) });
                ExpeditionSaveStore.Write(save, true);
                Assert.That(ExpeditionSaveStore.TryRead(out var migrated, out var error), Is.True, error);
                foreach (var snapshot in new[] { migrated.current, migrated.dayStart, migrated.journal[0].checkpoint })
                {
                    CollectionAssert.AreEquivalent(new[] { "Z1_L1_PHOTO", "Z1_L1_CAPTURE", "Z1_L2_PHOTO", "Z1_GATE_INSTALL_BIO_LAMP" }, snapshot.zones[0].missionProgress.completedObjectives);
                    CollectionAssert.AreEquivalent(new[] { "Z2_L1_PHOTO", "Z2_L1_CAPTURE", "Z2_L2_PHOTO", "Z2_L2_CAPTURE", "Z2_GATE_INSTALL_PRESSURE_HULL" }, snapshot.zones[1].missionProgress.completedObjectives);
                    Assert.That(snapshot.zones[2].missionProgress.completedObjectives, Does.Contain("Z3_L3_PHOTO").And.Not.Contain("Z3_L3_CAPTURE"));
                    foreach (var zone in snapshot.zones) Assert.That(zone.missionProgress.worldFlags, Does.Contain("LEGACY_ROUTE_UNLOCKED_" + zone.zone));
                    Assert.That(snapshot.zones[0].zoneOneAudio.Has("knocks"), Is.True);
                    Assert.That(snapshot.zones[1].zoneTwoPresentation.Has("radar"), Is.True);
                    Assert.That(snapshot.zones[1].zoneTwoPresentation.firstCaptureDistance, Is.EqualTo(123));
                    Assert.That(snapshot.inventory.Single(item => item.id == "Z1_Creature_01").quantity, Is.EqualTo(5));
                    Assert.That(snapshot.inventory.Single(item => item.id == "Z3_ITEM_MECHANICAL_MODULE").quantity, Is.EqualTo(4));
                    Assert.That(snapshot.inventory.Any(item => item.id == "Z1_MATERIAL_LUMINESCENT_ALGAE"), Is.False);
                    var kept = snapshot.photos.Single(photo => photo.id == "local-retained");
                    Assert.That(kept.missionObjectiveId, Is.EqualTo("Z3_L3_PHOTO")); Assert.That(kept.missionPoiId, Is.EqualTo("zone03-l4"));
                    Assert.That(kept.missionTargetId, Is.EqualTo("Z3_Creature_03")); Assert.That(kept.png, Is.EqualTo(png)); Assert.That(kept.mission, Is.False);
                    Assert.That(snapshot.photos.Single(photo => photo.id == "local-removed").missionObjectiveId, Is.Null);
                }
                string once = JsonUtility.ToJson(migrated); ExpeditionSaveStore.Write(migrated, true);
                Assert.That(ExpeditionSaveStore.TryRead(out var reloaded, out error), Is.True, error);
                Assert.That(reloaded.version, Is.EqualTo(6)); Assert.That(JsonUtility.ToJson(reloaded), Is.EqualTo(once));
            }
            finally
            {
                Object.DestroyImmediate(image); ExpeditionSaveStore.PathOverride = oldPath;
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }
    }
}

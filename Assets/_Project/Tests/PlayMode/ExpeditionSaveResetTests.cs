using System;
using System.IO;
using G10.Prototype.Computer;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class ExpeditionSaveResetTests
    {
        private string folder;
        private string preferencesKey;
        private string previousSavePath;

        [SetUp] public void Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ExpeditionReset-" + Guid.NewGuid().ToString("N"));
            previousSavePath = ExpeditionSaveStore.PathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            preferencesKey = "ExpeditionResetTest.Volume." + Guid.NewGuid().ToString("N");
        }

        [TearDown] public void Cleanup()
        {
            ExpeditionSaveStore.PathOverride = previousSavePath;
            PlayerPrefs.DeleteKey(preferencesKey);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [Test] public void ExplicitNewGameReplacesTimelineAndRecoveryWithoutTouchingPreferences()
        {
            PlayerPrefs.SetFloat(preferencesKey, .37f);
            var old = ProgressedSave();
            ExpeditionSaveStore.Write(old);
            old.current.day++;
            ExpeditionSaveStore.Write(old);
            var fresh = ExpeditionSaveStore.ResetGameProgress();
            AssertFresh(fresh);
            Assert.That(ExpeditionSaveStore.TryRead(out var loaded, out _), Is.True);
            AssertFresh(loaded);
            File.WriteAllText(ExpeditionSaveStore.SavePath, "interrupted new-game file");
            Assert.That(ExpeditionSaveStore.TryRead(out var recovered, out _), Is.True);
            AssertFresh(recovered);
            Assert.That(PlayerPrefs.GetFloat(preferencesKey), Is.EqualTo(.37f));
        }

        [Test] public void ProgressionAndEndingStateRoundTripWithStableMissionIds()
        {
            ExpeditionSaveStore.Write(ProgressedSave());
            Assert.That(ExpeditionSaveStore.TryRead(out var restored, out _), Is.True);
            Assert.That(restored.version, Is.EqualTo(ExpeditionSaveStore.CurrentVersion));
            Assert.That(restored.current.zone, Is.EqualTo("Zone04"));
            Assert.That(restored.current.endingReached, Is.EqualTo("HiddenEnding"));
            var zone = restored.current.zones[0];
            Assert.That(zone.position, Is.EqualTo(new Vector2(1700, 500)));
            Assert.That(zone.progress.mainObjectivesComplete, Is.True);
            Assert.That(zone.progress.exitUnlocked, Is.True);
            Assert.That(zone.progress.rockDestroyed, Is.True);
            Assert.That(zone.progress.hiddenRouteUnlocked, Is.True);
            Assert.That(zone.progress.hiddenRouteComplete, Is.True);
            Assert.That(zone.progress.endingChoice, Is.EqualTo("ContinueExploring"));
            Assert.That(zone.missionProgress.completedObjectives, Does.Contain("Z4_STABLE_OBJECTIVE"));
            Assert.That(zone.missionProgress.revealedLocations, Does.Contain("Z4_HIDDEN_01"));
        }

        [Test] public void VersionOneWithoutNewFieldsMigratesWithoutGrantingProgression()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(ExpeditionSaveStore.SavePath,
                "{\"version\":1,\"current\":{\"day\":1,\"zone\":\"Zone01\",\"zones\":[{\"zone\":\"Zone01\",\"deadline\":5}],\"inventory\":[],\"photos\":[]},\"journal\":[]}");
            Assert.That(ExpeditionSaveStore.TryRead(out var restored, out _), Is.True);
            Assert.That(restored.version, Is.EqualTo(ExpeditionSaveStore.CurrentVersion));
            var zone = restored.current.zones[0];
            Assert.That(zone.progress, Is.Not.Null);
            Assert.That(zone.progress.exitUnlocked, Is.False);
            Assert.That(zone.progress.hiddenRouteUnlocked, Is.False);
            Assert.That(zone.progress.rockDestroyed, Is.False);
            Assert.That(zone.missionProgress.IsEmpty, Is.True);
            Assert.That(zone.creatureSpawns, Is.Empty);
            Assert.That(restored.current.endingReached, Is.Null.Or.Empty);
        }

        private static ExpeditionSave ProgressedSave()
        {
            var save = new ExpeditionSave();
            save.current.day = 9;
            save.current.zone = "Zone04";
            save.current.endingReached = "HiddenEnding";
            save.current.inventory.Add(new SavedCreature { id = "old-item", name = "Old test item" });
            var zone = new ExpeditionZoneState
            {
                zone = "Zone04", deadline = 15, hasVoyage = true, position = new Vector2(1700, 500),
                progress = new ZoneProgressState
                {
                    mainObjectivesComplete = true, exitUnlocked = true, rockDestroyed = true,
                    hiddenRouteUnlocked = true, hiddenRouteComplete = true, endingChoice = "ContinueExploring"
                }
            };
            zone.missionProgress.completedObjectives.Add("Z4_STABLE_OBJECTIVE");
            zone.missionProgress.revealedLocations.Add("Z4_HIDDEN_01");
            save.current.zones.Add(zone);
            save.journal.Add(new ExpeditionJournalEntry
            { day = 9, zone = "Zone04", checkpoint = ExpeditionSaveStore.Copy(save.current) });
            return save;
        }

        private static void AssertFresh(ExpeditionSave save)
        {
            Assert.That(save, Is.Not.Null);
            Assert.That(save.current.zone, Is.EqualTo("Zone01"));
            Assert.That(save.current.day, Is.EqualTo(1));
            Assert.That(save.current.zones, Is.Empty);
            Assert.That(save.current.inventory, Is.Empty);
            Assert.That(save.current.photos, Is.Empty);
            Assert.That(save.current.photosTaken, Is.Zero);
            Assert.That(save.current.hasShipState, Is.False);
            Assert.That(save.current.endingReached, Is.Null.Or.Empty);
            Assert.That(save.journal, Is.Empty);
        }
    }
}

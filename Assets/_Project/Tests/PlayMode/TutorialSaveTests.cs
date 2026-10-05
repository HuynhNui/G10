using System;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class TutorialSaveTests
    {
        private string folder, oldPath;
        [SetUp] public void Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"TutorialSave-"+Guid.NewGuid().ToString("N"));
            oldPath=ExpeditionSaveStore.PathOverride;
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"timeline.json");
        }
        [TearDown] public void Cleanup()
        {
            ExpeditionSaveStore.PathOverride=oldPath;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        private static ExpeditionSave Read()
        {
            Assert.That(ExpeditionSaveStore.TryRead(out var save,out var error),Is.True,error);
            return save;
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void EmptyTimelineRemainsFresh(int version)
        {
            ExpeditionSaveStore.Write(new ExpeditionSave{version=version});
            var save=Read();Assert.That(save.version,Is.EqualTo(4));Assert.That(save.tutorial.completed,Is.False);
        }
        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void EveryIndependentLegacyProgressSignalSkipsTutorialWithoutLosingEvidence(int version)
        {
            Action<ExpeditionSave>[] evidence={
                s=>s.current.day=2, s=>s.current.zone="Zone02", s=>s.current.photosTaken=1,
                s=>s.current.inventory.Add(new SavedCreature{id="blueprint"}),
                s=>s.current.zones[0].missionProgress.completedObjectives.Add("Z1_L1_PHOTO"),
                s=>s.current.zones[0].zoneOneStoryProgress=1,
                s=>s.current.zones[0].progress.exitUnlocked=true,
                s=>s.current.zones[0].position+=Vector2.right*20,
                s=>s.current.zones[0].distance=20 };
            foreach(var change in evidence)
            {
                var save=new ExpeditionSave{version=version};
                save.current.zones.Add(new ExpeditionZoneState{zone="Zone01",deadline=5,hasVoyage=true,
                    mapCoordinateVersion=3,position=new Vector2(960,154),heading=90,depth=230});
                change(save);string before=JsonUtility.ToJson(save.current);
                ExpeditionSaveStore.Write(save);var migrated=Read();
                Assert.That(migrated.tutorial.completed,Is.True);
                Assert.That(JsonUtility.ToJson(migrated.current),Is.EqualTo(before));
            }
        }
        [TestCase(0,600,99.81481f)][TestCase(1,625,106.94444f)]
        [TestCase(2,720,119.77778f)][TestCase(3,960,154)]
        public void UntouchedLegacyVoyageIsNotProgress(int coordinateVersion,float x,float y)
        {
            var save=new ExpeditionSave{version=3};
            save.current.zones.Add(new ExpeditionZoneState{zone="Zone01",deadline=5,hasVoyage=true,
                mapCoordinateVersion=coordinateVersion,position=new Vector2(x,y),heading=90,depth=230});
            ExpeditionSaveStore.Write(save);Assert.That(Read().tutorial.completed,Is.False);
        }
        [Test] public void NewGamePreservesKnowledgeAndExplicitResetOnlyClearsTutorial()
        {
            var save=new ExpeditionSave();save.current.day=3;
            save.tutorial.completed=true;save.tutorial.completedSteps.Add("Radar");
            ExpeditionSaveStore.Write(save);
            var fresh=ExpeditionSaveStore.ResetGameProgress();
            Assert.That(fresh.current.day,Is.EqualTo(1));Assert.That(fresh.tutorial.completed,Is.True);
            Assert.That(fresh.tutorial.completedSteps,Does.Contain("Radar"));
            fresh.current.day=2;ExpeditionSaveStore.Write(fresh);string timeline=JsonUtility.ToJson(fresh.current);
            ExpeditionSaveStore.ResetTutorialProgress();var reset=Read();
            Assert.That(reset.tutorial.completed,Is.False);Assert.That(reset.tutorial.completedSteps,Is.Empty);
            Assert.That(JsonUtility.ToJson(reset.current),Is.EqualTo(timeline));
        }
        [Test] public void MissingStateAndDuplicateIdsAreSafeAndUnknownKnowledgeIsPreserved()
        {
            var save=new ExpeditionSave{tutorial=null};ExpeditionSaveStore.Write(save);
            Assert.That(Read().tutorial.completed,Is.False);
            save.tutorial=new TutorialProgressState();save.tutorial.completedSteps.AddRange(new[]{"Radar","Radar","future-id",""});
            ExpeditionSaveStore.Write(save);var result=Read().tutorial;
            Assert.That(result.completedSteps,Is.EqualTo(new[]{"Radar","future-id"}));
        }
        [Test] public void ConfigRejectsInvalidThresholdsWithoutRuntimeState()
        {
            var config=ScriptableObject.CreateInstance<TutorialConfig>();
            try
            {
                config.movementDistance=float.NaN;config.headingDelta=1000;config.depthDelta=-5;
                config.pollInterval=0;config.retryDelay=float.PositiveInfinity;
                Assert.That(config.MovementThreshold,Is.EqualTo(20));Assert.That(config.HeadingThreshold,Is.EqualTo(180));
                Assert.That(config.DepthThreshold,Is.GreaterThan(0));Assert.That(config.PollInterval,Is.GreaterThan(0));
                Assert.That(config.RetryDelay,Is.EqualTo(3));Assert.That(config.IsConfigured,Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(config);}
        }
    }
}

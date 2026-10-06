using System;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class ExpeditionEconomyTests
    {
        private string folder, previousPath;
        [SetUp] public void Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "EconomyEdit-" + Guid.NewGuid().ToString("N"));
            previousPath = ExpeditionSaveStore.PathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
        }
        [TearDown] public void Cleanup()
        {
            ExpeditionSaveStore.PathOverride = previousPath;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        private static ShipState BaseShip()
        {
            var ship = new ShipState { speed=18, diveSpeed=5, ascentSpeed=5, maximumDepth=500,
                hullCapacity=100, energyCapacity=100, energyPerSecond=1, radarCapacity=10, photoCapacity=20, captureCapacity=5 };
            ship.Refill(); return ship;
        }
        [TestCase(ShipUpgrade.Hull, 1, 120)] [TestCase(ShipUpgrade.Hull, 2, 140)]
        [TestCase(ShipUpgrade.Speed, 1, 19.8f)] [TestCase(ShipUpgrade.Speed, 2, 21.6f)]
        [TestCase(ShipUpgrade.Energy, 1, .9f)] [TestCase(ShipUpgrade.Energy, 2, .8f)]
        public void ExactTargetsAreIdempotentAndDoNotBuffUnrelatedStats(ShipUpgrade branch, int level, float expected)
        {
            var ship = BaseShip();
            Assert.That(RegularShipUpgradeRules.TryApply(ship, branch, level, 18), Is.True);
            Assert.That(RegularShipUpgradeRules.TryApply(ship, branch, level, 18), Is.True);
            float value = branch == ShipUpgrade.Hull ? ship.hullCapacity : branch == ShipUpgrade.Speed ? ship.speed : ship.energyPerSecond;
            Assert.That(value, Is.EqualTo(expected).Within(.0001));
            Assert.That(ship.energyCapacity, Is.EqualTo(100)); Assert.That(ship.hull, Is.EqualTo(100));
            Assert.That(ship.diveSpeed, Is.EqualTo(5)); Assert.That(ship.ascentSpeed, Is.EqualTo(5));
            Assert.That(ship.radarCapacity, Is.EqualTo(10)); Assert.That(ship.captureCapacity, Is.EqualTo(5));
        }
        [Test] public void RecipesHaveExactTotalQuantitiesAndOnlyThreeBranches()
        {
            int tierOne=0, tierTwo=0, branches=0;
            foreach (ShipUpgrade branch in Enum.GetValues(typeof(ShipUpgrade)))
            {
                if (!RegularShipUpgradeRules.IsRegular(branch)) { Assert.That(RegularShipUpgradeRules.Cost(branch,0), Is.Zero); continue; }
                branches++; tierOne += RegularShipUpgradeRules.Cost(branch,0); tierTwo += RegularShipUpgradeRules.Cost(branch,1);
                Assert.That(RegularShipUpgradeRules.Cost(branch,2), Is.Zero);
            }
            Assert.That(branches, Is.EqualTo(3)); Assert.That(tierOne, Is.EqualTo(5)); Assert.That(tierTwo, Is.EqualTo(6));
        }
        [Test] public void StackCapacityAndAggregatedConsumptionAreAtomic()
        {
            var go = new GameObject("InventoryTest"); var inventory = go.AddComponent<CreatureInventory>();
            var material = ScriptableObject.CreateInstance<UpgradeMaterialDefinition>();
            JsonUtility.FromJsonOverwrite("{\"materialId\":\"Z2_Creature_02\"}", material);
            try
            {
                for(int i=0;i<3;i++) Assert.That(inventory.TryAdd(material.MaterialId,"Creature",null), Is.True);
                for(int i=1;i<CreatureInventory.Capacity;i++) inventory.TryAdd("other-"+i,"Other",null);
                Assert.That(inventory.IsFull, Is.True); Assert.That(inventory.TryAdd(material.MaterialId,"Creature",null), Is.True);
                Assert.That(inventory.GetCount(material.MaterialId), Is.EqualTo(4));
                Assert.That(inventory.TryAdd("new-type","New",null), Is.False);
                var requirements = new[] { new UpgradeMaterialRequirement(material,3),new UpgradeMaterialRequirement(material,2) };
                Assert.That(inventory.TryConsume(requirements), Is.False); Assert.That(inventory.GetCount(material.MaterialId), Is.EqualTo(4));
                Assert.That(inventory.TryConsume(new Dictionary<string,int> { [material.MaterialId]=3,["missing"]=1 }), Is.False);
                Assert.That(inventory.GetCount(material.MaterialId), Is.EqualTo(4));
                requirements[1]=new UpgradeMaterialRequirement(material,1);
                Assert.That(inventory.TryConsume(requirements), Is.True); Assert.That(inventory.Contains(material.MaterialId), Is.False);
                Assert.That(inventory.Items.Count, Is.EqualTo(11));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(material); }
        }
        [Test] public void VersionFourMissingQuantitiesAndLevelsMigrateEverySnapshot()
        {
            var legacy = new ExpeditionSave { version=4 };
            legacy.current.ship=BaseShip();legacy.current.hasShipState=true;
            legacy.current.zones.Add(new ExpeditionZoneState {zone="Zone01",deadline=15,hasVoyage=true});
            legacy.current.inventory.Add(new SavedCreature {id="Z2_Creature_02",name="Creature"});
            legacy.dayStart=ExpeditionSaveStore.Copy(legacy.current);legacy.hasDayStart=true;
            legacy.journal.Add(new ExpeditionJournalEntry {day=1,zone="Zone01",checkpoint=ExpeditionSaveStore.Copy(legacy.current)});
            Directory.CreateDirectory(folder);
            string json=JsonUtility.ToJson(legacy).Replace(",\"quantity\":1", "");
            json=System.Text.RegularExpressions.Regex.Replace(json, ",\"upgrades\":\\{[^}]*\\}", "");
            File.WriteAllText(ExpeditionSaveStore.SavePath,json);
            Assert.That(ExpeditionSaveStore.TryRead(out var loaded,out var error), Is.True,error);
            Assert.That(loaded.version, Is.EqualTo(5));
            Assert.That(loaded.current.inventory[0].quantity, Is.EqualTo(1));
            Assert.That(loaded.journal[0].checkpoint.inventory[0].quantity, Is.EqualTo(1));
            Assert.That(loaded.dayStart.inventory[0].quantity, Is.EqualTo(1));
            Assert.That(loaded.current.upgrades.Level(ShipUpgrade.Hull), Is.Zero);
            Assert.That(loaded.current.upgrades.Level(ShipUpgrade.Speed), Is.Zero);
            Assert.That(loaded.current.upgrades.Level(ShipUpgrade.Energy), Is.Zero);
            Assert.That(ExpeditionSaveStore.ResetGameProgress().current.upgrades.hullLevel, Is.Zero);
        }
        [TestCase(0,0)] [TestCase(-1,0)] [TestCase(1,3)]
        public void VersionFiveRejectsInvalidQuantityOrLevel(int quantity,int level)
        {
            var save=new ExpeditionSave(); save.current.inventory.Add(new SavedCreature {id="Z2_Creature_02",quantity=quantity});
            save.current.upgrades.hullLevel=level; ExpeditionSaveStore.Write(save);
            Assert.That(ExpeditionSaveStore.TryRead(out _,out _), Is.False);
        }
    }
}

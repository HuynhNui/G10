using System.Collections.Generic;
using System.Reflection;
using G10.Prototype.Missions;
using NUnit.Framework;

namespace G10.Prototype.Tests
{
    public sealed class MissionProgressStateTests
    {
        private static FieldInfo[] Lists => typeof(MissionProgressState).GetFields(BindingFlags.Public | BindingFlags.Instance);

        [Test] public void CopyDetachesEveryStateListWithoutLosingUnknownIds()
        {
            var source = new MissionProgressState();
            foreach (var field in Lists) ((List<string>)field.GetValue(source)).Add("legacy-" + field.Name);
            var copy = source.Copy();
            foreach (var field in Lists)
            {
                var original = (List<string>)field.GetValue(source);
                var detached = (List<string>)field.GetValue(copy);
                Assert.That(detached, Is.Not.SameAs(original));
                CollectionAssert.AreEqual(original, detached);
                detached.Clear(); detached.Add("future-" + field.Name);
                Assert.That(original, Is.EqualTo(new[] { "legacy-" + field.Name }));
            }
        }

        [Test] public void NormalizeIsIdempotentAndDeduplicatesEachReceiptAndObjectiveList()
        {
            var state = new MissionProgressState();
            foreach (var field in Lists) field.SetValue(state, new List<string> { null, "", " ", "stable", "stable", "Stable", "unknown-old-id" });
            for (int repeat = 0; repeat < 3; repeat++)
            {
                state.Normalize();
                foreach (var field in Lists)
                    CollectionAssert.AreEqual(new[] { "stable", "Stable", "unknown-old-id" }, (List<string>)field.GetValue(state));
            }
            Assert.That(state.IsEmpty, Is.False);
        }

        [Test] public void MissingLegacyListsNormalizeToEmptyWithoutManufacturingProgress()
        {
            var state = new MissionProgressState();
            foreach (var field in Lists) field.SetValue(state, null);
            state.Normalize(); Assert.That(state.IsEmpty, Is.True);
            var copy = state.Copy();
            foreach (var field in Lists) Assert.That((List<string>)field.GetValue(copy), Is.Empty);
        }
    }
}

using System;
using System.Collections.Generic;

namespace G10.Prototype.Missions
{
    /// <summary>Serializable mission state. Every entry is a stable authored ID, never an array index.</summary>
    [Serializable]
    public sealed class MissionProgressState
    {
        public List<string> completedObjectives = new();
        public List<string> grantedRewards = new();
        public List<string> researchData = new();
        public List<string> collectedItems = new();
        public List<string> unlockedRecipes = new();
        public List<string> worldFlags = new();
        public List<string> revealedLocations = new();
        public List<string> unlockedZones = new();
        public List<string> endings = new();

        public bool IsEmpty => completedObjectives.Count == 0 && grantedRewards.Count == 0 &&
            researchData.Count == 0 && collectedItems.Count == 0 && unlockedRecipes.Count == 0 &&
            worldFlags.Count == 0 && revealedLocations.Count == 0 && unlockedZones.Count == 0 && endings.Count == 0;

        public void Normalize()
        {
            completedObjectives ??= new List<string>();
            grantedRewards ??= new List<string>();
            researchData ??= new List<string>();
            collectedItems ??= new List<string>();
            unlockedRecipes ??= new List<string>();
            worldFlags ??= new List<string>();
            revealedLocations ??= new List<string>();
            unlockedZones ??= new List<string>();
            endings ??= new List<string>();
            RemoveInvalidAndDuplicates(completedObjectives);
            RemoveInvalidAndDuplicates(grantedRewards);
            RemoveInvalidAndDuplicates(researchData);
            RemoveInvalidAndDuplicates(collectedItems);
            RemoveInvalidAndDuplicates(unlockedRecipes);
            RemoveInvalidAndDuplicates(worldFlags);
            RemoveInvalidAndDuplicates(revealedLocations);
            RemoveInvalidAndDuplicates(unlockedZones);
            RemoveInvalidAndDuplicates(endings);
        }

        public MissionProgressState Copy()
        {
            Normalize();
            return new MissionProgressState
            {
                completedObjectives = new List<string>(completedObjectives),
                grantedRewards = new List<string>(grantedRewards),
                researchData = new List<string>(researchData),
                collectedItems = new List<string>(collectedItems),
                unlockedRecipes = new List<string>(unlockedRecipes),
                worldFlags = new List<string>(worldFlags),
                revealedLocations = new List<string>(revealedLocations),
                unlockedZones = new List<string>(unlockedZones),
                endings = new List<string>(endings)
            };
        }

        private static void RemoveInvalidAndDuplicates(List<string> values)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            values.RemoveAll(value => string.IsNullOrWhiteSpace(value) || !seen.Add(value));
        }
    }
}

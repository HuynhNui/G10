using System;
using System.Collections.Generic;
using G10.Prototype.Computer;
using UnityEngine;

namespace G10.Prototype.Tutorial
{
    [Serializable]
    public sealed class TutorialProgressState
    {
        public int version = 1;
        public bool completed;
        // Menu QA reset clears knowledge without injecting onboarding into the current voyage.
        public bool waitForNewGame;
        public List<string> completedSteps = new();
        public List<string> presentedSteps = new();
        // Additive save fields: older saves start at zero; milestones remain latched.
        public float helmDistance, helmTurn, helmDepth;
        public bool Has(TutorialStepId step) => completed || completedSteps.Contains(step.ToString());
        public bool Presented(TutorialStepId step) => presentedSteps.Contains(step.ToString());
        public void Normalize()
        {
            completedSteps ??= new(); presentedSteps ??= new();
            completedSteps = Clean(completedSteps); presentedSteps = Clean(presentedSteps);
            helmDistance = ValidProgress(helmDistance); helmTurn = ValidProgress(helmTurn); helmDepth = ValidProgress(helmDepth);
            if (completedSteps.Contains(nameof(TutorialStepId.Complete))) completed = true;
        }
        private static float ValidProgress(float value) => float.IsFinite(value) ? Mathf.Max(0, value) : 0;
        private static List<string> Clean(List<string> source)
        {
            var result = new List<string>();
            foreach (var id in source) if (!string.IsNullOrWhiteSpace(id) && !result.Contains(id)) result.Add(id);
            return result;
        }
        public static bool LegacyHasProgress(ExpeditionSave save)
        {
            var current = save.current;
            if (current.zone != "Zone01" || current.day > 1 || current.photosTaken > 0 || current.photos.Count > 0 ||
                current.inventory.Count > 0 || !string.IsNullOrEmpty(current.endingReached) || save.journal.Count > 0) return true;
            foreach (var zone in current.zones)
            {
                if (zone.zone != "Zone01" || zone.zoneOneStoryProgress != 0 || !zone.missionProgress.IsEmpty ||
                    zone.tasks.Length > 0 || zone.completedDay > 0 || zone.progress.mainObjectivesComplete ||
                    zone.progress.exitUnlocked || zone.progress.rockDestroyed || zone.progress.hiddenRouteUnlocked ||
                    zone.progress.hiddenRouteComplete || !string.IsNullOrEmpty(zone.progress.endingChoice)) return true;
                // The pre-v4 authored entry, expressed in each historical coordinate version.
                // This is migration evidence only; fresh runtime spawns always come from ActiveMap.
                Vector2 oldEntry = zone.mapCoordinateVersion switch {
                    0 => new Vector2(600, 99.81481f), 1 => new Vector2(625, 106.94444f),
                    2 => new Vector2(720, 119.77778f), _ => new Vector2(960,154) };
                if (zone.hasVoyage && (zone.distance > 1 || Vector2.Distance(zone.position, oldEntry) > 1 ||
                    Mathf.Abs(Mathf.DeltaAngle(zone.heading,90)) > 1 || Mathf.Abs(zone.depth-230) > 1)) return true;
            }
            return false;
        }
    }
}

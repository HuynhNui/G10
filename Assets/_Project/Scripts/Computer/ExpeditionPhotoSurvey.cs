using System;
using System.Collections.Generic;
using G10.Prototype.Missions;
using G10.Prototype.UI;

namespace G10.Prototype.Computer
{
    /// <summary>Derived photograph receipts, independent from stored images and capture/module objectives.</summary>
    public readonly struct PhotoSurveyCounts
    {
        public int MandatoryCompleted { get; }
        public int MandatoryTotal { get; }
        public int HiddenCompleted { get; }
        public int HiddenTotal { get; }
        public int Completed => MandatoryCompleted + HiddenCompleted;
        public int Total => MandatoryTotal + HiddenTotal;
        // These describe photographs only; mission/capture/route authorities remain independent.
        public bool AllMandatoryPhotosComplete => MandatoryTotal > 0 && MandatoryCompleted == MandatoryTotal;
        public bool AllPhotosComplete => Total > 0 && Completed == Total;

        internal PhotoSurveyCounts(int mandatoryCompleted, int mandatoryTotal, int hiddenCompleted, int hiddenTotal)
        {
            MandatoryCompleted = mandatoryCompleted; MandatoryTotal = mandatoryTotal;
            HiddenCompleted = hiddenCompleted; HiddenTotal = hiddenTotal;
        }
    }

    public static class ExpeditionPhotoSurvey
    {
        /// <summary>The active runtime supersedes its saved snapshot; other zones use the current timeline.</summary>
        public static PhotoSurveyCounts Read(ExpeditionLoop loop, CabinZoneSession session)
        {
            if (loop == null || session == null) return default;
            var configs = new List<ZoneMissionConfig>(4);
            var saved = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            for (int index = 1; index <= 4; index++)
            {
                string zone = "Zone0" + index;
                var config = session.ConfigFor(zone)?.missionConfig;
                if (config == null || config.zoneId != zone) continue;
                configs.Add(config);
                if (!loop.IsInitialized || zone == loop.Zone) continue;
                var receipts = loop.ReadZoneState(zone)?.missionProgress?.completedObjectives;
                if (receipts != null) saved.Add(zone, new HashSet<string>(receipts, StringComparer.Ordinal));
            }
            return Count(configs, (zone, objective) =>
            {
                if (!loop.IsInitialized) return false;
                if (zone == loop.Zone)
                    return loop.MissionRuntime?.config?.zoneId == zone && loop.MissionRuntime.HasObjective(objective);
                return saved.TryGetValue(zone, out var receipts) && receipts.Contains(objective);
            });
        }

        /// <summary>Count each authored required PHOTO once, keyed by its zone and stable objective ID.</summary>
        public static PhotoSurveyCounts Count(IEnumerable<ZoneMissionConfig> configs, Func<string, string, bool> hasObjective)
        {
            if (configs == null) return default;
            int mandatory = 0, mandatoryTotal = 0, hidden = 0, hiddenTotal = 0;
            var seen = new HashSet<(string zone, string objective)>();
            foreach (var config in configs)
            {
                if (config == null || string.IsNullOrEmpty(config.zoneId)) continue;
                foreach (var location in config.locations ?? Array.Empty<MissionLocationConfig>())
                {
                    if (location == null || location.visibility is not (LocationVisibility.Visible or LocationVisibility.HiddenRadar)) continue;
                    foreach (var objective in location.objectives ?? Array.Empty<MissionObjectiveConfig>())
                    {
                        if (objective == null || !objective.required || objective.type != MissionObjectiveType.Photograph ||
                            string.IsNullOrEmpty(objective.id) || !seen.Add((config.zoneId, objective.id))) continue;
                        bool completed = hasObjective?.Invoke(config.zoneId, objective.id) == true;
                        if (location.visibility == LocationVisibility.HiddenRadar)
                        { hiddenTotal++; if (completed) hidden++; }
                        else { mandatoryTotal++; if (completed) mandatory++; }
                    }
                }
            }
            return new PhotoSurveyCounts(mandatory, mandatoryTotal, hidden, hiddenTotal);
        }
    }
}

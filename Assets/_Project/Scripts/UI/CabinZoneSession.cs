using System;
using G10.Prototype.Computer;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using UnityEngine;

namespace G10.Prototype.UI
{
    /// <summary>Rebinds the existing cabin and its four authored map panels; no placeholder scene loads.</summary>
    [DisallowMultipleComponent]
    public sealed class CabinZoneSession : MonoBehaviour
    {
        private CabinStationView cabin;
        private WorldMapController world;
        private ZoneMapPresentation activePresentation;
        private MissionDefinition runtimeMission;
        public ZoneMapConfig ActiveConfig { get; private set; }
        public G10.Prototype.Feedback.RockBreakerSequence RockEffect { get; private set; }
        /// <summary>Do not publish v6 saves against stale serialized layouts after a partial source update.</summary>
        public bool ValidateMissionLayout(out string error)
        {
            string[][] pois = { new[] { "zone01-left", "zone01-north" }, new[] { "zone02-l1", "zone02-l3" },
                new[] { "zone03-l1", "zone03-l2", "zone03-l4" }, new[] { "zone04-l1", "zone04-l2", "zone04-l3", "zone04-l5" } };
            for (int index = 0; index < pois.Length; index++)
            {
                string zone = "Zone0" + (index + 1); var map = ConfigFor(zone); var locations = map?.missionConfig?.locations;
                if (locations == null || map.locations == null || locations.Length != pois[index].Length || map.locations.Length != pois[index].Length)
                { error = zone + ": mission layout assets require synchronization."; return false; }
                for (int site = 0; site < locations.Length; site++)
                {
                    var location = locations[site]; string id = "Z" + (index + 1) + "_L" + (site + 1);
                    if (location?.id != id || location.poiId != pois[index][site] || map.locations[site]?.id != pois[index][site])
                    { error = zone + ": stale mission/POI identity at " + id; return false; }
                }
            }
            var first = ConfigFor("Zone01").missionConfig.FindLocation("Z1_L1");
            var third = ConfigFor("Zone03").missionConfig.FindLocation("Z3_L3");
            if (!Array.Exists(first.objectives ?? Array.Empty<MissionObjectiveConfig>(), o => o?.id == "Z1_L1_CAPTURE" && o.targetId == "Z1_Creature_01") ||
                !Array.Exists(third.objectives ?? Array.Empty<MissionObjectiveConfig>(), o => o?.id == "Z3_L3_CAPTURE" && o.targetId == "Z3_Creature_03"))
            { error = "Mission capture targets require synchronization."; return false; }
            error = null; return true;
        }
        public ZoneMapConfig ConfigFor(string zoneId)
        {
            world ??= GetComponent<WorldMapController>();
            foreach (var panel in world?.zoneMaps ?? Array.Empty<GameObject>())
            {
                var config = panel != null ? panel.GetComponent<ZoneMapPresentation>()?.config : null;
                if (config != null && config.zoneId == zoneId) return config;
            }
            return null;
        }

        public bool Configure(string zoneId)
        {
            cabin ??= GetComponent<CabinStationView>();
            RockEffect ??= GetComponent<G10.Prototype.Feedback.RockBreakerSequence>();
            world ??= GetComponent<WorldMapController>();
            if (cabin == null || world?.zoneMaps == null) return false;
            int index = Array.FindIndex(world.zoneMaps, panel => panel != null &&
                panel.GetComponent<ZoneMapPresentation>()?.config?.zoneId == zoneId);
            if (index < 0) return false;
            var presentation = world.zoneMaps[index].GetComponent<ZoneMapPresentation>();
            var survey = GetComponent<PhotoSurveyZone>();
            var story = GetComponent<ZoneOneStory>();
            if (survey == null || story == null || presentation.config.missionConfig == null) return false;

            cabin.Brake();
            cabin.CancelDirectInteraction();
            GetComponent<CaptureMinigameController>()?.Cancel();
            FindAnyObjectByType<G10.Prototype.Dialogue.DialogueController>()?.Cancel();
            cabin.Panels?.CloseCurrentPanel();
            foreach (var panel in world.zoneMaps)
            {
                if (panel == null) continue;
                var view = panel.GetComponent<ZoneMapPresentation>();
                if (view == null) continue;
                view.navigation = null;
                view.MissionRuntime = null;
                view.Timeline = GetComponent<ExpeditionLoop>() ?? FindAnyObjectByType<ExpeditionLoop>();
                if (view.overlay != null)
                {
                    view.overlay.survey = null;
                    view.overlay.navigation = null;
                    view.overlay.missionRuntime = null;
                    view.overlay.SetPointer(null);
                }
                panel.SetActive(false);
            }
            ActiveConfig = presentation.config;
            var minigame = GetComponent<CaptureMinigameController>();
            if (minigame != null && ActiveConfig.captureMinigameProfile != null)
                minigame.profile = ActiveConfig.captureMinigameProfile;
            cabin.Navigation.ConfigureDepthRange(ActiveConfig.minimumDepth);
            activePresentation = presentation;
            story.config = ActiveConfig.missionConfig;
            story.survey = survey;
            story.navigation = cabin.Navigation;
            story.RockInteractionArea = ActiveConfig.rockInteractionArea;
            var loop = cabin.Panels != null ? cabin.Panels.Expedition : FindAnyObjectByType<ExpeditionLoop>();
            if (loop != null && loop.contentCatalog.Length > 0) story.contentCatalog = loop.contentCatalog;
            story.poiIds = Array.ConvertAll(ActiveConfig.locations, poi => poi.id);
            story.Restore(0);
            story.HiddenRouteAvailable = false;
            survey.Story = story;
            survey.MissionRuntime = story;
            survey.locations = ActiveConfig.locations;
            survey.targetDepth = ActiveConfig.entryDepth;
            survey.creatureId = ActiveConfig.locations.Length > 0
                ? story.TargetIdForPoi(ActiveConfig.locations[0].id) ?? zoneId : zoneId;
            survey.ResetContacts();
            survey.RestoreProgress(Array.Empty<PhotoSurveyZone.TaskKind>(), true);
            if (runtimeMission == null)
            {
                runtimeMission = survey.mission != null ? Instantiate(survey.mission) : ScriptableObject.CreateInstance<MissionDefinition>();
                runtimeMission.hideFlags = HideFlags.DontSave;
            }
            runtimeMission.zoneSceneName = zoneId;
            runtimeMission.zoneDisplayName = ActiveConfig.missionConfig.displayName;
            runtimeMission.targetPoiId = ActiveConfig.locations.Length > 0 ? ActiveConfig.locations[0].id : null;
            survey.mission = runtimeMission;
            cabin.SetActiveMap(world.zoneMaps[index]);
            world.RememberActiveZone(index);
            presentation.navigation = cabin.Navigation;
            presentation.MissionRuntime = story;
            presentation.RockEffect = RockEffect;
            if (presentation.overlay != null)
            {
                presentation.overlay.survey = survey;
                presentation.overlay.navigation = cabin.Navigation;
                presentation.overlay.missionRuntime = story;
            }
            ActiveConfig.ApplyTerrain(cabin.Navigation, false);
            cabin.Radar?.ResetScanState();
            foreach (var camera in GetComponentsInChildren<PhotoCameraView>(true)) camera.ResetZoneView();
            var progression = GetComponent<ExpeditionProgression>() ?? gameObject.AddComponent<ExpeditionProgression>();
            progression.Initialize(loop, this, cabin);
            return true;
        }

        public void RefreshHistoricalMaps()
        {
            foreach (var panel in world?.zoneMaps ?? Array.Empty<GameObject>())
                panel?.GetComponent<ZoneMapPresentation>()?.RefreshSavedProgress();
        }

        public void RefreshTerrain()
        {
            if (ActiveConfig == null) return;
            activePresentation.RefreshMap();
        }

        private void OnDestroy() { if (runtimeMission != null) Destroy(runtimeMission); }
    }
}

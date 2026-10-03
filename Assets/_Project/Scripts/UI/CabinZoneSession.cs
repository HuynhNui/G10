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

        public bool Configure(string zoneId)
        {
            cabin ??= GetComponent<CabinStationView>();
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

        public void RefreshTerrain()
        {
            if (ActiveConfig == null) return;
            activePresentation.RefreshMap();
        }

        private void OnDestroy() { if (runtimeMission != null) Destroy(runtimeMission); }
    }
}

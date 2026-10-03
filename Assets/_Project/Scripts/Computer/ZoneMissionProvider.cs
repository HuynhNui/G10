using UnityEngine;
using G10.Prototype.Navigation;
using G10.Prototype.UI;

namespace G10.Prototype.Computer
{
    public sealed class ZoneMissionProvider : MonoBehaviour, IMissionProvider
    {
        [SerializeField] private MissionDefinition mission;
        private PhotoSurveyZone survey;
        private PhotoSurveyZone Survey => survey != null ? survey :
            survey = GetComponentInParent<CabinStationView>(true)?.GetComponent<PhotoSurveyZone>();
        public MissionDefinition CurrentMission => Survey != null && Survey.MissionRuntime?.config != null ? Survey.mission :
            mission != null && mission.zoneSceneName == gameObject.scene.name ? mission : null;
        public string ZoneName => Survey?.MissionRuntime?.config != null ? Survey.MissionRuntime.config.displayName :
            CurrentMission != null ? CurrentMission.zoneDisplayName : gameObject.scene.name;
    }
}

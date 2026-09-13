using UnityEngine;

namespace G10.Prototype.Computer
{
    public sealed class ZoneMissionProvider : MonoBehaviour, IMissionProvider
    {
        [SerializeField] private MissionDefinition mission;
        public MissionDefinition CurrentMission => mission != null && mission.zoneSceneName == gameObject.scene.name ? mission : null;
        public string ZoneName => CurrentMission != null ? mission.zoneDisplayName : gameObject.scene.name;
    }
}

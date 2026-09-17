using System;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public enum MissionState { Locked, Active, Completed }
    [Serializable]
    public sealed class MissionStep
    {
        public string description;
        public MissionState state;
    }

    [CreateAssetMenu(menuName = "G10/Computer/Mission definition")]
    public sealed class MissionDefinition : ScriptableObject
    {
        public string zoneSceneName;
        public string zoneDisplayName;
        [TextArea] public string objective;
        public MissionState state;
        public bool isTemplate;
        [TextArea] public string integrationNote;
        [Tooltip("ID of a POI in the zone survey location list.")]
        public string targetPoiId;
        public MissionStep[] steps = Array.Empty<MissionStep>();
    }
}

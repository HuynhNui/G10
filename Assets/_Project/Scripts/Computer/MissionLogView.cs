using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    public sealed class MissionLogView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerSource;
        [SerializeField] private Text body;
        private IMissionProvider provider;
        public string DisplayedText => body.text;
        public ExpeditionLoop Expedition { get; set; }
        private void Awake() => provider = providerSource as IMissionProvider;
        private void OnEnable() => Refresh();
        public void Bind(IMissionProvider source) { provider = source; Refresh(); }
        public void Refresh()
        {
            if (Expedition != null) { body.text=Expedition.MissionText(); return; }
            if (provider == null) { body.text = "MISSION DATA OFFLINE"; return; }
            MissionDefinition mission = provider.CurrentMission;
            if (mission == null) { body.text = provider.ZoneName + "\n\nNO MISSION ASSIGNED"; return; }
            var text = new StringBuilder(provider.ZoneName).AppendLine().AppendLine();
            text.Append('[').Append(mission.state.ToString().ToUpperInvariant()).Append("] ").AppendLine(mission.objective);
            if (!string.IsNullOrEmpty(mission.targetPoiId))
                text.AppendLine($"TARGET POI: {mission.targetPoiId}");
            text.AppendLine();
            foreach (MissionStep step in mission.steps)
            {
                if (step == null) continue;
                text.Append(step.state == MissionState.Completed ? "[x] " : "[ ] ");
                text.Append(step.description).Append("  [").Append(step.state.ToString().ToUpperInvariant()).AppendLine("]");
            }
            if (mission.isTemplate) text.AppendLine().AppendLine("PREVIEW MISSION — PROGRESS NOT CONNECTED");
            if (!string.IsNullOrEmpty(mission.integrationNote)) text.AppendLine(mission.integrationNote);
            body.text = text.ToString();
        }
    }
}

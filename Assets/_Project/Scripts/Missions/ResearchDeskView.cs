using G10.Prototype.Computer;
using UnityEngine;

namespace G10.Prototype.Missions
{
    public sealed class ResearchDeskView : MonoBehaviour
    {
        public ZoneOneStory story;
        public UnityEngine.UI.Text body;
        public UnityEngine.UI.Button research, install;
        public ComputerScreenController screen;
        private void OnEnable() { if (story != null) story.Changed += Refresh; Refresh(); }
        private void OnDisable() { if (story != null) story.Changed -= Refresh; }
        public void Open() => screen.OpenApp(ComputerAppId.Research);
        public void Analyze() { if (story.Research()) screen.Expedition?.SaveCurrent(); Refresh(); }
        public void Install() { if (story.InstallHull()) screen.Expedition?.SaveCurrent(); Refresh(); }
        public void Refresh()
        {
            if (story == null || body == null) return;
            body.text = "BÀN NGHIÊN CỨU & CHẾ TẠO\n\n" + story.MissionText() + "\n\n" +
                (story.LastMessage ?? story.DiscoveryNotes);
            research.interactable = story.CanResearch;
            install.interactable = story.CanInstall;
        }
    }
}

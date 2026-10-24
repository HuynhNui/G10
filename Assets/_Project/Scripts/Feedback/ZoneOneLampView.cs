using G10.Prototype.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    /// <summary>External camera illumination only; never changes cabin lighting or saved photographs.</summary>
    public sealed class ZoneOneLampView : MonoBehaviour
    {
        public ZoneOneStory story;
        public ZoneOneAudioController audio;
        public Graphic darkness;
        public TMP_Text status;
        private void LateUpdate()
        {
            bool installed = story != null && story.BioLampInstalled;
            float level = installed ? audio != null ? audio.ExternalLightLevel : 1 : 0;
            bool applies = story != null && story.UsesBioLampStory;
            if (darkness != null) darkness.color = new Color(0, 0, 0, applies ? .28f * (1 - level) : 0);
            if (status != null) status.text = installed ? "EXTERNAL ILLUMINATION: ONLINE" : "EXTERNAL ILLUMINATION: OFFLINE";
        }
    }
}

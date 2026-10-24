using G10.Prototype.Computer;
using G10.Prototype.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    /// <summary>Passive install readout inside the existing Upgrade app; never opens, closes or locks a window.</summary>
    public sealed class ZoneOneBioLampView : MonoBehaviour
    {
        public ZoneOneAudioController audio;
        public ZoneOneStory story;
        public ComputerScreenController computer;
        public CanvasGroup card;
        public TMP_Text message;
        public Image fill;
        public bool IsVisible => card != null && card.alpha > 0;
        private void LateUpdate() => Render(audio != null ? audio.BioLampElapsed : -1);
        public void Render(float elapsed)
        {
            bool visible = story != null && story.UsesBioLampStory && story.BioLampInstalled && elapsed >= 0 && elapsed < 6 &&
                computer != null && computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.Upgrade;
            if (card == null) return;
            card.alpha = visible ? Mathf.Min(Mathf.Clamp01(elapsed / .15f), 1 - Mathf.InverseLerp(5, 6, elapsed)) : 0;
            if (!visible) return;
            message.text = elapsed < .6f ? "BIO LAMP\nLOCKING MODULE" : elapsed < 1.5f ? "BIO LAMP\nPOWERING MODULE" :
                elapsed < 2.2f ? "EXTERNAL ILLUMINATION\nRESTORING" : elapsed < 4 ? "EXTERNAL ILLUMINATION\nSTABILIZING" : "BIO LAMP INSTALLED\nONLINE";
            fill.fillAmount = Mathf.Clamp01(elapsed / 4);
            // A solid uGUI Image has no sprite mesh to crop; drive its anchored width directly.
            fill.rectTransform.anchorMax = new(fill.fillAmount, 1);
        }
        private void OnDisable() { if (card != null) card.alpha = 0; }
    }
}

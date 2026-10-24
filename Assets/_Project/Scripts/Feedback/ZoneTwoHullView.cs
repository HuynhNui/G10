using G10.Prototype.Computer;
using G10.Prototype.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace G10.Prototype.Feedback
{
    /// <summary>Passive installation diagram in the existing Upgrade application.</summary>
    public sealed class ZoneTwoHullView : MonoBehaviour
    {
        public ZoneTwoPresentationController presentation;
        public ZoneOneStory story;
        public ComputerScreenController computer;
        public CanvasGroup card;
        public TMP_Text message;
        public Image[] plates;
        public bool IsVisible => card != null && card.alpha > 0;
        private void LateUpdate() => Render(presentation != null ? presentation.HullInstallElapsed : -1);
        public void Render(float age)
        {
            bool visible = age >= 0 && age < 7 && story.UsesZoneTwoHullStory && story.ZoneTwoHullInstalled &&
                computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.Upgrade;
            card.alpha = visible ? 1 - Mathf.InverseLerp(6, 7, age) : 0;
            if (!visible) return;
            message.text = age < 1 ? "PRESSURE HULL INSTALLATION\nREINFORCEMENT PROCESS..." : age < 3 ?
                "PRESSURE HULL INSTALLATION\nMATERIALS INTEGRATED" : "INSTALLATION COMPLETE\n500 M → 750 M · SYSTEM STABLE";
            for (int i = 0; i < plates.Length; i++)
                plates[i].color = age >= .5f + i * .7f ? new Color(.30f, .78f, .66f) : new Color(.18f, .29f, .35f);
        }
        private void OnDisable() { if (card != null) card.alpha = 0; }
    }
}

using G10.Prototype.Navigation;
using G10.Prototype.Computer;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>Read-only meter; never intercepts helm or desktop input.</summary>
    public sealed class ShipEnergyBar : MonoBehaviour
    {
        private ZoneNavigation navigation;
        private RectTransform fill;
        private Image fillImage;
        private Text readout;
        private float refreshAt;
        private bool compact;

        /// <summary>Energy occupies the right half of the shared speed/energy card.</summary>
        public static ShipEnergyBar CreateInline(Transform card, ZoneNavigation owner, Text style)
        {
            var root = Rect(card, "ShipEnergyBar", new Vector2(32, 8), new Vector2(-40, -8));
            root.anchorMin = new Vector2(.5f, 0);
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(32, 8);
            root.offsetMax = new Vector2(-40, -8);
            var meter = root.gameObject.AddComponent<ShipEnergyBar>();
            meter.navigation = owner;
            meter.compact = true;

            var label = Rect(root, "EnergyReadout", Vector2.zero, Vector2.zero);
            label.anchorMax = new Vector2(.61f, 1);
            meter.readout = label.gameObject.AddComponent<Text>();
            meter.readout.font = style.font;
            meter.readout.fontSize = style.fontSize;
            meter.readout.alignment = TextAnchor.MiddleCenter;
            meter.readout.color = style.color;
            meter.readout.raycastTarget = false;
            meter.readout.resizeTextForBestFit = true;
            meter.readout.resizeTextMinSize = 20;
            meter.readout.resizeTextMaxSize = 26;

            var track = Rect(root, "Track", Vector2.zero, Vector2.zero);
            track.anchorMin = new Vector2(.64f, .31f);
            track.anchorMax = new Vector2(1, .69f);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(.16f, .23f, .53f, .3f);
            trackImage.raycastTarget = false;
            meter.fill = Rect(track, "Fill", Vector2.zero, Vector2.zero);
            meter.fillImage = meter.fill.gameObject.AddComponent<Image>();
            meter.fillImage.raycastTarget = false;
            meter.Refresh();
            return meter;
        }

        public static ShipEnergyBar Create(Transform parent, ZoneNavigation owner, Font font, Vector2 position, Vector2 size)
        {
            var root = Rect(parent, "ShipEnergyBar", Vector2.zero, Vector2.zero);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0, 0);
            root.anchoredPosition = position; root.sizeDelta = size;
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(.06f, .13f, .21f, .94f); background.raycastTarget = false;
            var meter = root.gameObject.AddComponent<ShipEnergyBar>(); meter.navigation = owner; meter.compact = size.x < 300;
            if (meter.compact) background.color = new Color(.22f, .32f, .58f, .74f);
            var track = Rect(root, "Track", new Vector2(12, 10), new Vector2(-12, -38));
            var trackImage = track.gameObject.AddComponent<Image>(); trackImage.color = new Color(.17f, .28f, .35f); trackImage.raycastTarget = false;
            meter.fill = Rect(track, "Fill", Vector2.zero, Vector2.zero);
            meter.fillImage = meter.fill.gameObject.AddComponent<Image>(); meter.fillImage.raycastTarget = false;
            var label = Rect(root, "EnergyReadout", new Vector2(12, 30), new Vector2(-12, -4));
            meter.readout = label.gameObject.AddComponent<Text>(); meter.readout.font = font;
            meter.readout.fontSize = 22; meter.readout.alignment = TextAnchor.MiddleLeft;
            meter.readout.color = new Color(.9f, .98f, 1); meter.readout.raycastTarget = false;
            meter.readout.horizontalOverflow = HorizontalWrapMode.Overflow;
            meter.readout.resizeTextForBestFit = true; meter.readout.resizeTextMinSize = 14; meter.readout.resizeTextMaxSize = 22;
            var skin = parent.GetComponentInParent<ComputerDesktopSkin>();
            if (skin != null && skin.font != null)
                label.gameObject.AddComponent<ComputerDesktopText>().Bind(meter.readout, skin.font, meter.readout.color);
            meter.Refresh(); return meter;
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = min; rect.offsetMax = max; return rect;
        }
        private void Update() { if (Time.unscaledTime >= refreshAt) Refresh(); }
        private void Refresh()
        {
            refreshAt = Time.unscaledTime + .1f;
            if (navigation == null || readout == null) return;
            var ship = navigation.Ship;
            float fraction = Mathf.Clamp01(ship.Energy / ship.EnergyCapacity);
            fill.anchorMax = new Vector2(fraction, 1);
            fillImage.color = fraction <= .2f ? new Color(1, .42f, .30f) : new Color(.35f, .85f, .76f);
            readout.text = compact ? $"ENERGY {ship.Energy:0}/{ship.EnergyCapacity:0}" :
                $"ENERGY  {ship.Energy:0.0} / {ship.EnergyCapacity:0.#}     •     {(navigation.IsMoving ? ship.EnergyPerSecond : 0):0.#}/s";
        }
    }
}

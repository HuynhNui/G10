using G10.Prototype.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    public sealed class CreatureInventoryView : MonoBehaviour
    {
        public CreatureInventory inventory;
        public RawImage[] icons;
        public Text[] labels;
        public Text summary;
        private void OnEnable() { if (inventory != null) { inventory.Changed += Refresh; Refresh(); } }
        private void OnDisable() { if (inventory != null) inventory.Changed -= Refresh; }
        private void Refresh()
        {
            summary.text = inventory.Items.Count == 0 ? "Balô trống" : $"Đã thu thập {inventory.Items.Count}/{CreatureInventory.Capacity} vật phẩm";
            for (int i = 0; i < icons.Length; i++)
            {
                bool occupied = i < inventory.Items.Count;
                icons[i].texture = occupied ? inventory.Items[i].Icon : null;
                icons[i].enabled = occupied;
                labels[i].text = occupied ? inventory.Items[i].Name + " ×1" : "Ô trống";
            }
        }
    }
}

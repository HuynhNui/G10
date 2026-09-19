using G10.Prototype.Audio;
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

        private void Start()
        {
            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null) continue;
                int slotIndex = i;
                var slotGo = icons[i].transform.parent != null ? icons[i].transform.parent.gameObject : icons[i].gameObject;
                var btn = slotGo.GetComponent<Button>();
                if (btn == null) btn = slotGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnSlotClicked(slotIndex));
            }
        }

        private void OnSlotClicked(int index)
        {
            if (inventory != null && index < inventory.Items.Count)
            {
                AudioManager.Instance?.PlayItemClick();
            }
            else
            {
                AudioManager.Instance?.PlayButtonClick();
            }
        }

        private void OnEnable()
        {
            if (inventory != null) { inventory.Changed += Refresh; Refresh(); }
            AudioManager.Instance?.PlayZipOpen();
        }
        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            AudioManager.Instance?.PlayZipClose();
        }
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

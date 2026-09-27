using System;
using G10.Prototype.Audio;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>Cargo presentation; inventory and expedition saves remain the state owners.</summary>
    public sealed class CreatureInventoryView : MonoBehaviour
    {
        public CreatureInventory inventory;
        public RawImage[] icons = Array.Empty<RawImage>();
        public Text[] labels = Array.Empty<Text>();
        public Text summary;
        public Button[] slots = Array.Empty<Button>();
        public Outline[] selectionFrames = Array.Empty<Outline>();
        public RawImage detailPreview;
        public Text detailName, detailQuantity, detailDescription;
        public string SelectedItemId { get; private set; }
        private UnityAction[] slotActions;

        private void Awake()
        {
            slotActions = new UnityAction[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slotActions[i] = () => SelectItem(index);
                if (slots[i] != null) slots[i].onClick.AddListener(slotActions[i]);
            }
        }
        private void OnEnable() { if (inventory != null) inventory.Changed += Refresh; Refresh(); }
        private void OnDisable() { if (inventory != null) inventory.Changed -= Refresh; }
        private void OnDestroy()
        {
            if (slotActions == null) return;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].onClick.RemoveListener(slotActions[i]);
        }
        public void SelectItem(int index)
        {
            if (inventory == null || index < 0 || index >= inventory.Items.Count) return;
            SelectedItemId = inventory.Items[index].Id;
            AudioManager.Instance?.PlayItemClick(); Refresh();
        }
        private void Refresh()
        {
            int count = inventory != null ? inventory.Items.Count : 0;
            if (count == 0) SelectedItemId = null;
            else if (string.IsNullOrEmpty(SelectedItemId) || !inventory.Contains(SelectedItemId)) SelectedItemId = inventory.Items[0].Id;
            if (summary != null) summary.text = $"{count} / {CreatureInventory.Capacity} Ô KHO";
            CreatureInventory.Item selected = null;
            for (int i = 0; i < icons.Length; i++)
            {
                var item = i < count ? inventory.Items[i] : null;
                bool isSelected = item != null && item.Id == SelectedItemId;
                if (isSelected) selected = item;
                SetImage(icons[i], item?.Icon);
                if (i < labels.Length && labels[i] != null) labels[i].text = item != null ? "×1" : "";
                if (i < slots.Length && slots[i] != null) slots[i].interactable = item != null;
                if (i < selectionFrames.Length && selectionFrames[i] != null) selectionFrames[i].enabled = isSelected;
            }
            SetImage(detailPreview, selected?.Icon);
            if (detailName != null) detailName.text = selected?.Name ?? "Kho đang trống";
            if (detailQuantity != null) detailQuantity.text = selected != null ? "×1" : "";
            if (detailDescription != null)
            {
                var definition = Definition(selected?.Id);
                detailDescription.text = definition != null ? definition.description : selected != null ?
                    "Vật phẩm thu thập trong chuyến thám hiểm." : "Vật phẩm bạn thu thập sẽ được lưu tại đây.";
            }
        }
        private static void SetImage(RawImage view, Texture2D texture)
        {
            if (view == null) return;
            view.texture = texture; view.enabled = texture != null;
            var fitter = view.GetComponent<AspectRatioFitter>();
            if (fitter != null && texture != null) fitter.aspectRatio = (float)texture.width / texture.height;
        }
        private SurveyContentDefinition Definition(string id)
        {
            if (string.IsNullOrEmpty(id) || inventory == null) return null;
            var story = inventory.GetComponent<ZoneOneStory>();
            return story != null ? story.FindContent(id) : inventory.GetComponent<ZoneMissionRuntime>()?.FindContent(id);
        }
    }
}

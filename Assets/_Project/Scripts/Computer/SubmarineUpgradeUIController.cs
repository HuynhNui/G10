using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>Renders scene-authored upgrade entries and forwards apply requests to their gameplay action.</summary>
    [DisallowMultipleComponent]
    public sealed class SubmarineUpgradeUIController : MonoBehaviour
    {
        [Header("Scene configuration")]
        [SerializeField] private Transform shipSystemsRoot;
        [SerializeField] private Transform modulesRoot;
        [SerializeField] private MonoBehaviour materialInventorySource;

        [Header("Selected upgrade")]
        [SerializeField] private UnityEngine.UI.Image selectedIcon;
        [SerializeField] private TMP_Text selectedName;
        [SerializeField] private TMP_Text selectedLevel;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Dynamic detail rows")]
        [SerializeField] private Transform comparisonRoot;
        [SerializeField] private UpgradeComparisonRowView comparisonRowPrefab;
        [SerializeField] private RectTransform materialsContent;
        [SerializeField] private UpgradeMaterialRequirementView materialRequirementPrefab;
        [SerializeField] private UnityEngine.UI.Scrollbar materialsScrollbar;

        [Header("Action")]
        [SerializeField] private UnityEngine.UI.Button actionButton;
        [SerializeField] private TMP_Text actionText;

        private readonly List<UpgradeEntryConfig> entries = new();
        private UpgradeEntryConfig selected;
        private G10.Prototype.Missions.ZoneOneStory story;
        private G10.Prototype.Navigation.CreatureInventory creatureInventory;
        private ExpeditionLoop expedition;
        private IUpgradeMaterialInventory Inventory => materialInventorySource as IUpgradeMaterialInventory;
        public UpgradeEntryConfig Selected => selected;

        private void OnEnable()
        {
            story = GetComponentInParent<G10.Prototype.Missions.ZoneOneStory>(true);
            if (story != null) story.Changed += RenderSelection;
            creatureInventory = materialInventorySource as G10.Prototype.Navigation.CreatureInventory;
            if (creatureInventory != null) creatureInventory.Changed += RenderSelection;
            expedition = FindAnyObjectByType<ExpeditionLoop>();
            if (expedition != null) expedition.Changed += RenderSelection;
            DiscoverEntries();
            if (actionButton != null) actionButton.onClick.AddListener(ApplySelected);
            Select(selected != null && entries.Contains(selected) ? selected : entries.Count > 0 ? entries[0] : null);
        }

        private void OnDisable()
        {
            if (story != null) story.Changed -= RenderSelection;
            if (creatureInventory != null) creatureInventory.Changed -= RenderSelection;
            if (expedition != null) expedition.Changed -= RenderSelection;
            foreach (var entry in entries) if (entry != null) entry.UnbindSelection();
            if (actionButton != null) actionButton.onClick.RemoveListener(ApplySelected);
        }

        public void DiscoverEntries()
        {
            foreach (var entry in entries) if (entry != null) entry.UnbindSelection();
            entries.Clear();
            AddEntries(shipSystemsRoot);
            AddEntries(modulesRoot);
            foreach (var entry in entries) entry.BindSelection(Select);
        }

        public void Select(UpgradeEntryConfig entry)
        {
            selected = entry;
            foreach (var candidate in entries) if (candidate != null) candidate.SetSelected(candidate == selected);
            RenderSelection();
        }

        private void AddEntries(Transform root)
        {
            if (root == null) return;
            foreach (var entry in root.GetComponentsInChildren<UpgradeEntryConfig>(true)) entries.Add(entry);
        }

        private void RenderSelection()
        {
            foreach (var entry in entries) if (entry != null) entry.SetSelected(entry == selected);
            ClearChildren(comparisonRoot);
            ClearChildren(materialsContent);
            if (selected == null)
            {
                if (selectedIcon != null) selectedIcon.enabled = false;
                if (selectedName != null) selectedName.text = "SELECT AN UPGRADE";
                if (selectedLevel != null) selectedLevel.text = "";
                if (descriptionText != null) descriptionText.text = "";
                if (actionButton != null) actionButton.interactable = false;
                return;
            }

            if (selectedIcon != null) { selectedIcon.enabled = true; selectedIcon.sprite = selected.Icon; selectedIcon.preserveAspect = true; }
            if (selectedName != null) selectedName.text = selected.DisplayName;
            if (selectedLevel != null) selectedLevel.text = $"Lv. {selected.Level}";
            if (descriptionText != null) descriptionText.text = selected.Description;

            foreach (var data in selected.ComparisonRows)
                if (data != null && comparisonRowPrefab != null && comparisonRoot != null)
                    Instantiate(comparisonRowPrefab, comparisonRoot, false).Bind(data);

            bool materialsAvailable = true;
            foreach (var requirement in selected.MaterialRequirements)
            {
                if (requirement == null || requirement.Material == null) continue;
                int? owned = Inventory != null ? Inventory.GetCount(requirement.Material.MaterialId) : null;
                if (!owned.HasValue || owned.Value < requirement.RequiredAmount) materialsAvailable = false;
                if (materialRequirementPrefab != null && materialsContent != null)
                    Instantiate(materialRequirementPrefab, materialsContent, false).Bind(requirement, owned);
            }

            if (actionText != null) actionText.text = selected.IsMax ? "MAX" : selected.Category == UpgradeCategory.ShipSystem ? "UPGRADE" : "ADD";
            if (selected.UpgradeId == "ExpeditionModule" && story != null)
            {
                if (actionText != null) actionText.text = story.ProgressionActionTitle;
                if (selectedName != null) selectedName.text = "EXPEDITION UPGRADE";
                if (descriptionText != null) descriptionText.text = story.ProgressionActionDescription;
                if (selectedLevel != null) selectedLevel.text = story.MainObjectivesComplete ? "COMPLETE" : story.config?.displayName ?? "";
            }
            if (actionButton != null) actionButton.interactable = selected.CanApply && materialsAvailable;
            Canvas.ForceUpdateCanvases();
            if (materialsScrollbar != null) materialsScrollbar.SetValueWithoutNotify(1);
        }

        private void ApplySelected()
        {
            if (selected == null || actionButton == null || !actionButton.interactable) return;
            if (!selected.TryApply()) { RenderSelection(); return; }
            // Gameplay action owns the complete purchase transaction; never consume again in UI.
            RenderSelection();
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            { root.GetChild(i).gameObject.SetActive(false); Destroy(root.GetChild(i).gameObject); }
        }
    }
}

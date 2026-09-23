using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace G10.Prototype.Computer
{
    [DisallowMultipleComponent]
    public sealed class UpgradeEntryConfig : MonoBehaviour
    {
        [Header("Upgrade data")]
        [SerializeField] private UpgradeCategory category;
        [SerializeField] private Sprite icon;
        [SerializeField] private string displayName;
        [Min(1), SerializeField] private int level = 1;
        [TextArea(2, 5), SerializeField] private string description;
        [SerializeField] private UpgradeComparisonData[] comparisonRows = Array.Empty<UpgradeComparisonData>();
        [SerializeField] private UpgradeMaterialRequirement[] materialRequirements = Array.Empty<UpgradeMaterialRequirement>();
        [SerializeField] private string upgradeId;
        [Tooltip("Optional component implementing IUpgradeAction. Gameplay remains outside the UI controller.")]
        [SerializeField] private MonoBehaviour actionSource;
        [SerializeField] private UnityEvent onApply = new();

        [Header("Prefab view")]
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Image background;
        [SerializeField] private UnityEngine.UI.Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite disabledSprite;

        private UnityAction selectionAction;

        public UpgradeCategory Category => category;
        public Sprite Icon => icon;
        public string DisplayName => displayName;
        public int Level => level;
        public string Description => description;
        public UpgradeComparisonData[] ComparisonRows => comparisonRows ?? Array.Empty<UpgradeComparisonData>();
        public UpgradeMaterialRequirement[] MaterialRequirements => materialRequirements ?? Array.Empty<UpgradeMaterialRequirement>();
        public string UpgradeId => upgradeId;
        public bool CanApply => actionSource is IUpgradeAction action && action.CanApply ||
                                actionSource == null && onApply != null && onApply.GetPersistentEventCount() > 0;

        private void Awake() => RefreshStaticView();

        private void OnValidate() => RefreshStaticView();

        public void BindSelection(Action<UpgradeEntryConfig> listener)
        {
            UnbindSelection();
            if (button == null || listener == null) return;
            selectionAction = () => listener(this);
            button.onClick.AddListener(selectionAction);
        }

        public void UnbindSelection()
        {
            if (button != null && selectionAction != null) button.onClick.RemoveListener(selectionAction);
            selectionAction = null;
        }

        public void SetSelected(bool selected)
        {
            RefreshStaticView();
            if (background == null) return;
            background.sprite = selected && selectedSprite != null ? selectedSprite :
                !CanApply && disabledSprite != null ? disabledSprite : defaultSprite;
            background.color = selected ? new Color(.68f, .94f, 1f) : !CanApply ? new Color(.9f, .93f, .96f) : Color.white;
        }

        public bool TryApply()
        {
            if (actionSource is IUpgradeAction action)
            {
                if (!action.TryApply()) return false;
                onApply?.Invoke();
                return true;
            }
            if (actionSource != null || onApply == null || onApply.GetPersistentEventCount() == 0) return false;
            onApply.Invoke();
            return true;
        }

        private void RefreshStaticView()
        {
            if (iconImage != null) { iconImage.sprite = icon; iconImage.preserveAspect = true; }
            if (nameText != null) nameText.text = string.IsNullOrEmpty(displayName) ? "" :
                System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(displayName.ToLowerInvariant()).Replace(" ", "\n");
            if (levelText != null) levelText.text = $"Lv. {Mathf.Max(1, level)}";
            if (background != null && background.sprite == null) background.sprite = defaultSprite;
        }
    }
}

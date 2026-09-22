using TMPro;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public sealed class UpgradeMaterialRequirementView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private Color sufficientColor = new(0.08f, 0.55f, 0.42f);
        [SerializeField] private Color insufficientColor = new(0.9f, 0.18f, 0.38f);
        [SerializeField] private Color neutralColor = new(0.1f, 0.25f, 0.48f);

        public void Bind(UpgradeMaterialRequirement requirement, int? owned)
        {
            if (requirement == null || requirement.Material == null) return;
            if (icon != null) { icon.sprite = requirement.Material.Icon; icon.preserveAspect = true; }
            if (nameText != null) nameText.text = requirement.Material.DisplayName;
            if (quantityText == null) return;
            quantityText.text = owned.HasValue ? $"{owned.Value} / {requirement.RequiredAmount}" : $"x{requirement.RequiredAmount}";
            quantityText.color = !owned.HasValue ? neutralColor :
                owned.Value >= requirement.RequiredAmount ? sufficientColor : insufficientColor;
        }
    }
}

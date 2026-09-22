using TMPro;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public sealed class UpgradeComparisonRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text statName;
        [SerializeField] private TMP_Text currentValue;
        [SerializeField] private TMP_Text nextValue;

        public void Bind(UpgradeComparisonData data)
        {
            if (data == null) return;
            if (statName != null) statName.text = data.StatName;
            if (currentValue != null) currentValue.text = data.CurrentValue;
            if (nextValue != null) nextValue.text = data.NextValue;
        }
    }
}

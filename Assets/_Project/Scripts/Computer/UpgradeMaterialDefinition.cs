using UnityEngine;

namespace G10.Prototype.Computer
{
    [CreateAssetMenu(menuName = "G10/Computer/Upgrade Material", fileName = "UpgradeMaterial")]
    public sealed class UpgradeMaterialDefinition : ScriptableObject
    {
        [SerializeField] private string materialId;
        [SerializeField] private Sprite icon;
        [SerializeField] private string displayName;

        public string MaterialId => materialId;
        public Sprite Icon => icon;
        public string DisplayName => displayName;
    }
}

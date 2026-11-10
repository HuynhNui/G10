using UnityEngine;

namespace G10.Prototype.Missions
{
    [CreateAssetMenu(menuName = "G10/Survey/Creature or Item")]
    public sealed class SurveyContentDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(3, 10)] public string description;
        public Texture2D image;
        [Tooltip("Standalone Single sprite (not a packed atlas). Replace this to update the prefab, photo subject and inventory art.")]
        public Sprite sprite;
        public Texture2D Image => sprite != null ? sprite.texture : image;
        [Header("Collected material presentation (does not change creature identity)")]
        public Sprite collectedMaterial;
        public string collectedMaterialName;
        [TextArea(2, 5)] public string collectedMaterialDescription;
        public Texture2D InventoryImage => collectedMaterial != null ? collectedMaterial.texture : Image;
        public string InventoryName => !string.IsNullOrEmpty(collectedMaterialName) ? collectedMaterialName : displayName;
        public string InventoryDescription => !string.IsNullOrEmpty(collectedMaterialDescription) ? collectedMaterialDescription : description;
        [Tooltip("Complete authored PSD painting for new survey photographs. Image/sprite remain capture and inventory artwork.")]
        public Texture2D photoArtwork;
        [Tooltip("Keep the authored survey measurement footprint when replacing presentation artwork.")]
        public bool preservePhotoCoverage;
        [Tooltip("Pre-replacement subject mask. Null retains the photo profile's original distance-specific fallback mask.")]
        public Texture2D photoCoverageMask;
        public Texture2D PhotoCoverageImage(Texture2D fallback) =>
            preservePhotoCoverage ? photoCoverageMask != null ? photoCoverageMask : fallback : Image != null ? Image : fallback;
        public GameObject prefab;
        [Tooltip("True until dedicated production artwork is supplied.")]
        public bool placeholderArt;
    }
}

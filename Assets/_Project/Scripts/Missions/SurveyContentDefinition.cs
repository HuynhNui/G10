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
        public GameObject prefab;
        [Tooltip("True until dedicated production artwork is supplied.")]
        public bool placeholderArt;
    }
}

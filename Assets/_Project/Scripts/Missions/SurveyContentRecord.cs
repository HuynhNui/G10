using UnityEngine;

namespace G10.Prototype.Missions
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SurveyContentRecord : MonoBehaviour
    {
        public SurveyContentDefinition definition;
        private void OnEnable() => Refresh();
        private void OnValidate() => Refresh();
        private void Refresh()
        { if (definition != null && TryGetComponent<SpriteRenderer>(out var renderer)) renderer.sprite = definition.sprite; }
    }
}

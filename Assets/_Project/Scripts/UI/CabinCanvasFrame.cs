using UnityEngine;

namespace G10.Prototype.UI
{
    /// <summary>Scales a fixed authoring surface uniformly, including its children and hit areas.</summary>
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class CabinCanvasFrame : MonoBehaviour
    {
        private void OnEnable() => Fit();
        private void LateUpdate() => Fit();
        private void Fit()
        {
            RectTransform rect = (RectTransform)transform;
            if (rect.parent is not RectTransform parent) return;
            float scale = Mathf.Min(parent.rect.width / 1920f, parent.rect.height / 1080f);
            Vector3 target = new(scale, scale, 1f);
            if (rect.localScale != target) rect.localScale = target;
        }
    }
}

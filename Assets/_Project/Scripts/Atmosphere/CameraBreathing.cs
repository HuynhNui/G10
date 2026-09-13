using UnityEngine;

namespace G10.Prototype.Atmosphere
{
    /// <summary>Attach to a Camera or to a separate UI presentation rig for ScreenSpaceOverlay cabins.</summary>
    public sealed class CameraBreathing : MonoBehaviour
    {
        public Vector2 drift = new(0.8f, 0.6f);
        [Min(0)] public float cyclesPerSecond = 0.024f;
        [Range(0, 0.01f)] public float zoomFraction = 0.0015f;
        public Camera opticalCamera;
        public CabinAtmosphere mood;
        private Vector3 origin, scale;
        private float size, elapsed;
        private void OnEnable()
        {
            origin = transform.localPosition; scale = transform.localScale; elapsed = 0;
            if (opticalCamera != null) size = opticalCamera.orthographicSize;
        }
        private void LateUpdate()
        {
            elapsed += Time.deltaTime;
            float t = elapsed * cyclesPerSecond * Mathf.PI * 2;
            float strength = mood != null ? mood.Motion : 1;
            transform.localPosition = origin + new Vector3(Mathf.Sin(t) * drift.x, Mathf.Sin(t * 0.73f) * drift.y) * strength;
            float zoom = 1 + Mathf.Sin(t * 0.83f) * zoomFraction * strength;
            if (opticalCamera != null && opticalCamera.orthographic) opticalCamera.orthographicSize = size / zoom;
            else transform.localScale = scale * zoom;
        }
        private void OnDisable()
        {
            transform.localPosition = origin; transform.localScale = scale;
            if (opticalCamera != null && opticalCamera.orthographic) opticalCamera.orthographicSize = size;
        }
    }
}

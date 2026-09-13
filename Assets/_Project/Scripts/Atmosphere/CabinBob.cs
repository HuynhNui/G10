using UnityEngine;

namespace G10.Prototype.Atmosphere
{
    /// <summary>Local, non-accumulating sway. Use pixels on UI rigs, world units on sprites.</summary>
    public sealed class CabinBob : MonoBehaviour
    {
        public Vector2 amplitude = new(2f, 3f);
        public Vector2 cyclesPerSecond = new(0.035f, 0.055f);
        [Range(0, 1)] public float rotationDegrees = 0.12f;
        [Min(0)] public float rotationCyclesPerSecond = 0.028f;
        public float phase;
        public CabinAtmosphere mood;
        private Vector3 origin;
        private Quaternion rotation;
        private float elapsed;
        private void OnEnable() { origin = transform.localPosition; rotation = transform.localRotation; elapsed = 0; }
        private void LateUpdate()
        {
            elapsed += Time.deltaTime;
            float strength = mood != null ? mood.Motion : 1;
            transform.localPosition = origin + new Vector3(
                Mathf.Sin(elapsed * cyclesPerSecond.x * Mathf.PI * 2 + phase) * amplitude.x,
                Mathf.Sin(elapsed * cyclesPerSecond.y * Mathf.PI * 2 + phase) * amplitude.y) * strength;
            transform.localRotation = rotation * Quaternion.Euler(0, 0,
                Mathf.Sin(elapsed * rotationCyclesPerSecond * Mathf.PI * 2 + phase) * rotationDegrees * strength);
        }
        private void OnDisable() { transform.localPosition = origin; transform.localRotation = rotation; }
    }
}

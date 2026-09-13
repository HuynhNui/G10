using UnityEngine;

namespace G10.Prototype.Atmosphere
{
    /// <summary>Decorative needle only; never attach to an authoritative navigation needle.</summary>
    public sealed class GaugeJitter : MonoBehaviour
    {
        [Range(0, 3)] public float amplitudeDegrees = 0.65f;
        [Min(0)] public float noiseSpeed = 0.25f;
        public float seed = 18.7f;
        public CabinAtmosphere mood;
        private Quaternion origin;
        private float elapsed;
        private void OnEnable() { origin = transform.localRotation; elapsed = 0; }
        private void LateUpdate()
        {
            elapsed += Time.deltaTime;
            float noise = Mathf.PerlinNoise(seed, elapsed * noiseSpeed) * 2 - 1;
            transform.localRotation = origin * Quaternion.Euler(0, 0, noise * amplitudeDegrees * (mood != null ? mood.Motion : 1));
        }
        private void OnDisable() => transform.localRotation = origin;
    }
}

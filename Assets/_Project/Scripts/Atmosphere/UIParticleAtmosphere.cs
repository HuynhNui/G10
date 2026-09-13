using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Atmosphere
{
    /// <summary>Draws a bounded native ParticleSystem into the existing overlay Canvas, without another camera.</summary>
    [RequireComponent(typeof(CanvasRenderer), typeof(ParticleSystem))]
    public sealed class UIParticleAtmosphere : MaskableGraphic
    {
        public Texture2D softParticle;
        public CabinAtmosphere mood;
        [Range(1, 128)] public int capacity = 40;
        public override Texture mainTexture => softParticle != null ? softParticle : Texture2D.whiteTexture;
        private ParticleSystem system;
        private ParticleSystem.Particle[] particles;
        private float nextFrame;
        protected override void OnEnable()
        {
            base.OnEnable(); raycastTarget = false;
            system = GetComponent<ParticleSystem>(); particles = new ParticleSystem.Particle[Mathf.Clamp(capacity, 1, 128)];
        }
        private void Update()
        { if (Time.time < nextFrame) return; nextFrame = Time.time + 1f / 30; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (system == null || particles == null) return;
            int count = system.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = particles[i].position; float half = particles[i].GetCurrentSize(system) * 0.5f;
                Color tint = particles[i].GetCurrentColor(system); tint.a *= mood != null ? mood.Particles : 1;
                int n = vh.currentVertCount;
                vh.AddVert(p + new Vector3(-half, -half), tint, new Vector2(0, 0));
                vh.AddVert(p + new Vector3(-half, half), tint, new Vector2(0, 1));
                vh.AddVert(p + new Vector3(half, half), tint, new Vector2(1, 1));
                vh.AddVert(p + new Vector3(half, -half), tint, new Vector2(1, 0));
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}

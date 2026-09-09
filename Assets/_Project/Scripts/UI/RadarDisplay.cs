using G10.Prototype.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RadarDisplay : MaskableGraphic
    {
        [SerializeField] private ZoneNavigation navigation;
        [SerializeField] private float range = 85f;
        private float scanStarted = -100f;
        private float nextRefresh;
        public bool IsScanning => Time.unscaledTime - scanStarted < 2f;
        public void Configure(ZoneNavigation owner) { navigation = owner; raycastTarget = false; }
        public void Scan() { scanStarted = Time.unscaledTime; SetVerticesDirty(); }
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.06f;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.49f;
            Color faint = new(0.5f, 1f, 0.89f, 0.3f);
            for (int ring = 1; ring <= 3; ring++)
            for (int i = 0; i < 80; i++)
            {
                float a = i * Mathf.PI * 2 / 80;
                float b = (i + 1) * Mathf.PI * 2 / 80;
                Line(vh, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius * ring / 3f), new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * (radius * ring / 3f), 1.4f, faint);
            }
            Line(vh, new(-radius, 0), new(radius, 0), 1f, faint);
            Line(vh, new(0, -radius), new(0, radius), 1f, faint);
            if (navigation == null) return;
            float elapsed = Time.unscaledTime - scanStarted;
            if (elapsed < 8f)
            {
                for (int y = -20; y <= 20; y++)
                for (int x = -20; x <= 20; x++)
                {
                    Vector2 offset = new Vector2(x, y) / 20f;
                    if (offset.sqrMagnitude > 1f || offset.magnitude > Mathf.Clamp01(elapsed / 2f)) continue;
                    if (!navigation.IsWater(navigation.Position + offset * range))
                    {
                        Vector2 point = offset * radius;
                        Line(vh, point - Vector2.right * 2f, point + Vector2.right * 2f, 4f, new Color(0.8f, 1f, 0.75f, Mathf.Clamp01(8f - elapsed)));
                    }
                }
                float sweep = elapsed * Mathf.PI;
                Line(vh, Vector2.zero, new Vector2(Mathf.Sin(sweep), Mathf.Cos(sweep)) * radius, 2.5f, Color.cyan);
            }
            float h = navigation.Heading * Mathf.Deg2Rad;
            Line(vh, Vector2.zero, new Vector2(Mathf.Sin(h), Mathf.Cos(h)) * 22f, 4f, Color.white);
            Line(vh, new(-4, 0), new(4, 0), 8f, Color.white);
        }
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * (width / 2);
            int i = vh.currentVertCount;
            vh.AddVert(a - n, tint, Vector2.zero); vh.AddVert(a + n, tint, Vector2.zero);
            vh.AddVert(b + n, tint, Vector2.zero); vh.AddVert(b - n, tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}

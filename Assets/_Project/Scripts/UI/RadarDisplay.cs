using System.Collections;
using G10.Prototype.Audio;
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
        public PhotoSurveyZone photoSurvey;
        public CreatureCatcher catcher;
        public Color captureRingColor = new(.65f, .9f, .08f, .9f);
        public Color contactColor = new(1f, 0.72f, 0.12f, 1f);
        private bool detected;
        private Vector2 scanOrigin;
        private Vector2 contactPosition;
        public const float SweepDuration = 2.0f;
        public const float PersistenceDuration = 6.0f;
        public int VisibleContactCount => detected && photoSurvey != null && photoSurvey.creaturePresent && 
            (Time.unscaledTime - scanStarted < PersistenceDuration) &&
            (Vector2.Distance(scanOrigin, contactPosition) / range <= Mathf.Clamp01((Time.unscaledTime - scanStarted) / SweepDuration)) ? 1 : 0;
        private float scanStarted = -100f;
        private float nextRefresh;

        public bool IsContinuousScanning => false;
        public bool IsScanning => (Time.unscaledTime - scanStarted) >= 0f && (Time.unscaledTime - scanStarted) < SweepDuration;

        public void Configure(ZoneNavigation owner) { navigation = owner; raycastTarget = false; }

        public void Scan()
        {
            if (navigation != null && navigation.ExpeditionBlocked) return;
            StartSweep();
        }

        public void ToggleContinuousScan() => Scan();
        public void StartContinuousScan() => Scan();

        public void StartSweep()
        {
            scanStarted = Time.unscaledTime;
            if (navigation != null) scanOrigin = navigation.Position;
            detected = navigation != null && photoSurvey != null && photoSurvey.Detectable(navigation, range);
            if (detected) contactPosition = photoSurvey.center;

            AudioManager.Instance?.PlayRadarPing();
            SetVerticesDirty();
        }

        public void StopContinuousScan()
        {
            if (IsScanning)
            {
                scanStarted = -100f;
                AudioManager.Instance?.StopRadarPing();
                SetVerticesDirty();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopContinuousScan();
        }

        private void Update()
        {
            if (IsScanning)
            {
                // Continuous 60fps update during the 360-degree sweep
                SetVerticesDirty();
            }
            else if (Time.unscaledTime - scanStarted < PersistenceDuration)
            {
                if (Time.unscaledTime < nextRefresh) return;
                nextRefresh = Time.unscaledTime + 0.08f;
                SetVerticesDirty();
            }
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
            if (catcher != null)
            {
                float reach = Mathf.Clamp01(catcher.captureRadius / range) * radius;
                for (int i = 0; i < 96; i++)
                {
                    float a = i * Mathf.PI * 2 / 96, b = (i + 1) * Mathf.PI * 2 / 96;
                    Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * reach;
                    Vector2 q = new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * reach;
                    int v = vh.currentVertCount;
                    Color fill = captureRingColor; fill.a *= .18f;
                    vh.AddVert(Vector2.zero, fill, Vector2.zero); vh.AddVert(p, fill, Vector2.zero); vh.AddVert(q, fill, Vector2.zero);
                    vh.AddTriangle(v, v + 1, v + 2);
                    Line(vh, p, q, 3f, captureRingColor);
                }
            }
            float elapsed = Time.unscaledTime - scanStarted;
            if (elapsed >= 0f && elapsed < PersistenceDuration)
            {
                for (int y = -20; y <= 20; y++)
                for (int x = -20; x <= 20; x++)
                {
                    Vector2 offset = new Vector2(x, y) / 20f;
                    if (offset.sqrMagnitude > 1f) continue;
                    float waveProgress = Mathf.Clamp01(elapsed / SweepDuration);
                    if (offset.magnitude > waveProgress && elapsed < SweepDuration) continue;

                    if (!navigation.IsWater(scanOrigin + offset * range))
                    {
                        Vector2 relative = (scanOrigin + offset * range - navigation.Position) / range;
                        if (relative.sqrMagnitude > 1f) continue;
                        Vector2 point = relative * radius;
                        float alpha = Mathf.Clamp01((PersistenceDuration - elapsed) / (PersistenceDuration - SweepDuration));
                        Line(vh, point - Vector2.right * 2f, point + Vector2.right * 2f, 4f, new Color(0.8f, 1f, 0.75f, alpha));
                    }
                }
                if (VisibleContactCount > 0 && Vector2.Distance(contactPosition, navigation.Position) <= range)
                {
                    Vector2 p = (contactPosition - navigation.Position) / range * radius;
                    float alpha = Mathf.Clamp01((PersistenceDuration - elapsed) / (PersistenceDuration - SweepDuration));
                    Color tint = contactColor; tint.a *= alpha;
                    Line(vh, p + Vector2.up * 7, p + Vector2.right * 7, 3, tint);
                    Line(vh, p + Vector2.right * 7, p + Vector2.down * 7, 3, tint);
                    Line(vh, p + Vector2.down * 7, p + Vector2.left * 7, 3, tint);
                    Line(vh, p + Vector2.left * 7, p + Vector2.up * 7, 3, tint);
                }

                // Needle ONLY drawn while scanning (0 <= elapsed <= SweepDuration)!
                // Exactly 360 degrees clockwise from 12 o'clock!
                if (elapsed <= SweepDuration)
                {
                    float sweepProgress = elapsed / SweepDuration;
                    float sweepAngle = sweepProgress * (Mathf.PI * 2f);
                    Vector2 needleEnd = new Vector2(Mathf.Sin(sweepAngle), Mathf.Cos(sweepAngle)) * radius;
                    Line(vh, Vector2.zero, needleEnd, 2.5f, Color.cyan);
                }
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

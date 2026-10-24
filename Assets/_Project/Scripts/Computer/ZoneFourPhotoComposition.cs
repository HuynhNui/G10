using System;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public enum ZoneFourPhotoLayer { Water, Seabed, Prop, Fog, Particles }
    public enum ZoneFourPhotoDetail { None, DistantWreck, SedimentFins, AsymmetricShadow }

    /// <summary>Cached environmental layers, baked through the real archival photo compositor.</summary>
    public sealed class ZoneFourPhotoComposition : IDisposable
    {
        private const int TextureWidth = 192, TextureHeight = 80;
        private Texture2D wreck, sedimentFins, asymmetricShadow;
        // Rendering-only diagnostic control for matched before/after captures. Never serialized as progression.
        public bool Enabled { get; set; } = true;
        public bool EnvironmentEnabled { get; set; } = true;
        public bool LastWreckComposited { get; private set; }
        public Rect LastWreckRect { get; private set; }
        public ZoneFourPhotoDetail LastDetailKind { get; private set; }
        public Rect LastDetailRect { get; private set; }
        public int CachedTextureCount => (wreck != null ? 1 : 0) + (sedimentFins != null ? 1 : 0) + (asymmetricShadow != null ? 1 : 0);
        public bool HasCachedTexture => CachedTextureCount > 0;
        public int TextureBuildCount { get; private set; }

        public void BeginFrame()
        { LastWreckComposited = false; LastWreckRect = default; LastDetailKind = ZoneFourPhotoDetail.None; LastDetailRect = default; }

        /// <summary>RGB only: the compositor's original source alpha and coverage authority remain intact.</summary>
        public Color EnvironmentTint(string zoneId, ZoneFourPhotoLayer layer)
        {
            if (!EnvironmentEnabled || zoneId != "Zone04") return Color.white;
            return layer switch
            {
                ZoneFourPhotoLayer.Water => new Color(.25f, .34f, .43f, 1),
                ZoneFourPhotoLayer.Seabed => new Color(.24f, .31f, .38f, 1),
                ZoneFourPhotoLayer.Prop => new Color(.29f, .36f, .42f, 1),
                ZoneFourPhotoLayer.Fog => new Color(.28f, .36f, .43f, 1),
                ZoneFourPhotoLayer.Particles => new Color(.37f, .46f, .50f, 1),
                _ => Color.white
            };
        }

        /// <summary>Only a real framed site can contribute detail; these layers never become photo subjects.</summary>
        public bool DrawSiteBackground(PhotoLayerComposer composer, string zoneId, string poiId, Rect subjectRect, float visibility)
        {
            if (!Enabled || composer == null || zoneId != "Zone04" || subjectRect.width <= 0 || subjectRect.height <= 0) return false;
            if (poiId == "zone04-l1") return DrawDistantWreck(composer, zoneId, poiId, subjectRect, visibility);
            if (poiId == "zone04-l3")
            {
                if (sedimentFins == null) sedimentFins = BuildSedimentFins();
                float width = Mathf.Lerp(.12f, .16f, Mathf.InverseLerp(.25f, 1.1f, subjectRect.height));
                float height = width * PhotoLayerComposer.Width / PhotoLayerComposer.Height * .75f;
                var center = new Vector2(Mathf.Clamp(subjectRect.center.x + .24f, .16f, .84f), .32f);
                var rect = new Rect(center - new Vector2(width, height) * .5f, new Vector2(width, height));
                composer.Draw(sedimentFins, rect, Mathf.Lerp(.63f, .84f, Mathf.Clamp01(visibility)), isSubject: false, occludes: false);
                RecordDetail(ZoneFourPhotoDetail.SedimentFins, rect); return true;
            }
            if (poiId == "zone04-l5")
            {
                if (asymmetricShadow == null) asymmetricShadow = BuildAsymmetricShadow();
                // The distant water darkens on one side; the unchanged creature is drawn later.
                float x = Mathf.Clamp(subjectRect.center.x + .01f, .43f, .57f);
                var rect = new Rect(x, -.04f, .66f, 1.08f);
                composer.Draw(asymmetricShadow, rect, Mathf.Lerp(.68f, .90f, Mathf.Clamp01(visibility)), isSubject: false, occludes: false);
                RecordDetail(ZoneFourPhotoDetail.AsymmetricShadow, rect); return true;
            }
            return false;
        }

        private void RecordDetail(ZoneFourPhotoDetail kind, Rect rect) { LastDetailKind = kind; LastDetailRect = rect; }

        public bool DrawDistantWreck(PhotoLayerComposer composer, string zoneId, string poiId, Rect subjectRect, float visibility)
        {
            if (!Enabled || composer == null || zoneId != "Zone04" || poiId != "zone04-l1" || subjectRect.width <= 0 || subjectRect.height <= 0)
                return false;
            if (wreck == null) BuildTexture();
            // Its much slower size change suggests distance behind the specimen. The actual
            // subject bearing shifts both in-frame; no new world contact or detection is added.
            float width = Mathf.Lerp(.12f, .15f, Mathf.InverseLerp(.25f, 1.1f, subjectRect.height));
            float height = width * PhotoLayerComposer.Width / PhotoLayerComposer.Height * TextureHeight / TextureWidth;
            // The generic kit's tall kelp prop is on the left of this site. The opposite
            // shoulder leaves part of the distant hull readable behind the creature.
            var center = new Vector2(Mathf.Clamp(subjectRect.center.x + .23f, .14f, .86f), .365f);
            var rect = new Rect(center - new Vector2(width, height) * .5f, new Vector2(width, height));
            composer.Draw(wreck, rect, Mathf.Lerp(.55f, .70f, Mathf.Clamp01(visibility)), isSubject: false, occludes: false);
            LastWreckRect = rect; LastWreckComposited = true; RecordDetail(ZoneFourPhotoDetail.DistantWreck, rect); return true;
        }

        private Texture2D BuildSedimentFins()
        {
            const int width = 128, height = 96;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float u = (x + .5f) / width, v = (y + .5f) / height;
                float wash = Mathf.PerlinNoise(u * 8.3f + 9.1f, v * 6.1f + 2.8f);
                float paper = Mathf.PerlinNoise(u * 36 + 2, v * 28 + 17);
                float shape = 0;
                for (int i = 0; i < 5; i++)
                {
                    var start = new Vector2(.27f + i * .092f, .22f + i * .012f);
                    var end = new Vector2(.18f + i * .148f, .60f + Mathf.Sin(i * .94f + .2f) * .19f);
                    Vector2 axis = end - start, delta = new Vector2(u, v) - start;
                    float along = Mathf.Clamp01(Vector2.Dot(delta, axis) / axis.sqrMagnitude);
                    float distance = (delta - axis * along).magnitude;
                    float thickness = Mathf.Lerp(.023f, .006f, along) * Mathf.Lerp(.7f, 1.15f, wash);
                    float fin = 1 - Smooth(thickness, thickness + .012f, distance);
                    // Interrupted edges and uneven heights read as distant sediment, not clean symbols.
                    fin *= Mathf.Lerp(.46f, 1, paper) * (1 - Smooth(.82f, 1, along) * .25f);
                    shape = Mathf.Max(shape, fin);
                }
                float haze = Smooth(.15f, .34f, v) * (1 - Smooth(.72f, .93f, v));
                var color = Color.Lerp(new Color(.17f, .28f, .33f), new Color(.28f, .40f, .43f), wash * .7f);
                color.a = shape * haze * Mathf.Lerp(.62f, .86f, wash);
                pixels[y * width + x] = color;
            }
            return CacheTexture(width, height, pixels, "Zone 4 hidden A distant sediment fins");
        }

        private Texture2D BuildAsymmetricShadow()
        {
            const int width = 160, height = 128;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float u = (x + .5f) / width, v = (y + .5f) / height;
                float wash = Mathf.PerlinNoise(u * 3.4f + 18, v * 4.2f + 7);
                float edge = .17f + (1 - v) * .20f + Mathf.Sin(v * 5.4f) * .035f;
                float lateral = Smooth(edge, edge + .42f, u);
                float vertical = Smooth(.02f, .23f, v) * (1 - Smooth(.85f, 1, v));
                // The right padding lies outside the photo, preventing a hard rectangular edge.
                float alpha = lateral * vertical * (1 - Smooth(.92f, 1, u)) * Mathf.Lerp(.48f, .63f, wash);
                pixels[y * width + x] = new Color(.018f, .044f, .064f, alpha);
            }
            return CacheTexture(width, height, pixels, "Zone 4 hidden B asymmetric deep water");
        }

        private Texture2D CacheTexture(int width, int height, Color32[] pixels, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels); texture.Apply(false, false); TextureBuildCount++; return texture;
        }

        private void BuildTexture()
        {
            var pixels = new Color32[TextureWidth * TextureHeight];
            const float tilt = -.075f;
            float cosine = Mathf.Cos(tilt), sine = Mathf.Sin(tilt);
            for (int y = 0; y < TextureHeight; y++) for (int x = 0; x < TextureWidth; x++)
            {
                float u = (x + .5f) / TextureWidth, v = (y + .5f) / TextureHeight;
                var q = new Vector2(u - .5f, v - .5f);
                var p = new Vector2(q.x * cosine - q.y * sine, q.x * sine + q.y * cosine);
                float wash = Mathf.PerlinNoise(u * 6.7f + 14.3f, v * 4.1f + 3.8f);
                float paper = Mathf.PerlinNoise(u * 39 + 5, v * 23 + 12);
                float body = Ellipse(p, new Vector2(-.015f, -.055f), new Vector2(.35f, .095f), .14f);
                // A damaged bow and incomplete lower contour keep it from reading as a clean icon.
                float brokenBow = Ellipse(p, new Vector2(.30f, -.004f), new Vector2(.060f, .080f), .25f);
                body *= 1 - brokenBow * .72f;
                float tower = SoftBox(p, new Vector2(.005f, .055f), new Vector2(.050f, .075f), .012f);
                float mast = SoftBox(new Vector2(p.x + p.y * .10f, p.y), new Vector2(.006f, .165f), new Vector2(.008f, .060f), .008f);
                float tail = SoftBox(p, new Vector2(-.287f, .035f), new Vector2(.023f, .092f), .014f);
                tail *= 1 - Smooth(.045f, .15f, p.y) * Smooth(-.30f, -.265f, p.x);
                float lowerFin = Ellipse(p, new Vector2(-.268f, -.142f), new Vector2(.045f, .030f), .35f) * .65f;
                float shape = Mathf.Max(body, Mathf.Max(tower, Mathf.Max(mast * .65f, Mathf.Max(tail, lowerFin))));
                // Depth attenuation erases parts of the underside and breaks the far contour.
                // Transparent padding remains transparent; there is no rectangular matte.
                float haze = Mathf.Lerp(.25f, 1, Smooth(.24f, .49f, v + (wash - .5f) * .11f));
                float alpha = shape * haze * Mathf.Lerp(.52f, .78f, wash) * Mathf.Lerp(.83f, 1, paper);
                var color = Color.Lerp(new Color(.10f, .20f, .27f), new Color(.19f, .30f, .35f), wash * .65f + paper * .15f);
                color.a = alpha;
                pixels[y * TextureWidth + x] = color;
            }
            wreck = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            { name = "Zone 4 distant wreck photo layer", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            wreck.SetPixels32(pixels); wreck.Apply(false, false); TextureBuildCount++;
        }
        private static float Ellipse(Vector2 p, Vector2 center, Vector2 radius, float softness)
            => 1 - Smooth(1 - softness, 1 + softness, new Vector2((p.x - center.x) / radius.x, (p.y - center.y) / radius.y).magnitude);
        private static float SoftBox(Vector2 p, Vector2 center, Vector2 radius, float softness)
        {
            var delta = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - radius;
            return 1 - Smooth(-softness, softness, Mathf.Max(delta.x, delta.y));
        }
        private static float Smooth(float min, float max, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(min, max, value));
        public void Dispose()
        {
            DestroyCached(wreck); DestroyCached(sedimentFins); DestroyCached(asymmetricShadow);
            wreck = null; sedimentFins = null; asymmetricShadow = null; BeginFrame();
        }
        private static void DestroyCached(Texture2D texture)
        { if (texture != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(texture); else UnityEngine.Object.DestroyImmediate(texture); } }
    }
}

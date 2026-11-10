using System;
using System.Collections.Generic;
using G10.Prototype.Computer;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class CreatureArtworkCompositorTests
    {
        private static readonly Rect SubjectBox = new(.28f, .18f, .32f, .64f);
        private readonly List<Texture2D> textures = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (var texture in textures) Object.DestroyImmediate(texture);
            textures.Clear();
        }

        [TestCase(80, 160, 0)]
        [TestCase(80, 160, 1)]
        [TestCase(80, 160, 2)]
        [TestCase(240, 80, 0)]
        [TestCase(240, 80, 1)]
        [TestCase(240, 80, 2)]
        public void NativeArtworkChangesPixelsWithoutChangingLegacyCoverageOrForegroundOcclusion(int width, int height, int pattern)
        {
            var legacy = MakeTexture(13, 19, (x, y) =>
            {
                int distance = Math.Abs(x - 6) + Math.Abs(y - 9);
                byte alpha = distance < 5 ? (byte)255 : distance < 8 ? (byte)85 : (byte)0;
                return new Color32(20, 80, 190, alpha);
            });
            var artwork = MakeArtwork(width, height, pattern);
            var occluder = MakeTexture(3, 3, (_, _) => new Color32(30, 45, 50, 255));

            var baseline = Compose(legacy, null, false, occluder, out float oldCoverage, out float oldOcclusion);
            var replacement = Compose(artwork, legacy, true, occluder, out float newCoverage, out float newOcclusion);

            Assert.That(oldCoverage, Is.GreaterThan(0));
            Assert.That(oldOcclusion, Is.GreaterThan(0).And.LessThan(1));
            Assert.That(newCoverage, Is.EqualTo(oldCoverage), "Artwork alpha, glow and padding must not change the survey footprint.");
            Assert.That(newOcclusion, Is.EqualTo(oldOcclusion), "Foreground terrain must keep covering the original footprint.");

            var oldPixels = baseline.GetPixels32();
            var newPixels = replacement.GetPixels32();
            int changed = 0;
            for (int i = 0; i < oldPixels.Length; i++)
                if (!oldPixels[i].Equals(newPixels[i])) changed++;
            Assert.That(changed, Is.GreaterThan(100), "The measurement mask must not replace the new visible illustration.");
        }

        [TestCase(80, 160)]
        [TestCase(240, 80)]
        [TestCase(640, 360)]
        public void AspectFitUsesPhotoPixelDimensionsAndStaysCenteredInsideItsBox(int width, int height)
        {
            var artwork = MakeArtwork(width, height, 0);
            var fitted = PhotoLayerComposer.FitAspect(artwork, SubjectBox);
            float pixelAspect = fitted.width * PhotoLayerComposer.Width / (fitted.height * PhotoLayerComposer.Height);

            Assert.That(pixelAspect, Is.EqualTo((float)width / height).Within(.00001f));
            Assert.That(fitted.center.x, Is.EqualTo(SubjectBox.center.x).Within(.000001f));
            Assert.That(fitted.center.y, Is.EqualTo(SubjectBox.center.y).Within(.000001f));
            Assert.That(fitted.xMin, Is.GreaterThanOrEqualTo(SubjectBox.xMin - .000001f));
            Assert.That(fitted.xMax, Is.LessThanOrEqualTo(SubjectBox.xMax + .000001f));
            Assert.That(fitted.yMin, Is.GreaterThanOrEqualTo(SubjectBox.yMin - .000001f));
            Assert.That(fitted.yMax, Is.LessThanOrEqualTo(SubjectBox.yMax + .000001f));
        }

        [Test]
        public void DefaultDrawStillMeasuresItsOwnAlphaIndependentlyOfVisualOpacity()
        {
            var opaque = MakeTexture(16, 16, (_, _) => new Color32(200, 100, 40, 255));
            var sparse = MakeTexture(16, 16, (x, y) => new Color32(200, 100, 40,
                x >= 6 && x < 10 && y >= 6 && y < 10 ? (byte)255 : (byte)0));
            var transparent = MakeTexture(16, 16, (_, _) => new Color32(200, 100, 40, 0));
            float opaqueCoverage = DefaultCoverage(opaque, 1);

            Assert.That(DefaultCoverage(opaque, 0), Is.EqualTo(opaqueCoverage));
            Assert.That(DefaultCoverage(sparse, 1), Is.GreaterThan(0).And.LessThan(opaqueCoverage / 2));
            Assert.That(DefaultCoverage(transparent, 1), Is.Zero);
        }

        [Test]
        public void ForegroundAlphaStillAccumulatesOverStableMask()
        {
            var legacy = MakeTexture(5, 9, (_, _) => new Color32(30, 90, 180, 85));
            var artwork = MakeArtwork(240, 80, 1);
            var foreground = MakeTexture(2, 2, (_, _) => new Color32(0, 0, 0, 255));
            var composer = new PhotoLayerComposer();
            composer.Begin();
            composer.Draw(artwork, SubjectBox, .2f, isSubject: true, measurementMask: legacy, preserveAspect: true);
            var clear = composer.Finish(out float clearCoverage, out float clearOcclusion);
            textures.Add(clear);
            composer.Draw(foreground, SubjectBox, .5f, occludes: true);
            composer.Draw(foreground, SubjectBox, .5f, occludes: true);
            var covered = composer.Finish(out float coveredCoverage, out float coveredOcclusion);
            textures.Add(covered);

            Assert.That(clearOcclusion, Is.Zero);
            Assert.That(coveredCoverage, Is.EqualTo(clearCoverage));
            Assert.That(coveredOcclusion, Is.EqualTo(.75f).Within(.001f));
        }

        private Texture2D Compose(Texture2D artwork, Texture2D mask, bool preserveAspect, Texture2D occluder,
            out float coverage, out float occlusion)
        {
            var composer = new PhotoLayerComposer();
            composer.Begin();
            composer.Draw(artwork, SubjectBox, .65f, isSubject: true, tint: new Color(.8f, .9f, .7f, .6f),
                measurementMask: mask, preserveAspect: preserveAspect);
            composer.Draw(occluder, new Rect(SubjectBox.center.x, SubjectBox.y, SubjectBox.width / 2, SubjectBox.height),
                .55f, occludes: true);
            var image = composer.Finish(out coverage, out occlusion);
            textures.Add(image);
            return image;
        }

        private float DefaultCoverage(Texture2D artwork, float opacity)
        {
            var composer = new PhotoLayerComposer();
            composer.Begin();
            composer.Draw(artwork, SubjectBox, opacity, isSubject: true);
            textures.Add(composer.Finish(out float coverage, out _));
            return coverage;
        }

        private Texture2D MakeArtwork(int width, int height, int pattern)
            => MakeTexture(width, height, (x, y) =>
            {
                float dx = (x + .5f) / width - .5f;
                float dy = (y + .5f) / height - .5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                byte alpha = pattern == 0 ? (byte)255 : pattern == 1
                    ? (radius < .14f ? (byte)255 : (byte)0)
                    : (byte)Mathf.RoundToInt(Mathf.Clamp01(1 - radius / .7f) * 255);
                return new Color32(240, 140, 35, alpha);
            });

        private Texture2D MakeTexture(int width, int height, Func<int, int, Color32> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var colors = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) colors[y * width + x] = pixel(x, y);
            texture.SetPixels32(colors);
            texture.Apply();
            textures.Add(texture);
            return texture;
        }
    }
}

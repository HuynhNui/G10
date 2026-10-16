using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using G10.Prototype.Tutorial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class TutorialSpotlightPlayModeTests
    {
        private GameObject owner, cameraOwner, eventOwner, sourceOwner;
        private Canvas canvas;
        private RectTransform target;
        private UnityEngine.UI.Image targetImage, background;
        private UnityEngine.UI.Button button;
        private TutorialSpotlight spotlight;
        private static readonly MethodInfo Populate = typeof(TutorialSpotlight).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(UnityEngine.UI.VertexHelper) }, null);

        [UnitySetUp] public IEnumerator Setup()
        {
            owner = new GameObject("Spotlight test canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = owner.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = owner.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            background = MakeImage("Background", owner.transform);
            Stretch(background.rectTransform);
            targetImage = MakeImage("Required control", owner.transform);
            target = targetImage.rectTransform;
            target.anchorMin = target.anchorMax = new Vector2(.62f, .57f);
            target.sizeDelta = new Vector2(200, 90);
            targetImage.color = new Color(.65f, .9f, .75f, 1);
            button = target.gameObject.AddComponent<UnityEngine.UI.Button>();
            spotlight = TutorialSpotlight.Create(owner.transform);
            if (EventSystem.current == null) eventOwner = new GameObject("Spotlight test events", typeof(EventSystem));
            Canvas.ForceUpdateCanvases(); yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(owner);
            if (cameraOwner != null) Object.Destroy(cameraOwner);
            if (eventOwner != null) Object.Destroy(eventOwner);
            if (sourceOwner != null) Object.Destroy(sourceOwner);
            yield return null;
        }

        [UnityTest] public IEnumerator OpeningHasNoPaintAndOutsideIsExactlyFourNonOverlappingQuads()
        {
            Color original = targetImage.color;
            spotlight.SetTarget(target); Canvas.ForceUpdateCanvases(); yield return null;
            Assert.That(spotlight.IsVisible, Is.True);
            Assert.That(spotlight.Target, Is.SameAs(target));
            Assert.That(spotlight.raycastTarget, Is.False);
            Assert.That(spotlight.color.a, Is.GreaterThan(.2f).And.LessThan(.5f));
            Assert.That(targetImage.color, Is.EqualTo(original), "The original control must keep its brightness.");
            AssertScreenPadding();
            var mesh = ReadMesh();
            try
            {
                Assert.That(mesh.vertexCount, Is.EqualTo(16));
                Assert.That(mesh.triangles.Length, Is.EqualTo(24));
                Vector3[] vertices = mesh.vertices; int[] triangles = mesh.triangles;
                float painted = 0;
                Rect hole = spotlight.Aperture;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                    bool outside = Mathf.Max(a.x, Mathf.Max(b.x, c.x)) <= hole.xMin || Mathf.Min(a.x, Mathf.Min(b.x, c.x)) >= hole.xMax ||
                        Mathf.Max(a.y, Mathf.Max(b.y, c.y)) <= hole.yMin || Mathf.Min(a.y, Mathf.Min(b.y, c.y)) >= hole.yMax;
                    Assert.That(outside, Is.True, "No dimmer triangle may cross the aperture.");
                    painted += Vector3.Cross(b - a, c - a).magnitude * .5f;
                }
                Rect full = spotlight.rectTransform.rect;
                float expected = full.width * full.height - hole.width * hole.height;
                Assert.That(painted, Is.EqualTo(expected).Within(expected * .00001f), "No overlapping panels or uncovered seams outside the aperture.");
            }
            finally { Object.Destroy(mesh); }
        }

        [UnityTest] public IEnumerator PositionSizeRotationAndCanvasScalingKeepSixPixelPadding()
        {
            spotlight.SetTarget(target);
            var scaler = owner.GetComponent<UnityEngine.UI.CanvasScaler>();
            foreach (var resolution in new[] { new Vector2(1280, 720), new Vector2(1920, 1080), new Vector2(1024, 1280) })
            {
                scaler.referenceResolution = resolution;
                scaler.matchWidthOrHeight = resolution.x > resolution.y ? 0 : 1;
                target.anchoredPosition += new Vector2(-12, 9);
                target.sizeDelta += new Vector2(15, 8);
                target.localEulerAngles = new Vector3(0, 0, 17);
                yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(spotlight.IsVisible, Is.True);
                AssertScreenPadding();
            }
        }

        [UnityTest] public IEnumerator ScreenCameraTargetsProjectIntoOverlayAndCameraCanvases()
        {
            cameraOwner = new GameObject("Spotlight canvas camera", typeof(Camera));
            var camera = cameraOwner.GetComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 5;
            camera.transform.position = new Vector3(0, 0, -10);
            sourceOwner = new GameObject("Source camera canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            var source = sourceOwner.GetComponent<Canvas>();
            source.renderMode = RenderMode.ScreenSpaceCamera; source.worldCamera = camera; source.planeDistance = 5;
            target.SetParent(source.transform, false);
            target.anchorMin = target.anchorMax = new Vector2(.35f, .6f);
            target.sizeDelta = new Vector2(140, 60);
            spotlight.SetTarget(target);
            yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(spotlight.IsVisible, Is.True);
            AssertScreenPadding();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 4;
            camera.rect = new Rect(.1f, .08f, .8f, .84f);
            yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(spotlight.IsVisible, Is.True);
            AssertScreenPadding();
            source.enabled = false;
            yield return null;
            Assert.That(spotlight.IsVisible, Is.False);
        }

        [UnityTest] public IEnumerator HighlightAndDarkenedRegionBothAllowTheirExistingPointerInteractions()
        {
            spotlight.SetTarget(target); Canvas.ForceUpdateCanvases(); yield return null;
            var raycaster = owner.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            var results = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, target.position), button = PointerEventData.InputButton.Left };
            raycaster.Raycast(pointer, results);
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results[0].gameObject, Is.SameAs(target.gameObject));
            Assert.That(results.Exists(hit => hit.gameObject == spotlight.gameObject), Is.False);
            int clicks = 0; button.onClick.AddListener(() => clicks++);
            ExecuteEvents.Execute(results[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(clicks, Is.EqualTo(1));
            results.Clear(); pointer.position = new Vector2(Screen.width * .05f, Screen.height * .05f);
            raycaster.Raycast(pointer, results);
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results[0].gameObject, Is.SameAs(background.gameObject), "Dimmed controls retain their existing interaction rules.");
        }

        [UnityTest] public IEnumerator HiddenOffscreenDestroyedTargetsTransitionsAndDisableRemoveTheOverlay()
        {
            spotlight.SetTarget(target); yield return null;
            Assert.That(spotlight.IsVisible, Is.True);
            target.gameObject.SetActive(false); yield return null;
            AssertHidden();
            target.gameObject.SetActive(true); yield return null;
            Assert.That(spotlight.IsVisible, Is.True);
            target.anchoredPosition = new Vector2(Screen.width * 10, 0); yield return null;
            AssertHidden();
            target.anchorMin = target.anchorMax = new Vector2(1, .5f); target.anchoredPosition = Vector2.zero;
            yield return null;
            Assert.That(spotlight.IsVisible, Is.True);
            Assert.That(spotlight.Aperture.xMax, Is.EqualTo(spotlight.rectTransform.rect.xMax).Within(.01f));
            spotlight.SetTarget(background.rectTransform);
            Assert.That(spotlight.Target, Is.SameAs(background.rectTransform));
            spotlight.Hide(); AssertHidden(); Assert.That(spotlight.Target, Is.Null);
            spotlight.SetTarget(target); spotlight.enabled = false;
            AssertHidden(); Assert.That(spotlight.Target, Is.Null);
            spotlight.enabled = true; yield return null; AssertHidden();
            spotlight.SetTarget(target); Object.Destroy(target.gameObject); yield return null;
            AssertHidden();
        }

        private void AssertHidden()
        {
            Assert.That(spotlight.IsVisible, Is.False);
            Assert.That(spotlight.Aperture, Is.EqualTo(default(Rect)));
            var mesh = ReadMesh();
            try { Assert.That(mesh.vertexCount, Is.Zero); }
            finally { Object.Destroy(mesh); }
        }

        private Mesh ReadMesh()
        {
            using var helper = new UnityEngine.UI.VertexHelper();
            Populate.Invoke(spotlight, new object[] { helper });
            var mesh = new Mesh(); helper.FillMesh(mesh); return mesh;
        }

        private void AssertScreenPadding()
        {
            Rect expected = ScreenBounds(target), full = ScreenBounds(spotlight.rectTransform);
            expected = Rect.MinMaxRect(Mathf.Max(full.xMin, expected.xMin - 6), Mathf.Max(full.yMin, expected.yMin - 6),
                Mathf.Min(full.xMax, expected.xMax + 6), Mathf.Min(full.yMax, expected.yMax + 6));
            Rect hole = spotlight.Aperture;
            var rect = spotlight.rectTransform; var camera = RenderCamera(rect);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(hole.xMin, hole.yMin)));
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(hole.xMax, hole.yMax)));
            Assert.That(min.x, Is.EqualTo(expected.xMin).Within(.1f));
            Assert.That(min.y, Is.EqualTo(expected.yMin).Within(.1f));
            Assert.That(max.x, Is.EqualTo(expected.xMax).Within(.1f));
            Assert.That(max.y, Is.EqualTo(expected.yMax).Within(.1f));
        }

        private static Rect ScreenBounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity), max = new(float.NegativeInfinity, float.NegativeInfinity);
            var camera = RenderCamera(rect);
            foreach (var corner in corners)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, screen); max = Vector2.Max(max, screen);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Camera RenderCamera(RectTransform rect)
        {
            var root = rect.GetComponentInParent<Canvas>().rootCanvas;
            return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }

        private static UnityEngine.UI.Image MakeImage(string name, Transform parent)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); owner.transform.SetParent(parent, false);
            return owner.GetComponent<UnityEngine.UI.Image>();
        }

        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}

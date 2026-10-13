using System.Collections;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class HelmMapPreviewPlayModeTests
    {
        private LegacyCabinTestSession session;
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private HelmMapPreview Preview => Cabin.NavigationPanel.GetComponentInChildren<HelmMapPreview>(true);

        [UnitySetUp] public IEnumerator Setup()
        {
            session = new LegacyCabinTestSession();
            yield return session.Begin();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            PauseMenuController.Instance?.Resume();
            if (session != null) yield return session.End();
        }

        [UnityTest] public IEnumerator PreviewIsArtOnlyFitsPocketAndReceivesPointerWithoutOverlays()
        {
            Cabin.OpenNavigation(); yield return null;
            Assert.That(Preview, Is.Not.Null);
            Assert.That(Preview.GetComponentsInChildren<PhotoSurveyMap>(true), Is.Empty);
            Assert.That(Preview.GetComponentsInChildren<ZoneMapPresentation>(true), Is.Empty);
            Assert.That(Preview.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Length, Is.EqualTo(1));
            var root = (RectTransform)Preview.transform;
            Assert.That(root.sizeDelta, Is.EqualTo(new Vector2(276, 218)));
            Assert.That(root.anchoredPosition, Is.EqualTo(new Vector2(1063, -811.5f)));
            Assert.That(Mathf.DeltaAngle(0, root.localEulerAngles.z), Is.EqualTo(1f).Within(.001f));
            foreach (Transform layer in root)
                Assert.That(Quaternion.Angle(layer.localRotation, Quaternion.identity), Is.LessThan(.001f),
                    layer.name + " must inherit the pocket angle rather than rotate independently.");
            Assert.That(Preview.GetComponent<UnityEngine.UI.Image>().color.a, Is.EqualTo(1), "Hide artwork's painted grid.");
            var image = Preview.MapImage;
            Assert.That(image.uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
            Assert.That(image.texture, Is.SameAs(Cabin.MapPanel.GetComponent<ZoneMapPresentation>().config.map));
            Assert.That(image.rectTransform.rect.width / image.rectTransform.rect.height,
                Is.EqualTo((float)image.texture.width / image.texture.height).Within(.01f));
            Assert.That(image.rectTransform.rect.width, Is.LessThanOrEqualTo(root.rect.width + .01f));
            Assert.That(image.rectTransform.rect.height, Is.LessThanOrEqualTo(root.rect.height + .01f));
            foreach (var graphic in Preview.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.EqualTo(graphic.gameObject == Preview.gameObject), graphic.name);

            // Use the real EventSystem raycast, not just a programmatic button invocation.
            Canvas.ForceUpdateCanvases();
            var canvas = Preview.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, root.TransformPoint(root.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject, Is.SameAs(Preview.gameObject));
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "helm-preview-aligned-1920.png");
            yield return CabinNavigationPlayModeTests.CaptureArt(Cabin, "helm-preview-aligned-1366.png", 1366, 768);
            ExecuteEvents.Execute(Preview.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(Cabin.Panels.CurrentPanel, Is.SameAs(Cabin.MapPanel));
            Assert.That(Cabin.MapPanel.GetComponent<ZoneMapPresentation>().overlay, Is.Not.Null, "Full chart still owns its grid and objectives.");
        }

        [UnityTest] public IEnumerator ReopeningPreviewKeepsCurrentZoneDespiteHistoricalAndWorldSelection()
        {
            var world = Cabin.GetComponent<WorldMapController>();
            string path = ExpeditionSaveStore.SavePath, before = File.ReadAllText(path);
            var position = Cabin.Navigation.Position;
            for (int repeat = 0; repeat < 3; repeat++)
            {
                world.OpenZone(2); world.OpenWorld();
                Cabin.OpenNavigation(); yield return null;
                Assert.That(Preview.MapImage.texture, Is.SameAs(Cabin.MapPanel.GetComponent<ZoneMapPresentation>().config.map));
                Preview.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.That(Cabin.Panels.CurrentPanel, Is.SameAs(Cabin.MapPanel));
                Assert.That(world.LastZoneIndex, Is.Zero);
            }
            Assert.That(Cabin.Navigation.Position, Is.EqualTo(position));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before), "Art-only preview must not write timeline state.");
        }

        [UnityTest] public IEnumerator StalePreviewCallbackCannotReplacePauseModalOrLockedPanel()
        {
            Cabin.OpenNavigation(); yield return null;
            var button = Preview.GetComponent<UnityEngine.UI.Button>();
            var panel = Cabin.Panels.CurrentPanel;
            Cabin.OpenPause(); Assert.That(PauseMenuController.Instance.IsPaused, Is.True);
            button.onClick.Invoke(); Assert.That(Cabin.Panels.CurrentPanel, Is.SameAs(panel));
            PauseMenuController.Instance.Resume();
            Cabin.Panels.LockedPanel = panel;
            button.onClick.Invoke(); Assert.That(Cabin.Panels.CurrentPanel, Is.SameAs(panel));
            Cabin.Panels.LockedPanel = null;
            var modal = new GameObject("Preview test modal");
            try
            {
                Assert.That(Cabin.Panels.TryOpenModal(modal), Is.True);
                Cabin.OpenCurrentMap(); Assert.That(Cabin.Panels.CurrentPanel, Is.SameAs(modal));
                Cabin.Panels.EndModal(modal);
                yield return null;
                button.onClick.Invoke(); Assert.That(Cabin.Panels.CurrentPanel, Is.SameAs(Cabin.MapPanel));
            }
            finally { Cabin.Panels.EndModal(modal); Object.Destroy(modal); }
        }
    }
}

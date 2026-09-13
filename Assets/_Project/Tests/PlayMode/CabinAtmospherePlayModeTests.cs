using System.Collections;
using System.Collections.Generic;
using System.Linq;
using G10.Prototype.Atmosphere;
using G10.Prototype.Core;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace G10.Prototype.Tests
{
    public sealed class CabinAtmospherePlayModeTests
    {
        [UnityTest]
        public IEnumerator LivingCabinKeepsHotspotsAlignedAndPanelsStable()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            var mood = cabin.GetComponentInChildren<CabinAtmosphere>();
            Assert.That(mood, Is.Not.Null);
            var bob = mood.GetComponentInChildren<CabinBob>();
            var rig = mood.GetComponentInChildren<CameraBreathing>();
            var art = mood.GetComponentsInChildren<RawImage>().Single(image => image.name == "CabinArt");
            Assert.That(art.texture, Is.SameAs(cabin.CabinArt));
            Assert.That(art.transform.IsChildOf(bob.transform), Is.True);
            foreach (Graphic graphic in mood.GetComponentsInChildren<Graphic>())
                if (graphic.GetComponent<Button>() == null) Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            yield return new WaitForSeconds(0.4f);
            Assert.That(bob.transform.localPosition.magnitude, Is.LessThan(6));
            Assert.That(Quaternion.Angle(Quaternion.identity, bob.transform.localRotation), Is.LessThan(0.5));
            var monitor = mood.GetComponentsInChildren<Button>().Single(button => button.name == "MonitorHotspot");
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)monitor.transform;
            var pointer = new PointerEventData(EventSystem.current)
            { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.EqualTo(monitor.gameObject));
            monitor.onClick.Invoke(); yield return null;
            Assert.That(cabin.Panels.IsPanelOpen, Is.True);
            Assert.That(mood.presentation.alpha, Is.Zero);
            Assert.That(cabin.Panels.CurrentPanel.transform.IsChildOf(rig.transform), Is.False);
            cabin.ClosePanel(); yield return null;
            Assert.That(mood.presentation.alpha, Is.EqualTo(1));
            float previousGlow = mood.Glow;
            mood.SetMood(CabinMood.LowPower); yield return new WaitForSeconds(0.3f);
            Assert.That(mood.Glow, Is.LessThan(previousGlow));
            mood.SetMood(CabinMood.Normal); cabin.Scan(); yield return null;
            Assert.That(mood.EffectiveMood, Is.EqualTo(CabinMood.ScanActive));
            foreach (ParticleSystem particles in mood.GetComponentsInChildren<ParticleSystem>())
            {
                Assert.That(particles.main.maxParticles, Is.LessThanOrEqualTo(40));
                Assert.That(particles.particleCount, Is.GreaterThan(0), particles.name + " must actually simulate particles");
                Assert.That(particles.GetComponent<UIParticleAtmosphere>(), Is.Not.Null);
            }
            bob.enabled = false;
            Assert.That(bob.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(bob.transform.localRotation, Is.EqualTo(Quaternion.identity));
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (SceneFlowController.Instance != null)
            { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
        }
    }
}

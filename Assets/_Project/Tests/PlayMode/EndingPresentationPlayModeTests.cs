using System.Collections;
using G10.Prototype.UI;
using G10.Prototype.Feedback;
using G10.Prototype.Computer;
using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class EndingPresentationPlayModeTests
    {
        [UnityTest] public IEnumerator PausingTheHiddenEndingSeeksItsProductionClockWithoutReplayingTheSignalOrWritingASave()
        {
            string oldPath = ExpeditionSaveStore.PathOverride;
            string isolated = Path.Combine(Application.temporaryCachePath, "EndingPreview-" + Guid.NewGuid().ToString("N"), "timeline.json");
            ExpeditionSaveStore.PathOverride = isolated;
            try
            {
                yield return SceneManager.LoadSceneAsync("Ending", LoadSceneMode.Single);
                var presenter = UnityEngine.Object.FindAnyObjectByType<EndingPresentation>(); presenter.Show("hidden");
                float deadline = Time.realtimeSinceStartup + 5;
                while (presenter.Elapsed < 2.6f && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(presenter.Elapsed, Is.GreaterThanOrEqualTo(2.6f));
                var executor = presenter.GetComponent<EffectAudioExecutor>();
                var signal = executor.GetComponentsInChildren<AudioSource>(true).Single(s => s.isPlaying && s.clip != null && s.clip.name == "final_signal");
                float oldTime = signal.time, pausedAt = presenter.Elapsed;
                Time.timeScale = 0; yield return new WaitForSecondsRealtime(.3f);
                Assert.That(presenter.Elapsed, Is.EqualTo(pausedAt).Within(.08f));
                Assert.That(executor.GetComponentsInChildren<AudioSource>(true).Any(s => s.isPlaying), Is.False);
                Time.timeScale = 1; yield return null; yield return null; yield return null;
                Assert.That(presenter.IsPlaying, Is.True); Assert.That(presenter.Elapsed, Is.LessThan(pausedAt + .4f));
                signal = executor.GetComponentsInChildren<AudioSource>(true).Single(s => s.isPlaying && s.clip != null && s.clip.name == "final_signal");
                Assert.That(signal.time, Is.GreaterThanOrEqualTo(oldTime - .08f), "Resume seeks the original cue instead of replaying its attack.");
                presenter.Show("normal"); yield return null;
                Assert.That(executor.GetComponentsInChildren<AudioSource>(true).Any(s => s.isPlaying && s.clip != null && s.clip.name == "final_signal"), Is.False);
                Assert.That(executor.GetComponentsInChildren<AudioSource>(true).Length, Is.LessThanOrEqualTo(16));
                Assert.That(File.Exists(isolated), Is.False, "Ending preview never publishes mission or ending state.");
            }
            finally { Time.timeScale = 1; ExpeditionSaveStore.PathOverride = oldPath; }
        }
        [UnityTest]
        public IEnumerator EndingVariantsKeepAuthoredArtworkAndMenuButton()
        {
            yield return SceneManager.LoadSceneAsync("Ending", LoadSceneMode.Single);
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var background = canvas.transform.Find("Background");
            var menu = canvas.transform.Find("MainMenuButton");
            var title = canvas.transform.Find("Title").GetComponent<UnityEngine.UI.Text>();
            Assert.That(background, Is.Not.Null);
            Assert.That(menu, Is.Not.Null);
            Vector2 titlePosition = title.rectTransform.anchoredPosition;
            var presenter = canvas.GetComponent<EndingPresentation>();
            if (presenter == null) presenter = canvas.gameObject.AddComponent<EndingPresentation>();
            presenter.Show("normal");
            Assert.That(title.text, Is.EqualTo("EXPEDITION COMPLETE"));
            Assert.That(presenter.EndingId, Is.EqualTo("normal"));
            var summary = canvas.transform.Find("EndingSummary").GetComponent<TMP_Text>();
            Assert.That(summary.text, Does.Contain("Khảo sát sinh học"));
            Assert.That(presenter.IsPlaying, Is.True);
            Assert.That(presenter.normalSequence.Duration, Is.EqualTo(12));
            presenter.Show("hidden");
            Assert.That(title.text, Is.EqualTo("HIDDEN ENDING"));
            Assert.That(summary.text, Does.Contain("Tín hiệu vẫn tiếp diễn"));
            Assert.That(presenter.hiddenSequence.Duration, Is.EqualTo(16));
            var veil = canvas.transform.Find("EndingDepthVeil").GetComponent<UnityEngine.UI.RawImage>();
            Assert.That(veil.raycastTarget, Is.False);
            Assert.That(veil.rectTransform.sizeDelta.x, Is.LessThan(((RectTransform)canvas.transform).rect.width));
            Assert.That(veil.rectTransform.sizeDelta.y, Is.LessThan(((RectTransform)canvas.transform).rect.height));
            Assert.That(veil.texture.width, Is.EqualTo(96));
            Assert.That(canvas.transform.Find("EndingSummary").GetComponent<TMP_Text>(), Is.SameAs(summary));
            Assert.That(canvas.transform.Find("Background"), Is.SameAs(background));
            Assert.That(canvas.transform.Find("MainMenuButton"), Is.SameAs(menu));
            Assert.That(title.rectTransform.anchoredPosition, Is.EqualTo(titlePosition));
            Assert.That(menu.GetComponent<UnityEngine.UI.Button>().interactable, Is.True);
            yield return null;
        }
    }
}

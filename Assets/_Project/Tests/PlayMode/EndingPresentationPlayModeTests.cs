using System.Collections;
using G10.Prototype.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class EndingPresentationPlayModeTests
    {
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
            Assert.That(summary.text, Does.Contain("Normal ending"));
            presenter.Show("hidden");
            Assert.That(title.text, Is.EqualTo("HIDDEN ENDING"));
            Assert.That(summary.text, Does.Contain("hidden locations"));
            Assert.That(canvas.transform.Find("EndingSummary").GetComponent<TMP_Text>(), Is.SameAs(summary));
            Assert.That(canvas.transform.Find("Background"), Is.SameAs(background));
            Assert.That(canvas.transform.Find("MainMenuButton"), Is.SameAs(menu));
            Assert.That(title.rectTransform.anchoredPosition, Is.EqualTo(titlePosition));
            Assert.That(menu.GetComponent<UnityEngine.UI.Button>().interactable, Is.True);
            yield return null;
        }
    }
}

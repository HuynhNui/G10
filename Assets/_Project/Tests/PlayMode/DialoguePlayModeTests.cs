using System.Collections;
using G10.Prototype.Dialogue;
using G10.Prototype.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace G10.Prototype.Tests
{
    public sealed class DialoguePlayModeTests
    {
        private DialogueController dialogue;
        private UIManager panels;
        private GameObject previousPanel;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return null;
            panels = Object.FindAnyObjectByType<UIManager>();
            dialogue = panels.GetComponent<DialogueController>();
            Assert.That(dialogue, Is.Not.Null, "Install the dialogue framework in GameplayCore.");
            Assert.That(dialogue.IsActive, Is.False, "Installing a framework must not introduce unsolicited story content.");
            Assert.That(dialogue.View.gameObject.activeSelf, Is.False);
            previousPanel = new GameObject("DialogueTestPreviousPanel", typeof(RectTransform));
            previousPanel.transform.SetParent(panels.transform, false);
            panels.OpenPanel(previousPanel);
        }

        private static DialogueLine[] Lines() => new[] {
            new DialogueLine(DialogueSpeakerKind.Story, "NGÀY 1", "Biển yên lặng."),
            new DialogueLine(DialogueSpeakerKind.Character, "Người lái", "Tôi đã sẵn sàng."),
            new DialogueLine(DialogueSpeakerKind.Guide, "HƯỚNG DẪN", "Kiểm tra kho trước khi đi.")
        };

        [UnityTest]
        public IEnumerator EmptyOrBusySessionsDoNotReplaceExistingPanels()
        {
            Assert.That(dialogue.TryBegin(new DialogueLine[0]), Is.False);
            Assert.That(dialogue.TryBegin(new[] { new DialogueLine(DialogueSpeakerKind.Story, "", " ") }), Is.False);
            Assert.That(panels.CurrentPanel, Is.EqualTo(previousPanel));
            var anotherModal = new GameObject("OtherModal", typeof(RectTransform));
            anotherModal.transform.SetParent(panels.transform, false);
            Assert.That(panels.TryOpenModal(anotherModal), Is.True);
            Assert.That(dialogue.TryBegin(Lines()), Is.False);
            Assert.That(panels.CurrentPanel, Is.EqualTo(anotherModal));
            panels.EndModal(anotherModal);
            Object.Destroy(anotherModal);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NextRevealsBeforeAdvancingAndCompletionRestoresPreviousPanelOnce()
        {
            int callbacks = 0;
            DialogueEndReason reason = DialogueEndReason.Cancelled;
            Assert.That(dialogue.TryBegin(Lines(), value => { callbacks++; reason = value; }), Is.True);
            Assert.That(panels.IsModalOpen, Is.True);
            Assert.That(dialogue.IsTyping, Is.True);
            Assert.That(previousPanel.activeSelf, Is.False);
            Assert.That(dialogue.TryBegin(Lines()), Is.False);
            panels.CloseCurrentPanel();
            Assert.That(panels.CurrentPanel, Is.EqualTo(dialogue.View.gameObject));
            var button = dialogue.View.transform.Find("PanelBackground/Controls/NextButton").GetComponent<Button>();
            button.onClick.Invoke();
            Assert.That(dialogue.IsTyping, Is.False);
            Assert.That(dialogue.CurrentLineIndex, Is.Zero);
            Assert.That(dialogue.History.Count, Is.EqualTo(1));
            button.onClick.Invoke();
            Assert.That(dialogue.CurrentLineIndex, Is.EqualTo(1));
            var speaker = dialogue.View.transform.Find("PanelBackground/SpeakerTab/SpeakerText").GetComponent<TMP_Text>();
            Assert.That(speaker.text, Is.EqualTo("Người lái"));
            var tab = dialogue.View.transform.Find("PanelBackground/SpeakerTab").GetComponent<RawImage>();
            Assert.That(tab.texture.name, Is.EqualTo("Tab_Character"));
            button.onClick.Invoke(); button.onClick.Invoke();
            Assert.That(tab.texture.name, Is.EqualTo("Tab_Guide"));
            button.onClick.Invoke(); button.onClick.Invoke();
            dialogue.Cancel(); dialogue.Skip(); dialogue.Next();
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(reason, Is.EqualTo(DialogueEndReason.Completed));
            Assert.That(dialogue.History.Count, Is.EqualTo(3));
            Assert.That(panels.IsModalOpen, Is.False);
            Assert.That(panels.CurrentPanel, Is.EqualTo(previousPanel));
            Assert.That(previousPanel.activeSelf, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogSuspendsAutoAndBackClosesLogBeforeCancellingDialogue()
        {
            Assert.That(dialogue.TryBegin(Lines()), Is.True);
            dialogue.ToggleAuto();
            dialogue.ToggleLog();
            Assert.That(dialogue.History.Count, Is.EqualTo(1));
            Assert.That(dialogue.View.IsLogOpen, Is.True);
            dialogue.Tick(30);
            Assert.That(dialogue.CurrentLineIndex, Is.Zero);
            var history = dialogue.View.transform.Find("DialogueLog/History/Viewport/Text").GetComponent<TMP_Text>();
            Assert.That(history.text, Does.Contain("Biển yên lặng."));
            Assert.That(history.text, Does.Not.Contain("Tôi đã sẵn sàng."));
            Assert.That(dialogue.View.TryHandleBack(), Is.True);
            Assert.That(dialogue.IsActive, Is.True);
            dialogue.Tick(2);
            Assert.That(dialogue.CurrentLineIndex, Is.EqualTo(1));
            dialogue.View.TryHandleBack();
            Assert.That(dialogue.IsActive, Is.False);
            Assert.That(panels.CurrentPanel, Is.EqualTo(previousPanel));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SkipDisableAndRepeatedOpenReleaseModalWithoutDuplicateListeners()
        {
            int callbacks = 0;
            DialogueEndReason result = DialogueEndReason.Completed;
            Assert.That(dialogue.TryBegin(Lines(), value => { callbacks++; result = value; }), Is.True);
            dialogue.Skip();
            Assert.That(result, Is.EqualTo(DialogueEndReason.Skipped));
            Assert.That(dialogue.History.Count, Is.Zero, "Skipping must not write unseen future lines to the history.");
            Assert.That(dialogue.TryBegin(Lines(), value => { callbacks++; result = value; }), Is.True);
            dialogue.View.gameObject.SetActive(false);
            Assert.That(result, Is.EqualTo(DialogueEndReason.Cancelled));
            Assert.That(panels.IsModalOpen, Is.False);
            Assert.That(callbacks, Is.EqualTo(2));
            Assert.That(dialogue.TryBegin(Lines()), Is.True);
            var button = dialogue.View.transform.Find("PanelBackground/Controls/NextButton").GetComponent<Button>();
            button.onClick.Invoke();
            Assert.That(dialogue.CurrentLineIndex, Is.Zero, "Reopening must not accumulate Next listeners.");
            Assert.That(dialogue.IsTyping, Is.False);
            dialogue.ToggleAuto();
            dialogue.Tick(2); dialogue.Tick(10); dialogue.Tick(2); dialogue.Tick(10); dialogue.Tick(2);
            Assert.That(dialogue.IsActive, Is.False);
            Assert.That(dialogue.History.Count, Is.EqualTo(3));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (dialogue != null) dialogue.Cancel();
            if (panels != null) panels.CloseCurrentPanel();
            if (previousPanel != null) Object.Destroy(previousPanel);
            yield return null;
        }
    }
}

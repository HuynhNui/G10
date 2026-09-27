using System;
using System.Collections.Generic;
using System.Text;
using G10.Prototype.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace G10.Prototype.Dialogue
{
    [Serializable]
    public sealed class DialogueTabArtwork
    {
        public Texture2D texture;
        public Rect uv = new(0, 0, 1, 1);
    }

    public sealed class DialogueView : MonoBehaviour, IPanelBackHandler
    {
        [SerializeField] private DialogueController controller;
        [SerializeField] private RawImage speakerTab;
        [SerializeField] private DialogueTabArtwork[] tabs = new DialogueTabArtwork[3];
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private ScrollRect dialogueScroll;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button logButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject logPanel;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private ScrollRect logScroll;
        [SerializeField] private Button closeLogButton;
        public bool IsLogOpen => logPanel != null && logPanel.activeSelf;
        public bool IsConfigured => controller != null && speakerText != null && dialogueText != null && speakerTab != null
            && nextButton != null && logButton != null && autoButton != null && skipButton != null
            && logPanel != null && logText != null && closeLogButton != null;

        private void OnEnable()
        {
            if (!IsConfigured) return;
            nextButton.onClick.AddListener(controller.Next);
            logButton.onClick.AddListener(controller.ToggleLog);
            autoButton.onClick.AddListener(controller.ToggleAuto);
            skipButton.onClick.AddListener(controller.Skip);
            closeLogButton.onClick.AddListener(controller.ToggleLog);
        }

        private void OnDisable()
        {
            if (controller == null) return;
            if (nextButton != null) nextButton.onClick.RemoveListener(controller.Next);
            if (logButton != null) logButton.onClick.RemoveListener(controller.ToggleLog);
            if (autoButton != null) autoButton.onClick.RemoveListener(controller.ToggleAuto);
            if (skipButton != null) skipButton.onClick.RemoveListener(controller.Skip);
            if (closeLogButton != null) closeLogButton.onClick.RemoveListener(controller.ToggleLog);
            controller.Cancel();
        }

        public void ResetSession()
        {
            logPanel.SetActive(false);
            nextButton.interactable = logButton.interactable = autoButton.interactable = skipButton.interactable = true;
            logText.text = string.Empty;
            SetAutoState(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
        }

        public int ShowLine(DialogueLine line)
        {
            int tabIndex = Mathf.Clamp((int)line.Kind, 0, 2);
            if (tabs != null && tabIndex < tabs.Length && tabs[tabIndex] != null)
            {
                speakerTab.texture = tabs[tabIndex].texture;
                speakerTab.uvRect = tabs[tabIndex].uv;
            }
            speakerText.text = line.Speaker;
            dialogueText.text = line.Text;
            dialogueText.maxVisibleCharacters = int.MaxValue;
            dialogueText.ForceMeshUpdate();
            int count = dialogueText.textInfo.characterCount;
            dialogueText.maxVisibleCharacters = 0;
            Canvas.ForceUpdateCanvases();
            if (dialogueScroll != null) dialogueScroll.verticalNormalizedPosition = 1;
            return count;
        }

        public void SetVisibleCharacters(int count) => dialogueText.maxVisibleCharacters = count;

        public void SetAutoState(bool enabled)
        {
            ColorBlock colors = autoButton.colors;
            colors.normalColor = enabled ? new Color(.70f, 1, .81f) : Color.white;
            autoButton.colors = colors;
        }

        public void SetLogOpen(bool open, IReadOnlyList<DialogueLine> history)
        {
            if (open)
            {
                var builder = new StringBuilder();
                for (int i = 0; i < history.Count; i++)
                {
                    if (!string.IsNullOrEmpty(history[i].Speaker)) builder.AppendLine(history[i].Speaker);
                    builder.AppendLine(history[i].Text).AppendLine();
                }
                logText.text = builder.ToString();
            }
            logPanel.SetActive(open);
            nextButton.interactable = logButton.interactable = autoButton.interactable = skipButton.interactable = !open;
            Canvas.ForceUpdateCanvases();
            if (open && logScroll != null) logScroll.verticalNormalizedPosition = 0;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(open ? closeLogButton.gameObject : nextButton.gameObject);
        }

        public bool TryHandleBack()
        {
            if (IsLogOpen) controller.ToggleLog();
            else controller.Cancel();
            return true;
        }
    }
}

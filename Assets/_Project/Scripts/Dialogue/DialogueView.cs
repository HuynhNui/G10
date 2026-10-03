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
        private UnityEngine.UI.Button firstChoiceButton;
        private UnityEngine.UI.Button secondChoiceButton;
        private TMP_Text firstChoiceLabel;
        private TMP_Text secondChoiceLabel;
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
            SetChoiceControls(false);
            logPanel.SetActive(false);
            nextButton.interactable = logButton.interactable = autoButton.interactable = skipButton.interactable = true;
            logText.text = string.Empty;
            SetAutoState(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
        }

        public void ShowChoices(string firstLabel, string secondLabel)
        {
            if (firstChoiceButton == null)
            {
                firstChoiceButton = CreateChoiceButton("FirstChoiceButton", 0, out firstChoiceLabel);
                secondChoiceButton = CreateChoiceButton("SecondChoiceButton", 1, out secondChoiceLabel);
                firstChoiceButton.navigation = new UnityEngine.UI.Navigation {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnRight = secondChoiceButton, selectOnDown = secondChoiceButton
                };
                secondChoiceButton.navigation = new UnityEngine.UI.Navigation {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnLeft = firstChoiceButton, selectOnUp = firstChoiceButton
                };
            }
            firstChoiceLabel.text = firstLabel;
            secondChoiceLabel.text = secondLabel;
            SetChoiceControls(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstChoiceButton.gameObject);
        }

        private UnityEngine.UI.Button CreateChoiceButton(string objectName, int index, out TMP_Text label)
        {
            // Keep the authored arrow artwork at its original aspect ratio and use the existing controls row.
            var root = new GameObject(objectName, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            root.transform.SetParent(nextButton.transform.parent, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = new(index * .5f, 0);
            rect.anchorMax = new((index + 1) * .5f, 1);
            rect.offsetMin = new(4, 0);
            rect.offsetMax = new(-4, 0);
            root.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            var button = root.GetComponent<UnityEngine.UI.Button>();
            button.colors = nextButton.colors;
            var arrow = Instantiate(nextButton.targetGraphic, root.transform, false);
            arrow.name = "Arrow";
            // Instantiating a component also clones its GameObject; only the parent handles selection/clicks.
            var clonedButton = arrow.GetComponent<UnityEngine.UI.Button>();
            if (clonedButton != null) { clonedButton.enabled = false; Destroy(clonedButton); }
            var clonedFeedback = arrow.GetComponent<DialogueButtonFeedback>();
            if (clonedFeedback != null) { clonedFeedback.enabled = false; Destroy(clonedFeedback); }
            arrow.raycastTarget = false;
            var arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = arrowRect.anchorMax = arrowRect.pivot = new(1, .5f);
            arrowRect.anchoredPosition = Vector2.zero;
            arrowRect.sizeDelta = new(66, 66);
            button.targetGraphic = arrow;
            root.AddComponent<DialogueButtonFeedback>();
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = dialogueText.font;
            label.fontSize = 28;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20;
            label.fontSizeMax = 28;
            label.color = dialogueText.color;
            label.alignment = TextAlignmentOptions.Midline;
            label.richText = false;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new(8, 4);
            label.rectTransform.offsetMax = new(-74, -4);
            button.onClick.AddListener(() => controller.SelectChoice(index));
            return button;
        }

        private void SetChoiceControls(bool choice)
        {
            nextButton.gameObject.SetActive(!choice);
            logButton.gameObject.SetActive(!choice);
            autoButton.gameObject.SetActive(!choice);
            skipButton.gameObject.SetActive(!choice);
            if (firstChoiceButton != null) firstChoiceButton.gameObject.SetActive(choice);
            if (secondChoiceButton != null) secondChoiceButton.gameObject.SetActive(choice);
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

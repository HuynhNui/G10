using System;
using System.Collections.Generic;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace G10.Prototype.Dialogue
{
    /// <summary>GameplayCore-owned dialogue session using the existing panel modal contract.</summary>
    [DisallowMultipleComponent]
    public sealed class DialogueController : MonoBehaviour
    {
        [SerializeField] private UIManager panels;
        [SerializeField] private DialogueView view;
        [SerializeField, Min(1)] private float charactersPerSecond = 38;
        [SerializeField, Min(.1f)] private float autoAdvanceDelay = 1.8f;
        private readonly List<DialogueLine> lines = new();
        private readonly List<DialogueLine> history = new();
        private Action<DialogueEndReason> completion;
        private Action<int> choiceCompletion;
        private GameObject previousSelection;
        private float visibleCharacters;
        private float autoTimer;
        private int characterCount;
        private bool lineRecorded;

        public bool IsActive { get; private set; }
        public bool IsChoiceActive { get; private set; }
        public bool IsTyping => IsActive && visibleCharacters < characterCount;
        public bool AutoAdvanceEnabled { get; private set; }
        public int CurrentLineIndex { get; private set; } = -1;
        public IReadOnlyList<DialogueLine> History => history;
        public DialogueView View => view;

        // Void entry point is also available to authored UnityEvents; it never auto-runs.
        public void Play(DialogueSequence sequence) => TryBegin(sequence);

        public bool TryBegin(DialogueSequence sequence, Action<DialogueEndReason> onComplete = null)
            => sequence != null && TryBegin(sequence.Lines, onComplete);

        public bool TryBegin(IReadOnlyList<DialogueLine> content, Action<DialogueEndReason> onComplete = null)
        {
            if (!isActiveAndEnabled || IsActive || panels == null || view == null || !view.IsConfigured || content == null)
                return false;
            lines.Clear();
            for (int i = 0; i < content.Count; i++)
                if (content[i] != null && !string.IsNullOrWhiteSpace(content[i].Text)) lines.Add(content[i]);
            if (lines.Count == 0) return false;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (!panels.TryOpenModal(view.gameObject)) { lines.Clear(); return false; }

            history.Clear();
            completion = onComplete;
            IsActive = true;
            AutoAdvanceEnabled = false;
            view.ResetSession();
            ShowLine(0);
            return true;
        }

        /// <summary>A deliberate decision on the existing dialogue modal; cancel never chooses an answer.</summary>
        public bool TryBeginChoice(DialogueLine prompt, string firstLabel, string secondLabel, Action<int> onChoice)
        {
            if (prompt == null || string.IsNullOrWhiteSpace(firstLabel) || string.IsNullOrWhiteSpace(secondLabel) || onChoice == null)
                return false;
            if (!TryBegin(new[] { prompt })) return false;
            choiceCompletion = onChoice;
            IsChoiceActive = true;
            RevealCurrentLine();
            view.ShowChoices(firstLabel, secondLabel);
            return true;
        }

        public void SelectChoice(int index)
        {
            if (!IsChoiceActive || index < 0 || index > 1) return;
            var callback = choiceCompletion;
            Finish(DialogueEndReason.Completed, true);
            callback?.Invoke(index);
        }

        private void Update() => Tick(Time.unscaledDeltaTime);

        /// <summary>Unscaled presentation clock; opening the log suspends typing and auto advance.</summary>
        public void Tick(float seconds)
        {
            if (!IsActive || IsChoiceActive || seconds <= 0 || view.IsLogOpen || (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)) return;
            if (IsTyping)
            {
                visibleCharacters = Mathf.Min(characterCount, visibleCharacters + Mathf.Max(1, charactersPerSecond) * seconds);
                view.SetVisibleCharacters(Mathf.FloorToInt(visibleCharacters));
                if (!IsTyping) RecordCurrentLine();
                // Start the reading delay only after the full line is visible.
                return;
            }
            if (!AutoAdvanceEnabled) return;
            autoTimer += seconds;
            if (autoTimer >= Mathf.Max(.1f, autoAdvanceDelay)) Next();
        }

        public void Next()
        {
            if (!IsActive || IsChoiceActive || view.IsLogOpen) return;
            if (IsTyping) { RevealCurrentLine(); return; }
            if (CurrentLineIndex + 1 < lines.Count) ShowLine(CurrentLineIndex + 1);
            else Finish(DialogueEndReason.Completed, true);
        }

        public void ToggleAuto()
        {
            if (!IsActive || IsChoiceActive || view.IsLogOpen) return;
            AutoAdvanceEnabled = !AutoAdvanceEnabled;
            autoTimer = 0;
            view.SetAutoState(AutoAdvanceEnabled);
        }

        public void ToggleLog()
        {
            if (!IsActive || IsChoiceActive) return;
            if (!view.IsLogOpen) RevealCurrentLine();
            view.SetLogOpen(!view.IsLogOpen, history);
            autoTimer = 0;
        }

        public void Skip() { if (IsActive && !IsChoiceActive) Finish(DialogueEndReason.Skipped, true); }
        public void Cancel() { if (IsActive) Finish(DialogueEndReason.Cancelled, true); }

        private void ShowLine(int index)
        {
            CurrentLineIndex = index;
            visibleCharacters = 0;
            autoTimer = 0;
            lineRecorded = false;
            characterCount = view.ShowLine(lines[index]);
            if (characterCount == 0) RecordCurrentLine();
        }

        private void RevealCurrentLine()
        {
            visibleCharacters = characterCount;
            view.SetVisibleCharacters(characterCount);
            RecordCurrentLine();
        }

        private void RecordCurrentLine()
        {
            if (lineRecorded) return;
            lineRecorded = true;
            history.Add(lines[CurrentLineIndex]);
        }

        private void Finish(DialogueEndReason reason, bool restorePrevious)
        {
            if (!IsActive) return;
            IsActive = false;
            IsChoiceActive = false;
            choiceCompletion = null;
            AutoAdvanceEnabled = false;
            CurrentLineIndex = -1;
            var callback = completion;
            completion = null;
            lines.Clear();
            if (panels != null) panels.EndModal(view != null ? view.gameObject : null, restorePrevious);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(restorePrevious && previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
            callback?.Invoke(reason);
        }

        private void OnDisable() { if (IsActive) Finish(DialogueEndReason.Cancelled, false); }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace G10.Prototype.Dialogue
{
    public enum DialogueSpeakerKind { Story, Character, Guide }
    public enum DialogueEndReason { Completed, Skipped, Cancelled }

    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private DialogueSpeakerKind kind;
        [SerializeField] private string speaker;
        [SerializeField, TextArea(3, 8)] private string text;

        public DialogueSpeakerKind Kind => kind;
        public string Speaker => speaker ?? string.Empty;
        public string Text => text ?? string.Empty;

        public DialogueLine(DialogueSpeakerKind kind, string speaker, string text)
        {
            this.kind = kind;
            this.speaker = speaker;
            this.text = text;
        }
    }

    [CreateAssetMenu(menuName = "G10/Dialogue/Sequence", fileName = "DialogueSequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();
        public IReadOnlyList<DialogueLine> Lines => lines;
    }
}

using System;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    public enum EffectPhase { Preparation, Disturbance, Peak, Recovery }
    [Serializable]
    public sealed class EffectCue
    {
        public EffectPhase phase;
        [Min(0)] public float offset;
        public EffectDefinition effect;
        [Min(.01f)] public float duration = 1;
        [Range(0, 1)] public float intensity = 1;
    }
    [CreateAssetMenu(menuName = "G10/Feedback/Effect Sequence")]
    public sealed class EffectSequence : ScriptableObject
    {
        public string id, channel = "horror.sequence";
        public EffectTarget target;
        public int priority = 20;
        public EffectConflict conflict = EffectConflict.ReplaceLowerPriority;
        public EffectCue[] cues = Array.Empty<EffectCue>();
        public float Duration
        {
            get
            {
                float result = .01f;
                foreach (var cue in cues)
                    if (cue != null && cue.effect != null) result = Mathf.Max(result, Mathf.Max(0, cue.offset) + Mathf.Max(.01f, cue.duration));
                return result;
            }
        }
    }
}

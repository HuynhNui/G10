using System;
using System.Collections.Generic;
namespace G10.Prototype.Feedback
{
    [Serializable]
    public sealed class ZoneTwoPresentationState
    {
        public List<string> played = new();
        public float firstCaptureDistance;
        public bool Has(string id) => played != null && played.Contains(id);
        public void Normalize()
        {
            played ??= new(); var seen = new HashSet<string>(StringComparer.Ordinal);
            played.RemoveAll(id => string.IsNullOrWhiteSpace(id) || !seen.Add(id));
            if (!float.IsFinite(firstCaptureDistance) || firstCaptureDistance < 0) firstCaptureDistance = 0;
        }
    }
}

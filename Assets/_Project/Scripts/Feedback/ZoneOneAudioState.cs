using System;
using System.Collections.Generic;

namespace G10.Prototype.Feedback
{
    /// <summary>Timeline data only; checkpoint copies contain no playback handles or Unity objects.</summary>
    [Serializable]
    public sealed class ZoneOneAudioState
    {
        public List<string> played = new();
        public float captureDistance;
        public bool Has(string id) => played != null && played.Contains(id);
        public void Normalize()
        {
            if (!float.IsFinite(captureDistance) || captureDistance < 0) captureDistance = 0;
            played ??= new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            played.RemoveAll(id => string.IsNullOrWhiteSpace(id) || !seen.Add(id));
        }
    }
}

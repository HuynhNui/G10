using System;
using System.Collections.Generic;

namespace G10.Prototype.Feedback
{
    [Serializable]
    public sealed class ZoneThreePresentationState
    {
        public List<string> played = new();
        public float captureDistance, radarDistance;
        public bool Has(string id) => played != null && played.Contains(id);
        public void Normalize()
        {
            played ??= new(); var seen = new HashSet<string>(StringComparer.Ordinal);
            played.RemoveAll(id => string.IsNullOrWhiteSpace(id) || !seen.Add(id));
            if (!float.IsFinite(captureDistance) || captureDistance < 0) captureDistance = 0;
            if (!float.IsFinite(radarDistance) || radarDistance < 0) radarDistance = 0;
        }
    }
}

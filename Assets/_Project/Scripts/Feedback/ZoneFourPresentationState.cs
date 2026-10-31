using System;
using System.Collections.Generic;

namespace G10.Prototype.Feedback
{
    [Serializable]
    public sealed class ZoneFourPresentationState
    {
        // An earned approach survives braking, Continue and Rest without becoming delivery.
        public const string FinalSignalOpportunity = "final.signal.opportunity";
        public List<string> played = new();
        public bool Has(string id) => played != null && played.Contains(id);
        public void Normalize()
        {
            played ??= new();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            played.RemoveAll(id => string.IsNullOrWhiteSpace(id) || !seen.Add(id));
        }
    }
}

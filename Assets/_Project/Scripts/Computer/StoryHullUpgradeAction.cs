using G10.Prototype.Missions;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>The existing Upgrade app performs the current zone's authored progression action.</summary>
    public sealed class StoryHullUpgradeAction : MonoBehaviour, IUpgradeAction
    {
        public ZoneOneStory story;
        public bool CanApply => story != null && story.CanApplyProgressionAction;
        public bool TryApply() => CanApply && story.ApplyProgressionAction();
    }
}

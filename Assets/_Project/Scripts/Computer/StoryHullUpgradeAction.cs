using G10.Prototype.Missions;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>The existing Upgrade app installs the hull earned by Zone 1 objectives.</summary>
    public sealed class StoryHullUpgradeAction : MonoBehaviour, IUpgradeAction
    {
        public ZoneOneStory story;
        public bool CanApply => story != null && story.CanInstall;
        public bool TryApply() => CanApply && story.InstallHull();
    }
}

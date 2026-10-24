using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Audio
{
    public sealed partial class AudioManager
    {
        private int zoneAudioOwner;
        private AudioClip zoneRadarClip, zoneCameraClip;
        public void AcquireZoneAudio(int owner, AudioClip radar, AudioClip camera)
        {
            zoneAudioOwner = owner; zoneRadarClip = radar; zoneCameraClip = camera;
            if (ambientFadeCoroutine != null) { StopCoroutine(ambientFadeCoroutine); ambientFadeCoroutine = null; }
            StopAmbient(false);
        }
        public void ReleaseZoneAudio(int owner)
        {
            if (zoneAudioOwner != owner) return;
            zoneAudioOwner = 0; zoneRadarClip = zoneCameraClip = null;
            UpdateAmbientForScene(SceneManager.GetActiveScene().name);
        }
    }
}

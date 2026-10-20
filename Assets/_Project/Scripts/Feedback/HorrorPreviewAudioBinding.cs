using G10.Prototype.Audio;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Only used by the isolated preview scene; no gameplay or save references.</summary>
    public sealed class HorrorPreviewAudioBinding : MonoBehaviour
    {
        public EffectDirector director;
        private void Start() => director.BindAudio(AudioManager.Instance);
    }
}

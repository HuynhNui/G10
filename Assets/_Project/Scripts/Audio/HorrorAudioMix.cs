using UnityEngine;
using UnityEngine.Audio;

namespace G10.Prototype.Audio
{
    public enum AudioCategory { Machinery, UnderwaterAmbient, Anomalies, Interface, Impacts }
    public enum HorrorMixState { None, Normal, Suspense, NearSilence }

    [CreateAssetMenu(menuName = "G10/Audio/Horror Mix")]
    public sealed class HorrorAudioMix : ScriptableObject
    {
        public AudioMixer mixer;
        public AudioMixerGroup machinery, underwaterAmbient, anomalies, userInterface, impacts;
        public AudioMixerSnapshot normal, suspense, nearSilence;
        public AudioMixerGroup Group(AudioCategory category) => category switch
        {
            AudioCategory.UnderwaterAmbient => underwaterAmbient,
            AudioCategory.Anomalies => anomalies,
            AudioCategory.Interface => userInterface,
            AudioCategory.Impacts => impacts,
            _ => machinery
        };
        public AudioMixerSnapshot Snapshot(HorrorMixState state) => state switch
        { HorrorMixState.Suspense => suspense, HorrorMixState.NearSilence => nearSilence, _ => normal };
    }
}

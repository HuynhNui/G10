using UnityEngine;

namespace G10.Prototype.Computer
{
    [CreateAssetMenu(menuName = "G10/Photo Capture Profile")]
    public sealed class PhotoCaptureProfile : ScriptableObject
    {
        public Texture2D shallow, mid, deep, seabed, fog, particles;
        public Texture2D creature, closeCreature, silhouette, kelp, rock;
        [Range(20,120)] public float fieldOfView = 70;
        [Min(1)] public float visibleDistance = 65;
        [Min(1)] public float creatureHeight = 16;
        public float shallowDepth = 200, deepDepth = 300;
        [Range(0,1)] public float goodCoverage = .035f, tooFarCoverage = .008f, maximumOcclusion = .35f;
    }
}

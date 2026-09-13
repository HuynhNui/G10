using System;
using System.Collections.Generic;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public sealed class EmptyPhotoRepository : MonoBehaviour, IPhotoRepository
    {
        public bool CameraOnline => false;
        public IReadOnlyList<PhotoRecord> Photos => Array.Empty<PhotoRecord>();
    }
}

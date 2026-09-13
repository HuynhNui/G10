using System;
using System.Collections.Generic;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public sealed class PhotoRecord
    {
        public string Id { get; }
        public Texture2D Image { get; }
        public Texture2D Thumbnail { get; }
        public DateTimeOffset CapturedAt { get; }
        public Vector2 MapCoordinate { get; }
        public bool IsMissionPhoto { get; }
        public float Depth { get; }
        public float Heading { get; }
        public string Result { get; }
        public PhotoRecord(string id, Texture2D image, Texture2D thumbnail, DateTimeOffset capturedAt,
            Vector2 mapCoordinate, bool isMissionPhoto, float depth = 0, float heading = 0, string result = "")
        {
            Id = id; Image = image; Thumbnail = thumbnail; CapturedAt = capturedAt;
            MapCoordinate = mapCoordinate; IsMissionPhoto = isMissionPhoto;
            Depth = depth; Heading = heading; Result = result;
        }
    }

    public interface IPhotoRepository
    {
        bool CameraOnline { get; }
        IReadOnlyList<PhotoRecord> Photos { get; }
    }

    public readonly struct ShipStatusSnapshot
    {
        public readonly bool RadarInstalled;
        public readonly bool RadarScanning;
        public readonly int? RadarUsesRemaining;
        public readonly float? EnergyCurrent, EnergyMaximum, EnergyDrain;
        public readonly string CameraState, CaptureState, PowerState;
        public readonly bool? LowResourceWarning;
        public ShipStatusSnapshot(bool radarInstalled, bool radarScanning, int? radarUsesRemaining,
            float? energyCurrent, float? energyMaximum, float? energyDrain,
            string cameraState, string captureState, string powerState, bool? lowResourceWarning)
        {
            RadarInstalled = radarInstalled; RadarScanning = radarScanning; RadarUsesRemaining = radarUsesRemaining;
            EnergyCurrent = energyCurrent; EnergyMaximum = energyMaximum; EnergyDrain = energyDrain;
            CameraState = cameraState; CaptureState = captureState; PowerState = powerState;
            LowResourceWarning = lowResourceWarning;
        }
    }

    public interface IShipStatusProvider { ShipStatusSnapshot ReadStatus(); }
    public interface IMissionProvider
    {
        string ZoneName { get; }
        MissionDefinition CurrentMission { get; }
    }
}

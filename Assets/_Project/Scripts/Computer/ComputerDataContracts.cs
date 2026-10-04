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
        public bool IsMissionPhoto { get; private set; }
        public string MissionZoneId { get; }
        public string MissionPoiId { get; }
        public string MissionObjectiveId { get; }
        public string MissionTargetId { get; }
        internal void MarkSubmitted() => IsMissionPhoto = true;
        public float Depth { get; }
        public float Heading { get; }
        public string Result { get; }
        public PhotoRecord(string id, Texture2D image, Texture2D thumbnail, DateTimeOffset capturedAt,
            Vector2 mapCoordinate, bool isMissionPhoto, float depth = 0, float heading = 0, string result = "",
            string missionZoneId = null, string missionPoiId = null, string missionObjectiveId = null, string missionTargetId = null)
        {
            Id = id; Image = image; Thumbnail = thumbnail; CapturedAt = capturedAt;
            MapCoordinate = mapCoordinate; IsMissionPhoto = isMissionPhoto;
            Depth = depth; Heading = heading; Result = result;
            MissionZoneId = missionZoneId; MissionPoiId = missionPoiId;
            MissionObjectiveId = missionObjectiveId; MissionTargetId = missionTargetId;
        }
    }

    public interface IPhotoRepository
    {
        bool CameraOnline { get; }
        IReadOnlyList<PhotoRecord> Photos { get; }
    }

    public enum PhotoSubmissionState { NotCandidate, Available, Submitted, ObjectiveCompleted, Unavailable }
    public interface IPhotoSubmissionRepository : IPhotoRepository
    {
        PhotoSubmissionState GetSubmissionState(PhotoRecord photo);
        bool CanSubmitPhoto(PhotoRecord photo);
        bool SubmitPhoto(PhotoRecord photo);
    }

    public readonly struct ShipStatusSnapshot
    {
        public readonly bool RadarInstalled;
        public readonly bool RadarScanning;
        public readonly int? RadarUsesRemaining;
        public readonly float? EnergyCurrent, EnergyMaximum, EnergyDrain;
        public readonly string CameraState, CaptureState, PowerState;
        public readonly bool? LowResourceWarning;
        public readonly G10.Prototype.Navigation.ShipState Ship;
        public ShipStatusSnapshot(bool radarInstalled, bool radarScanning, int? radarUsesRemaining,
            float? energyCurrent, float? energyMaximum, float? energyDrain,
            string cameraState, string captureState, string powerState, bool? lowResourceWarning, G10.Prototype.Navigation.ShipState ship = null)
        {
            RadarInstalled = radarInstalled; RadarScanning = radarScanning; RadarUsesRemaining = radarUsesRemaining;
            EnergyCurrent = energyCurrent; EnergyMaximum = energyMaximum; EnergyDrain = energyDrain;
            CameraState = cameraState; CaptureState = captureState; PowerState = powerState;
            LowResourceWarning = lowResourceWarning;
            Ship = ship;
        }
    }

    public interface IShipStatusProvider { ShipStatusSnapshot ReadStatus(); }
    public interface IMissionProvider
    {
        string ZoneName { get; }
        MissionDefinition CurrentMission { get; }
    }
}

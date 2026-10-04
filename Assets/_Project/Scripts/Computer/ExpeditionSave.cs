using System;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using UnityEngine;

namespace G10.Prototype.Computer
{
    [Serializable] public sealed class SavedPhoto
    {
        public string id, time, result, png;
        public Vector2 coordinate;
        public float depth, heading;
        public bool mission;
    }
    [Serializable] public sealed class SavedCreature
    { public string id, name; }
    [Serializable] public sealed class SavedCreatureSpawn
    {
        public int day;
        public string poiId;
        public Vector2 coordinate;
    }
    [Serializable] public sealed class ZoneProgressState
    {
        public bool mainObjectivesComplete, exitUnlocked, rockDestroyed;
        public bool hiddenRouteUnlocked, hiddenRouteComplete;
        public string endingChoice;
    }
    [Serializable] public sealed class ExpeditionZoneState
    {
        public string zone;
        public Vector2 position;
        public float heading, depth, distance;
        public int deadline, completedDay;
        public bool hasVoyage, creaturePresent = true;
        public string creatureId;
        public int zoneOneStoryProgress;
        public int mapCoordinateVersion;
        public MissionProgressState missionProgress = new();
        public ZoneProgressState progress = new();
        public PhotoSurveyZone.TaskKind[] tasks = Array.Empty<PhotoSurveyZone.TaskKind>();
        // Hidden gameplay state. Journal checkpoints retain it, but the Journal UI never renders it.
        public List<SavedCreatureSpawn> creatureSpawns = new();
    }
    [Serializable] public sealed class ExpeditionSnapshot
    {
        public int day = 1;
        public string zone = "Zone01";
        public List<ExpeditionZoneState> zones = new();
        public List<SavedCreature> inventory = new();
        public List<SavedPhoto> photos = new();
        public int photosTaken, dayStartPhotos, dayStartCaptures, dayStartTasks;
        public float dayStartDistance;
        // Optional in v1: older timelines start with the configured base ship.
        public ShipState ship;
        public bool hasShipState;
        public string endingReached;
    }
    [Serializable] public sealed class ExpeditionJournalEntry
    {
        public int day, photos, captures, tasks;
        public string zone;
        public float distance;
        public ExpeditionSnapshot checkpoint;
    }
    [Serializable] public sealed class ExpeditionSave
    {
        public int version = ExpeditionSaveStore.CurrentVersion;
        public ExpeditionSnapshot current = new();
        // Separate from end-of-day journal checkpoints. Null for legacy saves until their next Rest.
        public ExpeditionSnapshot dayStart;
        public bool hasDayStart;
        public List<ExpeditionJournalEntry> journal = new();
    }

    /// <summary>One timeline. Replace atomically; retain the prior complete file for interrupted/corrupt writes.</summary>
    public static class ExpeditionSaveStore
    {
        public const int CurrentVersion = 3;
        public static string PathOverride { get; set; }
        public static string SavePath => PathOverride ?? Path.Combine(Application.persistentDataPath, "Expedition", "timeline.json");
        public static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        /// <summary>Explicit new game. Replaces the timeline and its recovery copy; user preferences and photo files are untouched.</summary>
        public static ExpeditionSave ResetGameProgress()
        {
            var fresh = new ExpeditionSave();
            Write(fresh, discardFuture: true);
            return fresh;
        }
        public static bool TryRead(out ExpeditionSave save, out string error)
        {
            save = null; error = null;
            if (!File.Exists(SavePath) && !File.Exists(SavePath + ".bak")) return true;
            foreach (string path in new[] { SavePath, SavePath + ".bak" })
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    var candidate = JsonUtility.FromJson<ExpeditionSave>(File.ReadAllText(path));
                    if (candidate == null || candidate.version < 1 || candidate.version > CurrentVersion || candidate.journal == null)
                        throw new InvalidDataException("Invalid expedition save.");
                    Normalize(candidate.current);
                    Validate(candidate.current);
                    if (candidate.version < 3 || !candidate.hasDayStart) { candidate.dayStart = null; candidate.hasDayStart = false; }
                    if (candidate.hasDayStart)
                    {
                        Normalize(candidate.dayStart);
                        Validate(candidate.dayStart);
                        if (candidate.dayStart.day != candidate.current.day || !candidate.dayStart.hasShipState ||
                            candidate.dayStart.ship.hull <= 0 ||
                            !candidate.dayStart.zones.Exists(zone => zone.zone == candidate.dayStart.zone && zone.hasVoyage))
                            throw new InvalidDataException("Invalid day-start snapshot.");
                    }
                    int previousDay=0;
                    foreach(var entry in candidate.journal)
                    {
                        if(entry==null || entry.day<=previousDay || entry.day>candidate.current.day || entry.checkpoint==null ||
                            entry.checkpoint.day!=entry.day || entry.zone!=entry.checkpoint.zone) throw new InvalidDataException("Invalid journal.");
                        Normalize(entry.checkpoint);
                        Validate(entry.checkpoint);previousDay=entry.day;
                    }
                    candidate.version = CurrentVersion;
                    save = candidate;
                    if (path.EndsWith(".bak")) error = "Đã phục hồi từ bản lưu dự phòng.";
                    return true;
                }
                catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
                { error = "Không đọc được bản lưu hành trình. Giữ nguyên file để phục hồi."; }
            }
            return false;
        }
        public static bool IsZone(string zone) => zone == "Zone01" || zone == "Zone02" || zone == "Zone03" || zone == "Zone04";
        private static void Normalize(ExpeditionSnapshot state)
        {
            if (state?.zones == null) return;
            foreach (var zone in state.zones)
            {
                if (zone == null) continue;
                zone.tasks ??= Array.Empty<PhotoSurveyZone.TaskKind>();
                zone.creatureSpawns ??= new List<SavedCreatureSpawn>();
                zone.missionProgress ??= new MissionProgressState();
                zone.progress ??= new ZoneProgressState();
                zone.missionProgress.Normalize();
            }
        }
        private static void Validate(ExpeditionSnapshot state)
        {
            if(state==null || state.day<1 || !IsZone(state.zone) || state.zones==null || state.inventory==null || state.photos==null ||
                state.inventory.Count>CreatureInventory.Capacity || state.photos.Count>24) throw new InvalidDataException("Invalid snapshot.");
            var ids=new HashSet<string>();
            if (state.hasShipState && (state.ship == null || !state.ship.IsValid)) throw new InvalidDataException("Invalid ship resources.");
            foreach(var zone in state.zones)
                if(zone==null || !IsZone(zone.zone) || !ids.Add(zone.zone) || zone.deadline<1 || zone.tasks==null || zone.creatureSpawns==null || zone.zoneOneStoryProgress < 0 || zone.zoneOneStoryProgress > 511 ||
                    !float.IsFinite(zone.position.x) || !float.IsFinite(zone.position.y) || !float.IsFinite(zone.heading) ||
                    !float.IsFinite(zone.depth) || !float.IsFinite(zone.distance)) throw new InvalidDataException("Invalid zone.");
            foreach(var zone in state.zones)
            {
                ids.Clear();
                foreach(var spawn in zone.creatureSpawns)
                    if(spawn==null || spawn.day<1 || spawn.day>state.day || string.IsNullOrEmpty(spawn.poiId) || !ids.Add(spawn.poiId) ||
                        !float.IsFinite(spawn.coordinate.x) || !float.IsFinite(spawn.coordinate.y) ||
                        spawn.coordinate.x<0 || spawn.coordinate.y<0)
                        throw new InvalidDataException("Invalid creature spawn.");
            }
            ids.Clear();
            foreach(var item in state.inventory)
                if(item==null || string.IsNullOrEmpty(item.id) || !ids.Add(item.id)) throw new InvalidDataException("Invalid cargo.");
            ids.Clear();
            foreach(var photo in state.photos)
                if(photo==null || string.IsNullOrEmpty(photo.id) || !ids.Add(photo.id) || string.IsNullOrEmpty(photo.png) ||
                    !DateTimeOffset.TryParse(photo.time,out _)) throw new InvalidDataException("Invalid photo.");
        }
        public static void Write(ExpeditionSave save, bool discardFuture = false)
        {
            string path = SavePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string json = JsonUtility.ToJson(save);
            using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            if (discardFuture)
            {
                // Publish the truncated recovery timeline first. A crash can never recover discarded future history.
                File.Copy(path + ".tmp",path + ".bak.tmp",true);
                if(File.Exists(path + ".bak")) File.Replace(path + ".bak.tmp",path + ".bak",null);
                else File.Move(path + ".bak.tmp",path + ".bak");
            }
            if (File.Exists(path)) File.Replace(path + ".tmp", path, discardFuture ? null : path + ".bak");
            else File.Move(path + ".tmp", path);
        }
    }
}

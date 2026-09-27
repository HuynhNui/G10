using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Computer
{
    [Serializable] public sealed class ExpeditionZoneRules
    {
        public string zone = "Zone01";
        [Min(1)] public int baseDays = 5;
        public MapPoi[] restAreas = { new MapPoi { id = "Dock", mapPosition = new Vector2(600,100), arrivalRadius = 30 } };
    }
    [Serializable] public sealed class ExpeditionCreatureAsset
    { public string id; public Texture2D icon; }

    /// <summary>Day/checkpoint owner on GameplayCore. Existing components remain authoritative for gameplay.</summary>
    public sealed class ExpeditionLoop : MonoBehaviour
    {
        public ExpeditionZoneRules[] zones = {
            new() { zone="Zone01" }, new() { zone="Zone02" }, new() { zone="Zone03" }, new() { zone="Zone04" } };
        [Min(0)] public int maxCarryOverDays = 3;
        public ExpeditionCreatureAsset[] creatureCatalog = Array.Empty<ExpeditionCreatureAsset>();
        public G10.Prototype.Missions.SurveyContentDefinition[] contentCatalog = Array.Empty<G10.Prototype.Missions.SurveyContentDefinition>();
        private ExpeditionSave save;
        private CabinStationView cabin;
        private PhotoSurveyZone survey;
        private CreatureInventory inventory;
        private PhotoCaptureService photos;
        private ExpeditionComputerView computer;
        private readonly Dictionary<string, Texture2D> creatureIcons = new();
        private bool initialized, binding, saveReadable = true;
        private float checkAt;
        public string LastError { get; private set; }
        public int Day => save?.current.day ?? 1;
        public string Zone => save?.current.zone ?? "Zone01";
        public int Deadline => CurrentZone?.deadline ?? 0;
        public int RemainingDays => Mathf.Max(0, Deadline - Day);
        public bool Failed { get; private set; }
        public bool Blocked => Failed || !saveReadable || binding;
        public bool RequiredObjectivesComplete => survey != null && survey.IsComplete;
        public IReadOnlyList<ExpeditionJournalEntry> Journal => save.journal;
        public ZoneNavigation Navigation => cabin != null ? cabin.Navigation : null;
        public bool CanRest => !Blocked && cabin != null && !cabin.Panels.IsModalOpen;
        public bool NeedsRecovery => Navigation != null && !Navigation.Ship.CanMove;
        public bool CanRecover => NeedsRecovery && !Blocked && cabin != null && !cabin.Panels.IsModalOpen && RecoveryArea != null;
        private MapPoi RecoveryArea
        {
            get {
                foreach (var area in Rules(Zone)?.restAreas ?? Array.Empty<MapPoi>())
                    if (area != null && Navigation != null && Navigation.CanOccupy(area.mapPosition)) return area;
                return null;
            }
        }
        public bool InRestArea
        {
            get {
                var rule = Rules(Zone);
                if (Navigation == null || rule?.restAreas == null) return false;
                foreach (var area in rule.restAreas) if (area != null && area.Contains(Navigation.Position)) return true;
                return false;
            }
        }
        private ExpeditionZoneState CurrentZone => save?.current.zones.Find(z => z.zone == Zone);
        private ExpeditionZoneRules Rules(string zone) => Array.Find(zones, z => z.zone == zone);
        private void Awake()
        {
            foreach (var asset in creatureCatalog) if(asset != null && !string.IsNullOrEmpty(asset.id)) creatureIcons[asset.id]=asset.icon;
            foreach (var asset in contentCatalog) if(asset != null && !string.IsNullOrEmpty(asset.id)) creatureIcons[asset.id]=asset.Image;
            saveReadable = ExpeditionSaveStore.TryRead(out save, out string error);
            LastError = error; save ??= new ExpeditionSave();
        }
        private void OnEnable() => SceneManager.sceneLoaded += SceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= SceneLoaded;
        private IEnumerator Start() { yield return null; BindLoadedZone(); }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        { if (ExpeditionSaveStore.IsZone(scene.name)) StartCoroutine(BindAfterStart()); }
        private IEnumerator BindAfterStart() { yield return null; BindLoadedZone(); }
        public void BindLoadedZone()
        {
            var found = FindAnyObjectByType<CabinStationView>();
            if (found == null || found == cabin && initialized) return;
            if (found.gameObject.scene.name != Zone) return;
            cabin = found; survey = cabin.GetComponent<PhotoSurveyZone>();
            inventory = cabin.GetComponent<CreatureInventory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            var catcher = cabin.GetComponent<CreatureCatcher>();
            if (survey != null && survey.Story != null)
            {
                var story = survey.Story;
                foreach (var entry in new[] { story.creatureOne, story.creatureTwo, story.emmaTube })
                    if (entry != null) creatureIcons[entry.id] = entry.Image;
                if (story.emmaTube != null) creatureIcons[G10.Prototype.Missions.ZoneOneStory.EmmaBlueprint] = story.emmaTube.Image;
                if (story.creatureOne != null) creatureIcons["Creature01"] = story.creatureOne.Image;
                if (story.creatureTwo != null) creatureIcons["Creature02"] = story.creatureTwo.Image;
            }
            if (survey?.MissionRuntime?.contentCatalog != null)
                foreach (var entry in survey.MissionRuntime.contentCatalog)
                    if (entry != null && !string.IsNullOrEmpty(entry.id)) creatureIcons[entry.id] = entry.Image;
            if (catcher != null && survey != null) creatureIcons[survey.creatureId] = catcher.itemIcon;
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            computer = screen.GetComponent<ExpeditionComputerView>();
            if (computer == null) computer = screen.gameObject.AddComponent<ExpeditionComputerView>();
            computer.Initialize(this, screen);
            screen.Expedition = this;
            foreach (var status in screen.GetComponentsInChildren<ShipStatusView>(true)) status.Expedition = this;
            foreach (var mission in screen.GetComponentsInChildren<MissionLogView>(true)) mission.Expedition = this;
            EnsureZone(Zone, 0);
            try
            {
                if (CurrentZone.hasVoyage) ApplySnapshot(save.current);
                else
                {
                    // New zone encounter/navigation are fresh; persistent cargo and photographs travel with the ship.
                    bool ongoing=save.current.zones.Count>1;
                    var cargo=save.current.inventory; var archive=save.current.photos; int total=save.current.photosTaken;
                    var ship = save.current.ship;
                    bool hasShipState = save.current.hasShipState;
                    CaptureInto(save.current);
                    if(ongoing)
                    {
                        save.current.inventory=cargo;save.current.photos=archive;save.current.photosTaken=total;
                        save.current.ship = ship;
                        save.current.hasShipState = hasShipState;
                        ApplySnapshot(save.current);
                    }
                    else ResetSummaryBaseline();
                }
                EnsureDailyCreatureSpawns(save.current);
                initialized = true;
            }
            catch (Exception ex) { saveReadable = false; LastError = "Không khôi phục được dữ liệu: " + ex.Message; }
            binding = false; EvaluateDeadline();
            if (Blocked) ShowFailure();
        }
        private void EnsureZone(string zone, int carry)
        {
            if (save.current.zones.Exists(z => z.zone == zone)) return;
            save.current.zones.Add(new ExpeditionZoneState { zone=zone,
                deadline=Day + Mathf.Max(1, Rules(zone)?.baseDays ?? 5) + Mathf.Clamp(carry,0,maxCarryOverDays) - 1 });
        }
        private void Update()
        {
            if (!initialized || binding || Time.unscaledTime < checkAt) return;
            checkAt = Time.unscaledTime + .2f;
            EvaluateDeadline();
            if (Blocked && cabin != null && cabin.Panels != null && cabin.Panels.CurrentPanel != computer.gameObject)
                ShowFailure();
        }
        public void EvaluateDeadline()
        {
            if (CurrentZone == null) return;
            if (RequiredObjectivesComplete && CurrentZone.completedDay == 0) CurrentZone.completedDay = Day;
            bool before = Failed;
            Failed = Day > Deadline && !RequiredObjectivesComplete;
            if (Navigation != null) { Navigation.ExpeditionBlocked = Blocked; if (Blocked) Navigation.Brake(); }
            if (cabin != null && cabin.Panels != null) cabin.Panels.LockedPanel=Blocked ? computer.gameObject : null;
            if (Failed && !before) ShowFailure();
        }
        private void ShowFailure()
        {
            if (cabin == null || computer == null || cabin.Panels == null) return;
            cabin.GetComponent<CaptureMinigameController>()?.Cancel();
            cabin.OpenComputer(); computer.ShowFailure();
        }
        private void CaptureInto(ExpeditionSnapshot snapshot)
        {
            if (cabin == null) return;
            var zone = snapshot.zones.Find(z => z.zone == cabin.gameObject.scene.name);
            if (zone == null) return;
            EnsureDailyCreatureSpawns(snapshot);
            zone.hasVoyage=true; zone.position=Navigation.Position; zone.heading=Navigation.Heading;
            snapshot.ship = Navigation.Ship.Export();
            snapshot.hasShipState = true;
            zone.depth=Navigation.Depth; zone.distance=Navigation.DistanceTravelled;
            if (survey != null) { zone.tasks=survey.ExportProgress(); zone.creaturePresent=survey.creaturePresent; zone.creatureId=survey.creatureId; }
            if (survey?.MissionRuntime != null) zone.missionProgress = survey.MissionRuntime.ExportProgress();
            if (survey != null && survey.Story != null) zone.zoneOneStoryProgress = survey.Story.SavedProgress;
            if (inventory != null)
            {
                snapshot.inventory=new List<SavedCreature>();
                foreach (var item in inventory.Items) snapshot.inventory.Add(new SavedCreature { id=item.Id, name=item.Name });
            }
            if (photos != null) { snapshot.photos=photos.ExportPhotos(); snapshot.photosTaken=photos.TotalPhotosTaken; }
        }
        private void ApplySnapshot(ExpeditionSnapshot snapshot)
        {
            var zone = snapshot.zones.Find(z => z.zone == cabin.gameObject.scene.name);
            var restored = new List<CreatureInventory.Item>();
            foreach (var item in snapshot.inventory)
            {
                if (item.id == "Adhesive02") continue; // Removed from mission progression; old cargo is safely discarded.
                string id = item.id == "EmmaTube01" ? G10.Prototype.Missions.ZoneOneStory.EmmaBlueprint :
                    item.id == "Creature01" ? "Z1_Creature_01" : item.id == "Creature02" ? "Z1_Creature_02" : item.id;
                if (!creatureIcons.TryGetValue(id, out var icon)) throw new InvalidDataException("Unknown creature: " + id);
                restored.Add(new CreatureInventory.Item(id,item.name,icon));
            }
            if (zone == null) throw new InvalidDataException("Missing zone state.");
            if (survey != null && zone.creatureId != survey.creatureId) throw new InvalidDataException("Encounter ID changed.");
            // Older voyages stored the introductory photograph in generic tasks. Preserve that
            // work when loading the ordered story, without granting either collection reward.
            if (survey != null && survey.Story != null && zone.zoneOneStoryProgress == 0 &&
                Array.IndexOf(zone.tasks, PhotoSurveyZone.TaskKind.Photograph) >= 0)
            {
                zone.zoneOneStoryProgress = (int)(G10.Prototype.Missions.ZoneOneStory.Progress.RadarOne |
                    G10.Prototype.Missions.ZoneOneStory.Progress.PhotoOne);
                zone.tasks = Array.Empty<PhotoSurveyZone.TaskKind>();
                zone.completedDay = 0;
            }
            photos?.RestorePhotos(snapshot.photos, snapshot.photosTaken);
            inventory?.RestoreItems(restored);
            survey?.RestoreProgress(zone.tasks, zone.creaturePresent);
            if (survey?.MissionRuntime != null)
            {
                if (survey.Story != null && (zone.missionProgress == null || zone.missionProgress.IsEmpty)) survey.Story.Restore(zone.zoneOneStoryProgress);
                else survey.MissionRuntime.RestoreProgress(zone.missionProgress);
            }
            EnsureDailyCreatureSpawns(snapshot);
            Navigation.Ship.Restore(snapshot.hasShipState && !Navigation.UseSceneShipSettingsOnLoad ? snapshot.ship : Navigation.CreateInitialShipState());
            Navigation.RestoreVoyage(zone.position,zone.heading,zone.depth,zone.distance);
            cabin.Brake();
        }
        private int CompletedTasks()
        {
            int count=0;
            foreach(var zone in save.current.zones) count += zone.zone == Zone && survey != null ? survey.CompletedCount :
                zone.missionProgress != null && !zone.missionProgress.IsEmpty ? zone.missionProgress.completedObjectives.Count :
                zone.zoneOneStoryProgress != 0 ? G10.Prototype.Missions.ZoneOneStory.CountProgress(zone.zoneOneStoryProgress) : zone.tasks.Length;
            return count;
        }
        private float TotalDistance()
        {
            float total=0;
            foreach(var zone in save.current.zones) total += zone.zone == Zone && Navigation != null ? Navigation.DistanceTravelled : zone.distance;
            return total;
        }
        private void ResetSummaryBaseline()
        {
            save.current.dayStartDistance=TotalDistance(); save.current.dayStartTasks=CompletedTasks();
            save.current.dayStartPhotos=photos != null ? photos.TotalPhotosTaken : save.current.photosTaken;
            save.current.dayStartCaptures=inventory != null ? inventory.Items.Count : save.current.inventory.Count;
        }
        public bool Rest()
            => AdvanceDay(false);
        public bool RecoverShip()
            => AdvanceDay(true);
        private bool AdvanceDay(bool recovery)
        {
            if (recovery ? !CanRecover : !CanRest)
            {
                LastError = recovery ? "Không có điểm cứu hộ hợp lệ." : "Không thể nghỉ khi hành trình đang bị khóa.";
                return false;
            }
            EvaluateDeadline(); CaptureInto(save.current);
            var candidate=ExpeditionSaveStore.Copy(save);
            var entry=new ExpeditionJournalEntry { day=Day, zone=Zone, checkpoint=ExpeditionSaveStore.Copy(save.current),
                distance=Mathf.Max(0,TotalDistance()-save.current.dayStartDistance),
                photos=Mathf.Max(0,save.current.photosTaken-save.current.dayStartPhotos),
                captures=Mathf.Max(0,save.current.inventory.Count-save.current.dayStartCaptures),
                tasks=Mathf.Max(0,CompletedTasks()-save.current.dayStartTasks) };
            candidate.journal.RemoveAll(e=>e.day>=Day); candidate.journal.Add(entry);
            candidate.current.day++;
            candidate.current.dayStartDistance=TotalDistance(); candidate.current.dayStartPhotos=save.current.photosTaken;
            candidate.current.dayStartCaptures=save.current.inventory.Count; candidate.current.dayStartTasks=CompletedTasks();
            candidate.current.ship?.Refill();
            if (recovery) candidate.current.zones.Find(z => z.zone == Zone).position = RecoveryArea.mapPosition;
            EnsureDailyCreatureSpawns(candidate.current);
            if (!Commit(candidate)) { EnsureDailyCreatureSpawns(save.current); return false; }
            Navigation.Ship.Restore(candidate.current.ship);
            if (recovery) Navigation.RestoreVoyage(CurrentZone.position, CurrentZone.heading, CurrentZone.depth, CurrentZone.distance);
            EnsureDailyCreatureSpawns(save.current);
            EvaluateDeadline(); return true;
        }
        public bool RestoreDay(int day)
        {
            if (!saveReadable || binding || cabin != null && cabin.Panels.IsModalOpen) return false;
            var entry=save.journal.Find(e=>e.day==day);
            if (entry == null) return false;
            var candidate=ExpeditionSaveStore.Copy(save);
            candidate.current=ExpeditionSaveStore.Copy(entry.checkpoint);
            candidate.journal.RemoveAll(e=>e.day>day);
            bool sameZone=Zone==candidate.current.zone;
            if (!sameZone && SceneFlowController.Instance == null) { LastError="Khôi phục khác zone cần chạy game từ Bootstrap."; return false; }
            // Validate and apply assets before committing the new timeline; revert on write failure.
            CaptureInto(save.current);
            var previous=ExpeditionSaveStore.Copy(save.current);
            if (sameZone)
            {
                try { ApplySnapshot(candidate.current); }
                catch(Exception ex) { LastError="Không khôi phục được checkpoint: " + ex.Message; return false; }
            }
            if (!Commit(candidate,true)) { if(sameZone) ApplySnapshot(previous); return false; }
            Failed=false;
            if(sameZone) EvaluateDeadline();
            else { binding=true; initialized=false; cabin=null; SceneFlowController.Instance.RestoreZone(Zone); }
            return true;
        }
        private bool Commit(ExpeditionSave candidate, bool discardFuture = false)
        {
            try { ExpeditionSaveStore.Write(candidate,discardFuture); save=candidate; LastError=null; return true; }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
            { LastError="Không lưu được hành trình: " + ex.Message; return false; }
        }
        public bool SaveCurrent()
        {
            if (!initialized || !saveReadable || binding) return false;
            CaptureInto(save.current); return Commit(save);
        }
        public bool PrepareZone(string next)
        {
            if (next==Zone) return !Blocked;
            bool visited=save.current.zones.Exists(z=>z.zone==next);
            bool unlocked = survey?.MissionRuntime != null ? survey.MissionRuntime.HasZone(next) : RequiredObjectivesComplete;
            if (Blocked || (!visited && !unlocked) || Rules(next)==null) return false;
            EvaluateDeadline(); CaptureInto(save.current);
            var previous=ExpeditionSaveStore.Copy(save);
            int carry=visited ? 0 : Mathf.Clamp(Deadline-CurrentZone.completedDay,0,maxCarryOverDays);
            save.current.zone=next; EnsureZone(next,carry);
            if (!Commit(save)) { save=previous; return false; }
            initialized=false; cabin=null; survey=null; inventory=null; photos=null;
            return true;
        }
        private void EnsureDailyCreatureSpawns(ExpeditionSnapshot snapshot)
        {
            if (snapshot == null || survey == null || survey.locations == null) return;
            var zone = snapshot.zones.Find(z => z.zone == cabin.gameObject.scene.name);
            if (zone == null) return;
            zone.creatureSpawns ??= new List<SavedCreatureSpawn>();
            zone.creatureSpawns.RemoveAll(spawn =>
            {
                if (spawn == null || spawn.day != snapshot.day) return true;
                var poi = Array.Find(survey.locations, value => value != null && value.id == spawn.poiId);
                return poi == null || !ValidCreatureSpawn(poi, spawn.coordinate);
            });
            foreach (var poi in survey.locations)
            {
                if (poi == null || string.IsNullOrEmpty(poi.id)) continue;
                var spawn = zone.creatureSpawns.Find(value => value.poiId == poi.id);
                if (spawn == null)
                {
                    spawn = new SavedCreatureSpawn { day=snapshot.day, poiId=poi.id,
                        coordinate=GenerateCreatureCoordinate(zone.zone, poi, snapshot.day) };
                    zone.creatureSpawns.Add(spawn);
                }
            }
            foreach (var spawn in zone.creatureSpawns)
                survey.RestoreCreatureSpawn(spawn.day, spawn.poiId, spawn.coordinate);
        }
        private Vector2 GenerateCreatureCoordinate(string zone, MapPoi poi, int day)
        {
            var random = new System.Random(StableSpawnSeed(zone, poi.id, day));
            float half = ZoneNavigation.ChartCellSize * .5f - .25f;
            Vector2 candidate = poi.mapPosition;
            for (int attempt = 0; attempt < 64; attempt++)
            {
                candidate = poi.mapPosition + new Vector2(
                    ((float)random.NextDouble() * 2f - 1f) * half,
                    ((float)random.NextDouble() * 2f - 1f) * half);
                if (ValidCreatureSpawn(poi, candidate)) return candidate;
            }
            return ValidCreatureSpawn(poi, poi.mapPosition) ? poi.mapPosition : candidate;
        }
        private bool ValidCreatureSpawn(MapPoi poi, Vector2 point)
        {
            if (!InsideMissionCell(poi, point)) return false;
            if (Navigation == null) return true;
            if (!Navigation.CanOccupy(point)) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(poi.mapPosition, point) / 2f));
            for (int i = 0; i <= steps; i++)
                if (!Navigation.IsWater(Vector2.Lerp(poi.mapPosition, point, (float)i / steps))) return false;
            return true;
        }
        private static bool InsideMissionCell(MapPoi poi, Vector2 point)
        {
            Vector2 delta = point - poi.mapPosition;
            float half = ZoneNavigation.ChartCellSize * .5f;
            return Mathf.Abs(delta.x) <= half && Mathf.Abs(delta.y) <= half;
        }
        private static int StableSpawnSeed(string zone, string poiId, int day)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char value in (zone ?? string.Empty) + "|" + (poiId ?? string.Empty)) hash = (hash ^ value) * 16777619;
                hash = (hash ^ (uint)day) * 16777619;
                return (int)hash;
            }
        }
        public string StatusText() => $"DAY {Day:00}   •   {Zone}\nX {Navigation?.Position.x:0.0}  Y {Navigation?.Position.y:0.0}\nREST: {(CanRest ? "AVAILABLE" : "UNAVAILABLE")}";
        public string RestAreasText()
        {
            var text=new System.Text.StringBuilder();
            foreach(var area in Rules(Zone)?.restAreas ?? Array.Empty<MapPoi>())
                if(area!=null)text.Append($"\n{area.id}: X {area.mapPosition.x:0} Y {area.mapPosition.y:0} • R {area.arrivalRadius:0} m");
            return text.ToString();
        }
        public string MissionText() => $"{Zone}  •  DAY {Day:00}  •  DEADLINE: DAY {Deadline:00}\nREMAINING: {RemainingDays} DAYS\n\n" +
            (survey != null ? survey.TaskDescription() : "NO REQUIRED OBJECTIVES CONFIGURED") +
            (Failed ? "\n\nMISSION FAILED — RESTORE A JOURNAL CHECKPOINT" : "");
        private void OnApplicationPause(bool paused) { if(paused) SaveCurrent(); }
        private void OnApplicationQuit() => SaveCurrent();
    }
}

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
        public bool CanRest => !Blocked && cabin != null && !cabin.Panels.IsModalOpen && InRestArea;
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
                    CaptureInto(save.current);
                    if(ongoing)
                    {
                        save.current.inventory=cargo;save.current.photos=archive;save.current.photosTaken=total;
                        ApplySnapshot(save.current);
                    }
                    else ResetSummaryBaseline();
                }
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
            zone.hasVoyage=true; zone.position=Navigation.Position; zone.heading=Navigation.Heading;
            zone.depth=Navigation.Depth; zone.distance=Navigation.DistanceTravelled;
            if (survey != null) { zone.tasks=survey.ExportProgress(); zone.creaturePresent=survey.creaturePresent; zone.creatureId=survey.creatureId; }
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
                if (!creatureIcons.TryGetValue(item.id, out var icon)) throw new InvalidDataException("Unknown creature: " + item.id);
                restored.Add(new CreatureInventory.Item(item.id,item.name,icon));
            }
            if (zone == null) throw new InvalidDataException("Missing zone state.");
            if (survey != null && zone.creatureId != survey.creatureId) throw new InvalidDataException("Encounter ID changed.");
            photos?.RestorePhotos(snapshot.photos, snapshot.photosTaken);
            inventory?.RestoreItems(restored);
            survey?.RestoreProgress(zone.tasks, zone.creaturePresent);
            Navigation.RestoreVoyage(zone.position,zone.heading,zone.depth,zone.distance);
            cabin.Brake();
        }
        private int CompletedTasks()
        {
            int count=0;
            foreach(var zone in save.current.zones) count += zone.zone == Zone && survey != null ? survey.CompletedCount : zone.tasks.Length;
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
        {
            if (!CanRest) { LastError="Tàu phải ở khu nghỉ hợp lệ và không có thao tác đang diễn ra."; return false; }
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
            // The current game has no finite energy/photo/capture budgets. Preserve all existing progress.
            if (!Commit(candidate)) return false;
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
            if (Blocked || (!visited && !RequiredObjectivesComplete) || Rules(next)==null) return false;
            EvaluateDeadline(); CaptureInto(save.current);
            var previous=ExpeditionSaveStore.Copy(save);
            int carry=visited ? 0 : Mathf.Clamp(Deadline-CurrentZone.completedDay,0,maxCarryOverDays);
            save.current.zone=next; EnsureZone(next,carry);
            if (!Commit(save)) { save=previous; return false; }
            initialized=false; cabin=null; survey=null; inventory=null; photos=null;
            return true;
        }
        public string StatusText() => $"DAY {Day:00}   •   {Zone}\nX {Navigation?.Position.x:0.0}  Y {Navigation?.Position.y:0.0}\nREST: {(InRestArea ? "AVAILABLE" : "RETURN TO REST AREA")}\nPHOTO / CAPTURE ATTEMPTS: UNLIMITED";
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

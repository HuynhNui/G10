using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Core;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using G10.Prototype.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Computer
{
    [Serializable] public sealed class ExpeditionZoneRules
    {
        public string zone = "Zone01";
        [Min(1)] public int baseDays = 5;
        public MapPoi[] restAreas = Array.Empty<MapPoi>();
    }
    [Serializable] public sealed class ExpeditionCreatureAsset
    { public string id; public Texture2D icon; }

    /// <summary>Day/checkpoint owner on GameplayCore. Existing components remain authoritative for gameplay.</summary>
    public sealed class ExpeditionLoop : MonoBehaviour
    {
        private static readonly Vector2 LegacySaveWorldSize = new(24f * ZoneNavigation.DefaultGridSize, 14f * ZoneNavigation.DefaultGridSize);
        public ExpeditionZoneRules[] zones = {
            new() { zone="Zone01" }, new() { zone="Zone02" }, new() { zone="Zone03" }, new() { zone="Zone04" } };
        [Min(0)] public int maxCarryOverDays = 3;
        [SerializeField, Min(1)] private int totalExpeditionDays = 15;
        public ExpeditionCreatureAsset[] creatureCatalog = Array.Empty<ExpeditionCreatureAsset>();
        public G10.Prototype.Missions.SurveyContentDefinition[] contentCatalog = Array.Empty<G10.Prototype.Missions.SurveyContentDefinition>();
        [SerializeField, HideInInspector] private int mapCoordinateVersion;
        private ExpeditionSave save;
        private CabinStationView cabin;
        private CabinZoneSession session;
        private PhotoSurveyZone survey;
        private CreatureInventory inventory;
        private PhotoCaptureService photos;
        private ExpeditionComputerView computer;
        private readonly Dictionary<string, Texture2D> creatureIcons = new();
        private bool initialized, binding, saveReadable = true;
        private bool deathInProgress;
        private bool purchasingUpgrade;
        private bool TransitionBusy => SceneFlowController.Instance != null && SceneFlowController.Instance.IsTransitioning;
        public event Action Changed;
        public bool IsDeathInProgress => deathInProgress;
        private float checkAt;
        public string LastError { get; private set; }
        public int Day => save?.current.day ?? 1;
        public string Zone => save?.current.zone ?? "Zone01";
        public int TotalDays => Mathf.Max(1, totalExpeditionDays);
        public int DaysLeft => Mathf.Max(0, TotalDays - Day + 1);
        public int CapturesToday => Mathf.Max(0, (save?.current.capturesTaken ?? 0) - (save?.current.dayStartCaptures ?? 0));
        public int Deadline => TotalDays;
        public int RemainingDays => DaysLeft;
        public int UpgradeLevel(ShipUpgrade branch) => save?.current.upgrades?.Level(branch) ?? 0;
        public float BaseMovementSpeed => save?.current.upgrades?.baseSpeed > 0 ? save.current.upgrades.baseSpeed : Navigation?.CreateInitialShipState().speed ?? 0;
        public bool Failed { get; private set; }
        public bool Blocked => Failed || !saveReadable || binding || deathInProgress || Navigation != null && Navigation.Ship.Hull <= 0;
        public bool IsInitialized => initialized && !binding;
        public TutorialProgressState TutorialProgress => save?.tutorial;
        public bool SaveTutorialProgress(TutorialProgressState progress)
        {
            if (!IsInitialized || !saveReadable || deathInProgress || progress == null) return false;
            var candidate = ExpeditionSaveStore.Copy(save);
            candidate.tutorial = ExpeditionSaveStore.Copy(progress);
            candidate.tutorial.Normalize();
            CaptureInto(candidate.current);
            return Commit(candidate);
        }
        public bool ResetTutorialProgress()
        {
            if (!SaveTutorialProgress(new TutorialProgressState())) return false;
            cabin.GetComponent<TutorialManager>()?.Bind(this);
            return true;
        }
        public bool RequiredObjectivesComplete => MissionRuntime != null ? MissionRuntime.MainObjectivesComplete : survey != null && survey.IsComplete;
        public ZoneProgressState CurrentProgress => CurrentZone?.progress;
        public ZoneMissionRuntime MissionRuntime => survey?.MissionRuntime;
        public ZoneMapConfig ActiveMap => session != null ? session.ActiveConfig : null;
        public string EndingReached => save?.current.endingReached;
        public IReadOnlyList<ExpeditionJournalEntry> Journal => save.journal;
        public ZoneNavigation Navigation => cabin != null ? cabin.Navigation : null;
        public bool CanRest => !Blocked && !TransitionBusy && !purchasingUpgrade && cabin != null && !cabin.Panels.IsModalOpen;
        public bool NeedsRecovery => Navigation != null && Navigation.Ship.Hull > 0 && Navigation.Ship.Energy <= 0;
        public bool CanRecover => NeedsRecovery && CanRest && RecoveryArea != null;
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
        public bool MigrateAuthoredCoordinates(Func<string, Vector2> worldSizeForZone,
            Func<string, Vector2> locationSizedWorldSizeForZone,
            Func<string, Vector2> displayWorldSizeForZone, Vector2 originalWorldSize)
        {
            if (mapCoordinateVersion >= ZoneMapConfig.CurrentCoordinateVersion || worldSizeForZone == null) return false;
            foreach (ExpeditionZoneRules rule in zones ?? Array.Empty<ExpeditionZoneRules>())
            {
                if (rule == null) continue;
                Vector2 next = worldSizeForZone(rule.zone);
                Vector2 previousWorldSize = mapCoordinateVersion switch
                {
                    1 when locationSizedWorldSizeForZone != null => locationSizedWorldSizeForZone(rule.zone),
                    2 when displayWorldSizeForZone != null => displayWorldSizeForZone(rule.zone),
                    _ => originalWorldSize
                };
                Vector2 scale = new(next.x / Mathf.Max(.01f, previousWorldSize.x), next.y / Mathf.Max(.01f, previousWorldSize.y));
                foreach (MapPoi area in rule.restAreas ?? Array.Empty<MapPoi>())
                    if (area != null) area.mapPosition = Vector2.Scale(area.mapPosition, scale);
            }
            mapCoordinateVersion = ZoneMapConfig.CurrentCoordinateVersion;
            return true;
        }
        private void Awake()
        {
            foreach (var asset in creatureCatalog) if(asset != null && !string.IsNullOrEmpty(asset.id)) creatureIcons[asset.id]=asset.icon;
            foreach (var asset in contentCatalog) if(asset != null && !string.IsNullOrEmpty(asset.id)) creatureIcons[asset.id]=asset.Image;
            saveReadable = ExpeditionSaveStore.TryRead(out save, out string error);
            LastError = error; save ??= new ExpeditionSave();
        }
        private void OnEnable()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            if (inventory != null) { inventory.ItemAdded -= CountCapture; inventory.ItemAdded += CountCapture; }
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (inventory != null) inventory.ItemAdded -= CountCapture;
        }
        private void CountCapture() { if (IsInitialized && !purchasingUpgrade) save.current.capturesTaken++; }
        private IEnumerator Start() { yield return null; BindLoadedZone(); }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        { if (ExpeditionSaveStore.IsZone(scene.name)) StartCoroutine(BindAfterStart()); }
        private IEnumerator BindAfterStart() { yield return null; BindLoadedZone(); }
        public void BindLoadedZone()
        {
            var found = FindAnyObjectByType<CabinStationView>();
            if (found == null || found == cabin && initialized) return;
            binding = true;
            cabin = found;
            session = cabin.GetComponent<CabinZoneSession>();
            if (session == null) session = cabin.gameObject.AddComponent<CabinZoneSession>();
            if (!session.Configure(Zone))
            {
                LastError = "Không tìm thấy cấu hình bản đồ: " + Zone;
                saveReadable = false;
                binding = false;
                return;
            }
            survey = cabin.GetComponent<PhotoSurveyZone>();
            if (inventory != null) inventory.ItemAdded -= CountCapture;
            inventory = cabin.GetComponent<CreatureInventory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            if (inventory != null) inventory.ItemAdded += CountCapture;
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
            if (catcher != null && survey != null && survey.MissionRuntime == null) creatureIcons[survey.creatureId] = catcher.itemIcon;
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            computer = screen.GetComponent<ExpeditionComputerView>();
            if (computer == null) computer = screen.gameObject.AddComponent<ExpeditionComputerView>();
            computer.Initialize(this, screen);
            screen.Expedition = this;
            foreach (var status in screen.GetComponentsInChildren<ShipStatusView>(true)) status.Expedition = this;
            foreach (var mission in screen.GetComponentsInChildren<MissionLogView>(true)) mission.Expedition = this;
            EnsureZone(Zone, 0);
            bool freshDayOne = !save.hasDayStart && save.current.day == 1 && save.current.zones.Count == 1 && !CurrentZone.hasVoyage;
            MigrateSavedCoordinates(CurrentZone);
            try
            {
                if (CurrentZone.hasVoyage) ApplySnapshot(save.current);
                else
                {
                    // The timeline, not scene test state or loose legacy photo files, owns a new voyage.
                    CurrentZone.position = ActiveMap.entryPosition;
                    CurrentZone.heading = ActiveMap.entryHeading;
                    CurrentZone.depth = ActiveMap.entryDepth;
                    CurrentZone.distance = 0;
                    CurrentZone.creatureId = survey != null ? survey.creatureId : null;
                    CurrentZone.creaturePresent = true;
                    CurrentZone.hasVoyage = true;
                    ApplySnapshot(save.current);
                    if (save.current.zones.Count == 1) ResetSummaryBaseline();
                }
                EnsureDailyCreatureSpawns(save.current);
                initialized = true;
                if (freshDayOne && saveReadable)
                {
                    CaptureInto(save.current);
                    var candidate = ExpeditionSaveStore.Copy(save);
                    candidate.dayStart = ExpeditionSaveStore.Copy(candidate.current);
                    candidate.hasDayStart = true;
                    if (!Commit(candidate)) saveReadable = false;
                }
            }
            catch (Exception ex) { saveReadable = false; LastError = "Không khôi phục được dữ liệu: " + ex.Message; }
            binding = false; EvaluateDeadline();
            Changed?.Invoke();
            if (Failed || !saveReadable) ShowFailure();
            if (IsInitialized) cabin.GetComponent<TutorialManager>()?.Bind(this);
        }
        private void EnsureZone(string zone, int carry)
        {
            if (save.current.zones.Exists(z => z.zone == zone)) return;
            save.current.zones.Add(new ExpeditionZoneState { zone=zone, mapCoordinateVersion=ZoneMapConfig.CurrentCoordinateVersion,
                deadline=TotalDays });
        }
        private void MigrateSavedCoordinates(ExpeditionZoneState zone)
        {
            if (zone == null || zone.mapCoordinateVersion >= ZoneMapConfig.CurrentCoordinateVersion || Navigation == null) return;
            Vector2 worldSize = Navigation.MapWorldSize;
            ZoneMapConfig config = cabin != null
                ? cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>(true)?.mapConfig : null;
            Vector2 previousWorldSize = zone.mapCoordinateVersion switch
            {
                1 when config != null => config.LegacyLocationSizedWorldSize,
                2 when config != null => config.LegacyDisplayWorldSize,
                _ => LegacySaveWorldSize
            };
            Vector2 scale = new(worldSize.x / previousWorldSize.x, worldSize.y / previousWorldSize.y);
            if (zone.hasVoyage) zone.position = Vector2.Scale(zone.position, scale);
            foreach (SavedCreatureSpawn spawn in zone.creatureSpawns ?? new List<SavedCreatureSpawn>())
                if (spawn != null) spawn.coordinate = Vector2.Scale(spawn.coordinate, scale);
            zone.mapCoordinateVersion = ZoneMapConfig.CurrentCoordinateVersion;
        }
        private void Update()
        {
            if (initialized && !binding && !deathInProgress && Navigation != null && Navigation.Ship.Hull <= 0)
            {
                StartCoroutine(RollbackVesselDeath());
                return;
            }
            if (!initialized || binding || Time.unscaledTime < checkAt) return;
            if (deathInProgress) return;
            checkAt = Time.unscaledTime + .2f;
            EvaluateDeadline();
            if ((Failed || !saveReadable) && cabin != null && cabin.Panels != null && cabin.Panels.CurrentPanel != computer.gameObject)
                ShowFailure();
        }
        private IEnumerator RollbackVesselDeath()
        {
            deathInProgress = true;
            Navigation.ExpeditionBlocked = true;
            cabin.Brake();
            cabin.CancelDirectInteraction();
            cabin.GetComponent<CaptureMinigameController>()?.Cancel();
            FindAnyObjectByType<G10.Prototype.Dialogue.DialogueController>()?.Cancel();
            var flow = SceneFlowController.Instance;
            if (flow == null)
            {
                LastError = "Khôi phục tàu cần chạy game từ Bootstrap.";
                Debug.LogWarning(LastError, this);
                yield break;
            }
            while (flow.IsTransitioning) yield return null;
            if (!save.hasDayStart)
            {
                // Never manufacture a current-day-start from a mid-day save or Journal checkpoint.
                LastError = "VESSEL LOST\nNo day-start checkpoint in this save.\nContinue and Rest, or start a New Game.";
                flow.RestoreVesselDeath(null, LastError);
                yield break;
            }
            flow.RestoreVesselDeath(() =>
            {
                var candidate = ExpeditionSaveStore.Copy(save);
                candidate.current = ExpeditionSaveStore.Copy(candidate.dayStart);
                candidate.journal.RemoveAll(entry => entry.day >= candidate.current.day);
                if (!Commit(candidate, true)) return null;
                initialized = false; binding = true; Failed = false;
                return candidate.current.zone;
            });
            while (flow.IsTransitioning) yield return null;
            if (!IsInitialized || Navigation.Ship.Hull <= 0)
            { LastError ??= flow.LastError ?? "Không khôi phục được mốc đầu ngày."; yield break; }
            deathInProgress = false;
            EvaluateDeadline();
        }
        public void EvaluateDeadline()
        {
            if (CurrentZone == null) return;
            if (RequiredObjectivesComplete && CurrentZone.completedDay == 0) CurrentZone.completedDay = Day;
            bool before = Failed;
            Failed = Day > TotalDays && string.IsNullOrEmpty(EndingReached);
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
            var zone = snapshot.zones.Find(z => z.zone == snapshot.zone);
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
                foreach (var item in inventory.Items) snapshot.inventory.Add(new SavedCreature { id=item.Id, name=item.Name, quantity=item.Quantity });
            }
            if (photos != null) { snapshot.photos=photos.ExportPhotos(); snapshot.photosTaken=photos.TotalPhotosTaken; }
        }
        private void ApplySnapshot(ExpeditionSnapshot snapshot)
        {
            var zone = snapshot.zones.Find(z => z.zone == snapshot.zone);
            var restored = new List<CreatureInventory.Item>();
            foreach (var item in snapshot.inventory)
            {
                if (item.id == "Adhesive02") continue; // Removed from mission progression; old cargo is safely discarded.
                string id = item.id == "EmmaTube01" ? G10.Prototype.Missions.ZoneOneStory.EmmaBlueprint :
                    item.id == "Creature01" ? "Z1_Creature_01" : item.id == "Creature02" ? "Z1_Creature_02" : item.id;
                if (!creatureIcons.TryGetValue(id, out var icon)) throw new InvalidDataException("Unknown creature: " + id);
                restored.Add(new CreatureInventory.Item(id,item.name,icon,item.quantity));
            }
            if (zone == null) throw new InvalidDataException("Missing zone state.");
            if (survey != null && survey.MissionRuntime == null && zone.creatureId != survey.creatureId)
                throw new InvalidDataException("Encounter ID changed.");
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
                survey.MissionRuntime.HiddenRouteAvailable = zone.progress?.hiddenRouteUnlocked == true;
                if (survey.Story != null && (zone.missionProgress == null || zone.missionProgress.IsEmpty)) survey.Story.Restore(zone.zoneOneStoryProgress);
                else survey.MissionRuntime.RestoreProgress(zone.missionProgress);
            }
            session?.RefreshTerrain();
            EnsureDailyCreatureSpawns(snapshot);
            var progress = snapshot.upgrades ??= new ShipUpgradeProgress();
            if (progress.baseSpeed <= 0) progress.baseSpeed = Navigation.CreateInitialShipState().speed;
            // Explicit progression survives designer preview overrides without reapplying additive bonuses.
            bool upgraded = progress.hullLevel > 0 || progress.propulsionLevel > 0 || progress.efficiencyLevel > 0;
            Navigation.Ship.Restore(snapshot.hasShipState && (upgraded || deathInProgress || !Navigation.UseSceneShipSettingsOnLoad) ? snapshot.ship : Navigation.CreateInitialShipState());
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
            save.current.dayStartCaptures=save.current.capturesTaken;
        }
        public bool Rest()
            => AdvanceDay(false);
        public bool RecoverShip()
            => AdvanceDay(true);
        internal bool AdvanceRestDay(SceneFlowController presenter, bool recovery)
            => presenter != null && presenter.OwnsRestTransition(this) && AdvanceDay(recovery, true);
        private bool AdvanceDay(bool recovery, bool presented = false)
        {
            bool available = !Blocked && !purchasingUpgrade && cabin != null && !cabin.Panels.IsModalOpen &&
                (!recovery || NeedsRecovery && RecoveryArea != null);
            if (!available || TransitionBusy && !presented)
            {
                LastError = recovery ? "Không có điểm cứu hộ hợp lệ." : "Không thể nghỉ khi hành trình đang bị khóa.";
                return false;
            }
            EvaluateDeadline(); CaptureInto(save.current);
            var candidate=ExpeditionSaveStore.Copy(save);
            var entry=new ExpeditionJournalEntry { day=Day, zone=Zone, checkpoint=ExpeditionSaveStore.Copy(save.current),
                distance=Mathf.Max(0,TotalDistance()-save.current.dayStartDistance),
                photos=Mathf.Max(0,save.current.photosTaken-save.current.dayStartPhotos),
                captures=Mathf.Max(0,save.current.capturesTaken-save.current.dayStartCaptures),
                tasks=Mathf.Max(0,CompletedTasks()-save.current.dayStartTasks) };
            candidate.journal.RemoveAll(e=>e.day>=Day); candidate.journal.Add(entry);
            candidate.current.day++;
            candidate.current.dayStartDistance=TotalDistance(); candidate.current.dayStartPhotos=save.current.photosTaken;
            candidate.current.dayStartCaptures=save.current.capturesTaken; candidate.current.dayStartTasks=CompletedTasks();
            candidate.current.ship?.Refill();
            if (recovery) candidate.current.zones.Find(z => z.zone == Zone).position = RecoveryArea.mapPosition;
            EnsureDailyCreatureSpawns(candidate.current);
            candidate.dayStart = ExpeditionSaveStore.Copy(candidate.current);
            candidate.hasDayStart = true;
            if (!Commit(candidate)) { EnsureDailyCreatureSpawns(save.current); return false; }
            Navigation.Ship.Restore(candidate.current.ship);
            if (recovery) Navigation.RestoreVoyage(CurrentZone.position, CurrentZone.heading, CurrentZone.depth, CurrentZone.distance);
            EnsureDailyCreatureSpawns(save.current);
            EvaluateDeadline(); Changed?.Invoke(); return true;
        }
        public bool RestoreDay(int day)
        {
            if (!saveReadable || binding || deathInProgress || purchasingUpgrade || TransitionBusy || cabin != null && cabin.Panels.IsModalOpen) return false;
            var entry=save.journal.Find(e=>e.day==day);
            if (entry == null) return false;
            var candidate=ExpeditionSaveStore.Copy(save);
            candidate.current=ExpeditionSaveStore.Copy(entry.checkpoint);
            if (candidate.dayStart?.day != candidate.current.day) { candidate.dayStart = null; candidate.hasDayStart = false; }
            if (candidate.current.zone == Zone)
                MigrateSavedCoordinates(candidate.current.zones.Find(z => z.zone == candidate.current.zone));
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
            Changed?.Invoke();
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
            if (!initialized || !saveReadable || binding || deathInProgress || purchasingUpgrade || Navigation != null && Navigation.Ship.Hull <= 0) return false;
            CaptureInto(save.current); return Commit(save);
        }
        /// <summary>Permanent photo removal, independent of mission rewards and cumulative camera usage.</summary>
        internal bool CommitPhotoDeletion(PhotoCaptureService source, string photoId)
        {
            if (source == null || source != photos || !IsInitialized || !saveReadable || Blocked ||
                purchasingUpgrade || TransitionBusy || string.IsNullOrEmpty(photoId))
            { LastError = "Không thể xóa ảnh lúc này."; return false; }
            var candidate = ExpeditionSaveStore.Copy(save);
            CaptureInto(candidate.current);
            RemovePhoto(candidate.current, photoId);
            RemovePhoto(candidate.dayStart, photoId);
            foreach (var entry in candidate.journal) RemovePhoto(entry.checkpoint, photoId);
            // Sanitize the recovery copy too: a corrupt primary must not resurrect a deleted photo.
            return Commit(candidate, discardFuture: true);
        }
        private static void RemovePhoto(ExpeditionSnapshot snapshot, string photoId)
            => snapshot?.photos?.RemoveAll(photo => photo.id == photoId);
        public bool CanPurchaseUpgrade(ShipUpgrade branch)
        {
            int level = UpgradeLevel(branch);
            int cost = RegularShipUpgradeRules.Cost(branch, level);
            return IsInitialized && !Blocked && !TransitionBusy && !purchasingUpgrade && cost > 0 && inventory != null &&
                cabin.Panels != null && !cabin.Panels.IsModalOpen &&
                inventory.GetCount(RegularShipUpgradeRules.MaterialId(level)) >= cost;
        }
        /// <summary>Ship stats, explicit level, materials and timeline commit form one transaction.</summary>
        public bool TryPurchaseUpgrade(ShipUpgrade branch)
        {
            if (!CanPurchaseUpgrade(branch)) return false;
            var previousShip = Navigation.Ship.Export();
            var nextShip = previousShip.Copy();
            int level = UpgradeLevel(branch);
            if (!RegularShipUpgradeRules.TryApply(nextShip, branch, level + 1, BaseMovementSpeed)) return false;
            var previousItems = new List<CreatureInventory.Item>(inventory.Items);
            var candidate = ExpeditionSaveStore.Copy(save);
            candidate.current.upgrades.SetLevel(branch, level + 1);
            candidate.current.upgrades.baseSpeed = BaseMovementSpeed;
            bool committed = false;
            purchasingUpgrade = true;
            try
            {
                var cost = new Dictionary<string, int> { [RegularShipUpgradeRules.MaterialId(level)] = RegularShipUpgradeRules.Cost(branch, level) };
                if (!inventory.TryConsume(cost, notify:false)) return false;
                Navigation.Ship.Restore(nextShip);
                CaptureInto(candidate.current);
                committed = Commit(candidate);
                return committed;
            }
            finally
            {
                if (!committed)
                {
                    Navigation.Ship.Restore(previousShip);
                    inventory.RestoreItems(previousItems, notify:false);
                }
                purchasingUpgrade = false;
                inventory.NotifyChanged();
                Changed?.Invoke();
            }
        }
        /// <summary>Explicit New Game only. Suspend the old runtime before it can save over the fresh timeline.</summary>
        public bool ResetGameProgress()
        {
            try
            {
                var fresh = ExpeditionSaveStore.ResetGameProgress();
                save = fresh;
                initialized = false;
                binding = true;
                saveReadable = true;
                Failed = false;
                deathInProgress = false;
                LastError = null;
                if (Navigation != null) { Navigation.ExpeditionBlocked = true; Navigation.Brake(); }
                if (cabin != null && cabin.Panels != null) cabin.Panels.LockedPanel = null;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { LastError = "Không bắt đầu được hành trình mới: " + ex.Message; return false; }
        }
        [ContextMenu("Reset Save Data (Play Mode)")]
        private void DeveloperResetGameProgress()
        {
            if (!Application.isPlaying) return;
            if (SceneFlowController.Instance != null) SceneFlowController.Instance.StartNewGame();
            else if (ResetGameProgress()) RebindCurrentZone();
        }
        public void RebindCurrentZone()
        {
            initialized = false;
            BindLoadedZone();
        }
        public bool RecordEnding(string endingId)
        {
            if (!IsInitialized || !saveReadable || string.IsNullOrWhiteSpace(endingId)) return false;
            CaptureInto(save.current);
            var candidate = ExpeditionSaveStore.Copy(save);
            candidate.current.endingReached = endingId;
            return Commit(candidate);
        }
        public bool PrepareZone(string next)
        {
            if (next==Zone) return saveReadable && !Failed;
            bool visited=save.current.zones.Exists(z=>z.zone==next);
            if (!IsInitialized || Blocked || CurrentProgress?.exitUnlocked != true || ActiveMap == null ||
                next != ActiveMap.destinationZone || Rules(next) == null) return false;
            EvaluateDeadline(); CaptureInto(save.current);
            var previous=ExpeditionSaveStore.Copy(save);
            var carriedProgress = MissionRuntime?.ExportProgress();
            save.current.zone=next; EnsureZone(next,0);
            if (!visited && carriedProgress != null)
            {
                // Objective IDs and discovered locations stay in their own zone. Ship-wide unlocks travel.
                var progress = CurrentZone.missionProgress;
                progress.researchData = carriedProgress.researchData;
                progress.collectedItems = carriedProgress.collectedItems;
                progress.unlockedRecipes = carriedProgress.unlockedRecipes;
                progress.worldFlags = carriedProgress.worldFlags;
                progress.unlockedZones = carriedProgress.unlockedZones;
            }
            if (!Commit(save)) { save=previous; return false; }
            initialized=false; binding=true;
            if (Navigation != null) { Navigation.ExpeditionBlocked = true; Navigation.Brake(); }
            return true;
        }
        private void EnsureDailyCreatureSpawns(ExpeditionSnapshot snapshot)
        {
            if (snapshot == null || survey == null || survey.locations == null) return;
            var zone = snapshot.zones.Find(z => z.zone == snapshot.zone);
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
            float half = (Navigation != null ? Navigation.MapGridSize : ZoneNavigation.DefaultGridSize) * .5f - .25f;
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
        private bool InsideMissionCell(MapPoi poi, Vector2 point)
        {
            Vector2 delta = point - poi.mapPosition;
            float half = (Navigation != null ? Navigation.MapGridSize : ZoneNavigation.DefaultGridSize) * .5f;
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
        public string StatusText() => $"DAY {Day:00}   •   {Zone}   •   DAY LEFT: {DaysLeft}\nX {Navigation?.Position.x:0.0}  Y {Navigation?.Position.y:0.0}\nREST: {(CanRest ? "AVAILABLE" : "UNAVAILABLE")}";
        public string RestAreasText()
        {
            var text=new System.Text.StringBuilder();
            foreach(var area in Rules(Zone)?.restAreas ?? Array.Empty<MapPoi>())
                if(area!=null)text.Append($"\n{area.id}: X {area.mapPosition.x:0} Y {area.mapPosition.y:0} • R {area.arrivalRadius:0} m");
            return text.ToString();
        }
        public string MissionText() => $"{Zone}  •  DAY {Day:00}  •  DAY LEFT: {DaysLeft}\nEXPEDITION LIMIT: {TotalDays} DAYS\n\n" +
            (survey != null ? survey.TaskDescription() : "NO REQUIRED OBJECTIVES CONFIGURED") +
            (Failed ? "\n\nMISSION FAILED — RESTORE A JOURNAL CHECKPOINT" : "");
        private void OnApplicationPause(bool paused) { if(paused) SaveCurrent(); }
        private void OnApplicationQuit() => SaveCurrent();
    }
}

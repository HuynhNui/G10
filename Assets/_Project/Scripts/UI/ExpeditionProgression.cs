using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using UnityEngine;

namespace G10.Prototype.UI
{
    /// <summary>Mission results unlock routes; only physical arrival requests a transition.</summary>
    [DisallowMultipleComponent]
    public sealed class ExpeditionProgression : MonoBehaviour
    {
        private ExpeditionLoop loop;
        private CabinZoneSession session;
        private CabinStationView cabin;
        private DialogueController dialogue;
        private bool exitArmed, finalArmed;
        private bool exitObserved, exitWasUnlocked, exitTransitionRequested;
        private float exitMovementBaseline;
        private bool finalObserved, finalWasUnlocked;
        private float finalMovementBaseline;
        private uint observedVoyageRevision;
        private float nextCheck;
        public bool ExitArmed => exitArmed;
        public bool FinalArmed => finalArmed;
        public bool ExitNearby => session?.ActiveConfig?.exitArea != null && cabin?.Navigation != null &&
            !string.IsNullOrEmpty(session.ActiveConfig.destinationZone) &&
            Vector2.Distance(cabin.Navigation.Position, session.ActiveConfig.exitArea.mapPosition) <=
                session.ActiveConfig.exitArea.arrivalRadius + session.ActiveConfig.GridSize;
        public string ExitPrompt => DescribeExit(session?.ActiveConfig, loop?.MissionRuntime,
            cabin?.Navigation != null ? cabin.Navigation.Position : Vector2.zero);
        public bool FinalNearby => session?.ActiveConfig?.HiddenDestinationAvailable(loop?.MissionRuntime) == true &&
            cabin?.Navigation != null && Vector2.Distance(cabin.Navigation.Position, session.ActiveConfig.finalHiddenPoint.mapPosition) <=
                session.ActiveConfig.finalHiddenPoint.arrivalRadius + session.ActiveConfig.GridSize;
        public string FinalPrompt => DescribeFinal(session?.ActiveConfig, loop?.MissionRuntime,
            cabin?.Navigation != null ? cabin.Navigation.Position : Vector2.zero);

        public static string DescribeFinal(ZoneMapConfig config, ZoneMissionRuntime missions, Vector2 position)
        {
            if (config?.finalHiddenPoint == null || config.hiddenLocationIds == null || config.hiddenLocationIds.Length == 0) return "";
            if (!config.HiddenDestinationAvailable(missions)) return "FINAL SIGNAL LOCKED · Khám phá đủ các địa điểm ẩn";
            var point = config.finalHiddenPoint;
            if (point.Contains(position)) return "FINAL SIGNAL — APPROACH · Di chuyển tàu để hoàn tất tuyến ẩn";
            if (Vector2.Distance(position, point.mapPosition) <= point.arrivalRadius + config.GridSize)
                return "FINAL SIGNAL — APPROACH · Đi vào vùng viền để hoàn tất tuyến ẩn";
            return $"FINAL SIGNAL — READY · Đi đến ({point.mapPosition.x:0}, {point.mapPosition.y:0})";
        }

        public static string DescribeExit(ZoneMapConfig config, ZoneMissionRuntime missions, Vector2 position)
        {
            if (config?.exitArea == null || string.IsNullOrEmpty(config.destinationZone) || missions == null) return "";
            if (!missions.MainObjectivesComplete)
                return missions.MainLocationsComplete ? "EXIT LOCKED · Hoàn tất nâng cấp tuyến" :
                    "EXIT LOCKED · Hoàn thành khảo sát";
            var exit = config.exitArea;
            if (exit.Contains(position)) return $"ZONE EXIT — APPROACH · Di chuyển tàu để đến {config.destinationZone}";
            if (Vector2.Distance(position, exit.mapPosition) <= exit.arrivalRadius + config.GridSize)
                return $"ZONE EXIT — APPROACH · Đi vào vùng viền để đến {config.destinationZone}";
            return $"ZONE EXIT — READY · Đi đến ({exit.mapPosition.x:0}, {exit.mapPosition.y:0})";
        }

        public void Initialize(ExpeditionLoop owner, CabinZoneSession zoneSession, CabinStationView view)
        {
            if (loop != null) loop.Changed -= OnVoyageChanged;
            loop = owner;
            session = zoneSession;
            cabin = view;
            dialogue = FindAnyObjectByType<DialogueController>();
            exitArmed = finalArmed = false;
            exitObserved = exitWasUnlocked = exitTransitionRequested = false;
            finalObserved = finalWasUnlocked = false;
            observedVoyageRevision = cabin?.Navigation != null ? cabin.Navigation.VoyageRevision : 0;
            if (isActiveAndEnabled && loop != null) loop.Changed += OnVoyageChanged;
            nextCheck = 0;
        }

        private void OnEnable() { if (loop != null) loop.Changed += OnVoyageChanged; }
        private void OnDisable() { if (loop != null) loop.Changed -= OnVoyageChanged; }
        private void OnVoyageChanged()
        {
            // Rest/checkpoint restore can apply a voyage in-place without configuring a new zone.
            // Its saved distance is history, never fresh player movement into the exit.
            exitArmed = exitObserved = exitWasUnlocked = exitTransitionRequested = false;
            finalArmed = finalObserved = finalWasUnlocked = false;
            nextCheck = 0;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .15f;
            Evaluate();
        }

        public void Evaluate()
        {
            if (loop == null || !loop.IsInitialized || loop.Blocked || session?.ActiveConfig == null || cabin?.Navigation == null ||
                SceneFlowController.Instance != null && SceneFlowController.Instance.IsTransitioning) return;
            if (observedVoyageRevision != cabin.Navigation.VoyageRevision)
            {
                OnVoyageChanged();
                observedVoyageRevision = cabin.Navigation.VoyageRevision;
            }
            var state = loop.CurrentProgress;
            var config = session.ActiveConfig;
            var missions = loop.MissionRuntime;
            if (state == null || missions == null) return;
            missions.HiddenRouteAvailable = state.hiddenRouteUnlocked;
            bool changed = false;
            bool complete = missions.MainObjectivesComplete;
            if (complete && !state.mainObjectivesComplete) { state.mainObjectivesComplete = true; changed = true; }
            if (complete && !state.exitUnlocked && !string.IsNullOrEmpty(config.destinationZone))
            { state.exitUnlocked = true; changed = true; }
            if (config.UsesAlternate(missions) && !state.rockDestroyed)
            { state.rockDestroyed = true; changed = true; }
            session.RefreshTerrain();
            bool hiddenComplete = config.HiddenDestinationAvailable(missions);
            if (hiddenComplete && !state.hiddenRouteComplete) { state.hiddenRouteComplete = true; changed = true; }
            if (changed && !loop.SaveCurrent()) return;
            if (!string.IsNullOrEmpty(loop.EndingReached)) return;
            if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused || cabin.Panels == null || cabin.Panels.IsModalOpen) return;

            bool atExit = config.exitArea != null && config.exitArea.Contains(cabin.Navigation.Position);
            ObserveExit(state.exitUnlocked && complete, atExit);
            if (exitArmed && !exitTransitionRequested && CanExit() && SceneFlowController.Instance != null)
            {
                exitTransitionRequested = true;
                SceneFlowController.Instance.LoadZone(config.destinationZone);
                // A failed save must remain retryable, but one accepted transition is latched.
                if (!SceneFlowController.Instance.IsTransitioning) exitTransitionRequested = false;
                return;
            }
            bool atFinal = config.finalHiddenPoint != null && config.finalHiddenPoint.Contains(cabin.Navigation.Position);
            ObserveArrival(hiddenComplete && state.hiddenRouteComplete, atFinal,
                ref finalObserved, ref finalWasUnlocked, ref finalMovementBaseline, ref finalArmed);
            if (finalArmed && hiddenComplete && state.hiddenRouteComplete && atFinal)
            { ReachEnding("hidden"); return; }
            if (state.mainObjectivesComplete && config.hiddenLocationIds.Length > 0)
            {
                if (state.endingChoice == "end") { ReachEnding("normal"); return; }
                if (string.IsNullOrEmpty(state.endingChoice) && dialogue != null && !dialogue.IsActive)
                {
                    cabin.Brake();
                    dialogue.TryBeginChoice(new DialogueLine(DialogueSpeakerKind.Guide, "EXPEDITION",
                        "Mục tiêu chính đã hoàn thành. Bạn muốn kết thúc chuyến thám hiểm hay tiếp tục tìm những tín hiệu chưa được khám phá?"),
                        "KẾT THÚC THÁM HIỂM", "TIẾP TỤC KHÁM PHÁ", SelectEndingChoice);
                }
            }
        }

        private void ObserveExit(bool unlocked, bool atExit)
            => ObserveArrival(unlocked, atExit, ref exitObserved, ref exitWasUnlocked, ref exitMovementBaseline, ref exitArmed);

        private void ObserveArrival(bool unlocked, bool inside, ref bool observed, ref bool wasUnlocked,
            ref float movementBaseline, ref bool armed)
        {
            float distance = cabin.Navigation.DistanceTravelled;
            if (!observed || wasUnlocked != unlocked || distance < movementBaseline)
            {
                // Configure runs before RestoreVoyage. Observe only the restored, playable voyage,
                // so Continue/unlocking inside never turns a saved position into an arrival event.
                observed = true;
                wasUnlocked = unlocked;
                movementBaseline = distance;
                armed = unlocked && !inside;
                return;
            }
            if (!unlocked) { armed = false; return; }
            if (!inside || distance > movementBaseline + .01f) armed = true;
        }

        public bool CanExit() => loop != null && loop.IsInitialized && !loop.Blocked &&
            loop.CurrentProgress?.exitUnlocked == true && loop.MissionRuntime.MainObjectivesComplete &&
            session?.ActiveConfig != null && cabin?.Navigation != null &&
            !string.IsNullOrEmpty(session.ActiveConfig.destinationZone) &&
            session.ActiveConfig.exitArea != null && session.ActiveConfig.exitArea.Contains(cabin.Navigation.Position);

        public void SelectEndingChoice(int choice)
        {
            if (choice < 0 || choice > 1 || loop == null || loop.Blocked || loop.CurrentProgress == null ||
                !loop.MissionRuntime.MainObjectivesComplete || session.ActiveConfig.hiddenLocationIds.Length == 0 ||
                !string.IsNullOrEmpty(loop.CurrentProgress.endingChoice)) return;
            var state = loop.CurrentProgress;
            state.endingChoice = choice == 0 ? "end" : "explore";
            state.hiddenRouteUnlocked = choice == 1;
            if (!loop.SaveCurrent())
            { state.endingChoice = null; state.hiddenRouteUnlocked = false; return; }
            loop.MissionRuntime.HiddenRouteAvailable = state.hiddenRouteUnlocked;
            if (choice == 0) ReachEnding("normal");
        }

        private void ReachEnding(string ending)
        {
            if (SceneFlowController.Instance == null || SceneFlowController.Instance.IsTransitioning) return;
            if (loop.RecordEnding(ending)) SceneFlowController.Instance.LoadEnding();
        }
    }
}

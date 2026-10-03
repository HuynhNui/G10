using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Dialogue;
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
        private float nextCheck;

        public void Initialize(ExpeditionLoop owner, CabinZoneSession zoneSession, CabinStationView view)
        {
            loop = owner;
            session = zoneSession;
            cabin = view;
            dialogue = FindAnyObjectByType<DialogueController>();
            exitArmed = finalArmed = false;
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
            if (loop == null || !loop.IsInitialized || loop.Blocked || session.ActiveConfig == null ||
                SceneFlowController.Instance != null && SceneFlowController.Instance.IsTransitioning) return;
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
            bool hiddenComplete = state.hiddenRouteUnlocked && config.hiddenLocationIds.Length > 0;
            foreach (string id in config.hiddenLocationIds)
                hiddenComplete &= missions.config.FindLocation(id) != null &&
                    (missions.IsLocationRevealed(id) || missions.IsLocationComplete(id));
            if (hiddenComplete && !state.hiddenRouteComplete) { state.hiddenRouteComplete = true; changed = true; }
            if (changed && !loop.SaveCurrent()) return;
            if (!string.IsNullOrEmpty(loop.EndingReached)) return;
            if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused || cabin.Panels == null || cabin.Panels.IsModalOpen) return;

            bool atExit = config.exitArea != null && config.exitArea.Contains(cabin.Navigation.Position);
            if (state.exitUnlocked && !atExit) exitArmed = true;
            if (exitArmed && CanExit())
            {
                SceneFlowController.Instance?.LoadZone(config.destinationZone);
                return;
            }
            bool atFinal = config.finalHiddenPoint != null && config.finalHiddenPoint.Contains(cabin.Navigation.Position);
            if (state.hiddenRouteComplete && !atFinal) finalArmed = true;
            if (finalArmed && state.hiddenRouteComplete && atFinal)
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

        public bool CanExit() => loop != null && loop.IsInitialized && !loop.Blocked &&
            loop.CurrentProgress?.exitUnlocked == true && loop.MissionRuntime.MainObjectivesComplete &&
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

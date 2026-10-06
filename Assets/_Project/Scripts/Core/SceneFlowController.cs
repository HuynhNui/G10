using System.Collections;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Core
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowController : MonoBehaviour
    {
        public const string BootstrapScene = "Bootstrap";
        public const string MainMenuScene = "MainMenu";
        public const string GameplayCoreScene = "GameplayCore";
        public const string EndingScene = "Ending";
        private const string CabinScene = "Zone01";
        [SerializeField, Min(0)] private float fadeSeconds = .25f;
        [SerializeField, Range(1f, 1.5f)] private float restDayTextSeconds = 1.2f;
        public static SceneFlowController Instance { get; private set; }
        public bool IsTransitioning { get; private set; }
        // Logical zone ID; the existing cabin scene is shared by all four zones.
        public string CurrentZoneScene { get; private set; }
        public string LastError { get; private set; }
        private CanvasGroup fade;
        private string endingId;
        private ExpeditionLoop restOwner;
        private TMPro.TMP_Text transitionLabel;
        public string DayLeftPresentationText { get; private set; }
        internal bool OwnsRestTransition(ExpeditionLoop loop) => IsTransitioning && restOwner == loop;
        public static string FormatDayLeft(int daysLeft) => $"DAY LEFT: {Mathf.Max(0, daysLeft)}";

        public bool PresentRest(ExpeditionLoop loop, bool recovery = false)
        {
            if (IsTransitioning || loop == null || !loop.IsInitialized || (recovery ? !loop.CanRecover : !loop.CanRest)) return false;
            restOwner = loop;
            BeginTransition(RestThenReturn(loop, recovery));
            return true;
        }
        private IEnumerator RestThenReturn(ExpeditionLoop loop, bool recovery)
        {
            if (!loop.AdvanceRestDay(this, recovery)) { LastError = loop.LastError; yield break; }
            if (!loop.Failed) FindAnyObjectByType<CabinStationView>()?.ClosePanel();
            DayLeftPresentationText = FormatDayLeft(loop.DaysLeft);
            ShowTransitionLabel("RestDayLeft", DayLeftPresentationText);
            yield return new WaitForSecondsRealtime(Mathf.Clamp(restDayTextSeconds, 1f, 1.5f));
            HideTransitionLabel();
        }

        private void ShowTransitionLabel(string name, string text)
        {
            HideTransitionLabel();
            var obj = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            obj.transform.SetParent(fade.transform, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            transitionLabel = obj.GetComponent<TMPro.TextMeshProUGUI>();
            transitionLabel.text = text; transitionLabel.fontSize = 36;
            transitionLabel.alignment = TMPro.TextAlignmentOptions.Center;
            transitionLabel.color = Color.white; transitionLabel.raycastTarget = false;
        }
        private void HideTransitionLabel()
        {
            if (transitionLabel != null) { transitionLabel.gameObject.SetActive(false); Destroy(transitionLabel.gameObject); }
            transitionLabel = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start() { if (SceneManager.GetActiveScene().name == BootstrapScene) LoadMainMenu(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void LoadMainMenu() => BeginTransition(LoadSingleScene(MainMenuScene));

        public void StartNewGame()
        {
            if (IsTransitioning) return;
            try
            {
                var loop = FindAnyObjectByType<ExpeditionLoop>();
                if (loop != null)
                {
                    if (!loop.ResetGameProgress()) { LastError = loop.LastError; return; }
                }
                else ExpeditionSaveStore.ResetGameProgress();
                endingId = null;
                BeginTransition(LoadGameplayZone(CabinScene));
            }
            catch (System.Exception ex) when (ex is System.IO.IOException || ex is System.UnauthorizedAccessException)
            { LastError = "Cannot create a new voyage: " + ex.Message; Debug.LogError(LastError, this); }
        }

        public void ContinueGame()
        {
            if (IsTransitioning) return;
            if (!ExpeditionSaveStore.TryRead(out var saved, out var error) || saved == null)
            { LastError = error ?? "No saved voyage."; return; }
            endingId = saved.current.endingReached;
            BeginTransition(string.IsNullOrEmpty(endingId) ? LoadGameplayZone(saved.current.zone) : LoadSingleScene(EndingScene));
        }

        public void LoadZone(string zoneId)
        {
            if (IsTransitioning || !ExpeditionSaveStore.IsZone(zoneId)) return;
            var loop = FindAnyObjectByType<ExpeditionLoop>();
            var route = FindAnyObjectByType<ExpeditionProgression>();
            if (loop == null || route == null || !route.CanExit() || loop.ActiveMap.destinationZone != zoneId) return;
            if (!loop.PrepareZone(zoneId)) { LastError = loop.LastError; return; }
            BeginTransition(LoadGameplayZone(zoneId));
        }
        // Journal restore is separate from the physical exit contract.
        public void RestoreZone(string zoneId)
        { if (!IsTransitioning && ExpeditionSaveStore.IsZone(zoneId)) BeginTransition(LoadGameplayZone(zoneId)); }

        public void RestoreVesselDeath(System.Func<string> restoreDayStart, string unavailableReason = null)
        { if (!IsTransitioning) BeginTransition(DeathThenRestore(restoreDayStart, unavailableReason)); }

        private IEnumerator DeathThenRestore(System.Func<string> restoreDayStart, string unavailableReason)
        {
            ShowTransitionLabel("VesselDeath", unavailableReason ?? "VESSEL LOST\nReturning to the start of the day");
            yield return new WaitForSecondsRealtime(1f);
            HideTransitionLabel();
            if (unavailableReason != null)
            {
                // Legacy timelines cannot reconstruct a day they never recorded. Keep their save untouched.
                yield return LoadSingleScene(MainMenuScene);
                LastError = unavailableReason;
                yield break;
            }
            string zone = restoreDayStart();
            if (!ExpeditionSaveStore.IsZone(zone)) { LastError = "Could not restore day-start save."; yield break; }
            yield return LoadGameplayZone(zone);
        }

        public void LoadEnding()
        {
            if (IsTransitioning) return;
            var loop = FindAnyObjectByType<ExpeditionLoop>();
            if (loop == null || loop.Blocked || string.IsNullOrEmpty(loop.EndingReached)) return;
            endingId = loop.EndingReached;
            BeginTransition(LoadSingleScene(EndingScene));
        }
        public void QuitGame()
        {
            FindAnyObjectByType<ExpeditionLoop>()?.SaveCurrent();
#if UNITY_EDITOR
            Debug.Log("Quit requested. Application.Quit is ignored in the Unity Editor.", this);
#else
            Application.Quit();
#endif
        }
        private void BeginTransition(IEnumerator transition)
        {
            if (IsTransitioning) return;
            LastError = null;
            StartCoroutine(RunTransition(transition));
        }
        private IEnumerator RunTransition(IEnumerator transition)
        {
            IsTransitioning = true;
            PauseMenuController.Instance?.Resume();
            var cabin = FindAnyObjectByType<CabinStationView>();
            cabin?.Brake();
            if (cabin != null) cabin.Navigation.TransitionBlocked = true;
            EnsureFade();
            fade.blocksRaycasts = true;
            try
            {
                yield return FadeTo(1);
                yield return transition;
                yield return FadeTo(0);
            }
            finally
            {
                HideTransitionLabel();
                restOwner = null;
                cabin = FindAnyObjectByType<CabinStationView>();
                if (cabin != null) cabin.Navigation.TransitionBlocked = false;
                fade.alpha = 0;
                fade.blocksRaycasts = false;
                IsTransitioning = false;
            }
        }
        private IEnumerator LoadSingleScene(string sceneName)
        {
            FindAnyObjectByType<ExpeditionLoop>()?.SaveCurrent();
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            CurrentZoneScene = null;
            if (sceneName == EndingScene)
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var canvas = root.GetComponentInChildren<Canvas>();
                    if (canvas == null) continue;
                    (canvas.GetComponent<EndingPresentation>() ?? canvas.gameObject.AddComponent<EndingPresentation>()).Show(endingId);
                    break;
                }
        }
        private IEnumerator LoadGameplayZone(string zoneId)
        {
            if (!SceneManager.GetSceneByName(GameplayCoreScene).isLoaded)
                yield return SceneManager.LoadSceneAsync(GameplayCoreScene, LoadSceneMode.Single);
            if (!SceneManager.GetSceneByName(CabinScene).isLoaded)
                yield return SceneManager.LoadSceneAsync(CabinScene, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(CabinScene));
            yield return null;
            var loop = FindAnyObjectByType<ExpeditionLoop>();
            var cabin = FindAnyObjectByType<CabinStationView>();
            if (cabin != null) cabin.Navigation.TransitionBlocked = true;
            loop?.RebindCurrentZone();
            if (loop == null || !loop.IsInitialized || loop.Zone != zoneId)
            {
                LastError = loop?.LastError ?? "Could not initialize the zone.";
                Debug.LogError(LastError, this);
                yield break;
            }
            CurrentZoneScene = zoneId;
            cabin?.ClosePanel();
            loop.SaveCurrent();
        }
        private void EnsureFade()
        {
            if (fade != null) return;
            var overlay = new GameObject("ZoneTransitionFade", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup));
            overlay.transform.SetParent(transform, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            fade = overlay.GetComponent<CanvasGroup>();
            var image = new GameObject("Fade", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            image.transform.SetParent(overlay.transform, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            image.GetComponent<UnityEngine.UI.Image>().color = Color.black;
            fade.alpha = 0;
        }
        private IEnumerator FadeTo(float target)
        {
            float start = fade.alpha;
            for (float elapsed = 0; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            { fade.alpha = Mathf.Lerp(start, target, elapsed / fadeSeconds); yield return null; }
            fade.alpha = target;
        }
    }
}

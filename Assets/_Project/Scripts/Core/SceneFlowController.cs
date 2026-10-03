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
        public static SceneFlowController Instance { get; private set; }
        public bool IsTransitioning { get; private set; }
        // Logical zone ID; the existing cabin scene is shared by all four zones.
        public string CurrentZoneScene { get; private set; }
        public string LastError { get; private set; }
        private CanvasGroup fade;
        private string endingId;

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

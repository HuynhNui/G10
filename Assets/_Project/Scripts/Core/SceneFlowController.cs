using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using G10.Prototype.Computer;

namespace G10.Prototype.Core
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowController : MonoBehaviour
    {
        public const string BootstrapScene = "Bootstrap";
        public const string MainMenuScene = "MainMenu";
        public const string GameplayCoreScene = "GameplayCore";
        public const string EndingScene = "Ending";

        private static readonly string[] ZoneScenes =
        {
            "Zone01",
            "Zone02",
            "Zone03",
            "Zone04"
        };

        public static SceneFlowController Instance { get; private set; }

        public bool IsTransitioning { get; private set; }
        public string CurrentZoneScene { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == BootstrapScene)
            {
                LoadMainMenu();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadMainMenu()
        {
            BeginTransition(LoadSingleScene(MainMenuScene));
        }

        public void StartNewGame()
        {
            ExpeditionSaveStore.TryRead(out var saved, out _);
            LoadZone(saved?.current.zone ?? ZoneScenes[0]);
        }

        public void LoadZone(string zoneScene)
        {
            if (IsTransitioning) return;
            var loop=FindAnyObjectByType<ExpeditionLoop>();
            if (loop != null && !loop.PrepareZone(zoneScene)) return;
            if (!IsZoneScene(zoneScene))
            {
                Debug.LogError($"Unknown zone scene: {zoneScene}", this);
                return;
            }

            BeginTransition(LoadGameplayZone(zoneScene));
        }
        public void RestoreZone(string zoneScene)
        { if(IsZoneScene(zoneScene)) BeginTransition(LoadGameplayZone(zoneScene)); }

        public void LoadEnding()
        {
            if (IsTransitioning) return;
            var loop=FindAnyObjectByType<ExpeditionLoop>();
            if(loop != null && (loop.Blocked || !loop.RequiredObjectivesComplete)) return;
            loop?.SaveCurrent();
            BeginTransition(LoadSingleScene(EndingScene));
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Quit requested. Application.Quit is ignored in the Unity Editor.", this);
#else
            Application.Quit();
#endif
        }

        private void BeginTransition(IEnumerator transition)
        {
            if (IsTransitioning)
            {
                return;
            }

            StartCoroutine(RunTransition(transition));
        }

        private IEnumerator RunTransition(IEnumerator transition)
        {
            IsTransitioning = true;
            try
            {
                while (transition.MoveNext())
                {
                    yield return transition.Current;
                }
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        private IEnumerator LoadSingleScene(string sceneName)
        {
            FindAnyObjectByType<ExpeditionLoop>()?.SaveCurrent();
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (load == null)
            {
                Debug.LogError($"Could not start loading scene: {sceneName}", this);
                yield break;
            }

            yield return load;
            CurrentZoneScene = null;
        }

        private IEnumerator LoadGameplayZone(string zoneScene)
        {
            if (!SceneManager.GetSceneByName(GameplayCoreScene).isLoaded)
            {
                AsyncOperation coreLoad = SceneManager.LoadSceneAsync(GameplayCoreScene, LoadSceneMode.Single);
                if (coreLoad == null)
                {
                    Debug.LogError($"Could not start loading scene: {GameplayCoreScene}", this);
                    yield break;
                }

                yield return coreLoad;
            }

            foreach (string sceneName in ZoneScenes)
            {
                Scene loadedZone = SceneManager.GetSceneByName(sceneName);
                if (loadedZone.isLoaded && sceneName != zoneScene)
                {
                    yield return SceneManager.UnloadSceneAsync(loadedZone);
                }
            }

            Scene targetZone = SceneManager.GetSceneByName(zoneScene);
            if (!targetZone.isLoaded)
            {
                AsyncOperation zoneLoad = SceneManager.LoadSceneAsync(zoneScene, LoadSceneMode.Additive);
                if (zoneLoad == null)
                {
                    Debug.LogError($"Could not start loading scene: {zoneScene}", this);
                    yield break;
                }

                yield return zoneLoad;
                targetZone = SceneManager.GetSceneByName(zoneScene);
            }

            if (targetZone.IsValid() && targetZone.isLoaded)
            {
                SceneManager.SetActiveScene(targetZone);
                CurrentZoneScene = zoneScene;
            }
        }

        private static bool IsZoneScene(string sceneName)
        {
            foreach (string zoneScene in ZoneScenes)
            {
                if (zoneScene == sceneName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

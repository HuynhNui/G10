using System;
using System.Linq;
using G10.Prototype.Core;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    public static class ProjectSceneSetupBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        private static readonly string[] SceneNames =
        {
            "Bootstrap",
            "MainMenu",
            "GameplayCore",
            "Zone01",
            "Zone02",
            "Zone03",
            "Zone04",
            "Ending"
        };

        private static readonly Color BackgroundColor = new(0.055f, 0.075f, 0.11f, 1f);
        private static readonly Color DeepNavy = new(0.04f, 0.14f, 0.20f, 0.95f);
        private static readonly Color SeafoamGlow = new(0.15f, 0.45f, 0.55f, 1f);
        private static readonly Color MintText = new(0.72f, 1f, 0.88f, 1f);
        private static readonly Color PearlText = new(0.92f, 0.96f, 0.98f, 1f);

        private static Font CachedBoldFont;
        private static Font GetThemeFont()
        {
            if (CachedBoldFont == null)
                CachedBoldFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Bold.ttf");
            return CachedBoldFont != null ? CachedBoldFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Sprite GetThemeBorder(string name = "panel-001.png") =>
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/Borders/" + name);

        private static Sprite GetThemeBackground(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/Backgrounds/" + name);

        [InitializeOnLoadMethod]
        private static void ScheduleInitialSetup()
        {
            if (!IsSetupComplete())
            {
                EditorApplication.delayCall += RunInitialSetupWhenReady;
            }
        }

        [DidReloadScripts]
        private static void ScheduleInitialSetupAfterScriptReload()
        {
            ScheduleInitialSetup();
        }

        [MenuItem("G10/Setup Point And Click Scenes")]
        public static void BuildScenes()
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new BuildFailedException($"Input Actions could not be loaded at {InputActionsPath}.");
            }

            EnsureSceneFolder();
            BuildScene("Bootstrap", BuildBootstrap);
            BuildScene("MainMenu", () => BuildMainMenu(inputActions));
            BuildScene("GameplayCore", () => BuildGameplayCore(inputActions));

            for (int index = 0; index < 4; index++)
            {
                int zoneIndex = index;
                string zoneName = SceneNames[index + 3];
                BuildScene(zoneName, () => BuildZone(zoneName, zoneIndex));
            }

            BuildScene("Ending", () => BuildEnding(inputActions));
            UpdateBuildSettings();
            EditorSceneManager.OpenScene(ScenePath("Bootstrap"), OpenSceneMode.Single);
            RemoveObsoletePrototypeAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath("Bootstrap"));
            Debug.Log("Created point-and-click scene structure and updated Build Settings.");
        }

        public static void BuildAndValidate()
        {
            BuildScenes();
            ValidateScenes();
            Debug.Log("POINT_AND_CLICK_SCENE_SETUP_SUCCEEDED");
        }

        public static void ValidateScenes()
        {
            string[] configuredPaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            string[] expectedPaths = SceneNames.Select(ScenePath).ToArray();

            if (!configuredPaths.SequenceEqual(expectedPaths))
            {
                throw new BuildFailedException(
                    $"Build Settings scene order is invalid. Expected: {string.Join(", ", expectedPaths)}");
            }

            foreach (string sceneName in SceneNames)
            {
                ValidateScene(sceneName);
            }

            Debug.Log("Validated all 8 scenes, required roots, and Build Settings order.");
        }

        private static void RunInitialSetupWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunInitialSetupWhenReady;
                return;
            }

            if (!IsSetupComplete())
            {
                BuildAndValidate();
            }
        }

        private static bool IsSetupComplete()
        {
            string[] configuredPaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            string[] expectedPaths = SceneNames.Select(ScenePath).ToArray();
            return configuredPaths.SequenceEqual(expectedPaths)
                && expectedPaths.All(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null);
        }

        private static void BuildBootstrap()
        {
            GameObject bootstrap = CreateObject("Bootstrap");
            bootstrap.AddComponent<SceneFlowController>();
        }

        private static void BuildMainMenu(InputActionAsset inputActions)
        {
            CreateCamera("Main Camera");
            Canvas canvas = CreateCanvas("MainMenuCanvas");
            Sprite bgSprite = GetThemeBackground("MainMenu_Background.png");
            if (bgSprite != null)
                CreateFullScreenSpritePanel("Background", canvas.transform, bgSprite);
            else
                CreateFullScreenPanel("Background", canvas.transform, BackgroundColor);

            CreateText("Title", "PELAGIC : G10", canvas.transform, new Vector2(0f, 160f), new Vector2(900f, 100f), 78, FontStyle.Bold);
            CreateText("Subtitle", "POINT & CLICK SUBMARINE EXPLORATION", canvas.transform, new Vector2(0f, 95f), new Vector2(800f, 50f), 26, FontStyle.Normal);
            CreateNavigationButton("StartButton", "START VOYAGE", canvas.transform, new Vector2(0f, -20f), SceneNavigationAction.StartGame, "", 340f, 64f, "panel-001.png");
            CreateNavigationButton("QuitButton", "QUIT EXPEDITION", canvas.transform, new Vector2(0f, -100f), SceneNavigationAction.Quit, "", 340f, 64f, "panel-000.png");
            CreateEventSystem(inputActions);
        }

        private static void BuildGameplayCore(InputActionAsset inputActions)
        {
            GameObject root = CreateObject("GameplayCore");
            Camera gameplayCamera = CreateCamera("Main Camera");
            GameObject systems = CreateObject("Systems", root.transform);

            Canvas canvas = CreateCanvas("GameplayCanvas");
            GameObject dimBackground = CreateFullScreenPanel("DimBackground", canvas.transform, new Color(0f, 0f, 0f, 0.55f));
            dimBackground.SetActive(false);

            GameObject uiObject = CreateObject("UIManager", systems.transform);
            UIManager uiManager = uiObject.AddComponent<UIManager>();
            SetObjectReference(uiManager, "dimBackground", dimBackground);

            GameObject inputObject = CreateObject("PointAndClickInput", systems.transform);
            PointAndClickInputController inputController = inputObject.AddComponent<PointAndClickInputController>();
            SetObjectReference(inputController, "gameplayCamera", gameplayCamera);
            SetObjectReference(inputController, "uiManager", uiManager);

            CreateEventSystem(inputActions);
        }

        private static void BuildZone(string zoneName, int zoneIndex)
        {
            GameObject zoneRoot = CreateObject(zoneName);
            CreateObject("Environment", zoneRoot.transform);
            CreateObject("Content", zoneRoot.transform);
            CreateObject("Interactables", zoneRoot.transform);

            Canvas canvas = CreateCanvas("ZoneCanvas");
            string[] bgs = { "MainMenu_Background.png", "Zone02_Background.png", "Zone03_Background.png", "Zone04_Background.png" };
            Sprite bgSprite = GetThemeBackground(bgs[Mathf.Clamp(zoneIndex, 0, bgs.Length - 1)]);
            if (bgSprite != null)
            {
                var bg = CreateFullScreenSpritePanel("Background", canvas.transform, bgSprite);
                bg.transform.SetAsFirstSibling();
            }

            CreateText("ZoneLabel", zoneName.ToUpperInvariant(), canvas.transform, new Vector2(0f, 420f), new Vector2(500f, 60f), 38, FontStyle.Bold);

            if (zoneIndex > 0)
            {
                CreateNavigationButton(
                    "PreviousZoneButton",
                    "< PREVIOUS",
                    canvas.transform,
                    new Vector2(-680f, -420f),
                    SceneNavigationAction.Zone,
                    SceneNames[zoneIndex + 2],
                    260f,
                    56f,
                    "panel-000.png");
            }

            if (zoneIndex < 3)
            {
                CreateNavigationButton(
                    "NextZoneButton",
                    "NEXT >",
                    canvas.transform,
                    new Vector2(680f, -420f),
                    SceneNavigationAction.Zone,
                    SceneNames[zoneIndex + 4],
                    260f,
                    56f,
                    "panel-000.png");
            }
            else
            {
                CreateNavigationButton(
                    "EndingButton",
                    "FINISH >",
                    canvas.transform,
                    new Vector2(680f, -420f),
                    SceneNavigationAction.Ending,
                    "",
                    260f,
                    56f,
                    "panel-001.png");
            }
        }

        private static void BuildEnding(InputActionAsset inputActions)
        {
            CreateCamera("Main Camera");
            Canvas canvas = CreateCanvas("EndingCanvas");
            Sprite bgSprite = GetThemeBackground("Ending_Background.png");
            if (bgSprite != null)
                CreateFullScreenSpritePanel("Background", canvas.transform, bgSprite);
            else
                CreateFullScreenPanel("Background", canvas.transform, BackgroundColor);

            CreateText("Title", "EXPEDITION COMPLETE", canvas.transform, new Vector2(0f, 90f), new Vector2(1000f, 100f), 68, FontStyle.Bold);
            CreateNavigationButton("MainMenuButton", "RETURN TO MAIN MENU", canvas.transform, new Vector2(0f, -40f), SceneNavigationAction.MainMenu, "", 360f, 64f, "panel-001.png");
            CreateEventSystem(inputActions);
        }

        private static void BuildScene(string sceneName, Action buildContent)
        {
            Scene previousActiveScene = EditorSceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);

            try
            {
                buildContent();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath(sceneName)))
                {
                    throw new BuildFailedException($"Could not save scene: {sceneName}");
                }
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    EditorSceneManager.SetActiveScene(previousActiveScene);
                }

                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateScene(string sceneName)
        {
            string path = ScenePath(sceneName);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                throw new BuildFailedException($"Required scene is missing: {path}");
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                switch (sceneName)
                {
                    case "Bootstrap":
                        RequireComponent<SceneFlowController>(scene);
                        break;
                    case "MainMenu":
                        RequireObject(scene, "MainMenuCanvas/StartButton");
                        break;
                    case "GameplayCore":
                        RequireComponent<PointAndClickInputController>(scene);
                        RequireComponent<UIManager>(scene);
                        RequireObject(scene, "EventSystem");
                        break;
                    case "Ending":
                        RequireObject(scene, "EndingCanvas/MainMenuButton");
                        break;
                    default:
                        RequireObject(scene, $"{sceneName}/Environment");
                        RequireObject(scene, $"{sceneName}/Content");
                        RequireObject(scene, $"{sceneName}/Interactables");
                        break;
                }
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static Camera CreateCamera(string objectName)
        {
            GameObject cameraObject = CreateObject(objectName);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Canvas CreateCanvas(string objectName)
        {
            GameObject canvasObject = new(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateEventSystem(InputActionAsset inputActions)
        {
            GameObject eventSystemObject = CreateObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            module.actionsAsset = inputActions;
            module.AssignDefaultActions();
        }

        private static GameObject CreateFullScreenPanel(string objectName, Transform parent, Color color)
        {
            GameObject panel = CreateUiObject(objectName, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static GameObject CreateFullScreenSpritePanel(string objectName, Transform parent, Sprite sprite)
        {
            GameObject panel = CreateUiObject(objectName, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            return panel;
        }

        private static void CreateNavigationButton(
            string objectName,
            string label,
            Transform parent,
            Vector2 anchoredPosition,
            SceneNavigationAction action,
            string zoneSceneName = "",
            float width = 320f,
            float height = 64f,
            string borderSpriteName = "panel-001.png")
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = anchoredPosition;

            Image image = buttonObject.AddComponent<Image>();
            Sprite borderSprite = GetThemeBorder(borderSpriteName);
            if (borderSprite != null)
            {
                image.sprite = borderSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = DeepNavy;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = DeepNavy;
            colors.highlightedColor = SeafoamGlow;
            colors.pressedColor = new Color(0.02f, 0.10f, 0.14f, 1f);
            colors.selectedColor = DeepNavy;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            SceneNavigationButton navigation = buttonObject.AddComponent<SceneNavigationButton>();
            SetEnum(navigation, "action", (int)action);
            SetString(navigation, "zoneSceneName", zoneSceneName);
            UnityEventTools.AddPersistentListener(button.onClick, navigation.Navigate);

            Text text = CreateText("Label", label, buttonObject.transform, Vector2.zero, rect.sizeDelta, 24, FontStyle.Bold);
            text.color = PearlText;
        }

        private static Text CreateText(
            string objectName,
            string content,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            FontStyle fontStyle)
        {
            GameObject textObject = CreateUiObject(objectName, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Text text = textObject.AddComponent<Text>();
            text.text = content;
            text.font = GetThemeFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = PearlText;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject(string name, Transform parent = null)
        {
            GameObject gameObject = new(name);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = SceneNames
                .Select(sceneName => new EditorBuildSettingsScene(ScenePath(sceneName), true))
                .ToArray();
        }

        private static void RemoveObsoletePrototypeAssets()
        {
            AssetDatabase.DeleteAsset("Assets/_Project/Scenes/RoomPrototype.unity");
            AssetDatabase.DeleteAsset("Assets/_Project/Prefabs/Player/Player.prefab");
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                AssetDatabase.CreateFolder("Assets", "_Project");
            }

            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            EnsureFolder(SceneFolder, "Bootstrap");
            EnsureFolder(SceneFolder, "MainMenu");
            EnsureFolder(SceneFolder, "Gameplay");
            EnsureFolder(SceneFolder, "Ending");
        }

        private static string ScenePath(string sceneName)
        {
            return sceneName switch
            {
                "Bootstrap" => $"{SceneFolder}/Bootstrap/Bootstrap.unity",
                "MainMenu" => $"{SceneFolder}/MainMenu/MainMenu.unity",
                "Ending" => $"{SceneFolder}/Ending/Ending.unity",
                _ => $"{SceneFolder}/Gameplay/{sceneName}.unity"
            };
        }

        private static void EnsureFolder(string parentFolder, string childFolder)
        {
            string path = $"{parentFolder}/{childFolder}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentFolder, childFolder);
            }
        }

        private static GameObject RequireObject(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            GameObject gameObject = scene.GetRootGameObjects().FirstOrDefault(root => root.name == segments[0]);
            if (gameObject != null && segments.Length > 1)
            {
                Transform child = gameObject.transform.Find(string.Join("/", segments.Skip(1)));
                gameObject = child != null ? child.gameObject : null;
            }

            if (gameObject == null)
            {
                throw new BuildFailedException($"Required scene object is missing: {path}");
            }

            return gameObject;
        }

        private static T RequireComponent<T>(Scene scene) where T : Component
        {
            T component = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
            if (component == null)
            {
                throw new BuildFailedException($"Required component is missing in {scene.name}: {typeof(T).Name}");
            }

            return component;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SetProperty(target, propertyName, property => property.objectReferenceValue = value);
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            SetProperty(target, propertyName, property => property.enumValueIndex = value);
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SetProperty(target, propertyName, property => property.stringValue = value);
        }

        private static void SetProperty(Object target, string propertyName, Action<SerializedProperty> setter)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new BuildFailedException($"Serialized property {propertyName} was not found on {target.GetType().Name}.");
            }

            setter(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}

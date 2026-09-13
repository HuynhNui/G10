using System;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    public static class ComputerStationEditor
    {
        private static readonly Color Background = new(0.025f, 0.06f, 0.055f, 1);
        private static readonly Color Surface = new(0.06f, 0.15f, 0.12f, 1);
        private static readonly Color Foreground = new(0.7f, 0.95f, 0.74f, 1);

        [MenuItem("G10/Computer/Install Phase B Apps")]
        public static void InstallApps()
        {
            if (EditorApplication.isPlaying) return;
            CabinStationView cabin = Object.FindAnyObjectByType<CabinStationView>();
            ComputerScreenController screen = cabin != null ? cabin.GetComponentInChildren<ComputerScreenController>(true) : null;
            if (screen == null) throw new InvalidOperationException("Install and validate Phase A first.");
            if (screen.GetComponentInChildren<PhotoLabView>(true) != null)
            { Selection.activeGameObject = screen.gameObject; return; }
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install computer apps");
            Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Install computer apps");
            RectTransform providers = Box("DataProviders", screen.transform, 0, 0, 0, 0);
            EmptyPhotoRepository photos = providers.gameObject.AddComponent<EmptyPhotoRepository>();
            ExistingShipStatusProvider status = providers.gameObject.AddComponent<ExistingShipStatusProvider>();
            ZoneMissionProvider missions = providers.gameObject.AddComponent<ZoneMissionProvider>();
            Set(status, "radar", cabin.Radar);
            const string dataFolder = "Assets/_Project/Data/Computer";
            if (!AssetDatabase.IsValidFolder(dataFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Computer");
            const string missionPath = dataFolder + "/Zone01Mission.asset";
            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(missionPath);
            if (mission == null)
            {
                mission = ScriptableObject.CreateInstance<MissionDefinition>();
                mission.zoneSceneName = "Zone01"; mission.zoneDisplayName = "ZONE 1 — MIỀN TẢO HÁT";
                mission.objective = "Thu thập vật liệu để sửa bộ phận chịu áp.";
                mission.state = MissionState.Locked; mission.isTemplate = true;
                mission.integrationNote = "Chờ hệ tọa độ, sinh vật và kho đồ được kết nối.";
                mission.steps = new[] {
                    new MissionStep { description = "Xác định tín hiệu sinh vật", state = MissionState.Locked },
                    new MissionStep { description = "Thu thập mẫu cần thiết", state = MissionState.Locked },
                    new MissionStep { description = "Quay về trạm", state = MissionState.Locked }
                };
                AssetDatabase.CreateAsset(mission, missionPath);
            }
            Set(missions, "mission", mission);
            foreach (ComputerAppPanel app in screen.Apps)
            {
                Text body = Label("Body", app.panel.transform, "", 120, 145, 1680, 600, 32);
                switch (app.id)
                {
                    case ComputerAppId.PhotoLab:
                        PhotoLabView photoView = app.panel.AddComponent<PhotoLabView>();
                        Set(photoView, "repositorySource", photos); Set(photoView, "body", body);
                        body.fontSize = 44; body.alignment = TextAnchor.MiddleCenter;
                        break;
                    case ComputerAppId.ShipStatus:
                        ShipStatusView statusView = app.panel.AddComponent<ShipStatusView>();
                        Set(statusView, "providerSource", status); Set(statusView, "body", body);
                        body.alignment = TextAnchor.UpperLeft;
                        break;
                    case ComputerAppId.MissionLog:
                        MissionLogView missionView = app.panel.AddComponent<MissionLogView>();
                        Set(missionView, "providerSource", missions); Set(missionView, "body", body);
                        body.alignment = TextAnchor.UpperLeft;
                        break;
                }
            }
            Undo.RegisterCreatedObjectUndo(providers.gameObject, "Create computer providers");
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            Selection.activeGameObject = screen.gameObject;
            Undo.CollapseUndoOperations(group);
            Debug.Log("Phase B apps installed. Mission is a locked preview; no camera/capture or coordinate foundation was added.");
        }

        [MenuItem("G10/Computer/Install Phase A Shell")]
        public static void InstallShell()
        {
            if (EditorApplication.isPlaying) return;
            CabinStationView cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01")
                throw new InvalidOperationException("Open GameplayCore and Zone01 before installing the computer.");
            Transform frame = cabin.transform.Find("CabinCanvas/CabinFrame");
            if (frame.Find("ComputerScreen") != null) { Selection.activeGameObject = frame.Find("ComputerScreen").gameObject; return; }
            Button monitor = frame.GetComponentsInChildren<Button>(true).Single(button => button.name == "MonitorHotspot");
            if (monitor.onClick.GetPersistentEventCount() != 1 || monitor.onClick.GetPersistentTarget(0) != cabin ||
                monitor.onClick.GetPersistentMethodName(0) != "OpenCamera")
                throw new InvalidOperationException("Monitor wiring differs from the inspected cabin; review it before replacing.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install computer shell");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install computer shell");
            RectTransform screen = SurfaceBox("ComputerScreen", frame, 0, 0, 1920, 1080, Background);
            ComputerScreenController controller = screen.gameObject.AddComponent<ComputerScreenController>();
            Set(controller, "cabin", cabin);
            Set(cabin, "computerScreen", controller);
            Label("SystemTitle", screen, "PELAGIC SYSTEMS  /  SHIP TERMINAL", 100, 50, 1440, 75, 38);
            Label("SystemRule", screen, "ONBOARD INFORMATION SYSTEM", 100, 135, 1500, 60, 25);
            MakeButton("Exit", screen, "EXIT", 1620, 55, 200, 75, controller.Exit);
            RectTransform desktop = Box("Desktop", screen, 0, 220, 1920, 760);
            Set(controller, "desktop", desktop.gameObject);
            Label("DesktopTitle", desktop, "SELECT APPLICATION", 120, 45, 1400, 70, 36);
            MakeButton("PhotoLabIcon", desktop, "[ + ]\n\nPHOTO LAB", 120, 190, 500, 300, controller.OpenPhotoLab);
            MakeButton("ShipStatusIcon", desktop, "[ = ]\n\nSHIP STATUS", 710, 190, 500, 300, controller.OpenShipStatus);
            MakeButton("MissionLogIcon", desktop, "[ > ]\n\nMISSION LOG", 1300, 190, 500, 300, controller.OpenMissionLog);
            Label("Footer", screen, "ESC: BACK / EXIT     |     VESSEL SIMULATION REMAINS ACTIVE", 100, 985, 1720, 65, 25);
            var panels = new GameObject[3];
            string[] names = { "PHOTO LAB", "SHIP STATUS", "MISSION LOG" };
            for (int i = 0; i < panels.Length; i++)
            {
                RectTransform panel = Box(((ComputerAppId)(i + 1)) + "Panel", screen, 0, 220, 1920, 760);
                panels[i] = panel.gameObject;
                Label("AppTitle", panel, names[i], 120, 25, 1400, 75, 40);
                MakeButton("Back", panel, "BACK", 1620, 25, 200, 70, controller.ShowDesktop);
                panel.gameObject.SetActive(false);
            }
            SerializedObject so = new(controller); SerializedProperty apps = so.FindProperty("apps"); apps.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                apps.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex = i + 1;
                apps.GetArrayElementAtIndex(i).FindPropertyRelative("panel").objectReferenceValue = panels[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.RemovePersistentListener(monitor.onClick, 0);
            UnityEventTools.AddPersistentListener(monitor.onClick, cabin.OpenComputer);
            monitor.GetComponent<CabinPointerTarget>().Configure(cabin, "Máy tính trung tâm");
            screen.gameObject.SetActive(false);
            Undo.RegisterCreatedObjectUndo(screen.gameObject, "Create computer shell");
            EditorUtility.SetDirty(cabin); EditorUtility.SetDirty(monitor);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            Selection.activeGameObject = screen.gameObject;
            Undo.CollapseUndoOperations(group);
            Debug.Log("Phase A computer shell installed in Zone01. Save the scene, then run Play Mode tests.");
        }

        internal static RectTransform Box(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        internal static RectTransform SurfaceBox(string name, Transform parent, float x, float y, float w, float h, Color color)
        { var rect = Box(name, parent, x, y, w, h); rect.gameObject.AddComponent<Image>().color = color; return rect; }
        internal static Text Label(string name, Transform parent, string text, float x, float y, float w, float h, int size)
        {
            Text label = Box(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = size;
            label.text = text; label.color = Foreground; label.raycastTarget = false;
            label.alignment = TextAnchor.MiddleLeft; return label;
        }
        private static void MakeButton(string name, Transform parent, string text, float x, float y, float w, float h, UnityAction action)
        {
            RectTransform rect = SurfaceBox(name, parent, x, y, w, h, Surface);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(button.onClick, action);
            Text label = Label("Label", rect, text, 15, 10, w - 30, h - 20, 34); label.alignment = TextAnchor.MiddleCenter;
        }
        internal static void Set(Object target, string field, Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}

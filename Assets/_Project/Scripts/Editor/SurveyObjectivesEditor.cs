using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Editor
{
    public static class SurveyObjectivesEditor
    {
        [MenuItem("G10/Zone 1/Install Survey Objectives")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01") return;
            var check = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Map/check.png");
            var item = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Sprites/Creature/Zone1/Item1_real.png");
            var survey = cabin.GetComponent<PhotoSurveyZone>();
            var catcher = cabin.GetComponent<CreatureCatcher>();
            if (check == null || item == null || survey == null || catcher == null)
            { Debug.LogError("Survey objectives require check.png, Item1_real.png and creature catching."); return; }
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install survey objectives");
            catcher.itemIcon = item;
            // Initialize only if absent; preserve the designer's authored objective list on reruns.
            if (survey.tasks == null || survey.tasks.Length == 0)
                survey.tasks = new[] { PhotoSurveyZone.TaskKind.Photograph, PhotoSurveyZone.TaskKind.Capture };
            foreach (var overlay in cabin.GetComponentsInChildren<PhotoSurveyMap>(true))
            {
                var existing = overlay.transform.Find("CompletedCheck");
                if (existing == null)
                {
                    var go = new GameObject("CompletedCheck", typeof(RectTransform), typeof(RawImage));
                    Undo.RegisterCreatedObjectUndo(go, "Create completion check"); go.transform.SetParent(overlay.transform, false);
                    existing = go.transform;
                }
                var icon = existing.GetComponent<RawImage>(); icon.texture = check; icon.raycastTarget = false; icon.enabled = false;
                overlay.completionIcon = icon; EditorUtility.SetDirty(overlay);
            }
            var map = cabin.MapPanel.transform;
            var taskPanel = map.Find("SurveyTasks");
            if (taskPanel == null)
            {
                var go = new GameObject("SurveyTasks", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Create survey task list"); go.transform.SetParent(map, false); taskPanel = go.transform;
                var rect = (RectTransform)taskPanel; rect.anchorMin = rect.anchorMax = new(0, 1);
                rect.sizeDelta = new(700, 240); rect.anchoredPosition = new(580, -240);
                var bg = go.GetComponent<Image>(); bg.color = new(.035f, .075f, .09f, .96f); bg.raycastTarget = false;
                var label = new GameObject("Label", typeof(RectTransform), typeof(Text)); label.transform.SetParent(taskPanel, false);
                var lr = (RectTransform)label.transform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
                lr.offsetMin = new(25, 20); lr.offsetMax = new(-25, -20);
                var text = label.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 30; text.alignment = TextAnchor.MiddleLeft; text.color = new(.85f, 1, .8f); text.raycastTarget = false;
            }
            cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>(true).taskReadout = taskPanel.GetComponentInChildren<Text>(true);
            taskPanel.gameObject.SetActive(false);
            foreach (var bag in cabin.GetComponentsInChildren<CreatureInventoryView>(true))
                foreach (var icon in bag.icons)
                {
                    var fit = icon.GetComponent<AspectRatioFitter>();
                    if (fit != null) { fit.aspectRatio = (float)item.width / item.height; EditorUtility.SetDirty(fit); }
                }
            EditorUtility.SetDirty(catcher); EditorUtility.SetDirty(survey);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene); Selection.activeGameObject = cabin.gameObject;
            Debug.Log("P01 objectives installed: photograph then capture; check.png replaces completed marker; Item1_real assigned. Save Zone01.");
        }
    }
}

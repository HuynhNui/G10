using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Editor
{
    public static class PhotoSurveyEditor
    {
        [MenuItem("G10/Zone 1/Install Depth And Photo Survey")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01") return;
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install depth and survey");
            var nav = cabin.GetComponent<ZoneNavigation>();
            var survey = cabin.GetComponent<PhotoSurveyZone>();
            if (survey == null)
            {
                // Select a reachable grid-cell centre using the existing chart collision data.
                var config = new SerializedObject(nav);
                Vector2 start = config.FindProperty("startPosition").vector2Value;
                Vector2 chosen = Vector2.zero; float best = float.MaxValue;
                for (int y = 0; y < 14; y++) for (int x = 0; x < 24; x++)
                {
                    Vector2 point = new(x * 50 + 25, y * 50 + 25);
                    float distance = Vector2.Distance(start, point);
                    if (distance < 25 || distance > 150 || distance >= best || !nav.CanOccupy(point)) continue;
                    bool clear = true;
                    for (int i = 0; i <= Mathf.CeilToInt(distance); i++)
                        if (!nav.CanOccupy(Vector2.Lerp(start, point, i / Mathf.Ceil(distance)))) { clear = false; break; }
                    if (clear) { best = distance; chosen = point; }
                }
                if (best == float.MaxValue) { Debug.LogError("No directly reachable survey cell found; author a survey location manually."); return; }
                survey = Undo.AddComponent<PhotoSurveyZone>(cabin.gameObject);
                survey.center = chosen;
                survey.targetDepth = config.FindProperty("startDepth").floatValue;
            }
            var wiring = new SerializedObject(cabin);
            var helm = ((GameObject)wiring.FindProperty("navigationPanel").objectReferenceValue).transform;
            wiring.FindProperty("depthReadout").objectReferenceValue = helm.Find("Depth").GetComponent<Text>();
            wiring.FindProperty("photoSurvey").objectReferenceValue = survey;
            wiring.ApplyModifiedProperties();
            // The original Zone label sits over the painted depth buttons.
            var zoneLabel = helm.Find("Zone");
            if (zoneLabel != null) zoneLabel.gameObject.SetActive(false);
            AddHold(helm, cabin, "AscendHotspot", 580, 735, 255, 115, 5, "Giữ để nổi lên — giảm độ sâu");
            AddHold(helm, cabin, "DiveHotspot", 580, 855, 255, 115, 6, "Giữ để lặn xuống — tăng độ sâu");
            var map = ((GameObject)wiring.FindProperty("mapPanel").objectReferenceValue).transform;
            AddOverlay(map.Find("SquareChartContent") ?? map, survey, nav);
            AddOverlay(helm.Find("MiniMap"), survey, nav);
            if (map.Find("SurveyLegend") == null)
            {
                var legend = new GameObject("SurveyLegend", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(legend, "Create survey legend"); legend.transform.SetParent(map, false);
                var rect = (RectTransform)legend.transform;
                rect.anchorMin = rect.anchorMax = new(.5f,1); rect.sizeDelta = new(1080,55); rect.anchoredPosition = new(0,-42);
                legend.GetComponent<Image>().color = new(.03f,.08f,.1f,.9f); legend.GetComponent<Image>().raycastTarget = false;
                var label = new GameObject("Label", typeof(RectTransform), typeof(Text)); label.transform.SetParent(legend.transform,false);
                var lr = (RectTransform)label.transform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
                var text = label.GetComponent<Text>(); text.font = helm.Find("Depth").GetComponent<Text>().font;
                text.fontSize = 26; text.alignment = TextAnchor.MiddleCenter; text.color = new(1,.8f,.35f); text.raycastTarget = false;
                text.text = $"P01 • VÙNG CHỤP / QUÉT SINH VẬT • X {survey.center.x:0} Y {survey.center.y:0} • {survey.targetDepth:0} m";
            }
            var radar = (RadarDisplay)wiring.FindProperty("radarDisplay").objectReferenceValue;
            Undo.RecordObject(radar, "Connect survey radar"); radar.photoSurvey = survey;
            EditorUtility.SetDirty(radar); EditorUtility.SetDirty(survey);
            Selection.activeGameObject = cabin.gameObject;
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            Debug.Log($"Photo survey P01 ready: X {survey.center.x}, Y {survey.center.y}, depth {survey.targetDepth} m. Save Zone01.");
        }
        private static void AddOverlay(Transform parent, PhotoSurveyZone survey, ZoneNavigation nav)
        {
            if (parent.Find("PhotoSurveyOverlay") != null) return;
            var go = new GameObject("PhotoSurveyOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(PhotoSurveyMap));
            Undo.RegisterCreatedObjectUndo(go, "Create survey overlay"); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var overlay = go.GetComponent<PhotoSurveyMap>(); overlay.survey = survey; overlay.navigation = nav; overlay.raycastTarget = false;
        }
        private static void AddHold(Transform parent, CabinStationView cabin, string name, float x, float y, float w, float h, int command, string hint)
        {
            if (parent.Find(name) != null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CabinPointerTarget));
            Undo.RegisterCreatedObjectUndo(go, "Create depth button"); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new(0,1);
            rect.sizeDelta = new(w,h); rect.anchoredPosition = new(x+w/2,-y-h/2);
            var button = go.GetComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            var colors = button.colors; colors.normalColor = new(1,1,1,0); colors.highlightedColor = new(.6f,1,.85f,.18f);
            colors.pressedColor = new(.6f,1,.85f,.3f); colors.selectedColor = colors.normalColor; button.colors = colors;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            go.GetComponent<CabinPointerTarget>().Configure(cabin, hint, command);
        }
    }
}

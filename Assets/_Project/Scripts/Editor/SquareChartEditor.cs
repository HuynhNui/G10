using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Editor
{
    /// <summary>Fit the chart, its pointer target and markers together; preserve modal controls.</summary>
    public static class SquareChartEditor
    {
        [MenuItem("G10/Zone 1/Fix Square Map Grid")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01") return;
            var config = new SerializedObject(cabin);
            var map = (GameObject)config.FindProperty("mapPanel").objectReferenceValue;
            Undo.RegisterFullObjectHierarchyUndo(map, "Fit square chart grid");
            var content = map.transform.Find("SquareChartContent") as RectTransform;
            if (content == null)
            {
                var source = map.GetComponent<RawImage>();
                if (source == null || source.texture == null) return;
                var go = new GameObject("SquareChartContent", typeof(RectTransform), typeof(RawImage),
                    typeof(AspectRatioFitter), typeof(CabinPointerTarget));
                Undo.RegisterCreatedObjectUndo(go, "Create calibrated chart content");
                content = (RectTransform)go.transform;
                content.SetParent(map.transform, false);
                content.SetAsFirstSibling();
                var art = go.GetComponent<RawImage>();
                art.texture = source.texture; art.color = source.color; art.uvRect = source.uvRect;
                source.texture = null; source.color = new Color(.035f, .045f, .04f, 1);
                var oldPointer = map.GetComponent<CabinPointerTarget>();
                if (oldPointer != null) Undo.DestroyObjectImmediate(oldPointer);
                go.GetComponent<CabinPointerTarget>().Configure(cabin, "", isChart: true);
                var overlay = map.transform.Find("PhotoSurveyOverlay") as RectTransform;
                if (overlay != null)
                {
                    Undo.SetTransformParent(overlay, content, "Move chart markers with chart");
                    overlay.localScale = Vector3.one;
                    overlay.localRotation = Quaternion.identity;
                    overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
                    overlay.offsetMin = overlay.offsetMax = Vector2.zero;
                }
            }
            // Derive aspect from calibration, not the texture's original aspect:
            // the painted chart has unequal pixels per metre on X and Y.
            Vector2 origin = ZoneNavigation.CoordinatesToUV(Vector2.zero);
            Vector2 unit = ZoneNavigation.CoordinatesToUV(Vector2.one) - origin;
            var fit = content.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = unit.y / unit.x;
            EditorUtility.SetDirty(fit);
            EditorSceneManager.MarkSceneDirty(map.scene);
            Selection.activeGameObject = content.gameObject;
            Debug.Log("Map fitted to calibrated square 50 m cells. Save Zone01.");
        }
    }
}

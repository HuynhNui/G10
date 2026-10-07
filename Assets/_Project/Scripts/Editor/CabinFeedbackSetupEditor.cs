using System;
using System.Linq;
using G10.Prototype.Atmosphere;
using G10.Prototype.Feedback;
using G10.Prototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    public static class CabinFeedbackSetupEditor
    {
        [MenuItem("G10/Feedback/Apply Gameplay Feedback Setup")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null) throw new InvalidOperationException("Open the existing Zone01 cabin scene.");
            var frame = cabin.transform.Find("CabinCanvas/CabinFrame");
            if (frame == null) throw new InvalidOperationException("Existing CabinFrame is required.");
            // Validate every user asset before making scene mutations; no runtime filename searches.
            string[] audioPaths = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio/edgecases" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            AudioClip Clip(string fragment)
            {
                var path = audioPaths.Single(value => value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            var clips = new[] { Clip("Hull_Impact"), Clip("Hull_Creak"), Clip("Critical_Warning"), Clip("Power_Hum"), Clip("Power_Down") };
            var scrapes = audioPaths.Where(value => value.Contains("Exterior_Scrape")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
            if (scrapes.Length != 2) throw new InvalidOperationException("Exactly two authored Exterior_Scrape variants are required.");
            var hull = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/edgecases/LowHull.png");
            var energy = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/edgecases/Anh-nen-den-13.jpg");
            if (hull == null || energy == null || clips.Any(value => value == null)) throw new InvalidOperationException("Missing feedback assets.");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Wire gameplay feedback");
            var feedback = cabin.GetComponent<CabinFeedbackController>() ?? Undo.AddComponent<CabinFeedbackController>(cabin.gameObject);
            // Existing outer frame moves art and ALL matching UI hitboxes together. Breathing/bob own child rigs.
            var shake = frame.GetComponent<ImpactShake>() ?? Undo.AddComponent<ImpactShake>(frame.gameObject);
            var overlay = cabin.transform.Find("FeedbackOverlayCanvas");
            if (overlay == null)
            {
                var obj = new GameObject("FeedbackOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
                Undo.RegisterCreatedObjectUndo(obj, "Create feedback overlay"); obj.transform.SetParent(cabin.transform, false); overlay = obj.transform;
            }
            var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 31000;
            var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var powerImage = Image(overlay, "LowEnergyOverlay", energy);
            var hullImage = Image(overlay, "LowHullOverlay", hull);
            var text = overlay.Find("FeedbackNotification");
            if (text == null)
            {
                var obj = new GameObject("FeedbackNotification", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(obj, "Create feedback notification"); obj.transform.SetParent(overlay, false); text = obj.transform;
            }
            var rect = (RectTransform)text; rect.anchorMin = new Vector2(.15f, .04f); rect.anchorMax = new Vector2(.85f, .14f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = text.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.fontSize = 26;
            label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false; label.text = "";
            var so = new SerializedObject(feedback);
            so.FindProperty("cabin").objectReferenceValue = cabin;
            so.FindProperty("atmosphere").objectReferenceValue = cabin.GetComponentInChildren<CabinAtmosphere>(true);
            so.FindProperty("impactShake").objectReferenceValue = shake;
            so.FindProperty("lowHullOverlay").objectReferenceValue = hullImage;
            so.FindProperty("lowEnergyOverlay").objectReferenceValue = powerImage;
            so.FindProperty("notification").objectReferenceValue = label;
            string[] names = { "hullImpact", "hullCreak", "criticalWarning", "lowPowerHum", "powerDown" };
            for (int i = 0; i < names.Length; i++) so.FindProperty(names[i]).objectReferenceValue = clips[i];
            var array = so.FindProperty("exteriorScrapes"); array.arraySize = 2;
            for (int i = 0; i < 2; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = scrapes[i];
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
        }
        private static UnityEngine.UI.RawImage Image(Transform parent, string name, Texture texture)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
                Undo.RegisterCreatedObjectUndo(obj, "Create resource overlay"); obj.transform.SetParent(parent, false); child = obj.transform;
            }
            var rect = (RectTransform)child; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = child.GetComponent<UnityEngine.UI.RawImage>(); image.texture = texture; image.color = new Color(1, 1, 1, 0); image.raycastTarget = false;
            return image;
        }
    }
}

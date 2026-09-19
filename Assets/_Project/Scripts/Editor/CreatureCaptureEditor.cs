using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace G10.Prototype.Editor
{
    public static class CreatureCaptureEditor
    {
        [MenuItem("G10/Zone 1/Install Creature Catching")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01") return;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Sprites/Creature/Zone1/Item1_real.png");
            if (icon == null || cabin.GetComponent<PhotoSurveyZone>() == null || cabin.Radar == null)
            { Debug.LogError("Creature catching requires Item1, survey and radar."); return; }
            if (cabin.GetComponent<CreatureCatcher>() != null)
            {
                var existing = cabin.GetComponent<CreatureCatcher>();
                Undo.RecordObject(existing, "Replace creature inventory art"); existing.itemIcon = icon; EditorUtility.SetDirty(existing);
                SeparateCapturePanel(cabin); FitTitle(cabin); Selection.activeGameObject = cabin.gameObject; return;
            }
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install creature catching");
            var inventory = Undo.AddComponent<CreatureInventory>(cabin.gameObject);
            var catcher = Undo.AddComponent<CreatureCatcher>(cabin.gameObject);
            catcher.navigation = cabin.Navigation; catcher.survey = cabin.GetComponent<PhotoSurveyZone>();
            catcher.inventory = inventory; catcher.itemIcon = icon;
            cabin.Radar.catcher = catcher; EditorUtility.SetDirty(cabin.Radar);
            var radar = cabin.RadarPanel.transform;
            var captureView = Undo.AddComponent<CreatureCaptureView>(radar.gameObject); captureView.catcher = catcher;
            var controls = Box(radar, "CatchingControls", 1240, 280, 590, 600);
            controls.gameObject.AddComponent<Image>().color = new(.04f, .09f, .1f, .92f);
            Label(controls, "Title", "MÁY BẮT SINH VẬT", 20, 25, 550, 70, 32);
            Button(controls, "Catch", "BẮT", 55, 130, 480, 80, captureView.Catch);
            Button(controls, "Inventory", "MỞ BALÔ", 55, 235, 480, 80, cabin.OpenCargo);
            captureView.status = Label(controls, "Result", "", 25, 350, 540, 210, 28);
            var so = new SerializedObject(cabin);
            var bag = ((GameObject)so.FindProperty("cargoPanel").objectReferenceValue).transform;
            var placeholder = bag.Find("Message"); if (placeholder != null) placeholder.gameObject.SetActive(false);
            var view = Undo.AddComponent<CreatureInventoryView>(bag.gameObject); view.inventory = inventory;
            view.summary = Label(bag, "InventorySummary", "Balô trống", 350, 170, 1220, 75, 32);
            view.icons = new RawImage[CreatureInventory.Capacity]; view.labels = new Text[CreatureInventory.Capacity];
            for (int i = 0; i < CreatureInventory.Capacity; i++)
            {
                var slot = Box(bag, "ItemSlot" + (i + 1), 150 + i * 410, 300, 380, 475);
                slot.gameObject.AddComponent<Image>().color = new(.1f, .19f, .2f, 1);
                var frame = Box(slot, "IconFrame", 25, 20, 330, 340);
                var art = Box(frame, "ItemIcon", 0, 0, 330, 340);
                var fit = art.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = (float)icon.width / icon.height;
                view.icons[i] = art.gameObject.AddComponent<RawImage>(); view.icons[i].raycastTarget = false; view.icons[i].enabled = false;
                view.labels[i] = Label(slot, "Name", "Ô trống", 10, 375, 360, 80, 26);
            }
            Button(bag, "BackToRadar", "VỀ RADAR", 740, 875, 440, 80, cabin.OpenRadar);
            FitTitle(cabin);
            SeparateCapturePanel(cabin);
            EditorUtility.SetDirty(catcher); EditorUtility.SetDirty(inventory); EditorUtility.SetDirty(captureView); EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            Selection.activeGameObject = cabin.gameObject;
            Debug.Log("Creature catching installed: 20 m lime circle, catch button, Item1 inventory. Save Zone01.");
        }
        private static void SeparateCapturePanel(CabinStationView cabin)
        {
            var so = new SerializedObject(cabin);
            var panel = (GameObject)so.FindProperty("capturePanel").objectReferenceValue;
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Separate creature capture screen");
            var controls = panel.transform.Find("CatchingControls") as RectTransform;
            if (controls == null)
            {
                controls = cabin.RadarPanel.transform.Find("CatchingControls") as RectTransform;
                if (controls == null) return;
                Undo.SetTransformParent(controls, panel.transform, "Move controls to capture screen");
            }
            var view = panel.GetComponent<CreatureCaptureView>();
            if (view == null) view = Undo.AddComponent<CreatureCaptureView>(panel);
            view.catcher = cabin.GetComponent<CreatureCatcher>();
            view.status = controls.Find("Result").GetComponent<Text>();
            var button = controls.Find("Catch").GetComponent<Button>();
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, view.Catch);
            var previousView = cabin.RadarPanel.GetComponent<CreatureCaptureView>();
            if (previousView != null) Undo.DestroyObjectImmediate(previousView);
            foreach (string name in new[] { "Title", "Message" })
            {
                var placeholder = panel.transform.Find(name);
                if (placeholder != null) placeholder.gameObject.SetActive(false);
            }
            Place(controls, 320, 155, 1280, 720);
            Place((RectTransform)controls.Find("Title"), 80, 40, 1120, 80);
            Place(view.status.rectTransform, 80, 200, 1120, 240);
            Place((RectTransform)controls.Find("Catch"), 270, 560, 340, 80);
            Place((RectTransform)controls.Find("Inventory"), 670, 560, 340, 80);
            foreach (string name in new[] { "Catch", "Inventory" })
                Place((RectTransform)controls.Find(name + "/Label"), 0, 0, 340, 80);
            EditorUtility.SetDirty(view); EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(panel.scene);
        }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            rect.anchorMin = rect.anchorMax = new(0, 1); rect.pivot = new(.5f, .5f);
            rect.sizeDelta = new(w, h); rect.anchoredPosition = new(x + w / 2, -y - h / 2);
        }
        private static void FitTitle(CabinStationView cabin)
        {
            var so = new SerializedObject(cabin);
            var bag = (GameObject)so.FindProperty("cargoPanel").objectReferenceValue;
            var title = bag.transform.Find("Title") as RectTransform;
            if (title == null) return;
            Undo.RecordObject(title, "Position inventory heading");
            title.anchorMin = title.anchorMax = new(0, 1); title.pivot = new(.5f, .5f);
            title.anchoredPosition = new(960, -100); title.sizeDelta = new(1200, 80);
            EditorSceneManager.MarkSceneDirty(bag.scene);
        }
        private static Font GetThemeFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Bold.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        private static Sprite GetThemeBorder(string name = "panel-001.png") =>
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/Borders/" + name);

        private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "Create creature UI");
            go.transform.SetParent(parent, false); var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new(0, 1); r.sizeDelta = new(w, h); r.anchoredPosition = new(x + w / 2, -y - h / 2); return r;
        }
        private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            var t = Box(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            t.font = GetThemeFont(); t.fontSize = size; t.text = value;
            t.color = new(.88f, .98f, .92f); t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false; return t;
        }
        private static void Button(Transform parent, string name, string title, float x, float y, float w, float h, UnityAction action)
        {
            var r = Box(parent, name, x, y, w, h); var image = r.gameObject.AddComponent<Image>();
            Sprite border = GetThemeBorder("panel-001.png");
            if (border != null) { image.sprite = border; image.type = Image.Type.Sliced; }
            image.color = new Color(0.04f, 0.14f, 0.20f, 0.95f);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            ColorBlock colors = b.colors;
            colors.normalColor = new Color(0.04f, 0.14f, 0.20f, 0.95f);
            colors.highlightedColor = new Color(0.15f, 0.45f, 0.55f, 1f);
            colors.pressedColor = new Color(0.02f, 0.10f, 0.14f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            b.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(b.onClick, action);
            var text = Label(r, "Label", title, 0, 0, w, h, (int)Mathf.Clamp(h * 0.4f, 18, 30));
            text.color = new Color(0.92f, 0.96f, 0.98f, 1f);
        }
    }
}

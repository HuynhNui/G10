using System;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>Targeted migration of backpack/research UI into the existing desktop.</summary>
    public static class CargoAppEditor
    {
        private const string Art = "Assets/_Project/Art/UI/Desktop/";
        private static readonly Color Ink = new(.16f, .23f, .48f);
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular.ttf");

        [MenuItem("G10/Computer/Install Cargo and Remove Research")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = Object.FindObjectsByType<CabinStationView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(v => v.gameObject.scene.name == "Zone01");
            if (cabin == null) throw new InvalidOperationException("Load Zone01 first.");
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            if (screen == null) throw new InvalidOperationException("Zone01 requires its existing computer.");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Replace backpack with Cargo app");
            var serialized = new SerializedObject(screen);
            var apps = serialized.FindProperty("apps");
            for (int i = apps.arraySize - 1; i >= 0; i--)
                if (apps.GetArrayElementAtIndex(i).FindPropertyRelative("id").intValue == 7) apps.DeleteArrayElementAtIndex(i);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Destroy(screen.transform.Find("ResearchPanel"));
            Destroy(screen.Desktop.transform.Find("ResearchIcon"));
            var panel = screen.transform.Find("CargoPanel");
            if (panel == null)
            {
                panel = Rect(screen.transform, "CargoPanel", 0, 220, 1920, 760);
                panel.gameObject.SetActive(false);
                var view = panel.gameObject.AddComponent<CreatureInventoryView>();
                view.inventory = cabin.GetComponent<CreatureInventory>();
                BuildStorage(panel, view);
                var back = Button(panel, "Back", 1510, 0, 300, 70);
                UnityEventTools.AddPersistentListener(back.onClick, screen.ShowDesktop);
                Label(back.transform, "Label", "DESKTOP", 10, 10, 280, 50, 28);
            }
            screen.RegisterApp(ComputerAppId.Cargo, panel.gameObject);
            // Keep the footer tall enough for both the painted border and live text.
            Place(panel.Find("CapacityBar"), 0, 616, 1240, 64);
            Place(panel.Find("CapacityIcon"), 20, 627, 48, 42);
            Place(panel.Find("InventorySummary"), 78, 632, 560, 36);
            Place(panel.Find("ScrollHint"), 780, 634, 420, 36);
            Place(panel.Find("ItemDescription"), 650, 551, 568, 62);
            if (screen.Desktop.transform.Find("CargoIcon") == null)
            {
                var shortcut = Button(screen.Desktop.transform, "CargoIcon", 0, 0, 190, 220);
                UnityEventTools.AddPersistentListener(shortcut.onClick, screen.OpenCargo);
                Label(shortcut.transform, "Label", "CARGO", 0, 180, 190, 40, 25);
            }
            var cabinData = new SerializedObject(cabin);
            var oldPanel = cabinData.FindProperty("cargoPanel").objectReferenceValue as GameObject;
            cabinData.FindProperty("cargoPanel").objectReferenceValue = panel.gameObject;
            cabinData.ApplyModifiedPropertiesWithoutUndo();
            if (oldPanel != null && oldPanel != panel.gameObject) Undo.DestroyObjectImmediate(oldPanel);
            foreach (var hotspot in cabin.GetComponentsInChildren<CabinPointerTarget>(true))
                if (hotspot.name == "Backpack" || hotspot.name == "BackpackHotspot") Undo.DestroyObjectImmediate(hotspot.gameObject);
            foreach (var label in cabin.GetComponentsInChildren<Text>(true))
                if (label.text == "MỞ BALÔ") { Undo.RecordObject(label, "Cargo label"); label.text = "MỞ CARGO"; }
            var skin = screen.GetComponent<ComputerDesktopSkin>();
            if (skin != null)
            {
                skin.cargoIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Cargo_icon.png");
                skin.font = DialogueUIEditor.EnsureFont();
                EditorUtility.SetDirty(skin);
            }
            InstallStoryHull(cabin, screen);
            EditorUtility.SetDirty(screen); EditorUtility.SetDirty(cabin);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
        }

        private static void BuildStorage(Transform panel, CreatureInventoryView view)
        {
            Label(panel, "StorageHeading", "ITEM STORAGE", 12, 0, 590, 44, 27);
            Label(panel, "DetailHeading", "ITEM DETAIL", 650, 0, 570, 44, 27);
            Line(panel, "StorageRule", 12, 44, 582, 2);
            Line(panel, "VerticalRule", 622, 0, 2, 600);
            var viewport = Rect(panel, "StorageViewport", 12, 58, 590, 540);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var grid = Rect(viewport, "Items", 0, 0, 580, 746);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = grid;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 34;
            view.icons = new RawImage[CreatureInventory.Capacity]; view.labels = new Text[CreatureInventory.Capacity];
            view.slots = new Button[CreatureInventory.Capacity]; view.selectionFrames = new Outline[CreatureInventory.Capacity];
            var slotSprite = PanelSprite("Panels/Panel_Small.png");
            for (int i = 0; i < CreatureInventory.Capacity; i++)
            {
                var slot = Button(grid, "ItemSlot" + (i + 1), 4 + i % 3 * 193, 4 + i / 3 * 187, 180, 174);
                slot.image.sprite = slotSprite; slot.image.type = Image.Type.Sliced;
                var colors = slot.colors; colors.normalColor = Color.white; colors.highlightedColor = new(.82f, .95f, 1);
                colors.pressedColor = new(.65f, .82f, 1); colors.disabledColor = new(.82f, .9f, 1, .65f); slot.colors = colors;
                view.slots[i] = slot;
                var outline = slot.gameObject.AddComponent<Outline>();
                outline.effectColor = new(.22f, .52f, 1); outline.effectDistance = new(3, -3); outline.enabled = false;
                view.selectionFrames[i] = outline;
                view.icons[i] = Preview(Rect(slot.transform, "IconFrame", 14, 10, 150, 140), "ItemIcon");
                view.labels[i] = Label(slot.transform, "Quantity", "", 110, 138, 55, 32, 24);
                view.labels[i].alignment = TextAnchor.MiddleRight;
            }
            Picture(panel, "PreviewFrame", PanelSprite("Panels/Panel_Large.png"), 647, 58, 575, 385);
            view.detailPreview = Preview(Rect(panel, "PreviewArtwork", 675, 80, 517, 340), "ItemPreview");
            view.detailName = Label(panel, "ItemName", "", 650, 458, 460, 76, 31);
            view.detailQuantity = Label(panel, "ItemQuantity", "", 1120, 463, 96, 55, 34);
            view.detailQuantity.alignment = TextAnchor.MiddleRight;
            Line(panel, "DetailRule", 650, 539, 570, 2);
            view.detailDescription = Label(panel, "ItemDescription", "", 650, 551, 568, 76, 22);
            Picture(panel, "CapacityBar", PanelSprite("Panels/Header_Long.png"), 0, 630, 1240, 50);
            var icon = Rect(panel, "CapacityIcon", 18, 634, 48, 42).gameObject.AddComponent<RawImage>();
            icon.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Cargo_icon.png"); icon.raycastTarget = false;
            view.summary = Label(panel, "InventorySummary", "", 78, 637, 560, 36, 25);
            Label(panel, "ScrollHint", "Cuộn để xem thêm ô kho", 780, 637, 420, 36, 21).alignment = TextAnchor.MiddleRight;
        }

        private static void InstallStoryHull(CabinStationView cabin, ComputerScreenController screen)
        {
            var story = cabin.GetComponent<ZoneOneStory>(); if (story == null) return;
            if (story.creatureTwo != null && story.creatureTwo.description.Contains("ở Rãnh San Hô Cổ"))
            {
                story.creatureTwo.description = story.creatureTwo.description.Replace("ở Rãnh San Hô Cổ", "ở Thềm Biển Sâu");
                EditorUtility.SetDirty(story.creatureTwo);
            }
            SubmarineUpgradeUIEditor.CutRegularUpgradeScope(screen);
            var entry = screen.GetComponentsInChildren<UpgradeEntryConfig>(true).FirstOrDefault(e => e.UpgradeId == "ExpeditionModule");
            if (entry == null) throw new InvalidOperationException("Existing Upgrade app is missing ExpeditionModule.");
            var action = entry.GetComponent<StoryHullUpgradeAction>();
            if (action == null) action = Undo.AddComponent<StoryHullUpgradeAction>(entry.gameObject);
            action.story = story;
            var data = new SerializedObject(entry);
            data.FindProperty("actionSource").objectReferenceValue = action;
            data.FindProperty("description").stringValue = "Lớp vỏ chịu áp lực Tầng 1. Hoàn thành ba địa điểm, thu dịch kết dính và bản vẽ E.A để lắp vỏ.";
            data.FindProperty("materialRequirements").arraySize = 0;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(action);
        }
        private static Sprite PanelSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Update_UIKit/Sprites/" + path);
        private static void Destroy(Transform target) { if (target != null) Undo.DestroyObjectImmediate(target.gameObject); }
        private static void Place(Transform target, float x, float y, float w, float h)
        {
            if (target is not RectTransform rect) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new(0, 1);
            rect.anchoredPosition = new(x, -y); rect.sizeDelta = new(w, h);
        }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new(0, 1);
            rect.anchoredPosition = new(x, -y); rect.sizeDelta = new(w, h); return rect;
        }
        private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            var label = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = Font; label.fontSize = size; label.text = value; label.color = Ink;
            label.alignment = TextAnchor.UpperLeft; label.raycastTarget = false; return label;
        }
        private static Image Picture(Transform parent, string name, Sprite sprite, float x, float y, float w, float h)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.type = Image.Type.Sliced; image.raycastTarget = false; return image;
        }
        private static void Line(Transform parent, string name, float x, float y, float w, float h)
        { Picture(parent, name, null, x, y, w, h).color = new(Ink.r, Ink.g, Ink.b, .45f); }
        private static Button Button(Transform parent, string name, float x, float y, float w, float h)
        {
            var image = Picture(parent, name, null, x, y, w, h); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; return button;
        }
        private static RawImage Preview(Transform parent, string name)
        {
            var rect = Rect(parent, name, 0, 0, 100, 100);
            var fitter = rect.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var image = rect.gameObject.AddComponent<RawImage>(); image.raycastTarget = false; image.enabled = false; return image;
        }
    }
}

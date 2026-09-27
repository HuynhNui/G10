using System;
using System.IO;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace G10.Prototype.Editor
{
    public static class ZoneOneStoryEditor
    {
        private const string Content = "Assets/_Project/Content/Zone01";
        private const string Kit = "Assets/_Project/Art/UI/Desktop/Update_UIKit/Sprites/Icons/";
        [MenuItem("G10/Zone 1/Install Story and Polish")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01") throw new InvalidOperationException("Open Zone01 first.");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install Zone01 story");
            var story = cabin.GetComponent<ZoneOneStory>();
            bool fresh = story == null;
            if (fresh) story = Undo.AddComponent<ZoneOneStory>(cabin.gameObject);
            story.cabin = cabin; story.navigation = cabin.Navigation;
            story.survey = cabin.GetComponent<PhotoSurveyZone>(); story.inventory = cabin.GetComponent<CreatureInventory>();
            story.survey.Story = story;
            story.creatureOne = Entry("Creatures/001", "Z1_Creature_01", "Sinh vật 001 — Giáp xác rạn tảo", "Giáp xác nhỏ uốn lượn giữa tảo đỏ. Biểu bì dạng lưới phân tán áp lực thay vì gồng mình chống lại nước.", "Assets/_Project/Art/PhotoCaptureRuntime/Z1_Creature_01_Main.png", false);
            story.creatureTwo = Entry("Creatures/002", "Z1_Creature_02", "Sinh vật 002 — Thân mềm bám đá", "Sinh vật thân mềm bám đá ở Thềm Biển Sâu.", Kit + "Modules/Rock_Breaker.png", true);
            story.emmaTube = Entry("Items/001_EmmaTube", ZoneOneStory.EmmaBlueprint, "Bản vẽ của Emma", "Di vật: giấy can ép plastic vẽ công thức liên kết mô sinh học chịu áp lực, ký E.A. Không có bản ghi âm.", Kit + "Materials/Energy_Cell.png", true);
            string[] intendedOrder = { "zone01-left", "zone01-east", "zone01-north" };
            if (intendedOrder.Any(id => !story.survey.locations.Any(p => p != null && p.id == id)))
                throw new InvalidOperationException("Zone01 is missing an authored mission POI.");
            story.poiIds = intendedOrder;
            story.config = MissionConfigEditor.GetOrCreateZoneConfigs()[0];
            story.contentCatalog = new[] { story.creatureOne, story.creatureTwo, story.emmaTube };
            if (story.poiIds.Length != 3) throw new InvalidOperationException("Expected the three existing Zone01 POIs.");
            if (fresh)
            {
                MapPoi startPoi = story.survey.FindPoi(story.poiIds[0]);
                Vector2 start = startPoi.mapPosition + Vector2.down * 14;
                if (!cabin.Navigation.CanOccupy(start)) start = startPoi.mapPosition;
                if (!cabin.Navigation.CanOccupy(start)) throw new InvalidOperationException("Start POI is not navigable.");
                var nav = new SerializedObject(cabin.Navigation);
                nav.FindProperty("startPosition").vector2Value = start;
                nav.FindProperty("startHeading").floatValue = 0;
                nav.ApplyModifiedPropertiesWithoutUndo();
            }
            CargoAppEditor.Install();

            foreach (var view in cabin.GetComponentsInChildren<CreatureCaptureView>(true))
            {
                var title = view.transform.Find("CatchingControls/Title");
                if (title != null && title.TryGetComponent<UnityEngine.UI.Text>(out var text)) text.text = "CÁNH TAY GẮP / THU THẬP";
                foreach (var button in view.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    if (button.name == "Catch") button.GetComponentInChildren<UnityEngine.UI.Text>(true).text = "THU THẬP";
            }
            var loop = UnityEngine.Object.FindAnyObjectByType<ExpeditionLoop>();
            if (loop != null)
            {
                var catalog = loop.creatureCatalog.ToList();
                foreach (var entry in new[] { story.creatureOne, story.creatureTwo, story.emmaTube })
                {
                    var existing = catalog.Find(c => c.id == entry.id);
                    if (existing == null) catalog.Add(new ExpeditionCreatureAsset { id = entry.id, icon = entry.Image });
                    else existing.icon = entry.Image;
                }
                loop.creatureCatalog = catalog.ToArray();
                loop.contentCatalog = new[] { story.creatureOne, story.creatureTwo, story.emmaTube };
                if (fresh && loop.zones.Length > 0)
                    loop.zones[0].restAreas = new[] { new MapPoi { id = "Rạn Tảo Đỏ • Điểm xuất phát", mapPosition = story.survey.FindPoi(story.poiIds[0]).mapPosition, arrivalRadius = 35 },
                        new MapPoi { id = "Bến cũ", mapPosition = new Vector2(600, 100), arrivalRadius = 30 } };
                EditorUtility.SetDirty(loop); EditorSceneManager.MarkSceneDirty(loop.gameObject.scene);
            }
            EditorUtility.SetDirty(story); EditorUtility.SetDirty(story.survey);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            SubmarineUpgradeUIEditor.FixVisualLayout();
            AssetDatabase.SaveAssets();
            Debug.Log("Zone01 story installed: three independent POIs, stable-ID objectives, 12 Cargo slots, scene ship configuration.");
        }
        private static SurveyContentDefinition Entry(string folder, string id, string name, string description, string art, bool placeholder)
        {
            string root = Content + "/" + folder;
            Directory.CreateDirectory(root); AssetDatabase.Refresh();
            string path = root + "/Definition.asset";
            var entry = AssetDatabase.LoadAssetAtPath<SurveyContentDefinition>(path);
            if (entry != null)
            {
                entry.id = id;
                if (string.IsNullOrEmpty(entry.displayName)) entry.displayName = name;
                EditorUtility.SetDirty(entry);
                return entry; // Preserve designer descriptions and replacement sprites on reruns.
            }
            string imagePath = root + "/Preview.png";
            if (!AssetDatabase.CopyAsset(art, imagePath)) throw new InvalidOperationException("Cannot copy " + art);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            entry = ScriptableObject.CreateInstance<SurveyContentDefinition>();
            entry.id = id; entry.displayName = name; entry.description = description; entry.placeholderArt = placeholder;
            entry.image = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath); entry.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            AssetDatabase.CreateAsset(entry, path);
            var go = new GameObject(id, typeof(SpriteRenderer), typeof(SurveyContentRecord));
            go.GetComponent<SurveyContentRecord>().definition = entry;
            go.GetComponent<SpriteRenderer>().sprite = entry.sprite;
            entry.prefab = PrefabUtility.SaveAsPrefabAsset(go, root + "/" + id + ".prefab");
            UnityEngine.Object.DestroyImmediate(go); EditorUtility.SetDirty(entry);
            return entry;
        }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        { var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; rect.SetParent(parent, false); Place(rect, x, y, w, h); return rect; }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); }
        private static UnityEngine.UI.Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            var text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.text = value;
            text.color = new Color(.1f, .2f, .35f); text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false; return text;
        }
        private static UnityEngine.UI.Button Button(Transform parent, string name, string caption, float x, float y, float w, float h, UnityAction action)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.7f, .91f, .96f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            var text = Label(rect, "Label", caption, 8, 8, w - 16, h - 16, 25); text.alignment = TextAnchor.MiddleCenter; return button;
        }
        public static void InstallBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/GameplayCore.unity");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity", OpenSceneMode.Additive);
            Install(); EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        }
    }
}

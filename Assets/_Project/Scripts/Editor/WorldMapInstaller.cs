using System;
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
    public static class WorldMapInstaller
    {
        [MenuItem("G10/Zone 1/Install World Map UI")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01") throw new InvalidOperationException("Open Zone01 first.");
            if (cabin.GetComponent<WorldMapController>() != null) return;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environment/Map/Map.png");
            if (texture == null) throw new InvalidOperationException("Missing World Map.png.");
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install world map UI");
            var owner = cabin.gameObject.AddComponent<WorldMapController>(); owner.cabin = cabin;
            Transform frame = cabin.MapPanel.transform.parent;
            var panel = Box("WorldMapPanel", frame, 0, 0, 1920, 1080);
            panel.gameObject.AddComponent<Image>().color = new(.02f,.055f,.075f,1);
            owner.worldPanel = panel.gameObject;
            var art = Box("WorldArtwork", panel, 96, 95, 1728, 972).gameObject.AddComponent<RawImage>();
            art.texture = texture; art.raycastTarget = false;
            var readout = Label(panel, "ZoneReadout", "Chọn khu vực để mở bản đồ", 425, 15, 1080, 60);
            owner.zoneMaps = new GameObject[4]; owner.zoneMaps[0] = cabin.MapPanel;
            owner.zone01Overlay = cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>(true);
            // Hand-traced polygon boundaries from Mapline.jpg, in original 1920x1080 pixels.
            Vector2[][] shapes = {
                new Vector2[] { new(315,90),new(500,40),new(665,50),new(850,150),new(955,260),new(900,310),new(810,385),new(730,490),new(655,550),new(530,580),new(400,575),new(270,500),new(220,390),new(255,290) },
                new Vector2[] { new(960,265),new(1080,240),new(1195,275),new(1255,340),new(1285,490),new(1220,595),new(1095,650),new(935,650),new(815,600),new(765,510),new(800,400),new(880,315) },
                new Vector2[] { new(1270,300),new(1390,265),new(1500,300),new(1600,380),new(1650,490),new(1610,610),new(1515,680),new(1400,730),new(1275,730),new(1200,700),new(1210,590),new(1280,500) },
                new Vector2[] { new(770,645),new(950,660),new(1080,710),new(1180,790),new(1205,900),new(1150,990),new(1030,1040),new(820,1050),new(730,980),new(665,880),new(650,775),new(690,690) }
            };
            for (int i = 0; i < shapes.Length; i++)
            {
                string title = $"ZONE {i + 1:00}";
                var rect = Box("Zone" + (i + 1).ToString("00") + "Hotspot", art.transform, 0, 0, 1728, 972);
                var hotspot = rect.gameObject.AddComponent<WorldMapZoneHotspot>();
                hotspot.controller = owner; hotspot.zoneIndex = i; hotspot.readout = readout;
                hotspot.zoneLabel = title + (i == 0 ? " • MỞ BẢN ĐỒ" : " • CHƯA CÓ BẢN ĐỒ");
                hotspot.polygon = shapes[i];
                for (int p = 0; p < hotspot.polygon.Length; p++)
                    hotspot.polygon[p] = new(hotspot.polygon[p].x / 1920, 1 - hotspot.polygon[p].y / 1080);
                if (i > 0)
                {
                    var placeholder = Box("Zone" + (i + 1).ToString("00") + "MapPanel", frame, 0, 0, 1920, 1080);
                    placeholder.gameObject.AddComponent<Image>().color = new(.03f,.08f,.11f,1);
                    Label(placeholder, "Title", title, 500, 350, 920, 80);
                    Label(placeholder, "Unavailable", "CHƯA CÓ BẢN ĐỒ KHU VỰC", 400, 455, 1120, 80);
                    Button(placeholder, "WorldMap", "MAP TỔNG / ESC", 40, 15, 330, owner.OpenWorld);
                    owner.zoneMaps[i] = placeholder.gameObject;
                    placeholder.gameObject.SetActive(false);
                }
                var back = owner.zoneMaps[i].AddComponent<ZoneMapBackHandler>(); back.worldMap = owner;
            }
            Button(panel, "Back", "CABIN / ESC", 1600, 15, 290, owner.CloseWorld);
            Button(panel, "Resume", "MỞ LẠI KHU VỰC", 35, 15, 345, owner.ResumeZone);
            Button(cabin.MapPanel.transform, "WorldMap", "MAP TỔNG", 270, 15, 140, owner.OpenWorld);
            cabin.MapPanel.transform.Find("WorldMap/Label").GetComponent<Text>().fontSize = 24;
            // The existing top-right map back button now follows the zone -> world -> cabin flow.
            var oldBack = cabin.MapPanel.transform.Find("BackToCabin").GetComponent<Button>();
            while (oldBack.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(oldBack.onClick, 0);
            UnityEventTools.AddPersistentListener(oldBack.onClick, owner.OpenWorld);
            oldBack.GetComponentInChildren<Text>().text = "MAP TỔNG / ESC";
            panel.gameObject.SetActive(false);
            EditorUtility.SetDirty(cabin); EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
        }
        public static void InstallBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity");
            Install();
            CreatureArtPrepTool.Install();
            EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        }
        private static RectTransform Box(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new(0,1);
            r.sizeDelta = new(w,h); r.anchoredPosition = new(x+w/2,-y-h/2); return r;
        }
        private static Font GetThemeFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Bold.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        private static Sprite GetThemeBorder(string name = "panel-000.png") =>
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/Borders/" + name);

        private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h)
        {
            var label = Box(name,parent,x,y,w,h).gameObject.AddComponent<Text>();
            label.font = GetThemeFont(); label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter; label.color = new(.77f,1,.9f); label.raycastTarget = false; label.text = value; return label;
        }
        private static void Button(Transform parent, string name, string label, float x, float y, float w, UnityAction action)
        {
            var rect = Box(name,parent,x,y,w,60); var image = rect.gameObject.AddComponent<Image>();
            Sprite border = GetThemeBorder("panel-000.png");
            if (border != null) { image.sprite = border; image.type = Image.Type.Sliced; }
            image.color = new(.04f,.14f,.18f,.96f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(button.onClick, action); Label(rect,"Label",label,0,0,w,60);
        }
    }
}

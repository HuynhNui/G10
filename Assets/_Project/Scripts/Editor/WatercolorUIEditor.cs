using System;
using System.Collections.Generic;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>Installs individual, editable UI sprites without replacing gameplay objects or callbacks.</summary>
    public static class WatercolorUIEditor
    {
        private const string Pack = "Assets/_Project/Art/UI/UI_AssetPack/";
        private static readonly Color Ink = new(.16f, .23f, .53f, 1);
        private static readonly Dictionary<string, Sprite> Sprites = new();
        private readonly struct Slice
        {
            public readonly string Name;
            public readonly Rect Rect;
            public readonly Vector4 Border;
            public Slice(string name, float x, float y, float w, float h, float border = 0)
            { Name = name; Rect = new(x, y, w, h); Border = Vector4.one * border; }
        }

        [MenuItem("G10/UI/Apply Watercolor Asset Pack")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before applying the UI pack.");
            var cabin = Object.FindObjectsByType<CabinStationView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(v => v.gameObject.scene.name == "Zone01");
            if (cabin == null) throw new InvalidOperationException("Open Zone01 before applying the watercolor UI pack.");
            PrepareSprites();
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Apply watercolor UI pack");
            var frame = cabin.MapPanel.transform.parent;
            var nav = cabin.NavigationPanel.transform;
            var radar = frame.Find("RadarPanel");
            var camera = frame.Find("CameraPanel");
            var map = cabin.MapPanel.transform;
            SharedHud(nav, cabin, "Helm");
            Hide(nav, "MapShortcut", "RadarShortcut", "BackToCabin");
            // The helm retains its painted controls and embedded values; no added side cards.
            var navigationInfo = nav.Find("NavigationInfo");
            if (navigationInfo != null) Place(navigationInfo, 65, 990, 1790, 65);
            StyleExistingPanel(navigationInfo, "card");
            if (navigationInfo != null) navigationInfo.GetComponent<Image>().pixelsPerUnitMultiplier = 5;
            RestyleText(nav.Find("NavigationStatus")?.GetComponent<Text>(), 26);
            Remove(nav, "Brake");
            if (radar != null) RefreshRadar(radar, cabin);
            RefreshChart(map, cabin);
            var world = cabin.GetComponent<WorldMapController>();
            if (world != null && world.worldPanel != null) RefreshWorld(world, cabin);
            if (camera != null) RefreshCamera(camera, cabin);
            WatercolorSurfaceEditor.ApplyTo(cabin);
            foreach (var scaler in cabin.GetComponentsInChildren<CanvasScaler>(true))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
            }
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Watercolor UI pack applied: sliced assets, shared HUD, chart, region map, radar and camera. Save Zone01.");
        }

        public static void ApplyBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity");
            Apply();
            EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("G10/UI/Slice Watercolor Asset Pack")]
        public static void PrepareSprites()
        {
            Sprites.Clear();
            SliceSheet("Common/UI_Common_CoreSheet.png", 1086, new[] {
                new Slice("hud", 14, 38, 1420, 145, 60), new Slice("title", 347, 202, 754, 149, 60),
                new Slice("button", 27, 365, 260, 111, 40), new Slice("button-active", 295, 363, 270, 111, 40),
                new Slice("button-pink", 566, 365, 264, 109, 40), new Slice("button-dark", 832, 365, 283, 111, 40),
                new Slice("capsule", 561, 484, 508, 157, 60), new Slice("card", 23, 638, 520, 236, 60),
                new Slice("bubble", 551, 657, 480, 223, 68), new Slice("footer", 15, 887, 1153, 180, 70),
                new Slice("small-card", 1094, 499, 330, 156, 48)
            });
            SliceSheet("Common/UI_Common_IconsSheet.png", 1254, new[] {
                new Slice("map",25,65,290,220), new Slice("helm",339,20,287,283), new Slice("radar",648,32,258,259),
                new Slice("home",952,43,267,241), new Slice("camera",35,319,290,229), new Slice("photo",360,327,257,224),
                new Slice("pin",676,307,220,249), new Slice("focus",963,316,245,234), new Slice("energy",55,566,252,245),
                new Slice("speed",357,562,267,253), new Slice("depth",650,567,259,244), new Slice("compass",951,564,272,265),
                new Slice("back",368,831,220,215), new Slice("close",667,831,224,215), new Slice("confirm",966,831,220,215),
                new Slice("lock",382,1037,204,210), new Slice("warning",651,1037,240,210)
            });
            SliceSheet("Map/UI_Map_AssetSheet.png", 1086, new[] {
                new Slice("marker",39,94,287,354), new Slice("marker-active",307,41,394,445), new Slice("marker-locked",682,92,291,350),
                new Slice("marker-focus",47,468,237,239), new Slice("selection-ring",273,462,246,249),
                new Slice("point-title",521,494,701,215,68), new Slice("zone-card",20,712,612,319,70),
                new Slice("point-card",635,733,451,279,68)
            });
            SliceSheet("Camera/UI_Camera_AssetSheet.png", 1086, new[] {
                new Slice("camera-header",9,50,1430,171,64), new Slice("photo-badge",15,275,364,134,48),
                new Slice("empty-badge",384,275,365,136,48), new Slice("corner-tl",763,251,103,88),
                new Slice("corner-tr",929,250,103,91), new Slice("corner-bl",763,351,106,94),
                new Slice("corner-br",928,351,106,94), new Slice("crosshair",1055,280,149,146),
                new Slice("camera-focus",1210,236,229,229), new Slice("metadata",15,462,804,145,55),
                new Slice("camera-footer",18,610,802,152,58), new Slice("thumbnail",1136,469,291,270,40),
                new Slice("saved",831,729,602,140,48), new Slice("shutter",766,877,161,174),
                new Slice("shutter-hover",927,869,175,183), new Slice("shutter-pressed",1101,875,163,175),
                new Slice("shutter-disabled",1266,875,168,176)
            });
            SliceSheet("Ornaments/UI_Ornaments_Sheet.png", 1086, new[] {
                new Slice("seaweed-left",15,10,620,366), new Slice("seaweed-right",811,8,625,369),
                new Slice("coral",577,405,211,247), new Slice("bubbles",1078,410,151,221),
                new Slice("divider",87,677,1048,120), new Slice("accent",728,971,95,100)
            });
        }

        public static Sprite GetSprite(string name)
        {
            if (Sprites.Count == 0)
                foreach (string id in AssetDatabase.FindAssets("t:Texture2D", new[] { Pack.TrimEnd('/') }))
                    foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(id)).OfType<Sprite>())
                        Sprites[sprite.name] = sprite;
            return Sprites.TryGetValue(name, out var result) ? result : null;
        }

        private static void SliceSheet(string relative, int height, Slice[] slices)
        {
            string path = Pack + relative;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter
                ?? throw new InvalidOperationException("Missing UI sheet: " + path);
            var data = slices.Select(s => new SpriteMetaData {
                name = s.Name, rect = new(s.Rect.x, height - s.Rect.yMax, s.Rect.width, s.Rect.height),
                alignment = (int)SpriteAlignment.Center, pivot = new(.5f,.5f), border = s.Border
            }).ToArray();
#pragma warning disable 0618
            var old = importer.spritesheet;
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple ||
                importer.mipmapEnabled || !importer.alphaIsTransparency || importer.filterMode != FilterMode.Bilinear ||
                importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize != 2048 ||
                old.Length != data.Length || !old.Zip(data, (a,b) => a.name == b.name && a.rect == b.rect && a.border == b.border).All(v => v);
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
                importer.spritePixelsPerUnit = 100;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.spritesheet = data;
                importer.SaveAndReimport();
            }
#pragma warning restore 0618
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) Sprites[sprite.name] = sprite;
        }

        private static void SharedHud(Transform panel, CabinStationView cabin, string selected)
        {
            var hud = Panel(panel, "WatercolorHUD", "hud", 16, 8, 1888, 98);
            hud.transform.SetAsLastSibling();
            NavButton(hud.transform,"Map","BẢN ĐỒ","map",selected == "Map",105,14,270,cabin.OpenMap);
            NavButton(hud.transform,"Helm","BÀN LÁI","helm",selected == "Helm",389,14,280,cabin.OpenNavigation);
            NavButton(hud.transform,"Radar","RADAR","radar",selected == "Radar",683,14,260,cabin.OpenRadar);
            Remove(hud.transform,"Camera");
            Remove(hud.transform,"Cabin"); // Escape remains the return-to-cabin action.
        }

        private static void RefreshRadar(Transform radar, CabinStationView cabin)
        {
            Hide(radar, "RadarHelm", "BackToCabin");
            SharedHud(radar,cabin,"Radar");
            var info = radar.Find("RadarInfo");
            if (info != null) { Place(info,260,904,1010,145); StyleExistingPanel(info,"capsule"); }
            var status = radar.Find("RadarStatus");
            if (status != null) { Place(status,325,920,875,106); RestyleText(status.GetComponent<Text>(),26); }
            // Scan remains the physical button in the authored radar artwork.
        }

        private static void RefreshChart(Transform map, CabinStationView cabin)
        {
            Hide(map,"HelmShortcut","BackToCabin","WorldMap");
            SharedHud(map,cabin,"Map");
            var world=cabin.GetComponent<WorldMapController>();
            if(world!=null) NavButton(map,"WatercolorWorld","MAP TỔNG","map",false,36,111,260,world.OpenWorld);
            var title = map.Find("SurveyLegend");
            TMP_Text coordinateReadout=null;
            if(title!=null)
            {
                Place(title,560,118,800,58);
                SimplePanel(title,new(.97f,.96f,.86f,.94f));
                var legacy=title.GetComponentInChildren<Text>(true);if(legacy!=null)legacy.enabled=false;
                coordinateReadout=NewLabel(title,"Coordinate","RÊ CHUỘT TRÊN BẢN ĐỒ ĐỂ ĐỌC TỌA ĐỘ",24,7,752,44,24);
            }
            // A flat paper frame stays sharp at this size; watercolor remains in the map and primary HUD.
            var background = SimplePanel(map,"WatercolorChartBackground",26,108,1868,948,new(.95f,.97f,.91f,.98f));
            background.transform.SetAsFirstSibling();
            var mapImage = map.GetComponent<Image>();
            if (mapImage != null) mapImage.color = new(.73f,.87f,.94f,1);
            var content=map.Find("SquareChartContent");
            var frame=map.Find("ChartOuterFrame");
            var mapConfig=map.GetComponent<ZoneMapPresentation>()?.config;
            Vector2 mapWorldSize=mapConfig!=null?mapConfig.WorldSize:Vector2.one*ZoneNavigation.DefaultGridSize;
            if(content!=null) Place(content,240,185,1440,840);
            if(frame!=null)
            {
                Place(frame,174,165,1552,903);
                var frameImage=frame.GetComponent<Image>();
                if(frameImage!=null) { frameImage.sprite=null;frameImage.color=Color.clear;frameImage.raycastTarget=false; }
                foreach(var t in frame.GetComponentsInChildren<Text>(true))
                {
                    if(t.name.StartsWith("X") && int.TryParse(t.name.Substring(1),out int x))
                    { t.gameObject.SetActive(x%100==0&&x<=mapWorldSize.x); Place(t.transform,66+x/mapWorldSize.x*1440-30,860,60,30); }
                    if(t.name.StartsWith("Y") && int.TryParse(t.name.Substring(1),out int y))
                    { t.gameObject.SetActive(y%100==0&&y<=mapWorldSize.y); Place(t.transform,3,20+(mapWorldSize.y-y)/mapWorldSize.y*840-15,53,30); }
                    ConvertLegacyText(t,20);
                }
            }
            var footer=map.Find("ChartFooter"); if(footer!=null)footer.gameObject.SetActive(false);
            Remove(map,"ChartCoordinate");
            foreach(var overlay in cabin.GetComponentsInChildren<PhotoSurveyMap>(true))
            {
                if(overlay.Locations==null)continue;
                RemoveStartingWith(overlay.transform,"WatercolorLocation");
                var locationTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environment/Zone1/location.png");
                overlay.locationIcons=new RawImage[overlay.Locations.Length];
                for(int i=0;i<overlay.locationIcons.Length;i++)
                {
                    string name=i==0?"SurveyLocation":"MapLocation"+(i+1);
                    var child=overlay.transform.Find(name);
                    if(child==null)
                    {
                        child=new GameObject(name,typeof(RectTransform),typeof(RawImage)).transform;
                        child.SetParent(overlay.transform,false);
                        Undo.RegisterCreatedObjectUndo(child.gameObject,"Restore map location");
                    }
                    var icon=child.GetComponent<RawImage>()??child.gameObject.AddComponent<RawImage>();
                    icon.texture=locationTexture;icon.enabled=true;icon.raycastTarget=false;child.gameObject.SetActive(true);
                    var rect=icon.rectTransform;
                    Vector2 worldSize=overlay.mapConfig!=null?overlay.mapConfig.WorldSize:Vector2.one*ZoneNavigation.DefaultGridSize;
                    float gridSize=overlay.mapConfig!=null?overlay.mapConfig.GridSize:ZoneNavigation.DefaultGridSize;
                    rect.anchorMin=rect.anchorMax=overlay.mapConfig!=null
                        ?overlay.mapConfig.CoordinatesToUV(overlay.mapConfig.GridCellCenter(overlay.Locations[i].mapPosition))
                        :ZoneNavigation.CoordinatesToUV(ZoneNavigation.CellCenter(overlay.Locations[i].mapPosition),worldSize);
                    rect.anchoredPosition=Vector2.zero;
                    rect.sizeDelta=Vector2.Scale(overlay.rectTransform.rect.size,new Vector2(gridSize/worldSize.x,gridSize/worldSize.y));
                    overlay.locationIcons[i]=icon;
                }
                overlay.locationIcon=overlay.locationIcons.Length>0?overlay.locationIcons[0]:null;
                if(overlay.taskReadout!=null)
                {
                    overlay.positionTaskCardAtMarker=true;
                    var card=overlay.taskReadout.transform.parent;
                    SimplePanel(card,new(.97f,.96f,.88f,.96f));
                    Place(card,1230,220,540,180);
                    overlay.styledTaskReadout=NewLabel(card,"TaskText",overlay.taskReadout.text,24,18,492,144,23);
                    overlay.styledTaskReadout.alignment=TextAlignmentOptions.TopLeft;
                    overlay.taskReadout.enabled=false;
                    card.SetAsLastSibling();
                }
                if(overlay.transform.IsChildOf(map)&&coordinateReadout!=null)
                {
                    overlay.coordinateReadout=coordinateReadout;
                    if(overlay.destinationReadout!=null)overlay.destinationReadout.enabled=false;
                }
                EditorUtility.SetDirty(overlay);
            }
        }

        private static void RefreshWorld(WorldMapController world, CabinStationView cabin)
        {
            var panel=world.worldPanel.transform;
            Hide(panel,"Back","Resume","ZoneReadout");
            RemoveStartingWith(panel,"WatercolorRegion");
            var hud=Panel(panel,"WatercolorHUD","hud",16,8,1888,98);hud.transform.SetAsLastSibling();
            NavButton(hud.transform,"Resume","MỞ LẠI KHU VỰC","back",false,75,14,370,world.ResumeZone);
            NewLabel(hud.transform,"Title","CHỌN KHU VỰC ĐỂ MỞ BẢN ĐỒ",470,15,940,65,33);
            Remove(hud.transform,"Cabin");
            foreach(var hotspot in panel.GetComponentsInChildren<WorldMapZoneHotspot>(true))
            {
                Remove(hotspot.transform,"WatercolorMarker");
                hotspot.surroundingDimOpacity=.42f;EditorUtility.SetDirty(hotspot);
            }
            if(world.zoneMaps==null)return;
            foreach(var zone in world.zoneMaps)
            {
                if(zone==null||zone==cabin.MapPanel)continue;
                SharedHud(zone.transform,cabin,"Map");
                StyleExistingButton(zone.transform.Find("WorldMap"),"MAP TỔNG");
            }
        }

        private static void RefreshCamera(Transform panel, CabinStationView cabin)
        {
            var view=panel.GetComponent<PhotoCameraView>();if(view==null)return;
            Hide(panel,"Title","Message","CameraTitle","BackToCabin");
            var background=panel.GetComponent<Image>();if(background!=null)background.color=new(.32f,.53f,.76f,1);
            var header=Panel(panel,"WatercolorHeader","camera-header",14,7,1892,106);header.transform.SetAsLastSibling();
            NewLabel(header.transform,"Title","CAMERA · ZONE 1",530,20,810,65,39);
            Remove(header.transform,"Cabin");
            Panel(panel,"PreviewFrame","hud",57,128,1806,774).transform.SetSiblingIndex(0);
            if(view.preview!=null)
            {
                Place(view.preview.transform,76,144,1768,733);
                view.preview.transform.SetSiblingIndex(1);
            }
            var badge=SimplePanel(panel,"PhotoCountBadge",100,152,330,68,new(.97f,.96f,.88f,.94f));
            Panel(badge.transform,"PhotoIcon","photo",20,10,48,48).preserveAspect=true;
            view.photoCount=NewLabel(badge.transform,"Value","",80,9,230,50,25);
            Panel(panel,"ViewfinderTL","corner-tl",106,244,73,64);
            Panel(panel,"ViewfinderTR","corner-tr",1741,244,73,64);
            Panel(panel,"ViewfinderBL","corner-bl",106,756,73,64);
            Panel(panel,"ViewfinderBR","corner-br",1741,756,73,64);
            Panel(panel,"ViewfinderCenter","crosshair",928,479,64,64);
            var footer=SimplePanel(panel,"CaptureFooter",58,910,1804,138,new(.97f,.96f,.88f,.96f));
            var meta=SimplePanel(panel,"MetadataCard",560,790,800,92,new(.96f,.97f,.91f,.9f));
            view.metadata=NewLabel(meta.transform,"Value","",28,10,744,72,25);
            var button=panel.Find("TakePhoto");
            if(button!=null)
            {
                Place(button,1370,921,116,116);
                var image=button.GetComponent<Image>(); image.sprite=GetSprite("shutter");image.type=Image.Type.Simple;image.color=Color.white;
                view.shutter=button.GetComponent<Button>();view.shutter.transition=Selectable.Transition.SpriteSwap;
                view.shutter.spriteState=new SpriteState{highlightedSprite=GetSprite("shutter-hover"),pressedSprite=GetSprite("shutter-pressed"),selectedSprite=GetSprite("shutter-hover"),disabledSprite=GetSprite("shutter-disabled")};
                Hide(button,"Label");button.SetAsLastSibling();
                NewLabel(footer.transform,"CaptureLabel","CHỤP",1460,35,240,70,36);
            }
            if(view.status!=null)view.status.enabled=false;
            view.instruction=NewLabel(footer.transform,"Instruction","Ảnh theo hướng mũi tàu.\nBấm CHỤP để lưu ảnh vào Photo Lab.",52,22,1040,92,25);
            view.instruction.alignment=TextAlignmentOptions.MidlineLeft;
            var saved=SimplePanel(panel,"SavedToast",1320,700,520,76,new(.97f,.96f,.88f,.96f));
            Panel(saved.transform,"SavedIcon","confirm",22,12,52,52).preserveAspect=true;
            NewLabel(saved.transform,"Label","ĐÃ LƯU VÀO PHOTO LAB",90,10,408,56,23);
            view.savedToast=saved.gameObject;saved.gameObject.SetActive(false);
            header.transform.SetAsLastSibling();EditorUtility.SetDirty(view);
        }

        private static void NavButton(Transform parent,string name,string label,string icon,bool active,float x,float y,float width,UnityAction action)
        {
            var image=Panel(parent,name,active?"button-active":"button",x,y,width,70);
            // Keep the original watercolor edges, with roughly 7px corners at a 70px button height.
            image.pixelsPerUnitMultiplier=5;
            image.raycastTarget=true;
            var button=image.GetComponent<Button>()??image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            while(button.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(button.onClick,0);
            UnityEventTools.AddPersistentListener(button.onClick,action);
            button.transition=Selectable.Transition.ColorTint;
            var colors=ColorBlock.defaultColorBlock;colors.normalColor=Color.white;colors.highlightedColor=new(.81f,.96f,1);colors.pressedColor=new(.65f,.85f,1);colors.selectedColor=Color.white;button.colors=colors;
            bool hasIcon=!string.IsNullOrEmpty(icon);
            if(hasIcon) { var glyph=Panel(image.transform,"Icon",icon,22,14,43,43);glyph.preserveAspect=true; }
            else Remove(image.transform,"Icon");
            NewLabel(image.transform,"Label",label,hasIcon?72:16,9,width-(hasIcon?88:32),50,28);
        }
        private static Image Panel(Transform parent,string name,string sprite,float x,float y,float w,float h)
        {
            var child=parent.Find(name);
            if(child==null){child=new GameObject(name,typeof(RectTransform),typeof(Image)).transform;child.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(child.gameObject,"Create watercolor UI");}
            Place(child,x,y,w,h);
            var image=child.GetComponent<Image>()??child.gameObject.AddComponent<Image>();image.sprite=GetSprite(sprite);image.color=Color.white;
            image.type=image.sprite!=null&&image.sprite.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple;
            image.pixelsPerUnitMultiplier=1;
            image.raycastTarget=false;return image;
        }
        private static Image SimplePanel(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var child=parent.Find(name);
            if(child==null){child=new GameObject(name,typeof(RectTransform),typeof(Image)).transform;child.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(child.gameObject,"Create simple UI panel");}
            Place(child,x,y,w,h);SimplePanel(child,color);return child.GetComponent<Image>();
        }
        private static void SimplePanel(Transform target,Color color)
        {
            var image=target.GetComponent<Image>()??target.gameObject.AddComponent<Image>();
            image.sprite=null;image.type=Image.Type.Simple;image.color=color;image.raycastTarget=false;
            var outline=target.GetComponent<Outline>()??target.gameObject.AddComponent<Outline>();
            outline.effectColor=new(.14f,.34f,.72f,.78f);outline.effectDistance=new(2,-2);outline.useGraphicAlpha=true;
        }
        private static TMP_Text NewLabel(Transform parent,string name,string text,float x,float y,float w,float h,int size)
        {
            var child=parent.Find(name);
            if(child==null){child=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).transform;child.SetParent(parent,false);}
            Place(child,x,y,w,h);
            var label=child.GetComponent<TextMeshProUGUI>()??child.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset")??TMP_Settings.defaultFontAsset;
            label.text=text;label.fontSize=size;label.color=Ink;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            label.enableAutoSizing=true;label.fontSizeMin=size*.8f;label.fontSizeMax=size;label.textWrappingMode=TextWrappingModes.Normal;
            return label;
        }
        private static void StyleExistingPanel(Transform target,string sprite)
        {
            if(target==null)return;
            var image=target.GetComponent<Image>();if(image==null)return;
            image.sprite=GetSprite(sprite);image.type=Image.Type.Sliced;image.color=Color.white;image.raycastTarget=false;
            foreach(var text in target.GetComponentsInChildren<Text>(true))RestyleText(text,text.fontSize);
        }
        private static void StyleExistingButton(Transform target,string title)
        {
            if(target==null)return;StyleExistingPanel(target,"button");
            var image=target.GetComponent<Image>();if(image!=null){image.raycastTarget=true;image.pixelsPerUnitMultiplier=5;}
            var button=target.GetComponent<Button>();if(button!=null)button.colors=ColorBlock.defaultColorBlock;
            var label=target.GetComponentInChildren<Text>(true);if(label!=null){RestyleText(label,28);label.text=title;}
        }
        private static void RestyleText(Text text,int size)
        {
            if(text==null)return;text.color=Ink;text.font=UIThemeStylerEditor.GetBoldFont();text.fontSize=size;
            foreach(var shadow in text.GetComponents<Shadow>())shadow.enabled=false;
        }
        private static TMP_Text ConvertLegacyText(Text source,int size)
        {
            if(source==null)return null;
            source.enabled=false;
            var label=NewLabel(source.transform,"TMP",source.text,0,0,source.rectTransform.rect.width,source.rectTransform.rect.height,size);
            var rect=label.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            return label;
        }
        private static void Hide(Transform parent,params string[] names)
        {foreach(string name in names){var item=parent.Find(name);if(item!=null)item.gameObject.SetActive(false);}}
        private static void Remove(Transform parent,string name)
        {var item=parent.Find(name);if(item!=null)Undo.DestroyObjectImmediate(item.gameObject);}
        private static void RemoveStartingWith(Transform parent,string prefix)
        {for(int i=parent.childCount-1;i>=0;i--){var child=parent.GetChild(i);if(child.name.StartsWith(prefix,StringComparison.Ordinal))Undo.DestroyObjectImmediate(child.gameObject);}}
        private static void Place(Transform target,float x,float y,float w,float h)
        {
            var rect=(RectTransform)target;rect.anchorMin=rect.anchorMax=new(0,1);rect.pivot=new(.5f,.5f);
            rect.anchoredPosition=new(x+w*.5f,-y-h*.5f);rect.sizeDelta=new(w,h);rect.localScale=Vector3.one;
        }
    }
}

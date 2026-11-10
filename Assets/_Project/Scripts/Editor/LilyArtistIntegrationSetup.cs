using System;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace G10.Prototype.Editor
{
    /// <summary>Targeted Editor authoring; never rebuilds the existing cabin, Photo Lab or mission rules.</summary>
    public static class LilyArtistIntegrationSetup
    {
        private const string Art = "Assets/_Project/Art/Lily25Days/";
        private static readonly Color Ink = new(.12f, .20f, .29f);
        private static TMP_FontAsset bodyFont, headingFont;

        [MenuItem("G10/Artwork/Integrate Lily 25 Days Artist Assets")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Install in Edit Mode.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the open scene before installation.");
            ImportTextures();
            bodyFont = TMP_Settings.defaultFontAsset;
            headingFont = CreateDocumentFont();
            BindMaterial("Z2_Creature_01", "BioPlastic", "Túi nhựa sinh học", "Lớp túi sinh học thu từ mẫu Zone 2. Vật liệu kết dính dùng để gia cố vỏ chịu áp.");
            BindMaterial("Z2_Creature_02", "Letonium", "Letonium", "Vật liệu cứng thu từ mẫu Zone 2. Kết hợp với túi nhựa sinh học trong bộ vỏ chịu áp.");
            BindMaterial("Z3_Creature_01", "Explosive", "Thuốc nổ sinh học", "Vật liệu nổ thu từ mẫu Zone 3. Dùng khi chế tạo bộ phá đá; không tự kích nổ trong kho.");
            BindMaterial("Z3_Creature_03", "FireStone", "Đá đánh lửa", "Vật liệu kích nổ thu từ mẫu Zone 3. Kết hợp với thuốc nổ trong công đoạn chế tạo bộ phá đá.");
            ConfigureMenu(); ConfigureCabin(); ConfigureEnding();
            AssetDatabase.Refresh();
        }
        [MenuItem("G10/Artwork/Apply Minimal Lily Menu")]
        public static void InstallMenuOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Install in Edit Mode.");
            bodyFont = TMP_Settings.defaultFontAsset;
            ConfigureMenu();
        }
        private static TMP_FontAsset CreateMenuFont()
        {
            string path = Art + "UTM Avo SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) return font;
            font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Art + "UTM Avo.ttf"), 90, 9,
                GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            font.name = "UTM Avo SDF";
            string characters = string.Concat(Enumerable.Range(32,352).Concat(Enumerable.Range(0x1EA0,90)).Select(c=>(char)c)) + "ƠơƯư·—→×…";
            font.TryAddCharacters(characters,out string missing);
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { bodyFont };
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font,path); AssetDatabase.AddObjectToAsset(font.material,font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas,font);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font); return font;
        }
        private static void ImportTextures()
        {
            foreach (string path in System.IO.Directory.GetFiles(Art, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.npotScale = TextureImporterNPOTScale.None; importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
                importer.sRGBTexture = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
                importer.isReadable = false;
                importer.spriteBorder = path.EndsWith("MenuButton.png") || path.EndsWith("ArchiveBorder.png") ? new Vector4(12,12,12,12) : Vector4.zero;
                importer.SaveAndReimport();
            }
        }
        private static TMP_FontAsset CreateDocumentFont()
        {
            string path = Art + "UTM AmericanaBEx SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) return font;
            font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Art + "UTM AmericanaBEx.ttf"), 64, 7,
                GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            font.name = "UTM AmericanaBEx SDF";
            string characters = string.Concat(Enumerable.Range(32, 352).Concat(Enumerable.Range(0x1EA0, 90)).Select(c => (char)c)) + "ƠơƯư·—→×…";
            font.TryAddCharacters(characters, out string missing);
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { bodyFont };
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font);
            return font;
        }
        private static void BindMaterial(string id, string art, string name, string description)
        {
            var definition = AssetDatabase.FindAssets("t:SurveyContentDefinition", new[] { "Assets/_Project/Content" })
                .Select(g => AssetDatabase.LoadAssetAtPath<SurveyContentDefinition>(AssetDatabase.GUIDToAssetPath(g))).Single(d => d.id == id);
            Undo.RecordObject(definition, "Bind collected material illustration");
            definition.collectedMaterial = Sprite(art); definition.collectedMaterialName = name; definition.collectedMaterialDescription = description;
            EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
            foreach (var guid in AssetDatabase.FindAssets("t:UpgradeMaterialDefinition", new[] { "Assets/_Project/Data/Upgrade" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<UpgradeMaterialDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.MaterialId != id) continue;
                var data = new SerializedObject(material); data.FindProperty("icon").objectReferenceValue = definition.collectedMaterial;
                data.FindProperty("displayName").stringValue = name; data.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(material);
            }
        }
        private static void ConfigureMenu()
        {
            string path = "Assets/_Project/Scenes/MainMenu/MainMenu.unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Single();
            var background = canvas.transform.Find("Background").GetComponent<Image>();
            background.sprite = Sprite("MenuBackground"); background.color = Color.white;
            canvas.transform.Find("Title").gameObject.SetActive(false);
            canvas.transform.Find("Title").GetComponent<UnityEngine.UI.Text>().text = "Lily 25 Days";
            canvas.transform.Find("Subtitle").gameObject.SetActive(false);
            Picture(canvas.transform, "Lily25DaysLogo", Sprite("Lily25DaysLogo"), 70, 73, 505, 208);
            var subtitle = canvas.transform.Find("Lily25DaysSubtitle");
            if (subtitle != null) subtitle.gameObject.SetActive(false);
            var skin = canvas.GetComponent<LilyMenuPresentation>() ?? Undo.AddComponent<LilyMenuPresentation>(canvas.gameObject);
            skin.bodyFont = CreateMenuFont(); skin.buttonArtwork = null; EditorUtility.SetDirty(skin);
            string[] names={"StartButton","ContinueButton","QuitButton"};
            string[] captions={"Chơi mới","Tiếp tục","Thoát"};
            for(int i=0;i<names.Length;i++)
            {
                var node=canvas.transform.Find(names[i]);if(node==null)continue;
                Rect(canvas.transform,names[i],85,375+i*122,760,112);
                var image=node.GetComponent<Image>();image.sprite=null;image.color=Color.clear;image.raycastTarget=true;
                foreach(var label in node.GetComponentsInChildren<Text>(true))
                { label.text=captions[i];label.fontSize=80;label.alignment=TextAnchor.MiddleLeft;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero; }
            }
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        private static void ConfigureCabin()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity", OpenSceneMode.Additive);
            var cabin = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CabinStationView>(true)).Single();
            var log = cabin.GetComponentInChildren<MissionLogView>(true);
            var archive = log.GetComponent<MissionArchiveView>() ?? Undo.AddComponent<MissionArchiveView>(log.gameObject);
            archive.log = log; archive.lily = Sprite("LilyDossier"); archive.emma = Sprite("EmmaCard"); archive.coordinatePaper = Sprite("CoordinatePaper");
            archive.abyssMap = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>("Assets/_Project/Content/Maps/Zone04.asset");
            var root = log.transform.Find("ContentRoot") ?? log.transform;
            if (archive.reader == null)
            {
                Button(root, "OpenMissionArchive", "HỒ SƠ THÁM HIỂM", 870, 8, 340, 48, archive.Open);
                var reader = Rect(root, "ArtistMissionArchive", 10, 62, 1220, 610);
                var panel = reader.gameObject.AddComponent<Image>(); panel.sprite = Sprite("MenuButton"); panel.type = Image.Type.Sliced;
                panel.color = new Color(.89f,.94f,.94f); panel.raycastTarget = true;
                archive.reader = reader.gameObject;
                archive.heading = Label(reader,"DocumentHeading","HỒ SƠ LILY",35,22,1040,50,30,true);
                archive.document = Picture(reader,"ArtistDocument",archive.lily,35,85,715,430);
                archive.transcription = Label(reader,"Transcription","",790,92,390,420,25,false);
                archive.paperA = Label(reader,"PaperCoordinateA","",95,210,290,170,24,true);
                archive.paperB = Label(reader,"PaperCoordinateB","",425,210,290,170,24,true);
                archive.paperA.gameObject.SetActive(false); archive.paperB.gameObject.SetActive(false);
                Button(reader,"PreviousDocument","TRƯỚC",35,535,210,52,archive.Previous);
                archive.pageLabel = Label(reader,"DocumentCounter","1 / 3",265,542,120,40,26,false);
                Button(reader,"NextDocument","SAU",405,535,210,52,archive.Next);
                Button(reader,"CloseDocuments","NHIỆM VỤ",850,535,320,52,archive.Close);
                reader.gameObject.SetActive(false);
            }
            EditorUtility.SetDirty(archive);
            var readerImage = archive.reader.GetComponent<Image>();
            readerImage.sprite = DossierPaper(cabin.GetComponentInChildren<ComputerDesktopSkin>(true).journalSheet);
            readerImage.type = Image.Type.Sliced; readerImage.preserveAspect = false; readerImage.color = Color.white;
            var oldBorder = archive.reader.transform.Find("ArtistArchiveBorder");
            if (oldBorder != null) Undo.DestroyObjectImmediate(oldBorder.gameObject);
            var fragment = cabin.GetComponentInChildren<ZoneFourFragmentView>(true);
            fragment.paperArtwork = Sprite("CoordinatePaper"); fragment.documentFont = headingFont; EditorUtility.SetDirty(fragment);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); EditorSceneManager.CloseScene(scene,true);
        }
        private static void ConfigureEnding()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Ending/Ending.unity",OpenSceneMode.Additive);
            var canvas = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).First();
            Picture(canvas.transform,"Lily25DaysLogo",Sprite("Lily25DaysLogo"),64,45,252,104);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); EditorSceneManager.CloseScene(scene,true);
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
        private static Sprite DossierPaper(Texture2D source)
        {
            string path=Art+"DossierPaper.asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite!=null)return sprite;
            sprite=UnityEngine.Sprite.Create(source,new UnityEngine.Rect(1336,622,307,290),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(42,42,42,42));
            sprite.name="DossierPaper";AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var existing = parent.Find(name) as RectTransform;
            var rect = existing != null ? existing : (RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            if (existing == null) { Undo.RegisterCreatedObjectUndo(rect.gameObject,"Author artist UI"); rect.SetParent(parent,false); }
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1); rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(w,h); return rect;
        }
        private static Image Picture(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
        {
            var rect = Rect(parent,name,x,y,w,h); var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = Color.white; image.preserveAspect = true; image.raycastTarget = false; return image;
        }
        private static TMP_Text Label(Transform parent,string name,string text,float x,float y,float w,float h,float size,bool heading)
        {
            var rect = Rect(parent,name,x,y,w,h); var label = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = heading ? headingFont : bodyFont; label.text = text; label.fontSize = size; label.color = Ink;
            label.richText = false; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow; return label;
        }
        private static void Button(Transform parent,string name,string text,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var image = Picture(parent,name,Sprite("MenuButton"),x,y,w,h); image.type = Image.Type.Sliced; image.preserveAspect = false;
            image.color = new Color(.76f,.89f,.91f); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            UnityEventTools.AddPersistentListener(button.onClick,action);
            var label = Label(button.transform,"Label",text,8,5,w-16,h-10,24,false); label.alignment = TextAlignmentOptions.Center;
        }
    }
}

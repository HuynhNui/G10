using System;
using System.Linq;
using G10.Prototype.Computer;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    public static class ComputerDesktopSkinEditor
    {
        private const string Root = "Assets/_Project/Art/UI/Desktop/";
        private const string Desktop = Root + "Pelagic_Desktop_UI_Assets_No_Text/";
        private const string Kit = Root + "Underwater_UI_Kit_Assets/";

        [MenuItem("G10/Computer/Install Watercolor Desktop")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            if (!AssetDatabase.FindAssets("t:TMP_Settings").Any())
            {
                AssetDatabase.importPackageCompleted -= Imported;
                AssetDatabase.importPackageCompleted += Imported;
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                return;
            }
            var screen = UnityEngine.Object.FindAnyObjectByType<ComputerScreenController>(FindObjectsInactive.Include);
            if (screen == null) throw new InvalidOperationException("Open the existing Zone01 scene before installing the desktop.");
            var skin = screen.GetComponent<ComputerDesktopSkin>();
            if (skin == null) skin = Undo.AddComponent<ComputerDesktopSkin>(screen.gameObject);
            Undo.RecordObject(skin, "Assign watercolor desktop artwork");
            skin.wallpaper = Texture(Desktop + "01_Desktop_Background.png");
            skin.taskbar = Texture(Desktop + "08_Taskbar_No_Text.png");
            skin.windowFrame = Texture(Kit + "01_Window_Frame_Active.png");
            skin.buttonKit = Texture(Kit + "03_Button_Control_Kit.png");
            skin.warningDialog = Texture(Kit + "05_Warning_Dialog.png");
            skin.journalSheet = Texture(Kit + "06_Journal_UI_Asset_Sheet.png");
            string[] icons = { "02_Icon_Photo_Lab_No_Text", "03_Icon_Ship_Status_No_Text", "04_Icon_Mission_Log_No_Text", "05_Icon_Journal_No_Text", "06_Icon_Rest_No_Text", "07_Icon_Exit_No_Text" };
            skin.shortcutIcons = icons.Select(name => Texture(Desktop + name + ".png")).ToArray();
            skin.font = TMP_Settings.defaultFontAsset;
            if (skin.font == null) throw new InvalidOperationException("TMP Essentials must finish importing first.");
            // Use the installed TMP font, including Vietnamese glyphs from its bundled source font.
            if (skin.font.sourceFontFile != null)
            {
                skin.font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                skin.font.isMultiAtlasTexturesEnabled = true;
                EditorUtility.SetDirty(skin.font);
            }
            EditorUtility.SetDirty(skin);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
            EditorSceneManager.SaveScene(screen.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = screen.gameObject;
            Debug.Log("Watercolor desktop assigned to the existing Central Computer. Existing callbacks and gameplay state are preserved.");
        }
        private static void Imported(string package)
        {
            if (package != "TMP Essential Resources") return;
            AssetDatabase.importPackageCompleted -= Imported;
            EditorApplication.delayCall += Install;
        }
        private static Texture2D Texture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Missing desktop artwork: " + path);
            // Preserve source detail and authored transparency; no resizing or nearest-neighbour sampling.
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using G10.Prototype.Missions;
using UnityEditor;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>Repeatable editor-only import/binding. The catalog remains the runtime authority.</summary>
    public static class CreatureArtworkImportTools
    {
        public const string ArtworkRoot = "Assets/_Project/Art/Creatures/Finished";
        public const string PaintingRoot = "Assets/_Project/Art/SurveyPaintings";

        public static string BindPaintingPrototype() => BindPainting("Z2_Creature_01");
        public static string BindAllPaintings() => string.Join("\n", Definitions().Where(d => d.id.Contains("_Creature_")).OrderBy(d => d.id).Select(d => BindPainting(d.id)));
        public static string BindPainting(string id)
        {
            RequireEditMode();
            var matches = Definitions().Where(d => d.id == id).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected one catalog definition for " + id);
            string path = PaintingRoot + "/" + id + ".png";
            if (!File.Exists(path)) throw new FileNotFoundException("Export the complete painting first.", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.isReadable = true; // Native pixels are copied once at shutter time and verified against source.
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Undo.RecordObject(matches[0], "Bind complete survey painting");
            matches[0].photoArtwork = texture;
            EditorUtility.SetDirty(matches[0]); AssetDatabase.SaveAssetIfDirty(matches[0]);
            return id + " PHOTO -> " + path + " (" + texture.width + "x" + texture.height + "), capture sprite unchanged";
        }

        public static string BindPrototype() => Bind("Z2_Creature_01");

        public static string BindAll()
        {
            RequireEditMode();
            string result = string.Join("\n", Definitions().Where(d => d.id.Contains("_Creature_"))
                .OrderBy(d => d.id).Select(d => Bind(d.id)));
            // Prefer the authored collected material when present; encounter art remains the creature.
            foreach (var guid in AssetDatabase.FindAssets("t:UpgradeMaterialDefinition", new[] { "Assets/_Project/Data/Upgrade" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<G10.Prototype.Computer.UpgradeMaterialDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var definition = Definitions().FirstOrDefault(d => d.id == material.MaterialId && d.id.Contains("_Creature_"));
                if (definition == null) continue;
                var serialized = new SerializedObject(material);
                serialized.FindProperty("icon").objectReferenceValue = definition.collectedMaterial != null ? definition.collectedMaterial : definition.sprite;
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(material);
                result += "\nmaterial " + material.MaterialId + " -> catalog sprite";
            }
            foreach (string path in Directory.GetFiles(ArtworkRoot + "/Reserve", "*.png")) ConfigureTexture(path.Replace('\\', '/'));
            return result;
        }

        public static string Bind(string id)
        {
            RequireEditMode();
            var matches = Definitions().Where(d => d.id == id).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected one catalog definition for " + id);
            var definition = matches[0];
            string path = ArtworkRoot + "/" + id + ".png";
            if (!File.Exists(path)) throw new FileNotFoundException("Export the native artwork first.", path);
            ConfigureTexture(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (texture == null || sprite == null) throw new InvalidOperationException("Missing native texture/sprite: " + path);
            Undo.RecordObject(definition, "Bind finished creature artwork");
            // Capture this exactly once. Null means the existing profile's distance-specific mask.
            if (!definition.preservePhotoCoverage) definition.photoCoverageMask = definition.Image;
            definition.preservePhotoCoverage = true;
            definition.image = texture;
            definition.sprite = sprite;
            definition.placeholderArt = false;
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssetIfDirty(definition);
            return id + " -> " + path + " (" + texture.width + "x" + texture.height + "), mask=" +
                (definition.photoCoverageMask != null ? AssetDatabase.GetAssetPath(definition.photoCoverageMask) : "original profile fallback");
        }

        private static void ConfigureTexture(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        public static string Audit()
        {
            return string.Join("\n", Definitions().Where(d => d.id.Contains("_Creature_")).OrderBy(d => d.id).Select(d =>
                d.id + " | " + AssetDatabase.GetAssetPath(d.Image) + " | preserved=" + d.preservePhotoCoverage +
                " | mask=" + AssetDatabase.GetAssetPath(d.photoCoverageMask) + " | placeholder=" + d.placeholderArt));
        }

        private static SurveyContentDefinition[] Definitions() => AssetDatabase.FindAssets("t:SurveyContentDefinition", new[] { "Assets/_Project/Content" })
            .Select(g => AssetDatabase.LoadAssetAtPath<SurveyContentDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(d => d != null).ToArray();

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Bind artwork only in Edit Mode.");
        }
    }
}

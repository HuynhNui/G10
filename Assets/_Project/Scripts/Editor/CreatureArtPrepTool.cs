using System;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    public static class CreatureArtPrepTool
    {
        public const string Folder = "Assets/_Project/Art/Sprites/Creature/Zone1/Processed";
        [MenuItem("G10/Zone 1/Install Clean Creature 001")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) return;
            Directory.CreateDirectory(Folder);
            if (!File.Exists(Folder + "/Creature001_Clean.png"))
                Process("AssetInbox/Creature001_Extraction.png");
            AssetDatabase.Refresh();
            Texture2D clean = Import("Creature001_Clean.png"), silhouette = Import("Creature001_Silhouette.png");
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null) throw new InvalidOperationException("Open Zone01.");
            var capture = cabin.GetComponent<PhotoCaptureService>();
            Undo.RecordObject(capture.profile, "Assign clean creature");
            capture.profile.creature = clean; capture.profile.closeCreature = clean; capture.profile.silhouette = silhouette;
            EditorUtility.SetDirty(capture.profile);
            // Keep the separately authored caught-item icon; this sprite depicts the living organism.
        }
        public static void Process(string extractionPath)
        {
            // The extraction model returned RGB checkerboard, not an alpha channel.
            // Remove its neutral matte, including holes between the colored tendrils.
            // This threshold is specific to this blue/pink subject, not a generic art importer.
            var input = new Texture2D(2,2,TextureFormat.RGBA32,false);
            Texture2D clean = null, silhouette = null;
            try
            {
                if (!input.LoadImage(File.ReadAllBytes(extractionPath))) throw new InvalidOperationException("Invalid extraction PNG.");
                var pixels = input.GetPixels();
                int left=input.width, bottom=input.height, right=0, top=0;
                for (int y=0;y<input.height;y++) for(int x=0;x<input.width;x++)
                {
                    int i=y*input.width+x; var p=pixels[i];
                    float chroma=Mathf.Max(p.r,p.g,p.b)-Mathf.Min(p.r,p.g,p.b);
                    p.a *= Mathf.SmoothStep(0,1,Mathf.InverseLerp(.035f,.16f,chroma)); pixels[i]=p;
                    if(p.a>.05f){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
                }
                if(right<=left||top<=bottom)throw new InvalidOperationException("No colored creature found.");
                const int pad=20; int width=right-left+1+pad*2, height=top-bottom+1+pad*2;
                var output=new Color[width*height];var shadow=new Color[width*height];
                for(int y=bottom;y<=top;y++)for(int x=left;x<=right;x++)
                {
                    int index=(y-bottom+pad)*width+x-left+pad;var p=pixels[y*input.width+x];
                    output[index]=p;shadow[index]=new Color(.055f,.16f,.23f,p.a);
                }
                clean=new Texture2D(width,height,TextureFormat.RGBA32,false);clean.SetPixels(output);clean.Apply();
                silhouette=new Texture2D(width,height,TextureFormat.RGBA32,false);silhouette.SetPixels(shadow);silhouette.Apply();
                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(Folder+"/Creature001_Clean.png",clean.EncodeToPNG());
                File.WriteAllBytes(Folder+"/Creature001_Silhouette.png",silhouette.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(input); if(clean!=null)Object.DestroyImmediate(clean);if(silhouette!=null)Object.DestroyImmediate(silhouette); }
        }
        private static Texture2D Import(string file)
        {
            string path=Folder+"/"+file;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default; importer.isReadable=true;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}

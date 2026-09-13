using System;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace G10.Prototype.Editor
{
    public static class PhotoCaptureEditor
    {
        private const string Source="Assets/_Project/Art/Zone1_PhotoKit_Package/Zone1_PhotoKit/";
        private const string Output="Assets/_Project/Art/PhotoCaptureRuntime";
        [MenuItem("G10/Zone 1/Install Photo Capture")]
        public static void Install()
        {
            if(EditorApplication.isPlaying)return;
            var cabin=Object.FindAnyObjectByType<CabinStationView>();
            if(cabin==null||cabin.GetComponent<PhotoSurveyZone>()==null)throw new InvalidOperationException("Zone01 photo survey must be installed first.");
            if(cabin.GetComponent<PhotoCaptureService>()!=null){FitGallery(cabin);Selection.activeGameObject=cabin.gameObject;return;}
            if(!AssetDatabase.IsValidFolder(Output))AssetDatabase.CreateFolder("Assets/_Project/Art","PhotoCaptureRuntime");
            string path=Output+"/Zone01CaptureProfile.asset";
            var profile=AssetDatabase.LoadAssetAtPath<PhotoCaptureProfile>(path);
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<PhotoCaptureProfile>();
                profile.shallow=Copy("Backgrounds/Z1_BG_OpenWater_Shallow_A.png");profile.mid=Copy("Backgrounds/Z1_BG_OpenWater_Mid_A.png");
                profile.deep=Copy("Backgrounds/Z1_BG_OpenWater_Deep_A.png");profile.seabed=Copy("Seabed/Z1_Seabed_DistantFloor_A.png");
                profile.fog=Copy("Overlays/Z1_Overlay_Fog_Mid.png");profile.particles=Copy("Overlays/Z1_Overlay_Particles_Light_A.png");
                profile.creature=Copy("Creatures/Creature01/Z1_Creature_01_Main.png");profile.closeCreature=Copy("Creatures/Creature01/Z1_Creature_01_Close.png");
                profile.silhouette=Copy("Creatures/Creature01/Z1_Creature_01_Silhouette.png");
                profile.kelp=Copy("Landmarks/Z1_Landmark_SingingKelp_Cluster_A.png");profile.rock=Copy("Props/Z1_Prop_SmallRock_A.png");
                AssetDatabase.CreateAsset(profile,path);
            }
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject,"Install photo capture");
            var capture=Undo.AddComponent<PhotoCaptureService>(cabin.gameObject);
            capture.navigation=cabin.GetComponent<ZoneNavigation>();capture.survey=cabin.GetComponent<PhotoSurveyZone>();capture.profile=profile;
            var wiring=new SerializedObject(cabin);
            var panel=((GameObject)wiring.FindProperty("cameraPanel").objectReferenceValue).transform;
            // Retain authored placeholder text in hierarchy for Undo; hide it under the new camera view.
            foreach(var label in panel.GetComponentsInChildren<Text>(true))if(label.name=="Title"||label.name=="Message")label.gameObject.SetActive(false);
            var view=Undo.AddComponent<PhotoCameraView>(panel.gameObject);view.capture=capture;
            Label(panel,"CameraTitle","CAMERA • ZONE 1",330,60,1260,65,36);
            view.preview=Box("PhotoPreview",panel,320,155,1280,720).gameObject.AddComponent<RawImage>();view.preview.color=new(.04f,.09f,.11f);view.preview.raycastTarget=false;
            view.status=Label(panel,"CaptureStatus","",260,885,1400,95,27);
            Button(panel,"TakePhoto","CHỤP",770,985,380,70,view.TakePhoto);
            var lab=cabin.GetComponentInChildren<PhotoLabView>(true);
            var so=new SerializedObject(lab);so.FindProperty("repositorySource").objectReferenceValue=capture;
            var body=(Text)so.FindProperty("body").objectReferenceValue;so.ApplyModifiedProperties();
            var rect=body.rectTransform;rect.anchoredPosition=new(960,-750);rect.sizeDelta=new(1640,125);body.fontSize=28;
            lab.preview=Box("ArchivePreview",lab.transform,480,160,960,540).gameObject.AddComponent<RawImage>();lab.preview.raycastTarget=false;
            Button(lab.transform,"PreviousPhoto","← ẢNH TRƯỚC",510,825,400,65,lab.Previous);
            Button(lab.transform,"NextPhoto","ẢNH SAU →",1010,825,400,65,lab.Next);
            var status=cabin.GetComponentInChildren<ExistingShipStatusProvider>(true);if(status!=null){status.photoCapture=capture;EditorUtility.SetDirty(status);}
            FitGallery(cabin);
            EditorUtility.SetDirty(lab);EditorUtility.SetDirty(view);EditorUtility.SetDirty(capture);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);Selection.activeGameObject=cabin.gameObject;
            Debug.Log("Zone01 capture and Photo Lab installed. Save Zone01. Aim at P01 using the helm heading before taking a photo.");
        }
        private static void FitGallery(CabinStationView cabin)
        {
            var lab=cabin.GetComponentInChildren<PhotoLabView>(true);if(lab==null||lab.preview==null)return;
            Undo.RegisterFullObjectHierarchyUndo(lab.gameObject,"Fit photo gallery");
            Place(lab.preview.rectTransform,590,115,740,416);
            var so=new SerializedObject(lab);var body=(Text)so.FindProperty("body").objectReferenceValue;
            Place(body.rectTransform,160,545,1600,95);body.alignment=TextAnchor.MiddleCenter;body.fontSize=27;
            Place((RectTransform)lab.transform.Find("PreviousPhoto"),550,655,350,60);
            Place((RectTransform)lab.transform.Find("NextPhoto"),1020,655,350,60);
            EditorUtility.SetDirty(body);EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
        }
        private static void Place(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=new(0,1);r.pivot=new(.5f,.5f);r.sizeDelta=new(w,h);r.anchoredPosition=new(x+w/2,-y-h/2);}
        private static Texture2D Copy(string relative)
        {
            string path=Output+"/"+Path.GetFileName(relative);
            if(!File.Exists(path))AssetDatabase.CopyAsset(Source+relative,path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.maxTextureSize=512;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static RectTransform Box(string name,Transform parent,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Create photo UI");go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new(0,1);r.sizeDelta=new(w,h);r.anchoredPosition=new(x+w/2,-y-h/2);return r;
        }
        private static Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size)
        {
            var t=Box(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;
            t.color=new(.75f,1,.87f);t.text=value;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
        }
        private static void Button(Transform parent,string name,string title,float x,float y,float w,float h,UnityAction action)
        {
            var r=Box(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new(.08f,.2f,.23f);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            UnityEventTools.AddPersistentListener(b.onClick,action);Label(r,"Label",title,0,0,w,h,28);
        }
    }
}

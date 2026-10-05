using System;
using G10.Prototype.Computer;
using G10.Prototype.Tutorial;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    public static class TutorialEditor
    {
        public const string ConfigPath="Assets/_Project/Data/Tutorial/ZoneOneTutorialConfig.asset";
        [MenuItem("G10/Tutorial/Install Zone01 Tutorial")]
        public static void Install()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before installing the tutorial.");
            var cabin=UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if(cabin==null||cabin.gameObject.scene.name!="Zone01")throw new InvalidOperationException("Open the existing Zone01 cabin scene first.");
            if(!AssetDatabase.IsValidFolder("Assets/_Project/Data"))AssetDatabase.CreateFolder("Assets/_Project","Data");
            if(!AssetDatabase.IsValidFolder("Assets/_Project/Data/Tutorial"))AssetDatabase.CreateFolder("Assets/_Project/Data","Tutorial");
            var config=AssetDatabase.LoadAssetAtPath<TutorialConfig>(ConfigPath);
            if(config==null){config=ScriptableObject.CreateInstance<TutorialConfig>();AssetDatabase.CreateAsset(config,ConfigPath);}
            if(cabin.GetComponent<TutorialManager>()==null)Undo.AddComponent<TutorialManager>(cabin.gameObject);
            var director=cabin.GetComponent<ZoneOneTutorialDirector>()??Undo.AddComponent<ZoneOneTutorialDirector>(cabin.gameObject);
            if(director.config==null){Undo.RecordObject(director,"Configure Zone01 tutorial");director.config=config;EditorUtility.SetDirty(director);}
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            AssetDatabase.SaveAssetIfDirty(config);
        }
        [MenuItem("G10/Tutorial/Reset Tutorial Progress")]
        public static void ResetProgress()
        {
            if(!EditorUtility.DisplayDialog("Reset tutorial knowledge", "Keep the expedition but clear learned tutorial steps? This is intended for QA.","Reset Tutorial","Cancel"))return;
            if(EditorApplication.isPlaying)
            {
                var loop=UnityEngine.Object.FindAnyObjectByType<ExpeditionLoop>();
                if(loop!=null){if(!loop.ResetTutorialProgress())Debug.LogWarning(loop.LastError??"Tutorial cannot reset during a transition.");return;}
            }
            ExpeditionSaveStore.ResetTutorialProgress();
        }
    }
}

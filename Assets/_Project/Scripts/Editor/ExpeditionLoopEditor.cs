using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    public static class ExpeditionLoopEditor
    {
        [MenuItem("G10/Computer/Install Day Rest Journal")]
        public static void Install()
        {
            if(EditorApplication.isPlaying) return;
            var panels=Object.FindAnyObjectByType<UIManager>();
            if(panels==null) throw new System.InvalidOperationException("Open GameplayCore and Zone01 first.");
            var loop=panels.GetComponent<ExpeditionLoop>();
            if(loop==null) loop=Undo.AddComponent<ExpeditionLoop>(panels.gameObject);
            var known=loop.creatureCatalog.ToList();
            foreach(var catcher in Object.FindObjectsByType<CreatureCatcher>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(catcher.survey!=null && !known.Any(a=>a.id==catcher.survey.creatureId))
                    known.Add(new ExpeditionCreatureAsset {id=catcher.survey.creatureId,icon=catcher.itemIcon});
            loop.creatureCatalog=known.ToArray();
            EditorUtility.SetDirty(loop);EditorSceneManager.MarkSceneDirty(panels.gameObject.scene);
            EditorSceneManager.SaveScene(panels.gameObject.scene);
            Selection.activeGameObject=panels.gameObject;
        }
        public static void InstallBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/GameplayCore.unity");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity",OpenSceneMode.Additive);
            Install();
        }
    }
}

using System;
using G10.Prototype.Computer;
using G10.Prototype.Dialogue;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Editor
{
    /// <summary>Applies this scope revision without rebuilding the authored scenes.</summary>
    public static class ScopeUIIntegrationEditor
    {
        [MenuItem("G10/UI/Install Scope Revision")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the currently edited scene before installing the scope revision.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var core = EnsureScene("GameplayCore");
                DialogueUIEditor.Install();
                EditorSceneManager.SaveScene(core);
                var zone = EnsureScene("Zone01");
                CargoAppEditor.Install();
                WatercolorUIEditor.Apply();
                Validate(core, zone);
                EditorSceneManager.SaveScene(zone);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                // Batch mode starts without a loaded scene; there is no previous setup to restore.
                if (Array.Exists(setup, scene => scene.isLoaded && scene.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
        private static Scene EnsureScene(string name)
        {
            var scene = SceneManager.GetSceneByName(name);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/" + name + ".unity", OpenSceneMode.Additive);
            return scene;
        }
        private static void Validate(params Scene[] scenes)
        {
            foreach (var scene in scenes)
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                            throw new InvalidOperationException("Missing script after migration: " + scene.name + "/" + child.name);
            var view = UnityEngine.Object.FindAnyObjectByType<CreatureInventoryView>(FindObjectsInactive.Include);
            if (view == null || view.slots.Length != 12 || view.detailPreview == null) throw new InvalidOperationException("Cargo references missing.");
            var dialogue = UnityEngine.Object.FindAnyObjectByType<DialogueController>(FindObjectsInactive.Include);
            if (dialogue == null || !dialogue.View.IsConfigured) throw new InvalidOperationException("Dialogue references missing.");
            var screen = UnityEngine.Object.FindAnyObjectByType<ComputerScreenController>(FindObjectsInactive.Include);
            if (Array.Exists(screen.Apps, app => (int)app.id == 7)) throw new InvalidOperationException("Research registration remains.");
        }
    }
}

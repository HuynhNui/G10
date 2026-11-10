using System;
using System.IO;
using System.Linq;
using G10.Prototype.Feedback;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>Bind supplied PSD apparition groups to existing story clocks. No mission/save installers.</summary>
    public static class NativeScareArtworkSetup
    {
        public const string GhostPath = CreatureArtworkImportTools.ArtworkRoot + "/Reserve/Zone02_03_Ghost.png";
        public const string HelmetPath = CreatureArtworkImportTools.ArtworkRoot + "/Reserve/Zone04_HelmetGhost.png";
        public const string HelmetDefinitionPath = "Assets/_Project/FeedbackEffects/Zone04/visual.helmet-reflection.asset";

        [MenuItem("G10/Artwork/Bind Native Story Apparitions")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>(FindObjectsInactive.Include);
            if (cabin == null || cabin.gameObject.scene.path != "Assets/_Project/Scenes/Gameplay/Zone01.unity" || cabin.gameObject.scene.isDirty)
                throw new InvalidOperationException("Open the clean shared Zone01 cabin; preserve any user scene edits first.");
            var director = cabin.GetComponent<EffectDirector>();
            var ghost = AssetDatabase.LoadAssetAtPath<Sprite>(GhostPath);
            var helmet = AssetDatabase.LoadAssetAtPath<Texture2D>(HelmetPath);
            if (ghost == null || helmet == null) throw new FileNotFoundException("Import the original native PSD reserve artwork first.");
            var sequence = cabin.GetComponent<ZoneTwoPresentationController>().reflection;
            int step = 0;
            foreach (var cue in sequence.cues.Where(c => c.effect != null && c.effect.silhouette != null))
            {
                var d = cue.effect; Undo.RecordObject(d, "Bind native Zone02 reflection");
                d.silhouette = ghost; d.preserveSilhouetteColors = true;
                d.silhouetteSize = new Vector2(170, 190);
                d.silhouettePosition = new Vector2(-24 + step * 24, step);
                d.silhouetteOpacity = .78f; step++;
                EditorUtility.SetDirty(d); AssetDatabase.SaveAssetIfDirty(d);
            }
            if (step != 3) throw new InvalidOperationException("Expected the existing three drifting reflection steps.");

            var reflection = director.Surface(EffectTarget.Reflection).visual.procedural;
            Undo.RecordObject(reflection, "Bind native Zone03 monitor reflection");
            reflection.reflectionArtwork = ghost.texture; EditorUtility.SetDirty(reflection);
            var helm = director.Surface(EffectTarget.Helm).visual.procedural;
            Undo.RecordObject(helm, "Bind native Zone04 helmet reflection");
            helm.reflectionArtwork = helmet; EditorUtility.SetDirty(helm);

            var definition = AssetDatabase.LoadAssetAtPath<EffectDefinition>(HelmetDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EffectDefinition>();
                AssetDatabase.CreateAsset(definition, HelmetDefinitionPath);
            }
            Undo.RecordObject(definition, "Configure native final-signal apparition");
            definition.id = "z4.visual.helmet-reflection"; definition.channel = definition.id;
            definition.target = EffectTarget.Helm; definition.priority = 45;
            definition.interference = ScreenInterference.ProceduralReflection;
            definition.interferenceIntensity = .90f; definition.duration = .9f;
            definition.fadeIn = .04f; definition.fadeOut = .18f;
            EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
            var final = cabin.GetComponent<ZoneFourPresentationController>().finalSignal;
            Undo.RecordObject(final, "Attach helmet artwork to existing final signal onset");
            final.cues = final.cues.Where(c => c.effect != definition).Append(new EffectCue {
                effect = definition, offset = 1.2f, duration = .9f, intensity = 1, phase = EffectPhase.Peak }).ToArray();
            EditorUtility.SetDirty(final); AssetDatabase.SaveAssetIfDirty(final);
            var serialized = new SerializedObject(director);
            var definitions = serialized.FindProperty("definitions");
            if (!Enumerable.Range(0, definitions.arraySize).Any(i => definitions.GetArrayElementAtIndex(i).objectReferenceValue == definition))
            {
                int index = definitions.arraySize; definitions.InsertArrayElementAtIndex(index);
                definitions.GetArrayElementAtIndex(index).objectReferenceValue = definition;
                serialized.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            EditorSceneManager.SaveScene(cabin.gameObject.scene);
        }
    }
}

using System;
using G10.Prototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    /// <summary>Restores only the existing helm's map pocket, without rebuilding the cabin.</summary>
    public static class HelmMapPreviewEditor
    {
        [MenuItem("G10/Zone 1/Restore Helm Map Preview")]
        public static void InstallCurrentCabin()
        {
            if (EditorApplication.isPlaying) return;
            var cabin = UnityEngine.Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01")
                throw new InvalidOperationException("Open Zone01 before restoring the helm map preview.");
            Install(cabin);
        }

        public static void Install(CabinStationView cabin)
        {
            if (cabin == null || cabin.NavigationPanel == null) return;
            Transform helm = cabin.NavigationPanel.transform;
            Transform existing = helm.Find("MiniMap");
            GameObject root;
            if (existing == null)
            {
                root = new GameObject("MiniMap", typeof(RectTransform), typeof(UnityEngine.UI.Image),
                    typeof(UnityEngine.UI.Button), typeof(CabinPointerTarget), typeof(HelmMapPreview));
                root.transform.SetParent(helm, false);
                Undo.RegisterCreatedObjectUndo(root, "Restore helm map preview");
            }
            else
            {
                root = existing.gameObject;
                Undo.RegisterFullObjectHierarchyUndo(root, "Configure helm map preview");
            }
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(276, 218);
            rect.anchoredPosition = new Vector2(1063, -811.5f);
            // Follow the hand-painted pocket's slightly rising top/bottom edges.
            // Rotate the backing, map and glass together, including the clickable surface.
            rect.localRotation = Quaternion.Euler(0, 0, 1f);
            rect.localScale = Vector3.one;

            // Opaque backing hides the 3x3 lines painted into the original helm artwork.
            var backing = Ensure<UnityEngine.UI.Image>(root);
            backing.color = new Color(.22f, .40f, .60f, 1);
            backing.raycastTarget = true;
            var button = Ensure<UnityEngine.UI.Button>(root);
            button.targetGraphic = backing;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = UnityEngine.UI.ColorBlock.defaultColorBlock;
            colors.highlightedColor = new Color(.91f, 1, 1, 1);
            colors.pressedColor = new Color(.78f, .9f, 1, 1);
            colors.fadeDuration = .12f;
            button.colors = colors;

            var image = Child<UnityEngine.UI.RawImage>(rect, "MapImage");
            image.color = new Color(.62f, .82f, .91f, 1);
            image.uvRect = new Rect(0, 0, 1, 1);
            image.raycastTarget = false;
            var aspect = Ensure<UnityEngine.UI.AspectRatioFitter>(image.gameObject);
            aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;

            Layer(rect, "BlueGlass", new Color(.18f, .52f, .73f, .18f));
            Layer(rect, "LavenderGlass", new Color(.40f, .33f, .60f, .12f));
            var reflection = Layer(rect, "SoftReflection", new Color(.87f, .97f, 1, .055f));
            reflection.rectTransform.anchorMin = new Vector2(0, .53f);
            var shade = Layer(rect, "BottomShade", new Color(.08f, .17f, .32f, .27f));
            shade.rectTransform.anchorMax = new Vector2(1, 0);
            shade.rectTransform.offsetMax = new Vector2(0, 6);
            var glint = Layer(rect, "TopGlint", new Color(.73f, .95f, 1, .25f));
            glint.rectTransform.anchorMin = new Vector2(0, 1);
            glint.rectTransform.offsetMin = new Vector2(0, -2);

            Ensure<CabinPointerTarget>(root).Configure(cabin, "Mở bản đồ khu vực hiện tại");
            var preview = Ensure<HelmMapPreview>(root);
            preview.Configure(cabin, image, aspect, button);
            EditorUtility.SetDirty(preview);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
        }

        private static T Ensure<T>(GameObject target) where T : Component =>
            target.GetComponent<T>() ?? Undo.AddComponent<T>(target);

        private static T Child<T>(RectTransform parent, string name) where T : Component
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(T));
                go.transform.SetParent(parent, false);
                child = go.transform;
                Undo.RegisterCreatedObjectUndo(go, "Create preview layer");
            }
            var rect = (RectTransform)child;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            child.SetAsLastSibling();
            return Ensure<T>(child.gameObject);
        }

        private static UnityEngine.UI.Image Layer(RectTransform parent, string name, Color color)
        {
            var image = Child<UnityEngine.UI.Image>(parent, name);
            image.color = color; image.raycastTarget = false;
            return image;
        }
    }
}

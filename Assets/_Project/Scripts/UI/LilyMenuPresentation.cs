using System.Collections;
using G10.Prototype.Computer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace G10.Prototype.UI
{
    /// <summary>Skins the existing navigation and developer controls after their normal initialization.</summary>
    public sealed class LilyMenuPresentation : MonoBehaviour
    {
        public TMP_FontAsset bodyFont;
        public Sprite buttonArtwork;
        private GameObject debugControl, resetControl;
        private bool developerControlsVisible;
        private IEnumerator Start()
        {
            yield return null;
            Place("StartButton", 85, 375, "Chơi mới");
            Place("ContinueButton", 85, 497, "Tiếp tục");
            Place("QuitButton", 85, 619, "Thoát");
            var debug = transform.Find("DebugStartButton");
            if (debug != null)
            {
                debugControl = debug.gameObject;
                Place("DebugStartButton", 1530, 960, "Debug", 26, 280, 54);
                debugControl.SetActive(false);
            }
            var reset = transform.Find("TutorialResetQA");
            if (reset != null)
            {
                resetControl = reset.gameObject;
                Position((RectTransform)reset, 1100, 960, 380, 54);
                var feedback = reset.Find("Feedback") as RectTransform;
                if (feedback != null) { feedback.sizeDelta = new Vector2(600, 65); feedback.anchoredPosition = new Vector2(0, 65); }
                resetControl.SetActive(false);
            }
        }
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current?.f8Key.wasPressedThisFrame != true || GetComponent<MainMenuDebugStart>()?.IsOpen == true) return;
            developerControlsVisible = !developerControlsVisible;
            if (debugControl != null) debugControl.SetActive(developerControlsVisible);
            if (resetControl != null) resetControl.SetActive(developerControlsVisible);
#endif
        }
        private void Place(string name, float x, float y, string label, int size = 80, float width = 760, float height = 112)
        {
            var node = transform.Find(name);
            if (node == null) return;
            Position((RectTransform)node, x, y, width, height);
            var image = node.GetComponent<Image>();
            if (image != null) { image.sprite = null; image.type = Image.Type.Simple; image.color = Color.clear; image.raycastTarget = true; }
            var button = node.GetComponent<Button>();
            foreach (var text in node.GetComponentsInChildren<Text>(true))
            {
                text.text = label; text.fontSize = size; text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
                var adapter = text.GetComponent<ComputerDesktopText>();
                if (adapter == null) { adapter = text.gameObject.AddComponent<ComputerDesktopText>(); adapter.Bind(text, bodyFont, new Color(.91f, .92f, .92f)); }
                var rendered = adapter.RenderedText;
                rendered.font = bodyFont; rendered.fontSize = size; rendered.fontStyle = FontStyles.Normal;
                rendered.alignment = TextAlignmentOptions.MidlineLeft; rendered.outlineWidth = 0;
                rendered.rectTransform.offsetMin = rendered.rectTransform.offsetMax = Vector2.zero;
                if (button != null) button.targetGraphic = rendered;
            }
            if (button != null)
            {
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.65f, .94f, 1);
                colors.pressedColor = new Color(.44f, .72f, .85f); colors.disabledColor = new Color(.45f, .49f, .53f); button.colors = colors;
            }
        }
        private static void Position(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }
    }
}

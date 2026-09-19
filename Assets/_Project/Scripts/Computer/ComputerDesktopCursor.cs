using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    /// <summary>Owns the pointer only while it is over the computer's UI.</summary>
    [DisallowMultipleComponent]
    public sealed class ComputerDesktopCursor : MonoBehaviour
    {
        private enum Shape { Arrow, Link, Pressed, Move, Horizontal, Vertical, DiagonalDown, DiagonalUp, Blocked }
        private readonly Texture2D[] textures = new Texture2D[9];
        private readonly Vector2[] hotspots = new Vector2[9];
        private readonly List<RaycastResult> hits = new(32);
        private PointerEventData pointer;
        private EventSystem events;
        private int current = -1;
        private ComputerWindowHandle draggedHandle;
        private bool ready;

        public void Initialize(Texture2D sheet)
        {
            if (ready || sheet == null) return;
            // Authored sheet coordinates use a top-left origin. Keep each crop's aspect ratio.
            Create(sheet, Shape.Arrow, new Rect(90, 100, 200, 260), new Vector2(42, 42));
            Create(sheet, Shape.Link, new Rect(320, 80, 245, 280), new Vector2(53, 60));
            Create(sheet, Shape.Pressed, new Rect(590, 100, 215, 260), new Vector2(50, 42));
            Create(sheet, Shape.Move, new Rect(340, 400, 255, 240), new Vector2(123, 120));
            Create(sheet, Shape.Horizontal, new Rect(605, 440, 250, 150), new Vector2(125, 78));
            Create(sheet, Shape.Vertical, new Rect(890, 395, 165, 245), new Vector2(80, 123));
            Create(sheet, Shape.DiagonalDown, new Rect(1100, 410, 200, 205), new Vector2(100, 102));
            Create(sheet, Shape.DiagonalUp, new Rect(1370, 410, 205, 205), new Vector2(102, 102));
            Create(sheet, Shape.Blocked, new Rect(740, 645, 230, 245), new Vector2(45, 40));
            ready = true;
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (!ready || mouse == null || !Application.isFocused || !Cursor.visible || Cursor.lockState != CursorLockMode.None)
            { ResetCursor(); return; }
            var position = mouse.position.ReadValue();
            if (position.x < 0 || position.y < 0 || position.x >= Screen.width || position.y >= Screen.height)
            { ResetCursor(); return; }
            if (!mouse.leftButton.isPressed) draggedHandle = null;
            if (draggedHandle != null && draggedHandle.isActiveAndEnabled)
            { SetShape(HandleShape(draggedHandle)); return; }

            if (EventSystem.current == null) { ResetCursor(); return; }
            if (events != EventSystem.current)
            { events = EventSystem.current; pointer = new PointerEventData(events); }
            pointer.Reset(); pointer.position = position;
            hits.Clear(); events.RaycastAll(pointer, hits);
            if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(transform))
            { ResetCursor(); return; }

            var target = hits[0].gameObject;
            var handle = target.GetComponentInParent<ComputerWindowHandle>();
            if (handle != null && (handle.move || handle.edges != 0 && !handle.window.Maximized))
            {
                if (mouse.leftButton.wasPressedThisFrame) draggedHandle = handle;
                SetShape(HandleShape(handle)); return;
            }
            var selectable = target.GetComponentInParent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) SetShape(Shape.Blocked);
            else if (selectable != null && selectable.transition != Selectable.Transition.None)
                SetShape(mouse.leftButton.isPressed ? Shape.Pressed : Shape.Link);
            else SetShape(Shape.Arrow);
        }

        private static Shape HandleShape(ComputerWindowHandle handle)
        {
            if (handle.move) return Shape.Move;
            switch (handle.edges)
            {
                case 1: case 2: return Shape.Horizontal;
                case 4: case 8: return Shape.Vertical;
                case 5: case 10: return Shape.DiagonalDown;
                default: return Shape.DiagonalUp;
            }
        }

        private void Create(Texture2D sheet, Shape shape, Rect crop, Vector2 hotspot)
        {
            const float size = 48;
            float scale = size / Mathf.Max(crop.width, crop.height);
            int width = Mathf.RoundToInt(crop.width * scale), height = Mathf.RoundToInt(crop.height * scale);
            var temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(sheet, temporary, new Vector2(crop.width / sheet.width, crop.height / sheet.height),
                    new Vector2(crop.x / sheet.width, (sheet.height - crop.yMax) / sheet.height));
                RenderTexture.active = temporary;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = "Pelagic Cursor " + shape, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false); // Cursor.SetCursor requires readable RGBA32 data.
                textures[(int)shape] = texture;
                hotspots[(int)shape] = new Vector2(hotspot.x * width / crop.width, hotspot.y * height / crop.height);
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temporary); }
        }

        private void SetShape(Shape shape)
        {
            if (current == (int)shape) return;
            current = (int)shape;
            Cursor.SetCursor(textures[current], hotspots[current], CursorMode.Auto);
        }
        private void ResetCursor()
        {
            draggedHandle = null;
            if (current < 0) return;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); current = -1;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetCursor(); }
        private void OnDisable() => ResetCursor();
        private void OnDestroy()
        {
            ResetCursor();
            foreach (var texture in textures) if (texture != null) Destroy(texture);
        }
    }
}

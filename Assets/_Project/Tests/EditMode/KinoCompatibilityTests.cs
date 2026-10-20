using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class KinoCompatibilityTests
    {
        [UnityTest] public IEnumerator AnalogGlitchOnCopied2DRendererChangesCameraTexture()
        {
            var controllerType = FindType("KinoGlitch.AnalogGlitchController");
            if (controllerType == null) { Assert.Ignore("KinoGlitch is intentionally not a production dependency; install 1.0.0 to rerun its isolated evaluation."); yield break; }
            var previous = GraphicsSettings.defaultRenderPipeline;
            var previousQuality = QualitySettings.renderPipeline;
            var previousTarget = RenderTexture.active;
            var objects = new List<Object>(); Camera renderCamera = null;
            try
            {
                var pipeline = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset")); objects.Add(pipeline);
                var renderer = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Renderer2D.asset")); objects.Add(renderer);
                var analog = ScriptableObject.CreateInstance(FindType("KinoGlitch.AnalogGlitchRendererFeature")); objects.Add(analog);
                var features = renderer.GetType().GetProperty("rendererFeatures", BindingFlags.Instance | BindingFlags.Public).GetValue(renderer) as IList;
                features.Add(analog);
                var data = new SerializedObject(pipeline); data.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer; data.ApplyModifiedPropertiesWithoutUndo();
                GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
                var go = new GameObject("Isolated Kino camera", typeof(Camera)); objects.Add(go); var camera = go.GetComponent<Camera>(); renderCamera = camera;
                camera.orthographic = true; camera.orthographicSize = 1; camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                var controller = go.AddComponent(controllerType); var enabled = (Behaviour)controller; enabled.enabled = false;
                var texture = new Texture2D(64, 64); objects.Add(texture); var colors = new Color[4096];
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) colors[y * 64 + x] = (x / 4 + y / 4) % 2 == 0 ? Color.green : Color.red;
                texture.SetPixels(colors); texture.Apply();
                var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * .5f, 32); objects.Add(sprite);
                var spriteObject = new GameObject("Camera test pattern", typeof(SpriteRenderer)); objects.Add(spriteObject);
                spriteObject.GetComponent<SpriteRenderer>().sprite = sprite;
                var material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")); objects.Add(material); spriteObject.GetComponent<SpriteRenderer>().sharedMaterial = material;
                var target = new RenderTexture(128, 128, 24); objects.Add(target); camera.targetTexture = target;
                yield return null; camera.Render();
                var read = new Texture2D(128, 128, TextureFormat.RGB24, false); objects.Add(read); RenderTexture.active = target; read.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); read.Apply(); var baseline = read.GetPixels32();
                controllerType.GetProperty("HorizontalRipple").SetValue(controller, .8f); controllerType.GetProperty("ColorDrift").SetValue(controller, .8f); enabled.enabled = true;
                yield return null; camera.Render(); RenderTexture.active = target; read.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); read.Apply(); var changed = read.GetPixels32();
                int differences = 0; for (int i = 0; i < changed.Length; i++) if (!changed[i].Equals(baseline[i])) differences++;
                System.IO.Directory.CreateDirectory("Logs/HorrorFoundation"); System.IO.File.WriteAllBytes("Logs/HorrorFoundation/kino-2d-probe.png", read.EncodeToPNG());
                TestContext.WriteLine("Changed camera pixels: " + differences + " / " + changed.Length);
                Assert.That(differences, Is.GreaterThan(100), "The copied 2D Renderer must actually execute the analog pass.");
            }
            finally
            {
                if (renderCamera != null) renderCamera.targetTexture = null;
                GraphicsSettings.defaultRenderPipeline = previous; QualitySettings.renderPipeline = previousQuality; RenderTexture.active = previousTarget;
                for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            }
        }
        private static Type FindType(string name)
        { foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) { var type = assembly.GetType(name); if (type != null) return type; } return null; }
    }
}

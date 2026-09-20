using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

public static class UIDebugger
{
    [MenuItem("Tools/Debug UI State")]
    public static void DebugUIState()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== UI DEBUG STATE ===");

        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"EventSystems found: {eventSystems.Length}");
        foreach (var es in eventSystems)
        {
            sb.AppendLine($"- {es.name} (Active: {es.gameObject.activeInHierarchy})");
            var module = es.GetComponent<BaseInputModule>();
            sb.AppendLine($"  Module: {(module != null ? module.GetType().Name : "NONE")}");
        }

        var raycasters = Object.FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"\nGraphicRaycasters found: {raycasters.Length}");
        foreach (var gr in raycasters)
        {
            if (gr.gameObject.activeInHierarchy && gr.enabled)
            {
                sb.AppendLine($"- ACTIVE: {gr.name} on Canvas {gr.GetComponent<Canvas>()?.sortingOrder}");
                // Find active graphics that could block
                var graphics = gr.GetComponentsInChildren<Graphic>(false);
                foreach (var g in graphics)
                {
                    if (g.raycastTarget)
                    {
                        sb.AppendLine($"    -> Blocks Raycasts: {g.name} (Color: {g.color})");
                    }
                }
            }
        }

        if (EventSystem.current == null)
        {
            sb.AppendLine("\nERROR: EventSystem.current is NULL!");
        }
        else
        {
            sb.AppendLine($"\nEventSystem.current is {EventSystem.current.name}");
        }

        sb.AppendLine($"Time.timeScale: {Time.timeScale}");

        Debug.Log(sb.ToString());
    }
}

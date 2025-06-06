#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class VRFitnessSceneViewTools
{
    static VRFitnessSceneViewTools()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        // Draw bridge zone handles
        var bridgeBuilder = Object.FindObjectOfType<MultiPlankBridgeBuilder>();
        if (bridgeBuilder != null)
        {
            DrawBridgeHandles(bridgeBuilder);
        }

        // Draw target ring handles
        var targetRings = Object.FindObjectsOfType<TargetRing>();
        foreach (var ring in targetRings)
        {
            DrawTargetRingHandles(ring);
        }
    }

    static void DrawBridgeHandles(MultiPlankBridgeBuilder builder)
    {
        Handles.color = Color.yellow;
        
        Vector3 center = builder.transform.position;
        Vector3 size = new Vector3(2f, 0.1f, 8f); // Approximate bridge size
        
        Handles.DrawWireCube(center, size);
        
        // Draw player spawn position
        Handles.color = Color.green;
        Vector3 spawnPos = center + new Vector3(0, 1.8f, -5f);
        Handles.DrawWireDisc(spawnPos, Vector3.up, 0.5f);
        Handles.Label(spawnPos + Vector3.up, "Player Spawn");
    }

    static void DrawTargetRingHandles(TargetRing ring)
    {
        Handles.color = Color.red;
        
        Vector3 position = ring.transform.position;
        float radius = ring.transform.lossyScale.x * 0.5f;
        
        Handles.DrawWireDisc(position, ring.transform.forward, radius);
        
        // Draw target zone
        Handles.color = new Color(1, 0, 0, 0.1f);
        Handles.DrawSolidDisc(position, ring.transform.forward, radius);
    }
}
#endif
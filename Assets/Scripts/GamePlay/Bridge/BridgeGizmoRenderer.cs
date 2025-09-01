using UnityEngine;


public class BridgeGizmoRenderer
{
    private readonly BridgeConfiguration config;
    private readonly Transform transform;
    private readonly bool showPlayerSpawn;

    public BridgeGizmoRenderer(BridgeConfiguration config, Transform transform, bool showPlayerSpawn)
    {
        this.config = config;
        this.transform = transform;
        this.showPlayerSpawn = showPlayerSpawn;
    }

    public void DrawBridgeGizmos()
    {
        if (config.createPlatforms)
        {
            DrawPlatforms();
            if (showPlayerSpawn) DrawPlayerSpawn();
        }

        DrawBridgeStructure();
        DrawPlanks();
        DrawAnchors();
    }

    private void DrawPlatforms()
    {
        Gizmos.color = Color.cyan;
        
        float bridgeStartZ = -config.totalBridgeLength * 0.5f;
        float bridgeEndZ = config.totalBridgeLength * 0.5f;
        
        float startPlatformZ = bridgeStartZ - (config.platformLength * 0.5f + config.platformGap);
        var startPlatformPos = transform.position + new Vector3(0, 0, startPlatformZ);
        var platformSize = new Vector3(config.platformWidth, config.platformThickness, config.platformLength);
        Gizmos.DrawWireCube(startPlatformPos, platformSize);
        
        float endPlatformZ = bridgeEndZ + (config.platformLength * 0.5f + config.platformGap);
        var endPlatformPos = transform.position + new Vector3(0, 0, endPlatformZ);
        Gizmos.DrawWireCube(endPlatformPos, platformSize);

        // Draw connection points
        Gizmos.color = Color.yellow;
        var startConnectionPoint = startPlatformPos + new Vector3(0, 0, config.platformLength * 0.5f);
        var endConnectionPoint = endPlatformPos + new Vector3(0, 0, -config.platformLength * 0.5f);
        Gizmos.DrawWireSphere(startConnectionPoint, 0.1f);
        Gizmos.DrawWireSphere(endConnectionPoint, 0.1f);
    }

    private void DrawPlayerSpawn()
    {
        Gizmos.color = Color.magenta;
        float bridgeStartZ = -config.totalBridgeLength * 0.5f;
        float startPlatformZ = bridgeStartZ - (config.platformLength * 0.5f + config.platformGap);
        var startPlatformPos = transform.position + new Vector3(0, 0, startPlatformZ);
        var spawnPos = startPlatformPos + config.playerSpawnOffset;
        Gizmos.DrawWireSphere(spawnPos, 0.3f);
        Gizmos.DrawRay(spawnPos, Vector3.forward * 0.5f);
    }

    private void DrawBridgeStructure()
    {
        Gizmos.color = Color.yellow;
        var bridgeCenter = transform.position;
        var bridgeSize = new Vector3(config.plankWidth, config.plankThickness, config.totalBridgeLength);
        Gizmos.DrawWireCube(bridgeCenter, bridgeSize);
    }

    private void DrawPlanks()
    {
        Gizmos.color = Color.green;
        float startZ = -config.totalBridgeLength * 0.5f + config.PlankLength * 0.5f;
        
        for (int i = 0; i < config.numberOfPlanks; i++)
        {
            float zPos = startZ + i * config.PlankSpacing;
            var plankPos = transform.position + new Vector3(0, 0, zPos);
            var plankSize = new Vector3(config.plankWidth, config.plankThickness, config.PlankLength);
            Gizmos.DrawWireCube(plankPos, plankSize);
        }
    }

    private void DrawAnchors()
    {
        Gizmos.color = Color.red;
        var startAnchorPos = transform.position + new Vector3(0, -0.1f, -config.totalBridgeLength * 0.5f);
        var endAnchorPos = transform.position + new Vector3(0, -0.1f, config.totalBridgeLength * 0.5f);
        Gizmos.DrawWireSphere(startAnchorPos, 0.1f);
        Gizmos.DrawWireSphere(endAnchorPos, 0.1f);
    }
}
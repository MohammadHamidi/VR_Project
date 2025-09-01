using System.Collections.Generic;
using UnityEngine;

// ===== BRIDGE CONSTRUCTION PROCESS (Command Pattern) =====
public class BridgeConstructionProcess
{
    private readonly BridgeConfiguration config;
    private readonly Transform parent;
    private readonly IBridgeComponentFactory<BridgePlank> plankFactory;
    private readonly IBridgeComponentFactory<BridgePlatform> platformFactory;
    private readonly AnchorFactory anchorFactory; // Updated to use specific AnchorFactory
    private readonly IPhysicsConnector physicsConnector;

    public BridgeConstructionProcess(
        BridgeConfiguration config,
        Transform parent,
        IBridgeComponentFactory<BridgePlank> plankFactory,
        IBridgeComponentFactory<BridgePlatform> platformFactory,
        AnchorFactory anchorFactory, // Updated parameter type
        IPhysicsConnector physicsConnector)
    {
        this.config = config;
        this.parent = parent;
        this.plankFactory = plankFactory;
        this.platformFactory = platformFactory;
        this.anchorFactory = anchorFactory;
        this.physicsConnector = physicsConnector;
    }

    public BridgeData Build()
    {
        var bridgeData = new BridgeData();

        if (config.createPlatforms)
            bridgeData.Platforms = CreatePlatforms();

        bridgeData.Anchors = CreateAnchors();
        bridgeData.Planks = CreatePlanks();
        
        ConnectComponents(bridgeData);

        return bridgeData;
    }

    private IBridgeComponent[] CreatePlatforms()
    {
        // Calculate positions to ensure seamless connection with bridge
        float bridgeStartZ = -config.totalBridgeLength * 0.5f;
        float bridgeEndZ = config.totalBridgeLength * 0.5f;
        
        // Position platforms to connect directly with bridge edges
        float startPlatformZ = bridgeStartZ - (config.platformLength * 0.5f + config.platformGap);
        float endPlatformZ = bridgeEndZ + (config.platformLength * 0.5f + config.platformGap);

        var startPlatform = platformFactory.Create("StartPlatform", new Vector3(0, 0, startPlatformZ), config, parent);
        var endPlatform = platformFactory.Create("EndPlatform", new Vector3(0, 0, endPlatformZ), config, parent);

        var platforms = new List<IBridgeComponent>();
        if (startPlatform != null) platforms.Add(startPlatform);
        if (endPlatform != null) platforms.Add(endPlatform);

        return platforms.ToArray();
    }

    private IBridgeComponent[] CreateAnchors()
    {
        var anchors = new List<IBridgeComponent>();
        
        if (config.createPlatforms)
        {
            // Position anchors UNDER the platforms for better aesthetics
            float startPlatformZ = -(config.totalBridgeLength * 0.5f + config.platformGap + config.platformLength * 0.5f);
            float endPlatformZ = config.totalBridgeLength * 0.5f + config.platformGap + config.platformLength * 0.5f;
            
            var startAnchor = anchorFactory.Create("StartAnchor", 
                new Vector3(0, -1f, startPlatformZ), config, parent); // Underground
            var endAnchor = anchorFactory.Create("EndAnchor", 
                new Vector3(0, -1f, endPlatformZ), config, parent); // Underground
            
            if (startAnchor != null) anchors.Add(startAnchor);
            if (endAnchor != null) anchors.Add(endAnchor);
        }
        else
        {
            // Original positioning for bridges without platforms
            float bridgeStartZ = -config.totalBridgeLength * 0.5f;
            float bridgeEndZ = config.totalBridgeLength * 0.5f;
            
            var startAnchor = anchorFactory.Create("StartAnchor", 
                new Vector3(0, -0.5f, bridgeStartZ), config, parent);
            var endAnchor = anchorFactory.Create("EndAnchor", 
                new Vector3(0, -0.5f, bridgeEndZ), config, parent);
                
            if (startAnchor != null) anchors.Add(startAnchor);
            if (endAnchor != null) anchors.Add(endAnchor);
        }

        return anchors.ToArray();
    }

    private IBridgeComponent[] CreatePlanks()
    {
        var planks = new List<IBridgeComponent>();
        float startZ = -config.totalBridgeLength * 0.5f + config.PlankLength * 0.5f;

        for (int i = 0; i < config.numberOfPlanks; i++)
        {
            float zPos = startZ + i * config.PlankSpacing;
            var plank = plankFactory.Create($"Plank_{i:00}", new Vector3(0, 0, zPos), config, parent);
            
            if (plank != null)
            {
                planks.Add(plank);
            }
            else
            {
                Debug.LogError($"Failed to create plank {i}");
            }
        }

        return planks.ToArray();
    }

    private void ConnectComponents(BridgeData bridgeData)
    {
        ConnectPlanks(bridgeData.Planks);
        ConnectPlanksToAnchors(bridgeData.Planks, bridgeData.Anchors);
        
        if (bridgeData.Platforms != null)
        {
            ConnectPlatformsToBridge(bridgeData.Platforms, bridgeData.Planks);
            AddPlatformAnchorSupports(bridgeData.Platforms, bridgeData.Anchors);
        }
    }

    private void ConnectPlanks(IBridgeComponent[] planks)
    {
        for (int i = 0; i < planks.Length - 1; i++)
        {
            physicsConnector.ConnectComponents(planks[i], planks[i + 1], config);
        }
    }

    private void ConnectPlanksToAnchors(IBridgeComponent[] planks, IBridgeComponent[] anchors)
    {
        if (planks.Length > 0 && anchors.Length >= 2)
        {
            physicsConnector.ConnectToAnchor(planks[0], anchors[0], 
                new Vector3(0, 0, -config.PlankLength * 0.5f), config);
            
            physicsConnector.ConnectToAnchor(planks[planks.Length - 1], anchors[1], 
                new Vector3(0, 0, config.PlankLength * 0.5f), config);
        }
    }

    private void ConnectPlatformsToBridge(IBridgeComponent[] platforms, IBridgeComponent[] planks)
    {
        if (platforms.Length >= 2 && planks.Length > 0)
        {
            ConnectPlatformToPlank(platforms[0], planks[0], true);
            ConnectPlatformToPlank(platforms[1], planks[planks.Length - 1], false);
        }
    }

    private void ConnectPlatformToPlank(IBridgeComponent platform, IBridgeComponent plank, bool isStartPlatform)
    {
        var platformRb = platform.GameObject.GetComponent<Rigidbody>();
        var plankRb = plank.GameObject.GetComponent<Rigidbody>();
        
        if (platformRb == null || plankRb == null) return;

        // Create robust connection using FixedJoint for stability
        var fixedJoint = platform.GameObject.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = plankRb;
        
        // Set anchor points for seamless connection
        if (isStartPlatform)
        {
            fixedJoint.anchor = new Vector3(0, 0, config.platformLength * 0.5f);
            fixedJoint.connectedAnchor = new Vector3(0, 0, -config.PlankLength * 0.5f);
        }
        else
        {
            fixedJoint.anchor = new Vector3(0, 0, -config.platformLength * 0.5f);
            fixedJoint.connectedAnchor = new Vector3(0, 0, config.PlankLength * 0.5f);
        }

        // Additional spring joint for flexibility
        var springJoint = platform.GameObject.AddComponent<SpringJoint>();
        springJoint.connectedBody = plankRb;
        springJoint.anchor = fixedJoint.anchor;
        springJoint.connectedAnchor = fixedJoint.connectedAnchor;
        springJoint.spring = config.jointSpring * 5f;
        springJoint.damper = config.jointDamper * 3f;
        springJoint.maxDistance = 0.05f;
        springJoint.minDistance = 0f;
    }

    private void AddPlatformAnchorSupports(IBridgeComponent[] platforms, IBridgeComponent[] anchors)
    {
        if (platforms.Length >= 2 && anchors.Length >= 2)
        {
            // Connect start platform to start anchor for additional stability
            var startPlatformRb = platforms[0].GameObject.GetComponent<Rigidbody>();
            var startAnchorRb = anchors[0].GameObject.GetComponent<Rigidbody>();
            
            if (startPlatformRb != null && startAnchorRb != null)
            {
                var supportJoint = platforms[0].GameObject.AddComponent<SpringJoint>();
                supportJoint.connectedBody = startAnchorRb;
                supportJoint.anchor = new Vector3(0, 0, config.platformLength * 0.4f);
                supportJoint.spring = config.jointSpring * 2f;
                supportJoint.damper = config.jointDamper * 2f;
                supportJoint.maxDistance = 0.2f;
            }

            // Connect end platform to end anchor for additional stability
            var endPlatformRb = platforms[1].GameObject.GetComponent<Rigidbody>();
            var endAnchorRb = anchors[1].GameObject.GetComponent<Rigidbody>();
            
            if (endPlatformRb != null && endAnchorRb != null)
            {
                var supportJoint = platforms[1].GameObject.AddComponent<SpringJoint>();
                supportJoint.connectedBody = endAnchorRb;
                supportJoint.anchor = new Vector3(0, 0, -config.platformLength * 0.4f);
                supportJoint.spring = config.jointSpring * 2f;
                supportJoint.damper = config.jointDamper * 2f;
                supportJoint.maxDistance = 0.2f;
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simplified bridge construction process that directly uses GameObjects instead of interfaces.
/// Eliminates the need for IBridgeComponent and factory interfaces.
/// </summary>
public class SimplifiedBridgeConstruction
{
    private readonly BridgeConfig config;
    private readonly Transform parent;
    private readonly BridgeComponentFactory componentFactory;
    private readonly AnchorType anchorType;

    public SimplifiedBridgeConstruction(
        BridgeConfig config,
        Transform parent,
        BridgeComponentFactory componentFactory,
        AnchorType anchorType)
    {
        this.config = config;
        this.parent = parent;
        this.componentFactory = componentFactory;
        this.anchorType = anchorType;
    }

    public BridgeData Build()
    {
        var bridgeData = new BridgeData();

        if (config.enablePlatforms)
            bridgeData.Platforms = CreatePlatforms();

        bridgeData.Anchors = CreateAnchors();
        bridgeData.Planks = CreatePlanks();

        ConnectComponents(bridgeData);

        return bridgeData;
    }

    private GameObject[] CreatePlatforms()
    {
        // Calculate positions to ensure seamless connection with bridge
        float bridgeStartZ = -config.bridgeLength * 0.5f;
        float bridgeEndZ = config.bridgeLength * 0.5f;

        // Position platforms to connect directly with bridge edges
        float startPlatformZ = bridgeStartZ - (config.platformLength * 0.5f + config.plankGap);
        float endPlatformZ = bridgeEndZ + (config.platformLength * 0.5f + config.plankGap);

        var startPlatform = componentFactory.CreatePlatform("StartPlatform", new Vector3(0, 0, startPlatformZ), config, parent);
        var endPlatform = componentFactory.CreatePlatform("EndPlatform", new Vector3(0, 0, endPlatformZ), config, parent);

        var platforms = new List<GameObject>();
        if (startPlatform != null) platforms.Add(startPlatform);
        if (endPlatform != null) platforms.Add(endPlatform);

        return platforms.ToArray();
    }

    private GameObject[] CreateAnchors()
    {
        var anchors = new List<GameObject>();

        if (config.enablePlatforms)
        {
            // Position anchors UNDER the platforms for better aesthetics
            float startPlatformZ = -(config.bridgeLength * 0.5f + config.plankGap + config.platformLength * 0.5f);
            float endPlatformZ = config.bridgeLength * 0.5f + config.plankGap + config.platformLength * 0.5f;

            var startAnchor = componentFactory.CreateAnchor("StartAnchor",
                new Vector3(0, -1f, startPlatformZ), config, parent, anchorType); // Underground
            var endAnchor = componentFactory.CreateAnchor("EndAnchor",
                new Vector3(0, -1f, endPlatformZ), config, parent, anchorType); // Underground

            if (startAnchor != null) anchors.Add(startAnchor);
            if (endAnchor != null) anchors.Add(endAnchor);
        }
        else
        {
            // Original positioning for bridges without platforms
            float bridgeStartZ = -config.bridgeLength * 0.5f;
            float bridgeEndZ = config.bridgeLength * 0.5f;

            var startAnchor = componentFactory.CreateAnchor("StartAnchor",
                new Vector3(0, -0.5f, bridgeStartZ), config, parent, anchorType);
            var endAnchor = componentFactory.CreateAnchor("EndAnchor",
                new Vector3(0, -0.5f, bridgeEndZ), config, parent, anchorType);

            if (startAnchor != null) anchors.Add(startAnchor);
            if (endAnchor != null) anchors.Add(endAnchor);
        }

        return anchors.ToArray();
    }

    private GameObject[] CreatePlanks()
    {
        var planks = new List<GameObject>();
        float startZ = -config.bridgeLength * 0.5f + config.PlankLength * 0.5f;

        for (int i = 0; i < config.plankCount; i++)
        {
            float zPos = startZ + i * config.PlankSpacing;
            var plank = componentFactory.CreatePlank($"Plank_{i:00}", new Vector3(0, 0, zPos), config, parent);

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

    private void ConnectPlanks(GameObject[] planks)
    {
        for (int i = 0; i < planks.Length - 1; i++)
        {
            ConnectPlankToPlank(planks[i], planks[i + 1], config);
        }
    }

    private void ConnectPlanksToAnchors(GameObject[] planks, GameObject[] anchors)
    {
        if (planks.Length > 0 && anchors.Length >= 2)
        {
            ConnectPlankToAnchor(planks[0], anchors[0],
                new Vector3(0, 0, -config.PlankLength * 0.5f), config);

            ConnectPlankToAnchor(planks[planks.Length - 1], anchors[1],
                new Vector3(0, 0, config.PlankLength * 0.5f), config);
        }
    }

    private void ConnectPlatformsToBridge(GameObject[] platforms, GameObject[] planks)
    {
        if (platforms.Length >= 2 && planks.Length > 0)
        {
            ConnectPlatformToPlank(platforms[0], planks[0], true);
            ConnectPlatformToPlank(platforms[1], planks[planks.Length - 1], false);
        }
    }

    private void AddPlatformAnchorSupports(GameObject[] platforms, GameObject[] anchors)
    {
        if (platforms.Length >= 2 && anchors.Length >= 2)
        {
            // Connect start platform to start anchor for additional stability
            var startPlatformRb = platforms[0].GetComponent<Rigidbody>();
            var startAnchorRb = anchors[0].GetComponent<Rigidbody>();

            if (startPlatformRb != null && startAnchorRb != null)
            {
                var supportJoint = platforms[0].AddComponent<SpringJoint>();
                supportJoint.connectedBody = startAnchorRb;
                supportJoint.anchor = new Vector3(0, 0, config.platformLength * 0.4f);
                supportJoint.spring = config.jointSpring * 2f;
                supportJoint.damper = config.jointDamper * 2f;
                supportJoint.maxDistance = 0.2f;
            }

            // Connect end platform to end anchor for additional stability
            var endPlatformRb = platforms[1].GetComponent<Rigidbody>();
            var endAnchorRb = anchors[1].GetComponent<Rigidbody>();

            if (endPlatformRb != null && endAnchorRb != null)
            {
                var supportJoint = platforms[1].AddComponent<SpringJoint>();
                supportJoint.connectedBody = endAnchorRb;
                supportJoint.anchor = new Vector3(0, 0, -config.platformLength * 0.4f);
                supportJoint.spring = config.jointSpring * 2f;
                supportJoint.damper = config.jointDamper * 2f;
                supportJoint.maxDistance = 0.2f;
            }
        }
    }

    // ===== PHYSICS CONNECTION METHODS =====

    private static void ConnectPlankToPlank(GameObject plankA, GameObject plankB, BridgeConfig config)
    {
        if (plankA == null || plankB == null) return;

        var rbA = plankA.GetComponent<Rigidbody>();
        var rbB = plankB.GetComponent<Rigidbody>();

        if (rbA == null || rbB == null) return;

        var joint = plankA.AddComponent<HingeJoint>();
        joint.connectedBody = rbB;
        joint.anchor = new Vector3(0, 0, config.PlankLength * 0.5f);
        joint.connectedAnchor = new Vector3(0, 0, -config.PlankLength * 0.5f);
        joint.axis = Vector3.right;

        joint.useSpring = true;
        var spring = new JointSpring
        {
            spring = config.jointSpring,
            damper = config.jointDamper,
            targetPosition = 0f
        };
        joint.spring = spring;

        joint.useLimits = true;
        joint.limits = new JointLimits { min = -5f, max = 5f };
    }

    private static void ConnectPlankToAnchor(GameObject plank, GameObject anchor, Vector3 anchorPoint, BridgeConfig config)
    {
        if (plank == null || anchor == null) return;

        var plankRb = plank.GetComponent<Rigidbody>();
        var anchorRb = anchor.GetComponent<Rigidbody>();

        if (plankRb == null || anchorRb == null) return;

        var joint = plank.AddComponent<SpringJoint>();
        joint.connectedBody = anchorRb;
        joint.anchor = anchorPoint;
        joint.spring = config.jointSpring * 2f;
        joint.damper = config.jointDamper;
        joint.maxDistance = 0.1f;
    }

    private static void ConnectPlatformToPlank(GameObject platform, GameObject plank, bool isStartPlatform)
    {
        var platformRb = platform.GetComponent<Rigidbody>();
        var plankRb = plank.GetComponent<Rigidbody>();

        if (platformRb == null || plankRb == null) return;

        // Create robust connection using FixedJoint for stability
        var fixedJoint = platform.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = plankRb;

        // Set anchor points for seamless connection
        if (isStartPlatform)
        {
            fixedJoint.anchor = new Vector3(0, 0, 0.5f);
            fixedJoint.connectedAnchor = new Vector3(0, 0, -0.5f);
        }
        else
        {
            fixedJoint.anchor = new Vector3(0, 0, -0.5f);
            fixedJoint.connectedAnchor = new Vector3(0, 0, 0.5f);
        }

        // Additional spring joint for flexibility
        var springJoint = platform.AddComponent<SpringJoint>();
        springJoint.connectedBody = plankRb;
        springJoint.anchor = fixedJoint.anchor;
        springJoint.connectedAnchor = fixedJoint.connectedAnchor;
        springJoint.spring = 1500f; // Simplified spring value
        springJoint.damper = 50f;   // Simplified damper value
        springJoint.maxDistance = 0.05f;
        springJoint.minDistance = 0f;
    }
}

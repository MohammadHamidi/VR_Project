using System.Collections.Generic;
using UnityEngine;
using GamePlay.Bridge;
using Unity.XR.CoreUtils;


public class SOLIDBridgeBuilder : MonoBehaviour
{
    [SerializeField] private BridgeConfiguration config;
    [SerializeField] private AnchorFactory.AnchorType anchorType = AnchorFactory.AnchorType.Invisible; // Anchor type selector
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showRopeConnections = true;
    [SerializeField] private bool showPlayerSpawn = true;
    [SerializeField] private bool showAnchorGizmos = false; // Option to show/hide anchor gizmos
    [SerializeField] private XROrigin player;

    // Dependencies with updated anchor factory
    private readonly IBridgeComponentFactory<BridgePlank> plankFactory = new PlankFactory();
    private readonly IBridgeComponentFactory<BridgePlatform> platformFactory = new PlatformFactory();
    private AnchorFactory anchorFactory; // Will be initialized based on selected type
    private readonly IPlayerPositioner playerPositioner = new UnityPlayerPositioner();
    private readonly IPhysicsConnector physicsConnector = new BridgePhysicsConnector();
    private readonly IBridgeValidator validator = new BridgeValidator();

    private BridgeData currentBridge = new BridgeData();
    private bool bridgeBuilt = false;

    void Awake()
    {
        // Initialize anchor factory with selected type
        anchorFactory = new AnchorFactory(anchorType);
    }

    private void OnEnable() 
    {
        if (Application.isPlaying)
        {
            BridgeStageEventHandler.onPlayerFall += TeleportPlayerToStart;
        }
    }
    
    private void OnDestroy() 
    {
        if (Application.isPlaying)
        {
            BridgeStageEventHandler.onPlayerFall -= TeleportPlayerToStart;
        }
    }

    void Start()
    {
        if (Application.isPlaying && !bridgeBuilt)
        {
            BuildBridge();
        }
    }

    [ContextMenu("Build Bridge")]
    public void BuildBridge()
    {
        if (bridgeBuilt && Application.isPlaying)
        {
            Debug.Log("Bridge already built. Use 'Rebuild Bridge' if needed.");
            return;
        }

        ClearExistingBridge();
        
        // Ensure anchor factory is initialized with current type
        anchorFactory = new AnchorFactory(anchorType);
        
        var bridgeBuilder = new BridgeConstructionProcess(
            config, transform, plankFactory, platformFactory, 
            anchorFactory, physicsConnector);
            
        currentBridge = bridgeBuilder.Build();

        var validationResult = validator.Validate(currentBridge);
        if (validationResult.IsValid)
        {
            AddBridgeComponents();
            if (config.autoPositionPlayer) TeleportPlayerToStart();
            LogBuildSuccess();
            bridgeBuilt = true;
        }
        else
        {
            LogValidationErrors(validationResult);
        }
    }

    [ContextMenu("Rebuild Bridge")]
    public void RebuildBridge()
    {
        bridgeBuilt = false;
        BuildBridge();
    }

    [ContextMenu("Change Anchor Type to Standard")]
    public void ChangeToStandardAnchors()
    {
        anchorType = AnchorFactory.AnchorType.Standard;
        RebuildBridge();
    }

    [ContextMenu("Change Anchor Type to Improved")]
    public void ChangeToImprovedAnchors()
    {
        anchorType = AnchorFactory.AnchorType.Improved;
        RebuildBridge();
    }

    [ContextMenu("Change Anchor Type to Invisible")]
    public void ChangeToInvisibleAnchors()
    {
        anchorType = AnchorFactory.AnchorType.Invisible;
        RebuildBridge();
    }

    [ContextMenu("Hide Anchors")]
    public void HideAnchors()
    {
        if (currentBridge.Anchors != null)
        {
            foreach (var anchor in currentBridge.Anchors)
            {
                if (anchor?.GameObject != null)
                {
                    var renderer = anchor.GameObject.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }
        Debug.Log("Anchors hidden!");
    }

    [ContextMenu("Show Anchors")]
    public void ShowAnchors()
    {
        if (currentBridge.Anchors != null)
        {
            foreach (var anchor in currentBridge.Anchors)
            {
                if (anchor?.GameObject != null)
                {
                    var renderer = anchor.GameObject.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }
            }
        }
        Debug.Log("Anchors shown!");
    }

    [ContextMenu("Remove Anchor Visuals")]
    public void RemoveAnchorVisuals()
    {
        if (currentBridge.Anchors != null)
        {
            foreach (var anchor in currentBridge.Anchors)
            {
                if (anchor?.GameObject != null)
                {
                    var visual = anchor.GameObject.transform.Find("Visual");
                    if (visual != null)
                    {
                        if (Application.isPlaying)
                            Destroy(visual.gameObject);
                        else
                            DestroyImmediate(visual.gameObject);
                    }
                }
            }
        }
        Debug.Log("Anchor visuals removed!");
    }

    [ContextMenu("Fix Bridge Connections")]
    public void FixBridgeConnections()
    {
        if (!bridgeBuilt || currentBridge.Planks == null || currentBridge.Platforms == null)
        {
            Debug.LogWarning("Bridge must be built before fixing connections.");
            return;
        }

        // Remove existing joints on platforms
        foreach (var platform in currentBridge.Platforms)
        {
            if (platform?.GameObject != null)
            {
                var joints = platform.GameObject.GetComponents<Joint>();
                foreach (var joint in joints)
                {
                    if (Application.isPlaying)
                        Destroy(joint);
                    else
                        DestroyImmediate(joint);
                }
            }
        }

        // Reposition platforms for better alignment
        if (currentBridge.Platforms.Length >= 2 && currentBridge.Planks.Length > 0)
        {
            var startPlatform = currentBridge.Platforms[0];
            var firstPlank = currentBridge.Planks[0];
            float targetStartZ = firstPlank.Position.z - (config.PlankLength * 0.5f + config.platformGap + config.platformLength * 0.5f);
            startPlatform.GameObject.transform.position = new Vector3(0, 0, targetStartZ);

            var endPlatform = currentBridge.Platforms[1];
            var lastPlank = currentBridge.Planks[currentBridge.Planks.Length - 1];
            float targetEndZ = lastPlank.Position.z + (config.PlankLength * 0.5f + config.platformGap + config.platformLength * 0.5f);
            endPlatform.GameObject.transform.position = new Vector3(0, 0, targetEndZ);
        }

        // Reconnect with improved connection system
        ConnectPlatformsToBridgeManual(currentBridge.Platforms, currentBridge.Planks);
        AddPlatformAnchorSupportsManual(currentBridge.Platforms, currentBridge.Anchors);
        
        Debug.Log("Bridge connections fixed!");
    }

    private void ConnectPlatformsToBridgeManual(IBridgeComponent[] platforms, IBridgeComponent[] planks)
    {
        if (platforms.Length >= 2 && planks.Length > 0)
        {
            ConnectPlatformToPlankManual(platforms[0], planks[0], true);
            ConnectPlatformToPlankManual(platforms[1], planks[planks.Length - 1], false);
        }
    }

    private void ConnectPlatformToPlankManual(IBridgeComponent platform, IBridgeComponent plank, bool isStartPlatform)
    {
        var platformRb = platform.GameObject.GetComponent<Rigidbody>();
        var plankRb = plank.GameObject.GetComponent<Rigidbody>();
        
        if (platformRb == null || plankRb == null) return;

        var fixedJoint = platform.GameObject.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = plankRb;
        
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

        var springJoint = platform.GameObject.AddComponent<SpringJoint>();
        springJoint.connectedBody = plankRb;
        springJoint.anchor = fixedJoint.anchor;
        springJoint.connectedAnchor = fixedJoint.connectedAnchor;
        springJoint.spring = config.jointSpring * 5f;
        springJoint.damper = config.jointDamper * 3f;
        springJoint.maxDistance = 0.05f;
        springJoint.minDistance = 0f;
    }

    private void AddPlatformAnchorSupportsManual(IBridgeComponent[] platforms, IBridgeComponent[] anchors)
    {
        if (platforms.Length >= 2 && anchors.Length >= 2)
        {
            // Start platform to start anchor
            var supportJoint1 = platforms[0].GameObject.AddComponent<SpringJoint>();
            supportJoint1.connectedBody = anchors[0].GameObject.GetComponent<Rigidbody>();
            supportJoint1.anchor = new Vector3(0, 0, config.platformLength * 0.4f);
            supportJoint1.spring = config.jointSpring * 2f;
            supportJoint1.damper = config.jointDamper * 2f;
            supportJoint1.maxDistance = 0.2f;

            // End platform to end anchor
            var supportJoint2 = platforms[1].GameObject.AddComponent<SpringJoint>();
            supportJoint2.connectedBody = anchors[1].GameObject.GetComponent<Rigidbody>();
            supportJoint2.anchor = new Vector3(0, 0, -config.platformLength * 0.4f);
            supportJoint2.spring = config.jointSpring * 2f;
            supportJoint2.damper = config.jointDamper * 2f;
            supportJoint2.maxDistance = 0.2f;
        }
    }

    private void ClearExistingBridge()
    {
        bridgeBuilt = false;
        
        var children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            children[i] = transform.GetChild(i);

        foreach (var child in children)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void AddBridgeComponents()
    {
        // Add balance checker to middle plank
        if (currentBridge.Planks?.Length > 0)
        {
            int middlePlank = currentBridge.Planks.Length / 2;
            var balanceChecker = currentBridge.Planks[middlePlank].GameObject.GetComponent<BalanceChecker>();
            if (balanceChecker == null)
            {
                currentBridge.Planks[middlePlank].GameObject.AddComponent<BalanceChecker>();
            }
        }

        CreateBridgeGoal();
    }

    private void CreateBridgeGoal()
    {
        GameObject goalParent = currentBridge.Platforms?.Length > 1 ? 
            currentBridge.Platforms[1]?.GameObject : transform.gameObject;
        
        Vector3 goalPosition = currentBridge.Platforms?.Length > 1 ? 
            new Vector3(0, config.platformThickness * 0.5f, 0) :
            new Vector3(0, 0, config.totalBridgeLength * 0.5f + 0.5f);

        var goal = new GameObject("BridgeGoal");
        goal.transform.SetParent(goalParent.transform, false);
        goal.transform.localPosition = goalPosition;

        var trigger = goal.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = currentBridge.Platforms?.Length > 1 ?
            new Vector3(config.platformWidth * 0.8f, 2f, config.platformLength * 0.8f) :
            new Vector3(config.plankWidth + 0.5f, 2f, 1f);

        goal.AddComponent<BridgeGoal>();
    }

    public void TeleportPlayerToStart()
    {
        if (currentBridge.Platforms?.Length > 0)
        {
            Vector3 targetPosition = currentBridge.Platforms[0].Position;
            
            bool positioned = false;
            if (player != null)
            {
                Vector3 finalPosition = targetPosition + config.playerSpawnOffset;
                player.transform.position = finalPosition;
                player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                Debug.Log($"Player positioned using direct reference at {finalPosition}");
                positioned = true;
            }
            
            if (!positioned)
            {
                positioned = playerPositioner.PositionPlayer(targetPosition, config.playerSpawnOffset);
            }
            
            if (!positioned)
            {
                Debug.LogWarning("Could not position player. Please ensure XROrigin is assigned or present in scene.");
            }
        }
        else
        {
            Debug.LogWarning("No start platform found for player positioning.");
        }
    }

    private void LogBuildSuccess()
    {
        string platformInfo = config.createPlatforms ? " with start/end platforms" : "";
        string ropeInfo = (config.useRopes && currentBridge.Ropes?.Length > 0) ? " and rope supports" : "";
        string anchorInfo = $" using {anchorType} anchors";
        Debug.Log($"Bridge validation passed! Created {currentBridge.Planks?.Length ?? 0} planks{platformInfo}{ropeInfo}{anchorInfo}");
    }

    private void LogValidationErrors(ValidationResult result)
    {
        foreach (var error in result.Errors)
            Debug.LogError($"Bridge validation error: {error}");
        
        foreach (var warning in result.Warnings)
            Debug.LogWarning($"Bridge validation warning: {warning}");
    }

    public void SetBridgeConfiguration(int planks, float length, float width)
    {
        config.numberOfPlanks = planks;
        config.totalBridgeLength = length;
        config.plankWidth = width;
        bridgeBuilt = false;
        if (Application.isPlaying) BuildBridge();
    }

    /// <summary>
    /// Sets the anchor type and rebuilds the bridge
    /// </summary>
    public void SetAnchorType(AnchorFactory.AnchorType newAnchorType)
    {
        anchorType = newAnchorType;
        anchorFactory = new AnchorFactory(anchorType);
        bridgeBuilt = false;
        if (Application.isPlaying) BuildBridge();
    }

    /// <summary>
    /// Gets the current bridge configuration
    /// </summary>
    public BridgeConfiguration GetBridgeConfiguration()
    {
        return config;
    }

    /// <summary>
    /// Sets the bridge configuration
    /// </summary>
    public void SetBridgeConfiguration(BridgeConfiguration newConfig)
    {
        config = newConfig;
        bridgeBuilt = false;
        if (Application.isPlaying) BuildBridge();
    }

    /// <summary>
    /// Gets the current anchor type
    /// </summary>
    public AnchorFactory.AnchorType GetAnchorType()
    {
        return anchorType;
    }

    public IBridgeComponent[] GetPlanks() => currentBridge.Planks ?? new IBridgeComponent[0];
    public IBridgeComponent GetStartPlatform() => currentBridge.Platforms?.Length > 0 ? currentBridge.Platforms[0] : null;
    public IBridgeComponent GetEndPlatform() => currentBridge.Platforms?.Length > 1 ? currentBridge.Platforms[1] : null;

    private void OnDrawGizmos()
    {
        if (!showGizmos || config == null) return;
        
        var gizmoRenderer = new BridgeGizmoRenderer(config, transform, showPlayerSpawn);
        gizmoRenderer.DrawBridgeGizmos();
        
        // Draw anchor gizmos if enabled
        if (showAnchorGizmos && currentBridge.Anchors != null)
        {
            Gizmos.color = Color.red;
            foreach (var anchor in currentBridge.Anchors)
            {
                if (anchor?.GameObject != null)
                {
                    Gizmos.DrawWireSphere(anchor.Position, 0.2f);
                }
            }
        }
    }

    void OnValidate()
    {
        if (config != null)
        {
            config.numberOfPlanks = Mathf.Clamp(config.numberOfPlanks, 1, 20);
            config.totalBridgeLength = Mathf.Clamp(config.totalBridgeLength, 1f, 50f);
            config.plankWidth = Mathf.Clamp(config.plankWidth, 0.1f, 2f);
            config.plankThickness = Mathf.Clamp(config.plankThickness, 0.01f, 0.5f);
            config.plankGap = Mathf.Clamp(config.plankGap, 0f, 1f);
        }
        
        // Reinitialize anchor factory if type changed
        if (anchorFactory == null || Application.isPlaying)
        {
            anchorFactory = new AnchorFactory(anchorType);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;

public enum AnchorType { Standard, Improved, Invisible }

public class SOLIDBridgeBuilder : MonoBehaviour
{
    [SerializeField] private BridgeConfig bridgeConfig;
    [SerializeField] private AnchorType anchorType = AnchorType.Invisible;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showRopeConnections = true;
    [SerializeField] private bool showPlayerSpawn = true;
    [SerializeField] private bool showAnchorGizmos = false;
    [SerializeField] private XROrigin player;

    private readonly BridgeComponentFactory componentFactory = new BridgeComponentFactory();
    [SerializeField] private PlayerManager playerManager;

    private BridgeData currentBridge = new BridgeData();
    private bool bridgeBuilt = false;

    void Awake()
    {
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
            if (playerManager == null)
            {
                GameObject pmObj = new GameObject("PlayerManager");
                playerManager = pmObj.AddComponent<PlayerManager>();
                pmObj.transform.SetParent(transform.parent);
            }
        }

        if (player != null && playerManager != null)
        {
            playerManager.SetXROrigin(player);
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

        var bridgeBuilder = new SimplifiedBridgeConstruction(
            bridgeConfig, transform, componentFactory, anchorType);

        currentBridge = bridgeBuilder.Build();

        if (ValidateBridge(currentBridge))
        {
            AddBridgeComponents();

            if (bridgeConfig.autoPositionPlayer)
                TeleportPlayerToStart(); // will also notify tracker

            LogBuildSuccess();
            bridgeBuilt = true;
        }
        else
        {
            Debug.LogError("Bridge validation failed!");
        }
    }

    [ContextMenu("Rebuild Bridge")]
    public void RebuildBridge()
    {
        bridgeBuilt = false;
        BuildBridge();
    }

    [ContextMenu("Change Anchor Type to Standard")]
    public void ChangeToStandardAnchors() { anchorType = AnchorType.Standard; RebuildBridge(); }

    [ContextMenu("Change Anchor Type to Improved")]
    public void ChangeToImprovedAnchors() { anchorType = AnchorType.Improved; RebuildBridge(); }

    [ContextMenu("Change Anchor Type to Invisible")]
    public void ChangeToInvisibleAnchors() { anchorType = AnchorType.Invisible; RebuildBridge(); }

    [ContextMenu("Hide Anchors")]
    public void HideAnchors()
    {
        if (currentBridge.Anchors != null)
        {
            foreach (var anchor in currentBridge.Anchors)
            {
                if (!anchor) continue;
                var renderer = anchor.GetComponentInChildren<Renderer>();
                if (renderer != null) renderer.enabled = false;
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
                if (!anchor) continue;
                var renderer = anchor.GetComponentInChildren<Renderer>();
                if (renderer != null) renderer.enabled = true;
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
                if (!anchor) continue;
                var visual = anchor.transform.Find("Visual");
                if (visual != null)
                {
                    if (Application.isPlaying) Destroy(visual.gameObject);
                    else DestroyImmediate(visual.gameObject);
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

        foreach (var platform in currentBridge.Platforms)
        {
            if (!platform) continue;
            var joints = platform.GetComponents<Joint>();
            foreach (var joint in joints)
            {
                if (Application.isPlaying) Destroy(joint);
                else DestroyImmediate(joint);
            }
        }

        if (currentBridge.Platforms.Length >= 2 && currentBridge.Planks.Length > 0)
        {
            var startPlatform = currentBridge.Platforms[0];
            var firstPlank = currentBridge.Planks[0];
            float targetStartZ = firstPlank.transform.position.z - (bridgeConfig.PlankLength * 0.5f + bridgeConfig.platformGap + bridgeConfig.platformLength * 0.5f);
            startPlatform.transform.position = new Vector3(0, 0, targetStartZ);

            var endPlatform = currentBridge.Platforms[1];
            var lastPlank = currentBridge.Planks[currentBridge.Planks.Length - 1];
            float targetEndZ = lastPlank.transform.position.z + (bridgeConfig.PlankLength * 0.5f + bridgeConfig.platformGap + bridgeConfig.platformLength * 0.5f);
            endPlatform.transform.position = new Vector3(0, 0, targetEndZ);
        }

        ConnectPlatformsToBridgeManual(currentBridge.Platforms, currentBridge.Planks);
        AddPlatformAnchorSupportsManual(currentBridge.Platforms, currentBridge.Anchors);

        Debug.Log("Bridge connections fixed!");
    }

    private void ConnectPlatformsToBridgeManual(GameObject[] platforms, GameObject[] planks)
    {
        if (platforms.Length >= 2 && planks.Length > 0)
        {
            ConnectPlatformToPlankManual(platforms[0], planks[0], true);
            ConnectPlatformToPlankManual(platforms[1], planks[planks.Length - 1], false);
        }
    }

    private void ConnectPlatformToPlankManual(GameObject platform, GameObject plank, bool isStartPlatform)
    {
        var platformRb = platform.GetComponent<Rigidbody>();
        var plankRb = plank.GetComponent<Rigidbody>();
        if (platformRb == null || plankRb == null) return;

        var fixedJoint = platform.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = plankRb;

        if (isStartPlatform)
        {
            fixedJoint.anchor = new Vector3(0, 0, bridgeConfig.platformLength * 0.5f);
            fixedJoint.connectedAnchor = new Vector3(0, 0, -bridgeConfig.PlankLength * 0.5f);
        }
        else
        {
            fixedJoint.anchor = new Vector3(0, 0, -bridgeConfig.platformLength * 0.5f);
            fixedJoint.connectedAnchor = new Vector3(0, 0, bridgeConfig.PlankLength * 0.5f);
        }

        var springJoint = platform.AddComponent<SpringJoint>();
        springJoint.connectedBody = plankRb;
        springJoint.anchor = fixedJoint.anchor;
        springJoint.connectedAnchor = fixedJoint.connectedAnchor;
        springJoint.spring = bridgeConfig.jointSpring * 5f;
        springJoint.damper = bridgeConfig.jointDamper * 3f;
        springJoint.maxDistance = 0.05f;
        springJoint.minDistance = 0f;
    }

    private void AddPlatformAnchorSupportsManual(GameObject[] platforms, GameObject[] anchors)
    {
        if (platforms.Length >= 2 && anchors.Length >= 2)
        {
            var supportJoint1 = platforms[0].AddComponent<SpringJoint>();
            supportJoint1.connectedBody = anchors[0].GetComponent<Rigidbody>();
            supportJoint1.anchor = new Vector3(0, 0, bridgeConfig.platformLength * 0.4f);
            supportJoint1.spring = bridgeConfig.jointSpring * 2f;
            supportJoint1.damper = bridgeConfig.jointDamper * 2f;
            supportJoint1.maxDistance = 0.2f;

            var supportJoint2 = platforms[1].AddComponent<SpringJoint>();
            supportJoint2.connectedBody = anchors[1].GetComponent<Rigidbody>();
            supportJoint2.anchor = new Vector3(0, 0, -bridgeConfig.platformLength * 0.4f);
            supportJoint2.spring = bridgeConfig.jointSpring * 2f;
            supportJoint2.damper = bridgeConfig.jointDamper * 2f;
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
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }

    private void AddBridgeComponents()
    {
        // Add/Find BridgeTracker on the root
        var bridgeTracker = GetComponent<BridgeTracker>();
        if (bridgeTracker == null) bridgeTracker = gameObject.AddComponent<BridgeTracker>();

        // Set the start/end for progress calc
        if (currentBridge.Platforms?.Length >= 2)
        {
            bridgeTracker.SetBridgePoints(
                currentBridge.Platforms[0].transform,
                currentBridge.Platforms[1].transform
            );
        }

        CreateBridgeGoal();
    }

    private void CreateBridgeGoal()
    {
        GameObject goalParent = currentBridge.Platforms?.Length > 1 ?
            currentBridge.Platforms[1] : transform.gameObject;

        Vector3 goalPosition = currentBridge.Platforms?.Length > 1 ?
            new Vector3(0, bridgeConfig.platformThickness * 0.5f, 0) :
            new Vector3(0, 0, bridgeConfig.totalBridgeLength * 0.5f + 0.5f);

        var goal = new GameObject("BridgeGoal");
        goal.transform.SetParent(goalParent.transform, false);
        goal.transform.localPosition = goalPosition;

        var trigger = goal.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = currentBridge.Platforms?.Length > 1 ?
            new Vector3(bridgeConfig.platformWidth * 0.8f, 2f, bridgeConfig.platformLength * 0.8f) :
            new Vector3(bridgeConfig.plankWidth + 0.5f, 2f, 1f);

        goal.AddComponent<BridgeGoal>();
    }

    public void TeleportPlayerToStart()
    {
        if (currentBridge.Platforms?.Length > 0 && playerManager != null)
        {
            Transform start = currentBridge.Platforms[0].transform;
            Transform end   = currentBridge.Platforms.Length > 1 ? currentBridge.Platforms[1].transform : null;

            bool positioned = playerManager.PositionAtBridgeStart(
                currentBridge.Platforms[0],
                end,
                bridgeConfig.playerSpawnOffset
            );

            if (!positioned)
            {
                Debug.LogWarning("PlayerManager: Failed to position player at bridge start.");
            }
            else
            {
                // IMPORTANT: grant a teleport grace window to avoid instant failure
                var tracker = GetComponent<BridgeTracker>();
                if (tracker != null) tracker.NotifyTeleported();
            }
        }
        else
        {
            Debug.LogWarning("No start platform found or PlayerManager not available for player positioning.");
        }
    }

    private void LogBuildSuccess()
    {
        string platformInfo = bridgeConfig.enablePlatforms ? " with start/end platforms" : "";
        string anchorInfo = $" using {anchorType} anchors";
        Debug.Log($"Bridge validation passed! Created {currentBridge.Planks?.Length ?? 0} planks{platformInfo}{anchorInfo}");
    }

    private bool ValidateBridge(BridgeData bridgeData)
    {
        if (bridgeData.Planks == null || bridgeData.Planks.Length == 0)
        {
            Debug.LogError("Bridge validation failed: No planks found");
            return false;
        }

        foreach (var plank in bridgeData.Planks)
        {
            if (!plank) { Debug.LogError("Bridge validation failed: Invalid plank found"); return false; }
            if (plank.GetComponent<Rigidbody>() == null)
                Debug.LogWarning("Bridge validation warning: Plank missing Rigidbody");
        }

        if (bridgeData.Platforms != null)
        {
            foreach (var platform in bridgeData.Platforms)
            {
                if (!platform) { Debug.LogError("Bridge validation failed: Invalid platform found"); return false; }
            }
        }

        return true;
    }

    public void SetBridgeConfiguration(int planks, float length, float width)
    {
        if (bridgeConfig != null)
        {
            bridgeConfig.plankCount = planks;
            bridgeConfig.bridgeLength = length;
            bridgeConfig.plankWidth = width;
        }
        bridgeBuilt = false;
        if (Application.isPlaying) BuildBridge();
    }

    public void SetAnchorType(AnchorType newAnchorType)
    {
        anchorType = newAnchorType;
        bridgeBuilt = false;
        if (Application.isPlaying) BuildBridge();
    }

    public BridgeConfig GetBridgeConfiguration() => bridgeConfig;
    public void SetBridgeConfiguration(BridgeConfig newConfig)
    {
        bridgeConfig = newConfig;
        bridgeBuilt = false;
        if (Application.isPlaying) BuildBridge();
    }
    public AnchorType GetAnchorType() => anchorType;

    public GameObject[] GetPlanks() => currentBridge.Planks ?? new GameObject[0];
    public GameObject GetStartPlatform() => currentBridge.Platforms?.Length > 0 ? currentBridge.Platforms[0] : null;
    public GameObject GetEndPlatform() => currentBridge.Platforms?.Length > 1 ? currentBridge.Platforms[1] : null;

    private void OnDrawGizmos()
    {
        if (!showGizmos || bridgeConfig == null) return;

        var gizmoRenderer = new BridgeGizmoRenderer(bridgeConfig, transform, showPlayerSpawn);
        gizmoRenderer.DrawBridgeGizmos();

        if (showAnchorGizmos && currentBridge.Anchors != null)
        {
            Gizmos.color = Color.red;
            foreach (var anchor in currentBridge.Anchors)
            {
                if (anchor != null) Gizmos.DrawWireSphere(anchor.transform.position, 0.2f);
            }
        }
    }

    void OnValidate()
    {
        if (bridgeConfig != null)
        {
            bridgeConfig.plankCount = Mathf.Clamp(bridgeConfig.plankCount, 1, 20);
            bridgeConfig.bridgeLength = Mathf.Clamp(bridgeConfig.bridgeLength, 1f, 50f);
            bridgeConfig.plankWidth = Mathf.Clamp(bridgeConfig.plankWidth, 0.1f, 2f);
            bridgeConfig.plankThickness = Mathf.Clamp(bridgeConfig.plankThickness, 0.01f, 0.5f);
            bridgeConfig.plankGap = Mathf.Clamp(bridgeConfig.plankGap, 0f, 1f);
        }
    }
}

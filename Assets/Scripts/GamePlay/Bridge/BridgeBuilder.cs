using System;
using UnityEngine;
using System.Collections.Generic;
using GamePlay.Bridge;
using Unity.XR.CoreUtils;

[ExecuteAlways]
public class MultiPlankBridgeBuilder : MonoBehaviour
{
    [Header("Bridge Configuration")]
    [SerializeField] private int numberOfPlanks = 8;
    [SerializeField] private float totalBridgeLength = 8f;
    [SerializeField] private float plankWidth = 0.4f;
    [SerializeField] private float plankThickness = 0.05f;
    [SerializeField] private float plankGap = 0.02f;

    [Header("Platform Settings")]
    [SerializeField] private bool createPlatforms = true;
    [SerializeField] private float platformLength = 2f;
    [SerializeField] private float platformWidth = 2f;
    [SerializeField] private float platformThickness = 0.2f;
    [SerializeField] private float platformGap = 0.1f;

    [Header("Support Structure")]
    [SerializeField] private bool useRopes = true;
    [SerializeField] private float ropeHeight = 1f;
    [SerializeField] private float ropeSag = 0.3f;
    [SerializeField] private int supportPostsCount = 0;

    [Header("Physics Settings")]
    [SerializeField] private float plankMass = 2f;
    [SerializeField] private float platformMass = 50f;
    [SerializeField] private float jointSpring = 30f;
    [SerializeField] private float jointDamper = 2f;
    [SerializeField] private float ropeSpring = 100f;
    [SerializeField] private float ropeDamper = 5f;

    [Header("Materials")]
    [SerializeField] private Material plankMaterial;
    [SerializeField] private Material platformMaterial;
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private Material anchorMaterial;

    [Header("Player Spawn")]
    [SerializeField] private bool autoPositionPlayer = true;
    [SerializeField] private Vector3 playerSpawnOffset = new Vector3(0, 1.8f, 0);

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showRopeConnections = true;
    [SerializeField] private bool showPlayerSpawn = true;

    [SerializeField] private XROrigin player;
    // Runtime references
    private List<GameObject> planks = new List<GameObject>();
    private List<GameObject> ropes = new List<GameObject>();
    private List<GameObject> anchors = new List<GameObject>();
    private GameObject startAnchor, endAnchor;
    private GameObject startPlatform, endPlatform;

    // Calculated values
    private float plankLength => (totalBridgeLength - (numberOfPlanks - 1) * plankGap) / numberOfPlanks;
    private float plankSpacing => plankLength + plankGap;
    private float totalSystemLength => totalBridgeLength + (createPlatforms ? (platformLength * 2 + platformGap * 2) : 0);

    private void OnEnable()
    {
        BridgeStageEventHandler.onPlayerFall += PositionPlayer;
    }
    private void OnDestroy()
    {
        BridgeStageEventHandler.onPlayerFall -= PositionPlayer;
    }

    void Start()
    {
        if (Application.isPlaying)
            BuildBridge();
    }

    [ContextMenu("Build Bridge")]
    public void BuildBridge()
    {
        ClearExistingBridge();
        
        if (createPlatforms) CreatePlatforms();
        CreateAnchorPoints();
        CreatePlanks();
        if (useRopes) CreateRopeSupports();
        ConnectPlanks();
        if (createPlatforms) ConnectPlatformsToBridge();
        AddBridgeComponents();
        if (autoPositionPlayer) PositionPlayer();
        ValidateBridge();
    }

    private void ClearExistingBridge()
    {
        planks.Clear();
        ropes.Clear();
        anchors.Clear();
        startAnchor = null;
        endAnchor = null;
        startPlatform = null;
        endPlatform = null;

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

    private void CreatePlatforms()
    {
        float startPlatformZ = -(totalBridgeLength * 0.5f + platformGap + platformLength * 0.5f);
        float endPlatformZ = totalBridgeLength * 0.5f + platformGap + platformLength * 0.5f;

        startPlatform = CreatePlatform("StartPlatform", new Vector3(0, 0, startPlatformZ));
        endPlatform = CreatePlatform("EndPlatform", new Vector3(0, 0, endPlatformZ));
    }

    private GameObject CreatePlatform(string name, Vector3 position)
    {
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = name;
        platform.transform.SetParent(transform, false);
        platform.transform.localPosition = position;
        platform.transform.localScale = new Vector3(platformWidth, platformThickness, platformLength);

        var rb = platform.GetComponent<Rigidbody>();
        if (rb == null) rb = platform.AddComponent<Rigidbody>();
            
        rb.mass = platformMass;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (platformMaterial != null)
        {
            var renderer = platform.GetComponent<Renderer>();
            if (renderer != null) renderer.material = platformMaterial;
        }

        AddPlatformDetails(platform);
        return platform;
    }

    private void AddPlatformDetails(GameObject platform)
    {
        CreateRailing(platform, "LeftRailing", new Vector3(-platformWidth * 0.4f, platformThickness, 0));
        CreateRailing(platform, "RightRailing", new Vector3(platformWidth * 0.4f, platformThickness, 0));

        var edgeCollider = platform.AddComponent<BoxCollider>();
        edgeCollider.size = new Vector3(platformWidth * 1.1f, platformThickness * 3, platformLength * 1.1f);
        edgeCollider.center = new Vector3(0, platformThickness, 0);
    }

    private void CreateRailing(GameObject parent, string name, Vector3 position)
    {
        var railing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railing.name = name;
        railing.transform.SetParent(parent.transform, false);
        railing.transform.localPosition = position;
        railing.transform.localScale = new Vector3(0.05f, 1f, platformLength * 0.8f);

        var collider = railing.GetComponent<Collider>();
        if (collider != null) DestroyImmediate(collider);

        if (anchorMaterial != null)
        {
            var renderer = railing.GetComponent<Renderer>();
            if (renderer != null) renderer.material = anchorMaterial;
        }
    }

    private void CreateAnchorPoints()
    {
        float bridgeStartZ = -totalBridgeLength * 0.5f;
        float bridgeEndZ = totalBridgeLength * 0.5f;

        startAnchor = CreateAnchor("StartAnchor", new Vector3(0, 0, bridgeStartZ));
        endAnchor = CreateAnchor("EndAnchor", new Vector3(0, 0, bridgeEndZ));

        if (supportPostsCount > 0)
        {
            float spacing = totalBridgeLength / (supportPostsCount + 1);
            for (int i = 1; i <= supportPostsCount; i++)
            {
                float zPos = bridgeStartZ + spacing * i;
                var support = CreateAnchor($"Support_{i}", new Vector3(0, -0.5f, zPos));
                anchors.Add(support);
            }
        }
    }

    private GameObject CreateAnchor(string name, Vector3 position)
    {
        var anchor = new GameObject(name);
        anchor.transform.SetParent(transform, false);
        anchor.transform.localPosition = position;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(anchor.transform, false);
        visual.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        visual.transform.localPosition = Vector3.up * 0.25f;

        if (anchorMaterial != null)
        {
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null) renderer.material = anchorMaterial;
        }

        var rb = anchor.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        return anchor;
    }

    private void CreatePlanks()
    {
        float startZ = -totalBridgeLength * 0.5f + plankLength * 0.5f;

        for (int i = 0; i < numberOfPlanks; i++)
        {
            float zPos = startZ + i * plankSpacing;
            var plank = CreatePlank($"Plank_{i:00}", new Vector3(0, 0, zPos));
            planks.Add(plank);
        }
    }

    private GameObject CreatePlank(string name, Vector3 position)
    {
        var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = name;
        plank.transform.SetParent(transform, false);
        plank.transform.localPosition = position;
        plank.transform.localScale = new Vector3(plankWidth, plankThickness, plankLength);

        var rb = plank.GetComponent<Rigidbody>();
        if (rb == null) rb = plank.AddComponent<Rigidbody>();
            
        rb.mass = plankMass;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (plankMaterial != null)
        {
            var renderer = plank.GetComponent<Renderer>();
            if (renderer != null) renderer.material = plankMaterial;
        }

        AddPlankEdgeColliders(plank);
        return plank;
    }

    private void AddPlankEdgeColliders(GameObject plank)
    {
        var frontEdge = new GameObject("FrontEdge");
        frontEdge.transform.SetParent(plank.transform, false);
        frontEdge.transform.localPosition = new Vector3(0, 0, plankLength * 0.4f);
        var frontCollider = frontEdge.AddComponent<BoxCollider>();
        frontCollider.size = new Vector3(plankWidth, plankThickness * 2, plankLength * 0.2f);

        var backEdge = new GameObject("BackEdge");
        backEdge.transform.SetParent(plank.transform, false);
        backEdge.transform.localPosition = new Vector3(0, 0, -plankLength * 0.4f);
        var backCollider = backEdge.AddComponent<BoxCollider>();
        backCollider.size = new Vector3(plankWidth, plankThickness * 2, plankLength * 0.2f);
    }

    private void CreateRopeSupports()
    {
        CreateRopeLine("LeftRope", new Vector3(-plankWidth * 0.6f, ropeHeight, 0));
        CreateRopeLine("RightRope", new Vector3(plankWidth * 0.6f, ropeHeight, 0));
    }

    private void CreateRopeLine(string name, Vector3 offset)
    {
        var ropeParent = new GameObject(name);
        ropeParent.transform.SetParent(transform, false);

        int ropeSegments = numberOfPlanks + 1;
        List<GameObject> ropeNodes = new List<GameObject>();

        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = (float)i / ropeSegments;
            float zPos = Mathf.Lerp(-totalBridgeLength * 0.5f, totalBridgeLength * 0.5f, t);
            float sagY = -ropeSag * 1 * t * (1 - t);
            
            var ropeNode = CreateRopeNode($"Node_{i:00}", new Vector3(offset.x, offset.y + sagY, zPos));
            ropeNode.transform.SetParent(ropeParent.transform, false);
            ropeNodes.Add(ropeNode);
            ropes.Add(ropeNode);
        }

        for (int i = 0; i < ropeNodes.Count - 1; i++)
        {
            ConnectRopeNodes(ropeNodes[i], ropeNodes[i + 1]);
        }

        ConnectRopeToAnchor(ropeNodes[0], startAnchor);
        ConnectRopeToAnchor(ropeNodes[ropeNodes.Count - 1], endAnchor);

        for (int i = 0; i < planks.Count; i++)
        {
            if (i + 1 < ropeNodes.Count - 1)
            {
                ConnectRopeToPlank(ropeNodes[i + 1], planks[i], offset);
            }
        }
    }

    private GameObject CreateRopeNode(string name, Vector3 position)
    {
        var node = new GameObject(name);
        node.transform.localPosition = position;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(node.transform, false);
        visual.transform.localScale = Vector3.one * 0.02f;
        
        if (ropeMaterial != null)
        {
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null) renderer.material = ropeMaterial;
        }

        var rb = node.AddComponent<Rigidbody>();
        rb.mass = 0.1f;
        rb.useGravity = true;
        rb.drag = 2f;

        return node;
    }

    private void ConnectRopeNodes(GameObject nodeA, GameObject nodeB)
    {
        var rbA = nodeA.GetComponent<Rigidbody>();
        var rbB = nodeB.GetComponent<Rigidbody>();
        
        if (rbA == null || rbB == null) return;

        var joint = nodeA.AddComponent<SpringJoint>();
        joint.connectedBody = rbB;
        joint.spring = ropeSpring;
        joint.damper = ropeDamper;
        joint.autoConfigureConnectedAnchor = true;
    }

    private void ConnectRopeToAnchor(GameObject ropeNode, GameObject anchor)
    {
        var ropeRb = ropeNode.GetComponent<Rigidbody>();
        var anchorRb = anchor.GetComponent<Rigidbody>();
        
        if (ropeRb == null || anchorRb == null) return;

        var joint = ropeNode.AddComponent<FixedJoint>();
        joint.connectedBody = anchorRb;
    }

    private void ConnectRopeToPlank(GameObject ropeNode, GameObject plank, Vector3 offset)
    {
        var ropeRb = ropeNode.GetComponent<Rigidbody>();
        var plankRb = plank.GetComponent<Rigidbody>();
        
        if (ropeRb == null || plankRb == null) return;

        var joint = ropeNode.AddComponent<SpringJoint>();
        joint.connectedBody = plankRb;
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = new Vector3(offset.x, plankThickness * 0.5f, 0);
        joint.spring = ropeSpring * 0.5f;
        joint.damper = ropeDamper;
    }

    private void ConnectPlanks()
    {
        if (planks.Count > 0)
        {
            ConnectToAnchor(planks[0], startAnchor, new Vector3(0, 0, -plankLength * 0.5f));
        }

        for (int i = 0; i < planks.Count - 1; i++)
        {
            ConnectPlankToPlank(planks[i], planks[i + 1]);
        }

        if (planks.Count > 0)
        {
            ConnectToAnchor(planks[planks.Count - 1], endAnchor, new Vector3(0, 0, plankLength * 0.5f));
        }

        for (int i = 0; i < anchors.Count; i++)
        {
            int nearestPlankIndex = Mathf.RoundToInt((float)(i + 1) * planks.Count / (anchors.Count + 1)) - 1;
            nearestPlankIndex = Mathf.Clamp(nearestPlankIndex, 0, planks.Count - 1);
            
            if (nearestPlankIndex < planks.Count)
            {
                ConnectToSupport(planks[nearestPlankIndex], anchors[i]);
            }
        }
    }

    private void ConnectToAnchor(GameObject plank, GameObject anchor, Vector3 anchorPoint)
    {
        var plankRb = plank.GetComponent<Rigidbody>();
        var anchorRb = anchor.GetComponent<Rigidbody>();
        
        if (plankRb == null || anchorRb == null) return;

        var joint = plank.AddComponent<SpringJoint>();
        joint.connectedBody = anchorRb;
        joint.anchor = anchorPoint;
        joint.spring = jointSpring * 2f;
        joint.damper = jointDamper;
        joint.maxDistance = 0.1f;
    }

    private void ConnectPlankToPlank(GameObject plankA, GameObject plankB)
    {
        var rbA = plankA.GetComponent<Rigidbody>();
        var rbB = plankB.GetComponent<Rigidbody>();
        
        if (rbA == null || rbB == null) return;

        var joint = plankA.AddComponent<HingeJoint>();
        joint.connectedBody = rbB;
        joint.anchor = new Vector3(0, 0, plankLength * 0.5f);
        joint.connectedAnchor = new Vector3(0, 0, -plankLength * 0.5f);
        joint.axis = Vector3.right;
        
        joint.useSpring = true;
        var spring = new JointSpring
        {
            spring = jointSpring,
            damper = jointDamper,
            targetPosition = 0f
        };
        joint.spring = spring;

        joint.useLimits = true;
        joint.limits = new JointLimits { min = -5f, max = 5f };
    }

    private void ConnectToSupport(GameObject plank, GameObject support)
    {
        var plankRb = plank.GetComponent<Rigidbody>();
        var supportRb = support.GetComponent<Rigidbody>();
        
        if (plankRb == null || supportRb == null) return;

        var joint = plank.AddComponent<SpringJoint>();
        joint.connectedBody = supportRb;
        joint.spring = jointSpring * 0.5f;
        joint.damper = jointDamper;
        joint.maxDistance = 1f;
    }

    private void ConnectPlatformsToBridge()
    {
        if (planks.Count == 0) return;

        if (startPlatform != null && planks.Count > 0)
        {
            ConnectPlatformToPlank(startPlatform, planks[0], true);
        }

        if (endPlatform != null && planks.Count > 0)
        {
            ConnectPlatformToPlank(endPlatform, planks[planks.Count - 1], false);
        }
    }

    private void ConnectPlatformToPlank(GameObject platform, GameObject plank, bool isStartPlatform)
    {
        var platformRb = platform.GetComponent<Rigidbody>();
        var plankRb = plank.GetComponent<Rigidbody>();
        
        if (platformRb == null || plankRb == null) return;

        var joint = platform.AddComponent<SpringJoint>();
        joint.connectedBody = plankRb;
        
        if (isStartPlatform)
        {
            joint.anchor = new Vector3(0, 0, platformLength * 0.5f);
            joint.connectedAnchor = new Vector3(0, 0, -plankLength * 0.5f);
        }
        else
        {
            joint.anchor = new Vector3(0, 0, -platformLength * 0.5f);
            joint.connectedAnchor = new Vector3(0, 0, plankLength * 0.5f);
        }

        joint.spring = jointSpring * 3f;
        joint.damper = jointDamper * 2f;
        joint.maxDistance = platformGap + 0.1f;
    }

    private void PositionPlayer()
    {
        if (startPlatform == null) return;

        // Try to find XR Origin
        var xrOrigin = FindXROrigin();

        if (xrOrigin != null)
        {
            Vector3 targetPosition = startPlatform.transform.position + playerSpawnOffset;
            xrOrigin.transform.position = targetPosition;
            xrOrigin.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            
            Debug.Log($"Player positioned on start platform at {targetPosition}");
        }
        else
        {
            Debug.LogWarning("Could not find XR Origin to position player. Please position manually on the start platform.");
        }
    }

    private Transform FindXROrigin()
    {
        // Try multiple methods to find XR Origin
        var xrOriginComponent = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOriginComponent != null) return xrOriginComponent.transform;

        // Try common GameObject names
        string[] possibleNames = { "XR Origin", "XRRig", "XR Rig", "VRPlayer", "Player" };
        foreach (string name in possibleNames)
        {
            var obj = GameObject.Find(name);
            if (obj != null) return obj.transform;
        }

        return null;
    }

    private void AddBridgeComponents()
    {
        if (planks.Count > 0)
        {
            int middlePlank = planks.Count / 2;
            var balanceChecker = planks[middlePlank].GetComponent<BalanceChecker>();
            if (balanceChecker == null)
            {
                balanceChecker = planks[middlePlank].AddComponent<BalanceChecker>();
            }
        }

        GameObject goalParent = endPlatform != null ? endPlatform : transform.gameObject;
        Vector3 goalPosition = endPlatform != null 
            ? new Vector3(0, platformThickness * 0.5f, 0)
            : new Vector3(0, 0, totalBridgeLength * 0.5f + 0.5f);

        var goal = new GameObject("BridgeGoal");
        goal.transform.SetParent(goalParent.transform, false);
        goal.transform.localPosition = goalPosition;

        var trigger = goal.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        
        if (endPlatform != null)
        {
            trigger.size = new Vector3(platformWidth * 0.8f, 2f, platformLength * 0.8f);
        }
        else
        {
            trigger.size = new Vector3(plankWidth + 0.5f, 2f, 1f);
        }

        var bridgeGoal = goal.AddComponent<BridgeGoal>();
    }

    private void ValidateBridge()
    {
        bool isValid = true;
        
        for (int i = 0; i < planks.Count; i++)
        {
            if (planks[i] == null || planks[i].GetComponent<Rigidbody>() == null)
            {
                Debug.LogError($"Plank {i} validation failed!");
                isValid = false;
            }
        }
        
        if (createPlatforms)
        {
            if (startPlatform == null || endPlatform == null)
            {
                Debug.LogError("Platform validation failed!");
                isValid = false;
            }
        }
        
        if (isValid)
        {
            string platformInfo = createPlatforms ? " with start/end platforms" : "";
            string ropeInfo = useRopes ? " and rope supports" : "";
            Debug.Log($"Bridge validation passed! Created {planks.Count} planks{platformInfo}{ropeInfo}");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (createPlatforms)
        {
            Gizmos.color = Color.cyan;
            
            float startPlatformZ = -(totalBridgeLength * 0.5f + platformGap + platformLength * 0.5f);
            var startPlatformPos = transform.position + new Vector3(0, 0, startPlatformZ);
            var platformSize = new Vector3(platformWidth, platformThickness, platformLength);
            Gizmos.DrawWireCube(startPlatformPos, platformSize);
            
            float endPlatformZ = totalBridgeLength * 0.5f + platformGap + platformLength * 0.5f;
            var endPlatformPos = transform.position + new Vector3(0, 0, endPlatformZ);
            Gizmos.DrawWireCube(endPlatformPos, platformSize);

            if (showPlayerSpawn)
            {
                Gizmos.color = Color.magenta;
                var spawnPos = startPlatformPos + playerSpawnOffset;
                Gizmos.DrawWireSphere(spawnPos, 0.3f);
                Gizmos.DrawRay(spawnPos, Vector3.forward * 0.5f);
            }
        }

        Gizmos.color = Color.yellow;
        var bridgeCenter = transform.position;
        var bridgeSize = new Vector3(plankWidth, plankThickness, totalBridgeLength);
        Gizmos.DrawWireCube(bridgeCenter, bridgeSize);

        Gizmos.color = Color.green;
        float startZ = -totalBridgeLength * 0.5f + plankLength * 0.5f;
        
        for (int i = 0; i < numberOfPlanks; i++)
        {
            float zPos = startZ + i * plankSpacing;
            var plankPos = transform.position + new Vector3(0, 0, zPos);
            var plankSize = new Vector3(plankWidth, plankThickness, plankLength);
            Gizmos.DrawWireCube(plankPos, plankSize);
        }

        Gizmos.color = Color.red;
        var startAnchorPos = transform.position + new Vector3(0, 0, -totalBridgeLength * 0.5f);
        var endAnchorPos = transform.position + new Vector3(0, 0, totalBridgeLength * 0.5f);
        Gizmos.DrawWireSphere(startAnchorPos, 0.1f);
        Gizmos.DrawWireSphere(endAnchorPos, 0.1f);
    }

    void OnValidate()
    {
        numberOfPlanks = Mathf.Clamp(numberOfPlanks, 1, 20);
        totalBridgeLength = Mathf.Clamp(totalBridgeLength, 1f, 50f);
        plankWidth = Mathf.Clamp(plankWidth, 0.1f, 2f);
        plankThickness = Mathf.Clamp(plankThickness, 0.01f, 0.5f);
        plankGap = Mathf.Clamp(plankGap, 0f, 1f);
        
        if (createPlatforms)
        {
            platformLength = Mathf.Clamp(platformLength, 0.5f, 10f);
            platformWidth = Mathf.Clamp(platformWidth, plankWidth, 10f);
        }
    }

    // Public API
    public void SetBridgeConfiguration(int planks, float length, float width)
    {
        numberOfPlanks = planks;
        totalBridgeLength = length;
        plankWidth = width;
        if (Application.isPlaying) BuildBridge();
    }

    public void SetPlatformConfiguration(bool enablePlatforms, float length, float width)
    {
        createPlatforms = enablePlatforms;
        platformLength = length;
        platformWidth = width;
        if (Application.isPlaying) BuildBridge();
    }

    public List<GameObject> GetPlanks() => new List<GameObject>(planks);
    public GameObject GetStartPlatform() => startPlatform;
    public GameObject GetEndPlatform() => endPlatform;
    
    public void TeleportPlayerToStart()
    {
        if (startPlatform != null) PositionPlayer();
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BridgeEnvironmentGenerator : MonoBehaviour
{
    [Header("Bridge Sources")]
    public BridgeConfig bridgeConfig;
    public float totalLengthOverride = -1f;

    [Header("Rock Prefabs")]
    public List<GameObject> rockPrefabs = new List<GameObject>();
    public List<GameObject> endCapPrefabs = new List<GameObject>();

    [Header("Corridor Bounds")]
    [Tooltip("Extra clearance beyond the walkable/platform width.")]
    public float sideClearance = 0.6f;

    [Tooltip("Additional guaranteed clearance outside the corridor that rocks must respect.")]
    public float minHorizontalClearance = 0.3f;

    [Tooltip("How far from the corridor edge to center the rock bands (after clearance).")]
    public float sideBandOffset = 0.8f;

    [Tooltip("Half-width variance of the band (rocks can drift in/out from band center).")]
    public float sideBandHalfWidth = 0.6f;

    [Tooltip("Extra empty space before/after the bridge along Z.")]
    public float endClearanceZ = 1.0f;

    [Header("Distribution")]
    public float densityPer10m = 6f;
    public float minSpacingZ = 0.9f;
    public float extraSpacingZ = 0.9f;

    [Header("Scale / Rotation")]
    public Vector3 scaleMin = new Vector3(0.7f, 0.7f, 0.7f);
    public Vector3 scaleMax = new Vector3(1.6f, 1.6f, 1.6f);
    [Range(0f, 0.7f)] public float gaussianStdDev = 0.18f;
    public Vector2 gaussianClamp = new Vector2(0.6f, 1.8f);
    public bool randomYaw = true;
    public bool smallTilt = true;
    [Range(0f, 20f)] public float maxTiltDeg = 6f;

    [Header("Perlin Jitter")]
    public bool usePerlinJitter = true;
    [Min(0.0001f)] public float perlinFrequency = 0.25f;
    [Range(0f, 1f)] public float perlinPosAmplitude = 0.35f;
    [Range(0f, 0.5f)] public float perlinScaleAmplitude = 0.18f;

    [Header("End-caps")]
    public bool placeEndCaps = true;
    public float endCapScaleMultiplier = 2.8f;
    public float endCapOffset = 0.5f;

    [Header("Ground & River (optional)")]
    public bool createGroundPlane = true;
    public bool createRiverPlane = false;
    public float groundY = -0.05f;

    [Tooltip("Minimum vertical gap under the bridge top (keeps environment below it).")]
    public float minVerticalGap = 0.05f;

    public float riverY = -0.2f;
    public Vector2 planeSize = new Vector2(20f, 50f);
    public Material groundMaterial;
    public Material riverMaterial;

    [Header("Ground Alignment")]
    public bool alignToGround = false;
    public LayerMask groundMask = ~0;
    public float rayStartHeight = 25f;
    public float rayLength = 60f;

    [Header("Optional Bridge Collision Avoidance")]
    [Tooltip("If true, rocks will be pushed outward in X until they no longer overlap colliders on this mask (e.g., a 'Bridge' layer).")]
    public bool avoidOverlapWithBridge = true;
    public LayerMask bridgeMask = 0;
    [Tooltip("Bounds padding for overlap checks (meters).")]
    public Vector3 bridgeOverlapPadding = new Vector3(0.05f, 0.05f, 0.05f);
    public int pushOutMaxIterations = 6;
    public float pushOutStepX = 0.25f;

    [Header("Runtime & Editor")]
    public bool autoGenerateOnStart = true;
    public bool useSeed = true;
    public int seed = 1234;

    [Header("Gizmos")]
    public bool drawGuideGizmos = true;
    public Color leftColor = new Color(0.2f, 0.8f, 1f, 0.6f);
    public Color rightColor = new Color(1f, 0.6f, 0.2f, 0.6f);

    private const string ROOT_NAME = "Environment_Generated";
    private const string LEFT_BAND = "Rocks_Left";
    private const string RIGHT_BAND = "Rocks_Right";
    private const string ENDCAPS = "EndCaps";
    private const string GROUND = "GroundPlane";
    private const string RIVER = "RiverPlane";

    private Transform _root;

    private void Awake()
    {
        if (bridgeConfig == null)
        {
            var solid = GetComponent<SOLIDBridgeBuilder>();
            if (solid != null) bridgeConfig = solid.GetBridgeConfiguration();
        }
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        if (autoGenerateOnStart) Regenerate();
    }

    [ContextMenu("Regenerate Environment")]
    public void Regenerate()
    {
        ClearEnvironment();
        GenerateEnvironment();
    }

    [ContextMenu("Generate Environment")]
    public void GenerateEnvironment()
    {
        if (useSeed) UnityEngine.Random.InitState(seed);
        EnsureRoot();

        float length = GetTotalLength();
        float halfLen = length * 0.5f;

        float corridorHalfWidth = GetCorridorHalfWidth();
        float innerEdgeX = corridorHalfWidth + sideClearance + minHorizontalClearance;
        float bandCenterX = Mathf.Max(innerEdgeX + 0.2f, corridorHalfWidth + sideClearance + sideBandOffset);

        // planes first
        if (createGroundPlane) CreateOrUpdatePlane(GROUND, Mathf.Min(groundY, GetBridgeTopY() - minVerticalGap), groundMaterial);
        if (createRiverPlane) CreateOrUpdatePlane(RIVER, Mathf.Min(riverY, GetBridgeTopY() - minVerticalGap - 0.1f), riverMaterial);

        // bands
        var left  = EnsureChild(LEFT_BAND);
        var right = EnsureChild(RIGHT_BAND);

        float zStart = -halfLen + endClearanceZ;
        float zEnd   =  halfLen - endClearanceZ;

        PopulateSideBand(left,  -1, innerEdgeX, bandCenterX, zStart, zEnd);
        PopulateSideBand(right, +1, innerEdgeX, bandCenterX, zStart, zEnd);

        if (placeEndCaps) PlaceEndCaps(zStart: -halfLen, zEnd: halfLen, corridorHalfWidth, innerEdgeX);
    }

    [ContextMenu("Clear Environment")]
    public void ClearEnvironment()
    {
        var t = transform.Find(ROOT_NAME);
        if (t == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Clear Bridge Environment");
#endif
        if (Application.isPlaying) Destroy(t.gameObject);
        else DestroyImmediate(t.gameObject);
    }

    // ---------- generation internals ----------

  private void PopulateSideBand(Transform parent, int sideSign, float innerEdgeX, float bandCenterX, float zStart, float zEnd)
{
    if (rockPrefabs == null || rockPrefabs.Count == 0)
    {
        Debug.LogWarning("[EnvGen] No rockPrefabs assigned — nothing to place.");
        return;
    }

    // Clamp usable range if endClearanceZ ate it all.
    if (zEnd <= zStart)
    {
        // Fallback: place a single rock at each side mid Z of the whole bridge span.
        float midZ = (GetTotalLength() * 0.5f - endCapOffset) * 0.0f; // 0 actually; left for readability
        Debug.LogWarning("[EnvGen] z range collapsed (endClearanceZ too large?). Placing one fallback rock per side.");
        PlaceOne(parent, sideSign, innerEdgeX, bandCenterX, midZ, zStart, zEnd);
        return;
    }

    float length = zEnd - zStart;

    // Compute slot count so we always place at least one.
    int slots = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(0.01f, minSpacingZ)));

    for (int i = 0; i < slots; i++)
    {
        // Midpoint of each slot along Z
        float t = (i + 0.5f) / slots;
        float baseZ = Mathf.Lerp(zStart, zEnd, t);

        // Small jitter around the midpoint (uses extraSpacingZ as amplitude)
        float jitter = (extraSpacingZ > 0f) ? UnityEngine.Random.Range(-extraSpacingZ * 0.5f, extraSpacingZ * 0.5f) : 0f;
        float z = Mathf.Clamp(baseZ + jitter, zStart, zEnd);

        PlaceOne(parent, sideSign, innerEdgeX, bandCenterX, z, zStart, zEnd);
    }
}

private void PlaceOne(Transform parent, int sideSign, float innerEdgeX, float bandCenterX, float z, float zStart, float zEnd)
{
    // base X around band center + random band jitter
    float x = sideSign * (bandCenterX + UnityEngine.Random.Range(-sideBandHalfWidth, sideBandHalfWidth));

    // Perlin drift in X
    if (usePerlinJitter)
    {
        float tt = (z - zStart) * perlinFrequency * 0.1f + (useSeed ? seed * 0.37f : UnityEngine.Random.value);
        float n = Mathf.PerlinNoise(tt, sideSign < 0 ? 0.11f : 7.13f);
        float drift = (n - 0.5f) * 2f * perlinPosAmplitude;
        x += drift;
    }

    // HARD CLAMP: never inside corridor+clearance
    if (sideSign < 0) x = Mathf.Min(-innerEdgeX, x);
    else              x = Mathf.Max( innerEdgeX, x);

    // prefab
    var prefab = rockPrefabs[UnityEngine.Random.Range(0, rockPrefabs.Count)];
    if (!prefab) return;

    // rotation
    Quaternion rot;
    if (randomYaw)
    {
        float yaw   = UnityEngine.Random.Range(0f, 360f);
        float pitch = smallTilt ? UnityEngine.Random.Range(-maxTiltDeg, maxTiltDeg) : 0f;
        float roll  = smallTilt ? UnityEngine.Random.Range(-maxTiltDeg, maxTiltDeg) : 0f;
        rot = Quaternion.Euler(pitch, yaw, roll);
    }
    else rot = Quaternion.identity;

    // scale
    Vector3 scale = SampleScale(z);

    // position (stay below bridge)
    float baseY = Mathf.Min(groundY, GetBridgeTopY() - minVerticalGap);
    Vector3 pos = new Vector3(x, baseY, z);

    // ground align (clamped below bridge)
    if (alignToGround)
    {
        Vector3 rayStart = new Vector3(x, rayStartHeight, z);
        if (Physics.Raycast(rayStart, Vector3.down, out var hit, rayLength, groundMask, QueryTriggerInteraction.Ignore))
            pos.y = Mathf.Min(hit.point.y, GetBridgeTopY() - minVerticalGap);
    }

    // optional push-out vs bridge colliders
    if (avoidOverlapWithBridge && bridgeMask.value != 0)
        pos = PushOutFromBridge(pos, scale, rot, sideSign);

    CreateInstance(prefab, parent, pos, rot, scale);
}

    private void PlaceEndCaps(float zStart, float zEnd, float corridorHalfWidth, float innerEdgeX)
    {
        if (endCapPrefabs == null || endCapPrefabs.Count == 0) endCapPrefabs = rockPrefabs;
        var endParent = EnsureChild(ENDCAPS);

        float capZ0 = zStart - endCapOffset;
        float capZ1 = zEnd + endCapOffset;

        foreach (var sign in new[] { -1, +1 })
        {
            // Start end
            var prefab0 = endCapPrefabs[UnityEngine.Random.Range(0, endCapPrefabs.Count)];
            var s0 = SampleScale(capZ0) * endCapScaleMultiplier;

            float x0 = sign < 0 ? -innerEdgeX : innerEdgeX;
            Vector3 p0 = new Vector3(x0, Mathf.Min(groundY, GetBridgeTopY() - minVerticalGap), capZ0);
            Quaternion r0 = Quaternion.Euler(
                smallTilt ? UnityEngine.Random.Range(-maxTiltDeg, maxTiltDeg) : 0f,
                UnityEngine.Random.Range(0f, 360f),
                smallTilt ? UnityEngine.Random.Range(-maxTiltDeg, maxTiltDeg) : 0f
            );
            if (alignToGround)
            {
                Vector3 rayStart = new Vector3(p0.x, rayStartHeight, p0.z);
                if (Physics.Raycast(rayStart, Vector3.down, out var h0, rayLength, groundMask, QueryTriggerInteraction.Ignore))
                    p0.y = Mathf.Min(h0.point.y, GetBridgeTopY() - minVerticalGap);
            }
            if (avoidOverlapWithBridge && bridgeMask.value != 0)
                p0 = PushOutFromBridge(p0, s0, r0, sign);

            CreateInstance(prefab0, endParent, p0, r0, s0);

            // End end
            var prefab1 = endCapPrefabs[UnityEngine.Random.Range(0, endCapPrefabs.Count)];
            var s1 = SampleScale(capZ1) * endCapScaleMultiplier;

            float x1 = sign < 0 ? -innerEdgeX : innerEdgeX;
            Vector3 p1 = new Vector3(x1, Mathf.Min(groundY, GetBridgeTopY() - minVerticalGap), capZ1);
            Quaternion r1 = Quaternion.Euler(
                smallTilt ? UnityEngine.Random.Range(-maxTiltDeg, maxTiltDeg) : 0f,
                UnityEngine.Random.Range(0f, 360f),
                smallTilt ? UnityEngine.Random.Range(-maxTiltDeg, maxTiltDeg) : 0f
            );
            if (alignToGround)
            {
                Vector3 rayStart = new Vector3(p1.x, rayStartHeight, p1.z);
                if (Physics.Raycast(rayStart, Vector3.down, out var h1, rayLength, groundMask, QueryTriggerInteraction.Ignore))
                    p1.y = Mathf.Min(h1.point.y, GetBridgeTopY() - minVerticalGap);
            }
            if (avoidOverlapWithBridge && bridgeMask.value != 0)
                p1 = PushOutFromBridge(p1, s1, r1, sign);

            CreateInstance(prefab1, endParent, p1, r1, s1);
        }
    }

    // ---- helpers ----

    private float GetTotalLength()
    {
        if (totalLengthOverride > 0f) return totalLengthOverride;
        if (bridgeConfig == null) return 10f;
        float L = bridgeConfig.TotalLength;
        if (L <= 0f) L = bridgeConfig.bridgeLength;
        return Mathf.Max(1f, L);
    }

    private float GetCorridorHalfWidth()
    {
        if (bridgeConfig == null) return 1f;
        float w = bridgeConfig.enablePlatforms ? bridgeConfig.platformWidth : bridgeConfig.plankWidth;
        return Mathf.Max(0.1f, w * 0.5f);
    }

    private float GetBridgeTopY()
    {
        // Your builder places platforms/planks at Y=0 with thickness above/below.
        // Use platform thickness if available; else plank thickness; else 0.
        if (bridgeConfig == null) return 0f;
        float t = Mathf.Max(bridgeConfig.platformThickness, bridgeConfig.plankThickness);
        return 0f + t * 0.5f; // top surface approx
    }

    private Vector3 SampleScale(float z)
    {
        float sx = UnityEngine.Random.Range(scaleMin.x, scaleMax.x);
        float sy = UnityEngine.Random.Range(scaleMin.y, scaleMax.y);
        float sz = UnityEngine.Random.Range(scaleMax.z, scaleMax.z); // NOTE: typo fix below
        // (Fix) correct Z sampling:
        sz = UnityEngine.Random.Range(scaleMin.z, scaleMax.z);

        Vector3 s = new Vector3(Mathf.Max(0.0001f, sx), Mathf.Max(0.0001f, sy), Mathf.Max(0.0001f, sz));

        if (gaussianStdDev > 0.0001f)
        {
            float g = Mathf.Clamp(NextGaussian(1f, gaussianStdDev),
                                  Mathf.Min(gaussianClamp.x, gaussianClamp.y),
                                  Mathf.Max(gaussianClamp.x, gaussianClamp.y));
            s *= g;
        }

        if (usePerlinJitter && perlinScaleAmplitude > 0f)
        {
            float t = (z * perlinFrequency * 0.1f) + (useSeed ? seed * 0.19f : UnityEngine.Random.value);
            float n = Mathf.PerlinNoise(t, 3.71f);
            float mult = 1f + (n - 0.5f) * 2f * perlinScaleAmplitude;
            s *= mult;
        }
        return s;
    }

    private static float NextGaussian(float mean, float stdDev)
    {
        float u1 = 1f - UnityEngine.Random.value;
        float u2 = 1f - UnityEngine.Random.value;
        float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
        return mean + stdDev * randStdNormal;
    }

    private void EnsureRoot()
    {
        var t = transform.Find(ROOT_NAME);
        if (t == null)
        {
            var go = new GameObject(ROOT_NAME);
            _root = go.transform;
            _root.SetParent(transform, false);
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(go, "Create Environment Root");
#endif
        }
        else _root = t;
    }

    private Transform EnsureChild(string name)
    {
        var t = _root.Find(name);
        if (t) return t;
        var go = new GameObject(name);
        var child = go.transform;
        child.SetParent(_root, false);
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
#endif
        return child;
    }

    private void CreateOrUpdatePlane(string name, float y, Material mat)
    {
        var t = EnsureChild(name);
        var go = t.gameObject;

        // Always ensure these components exist
        var mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.AddComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.LogError($"[EnvGen] Failed to add MeshFilter to {go.name}.");
            return;
        }

        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();
        if (mr == null)
        {
            Debug.LogError($"[EnvGen] Failed to add MeshRenderer to {go.name}.");
            return;
        }

        // Build / assign a fresh quad mesh (XZ)
        var mesh = GenerateQuadXZ(planeSize.x, planeSize.y);
#if UNITY_EDITOR
        // prevent leaking meshes in Edit Mode: reuse sharedMesh if shape unchanged
        if (!Application.isPlaying && mf.sharedMesh != null &&
            Mathf.Approximately(mf.sharedMesh.bounds.size.x, planeSize.x) &&
            Mathf.Approximately(mf.sharedMesh.bounds.size.z, planeSize.y))
        {
            // reuse existing mesh
            mf.sharedMesh = mf.sharedMesh;
        }
        else
#endif
        {
            mf.sharedMesh = mesh;
        }

        mr.sharedMaterial = mat;

        t.localPosition = new Vector3(0f, y, 0f);
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    private static Mesh GenerateQuadXZ(float sizeX, float sizeZ)
    {
        var mesh = new Mesh { name = "Env_QuadXZ" };
        float hx = Mathf.Max(0.01f, sizeX) * 0.5f;
        float hz = Mathf.Max(0.01f, sizeZ) * 0.5f;

        mesh.vertices = new Vector3[]
        {
            new Vector3(-hx,0,-hz), new Vector3(-hx,0,hz),
            new Vector3( hx,0,hz),  new Vector3( hx,0,-hz),
        };
        mesh.uv = new Vector2[] { new(0,0), new(0,1), new(1,1), new(1,0) };
        mesh.triangles = new int[] { 0,1,2, 0,2,3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }


    private void CreateInstance(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale)
    {
#if UNITY_EDITOR
        GameObject inst;
        if (!Application.isPlaying)
        {
            inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.SetPositionAndRotation(pos, rot);
            inst.transform.localScale = scale;
            Undo.RegisterCreatedObjectUndo(inst, "Spawn Env Rock");
        }
        else
        {
            inst = Instantiate(prefab, pos, rot, parent);
            inst.transform.localScale = scale;
        }
#else
        var inst = Instantiate(prefab, pos, rot, parent);
        inst.transform.localScale = scale;
#endif
    }

    // Pushes the rock outward in X until it no longer overlaps "bridgeMask"
    private Vector3 PushOutFromBridge(Vector3 pos, Vector3 scale, Quaternion rot, int sideSign)
    {
        if (bridgeMask.value == 0) return pos;

        // Approximate box based on prefab scale; we don't have the prefab bounds here,
        // so use a conservative box around the transform scale.
        Vector3 half = (scale * 0.5f) + bridgeOverlapPadding;
        Vector3 p = pos;

        int it = 0;
        while (Physics.CheckBox(p, half, rot, bridgeMask, QueryTriggerInteraction.Ignore) && it++ < pushOutMaxIterations)
        {
            p.x += pushOutStepX * Mathf.Sign(sideSign);
            // also ensure we never cross into corridor
            float corridorHalfWidth = GetCorridorHalfWidth();
            float innerEdgeX = corridorHalfWidth + sideClearance + minHorizontalClearance;
            if (sideSign < 0) p.x = Mathf.Min(-innerEdgeX, p.x);
            else              p.x = Mathf.Max( innerEdgeX, p.x);
        }
        return p;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGuideGizmos) return;
        float length = GetTotalLength();
        float half = length * 0.5f;
        float corridor = GetCorridorHalfWidth();
        float innerEdgeX = corridor + sideClearance + minHorizontalClearance;
        float bandX = Mathf.Max(innerEdgeX + 0.2f, corridor + sideClearance + sideBandOffset);

        float y = Mathf.Min(groundY, GetBridgeTopY() - minVerticalGap);

        // center line
        Gizmos.color = Color.white * 0.8f;
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(0, y, -half)),
                        transform.TransformPoint(new Vector3(0, y,  half)));

        // inner corridor walls (no-rock zone)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-innerEdgeX, y, -half)),
                        transform.TransformPoint(new Vector3(-innerEdgeX, y,  half)));
        Gizmos.DrawLine(transform.TransformPoint(new Vector3( innerEdgeX, y, -half)),
                        transform.TransformPoint(new Vector3( innerEdgeX, y,  half)));

        // left/right band centers
        Gizmos.color = leftColor;
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-bandX, y, -half)),
                        transform.TransformPoint(new Vector3(-bandX, y,  half)));
        Gizmos.color = rightColor;
        Gizmos.DrawLine(transform.TransformPoint(new Vector3( bandX, y, -half)),
                        transform.TransformPoint(new Vector3( bandX, y,  half)));

        // end clearances
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-innerEdgeX, y, -half + endClearanceZ)),
                        transform.TransformPoint(new Vector3( innerEdgeX, y, -half + endClearanceZ)));
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-innerEdgeX, y,  half - endClearanceZ)),
                        transform.TransformPoint(new Vector3( innerEdgeX, y,  half - endClearanceZ)));
    }

    private void OnValidate()
    {
        densityPer10m        = Mathf.Max(0f, densityPer10m);
        minSpacingZ          = Mathf.Max(0.1f, minSpacingZ);
        extraSpacingZ        = Mathf.Max(0f, extraSpacingZ);
        sideClearance        = Mathf.Max(0f, sideClearance);
        minHorizontalClearance = Mathf.Max(0f, minHorizontalClearance);
        sideBandOffset       = Mathf.Max(0f, sideBandOffset);
        sideBandHalfWidth    = Mathf.Max(0f, sideBandHalfWidth);
        endClearanceZ        = Mathf.Max(0f, endClearanceZ);
        planeSize.x          = Mathf.Max(1f, planeSize.x);
        planeSize.y          = Mathf.Max(1f, planeSize.y);
        maxTiltDeg           = Mathf.Clamp(maxTiltDeg, 0f, 45f);
        pushOutMaxIterations = Mathf.Clamp(pushOutMaxIterations, 0, 32);
        pushOutStepX         = Mathf.Max(0.01f, pushOutStepX);
        minVerticalGap       = Mathf.Max(0f, minVerticalGap);
    }
}

// Assets/Scripts/CirclePopulator.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization; // for FormerlySerializedAs

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tools.Spawning
{
    public enum ScaleDistribution
    {
        UniformRange,   // single scalar in [min,max], applied to all axes
        PerAxisRange,   // each axis sampled independently in [min,max] per axis
        VariantList,    // pick from a weighted list of preset scale vectors
        Gaussian        // sample scalar (or per-axis) from Normal(mean,std), clamped to range
    }

    [Serializable]
    public class ScaleVariant
    {
        public Vector3 scale = Vector3.one;
        [Min(0f)] public float weight = 1f;
    }

    /// <summary>
    /// Randomly populates a circle around world origin (0,0,0).
    /// Works both in Editor (via buttons) and at runtime (if SpawnOnStart is true).
    /// </summary>
    [ExecuteAlways]
    public class CirclePopulator : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Prefabs to spawn randomly along the circle.")]
        public List<GameObject> prefabs = new List<GameObject>();

        [Header("Count & Radius")]
        [Min(0)] public int count = 12;
        [Min(0.01f)] public float radius = 10f;

        [Tooltip("Add thickness to the circle (0 = exact circle). Items randomize within [radius - thickness/2, radius + thickness/2].")]
        [Min(0f)] public float ringThickness = 0f;

        [Header("Placement")]
        [Tooltip("If true, choose a random Y rotation for each spawned object.")]
        public bool randomYaw = true;

        // ==== NEW: Scale controls ====
        [Header("Scale Randomization")]
        public ScaleDistribution scaleDistribution = ScaleDistribution.UniformRange;

        [Tooltip("Uniform scalar range applied to all axes (replaces old randomScale).")]
        [FormerlySerializedAs("randomScale")]
        public Vector2 uniformScale = new Vector2(1f, 1f);

        [Tooltip("Per-axis min (XYZ) for scale when using PerAxisRange.")]
        public Vector3 perAxisMin = Vector3.one;

        [Tooltip("Per-axis max (XYZ) for scale when using PerAxisRange.")]
        public Vector3 perAxisMax = Vector3.one;

        [Tooltip("Weighted preset scale vectors (used when distribution = VariantList).")]
        public List<ScaleVariant> scaleVariants = new List<ScaleVariant>();

        [Tooltip("Clamp range for Gaussian scale sampling.")]
        public Vector2 gaussianClamp = new Vector2(0.25f, 3f);
        [Tooltip("Mean for Gaussian sampling.")]
        public float gaussianMean = 1f;
        [Tooltip("Standard deviation for Gaussian sampling.")]
        [Min(0.0001f)] public float gaussianStdDev = 0.25f;
        [Tooltip("If true, sample Gaussian per axis, otherwise one scalar for all axes.")]
        public bool gaussianPerAxis = false;

        [Tooltip("Optional Perlin-based jitter multiplier applied to the chosen scale.")]
        public bool usePerlinJitter = false;
        [Tooltip("Frequency for Perlin jitter (higher = faster variation around the ring).")]
        [Min(0.0001f)] public float perlinFrequency = 1f;
        [Tooltip("Amplitude for Perlin jitter as a +/- multiplier (e.g., 0.2 => up to ±20%).")]
        [Range(0f, 1f)] public float perlinAmplitude = 0.2f;

        [Header("Ground Alignment")]
        [Tooltip("If true, cast a ray down and snap each item to ground (layerMask).")]
        public bool alignToGround = false;
        [Tooltip("Layers considered ground for alignment.")]
        public LayerMask groundMask = ~0;
        [Tooltip("Height to start the ground ray from (above world origin).")]
        public float groundRayStartHeight = 100f;
        [Tooltip("Max ray length for ground alignment.")]
        public float groundRayLength = 500f;

        [Header("Runtime")]
        [Tooltip("Spawn automatically in Play Mode (and destroy if disabled).")]
        public bool spawnOnStart = true;

        [Tooltip("Use a fixed random seed for deterministic placement.")]
        public bool useSeed = true;
        public int seed = 12345;

        [Header("Parenting")]
        [Tooltip("Optional parent for spawned instances (defaults to this transform).")]
        public Transform parentOverride;

        // Internal tracking for cleanup
        [SerializeField, HideInInspector] private string _sessionId;
        private const string SessionPrefix = "CirclePopulator_";

        private Transform TargetParent => parentOverride != null ? parentOverride : transform;

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (spawnOnStart)
            {
                ClearSpawned(); // ensure clean
                Spawn();
            }
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            ClearSpawned();
        }

        /// <summary>Spawn items along circle (or ring if thickness > 0) around world origin.</summary>
        public void Spawn()
        {
            if (prefabs == null || prefabs.Count == 0 || count <= 0)
                return;

            if (useSeed) UnityEngine.Random.InitState(seed);

            _sessionId = SessionPrefix + Guid.NewGuid().ToString("N");

            for (int i = 0; i < count; i++)
            {
                var prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
                if (prefab == null) continue;

                // Pick angle (evenly spaced with a small random nudge)
                float angle = ((float)i / Mathf.Max(1, count)) * Mathf.PI * 2f
                              + UnityEngine.Random.Range(-Mathf.Deg2Rad * 5f, Mathf.Deg2Rad * 5f);

                // Radius (ring thickness if any)
                float baseR = radius;
                if (ringThickness > 0f)
                {
                    float half = ringThickness * 0.5f;
                    baseR = UnityEngine.Random.Range(radius - half, radius + half);
                    baseR = Mathf.Max(0f, baseR);
                }

                // Position on XZ plane around world origin (0,0,0)
                Vector3 pos = new Vector3(Mathf.Cos(angle) * baseR, 0f, Mathf.Sin(angle) * baseR);

                // Optionally align to ground
                if (alignToGround)
                {
                    Vector3 rayStart = new Vector3(pos.x, groundRayStartHeight, pos.z);
                    if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
                    {
                        pos = hit.point;
                    }
                }

                // Rotation
                Quaternion rot = randomYaw
                    ? Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)
                    : Quaternion.identity;

                // ===== Scale (new versatility) =====
                Vector3 scale = SampleScale(i, angle);

                // Instantiate (Editor vs Play)
#if UNITY_EDITOR
                GameObject instance;
                if (!Application.isPlaying)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, TargetParent);
                    instance.transform.SetPositionAndRotation(pos, rot);
                    instance.transform.localScale = scale;
                    Undo.RegisterCreatedObjectUndo(instance, "Spawn Circle Item");
                }
                else
                {
                    instance = Instantiate(prefab, pos, rot, TargetParent);
                    instance.transform.localScale = scale;
                }
#else
                GameObject instance = Instantiate(prefab, pos, rot, TargetParent);
                instance.transform.localScale = scale;
#endif
                // Tag with a session marker for safe cleanup
                var marker = instance.GetComponent<SpawnMarker>();
                if (marker == null) marker = instance.AddComponent<SpawnMarker>();
                marker.sessionId = _sessionId;
            }
        }

        /// <summary>Clears only the items spawned by this component in its last session.</summary>
        public void ClearSpawned()
        {
            if (TargetParent == null) return;
            var markers = TargetParent.GetComponentsInChildren<SpawnMarker>(true);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterFullObjectHierarchyUndo(TargetParent.gameObject, "Clear Spawned Circle Items");
#endif
            foreach (var m in markers)
            {
                if (!string.IsNullOrEmpty(_sessionId) && m.sessionId == _sessionId)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) Undo.DestroyObjectImmediate(m.gameObject);
                    else Destroy(m.gameObject);
#else
                    Destroy(m.gameObject);
#endif
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw the circle (and ring bounds if any) around world origin.
            Gizmos.matrix = Matrix4x4.identity; // world space
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.8f);
            DrawWireCircle(Vector3.zero, radius);

            if (ringThickness > 0f)
            {
                float half = ringThickness * 0.5f;
                Gizmos.color = new Color(0f, 0.7f, 1f, 0.35f);
                DrawWireCircle(Vector3.zero, Mathf.Max(0f, radius - half));
                DrawWireCircle(Vector3.zero, Mathf.Max(0f, radius + half));
            }
        }

        private void DrawWireCircle(Vector3 center, float r, int segments = 64)
        {
            if (r <= 0f) return;
            Vector3 prev = center + new Vector3(r, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float t = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(t) * r, 0f, Mathf.Sin(t) * r);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        // ===== Helpers =====

        private Vector3 SampleScale(int index, float angleRad)
        {
            Vector3 scale;

            switch (scaleDistribution)
            {
                default:
                case ScaleDistribution.UniformRange:
                {
                    float s = UnityEngine.Random.Range(uniformScale.x, uniformScale.y);
                    scale = Vector3.one * Mathf.Max(0.0001f, s);
                    break;
                }
                case ScaleDistribution.PerAxisRange:
                {
                    float sx = UnityEngine.Random.Range(perAxisMin.x, perAxisMax.x);
                    float sy = UnityEngine.Random.Range(perAxisMin.y, perAxisMax.y);
                    float sz = UnityEngine.Random.Range(perAxisMin.z, perAxisMax.z);
                    scale = new Vector3(
                        Mathf.Max(0.0001f, sx),
                        Mathf.Max(0.0001f, sy),
                        Mathf.Max(0.0001f, sz)
                    );
                    break;
                }
                case ScaleDistribution.VariantList:
                {
                    scale = PickWeightedVariant();
                    break;
                }
                case ScaleDistribution.Gaussian:
                {
                    if (gaussianPerAxis)
                    {
                        float sx = Clamp(NextGaussian(gaussianMean, gaussianStdDev), gaussianClamp);
                        float sy = Clamp(NextGaussian(gaussianMean, gaussianStdDev), gaussianClamp);
                        float sz = Clamp(NextGaussian(gaussianMean, gaussianStdDev), gaussianClamp);
                        scale = new Vector3(sx, sy, sz);
                    }
                    else
                    {
                        float s = Clamp(NextGaussian(gaussianMean, gaussianStdDev), gaussianClamp);
                        scale = Vector3.one * s;
                    }
                    break;
                }
            }

            if (usePerlinJitter)
            {
                // Use ring angle + index to produce stable variation with seed
                float t = angleRad * perlinFrequency + (useSeed ? seed * 0.123f : UnityEngine.Random.value);
                float nX = Mathf.PerlinNoise(t, 0.37f);
                float nY = Mathf.PerlinNoise(t, 1.91f);
                float nZ = Mathf.PerlinNoise(t, 4.42f);
                // Map [0,1] -> [1 - A, 1 + A]
                float jx = 1f + (nX - 0.5f) * 2f * perlinAmplitude;
                float jy = 1f + (nY - 0.5f) * 2f * perlinAmplitude;
                float jz = 1f + (nZ - 0.5f) * 2f * perlinAmplitude;
                scale = new Vector3(
                    Mathf.Max(0.0001f, scale.x * jx),
                    Mathf.Max(0.0001f, scale.y * jy),
                    Mathf.Max(0.0001f, scale.z * jz)
                );
            }

            return scale;
        }

        private Vector3 PickWeightedVariant()
        {
            if (scaleVariants == null || scaleVariants.Count == 0)
                return Vector3.one;

            float total = 0f;
            foreach (var v in scaleVariants) total += Mathf.Max(0f, v.weight);
            if (total <= 0f) return scaleVariants[0].scale;

            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            foreach (var v in scaleVariants)
            {
                acc += Mathf.Max(0f, v.weight);
                if (r <= acc) return SafeScale(v.scale);
            }
            return SafeScale(scaleVariants[scaleVariants.Count - 1].scale);
        }

        private static Vector3 SafeScale(Vector3 s)
        {
            return new Vector3(
                Mathf.Max(0.0001f, s.x),
                Mathf.Max(0.0001f, s.y),
                Mathf.Max(0.0001f, s.z)
            );
        }

        // Box-Muller transform to sample a standard normal
        private static float NextGaussian(float mean, float stdDev)
        {
            // Two uniform(0,1] -> normal(0,1)
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = 1f - UnityEngine.Random.value;
            float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        private static float Clamp(float v, Vector2 clampRange)
        {
            return Mathf.Clamp(v, Mathf.Min(clampRange.x, clampRange.y), Mathf.Max(clampRange.x, clampRange.y));
        }
    }

    /// <summary>Marker used to identify objects spawned by a given session for cleanup.</summary>
    public class SpawnMarker : MonoBehaviour
    {
        public string sessionId;
    }
}

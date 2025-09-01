using UnityEngine;

public class BridgePlatform : IBridgeComponent
{
    public GameObject GameObject { get; private set; }
    public Vector3 Position => GameObject?.transform.position ?? Vector3.zero;

    public void Initialize(BridgeConfiguration config, Transform parent)
    {
        GameObject = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (GameObject == null)
        {
            Debug.LogError("Failed to create GameObject for BridgePlatform");
            return;
        }
        
        GameObject.transform.SetParent(parent, false);
        GameObject.transform.localScale = new Vector3(config.platformWidth, config.platformThickness, config.platformLength);
        
        ApplyMaterial(config.platformMaterial);
        AddRailings(config);
        AddExtendedCollider(config);
    }

    public void ApplyPhysics(BridgeConfiguration config)
    {
        if (GameObject == null)
        {
            Debug.LogError("GameObject is null in BridgePlatform.ApplyPhysics");
            return;
        }

        var rb = GameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = config.platformMass;
        rb.useGravity = false; // Keep platforms stable
        rb.isKinematic = true; // Keep platforms kinematic for stability
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void ApplyMaterial(Material material)
    {
        if (material != null)
        {
            var renderer = GameObject.GetComponent<Renderer>();
            if (renderer != null) renderer.material = material;
        }
    }

    private void AddRailings(BridgeConfiguration config)
    {
        CreateRailing("LeftRailing", new Vector3(-config.platformWidth * 0.4f, config.platformThickness, 0), config);
        CreateRailing("RightRailing", new Vector3(config.platformWidth * 0.4f, config.platformThickness, 0), config);
    }

    private void CreateRailing(string name, Vector3 position, BridgeConfiguration config)
    {
        var railing = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        railing.name = name;
        railing.transform.SetParent(GameObject.transform, false);
        railing.transform.localPosition = position;
        railing.transform.localScale = new Vector3(0.05f, 1f, config.platformLength * 0.8f);

        var collider = railing.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

        if (config.anchorMaterial != null)
        {
            var renderer = railing.GetComponent<Renderer>();
            if (renderer != null) renderer.material = config.anchorMaterial;
        }
    }

    private void AddExtendedCollider(BridgeConfiguration config)
    {
        var edgeCollider = GameObject.AddComponent<BoxCollider>();
        edgeCollider.size = new Vector3(config.platformWidth * 1.1f, config.platformThickness * 3, config.platformLength * 1.1f);
        edgeCollider.center = new Vector3(0, config.platformThickness, 0);
    }
}
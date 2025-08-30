using UnityEngine;

public class BridgePlank : IBridgeComponent
{
    public GameObject GameObject { get; private set; }
    public Vector3 Position => GameObject?.transform.position ?? Vector3.zero;

    public void Initialize(BridgeConfiguration config, Transform parent)
    {
        GameObject = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (GameObject == null)
        {
            Debug.LogError("Failed to create GameObject for BridgePlank");
            return;
        }
        
        GameObject.transform.SetParent(parent, false);
        GameObject.transform.localScale = new Vector3(config.plankWidth, config.plankThickness, config.PlankLength);
        
        ApplyMaterial(config.plankMaterial);
        AddEdgeColliders(config);
    }

    public void ApplyPhysics(BridgeConfiguration config)
    {
        if (GameObject == null)
        {
            Debug.LogError("GameObject is null in BridgePlank.ApplyPhysics");
            return;
        }

        var rb = GameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = config.plankMass;
        rb.useGravity = true;
        rb.isKinematic = false;
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

    private void AddEdgeColliders(BridgeConfiguration config)
    {
        CreateEdgeCollider("FrontEdge", new Vector3(0, 0, config.PlankLength * 0.4f), config);
        CreateEdgeCollider("BackEdge", new Vector3(0, 0, -config.PlankLength * 0.4f), config);
    }

    private void CreateEdgeCollider(string name, Vector3 position, BridgeConfiguration config)
    {
        var edge = new UnityEngine.GameObject(name);
        edge.transform.SetParent(GameObject.transform, false);
        edge.transform.localPosition = position;
        
        var collider = edge.AddComponent<BoxCollider>();
        collider.size = new Vector3(config.plankWidth, config.plankThickness * 2, config.PlankLength * 0.2f);
    }
}
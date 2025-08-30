using UnityEngine;

public class BridgeAnchor : IBridgeComponent
{
    public GameObject GameObject { get; private set; }
    public Vector3 Position => GameObject?.transform.position ?? Vector3.zero;

    public void Initialize(BridgeConfiguration config, Transform parent)
    {
        GameObject = new UnityEngine.GameObject();
        if (GameObject == null)
        {
            Debug.LogError("Failed to create GameObject for BridgeAnchor");
            return;
        }
        
        GameObject.transform.SetParent(parent, false);
        CreateVisual(config);
    }

    public void ApplyPhysics(BridgeConfiguration config)
    {
        if (GameObject == null)
        {
            Debug.LogError("GameObject is null in BridgeAnchor.ApplyPhysics");
            return;
        }

        var rb = GameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GameObject.AddComponent<Rigidbody>();
        }
        
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void CreateVisual(BridgeConfiguration config)
    {
        var visual = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(GameObject.transform, false);
        visual.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        visual.transform.localPosition = Vector3.up * 0.25f;

        if (config.anchorMaterial != null)
        {
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null) renderer.material = config.anchorMaterial;
        }
    }
}

public class ImprovedBridgeAnchor : IBridgeComponent
{
    public GameObject GameObject { get; private set; }
    public Vector3 Position => GameObject?.transform.position ?? Vector3.zero;

    public void Initialize(BridgeConfiguration config, Transform parent)
    {
        GameObject = new UnityEngine.GameObject();
        if (GameObject == null)
        {
            Debug.LogError("Failed to create GameObject for BridgeAnchor");
            return;
        }
        
        GameObject.transform.SetParent(parent, false);
        CreateVisual(config);
    }

    public void ApplyPhysics(BridgeConfiguration config)
    {
        if (GameObject == null)
        {
            Debug.LogError("GameObject is null in BridgeAnchor.ApplyPhysics");
            return;
        }

        var rb = GameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GameObject.AddComponent<Rigidbody>();
        }
        
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void CreateVisual(BridgeConfiguration config)
    {
        // OPTION 1: Make anchors much smaller and positioned underground
        var visual = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(GameObject.transform, false);
        
        // Make much smaller and position underground
        visual.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f); // Much smaller
        visual.transform.localPosition = Vector3.down * 0.5f; // Underground
        
        // Make them dark/invisible
        if (config.anchorMaterial != null)
        {
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null) 
            {
                renderer.material = config.anchorMaterial;
            }
        }
        else
        {
            // Make them nearly invisible if no material assigned
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.1f, 0.1f, 0.1f, 0.1f); // Very dark and transparent
                renderer.material = material;
            }
        }
    }
}
public class InvisibleBridgeAnchor : IBridgeComponent
{
    public GameObject GameObject { get; private set; }
    public Vector3 Position => GameObject?.transform.position ?? Vector3.zero;

    public void Initialize(BridgeConfiguration config, Transform parent)
    {
        // Create empty GameObject - no visual at all
        GameObject = new UnityEngine.GameObject();
        GameObject.transform.SetParent(parent, false);
        
        // Add a small invisible collider for connection purposes only
        var collider = GameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.1f, 0.1f, 0.1f);
        collider.isTrigger = true; // Non-physical, just for connection reference
    }

    public void ApplyPhysics(BridgeConfiguration config)
    {
        var rb = GameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}
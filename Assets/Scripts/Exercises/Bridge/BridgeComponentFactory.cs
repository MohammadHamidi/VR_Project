using UnityEngine;

/// <summary>
/// Consolidated factory for creating all bridge components.
/// Replaces separate PlankFactory, PlatformFactory, and AnchorFactory.
/// </summary>
public class BridgeComponentFactory
{
    /// <summary>
    /// Creates a bridge plank with the specified configuration
    /// </summary>
    public GameObject CreatePlank(string name, Vector3 position, BridgeConfig config, Transform parent)
    {
        GameObject plank = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (plank == null)
        {
            Debug.LogError($"Failed to create plank: {name}");
            return null;
        }

        plank.name = name;
        plank.transform.SetParent(parent, false);
        plank.transform.localPosition = position;
        plank.transform.localScale = new Vector3(config.plankWidth, config.plankThickness, config.PlankLength);

        // Apply material
        if (config.plankMaterial != null)
        {
            var renderer = plank.GetComponent<Renderer>();
            if (renderer != null) renderer.material = config.plankMaterial;
        }

        // Add physics
        var rb = plank.AddComponent<Rigidbody>();
        rb.mass = config.plankMass;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Add edge colliders for better collision detection
        CreateEdgeCollider(plank, "FrontEdge", new Vector3(0, 0, config.PlankLength * 0.4f), config);
        CreateEdgeCollider(plank, "BackEdge", new Vector3(0, 0, -config.PlankLength * 0.4f), config);

        return plank;
    }

    /// <summary>
    /// Creates a bridge platform with the specified configuration
    /// </summary>
    public GameObject CreatePlatform(string name, Vector3 position, BridgeConfig config, Transform parent)
    {
        GameObject platform = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (platform == null)
        {
            Debug.LogError($"Failed to create platform: {name}");
            return null;
        }

        platform.name = name;
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = position;
        platform.transform.localScale = new Vector3(config.platformWidth, config.platformThickness, config.platformLength);

        // Apply material
        if (config.platformMaterial != null)
        {
            var renderer = platform.GetComponent<Renderer>();
            if (renderer != null) renderer.material = config.platformMaterial;
        }

        // Add physics
        var rb = platform.AddComponent<Rigidbody>();
        rb.mass = config.platformMass;
        rb.useGravity = false; // Keep platforms stable
        rb.isKinematic = true; // Keep platforms kinematic for stability
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Add railings
        CreateRailing(platform, "LeftRailing", new Vector3(-config.platformWidth * 0.4f, config.platformThickness, 0), config);
        CreateRailing(platform, "RightRailing", new Vector3(config.platformWidth * 0.4f, config.platformThickness, 0), config);

        // Add extended collider
        var edgeCollider = platform.AddComponent<BoxCollider>();
        edgeCollider.size = new Vector3(config.platformWidth * 1.1f, config.platformThickness * 3, config.platformLength * 1.1f);
        edgeCollider.center = new Vector3(0, config.platformThickness, 0);

        return platform;
    }

    /// <summary>
    /// Creates a bridge anchor with the specified configuration and type
    /// </summary>
    public GameObject CreateAnchor(string name, Vector3 position, BridgeConfig config, Transform parent, AnchorType anchorType = AnchorType.Improved)
    {
        GameObject anchor = new GameObject(name);
        if (anchor == null)
        {
            Debug.LogError($"Failed to create anchor: {name}");
            return null;
        }

        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = position;

        // Create visual based on anchor type
        switch (anchorType)
        {
            case AnchorType.Standard:
                CreateStandardAnchorVisual(anchor, config);
                break;
            case AnchorType.Invisible:
                CreateInvisibleAnchor(anchor);
                break;
            default: // Improved or fallback
                CreateImprovedAnchorVisual(anchor, config);
                break;
        }

        // Add physics
        var rb = anchor.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        return anchor;
    }

    private void CreateStandardAnchorVisual(GameObject anchor, BridgeConfig config)
    {
        var visual = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(anchor.transform, false);
        visual.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        visual.transform.localPosition = Vector3.up * 0.25f;

        if (config.anchorMaterial != null)
        {
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null) renderer.material = config.anchorMaterial;
        }
    }

    private void CreateImprovedAnchorVisual(GameObject anchor, BridgeConfig config)
    {
        var visual = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(anchor.transform, false);

        // Make smaller and position underground
        visual.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
        visual.transform.localPosition = Vector3.down * 0.5f;

        if (config.anchorMaterial != null)
        {
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null) renderer.material = config.anchorMaterial;
        }
        else
        {
            // Make nearly invisible if no material assigned
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.1f, 0.1f, 0.1f, 0.1f); // Very dark and transparent
                renderer.material = material;
            }
        }
    }

    private void CreateInvisibleAnchor(GameObject anchor)
    {
        // Add a small invisible collider for connection purposes only
        var collider = anchor.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.1f, 0.1f, 0.1f);
        collider.isTrigger = true; // Non-physical, just for connection reference
    }

    private void CreateEdgeCollider(GameObject plank, string name, Vector3 position, BridgeConfig config)
    {
        var edge = new GameObject(name);
        edge.transform.SetParent(plank.transform, false);
        edge.transform.localPosition = position;

        var collider = edge.AddComponent<BoxCollider>();
        collider.size = new Vector3(config.plankWidth, config.plankThickness * 2, config.PlankLength * 0.2f);
    }

    private void CreateRailing(GameObject platform, string name, Vector3 position, BridgeConfig config)
    {
        var railing = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
        railing.name = name;
        railing.transform.SetParent(platform.transform, false);
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
}

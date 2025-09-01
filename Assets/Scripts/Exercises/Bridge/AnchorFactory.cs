using UnityEngine;

public class AnchorFactory : IBridgeComponentFactory<IBridgeComponent>
{
    public enum AnchorType { Standard, Improved, Invisible }
    private AnchorType anchorType;

    public AnchorFactory(AnchorType type = AnchorType.Improved)
    {
        anchorType = type;
    }

    public IBridgeComponent Create(string name, Vector3 position, BridgeConfiguration config, Transform parent)
    {
        IBridgeComponent anchor = anchorType switch
        {
            AnchorType.Improved => new ImprovedBridgeAnchor(),
            AnchorType.Invisible => new InvisibleBridgeAnchor(),
            _ => new BridgeAnchor() // Original
        };

        anchor.Initialize(config, parent);
        
        if (anchor.GameObject == null)
        {
            Debug.LogError($"Failed to create anchor: {name}");
            return null;
        }
        
        anchor.GameObject.name = name;
        anchor.GameObject.transform.localPosition = position;
        anchor.ApplyPhysics(config);
        return anchor;
    }
}

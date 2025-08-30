using UnityEngine;

public interface IPhysicsConnector
{
    void ConnectComponents(IBridgeComponent componentA, IBridgeComponent componentB, BridgeConfiguration config);
    void ConnectToAnchor(IBridgeComponent component, IBridgeComponent anchor, Vector3 anchorPoint, BridgeConfiguration config);
}
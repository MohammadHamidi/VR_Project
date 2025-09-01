using UnityEngine;

public interface IBridgeComponent
{
    GameObject GameObject { get; }
    Vector3 Position { get; }
    void Initialize(BridgeConfiguration config, Transform parent);
    void ApplyPhysics(BridgeConfiguration config);
}
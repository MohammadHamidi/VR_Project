using UnityEngine;

public class PlatformFactory : IBridgeComponentFactory<BridgePlatform>
{
    public BridgePlatform Create(string name, Vector3 position, BridgeConfiguration config, Transform parent)
    {
        var platform = new BridgePlatform();
        platform.Initialize(config, parent);
        
        if (platform.GameObject == null)
        {
            Debug.LogError($"Failed to create platform: {name}");
            return null;
        }
        
        platform.GameObject.name = name;
        platform.GameObject.transform.localPosition = position;
        platform.ApplyPhysics(config);
        return platform;
    }
}

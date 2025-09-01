using UnityEngine;

public class PlankFactory : IBridgeComponentFactory<BridgePlank>
{
    public BridgePlank Create(string name, Vector3 position, BridgeConfiguration config, Transform parent)
    {
        var plank = new BridgePlank();
        plank.Initialize(config, parent);
        
        if (plank.GameObject == null)
        {
            Debug.LogError($"Failed to create plank: {name}");
            return null;
        }
        
        plank.GameObject.name = name;
        plank.GameObject.transform.localPosition = position;
        plank.ApplyPhysics(config);
        return plank;
    }
}

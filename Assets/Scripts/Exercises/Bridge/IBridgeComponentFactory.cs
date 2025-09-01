using UnityEngine;

public interface IBridgeComponentFactory<T> where T : IBridgeComponent
{
    T Create(string name, Vector3 position, BridgeConfiguration config, Transform parent);
}
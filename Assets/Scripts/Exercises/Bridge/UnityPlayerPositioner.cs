using UnityEngine;

public class UnityPlayerPositioner : IPlayerPositioner
{
    public bool PositionPlayer(Vector3 targetPosition, Vector3 spawnOffset)
    {
        var xrOrigin = FindXROrigin();
        if (xrOrigin != null)
        {
            Vector3 finalPosition = targetPosition + spawnOffset;
            xrOrigin.transform.position = finalPosition;
            xrOrigin.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            
            Debug.Log($"Player positioned at {finalPosition}");
            return true;
        }
        
        Debug.LogWarning("Could not find XR Origin to position player.");
        return false;
    }

    private Transform FindXROrigin()
    {
        var xrOriginComponent = UnityEngine.Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOriginComponent != null) return xrOriginComponent.transform;

        string[] possibleNames = { "XR Origin", "XRRig", "XR Rig", "VRPlayer", "Player" };
        foreach (string name in possibleNames)
        {
            var obj = UnityEngine.GameObject.Find(name);
            if (obj != null) return obj.transform;
        }

        return null;
    }
}
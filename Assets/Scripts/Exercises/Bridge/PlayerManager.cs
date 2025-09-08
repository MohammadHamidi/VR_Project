using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Manages player positioning and teleportation for the bridge exercise.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Transform playerTransform;

    private void Awake()
    {
        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>();

        if (playerTransform == null)
        {
            if (xrOrigin != null && xrOrigin.Camera != null)
                playerTransform = xrOrigin.Camera.transform; // use the XR camera
            else if (Camera.main != null)
                playerTransform = Camera.main.transform;
        }
    }

    /// <summary>
    /// Teleports the player with a specific rotation and optional offset.
    /// </summary>
    public bool TeleportPlayer(Vector3 targetPosition, Quaternion targetRotation, Vector3 offset = default)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("PlayerManager: No player transform found!");
            return false;
        }

        Vector3 finalPosition = targetPosition + offset;
        playerTransform.SetPositionAndRotation(finalPosition, targetRotation);
        Debug.Log($"Player teleported to {finalPosition}, rot={targetRotation.eulerAngles}");
        return true;
    }

    /// <summary>
    /// Backward compatible: teleports the player facing world forward.
    /// </summary>
    public bool TeleportPlayer(Vector3 targetPosition, Vector3 offset = default)
    {
        return TeleportPlayer(targetPosition, Quaternion.LookRotation(Vector3.forward, Vector3.up), offset);
    }

    /// <summary>
    /// Positions the player at the start platform, facing towards 'lookAt' (usually end platform).
    /// </summary>
    public bool PositionAtBridgeStart(GameObject startPlatform, Transform lookAt = null, Vector3 offset = default)
    {
        if (startPlatform == null)
        {
            Debug.LogWarning("PlayerManager: Start platform is null!");
            return false;
        }

        Vector3 forward = Vector3.forward;
        if (lookAt != null)
        {
            Vector3 dir = lookAt.position - startPlatform.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) forward = dir.normalized;
        }
        else
        {
            Vector3 dir = startPlatform.transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) forward = dir.normalized;
        }

        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        return TeleportPlayer(startPlatform.transform.position, rot, offset);
    }

    public Vector3 GetPlayerPosition() =>
        playerTransform != null ? playerTransform.position : Vector3.zero;

    public void SetPlayerTransform(Transform transform) => playerTransform = transform;

    public void SetXROrigin(XROrigin xr)
    {
        xrOrigin = xr;
        if (playerTransform == null && xrOrigin != null && xrOrigin.Camera != null)
            playerTransform = xrOrigin.Camera.transform;
    }
}

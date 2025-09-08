using UnityEngine;
using UnityEngine.Events;
using Unity.XR.CoreUtils;

/// <summary>
/// Detects when the player falls and triggers UnityEvents (robust XR-friendly checks).
/// </summary>
public class PlayFallDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask playerLayers = ~0;   // optional layer filter
    [SerializeField] private bool detectXRCamera = true;

    [Header("Events")]
    public UnityEvent OnPlayerFall;

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            OnPlayerFall?.Invoke();
        }
    }

    private bool IsPlayer(Collider col)
    {
        // Layer match
        if (((1 << col.gameObject.layer) & playerLayers) != 0)
            return true;

        // Common tags
        if (col.CompareTag("Player") || col.CompareTag("MainCamera"))
            return true;

        // XR origin / camera
        if (detectXRCamera && col.GetComponentInParent<XROrigin>() != null)
            return true;

        return false;
    }
}
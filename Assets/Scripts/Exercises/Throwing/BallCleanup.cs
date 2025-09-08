using UnityEngine;

/// <summary>
/// Component that handles ball cleanup notifications when the ball is destroyed.
/// Used by BallSpawner to track ball instances.
/// </summary>
public class BallCleanup : MonoBehaviour
{
    public System.Action OnBallDestroyed;

    void OnDestroy()
    {
        OnBallDestroyed?.Invoke();
    }
}

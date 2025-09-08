using UnityEngine;

public class BallRespawnZone : MonoBehaviour
{
    [Tooltip("Link to your BallSpawner")]
    public BallSpawner spawner;

    [Tooltip("Delay before destroying the fallen ball (allows for visual feedback)")]
    public float destroyDelay = 0.5f;

    [Tooltip("Spawn replacement immediately when ball enters zone")]
    public bool spawnImmediately = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Throwable"))
            return;

        // Validate spawner reference
        if (spawner == null)
        {
            Debug.LogError("BallRespawnZone: Spawner reference is null!");
            return;
        }

        // Check if this is the currently tracked ball
        if (spawner.GetCurrentBall() == other.gameObject)
        {
            HandleBallRespawn(other.gameObject);
        }
        else
        {
            // It's not the current ball, just destroy it immediately
            Debug.LogWarning("BallRespawnZone: Destroying untracked ball");
            Destroy(other.gameObject);
        }
    }

    private void HandleBallRespawn(GameObject fallenBall)
    {
        if (spawnImmediately)
        {
            // Spawn replacement immediately to eliminate gap
            spawner.SpawnBall();

            // Destroy the fallen ball after a brief delay for visual feedback
            Destroy(fallenBall, destroyDelay);
        }
        else
        {
            // Use the original delayed respawn method
            Destroy(fallenBall);
            spawner.RespawnAfterDelay();
        }
    }
}
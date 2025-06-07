using UnityEngine;

public class BallRespawnZone : MonoBehaviour
{
    [Tooltip("Link to your BallSpawner")]
    public BallSpawner spawner;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Throwable"))
            return;

        // Destroy the fallen ball
        Destroy(other.gameObject);

        // Spawn a replacement after delay
        if (spawner != null)
            spawner.RespawnAfterDelay();
    }
}
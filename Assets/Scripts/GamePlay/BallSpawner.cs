using System.Collections;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Tooltip("OrangeBall prefab")]
    public GameObject ballPrefab;
    [Tooltip("Where to spawn each ball")]
    public Transform spawnPoint;
    [Tooltip("Seconds to wait before respawning (when ball falls)")]
    public float respawnDelay = 1f;

    void Start()
    {
        SpawnBall();
    }

    /// <summary>
    /// Instantiates a new ball at the spawnPoint and hooks up its GrabRespawner.
    /// </summary>
    public void SpawnBall()
    {
        GameObject ball = Instantiate(
            ballPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Assign this spawner to its GrabRespawner so on-grab it can spawn the next
        var respawner = ball.GetComponent<GrabRespawner>();
        if (respawner == null)
            respawner = ball.AddComponent<GrabRespawner>();

        respawner.spawner = this;
    }

    /// <summary>
    /// Call to spawn after a delay (used by the respawn zone).
    /// </summary>
    public void RespawnAfterDelay()
    {
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnBall();
    }
}
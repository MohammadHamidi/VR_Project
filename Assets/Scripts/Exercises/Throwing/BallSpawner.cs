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

    [Header("Ball Limits")]
    [Tooltip("Maximum number of balls allowed in scene (0 = unlimited)")]
    public int maxBallsInScene = 3;

    [Tooltip("Automatically clean up orphaned balls on spawn")]
    public bool autoCleanupOrphanedBalls = true;

    // Ball tracking
    private GameObject currentBall;
    private bool isRespawning = false;
    private static int totalBallCount = 0;

    void Start()
    {
        SpawnBall();
    }

    /// <summary>
    /// Instantiates a new ball at the spawnPoint and hooks up its GrabRespawner.
    /// Prevents multiple balls from existing simultaneously.
    /// </summary>
    public void SpawnBall()
    {
        // Check ball limits
        if (maxBallsInScene > 0 && totalBallCount >= maxBallsInScene)
        {
            Debug.LogWarning($"BallSpawner: Cannot spawn ball. Maximum ball limit ({maxBallsInScene}) reached.");
            return;
        }

        // Prevent multiple balls
        if (currentBall != null)
        {
            Debug.LogWarning("BallSpawner: Attempted to spawn ball while one already exists. Ignoring.");
            return;
        }

        if (ballPrefab == null)
        {
            Debug.LogError("BallSpawner: ballPrefab is null! Cannot spawn ball.");
            return;
        }

        // Auto-cleanup orphaned balls if enabled
        if (autoCleanupOrphanedBalls)
        {
            CleanupOrphanedBalls();
        }

        GameObject ball = Instantiate(
            ballPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        currentBall = ball;
        isRespawning = false;
        totalBallCount++;

        // Assign this spawner to its GrabRespawner so on-grab it can spawn the next
        var respawner = ball.GetComponent<GrabRespawner>();
        if (respawner == null)
        {
            Debug.LogWarning("BallSpawner: Ball prefab missing GrabRespawner component. Adding one dynamically.");
            respawner = ball.AddComponent<GrabRespawner>();
        }

        respawner.spawner = this;

        // Hook up cleanup when ball is destroyed
        var ballCleanup = ball.GetComponent<BallCleanup>() ?? ball.AddComponent<BallCleanup>();
        ballCleanup.OnBallDestroyed += HandleBallDestroyed;
    }

    /// <summary>
    /// Call to spawn after a delay (used by the respawn zone).
    /// </summary>
    public void RespawnAfterDelay()
    {
        if (isRespawning)
        {
            Debug.LogWarning("BallSpawner: Respawn already in progress. Ignoring.");
            return;
        }

        isRespawning = true;
        StartCoroutine(RespawnRoutine());
    }

    /// <summary>
    /// Gets the currently active ball (null if none exists)
    /// </summary>
    public GameObject GetCurrentBall()
    {
        return currentBall;
    }

    /// <summary>
    /// Destroys the current ball if it exists
    /// </summary>
    public void DestroyCurrentBall()
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
            // HandleBallDestroyed will be called automatically
        }
    }

    private void HandleBallDestroyed()
    {
        currentBall = null;
        isRespawning = false;
        totalBallCount = Mathf.Max(0, totalBallCount - 1);
    }

    /// <summary>
    /// Cleans up balls that exist in the scene but are not properly tracked
    /// </summary>
    private void CleanupOrphanedBalls()
    {
        GameObject[] allBalls = GameObject.FindGameObjectsWithTag("Throwable");
        int orphanedCount = 0;

        foreach (var ball in allBalls)
        {
            // Check if this ball has a BallCleanup component (indicating it's tracked)
            var cleanup = ball.GetComponent<BallCleanup>();
            if (cleanup == null)
            {
                Destroy(ball);
                orphanedCount++;
            }
        }

        if (orphanedCount > 0)
        {
            Debug.Log($"BallSpawner: Cleaned up {orphanedCount} orphaned balls.");
        }
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnBall();
    }

    void OnDestroy()
    {
        // Clean up any remaining ball
        if (currentBall != null)
        {
            Destroy(currentBall);
            totalBallCount = Mathf.Max(0, totalBallCount - 1);
        }
    }

    /// <summary>
    /// Gets the total number of balls currently in the scene
    /// </summary>
    public static int GetTotalBallCount()
    {
        return totalBallCount;
    }
}
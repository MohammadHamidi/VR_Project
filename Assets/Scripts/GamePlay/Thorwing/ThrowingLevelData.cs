// Assets/Scripts/ThrowingLevelData.cs
using System.Collections.Generic;
using UnityEngine;




[CreateAssetMenu(fileName = "ThrowingLevelData", menuName = "Game/Throwing Level Data")]
public class ThrowingLevelData : ScriptableObject
{
    [Header("XR Settings")]
    [Tooltip("Prefab containing your XR Origin / Interaction Manager, etc.")]
    public GameObject xrRigPrefab;

    [Header("UI Settings")]
    [Tooltip("Prefab containing your StageUIController (e.g. Canvas + UI scripts)")]
    public GameObject uiControllerPrefab;

    [Header("Environment Settings")]
    [Tooltip("List of environment prefabs (e.g. ground plane, surrounding meshes). Instantiate all and activate one by default.")]
    public List<GameObject> environmentPrefabs = new List<GameObject>();

    [Tooltip("Which environment prefab (by index) should be active at level start.")]
    public int defaultEnvironmentIndex = 0;

    [Header("Ball Settings")]
    [Tooltip("Prefab for the throwable ball (should contain Rigidbody, HoverAndRelease, GrabRespawner, XRGrabInteractable, etc.)")]
    public GameObject ballPrefab;

    [Tooltip("World position where the ball spawns at start")]
    public Vector3 ballSpawnPosition = Vector3.zero;

    [Tooltip("Seconds to wait before respawning a lost ball")]
    public float respawnDelay = 1f;

    [Header("Respawn Zone")]
    [Tooltip("Prefab for the respawn kill-plane or trigger zone (must have BallRespawnZone attached)")]
    public GameObject respawnZonePrefab;

    [Tooltip("World position of the respawn zone")]
    public Vector3 respawnZonePosition = Vector3.zero;

    [Header("Target Rings")]
    [Tooltip("Prefab used for each TargetRing (must have TargetRing component and a trigger collider)")]
    public GameObject ringPrefab;

    [Tooltip("List of world positions where each ring should be instantiated")]
    public List<Vector3> ringPositions = new List<Vector3>();

    [Header("Stage Settings")]
    [Tooltip("Time (in seconds) allotted to hit all rings")]
    public float stageTime = 60f;

    [Tooltip("If true, the countdown timer is enabled; otherwise no timer")]
    public bool enableTimer = true;
}

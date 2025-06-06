using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TargetLevel", menuName = "VR Fitness/Target Level")]
public class TargetLevelData : ScriptableObject
{
    [Header("Basic Configuration")]
    public string levelName = "New Target Level";
    public string description = "Ball throwing challenge";
    public float timeLimit = 60f;
    public int difficultyRating = 1;

    [Header("Target Configuration")]
    public List<TargetRingSetup> targetRings = new List<TargetRingSetup>();
    public int requiredHits = 5;
    public bool requireAllTargets = true;

    [Header("Ball Physics")]
    public float ballRespawnDelay = 1f;
    public Vector3 ballSpawnPosition = Vector3.zero;
    public int maxActiveBalls = 3;

    [Header("Scoring")]
    public int pointsPerHit = 100;
    public int timeBonus = 10;
    public int accuracyBonus = 50;

    [System.Serializable]
    public class TargetRingSetup
    {
        public Vector3 position;
        public Vector3 rotation;
        public float scale = 1f;
        public Color ringColor = Color.red;
        public bool isMoving = false;
        public Vector3 movementRange = Vector3.zero;
        public float movementSpeed = 1f;
        public int pointValue = 100;
    }
}
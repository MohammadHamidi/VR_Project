using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CombatLevel", menuName = "VR Fitness/Combat Level")]
public class CombatLevelData : ScriptableObject
{
    [Header("Basic Configuration")]
    public string levelName = "New Combat Level";
    public string description = "Squat and dodge challenge";
    public float levelDuration = 120f;
    public int difficultyRating = 1;

    [Header("Spawn Configuration")]
    public float baseSpawnInterval = 2f;
    public float minSpawnInterval = 0.5f;
    public float difficultyIncrement = 0.05f;
    public int maxSimultaneousCubes = 5;

    [Header("Cube Properties")]
    public float cubeSpeed = 2f;
    public float cubeSize = 1f;
    public Vector3 spawnOffset = new Vector3(0, 1, 10);
    public float destroyZ = -5f;

    [Header("Dodge Mechanics")]
    public float dodgeZoneStart = 3f;
    public float dodgeZoneEnd = 1f;
    public float squatThreshold = 0.3f;
    public float dodgeDuration = 0.5f;

    [Header("Scoring")]
    public int basePointsPerCube = 10;
    public int bonusMultiplier = 2;
    public int coinValue = 5;

    [Header("Spawn Patterns")]
    public List<CubeSpawnWave> spawnWaves = new List<CubeSpawnWave>();

    [System.Serializable]
    public class CubeSpawnWave
    {
        public float startTime;
        public float duration;
        public int[] spawnLanes; // Which lanes to spawn in
        public float customSpawnRate;
        public bool isBossWave;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CombatSystem.Pooling;
using CombatSystem.Events;
using CombatSystem.Obstacles;
using CombatSystem.Collectibles;

namespace CombatSystem.Management
{
    public class CombatStageManager : MonoBehaviour
    {
        public static CombatStageManager Instance { get; private set; }

        [Header("Prefab References")]
        [SerializeField] private CubeMover cubePrefab;
        [SerializeField] private CoinPickup coinPrefab;

        [Header("Spawn Configuration")]
        [SerializeField] private Transform[] spawnRails;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0, 1, 10);
        [SerializeField] private float baseSpawnInterval = 2f;
        [SerializeField] private float difficultyIncrement = 0.05f;
        [SerializeField] private float minSpawnInterval = 0.5f;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI debugText;

        [Header("Performance Settings")]
        [SerializeField] private int cubePoolSize = 20;
        [SerializeField] private int coinPoolSize = 15;

        // Object Pools
        private ObjectPool<CubeMover> _cubePool;
        private ObjectPool<CoinPickup> _coinPool;

        // Active tracking
        private readonly HashSet<CubeMover> _activeCubesInDodgeZone = new HashSet<CubeMover>();
        private readonly List<CubeMover> _cubesToRemove = new List<CubeMover>();

        // Game State
        private int _coins = 0;
        private int _score = 0;
        private float _currentSpawnInterval;
        private Coroutine _spawnCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            InitializePools();
            InitializeGameState();
            SubscribeToEvents();
            StartGameLoop();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (Instance == this)
                Instance = null;
        }

        #region Initialization
        private void InitializePools()
        {
            var cubeParent = new GameObject("CubePool").transform;
            var coinParent = new GameObject("CoinPool").transform;
            cubeParent.SetParent(transform);
            coinParent.SetParent(transform);

            _cubePool = new ObjectPool<CubeMover>(cubePrefab, cubeParent, cubePoolSize);
            _coinPool = new ObjectPool<CoinPickup>(coinPrefab, coinParent, coinPoolSize);
        }

        private void InitializeGameState()
        {
            _currentSpawnInterval = baseSpawnInterval;
            UpdateUI();
        }

        private void SubscribeToEvents()
        {
            CombatEvents.OnPlayerDodge += HandlePlayerDodge;
            CombatEvents.OnCubeEnterDodgeZone += HandleCubeEnterDodgeZone;
            CombatEvents.OnCubeExitDodgeZone += HandleCubeExitDodgeZone;
            CombatEvents.OnCubeDestroyed += HandleCubeDestroyed;
            CombatEvents.OnCoinCollected += HandleCoinCollected;
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnPlayerDodge -= HandlePlayerDodge;
            CombatEvents.OnCubeEnterDodgeZone -= HandleCubeEnterDodgeZone;
            CombatEvents.OnCubeExitDodgeZone -= HandleCubeExitDodgeZone;
            CombatEvents.OnCubeDestroyed -= HandleCubeDestroyed;
            CombatEvents.OnCoinCollected -= HandleCoinCollected;
        }
        #endregion

        #region Game Loop
        private void StartGameLoop()
        {
            _spawnCoroutine = StartCoroutine(SpawnCubesRoutine());
        }

        private IEnumerator SpawnCubesRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_currentSpawnInterval);
                SpawnCube();
                IncreaseDifficulty();
            }
        }

        private void SpawnCube()
        {
            if (spawnRails.Length == 0) return;

            var rail = spawnRails[Random.Range(0, spawnRails.Length)];
            var spawnPosition = rail.position + spawnOffset;
            
            var cube = _cubePool.Get();
            cube.transform.position = spawnPosition;
            cube.transform.SetParent(rail);
        }

        private void IncreaseDifficulty()
        {
            _currentSpawnInterval = Mathf.Max(
                _currentSpawnInterval - difficultyIncrement,
                minSpawnInterval
            );
        }
        #endregion

        #region Event Handlers
        private void HandlePlayerDodge()
        {
            if (_activeCubesInDodgeZone.Count == 0) return;

            int destroyedCount = 0;
            _cubesToRemove.Clear();

            foreach (var cube in _activeCubesInDodgeZone)
            {
                if (cube != null && !cube.WasDestroyed)
                {
                    cube.DestroyAndSpawnCoin();
                    _cubesToRemove.Add(cube);
                    destroyedCount++;
                }
            }

            foreach (var cube in _cubesToRemove)
            {
                _activeCubesInDodgeZone.Remove(cube);
            }

            int bonusPoints = destroyedCount > 1 ? (destroyedCount - 1) * 10 : 0;
            AddScore(destroyedCount * 10 + bonusPoints);
        }

        private void HandleCubeEnterDodgeZone(CubeMover cube)
        {
            _activeCubesInDodgeZone.Add(cube);
            UpdateDebugUI();
        }

        private void HandleCubeExitDodgeZone(CubeMover cube)
        {
            _activeCubesInDodgeZone.Remove(cube);
            UpdateDebugUI();
        }

        private void HandleCubeDestroyed(Vector3 position)
        {
            SpawnCoin(position);
        }

        private void HandleCoinCollected(int value)
        {
            AddCoins(value);
            AddScore(value * 5);
        }
        #endregion

        #region Game State Management
        private void SpawnCoin(Vector3 position)
        {
            var coin = _coinPool.Get();
            coin.transform.position = position + Vector3.up * 0.5f;
        }

        public void AddCoins(int value)
        {
            _coins += value;
            UpdateUI();
        }

        public void AddScore(int points)
        {
            _score += points;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (coinText != null)
                coinText.text = $"Coins: {_coins}";
            
            if (scoreText != null)
                scoreText.text = $"Score: {_score}";
        }

        private void UpdateDebugUI()
        {
            if (debugText != null)
                debugText.text = $"Cubes in Zone: {_activeCubesInDodgeZone.Count}\nSpawn Interval: {_currentSpawnInterval:F2}s";
        }
        #endregion

        public void ResetGame()
        {
            _coins = 0;
            _score = 0;
            _currentSpawnInterval = baseSpawnInterval;
            _activeCubesInDodgeZone.Clear();
            UpdateUI();
        }
    }
}
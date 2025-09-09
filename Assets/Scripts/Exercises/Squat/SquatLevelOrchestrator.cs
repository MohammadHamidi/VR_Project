using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CombatSystem;
using CombatSystem.Combat;
using CombatSystem.Events;
using CombatSystem.Spawning;

namespace CombatSystem.Campaign
{
	/// <summary>
	/// Drop-in orchestrator that runs a simple multi-level campaign for the Squat game.
	/// - Configure a sequence of levels (lives + difficulty)
	/// - Auto-finds WaveManager/CombatScoring and disables SquatGameManager to avoid conflicts
	/// - Starts each level and waits for session completion before advancing
	/// Attach to any GameObject in the Squat scene and press Play.
	/// </summary>
	public class SquatLevelOrchestrator : MonoBehaviour
	{
		[System.Serializable]
		public class LevelConfig
		{
			[Header("Level Settings")]
			[Tooltip("Difficulty for this level (used by WaveManager)")]
			public WaveGenerator.DifficultyLevel difficulty = WaveGenerator.DifficultyLevel.Easy;

			[Tooltip("Player lives for this level (uses CombatScoring.SetLives)")]
			public int lives = 3;

			[Tooltip("Optional delay before the level starts (seconds)")]
			public float preLevelDelay = 1f;

			[Tooltip("Optional delay after the level ends (seconds)")]
			public float postLevelDelay = 1f;
		}

		[Header("Campaign Settings")]
		[Tooltip("Levels will be played in order")]
		[SerializeField] private List<LevelConfig> levels = new List<LevelConfig>
		{
			new LevelConfig { difficulty = WaveGenerator.DifficultyLevel.Easy, lives = 3, preLevelDelay = 1f, postLevelDelay = 1f },
			new LevelConfig { difficulty = WaveGenerator.DifficultyLevel.Medium, lives = 3, preLevelDelay = 1f, postLevelDelay = 1f },
			new LevelConfig { difficulty = WaveGenerator.DifficultyLevel.Hard, lives = 3, preLevelDelay = 1f, postLevelDelay = 1f },
		};

		[Tooltip("Automatically disable SquatGameManager in scene to avoid double control")]
		[SerializeField] private bool disableSquatGameManager = true;

		[Header("Auto-Discovery (Optional Overrides)")]
		[SerializeField] private WaveManager waveManager;
		[SerializeField] private CombatScoring combatScoring;

		private bool _campaignRunning = false;

		private void Awake()
		{
			// Optional: disable the default scene game manager to prevent conflicting control
			if (disableSquatGameManager)
			{
				var gm = FindObjectOfType<SquatGameManager>();
				if (gm != null && gm.enabled)
				{
					gm.enabled = false;
					Debug.Log("SquatLevelOrchestrator: Disabled existing SquatGameManager to avoid conflicts.");
				}
			}

			// Auto-find core systems if not wired in Inspector
			if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
			if (combatScoring == null) combatScoring = FindObjectOfType<CombatScoring>();
		}

		private void Start()
		{
			if (waveManager == null)
			{
				Debug.LogError("SquatLevelOrchestrator: WaveManager not found in scene.");
				return;
			}

			if (combatScoring == null)
			{
				Debug.LogWarning("SquatLevelOrchestrator: CombatScoring not found. Lives UI/logic will not be configured.");
			}

			if (!_campaignRunning)
			{
				StartCoroutine(RunCampaign());
			}
		}

		private IEnumerator RunCampaign()
		{
			_campaignRunning = true;
			Debug.Log($"SquatLevelOrchestrator: Starting campaign with {levels.Count} level(s).");

			for (int i = 0; i < levels.Count; i++)
			{
				var lvl = levels[i];
				Debug.Log($"SquatLevelOrchestrator: Preparing Level {i + 1}/{levels.Count} - Difficulty: {lvl.difficulty}, Lives: {lvl.lives}");

				// Pre-level delay
				if (lvl.preLevelDelay > 0f)
					yield return new WaitForSeconds(lvl.preLevelDelay);

				// Configure lives via CombatScoring (acts as player's health/attempts for the session)
				if (combatScoring != null)
				{
					combatScoring.SetLives(Mathf.Max(0, lvl.lives));
				}

				// Start session at requested difficulty
				bool sessionEnded = false;
				System.Action onSessionCompleted = () => { sessionEnded = true; };
				WaveManager.OnSessionCompleted += onSessionCompleted;

				waveManager.StartNewSession(lvl.difficulty);
				Debug.Log($"SquatLevelOrchestrator: Level {i + 1} started.");

				// Wait for the session to complete
				while (!sessionEnded)
					yield return null;

				WaveManager.OnSessionCompleted -= onSessionCompleted;
				Debug.Log($"SquatLevelOrchestrator: Level {i + 1} completed.");

				// Post-level delay
				if (lvl.postLevelDelay > 0f)
					yield return new WaitForSeconds(lvl.postLevelDelay);
			}

			Debug.Log("SquatLevelOrchestrator: Campaign complete.");
			CombatEvents.OnGameEnd?.Invoke();
			_campaignRunning = false;
		}
	}
}



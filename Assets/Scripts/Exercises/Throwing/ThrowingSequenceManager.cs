using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a sequence of throwing levels defined by a list of ThrowingLevelData assets.
/// When the player completes a level (hits all targets or time runs out), it automatically 
/// switches to the next configuration and regenerates the level using the existing 
/// LevelGenerator in the scene.
/// </summary>
public class ThrowingSequenceManager : MonoBehaviour
{
	[Header("Sequence Settings")]
	[SerializeField] private List<ThrowingLevelData> levelSequence = new List<ThrowingLevelData>();
	[Tooltip("Advance to next level when stage is completed (all targets hit).")]
	[SerializeField] private bool advanceOnCompletion = true;
	[Tooltip("Advance to next level when time runs out (even if not all targets hit).")]
	[SerializeField] private bool advanceOnTimeUp = false;
	[Tooltip("Restart from the first level after reaching the end.")]
	[SerializeField] private bool loopSequence = false;

	[Header("References")]
	[SerializeField] private LevelGenerator levelGenerator; // If null, will auto-find on this GameObject or in scene

	[Header("Events")]
	public UnityEvent<int, ThrowingLevelData> OnLevelLoaded; // (index, levelData)
	public UnityEvent OnSequenceCompleted;
	public UnityEvent OnLevelCompleted; // Fired when any level is completed
	public UnityEvent OnLevelTimeUp; // Fired when any level times out

	private ObjectStageManager stageManager;
	private int currentIndex = -1;
	private bool isAdvancing;

	void Awake()
	{
		EnsureReferences();
		// If there is a sequence set, make sure first level is applied early
		if (levelSequence != null && levelSequence.Count > 0 && levelGenerator != null)
		{
			LoadLevelAtIndex(0);
		}
	}

	void Start()
	{
		// StageManager is created during level generation; delay subscription to end of frame
		StartCoroutine(SubscribeToStageManagerWhenReady());
	}

	private void EnsureReferences()
	{
		if (levelGenerator == null)
		{
			levelGenerator = GetComponent<LevelGenerator>();
			if (levelGenerator == null)
			{
				levelGenerator = FindObjectOfType<LevelGenerator>();
			}
		}
	}

	private IEnumerator SubscribeToStageManagerWhenReady()
	{
		// Wait a few frames to allow LevelGenerator to create and attach ObjectStageManager
		for (int i = 0; i < 60; i++) // ~1 second at 60 FPS
		{
			stageManager = FindObjectOfType<ObjectStageManager>();
			if (stageManager != null) break;
			yield return null;
		}
		if (stageManager != null)
		{
			// Double-check instance still valid before subscribing
			stageManager.OnStageCompleted?.AddListener(HandleStageCompleted);
			stageManager.OnTimeUp?.AddListener(HandleTimeUp);
			Debug.Log("ThrowingSequenceManager: Subscribed to ObjectStageManager events.");
		}
		else
		{
			Debug.LogWarning("ThrowingSequenceManager: ObjectStageManager not found to subscribe.");
		}
	}

	private void OnDisable()
	{
		UnsubscribeFromStageManager();
	}

	private void UnsubscribeFromStageManager()
	{
		if (stageManager != null)
		{
			stageManager.OnStageCompleted?.RemoveListener(HandleStageCompleted);
			stageManager.OnTimeUp?.RemoveListener(HandleTimeUp);
		}
	}

	private void HandleStageCompleted()
	{
		if (isAdvancing) return;
		Debug.Log("ThrowingSequenceManager: Stage completed event received.");
		OnLevelCompleted?.Invoke();
		
		if (advanceOnCompletion)
		{
			StartCoroutine(AdvanceAfterFrame());
		}
	}

	private void HandleTimeUp()
	{
		if (isAdvancing) return;
		Debug.Log("ThrowingSequenceManager: Time up event received.");
		OnLevelTimeUp?.Invoke();
		
		if (advanceOnTimeUp)
		{
			StartCoroutine(AdvanceAfterFrame());
		}
	}

	private IEnumerator AdvanceAfterFrame()
	{
		isAdvancing = true; // guard against multiple triggers
		yield return null; // allow UI/events to settle this frame
		AdvanceToNextLevel();
		isAdvancing = false;
	}

	public void AdvanceToNextLevel()
	{
		if (levelSequence == null || levelSequence.Count == 0 || levelGenerator == null) return;

		int nextIndex = currentIndex + 1;
		if (nextIndex >= levelSequence.Count)
		{
			if (loopSequence)
			{
				nextIndex = 0;
			}
			else
			{
				OnSequenceCompleted?.Invoke();
				return;
			}
		}

		LoadLevelAtIndex(nextIndex);
	}

	public void RestartSequence()
	{
		LoadLevelAtIndex(0);
	}

	public void LoadLevelAtIndex(int index)
	{
		if (levelGenerator == null) return;
		if (levelSequence == null || index < 0 || index >= levelSequence.Count) return;

		currentIndex = index;
		ThrowingLevelData levelData = levelSequence[currentIndex];
		if (levelData == null)
		{
			Debug.LogWarning($"ThrowingSequenceManager: LevelData at index {currentIndex} is null.");
			return;
		}

		// Set the level data and regenerate
		levelGenerator.levelData = levelData;
		levelGenerator.currentLevelIndex = currentIndex;
		levelGenerator.RegenerateLevel();

		// After regenerating, stage manager may have been recreated; resubscribe next frame
		StartCoroutine(ResubscribeNextFrame());

		OnLevelLoaded?.Invoke(currentIndex, levelData);
		Debug.Log($"ThrowingSequenceManager: Loaded level index {currentIndex} -> {levelData.name}");
	}

	private IEnumerator ResubscribeNextFrame()
	{
		// Give time for regeneration and component reattachment
		for (int i = 0; i < 120; i++) // up to ~2 seconds
		{
			UnsubscribeFromStageManager();
			stageManager = FindObjectOfType<ObjectStageManager>();
			if (stageManager != null) break;
			yield return null;
		}
		if (stageManager != null)
		{
			stageManager.OnStageCompleted?.AddListener(HandleStageCompleted);
			stageManager.OnTimeUp?.AddListener(HandleTimeUp);
		}
	}

	// --- Public setters/getters ---
	public void SetSequence(List<ThrowingLevelData> sequence)
	{
		levelSequence = sequence ?? new List<ThrowingLevelData>();
	}

	public int GetCurrentIndex() => currentIndex;
	public ThrowingLevelData GetCurrentLevelData() => (currentIndex >= 0 && currentIndex < (levelSequence?.Count ?? 0)) ? levelSequence[currentIndex] : null;
	public int GetTotalLevels() => levelSequence?.Count ?? 0;
	public bool IsSequenceCompleted() => currentIndex >= (levelSequence?.Count ?? 0) - 1;
}

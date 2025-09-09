using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a sequence of bridges defined by a list of BridgeConfig assets.
/// When the player reaches a progress threshold (default 90%) on the current bridge,
/// it automatically switches to the next configuration and rebuilds the bridge
/// using the existing SOLIDBridgeBuilder in the scene.
/// </summary>
public class BridgeSequenceManager : MonoBehaviour
{
	[Header("Sequence Settings")]
	[SerializeField] private List<BridgeConfig> bridgeSequence = new List<BridgeConfig>();
	[Tooltip("Progress threshold (0-1) to advance to the next bridge.")]
	[SerializeField] [Range(0.1f, 1f)] private float advanceAtProgress = 0.95f;
	[Tooltip("Restart from the first bridge after reaching the end.")]
	[SerializeField] private bool loopSequence = false;

	[Header("References")]
	[SerializeField] private SOLIDBridgeBuilder bridgeBuilder; // If null, will auto-find on this GameObject or in scene

	[Header("Events")]
	public UnityEvent<int, BridgeConfig> OnBridgeLoaded; // (index, config)
	public UnityEvent OnSequenceCompleted;

	private BridgeTracker tracker;
	private int currentIndex = -1;
	private bool isAdvancing;

	void Awake()
	{
		EnsureReferences();
		// If there is a sequence set, make sure first config is applied early
		if (bridgeSequence != null && bridgeSequence.Count > 0 && bridgeBuilder != null)
		{
			LoadBridgeAtIndex(0);
		}
	}

	void Start()
	{
		// Tracker is added during build by SOLIDBridgeBuilder; delay subscription to end of frame
		StartCoroutine(SubscribeToTrackerWhenReady());
	}

	private void EnsureReferences()
	{
		if (bridgeBuilder == null)
		{
			bridgeBuilder = GetComponent<SOLIDBridgeBuilder>();
			if (bridgeBuilder == null)
			{
				bridgeBuilder = FindObjectOfType<SOLIDBridgeBuilder>();
			}
		}
	}

	private IEnumerator SubscribeToTrackerWhenReady()
	{
		// Wait a few frames to allow SOLIDBridgeBuilder to build and attach BridgeTracker
		for (int i = 0; i < 60; i++) // ~1 second at 60 FPS
		{
			tracker = GetComponent<BridgeTracker>();
			if (tracker == null && bridgeBuilder != null)
			{
				tracker = bridgeBuilder.GetComponent<BridgeTracker>();
			}
			if (tracker != null) break;
			yield return null;
		}
		if (tracker != null)
		{
			// Double-check instance still valid before subscribing
			tracker.OnProgressChanged?.AddListener(HandleProgressChanged);
			tracker.OnBridgeCompleted?.AddListener(HandleBridgeCompleted);
			Debug.Log("BridgeSequenceManager: Subscribed to BridgeTracker events.");
		}
		else
		{
			Debug.LogWarning("BridgeSequenceManager: BridgeTracker not found to subscribe.");
		}
	}

	private void OnDisable()
	{
		UnsubscribeFromTracker();
	}

	private void UnsubscribeFromTracker()
	{
		if (tracker != null)
		{
			tracker.OnProgressChanged.RemoveListener(HandleProgressChanged);
			tracker.OnBridgeCompleted.RemoveListener(HandleBridgeCompleted);
		}
	}

	private void HandleProgressChanged(float progress01)
	{
		if (isAdvancing) return;
		if (progress01 >= advanceAtProgress)
		{
			StartCoroutine(AdvanceAfterFrame());
		}
	}

	private IEnumerator AdvanceAfterFrame()
	{
		isAdvancing = true; // guard against multiple triggers
		yield return null; // allow UI/events to settle this frame
		AdvanceToNextBridge();
		isAdvancing = false;
	}

	private void HandleBridgeCompleted()
	{
		if (isAdvancing) return;
		Debug.Log("BridgeSequenceManager: Bridge completed event received. Advancing to next bridge.");
		StartCoroutine(AdvanceAfterFrame());
	}

	public void AdvanceToNextBridge()
	{
		if (bridgeSequence == null || bridgeSequence.Count == 0 || bridgeBuilder == null) return;

		int nextIndex = currentIndex + 1;
		if (nextIndex >= bridgeSequence.Count)
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

		LoadBridgeAtIndex(nextIndex);
	}

	public void RestartSequence()
	{
		LoadBridgeAtIndex(0);
	}

	public void LoadBridgeAtIndex(int index)
	{
		if (bridgeBuilder == null) return;
		if (bridgeSequence == null || index < 0 || index >= bridgeSequence.Count) return;

		currentIndex = index;
		BridgeConfig config = bridgeSequence[currentIndex];
		if (config == null)
		{
			Debug.LogWarning($"BridgeSequenceManager: Config at index {currentIndex} is null.");
			return;
		}

		// Rebuild via builder API
		bridgeBuilder.SetBridgeConfiguration(config);
		Debug.Log($"BridgeSequenceManager: Loaded bridge index {currentIndex} -> {config.name}");

		// After rebuilding, tracker may have been recreated; resubscribe next frame
		StartCoroutine(ResubscribeNextFrame());

		OnBridgeLoaded?.Invoke(currentIndex, config);
	}

	private IEnumerator ResubscribeNextFrame()
	{
		// Give time for rebuild and component reattachment
		for (int i = 0; i < 120; i++) // up to ~2 seconds
		{
			UnsubscribeFromTracker();
			tracker = GetComponent<BridgeTracker>();
			if (tracker == null && bridgeBuilder != null)
			{
				tracker = bridgeBuilder.GetComponent<BridgeTracker>();
			}
			if (tracker != null) break;
			yield return null;
		}
		if (tracker != null)
		{
			tracker.OnProgressChanged?.AddListener(HandleProgressChanged);
			tracker.OnBridgeCompleted?.AddListener(HandleBridgeCompleted);
			tracker.ResetProgress();
		}
	}

	// --- Public setters/getters ---
	public void SetSequence(List<BridgeConfig> sequence)
	{
		bridgeSequence = sequence ?? new List<BridgeConfig>();
	}

	public int GetCurrentIndex() => currentIndex;
	public BridgeConfig GetCurrentConfig() => (currentIndex >= 0 && currentIndex < (bridgeSequence?.Count ?? 0)) ? bridgeSequence[currentIndex] : null;
}



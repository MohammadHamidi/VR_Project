using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class ObjectStageManager : MonoBehaviour
{
    [Header("Stage Settings")]
    [Tooltip("All of the TargetRing instances in this scene")]
    public TargetRing[] targets;
    [Tooltip("How long the timer runs (seconds)")]
    public float stageTime = 60f;
    [Tooltip("Turn off if you don't want a countdown")]
    public bool enableTimer = true;

    [Header("UI Controller")]
    [Tooltip("Drag in your StageUIController here")]
    public StageUIController uiController;

    [Header("Events")]
    public UnityEvent<TargetRing> OnTargetHit;
    public UnityEvent OnStageCompleted;
    public UnityEvent OnTimeUp;

    float _timeLeft;
    List<TargetRing> _ringList;

    void Awake()
    {
        // Ensure UnityEvents are initialized to prevent null refs when listeners are added at runtime
        if (OnTargetHit == null) OnTargetHit = new UnityEvent<TargetRing>();
        if (OnStageCompleted == null) OnStageCompleted = new UnityEvent();
        if (OnTimeUp == null) OnTimeUp = new UnityEvent();
    }

    void Start()
    {
        // Turn the array into a list    
        _ringList = new List<TargetRing>(targets);

        // Initialize timer    
        _timeLeft = stageTime;

        // Initialize UI: header, ring icons, and slider    
        uiController.Initialize(
            _ringList,
            initialHeader: "Hit all targets!",
            enableTimer: enableTimer,
            totalTime: stageTime
        );

        // Subscribe so that each hit flips its icon and we can check for completion    
        foreach (var ring in _ringList)
            ring.OnHit += OnRingHit;
    }

    void Update()
    {
        if (!enableTimer)
            return;

        // 1) Decrease the remaining time
        _timeLeft -= Time.deltaTime;

        // 2) Update the slider
        uiController.UpdateTimer(_timeLeft);

        // 3) If time's up, restart
        if (_timeLeft <= 0f)
        {
            OnTimeUp?.Invoke();
            RetryStage();
        }
    }

    void OnRingHit(TargetRing ring)
    {
        // Fire target hit event
        OnTargetHit?.Invoke(ring);

        // All rings down? advance    
        bool allDown = true;
        foreach (var r in _ringList)
            if (r.gameObject.activeSelf)
            {
                allDown = false;
                break;
            }

        if (allDown)
        {
            OnStageCompleted?.Invoke();
            AdvanceStage();
        }
    }

    void AdvanceStage()
    {
       // SceneManager.LoadScene("BridgeScene");
    }

    void RetryStage()
    {
       // SceneManager.LoadScene("ObjectStage");
    }
}
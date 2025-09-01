using UnityEngine;

public class CombatStageManager : MonoBehaviour
{
    [Header("Game Settings")]
    [Tooltip("Total length of the combat round in seconds.")]
    public float gameDuration = 90f;

    private float _timeRemaining;
    private bool  _gameActive = false;
    //
    // private ScoreManager       _scoreManager;
    // private CombatUIController _uiController;
    //
    // void Start()
    // {
    //     _timeRemaining = gameDuration;
    //     _gameActive    = true;
    //
    //     _scoreManager = FindObjectOfType<ScoreManager>();
    //     if (_scoreManager == null)
    //         Debug.LogError("[CombatStageManager] No ScoreManager found in scene.");
    //
    //     _uiController = FindObjectOfType<CombatUIController>();
    //     if (_uiController == null)
    //         Debug.LogError("[CombatStageManager] No CombatUIController found in scene.");
    //
    //     // Initialize UI to 0
    //     _uiController.UpdateScore(0);
    //     _uiController.UpdateTimer(_timeRemaining);
    //     _uiController.UpdateCombo(1);
    //     _uiController.UpdateHealth(3);
    // }

    // void Update()
    // {
    //     if (!_gameActive) return;
    //
    //     _timeRemaining -= Time.deltaTime;
    //     if (_timeRemaining < 0f) _timeRemaining = 0f;
    //
    //     _uiController.UpdateTimer(_timeRemaining);
    //
    //     if (_timeRemaining <= 0f)
    //     {
    //         _gameActive = false;
    //         EndGame();
    //     }
    // }
    //
    // /// <summary>
    // /// Called by SquatDetector whenever the player squats.
    // /// Awards 10 points per squat.
    // /// </summary>
    // public void PlayerSquatted()
    // {
    //     if (!_gameActive) return;
    //
    //     if (_scoreManager != null)
    //     {
    //         _scoreManager.AddScore(10);
    //         _uiController.UpdateScore(_scoreManager.currentScore);
    //     }
    // }

    // private void EndGame()
    // {
    //     Debug.Log($"[CombatStageManager] Combat Game Over! Final Score: {_scoreManager.currentScore}");
    //     // TODO: Show a “Game Over” panel or load next scene.
    // }
}
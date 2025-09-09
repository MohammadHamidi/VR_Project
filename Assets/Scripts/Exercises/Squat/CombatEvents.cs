using System;
using UnityEngine;

namespace CombatSystem.Events
{
    /// <summary>
    /// Centralized event system for combat mechanics
    /// Promotes loose coupling between components
    /// </summary>
    public class CombatEvents : MonoBehaviour
    {
        // Player Events
        public static Action OnPlayerDodge;
        public static Action<float> OnPlayerSquatDepthChanged;
        public static Action<Vector3> OnPlayerHit;
        public static Action<float, float> OnValidSquat; // (depthNorm, quality)
        public static Action<float> OnPlayerTakeDamage; // damage amount
        public static Action OnPlayerDeath;
        public static Action<float> OnPerfectSquat; // quality score for perfect squats
        
        // Drone Events
        public static Action<CombatSystem.Drones.DroneController> OnDroneDestroyed;
        public static Action<Vector3> OnShockwaveTriggered;
        public static Action OnShockwaveActivated; // When player activates shockwave
        public static Action<Vector3> OnDroneSpawnedAtPosition; // Position where drone spawned
        public static Action<float> OnDroneDamageDealt; // Damage amount dealt by drone
        
        // Wave Events
        public static Action OnWaveStarted;
        public static Action OnWaveEnded;
        public static Action<int> OnWaveNumberChanged; // Current wave number
        public static Action<float> OnWaveProgressChanged; // 0-1 progress through current wave
        public static Action<float> OnWaveTimeRemaining; // Seconds remaining in wave
        public static Action OnWaveStart; // Alternative naming for compatibility
        public static Action OnWaveComplete; // Alternative naming for compatibility
        
        // Portal Events
        public static Action OnPortalOpening;
        public static Action OnPortalOpened;
        public static Action OnPortalClosing;
        public static Action OnPortalClosed;
        public static Action<int> OnPortalSpawnPointActivated; // spawn point index
        public static Action<int> OnPortalSpawnPointUsed; // spawn point index when drone spawns
        public static Action<CombatSystem.Portals.PortalController> OnPortalStateChanged;
        
        // Spawning Events
        public static Action<bool> OnSpawningModeChanged; // true = portal spawning, false = regular spawning
        public static Action<float> OnSpawnRateChanged; // Current spawn interval
        public static Action<int> OnMaxSimultaneousDronesChanged; // Current max drone limit
        public static Action<int> OnDroneSpawned; // Simple version with count
        
        // Power Meter Events
        public static Action<float> OnPowerMeterChanged; // 0-100 (legacy naming)
        public static Action<float> OnPowerChanged; // Current power level (new naming)
        public static Action<float> OnPowerGained; // Power amount gained
        public static Action<float> OnPowerSpent; // Power amount spent
        public static Action<bool> OnOverchargeStateChanged; // true/false
        
        // Health/Lives Events
        public static Action<int> OnLivesChanged; // Current lives remaining
        
        // Cube Events (Legacy - keeping for compatibility)
        public static Action<CombatSystem.Obstacles.CubeMover> OnCubeEnterDodgeZone;
        public static Action<CombatSystem.Obstacles.CubeMover> OnCubeExitDodgeZone;
        public static Action<Vector3> OnCubeDestroyed;
        
        // Coin Events
        public static Action<Vector3> OnCoinSpawned;
        public static Action<int> OnCoinCollected;
        
        // Scoring Events
        public static Action<int> OnScoreChanged; // Current score
        
        public static Action<int> OnComboBreak; // Final combo value when broken
        public static Action<float> OnComboChanged; // Legacy float version for compatibility
        
        // Game State Events
        public static Action<float> OnDifficultyChanged;
        public static Action OnGamePaused;
        public static Action OnGameResumed;
        public static Action OnGameStart; // Game session start
        public static Action OnGameEnd; // Game session end
        public static Action OnGamePause; // Alternative naming
        public static Action OnGameResume; // Alternative naming
        
        // Performance Events
        public static Action<int> OnActiveCubesCountChanged;
        public static Action<int> OnActiveDronesCountChanged;
        
        // Audio Events
        public static Action<string> OnPlaySoundEffect; // Sound effect name/path
        public static Action<string> OnPlayMusic; // Music track name/path
        public static Action OnStopAllAudio;
        
        // Visual Effects Events
        public static Action<Vector3, string> OnTriggerVFX; // Position and effect name
        public static Action<Vector3, Color> OnTriggerExplosion; // Position and color
        public static Action<Vector3, float> OnCameraShake; // Position and intensity

        /// <summary>
        /// Clear all event subscriptions (call this on scene transitions)
        /// </summary>
        public static void ClearAllEvents()
        {
            // Player Events
            OnPlayerDodge = null;
            OnPlayerSquatDepthChanged = null;
            OnPlayerHit = null;
            OnValidSquat = null;
            OnPlayerTakeDamage = null;
            OnPlayerDeath = null;
            OnPerfectSquat = null;
            
            // Drone Events
            OnDroneDestroyed = null;
            OnDroneSpawned = null;
            OnShockwaveTriggered = null;
            OnShockwaveActivated = null;
            OnDroneSpawnedAtPosition = null;
            OnDroneDamageDealt = null;
            
            // Wave Events
            OnWaveStarted = null;
            OnWaveEnded = null;
            OnWaveNumberChanged = null;
            OnWaveProgressChanged = null;
            OnWaveTimeRemaining = null;
            OnWaveStart = null;
            OnWaveComplete = null;
            
            // Portal Events
            OnPortalOpening = null;
            OnPortalOpened = null;
            OnPortalClosing = null;
            OnPortalClosed = null;
            OnPortalSpawnPointActivated = null;
            OnPortalSpawnPointUsed = null;
            OnPortalStateChanged = null;
            
            // Spawning Events
            OnSpawningModeChanged = null;
            OnSpawnRateChanged = null;
            OnMaxSimultaneousDronesChanged = null;
            
            // Power Meter Events
            OnPowerMeterChanged = null;
            OnPowerChanged = null;
            OnPowerGained = null;
            OnPowerSpent = null;
            OnOverchargeStateChanged = null;
            
            // Health/Lives Events
            OnLivesChanged = null;
            
            // Cube Events (Legacy)
            OnCubeEnterDodgeZone = null;
            OnCubeExitDodgeZone = null;
            OnCubeDestroyed = null;
            
            // Coin Events
            OnCoinSpawned = null;
            OnCoinCollected = null;
            
            // Scoring Events
            OnScoreChanged = null;
            OnComboChanged = null;
            OnComboBreak = null;
            
            // Game State Events
            OnDifficultyChanged = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameStart = null;
            OnGameEnd = null;
            OnGamePause = null;
            OnGameResume = null;
            
            // Performance Events
            OnActiveCubesCountChanged = null;
            OnActiveDronesCountChanged = null;
            
            // Audio Events
            OnPlaySoundEffect = null;
            OnPlayMusic = null;
            OnStopAllAudio = null;
            
            // Visual Effects Events
            OnTriggerVFX = null;
            OnTriggerExplosion = null;
            OnCameraShake = null;
            
            Debug.Log("All combat events cleared");
        }

        /// <summary>
        /// Debug method to log all active event subscriptions
        /// Useful for debugging memory leaks or orphaned subscriptions
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogActiveSubscriptions()
        {
            Debug.Log("=== Active Combat Event Subscriptions ===");
            
            // Player Events
            LogEventSubscribers("OnPlayerDodge", OnPlayerDodge);
            LogEventSubscribers("OnPlayerHit", OnPlayerHit);
            LogEventSubscribers("OnPlayerTakeDamage", OnPlayerTakeDamage);
            LogEventSubscribers("OnPlayerDeath", OnPlayerDeath);
            LogEventSubscribers("OnValidSquat", OnValidSquat);
            LogEventSubscribers("OnPerfectSquat", OnPerfectSquat);
            
            // Drone Events  
            LogEventSubscribers("OnDroneSpawned", OnDroneSpawned);
            LogEventSubscribers("OnDroneDestroyed", OnDroneDestroyed);
            LogEventSubscribers("OnShockwaveActivated", OnShockwaveActivated);
            
            // Wave Events
            LogEventSubscribers("OnWaveStarted", OnWaveStarted);
            LogEventSubscribers("OnWaveEnded", OnWaveEnded);
            
            // Portal Events
            LogEventSubscribers("OnPortalOpened", OnPortalOpened);
            LogEventSubscribers("OnPortalClosed", OnPortalClosed);
            
            // Power Events
            LogEventSubscribers("OnPowerChanged", OnPowerChanged);
            LogEventSubscribers("OnPowerGained", OnPowerGained);
            LogEventSubscribers("OnPowerSpent", OnPowerSpent);
            
            // Game State Events
            LogEventSubscribers("OnLivesChanged", OnLivesChanged);
            LogEventSubscribers("OnScoreChanged", OnScoreChanged);
            LogEventSubscribers("OnComboChanged", OnComboChanged);
            
            Debug.Log("=== End Event Subscriptions ===");
        }

        /// <summary>
        /// Helper method to log event subscription count
        /// </summary>
        private static void LogEventSubscribers(string eventName, System.Delegate eventDelegate)
        {
            if (eventDelegate != null)
            {
                int listenerCount = eventDelegate.GetInvocationList().Length;
                Debug.Log($"{eventName}: {listenerCount} subscribers");
                
                // Optionally log subscriber details in debug builds
                #if UNITY_EDITOR && COMBAT_DEBUG_VERBOSE
                foreach (var subscriber in eventDelegate.GetInvocationList())
                {
                    Debug.Log($"  - {subscriber.Target?.GetType().Name ?? "Static"}.{subscriber.Method.Name}");
                }
                #endif
            }
            else
            {
                Debug.Log($"{eventName}: No subscribers");
            }
        }

        /// <summary>
        /// Check for potential memory leaks by counting total event subscriptions
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void CheckForMemoryLeaks()
        {
            int totalSubscriptions = 0;
            
            // Count all active subscriptions
            if (OnPlayerDodge != null) totalSubscriptions += OnPlayerDodge.GetInvocationList().Length;
            if (OnPlayerTakeDamage != null) totalSubscriptions += OnPlayerTakeDamage.GetInvocationList().Length;
            if (OnDroneDestroyed != null) totalSubscriptions += OnDroneDestroyed.GetInvocationList().Length;
            // ... add more as needed
            
            if (totalSubscriptions > 50) // Arbitrary threshold
            {
                Debug.LogWarning($"High number of event subscriptions detected: {totalSubscriptions}. " +
                    "Consider checking for memory leaks or unsubscribed events.");
            }
            else
            {
                Debug.Log($"Event subscription count looks healthy: {totalSubscriptions}");
            }
        }

        /// <summary>
        /// Force fire all critical events with default values (for testing)
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DebugFireAllEvents()
        {
            Debug.Log("Firing all events with test values...");
            
            OnPlayerDodge?.Invoke();
            OnPlayerSquatDepthChanged?.Invoke(0.5f);
            OnValidSquat?.Invoke(0.8f, 85f);
            OnPlayerTakeDamage?.Invoke(1f);
            
            OnPowerGained?.Invoke(10f);
            OnLivesChanged?.Invoke(3);
            OnScoreChanged?.Invoke(100);
            
            Debug.Log("All test events fired");
        }

        void OnDestroy()
        {
            // Auto-clear events when this component is destroyed
            ClearAllEvents();
        }
    }
}
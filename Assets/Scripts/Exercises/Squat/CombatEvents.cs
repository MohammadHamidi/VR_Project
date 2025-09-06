using System;
using UnityEngine;

namespace CombatSystem.Events
{
    /// <summary>
    /// Centralized event system for combat mechanics
    /// Promotes loose coupling between components
    /// </summary>
    public  class CombatEvents:MonoBehaviour
    {
        // Player Events
        public static Action OnPlayerDodge;
        public static Action<float> OnPlayerSquatDepthChanged;
        public static Action<Vector3> OnPlayerHit;
        public static Action<float, float> OnValidSquat; // (depthNorm, quality)
        
        // Drone Events
        public static Action<CombatSystem.Drones.DroneController> OnDroneDestroyed;
        public static Action<CombatSystem.Drones.DroneController> OnDroneSpawned;
        public static Action<Vector3> OnShockwaveTriggered;
        public static Action<Vector3> OnDroneSpawnedAtPosition; // Position where drone spawned
        
        // Wave Events
        public static Action OnWaveStarted;
        public static Action OnWaveEnded;
        public static Action<int> OnWaveNumberChanged; // Current wave number
        public static Action<float> OnWaveProgressChanged; // 0-1 progress through current wave
        public static Action<float> OnWaveTimeRemaining; // Seconds remaining in wave
        
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
        
        // Power Meter Events
        public static Action<float> OnPowerMeterChanged; // 0-100
        public static Action<bool> OnOverchargeStateChanged; // true/false
        
        // Cube Events (Legacy - keeping for compatibility)
        public static Action<CombatSystem.Obstacles.CubeMover> OnCubeEnterDodgeZone;
        public static Action<CombatSystem.Obstacles.CubeMover> OnCubeExitDodgeZone;
        public static Action<Vector3> OnCubeDestroyed;
        
        // Coin Events
        public static Action<Vector3> OnCoinSpawned;
        public static Action<int> OnCoinCollected;
        
        // Game State Events
        public static Action<int> OnScoreChanged;
        public static Action<float> OnDifficultyChanged;
        public static Action<int> OnLivesChanged;
        public static Action<float> OnComboChanged;
        public static Action OnGamePaused;
        public static Action OnGameResumed;
        
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
            
            // Drone Events
            OnDroneDestroyed = null;
            OnDroneSpawned = null;
            OnShockwaveTriggered = null;
            OnDroneSpawnedAtPosition = null;
            
            // Wave Events
            OnWaveStarted = null;
            OnWaveEnded = null;
            OnWaveNumberChanged = null;
            OnWaveProgressChanged = null;
            OnWaveTimeRemaining = null;
            
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
            OnOverchargeStateChanged = null;
            
            // Cube Events (Legacy)
            OnCubeEnterDodgeZone = null;
            OnCubeExitDodgeZone = null;
            OnCubeDestroyed = null;
            
            // Coin Events
            OnCoinSpawned = null;
            OnCoinCollected = null;
            
            // Game State Events
            OnScoreChanged = null;
            OnDifficultyChanged = null;
            OnLivesChanged = null;
            OnComboChanged = null;
            OnGamePaused = null;
            OnGameResumed = null;
            
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
            if (OnPlayerDodge != null) Debug.Log($"OnPlayerDodge: {OnPlayerDodge.GetInvocationList().Length} subscribers");
            if (OnPlayerHit != null) Debug.Log($"OnPlayerHit: {OnPlayerHit.GetInvocationList().Length} subscribers");
            
            // Drone Events  
            if (OnDroneSpawned != null) Debug.Log($"OnDroneSpawned: {OnDroneSpawned.GetInvocationList().Length} subscribers");
            if (OnDroneDestroyed != null) Debug.Log($"OnDroneDestroyed: {OnDroneDestroyed.GetInvocationList().Length} subscribers");
            
            // Wave Events
            if (OnWaveStarted != null) Debug.Log($"OnWaveStarted: {OnWaveStarted.GetInvocationList().Length} subscribers");
            if (OnWaveEnded != null) Debug.Log($"OnWaveEnded: {OnWaveEnded.GetInvocationList().Length} subscribers");
            
            // Portal Events
            if (OnPortalOpened != null) Debug.Log($"OnPortalOpened: {OnPortalOpened.GetInvocationList().Length} subscribers");
            if (OnPortalClosed != null) Debug.Log($"OnPortalClosed: {OnPortalClosed.GetInvocationList().Length} subscribers");
            
            Debug.Log("=== End Event Subscriptions ===");
        }
    }
}
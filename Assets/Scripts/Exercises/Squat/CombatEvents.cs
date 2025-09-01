using System;
using UnityEngine;

namespace CombatSystem.Events
{
    /// <summary>
    /// Centralized event system for combat mechanics
    /// Promotes loose coupling between components
    /// </summary>
    public static class CombatEvents
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
        
        // Performance Events
        public static Action<int> OnActiveCubesCountChanged;
        public static Action<int> OnActiveDronesCountChanged;

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
            
            // Performance Events
            OnActiveCubesCountChanged = null;
            OnActiveDronesCountChanged = null;
        }
    }
}
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
        
        // Cube Events
        public static Action<CombatSystem.Obstacles.CubeMover> OnCubeEnterDodgeZone;
        public static Action<CombatSystem.Obstacles.CubeMover> OnCubeExitDodgeZone;
        public static Action<Vector3> OnCubeDestroyed;
        
        // Coin Events
        public static Action<Vector3> OnCoinSpawned;
        public static Action<int> OnCoinCollected;
        
        // Game State Events
        public static Action<int> OnScoreChanged;
        public static Action<float> OnDifficultyChanged;
        
        // Performance Events
        public static Action<int> OnActiveCubesCountChanged;

        /// <summary>
        /// Clear all event subscriptions (call this on scene transitions)
        /// </summary>
        public static void ClearAllEvents()
        {
            OnPlayerDodge = null;
            OnPlayerSquatDepthChanged = null;
            OnCubeEnterDodgeZone = null;
            OnCubeExitDodgeZone = null;
            OnCubeDestroyed = null;
            OnCoinSpawned = null;
            OnCoinCollected = null;
            OnScoreChanged = null;
            OnDifficultyChanged = null;
            OnActiveCubesCountChanged = null;
        }
    }
}
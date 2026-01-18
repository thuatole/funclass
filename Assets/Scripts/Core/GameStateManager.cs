using UnityEngine;
using System;

namespace FunClass.Core
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }
        public event Action<GameState, GameState> OnStateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameStateManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentState = GameState.Boot;
            Debug.Log("[GameStateManager] Initialized in Boot state");
        }

        void Start()
        {
            // Delay transition to ensure all managers are initialized
            StartCoroutine(DelayedLevelStart());
        }

        private System.Collections.IEnumerator DelayedLevelStart()
        {
            Debug.Log("[GameStateManager] Waiting for managers to initialize...");
            
            // Wait one frame for all managers to initialize
            yield return null;
            
            Debug.Log("[GameStateManager] Starting level transition to InLevel");
            TransitionTo(GameState.InLevel);
            
            Debug.Log($"[GameStateManager] Current state after transition: {CurrentState}");
        }

        public void TransitionTo(GameState newState)
        {
            if (CurrentState == newState)
            {
                Debug.Log($"[GameStateManager] Already in {newState} state, skipping transition");
                return;
            }

            GameState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameStateManager] STATE TRANSITION: {oldState} -> {newState}");
            
            if (OnStateChanged != null)
            {
                int listenerCount = OnStateChanged.GetInvocationList().Length;
                Debug.Log($"[GameStateManager] Notifying {listenerCount} listeners of state change");
                OnStateChanged.Invoke(oldState, newState);
            }
            else
            {
                Debug.LogWarning("[GameStateManager] No listeners subscribed to OnStateChanged!");
            }
        }

        public void CompleteLevel()
        {
            if (CurrentState == GameState.InLevel)
            {
                TransitionTo(GameState.LevelComplete);
            }
        }

        public void StartLevel()
        {
            TransitionTo(GameState.InLevel);
        }
    }
}

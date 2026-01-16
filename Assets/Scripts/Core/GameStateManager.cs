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
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentState = GameState.Boot;
        }

        void Start()
        {
            TransitionTo(GameState.InLevel);
        }

        public void TransitionTo(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameStateManager] {oldState} -> {newState}");
            OnStateChanged?.Invoke(oldState, newState);
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

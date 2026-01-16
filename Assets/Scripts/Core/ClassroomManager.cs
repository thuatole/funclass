using UnityEngine;
using System;

namespace FunClass.Core
{
    public class ClassroomManager : MonoBehaviour
    {
        public static ClassroomManager Instance { get; private set; }

        public ClassroomState CurrentState { get; private set; }
        public event Action<ClassroomState, ClassroomState> OnStateChanged;

        private bool isActive = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        void Start()
        {
            if (GameStateManager.Instance != null)
            {
                HandleGameStateChanged(GameStateManager.Instance.CurrentState, GameStateManager.Instance.CurrentState);
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.InLevel)
            {
                ActivateClassroom();
            }
            else
            {
                DeactivateClassroom();
            }
        }

        private void ActivateClassroom()
        {
            isActive = true;
            ResetClassroom();
            Debug.Log("[ClassroomManager] Classroom activated");
        }

        private void DeactivateClassroom()
        {
            isActive = false;
            Debug.Log("[ClassroomManager] Classroom deactivated");
        }

        private void ResetClassroom()
        {
            ChangeState(ClassroomState.Calm);
        }

        public void ChangeState(ClassroomState newState)
        {
            if (!isActive)
            {
                Debug.LogWarning("[ClassroomManager] Cannot change state - classroom is not active");
                return;
            }

            if (CurrentState == newState) return;

            ClassroomState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[ClassroomManager] Classroom atmosphere: {oldState} -> {newState}");
            OnStateChanged?.Invoke(oldState, newState);
        }

        public void EscalateState()
        {
            if (!isActive) return;

            ClassroomState nextState = CurrentState switch
            {
                ClassroomState.Calm => ClassroomState.Noisy,
                ClassroomState.Noisy => ClassroomState.Tense,
                ClassroomState.Tense => ClassroomState.Chaotic,
                ClassroomState.Chaotic => ClassroomState.Chaotic,
                _ => ClassroomState.Calm
            };

            ChangeState(nextState);
        }

        public void DeescalateState()
        {
            if (!isActive) return;

            ClassroomState nextState = CurrentState switch
            {
                ClassroomState.Chaotic => ClassroomState.Tense,
                ClassroomState.Tense => ClassroomState.Noisy,
                ClassroomState.Noisy => ClassroomState.Calm,
                ClassroomState.Calm => ClassroomState.Calm,
                _ => ClassroomState.Calm
            };

            ChangeState(nextState);
        }
    }
}

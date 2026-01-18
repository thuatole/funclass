using UnityEngine;
using System;

namespace FunClass.Core
{
    public class ClassroomManager : MonoBehaviour
    {
        public static ClassroomManager Instance { get; private set; }

        public ClassroomState CurrentState { get; private set; }
        public event Action<ClassroomState, ClassroomState> OnStateChanged;

        public float DisruptionLevel { get; private set; }
        public event Action<float> OnDisruptionChanged;

        private bool isActive = false;
        private const float MAX_DISRUPTION = 100f;
        private const float MIN_DISRUPTION = 0f;

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

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.OnEventLogged += HandleStudentEvent;
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.OnEventLogged -= HandleStudentEvent;
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
            DisruptionLevel = 0f;
            OnDisruptionChanged?.Invoke(DisruptionLevel);
            Debug.Log("[ClassroomManager] Disruption level reset to 0");
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

        private void HandleStudentEvent(StudentEvent evt)
        {
            if (!isActive) return;

            float disruptionChange = GetDisruptionChangeForEvent(evt.eventType);
            
            if (disruptionChange != 0)
            {
                AddDisruption(disruptionChange, evt.description);
            }
        }

        private float GetDisruptionChangeForEvent(StudentEventType eventType)
        {
            return eventType switch
            {
                StudentEventType.MakingNoise => 5f,
                StudentEventType.KnockedOverObject => 8f,
                StudentEventType.ThrowingObject => 15f,
                StudentEventType.LeftSeat => 10f,
                StudentEventType.WanderingAround => 7f,
                StudentEventType.StudentCalmed => -5f,
                StudentEventType.StudentReturnedToSeat => -10f,
                StudentEventType.StudentStoppedAction => -3f,
                _ => 0f
            };
        }

        public void AddDisruption(float amount, string reason = "")
        {
            if (!isActive) return;

            float oldDisruption = DisruptionLevel;
            DisruptionLevel = Mathf.Clamp(DisruptionLevel + amount, MIN_DISRUPTION, MAX_DISRUPTION);

            if (Mathf.Abs(oldDisruption - DisruptionLevel) > 0.01f)
            {
                string changeType = amount > 0 ? "increased" : "decreased";
                Debug.Log($"[ClassroomManager] Disruption {changeType} by {Mathf.Abs(amount):F1} to {DisruptionLevel:F1}/100 ({reason})");
                OnDisruptionChanged?.Invoke(DisruptionLevel);

                UpdateClassroomStateBasedOnDisruption();
            }
        }

        public void ReduceDisruption(float amount)
        {
            AddDisruption(-amount, "item confiscated");
        }

        private void UpdateClassroomStateBasedOnDisruption()
        {
            ClassroomState targetState = DisruptionLevel switch
            {
                >= 75f => ClassroomState.Chaotic,
                >= 50f => ClassroomState.Tense,
                >= 25f => ClassroomState.Noisy,
                _ => ClassroomState.Calm
            };

            if (targetState != CurrentState)
            {
                ChangeState(targetState);
            }
        }

        public float GetDisruptionPercentage()
        {
            return DisruptionLevel / MAX_DISRUPTION;
        }
    }
}

using UnityEngine;
using System;

namespace FunClass.Core
{
    public class StudentAgent : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private StudentConfig config;

        public StudentState CurrentState { get; private set; }
        public StudentConfig Config => config;
        public event Action<StudentState, StudentState> OnStateChanged;

        private float nextBehaviorTime;
        private bool isActive = false;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool isPerformingSequence = false;
        private StudentInteractableObject targetObject;

        void Start()
        {
            if (config != null)
            {
                Initialize(config);
            }
            else
            {
                Debug.LogWarning($"[StudentAgent] {gameObject.name} has no config assigned");
            }

            originalPosition = transform.position;
            originalRotation = transform.rotation;
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

        void Update()
        {
            if (!isActive) return;

            if (!isPerformingSequence && Time.time >= nextBehaviorTime)
            {
                PerformAutonomousBehavior();
                ScheduleNextBehavior();
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.InLevel)
            {
                ActivateStudent();
            }
            else
            {
                DeactivateStudent();
            }
        }

        private void ActivateStudent()
        {
            isActive = true;
            ScheduleNextBehavior();
            Debug.Log($"[StudentAgent] {config?.studentName ?? gameObject.name} activated");
        }

        private void DeactivateStudent()
        {
            isActive = false;
            Debug.Log($"[StudentAgent] {config?.studentName ?? gameObject.name} deactivated");
        }

        public void Initialize(StudentConfig studentConfig)
        {
            config = studentConfig;
            CurrentState = config.initialState;
            Debug.Log($"[StudentAgent] Initialized {config.studentName} with state: {CurrentState}");
        }

        public void ChangeState(StudentState newState)
        {
            if (CurrentState == newState) return;

            StudentState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[StudentAgent] {config?.studentName ?? gameObject.name}: {oldState} -> {newState}");
            OnStateChanged?.Invoke(oldState, newState);
        }

        private void PerformAutonomousBehavior()
        {
            if (config == null) return;

            float interactionChance = GetInteractionChanceForState();
            bool shouldInteractWithObject = UnityEngine.Random.value < interactionChance;

            if (shouldInteractWithObject)
            {
                TryInteractWithNearbyObject();
                return;
            }

            float roll = UnityEngine.Random.value;

            if (CurrentState == StudentState.Calm)
            {
                if (config.canLookAround && roll < 0.4f)
                {
                    LookAround();
                }
                else if (config.canFidget && roll < 0.7f)
                {
                    Fidget();
                }

                if (UnityEngine.Random.value < (1f - config.attentionSpan) * 0.1f)
                {
                    EscalateState();
                }
            }
            else if (CurrentState == StudentState.Distracted)
            {
                if (config.canLookAround && roll < 0.6f)
                {
                    LookAround();
                }
                else if (config.canFidget && roll < 0.9f)
                {
                    Fidget();
                }

                if (UnityEngine.Random.value < config.impulsiveness * 0.15f)
                {
                    EscalateState();
                }
            }
            else if (CurrentState == StudentState.ActingOut)
            {
                if (config.canStandUp && roll < 0.5f)
                {
                    StandUp();
                }
                else if (config.canMoveAround && roll < 0.3f)
                {
                    MoveAround();
                }
                else if (config.canLookAround)
                {
                    LookAround();
                }

                if (UnityEngine.Random.value < config.impulsiveness * 0.2f)
                {
                    EscalateState();
                }
            }
            else if (CurrentState == StudentState.Critical)
            {
                if (config.canMoveAround && roll < 0.7f)
                {
                    MoveAround();
                }
                else if (config.canStandUp)
                {
                    StandUp();
                }
            }
        }

        private void EscalateState()
        {
            StudentState nextState = CurrentState switch
            {
                StudentState.Calm => StudentState.Distracted,
                StudentState.Distracted => StudentState.ActingOut,
                StudentState.ActingOut => StudentState.Critical,
                StudentState.Critical => StudentState.Critical,
                _ => StudentState.Calm
            };

            ChangeState(nextState);
        }

        public void DeescalateState()
        {
            StudentState nextState = CurrentState switch
            {
                StudentState.Critical => StudentState.ActingOut,
                StudentState.ActingOut => StudentState.Distracted,
                StudentState.Distracted => StudentState.Calm,
                StudentState.Calm => StudentState.Calm,
                _ => StudentState.Calm
            };

            ChangeState(nextState);
        }

        private void ScheduleNextBehavior()
        {
            if (config == null) return;

            float waitTime = UnityEngine.Random.Range(config.minIdleTime, config.maxIdleTime);
            nextBehaviorTime = Time.time + waitTime;
        }

        private void Fidget()
        {
            Debug.Log($"[StudentAgent] {config.studentName} fidgets");
        }

        private void LookAround()
        {
            Debug.Log($"[StudentAgent] {config.studentName} looks around");
            
            float randomYaw = UnityEngine.Random.Range(-45f, 45f);
            transform.rotation = originalRotation * Quaternion.Euler(0f, randomYaw, 0f);
        }

        private void StandUp()
        {
            Debug.Log($"[StudentAgent] {config.studentName} stands up");
        }

        private void MoveAround()
        {
            Debug.Log($"[StudentAgent] {config.studentName} moves around");
            
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-0.5f, 0.5f),
                0f,
                UnityEngine.Random.Range(-0.5f, 0.5f)
            );
            
            transform.position = originalPosition + randomOffset;
        }

        private float GetInteractionChanceForState()
        {
            if (config == null) return 0f;

            return CurrentState switch
            {
                StudentState.Calm => config.calmInteractionChance,
                StudentState.Distracted => config.distractedInteractionChance,
                StudentState.ActingOut => config.actingOutInteractionChance,
                StudentState.Critical => config.criticalInteractionChance,
                _ => 0f
            };
        }

        private void TryInteractWithNearbyObject()
        {
            StudentInteractableObject nearbyObject = FindNearbyInteractableObject();
            
            if (nearbyObject != null)
            {
                PerformObjectInteraction(nearbyObject);
            }
        }

        private StudentInteractableObject FindNearbyInteractableObject()
        {
            if (config == null) return null;

            Collider[] colliders = Physics.OverlapSphere(transform.position, config.interactionRange);
            
            foreach (Collider col in colliders)
            {
                StudentInteractableObject interactable = col.GetComponent<StudentInteractableObject>();
                if (interactable != null && interactable.gameObject != gameObject)
                {
                    return interactable;
                }
            }

            return null;
        }

        private void PerformObjectInteraction(StudentInteractableObject obj)
        {
            if (config == null || obj == null) return;

            float roll = UnityEngine.Random.value;

            if (config.canKnockOverObjects && obj.canBeKnockedOver && roll < 0.3f)
            {
                WalkToObjectAndInteract(obj, () => obj.KnockOver(this));
            }
            else if (config.canMakeNoiseWithObjects && obj.canMakeNoise && roll < 0.5f)
            {
                WalkToObjectAndInteract(obj, () => obj.MakeNoise(this));
            }
            else if (config.canThrowObjects && obj.canBeThrown && roll < 0.7f)
            {
                WalkToObjectAndInteract(obj, () => obj.Throw(this));
            }
            else if (config.canDropItems && obj.canBeDropped && roll < 0.85f)
            {
                WalkToObjectAndInteract(obj, () => obj.Drop(this));
            }
            else if (config.canTouchObjects)
            {
                WalkToObjectAndInteract(obj, () => obj.Touch(this));
            }
        }

        private void WalkToObjectAndInteract(StudentInteractableObject obj, System.Action interactionAction)
        {
            if (obj == null) return;

            targetObject = obj;
            isPerformingSequence = true;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.WanderingAround,
                    $"is walking toward {obj.objectName}",
                    obj.gameObject
                );
            }

            Vector3 targetPosition = obj.transform.position;
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            Vector3 moveToPosition = targetPosition + (-direction * 1f);
            transform.position = moveToPosition;

            interactionAction?.Invoke();

            isPerformingSequence = false;
            targetObject = null;
        }

        public void LeaveSeat()
        {
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.LeftSeat,
                    "left their seat"
                );
            }

            Vector3 wanderPosition = originalPosition + new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                0f,
                UnityEngine.Random.Range(-2f, 2f)
            );

            transform.position = wanderPosition;
        }

        public void InteractWithTeacher(TeacherController teacher)
        {
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.TeacherInteracted,
                    "is being addressed by the teacher"
                );
            }

            Debug.Log($"[StudentAgent] Teacher is interacting with {config?.studentName}");
        }

        public void CalmDown()
        {
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.StudentCalmed,
                    "is calming down"
                );
            }

            DeescalateState();
            StopCurrentAction();

            Debug.Log($"[StudentAgent] {config?.studentName} is calming down");
        }

        public void StopCurrentAction()
        {
            if (isPerformingSequence)
            {
                isPerformingSequence = false;
                targetObject = null;

                if (StudentEventManager.Instance != null)
                {
                    StudentEventManager.Instance.LogEvent(
                        this,
                        StudentEventType.StudentStoppedAction,
                        "stopped their current action"
                    );
                }
            }

            Debug.Log($"[StudentAgent] {config?.studentName} stopped their action");
        }

        public void ReturnToSeat()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.StudentReturnedToSeat,
                    "returned to their seat"
                );
            }

            DeescalateState();
            StopCurrentAction();

            Debug.Log($"[StudentAgent] {config?.studentName} returned to seat");
        }

        public void TakeObjectAway(GameObject obj)
        {
            if (obj == null) return;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.ObjectTakenAway,
                    $"had {obj.name} taken away",
                    obj
                );
            }

            obj.SetActive(false);
            DeescalateState();

            Debug.Log($"[StudentAgent] Teacher took {obj.name} away from {config?.studentName}");
        }

        public string GetInteractionPrompt()
        {
            if (config == null) return "Interact with student";

            return CurrentState switch
            {
                StudentState.Calm => $"Talk to {config.studentName}",
                StudentState.Distracted => $"Calm down {config.studentName}",
                StudentState.ActingOut => $"Stop {config.studentName}",
                StudentState.Critical => $"Send {config.studentName} back to seat",
                _ => $"Interact with {config.studentName}"
            };
        }
    }
}

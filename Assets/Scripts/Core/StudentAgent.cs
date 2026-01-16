using UnityEngine;
using System;
using System.Collections.Generic;

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
        private StudentReactionType currentReaction = StudentReactionType.None;
        private float reactionEndTime = 0f;
        private StudentInteractionSequence currentSequence = null;
        private List<StudentInteractionSequence> availableSequences = new List<StudentInteractionSequence>();

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

            if (currentReaction != StudentReactionType.None && Time.time >= reactionEndTime)
            {
                ClearReaction();
            }

            if (IsInSequence() && currentSequence.CanAutoFailStep())
            {
                HandleSequenceTimeout();
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
            InitializeSequences();
            Debug.Log($"[StudentAgent] Initialized {config.studentName} with state: {CurrentState}");
        }

        private void InitializeSequences()
        {
            availableSequences.Clear();

            if (LevelLoader.Instance == null || LevelLoader.Instance.CurrentLevel == null)
            {
                Debug.LogWarning($"[StudentAgent] {config?.studentName} cannot load sequences - no level loaded");
                return;
            }

            LevelConfig currentLevel = LevelLoader.Instance.CurrentLevel;
            
            if (currentLevel.availableSequences == null || currentLevel.availableSequences.Count == 0)
            {
                Debug.Log($"[StudentAgent] {config?.studentName} has no sequences available in this level");
                return;
            }

            foreach (StudentSequenceConfig sequenceConfig in currentLevel.availableSequences)
            {
                if (sequenceConfig != null)
                {
                    StudentInteractionSequence sequence = sequenceConfig.ToInteractionSequence();
                    availableSequences.Add(sequence);
                    Debug.Log($"[StudentAgent] {config?.studentName} loaded sequence: {sequence.sequenceId}");
                }
            }

            Debug.Log($"[StudentAgent] {config?.studentName} loaded {availableSequences.Count} sequences from level");
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

            float reactionRoll = UnityEngine.Random.value;
            if (reactionRoll < 0.6f)
            {
                TriggerReaction(StudentReactionType.Apologize, 4f);
            }
            else if (reactionRoll < 0.8f)
            {
                TriggerReaction(StudentReactionType.Embarrassed, 3f);
            }

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

            float reactionRoll = UnityEngine.Random.value;
            if (CurrentState == StudentState.Critical && reactionRoll < 0.5f)
            {
                TriggerReaction(StudentReactionType.Cry, 5f);
            }
            else if (CurrentState == StudentState.ActingOut && reactionRoll < 0.4f)
            {
                TriggerReaction(StudentReactionType.Scared, 4f);
            }
            else if (reactionRoll < 0.3f)
            {
                TriggerReaction(StudentReactionType.Angry, 3f);
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

            float reactionRoll = UnityEngine.Random.value;
            if (reactionRoll < 0.7f)
            {
                TriggerReaction(StudentReactionType.Embarrassed, 5f);
            }
            else if (reactionRoll < 0.9f)
            {
                TriggerReaction(StudentReactionType.Apologize, 4f);
            }

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

        public void TriggerReaction(StudentReactionType reaction, float duration = 3f)
        {
            if (reaction == StudentReactionType.None) return;

            currentReaction = reaction;
            reactionEndTime = Time.time + duration;

            string reactionText = reaction switch
            {
                StudentReactionType.Cry => "started crying",
                StudentReactionType.Apologize => "apologized",
                StudentReactionType.Angry => "looks angry",
                StudentReactionType.Scared => "looks scared",
                StudentReactionType.Embarrassed => "looks embarrassed",
                StudentReactionType.Confused => "looks confused",
                _ => "reacted"
            };

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.StudentReacted,
                    reactionText
                );
            }

            Debug.Log($"[Reaction] {config?.studentName} {reactionText}");
        }

        public void ClearReaction()
        {
            if (currentReaction == StudentReactionType.None) return;

            StudentReactionType previousReaction = currentReaction;
            currentReaction = StudentReactionType.None;
            reactionEndTime = 0f;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.ReactionEnded,
                    $"stopped reacting ({previousReaction})"
                );
            }

            Debug.Log($"[Reaction] {config?.studentName} stopped reacting");
        }

        public StudentReactionType GetCurrentReaction()
        {
            return currentReaction;
        }

        public void StartSequence(StudentInteractionSequence sequence)
        {
            if (sequence == null) return;

            currentSequence = sequence;
            currentSequence.Reset();
            currentSequence.SetStepStartTime(Time.time);

            Debug.Log($"[Sequence] {config?.studentName} started sequence: {sequence.sequenceId}");

            StudentActionStep firstStep = currentSequence.GetCurrentStep();
            if (firstStep != null)
            {
                ExecuteStep(firstStep);
            }
        }

        public bool AdvanceSequence(TeacherActionType action)
        {
            if (currentSequence == null) return false;

            StudentActionStep currentStep = currentSequence.GetCurrentStep();
            if (currentStep == null) return false;

            if (currentStep.enableBranching)
            {
                bool isSuccess;
                if (currentSequence.CanBranch(action, CurrentState, out isSuccess))
                {
                    ExecuteStep(currentStep);

                    string branchType = isSuccess ? "SUCCESS" : "FAILURE";
                    Debug.Log($"[Sequence] {config?.studentName} taking {branchType} branch (action: {action})");

                    StudentActionStep nextStep = currentSequence.AdvanceStepWithResult(isSuccess);

                    if (currentSequence.IsComplete())
                    {
                        Debug.Log($"[Sequence] {config?.studentName} completed sequence: {currentSequence.sequenceId} via {branchType} path");
                        Debug.Log($"[Sequence] Outcome: {currentSequence.finalOutcomeDescription}");
                        currentSequence = null;
                        return true;
                    }
                    else if (nextStep != null)
                    {
                        Debug.Log($"[Sequence] {config?.studentName} advanced to step {currentSequence.GetCurrentStep()?.stepDescription}");
                        return true;
                    }
                }
                else
                {
                    Debug.Log($"[Sequence] {config?.studentName} action {action} doesn't match branching requirements");
                    return false;
                }
            }
            else if (currentSequence.CanAdvance(action, CurrentState))
            {
                ExecuteStep(currentStep);

                StudentActionStep nextStep = currentSequence.AdvanceStep();

                if (currentSequence.IsComplete())
                {
                    Debug.Log($"[Sequence] {config?.studentName} completed sequence: {currentSequence.sequenceId}");
                    Debug.Log($"[Sequence] Outcome: {currentSequence.finalOutcomeDescription}");
                    currentSequence = null;
                    return true;
                }
                else if (nextStep != null)
                {
                    Debug.Log($"[Sequence] {config?.studentName} advanced to next step: {nextStep.stepDescription}");
                    return true;
                }
            }
            else
            {
                Debug.Log($"[Sequence] {config?.studentName} action {action} doesn't match current step requirements");
            }

            return false;
        }

        public void CancelSequence()
        {
            if (currentSequence != null)
            {
                Debug.Log($"[Sequence] {config?.studentName} cancelled sequence: {currentSequence.sequenceId}");
                currentSequence = null;
            }
        }

        public bool IsInSequence()
        {
            return currentSequence != null;
        }

        private void ExecuteStep(StudentActionStep step)
        {
            if (step == null) return;

            Debug.Log($"[Sequence] {config?.studentName} executing step: {step.stepDescription}");

            if (step.resultingReaction != StudentReactionType.None)
            {
                TriggerReaction(step.resultingReaction, 4f);
            }

            if (step.resultingStateChange.HasValue)
            {
                ChangeState(step.resultingStateChange.Value);
            }

            if (step.enableTimeout)
            {
                Debug.Log($"[Sequence] {config?.studentName} step has {step.timeoutSeconds}s timeout");
            }
        }

        private void HandleSequenceTimeout()
        {
            if (currentSequence == null) return;

            StudentActionStep currentStep = currentSequence.GetCurrentStep();
            if (currentStep == null || !currentStep.enableTimeout) return;

            Debug.Log($"[Sequence] {config?.studentName} TIMEOUT on step: {currentStep.stepDescription}");

            if (currentStep.timeoutReaction != StudentReactionType.None)
            {
                TriggerReaction(currentStep.timeoutReaction, 4f);
            }

            StudentActionStep nextStep = currentSequence.HandleTimeout();

            if (nextStep != null)
            {
                Debug.Log($"[Sequence] {config?.studentName} taking TIMEOUT branch to: {nextStep.stepDescription}");
                ExecuteStep(nextStep);
            }
            else
            {
                Debug.Log($"[Sequence] {config?.studentName} timeout ended sequence: {currentSequence.sequenceId}");
                currentSequence = null;
            }
        }

        public bool TryStartSequence(TeacherActionType action)
        {
            foreach (StudentInteractionSequence sequence in availableSequences)
            {
                if (sequence.MatchesEntry(CurrentState, action))
                {
                    StartSequence(sequence);
                    return true;
                }
            }
            return false;
        }

        public void HandleTeacherAction(TeacherActionType action)
        {
            if (IsInSequence())
            {
                bool advanced = AdvanceSequence(action);
                if (!advanced)
                {
                    Debug.Log($"[Sequence] {config?.studentName} couldn't advance, cancelling sequence");
                    CancelSequence();
                    ExecuteFallbackAction(action);
                }
            }
            else
            {
                bool sequenceStarted = TryStartSequence(action);
                if (!sequenceStarted)
                {
                    ExecuteFallbackAction(action);
                }
            }
        }

        private void ExecuteFallbackAction(TeacherActionType action)
        {
            switch (action)
            {
                case TeacherActionType.Talk:
                    InteractWithTeacher(TeacherController.Instance);
                    break;
                case TeacherActionType.Calm:
                    CalmDown();
                    break;
                case TeacherActionType.Stop:
                    StopCurrentAction();
                    break;
                case TeacherActionType.SendToSeat:
                    ReturnToSeat();
                    break;
                case TeacherActionType.Scold:
                    StopCurrentAction();
                    break;
                case TeacherActionType.Praise:
                    TriggerReaction(StudentReactionType.Embarrassed, 3f);
                    break;
            }
        }
    }
}

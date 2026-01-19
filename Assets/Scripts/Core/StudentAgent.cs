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
        private bool isFollowingRoute = false;
        private StudentInfluenceSources influenceSources;

        public Vector3 OriginalSeatPosition => originalPosition;
        public bool IsFollowingRoute => isFollowingRoute;
        public StudentInfluenceSources InfluenceSources => influenceSources;

        void Start()
        {
            Debug.Log($"[StudentAgent] {gameObject.name} Start() called - config: {(config != null ? config.studentName : "NULL")}");
            
            if (config != null)
            {
                Initialize(config);
            }
            else
            {
                Debug.LogWarning($"[StudentAgent] {gameObject.name} has no config assigned in Start() - attempting to find from LevelLoader");
                
                // Try to find config from LevelLoader as fallback
                if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevel != null)
                {
                    var levelConfig = LevelLoader.Instance.CurrentLevel;
                    if (levelConfig.students != null && levelConfig.students.Count > 0)
                    {
                        // Try to match by GameObject name
                        string studentName = gameObject.name.Replace("Student_", "");
                        var matchingConfig = levelConfig.students.Find(s => s.studentName == studentName);
                        
                        if (matchingConfig != null)
                        {
                            Debug.Log($"[StudentAgent] Found matching config for {studentName} from LevelLoader");
                            Initialize(matchingConfig);
                        }
                        else
                        {
                            Debug.LogError($"[StudentAgent] Could not find config for {studentName} in LevelLoader");
                        }
                    }
                }
            }

            originalPosition = transform.position;
            originalRotation = transform.rotation;
            
            Debug.Log($"[StudentAgent] {gameObject.name} initialized at position {originalPosition}");
            
            // Fallback subscription if OnEnable was called before GameStateManager existed
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                Debug.Log($"[StudentAgent] {gameObject.name} subscribed to GameStateManager in Start()");
                
                // If already in InLevel state, activate immediately
                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    Debug.Log($"[StudentAgent] {gameObject.name} already in InLevel state, activating immediately");
                    ActivateStudent();
                }
            }
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

            // Check if following a route (managed by StudentMovementManager)
            if (StudentMovementManager.Instance != null)
            {
                isFollowingRoute = StudentMovementManager.Instance.IsMoving(this);
            }

            // Skip autonomous behavior if following a route
            if (!isPerformingSequence && !isFollowingRoute && Time.time >= nextBehaviorTime)
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
            Debug.Log($"[StudentAgent] {gameObject.name} HandleGameStateChanged: {oldState} -> {newState}");
            
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
            influenceSources = new StudentInfluenceSources(this);
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
            float roll = UnityEngine.Random.value;
            bool shouldInteractWithObject = roll < interactionChance;

            Debug.Log($"[StudentAgent] {config?.studentName} behavior check - State: {CurrentState}, InteractionChance: {interactionChance:F2}, Roll: {roll:F2}, ShouldInteract: {shouldInteractWithObject}");

            if (shouldInteractWithObject)
            {
                TryInteractWithNearbyObject();
                return;
            }

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

        public void EscalateState()
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

        private float influenceImmunityUntil = 0f;
        
        public bool IsImmuneToInfluence()
        {
            return Time.time < influenceImmunityUntil;
        }
        
        public void SetInfluenceImmunity(float duration)
        {
            influenceImmunityUntil = Time.time + duration;
            Debug.Log($"[StudentAgent] {Config?.studentName} immune to influence for {duration}s");
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
                Debug.Log($"[StudentAgent] {config?.studentName} found nearby object: {nearbyObject.objectName}");
                PerformObjectInteraction(nearbyObject);
            }
            else
            {
                Debug.Log($"[StudentAgent] {config?.studentName} no nearby objects found (range: {config?.interactionRange})");
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

        private enum ObjectInteractionType
        {
            KnockOver,
            MakeNoise,
            Throw,
            Drop,
            Touch
        }

        private void PerformObjectInteraction(StudentInteractableObject obj)
        {
            if (config == null || obj == null) return;

            float roll = UnityEngine.Random.value;
            Debug.Log($"[StudentAgent] {config?.studentName} interaction roll: {roll:F2}, checking permissions...");
            Debug.Log($"[StudentAgent] canKnockOver: {config.canKnockOverObjects}, canMakeNoise: {config.canMakeNoiseWithObjects}, canThrow: {config.canThrowObjects}, canDrop: {config.canDropItems}, canTouch: {config.canTouchObjects}");

            if (config.canKnockOverObjects && obj.canBeKnockedOver && roll < 0.3f)
            {
                Debug.Log($"[StudentAgent] {config?.studentName} will knock over {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.KnockOver);
            }
            else if (config.canMakeNoiseWithObjects && obj.canMakeNoise && roll < 0.5f)
            {
                Debug.Log($"[StudentAgent] {config?.studentName} will make noise with {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.MakeNoise);
            }
            else if (config.canThrowObjects && obj.canBeThrown && roll < 0.7f)
            {
                Debug.Log($"[StudentAgent] {config?.studentName} will throw {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.Throw);
            }
            else if (config.canDropItems && obj.canBeDropped && roll < 0.85f)
            {
                Debug.Log($"[StudentAgent] {config?.studentName} will drop {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.Drop);
            }
            else if (config.canTouchObjects)
            {
                Debug.Log($"[StudentAgent] {config?.studentName} will touch {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.Touch);
            }
            else
            {
                Debug.Log($"[StudentAgent] {config?.studentName} no valid interaction with {obj.objectName} (roll: {roll:F2})");
            }
        }

        private void WalkToObjectAndInteract(StudentInteractableObject obj, ObjectInteractionType interactionType)
        {
            if (obj == null)
            {
                Debug.LogWarning($"[StudentAgent] {config?.studentName} WalkToObjectAndInteract called with null obj!");
                return;
            }

            Debug.Log($"[StudentAgent] {config?.studentName} WalkToObjectAndInteract - obj: {obj.objectName}, type: {obj.GetType().Name}, interaction: {interactionType}");

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

            Debug.Log($"[StudentAgent] {config?.studentName} reached {obj.objectName}, performing {interactionType}...");
            
            try
            {
                switch (interactionType)
                {
                    case ObjectInteractionType.KnockOver:
                        obj.KnockOver(this);
                        break;
                    case ObjectInteractionType.MakeNoise:
                        obj.MakeNoise(this);
                        break;
                    case ObjectInteractionType.Throw:
                        obj.Throw(this);
                        break;
                    case ObjectInteractionType.Drop:
                        obj.Drop(this);
                        break;
                    case ObjectInteractionType.Touch:
                        obj.Touch(this);
                        break;
                }
                Debug.Log($"[StudentAgent] {config?.studentName} interaction {interactionType} completed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudentAgent] {config?.studentName} interaction {interactionType} threw exception: {e.Message}");
                Debug.LogException(e);
            }

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
            // Stop any active route movement
            if (StudentMovementManager.Instance != null)
            {
                StudentMovementManager.Instance.StopMovement(this);
            }

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

            string itemName = obj.name;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    this,
                    StudentEventType.ObjectTakenAway,
                    $"had {itemName} taken away",
                    obj
                );
            }

            obj.SetActive(false);

            OnItemConfiscated(itemName);

            Debug.Log($"[StudentAgent] Teacher took {itemName} away from {config?.studentName}");
        }

        /// <summary>
        /// Hook method called when an item is confiscated from this student.
        /// Determines the appropriate reaction and state change based on the item type.
        /// This is the extension point for adding custom confiscation behavior.
        /// </summary>
        /// <param name="itemName">The name of the confiscated item</param>
        public void OnItemConfiscated(string itemName)
        {
            if (config == null)
            {
                Debug.LogWarning($"[StudentAgent] OnItemConfiscated called but config is null");
                return;
            }

            Debug.Log($"[StudentAgent] {config.studentName} processing confiscation of: {itemName}");

            ItemConfiscationBehavior matchedBehavior = FindMatchingConfiscationBehavior(itemName);

            if (matchedBehavior != null)
            {
                ApplyConfiscationBehavior(matchedBehavior, itemName);
            }
            else
            {
                ApplyDefaultConfiscationBehavior(itemName);
            }
        }

        /// <summary>
        /// Finds the first confiscation behavior that matches the given item name.
        /// Returns null if no match is found.
        /// </summary>
        private ItemConfiscationBehavior FindMatchingConfiscationBehavior(string itemName)
        {
            if (config.confiscationBehaviors == null || config.confiscationBehaviors.Length == 0)
            {
                return null;
            }

            foreach (ItemConfiscationBehavior behavior in config.confiscationBehaviors)
            {
                if (behavior != null && behavior.MatchesItem(itemName))
                {
                    Debug.Log($"[StudentAgent] {config.studentName} matched confiscation behavior for: {itemName}");
                    return behavior;
                }
            }

            return null;
        }

        /// <summary>
        /// Applies a specific confiscation behavior (reaction, state change, disruption reduction).
        /// </summary>
        private void ApplyConfiscationBehavior(ItemConfiscationBehavior behavior, string itemName)
        {
            Debug.Log($"[StudentAgent] {config.studentName} applying custom confiscation behavior for: {itemName}");

            if (behavior.reaction != StudentReactionType.None)
            {
                TriggerReaction(behavior.reaction, behavior.reactionDuration);
            }

            if (behavior.changeState)
            {
                StudentState oldState = CurrentState;
                ChangeState(behavior.newState);
                Debug.Log($"[StudentAgent] {config.studentName} state changed from {oldState} to {behavior.newState} due to confiscation of {itemName}");
            }

            if (behavior.disruptionReduction > 0f && ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.ReduceDisruption(behavior.disruptionReduction);
                Debug.Log($"[StudentAgent] {config.studentName} confiscation reduced disruption by {behavior.disruptionReduction}");
            }
        }

        /// <summary>
        /// Applies the default confiscation behavior when no specific behavior matches.
        /// </summary>
        private void ApplyDefaultConfiscationBehavior(string itemName)
        {
            Debug.Log($"[StudentAgent] {config.studentName} applying default confiscation behavior for: {itemName}");

            if (config.defaultConfiscationReaction != StudentReactionType.None)
            {
                TriggerReaction(config.defaultConfiscationReaction, config.defaultConfiscationReactionDuration);
            }

            if (config.defaultConfiscationChangesState)
            {
                StudentState oldState = CurrentState;
                ChangeState(config.defaultConfiscationNewState);
                Debug.Log($"[StudentAgent] {config.studentName} state changed from {oldState} to {config.defaultConfiscationNewState} due to default confiscation behavior");
            }
            else
            {
                DeescalateState();
            }
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
                case TeacherActionType.CallStudentBack:
                    // Student is being called back - show embarrassed or apologetic reaction
                    if (CurrentState == StudentState.Critical)
                    {
                        TriggerReaction(StudentReactionType.Scared, 4f);
                    }
                    else
                    {
                        TriggerReaction(StudentReactionType.Embarrassed, 3f);
                    }
                    DeescalateState();
                    break;
                case TeacherActionType.EscortStudentBack:
                    // Being escorted - show stronger reaction
                    TriggerReaction(StudentReactionType.Apologize, 5f);
                    DeescalateState();
                    DeescalateState(); // Double de-escalate for escort
                    break;
                case TeacherActionType.ForceReturnToSeat:
                    // Forced return - may trigger negative reaction
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        TriggerReaction(StudentReactionType.Angry, 2f);
                    }
                    else
                    {
                        TriggerReaction(StudentReactionType.Embarrassed, 3f);
                    }
                    break;
            }
        }

        /// <summary>
        /// Starts the student on a predefined route
        /// </summary>
        public void StartRoute(StudentRoute route)
        {
            if (route == null)
            {
                Debug.LogWarning($"[StudentAgent] {config?.studentName} cannot start null route");
                return;
            }

            if (StudentMovementManager.Instance != null)
            {
                StudentMovementManager.Instance.StartRoute(this, route);
                Debug.Log($"[StudentAgent] {config?.studentName} starting route: {route.routeName}");
            }
        }

        /// <summary>
        /// Stops the student's current route movement
        /// </summary>
        public void StopRoute()
        {
            if (StudentMovementManager.Instance != null)
            {
                StudentMovementManager.Instance.StopMovement(this);
                Debug.Log($"[StudentAgent] {config?.studentName} stopped route");
            }
        }


        /// <summary>
        /// Gets the current route the student is following (null if not on a route)
        /// </summary>
        public StudentRoute GetCurrentRoute()
        {
            if (StudentMovementManager.Instance != null)
            {
                return StudentMovementManager.Instance.GetCurrentRoute(this);
            }
            return null;
        }
    }
}

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
        private float influenceImmunityUntil = 0f;

        public Vector3 OriginalSeatPosition => originalPosition;
        public bool IsFollowingRoute => isFollowingRoute;
        public StudentInfluenceSources InfluenceSources => influenceSources;

        void Start()
        {
            GameLogger.Detail("StudentAgent", $"{gameObject.name} Start() - config: {(config != null ? config.studentName : "NULL")}");
            
            if (config != null)
            {
                Initialize(config);
            }
            else
            {
                GameLogger.Warning("StudentAgent", $"{gameObject.name} has no config assigned - attempting to find from LevelLoader");
                
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
                            GameLogger.Detail("StudentAgent", $"Found matching config for {studentName} from LevelLoader");
                            Initialize(matchingConfig);
                        }
                        else
                        {
                            GameLogger.Error("StudentAgent", $"Could not find config for {studentName} in LevelLoader");
                        }
                    }
                }
            }

            originalPosition = transform.position;
            originalRotation = transform.rotation;
            
            GameLogger.Detail("StudentAgent", $"{gameObject.name} initialized at {originalPosition}");
            
            // Fallback subscription if OnEnable was called before GameStateManager existed
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                GameLogger.Detail("StudentAgent", $"{gameObject.name} subscribed to GameStateManager in Start()");
                
                // If already in InLevel state, activate immediately
                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    GameLogger.Detail("StudentAgent", $"{gameObject.name} already in InLevel state, activating immediately");
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
            GameLogger.Trace("StudentAgent", $"{config?.studentName ?? gameObject.name}: Update - isActive={isActive}, isFollowingRoute={isFollowingRoute}, currentReaction={currentReaction}");
            
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
            GameLogger.Detail("StudentAgent", $"{gameObject.name}: {oldState} → {newState}");
            
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
            GameLogger.Milestone("StudentAgent", $"{config?.studentName ?? gameObject.name} activated");
        }

        private void DeactivateStudent()
        {
            isActive = false;
            GameLogger.Detail("StudentAgent", $"{config?.studentName ?? gameObject.name} deactivated");
        }

        public void Initialize(StudentConfig studentConfig)
        {
            config = studentConfig;
            CurrentState = config.initialState;
            influenceSources = new StudentInfluenceSources(this);
            InitializeSequences();
            GameLogger.Milestone("StudentAgent", $"{config.studentName} initialized - state: {CurrentState}");
        }

        private void InitializeSequences()
        {
            availableSequences.Clear();

            if (LevelLoader.Instance == null || LevelLoader.Instance.CurrentLevel == null)
            {
                GameLogger.Detail("StudentAgent", $"{config?.studentName} cannot load sequences - no level loaded");
                return;
            }

            LevelConfig currentLevel = LevelLoader.Instance.CurrentLevel;
            
            if (currentLevel.availableSequences == null || currentLevel.availableSequences.Count == 0)
            {
                GameLogger.Detail("StudentAgent", $"{config?.studentName} has no sequences available in this level");
                return;
            }

            foreach (StudentSequenceConfig sequenceConfig in currentLevel.availableSequences)
            {
                if (sequenceConfig != null)
                {
                    StudentInteractionSequence sequence = sequenceConfig.ToInteractionSequence();
                    availableSequences.Add(sequence);
                    GameLogger.Detail("StudentAgent", $"{config?.studentName} loaded sequence: {sequence.sequenceId}");
                }
            }

            GameLogger.Detail("StudentAgent", $"{config?.studentName} loaded {availableSequences.Count} sequences");
        }

        public void ChangeState(StudentState newState)
        {
            if (CurrentState == newState) return;

            StudentState oldState = CurrentState;
            CurrentState = newState;

            GameLogger.Milestone("StudentAgent", $"{config?.studentName ?? gameObject.name}: {oldState} → {newState}");
            OnStateChanged?.Invoke(oldState, newState);
        }

        private void PerformAutonomousBehavior()
        {
            if (config == null) return;

            float interactionChance = GetInteractionChanceForState();
            float roll = UnityEngine.Random.value;
            bool shouldInteractWithObject = roll < interactionChance;

            GameLogger.Detail("StudentAgent", 
                $"{config?.studentName}: behavior check - state={CurrentState}, chance={interactionChance:F2}, roll={roll:F2}, interact={shouldInteractWithObject}");

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

        public bool IsImmuneToInfluence()
        {
            return Time.time < influenceImmunityUntil;
        }
        
        public void SetInfluenceImmunity(float duration)
        {
            influenceImmunityUntil = Time.time + duration;
            GameLogger.Detail("StudentAgent", $"{Config?.studentName} immune to influence for {duration}s");
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
            GameLogger.Detail("StudentAgent", $"{config?.studentName}: next behavior in {waitTime:F1}s");
        }

        private void Fidget()
        {
            GameLogger.Detail("StudentAgent", $"{config.studentName} fidgets");
        }

        private void LookAround()
        {
            GameLogger.Detail("StudentAgent", $"{config.studentName} looks around");
            
            float randomYaw = UnityEngine.Random.Range(-45f, 45f);
            transform.rotation = originalRotation * Quaternion.Euler(0f, randomYaw, 0f);
        }

        private void StandUp()
        {
            GameLogger.Detail("StudentAgent", $"{config.studentName} stands up");
        }

        private void MoveAround()
        {
            GameLogger.Detail("StudentAgent", $"{config.studentName} moves around");
            
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
                GameLogger.Milestone("StudentAgent", $"{config?.studentName} interacts with {nearbyObject.objectName}");
                PerformObjectInteraction(nearbyObject);
            }
            else
            {
                GameLogger.Detail("StudentAgent", 
                    $"{config?.studentName} no nearby objects found (range: {config?.interactionRange})");
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
            GameLogger.Detail("StudentAgent", 
                $"{config?.studentName} interaction roll: {roll:F2}");

            if (config.canKnockOverObjects && obj.canBeKnockedOver && roll < 0.3f)
            {
                GameLogger.Detail("StudentAgent", $"{config?.studentName} will knock over {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.KnockOver);
            }
            else if (config.canMakeNoiseWithObjects && obj.canMakeNoise && roll < 0.5f)
            {
                GameLogger.Detail("StudentAgent", $"{config?.studentName} will make noise with {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.MakeNoise);
            }
            else if (config.canThrowObjects && obj.canBeThrown && roll < 0.7f)
            {
                GameLogger.Detail("StudentAgent", $"{config?.studentName} will throw {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.Throw);
            }
            else if (config.canDropItems && obj.canBeDropped && roll < 0.85f)
            {
                GameLogger.Detail("StudentAgent", $"{config?.studentName} will drop {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.Drop);
            }
            else if (config.canTouchObjects)
            {
                GameLogger.Detail("StudentAgent", $"{config?.studentName} will touch {obj.objectName}");
                WalkToObjectAndInteract(obj, ObjectInteractionType.Touch);
            }
            else
            {
                GameLogger.Detail("StudentAgent", 
                    $"{config?.studentName} no valid interaction with {obj.objectName} (roll: {roll:F2})");
            }
        }

        private void WalkToObjectAndInteract(StudentInteractableObject obj, ObjectInteractionType interactionType)
        {
            if (obj == null)
            {
                GameLogger.Warning("StudentAgent", $"{config?.studentName} WalkToObjectAndInteract called with null obj!");
                return;
            }

            GameLogger.Detail("StudentAgent", $"{config?.studentName} walking to {obj.objectName}");

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

            GameLogger.Detail("StudentAgent", $"{config?.studentName} performing {interactionType} on {obj.objectName}");
            
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
                GameLogger.Detail("StudentAgent", $"{config?.studentName} completed {interactionType}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error("StudentAgent", $"{config?.studentName} {interactionType} failed: {e.Message}");
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

            GameLogger.Detail("StudentAgent", $"Teacher interacting with {config?.studentName}");
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

            GameLogger.Detail("StudentAgent", $"{config?.studentName} calming down");
        }

        public void StopCurrentAction()
        {
            if (isPerformingSequence)
            {
                isPerformingSequence = false;

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

            GameLogger.Detail("StudentAgent", $"{config?.studentName} stopped action");
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

            GameLogger.Milestone("StudentAgent", $"{config?.studentName} returned to seat");
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

            GameLogger.Detail("StudentAgent", $"Teacher took {itemName} from {config?.studentName}");
        }

        public void OnItemConfiscated(string itemName)
        {
            if (config == null)
            {
                GameLogger.Warning("StudentAgent", "OnItemConfiscated called but config is null");
                return;
            }

            GameLogger.Detail("StudentAgent", $"{config.studentName} processing confiscation of: {itemName}");

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
                    GameLogger.Detail("StudentAgent", $"{config.studentName} matched confiscation behavior for: {itemName}");
                    return behavior;
                }
            }

            return null;
        }

        private void ApplyConfiscationBehavior(ItemConfiscationBehavior behavior, string itemName)
        {
            GameLogger.Detail("StudentAgent", $"{config.studentName} applying custom confiscation behavior for: {itemName}");

            if (behavior.reaction != StudentReactionType.None)
            {
                TriggerReaction(behavior.reaction, behavior.reactionDuration);
            }

            if (behavior.changeState)
            {
                StudentState oldState = CurrentState;
                ChangeState(behavior.newState);
                GameLogger.Detail("StudentAgent", $"{config.studentName} state changed: {oldState} → {behavior.newState}");
            }

            if (behavior.disruptionReduction > 0f && ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.ReduceDisruption(behavior.disruptionReduction);
                GameLogger.Detail("StudentAgent", $"{config.studentName} confiscation reduced disruption by {behavior.disruptionReduction}");
            }
        }

        private void ApplyDefaultConfiscationBehavior(string itemName)
        {
            GameLogger.Detail("StudentAgent", $"{config.studentName} applying default confiscation behavior for: {itemName}");

            if (config.defaultConfiscationReaction != StudentReactionType.None)
            {
                TriggerReaction(config.defaultConfiscationReaction, config.defaultConfiscationReactionDuration);
            }

            if (config.defaultConfiscationChangesState)
            {
                StudentState oldState = CurrentState;
                ChangeState(config.defaultConfiscationNewState);
                GameLogger.Detail("StudentAgent", $"{config.studentName} state changed: {oldState} → {config.defaultConfiscationNewState}");
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

        public void HandleTeacherAction(TeacherActionType action)
        {
            switch (action)
            {
                case TeacherActionType.Calm:
                    CalmDown();
                    break;
                case TeacherActionType.SendToSeat:
                    ReturnToSeat();
                    break;
                case TeacherActionType.Stop:
                    StopCurrentAction();
                    break;
                case TeacherActionType.Talk:
                    InteractWithTeacher(null);
                    break;
                case TeacherActionType.Scold:
                    if (UnityEngine.Random.value < 0.7f)
                    {
                        TriggerReaction(StudentReactionType.Embarrassed, 4f);
                    }
                    break;
                case TeacherActionType.Praise:
                    if (UnityEngine.Random.value < 0.6f)
                    {
                        DeescalateState();
                    }
                    break;
            }
        }

        public void TriggerReaction(StudentReactionType reaction, float duration)
        {
            currentReaction = reaction;
            reactionEndTime = Time.time + duration;
            GameLogger.Detail("StudentAgent", $"{config?.studentName} reaction: {reaction} for {duration}s");
        }

        private void ClearReaction()
        {
            currentReaction = StudentReactionType.None;
        }

        private bool IsInSequence()
        {
            return currentSequence != null;
        }

        private void HandleSequenceTimeout()
        {
            currentSequence = null;
            isPerformingSequence = false;
        }
    }
}

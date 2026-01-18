using UnityEngine;
using System;
using System.Collections.Generic;

namespace FunClass.Core
{
    public class ClassroomManager : MonoBehaviour
    {
        public static ClassroomManager Instance { get; private set; }

        [Header("Level Configuration")]
        public LevelConfig levelConfig;

        public ClassroomState CurrentState { get; private set; }
        public event Action<ClassroomState, ClassroomState> OnStateChanged;

        public float DisruptionLevel { get; private set; }
        public event Action<float> OnDisruptionChanged;

        [Header("Outside Student Tracking")]
        [Tooltip("Disruption added per student outside per check interval")]
        [SerializeField] private float outsideDisruptionRate = 0.5f;
        
        [Tooltip("How often to check and apply outside student penalty (seconds)")]
        [SerializeField] private float outsideCheckInterval = 5f;
        
        [Tooltip("Maximum disruption that can be added from students outside")]
        [SerializeField] private float maxOutsideDisruptionPenalty = 30f;

        public int OutsideStudentCount { get; private set; }
        public event Action<int> OnOutsideStudentCountChanged;

        private bool isActive = false;
        private const float MAX_DISRUPTION = 100f;
        private const float MIN_DISRUPTION = 0f;
        
        private Dictionary<StudentAgent, float> studentsOutside = new Dictionary<StudentAgent, float>();
        private float nextOutsideCheckTime = 0f;
        private float totalOutsideDisruptionApplied = 0f;
        
        // Disruption Timeout Tracking
        private bool isDisruptionTimeoutActive = false;
        private float disruptionTimeoutStartTime = 0f;
        private bool hasShownTimeoutWarning = false;
        public event Action<float> OnDisruptionTimeoutWarning; // Remaining seconds
        public event Action OnDisruptionTimeoutLose;

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
            Debug.Log("[ClassroomManager] Start called");
            
            // Don't manually call HandleGameStateChanged here
            // Let the event system handle it when state actually transitions to InLevel
            // This prevents premature deactivation when state is still Boot
            
            if (GameStateManager.Instance != null)
            {
                Debug.Log($"[ClassroomManager] Current game state: {GameStateManager.Instance.CurrentState}");
                
                // Only activate if already in InLevel (shouldn't happen normally)
                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    Debug.Log("[ClassroomManager] Already in InLevel, activating classroom");
                    ActivateClassroom();
                }
            }
        }

        void Update()
        {
            if (!isActive) return;

            // Check for outside student disruption penalty
            if (Time.time >= nextOutsideCheckTime)
            {
                ProcessOutsideStudentDisruption();
                nextOutsideCheckTime = Time.time + outsideCheckInterval;
            }
            
            // Check disruption timeout
            CheckDisruptionTimeout();
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            Debug.Log($"[ClassroomManager] HandleGameStateChanged: {oldState} -> {newState}");
            
            if (newState == GameState.InLevel)
            {
                Debug.Log("[ClassroomManager] State is InLevel, activating classroom");
                ActivateClassroom();
            }
            else
            {
                Debug.Log($"[ClassroomManager] State is {newState}, deactivating classroom");
                DeactivateClassroom();
            }
        }

        private void ActivateClassroom()
        {
            isActive = true;
            ResetClassroom();
            Debug.Log($"[ClassroomManager] ✅ Classroom ACTIVATED - isActive: {isActive}");
        }

        private void DeactivateClassroom()
        {
            isActive = false;
            Debug.Log($"[ClassroomManager] ❌ Classroom DEACTIVATED - isActive: {isActive}");
            Debug.LogWarning("[ClassroomManager] DEACTIVATION STACK TRACE:", this);
        }

        private void ResetClassroom()
        {
            ChangeState(ClassroomState.Calm);
            DisruptionLevel = 0f;
            OnDisruptionChanged?.Invoke(DisruptionLevel);
            ResetOutsideTracking();
            ResetDisruptionTimeout();
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

        /// <summary>
        /// Registers a student as being outside the classroom
        /// </summary>
        public void RegisterStudentOutside(StudentAgent student)
        {
            if (student == null || studentsOutside.ContainsKey(student)) return;

            studentsOutside[student] = Time.time;
            OutsideStudentCount = studentsOutside.Count;
            OnOutsideStudentCountChanged?.Invoke(OutsideStudentCount);

            Debug.Log($"[ClassroomManager] {student.Config?.studentName} is now outside classroom (Total outside: {OutsideStudentCount})");
        }

        /// <summary>
        /// Unregisters a student as being outside (they returned to classroom)
        /// </summary>
        public void UnregisterStudentOutside(StudentAgent student)
        {
            if (student == null || !studentsOutside.ContainsKey(student)) return;

            float timeOutside = Time.time - studentsOutside[student];
            studentsOutside.Remove(student);
            OutsideStudentCount = studentsOutside.Count;
            OnOutsideStudentCountChanged?.Invoke(OutsideStudentCount);

            Debug.Log($"[ClassroomManager] {student.Config?.studentName} returned to classroom after {timeOutside:F1}s (Total outside: {OutsideStudentCount})");
        }

        /// <summary>
        /// Gets how long a student has been outside (in seconds)
        /// </summary>
        public float GetStudentOutsideDuration(StudentAgent student)
        {
            if (student == null || !studentsOutside.ContainsKey(student)) return 0f;
            return Time.time - studentsOutside[student];
        }

        /// <summary>
        /// Checks if a student is currently registered as outside
        /// </summary>
        public bool IsStudentOutside(StudentAgent student)
        {
            return student != null && studentsOutside.ContainsKey(student);
        }

        /// <summary>
        /// Processes continuous disruption increase from students outside
        /// </summary>
        private void ProcessOutsideStudentDisruption()
        {
            if (OutsideStudentCount == 0) return;

            // Calculate disruption to add
            float disruptionToAdd = OutsideStudentCount * outsideDisruptionRate;

            // Check if we've exceeded max penalty
            if (totalOutsideDisruptionApplied + disruptionToAdd > maxOutsideDisruptionPenalty)
            {
                disruptionToAdd = Mathf.Max(0, maxOutsideDisruptionPenalty - totalOutsideDisruptionApplied);
            }

            if (disruptionToAdd > 0.01f)
            {
                totalOutsideDisruptionApplied += disruptionToAdd;
                AddDisruption(disruptionToAdd, $"{OutsideStudentCount} student(s) outside");
                
                Debug.Log($"[ClassroomManager] {OutsideStudentCount} students are currently outside classroom");
            }
        }

        /// <summary>
        /// Resets the outside student tracking (called when level starts)
        /// </summary>
        private void ResetOutsideTracking()
        {
            studentsOutside.Clear();
            OutsideStudentCount = 0;
            totalOutsideDisruptionApplied = 0f;
            nextOutsideCheckTime = Time.time + outsideCheckInterval;
        }

        /// <summary>
        /// Checks if disruption has been above threshold for too long
        /// </summary>
        private void CheckDisruptionTimeout()
        {
            if (levelConfig == null || levelConfig.levelGoal == null) return;
            if (!levelConfig.levelGoal.enableDisruptionTimeout) return;

            float threshold = levelConfig.levelGoal.disruptionTimeoutThreshold;
            float timeoutDuration = levelConfig.levelGoal.disruptionTimeoutSeconds;
            float warningTime = levelConfig.levelGoal.disruptionTimeoutWarningSeconds;

            // Check if disruption is above threshold
            if (DisruptionLevel >= threshold)
            {
                // Start timeout if not already active
                if (!isDisruptionTimeoutActive)
                {
                    isDisruptionTimeoutActive = true;
                    disruptionTimeoutStartTime = Time.time;
                    hasShownTimeoutWarning = false;
                    Debug.LogWarning($"[ClassroomManager] ⚠️ Disruption Timeout Started! Disruption at {DisruptionLevel:F1}% (threshold: {threshold}%)");
                }

                // Calculate elapsed time
                float elapsedTime = Time.time - disruptionTimeoutStartTime;
                float remainingTime = timeoutDuration - elapsedTime;

                // Show warning
                if (!hasShownTimeoutWarning && remainingTime <= warningTime)
                {
                    hasShownTimeoutWarning = true;
                    OnDisruptionTimeoutWarning?.Invoke(remainingTime);
                    Debug.LogWarning($"[ClassroomManager] ⚠️ WARNING: {remainingTime:F0}s to reduce disruption below {threshold}%!");
                }

                // Check if timeout exceeded
                if (elapsedTime >= timeoutDuration)
                {
                    TriggerDisruptionTimeoutLose();
                }
            }
            else
            {
                // Reset timeout if disruption drops below threshold
                if (isDisruptionTimeoutActive)
                {
                    ResetDisruptionTimeout();
                    Debug.Log($"[ClassroomManager] ✅ Disruption Timeout Reset - disruption reduced to {DisruptionLevel:F1}%");
                }
            }
        }

        /// <summary>
        /// Resets disruption timeout tracking
        /// </summary>
        private void ResetDisruptionTimeout()
        {
            isDisruptionTimeoutActive = false;
            disruptionTimeoutStartTime = 0f;
            hasShownTimeoutWarning = false;
        }

        /// <summary>
        /// Triggers lose condition due to disruption timeout
        /// </summary>
        private void TriggerDisruptionTimeoutLose()
        {
            Debug.LogError($"[ClassroomManager] 💥 GAME OVER - Disruption stayed above {levelConfig.levelGoal.disruptionTimeoutThreshold}% for {levelConfig.levelGoal.disruptionTimeoutSeconds}s!");
            
            OnDisruptionTimeoutLose?.Invoke();
            
            // Transition to lose state
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.TransitionTo(GameState.LevelFailed);
            }
            
            isDisruptionTimeoutActive = false;
        }

        /// <summary>
        /// Gets remaining time before disruption timeout (0 if not active)
        /// </summary>
        public float GetDisruptionTimeoutRemaining()
        {
            if (!isDisruptionTimeoutActive || levelConfig == null || levelConfig.levelGoal == null)
                return 0f;

            float elapsed = Time.time - disruptionTimeoutStartTime;
            float remaining = levelConfig.levelGoal.disruptionTimeoutSeconds - elapsed;
            return Mathf.Max(0f, remaining);
        }

        /// <summary>
        /// Checks if disruption timeout is currently active
        /// </summary>
        public bool IsDisruptionTimeoutActive()
        {
            return isDisruptionTimeoutActive;
        }
    }
}

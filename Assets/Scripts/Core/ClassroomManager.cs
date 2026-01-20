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
            Debug.Log($"[ClassroomManager] ★ Awake called! Instance exists: {Instance != null}");

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ClassroomManager] Duplicate instance detected, destroying this one!");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[ClassroomManager] ★ Instance set successfully");
        }

        void OnEnable()
        {
            Debug.Log($"[ClassroomManager] ★ OnEnable! GameObject: {gameObject.name}, active: {gameObject.activeInHierarchy}");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                Debug.Log($"[ClassroomManager] Subscribed. Current GameState: {GameStateManager.Instance.CurrentState}");
            }
            else
            {
                Debug.LogWarning("[ClassroomManager] GameStateManager.Instance is NULL in OnEnable - will retry in Start()");
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

        private bool hasSubscribedToGameState = false;

        void Start()
        {
            Debug.Log("[ClassroomManager] Start called");

            // Auto-load levelConfig from LevelLoader if not assigned
            if (levelConfig == null && LevelLoader.Instance != null)
            {
                levelConfig = LevelLoader.Instance.CurrentLevel;
                if (levelConfig != null)
                {
                    Debug.Log($"[ClassroomManager] ★ Auto-loaded levelConfig from LevelLoader: {levelConfig.name}");
                    if (levelConfig.levelGoal != null)
                    {
                        Debug.Log($"[ClassroomManager] ★ levelGoal loaded: enableTimeout={levelConfig.levelGoal.enableDisruptionTimeout}, threshold={levelConfig.levelGoal.disruptionTimeoutThreshold}");
                    }
                }
                else
                {
                    Debug.LogWarning("[ClassroomManager] LevelLoader.CurrentLevel is NULL!");
                }
            }

            // Try to subscribe if we couldn't in OnEnable (timing issue)
            if (!hasSubscribedToGameState && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                Debug.Log($"[ClassroomManager] ★ Late subscription to GameStateManager. Current state: {GameStateManager.Instance.CurrentState}");

                // Check if already in InLevel
                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    Debug.Log("[ClassroomManager] Already in InLevel, activating classroom");
                    ActivateClassroom();
                }
            }
            else if (GameStateManager.Instance == null)
            {
                Debug.LogError("[ClassroomManager] GameStateManager STILL NULL in Start! Cannot track game state.");
            }
        }

        private float lastDebugLogTime = 0f;
        private bool hasLoggedInactive = false;

        void Update()
        {
            // Debug: Log even when inactive (every ~5 seconds)
            if (!isActive)
            {
                if (!hasLoggedInactive || Time.frameCount % 300 == 0)
                {
                    hasLoggedInactive = true;
                    Debug.LogWarning($"[ClassroomManager] ⚠️ isActive=FALSE! GameState: {GameStateManager.Instance?.CurrentState}");
                }
                return;
            }
            hasLoggedInactive = false;

            // Check for outside student disruption penalty
            if (Time.time >= nextOutsideCheckTime)
            {
                ProcessOutsideStudentDisruption();
                nextOutsideCheckTime = Time.time + outsideCheckInterval;
            }

            // DEBUG: Log disruption status every 5 seconds
            if (Time.time - lastDebugLogTime >= 5f)
            {
                lastDebugLogTime = Time.time;
                bool hasConfig = levelConfig != null;
                bool hasGoal = hasConfig && levelConfig.levelGoal != null;
                bool timeoutEnabled = hasGoal && levelConfig.levelGoal.enableDisruptionTimeout;
                float threshold = hasGoal ? levelConfig.levelGoal.disruptionTimeoutThreshold : -1;

                Debug.Log($"[ClassroomManager] ═══ STATUS CHECK ═══");
                Debug.Log($"[ClassroomManager] Disruption: {DisruptionLevel:F1}%");
                Debug.Log($"[ClassroomManager] Outside students: {OutsideStudentCount}");
                Debug.Log($"[ClassroomManager] levelConfig: {(hasConfig ? "OK" : "NULL")}");
                Debug.Log($"[ClassroomManager] levelGoal: {(hasGoal ? "OK" : "NULL")}");
                Debug.Log($"[ClassroomManager] enableDisruptionTimeout: {timeoutEnabled}");
                Debug.Log($"[ClassroomManager] Threshold: {threshold}% (current: {DisruptionLevel:F1}%)");
                Debug.Log($"[ClassroomManager] Timeout active: {isDisruptionTimeoutActive}");

                if (DisruptionLevel >= threshold && threshold > 0)
                {
                    Debug.LogWarning($"[ClassroomManager] ⚠️ DISRUPTION ABOVE THRESHOLD! Should trigger timeout!");
                }
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
            // Debug: Check if config is loaded
            if (levelConfig == null)
            {
                Debug.LogWarning("[ClassroomManager] CheckDisruptionTimeout: levelConfig is NULL!");
                return;
            }
            if (levelConfig.levelGoal == null)
            {
                Debug.LogWarning("[ClassroomManager] CheckDisruptionTimeout: levelGoal is NULL!");
                return;
            }
            if (!levelConfig.levelGoal.enableDisruptionTimeout)
            {
                // Only log once per session
                return;
            }

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
                    Debug.LogWarning($"[ClassroomManager] ⚠️ ⚠️ ⚠️ DISRUPTION TIMEOUT STARTED! ⚠️ ⚠️ ⚠️");
                    Debug.LogWarning($"[ClassroomManager] Disruption: {DisruptionLevel:F1}% ≥ Threshold: {threshold}%");
                    Debug.LogWarning($"[ClassroomManager] You have {timeoutDuration:F0} seconds to reduce disruption below {threshold}% or YOU WILL LOSE!");
                    Debug.LogWarning($"[ClassroomManager] Students outside: {OutsideStudentCount}");
                }

                // Calculate elapsed time
                float elapsedTime = Time.time - disruptionTimeoutStartTime;
                float remainingTime = timeoutDuration - elapsedTime;

                // Show warning
                if (!hasShownTimeoutWarning && remainingTime <= warningTime)
                {
                    hasShownTimeoutWarning = true;
                    OnDisruptionTimeoutWarning?.Invoke(remainingTime);
                    Debug.LogWarning($"[ClassroomManager] ⏰ ⏰ ⏰ FINAL WARNING! ⏰ ⏰ ⏰");
                    Debug.LogWarning($"[ClassroomManager] Only {remainingTime:F0} seconds left before LOSE!");
                    Debug.LogWarning($"[ClassroomManager] Current disruption: {DisruptionLevel:F1}% (must reduce below {threshold}%)");
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
            Debug.LogError($"[ClassroomManager] ═══════════════════════════════════════════════════");
            Debug.LogError($"[ClassroomManager] ❌ ❌ ❌ GAME OVER - YOU LOSE! ❌ ❌ ❌");
            Debug.LogError($"[ClassroomManager] ═══════════════════════════════════════════════════");
            Debug.LogError($"[ClassroomManager] Reason: Disruption Timeout");
            Debug.LogError($"[ClassroomManager] Disruption stayed above {levelConfig.levelGoal.disruptionTimeoutThreshold}% for {levelConfig.levelGoal.disruptionTimeoutSeconds} seconds!");
            Debug.LogError($"[ClassroomManager] Final disruption: {DisruptionLevel:F1}%");
            Debug.LogError($"[ClassroomManager] Students outside: {OutsideStudentCount}");
            Debug.LogError($"[ClassroomManager] ═══════════════════════════════════════════════════");

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

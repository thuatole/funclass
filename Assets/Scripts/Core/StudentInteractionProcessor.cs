using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Processes student-to-student interactions defined in level configuration
    /// Handles SingleStudent scope influences with specific source-target pairs
    /// Supports both random and scripted (time-based) events
    /// </summary>
    public class StudentInteractionProcessor : MonoBehaviour
    {
        public static StudentInteractionProcessor Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float checkInterval = 0.5f;  // Check every 0.5 seconds for time-based triggers

        [Header("One-Time Triggers")]
        [Tooltip("Each interaction only triggers once per level")]
        [SerializeField] private bool oneTimeOnly = true;

        [Header("Time-Based Triggers")]
        [Tooltip("Tolerance for time-based triggers (seconds)")]
        [SerializeField] private float timeTolerance = 0.5f;

        private List<StudentInteractionConfig> interactions = new List<StudentInteractionConfig>();
        private HashSet<string> triggeredInteractions = new HashSet<string>();
        private Dictionary<string, StudentAgent> studentsById = new Dictionary<string, StudentAgent>();
        private Dictionary<string, StudentAgent> studentsByName = new Dictionary<string, StudentAgent>();
        private float lastCheckTime = 0f;
        private bool isActive = false;
        private bool interactionsLoaded = false;
        private bool hasSubscribedToGameState = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            GameLogger.Milestone("StudentInteractionProcessor", "Awake - Instance created");
        }

        void Start()
        {
            GameLogger.Detail("StudentInteractionProcessor", $"Start - Interactions loaded: {interactions.Count}");
            
            // Retry subscription if failed in OnEnable
            if (!hasSubscribedToGameState && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                GameLogger.Detail("StudentInteractionProcessor", $"Late subscription - Current state: {GameStateManager.Instance.CurrentState}");
                
                // Check if already in InLevel
                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    GameLogger.Detail("StudentInteractionProcessor", "Already in InLevel, activating processor");
                    isActive = true;
                    RefreshStudentList();
                }
            }
        }

        void OnEnable()
        {
            GameLogger.Detail("StudentInteractionProcessor", "OnEnable called");
            
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                GameLogger.Detail("StudentInteractionProcessor", "Subscribed to GameStateManager");
            }
            else
            {
                GameLogger.Warning("StudentInteractionProcessor", "GameStateManager.Instance is null!");
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
                hasSubscribedToGameState = false;
            }
        }

        void Update()
        {
            if (!isActive || !interactionsLoaded || interactions.Count == 0) return;

            GameLogger.Trace("StudentInteractionProcessor", $"Update tick - checking {interactions.Count} interactions");

            if (Time.time - lastCheckTime >= checkInterval)
            {
                lastCheckTime = Time.time;
                CheckAndTriggerInteractions();
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            isActive = (newState == GameState.InLevel);

            if (isActive)
            {
                RefreshStudentList();
                GameLogger.Milestone("StudentInteractionProcessor", $"Activated - Found {studentsById.Count} students, {interactions.Count} interactions loaded");
            }
            else
            {
                GameLogger.Detail("StudentInteractionProcessor", "Deactivated");
            }
        }

        private void RefreshStudentList()
        {
            studentsById.Clear();
            studentsByName.Clear();
            StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();

            foreach (StudentAgent student in allStudents)
            {
                if (student.Config != null)
                {
                    studentsById[student.Config.studentId] = student;
                    studentsByName[student.Config.studentName] = student;
                }
            }

            GameLogger.Detail("StudentInteractionProcessor", $"Found {studentsById.Count} students");
        }

        /// <summary>
        /// Load student interactions from level configuration (Editor import path)
        /// </summary>
        public void LoadInteractions(List<StudentInteractionConfig> configs)
        {
            interactions.Clear();
            triggeredInteractions.Clear();
            interactionsLoaded = false;
            
            if (configs == null || configs.Count == 0)
            {
                GameLogger.Detail("StudentInteractionProcessor", "No interactions to load");
                return;
            }

            interactions.AddRange(configs);
            interactionsLoaded = true;
            GameLogger.Milestone("StudentInteractionProcessor", $"Loaded {interactions.Count} student interactions");

            foreach (var config in interactions)
            {
                GameLogger.Detail("StudentInteractionProcessor", 
                    $"  {config.sourceStudent} → {config.targetStudent} ({config.eventType}, {config.triggerCondition})");
            }
        }

        /// <summary>
        /// Load runtime student interactions from level JSON (Runtime path)
        /// </summary>
        public void LoadRuntimeInteractions(List<RuntimeStudentInteraction> runtimeConfigs)
        {
            interactions.Clear();
            triggeredInteractions.Clear();
            interactionsLoaded = false;

            if (runtimeConfigs == null || runtimeConfigs.Count == 0)
            {
                GameLogger.Detail("StudentInteractionProcessor", "No runtime interactions to load");
                return;
            }

            foreach (var runtimeConfig in runtimeConfigs)
            {
                 StudentInteractionConfig config = new StudentInteractionConfig
                 {
                     sourceStudent = runtimeConfig.sourceStudentId,
                     targetStudent = runtimeConfig.targetStudentId ?? runtimeConfig.sourceStudentId,
                     eventType = runtimeConfig.eventType,
                     triggerCondition = runtimeConfig.triggerCondition,
                     probability = runtimeConfig.probability,
                     customSeverity = runtimeConfig.triggerValue,  // Map triggerValue for time-based events
                     description = runtimeConfig.description
                 };
                interactions.Add(config);
            }

            interactionsLoaded = true;
            GameLogger.Milestone("StudentInteractionProcessor", $"Loaded {interactions.Count} runtime student interactions");
            
            foreach (var config in interactions)
            {
                GameLogger.Detail("StudentInteractionProcessor", 
                    $"  {config.sourceStudent} → {config.targetStudent} ({config.eventType}, trigger={config.triggerCondition}, value={config.customSeverity})");
            }
        }

        /// <summary>
        /// Get student by ID or name (supports both)
        /// </summary>
        private bool TryGetStudent(string identifier, out StudentAgent student)
        {
            // Try by ID first
            if (studentsById.TryGetValue(identifier, out student))
            {
                return true;
            }
            // Try by name
            if (studentsByName.TryGetValue(identifier, out student))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check all interactions and trigger if conditions met
        /// </summary>
        private void CheckAndTriggerInteractions()
        {
            GameLogger.Trace("StudentInteractionProcessor", $"Checking {interactions.Count} interactions");
            
            foreach (var interaction in interactions)
            {
                if (ShouldTriggerInteraction(interaction))
                {
                    TriggerInteraction(interaction);
                }
            }
        }

        /// <summary>
        /// Check if interaction should trigger based on conditions
        /// </summary>
        private bool ShouldTriggerInteraction(StudentInteractionConfig interaction)
        {
            // Get source student
            if (!TryGetStudent(interaction.sourceStudent, out StudentAgent source))
            {
                GameLogger.Trace("StudentInteractionProcessor", $"Skip - source student not found: {interaction.sourceStudent}");
                return false;
            }

            // Get target student (optional for self-events)
            StudentAgent target = null;
            if (!string.IsNullOrEmpty(interaction.targetStudent) && interaction.targetStudent != interaction.sourceStudent)
            {
                if (!TryGetStudent(interaction.targetStudent, out target))
                {
                    GameLogger.Trace("StudentInteractionProcessor", $"Skip - target student not found: {interaction.targetStudent}");
                    return false;
                }
            }

            // Check if already triggered (one-time only)
            string interactionKey = $"{interaction.sourceStudent}_{interaction.targetStudent}_{interaction.eventType}";
            if (oneTimeOnly && triggeredInteractions.Contains(interactionKey))
            {
                GameLogger.Trace("StudentInteractionProcessor", $"Skip - already triggered: {interactionKey}");
                return false;
            }

            // Check if source is immune
            if (source.IsImmuneToInfluence())
            {
                GameLogger.Trace("StudentInteractionProcessor", $"Skip - source immune: {interaction.sourceStudent}");
                return false;
            }

            // Check if source is following a route (skip for scripted time-based events)
            if (interaction.triggerCondition != "timeElapsed" && source.IsFollowingRoute)
            {
                GameLogger.Trace("StudentInteractionProcessor", $"Skip - source following route: {interaction.sourceStudent}");
                return false;
            }

            // Check trigger condition
            return CheckTriggerCondition(interaction, source, target);
        }

        /// <summary>
        /// Check specific trigger condition
        /// </summary>
        private bool CheckTriggerCondition(StudentInteractionConfig interaction, StudentAgent source, StudentAgent target)
        {
            float roll;

            switch (interaction.triggerCondition)
            {
                case "timeElapsed":
                    return CheckTimeElapsedCondition(interaction, source);

                case "Always":
                    roll = Random.value;
                    GameLogger.Detail("StudentInteractionProcessor", 
                        $"Always trigger - roll={roll:F3} <= prob={interaction.probability:F3}");
                    return roll <= interaction.probability;

                case "OnActingOut":
                    bool actingOut = source.CurrentState == StudentState.ActingOut;
                    roll = Random.value;
                    GameLogger.Detail("StudentInteractionProcessor", 
                        $"OnActingOut - state={source.CurrentState}, roll={roll:F3} <= prob={interaction.probability:F3}");
                    return actingOut && roll <= interaction.probability;

                case "OnCritical":
                    bool critical = source.CurrentState == StudentState.Critical;
                    roll = Random.value;
                    GameLogger.Detail("StudentInteractionProcessor", 
                        $"OnCritical - state={source.CurrentState}, roll={roll:F3} <= prob={interaction.probability:F3}");
                    return critical && roll <= interaction.probability;

                case "OnDistracted":
                    bool distracted = source.CurrentState == StudentState.Distracted;
                    roll = Random.value;
                    GameLogger.Detail("StudentInteractionProcessor", 
                        $"OnDistracted - state={source.CurrentState}, roll={roll:F3} <= prob={interaction.probability:F3}");
                    return distracted && roll <= interaction.probability;

                case "Random":
                    roll = Random.value;
                    GameLogger.Detail("StudentInteractionProcessor", 
                        $"Random - roll={roll:F3} <= prob={interaction.probability:F3}");
                    return roll <= interaction.probability;

                default:
                    GameLogger.Warning("StudentInteractionProcessor", $"Unknown trigger condition: {interaction.triggerCondition}");
                    return false;
            }
        }

        /// <summary>
        /// Check time-based trigger condition
        /// </summary>
        private bool CheckTimeElapsedCondition(StudentInteractionConfig interaction, StudentAgent source)
        {
            if (LevelManager.Instance == null || !LevelManager.Instance.IsLevelActive)
            {
                return false;
            }

            float elapsed = LevelManager.Instance.LevelTimeElapsed;
            float targetTime = interaction.customSeverity;

            // Check if we're within the tolerance window
            bool withinWindow = Mathf.Abs(elapsed - targetTime) <= timeTolerance;

            if (withinWindow)
            {
                GameLogger.Milestone("StudentInteractionProcessor", 
                    $"Time-based trigger: {interaction.sourceStudent} → {interaction.targetStudent} ({interaction.eventType}) at {elapsed:F1}s");
            }
            else
            {
                GameLogger.Trace("StudentInteractionProcessor", 
                    $"Time check - elapsed={elapsed:F1}, target={targetTime:F1}, withinWindow={withinWindow}");
            }

            return withinWindow;
        }

        /// <summary>
        /// Get trigger value from config (handles different field names)
        /// </summary>
        private float GetTriggerValue(StudentInteractionConfig interaction)
        {
            // Try customSeverity first (used in Editor config), then check for triggerValue
            if (interaction.customSeverity > 0 && interaction.customSeverity < 1000)
            {
                return interaction.customSeverity;
            }
            // Default to 30 seconds if not specified
            return 30f;
        }

        /// <summary>
        /// Trigger the interaction event
        /// </summary>
        private void TriggerInteraction(StudentInteractionConfig interaction)
        {
            string interactionKey = $"{interaction.sourceStudent}_{interaction.targetStudent}_{interaction.eventType}";
            triggeredInteractions.Add(interactionKey);

            GameLogger.Milestone("StudentInteractionProcessor", 
                $"Triggered: {interaction.sourceStudent} → {interaction.targetStudent} ({interaction.eventType})");

            if (!TryGetStudent(interaction.sourceStudent, out StudentAgent source))
            {
                GameLogger.Warning("StudentInteractionProcessor", $"Source student not found: {interaction.sourceStudent}");
                return;
            }

            StudentAgent target = null;
            if (!string.IsNullOrEmpty(interaction.targetStudent) && interaction.targetStudent != interaction.sourceStudent)
            {
                TryGetStudent(interaction.targetStudent, out target);
            }

            // Parse event type
            if (!System.Enum.TryParse(interaction.eventType, out StudentEventType eventType))
            {
                GameLogger.Warning("StudentInteractionProcessor", $"Invalid event type: {interaction.eventType}");
                return;
            }

            // Create event - target can be null for self-events or WholeClass scope
            InfluenceScope scope = target != null ? InfluenceScope.SingleStudent : InfluenceScope.WholeClass;

            if (StudentEventManager.Instance != null)
            {
                StudentEvent evt = new StudentEvent(
                    source,
                    eventType,
                    interaction.description ?? $"{interaction.sourceStudent} {interaction.eventType}",
                    targetObject: null,
                    targetStudent: target,
                    scope: scope
                );

                StudentEventManager.Instance.LogEvent(evt);
            }
        }
    }

    /// <summary>
    /// Configuration for a student-to-student interaction
    /// </summary>
    [System.Serializable]
    public class StudentInteractionConfig
    {
        public string sourceStudent;
        public string targetStudent;
        public string eventType;
        public string triggerCondition;
        public float probability = 1.0f;
        public float customSeverity = -1f;  // Can be used for time values
        public string description;
    }
}

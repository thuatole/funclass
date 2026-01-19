using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Processes student-to-student interactions defined in level configuration
    /// Handles SingleStudent scope influences with specific source-target pairs
    /// </summary>
    public class StudentInteractionProcessor : MonoBehaviour
    {
        public static StudentInteractionProcessor Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float checkInterval = 2f; // Check every 2 seconds

        [Header("One-Time Triggers")]
        [Tooltip("Each interaction only triggers once per level")]
        [SerializeField] private bool oneTimeOnly = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private List<StudentInteractionConfig> interactions = new List<StudentInteractionConfig>();
        private HashSet<string> triggeredInteractions = new HashSet<string>();
        private Dictionary<string, StudentAgent> studentsByName = new Dictionary<string, StudentAgent>();
        private float lastCheckTime = 0f;
        private bool isActive = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Log("[StudentInteractionProcessor] Awake - Instance created");
        }

        void Start()
        {
            Log($"[StudentInteractionProcessor] Start - Interactions loaded: {interactions.Count}");
        }

        void OnEnable()
        {
            Log("[StudentInteractionProcessor] OnEnable called");
            
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                Log("[StudentInteractionProcessor] Subscribed to GameStateManager");
            }
            else
            {
                Log("[StudentInteractionProcessor] WARNING: GameStateManager.Instance is null!");
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
            if (!isActive || interactions.Count == 0) return;

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
                Log("[StudentInteractionProcessor] Activated");
            }
            else
            {
                Log("[StudentInteractionProcessor] Deactivated");
            }
        }

        private void RefreshStudentList()
        {
            studentsByName.Clear();
            StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();

            foreach (StudentAgent student in allStudents)
            {
                if (student.Config != null)
                {
                    studentsByName[student.Config.studentName] = student;
                }
            }

            Log($"[StudentInteractionProcessor] Found {studentsByName.Count} students");
        }

        /// <summary>
        /// Load student interactions from level configuration
        /// </summary>
        public void LoadInteractions(List<StudentInteractionConfig> configs)
        {
            interactions.Clear();
            
            if (configs == null || configs.Count == 0)
            {
                Log("[StudentInteractionProcessor] No interactions to load");
                return;
            }

            interactions.AddRange(configs);
            Log($"[StudentInteractionProcessor] Loaded {interactions.Count} student interactions");

            foreach (var config in interactions)
            {
                Log($"[StudentInteractionProcessor]   - {config.sourceStudent} → {config.targetStudent} ({config.eventType}, {config.triggerCondition}, prob: {config.probability})");
            }
        }

        /// <summary>
        /// Check all interactions and trigger if conditions met
        /// </summary>
        private void CheckAndTriggerInteractions()
        {
            Log($"[StudentInteractionProcessor] >>> Checking {interactions.Count} interactions");
            
            foreach (var interaction in interactions)
            {
                Log($"[StudentInteractionProcessor] Checking: {interaction.sourceStudent} → {interaction.targetStudent} ({interaction.triggerCondition})");
                
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
            // Get source and target students
            if (!studentsByName.TryGetValue(interaction.sourceStudent, out StudentAgent source))
            {
                Log($"[StudentInteractionProcessor]   ✗ Source student not found: {interaction.sourceStudent}");
                return false;
            }

            if (!studentsByName.TryGetValue(interaction.targetStudent, out StudentAgent target))
            {
                Log($"[StudentInteractionProcessor]   ✗ Target student not found: {interaction.targetStudent}");
                return false;
            }

            // Check if source is immune
            if (source.IsImmuneToInfluence())
            {
                Log($"[StudentInteractionProcessor]   ✗ {interaction.sourceStudent} is immune");
                return false;
            }

            // Check if source is following a route
            if (source.IsFollowingRoute)
            {
                Log($"[StudentInteractionProcessor]   ✗ {interaction.sourceStudent} is following route");
                return false;
            }

            // Check if source and target are in same location
            if (!StudentLocationHelper.AreInSameLocation(source, target))
            {
                string sourceLocation = StudentLocationHelper.GetLocationString(source);
                string targetLocation = StudentLocationHelper.GetLocationString(target);
                Log($"[StudentInteractionProcessor]   ✗ Different locations: {interaction.sourceStudent} ({sourceLocation}) vs {interaction.targetStudent} ({targetLocation})");
                return false;
            }

            // Check trigger condition
            bool conditionMet = false;
            string sourceState = source.CurrentState.ToString();

            switch (interaction.triggerCondition)
            {
                case "Always":
                    conditionMet = true;
                    break;

                case "OnActingOut":
                    conditionMet = (source.CurrentState == StudentState.ActingOut);
                    break;

                case "OnCritical":
                    conditionMet = (source.CurrentState == StudentState.Critical);
                    break;

                case "Random":
                    conditionMet = true; // Will be filtered by probability
                    break;

                default:
                    Log($"[StudentInteractionProcessor]   ✗ Unknown trigger condition: {interaction.triggerCondition}");
                    return false;
            }

            if (!conditionMet)
            {
                Log($"[StudentInteractionProcessor]   ✗ Condition not met: {interaction.triggerCondition} (current state: {sourceState})");
                return false;
            }

            // Check probability
            float roll = Random.value;
            if (roll > interaction.probability)
            {
                Log($"[StudentInteractionProcessor]   ✗ Probability check failed: {roll:F2} > {interaction.probability}");
                return false;
            }

            Log($"[StudentInteractionProcessor]   All checks passed! (state: {sourceState}, roll: {roll:F2} <= {interaction.probability})");
            return true;
        }

        /// <summary>
        /// Trigger the interaction event
        /// </summary>
        private void TriggerInteraction(StudentInteractionConfig interaction)
        {
            // Check if already triggered (one-time only)
            string interactionKey = $"{interaction.sourceStudent}_{interaction.targetStudent}_{interaction.eventType}";
            if (oneTimeOnly && triggeredInteractions.Contains(interactionKey))
            {
                Log($"[StudentInteractionProcessor]   Already triggered (one-time only): {interactionKey}");
                return;
            }

            triggeredInteractions.Add(interactionKey);
            Log($"[StudentInteractionProcessor] >>> Triggering: {interaction.sourceStudent} → {interaction.targetStudent} ({interaction.eventType})");

            if (!studentsByName.TryGetValue(interaction.sourceStudent, out StudentAgent source))
            {
                return;
            }

            if (!studentsByName.TryGetValue(interaction.targetStudent, out StudentAgent target))
            {
                return;
            }

            Log($"[StudentInteractionProcessor] >>> Triggering: {interaction.sourceStudent} → {interaction.targetStudent} ({interaction.eventType})");

            // Parse event type
            if (!System.Enum.TryParse(interaction.eventType, out StudentEventType eventType))
            {
                Log($"[StudentInteractionProcessor] Invalid event type: {interaction.eventType}");
                return;
            }

            // Create event with target student for SingleStudent scope
            if (StudentEventManager.Instance != null)
            {
                StudentEvent evt = new StudentEvent(
                    source,
                    eventType,
                    interaction.description ?? $"{interaction.sourceStudent} {interaction.eventType} {interaction.targetStudent}",
                    targetObject: null,
                    targetStudent: target,
                    scope: InfluenceScope.SingleStudent
                );

                StudentEventManager.Instance.LogEvent(evt);
                Log($"[StudentInteractionProcessor] ✓ Triggered interaction event");
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
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
        public float customSeverity = -1f;
        public string description;
    }
}

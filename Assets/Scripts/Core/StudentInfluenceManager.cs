using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Manages peer influence between students. When a student performs a significant action,
    /// nearby students may be affected based on distance, severity, and their own susceptibility.
    /// This is a standalone layer that listens to existing events without modifying core logic.
    /// </summary>
    public class StudentInfluenceManager : MonoBehaviour
    {
        public static StudentInfluenceManager Instance { get; private set; }

        [Header("Influence Settings")]
        [Tooltip("Maximum distance for influence to propagate")]
        [SerializeField] private float maxInfluenceRadius = 6f;

        [Tooltip("Distance thresholds for influence strength")]
        [SerializeField] private float strongInfluenceDistance = 2f;
        [SerializeField] private float mediumInfluenceDistance = 4f;

        [Header("Influence Strength Multipliers")]
        [SerializeField] private float strongInfluenceMultiplier = 1.0f;
        [SerializeField] private float mediumInfluenceMultiplier = 0.6f;
        [SerializeField] private float weakInfluenceMultiplier = 0.3f;

        [Header("Mess Influence Settings")]
        [Tooltip("Base severity for vomit/mess influence events")]
        [Range(0f, 1f)]
        [SerializeField] private float vomitInfluenceBaseSeverity = 0.85f;
        
        [Tooltip("Maximum radius for vomit panic influence")]
        [SerializeField] private float vomitPanicRadius = 6f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private bool isActive = false;
        private List<StudentAgent> allStudents = new List<StudentAgent>();

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
                ActivateInfluenceSystem();
            }
            else
            {
                DeactivateInfluenceSystem();
            }
        }

        private void ActivateInfluenceSystem()
        {
            isActive = true;
            RefreshStudentList();
            Log("[StudentInfluenceManager] Influence system activated");
        }

        private void DeactivateInfluenceSystem()
        {
            isActive = false;
            allStudents.Clear();
            Log("[StudentInfluenceManager] Influence system deactivated");
        }

        private void RefreshStudentList()
        {
            allStudents.Clear();
            StudentAgent[] students = FindObjectsOfType<StudentAgent>();
            allStudents.AddRange(students);
            Log($"[StudentInfluenceManager] Found {allStudents.Count} students in scene");
        }

        private void HandleStudentEvent(StudentEvent evt)
        {
            if (!isActive || evt.student == null) return;

            if (IsInfluenceTrigger(evt.eventType))
            {
                ProcessInfluence(evt);
            }
        }

        /// <summary>
        /// Determines if an event type should trigger peer influence
        /// </summary>
        private bool IsInfluenceTrigger(StudentEventType eventType)
        {
            return eventType switch
            {
                StudentEventType.ThrowingObject => true,
                StudentEventType.MakingNoise => true,
                StudentEventType.KnockedOverObject => true,
                StudentEventType.LeftSeat => true,
                StudentEventType.WanderingAround => true,
                StudentEventType.MessCreated => true,
                StudentEventType.StudentReacted when IsHighIntensityReaction(eventType) => true,
                _ => false
            };
        }

        private bool IsHighIntensityReaction(StudentEventType eventType)
        {
            // Can be extended to check reaction intensity
            return false;
        }

        /// <summary>
        /// Gets the base influence severity for an event type
        /// </summary>
        private float GetInfluenceSeverity(StudentEventType eventType)
        {
            return eventType switch
            {
                StudentEventType.ThrowingObject => 0.9f,
                StudentEventType.MessCreated => vomitInfluenceBaseSeverity,
                StudentEventType.KnockedOverObject => 0.7f,
                StudentEventType.MakingNoise => 0.6f,
                StudentEventType.LeftSeat => 0.5f,
                StudentEventType.WanderingAround => 0.4f,
                _ => 0.3f
            };
        }

        /// <summary>
        /// Main influence processing method
        /// </summary>
        private void ProcessInfluence(StudentEvent evt)
        {
            StudentAgent sourceStudent = evt.student;
            Vector3 sourcePosition = sourceStudent.transform.position;
            float baseSeverity = GetInfluenceSeverity(evt.eventType);

            // Use special radius for mess events
            float influenceRadius = (evt.eventType == StudentEventType.MessCreated) 
                ? vomitPanicRadius 
                : maxInfluenceRadius;

            if (evt.eventType == StudentEventType.MessCreated)
            {
                Log($"[Influence] Mess created by {sourceStudent.Config?.studentName} triggered panic influence");
            }
            else
            {
                Log($"[Influence] {sourceStudent.Config?.studentName} triggered influence event: {evt.eventType}");
            }

            List<StudentAgent> affectedStudents = FindNearbyStudents(sourceStudent, influenceRadius);

            if (affectedStudents.Count == 0)
            {
                Log($"[Influence] No nearby students within {influenceRadius}m");
                return;
            }

            Log($"[Influence] {affectedStudents.Count} nearby students detected within {influenceRadius}m");

            foreach (StudentAgent targetStudent in affectedStudents)
            {
                float distance = Vector3.Distance(sourcePosition, targetStudent.transform.position);
                float influenceStrength = CalculateInfluenceStrength(distance, baseSeverity, targetStudent);

                if (influenceStrength > 0.01f)
                {
                    ApplyInfluence(targetStudent, sourceStudent, influenceStrength, evt.eventType);
                }
            }
        }

        /// <summary>
        /// Finds all students within the specified radius, excluding the source student
        /// </summary>
        private List<StudentAgent> FindNearbyStudents(StudentAgent sourceStudent, float radius)
        {
            List<StudentAgent> nearbyStudents = new List<StudentAgent>();
            Vector3 sourcePosition = sourceStudent.transform.position;

            foreach (StudentAgent student in allStudents)
            {
                if (student == sourceStudent || student == null || student.Config == null)
                    continue;

                float distance = Vector3.Distance(sourcePosition, student.transform.position);
                if (distance <= radius)
                {
                    nearbyStudents.Add(student);
                }
            }

            return nearbyStudents;
        }

        /// <summary>
        /// Calculates influence strength based on distance, severity, and target susceptibility
        /// </summary>
        private float CalculateInfluenceStrength(float distance, float baseSeverity, StudentAgent targetStudent)
        {
            // Distance-based multiplier
            float distanceMultiplier;
            if (distance <= strongInfluenceDistance)
            {
                distanceMultiplier = strongInfluenceMultiplier;
            }
            else if (distance <= mediumInfluenceDistance)
            {
                distanceMultiplier = mediumInfluenceMultiplier;
            }
            else
            {
                distanceMultiplier = weakInfluenceMultiplier;
            }

            // Apply distance falloff
            float normalizedDistance = distance / maxInfluenceRadius;
            float falloff = 1f - normalizedDistance;

            // Get target's susceptibility from config
            float susceptibility = targetStudent.Config.influenceSusceptibility;
            float resistance = targetStudent.Config.influenceResistance;

            // Calculate final strength
            float strength = baseSeverity * distanceMultiplier * falloff * susceptibility * (1f - resistance);

            return Mathf.Clamp01(strength);
        }

        /// <summary>
        /// Applies influence effects to a target student
        /// </summary>
        private void ApplyInfluence(StudentAgent targetStudent, StudentAgent sourceStudent, float strength, StudentEventType triggerEvent)
        {
            if (targetStudent.Config == null) return;

            string targetName = targetStudent.Config.studentName;
            string sourceName = sourceStudent.Config?.studentName ?? "Unknown";
            float distance = Vector3.Distance(sourceStudent.transform.position, targetStudent.transform.position);

            Log($"[Influence] {targetName} affected by {sourceName} at {distance:F1}m with strength {strength:F2}");

            // Determine effect based on strength and student's panic threshold
            if (strength >= targetStudent.Config.panicThreshold)
            {
                ApplyStrongInfluence(targetStudent, strength, sourceName);
            }
            else if (strength >= 0.3f)
            {
                ApplyMediumInfluence(targetStudent, strength, sourceName);
            }
            else
            {
                ApplyWeakInfluence(targetStudent, strength, sourceName);
            }
        }

        /// <summary>
        /// Applies strong influence effects (high strength or panic threshold exceeded)
        /// </summary>
        private void ApplyStrongInfluence(StudentAgent targetStudent, float strength, string sourceName)
        {
            string targetName = targetStudent.Config.studentName;

            // Strong influence can escalate state
            if (targetStudent.CurrentState != StudentState.Critical)
            {
                StudentState oldState = targetStudent.CurrentState;
                targetStudent.EscalateState();
                Log($"[Influence] {targetName} escalated from {oldState} to {targetStudent.CurrentState} (strong influence from {sourceName})");
            }

            // Trigger panic or scared reaction
            if (strength >= 0.8f)
            {
                targetStudent.TriggerReaction(StudentReactionType.Scared, 4f);
                Log($"[Influence] {targetName} → reaction Scared (strength {strength:F2})");
                
                // Trigger escape behavior if panic threshold exceeded and escape route available
                if (strength >= targetStudent.Config.panicThreshold && LevelManager.Instance != null)
                {
                    LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
                    if (currentLevel != null && currentLevel.escapeRoute != null)
                    {
                        targetStudent.StartRoute(currentLevel.escapeRoute);
                        Log($"[Influence] {targetName} started escape route due to panic");
                    }
                }
            }
            else
            {
                targetStudent.TriggerReaction(StudentReactionType.Confused, 3f);
                Log($"[Influence] {targetName} → reaction Confused (strength {strength:F2})");
            }
        }

        /// <summary>
        /// Applies medium influence effects
        /// </summary>
        private void ApplyMediumInfluence(StudentAgent targetStudent, float strength, string sourceName)
        {
            string targetName = targetStudent.Config.studentName;

            // Medium influence may escalate state based on current state
            if (targetStudent.CurrentState == StudentState.Calm && UnityEngine.Random.value < strength)
            {
                StudentState oldState = targetStudent.CurrentState;
                targetStudent.EscalateState();
                Log($"[Influence] {targetName} escalated from {oldState} to {targetStudent.CurrentState} (medium influence from {sourceName})");
            }
            else if (targetStudent.CurrentState == StudentState.Distracted && strength >= 0.5f)
            {
                StudentState oldState = targetStudent.CurrentState;
                targetStudent.EscalateState();
                Log($"[Influence] {targetName} escalated from {oldState} to {targetStudent.CurrentState} (medium influence from {sourceName})");
            }

            // Trigger reaction
            if (UnityEngine.Random.value < 0.6f)
            {
                targetStudent.TriggerReaction(StudentReactionType.Confused, 2f);
                Log($"[Influence] {targetName} → reaction Confused (strength {strength:F2})");
            }
        }

        /// <summary>
        /// Applies weak influence effects
        /// </summary>
        private void ApplyWeakInfluence(StudentAgent targetStudent, float strength, string sourceName)
        {
            string targetName = targetStudent.Config.studentName;

            // Weak influence only affects calm students occasionally
            if (targetStudent.CurrentState == StudentState.Calm && UnityEngine.Random.value < strength * 0.5f)
            {
                StudentState oldState = targetStudent.CurrentState;
                targetStudent.EscalateState();
                Log($"[Influence] {targetName} escalated from {oldState} to {targetStudent.CurrentState} (weak influence from {sourceName})");
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Public method to manually refresh the student list (useful after spawning new students)
        /// </summary>
        public void ForceRefreshStudentList()
        {
            if (isActive)
            {
                RefreshStudentList();
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using FunClass.Core.UI;

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

        [Header("SingleStudent Influence Settings")]
        [Tooltip("Maximum distance for SingleStudent influence to apply")]
        [SerializeField] private float singleStudentMaxDistance = 2f;

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
            Log("[StudentInfluenceManager] Start() called");
            
            // Fallback subscription if OnEnable was called before GameStateManager existed
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                Log("[StudentInfluenceManager] Subscribed to GameStateManager in Start()");
            }
            
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.OnEventLogged -= HandleStudentEvent;
                StudentEventManager.Instance.OnEventLogged += HandleStudentEvent;
                Log("[StudentInfluenceManager] Subscribed to StudentEventManager in Start()");
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            Log($"[StudentInfluenceManager] HandleGameStateChanged: {oldState} -> {newState}");
            
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
            Log($"[StudentInfluenceManager] HandleStudentEvent called - Type: {evt.eventType}, Student: {evt.student?.Config?.studentName}, Active: {isActive}");
            
            if (!isActive)
            {
                Log("[StudentInfluenceManager] Event ignored - system not active");
                return;
            }
            
            if (evt.student == null)
            {
                Log("[StudentInfluenceManager] Event ignored - student is null");
                return;
            }

            // Handle StudentCalmed event - resolve influence sources from this student
            if (evt.eventType == StudentEventType.StudentCalmed)
            {
                ResolveInfluenceSourcesFromStudent(evt.student);
            }

            // Handle MessCleaned event - resolve influence sources from student who created the mess
            if (evt.eventType == StudentEventType.MessCleaned)
            {
                if (evt.student != null)
                {
                    Log($"[StudentInfluenceManager] Mess cleaned - resolving sources from {evt.student.Config?.studentName}");
                    ResolveInfluenceSourcesFromStudent(evt.student);
                }
            }

            if (IsInfluenceTrigger(evt.eventType))
            {
                Log($"[StudentInfluenceManager] Processing influence for {evt.eventType}");
                ProcessInfluence(evt);
            }
            else
            {
                Log($"[StudentInfluenceManager] Event type {evt.eventType} is not an influence trigger");
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
                StudentEventType.LeftSeat => false,  // No influence - just state change
                StudentEventType.WanderingAround => false,  // No influence - can't be resolved by calming
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
        /// Handles WholeClass and SingleStudent scopes WITHOUT distance checks
        /// </summary>
        private void ProcessInfluence(StudentEvent evt)
        {
            StudentAgent sourceStudent = evt.student;
            float baseSeverity = GetInfluenceSeverity(evt.eventType);
            InfluenceScope scope = evt.influenceScope;

            Log($"[Influence] {sourceStudent.Config?.studentName} triggered {scope} influence: {evt.eventType}");

            if (scope == InfluenceScope.None)
            {
                Log($"[Influence] Event has no influence scope, skipping");
                return;
            }

            if (scope == InfluenceScope.SingleStudent)
            {
                // Single student influence - WITH distance check (<= 2m)
                if (evt.targetStudent != null)
                {
                    // Check distance between source and target
                    float distance = Vector3.Distance(sourceStudent.transform.position, evt.targetStudent.transform.position);

                    if (distance > singleStudentMaxDistance)
                    {
                        Log($"[Influence] SingleStudent: {sourceStudent.Config?.studentName} → {evt.targetStudent.Config?.studentName} BLOCKED - distance {distance:F2}m > {singleStudentMaxDistance}m");
                        Log($"[Influence] ✗ Influence not applied - source too far from target");
                        return;
                    }

                    Log($"[Influence] SingleStudent: {sourceStudent.Config?.studentName} → {evt.targetStudent.Config?.studentName} (distance: {distance:F2}m <= {singleStudentMaxDistance}m ✓)");

                    // Check if target has config
                    if (evt.targetStudent.Config == null)
                    {
                        Debug.LogError($"[Influence] Target student {evt.targetStudent.name} has NULL Config!");
                        return;
                    }

                    // Calculate influence strength based on susceptibility and resistance only
                    float susceptibility = evt.targetStudent.Config.influenceSusceptibility;
                    float resistance = evt.targetStudent.Config.influenceResistance;
                    float influenceStrength = baseSeverity * susceptibility * (1f - resistance);

                    Log($"[Influence] Calculating strength: base={baseSeverity:F2}, susceptibility={susceptibility:F2}, resistance={resistance:F2}");
                    Log($"[Influence] Result strength: {influenceStrength:F2}");

                    if (influenceStrength > 0.01f)
                    {
                        Log($"[Influence] Adding influence source...");
                        // Add influence source tracking
                        evt.targetStudent.InfluenceSources.AddSource(sourceStudent, evt.eventType, influenceStrength);
                        ApplyInfluence(evt.targetStudent, sourceStudent, influenceStrength, evt.eventType);

                        // Update influence icons
                        UpdateInfluenceIcons(sourceStudent, evt.targetStudent, true);

                        Log($"[Influence] ✓ Added source and applied influence");
                    }
                    else
                    {
                        Log($"[Influence] Strength too low ({influenceStrength:F2}), not adding source");
                    }
                }
                else
                {
                    Log($"[Influence] Warning: SingleStudent scope but no target student specified");
                }
            }
            else if (scope == InfluenceScope.WholeClass)
            {
                // Whole class influence - affects students in SAME LOCATION only
                StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();
                int affectedCount = 0;
                int skippedDifferentLocation = 0;

                string sourceLocation = StudentLocationHelper.GetLocationString(sourceStudent);
                Log($"[Influence] WholeClass: {sourceStudent.Config?.studentName} ({sourceLocation}) affects students in same location");

                foreach (StudentAgent targetStudent in allStudents)
                {
                    // Skip self
                    if (targetStudent == sourceStudent) continue;

                    // Check if in same location (inside/outside)
                    if (!StudentLocationHelper.AreInSameLocation(sourceStudent, targetStudent))
                    {
                        string targetLocation = StudentLocationHelper.GetLocationString(targetStudent);
                        Log($"[Influence]   ✗ {targetStudent.Config?.studentName} ({targetLocation}) - different location, no influence");
                        skippedDifferentLocation++;
                        continue;
                    }

                    // Calculate influence strength
                    float susceptibility = targetStudent.Config.influenceSusceptibility;
                    float resistance = targetStudent.Config.influenceResistance;
                    float influenceStrength = baseSeverity * susceptibility * (1f - resistance);

                    if (influenceStrength > 0.01f)
                    {
                        // Add influence source tracking
                        targetStudent.InfluenceSources.AddSource(sourceStudent, evt.eventType, influenceStrength);
                        ApplyInfluence(targetStudent, sourceStudent, influenceStrength, evt.eventType);

                        // Update influence icons
                        UpdateInfluenceIcons(sourceStudent, targetStudent, true);

                        affectedCount++;
                    }
                }

                Log($"[Influence] WholeClass affected {affectedCount} students (skipped {skippedDifferentLocation} in different location)");
            }
        }

        /// <summary>
        /// Resolve influence sources from a student who has been calmed down
        /// This marks the student's influence on others as resolved
        /// </summary>
        private void ResolveInfluenceSourcesFromStudent(StudentAgent calmedStudent)
        {
            Log($"[Influence] === TEACHER ACTION: Calming {calmedStudent.Config?.studentName} ===");
            Log($"[Influence] Resolving influence sources from {calmedStudent.Config?.studentName}");

            int resolvedCount = 0;
            foreach (StudentAgent student in allStudents)
            {
                if (student == null || student == calmedStudent) continue;

                if (student.InfluenceSources != null)
                {
                    int beforeCount = student.InfluenceSources.GetUnresolvedSourceCount();
                    student.InfluenceSources.ResolveSource(calmedStudent);
                    int afterCount = student.InfluenceSources.GetUnresolvedSourceCount();

                    if (beforeCount > afterCount)
                    {
                        resolvedCount++;

                        // Update influence icons - calmedStudent no longer influencing this student
                        UpdateInfluenceIcons(calmedStudent, student, false);

                        Log($"[Influence] ✓ Resolved {calmedStudent.Config?.studentName}'s influence on {student.Config?.studentName}");
                        Log($"[Influence]   {student.Config?.studentName} now has {afterCount} unresolved source(s)");
                    }
                }
            }

            // Hide influencer icon on calmed student
            var calmedIcon = calmedStudent.GetComponent<InfluenceStatusIcon>();
            if (calmedIcon != null)
            {
                calmedIcon.HideAllIcons();
            }

            Log($"[Influence] === Teacher calmed {calmedStudent.Config?.studentName} - resolved influence on {resolvedCount} student(s) ===");
        }

        /// <summary>
        /// Updates influence status icons on source and target students
        /// </summary>
        private void UpdateInfluenceIcons(StudentAgent sourceStudent, StudentAgent targetStudent, bool isAddingInfluence)
        {
            // Update source student's influencer icon
            var sourceIcon = sourceStudent.GetComponent<InfluenceStatusIcon>();
            if (sourceIcon != null)
            {
                if (isAddingInfluence)
                {
                    sourceIcon.ShowInfluencerIcon(targetStudent);
                }
                else
                {
                    sourceIcon.OnInfluenceResolved(targetStudent);
                }
            }

            // Target student's influenced icon is updated automatically via polling in InfluenceStatusIcon.Update()
        }

        /// <summary>
        /// Finds all students within the specified radius, excluding the source student
        /// </summary>
        private List<StudentAgent> FindNearbyStudents(StudentAgent sourceStudent, float radius)
        {
            List<StudentAgent> nearbyStudents = new List<StudentAgent>();
            Vector3 sourcePosition = sourceStudent.transform.position;
            
            Log($"[FindNearbyStudents] Searching for students near {sourceStudent.Config?.studentName} within {radius}m. Total students in list: {allStudents.Count}");

            foreach (StudentAgent student in allStudents)
            {
                if (student == sourceStudent)
                {
                    Log($"[FindNearbyStudents] Skipping source student: {student.Config?.studentName}");
                    continue;
                }
                
                if (student == null)
                {
                    Log($"[FindNearbyStudents] Found null student in list");
                    continue;
                }
                
                if (student.Config == null)
                {
                    Log($"[FindNearbyStudents] Student has null config: {student.gameObject.name}");
                    continue;
                }

                float distance = Vector3.Distance(sourcePosition, student.transform.position);
                Log($"[FindNearbyStudents] {student.Config.studentName} at distance {distance:F2}m (radius: {radius}m)");
                
                if (distance <= radius)
                {
                    nearbyStudents.Add(student);
                    Log($"[FindNearbyStudents] ✓ Added {student.Config.studentName} to affected list");
                }
            }
            
            Log($"[FindNearbyStudents] Found {nearbyStudents.Count} nearby students");
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

            // Check if student is immune to influence (e.g., after being calmed down)
            if (targetStudent.IsImmuneToInfluence())
            {
                Log($"[Influence] {targetName} is immune to influence, skipping");
                return;
            }

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
            if (strength >= 0.5f)
            {
                targetStudent.TriggerReaction(StudentReactionType.Scared, 4f);
                Log($"[Influence] {targetName} → reaction Scared (strength {strength:F2})");
                
                Log($"[Influence] Checking escape conditions - strength: {strength:F2}, panicThreshold: {targetStudent.Config.panicThreshold}, LevelManager exists: {LevelManager.Instance != null}");
                
                // Trigger escape behavior if panic threshold exceeded and escape route available
                if (strength >= targetStudent.Config.panicThreshold)
                {
                    Log($"[Influence] Strength >= panicThreshold, checking LevelManager...");
                    
                    if (LevelManager.Instance != null)
                    {
                        Log($"[Influence] LevelManager exists, getting CurrentLevel...");
                        LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
                        
                        if (currentLevel != null && currentLevel.escapeRoute != null)
                        {
                            Log($"[Influence] === ESCAPE TRIGGERED ===");
                            Log($"[Influence] Student: {targetName}");
                            Log($"[Influence] Current State: {targetStudent.CurrentState}");
                            Log($"[Influence] Influence Strength: {strength:F2}");
                            Log($"[Influence] Panic Threshold: {targetStudent.Config.panicThreshold}");
                            Log($"[Influence] Source: {sourceName}");
                            Log($"[Influence] Already following route: {targetStudent.IsFollowingRoute}");
                            
                            // Only start escape if not already escaping AND in high panic state
                            if (!targetStudent.IsFollowingRoute)
                            {
                                // Only trigger escape for ActingOut or Critical students
                                if (targetStudent.CurrentState == StudentState.ActingOut || 
                                    targetStudent.CurrentState == StudentState.Critical)
                                {
                                    targetStudent.StartRoute(currentLevel.escapeRoute);
                                    Log($"[Influence] ✓ {targetName} started escape route");
                                }
                                else
                                {
                                    Log($"[Influence] ⚠ {targetName} not in panic state ({targetStudent.CurrentState}), skipping escape");
                                }
                            }
                            else
                            {
                                Log($"[Influence] ⚠ {targetName} already following a route, skipping escape");
                            }
                        }
                        else
                        {
                            Log($"[Influence] {targetName} wants to escape but no route available - Level: {(currentLevel != null ? "exists" : "null")}, Route: {(currentLevel?.escapeRoute != null ? "exists" : "null")}");
                        }
                    }
                    else
                    {
                        Log($"[Influence] LevelManager.Instance is NULL - cannot trigger escape");
                    }
                }
                else
                {
                    Log($"[Influence] Strength {strength:F2} < panicThreshold {targetStudent.Config.panicThreshold} - no escape triggered");
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

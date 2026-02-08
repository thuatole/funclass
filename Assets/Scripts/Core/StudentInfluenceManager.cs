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
        [SerializeField] private float singleStudentMaxDistance = 6f;

        private bool isActive = false;
        private List<StudentAgent> allStudents = new List<StudentAgent>();

        private InfluenceScopeConfig GetInfluenceScopeConfig()
        {
            if (LevelManager.Instance == null) return null;
            var levelConfig = LevelManager.Instance.GetCurrentLevelConfig();
            return levelConfig?.influenceScopeConfig;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameLogger.Milestone("StudentInfluenceManager", "Awake - Instance created");
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
            GameLogger.Detail("StudentInfluenceManager", "Start() called");
            
            // Fallback subscription if OnEnable was called before GameStateManager existed
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                GameLogger.Detail("StudentInfluenceManager", "Subscribed to GameStateManager in Start()");
            }
            
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.OnEventLogged -= HandleStudentEvent;
                StudentEventManager.Instance.OnEventLogged += HandleStudentEvent;
                GameLogger.Detail("StudentInfluenceManager", "Subscribed to StudentEventManager in Start()");
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            GameLogger.Detail("StudentInfluenceManager", $"State change: {oldState} → {newState}");
            
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
            GameLogger.Milestone("StudentInfluenceManager", "Activated - influence system ready");
        }

        private void DeactivateInfluenceSystem()
        {
            isActive = false;
            allStudents.Clear();
            GameLogger.Detail("StudentInfluenceManager", "Deactivated");
        }

        private void RefreshStudentList()
        {
            allStudents.Clear();
            StudentAgent[] students = FindObjectsOfType<StudentAgent>();
            allStudents.AddRange(students);
            GameLogger.Detail("StudentInfluenceManager", $"Found {allStudents.Count} students in scene");
        }

        private void HandleStudentEvent(StudentEvent evt)
        {
            GameLogger.Trace("StudentInfluenceManager", 
                $"Event: {evt.eventType}, Student: {evt.student?.Config?.studentName}, Active: {isActive}");
            
            if (!isActive)
            {
                GameLogger.Trace("StudentInfluenceManager", "Event ignored - system not active");
                return;
            }
            
            if (evt.student == null)
            {
                GameLogger.Warning("StudentInfluenceManager", "Event ignored - student is null");
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
                    GameLogger.Detail("StudentInfluenceManager", 
                        $"Mess cleaned - resolving sources from {evt.student.Config?.studentName}");
                    ResolveInfluenceSourcesFromStudent(evt.student);
                }
            }

            if (IsInfluenceTrigger(evt.eventType))
            {
                GameLogger.Detail("StudentInfluenceManager", 
                    $"Processing influence for {evt.eventType}");
                ProcessInfluence(evt);
            }
            else
            {
                GameLogger.Trace("StudentInfluenceManager", 
                    $"Event type {evt.eventType} is not an influence trigger");
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
                StudentEventType.LeftSeat => false,
                StudentEventType.WanderingAround => false,
                StudentEventType.MessCreated => true,
                StudentEventType.StudentReacted when IsHighIntensityReaction(eventType) => true,
                _ => false
            };
        }

        private bool IsHighIntensityReaction(StudentEventType eventType)
        {
            return false;
        }

        /// <summary>
        /// Gets the base influence severity for an event type
        /// </summary>
        private float GetInfluenceSeverity(StudentEventType eventType)
        {
            var config = GetInfluenceScopeConfig();
            if (config != null && config.ContainsEventType(eventType.ToString()))
            {
                return config.GetBaseSeverity(eventType.ToString());
            }
            
            // Fallback to hardcoded defaults
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
        /// Handles WholeClass and SingleStudent scopes
        /// </summary>
        private void ProcessInfluence(StudentEvent evt)
        {
            StudentAgent sourceStudent = evt.student;
            float baseSeverity = GetInfluenceSeverity(evt.eventType);
            InfluenceScope scope = evt.influenceScope;

            GameLogger.Detail("StudentInfluenceManager", 
                $"{sourceStudent.Config?.studentName} triggered {scope} influence: {evt.eventType}");

            if (scope == InfluenceScope.None)
            {
                GameLogger.Detail("StudentInfluenceManager", "Event has no influence scope, skipping");
                return;
            }

            if (scope == InfluenceScope.SingleStudent)
            {
                // Handle SingleStudent influence with or without target
                if (evt.targetStudent == null)
                {
                    // No target specified - find nearest student or fallback to WholeClass
                    GameLogger.Detail("StudentInfluenceManager", 
                        "SingleStudent scope but no target specified, finding nearest student...");
                    StudentAgent nearestStudent = FindNearestStudentInRange(sourceStudent, singleStudentMaxDistance);
                    
                    if (nearestStudent != null)
                    {
                        GameLogger.Detail("StudentInfluenceManager", 
                            $"Found nearest student: {nearestStudent.Config?.studentName}");
                        evt.targetStudent = nearestStudent;
                    }
                    else
                    {
                        GameLogger.Detail("StudentInfluenceManager", 
                            $"No student within {singleStudentMaxDistance}m, falling back to WholeClass scope");
                        ProcessWholeClassInfluence(sourceStudent, baseSeverity, evt);
                        return;
                    }
                }
                
                // Now we have a target student (either specified or found)
                // Check distance between source and target
                float distance = Vector3.Distance(sourceStudent.transform.position, evt.targetStudent.transform.position);
                
                if (distance > singleStudentMaxDistance)
                {
                    GameLogger.Milestone("StudentInfluenceManager", 
                        $"BLOCKED: {sourceStudent.Config?.studentName} → {evt.targetStudent.Config?.studentName} (distance {distance:F1}m > {singleStudentMaxDistance}m)");
                    return;
                }
                
                GameLogger.Detail("StudentInfluenceManager", 
                    $"SingleStudent: {sourceStudent.Config?.studentName} → {evt.targetStudent.Config?.studentName} (distance: {distance:F1}m)");
                
                // Check if target has config
                if (evt.targetStudent.Config == null)
                {
                    GameLogger.Warning("StudentInfluenceManager", 
                        $"Target student {evt.targetStudent.name} has NULL Config!");
                    return;
                }
                
                // Calculate influence strength based on susceptibility and resistance only
                float susceptibility = evt.targetStudent.Config.influenceSusceptibility;
                float resistance = evt.targetStudent.Config.influenceResistance;
                float influenceStrength = baseSeverity * susceptibility * (1f - resistance);
                
                GameLogger.Detail("StudentInfluenceManager", 
                    $"Strength calculation: base={baseSeverity:F2}, susceptibility={susceptibility:F2}, resistance={resistance:F2}");
                
                if (influenceStrength > 0.01f)
                {
                    // Add influence source tracking
                    evt.targetStudent.InfluenceSources.AddSource(sourceStudent, evt.eventType, influenceStrength);
                    ApplyInfluence(evt.targetStudent, sourceStudent, influenceStrength, evt.eventType);
                    
                    // Update influence icons
                    UpdateInfluenceIcons(sourceStudent, evt.targetStudent, true);
                    
                    GameLogger.Milestone("StudentInfluenceManager", 
                        $"{sourceStudent.Config?.studentName} → {evt.targetStudent.Config?.studentName}: influence applied (strength={influenceStrength:F2})");
                }
                else
                {
                    GameLogger.Detail("StudentInfluenceManager", 
                        $"Strength too low ({influenceStrength:F2}), not adding source");
                }
            }
            else if (scope == InfluenceScope.WholeClass)
            {
                ProcessWholeClassInfluence(sourceStudent, baseSeverity, evt);
            }
        }

        /// <summary>
        /// Resolve influence sources from a student who has been calmed down
        /// This marks the student's influence on others as resolved
        /// </summary>
        private void ResolveInfluenceSourcesFromStudent(StudentAgent calmedStudent)
        {
            GameLogger.Milestone("StudentInfluenceManager", 
                $"Teacher action: calming {calmedStudent.Config?.studentName}");

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

                        GameLogger.Detail("StudentInfluenceManager", 
                            $"Resolved {calmedStudent.Config?.studentName}'s influence on {student.Config?.studentName} ({beforeCount} → {afterCount} sources)");
                    }
                }
            }

            // Hide influencer icon on calmed student
            var calmedIcon = calmedStudent.GetComponent<InfluenceStatusIcon>();
            if (calmedIcon != null)
            {
                calmedIcon.HideAllIcons();
            }

            GameLogger.Milestone("StudentInfluenceManager", 
                $"Calmed {calmedStudent.Config?.studentName} - resolved influence on {resolvedCount} student(s)");
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
        }

        /// <summary>
        /// Process WholeClass influence
        /// </summary>
        private void ProcessWholeClassInfluence(StudentAgent sourceStudent, float baseSeverity, StudentEvent evt)
        {
            // Whole class influence - affects students in SAME LOCATION only
            StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();
            int affectedCount = 0;
            int skippedDifferentLocation = 0;

            string sourceLocation = StudentLocationHelper.GetLocationString(sourceStudent);
            GameLogger.Detail("StudentInfluenceManager", 
                $"WholeClass: {sourceStudent.Config?.studentName} ({sourceLocation}) affects students in same location");

            foreach (StudentAgent targetStudent in allStudents)
            {
                // Skip self
                if (targetStudent == sourceStudent) continue;

                // Check if in same location (inside/outside)
                if (!StudentLocationHelper.AreInSameLocation(sourceStudent, targetStudent))
                {
                    string targetLocation = StudentLocationHelper.GetLocationString(targetStudent);
                    GameLogger.Trace("StudentInfluenceManager", 
                        $"  ✗ {targetStudent.Config?.studentName} ({targetLocation}) - different location, no influence");
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

            GameLogger.Milestone("StudentInfluenceManager", 
                $"WholeClass: {sourceStudent.Config?.studentName} affected {affectedCount} students (skipped {skippedDifferentLocation} in different location)");
        }

        /// <summary>
        /// Finds all students within the specified radius, excluding the source student
        /// </summary>
        private List<StudentAgent> FindNearbyStudents(StudentAgent sourceStudent, float radius)
        {
            List<StudentAgent> nearbyStudents = new List<StudentAgent>();
            Vector3 sourcePosition = sourceStudent.transform.position;
            
            GameLogger.Trace("StudentInfluenceManager", 
                $"Searching for students near {sourceStudent.Config?.studentName} within {radius}m");

            foreach (StudentAgent student in allStudents)
            {
                if (student == sourceStudent) continue;
                if (student == null) continue;
                if (student.Config == null) continue;

                float distance = Vector3.Distance(sourcePosition, student.transform.position);
                GameLogger.Trace("StudentInfluenceManager", 
                    $"  {student.Config.studentName}: {distance:F1}m");
                
                if (distance <= radius)
                {
                    nearbyStudents.Add(student);
                    GameLogger.Trace("StudentInfluenceManager", 
                        $"  ✓ Added {student.Config.studentName}");
                }
            }
            
            GameLogger.Trace("StudentInfluenceManager", 
                $"Found {nearbyStudents.Count} nearby students");
            return nearbyStudents;
        }

        /// <summary>
        /// Finds the nearest student within the specified range, excluding the source student
        /// Returns null if no student found within range
        /// </summary>
        private StudentAgent FindNearestStudentInRange(StudentAgent sourceStudent, float maxRange)
        {
            if (sourceStudent == null) return null;
            
            List<StudentAgent> nearbyStudents = FindNearbyStudents(sourceStudent, maxRange);
            
            if (nearbyStudents.Count == 0)
            {
                return null;
            }
            
            // Find the closest one
            StudentAgent nearestStudent = null;
            float nearestDistance = float.MaxValue;
            Vector3 sourcePos = sourceStudent.transform.position;
            
            foreach (StudentAgent student in nearbyStudents)
            {
                if (student == sourceStudent) continue;
                
                float distance = Vector3.Distance(sourcePos, student.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestStudent = student;
                }
            }
            
            return nearestStudent;
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

            // Check if student is immune to influence
            if (targetStudent.IsImmuneToInfluence())
            {
                GameLogger.Detail("StudentInfluenceManager", 
                    $"{targetName} is immune to influence, skipping");
                return;
            }

            GameLogger.Detail("StudentInfluenceManager", 
                $"{targetName} affected by {sourceName} at {distance:F1}m with strength {strength:F2}");

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
                GameLogger.Milestone("StudentInfluenceManager", 
                    $"{targetName} escalated: {oldState} → {targetStudent.CurrentState} (influence from {sourceName})");
            }

            // Trigger panic or scared reaction
            if (strength >= 0.5f)
            {
                targetStudent.TriggerReaction(StudentReactionType.Scared, 4f);
                GameLogger.Detail("StudentInfluenceManager", 
                    $"{targetName} → Scared reaction (strength {strength:F2})");
                
                // Trigger escape behavior if panic threshold exceeded and escape route available
                if (strength >= targetStudent.Config.panicThreshold)
                {
                    if (LevelManager.Instance != null)
                    {
                        LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
                        
                        if (currentLevel != null && currentLevel.escapeRoute != null)
                        {
                            // Only start escape if not already escaping AND in high panic state
                            if (!targetStudent.IsFollowingRoute)
                            {
                                if (targetStudent.CurrentState == StudentState.ActingOut || 
                                    targetStudent.CurrentState == StudentState.Critical)
                                {
                                    targetStudent.StartRoute(currentLevel.escapeRoute);
                                    GameLogger.Milestone("StudentInfluenceManager", 
                                        $"{targetName} ESCAPED (route: {currentLevel.escapeRoute.routeName})");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                targetStudent.TriggerReaction(StudentReactionType.Confused, 3f);
                GameLogger.Detail("StudentInfluenceManager", 
                    $"{targetName} → Confused reaction (strength {strength:F2})");
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
                GameLogger.Milestone("StudentInfluenceManager", 
                    $"{targetName} escalated: {oldState} → {targetStudent.CurrentState} (medium influence from {sourceName})");
            }
            else if (targetStudent.CurrentState == StudentState.Distracted && strength >= 0.5f)
            {
                StudentState oldState = targetStudent.CurrentState;
                targetStudent.EscalateState();
                GameLogger.Milestone("StudentInfluenceManager", 
                    $"{targetName} escalated: {oldState} → {targetStudent.CurrentState} (medium influence from {sourceName})");
            }

            // Trigger reaction
            if (UnityEngine.Random.value < 0.6f)
            {
                targetStudent.TriggerReaction(StudentReactionType.Confused, 2f);
                GameLogger.Detail("StudentInfluenceManager", 
                    $"{targetName} → Confused reaction (strength {strength:F2})");
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
                GameLogger.Milestone("StudentInfluenceManager", 
                    $"{targetName} escalated: {oldState} → {targetStudent.CurrentState} (weak influence from {sourceName})");
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

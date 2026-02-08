using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Configuration for GameLogger tiers and periodic summary
    /// Attach to a persistent game object (same as GameStateManager)
    /// </summary>
    public class GameLoggerConfig : MonoBehaviour
    {
        [Header("MILESTONE (Always On)")]
        [Tooltip("Critical game events - always visible")]
        public bool milestoneEnabled = true;

        [Header("TRACE (Master Toggle)")]
        [Tooltip("Verbose per-frame data - only enable when deep-diving")]
        public bool traceEnabled = false;

        [Header("DETAIL Toggles")]
        public bool studentInteractionProcessorDetail = false;
        public bool studentInfluenceManagerDetail = false;
        public bool studentAgentDetail = false;
        public bool teacherControllerDetail = false;
        public bool levelManagerDetail = false;
        public bool gameStateManagerDetail = false;

        [Header("Periodic Summary")]
        [Tooltip("Enable periodic scenario summary")]
        public bool summaryEnabled = true;
        [Tooltip("Summary interval in seconds")]
        public float summaryInterval = 30f;

        private Coroutine summaryCoroutine;
        private bool isInitialized = false;

        void Awake()
        {
            // Apply settings to GameLogger
            GameLogger.MilestoneEnabled = milestoneEnabled;
            GameLogger.TraceEnabled = traceEnabled;
            
            GameLogger.SetDetailEnabled("StudentInteractionProcessor", studentInteractionProcessorDetail);
            GameLogger.SetDetailEnabled("StudentInfluenceManager", studentInfluenceManagerDetail);
            GameLogger.SetDetailEnabled("StudentAgent", studentAgentDetail);
            GameLogger.SetDetailEnabled("TeacherController", teacherControllerDetail);
            GameLogger.SetDetailEnabled("LevelManager", levelManagerDetail);
            GameLogger.SetDetailEnabled("GameStateManager", gameStateManagerDetail);

            GameLogger.Initialize(this);
            
            isInitialized = true;
            Debug.Log($"[GameLoggerConfig] ★ Initialized with milestone={milestoneEnabled}, trace={traceEnabled}");
        }

        void OnEnable()
        {
            StartSummaryIfEnabled();
        }

        void OnDisable()
        {
            StopSummary();
        }

        /// <summary>
        /// Start periodic summary coroutine if enabled
        /// </summary>
        public void StartSummaryIfEnabled()
        {
            if (summaryEnabled && summaryCoroutine == null)
            {
                summaryCoroutine = StartCoroutine(PeriodicSummaryRoutine());
            }
        }

        /// <summary>
        /// Stop periodic summary coroutine
        /// </summary>
        public void StopSummary()
        {
            if (summaryCoroutine != null)
            {
                StopCoroutine(summaryCoroutine);
                summaryCoroutine = null;
            }
        }

        /// <summary>
        /// Trigger summary immediately (for testing or on specific events)
        /// </summary>
        public void TriggerSummaryNow()
        {
            if (this != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(PeriodicSummaryRoutine());
            }
        }

        /// <summary>
        /// Coroutine that outputs summary every N seconds
        /// </summary>
        private IEnumerator PeriodicSummaryRoutine()
        {
            while (summaryEnabled)
            {
                yield return new WaitForSeconds(summaryInterval);
                OutputScenarioSummary();
            }
        }

        /// <summary>
        /// Output a summary of current game state
        /// </summary>
        private void OutputScenarioSummary()
        {
            if (LevelManager.Instance == null || !LevelManager.Instance.IsLevelActive)
            {
                GameLogger.Milestone("GameLogger", "No active level - skipping summary");
                return;
            }

            float elapsed = LevelManager.Instance.LevelTimeElapsed;
            
            GameLogger.Milestone("GameLogger", $"=== SCENARIO SUMMARY @ {elapsed:F1}s ===");
            
            // Student states
            StudentAgent[] students = FindObjectsOfType<StudentAgent>();
            int influencedCount = 0;
            
            foreach (var student in students)
            {
                if (student.Config != null)
                {
                    bool hasInfluence = student.InfluenceSources != null && 
                                       student.InfluenceSources.GetUnresolvedSourceCount() > 0;
                    if (hasInfluence) influencedCount++;
                    
                    string influenceInfo = hasInfluence ? 
                        $" [influenced by {student.InfluenceSources.GetUnresolvedSourceCount()}]" : "";
                    
                    GameLogger.Milestone("GameLogger", 
                        $"  {student.Config.studentName}: {student.CurrentState}{influenceInfo}");
                }
            }
            
            // Disruption level
            if (ClassroomManager.Instance != null)
            {
                float disruption = ClassroomManager.Instance.CurrentDisruption;
                GameLogger.Milestone("GameLogger", $"  Disruption: {disruption:F1}%");
            }
            
            // Scripted interactions
            if (StudentInteractionProcessor.Instance != null)
            {
                // Note: triggeredInteractions is private, this is informational
                GameLogger.Milestone("GameLogger", $"  Scripted interactions: monitoring active");
            }
            
            GameLogger.Milestone("GameLogger", $"=== END SUMMARY ({students.Length} students, {influencedCount} influenced) ===");
        }

        /// <summary>
        /// Refresh detail flags from Inspector values (call after modifying in runtime)
        /// </summary>
        public void RefreshDetailFlags()
        {
            if (!isInitialized) return;
            
            GameLogger.SetDetailEnabled("StudentInteractionProcessor", studentInteractionProcessorDetail);
            GameLogger.SetDetailEnabled("StudentInfluenceManager", studentInfluenceManagerDetail);
            GameLogger.SetDetailEnabled("StudentAgent", studentAgentDetail);
            GameLogger.SetDetailEnabled("TeacherController", teacherControllerDetail);
            GameLogger.SetDetailEnabled("LevelManager", levelManagerDetail);
            GameLogger.SetDetailEnabled("GameStateManager", gameStateManagerDetail);
            
            GameLogger.TraceEnabled = traceEnabled;
        }
    }
}

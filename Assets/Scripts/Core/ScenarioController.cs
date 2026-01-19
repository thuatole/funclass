using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Controls one-time scenario events for level
    /// Each action happens exactly once per level
    /// </summary>
    public class ScenarioController : MonoBehaviour
    {
        [Header("Scenario Settings")]
        [SerializeField] private float delayBeforeStart = 3f;
        
        [Header("Debug")]
        [SerializeField] private bool enableLogs = true;
        
        private HashSet<string> triggeredEvents = new HashSet<string>();
        private bool scenarioStarted = false;
        private float startTime;

        void Start()
        {
            startTime = Time.time;
            Log("[ScenarioController] Initialized");
            
            // Subscribe to GameStateManager if available
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                Log("[ScenarioController] Subscribed to GameStateManager");
            }
            else
            {
                // Fallback: Start scenario after delay without GameStateManager
                Log("[ScenarioController] No GameStateManager - using fallback timer");
                Invoke(nameof(StartScenario), delayBeforeStart);
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.InLevel && !scenarioStarted)
            {
                Log("[ScenarioController] Game state changed to InLevel - beginning scenario");
                Invoke(nameof(StartScenario), delayBeforeStart);
            }
        }

        private void StartScenario()
        {
            scenarioStarted = true;
            Log("[ScenarioController] === SCENARIO START ===");
            
            // Trigger initial event: Student_A vomits
            TriggerVomit("Student_A");
            
            // Trigger B→C interaction after delay (when they're outside)
            Invoke(nameof(TriggerBCInteraction), 8f);
        }
        
        private void TriggerBCInteraction()
        {
            Log("[ScenarioController] Triggering B→C interaction");
            TriggerInteraction("Student_B", "Student_C", StudentEventType.ThrowingObject);
        }

        /// <summary>
        /// Trigger vomit for specific student (one-time only)
        /// </summary>
        public void TriggerVomit(string studentName)
        {
            string eventKey = $"Vomit_{studentName}";
            if (triggeredEvents.Contains(eventKey))
            {
                Log($"[ScenarioController] Vomit for {studentName} already triggered - skipping");
                return;
            }

            triggeredEvents.Add(eventKey);
            Log($"[ScenarioController] >>> Triggering VOMIT for {studentName}");

            // Find student
            StudentAgent student = FindStudent(studentName);
            if (student == null)
            {
                Debug.LogError($"[ScenarioController] Student {studentName} not found!");
                return;
            }

            // Trigger vomit
            var messCreator = student.GetComponent<StudentMessCreator>();
            if (messCreator != null)
            {
                messCreator.PerformVomit();
                Log($"[ScenarioController] ✓ {studentName} vomited");
            }
            else
            {
                Debug.LogError($"[ScenarioController] {studentName} has no StudentMessCreator component!");
            }
        }

        /// <summary>
        /// Trigger interaction between students (one-time only)
        /// </summary>
        public void TriggerInteraction(string sourceStudent, string targetStudent, StudentEventType eventType)
        {
            string eventKey = $"Interaction_{sourceStudent}_{targetStudent}_{eventType}";
            if (triggeredEvents.Contains(eventKey))
            {
                Log($"[ScenarioController] Interaction {sourceStudent}→{targetStudent} ({eventType}) already triggered - skipping");
                return;
            }

            triggeredEvents.Add(eventKey);
            Log($"[ScenarioController] >>> Triggering INTERACTION: {sourceStudent} → {targetStudent} ({eventType})");

            StudentAgent source = FindStudent(sourceStudent);
            StudentAgent target = FindStudent(targetStudent);

            if (source == null || target == null)
            {
                Debug.LogError($"[ScenarioController] Source or target student not found!");
                return;
            }

            // Trigger event
            if (StudentEventManager.Instance != null)
            {
                var evt = new StudentEvent(
                    student: source,
                    eventType: eventType,
                    description: $"{sourceStudent} → {targetStudent}",
                    targetObject: null,
                    targetStudent: target,
                    scope: InfluenceScope.SingleStudent
                );

                StudentEventManager.Instance.LogEvent(evt);
                Log($"[ScenarioController] ✓ Interaction triggered");
            }
        }

        /// <summary>
        /// Check if event has been triggered
        /// </summary>
        public bool HasTriggered(string eventKey)
        {
            return triggeredEvents.Contains(eventKey);
        }

        /// <summary>
        /// Reset all triggered events (for testing)
        /// </summary>
        [ContextMenu("Reset All Events")]
        public void ResetAllEvents()
        {
            triggeredEvents.Clear();
            scenarioStarted = false;
            Log("[ScenarioController] All events reset");
        }

        private StudentAgent FindStudent(string studentName)
        {
            StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();
            foreach (StudentAgent student in allStudents)
            {
                if (student.Config != null && student.Config.studentName == studentName)
                {
                    return student;
                }
            }
            return null;
        }

        private void Log(string message)
        {
            if (enableLogs)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Manual trigger methods for testing
        /// </summary>
        [ContextMenu("Test: Trigger Vomit A")]
        public void TestVomitA() => TriggerVomit("Student_A");

        [ContextMenu("Test: Trigger B→C Interaction")]
        public void TestInteractionBC() => TriggerInteraction("Student_B", "Student_C", StudentEventType.ThrowingObject);
    }
}

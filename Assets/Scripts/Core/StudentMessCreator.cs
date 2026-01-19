using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// MonoBehaviour that can be added to StudentAgent to handle mess creation
    /// Add this component to StudentAgent GameObject if you want automatic mess creation
    /// </summary>
    public class StudentMessCreator : MonoBehaviour
    {
        [Header("Mess Prefabs")]
        [Tooltip("Prefab for vomit puddle visual")]
        [SerializeField] private GameObject vomitPuddlePrefab;

        [Header("Vomit Settings")]
        [Tooltip("Chance of vomiting when in Critical state (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float vomitChanceWhenCritical = 0.1f;

        [Tooltip("Minimum time between vomit actions (seconds)")]
        [SerializeField] private float vomitCooldown = 30f;

        private StudentAgent studentAgent;
        private float lastVomitTime = -999f;
        private bool hasVomitedThisLevel = false;

        void Awake()
        {
            studentAgent = GetComponent<StudentAgent>();
            if (studentAgent == null)
            {
                Debug.LogError("[StudentMessCreator] No StudentAgent found on GameObject");
                enabled = false;
            }
        }

        void Update()
        {
            // Automatic vomit disabled - use ScenarioController for one-time events
            // This prevents repeated vomit actions
            
            // Original automatic vomit code (now disabled):
            // if (studentAgent.CurrentState == StudentState.Critical)
            // {
            //     if (Time.time - lastVomitTime > vomitCooldown)
            //     {
            //         if (Random.value < vomitChanceWhenCritical * Time.deltaTime)
            //         {
            //             PerformVomit();
            //         }
            //     }
            // }
        }

        /// <summary>
        /// Public method to trigger vomit action
        /// Can be called from StudentAgent or other systems
        /// ONE-TIME ONLY per level
        /// </summary>
        public void PerformVomit()
        {
            // One-time check - only allow vomit once per level
            if (hasVomitedThisLevel)
            {
                Debug.Log($"[StudentMessCreator] {studentAgent.Config?.studentName} already vomited this level - skipping");
                return;
            }
            
            if (Time.time - lastVomitTime < vomitCooldown)
            {
                Debug.Log($"[StudentMessCreator] {studentAgent.Config?.studentName} vomit on cooldown");
                return;
            }

            hasVomitedThisLevel = true;
            lastVomitTime = Time.time;

            Debug.Log($"[StudentMessCreator] {studentAgent.Config?.studentName} is vomiting!");

            // Create the mess
            VomitMess vomitMess = StudentAgentMessIntegration.CreateVomitMess(studentAgent, vomitPuddlePrefab);

            Debug.Log($"[StudentMessCreator] VomitMess created: {vomitMess != null}, StudentEventManager exists: {StudentEventManager.Instance != null}");

            // Log event to StudentEventManager for influence system
            if (StudentEventManager.Instance != null && vomitMess != null)
            {
                Debug.Log($"[StudentMessCreator] Calling StudentEventManager.LogEvent for MessCreated");
                StudentEventManager.Instance.LogEvent(
                    studentAgent,
                    StudentEventType.MessCreated,
                    "created vomit"
                );
                Debug.Log($"[StudentEvent] {studentAgent.Config?.studentName}: created vomit");
            }
            else
            {
                Debug.LogWarning($"[StudentMessCreator] Cannot log event - StudentEventManager: {StudentEventManager.Instance != null}, VomitMess: {vomitMess != null}");
            }

            // Trigger reaction
            studentAgent.TriggerReaction(StudentReactionType.Embarrassed, 5f);
        }
    }
}

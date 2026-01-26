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
        private bool wasInCriticalLastFrame = false;

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
            // Only check when in Critical state and not on cooldown
            if (studentAgent.CurrentState != StudentState.Critical)
            {
                wasInCriticalLastFrame = false;
                return;
            }

            // Already vomited this level
            if (hasVomitedThisLevel)
            {
                return;
            }

            // On cooldown - reset flag để retry sau cooldown
            if (Time.time - lastVomitTime < vomitCooldown)
            {
                return;
            }

            // Already checked this Critical state with success (prevent spam)
            // Nếu check thất bại (Random >= chance), vẫn reset để retry
            // vì student có thể stay Critical lâu và chúng ta muốn retry
            if (wasInCriticalLastFrame)
            {
                // Reset mỗi 5 giây nếu vẫn Critical để retry
                if (Time.time - lastVomitTime >= 5f)
                {
                    wasInCriticalLastFrame = false;
                }
                else
                {
                    return;
                }
            }

            // Check if should vomit
            float chance = GetVomitChance();
            Debug.Log($"[StudentMessCreator] Critical check: chance={chance}, Random={Random.value}");

            if (Random.value < chance)
            {
                PerformVomit();
            }

            // Mark as checked for this Critical state
            wasInCriticalLastFrame = true;
        }

        /// <summary>
        /// Get vomit chance from StudentAgent config, or use inspector default
        /// </summary>
        private float GetVomitChance()
        {
            // Try to get from config (if available)
            if (studentAgent.Config != null)
            {
                // Use impulsiveness as vomit chance - more impulsive = more likely to vomit
                // If impulsiveness is not set (0), use default
                float impulsiveness = studentAgent.Config.impulsiveness;
                float chance = (impulsiveness > 0) ? impulsiveness : vomitChanceWhenCritical;
                Debug.Log($"[StudentMessCreator] GetVomitChance: impulsiveness={impulsiveness}, finalChance={chance}");
                return chance;
            }

            // Fallback to inspector value
            Debug.Log($"[StudentMessCreator] GetVomitChance: using default={vomitChanceWhenCritical}");
            return vomitChanceWhenCritical;
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

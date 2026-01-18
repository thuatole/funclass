using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Extension methods and helper for integrating mess creation with StudentAgent
    /// This file provides example code for how to create mess objects from student actions
    /// </summary>
    public static class StudentAgentMessIntegration
    {
        /// <summary>
        /// Example: Create a vomit mess at student's position
        /// Call this from StudentAgent when a vomit action occurs
        /// </summary>
        /// <param name="student">The student creating the mess</param>
        /// <param name="vomitPuddlePrefab">Optional prefab for visual representation</param>
        /// <returns>The created VomitMess object</returns>
        public static VomitMess CreateVomitMess(StudentAgent student, GameObject vomitPuddlePrefab = null)
        {
            if (student == null)
            {
                Debug.LogWarning("[MessIntegration] Cannot create vomit mess - student is null");
                return null;
            }

            // Calculate position in front of student
            Vector3 messPosition = student.transform.position + student.transform.forward * 0.5f;
            messPosition.y = 0.01f; // Slightly above ground to avoid z-fighting

            // Create the vomit mess
            VomitMess vomitMess = VomitMess.Create(messPosition, student, vomitPuddlePrefab);

            Debug.Log($"[MessIntegration] {student.Config?.studentName} created vomit mess at {messPosition}");

            return vomitMess;
        }

        /// <summary>
        /// Example: Add this method to StudentAgent to trigger vomit action
        /// 
        /// Usage in StudentAgent.cs:
        /// 
        /// [Header("Mess Settings")]
        /// [SerializeField] private GameObject vomitPuddlePrefab;
        /// 
        /// public void PerformVomitAction()
        /// {
        ///     // Trigger vomit animation if you have one
        ///     // animator.SetTrigger("Vomit");
        ///     
        ///     // Create the vomit mess
        ///     StudentAgentMessIntegration.CreateVomitMess(this, vomitPuddlePrefab);
        ///     
        ///     // Trigger reaction
        ///     TriggerReaction(StudentReactionType.Embarrassed, 5f);
        ///     
        ///     // Change state if needed
        ///     if (CurrentState == StudentState.Calm)
        ///     {
        ///         ChangeState(StudentState.Distracted);
        ///     }
        /// }
        /// </summary>
        public static void ExampleVomitActionInStudentAgent()
        {
            // This is just documentation - see method summary above
        }
    }

    /// <summary>
    /// Example MonoBehaviour that can be added to StudentAgent to handle mess creation
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
            // Example: Random chance to vomit when in Critical state
            if (studentAgent.CurrentState == StudentState.Critical)
            {
                if (Time.time - lastVomitTime > vomitCooldown)
                {
                    if (Random.value < vomitChanceWhenCritical * Time.deltaTime)
                    {
                        PerformVomit();
                    }
                }
            }
        }

        /// <summary>
        /// Public method to trigger vomit action
        /// Can be called from StudentAgent or other systems
        /// </summary>
        public void PerformVomit()
        {
            if (Time.time - lastVomitTime < vomitCooldown)
            {
                Debug.Log($"[StudentMessCreator] {studentAgent.Config?.studentName} vomit on cooldown");
                return;
            }

            lastVomitTime = Time.time;

            Debug.Log($"[StudentMessCreator] {studentAgent.Config?.studentName} is vomiting!");

            // Create the mess
            StudentAgentMessIntegration.CreateVomitMess(studentAgent, vomitPuddlePrefab);

            // Trigger reaction
            studentAgent.TriggerReaction(StudentReactionType.Embarrassed, 5f);
        }
    }
}

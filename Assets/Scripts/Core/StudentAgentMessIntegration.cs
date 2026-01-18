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

}

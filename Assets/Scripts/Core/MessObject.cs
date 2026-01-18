using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Base class for all mess objects that can be created by students and cleaned by teachers
    /// </summary>
    public abstract class MessObject : MonoBehaviour
    {
        [Header("Mess Configuration")]
        [Tooltip("Unique identifier for this mess type")]
        public string messId;
        
        [Tooltip("Display name for interaction prompts")]
        public string messName = "Mess";
        
        [Tooltip("How severe is this mess? (affects disruption)")]
        [Range(1, 10)]
        public int severityLevel = 5;
        
        [Tooltip("How much disruption this mess adds to classroom")]
        public float disruptionAmount = 10f;
        
        [Tooltip("How much disruption is reduced when cleaned")]
        public float cleanupDisruptionReduction = 15f;
        
        [Tooltip("Score awarded for cleaning this mess")]
        public int cleanupScore = 10;
        
        [Tooltip("Time required to clean (seconds) - 0 for instant")]
        public float cleanupTime = 0f;

        [Header("References")]
        [Tooltip("Student who created this mess")]
        protected StudentAgent creator;
        
        [Tooltip("Visual representation of the mess")]
        protected GameObject messVisual;

        protected bool isCleaned = false;
        protected float creationTime;

        protected virtual void Awake()
        {
            creationTime = Time.time;
        }

        /// <summary>
        /// Called when the mess is first created
        /// </summary>
        public virtual void Initialize(StudentAgent student)
        {
            creator = student;
            
            // Add disruption to classroom
            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(disruptionAmount, $"{messName} created");
            }

            // Log event
            if (StudentEventManager.Instance != null && creator != null)
            {
                StudentEventManager.Instance.LogEvent(
                    creator,
                    StudentEventType.MessCreated,
                    $"created {messName}",
                    gameObject
                );
            }

            Debug.Log($"[Mess] {creator?.Config?.studentName ?? "Unknown"} created {messName}");
        }

        /// <summary>
        /// Gets the interaction prompt for this mess
        /// </summary>
        public virtual string GetInteractionPrompt()
        {
            if (isCleaned)
            {
                return $"{messName} (already cleaned)";
            }
            return $"Press E to clean {messName}";
        }

        /// <summary>
        /// Called when teacher starts cleaning this mess
        /// </summary>
        public virtual void StartCleanup(TeacherController teacher)
        {
            if (isCleaned)
            {
                Debug.LogWarning($"[Mess] Attempted to clean already cleaned {messName}");
                return;
            }

            Debug.Log($"[Teacher] Started cleaning {messName}");

            if (cleanupTime > 0)
            {
                // Start cleanup coroutine for timed cleanup
                StartCoroutine(CleanupCoroutine(teacher));
            }
            else
            {
                // Instant cleanup
                CompleteCleanup(teacher);
            }
        }

        /// <summary>
        /// Coroutine for timed cleanup
        /// </summary>
        protected virtual System.Collections.IEnumerator CleanupCoroutine(TeacherController teacher)
        {
            yield return new WaitForSeconds(cleanupTime);
            CompleteCleanup(teacher);
        }

        /// <summary>
        /// Called when cleanup is completed
        /// </summary>
        protected virtual void CompleteCleanup(TeacherController teacher)
        {
            if (isCleaned) return;

            isCleaned = true;

            // Reduce disruption
            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-cleanupDisruptionReduction, $"{messName} cleaned");
            }

            // Award score
            if (TeacherScoreManager.Instance != null)
            {
                TeacherScoreManager.Instance.AddScore(cleanupScore, $"Cleaned {messName}");
            }

            // Log cleanup event
            if (StudentEventManager.Instance != null && creator != null)
            {
                StudentEventManager.Instance.LogEvent(
                    creator,
                    StudentEventType.MessCleaned,
                    $"{messName} was cleaned by teacher",
                    gameObject
                );
            }

            Debug.Log($"[Teacher] Cleaned {messName} successfully");

            // Destroy the mess object
            OnCleanupComplete();
            Destroy(gameObject, 0.1f);
        }

        /// <summary>
        /// Called just before the mess is destroyed after cleanup
        /// Override for custom cleanup behavior
        /// </summary>
        protected virtual void OnCleanupComplete()
        {
            // Override in derived classes for specific behavior
        }

        /// <summary>
        /// Gets the student who created this mess
        /// </summary>
        public StudentAgent GetCreator()
        {
            return creator;
        }

        /// <summary>
        /// Checks if this mess has been cleaned
        /// </summary>
        public bool IsCleaned()
        {
            return isCleaned;
        }

        /// <summary>
        /// Gets how long this mess has existed (in seconds)
        /// </summary>
        public float GetAge()
        {
            return Time.time - creationTime;
        }
    }
}

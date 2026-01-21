using UnityEngine;

namespace FunClass.Core
{
    public class StudentInteractableObject : MonoBehaviour
    {
        [Header("Object Properties")]
        public string objectName;
        public bool canBeKnockedOver = false;
        public bool canMakeNoise = false;
        public bool canBeDropped = false;
        public bool canBeThrown = false;

        [Header("Visual Feedback")]
        public bool isKnockedOver = false;

        private Vector3 originalPosition;
        private Quaternion originalRotation;

        void Start()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }

        public void KnockOver(StudentAgent student)
        {
            Debug.Log($"[StudentInteractableObject] KnockOver called on {objectName}, canBeKnockedOver: {canBeKnockedOver}");
            
            if (!canBeKnockedOver)
            {
                Debug.Log($"[StudentInteractableObject] {objectName} cannot be knocked over - returning");
                return;
            }

            isKnockedOver = true;
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);

            Debug.Log($"[StudentInteractableObject] {objectName} knocked over, logging event...");

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.KnockedOverObject,
                    $"knocked over {objectName}",
                    gameObject
                );
            }
            else
            {
                Debug.LogWarning($"[StudentInteractableObject] StudentEventManager.Instance is null!");
            }
        }

        public void MakeNoise(StudentAgent student)
        {
            Debug.Log($"[StudentInteractableObject] MakeNoise called on {objectName}, canMakeNoise: {canMakeNoise}");
            
            if (!canMakeNoise)
            {
                Debug.Log($"[StudentInteractableObject] {objectName} cannot make noise - returning");
                return;
            }

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.MakingNoise,
                    $"is making noise with {objectName}",
                    gameObject
                );
            }
        }

        public void Drop(StudentAgent student)
        {
            Debug.Log($"[StudentInteractableObject] Drop called on {objectName}, canBeDropped: {canBeDropped}");
            
            if (!canBeDropped)
            {
                Debug.Log($"[StudentInteractableObject] {objectName} cannot be dropped - returning");
                return;
            }

            Vector3 dropPosition = student.transform.position + student.transform.forward * 0.5f;
            transform.position = dropPosition;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.DroppedItem,
                    $"dropped {objectName}",
                    gameObject
                );
            }
        }

        public void Throw(StudentAgent student)
        {
            Debug.Log($"[StudentInteractableObject] Throw called on {objectName}, canBeThrown: {canBeThrown}");

            if (!canBeThrown)
            {
                Debug.Log($"[StudentInteractableObject] {objectName} cannot be thrown - returning");
                return;
            }

            Vector3 throwDirection = student.transform.forward + Vector3.up * 0.5f;
            transform.position += throwDirection * 2f;

            Debug.Log($"[StudentInteractableObject] {objectName} thrown, logging event... StudentEventManager.Instance: {(StudentEventManager.Instance != null ? "OK" : "NULL")}");

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.ThrowingObject,
                    $"threw {objectName}",
                    gameObject
                );
                Debug.Log($"[StudentInteractableObject] {objectName} event logged successfully");
            }
            else
            {
                Debug.LogWarning($"[StudentInteractableObject] StudentEventManager.Instance is null!");
            }
        }

        /// <summary>
        /// Required distance for throwing at a specific student (SingleStudent influence)
        /// </summary>
        public const float THROW_AT_STUDENT_REQUIRED_DISTANCE = 2f;

        /// <summary>
        /// Throw at a specific student. If distance > 2m, source will move closer first.
        /// </summary>
        public void ThrowAt(StudentAgent sourceStudent, StudentAgent targetStudent)
        {
            Debug.Log($"[StudentInteractableObject] ThrowAt called - source: {sourceStudent?.Config?.studentName}, target: {targetStudent?.Config?.studentName}");

            if (!canBeThrown)
            {
                Debug.Log($"[StudentInteractableObject] {objectName} cannot be thrown - returning");
                return;
            }

            if (sourceStudent == null || targetStudent == null)
            {
                Debug.LogWarning($"[StudentInteractableObject] ThrowAt - source or target is null");
                return;
            }

            float distance = Vector3.Distance(sourceStudent.transform.position, targetStudent.transform.position);
            Debug.Log($"[StudentInteractableObject] Distance between {sourceStudent.Config?.studentName} and {targetStudent.Config?.studentName}: {distance:F2}m (required: <= {THROW_AT_STUDENT_REQUIRED_DISTANCE}m)");

            if (distance <= THROW_AT_STUDENT_REQUIRED_DISTANCE)
            {
                // Close enough, throw immediately
                ExecuteThrowAt(sourceStudent, targetStudent);
            }
            else
            {
                // Need to move closer first
                Debug.Log($"[StudentInteractableObject] {sourceStudent.Config?.studentName} needs to move closer to {targetStudent.Config?.studentName}");

                if (StudentMovementManager.Instance != null)
                {
                    StudentMovementManager.Instance.MoveToStudent(
                        sourceStudent,
                        targetStudent,
                        THROW_AT_STUDENT_REQUIRED_DISTANCE,
                        () => ExecuteThrowAt(sourceStudent, targetStudent)
                    );
                }
                else
                {
                    Debug.LogWarning("[StudentInteractableObject] StudentMovementManager.Instance is null - executing throw anyway");
                    ExecuteThrowAt(sourceStudent, targetStudent);
                }
            }
        }

        /// <summary>
        /// Execute the throw action at a specific student (called after distance requirement met)
        /// </summary>
        private void ExecuteThrowAt(StudentAgent sourceStudent, StudentAgent targetStudent)
        {
            Debug.Log($"[StudentInteractableObject] ExecuteThrowAt - {sourceStudent.Config?.studentName} throwing {objectName} at {targetStudent.Config?.studentName}");

            // Move object toward target
            Vector3 throwDirection = (targetStudent.transform.position - sourceStudent.transform.position).normalized;
            throwDirection.y = 0.3f; // Slight upward arc
            transform.position = targetStudent.transform.position + Vector3.up * 0.5f;

            // Log event with target student for SingleStudent influence
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    sourceStudent,
                    StudentEventType.ThrowingObject,
                    $"threw {objectName} at {targetStudent.Config?.studentName}",
                    gameObject,
                    targetStudent,  // Specify target for SingleStudent influence
                    InfluenceScope.SingleStudent
                );
                Debug.Log($"[StudentInteractableObject] ThrowAt event logged - SingleStudent influence on {targetStudent.Config?.studentName}");
            }
        }

        public void Touch(StudentAgent student)
        {
            Debug.Log($"[StudentInteractableObject] Touch called on {objectName}");
            
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.TouchedObject,
                    $"touched {objectName}",
                    gameObject
                );
            }
        }

        public void Reset()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            isKnockedOver = false;
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    public enum SizeCategory { Small, Large }

    public class StudentInteractableObject : MonoBehaviour
    {
        [Header("Object Properties")]
        public string objectName;
        [Tooltip("Tên tiếng Việt dùng trong popup dialogue — fallback về objectName nếu rỗng")]
        public string displayName;
        public bool canBeKnockedOver = false;
        public bool canMakeNoise = false;
        public bool canBeDropped = false;
        public bool canBeThrown = false;

        [Header("Size")]
        public SizeCategory sizeCategory = SizeCategory.Small;

        [Header("Mess on Knock")]
        [Tooltip("Loại mess spawn khi object bị knock over. Set None để không spawn (vd: bàn).")]
        public MessType knockMessType = MessType.BrokenGlass;

        [Header("Visual Feedback")]
        public bool isKnockedOver = false;

        // Attach offsets (world-space relative to parent transform)
        private static readonly Vector3 HEAD_OFFSET = new Vector3(0f, 1.7f, 0f);
        private static readonly Vector3 DESK_FORWARD_OFFSET = new Vector3(0f, 0.75f, 0.5f);
        private const float DESK_RANDOM_TILT_DEG = 15f;

        // Stack tracking — key: target student, value: stack count on their desk
        private static readonly Dictionary<StudentAgent, int> deskStackCount = new Dictionary<StudentAgent, int>();

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Transform originalParent;

        public string DisplayNameOrFallback => string.IsNullOrEmpty(displayName) ? objectName : displayName;

        public const float THROW_AT_STUDENT_REQUIRED_DISTANCE = 2f;

        void Start()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalParent = transform.parent;
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public void KnockOver(StudentAgent student)
        {
            if (!canBeKnockedOver) return;

            isKnockedOver = true;
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.KnockedOverObject,
                    $"knocked over {objectName}",
                    gameObject
                );
            }

            MessSpawner.Instance?.SpawnAt(transform.position, knockMessType);
            GameLogger.Milestone("StudentInteractableObject", $"{objectName} knocked over by {student?.Config?.studentName}", "ObjectKnockedOver", student?.Config?.studentName, null);
        }

        public void MakeNoise(StudentAgent student)
        {
            if (!canMakeNoise) return;

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
            if (!canBeDropped) return;

            transform.position = student.transform.position + student.transform.forward * 0.5f;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.DroppedItem,
                    $"dropped {objectName}",
                    gameObject
                );
            }

            MessSpawner.Instance?.SpawnAt(transform.position, MessType.TornPaper);
        }

        public void Throw(StudentAgent student)
        {
            if (!canBeThrown) return;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.ThrowingObject,
                    $"threw {objectName}",
                    gameObject
                );
            }

            ThrowNoTarget(student);
        }

        public void ThrowAt(StudentAgent sourceStudent, StudentAgent targetStudent)
        {
            if (!canBeThrown) return;
            if (sourceStudent == null || targetStudent == null) return;

            float distance = Vector3.Distance(sourceStudent.transform.position, targetStudent.transform.position);

            if (distance <= THROW_AT_STUDENT_REQUIRED_DISTANCE)
            {
                ExecuteThrowAt(sourceStudent, targetStudent);
            }
            else
            {
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
                    ExecuteThrowAt(sourceStudent, targetStudent);
                }
            }
        }

        public void Touch(StudentAgent student)
        {
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

        /// <summary>
        /// Public wrapper — attach this object to target (head if Small, desk if Large).
        /// Used by ThrowableSpawner.SpawnAtTarget for on-demand spawning.
        /// </summary>
        public void AttachToTarget(StudentAgent target)
        {
            if (target == null) return;
            if (sizeCategory == SizeCategory.Small)
                AttachToHead(target);
            else
                AttachToDesk(target);
        }

        public void ResetObject()
        {
            transform.SetParent(originalParent);
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            isKnockedOver = false;

            GameLogger.Detail("StudentInteractableObject", $"{objectName} reset to original position");
        }

        // ------------------------------------------------------------------
        // Internal
        // ------------------------------------------------------------------

        private void ExecuteThrowAt(StudentAgent sourceStudent, StudentAgent targetStudent)
        {
            // Re-validate after walk-closer callback — target/source may have been destroyed during walk
            if (sourceStudent == null || targetStudent == null)
            {
                GameLogger.Detail("StudentInteractableObject", $"{objectName} ExecuteThrowAt aborted — source or target destroyed during walk");
                return;
            }

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    sourceStudent,
                    StudentEventType.ThrowingObject,
                    $"threw {objectName} at {targetStudent.Config?.studentName}",
                    gameObject,
                    targetStudent,
                    InfluenceScope.SingleStudent
                );
            }

            if (sizeCategory == SizeCategory.Small)
                AttachToHead(targetStudent);
            else
                AttachToDesk(targetStudent);

            MessType messType = sizeCategory == SizeCategory.Small ? MessType.TornPaper : MessType.BrokenGlass;
            MessSpawner.Instance?.SpawnAt(targetStudent.transform.position, messType);
        }

        private void ThrowNoTarget(StudentAgent student)
        {
            DetachToFloor(student);
            MessSpawner.Instance?.SpawnAt(transform.position, MessType.TornPaper);
        }

        private void AttachToHead(StudentAgent target)
        {
            transform.SetParent(target.transform);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = HEAD_OFFSET;
            Physics.SyncTransforms();

            // Adjust so combined mesh bottom sits at HEAD_OFFSET.y world Y
            float headWorldY = transform.position.y;
            float currentBottomY = ComputeWorldBottomY(gameObject, headWorldY);
            float adjustment = headWorldY - currentBottomY;
            transform.localPosition = HEAD_OFFSET + Vector3.up * adjustment;

            GameLogger.Detail("StudentInteractableObject",
                $"{objectName} head attach: headY={headWorldY:F2}, bottom={currentBottomY:F2}, adjust={adjustment:F2}");

            GameLogger.Milestone("StudentInteractableObject",
                $"{objectName} attached to head of {target.Config?.studentName}",
                "ObjectAttachedToTarget", null, target.Config?.studentName);
        }

        private void AttachToDesk(StudentAgent target)
        {
            int stackIndex = 0;
            if (deskStackCount.ContainsKey(target))
            {
                stackIndex = deskStackCount[target];
                deskStackCount[target]++;
            }
            else
            {
                deskStackCount[target] = 1;
            }

            // Determine desk top anchor (world-space)
            Transform desk = ThrowableSpawner.Instance?.GetDeskForStudent(target);
            float deskTopY;
            Vector3 anchorXZ;
            if (desk != null)
            {
                deskTopY = ThrowableSpawner.GetDeskTopY(desk);
                anchorXZ = new Vector3(desk.position.x, 0f, desk.position.z);
            }
            else
            {
                deskTopY = target.OriginalSeatPosition.y + DESK_FORWARD_OFFSET.y;
                Vector3 fallback = target.OriginalSeatPosition + target.transform.forward * DESK_FORWARD_OFFSET.z;
                anchorXZ = new Vector3(fallback.x, 0f, fallback.z);
            }

            transform.SetParent(null);  // world space — stays on desk even after LeftSeat

            // Apply rotation BEFORE measuring bounds so they reflect the tilted mesh
            transform.rotation = Quaternion.Euler(
                Random.Range(-DESK_RANDOM_TILT_DEG, DESK_RANDOM_TILT_DEG),
                Random.Range(0f, 360f),
                Random.Range(-DESK_RANDOM_TILT_DEG, DESK_RANDOM_TILT_DEG)
            );

            // Initial placement so renderer bounds reflect deskTopY anchor
            transform.position = new Vector3(anchorXZ.x, deskTopY, anchorXZ.z);
            Physics.SyncTransforms();  // flush transform → bounds cache

            // Combined bounds across ALL child renderers (laptop has multi-mesh: screen+body+keyboard)
            float currentBottomY = ComputeWorldBottomY(gameObject, transform.position.y);
            float adjustment = deskTopY - currentBottomY;
            transform.position = new Vector3(
                anchorXZ.x,
                transform.position.y + adjustment + stackIndex * 0.1f,
                anchorXZ.z
            );

            GameLogger.Detail("StudentInteractableObject",
                $"{objectName} desk attach: deskTopY={deskTopY:F2}, currentBottom={currentBottomY:F2}, adjust={adjustment:F2}, finalY={transform.position.y:F2}");

            GameLogger.Milestone("StudentInteractableObject",
                $"{objectName} landed on desk of {target.Config?.studentName} (stack {stackIndex})",
                "ObjectAttachedToTarget", null, target.Config?.studentName);
        }

        /// <summary>
        /// Compute the world-space Y of the lowest point across ALL child renderers.
        /// Falls back to the provided pivot Y if no renderers found.
        /// </summary>
        private static float ComputeWorldBottomY(GameObject obj, float fallbackPivotY)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return fallbackPivotY;

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combined.Encapsulate(renderers[i].bounds);
            return combined.min.y;
        }

        private void DetachToFloor(StudentAgent student = null)
        {
            transform.SetParent(null);
            Vector3 dropPos = student != null
                ? student.transform.position + student.transform.forward * 1f
                : transform.position;
            dropPos.y = 0f;  // sàn classroom (y=0 in level layout)
            transform.position = dropPos;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        // Called by LevelManager.EndLevel to clean up
        public static void ClearDeskStackCounts()
        {
            deskStackCount.Clear();
        }

    }
}

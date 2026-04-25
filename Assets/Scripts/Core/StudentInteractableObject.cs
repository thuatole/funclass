using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    public enum SizeCategory { Small, Large }

    /// <summary>
    /// Visual artifact spawned at a target student during a scripted interaction.
    /// Single responsibility: identify itself (objectName, displayName, sizeCategory)
    /// and attach to target's head (Small) or desk (Large).
    /// All autonomous interaction methods (KnockOver, Throw, etc.) were removed —
    /// the only valid flow is ThrowableSpawner.SpawnAtTarget → AttachToTarget.
    /// </summary>
    public class StudentInteractableObject : MonoBehaviour
    {
        [Header("Identity")]
        public string objectName;
        [Tooltip("Tên tiếng Việt dùng trong popup dialogue — fallback về objectName nếu rỗng")]
        public string displayName;
        public SizeCategory sizeCategory = SizeCategory.Small;

        // Attach offsets
        private static readonly Vector3 HEAD_OFFSET = new Vector3(0f, 1.7f, 0f);
        private const float DESK_RANDOM_TILT_DEG = 15f;

        // Stack tracking — key: target student, value: stack count on their desk
        private static readonly Dictionary<StudentAgent, int> deskStackCount = new Dictionary<StudentAgent, int>();

        public string DisplayNameOrFallback => string.IsNullOrEmpty(displayName) ? objectName : displayName;

        /// <summary>
        /// Attach this object to target — head (Small) or desk top (Large).
        /// </summary>
        public void AttachToTarget(StudentAgent target)
        {
            if (target == null) return;
            if (sizeCategory == SizeCategory.Small)
                AttachToHead(target);
            else
                AttachToDesk(target);
        }

        // ------------------------------------------------------------------
        // Internal
        // ------------------------------------------------------------------

        private void AttachToHead(StudentAgent target)
        {
            transform.SetParent(target.transform);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = HEAD_OFFSET;
            Physics.SyncTransforms();

            // Adjust so combined mesh bottom sits at HEAD_OFFSET.y world Y (compensate for pivot offset)
            float headWorldY = transform.position.y;
            float currentBottomY = ComputeWorldBottomY(gameObject, headWorldY);
            float adjustment = headWorldY - currentBottomY;
            transform.localPosition = HEAD_OFFSET + Vector3.up * adjustment;

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

            // Determine desk top anchor
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
                // Fallback: derive roughly from seat
                deskTopY = target.OriginalSeatPosition.y + 0.75f;
                Vector3 fb = target.OriginalSeatPosition + target.transform.forward * 0.5f;
                anchorXZ = new Vector3(fb.x, 0f, fb.z);
            }

            transform.SetParent(null);  // world space — stays on desk even after LeftSeat
            transform.rotation = Quaternion.Euler(
                Random.Range(-DESK_RANDOM_TILT_DEG, DESK_RANDOM_TILT_DEG),
                Random.Range(0f, 360f),
                Random.Range(-DESK_RANDOM_TILT_DEG, DESK_RANDOM_TILT_DEG)
            );
            transform.position = new Vector3(anchorXZ.x, deskTopY, anchorXZ.z);
            Physics.SyncTransforms();

            // Compensate for pivot offset so mesh bottom sits on desk surface
            float currentBottomY = ComputeWorldBottomY(gameObject, transform.position.y);
            float adjustment = deskTopY - currentBottomY;
            transform.position = new Vector3(
                anchorXZ.x,
                transform.position.y + adjustment + stackIndex * 0.1f,
                anchorXZ.z
            );

            GameLogger.Milestone("StudentInteractableObject",
                $"{objectName} landed on desk of {target.Config?.studentName} (stack {stackIndex})",
                "ObjectAttachedToTarget", null, target.Config?.studentName);
        }

        /// <summary>
        /// World-space Y of the lowest point across all child renderers.
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

        public static void ClearDeskStackCounts()
        {
            deskStackCount.Clear();
        }
    }
}

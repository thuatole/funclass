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
            if (!canBeThrown) return;

            Vector3 throwDirection = student.transform.forward + Vector3.up * 0.5f;
            transform.position += throwDirection * 2f;

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.ThrowingObject,
                    $"threw {objectName}",
                    gameObject
                );
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

        public void Reset()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            isKnockedOver = false;
        }
    }
}

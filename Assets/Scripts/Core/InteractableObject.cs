using UnityEngine;
using System;

namespace FunClass.Core
{
    public class InteractableObject : MonoBehaviour
    {
        [Header("Interaction Type")]
        public bool isPickable = false;
        public bool isUsable = false;

        [Header("Object Info")]
        public string objectName;
        [TextArea(2, 3)]
        public string interactionDescription;

        public event Action<TeacherController> OnPickedUp;
        public event Action<TeacherController> OnUsed;

        public virtual void Interact(TeacherController teacher)
        {
            if (isPickable)
            {
                PickUp(teacher);
            }
            else if (isUsable)
            {
                Use(teacher);
            }
        }

        protected virtual void PickUp(TeacherController teacher)
        {
            Debug.Log($"[InteractableObject] {objectName} picked up");
            OnPickedUp?.Invoke(teacher);

            HeldItem heldItem = GetComponent<HeldItem>();
            if (heldItem != null)
            {
                teacher.HoldItem(heldItem);
            }
        }

        protected virtual void Use(TeacherController teacher)
        {
            Debug.Log($"[InteractableObject] {objectName} used");
            OnUsed?.Invoke(teacher);
        }

        public string GetInteractionPrompt()
        {
            if (isPickable) return $"Pick up {objectName}";
            if (isUsable) return $"Use {objectName}";
            return objectName;
        }
    }
}

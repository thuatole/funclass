using UnityEngine;
using UnityEditor;

namespace FunClass.Editor
{
    public static class DebugSceneState
    {
        [MenuItem("FunClass/Debug/Check Teacher State")]
        public static void CheckTeacherState()
        {
            var teacher = GameObject.Find("Teacher");
            if (teacher == null)
            {
                Debug.LogError("[Debug] Teacher GameObject not found in scene!");
                return;
            }

            Debug.Log($"[Debug] Teacher GameObject found");
            Debug.Log($"[Debug] - Active in hierarchy: {teacher.activeInHierarchy}");
            Debug.Log($"[Debug] - Active self: {teacher.activeSelf}");
            
            var controller = teacher.GetComponent<FunClass.Core.TeacherController>();
            if (controller == null)
            {
                Debug.LogError("[Debug] TeacherController component not found!");
            }
            else
            {
                Debug.Log($"[Debug] TeacherController found");
                Debug.Log($"[Debug] - Enabled: {controller.enabled}");
                Debug.Log($"[Debug] - Is Instance: {FunClass.Core.TeacherController.Instance == controller}");
            }
            
            var charController = teacher.GetComponent<CharacterController>();
            if (charController == null)
            {
                Debug.LogError("[Debug] CharacterController component not found!");
            }
            else
            {
                Debug.Log($"[Debug] CharacterController found");
                Debug.Log($"[Debug] - Enabled: {charController.enabled}");
            }
            
            var camera = teacher.GetComponentInChildren<Camera>();
            if (camera == null)
            {
                Debug.LogError("[Debug] Camera not found in Teacher children!");
            }
            else
            {
                Debug.Log($"[Debug] Camera found: {camera.gameObject.name}");
                Debug.Log($"[Debug] - Active: {camera.gameObject.activeInHierarchy}");
                Debug.Log($"[Debug] - Tag: {camera.tag}");
            }
            
            // Check GameStateManager
            if (FunClass.Core.GameStateManager.Instance == null)
            {
                Debug.LogError("[Debug] GameStateManager.Instance is NULL!");
            }
            else
            {
                Debug.Log($"[Debug] GameStateManager.Instance exists");
                Debug.Log($"[Debug] - Current state: {FunClass.Core.GameStateManager.Instance.CurrentState}");
            }
        }
        
        [MenuItem("FunClass/Debug/Force Activate Teacher")]
        public static void ForceActivateTeacher()
        {
            var teacher = GameObject.Find("Teacher");
            if (teacher == null)
            {
                Debug.LogError("[Debug] Teacher GameObject not found!");
                return;
            }
            
            teacher.SetActive(true);
            
            var controller = teacher.GetComponent<FunClass.Core.TeacherController>();
            if (controller != null)
            {
                controller.enabled = true;
                Debug.Log("[Debug] Teacher activated and controller enabled");
            }
            
            // Manually trigger state change
            if (FunClass.Core.GameStateManager.Instance != null)
            {
                FunClass.Core.GameStateManager.Instance.StartLevel();
                Debug.Log("[Debug] Manually called StartLevel()");
            }
        }
    }
}

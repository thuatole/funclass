using UnityEngine;
using UnityEditor;
using FunClass.Core;

namespace FunClass.Editor
{
    /// <summary>
    /// Quick setup utility to add missing student systems to scene
    /// </summary>
    public class QuickSetupStudentSystems : EditorWindow
    {
        [MenuItem("FunClass/Quick Setup/Add Student Systems")]
        public static void ShowWindow()
        {
            GetWindow<QuickSetupStudentSystems>("Setup Student Systems");
        }

        private void OnGUI()
        {
            GUILayout.Label("Student Systems Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Add StudentInteractionProcessor to Scene"))
            {
                AddStudentInteractionProcessor();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Add StudentVisualMarker to All Students"))
            {
                AddVisualMarkersToStudents();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Setup All (Processor + Visual Markers)"))
            {
                AddStudentInteractionProcessor();
                AddVisualMarkersToStudents();
            }
        }

        private static void AddStudentInteractionProcessor()
        {
            // Check if already exists
            StudentInteractionProcessor existing = FindObjectOfType<StudentInteractionProcessor>();
            if (existing != null)
            {
                Debug.Log("[QuickSetup] StudentInteractionProcessor already exists in scene");
                EditorUtility.DisplayDialog("Already Exists", "StudentInteractionProcessor already exists in scene", "OK");
                return;
            }

            // Find or create Managers group
            GameObject managersGroup = GameObject.Find("=== MANAGERS ===");
            if (managersGroup == null)
            {
                managersGroup = new GameObject("=== MANAGERS ===");
                Debug.Log("[QuickSetup] Created Managers group");
            }

            // Create processor
            GameObject processorObj = new GameObject("StudentInteractionProcessor");
            processorObj.transform.SetParent(managersGroup.transform);
            processorObj.AddComponent<StudentInteractionProcessor>();

            Debug.Log("[QuickSetup] ✓ Added StudentInteractionProcessor to scene");
            EditorUtility.DisplayDialog("Success", "StudentInteractionProcessor added to scene!\n\nNote: You need to load interactions via code or JSON importer.", "OK");
        }

        private static void AddVisualMarkersToStudents()
        {
            StudentAgent[] students = FindObjectsOfType<StudentAgent>();
            
            if (students.Length == 0)
            {
                Debug.LogWarning("[QuickSetup] No StudentAgent found in scene");
                EditorUtility.DisplayDialog("No Students", "No StudentAgent components found in scene", "OK");
                return;
            }

            int added = 0;
            foreach (StudentAgent student in students)
            {
                // Check if already has marker
                if (student.GetComponent<StudentVisualMarker>() != null)
                {
                    Debug.Log($"[QuickSetup] {student.gameObject.name} already has StudentVisualMarker");
                    continue;
                }

                // Add marker
                student.gameObject.AddComponent<StudentVisualMarker>();
                added++;
                Debug.Log($"[QuickSetup] ✓ Added StudentVisualMarker to {student.gameObject.name}");
            }

            Debug.Log($"[QuickSetup] Added StudentVisualMarker to {added}/{students.Length} students");
            EditorUtility.DisplayDialog("Success", $"Added StudentVisualMarker to {added} students!\n\nColors:\nA=Red, B=Blue, C=Green, D=Yellow\nE=Orange, F=Purple, G=Cyan, H=Pink", "OK");
        }
    }
}

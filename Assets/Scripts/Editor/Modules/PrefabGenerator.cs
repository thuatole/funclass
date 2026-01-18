using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FunClass.Editor.Data;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tạo và quản lý prefabs
    /// </summary>
    public static class PrefabGenerator
    {
        /// <summary>
        /// Tạo prefabs từ data
        /// </summary>
        public static void CreatePrefabsFromData(List<PrefabData> prefabsData)
        {
            foreach (var prefabData in prefabsData)
            {
                CreatePrefabInstance(prefabData);
            }
        }

        /// <summary>
        /// Tạo một prefab instance trong scene
        /// </summary>
        public static GameObject CreatePrefabInstance(PrefabData data)
        {
            GameObject prefabAsset = null;

            // Try to load prefab from path
            if (!string.IsNullOrEmpty(data.prefabPath))
            {
                prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefabPath);
            }

            GameObject instance;

            if (prefabAsset != null)
            {
                // Instantiate from prefab
                instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            }
            else
            {
                // Create new GameObject if prefab not found
                instance = CreateDefaultPrefab(data.prefabType);
                instance.name = data.prefabName;
            }

            if (instance != null)
            {
                // Set transform
                instance.transform.position = data.position.ToVector3();
                instance.transform.rotation = Quaternion.Euler(data.rotation.ToVector3());
                instance.transform.localScale = data.scale.ToVector3();

                // Parent to appropriate group
                ParentToGroup(instance, data.prefabType);

                Undo.RegisterCreatedObjectUndo(instance, "Create Prefab Instance");
            }

            return instance;
        }

        /// <summary>
        /// Tạo prefab từ GameObject hiện có
        /// </summary>
        public static GameObject CreatePrefabFromGameObject(GameObject obj, string savePath)
        {
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, savePath);
            
            if (prefab != null)
            {
                Debug.Log($"[PrefabGenerator] Created prefab: {savePath}");
            }
            else
            {
                Debug.LogError($"[PrefabGenerator] Failed to create prefab: {savePath}");
            }

            return prefab;
        }

        /// <summary>
        /// Tạo prefab variant
        /// </summary>
        public static GameObject CreatePrefabVariant(GameObject basePrefab, string variantPath)
        {
            if (basePrefab == null)
            {
                Debug.LogError("[PrefabGenerator] Base prefab is null");
                return null;
            }

            GameObject variant = PrefabUtility.SaveAsPrefabAsset(basePrefab, variantPath);
            
            if (variant != null)
            {
                Debug.Log($"[PrefabGenerator] Created variant: {variantPath}");
            }

            return variant;
        }

        /// <summary>
        /// Export GameObject thành PrefabData
        /// </summary>
        public static PrefabData ExportGameObjectToPrefabData(GameObject obj, string prefabPath = "")
        {
            return new PrefabData
            {
                prefabName = obj.name,
                prefabType = DeterminePrefabType(obj),
                position = obj.transform.position,
                rotation = obj.transform.rotation.eulerAngles,
                scale = obj.transform.localScale,
                prefabPath = prefabPath
            };
        }

        /// <summary>
        /// Batch create prefabs từ selection
        /// </summary>
        [MenuItem("Tools/FunClass/Prefabs/Create Prefabs from Selection")]
        public static void CreatePrefabsFromSelection()
        {
            GameObject[] selected = Selection.gameObjects;
            
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select GameObjects to create prefabs", "OK");
                return;
            }

            string folderPath = EditorUtility.OpenFolderPanel("Select Prefab Folder", "Assets/Prefabs", "");
            
            if (string.IsNullOrEmpty(folderPath))
                return;

            // Convert to relative path
            folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);

            int count = 0;
            foreach (GameObject obj in selected)
            {
                string prefabPath = $"{folderPath}/{obj.name}.prefab";
                if (CreatePrefabFromGameObject(obj, prefabPath) != null)
                {
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Created {count} prefabs in {folderPath}", "OK");
        }

        private static GameObject CreateDefaultPrefab(string prefabType)
        {
            GameObject obj;

            switch (prefabType.ToLower())
            {
                case "student":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    obj.name = "Student";
                    break;

                case "furniture":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = "Furniture";
                    break;

                case "interactable":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.name = "Interactable";
                    obj.AddComponent<FunClass.Core.StudentInteractableObject>();
                    break;

                case "decoration":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = "Decoration";
                    break;

                default:
                    obj = new GameObject("Object");
                    break;
            }

            return obj;
        }

        private static void ParentToGroup(GameObject obj, string prefabType)
        {
            GameObject parent = null;

            switch (prefabType.ToLower())
            {
                case "student":
                    parent = GameObject.Find("=== STUDENTS ===");
                    break;

                case "furniture":
                    GameObject classroom = GameObject.Find("=== CLASSROOM ===");
                    if (classroom != null)
                    {
                        parent = classroom.transform.Find("Furniture")?.gameObject;
                    }
                    break;

                case "interactable":
                case "decoration":
                    parent = GameObject.Find("=== CLASSROOM ===");
                    break;
            }

            if (parent != null)
            {
                obj.transform.SetParent(parent.transform);
            }
        }

        private static string DeterminePrefabType(GameObject obj)
        {
            if (obj.GetComponent<FunClass.Core.StudentAgent>() != null)
                return "Student";
            
            if (obj.GetComponent<FunClass.Core.StudentInteractableObject>() != null)
                return "Interactable";

            if (obj.GetComponent<FunClass.Core.TeacherController>() != null)
                return "Teacher";

            return "Decoration";
        }
    }
}

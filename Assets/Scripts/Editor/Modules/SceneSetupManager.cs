using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Manages scene creation, clearing, and saving
    /// </summary>
    public static class SceneSetupManager
    {
        /// <summary>
        /// Create new scene or clear existing scene for level import
        /// </summary>
        public static void CreateOrClearScene(string levelId)
        {
            Debug.Log($"[SceneSetupManager] Preparing scene for level: {levelId}");
            
            // Check if we have an existing scene open
            var currentScene = EditorSceneManager.GetActiveScene();
            bool isNewScene = string.IsNullOrEmpty(currentScene.path);
            
            if (isNewScene)
            {
                // Create new scene
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                Debug.Log("[SceneSetupManager] Created new scene");
            }
            else
            {
                // Clear existing level objects but keep essential ones
                ClearLevelObjects();
                Debug.Log("[SceneSetupManager] Cleared existing level objects");
            }
            
            // Setup scene hierarchy structure
            SetupSceneHierarchy();
            
            // Create level-specific groups
            CreateLevelGroups(levelId);
        }
        
        /// <summary>
        /// Clear level-specific objects from scene
        /// </summary>
        private static void ClearLevelObjects()
        {
            // Objects to preserve (don't delete these)
            string[] preserveObjects = {
                "Main Camera",
                "Directional Light",
                "Lighting",
                "=== MANAGERS ===" // Keep managers if they exist
            };
            
            // Delete level-specific groups
            string[] levelGroups = {
                "=== CLASSROOM ===",
                "=== STUDENTS ===",
                "=== TEACHER ===",
                "=== UI ===",
                "Desks",
                "Students", 
                "Board",
                "Door",
                "Walls",
                "Floor",
                "TeacherArea",
                "Routes",
                "Waypoints"
            };
            
            foreach (string groupName in levelGroups)
            {
                GameObject group = GameObject.Find(groupName);
                if (group != null)
                {
                    Object.DestroyImmediate(group);
                }
            }
            
            // Clean up any orphaned objects (except preserved ones)
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.transform.parent == null) // Root objects only
                {
                    bool shouldPreserve = false;
                    foreach (string preserveName in preserveObjects)
                    {
                        if (obj.name == preserveName)
                        {
                            shouldPreserve = true;
                            break;
                        }
                    }
                    
                    if (!shouldPreserve && !obj.name.StartsWith("==") && !IsEssentialObject(obj))
                    {
                        Object.DestroyImmediate(obj);
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if object is essential (should not be deleted)
        /// </summary>
        private static bool IsEssentialObject(GameObject obj)
        {
            // Check components that indicate essential object
            if (obj.GetComponent<Camera>() != null) return true;
            if (obj.GetComponent<Light>() != null && obj.name.Contains("Light")) return true;
            if (obj.GetComponent<AudioListener>() != null) return true;
            
            return false;
        }
        
        /// <summary>
        /// Setup scene hierarchy structure
        /// </summary>
        private static void SetupSceneHierarchy()
        {
            // Create main organizational groups
            CreateOrFindGameObject("=== CLASSROOM ===");
            CreateOrFindGameObject("=== STUDENTS ===");
            CreateOrFindGameObject("=== TEACHER ===");
            CreateOrFindGameObject("=== UI ===");
            CreateOrFindGameObject("=== MANAGERS ===");
            CreateOrFindGameObject("Lighting");
            CreateOrFindGameObject("Routes");
        }
        
        /// <summary>
        /// Create level-specific groups
        /// </summary>
        private static void CreateLevelGroups(string levelId)
        {
            // Create level root group
            GameObject levelRoot = CreateOrFindGameObject($"Level_{levelId}");
            
            // Parent main groups under level root (optional)
            // For now, we'll keep them at root for compatibility
        }
        
        /// <summary>
        /// Get or create classroom group
        /// </summary>
        public static GameObject GetOrCreateClassroomGroup()
        {
            return CreateOrFindGameObject("=== CLASSROOM ===");
        }
        
        /// <summary>
        /// Get or create students group
        /// </summary>
        public static GameObject GetOrCreateStudentsGroup()
        {
            return CreateOrFindGameObject("=== STUDENTS ===");
        }
        
        /// <summary>
        /// Create or find GameObject by name
        /// </summary>
        public static GameObject CreateOrFindGameObject(string name, Transform parent = null)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                if (parent != null)
                {
                    obj.transform.SetParent(parent);
                }
                
                #if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
                #endif
            }
            else if (parent != null && obj.transform.parent != parent)
            {
                obj.transform.SetParent(parent);
            }
            
            return obj;
        }
        
        /// <summary>
        /// Save scene to Assets/Levels/Generated folder
        /// </summary>
        public static string SaveScene(string levelId)
        {
            // Ensure directory exists
            string levelsDir = "Assets/Levels/Generated";
            EditorUtils.CreateFolderIfNotExists(levelsDir);
            
            // Create scene path
            string sceneName = $"Level_{levelId}.unity";
            string scenePath = Path.Combine(levelsDir, sceneName).Replace('\\', '/');
            
            // Check if scene already exists
            if (File.Exists(scenePath))
            {
                // Ask if user wants to overwrite
                bool overwrite = EditorUtility.DisplayDialog(
                    "Scene Already Exists",
                    $"Scene '{sceneName}' already exists. Overwrite?",
                    "Overwrite",
                    "Cancel");
                
                if (!overwrite)
                {
                    // Generate new name with timestamp
                    string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    sceneName = $"Level_{levelId}_{timestamp}.unity";
                    scenePath = Path.Combine(levelsDir, sceneName).Replace('\\', '/');
                }
            }
            
            // Save scene
            var scene = EditorSceneManager.GetActiveScene();
            bool saveResult = EditorSceneManager.SaveScene(scene, scenePath);
            
            if (saveResult)
            {
                Debug.Log($"[SceneSetupManager] Scene saved to: {scenePath}");
                
                // Refresh asset database
                AssetDatabase.Refresh();
                
                // Mark scene as dirty to ensure changes are saved
                EditorSceneManager.MarkSceneDirty(scene);
                
                return scenePath;
            }
            else
            {
                Debug.LogError($"[SceneSetupManager] Failed to save scene to: {scenePath}");
                return null;
            }
        }
        
        /// <summary>
        /// Load existing scene if it exists
        /// </summary>
        public static bool LoadScene(string levelId)
        {
            string scenePath = $"Assets/Levels/Generated/Level_{levelId}.unity";
            
            if (File.Exists(scenePath))
            {
                try
                {
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    return scene.IsValid();
                }
                catch
                {
                    return false;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Setup essential managers in the scene
        /// </summary>
        public static void SetupEssentialManagers()
        {
            GameObject managersGroup = CreateOrFindGameObject("=== MANAGERS ===");
            
            // Add essential manager components if they don't exist
            AddManagerIfMissing<FunClass.Core.ClassroomManager>("ClassroomManager", managersGroup);
            AddManagerIfMissing<FunClass.Core.LevelManager>("LevelManager", managersGroup);
            AddManagerIfMissing<FunClass.Core.GameStateManager>("GameStateManager", managersGroup);
            AddManagerIfMissing<FunClass.Core.StudentInfluenceManager>("StudentInfluenceManager", managersGroup);
            
            Debug.Log("[SceneSetupManager] Essential managers setup complete");
        }
        
        /// <summary>
        /// Add manager component if it doesn't exist
        /// </summary>
        private static void AddManagerIfMissing<T>(string name, GameObject parent) where T : Component
        {
            T manager = Object.FindObjectOfType<T>();
            if (manager == null)
            {
                GameObject managerObj = new GameObject(name);
                managerObj.transform.SetParent(parent.transform);
                managerObj.AddComponent<T>();
                
                Debug.Log($"[SceneSetupManager] Added {name} manager");
            }
        }
    }
}
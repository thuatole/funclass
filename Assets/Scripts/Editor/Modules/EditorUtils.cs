using UnityEngine;
using UnityEditor;
using System.IO;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Utility functions dùng chung cho editor scripts
    /// </summary>
    public static class EditorUtils
    {
        /// <summary>
        /// Tạo hoặc tìm GameObject trong scene
        /// </summary>
        public static GameObject CreateOrFind(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            }
            return obj;
        }

        /// <summary>
        /// Tạo child GameObject
        /// </summary>
        public static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(child, "Create " + name);
            return child;
        }

        /// <summary>
        /// Tạo ScriptableObject asset
        /// </summary>
        public static T CreateScriptableObject<T>(string path) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>
        /// Tạo folder nếu chưa tồn tại
        /// </summary>
        public static void CreateFolderIfNotExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);
                
                // Ensure parent exists
                if (!string.IsNullOrEmpty(parentFolder) && !AssetDatabase.IsValidFolder(parentFolder))
                {
                    CreateFolderIfNotExists(parentFolder);
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        /// <summary>
        /// Tạo cấu trúc folder cho level
        /// </summary>
        public static void CreateLevelFolderStructure(string levelName)
        {
            CreateFolderIfNotExists("Assets/Scenes");
            CreateFolderIfNotExists("Assets/Configs");
            CreateFolderIfNotExists($"Assets/Configs/{levelName}");
            CreateFolderIfNotExists($"Assets/Configs/{levelName}/Students");
            CreateFolderIfNotExists($"Assets/Configs/{levelName}/Routes");
            CreateFolderIfNotExists("Assets/Prefabs");
        }

        /// <summary>
        /// Xóa group trong scene
        /// </summary>
        public static void DeleteGroup(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }
}

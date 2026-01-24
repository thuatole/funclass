using UnityEngine;
using UnityEditor;
using FunClass.Editor.Data;

namespace FunClass.Editor
{
    /// <summary>
    /// Menu item to create default asset map configuration
    /// </summary>
    public static class CreateDefaultAssetMap
    {
        [MenuItem("Tools/FunClass/Create Default Asset Map")]
        public static void CreateAssetMap()
        {
            // Ask for confirmation
            if (!EditorUtility.DisplayDialog(
                "Create Default Asset Map",
                "This will create a default AssetMapConfig at Assets/Configs/DefaultAssetMap.asset\n" +
                "with auto-assigned prefabs from the school directory.\n\n" +
                "Continue?",
                "Create",
                "Cancel"))
            {
                return;
            }
            
            // Create the asset map
            var assetMap = AssetMapConfig.CreateDefaultAssetMap();
            
            if (assetMap != null)
            {
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Default asset map created at:\n{AssetDatabase.GetAssetPath(assetMap)}\n\n" +
                    "You can now use the enhanced JSON import system.",
                    "OK");
                
                // Select the asset map in the Project window
                Selection.activeObject = assetMap;
                EditorGUIUtility.PingObject(assetMap);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Failed to create default asset map.\n" +
                    "Check the console for errors.",
                    "OK");
            }
        }
        
        [MenuItem("Tools/FunClass/Update Asset Map References")]
        public static void UpdateAssetMapReferences()
        {
            string assetMapPath = "Assets/Configs/DefaultAssetMap.asset";
            AssetMapConfig assetMap = AssetDatabase.LoadAssetAtPath<AssetMapConfig>(assetMapPath);
            
            if (assetMap == null)
            {
                EditorUtility.DisplayDialog(
                    "Asset Map Not Found",
                    $"Default asset map not found at:\n{assetMapPath}\n\n" +
                    "Create it first using 'Create Default Asset Map'.",
                    "OK");
                return;
            }
            
            // Try to auto-assign prefabs and materials
            assetMap.TryAutoAssignPrefabs();
            assetMap.TryAutoAssignMaterials();
            
            EditorUtility.SetDirty(assetMap);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog(
                "Success",
                $"Asset map references updated.\n" +
                $"Assigned {CountAssignedPrefabs(assetMap)} prefabs and {CountAssignedMaterials(assetMap)} materials.",
                "OK");
            
            // Select the asset map
            Selection.activeObject = assetMap;
            EditorGUIUtility.PingObject(assetMap);
        }
        
        private static int CountAssignedPrefabs(AssetMapConfig assetMap)
        {
            int count = 0;
            foreach (var mapping in assetMap.assetMappings)
            {
                if (mapping.prefabReference != null) count++;
            }
            return count;
        }
        
        private static int CountAssignedMaterials(AssetMapConfig assetMap)
        {
            int count = 0;
            foreach (var mapping in assetMap.materialMappings)
            {
                if (mapping.materialReference != null) count++;
            }
            return count;
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using FunClass.Editor.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Detects and fixes pink/missing materials (Phase 6)
    /// </summary>
    public static class MaterialFixer
    {
        /// <summary>
        /// Default material to use when fixing pink materials
        /// </summary>
        private static Material defaultMaterial;
        
        /// <summary>
        /// Cache of known pink materials to avoid repeated checks
        /// </summary>
        private static HashSet<Material> checkedMaterials = new HashSet<Material>();
        
        /// <summary>
        /// Scan scene for pink/missing materials and fix them
        /// </summary>
        public static void ScanAndFixPinkMaterials()
        {
            Debug.Log("[MaterialFixer] Scanning for pink/missing materials...");
            
            // Get or create default material
            defaultMaterial = GetDefaultMaterial();
            if (defaultMaterial == null)
            {
                Debug.LogError("[MaterialFixer] Cannot create default material. Aborting material fix.");
                return;
            }
            
            // Scan all renderers in scene
            Renderer[] allRenderers = Object.FindObjectsOfType<Renderer>(true); // Include inactive
            int fixedCount = 0;
            int pinkCount = 0;
            
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer == null) continue;
                
                Material[] materials = renderer.sharedMaterials;
                bool needsFix = false;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        Debug.LogWarning($"[MaterialFixer] Null material on {renderer.gameObject.name}");
                        needsFix = true;
                        materials[i] = defaultMaterial;
                        pinkCount++;
                    }
                    else if (IsPinkMaterial(materials[i]))
                    {
                        if (!checkedMaterials.Contains(materials[i]))
                        {
                            Debug.LogWarning($"[MaterialFixer] Pink material detected on {renderer.gameObject.name}: {materials[i].name}");
                            checkedMaterials.Add(materials[i]);
                        }
                        needsFix = true;
                        materials[i] = defaultMaterial;
                        pinkCount++;
                    }
                }
                
                if (needsFix)
                {
                    renderer.sharedMaterials = materials;
                    fixedCount++;
                    
                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(renderer);
                    #endif
                }
            }
            
            // Also check prefabs in project
            CheckProjectPrefabs();
            
            Debug.Log($"[MaterialFixer] Scan complete. Fixed {fixedCount} objects with {pinkCount} pink/missing materials.");
            
            if (pinkCount > 0)
            {
                Debug.LogWarning($"[MaterialFixer] Found {pinkCount} pink/missing materials. They have been replaced with default material.");
            }
        }
        
        /// <summary>
        /// Check if a material is pink (missing shader)
        /// </summary>
        private static bool IsPinkMaterial(Material material)
        {
            if (material == null) return true;
            
            // Pink materials usually have magenta color (RGB ~1,0,1)
            // but we should also check if shader is missing
            if (material.shader == null)
            {
                return true;
            }
            
            // Check for known pink shader names
            string shaderName = material.shader.name.ToLower();
            if (shaderName.Contains("error") || shaderName.Contains("hidden") || shaderName.Contains("fallback"))
            {
                return true;
            }
            
            // Visual check: if material is bright magenta/pink
            if (material.HasProperty("_Color"))
            {
                Color matColor = material.color;
                // Check if color is magenta/pink (R ~1, G ~0, B ~1)
                if (matColor.r > 0.9f && matColor.g < 0.1f && matColor.b > 0.9f)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get or create default material for fixing pink materials
        /// </summary>
        private static Material GetDefaultMaterial()
        {
            // Try to load from asset map first
            var assetMap = AssetDatabase.LoadAssetAtPath<AssetMapConfig>(
                "Assets/Configs/DefaultAssetMap.asset");
            if (assetMap != null)
            {
                Material defaultMat = assetMap.GetMaterial("Default");
                if (defaultMat != null)
                {
                    return defaultMat;
                }
            }
            
            // Try to load existing default material
            string defaultMaterialPath = "Assets/Art/Materials/Default.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(defaultMaterialPath);
            
            if (material == null)
            {
                // Create new default material
                Debug.Log("[MaterialFixer] Creating default material...");
                
                // Create folder if it doesn't exist
                EditorUtils.CreateFolderIfNotExists("Assets/Art/Materials");
                
                // Create material with Standard shader
                material = new Material(Shader.Find("Standard"));
                material.name = "Default";
                material.color = Color.gray; // Neutral gray color
                
                // Save to Assets
                AssetDatabase.CreateAsset(material, defaultMaterialPath);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"[MaterialFixer] Created default material at {defaultMaterialPath}");
            }
            
            return material;
        }
        
        /// <summary>
        /// Check prefabs in project for pink materials
        /// </summary>
        private static void CheckProjectPrefabs()
        {
            #if UNITY_EDITOR
            // Find all prefabs in project
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
            
            int prefabFixCount = 0;
            
            foreach (string guid in prefabPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab == null) continue;
                
                // Check if prefab has pink materials
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                bool needsFix = false;
                
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null || IsPinkMaterial(materials[i]))
                        {
                            needsFix = true;
                            break;
                        }
                    }
                    
                    if (needsFix) break;
                }
                
                if (needsFix)
                {
                    Debug.LogWarning($"[MaterialFixer] Prefab {prefab.name} has pink/missing materials. Consider fixing manually: {path}");
                    prefabFixCount++;
                }
            }
            
            if (prefabFixCount > 0)
            {
                Debug.LogWarning($"[MaterialFixer] Found {prefabFixCount} prefabs with pink/missing materials. These should be fixed manually.");
            }
            #endif
        }
        
        /// <summary>
        /// Apply material to a specific GameObject and its children
        /// </summary>
        public static void ApplyMaterialToHierarchy(GameObject root, Material material)
        {
            if (root == null || material == null) return;
            
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
                
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(renderer);
                #endif
            }
            
            Debug.Log($"[MaterialFixer] Applied material {material.name} to {renderers.Length} renderers in {root.name}");
        }
        
        /// <summary>
        /// Create a simple material with specified color
        /// </summary>
        public static Material CreateSimpleMaterial(string name, Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.name = name;
            material.color = color;
            
            // Save to Assets
            string folderPath = "Assets/Art/Materials/Generated";
            EditorUtils.CreateFolderIfNotExists(folderPath);
            
            string materialPath = $"{folderPath}/{name}.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();
            
            return material;
        }
    }
}
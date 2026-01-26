using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FunClass.Editor.Data
{
    /// <summary>
    /// ScriptableObject for mapping asset keys to prefab references
    /// Used by the enhanced level import system
    /// </summary>
    [CreateAssetMenu(fileName = "AssetMapConfig", menuName = "FunClass/Asset Map Config", order = 1)]
    public class AssetMapConfig : ScriptableObject
    {
        [System.Serializable]
        public class AssetMappingEntry
        {
            public string assetKey; // e.g., "DESK", "BOARD", "STUDENT", "CHAIR"
            public GameObject prefabReference;
            [TextArea(1, 3)]
            public string description;
        }
        
        [System.Serializable]
        public class MaterialMappingEntry
        {
            public string materialKey; // e.g., "White", "Floor", "Wall", "Default"
            public Material materialReference;
            [TextArea(1, 3)]
            public string description;
        }
        
        public List<AssetMappingEntry> assetMappings = new List<AssetMappingEntry>();
        public List<MaterialMappingEntry> materialMappings = new List<MaterialMappingEntry>();
        
        public GameObject GetPrefab(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey))
                return null;
                
            foreach (var entry in assetMappings)
            {
                if (entry.assetKey == assetKey)
                {
                    return entry.prefabReference;
                }
            }
            
            Debug.LogWarning($"[AssetMapConfig] No prefab found for asset key: {assetKey}");
            return null;
        }
        
        public Material GetMaterial(string materialKey)
        {
            if (string.IsNullOrEmpty(materialKey))
                return null;
                
            foreach (var entry in materialMappings)
            {
                if (entry.materialKey == materialKey)
                {
                    return entry.materialReference;
                }
            }
            
            Debug.LogWarning($"[AssetMapConfig] No material found for material key: {materialKey}");
            return null;
        }
        
        public void AddDefaultMappings()
        {
            // Clear existing
            assetMappings.Clear();
            materialMappings.Clear();
            
            // Add default asset mappings
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "DESK",
                description = "Student desk (use Chair prefab if no desk available)"
            });
            
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "CHAIR",
                description = "Chair for desk"
            });
            
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "STUDENT",
                description = "Student character prefab"
            });
            
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "BOARD",
                description = "Whiteboard/blackboard at front of classroom"
            });
            
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "TEACHER",
                description = "Teacher player prefab"
            });
            
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "DOOR",
                description = "Classroom door"
            });
            
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "FLOOR",
                description = "Classroom floor (plane or prefab)"
            });
            
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "WALL",
                description = "Classroom wall (cube or prefab)"
            });
            
            assetMappings.Add(new AssetMappingEntry
            {
                assetKey = "CEILING",
                description = "Classroom ceiling (optional)"
            });
            
            // Add default material mappings
            materialMappings.Add(new MaterialMappingEntry
            {
                materialKey = "Default",
                description = "Default fallback material for pink material fix"
            });
            
            materialMappings.Add(new MaterialMappingEntry
            {
                materialKey = "White",
                description = "White material for board"
            });
            
            materialMappings.Add(new MaterialMappingEntry
            {
                materialKey = "Floor",
                description = "Floor material"
            });
            
            materialMappings.Add(new MaterialMappingEntry
            {
                materialKey = "Wall",
                description = "Wall material"
            });
            
            Debug.Log("[AssetMapConfig] Added default mappings. Assign prefab references in the Inspector.");
            
            // Try to auto-assign prefabs and materials
            TryAutoAssignPrefabs();
            TryAutoAssignMaterials();
        }
        
        /// <summary>
        /// Attempt to auto-assign prefab references from known project paths
        /// ONLY uses prefabs from Assets/school/Prefabs/
        /// </summary>
        public void TryAutoAssignPrefabs()
        {
#if UNITY_EDITOR
            bool anyAssigned = false;

            // Define prefab paths - ONLY from Assets/school/Prefabs/
            var prefabPaths = new Dictionary<string, string[]>
            {
                { "DESK", new[] { "Assets/school/Prefabs/props/table1.prefab", "Assets/school/Prefabs/props/table2.prefab", "Assets/school/Prefabs/props/table3.prefab" } },
                { "CHAIR", new[] { "Assets/school/Prefabs/props/chair.prefab", "Assets/school/Prefabs/props/chair1.prefab" } },
                { "STUDENT", new[] { "Assets/Prefabs/Student.prefab" } },
                { "BOARD", new[] { "Assets/school/Prefabs/props/board.prefab", "Assets/school/Prefabs/props/board1.prefab", "Assets/school/Prefabs/props/board2.prefab" } },
                { "TEACHER", new[] { "Assets/Prefabs/TeacherPlayer.prefab" } },
                { "DOOR", new[] { "Assets/school/Prefabs/props/a door.prefab", "Assets/school/Prefabs/props/a door1.prefab" } },
                { "FLOOR", new[] { "Assets/school/Prefabs/road/floor.prefab", "Assets/school/Prefabs/road/floor1.prefab" } },
                { "WALL", new[] { "Assets/school/Prefabs/props/wall4 (2).prefab", "Assets/school/Prefabs/props/wall4.prefab", "Assets/school/Prefabs/props/wall2.prefab", "Assets/school/Prefabs/props/wall001.prefab" } },
                { "CEILING", new[] { "Assets/school/Prefabs/props/wall.prefab" } }
            };
            
            foreach (var mapping in assetMappings)
            {
                if (prefabPaths.ContainsKey(mapping.assetKey))
                {
                    foreach (var path in prefabPaths[mapping.assetKey])
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            // Always overwrite (even if already assigned)
                            bool wasNull = mapping.prefabReference == null;
                            mapping.prefabReference = prefab;
                            anyAssigned = true;
                            if (wasNull || mapping.prefabReference != prefab)
                            {
                                Debug.Log($"[AssetMapConfig] Auto-assigned {mapping.assetKey} to {path}");
                            }
                            break;
                        }
                    }
                }
            }
            
            if (anyAssigned)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
#endif
        }
        
        /// <summary>
        /// Attempt to auto-assign material references from known project paths
        /// </summary>
        public void TryAutoAssignMaterials()
        {
#if UNITY_EDITOR
            bool anyAssigned = false;
            
            // Define known material paths
            var materialPaths = new Dictionary<string, string[]>
            {
                { "Default", new[] { "Assets/school/material/Materials/1.mat", "Assets/Art/Materials/Default.mat" } },
                { "White", new[] { "Assets/school/material/Materials/1.mat", "Assets/Art/Materials/White.mat" } },
                { "Floor", new[] { "Assets/school/material/Materials/floor_color.mat", "Assets/Art/Materials/Floor.mat" } },
                { "Wall", new[] { "Assets/school/material/Materials/1.mat", "Assets/Art/Materials/Wall.mat" } }
            };
            
            foreach (var mapping in materialMappings)
            {
                if (mapping.materialReference == null && materialPaths.ContainsKey(mapping.materialKey))
                {
                    foreach (var path in materialPaths[mapping.materialKey])
                    {
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material != null)
                        {
                            mapping.materialReference = material;
                            anyAssigned = true;
                            Debug.Log($"[AssetMapConfig] Auto-assigned {mapping.materialKey} material to {path}");
                            break;
                        }
                    }
                }
            }
            
            if (anyAssigned)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
#endif
        }
        
        /// <summary>
        /// Create a default asset map with auto-assigned prefabs
        /// </summary>
        /// <param name="assetMapPath">Optional custom path for the asset map</param>
        public static AssetMapConfig CreateDefaultAssetMap(string assetMapPath = "Assets/Configs/DefaultAssetMap.asset")
        {
#if UNITY_EDITOR
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(assetMapPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Check if asset map already exists at this path
            AssetMapConfig existingAssetMap = AssetDatabase.LoadAssetAtPath<AssetMapConfig>(assetMapPath);
            if (existingAssetMap != null)
            {
                Debug.Log($"[AssetMapConfig] Using existing asset map at {assetMapPath}");
                existingAssetMap.TryAutoAssignPrefabs();
                existingAssetMap.TryAutoAssignMaterials();
                EditorUtility.SetDirty(existingAssetMap);
                AssetDatabase.SaveAssets();
                return existingAssetMap;
            }
            
            AssetMapConfig assetMap = ScriptableObject.CreateInstance<AssetMapConfig>();
            AssetDatabase.CreateAsset(assetMap, assetMapPath);
            assetMap.AddDefaultMappings();
            EditorUtility.SetDirty(assetMap);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[AssetMapConfig] Created default asset map at {assetMapPath}");
            return assetMap;
#else
            return null;
#endif
        }
    }
}
using UnityEngine;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using FunClass.Editor.Data;
using FunClass.Core;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Unified level importer that handles Auto, Manual, and Hybrid import modes
    /// Supports both legacy JSON format and enhanced JSON format
    /// </summary>
    public static class UnifiedLevelImporter
    {
        /// <summary>
        /// Main entry point: Import level from unified JSON schema
        /// </summary>
        public static void ImportLevelFromJSON(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[UnifiedLevelImporter] JSON file not found: {jsonPath}");
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("Import Level", "Loading JSON...", 0f);
                
                // 1. Load and parse unified schema
                string json = File.ReadAllText(jsonPath);
                Debug.Log($"[UnifiedLevelImporter] Original JSON length: {json.Length}");
                string cleanJson = StripJsonComments(json);
                Debug.Log($"[UnifiedLevelImporter] Cleaned JSON length: {cleanJson.Length}");
                if (cleanJson.Length < 500)
                {
                    Debug.Log($"[UnifiedLevelImporter] Cleaned JSON preview:\n{cleanJson}");
                }
                else
                {
                    Debug.Log($"[UnifiedLevelImporter] Cleaned JSON preview (first 500 chars):\n{cleanJson.Substring(0, 500)}...");
                }
                UnifiedLevelSchema schema = JsonUtility.FromJson<UnifiedLevelSchema>(cleanJson);
                
                if (schema == null)
                {
                    Debug.LogError($"[UnifiedLevelImporter] Failed to parse JSON: {jsonPath}");
                    EditorUtility.ClearProgressBar();
                    return;
                }
                
                // 2. Normalize schema (map legacy fields, set defaults, detect mode)
                schema.Normalize();
                
                Debug.Log($"[UnifiedLevelImporter] Loaded schema: {schema.levelId}, Mode: {schema.importMode}");
                
                // 3. Create or clear scene
                EditorUtility.DisplayProgressBar("Import Level", "Preparing scene...", 0.1f);
                SceneSetupManager.CreateOrClearScene(schema.levelId);
                
                // 4. Create essential managers and UI
                EditorUtility.DisplayProgressBar("Import Level", "Creating managers...", 0.15f);
                SceneHierarchyBuilder.CreateManagersGroup();
                SceneHierarchyBuilder.CreateUIGroup();
                
                // 5. Import based on detected mode
                List<EnhancedLevelImporter.StudentDeskPair> studentDeskPairs = null;
                
                switch (schema.importMode)
                {
                    case ImportMode.Auto:
                        studentDeskPairs = ImportAutoMode(schema);
                        break;
                        
                    case ImportMode.Manual:
                        studentDeskPairs = ImportManualMode(schema); // returns null for legacy importer
                        break;
                        
                    case ImportMode.Hybrid:
                        studentDeskPairs = ImportHybridMode(schema);
                        break;
                }
                
                // 6. Configure game systems (common for all modes)
                EditorUtility.DisplayProgressBar("Import Level", "Configuring game systems...", 0.85f);
                ConfigureGameSystems(schema, studentDeskPairs);
                
                // 7. Fix pink materials
                EditorUtility.DisplayProgressBar("Import Level", "Fixing materials...", 0.9f);
                MaterialFixer.ScanAndFixPinkMaterials();
                
                // 8. Save scene
                EditorUtility.DisplayProgressBar("Import Level", "Saving scene...", 0.95f);
                string scenePath = SceneSetupManager.SaveScene(schema.levelId);
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"[UnifiedLevelImporter] Level '{schema.levelId}' imported successfully in {schema.importMode} mode!");
                Debug.Log($"[UnifiedLevelImporter] Scene saved to: {scenePath}");
                
                EditorUtility.DisplayDialog("Success", 
                    $"Level '{schema.levelId}' imported successfully!\n" +
                    $"Mode: {schema.importMode}\n" +
                    $"Scene: {Path.GetFileName(scenePath)}", 
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[UnifiedLevelImporter] Error importing level: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to import level:\n{e.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Import in Auto mode (full auto-generation)
        /// </summary>
        private static List<EnhancedLevelImporter.StudentDeskPair> ImportAutoMode(UnifiedLevelSchema schema)
        {
            Debug.Log($"[UnifiedLevelImporter] Importing in Auto mode");
            
            // Convert UnifiedLevelSchema to EnhancedLevelSchema for compatibility
            EnhancedLevelSchema enhancedSchema = ConvertToEnhancedSchema(schema);
            
            // 1. Get or create asset map
            AssetMapConfig assetMap = GetOrCreateAssetMap(schema);
            if (assetMap == null)
            {
                Debug.LogError("[UnifiedLevelImporter] Failed to get asset map. Aborting.");
                return new List<EnhancedLevelImporter.StudentDeskPair>();
            }
            
            // 2. Generate desk grid
            EditorUtility.DisplayProgressBar("Import Level", "Generating desk grid...", 0.2f);
            List<EnhancedLevelImporter.DeskData> desks = DeskGridGenerator.GenerateDeskGrid(enhancedSchema);
            Debug.Log($"[UnifiedLevelImporter] Generated {desks.Count} desks for {schema.students} students");
            
            // 3. Instantiate desks and bind students
            EditorUtility.DisplayProgressBar("Import Level", "Instantiating desks and students...", 0.3f);
            List<EnhancedLevelImporter.StudentDeskPair> studentDeskPairs = StudentPlacementManager.PlaceStudentsAndDesks(
                enhancedSchema, desks, assetMap);
            
            // 4. Generate routes
            EditorUtility.DisplayProgressBar("Import Level", "Generating routes...", 0.5f);
            RouteGenerator.GenerateRoutes(enhancedSchema, studentDeskPairs);
            
            // 5. Setup board and environment
            EditorUtility.DisplayProgressBar("Import Level", "Setting up environment...", 0.7f);
            EnvironmentSetup.SetupEnvironment(enhancedSchema, assetMap);
            
            // 6. Create teacher
            EditorUtility.DisplayProgressBar("Import Level", "Creating teacher...", 0.75f);
            SceneHierarchyBuilder.CreateTeacherGroup();
            
            Debug.Log($"[UnifiedLevelImporter] Auto mode import completed: {studentDeskPairs.Count} students placed");
            
            return studentDeskPairs;
        }
        
        /// <summary>
        /// Import in Manual mode (legacy format)
        /// </summary>
        private static List<EnhancedLevelImporter.StudentDeskPair> ImportManualMode(UnifiedLevelSchema schema)
        {
            Debug.Log($"[UnifiedLevelImporter] Importing in Manual mode");
            
            // Convert UnifiedLevelSchema to legacy LevelDataSchema
            LevelDataSchema legacySchema = ConvertToLegacySchema(schema);
            
            if (legacySchema == null)
            {
                Debug.LogError("[UnifiedLevelImporter] Failed to convert to legacy schema");
                return null;
            }
            
            // Use legacy importer
            JSONLevelImporter.CreateLevelFromData(legacySchema);
            
            Debug.Log($"[UnifiedLevelImporter] Manual mode import completed via legacy importer");
            
            // Legacy importer creates its own configs, so return null
            return null;
        }
        
        /// <summary>
        /// Import in Hybrid mode (mix of auto-generation and manual overrides)
        /// </summary>
        private static List<EnhancedLevelImporter.StudentDeskPair> ImportHybridMode(UnifiedLevelSchema schema)
        {
            Debug.Log($"[UnifiedLevelImporter] Importing in Hybrid mode");
            
            // Convert to enhanced schema for auto-generation parts
            EnhancedLevelSchema enhancedSchema = ConvertToEnhancedSchema(schema);
            
            // 1. Get or create asset map
            AssetMapConfig assetMap = GetOrCreateAssetMap(schema);
            if (assetMap == null)
            {
                Debug.LogError("[UnifiedLevelImporter] Failed to get asset map. Aborting.");
                return new List<EnhancedLevelImporter.StudentDeskPair>();
            }
            
            // 2. Generate desk grid (auto)
            EditorUtility.DisplayProgressBar("Import Level", "Generating desk grid...", 0.2f);
            List<EnhancedLevelImporter.DeskData> desks = DeskGridGenerator.GenerateDeskGrid(enhancedSchema);
            Debug.Log($"[UnifiedLevelImporter] Generated {desks.Count} desks");
            
            // 3. Place students: use manual positions if provided, otherwise auto-place
            EditorUtility.DisplayProgressBar("Import Level", "Placing students...", 0.3f);
            List<EnhancedLevelImporter.StudentDeskPair> studentDeskPairs;
            
            if (schema.studentsManual != null && schema.studentsManual.Count > 0)
            {
                // Manual student placement
                studentDeskPairs = PlaceManualStudents(schema, desks, assetMap);
            }
            else
            {
                // Auto student placement
                studentDeskPairs = StudentPlacementManager.PlaceStudentsAndDesks(enhancedSchema, desks, assetMap);
            }
            
            // 4. Generate or use manual routes
            EditorUtility.DisplayProgressBar("Import Level", "Setting up routes...", 0.5f);
            if (schema.routesManual != null && schema.routesManual.Count > 0)
            {
                // Use manual routes
                CreateManualRoutes(schema);
            }
            else
            {
                // Generate auto routes
                RouteGenerator.GenerateRoutes(enhancedSchema, studentDeskPairs);
            }
            
            // 5. Setup environment: auto if no manual environment, otherwise manual
            EditorUtility.DisplayProgressBar("Import Level", "Setting up environment...", 0.7f);
            if (schema.environmentManual != null)
            {
                // Setup manual environment
                SetupManualEnvironment(schema, assetMap);
            }
            else
            {
                // Setup auto environment
                EnvironmentSetup.SetupEnvironment(enhancedSchema, assetMap);
            }
            
            // 6. Create teacher
            EditorUtility.DisplayProgressBar("Import Level", "Creating teacher...", 0.75f);
            SceneHierarchyBuilder.CreateTeacherGroup();
            
            // 7. Place manual prefabs if any
            if (schema.prefabs != null && schema.prefabs.Count > 0)
            {
                EditorUtility.DisplayProgressBar("Import Level", "Placing manual prefabs...", 0.77f);
                PrefabGenerator.CreatePrefabsFromData(schema.prefabs);
            }
            
            Debug.Log($"[UnifiedLevelImporter] Hybrid mode import completed: {studentDeskPairs.Count} students");
            
            return studentDeskPairs;
        }
        
        /// <summary>
        /// Convert UnifiedLevelSchema to legacy LevelDataSchema for manual mode
        /// </summary>
        private static LevelDataSchema ConvertToLegacySchema(UnifiedLevelSchema unified)
        {
            var legacy = new LevelDataSchema();
            
            // Map basic fields
            legacy.levelName = unified.levelId;
            legacy.difficulty = unified.difficulty;
            
            // Map game system settings
            legacy.goalSettings = unified.goalSettings;
            legacy.influenceScopeSettings = unified.influenceScopeSettings;
            // Note: studentInteractions is not in legacy schema, will be handled separately
            
            // Map manual data
            legacy.students = unified.studentsManual ?? new List<StudentData>();
            legacy.routes = unified.routesManual ?? new List<RouteData>();
            legacy.prefabs = unified.prefabs ?? new List<PrefabData>();
            legacy.interactableObjects = unified.interactableObjects ?? new List<InteractableObjectData>();
            legacy.messPrefabs = unified.messPrefabs ?? new List<MessPrefabData>();
            legacy.sequences = unified.sequences ?? new List<SequenceData>();
            legacy.environment = unified.environmentManual;
            
            Debug.Log($"[UnifiedLevelImporter] Converted to legacy schema: {legacy.students.Count} students, {legacy.routes.Count} routes");
            
            return legacy;
        }
        
        /// <summary>
        /// Convert UnifiedLevelSchema to EnhancedLevelSchema for auto mode
        /// </summary>
        private static EnhancedLevelSchema ConvertToEnhancedSchema(UnifiedLevelSchema unified)
        {
            var enhanced = new EnhancedLevelSchema();
            
            // Map basic fields
            enhanced.levelId = unified.levelId;
            enhanced.difficulty = unified.difficulty;
            
            // Map auto-generation fields
            enhanced.students = unified.students;
            enhanced.deskLayout = unified.deskLayout;
            enhanced.classroom = unified.classroom;
            enhanced.assetMapping = unified.assetMapping;
            enhanced.routeGeneration = unified.routeGeneration;
            enhanced.environment = unified.environment;
            enhanced.studentConfigs = unified.studentConfigs;
            
            // Map game system settings
            enhanced.goalSettings = unified.goalSettings;
            enhanced.influenceScopeSettings = unified.influenceScopeSettings;
            enhanced.studentInteractions = unified.studentInteractions;
            enhanced.routes = unified.routes;
            enhanced.expectedFlow = unified.expectedFlow;
            
            Debug.Log($"[UnifiedLevelImporter] Converted to enhanced schema: {enhanced.students} students, deskLayout={enhanced.deskLayout != null}");
            
            return enhanced;
        }
        
        /// <summary>
        /// Place students at manual positions (hybrid mode)
        /// </summary>
        private static List<EnhancedLevelImporter.StudentDeskPair> PlaceManualStudents(UnifiedLevelSchema schema, List<EnhancedLevelImporter.DeskData> desks, AssetMapConfig assetMap)
        {
            var studentDeskPairs = new List<EnhancedLevelImporter.StudentDeskPair>();
            
            // Get or create classroom group
            GameObject classroomGroup = SceneSetupManager.GetOrCreateClassroomGroup();
            GameObject desksGroup = SceneSetupManager.CreateOrFindGameObject("Desks", classroomGroup.transform);
            
            // For each manual student, find nearest desk or create at position
            for (int i = 0; i < schema.studentsManual.Count; i++)
            {
                var studentData = schema.studentsManual[i];
                
                EnhancedLevelImporter.DeskData desk = null;
                if (i < desks.Count)
                {
                    // Use pre-generated desk
                    desk = desks[i];
                }
                else
                {
                    // Create desk at student position (offset slightly)
                    desk = new EnhancedLevelImporter.DeskData
                    {
                        deskId = $"Desk_Manual_{i}",
                        position = studentData.position.ToVector3() + new Vector3(0, 0, -0.5f),
                        rotation = Quaternion.identity
                    };
                    // Instantiate desk prefab
                    GameObject deskPrefab = assetMap.GetPrefab("DESK");
                    if (deskPrefab != null)
                    {
                        // Use Object.Instantiate with position directly (simpler than PrefabUtility)
                        desk.deskObject = Object.Instantiate(deskPrefab, desk.position, desk.rotation);
                        desk.deskObject.name = $"Desk_Manual_{i}";
                        desk.deskObject.transform.SetParent(desksGroup.transform);
                        
                        // Add StudentInteractableObject for student interactions
                        AddDeskInteractableComponent(desk.deskObject);
                    }
                }
                
                // Instantiate student at manual position
                GameObject studentPrefab = assetMap.GetPrefab("STUDENT");
                if (studentPrefab == null)
                {
                    Debug.LogWarning($"[UnifiedLevelImporter] Student prefab not found in asset map");
                    continue;
                }
                
                Vector3 studentPosition = studentData.position.ToVector3();
                GameObject studentObj = Object.Instantiate(studentPrefab, studentPosition, Quaternion.identity);
                
                // Name student
                string studentName = studentData.studentName ?? $"Student_{i}";
                studentObj.name = studentName;
                
                // Parent to students group
                GameObject studentsGroup = GameObject.Find("=== STUDENTS ===");
                if (studentsGroup == null)
                {
                    studentsGroup = SceneHierarchyBuilder.CreateStudentsGroup(1, null, schema.levelId, null);
                }
                studentObj.transform.SetParent(studentsGroup.transform);
                
                // Add StudentAgent component if not present
                var studentAgent = studentObj.GetComponent<StudentAgent>();
                if (studentAgent == null)
                {
                    studentAgent = studentObj.AddComponent<StudentAgent>();
                }
                
                // Add NavMeshAgent for navigation
                var navMeshAgent = studentObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navMeshAgent == null)
                {
                    studentObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
                }
                
                // Create StudentDeskPair
                var pair = new EnhancedLevelImporter.StudentDeskPair
                {
                    studentId = $"student_{studentName.ToLower()}",
                    studentName = studentName,
                    desk = desk,
                    studentObject = studentObj
                };
                
                studentDeskPairs.Add(pair);
                
                Debug.Log($"[UnifiedLevelImporter] Placed manual student '{studentName}' at {studentPosition}");
            }
            
            return studentDeskPairs;
        }
        
        /// <summary>
        /// Create manual routes from schema.routesManual (hybrid mode)
        /// </summary>
        private static void CreateManualRoutes(UnifiedLevelSchema schema)
        {
            // Convert routesManual to legacy format and use legacy route creation
            var legacySchema = new LevelDataSchema
            {
                levelName = schema.levelId,
                routes = schema.routesManual
            };
            
            // Use legacy route creation logic
            var routes = JSONLevelImporterHelper.CreateRoutesFromData(legacySchema);
            
            Debug.Log($"[UnifiedLevelImporter] Created {routes.Count} manual routes");
        }
        
        /// <summary>
        /// Setup manual environment from schema.environmentManual (hybrid mode)
        /// </summary>
        private static void SetupManualEnvironment(UnifiedLevelSchema schema, AssetMapConfig assetMap)
        {
            Debug.Log($"[UnifiedLevelImporter] Setting up manual environment");
            
            // Create a temporary EnhancedLevelSchema using manual environment data
            EnhancedLevelSchema tempSchema = ConvertToEnhancedSchema(schema);
            
            // Override classroom dimensions with manual environment data if provided
            if (schema.environmentManual != null)
            {
                // Set classroom dimensions from environmentManual.classroomSize
                if (schema.environmentManual.classroomSize != null)
                {
                    Vector3 size = schema.environmentManual.classroomSize.ToVector3();
                    tempSchema.classroom.width = size.x;
                    tempSchema.classroom.depth = size.z;
                    tempSchema.classroom.height = size.y;
                    Debug.Log($"[UnifiedLevelImporter] Using manual classroom size: {size}");
                }
                
                // Set door position if provided
                if (schema.environmentManual.doorPosition != null)
                {
                    tempSchema.classroom.doorPosition = schema.environmentManual.doorPosition;
                    Debug.Log($"[UnifiedLevelImporter] Using manual door position: {schema.environmentManual.doorPosition.ToVector3()}");
                }
                
                // Set environment materials if provided
                if (tempSchema.environment == null)
                {
                    tempSchema.environment = new EnvironmentSettingsData();
                }
                
                if (!string.IsNullOrEmpty(schema.environmentManual.floorMaterial))
                {
                    tempSchema.environment.floorMaterial = schema.environmentManual.floorMaterial;
                    Debug.Log($"[UnifiedLevelImporter] Using manual floor material: {schema.environmentManual.floorMaterial}");
                }
                
                if (!string.IsNullOrEmpty(schema.environmentManual.wallMaterial))
                {
                    tempSchema.environment.wallMaterial = schema.environmentManual.wallMaterial;
                    Debug.Log($"[UnifiedLevelImporter] Using manual wall material: {schema.environmentManual.wallMaterial}");
                }
                
                // Note: windowPositions field is not currently used in EnvironmentSetup
                if (schema.environmentManual.windowPositions != null && schema.environmentManual.windowPositions.Count > 0)
                {
                    Debug.Log($"[UnifiedLevelImporter] Manual window positions provided (count: {schema.environmentManual.windowPositions.Count}) - currently not implemented");
                }
            }
            
            // Use the enhanced environment setup with the modified schema
            EnvironmentSetup.SetupEnvironment(tempSchema, assetMap);
            Debug.Log($"[UnifiedLevelImporter] Manual environment setup completed");
        }
        
        /// <summary>
        /// Get or create asset map configuration
        /// </summary>
        private static AssetMapConfig GetOrCreateAssetMap(UnifiedLevelSchema schema)
        {
            const string DEFAULT_ASSET_MAP_PATH = "Assets/Configs/DefaultAssetMap.asset";
            
            // Load or create default asset map as base
            AssetMapConfig defaultAssetMap = AssetDatabase.LoadAssetAtPath<AssetMapConfig>(DEFAULT_ASSET_MAP_PATH);
            
            if (defaultAssetMap == null)
            {
                Debug.LogWarning($"[UnifiedLevelImporter] Default asset map not found at {DEFAULT_ASSET_MAP_PATH}");
                defaultAssetMap = AssetMapConfig.CreateDefaultAssetMap(DEFAULT_ASSET_MAP_PATH);
            }
            
            if (defaultAssetMap == null)
            {
                Debug.LogError("[UnifiedLevelImporter] Failed to create asset map");
                return null;
            }
            
            // Check if schema has inline asset mapping
            if (schema.assetMapping == null || schema.assetMapping.prefabMapping == null)
            {
                // No inline mapping, return default asset map
                return defaultAssetMap;
            }
            
            Debug.Log("[UnifiedLevelImporter] Using inline asset mapping from JSON");
            
            // Create a temporary copy of the default asset map
            AssetMapConfig tempAssetMap = ScriptableObject.CreateInstance<AssetMapConfig>();
            
            // Copy asset mappings
            tempAssetMap.assetMappings = new List<AssetMapConfig.AssetMappingEntry>();
            foreach (var entry in defaultAssetMap.assetMappings)
            {
                tempAssetMap.assetMappings.Add(new AssetMapConfig.AssetMappingEntry
                {
                    assetKey = entry.assetKey,
                    prefabReference = entry.prefabReference,
                    description = entry.description
                });
            }
            
            // Copy material mappings
            tempAssetMap.materialMappings = new List<AssetMapConfig.MaterialMappingEntry>();
            foreach (var entry in defaultAssetMap.materialMappings)
            {
                tempAssetMap.materialMappings.Add(new AssetMapConfig.MaterialMappingEntry
                {
                    materialKey = entry.materialKey,
                    materialReference = entry.materialReference,
                    description = entry.description
                });
            }
            
            // Apply inline prefab mapping overrides
            foreach (var kvp in schema.assetMapping.prefabMapping)
            {
                string assetKey = kvp.Key;
                string prefabPath = kvp.Value;
                
                // Try to load prefab
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[UnifiedLevelImporter] Could not load prefab at path '{prefabPath}' for asset key '{assetKey}'");
                    continue;
                }
                
                // Find or create mapping entry
                var mappingEntry = tempAssetMap.assetMappings.Find(e => e.assetKey == assetKey);
                if (mappingEntry == null)
                {
                    mappingEntry = new AssetMapConfig.AssetMappingEntry
                    {
                        assetKey = assetKey,
                        description = $"Inline mapping from JSON"
                    };
                    tempAssetMap.assetMappings.Add(mappingEntry);
                }
                
                mappingEntry.prefabReference = prefab;
                Debug.Log($"[UnifiedLevelImporter] Applied inline prefab mapping: {assetKey} -> {prefabPath}");
            }
            
            // Apply inline material mapping overrides if present
            if (schema.assetMapping.materialOverrides != null)
            {
                foreach (var kvp in schema.assetMapping.materialOverrides)
                {
                    string materialKey = kvp.Key;
                    string materialPath = kvp.Value;
                    
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        Debug.LogWarning($"[UnifiedLevelImporter] Could not load material at path '{materialPath}' for material key '{materialKey}'");
                        continue;
                    }
                    
                    var mappingEntry = tempAssetMap.materialMappings.Find(e => e.materialKey == materialKey);
                    if (mappingEntry == null)
                    {
                        mappingEntry = new AssetMapConfig.MaterialMappingEntry
                        {
                            materialKey = materialKey,
                            description = $"Inline mapping from JSON"
                        };
                        tempAssetMap.materialMappings.Add(mappingEntry);
                    }
                    
                    mappingEntry.materialReference = material;
                    Debug.Log($"[UnifiedLevelImporter] Applied inline material mapping: {materialKey} -> {materialPath}");
                }
            }
            
            // Note: tempAssetMap is an in-memory ScriptableObject, not saved to disk
            Debug.Log($"[UnifiedLevelImporter] Created temporary asset map with {tempAssetMap.assetMappings.Count} asset mappings and {tempAssetMap.materialMappings.Count} material mappings");
            
            return tempAssetMap;
        }
        
        /// <summary>
        /// Configure game systems common to all import modes
        /// </summary>
        private static void ConfigureGameSystems(UnifiedLevelSchema schema, List<EnhancedLevelImporter.StudentDeskPair> studentDeskPairs)
        {
            Debug.Log($"[UnifiedLevelImporter] Configuring game systems for level '{schema.levelId}'");
            
            // 1. Create level folder structure
            EditorUtils.CreateLevelFolderStructure(schema.levelId);
            
            // 2. Create LevelGoalConfig from schema.goalSettings
            LevelGoalConfig goalConfig = CreateGoalConfigFromSchema(schema);
            
            // 3. Create LevelConfig and assign goalConfig
            LevelConfig levelConfig = CreateLevelConfigFromSchema(schema, goalConfig);
            
            // 4. Create InfluenceScopeConfig from schema.influenceScopeSettings
            InfluenceScopeConfig influenceScopeConfig = CreateInfluenceScopeConfigFromSchema(schema);
            if (influenceScopeConfig != null)
            {
                levelConfig.influenceScopeConfig = influenceScopeConfig;
                EditorUtility.SetDirty(levelConfig);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UnifiedLevelImporter] Assigned InfluenceScopeConfig to LevelConfig");
            }
            
            // 5. Create StudentConfigs (if studentDeskPairs provided)
            if (studentDeskPairs != null && studentDeskPairs.Count > 0)
            {
                List<StudentConfig> studentConfigs = CreateStudentConfigsFromPairs(schema, studentDeskPairs);
                if (studentConfigs != null && studentConfigs.Count > 0)
                {
                    // Log all configs before assignment
                    Debug.Log($"[UnifiedLevelImporter] Created {studentConfigs.Count} StudentConfigs:");
                    foreach (var config in studentConfigs)
                    {
                        Debug.Log($"[UnifiedLevelImporter]   - '{config.studentName}' (ID: {config.studentId}) at {config}");
                    }
                    
                    levelConfig.students = studentConfigs;
                    EditorUtility.SetDirty(levelConfig);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[UnifiedLevelImporter] Assigned {studentConfigs.Count} student configs to LevelConfig");
                    
                    // Verify assignment
                    if (levelConfig.students != null)
                    {
                        Debug.Log($"[UnifiedLevelImporter] LevelConfig.students now contains {levelConfig.students.Count} configs:");
                        for (int i = 0; i < levelConfig.students.Count; i++)
                        {
                            var cfg = levelConfig.students[i];
                            Debug.Log($"[UnifiedLevelImporter]   [{i}] '{cfg?.studentName}' (ID: {cfg?.studentId}) at {cfg}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[UnifiedLevelImporter] LevelConfig.students is NULL after assignment!");
                    }
                    
                    // Assign configs to StudentAgent components
                    AssignStudentConfigsToAgents(studentConfigs, studentDeskPairs);
                }
            }
            else
            {
                Debug.Log($"[UnifiedLevelImporter] No studentDeskPairs provided (Manual mode uses legacy configs)");
            }
            
            // 6. Assign routes to LevelConfig
            AssignRoutesToLevelConfig(schema, levelConfig);
            
            // 7. Assign LevelConfig to LevelLoader and ClassroomManager
            AssignLevelConfigToManagers(levelConfig);
            
            // 8. Create and configure StudentInteractionProcessor if interactions exist
            ConfigureStudentInteractionProcessor(schema);
            
            Debug.Log($"[UnifiedLevelImporter] Game systems configuration completed");
        }
        
        /// <summary>
        /// Create StudentConfigs from student desk pairs (adapted from EnhancedLevelImporter)
        /// </summary>
        private static List<StudentConfig> CreateStudentConfigsFromPairs(UnifiedLevelSchema schema, List<EnhancedLevelImporter.StudentDeskPair> studentDeskPairs)
        {
            if (studentDeskPairs == null || studentDeskPairs.Count == 0)
            {
                Debug.LogWarning("[UnifiedLevelImporter] No student desk pairs provided");
                return new List<StudentConfig>();
            }
            
            List<StudentConfig> studentConfigs = new List<StudentConfig>();
            
            for (int i = 0; i < studentDeskPairs.Count; i++)
            {
                var pair = studentDeskPairs[i];
                Debug.Log($"[UnifiedLevelImporter] Creating config for pair {i}: studentId='{pair.studentId}', studentName='{pair.studentName}', deskId='{pair.desk?.deskId}'");
                
                // Try to find matching enhanced student config
                EnhancedStudentData studentData = null;
                if (schema.studentConfigs != null)
                {
                    studentData = schema.studentConfigs.Find(s => s.studentId == pair.studentId || s.studentName == pair.studentName);
                }
                
                // Validate pair values
                if (string.IsNullOrEmpty(pair.studentName))
                {
                    Debug.LogError($"[UnifiedLevelImporter] studentName is null or empty for pair {i}! studentId='{pair.studentId}', desk='{pair.desk?.deskId}'");
                    continue;
                }
                if (string.IsNullOrEmpty(pair.studentId))
                {
                    Debug.LogError($"[UnifiedLevelImporter] studentId is null or empty for pair {i}! studentName='{pair.studentName}', desk='{pair.desk?.deskId}'");
                    continue;
                }
                
                string studentConfigPath = $"Assets/Configs/{schema.levelId}/Students/Student_{pair.studentName}.asset";
                Debug.Log($"[UnifiedLevelImporter] Creating StudentConfig at path: {studentConfigPath}");
                Debug.Log($"[UnifiedLevelImporter] Pair values: studentId='{pair.studentId}', studentName='{pair.studentName}'");
                
                // Always delete and recreate to ensure clean state
                if (AssetDatabase.LoadAssetAtPath<StudentConfig>(studentConfigPath) != null)
                {
                    Debug.Log($"[UnifiedLevelImporter] Deleting existing StudentConfig at {studentConfigPath}");
                    AssetDatabase.DeleteAsset(studentConfigPath);
                    AssetDatabase.SaveAssets();
                }
                
                StudentConfig config = EditorUtils.CreateScriptableObject<StudentConfig>(studentConfigPath);
                if (config == null)
                {
                    Debug.LogError($"[UnifiedLevelImporter] FAILED to create StudentConfig at {studentConfigPath}");
                    continue;
                }
                
                // Use SerializedObject to ensure proper serialization
                var serializedConfig = new SerializedObject(config);
                var studentIdProp = serializedConfig.FindProperty("studentId");
                var studentNameProp = serializedConfig.FindProperty("studentName");
                var initialStateProp = serializedConfig.FindProperty("initialState");
                
                if (studentIdProp == null) Debug.LogError($"[UnifiedLevelImporter] studentId property not found on StudentConfig!");
                if (studentNameProp == null) Debug.LogError($"[UnifiedLevelImporter] studentName property not found on StudentConfig!");
                if (initialStateProp == null) Debug.LogError($"[UnifiedLevelImporter] initialState property not found on StudentConfig!");
                
                if (studentIdProp != null) studentIdProp.stringValue = pair.studentId;
                if (studentNameProp != null) studentNameProp.stringValue = pair.studentName;
                if (initialStateProp != null) initialStateProp.enumValueIndex = (int)StudentState.Calm;
                serializedConfig.ApplyModifiedProperties();
                
                // Verify the values were set
                serializedConfig.Update();
                string verifiedStudentId = studentIdProp != null ? studentIdProp.stringValue : "PROP_NOT_FOUND";
                string verifiedStudentName = studentNameProp != null ? studentNameProp.stringValue : "PROP_NOT_FOUND";
                Debug.Log($"[UnifiedLevelImporter] Set config fields via SerializedObject: studentId='{pair.studentId}'->'{verifiedStudentId}', studentName='{pair.studentName}'->'{verifiedStudentName}', path='{studentConfigPath}'");
                
                if (studentData != null && studentData.personality != null)
                {
                    serializedConfig.FindProperty("patience").floatValue = studentData.personality.patience;
                    serializedConfig.FindProperty("attentionSpan").floatValue = studentData.personality.attentionSpan;
                    serializedConfig.FindProperty("impulsiveness").floatValue = studentData.personality.impulsiveness;
                    serializedConfig.FindProperty("influenceSusceptibility").floatValue = studentData.personality.influenceSusceptibility;
                    serializedConfig.FindProperty("influenceResistance").floatValue = studentData.personality.influenceResistance;
                    serializedConfig.FindProperty("panicThreshold").floatValue = studentData.personality.panicThreshold;
                }
                else
                {
                    // Generate personality based on difficulty
                    serializedConfig.FindProperty("patience").floatValue = Random.Range(0.4f, 0.7f);
                    serializedConfig.FindProperty("attentionSpan").floatValue = Random.Range(0.3f, 0.7f);
                    serializedConfig.FindProperty("impulsiveness").floatValue = Random.Range(0.3f, 0.7f);
                    serializedConfig.FindProperty("influenceSusceptibility").floatValue = Random.Range(0.4f, 0.7f);
                    serializedConfig.FindProperty("influenceResistance").floatValue = Random.Range(0.3f, 0.6f);
                    serializedConfig.FindProperty("panicThreshold").floatValue = Random.Range(0.4f, 0.8f);
                }
                serializedConfig.ApplyModifiedProperties();
                
                if (studentData != null && studentData.behaviors != null)
                {
                    serializedConfig.FindProperty("canFidget").boolValue = studentData.behaviors.canFidget;
                    serializedConfig.FindProperty("canLookAround").boolValue = studentData.behaviors.canLookAround;
                    serializedConfig.FindProperty("canStandUp").boolValue = studentData.behaviors.canStandUp;
                    serializedConfig.FindProperty("canMoveAround").boolValue = studentData.behaviors.canMoveAround;
                    serializedConfig.FindProperty("canDropItems").boolValue = studentData.behaviors.canDropItems;
                    serializedConfig.FindProperty("canKnockOverObjects").boolValue = studentData.behaviors.canKnockOverObjects;
                    serializedConfig.FindProperty("canMakeNoiseWithObjects").boolValue = studentData.behaviors.canMakeNoiseWithObjects;
                    serializedConfig.FindProperty("canThrowObjects").boolValue = studentData.behaviors.canThrowObjects;
                    serializedConfig.FindProperty("canTouchObjects").boolValue = studentData.behaviors.canTouchObjects;
                    serializedConfig.FindProperty("minIdleTime").floatValue = studentData.behaviors.minIdleTime;
                    serializedConfig.FindProperty("maxIdleTime").floatValue = studentData.behaviors.maxIdleTime;
                    
                    // Calculate interactionRange - auto from desk spacing if not specified
                    float interactionRange = studentData.behaviors.interactionRange;
                    if (interactionRange <= 0f && schema.deskLayout != null)
                    {
                        // Auto calculate: max(spacingX, spacingZ) + 1.0 buffer
                        float autoRange = Mathf.Max(schema.deskLayout.spacingX, schema.deskLayout.spacingZ) + 1.0f;
                        interactionRange = Mathf.Clamp(autoRange, 3.0f, 8.0f);
                        Debug.Log($"[UnifiedLevelImporter] Auto-calculated interactionRange for {pair.studentName}: {interactionRange} (spacingX={schema.deskLayout.spacingX}, spacingZ={schema.deskLayout.spacingZ})");
                    }
                    else if (interactionRange <= 0f)
                    {
                        interactionRange = 4.0f;  // Default fallback
                    }
                    serializedConfig.FindProperty("interactionRange").floatValue = interactionRange;
                    
                    // State-based interaction chances
                    serializedConfig.FindProperty("calmInteractionChance").floatValue = studentData.behaviors.calmInteractionChance;
                    serializedConfig.FindProperty("distractedInteractionChance").floatValue = studentData.behaviors.distractedInteractionChance;
                    serializedConfig.FindProperty("actingOutInteractionChance").floatValue = studentData.behaviors.actingOutInteractionChance;
                    serializedConfig.FindProperty("criticalInteractionChance").floatValue = studentData.behaviors.criticalInteractionChance;
                }
                else
                {
                    // Default behaviors
                    serializedConfig.FindProperty("canFidget").boolValue = true;
                    serializedConfig.FindProperty("canLookAround").boolValue = true;
                    serializedConfig.FindProperty("canStandUp").boolValue = Random.value < 0.5f;
                    serializedConfig.FindProperty("canMoveAround").boolValue = Random.value < 0.4f;
                    serializedConfig.FindProperty("canDropItems").boolValue = Random.value < 0.3f;
                    serializedConfig.FindProperty("canKnockOverObjects").boolValue = Random.value < 0.3f;
                    serializedConfig.FindProperty("canMakeNoiseWithObjects").boolValue = true;
                    serializedConfig.FindProperty("canThrowObjects").boolValue = Random.value < 0.2f;
                    serializedConfig.FindProperty("canTouchObjects").boolValue = true;
                    serializedConfig.FindProperty("minIdleTime").floatValue = 2f;
                    serializedConfig.FindProperty("maxIdleTime").floatValue = 8f;
                    
                    // Auto-calculate interactionRange from deskLayout
                    float interactionRange = 4.0f;  // Default
                    if (schema.deskLayout != null)
                    {
                        float autoRange = Mathf.Max(schema.deskLayout.spacingX, schema.deskLayout.spacingZ) + 1.0f;
                        interactionRange = Mathf.Clamp(autoRange, 3.0f, 8.0f);
                    }
                    serializedConfig.FindProperty("interactionRange").floatValue = interactionRange;
                    
                    // Default state-based chances
                    serializedConfig.FindProperty("calmInteractionChance").floatValue = 0.1f;
                    serializedConfig.FindProperty("distractedInteractionChance").floatValue = 0.3f;
                    serializedConfig.FindProperty("actingOutInteractionChance").floatValue = 0.6f;
                    serializedConfig.FindProperty("criticalInteractionChance").floatValue = 0.9f;
                }
                serializedConfig.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(config);
                studentConfigs.Add(config);
                
                // Force refresh and verify
                AssetDatabase.Refresh();
                var verifiedConfig = AssetDatabase.LoadAssetAtPath<StudentConfig>(studentConfigPath);
                if (verifiedConfig != null)
                {
                    Debug.Log($"[UnifiedLevelImporter] Created StudentConfig: studentId='{verifiedConfig.studentId}', studentName='{verifiedConfig.studentName}', asset='{studentConfigPath}' (loaded from disk)");
                    if (string.IsNullOrEmpty(verifiedConfig.studentId) || string.IsNullOrEmpty(verifiedConfig.studentName))
                    {
                        Debug.LogError($"[UnifiedLevelImporter] WARNING: Config fields are empty after creation! studentId='{verifiedConfig.studentId}', studentName='{verifiedConfig.studentName}'");
                    }
                }
                else
                {
                    Debug.LogError($"[UnifiedLevelImporter] Failed to load created config from disk: {studentConfigPath}");
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"[UnifiedLevelImporter] Created {studentConfigs.Count} StudentConfig assets");
            Debug.Log($"[UnifiedLevelImporter] List of created configs:");
            for (int i = 0; i < studentConfigs.Count; i++)
            {
                var cfg = studentConfigs[i];
                Debug.Log($"[UnifiedLevelImporter]   [{i}] studentId='{cfg.studentId}', studentName='{cfg.studentName}', asset='{cfg.name}'");
            }
            
            return studentConfigs;
        }
        
        /// <summary>
        /// Assign StudentConfigs to StudentAgent components in scene
        /// </summary>
        private static void AssignStudentConfigsToAgents(List<StudentConfig> studentConfigs, List<EnhancedLevelImporter.StudentDeskPair> studentDeskPairs)
        {
            if (studentConfigs == null || studentDeskPairs == null)
            {
                Debug.LogWarning("[UnifiedLevelImporter] Cannot assign configs to agents - null parameters");
                return;
            }
            
            Debug.Log($"[UnifiedLevelImporter] Assigning {studentConfigs.Count} configs to {studentDeskPairs.Count} students");
            Debug.Log($"[UnifiedLevelImporter] Available configs (total {studentConfigs.Count}):");
            for (int i = 0; i < studentConfigs.Count; i++)
            {
                var config = studentConfigs[i];
                Debug.Log($"[UnifiedLevelImporter]   [{i}] '{config.studentName}' (ID: {config.studentId}) at {config}");
            }
            Debug.Log($"[UnifiedLevelImporter] Student desk pairs (total {studentDeskPairs.Count}):");
            for (int i = 0; i < studentDeskPairs.Count; i++)
            {
                var pair = studentDeskPairs[i];
                Debug.Log($"[UnifiedLevelImporter]   [{i}] '{pair.studentName}' (ID: {pair.studentId}) desk: {pair.desk?.deskId}, studentObj: {pair.studentObject?.name}");
            }
            
            int assignedCount = 0;
            int missingConfigCount = 0;
            int missingAgentCount = 0;
            
            foreach (var pair in studentDeskPairs)
            {
                if (pair.studentObject == null)
                {
                    Debug.LogWarning($"[UnifiedLevelImporter] Student object is null for {pair.studentName}");
                    continue;
                }
                
                var studentAgent = pair.studentObject.GetComponent<StudentAgent>();
                if (studentAgent == null)
                {
                    // Try to find in children
                    studentAgent = pair.studentObject.GetComponentInChildren<StudentAgent>();
                }
                
                if (studentAgent == null)
                {
                    Debug.LogWarning($"[UnifiedLevelImporter] StudentAgent component not found on {pair.studentObject.name}");
                    missingAgentCount++;
                    continue;
                }
                
                // Find matching config with detailed logging
                StudentConfig config = null;
                Debug.Log($"[UnifiedLevelImporter] Searching for config matching student '{pair.studentName}' (ID: {pair.studentId})");
                for (int ci = 0; ci < studentConfigs.Count; ci++)
                {
                    var testConfig = studentConfigs[ci];
                    bool idMatch = testConfig.studentId == pair.studentId;
                    bool nameMatch = testConfig.studentName == pair.studentName;
                    Debug.Log($"[UnifiedLevelImporter]   Checking config [{ci}]: '{testConfig.studentName}' (ID: {testConfig.studentId}) -> idMatch={idMatch}, nameMatch={nameMatch}");
                    if (idMatch || nameMatch)
                    {
                        config = testConfig;
                        Debug.Log($"[UnifiedLevelImporter]   MATCH found at index {ci}");
                        break;
                    }
                }
                
                if (config == null)
                {
                    Debug.LogWarning($"[UnifiedLevelImporter] No matching StudentConfig found for {pair.studentName} (ID: {pair.studentId})");
                    Debug.Log($"[UnifiedLevelImporter] Looking for config with studentName='{pair.studentName}' or studentId='{pair.studentId}'");
                    missingConfigCount++;
                    continue;
                }
                
                Debug.Log($"[UnifiedLevelImporter] Found config '{config.studentName}' for student '{pair.studentName}'");
                
                // Check if config already assigned
                if (studentAgent.Config != null)
                {
                    Debug.Log($"[UnifiedLevelImporter] StudentAgent already has config '{studentAgent.Config.studentName}', replacing with '{config.studentName}'");
                }
                
                // Use SerializedObject to set serialized field (proper scene serialization)
                try
                {
                    var serializedObject = new SerializedObject(studentAgent);
                    var configProperty = serializedObject.FindProperty("config");
                    if (configProperty != null)
                    {
                        configProperty.objectReferenceValue = config;
                        serializedObject.ApplyModifiedProperties();
                        
                        // Also call Initialize to ensure runtime state is set
                        studentAgent.Initialize(config);
                        
                        assignedCount++;
                        Debug.Log($"[UnifiedLevelImporter] Successfully assigned StudentConfig '{config.studentName}' to {pair.studentObject.name} using SerializedObject + Initialize()");
                    }
                    else
                    {
                        Debug.LogError($"[UnifiedLevelImporter] Could not find 'config' property on StudentAgent");
                        
                        // Fallback to public Initialize method
                        try
                        {
                            studentAgent.Initialize(config);
                            assignedCount++;
                            Debug.Log($"[UnifiedLevelImporter] Assigned StudentConfig '{config.studentName}' to {pair.studentObject.name} using Initialize() (no serialization)");
                        }
                        catch (System.Exception e2)
                        {
                            Debug.LogError($"[UnifiedLevelImporter] Failed to assign config via Initialize(): {e2.Message}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[UnifiedLevelImporter] Error assigning config via SerializedObject: {e.Message}");
                    
                    // Last resort: reflection
                    try
                    {
                        var configField = typeof(StudentAgent).GetField("config", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (configField != null)
                        {
                            configField.SetValue(studentAgent, config);
                            assignedCount++;
                            Debug.Log($"[UnifiedLevelImporter] Assigned StudentConfig '{config.studentName}' to {pair.studentObject.name} via reflection");
                        }
                    }
                    catch (System.Exception e2)
                    {
                        Debug.LogError($"[UnifiedLevelImporter] Reflection also failed: {e2.Message}");
                    }
                }
                
                // Verify assignment
                if (studentAgent.Config != null)
                {
                    Debug.Log($"[UnifiedLevelImporter] Verified: {pair.studentObject.name} now has config '{studentAgent.Config.studentName}'");
                }
                else
                {
                    Debug.LogError($"[UnifiedLevelImporter] FAILED: {pair.studentObject.name} still has null config after assignment!");
                }
            }
            
            Debug.Log($"[UnifiedLevelImporter] Assignment summary: {assignedCount}/{studentDeskPairs.Count} assigned, {missingConfigCount} missing configs, {missingAgentCount} missing agents");
        }
        
        /// <summary>
        /// Create LevelGoalConfig from schema.goalSettings (or defaults)
        /// </summary>
        private static LevelGoalConfig CreateGoalConfigFromSchema(UnifiedLevelSchema schema)
        {
            string goalConfigPath = $"Assets/Configs/{schema.levelId}/{schema.levelId}_Goal.asset";
            
            LevelGoalConfig goalConfig = EditorUtils.CreateScriptableObject<LevelGoalConfig>(goalConfigPath);
            
            if (schema.goalSettings != null)
            {
                // Map fields from LevelGoalData to LevelGoalConfig
                goalConfig.maxDisruptionThreshold = schema.goalSettings.maxDisruptionThreshold;
                goalConfig.catastrophicDisruptionLevel = schema.goalSettings.catastrophicDisruptionLevel;
                goalConfig.maxAllowedCriticalStudents = schema.goalSettings.maxAllowedCriticalStudents;
                goalConfig.catastrophicCriticalStudents = schema.goalSettings.catastrophicCriticalStudents;
                goalConfig.maxAllowedOutsideStudents = schema.goalSettings.maxAllowedOutsideStudents;
                goalConfig.catastrophicOutsideStudents = schema.goalSettings.catastrophicOutsideStudents;
                goalConfig.maxOutsideTimePerStudent = schema.goalSettings.maxOutsideTimePerStudent;
                goalConfig.maxAllowedOutsideGracePeriod = schema.goalSettings.maxAllowedOutsideGracePeriod;
                goalConfig.timeLimitSeconds = schema.goalSettings.timeLimitSeconds;
                goalConfig.requiredResolvedProblems = schema.goalSettings.requiredResolvedProblems;
                goalConfig.requiredCalmDowns = schema.goalSettings.requiredCalmDowns;
                
                // Disruption Timeout
                goalConfig.enableDisruptionTimeout = schema.goalSettings.enableDisruptionTimeout;
                goalConfig.disruptionTimeoutThreshold = schema.goalSettings.disruptionTimeoutThreshold;
                goalConfig.disruptionTimeoutSeconds = schema.goalSettings.disruptionTimeoutSeconds;
                goalConfig.disruptionTimeoutWarningSeconds = schema.goalSettings.disruptionTimeoutWarningSeconds;
                
                goalConfig.oneStarScore = schema.goalSettings.oneStarScore;
                goalConfig.twoStarScore = schema.goalSettings.twoStarScore;
                goalConfig.threeStarScore = schema.goalSettings.threeStarScore;
                
                Debug.Log($"[UnifiedLevelImporter] Created LevelGoalConfig from JSON settings");
            }
            else
            {
                // Use defaults
                Debug.Log($"[UnifiedLevelImporter] Created default LevelGoalConfig (no goalSettings in JSON)");
            }
            
            EditorUtility.SetDirty(goalConfig);
            AssetDatabase.SaveAssets();
            
            return goalConfig;
        }
        
        /// <summary>
        /// Create LevelConfig and assign LevelGoalConfig
        /// </summary>
        private static LevelConfig CreateLevelConfigFromSchema(UnifiedLevelSchema schema, LevelGoalConfig goalConfig)
        {
            string levelConfigPath = $"Assets/Configs/{schema.levelId}/{schema.levelId}_Config.asset";
            
            LevelConfig levelConfig = EditorUtils.CreateScriptableObject<LevelConfig>(levelConfigPath);
            
            levelConfig.levelId = schema.levelId;
            levelConfig.levelGoal = goalConfig;
            
            // Load student interactions as RuntimeStudentInteraction list
            if (schema.studentInteractions != null && schema.studentInteractions.Count > 0)
            {
                // Clear existing interactions first to avoid duplicates
                levelConfig.studentInteractions.Clear();
                
                foreach (var interactionData in schema.studentInteractions)
                {
                    RuntimeStudentInteraction runtimeInteraction = new RuntimeStudentInteraction
                    {
                        id = interactionData.id ?? $"{interactionData.sourceStudentId}_{interactionData.targetStudentId}_{interactionData.eventType}",
                        sourceStudentId = interactionData.sourceStudentId,
                        targetStudentId = interactionData.targetStudentId,
                        eventType = interactionData.eventType,
                        triggerCondition = interactionData.triggerCondition,
                        triggerValue = interactionData.triggerValue,
                        probability = interactionData.probability,
                        oneTimeOnly = interactionData.oneTimeOnly,
                        description = interactionData.description
                    };
                    levelConfig.studentInteractions.Add(runtimeInteraction);
                }
                Debug.Log($"[UnifiedLevelImporter] Added {levelConfig.studentInteractions.Count} student interactions to LevelConfig");
            }
            else
            {
                Debug.Log($"[UnifiedLevelImporter] No student interactions in schema (null: {schema.studentInteractions == null}, count: {schema.studentInteractions?.Count ?? 0})");
            }
            
            EditorUtility.SetDirty(levelConfig);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[UnifiedLevelImporter] Created LevelConfig at {levelConfigPath}");
            
            return levelConfig;
        }
        
        /// <summary>
        /// Create InfluenceScopeConfig from schema.influenceScopeSettings
        /// </summary>
        private static InfluenceScopeConfig CreateInfluenceScopeConfigFromSchema(UnifiedLevelSchema schema)
        {
            if (schema.influenceScopeSettings == null)
            {
                Debug.Log("[UnifiedLevelImporter] No influenceScopeSettings in JSON - skipping InfluenceScopeConfig creation");
                return null;
            }
            
            string configPath = $"Assets/Configs/{schema.levelId}/{schema.levelId}_InfluenceScope.asset";
            InfluenceScopeConfig config = EditorUtils.CreateScriptableObject<InfluenceScopeConfig>(configPath);
            
            // Load data from schema
            config.description = schema.influenceScopeSettings.description;
            config.disruptionPenaltyPerUnresolvedSource = schema.influenceScopeSettings.disruptionPenaltyPerUnresolvedSource;
            
            config.eventScopes.Clear();
            
            if (schema.influenceScopeSettings.eventScopes != null)
            {
                foreach (var eventScope in schema.influenceScopeSettings.eventScopes)
                {
                    config.eventScopes.Add(new InfluenceScopeConfig.EventScopeEntry
                    {
                        eventTypeName = eventScope.eventTypeName,
                        scope = eventScope.scope,
                        baseSeverity = eventScope.baseSeverity,
                        description = eventScope.description
                    });
                }
            }
            
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[UnifiedLevelImporter] Created InfluenceScopeConfig at {configPath}");
            
            return config;
        }
        

        
        /// <summary>
        /// Assign routes to LevelConfig
        /// </summary>
        private static void AssignRoutesToLevelConfig(UnifiedLevelSchema schema, LevelConfig levelConfig)
        {
            Debug.Log($"[UnifiedLevelImporter] Assigning routes to LevelConfig for {schema.levelId}");
            
            // 1. First check for manual routes (routesManual) - highest priority
            if (schema.routesManual != null && schema.routesManual.Count > 0)
            {
                AssignManualRoutes(schema, levelConfig);
                return;
            }
            
            // 2. Check for enhanced schema routes (schema.routes) - second priority
            if (schema.routes != null && schema.routes.Count > 0)
            {
                AssignEnhancedRoutes(schema, levelConfig);
                return;
            }
            
            // 3. Auto-generated routes - look in AutoGenerated/Routes folder
            AssignAutoGeneratedRoutes(schema, levelConfig);
        }
        
        /// <summary>
        /// Assign manual routes from schema.routesManual
        /// </summary>
        private static void AssignManualRoutes(UnifiedLevelSchema schema, LevelConfig levelConfig)
        {
            Debug.Log($"[UnifiedLevelImporter] Looking for manual routes for level '{schema.levelId}'");
            
            // Routes should already be created by legacy importer in Manual mode
            // Look for route assets in the level's config folder
            string levelRoutesFolder = $"Assets/Configs/{schema.levelId}/Routes";
            
            if (!AssetDatabase.IsValidFolder(levelRoutesFolder))
            {
                Debug.LogWarning($"[UnifiedLevelImporter] Manual routes folder not found: {levelRoutesFolder}");
                return;
            }
            
            string[] routeGuids = AssetDatabase.FindAssets("t:StudentRoute", new[] { levelRoutesFolder });
            if (routeGuids.Length == 0)
            {
                Debug.LogWarning($"[UnifiedLevelImporter] No StudentRoute assets found in {levelRoutesFolder}");
                return;
            }
            
            Debug.Log($"[UnifiedLevelImporter] Found {routeGuids.Length} manual route assets");
            
            // Separate escape and return routes by name pattern
            StudentRoute escapeRoute = null;
            StudentRoute returnRoute = null;
            List<StudentRoute> allRoutes = new List<StudentRoute>();
            
            foreach (string guid in routeGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StudentRoute route = AssetDatabase.LoadAssetAtPath<StudentRoute>(path);
                if (route == null) continue;
                
                allRoutes.Add(route);
                
                string lowerName = route.routeName.ToLower();
                if (lowerName.Contains("escape"))
                {
                    if (escapeRoute == null)
                    {
                        escapeRoute = route;
                        Debug.Log($"[UnifiedLevelImporter] Selected manual escape route: {route.routeName}");
                    }
                }
                else if (lowerName.Contains("return"))
                {
                    if (returnRoute == null)
                    {
                        returnRoute = route;
                        Debug.Log($"[UnifiedLevelImporter] Selected manual return route: {route.routeName}");
                    }
                }
            }
            
            // Create generic route copies in level folder for LevelLoader compatibility
            StudentRoute genericEscapeRoute = null;
            StudentRoute genericReturnRoute = null;
            
            if (escapeRoute != null)
            {
                genericEscapeRoute = CreateGenericRouteCopy(escapeRoute, "EscapeRoute", levelRoutesFolder);
                if (genericEscapeRoute != null)
                {
                    Debug.Log($"[UnifiedLevelImporter] Created generic escape route at {AssetDatabase.GetAssetPath(genericEscapeRoute)}");
                }
            }
            
            if (returnRoute != null)
            {
                genericReturnRoute = CreateGenericRouteCopy(returnRoute, "ReturnRoute", levelRoutesFolder);
                if (genericReturnRoute != null)
                {
                    Debug.Log($"[UnifiedLevelImporter] Created generic return route at {AssetDatabase.GetAssetPath(genericReturnRoute)}");
                }
            }
            
            // Assign generic routes to LevelConfig (for LevelLoader compatibility)
            if (genericEscapeRoute != null)
            {
                levelConfig.escapeRoute = genericEscapeRoute;
                Debug.Log($"[UnifiedLevelImporter] Assigned generic escape route '{genericEscapeRoute.routeName}' to LevelConfig");
            }
            else if (escapeRoute != null)
            {
                levelConfig.escapeRoute = escapeRoute;
                Debug.Log($"[UnifiedLevelImporter] Assigned manual escape route '{escapeRoute.routeName}' to LevelConfig (fallback)");
            }
            
            if (genericReturnRoute != null)
            {
                levelConfig.returnRoute = genericReturnRoute;
                Debug.Log($"[UnifiedLevelImporter] Assigned generic return route '{genericReturnRoute.routeName}' to LevelConfig");
            }
            else if (returnRoute != null)
            {
                levelConfig.returnRoute = returnRoute;
                Debug.Log($"[UnifiedLevelImporter] Assigned manual return route '{returnRoute.routeName}' to LevelConfig (fallback)");
            }
            
            // Add all routes to availableRoutes
            if (allRoutes.Count > 0)
            {
                levelConfig.availableRoutes = allRoutes;
                Debug.Log($"[UnifiedLevelImporter] Added {allRoutes.Count} routes to availableRoutes");
            }
            
            EditorUtility.SetDirty(levelConfig);
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Assign enhanced schema routes from schema.routes
        /// </summary>
        private static void AssignEnhancedRoutes(UnifiedLevelSchema schema, LevelConfig levelConfig)
        {
            Debug.Log($"[UnifiedLevelImporter] Assigning enhanced routes from schema.routes");
            
            if (schema.routes == null || schema.routes.Count == 0)
            {
                Debug.Log($"[UnifiedLevelImporter] No enhanced routes defined in schema.routes, falling back to auto-generated routes");
                AssignAutoGeneratedRoutes(schema, levelConfig);
                return;
            }
            
            // Treat enhanced routes as manual routes for creation
            // Create a temporary schema with routesManual set to schema.routes
            var tempSchema = new UnifiedLevelSchema
            {
                levelId = schema.levelId,
                routesManual = schema.routes
            };
            
            // Create route assets and scene waypoints using existing manual route creation logic
            CreateManualRoutes(tempSchema);
            
            // Now assign the created routes to LevelConfig
            // The routes should be saved in the level's config folder
            AssignManualRoutes(tempSchema, levelConfig);
            
            Debug.Log($"[UnifiedLevelImporter] Enhanced routes assignment completed for {schema.routes.Count} routes");
        }
        
        /// <summary>
        /// Assign auto-generated routes from AutoGenerated/Routes folder
        /// </summary>
        private static void AssignAutoGeneratedRoutes(UnifiedLevelSchema schema, LevelConfig levelConfig)
        {
            Debug.Log($"[UnifiedLevelImporter] Looking for auto-generated routes");
            
            string autoRoutesFolder = "Assets/Configs/AutoGenerated/Routes";
            string levelRoutesFolder = $"Assets/Configs/{schema.levelId}/Routes";
            
            if (!AssetDatabase.IsValidFolder(autoRoutesFolder))
            {
                Debug.LogWarning($"[UnifiedLevelImporter] Auto-generated routes folder not found: {autoRoutesFolder}");
                return;
            }
            
            // Ensure level routes folder exists
            EditorUtils.CreateFolderIfNotExists(levelRoutesFolder);
            
            string[] routeGuids = AssetDatabase.FindAssets("t:StudentRoute", new[] { autoRoutesFolder });
            if (routeGuids.Length == 0)
            {
                Debug.LogWarning($"[UnifiedLevelImporter] No StudentRoute assets found in {autoRoutesFolder}");
                return;
            }
            
            Debug.Log($"[UnifiedLevelImporter] Found {routeGuids.Length} auto-generated route assets");
            
            // Separate escape and return routes by name pattern
            StudentRoute escapeRoute = null;
            StudentRoute returnRoute = null;
            List<StudentRoute> allRoutes = new List<StudentRoute>();
            
            foreach (string guid in routeGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StudentRoute route = AssetDatabase.LoadAssetAtPath<StudentRoute>(path);
                if (route == null) continue;
                
                allRoutes.Add(route);
                
                if (route.routeName.Contains("EscapeRoute"))
                {
                    if (escapeRoute == null)
                    {
                        escapeRoute = route;
                        Debug.Log($"[UnifiedLevelImporter] Selected auto-generated escape route: {route.routeName}");
                    }
                }
                else if (route.routeName.Contains("ReturnRoute"))
                {
                    if (returnRoute == null)
                    {
                        returnRoute = route;
                        Debug.Log($"[UnifiedLevelImporter] Selected auto-generated return route: {route.routeName}");
                    }
                }
            }
            
            // Create generic route copies in level folder for LevelLoader compatibility
            StudentRoute genericEscapeRoute = null;
            StudentRoute genericReturnRoute = null;
            
            if (escapeRoute != null)
            {
                genericEscapeRoute = CreateGenericRouteCopy(escapeRoute, "EscapeRoute", levelRoutesFolder);
                if (genericEscapeRoute != null)
                {
                    Debug.Log($"[UnifiedLevelImporter] Created generic escape route at {AssetDatabase.GetAssetPath(genericEscapeRoute)}");
                }
            }
            
            if (returnRoute != null)
            {
                genericReturnRoute = CreateGenericRouteCopy(returnRoute, "ReturnRoute", levelRoutesFolder);
                if (genericReturnRoute != null)
                {
                    Debug.Log($"[UnifiedLevelImporter] Created generic return route at {AssetDatabase.GetAssetPath(genericReturnRoute)}");
                }
            }
            
            // Assign generic routes to LevelConfig (for LevelLoader compatibility)
            if (genericEscapeRoute != null)
            {
                levelConfig.escapeRoute = genericEscapeRoute;
                Debug.Log($"[UnifiedLevelImporter] Assigned generic escape route '{genericEscapeRoute.routeName}' to LevelConfig");
            }
            else if (escapeRoute != null)
            {
                levelConfig.escapeRoute = escapeRoute;
                Debug.Log($"[UnifiedLevelImporter] Assigned auto-generated escape route '{escapeRoute.routeName}' to LevelConfig (fallback)");
            }
            
            if (genericReturnRoute != null)
            {
                levelConfig.returnRoute = genericReturnRoute;
                Debug.Log($"[UnifiedLevelImporter] Assigned generic return route '{genericReturnRoute.routeName}' to LevelConfig");
            }
            else if (returnRoute != null)
            {
                levelConfig.returnRoute = returnRoute;
                Debug.Log($"[UnifiedLevelImporter] Assigned auto-generated return route '{returnRoute.routeName}' to LevelConfig (fallback)");
            }
            
            // Add all routes to availableRoutes
            if (allRoutes.Count > 0)
            {
                levelConfig.availableRoutes = allRoutes;
                Debug.Log($"[UnifiedLevelImporter] Added {allRoutes.Count} routes to availableRoutes");
            }
            
            EditorUtility.SetDirty(levelConfig);
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Create a generic copy of a route (removes student-specific suffix)
        /// </summary>
        private static StudentRoute CreateGenericRouteCopy(StudentRoute sourceRoute, string genericName, string targetFolder)
        {
            if (sourceRoute == null)
                return null;
            
            string targetPath = $"{targetFolder}/{genericName}.asset";
            
            // Check if already exists
            StudentRoute existingRoute = AssetDatabase.LoadAssetAtPath<StudentRoute>(targetPath);
            if (existingRoute != null)
            {
                Debug.Log($"[UnifiedLevelImporter] Generic route already exists at {targetPath}, reusing it");
                return existingRoute;
            }
            
            // Create a copy
            StudentRoute genericRoute = ScriptableObject.Instantiate(sourceRoute);
            genericRoute.routeName = genericName;
            
            Debug.Log($"[UnifiedLevelImporter] Generic copy waypoints count: {genericRoute.waypoints?.Count ?? 0}");
            if (genericRoute.waypoints != null)
            {
                for (int i = 0; i < genericRoute.waypoints.Count; i++)
                {
                    var wp = genericRoute.waypoints[i];
                    Debug.Log($"[UnifiedLevelImporter]   Waypoint {i}: {wp?.waypointName ?? "null"} at {wp?.transform?.position}");
                }
            }
            
            AssetDatabase.CreateAsset(genericRoute, targetPath);
            EditorUtility.SetDirty(genericRoute);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[UnifiedLevelImporter] Created generic route copy: {sourceRoute.routeName} -> {genericName} at {targetPath}");
            return genericRoute;
        }
        
        /// <summary>
        /// Assign LevelConfig to LevelLoader and ClassroomManager
        /// </summary>
        private static void AssignLevelConfigToManagers(LevelConfig levelConfig)
        {
            // Assign to LevelLoader
            var levelLoader = Object.FindObjectOfType<LevelLoader>();
            if (levelLoader != null)
            {
                var levelLoaderField = typeof(LevelLoader).GetField("currentLevel", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (levelLoaderField != null)
                {
                    levelLoaderField.SetValue(levelLoader, levelConfig);
                    Debug.Log($"[UnifiedLevelImporter] Assigned LevelConfig to LevelLoader");
                }
            }
            
            // Assign to ClassroomManager
            var classroomManager = Object.FindObjectOfType<ClassroomManager>();
            if (classroomManager != null)
            {
                var classroomField = typeof(ClassroomManager).GetField("levelConfig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (classroomField != null)
                {
                    classroomField.SetValue(classroomManager, levelConfig);
                    Debug.Log($"[UnifiedLevelImporter] Assigned LevelConfig to ClassroomManager");
                }
            }
            
            // Create classroom door marker if not exists
            GameObject classroomGroup = GameObject.Find("=== CLASSROOM ===");
            if (classroomGroup != null && levelConfig.classroomDoor == null)
            {
                GameObject doorMarker = new GameObject("ClassroomDoor");
                doorMarker.transform.SetParent(classroomGroup.transform);
                doorMarker.transform.position = new Vector3(0, 0, 5); // Default position
                levelConfig.classroomDoor = doorMarker.transform;
                EditorUtility.SetDirty(levelConfig);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UnifiedLevelImporter] Created and assigned classroom door");
            }
        }
        
        /// <summary>
        /// Create and configure StudentInteractionProcessor if interactions exist
        /// </summary>
        private static void ConfigureStudentInteractionProcessor(UnifiedLevelSchema schema)
        {
            if (schema.studentInteractions == null || schema.studentInteractions.Count == 0)
            {
                Debug.Log("[UnifiedLevelImporter] No student interactions in JSON - skipping StudentInteractionProcessor setup");
                return;
            }
            
            // Find or create StudentInteractionProcessor
            StudentInteractionProcessor processor = Object.FindObjectOfType<StudentInteractionProcessor>();
            if (processor == null)
            {
                GameObject managersGroup = GameObject.Find("=== MANAGERS ===");
                if (managersGroup == null)
                {
                    Debug.LogWarning("[UnifiedLevelImporter] Managers group not found - cannot create StudentInteractionProcessor");
                    return;
                }
                
                GameObject processorObj = new GameObject("StudentInteractionProcessor");
                processorObj.transform.SetParent(managersGroup.transform);
                processor = processorObj.AddComponent<StudentInteractionProcessor>();
                Debug.Log($"[UnifiedLevelImporter] Created StudentInteractionProcessor");
            }
            
            // Convert StudentInteractionData to StudentInteractionConfig
            List<StudentInteractionConfig> interactionConfigs = new List<StudentInteractionConfig>();
            foreach (var interactionData in schema.studentInteractions)
            {
                StudentInteractionConfig config = new StudentInteractionConfig();
                config.sourceStudent = interactionData.sourceStudentId;
                config.targetStudent = interactionData.targetStudentId;
                config.eventType = interactionData.eventType;
                config.triggerCondition = interactionData.triggerCondition;
                config.probability = interactionData.probability;
                config.customSeverity = interactionData.triggerValue;  // Use triggerValue for time-based triggers
                config.description = interactionData.description;
                interactionConfigs.Add(config);
            }
            
            // Load interactions
            processor.LoadInteractions(interactionConfigs);
            Debug.Log($"[UnifiedLevelImporter] Loaded {interactionConfigs.Count} student interactions");
        }
        
        // Helper class to expose JSONLevelImporter's CreateRoutesFromData method
        /// <summary>
        /// Strip single-line comments (//) from JSON string
        /// Unity's JsonUtility doesn't support comments
        /// </summary>
        private static string StripJsonComments(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;
                
            // Remove UTF-8 BOM if present
            if (json.Length > 3 && json[0] == 0xEF && json[1] == 0xBB && json[2] == 0xBF)
            {
                json = json.Substring(3);
            }
                
            var lines = json.Split('\n');
            var result = new StringBuilder();
            
            foreach (string line in lines)
            {
                // Find comment start (//) that's not inside a string
                int commentIndex = -1;
                bool insideString = false;
                bool escaped = false;
                
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    
                    if (!escaped && c == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    
                    if (!escaped && c == '"')
                    {
                        insideString = !insideString;
                    }
                    
                    if (!insideString && i + 1 < line.Length && c == '/' && line[i + 1] == '/')
                    {
                        commentIndex = i;
                        break;
                    }
                    
                    escaped = false;
                }
                
                string processedLine = commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
                string trimmed = processedLine.Trim();
                
                // Skip empty lines
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                    
                result.AppendLine(trimmed);
            }
            
            return result.ToString().Trim();
        }
        
        private static class JSONLevelImporterHelper
        {
            public static System.Collections.Generic.List<StudentRoute> CreateRoutesFromData(LevelDataSchema data)
            {
                // Call the private method from JSONLevelImporter via reflection
                var method = typeof(JSONLevelImporter).GetMethod("CreateRoutesFromData", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (method != null)
                {
                    return (System.Collections.Generic.List<StudentRoute>)method.Invoke(null, new object[] { data });
                }
                
                Debug.LogError("[UnifiedLevelImporter] Failed to find CreateRoutesFromData method in JSONLevelImporter");
                return new System.Collections.Generic.List<StudentRoute>();
            }
        }
        
        /// <summary>
        /// Add StudentInteractableObject component to desk so students can interact with it
        /// </summary>
        private static void AddDeskInteractableComponent(GameObject deskObj)
        {
            if (deskObj == null) return;
            
            // Check if component already exists
            if (deskObj.GetComponent<StudentInteractableObject>() != null)
            {
                Debug.Log($"[UnifiedLevelImporter] Desk {deskObj.name} already has StudentInteractableObject");
                return;
            }
            
            // Add the component
            StudentInteractableObject interactable = deskObj.AddComponent<StudentInteractableObject>();
            
            // Configure the desk as an interactable object
            interactable.objectName = deskObj.name;
            interactable.canBeKnockedOver = true;
            interactable.canBeThrown = false;
            interactable.canMakeNoise = false;
            interactable.canBeDropped = false;
            
            // Ensure desk has a non-trigger collider for OverlapSphere detection
            Collider collider = deskObj.GetComponent<Collider>();
            if (collider != null)
            {
                if (collider.isTrigger)
                {
                    collider.isTrigger = false;
                    Debug.Log($"[UnifiedLevelImporter] Disabled isTrigger on desk {deskObj.name} collider for OverlapSphere detection");
                }
            }
            else
            {
                // Add BoxCollider if no collider exists
                BoxCollider boxCollider = deskObj.AddComponent<BoxCollider>();
                boxCollider.isTrigger = false;
                Debug.Log($"[UnifiedLevelImporter] Added BoxCollider to desk {deskObj.name} for OverlapSphere detection");
            }
            
            Debug.Log($"[UnifiedLevelImporter] Added StudentInteractableObject to desk {deskObj.name}");
        }
    }
}
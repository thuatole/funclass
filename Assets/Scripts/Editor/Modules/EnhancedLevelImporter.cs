using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using FunClass.Editor.Data;
using FunClass.Core;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Enhanced level importer following the spec from FunClass_Auto_Level_JSON_Import_A-Z.md
    /// Creates playable classroom level from a single JSON file with auto-generation features
    /// </summary>
    public static class EnhancedLevelImporter
    {
        private const string DEFAULT_ASSET_MAP_PATH = "Assets/Configs/DefaultAssetMap.asset";
        
        /// <summary>
        /// Main entry point: Import level from enhanced JSON schema
        /// </summary>
        public static void ImportLevelFromJSON(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[EnhancedLevelImporter] JSON file not found: {jsonPath}");
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("Import Level", "Loading JSON...", 0f);
                
                // 1. Load and validate JSON
                string json = File.ReadAllText(jsonPath);
                EnhancedLevelSchema schema = JsonUtility.FromJson<EnhancedLevelSchema>(json);
                
                if (schema == null)
                {
                    Debug.LogError($"[EnhancedLevelImporter] Failed to parse JSON: {jsonPath}");
                    EditorUtility.ClearProgressBar();
                    return;
                }
                
                Debug.Log($"[EnhancedLevelImporter] Successfully loaded schema: {schema.levelId}");
                
                // 2. Validate schema
                if (!ValidateSchema(schema))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
                
                // 3. Create or load asset map
                AssetMapConfig assetMap = GetOrCreateAssetMap(schema);
                if (assetMap == null)
                {
                    Debug.LogError("[EnhancedLevelImporter] Failed to get asset map. Aborting.");
                    EditorUtility.ClearProgressBar();
                    return;
                }
                
                EditorUtility.DisplayProgressBar("Import Level", "Creating new scene...", 0.1f);
                
                // 4. Create new scene or clear existing
                SceneSetupManager.CreateOrClearScene(schema.levelId);
                
                // 4.5. Create essential managers and UI (required for gameplay)
                EditorUtility.DisplayProgressBar("Import Level", "Creating managers and UI...", 0.15f);
                SceneHierarchyBuilder.CreateManagersGroup();
                SceneHierarchyBuilder.CreateUIGroup();
                
                // 5. Generate desk grid
                EditorUtility.DisplayProgressBar("Import Level", "Generating desk grid...", 0.2f);
                List<DeskData> desks = DeskGridGenerator.GenerateDeskGrid(schema);
                Debug.Log($"[EnhancedLevelImporter] Generated {desks.Count} desks for {schema.students} students");
                
                // 6. Instantiate desks and bind students
                EditorUtility.DisplayProgressBar("Import Level", "Instantiating desks and students...", 0.3f);
                List<StudentDeskPair> studentDeskPairs = StudentPlacementManager.PlaceStudentsAndDesks(
                    schema, desks, assetMap);
                
                // 7. Generate routes
                EditorUtility.DisplayProgressBar("Import Level", "Generating escape and return routes...", 0.5f);
                RouteGenerator.GenerateRoutes(schema, studentDeskPairs);
                
                // 8. Setup board and environment
                EditorUtility.DisplayProgressBar("Import Level", "Setting up board and environment...", 0.7f);
                EnvironmentSetup.SetupEnvironment(schema, assetMap);
                
                // 9. Create teacher
                EditorUtility.DisplayProgressBar("Import Level", "Creating teacher...", 0.75f);
                SceneHierarchyBuilder.CreateTeacherGroup();
                
                // 10. Configure game systems
                EditorUtility.DisplayProgressBar("Import Level", "Configuring game systems...", 0.78f);
                ConfigureGameSystems(schema, studentDeskPairs);
                
                // 12. Fix pink materials
                EditorUtility.DisplayProgressBar("Import Level", "Fixing materials...", 0.8f);
                MaterialFixer.ScanAndFixPinkMaterials();
                
                // 12. Setup lighting and cameras
                EditorUtility.DisplayProgressBar("Import Level", "Setting up lighting...", 0.9f);
                LightingSetup.SetupLighting(schema.environment);
                
                // 13. Save scene
                EditorUtility.DisplayProgressBar("Import Level", "Saving scene...", 0.95f);
                string scenePath = SceneSetupManager.SaveScene(schema.levelId);
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"[EnhancedLevelImporter] Level '{schema.levelId}' imported successfully!");
                Debug.Log($"[EnhancedLevelImporter] Scene saved to: {scenePath}");
                Debug.Log($"[EnhancedLevelImporter] Desks: {desks.Count}, Students: {studentDeskPairs.Count}");
                
                EditorUtility.DisplayDialog("Success", 
                    $"Level '{schema.levelId}' imported successfully!\n" +
                    $"Students: {studentDeskPairs.Count}\n" +
                    $"Scene: {Path.GetFileName(scenePath)}", 
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[EnhancedLevelImporter] Error importing level: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to import level:\n{e.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Validate the enhanced JSON schema
        /// </summary>
        private static bool ValidateSchema(EnhancedLevelSchema schema)
        {
            List<string> errors = new List<string>();
            
            // Check levelId
            if (string.IsNullOrEmpty(schema.levelId))
            {
                errors.Add("levelId is required");
            }
            else if (schema.levelId.Length > 50)
            {
                errors.Add("levelId must be 50 characters or less");
            }
            
            // Check difficulty
            if (string.IsNullOrEmpty(schema.difficulty))
            {
                errors.Add("difficulty is required (easy, medium, hard)");
            }
            else if (!IsValidDifficulty(schema.difficulty))
            {
                errors.Add($"difficulty must be 'easy', 'medium', or 'hard', got '{schema.difficulty}'");
            }
            
            // Check students count
            if (schema.students < 4 || schema.students > 10)
            {
                errors.Add("students must be between 4 and 10");
            }
            
            if (schema.students % 2 != 0)
            {
                errors.Add("students must be even (for 2-row grid)");
            }
            
            // Check desk layout
            if (schema.deskLayout == null)
            {
                schema.deskLayout = new DeskLayoutData(); // Use defaults
            }
            else if (schema.deskLayout.rows != 2)
            {
                Debug.LogWarning($"[EnhancedLevelImporter] Overriding rows to 2 (spec requires 2 rows)");
                schema.deskLayout.rows = 2;
            }
            
            // Validate desk layout values
            if (schema.deskLayout.spacingX < 1.0f || schema.deskLayout.spacingX > 5.0f)
            {
                errors.Add("deskLayout.spacingX must be between 1.0 and 5.0 meters");
            }
            
            if (schema.deskLayout.spacingZ < 1.0f || schema.deskLayout.spacingZ > 5.0f)
            {
                errors.Add("deskLayout.spacingZ must be between 1.0 and 5.0 meters");
            }
            
            if (schema.deskLayout.aisleWidth < 1.0f || schema.deskLayout.aisleWidth > 3.0f)
            {
                errors.Add("deskLayout.aisleWidth must be between 1.0 and 3.0 meters");
            }
            
            // Check classroom
            if (schema.classroom == null)
            {
                schema.classroom = new ClassroomData(); // Use defaults
            }
            
            // Validate classroom dimensions
            if (schema.classroom.width < 5.0f || schema.classroom.width > 30.0f)
            {
                errors.Add("classroom.width must be between 5.0 and 30.0 meters");
            }
            
            if (schema.classroom.depth < 5.0f || schema.classroom.depth > 30.0f)
            {
                errors.Add("classroom.depth must be between 5.0 and 30.0 meters");
            }
            
            if (schema.classroom.height < 2.0f || schema.classroom.height > 10.0f)
            {
                errors.Add("classroom.height must be between 2.0 and 10.0 meters");
            }
            
            // Validate door position if provided
            if (schema.classroom.doorPosition != null)
            {
                // Door should be within classroom bounds
                float halfWidth = schema.classroom.width / 2f;
                float halfDepth = schema.classroom.depth / 2f;
                
                if (Mathf.Abs(schema.classroom.doorPosition.x) > halfWidth)
                {
                    errors.Add($"doorPosition.x ({schema.classroom.doorPosition.x}) is outside classroom width (±{halfWidth})");
                }
                
                if (schema.classroom.doorPosition.y < 0 || schema.classroom.doorPosition.y > schema.classroom.height)
                {
                    errors.Add($"doorPosition.y ({schema.classroom.doorPosition.y}) is outside classroom height (0-{schema.classroom.height})");
                }
                
                if (Mathf.Abs(schema.classroom.doorPosition.z) > halfDepth)
                {
                    errors.Add($"doorPosition.z ({schema.classroom.doorPosition.z}) is outside classroom depth (±{halfDepth})");
                }
            }
            
            // Validate board position if provided
            if (schema.classroom.boardPosition != null)
            {
                // Board should be on front wall (negative Z for front of classroom)
                if (schema.classroom.boardPosition.z > -0.5f)
                {
                    errors.Add($"boardPosition.z ({schema.classroom.boardPosition.z}) should be negative (front wall)");
                }
                
                if (schema.classroom.boardPosition.y < 0.5f || schema.classroom.boardPosition.y > schema.classroom.height - 0.5f)
                {
                    errors.Add($"boardPosition.y ({schema.classroom.boardPosition.y}) should be between 0.5 and {schema.classroom.height - 0.5f} meters");
                }
            }
            
            if (errors.Count > 0)
            {
                string errorMsg = string.Join("\n", errors);
                Debug.LogError($"[EnhancedLevelImporter] Schema validation failed:\n{errorMsg}");
                EditorUtility.DisplayDialog("Validation Error", 
                    $"JSON validation failed:\n{errorMsg}", 
                    "OK");
                return false;
            }
            
            Debug.Log($"[EnhancedLevelImporter] Schema validation passed");
            return true;
        }
        
        private static bool IsValidDifficulty(string difficulty)
        {
            string lower = difficulty.ToLower();
            return lower == "easy" || lower == "medium" || lower == "hard";
        }
        
        /// <summary>
        /// Get or create asset map configuration
        /// </summary>
        private static AssetMapConfig GetOrCreateAssetMap(EnhancedLevelSchema schema)
        {
            
            // Load or create default asset map as base
            AssetMapConfig defaultAssetMap = AssetDatabase.LoadAssetAtPath<AssetMapConfig>(DEFAULT_ASSET_MAP_PATH);
            
            if (defaultAssetMap == null)
            {
                Debug.LogWarning($"[EnhancedLevelImporter] Default asset map not found at {DEFAULT_ASSET_MAP_PATH}");
                defaultAssetMap = AssetMapConfig.CreateDefaultAssetMap(DEFAULT_ASSET_MAP_PATH);
            }
            
            if (defaultAssetMap == null)
            {
                Debug.LogError("[EnhancedLevelImporter] Failed to create asset map");
                return null;
            }
            
            // Check if schema has inline asset mapping
            if (schema.assetMapping == null || schema.assetMapping.prefabMapping == null)
            {
                // No inline mapping, return default asset map
                // Validate required mappings
                if (!ValidateAssetMap(defaultAssetMap))
                {
                    Debug.LogWarning("[EnhancedLevelImporter] Asset map missing required mappings. Adding defaults...");
                    defaultAssetMap.AddDefaultMappings();
                    EditorUtility.SetDirty(defaultAssetMap);
                    AssetDatabase.SaveAssets();
                }
                return defaultAssetMap;
            }
            
            Debug.Log("[EnhancedLevelImporter] Using inline asset mapping from JSON");
            
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
                    Debug.LogWarning($"[EnhancedLevelImporter] Could not load prefab at path '{prefabPath}' for asset key '{assetKey}'");
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
                Debug.Log($"[EnhancedLevelImporter] Applied inline prefab mapping: {assetKey} -> {prefabPath}");
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
                        Debug.LogWarning($"[EnhancedLevelImporter] Could not load material at path '{materialPath}' for material key '{materialKey}'");
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
                    Debug.Log($"[EnhancedLevelImporter] Applied inline material mapping: {materialKey} -> {materialPath}");
                }
            }
            
            // Validate required mappings
            if (!ValidateAssetMap(tempAssetMap))
            {
                Debug.LogWarning("[EnhancedLevelImporter] Temporary asset map missing required mappings. Adding defaults...");
                tempAssetMap.AddDefaultMappings();
            }
            
            // Note: tempAssetMap is an in-memory ScriptableObject, not saved to disk
            Debug.Log($"[EnhancedLevelImporter] Created temporary asset map with {tempAssetMap.assetMappings.Count} asset mappings and {tempAssetMap.materialMappings.Count} material mappings");
            
            return tempAssetMap;
        }
        
        /// <summary>
        /// Create default asset map configuration
        /// </summary>
        private static AssetMapConfig CreateDefaultAssetMap()
        {
            Debug.Log($"[EnhancedLevelImporter] Creating default asset map at {DEFAULT_ASSET_MAP_PATH}");
            return AssetMapConfig.CreateDefaultAssetMap(DEFAULT_ASSET_MAP_PATH);
        }
        
        /// <summary>
        /// Validate that asset map has required mappings
        /// </summary>
        private static bool ValidateAssetMap(AssetMapConfig assetMap)
        {
            // Check for required asset keys
            string[] requiredAssets = { "DESK", "STUDENT", "BOARD" };
            
            foreach (string assetKey in requiredAssets)
            {
                if (assetMap.GetPrefab(assetKey) == null)
                {
                    Debug.LogWarning($"[EnhancedLevelImporter] Asset map missing prefab for key: {assetKey}");
                    return false;
                }
            }
            
            // Check for default material
            if (assetMap.GetMaterial("Default") == null)
            {
                Debug.LogWarning("[EnhancedLevelImporter] Asset map missing 'Default' material for pink material fix");
                return false;
            }
            
            return true;
        }
    
    /// <summary>
    /// Configure game systems: goals, configs, interactions
    /// </summary>
    private static void ConfigureGameSystems(EnhancedLevelSchema schema, List<StudentDeskPair> studentDeskPairs)
    {
        Debug.Log($"[EnhancedLevelImporter] Configuring game systems for level '{schema.levelId}'");
        
        // 1. Create level folder structure
        EditorUtils.CreateLevelFolderStructure(schema.levelId);
        
        // 2. Create LevelGoalConfig from schema.goalSettings
        LevelGoalConfig goalConfig = CreateGoalConfigFromSchema(schema);
        
        // 3. Create LevelConfig and assign goalConfig
        LevelConfig levelConfig = CreateLevelConfigFromSchema(schema, goalConfig);
        
        // 3.5 Create InfluenceScopeConfig from schema.influenceScopeSettings
        InfluenceScopeConfig influenceScopeConfig = CreateInfluenceScopeConfigFromSchema(schema);
        if (influenceScopeConfig != null)
        {
            levelConfig.influenceScopeConfig = influenceScopeConfig;
            EditorUtility.SetDirty(levelConfig);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnhancedLevelImporter] Assigned InfluenceScopeConfig to LevelConfig");
        }
        
        // 4. Create StudentConfigs from student data
        List<StudentConfig> studentConfigs = CreateStudentConfigsFromSchema(schema, studentDeskPairs);
        if (studentConfigs != null && studentConfigs.Count > 0)
        {
            levelConfig.students = studentConfigs;
            EditorUtility.SetDirty(levelConfig);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnhancedLevelImporter] Assigned {studentConfigs.Count} student configs to LevelConfig");
            
            // 4.5 Assign StudentConfigs to StudentAgent components
            AssignStudentConfigsToAgents(studentConfigs, studentDeskPairs);
        }
        
        // 5. Assign routes to LevelConfig (escapeRoute, returnRoute)
        AssignRoutesToLevelConfig(schema, levelConfig);
        
        // 6. Assign LevelConfig to LevelLoader and ClassroomManager
        AssignLevelConfigToManagers(levelConfig);
        
        // 7. Create and configure StudentInteractionProcessor if interactions exist
        ConfigureStudentInteractionProcessor(schema);
        
        // 8. Configure StudentInfluenceManager settings if provided
        ConfigureStudentInfluenceManager(schema);
        
        Debug.Log($"[EnhancedLevelImporter] Game systems configuration completed");
    }

    /// <summary>
    /// Create LevelGoalConfig from schema.goalSettings (or defaults)
    /// </summary>
    private static LevelGoalConfig CreateGoalConfigFromSchema(EnhancedLevelSchema schema)
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
            
            Debug.Log($"[EnhancedLevelImporter] Created LevelGoalConfig from JSON settings");
        }
        else
        {
            // Use defaults (already set by ScriptableObject)
            Debug.Log($"[EnhancedLevelImporter] Created default LevelGoalConfig (no goalSettings in JSON)");
        }
        
        EditorUtility.SetDirty(goalConfig);
        AssetDatabase.SaveAssets();
        
        return goalConfig;
    }
    
    /// <summary>
    /// Create LevelConfig and assign LevelGoalConfig
    /// </summary>
    private static LevelConfig CreateLevelConfigFromSchema(EnhancedLevelSchema schema, LevelGoalConfig goalConfig)
    {
        string levelConfigPath = $"Assets/Configs/{schema.levelId}/{schema.levelId}_Config.asset";
        
        LevelConfig levelConfig = EditorUtils.CreateScriptableObject<LevelConfig>(levelConfigPath);
        
        levelConfig.levelId = schema.levelId;
        levelConfig.levelGoal = goalConfig;
        
        EditorUtility.SetDirty(levelConfig);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[EnhancedLevelImporter] Created LevelConfig at {levelConfigPath}");
        
        return levelConfig;
    }
    
    /// <summary>
    /// Create InfluenceScopeConfig from schema.influenceScopeSettings
    /// </summary>
    private static InfluenceScopeConfig CreateInfluenceScopeConfigFromSchema(EnhancedLevelSchema schema)
    {
        if (schema.influenceScopeSettings == null)
        {
            Debug.Log("[EnhancedLevelImporter] No influenceScopeSettings in JSON - skipping InfluenceScopeConfig creation");
            return null;
        }
        
        string configPath = $"Assets/Configs/{schema.levelId}/{schema.levelId}_InfluenceScope.asset";
        InfluenceScopeConfig config = EditorUtils.CreateScriptableObject<InfluenceScopeConfig>(configPath);
        
        // Load data from schema (direct assignment to avoid editor/runtime assembly conflicts)
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
        
        Debug.Log($"[EnhancedLevelImporter] Created InfluenceScopeConfig at {configPath}");
        Debug.Log($"[EnhancedLevelImporter] Loaded {config.eventScopes.Count} event scope configurations");
        
        return config;
    }
    
    /// <summary>
    /// Create StudentConfig ScriptableObjects from enhanced student data
    /// </summary>
    private static List<StudentConfig> CreateStudentConfigsFromSchema(EnhancedLevelSchema schema, List<StudentDeskPair> studentDeskPairs)
    {
        if (studentDeskPairs == null || studentDeskPairs.Count == 0)
        {
            Debug.LogWarning("[EnhancedLevelImporter] No student desk pairs provided");
            return new List<StudentConfig>();
        }
        
        List<StudentConfig> studentConfigs = new List<StudentConfig>();
        
        for (int i = 0; i < studentDeskPairs.Count; i++)
        {
            var pair = studentDeskPairs[i];
            
            // Try to find matching enhanced student config
            EnhancedStudentData studentData = null;
            if (schema.studentConfigs != null)
            {
                studentData = schema.studentConfigs.Find(s => s.studentId == pair.studentId || s.studentName == pair.studentName);
            }
            
            string studentConfigPath = $"Assets/Configs/{schema.levelId}/Students/Student_{pair.studentName}.asset";
            StudentConfig config = EditorUtils.CreateScriptableObject<StudentConfig>(studentConfigPath);
            
            config.studentId = pair.studentId;
            config.studentName = pair.studentName;
            config.initialState = StudentState.Calm;
            
            if (studentData != null && studentData.personality != null)
            {
                config.patience = studentData.personality.patience;
                config.attentionSpan = studentData.personality.attentionSpan;
                config.impulsiveness = studentData.personality.impulsiveness;
                config.influenceSusceptibility = studentData.personality.influenceSusceptibility;
                config.influenceResistance = studentData.personality.influenceResistance;
                config.panicThreshold = studentData.personality.panicThreshold;
            }
            else
            {
                // Generate personality based on difficulty (same as StudentPlacementManager)
                config.patience = Random.Range(0.4f, 0.7f);
                config.attentionSpan = Random.Range(0.3f, 0.7f);
                config.impulsiveness = Random.Range(0.3f, 0.7f);
                config.influenceSusceptibility = Random.Range(0.4f, 0.7f);
                config.influenceResistance = Random.Range(0.3f, 0.6f);
                config.panicThreshold = Random.Range(0.4f, 0.8f);
            }
            
            if (studentData != null && studentData.behaviors != null)
            {
                config.canFidget = studentData.behaviors.canFidget;
                config.canLookAround = studentData.behaviors.canLookAround;
                config.canStandUp = studentData.behaviors.canStandUp;
                config.canMoveAround = studentData.behaviors.canMoveAround;
                config.canDropItems = studentData.behaviors.canDropItems;
                config.canKnockOverObjects = studentData.behaviors.canKnockOverObjects;
                config.canMakeNoiseWithObjects = studentData.behaviors.canMakeNoiseWithObjects;
                config.canThrowObjects = studentData.behaviors.canThrowObjects;
                config.minIdleTime = studentData.behaviors.minIdleTime;
                config.maxIdleTime = studentData.behaviors.maxIdleTime;
            }
            else
            {
                // Default behaviors
                config.canFidget = true;
                config.canLookAround = true;
                config.canStandUp = Random.value < 0.5f;
                config.canMoveAround = Random.value < 0.4f;
                config.canDropItems = Random.value < 0.3f;
                config.canKnockOverObjects = Random.value < 0.3f;
                config.canMakeNoiseWithObjects = true;
                config.canThrowObjects = Random.value < 0.2f;
                config.minIdleTime = 2f;
                config.maxIdleTime = 8f;
            }
            
            EditorUtility.SetDirty(config);
            studentConfigs.Add(config);
            
            Debug.Log($"[EnhancedLevelImporter] Created StudentConfig for {pair.studentName}");
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"[EnhancedLevelImporter] Created {studentConfigs.Count} StudentConfig assets");
        
        return studentConfigs;
    }
    
    /// <summary>
    /// Assign StudentConfigs to StudentAgent components in scene
    /// </summary>
    private static void AssignStudentConfigsToAgents(List<StudentConfig> studentConfigs, List<StudentDeskPair> studentDeskPairs)
    {
        if (studentConfigs == null || studentDeskPairs == null)
        {
            Debug.LogWarning("[EnhancedLevelImporter] Cannot assign configs to agents - null parameters");
            return;
        }
        
        int assignedCount = 0;
        
        foreach (var pair in studentDeskPairs)
        {
            if (pair.studentObject == null)
            {
                Debug.LogWarning($"[EnhancedLevelImporter] Student object is null for {pair.studentName}");
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
                Debug.LogWarning($"[EnhancedLevelImporter] StudentAgent component not found on {pair.studentObject.name}");
                continue;
            }
            
            // Find matching config
            StudentConfig config = studentConfigs.Find(c => c.studentId == pair.studentId || c.studentName == pair.studentName);
            if (config == null)
            {
                Debug.LogWarning($"[EnhancedLevelImporter] No matching StudentConfig found for {pair.studentName}");
                continue;
            }
            
            // Assign config using reflection (since config field is private)
            var configField = typeof(StudentAgent).GetField("config", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                configField.SetValue(studentAgent, config);
                assignedCount++;
                Debug.Log($"[EnhancedLevelImporter] Assigned StudentConfig '{config.studentName}' to {pair.studentObject.name}");
            }
            else
            {
                Debug.LogWarning($"[EnhancedLevelImporter] Could not find 'config' field on StudentAgent");
            }
        }
        
        Debug.Log($"[EnhancedLevelImporter] Assigned {assignedCount}/{studentDeskPairs.Count} StudentConfigs to StudentAgents");
    }
    
    /// <summary>
    /// Assign routes to LevelConfig (escapeRoute, returnRoute)
    /// </summary>
    private static void AssignRoutesToLevelConfig(EnhancedLevelSchema schema, LevelConfig levelConfig)
    {
        // Find auto-generated routes in Assets/Configs/AutoGenerated/Routes
        string autoRoutesFolder = "Assets/Configs/AutoGenerated/Routes";
        
        if (!AssetDatabase.IsValidFolder(autoRoutesFolder))
        {
            Debug.LogWarning($"[EnhancedLevelImporter] Auto-generated routes folder not found: {autoRoutesFolder}");
            return;
        }
        
        // Find all StudentRoute assets
        string[] routeGuids = AssetDatabase.FindAssets("t:StudentRoute", new[] { autoRoutesFolder });
        if (routeGuids.Length == 0)
        {
            Debug.LogWarning($"[EnhancedLevelImporter] No StudentRoute assets found in {autoRoutesFolder}");
            return;
        }
        
        Debug.Log($"[EnhancedLevelImporter] Found {routeGuids.Length} StudentRoute assets");
        
        // Separate escape and return routes by name pattern
        StudentRoute escapeRoute = null;
        StudentRoute returnRoute = null;
        
        foreach (string guid in routeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StudentRoute route = AssetDatabase.LoadAssetAtPath<StudentRoute>(path);
            if (route == null) continue;
            
            if (route.routeName.Contains("EscapeRoute"))
            {
                if (escapeRoute == null)
                {
                    escapeRoute = route;
                    Debug.Log($"[EnhancedLevelImporter] Selected escape route: {route.routeName}");
                }
            }
            else if (route.routeName.Contains("ReturnRoute"))
            {
                if (returnRoute == null)
                {
                    returnRoute = route;
                    Debug.Log($"[EnhancedLevelImporter] Selected return route: {route.routeName}");
                }
            }
        }
        
        // Assign to LevelConfig
        if (escapeRoute != null)
        {
            levelConfig.escapeRoute = escapeRoute;
            Debug.Log($"[EnhancedLevelImporter] Assigned escape route '{escapeRoute.routeName}' to LevelConfig");
        }
        
        if (returnRoute != null)
        {
            levelConfig.returnRoute = returnRoute;
            Debug.Log($"[EnhancedLevelImporter] Assigned return route '{returnRoute.routeName}' to LevelConfig");
        }
        
        // If manual routes provided in schema.routes, assign them (override)
        if (schema.routes != null && schema.routes.Count > 0)
        {
            Debug.Log($"[EnhancedLevelImporter] Manual routes provided in JSON, but automatic assignment not implemented yet");
        }
        
        EditorUtility.SetDirty(levelConfig);
        AssetDatabase.SaveAssets();
    }
    
    /// <summary>
    /// Find existing StudentRoute asset at path
    /// </summary>
    private static StudentRoute FindStudentRouteAsset(string path)
    {
        StudentRoute route = AssetDatabase.LoadAssetAtPath<StudentRoute>(path);
        if (route == null)
        {
            Debug.LogWarning($"[EnhancedLevelImporter] StudentRoute asset not found at {path}");
        }
        return route;
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
                Debug.Log($"[EnhancedLevelImporter] Assigned LevelConfig to LevelLoader");
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
                Debug.Log($"[EnhancedLevelImporter] Assigned LevelConfig to ClassroomManager");
            }
        }
        
        // Create classroom door marker (similar to legacy importer)
        GameObject classroomGroup = GameObject.Find("=== CLASSROOM ===");
        if (classroomGroup != null && levelConfig.classroomDoor == null)
        {
            GameObject doorMarker = new GameObject("ClassroomDoor");
            doorMarker.transform.SetParent(classroomGroup.transform);
            doorMarker.transform.position = new Vector3(0, 0, 5); // Default position
            levelConfig.classroomDoor = doorMarker.transform;
            EditorUtility.SetDirty(levelConfig);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnhancedLevelImporter] Created and assigned classroom door at {doorMarker.transform.position}");
        }
    }
    
    /// <summary>
    /// Create and configure StudentInteractionProcessor if interactions exist
    /// </summary>
    private static void ConfigureStudentInteractionProcessor(EnhancedLevelSchema schema)
    {
        if (schema.studentInteractions == null || schema.studentInteractions.Count == 0)
        {
            Debug.Log("[EnhancedLevelImporter] No student interactions in JSON - skipping StudentInteractionProcessor setup");
            return;
        }
        
        // Find or create StudentInteractionProcessor
        StudentInteractionProcessor processor = Object.FindObjectOfType<StudentInteractionProcessor>();
        if (processor == null)
        {
            GameObject managersGroup = GameObject.Find("=== MANAGERS ===");
            if (managersGroup == null)
            {
                Debug.LogWarning("[EnhancedLevelImporter] Managers group not found - cannot create StudentInteractionProcessor");
                return;
            }
            
            GameObject processorObj = new GameObject("StudentInteractionProcessor");
            processorObj.transform.SetParent(managersGroup.transform);
            processor = processorObj.AddComponent<StudentInteractionProcessor>();
            Debug.Log($"[EnhancedLevelImporter] Created StudentInteractionProcessor");
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
            config.customSeverity = interactionData.triggerValue;  // triggerValue maps to customSeverity for time-based triggers
            config.description = interactionData.description;
            interactionConfigs.Add(config);
        }
        
        // Load interactions
        processor.LoadInteractions(interactionConfigs);
        Debug.Log($"[EnhancedLevelImporter] Loaded {interactionConfigs.Count} student interactions into StudentInteractionProcessor");
    }
    
    /// <summary>
    /// Configure StudentInfluenceManager with custom settings from JSON
    /// </summary>
    private static void ConfigureStudentInfluenceManager(EnhancedLevelSchema schema)
    {
        if (schema.influenceScopeSettings == null)
        {
            Debug.Log("[EnhancedLevelImporter] No influenceScopeSettings in JSON - using defaults");
            return;
        }
        
        // Find StudentInfluenceManager
        StudentInfluenceManager influenceManager = Object.FindObjectOfType<StudentInfluenceManager>();
        if (influenceManager == null)
        {
            Debug.LogWarning("[EnhancedLevelImporter] StudentInfluenceManager not found - cannot configure settings");
            return;
        }
        
        // Log influence scope settings
        Debug.Log($"[EnhancedLevelImporter] Influence scope settings loaded:");
        Debug.Log($"[EnhancedLevelImporter]   Disruption penalty per unresolved source: {schema.influenceScopeSettings.disruptionPenaltyPerUnresolvedSource}");
        if (schema.influenceScopeSettings.eventScopes != null)
        {
            Debug.Log($"[EnhancedLevelImporter]   Event scopes count: {schema.influenceScopeSettings.eventScopes.Count}");
            foreach (var eventScope in schema.influenceScopeSettings.eventScopes)
            {
                Debug.Log($"[EnhancedLevelImporter]     - {eventScope.eventTypeName}: {eventScope.scope} (severity: {eventScope.baseSeverity})");
            }
        }
        
        // Note: InfluenceScopeConfig has been created and assigned to LevelConfig.
        // StudentInfluenceManager will read from LevelConfig.influenceScopeConfig at runtime.
        Debug.Log($"[EnhancedLevelImporter] Influence scope settings applied via InfluenceScopeConfig.");
    }
        


    // Data classes for internal use
    public class DeskData
    {
        public string deskId;
        public Vector3 position;
        public Quaternion rotation;
        public GameObject deskObject; // Instantiated desk prefab
        public GameObject studentSlot; // Position where student stands
        public GameObject messSlot; // Position where mess can spawn
    }
    
    public class StudentDeskPair
    {
        public string studentId;
        public string studentName;
        public DeskData desk;
        public GameObject studentObject; // Instantiated student prefab
    }
}
}
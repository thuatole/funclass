using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using FunClass.Editor.Data;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module import level data từ JSON file
    /// </summary>
    public static class JSONLevelImporter
    {
        /// <summary>
        /// Import level từ JSON file
        /// </summary>
        public static LevelDataSchema ImportFromJSON(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[JSONLevelImporter] File not found: {jsonPath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                LevelDataSchema data = JsonUtility.FromJson<LevelDataSchema>(json);
                
                Debug.Log($"[JSONLevelImporter] Successfully imported level: {data.levelName}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSONLevelImporter] Failed to parse JSON: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Export level data ra JSON file
        /// </summary>
        public static void ExportToJSON(LevelDataSchema data, string jsonPath)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(jsonPath, json);
                
                AssetDatabase.Refresh();
                Debug.Log($"[JSONLevelImporter] Exported to: {jsonPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSONLevelImporter] Failed to export JSON: {e.Message}");
            }
        }

        /// <summary>
        /// Tạo level từ JSON data
        /// </summary>
        public static void CreateLevelFromData(LevelDataSchema data)
        {
            if (data == null)
            {
                Debug.LogError("[JSONLevelImporter] Data is null");
                return;
            }

            EditorUtility.DisplayProgressBar("Import Level", "Creating level from JSON...", 0f);

            try
            {
                // 1. Create folders
                EditorUtils.CreateLevelFolderStructure(data.levelName);
                EditorUtility.DisplayProgressBar("Import Level", "Creating folders...", 0.1f);

                // 2. Create level configs
                var (goalConfig, levelConfig) = CreateLevelConfigsFromData(data);
                EditorUtility.DisplayProgressBar("Import Level", "Creating configs...", 0.3f);

                // 3. Create student configs
                var studentConfigs = CreateStudentConfigsFromData(data);
                
                // Assign students to levelConfig
                if (studentConfigs != null && studentConfigs.Length > 0)
                {
                    levelConfig.students = new System.Collections.Generic.List<FunClass.Core.StudentConfig>(studentConfigs);
                    EditorUtility.SetDirty(levelConfig);
                    Debug.Log($"[JSONLevelImporter] Assigned {studentConfigs.Length} students to LevelConfig");
                }
                
                EditorUtility.DisplayProgressBar("Import Level", "Creating student configs...", 0.5f);

                // 4. Create scene hierarchy FIRST (so Waypoints group exists for routes)
                SceneHierarchyBuilder.CreateManagersGroup();
                SceneHierarchyBuilder.CreateClassroomGroup();
                SceneHierarchyBuilder.CreateTeacherGroup();
                SceneHierarchyBuilder.CreateUIGroup();
                
                // Pass student data with positions to SceneHierarchyBuilder
                var studentDataArray = data.students?.ToArray();
                SceneHierarchyBuilder.CreateStudentsGroup(data.students.Count, studentConfigs, data.levelName, studentDataArray);
                EditorUtility.DisplayProgressBar("Import Level", "Creating scene...", 0.6f);

                // 5. Create routes (after scene hierarchy so Waypoints group exists)
                Debug.Log($"[JSONLevelImporter] Routes in data: {(data.routes != null ? data.routes.Count : 0)}");
                if (data.routes != null && data.routes.Count > 0)
                {
                    var routes = CreateRoutesFromData(data);
                    Debug.Log($"[JSONLevelImporter] Created {routes.Count} routes");
                    
                    // Assign routes to level config
                    var escapeRoute = routes.Find(r => r.routeName.ToLower().Contains("escape"));
                    var returnRoute = routes.Find(r => r.routeName.ToLower().Contains("return"));
                    
                    if (escapeRoute != null)
                    {
                        // Force refresh waypoints from scene before assigning
                        escapeRoute.RefreshWaypointsFromScene();
                        EditorUtility.SetDirty(escapeRoute);
                        AssetDatabase.SaveAssets();
                        
                        // Direct assignment
                        levelConfig.escapeRoute = escapeRoute;
                        EditorUtility.SetDirty(levelConfig);
                        AssetDatabase.SaveAssets();
                        
                        Debug.Log($"[JSONLevelImporter] Assigned escape route '{escapeRoute.routeName}' with {escapeRoute.waypoints.Count} waypoints");
                    }
                    else
                    {
                        Debug.LogWarning($"[JSONLevelImporter] No escape route found in {routes.Count} routes");
                    }
                    
                    if (returnRoute != null)
                    {
                        // Force refresh waypoints from scene before assigning
                        returnRoute.RefreshWaypointsFromScene();
                        EditorUtility.SetDirty(returnRoute);
                        AssetDatabase.SaveAssets();
                        
                        // Direct assignment
                        levelConfig.returnRoute = returnRoute;
                        EditorUtility.SetDirty(levelConfig);
                        AssetDatabase.SaveAssets();
                        
                        Debug.Log($"[JSONLevelImporter] Assigned return route '{returnRoute.routeName}' with {returnRoute.waypoints.Count} waypoints");
                    }
                    else
                    {
                        Debug.LogWarning($"[JSONLevelImporter] No return route found in {routes.Count} routes");
                    }
                    
                    // Final save and reload to ensure persistence
                    EditorUtility.SetDirty(levelConfig);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // Reload LevelConfig from disk to verify
                    string configPath = AssetDatabase.GetAssetPath(levelConfig);
                    levelConfig = AssetDatabase.LoadAssetAtPath<FunClass.Core.LevelConfig>(configPath);
                    
                    Debug.Log($"[JSONLevelImporter] Reloaded LevelConfig from disk for verification");
                    
                    // Verify assignments
                    Debug.Log($"[JSONLevelImporter] === Route Assignment Verification ===");
                    Debug.Log($"[JSONLevelImporter] Escape Route: {(levelConfig.escapeRoute != null ? $"{levelConfig.escapeRoute.routeName} ({levelConfig.escapeRoute.waypoints.Count} waypoints)" : "NULL")}");
                    Debug.Log($"[JSONLevelImporter] Return Route: {(levelConfig.returnRoute != null ? $"{levelConfig.returnRoute.routeName} ({levelConfig.returnRoute.waypoints.Count} waypoints)" : "NULL")}");
                    
                    EditorUtility.DisplayProgressBar("Import Level", "Creating routes...", 0.8f);
                }
                else
                {
                    Debug.LogWarning("[JSONLevelImporter] No routes in JSON data");
                }

                // 6. Create and assign classroom door reference
                GameObject classroomGroup = GameObject.Find("=== CLASSROOM ===");
                if (classroomGroup != null && levelConfig != null)
                {
                    // Create Door marker at position (0, 0, 5) - same as Door waypoint
                    GameObject doorMarker = new GameObject("ClassroomDoor");
                    doorMarker.transform.SetParent(classroomGroup.transform);
                    doorMarker.transform.position = new UnityEngine.Vector3(0, 0, 5);
                    
                    levelConfig.classroomDoor = doorMarker.transform;
                    EditorUtility.SetDirty(levelConfig);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[JSONLevelImporter] Created and assigned classroom door at {doorMarker.transform.position}");
                }
                
                // Verify all critical references before continuing
                if (levelConfig != null)
                {
                    Debug.Log($"[JSONLevelImporter] === LevelConfig Verification ===");
                    Debug.Log($"[JSONLevelImporter] Escape Route: {(levelConfig.escapeRoute != null ? levelConfig.escapeRoute.routeName : "NULL")}");
                    Debug.Log($"[JSONLevelImporter] Classroom Door: {(levelConfig.classroomDoor != null ? levelConfig.classroomDoor.position.ToString() : "NULL")}");
                    Debug.Log($"[JSONLevelImporter] Return Route: {(levelConfig.returnRoute != null ? levelConfig.returnRoute.routeName : "NULL")}");
                }

                // 7. Assign level config to LevelLoader
                var levelLoader = UnityEngine.Object.FindObjectOfType<FunClass.Core.LevelLoader>();
                if (levelLoader != null)
                {
                    var levelLoaderField = typeof(FunClass.Core.LevelLoader).GetField("currentLevel", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (levelLoaderField != null)
                    {
                        levelLoaderField.SetValue(levelLoader, levelConfig);
                        Debug.Log($"[JSONLevelImporter] Assigned LevelConfig to LevelLoader");
                    }
                }
                
                // Add diagnostic and scenario controller
                GameObject managersGroup = GameObject.Find("=== MANAGERS ===");
                if (managersGroup != null)
                {
                    // Add diagnostic script
                    var existingDiagnostic = managersGroup.GetComponentInChildren<FunClass.Core.LevelConfigDiagnostic>();
                    if (existingDiagnostic == null)
                    {
                        GameObject diagnosticObj = new GameObject("LevelConfigDiagnostic");
                        diagnosticObj.transform.SetParent(managersGroup.transform);
                        diagnosticObj.AddComponent<FunClass.Core.LevelConfigDiagnostic>();
                        Debug.Log($"[JSONLevelImporter] Added LevelConfigDiagnostic for debugging");
                    }
                    
                    // Add scenario controller for one-time events
                    var existingScenario = managersGroup.GetComponentInChildren<FunClass.Core.ScenarioController>();
                    if (existingScenario == null)
                    {
                        GameObject scenarioObj = new GameObject("ScenarioController");
                        scenarioObj.transform.SetParent(managersGroup.transform);
                        scenarioObj.AddComponent<FunClass.Core.ScenarioController>();
                        Debug.Log($"[JSONLevelImporter] Added ScenarioController for one-time events");
                    }
                    
                    // Add runtime waypoint creator to solve editor waypoint persistence issue
                    var existingWaypointCreator = managersGroup.GetComponentInChildren<FunClass.Core.RuntimeWaypointCreator>();
                    if (existingWaypointCreator == null)
                    {
                        GameObject waypointCreatorObj = new GameObject("RuntimeWaypointCreator");
                        waypointCreatorObj.transform.SetParent(managersGroup.transform);
                        waypointCreatorObj.AddComponent<FunClass.Core.RuntimeWaypointCreator>();
                        Debug.Log($"[JSONLevelImporter] Added RuntimeWaypointCreator for runtime waypoint generation");
                    }
                }
                else
                {
                    Debug.LogWarning("[JSONLevelImporter] LevelLoader not found in scene - level config not loaded");
                }

                // 7. Assign level config to ClassroomManager
                var classroomManager = UnityEngine.Object.FindObjectOfType<FunClass.Core.ClassroomManager>();
                if (classroomManager != null)
                {
                    var classroomField = typeof(FunClass.Core.ClassroomManager).GetField("levelConfig", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (classroomField != null)
                    {
                        classroomField.SetValue(classroomManager, levelConfig);
                        Debug.Log($"[JSONLevelImporter] Assigned LevelConfig to ClassroomManager");
                    }
                }

                // 8. Create prefabs if specified
                if (data.prefabs != null && data.prefabs.Count > 0)
                {
                    PrefabGenerator.CreatePrefabsFromData(data.prefabs);
                }

                // 10. Bake NavMesh for student navigation
                BakeNavMesh();

                // 10. Save scene to persist waypoints and other scene objects
                var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                if (!string.IsNullOrEmpty(currentScene.path))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene);
                    Debug.Log($"[JSONLevelImporter] Scene saved: {currentScene.path}");
                }
                else
                {
                    Debug.LogWarning("[JSONLevelImporter] Scene has no path - save scene manually to persist waypoints");
                }

                // 11. Verify waypoints persist in scene after save
                GameObject waypointsRoot = GameObject.Find("Waypoints");
                if (waypointsRoot != null)
                {
                    int totalWaypoints = 0;
                    foreach (Transform routeGroup in waypointsRoot.transform)
                    {
                        var waypoints = routeGroup.GetComponentsInChildren<FunClass.Core.StudentWaypoint>(true); // Include inactive
                        totalWaypoints += waypoints.Length;
                        Debug.Log($"[JSONLevelImporter] Verified route '{routeGroup.name}' has {waypoints.Length} waypoint components after save");
                        
                        // List each waypoint
                        foreach (var wp in waypoints)
                        {
                            Debug.Log($"[JSONLevelImporter]   - Waypoint: {wp.waypointName} at {wp.transform.position}");
                        }
                    }
                    Debug.Log($"[JSONLevelImporter] ✓ Total waypoints persisted: {totalWaypoints}");
                }
                else
                {
                    Debug.LogWarning("[JSONLevelImporter] Waypoints root not found in scene after save!");
                }

                EditorUtility.ClearProgressBar();
                
                Debug.Log($"[JSONLevelImporter] Level '{data.levelName}' created successfully!");
                EditorUtility.DisplayDialog("Success", $"Level '{data.levelName}' imported successfully!", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[JSONLevelImporter] Error creating level: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to create level:\n{e.Message}", "OK");
            }
        }

        private static (FunClass.Core.LevelGoalConfig, FunClass.Core.LevelConfig) CreateLevelConfigsFromData(LevelDataSchema data)
        {
            var goalConfig = EditorUtils.CreateScriptableObject<FunClass.Core.LevelGoalConfig>(
                $"Assets/Configs/{data.levelName}/{data.levelName}_Goal.asset"
            );

            if (data.goalSettings != null)
            {
                goalConfig.maxDisruptionThreshold = data.goalSettings.maxDisruptionThreshold;
                goalConfig.catastrophicDisruptionLevel = data.goalSettings.catastrophicDisruptionLevel;
                goalConfig.maxAllowedCriticalStudents = data.goalSettings.maxAllowedCriticalStudents;
                goalConfig.catastrophicCriticalStudents = data.goalSettings.catastrophicCriticalStudents;
                goalConfig.maxAllowedOutsideStudents = data.goalSettings.maxAllowedOutsideStudents;
                goalConfig.catastrophicOutsideStudents = data.goalSettings.catastrophicOutsideStudents;
                goalConfig.maxOutsideTimePerStudent = data.goalSettings.maxOutsideTimePerStudent;
                goalConfig.maxAllowedOutsideGracePeriod = data.goalSettings.maxAllowedOutsideGracePeriod;
                goalConfig.timeLimitSeconds = data.goalSettings.timeLimitSeconds;
                goalConfig.requiredResolvedProblems = data.goalSettings.requiredResolvedProblems;
                goalConfig.requiredCalmDowns = data.goalSettings.requiredCalmDowns;
                
                // Disruption Timeout
                goalConfig.enableDisruptionTimeout = data.goalSettings.enableDisruptionTimeout;
                goalConfig.disruptionTimeoutThreshold = data.goalSettings.disruptionTimeoutThreshold;
                goalConfig.disruptionTimeoutSeconds = data.goalSettings.disruptionTimeoutSeconds;
                goalConfig.disruptionTimeoutWarningSeconds = data.goalSettings.disruptionTimeoutWarningSeconds;
                
                goalConfig.oneStarScore = data.goalSettings.oneStarScore;
                goalConfig.twoStarScore = data.goalSettings.twoStarScore;
                goalConfig.threeStarScore = data.goalSettings.threeStarScore;
            }

            EditorUtility.SetDirty(goalConfig);

            var levelConfig = EditorUtils.CreateScriptableObject<FunClass.Core.LevelConfig>(
                $"Assets/Configs/{data.levelName}/{data.levelName}_Config.asset"
            );
            
            levelConfig.levelGoal = goalConfig;
            EditorUtility.SetDirty(levelConfig);
            
            AssetDatabase.SaveAssets();
            
            return (goalConfig, levelConfig);
        }

        private static FunClass.Core.StudentConfig[] CreateStudentConfigsFromData(LevelDataSchema data)
        {
            if (data.students == null || data.students.Count == 0)
            {
                Debug.LogWarning("[JSONLevelImporter] No students in data");
                return new FunClass.Core.StudentConfig[0];
            }

            var configs = new FunClass.Core.StudentConfig[data.students.Count];

            for (int i = 0; i < data.students.Count; i++)
            {
                var studentData = data.students[i];
                Debug.Log($"[JSONLevelImporter] Creating config for student [{i}]: {studentData.studentName}");
                
                var config = EditorUtils.CreateScriptableObject<FunClass.Core.StudentConfig>(
                    $"Assets/Configs/{data.levelName}/Students/Student_{studentData.studentName}.asset"
                );

                config.studentId = $"student_{studentData.studentName.ToLower()}";
                config.studentName = studentData.studentName;
                config.initialState = FunClass.Core.StudentState.Calm;
                
                // Save immediately to persist studentName
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"[JSONLevelImporter] Config created: {config.name}, studentName: '{config.studentName}'");

                // Apply personality
                if (studentData.personality != null)
                {
                    config.patience = studentData.personality.patience;
                    config.attentionSpan = studentData.personality.attentionSpan;
                    config.impulsiveness = studentData.personality.impulsiveness;
                    config.influenceSusceptibility = studentData.personality.influenceSusceptibility;
                    config.influenceResistance = studentData.personality.influenceResistance;
                    config.panicThreshold = studentData.personality.panicThreshold;
                }

                // Apply behaviors
                if (studentData.behaviors != null)
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

                EditorUtility.SetDirty(config);
                configs[i] = config;
            }

            AssetDatabase.SaveAssets();
            return configs;
        }

        private static System.Collections.Generic.List<FunClass.Core.StudentRoute> CreateRoutesFromData(LevelDataSchema data)
        {
            var routes = new System.Collections.Generic.List<FunClass.Core.StudentRoute>();
            
            foreach (var routeData in data.routes)
            {
                Debug.Log($"[JSONLevelImporter] Creating route: '{routeData.routeName}'");
                
                var route = EditorUtils.CreateScriptableObject<FunClass.Core.StudentRoute>(
                    $"Assets/Configs/{data.levelName}/Routes/{routeData.routeName}.asset"
                );

                route.routeName = routeData.routeName;
                route.movementSpeed = routeData.movementSpeed;
                route.rotationSpeed = routeData.rotationSpeed;
                route.isRunning = routeData.isRunning;
                route.isLooping = routeData.isLooping;
                route.isPingPong = routeData.isPingPong;

                routes.Add(route);
                
                Debug.Log($"[JSONLevelImporter] Route created: '{route.routeName}', contains 'escape': {route.routeName.ToLower().Contains("escape")}");

                // Create waypoints in scene and assign to route
                var waypoints = CreateWaypointsFromData(routeData, data.levelName);
                if (waypoints != null && waypoints.Count > 0)
                {
                    route.waypoints = waypoints;
                    Debug.Log($"[JSONLevelImporter] Assigned {waypoints.Count} waypoints to route '{route.routeName}'");
                }
                
                EditorUtility.SetDirty(route);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[JSONLevelImporter] Returning {routes.Count} routes");
            return routes;
        }

        private static System.Collections.Generic.List<FunClass.Core.StudentWaypoint> CreateWaypointsFromData(RouteData routeData, string levelName)
        {
            var waypoints = new System.Collections.Generic.List<FunClass.Core.StudentWaypoint>();
            
            GameObject waypointsGroup = GameObject.Find("Waypoints");
            if (waypointsGroup == null)
            {
                Debug.LogWarning($"[JSONLevelImporter] Waypoints group not found in scene - cannot create waypoints for route '{routeData.routeName}'");
                return waypoints;
            }

            Debug.Log($"[JSONLevelImporter] Creating waypoints for route '{routeData.routeName}' - {routeData.waypoints.Count} waypoints");

            // Delete existing route group if it exists to prevent duplicates
            Transform existingRoute = waypointsGroup.transform.Find(routeData.routeName);
            if (existingRoute != null)
            {
                Debug.Log($"[JSONLevelImporter] Deleting existing route group '{routeData.routeName}'");
                UnityEngine.Object.DestroyImmediate(existingRoute.gameObject);
            }

            GameObject routeGroup = EditorUtils.CreateChild(waypointsGroup, routeData.routeName);

            foreach (var wpData in routeData.waypoints)
            {
                GameObject wpObj = new GameObject(wpData.waypointName);
                wpObj.transform.SetParent(routeGroup.transform);
                wpObj.transform.position = wpData.position.ToVector3();

                var waypoint = wpObj.AddComponent<FunClass.Core.StudentWaypoint>();
                waypoint.waypointName = wpData.waypointName;
                waypoint.waitDuration = wpData.waitDuration;
                
                waypoints.Add(waypoint);
                
                Debug.Log($"[JSONLevelImporter] Created waypoint '{wpData.waypointName}' at {wpData.position.ToVector3()}");
            }
            
            return waypoints;
        }

        /// <summary>
        /// Bakes NavMesh for student navigation
        /// </summary>
        private static void BakeNavMesh()
        {
            // Find Ground object with NavMeshSurface
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                Debug.LogWarning("[JSONLevelImporter] Ground object not found - cannot bake NavMesh");
                return;
            }

            // Get NavMeshSurface component using reflection
            var navMeshSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (navMeshSurfaceType == null)
            {
                Debug.LogWarning("[JSONLevelImporter] NavMeshSurface type not found - install AI Navigation package");
                return;
            }

            var navMeshSurface = ground.GetComponent(navMeshSurfaceType);
            if (navMeshSurface == null)
            {
                Debug.LogWarning("[JSONLevelImporter] NavMeshSurface component not found on Ground");
                return;
            }

            // Bake NavMesh using reflection
            var buildNavMeshMethod = navMeshSurfaceType.GetMethod("BuildNavMesh");
            if (buildNavMeshMethod != null)
            {
                buildNavMeshMethod.Invoke(navMeshSurface, null);
                Debug.Log("[JSONLevelImporter] ✓ NavMesh baked successfully!");
            }
            else
            {
                Debug.LogWarning("[JSONLevelImporter] BuildNavMesh method not found");
            }
        }
    }
}

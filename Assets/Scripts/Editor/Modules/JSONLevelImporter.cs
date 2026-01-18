using UnityEngine;
using UnityEditor;
using System.IO;
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
                EditorUtility.DisplayProgressBar("Import Level", "Creating student configs...", 0.5f);

                // 4. Create routes
                if (data.routes != null && data.routes.Count > 0)
                {
                    CreateRoutesFromData(data);
                    EditorUtility.DisplayProgressBar("Import Level", "Creating routes...", 0.7f);
                }

                // 5. Create scene hierarchy
                SceneHierarchyBuilder.CreateManagersGroup();
                SceneHierarchyBuilder.CreateClassroomGroup();
                SceneHierarchyBuilder.CreateTeacherGroup();
                SceneHierarchyBuilder.CreateUIGroup();
                SceneHierarchyBuilder.CreateStudentsGroup(data.students.Count, studentConfigs, data.levelName);
                EditorUtility.DisplayProgressBar("Import Level", "Creating scene...", 0.9f);

                // 6. Create prefabs if specified
                if (data.prefabs != null && data.prefabs.Count > 0)
                {
                    PrefabGenerator.CreatePrefabsFromData(data.prefabs);
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
                var config = EditorUtils.CreateScriptableObject<FunClass.Core.StudentConfig>(
                    $"Assets/Configs/{data.levelName}/Students/Student_{studentData.studentName}.asset"
                );

                config.studentId = $"student_{studentData.studentName.ToLower()}";
                config.studentName = studentData.studentName;
                config.initialState = FunClass.Core.StudentState.Calm;

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

        private static void CreateRoutesFromData(LevelDataSchema data)
        {
            foreach (var routeData in data.routes)
            {
                var route = EditorUtils.CreateScriptableObject<FunClass.Core.StudentRoute>(
                    $"Assets/Configs/{data.levelName}/Routes/{routeData.routeName}.asset"
                );

                route.routeName = routeData.routeName;
                route.movementSpeed = routeData.movementSpeed;
                route.rotationSpeed = routeData.rotationSpeed;
                route.isRunning = routeData.isRunning;
                route.isLooping = routeData.isLooping;
                route.isPingPong = routeData.isPingPong;

                EditorUtility.SetDirty(route);

                // Create waypoints in scene
                CreateWaypointsFromData(routeData, data.levelName);
            }

            AssetDatabase.SaveAssets();
        }

        private static void CreateWaypointsFromData(RouteData routeData, string levelName)
        {
            GameObject waypointsGroup = GameObject.Find("Waypoints");
            if (waypointsGroup == null) return;

            GameObject routeGroup = EditorUtils.CreateChild(waypointsGroup, routeData.routeName);

            foreach (var wpData in routeData.waypoints)
            {
                GameObject wpObj = new GameObject(wpData.waypointName);
                wpObj.transform.SetParent(routeGroup.transform);
                wpObj.transform.position = wpData.position.ToVector3();

                var waypoint = wpObj.AddComponent<FunClass.Core.StudentWaypoint>();
                waypoint.waypointName = wpData.waypointName;
                waypoint.waitDuration = wpData.waitDuration;
            }
        }
    }
}

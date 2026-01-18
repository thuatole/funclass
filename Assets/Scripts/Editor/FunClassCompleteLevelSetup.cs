using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace FunClass.Editor
{
    /// <summary>
    /// Tạo màn chơi hoàn chỉnh với 1 click - bao gồm scene, configs, students, waypoints
    /// Menu: Tools > FunClass > Create Complete Level
    /// </summary>
    public class FunClassCompleteLevelSetup : EditorWindow
    {
        private string levelName = "Level_01";
        private int studentCount = 5;
        private LevelDifficulty difficulty = LevelDifficulty.Normal;
        private bool createWaypoints = true;
        private bool createSampleData = true;

        private enum LevelDifficulty
        {
            Easy,
            Normal,
            Hard
        }

        [MenuItem("Tools/FunClass/Create Complete Level")]
        public static void ShowWindow()
        {
            FunClassCompleteLevelSetup window = GetWindow<FunClassCompleteLevelSetup>("Tạo Màn Chơi");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("TẠO MÀN CHƠI HOÀN CHỈNH", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Level Settings
            GUILayout.Label("Cài Đặt Màn Chơi:", EditorStyles.boldLabel);
            levelName = EditorGUILayout.TextField("Tên Màn:", levelName);
            studentCount = EditorGUILayout.IntSlider("Số Học Sinh:", studentCount, 3, 10);
            difficulty = (LevelDifficulty)EditorGUILayout.EnumPopup("Độ Khó:", difficulty);
            
            GUILayout.Space(10);
            
            // Options
            GUILayout.Label("Tùy Chọn:", EditorStyles.boldLabel);
            createWaypoints = EditorGUILayout.Toggle("Tạo Waypoints & Routes", createWaypoints);
            createSampleData = EditorGUILayout.Toggle("Tạo Sample Data", createSampleData);

            GUILayout.Space(20);

            // Preview
            EditorGUILayout.HelpBox(
                $"Sẽ tạo:\n" +
                $"• Scene: {levelName}.unity\n" +
                $"• {studentCount} học sinh với configs\n" +
                $"• Level Config với độ khó {difficulty}\n" +
                $"• Waypoints & Routes (nếu chọn)\n" +
                $"• Sample interaction data (nếu chọn)",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Create Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("TẠO MÀN CHƠI HOÀN CHỈNH", GUILayout.Height(40)))
            {
                CreateCompleteLevel();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            // Quick Templates
            GUILayout.Label("Templates Nhanh:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Màn Dễ (3 học sinh)"))
            {
                levelName = "Level_Easy";
                studentCount = 3;
                difficulty = LevelDifficulty.Easy;
            }
            
            if (GUILayout.Button("Màn Thường (5 học sinh)"))
            {
                levelName = "Level_Normal";
                studentCount = 5;
                difficulty = LevelDifficulty.Normal;
            }
            
            if (GUILayout.Button("Màn Khó (8 học sinh)"))
            {
                levelName = "Level_Hard";
                studentCount = 8;
                difficulty = LevelDifficulty.Hard;
            }
        }

        private void CreateCompleteLevel()
        {
            if (string.IsNullOrEmpty(levelName))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng nhập tên màn chơi!", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Đang khởi tạo...", 0f);

            try
            {
                // 1. Create folders
                CreateFolderStructure();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo folders...", 0.1f);

                // 2. Create new scene
                CreateNewScene();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo scene...", 0.2f);

                // 3. Create scene hierarchy
                FunClassSceneSetup.SetupScene();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo hierarchy...", 0.3f);

                // 4. Create ScriptableObject configs
                CreateLevelConfigs();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo configs...", 0.5f);

                // 5. Create student configs
                CreateStudentConfigs();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo student configs...", 0.6f);

                // 6. Setup students in scene
                SetupStudentsInScene();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Setup students...", 0.7f);

                // 7. Create waypoints & routes
                if (createWaypoints)
                {
                    CreateWaypointsAndRoutes();
                    EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo waypoints...", 0.8f);
                }

                // 8. Assign configs to managers
                AssignConfigsToManagers();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Assign configs...", 0.9f);

                // 9. Save scene
                SaveScene();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Lưu scene...", 1f);

                EditorUtility.ClearProgressBar();
                
                EditorUtility.DisplayDialog(
                    "Thành Công!", 
                    $"Đã tạo màn chơi '{levelName}' hoàn chỉnh!\n\n" +
                    $"Scene: Assets/Scenes/{levelName}.unity\n" +
                    $"Configs: Assets/Configs/{levelName}/\n\n" +
                    $"Bạn có thể chơi thử ngay bây giờ!",
                    "OK"
                );

                Debug.Log($"[LevelSetup] ✅ Tạo màn chơi '{levelName}' thành công!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Lỗi", $"Có lỗi xảy ra:\n{e.Message}", "OK");
                Debug.LogError($"[LevelSetup] Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private void CreateFolderStructure()
        {
            // Create main folders
            CreateFolderIfNotExists("Assets/Scenes");
            CreateFolderIfNotExists("Assets/Configs");
            CreateFolderIfNotExists($"Assets/Configs/{levelName}");
            CreateFolderIfNotExists($"Assets/Configs/{levelName}/Students");
            CreateFolderIfNotExists($"Assets/Configs/{levelName}/Routes");
            CreateFolderIfNotExists("Assets/Prefabs");
        }

        private void CreateNewScene()
        {
            // Create new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Remove default objects
            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null) DestroyImmediate(mainCamera);
            
            GameObject directionalLight = GameObject.Find("Directional Light");
            if (directionalLight != null) DestroyImmediate(directionalLight);
        }

        private void CreateLevelConfigs()
        {
            // Create LevelGoalConfig
            var goalConfig = CreateScriptableObject<FunClass.Core.LevelGoalConfig>(
                $"Assets/Configs/{levelName}/{levelName}_Goal.asset"
            );
            
            // Set difficulty-based values
            switch (difficulty)
            {
                case LevelDifficulty.Easy:
                    goalConfig.maxDisruptionThreshold = 90f;
                    goalConfig.catastrophicDisruptionLevel = 100f;
                    goalConfig.maxAllowedCriticalStudents = 3;
                    goalConfig.catastrophicCriticalStudents = 5;
                    goalConfig.maxAllowedOutsideStudents = 3;
                    goalConfig.catastrophicOutsideStudents = 6;
                    goalConfig.timeLimitSeconds = 600f;
                    goalConfig.requiredResolvedProblems = 3;
                    goalConfig.oneStarScore = 50;
                    goalConfig.twoStarScore = 150;
                    goalConfig.threeStarScore = 300;
                    break;
                    
                case LevelDifficulty.Normal:
                    goalConfig.maxDisruptionThreshold = 80f;
                    goalConfig.catastrophicDisruptionLevel = 95f;
                    goalConfig.maxAllowedCriticalStudents = 2;
                    goalConfig.catastrophicCriticalStudents = 4;
                    goalConfig.maxAllowedOutsideStudents = 2;
                    goalConfig.catastrophicOutsideStudents = 5;
                    goalConfig.timeLimitSeconds = 300f;
                    goalConfig.requiredResolvedProblems = 5;
                    goalConfig.oneStarScore = 100;
                    goalConfig.twoStarScore = 250;
                    goalConfig.threeStarScore = 500;
                    break;
                    
                case LevelDifficulty.Hard:
                    goalConfig.maxDisruptionThreshold = 70f;
                    goalConfig.catastrophicDisruptionLevel = 90f;
                    goalConfig.maxAllowedCriticalStudents = 1;
                    goalConfig.catastrophicCriticalStudents = 3;
                    goalConfig.maxAllowedOutsideStudents = 1;
                    goalConfig.catastrophicOutsideStudents = 3;
                    goalConfig.timeLimitSeconds = 180f;
                    goalConfig.requiredResolvedProblems = 8;
                    goalConfig.oneStarScore = 150;
                    goalConfig.twoStarScore = 400;
                    goalConfig.threeStarScore = 800;
                    break;
            }
            
            EditorUtility.SetDirty(goalConfig);
            
            // Create LevelConfig
            var levelConfig = CreateScriptableObject<FunClass.Core.LevelConfig>(
                $"Assets/Configs/{levelName}/{levelName}_Config.asset"
            );
            
            levelConfig.levelGoal = goalConfig;
            EditorUtility.SetDirty(levelConfig);
            
            AssetDatabase.SaveAssets();
        }

        private void CreateStudentConfigs()
        {
            string[] studentNames = { "Nam", "Lan", "Minh", "Hoa", "Tuan", "Mai", "Khoa", "Linh", "Duc", "Nga" };
            
            for (int i = 0; i < studentCount; i++)
            {
                var config = CreateScriptableObject<FunClass.Core.StudentConfig>(
                    $"Assets/Configs/{levelName}/Students/Student_{studentNames[i]}.asset"
                );
                
                config.studentId = $"student_{studentNames[i].ToLower()}";
                config.studentName = studentNames[i];
                config.initialState = FunClass.Core.StudentState.Calm;
                
                // Randomize personality based on difficulty
                float difficultyMultiplier = difficulty == LevelDifficulty.Easy ? 0.7f : 
                                            difficulty == LevelDifficulty.Normal ? 1f : 1.3f;
                
                // Personality parameters
                config.patience = Random.Range(0.3f, 0.7f) / difficultyMultiplier;
                config.attentionSpan = Random.Range(0.3f, 0.7f) / difficultyMultiplier;
                config.impulsiveness = Random.Range(0.3f, 0.7f) * difficultyMultiplier;
                
                // Autonomous behaviors (more allowed in harder difficulties)
                config.canFidget = true;
                config.canLookAround = true;
                config.canStandUp = difficulty != LevelDifficulty.Easy;
                config.canMoveAround = difficulty == LevelDifficulty.Hard;
                
                // Object interactions (more allowed in harder difficulties)
                config.canDropItems = difficulty != LevelDifficulty.Easy;
                config.canKnockOverObjects = difficulty == LevelDifficulty.Hard || (difficulty == LevelDifficulty.Normal && Random.value > 0.5f);
                config.canMakeNoiseWithObjects = true;
                config.canThrowObjects = difficulty == LevelDifficulty.Hard;
                config.canTouchObjects = true;
                config.interactionRange = 2f;
                
                // Behavior timing
                config.minIdleTime = difficulty == LevelDifficulty.Easy ? 3f : difficulty == LevelDifficulty.Normal ? 2f : 1f;
                config.maxIdleTime = difficulty == LevelDifficulty.Easy ? 10f : difficulty == LevelDifficulty.Normal ? 8f : 5f;
                
                // State-based interaction chances (higher in harder difficulties)
                config.calmInteractionChance = 0.1f * difficultyMultiplier;
                config.distractedInteractionChance = 0.3f * difficultyMultiplier;
                config.actingOutInteractionChance = 0.6f * difficultyMultiplier;
                config.criticalInteractionChance = 0.9f * difficultyMultiplier;
                
                // Influence settings
                config.influenceSusceptibility = Random.Range(0.5f, 0.9f) * difficultyMultiplier;
                config.influenceResistance = Random.Range(0.1f, 0.5f) / difficultyMultiplier;
                config.panicThreshold = Random.Range(0.6f, 0.8f) / difficultyMultiplier;
                
                EditorUtility.SetDirty(config);
            }
            
            AssetDatabase.SaveAssets();
        }

        private void SetupStudentsInScene()
        {
            GameObject studentsGroup = GameObject.Find("=== STUDENTS ===");
            if (studentsGroup == null) return;
            
            // Delete default students
            for (int i = studentsGroup.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(studentsGroup.transform.GetChild(i).gameObject);
            }
            
            // Create new students
            string[] studentNames = { "Nam", "Lan", "Minh", "Hoa", "Tuan", "Mai", "Khoa", "Linh", "Duc", "Nga" };
            
            for (int i = 0; i < studentCount; i++)
            {
                GameObject student = new GameObject($"Student_{studentNames[i]}");
                student.transform.SetParent(studentsGroup.transform);
                
                // Position in grid
                int row = i / 3;
                int col = i % 3;
                student.transform.position = new Vector3(col * 2f - 2f, 0, -row * 2f);
                
                // Add StudentAgent
                var agent = student.AddComponent<FunClass.Core.StudentAgent>();
                
                // Load and assign config
                var config = AssetDatabase.LoadAssetAtPath<FunClass.Core.StudentConfig>(
                    $"Assets/Configs/{levelName}/Students/Student_{studentNames[i]}.asset"
                );
                
                if (config != null)
                {
                    // Use reflection to set config (since it might be private)
                    var field = typeof(FunClass.Core.StudentAgent).GetField("config", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(agent, config);
                    }
                }
                
                // Add visual
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(student.transform);
                visual.transform.localPosition = Vector3.zero;
                
                // Random color for easy identification
                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                    renderer.material = mat;
                }
            }
        }

        private void CreateWaypointsAndRoutes()
        {
            GameObject waypointsGroup = GameObject.Find("Waypoints");
            if (waypointsGroup == null) return;
            
            // Create Escape Route
            CreateEscapeRoute(waypointsGroup);
            
            // Create Return Route
            CreateReturnRoute(waypointsGroup);
        }

        private void CreateEscapeRoute(GameObject parent)
        {
            GameObject escapeGroup = parent.transform.Find("EscapeRoute")?.gameObject;
            if (escapeGroup == null) return;
            
            // Create waypoints
            Vector3[] positions = new Vector3[]
            {
                new Vector3(0, 0, 0),      // Start (classroom)
                new Vector3(5, 0, 0),      // Middle
                new Vector3(10, 0, 0)      // Door/Outside
            };
            
            List<FunClass.Core.StudentWaypoint> waypoints = new List<FunClass.Core.StudentWaypoint>();
            
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject wpObj = new GameObject($"Waypoint_{i}");
                wpObj.transform.SetParent(escapeGroup.transform);
                wpObj.transform.position = positions[i];
                
                var waypoint = wpObj.AddComponent<FunClass.Core.StudentWaypoint>();
                waypoint.waypointName = $"Escape_{i}";
                waypoints.Add(waypoint);
            }
            
            // Create route ScriptableObject
            var route = CreateScriptableObject<FunClass.Core.StudentRoute>(
                $"Assets/Configs/{levelName}/Routes/EscapeRoute.asset"
            );
            
            route.routeName = "EscapeRoute";
            route.isRunning = true;
            route.movementSpeed = 4f;
            
            EditorUtility.SetDirty(route);
            AssetDatabase.SaveAssets();
        }

        private void CreateReturnRoute(GameObject parent)
        {
            GameObject returnGroup = parent.transform.Find("ReturnRoute")?.gameObject;
            if (returnGroup == null) return;
            
            // Create waypoints (reverse of escape)
            Vector3[] positions = new Vector3[]
            {
                new Vector3(10, 0, 0),     // Start (outside)
                new Vector3(5, 0, 0),      // Middle
                new Vector3(0, 0, 0)       // Classroom
            };
            
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject wpObj = new GameObject($"Waypoint_{i}");
                wpObj.transform.SetParent(returnGroup.transform);
                wpObj.transform.position = positions[i];
                
                var waypoint = wpObj.AddComponent<FunClass.Core.StudentWaypoint>();
                waypoint.waypointName = $"Return_{i}";
            }
            
            // Create route ScriptableObject
            var route = CreateScriptableObject<FunClass.Core.StudentRoute>(
                $"Assets/Configs/{levelName}/Routes/ReturnRoute.asset"
            );
            
            route.routeName = "ReturnRoute";
            route.movementSpeed = 2f;
            
            EditorUtility.SetDirty(route);
            AssetDatabase.SaveAssets();
        }

        private void AssignConfigsToManagers()
        {
            // Find LevelManager
            var levelManager = GameObject.Find("LevelManager")?.GetComponent<FunClass.Core.LevelManager>();
            
            // Load level config
            var levelConfig = AssetDatabase.LoadAssetAtPath<FunClass.Core.LevelConfig>(
                $"Assets/Configs/{levelName}/{levelName}_Config.asset"
            );
            
            // Assign escape and return routes to level config
            var escapeRoute = AssetDatabase.LoadAssetAtPath<FunClass.Core.StudentRoute>(
                $"Assets/Configs/{levelName}/Routes/EscapeRoute.asset"
            );
            var returnRoute = AssetDatabase.LoadAssetAtPath<FunClass.Core.StudentRoute>(
                $"Assets/Configs/{levelName}/Routes/ReturnRoute.asset"
            );
            
            if (levelConfig != null)
            {
                levelConfig.escapeRoute = escapeRoute;
                levelConfig.returnRoute = returnRoute;
                
                // Set door reference
                GameObject door = GameObject.Find("Door");
                if (door != null)
                {
                    levelConfig.classroomDoor = door.transform;
                }
                
                EditorUtility.SetDirty(levelConfig);
                AssetDatabase.SaveAssets();
            }
        }

        private void SaveScene()
        {
            string scenePath = $"Assets/Scenes/{levelName}.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private T CreateScriptableObject<T>(string path) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private void CreateFolderIfNotExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }
    }
}

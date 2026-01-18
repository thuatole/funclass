using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FunClass.Editor.Modules;

namespace FunClass.Editor
{
    /// <summary>
    /// Wrapper tự động tạo complete level với one-click
    /// Menu: Tools > FunClass > Generate Complete Level
    /// </summary>
    public class FullAutoLevelGenerator : EditorWindow
    {
        private string levelName = "AutoLevel_01";
        private LevelConfigGenerator.Difficulty difficulty = LevelConfigGenerator.Difficulty.Normal;
        private bool generateInteractables = true;
        private bool generateMessPrefabs = true;
        private bool generateSequences = true;
        private bool generateRoutes = true;

        // Advanced options
        private bool showAdvancedOptions = false;
        private int customStudentCount = 0; // 0 = auto based on difficulty
        private int customInteractableCount = 0; // 0 = auto
        private bool createTutorialLevel = false;
        private bool createBossLevel = false;

        [MenuItem("Tools/FunClass/Generate Complete Level")]
        public static void ShowWindow()
        {
            FullAutoLevelGenerator window = GetWindow<FullAutoLevelGenerator>("Auto Level Generator");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("FULL AUTO LEVEL GENERATOR", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "One-click tạo level hoàn chỉnh với:\n" +
                "• Scene hierarchy\n" +
                "• Level configs\n" +
                "• Students\n" +
                "• Routes\n" +
                "• Interactable objects\n" +
                "• Mess prefabs\n" +
                "• Sample sequences",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Basic settings
            GUILayout.Label("Basic Settings", EditorStyles.boldLabel);
            levelName = EditorGUILayout.TextField("Level Name:", levelName);
            difficulty = (LevelConfigGenerator.Difficulty)EditorGUILayout.EnumPopup("Difficulty:", difficulty);

            GUILayout.Space(10);

            // Quick presets
            GUILayout.Label("Quick Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Tutorial Level"))
            {
                SetupTutorialPreset();
            }
            
            if (GUILayout.Button("Boss Level"))
            {
                SetupBossPreset();
            }
            
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Generation options
            GUILayout.Label("Generation Options", EditorStyles.boldLabel);
            generateRoutes = EditorGUILayout.Toggle("Generate Routes", generateRoutes);
            generateInteractables = EditorGUILayout.Toggle("Generate Interactables", generateInteractables);
            generateMessPrefabs = EditorGUILayout.Toggle("Generate Mess Prefabs", generateMessPrefabs);
            generateSequences = EditorGUILayout.Toggle("Generate Sequences", generateSequences);

            GUILayout.Space(10);

            // Advanced options
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                
                customStudentCount = EditorGUILayout.IntField("Custom Student Count (0=auto):", customStudentCount);
                customInteractableCount = EditorGUILayout.IntField("Custom Interactable Count (0=auto):", customInteractableCount);
                
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(20);

            // Preview
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(GetPreviewText(), MessageType.None);

            GUILayout.Space(10);

            // Generate button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("GENERATE COMPLETE LEVEL", GUILayout.Height(50)))
            {
                GenerateCompleteLevel();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);

            // Quick generate buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Quick Easy", GUILayout.Height(30)))
            {
                QuickGenerate(LevelConfigGenerator.Difficulty.Easy);
            }
            
            if (GUILayout.Button("Quick Normal", GUILayout.Height(30)))
            {
                QuickGenerate(LevelConfigGenerator.Difficulty.Normal);
            }
            
            if (GUILayout.Button("Quick Hard", GUILayout.Height(30)))
            {
                QuickGenerate(LevelConfigGenerator.Difficulty.Hard);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateCompleteLevel()
        {
            if (string.IsNullOrEmpty(levelName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter level name", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Generating Level", "Starting...", 0f);

            var startTime = System.DateTime.Now;
            var report = new LevelGenerationReport
            {
                LevelName = levelName,
                Difficulty = difficulty.ToString()
            };

            try
            {
                // Determine if special level type
                LevelConfigGenerator.Difficulty actualDifficulty = difficulty;
                
                if (createTutorialLevel)
                {
                    GenerateTutorialLevel();
                    return;
                }
                
                if (createBossLevel)
                {
                    GenerateBossLevel();
                    return;
                }

                // 1. Create scene hierarchy
                EditorUtility.DisplayProgressBar("Generating Level", "Creating scene hierarchy...", 0.1f);
                SceneHierarchyBuilder.CreateCompleteHierarchy();
                
                report.HierarchyGroups.Add("=== MANAGERS ===");
                report.HierarchyGroups.Add("=== CLASSROOM ===");
                report.HierarchyGroups.Add("=== TEACHER ===");
                report.HierarchyGroups.Add("=== STUDENTS ===");
                report.HierarchyGroups.Add("=== UI ===");
                report.ManagerCount = 7;

                // 2. Generate level goal
                EditorUtility.DisplayProgressBar("Generating Level", "Generating level goals...", 0.2f);
                var goalConfig = LevelGoalGenerator.GenerateLevelGoal(levelName, actualDifficulty);
                
                report.GoalConfigPath = $"Assets/Configs/{levelName}/{levelName}_Goal.asset";
                report.MaxDisruption = goalConfig.maxDisruptionThreshold;
                report.TimeLimit = goalConfig.timeLimitSeconds;
                report.MinStudentsSeated = goalConfig.requiredCalmDowns;

                // 3. Generate students
                EditorUtility.DisplayProgressBar("Generating Level", "Generating students...", 0.3f);
                int studentCount = customStudentCount > 0 ? customStudentCount : 0;
                List<FunClass.Core.StudentConfig> studentConfigs;
                
                if (studentCount > 0)
                {
                    studentConfigs = StudentGenerator.GenerateStudents(levelName, studentCount, actualDifficulty);
                }
                else
                {
                    studentConfigs = StudentGenerator.GenerateStudents(levelName, actualDifficulty);
                }
                
                report.StudentCount = studentConfigs.Count;
                foreach (var config in studentConfigs)
                {
                    report.StudentConfigPaths.Add($"Assets/Configs/{levelName}/Students/{config.name}.asset");
                    // Use personality description instead of archetype
                    string personality = $"Patience: {config.patience:F1}, Impulsiveness: {config.impulsiveness:F1}";
                    report.StudentArchetypes[config.studentName] = personality;
                }

                // 4. Generate routes
                List<FunClass.Core.StudentRoute> routes = new List<FunClass.Core.StudentRoute>();
                if (generateRoutes)
                {
                    EditorUtility.DisplayProgressBar("Generating Level", "Generating routes...", 0.4f);
                    routes = WaypointRouteBuilder.CreateDefaultRoutes(levelName);
                    
                    report.RouteCount = routes.Count;
                    int totalWaypoints = 0;
                    foreach (var route in routes)
                    {
                        int waypointCount = route.waypoints != null ? route.waypoints.Count : 0;
                        totalWaypoints += waypointCount;
                        report.RouteWaypoints[route.name] = waypointCount;
                        report.RoutePaths.Add($"Assets/Configs/{levelName}/Routes/{route.name}.asset");
                    }
                    report.WaypointCount = totalWaypoints;
                }

                // 5. Generate sequences (before LevelConfig so we can assign them)
                List<FunClass.Core.StudentSequenceConfig> sequences = new List<FunClass.Core.StudentSequenceConfig>();
                if (generateSequences)
                {
                    EditorUtility.DisplayProgressBar("Generating Level", "Generating sequences...", 0.5f);
                    sequences = SequenceGenerator.CreateSampleSequences(levelName, actualDifficulty);
                    
                    report.SequenceCount = sequences.Count;
                    foreach (var seq in sequences)
                    {
                        report.SequencePaths.Add($"Assets/Configs/{levelName}/Sequences/{seq.name}.asset");
                    }
                }

                // 6. Create level config (with sequences)
                EditorUtility.DisplayProgressBar("Generating Level", "Creating level config...", 0.6f);
                var levelConfig = LevelConfigGenerator.CreateLevelConfig(
                    levelName, 
                    actualDifficulty, 
                    goalConfig, 
                    studentConfigs, 
                    routes,
                    sequences
                );
                
                report.LevelConfigPath = $"Assets/Configs/{levelName}/{levelName}_Config.asset";

                // 7. Generate interactable objects
                if (generateInteractables)
                {
                    EditorUtility.DisplayProgressBar("Generating Level", "Generating interactable objects...", 0.7f);
                    int interactableCount = customInteractableCount > 0 ? customInteractableCount : 0;
                    
                    if (interactableCount > 0)
                    {
                        InteractableObjectGenerator.CreateInteractableObjects(interactableCount, levelName);
                        report.InteractableCount = interactableCount;
                    }
                    else
                    {
                        InteractableObjectGenerator.CreateInteractableSetByDifficulty(actualDifficulty);
                        report.InteractableCount = 5; // Default count
                    }
                }

                // 8. Generate mess prefabs
                if (generateMessPrefabs)
                {
                    EditorUtility.DisplayProgressBar("Generating Level", "Generating mess prefabs...", 0.8f);
                    MessPrefabGenerator.CreateMessPrefabs(levelName);
                    report.MessPrefabCount = 6; // Standard mess types
                }

                // 9. Create student GameObjects in scene
                EditorUtility.DisplayProgressBar("Generating Level", "Creating student objects...", 0.9f);
                SceneHierarchyBuilder.CreateStudents(studentConfigs);

                // 10. Assign configs to managers
                EditorUtility.DisplayProgressBar("Generating Level", "Assigning configs...", 0.95f);
                AssignConfigsToManagers(levelConfig);

                EditorUtility.DisplayProgressBar("Generating Level", "Complete!", 1f);

                // Save scene
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Validate level
                EditorUtility.DisplayProgressBar("Validating Level", "Running validation checks...", 0.5f);
                var validationResult = LevelValidator.ValidateLevel(levelConfig);
                
                // Update report with validation results
                report.ValidationPassed = validationResult.isValid;
                report.ValidationErrors = validationResult.errors.Count;
                report.ValidationWarnings = validationResult.warnings.Count;
                
                foreach (var error in validationResult.errors)
                {
                    report.ValidationMessages.Add($"❌ {error}");
                }
                foreach (var warning in validationResult.warnings)
                {
                    report.ValidationMessages.Add($"⚠️ {warning}");
                }
                
                // Calculate generation time
                var endTime = System.DateTime.Now;
                report.GenerationTime = (float)(endTime - startTime).TotalSeconds;
                
                EditorUtility.ClearProgressBar();

                // Log comprehensive report
                Debug.Log(report.GetFormattedReport());
                Debug.Log(validationResult.GetReport());

                // Show success dialog with summary
                string validationStatus = validationResult.isValid ? "✅ VALIDATED" : "⚠️ HAS ISSUES";
                
                EditorUtility.DisplayDialog($"Level Generated - {validationStatus}", 
                    report.GetSummary(),
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Failed to generate level:\n{e.Message}", "OK");
                Debug.LogError($"[FullAutoLevelGenerator] Error: {e}");
            }
        }

        private void GenerateTutorialLevel()
        {
            EditorUtility.DisplayProgressBar("Generating Tutorial", "Creating tutorial level...", 0.5f);

            SceneHierarchyBuilder.CreateCompleteHierarchy();
            var goalConfig = LevelGoalGenerator.GenerateTutorialGoal(levelName);
            var studentConfigs = StudentGenerator.GenerateStudents(levelName, 3, LevelConfigGenerator.Difficulty.Easy);
            var routes = WaypointRouteBuilder.CreateDefaultRoutes(levelName);
            var levelConfig = LevelConfigGenerator.CreateLevelConfig(levelName, LevelConfigGenerator.Difficulty.Easy, goalConfig, studentConfigs, routes);
            
            InteractableObjectGenerator.CreateInteractableObjects(2, levelName);
            SequenceGenerator.CreateSampleSequences(levelName, LevelConfigGenerator.Difficulty.Easy);
            
            SceneHierarchyBuilder.CreateStudents(studentConfigs);
            AssignConfigsToManagers(levelConfig);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Success!", $"Tutorial level '{levelName}' created!", "OK");
        }

        private void GenerateBossLevel()
        {
            EditorUtility.DisplayProgressBar("Generating Boss Level", "Creating boss level...", 0.5f);

            SceneHierarchyBuilder.CreateCompleteHierarchy();
            var goalConfig = LevelGoalGenerator.GenerateBossGoal(levelName);
            var studentConfigs = StudentGenerator.GenerateStudents(levelName, 12, LevelConfigGenerator.Difficulty.Hard);
            var routes = WaypointRouteBuilder.CreateDefaultRoutes(levelName);
            var levelConfig = LevelConfigGenerator.CreateLevelConfig(levelName, LevelConfigGenerator.Difficulty.Hard, goalConfig, studentConfigs, routes);
            
            InteractableObjectGenerator.CreateInteractableObjects(10, levelName);
            MessPrefabGenerator.CreateMessPrefabs(levelName);
            SequenceGenerator.CreateSampleSequences(levelName, LevelConfigGenerator.Difficulty.Hard);
            
            SceneHierarchyBuilder.CreateStudents(studentConfigs);
            AssignConfigsToManagers(levelConfig);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Success!", $"Boss level '{levelName}' created!", "OK");
        }

        private void QuickGenerate(LevelConfigGenerator.Difficulty quickDifficulty)
        {
            levelName = $"QuickLevel_{quickDifficulty}_{System.DateTime.Now:HHmmss}";
            difficulty = quickDifficulty;
            generateInteractables = true;
            generateMessPrefabs = true;
            generateSequences = true;
            generateRoutes = true;
            customStudentCount = 0;
            customInteractableCount = 0;
            
            GenerateCompleteLevel();
        }

        private void SetupTutorialPreset()
        {
            levelName = "Tutorial_01";
            difficulty = LevelConfigGenerator.Difficulty.Easy;
            createTutorialLevel = true;
            createBossLevel = false;
            generateInteractables = true;
            generateMessPrefabs = false;
            generateSequences = true;
            generateRoutes = true;
            customStudentCount = 3;
        }

        private void SetupBossPreset()
        {
            levelName = "Boss_Final";
            difficulty = LevelConfigGenerator.Difficulty.Hard;
            createTutorialLevel = false;
            createBossLevel = true;
            generateInteractables = true;
            generateMessPrefabs = true;
            generateSequences = true;
            generateRoutes = true;
            customStudentCount = 12;
        }

        private void AssignConfigsToManagers(FunClass.Core.LevelConfig levelConfig)
        {
            // Find and assign to ClassroomManager
            var classroomManager = GameObject.FindObjectOfType<FunClass.Core.ClassroomManager>();
            if (classroomManager != null)
            {
                var so = new SerializedObject(classroomManager);
                so.FindProperty("levelConfig").objectReferenceValue = levelConfig;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(classroomManager);
            }
        }

        private string GetPreviewText()
        {
            int studentCount = customStudentCount > 0 ? customStudentCount : GetDefaultStudentCount(difficulty);
            int interactableCount = customInteractableCount > 0 ? customInteractableCount : GetDefaultInteractableCount(difficulty);
            int sequenceCount = GetDefaultSequenceCount(difficulty);

            return $"Level: {levelName}\n" +
                   $"Difficulty: {difficulty}\n" +
                   $"Students: {studentCount}\n" +
                   $"Routes: {(generateRoutes ? "2 (Escape + Return)" : "None")}\n" +
                   $"Interactables: {(generateInteractables ? interactableCount.ToString() : "None")}\n" +
                   $"Mess Prefabs: {(generateMessPrefabs ? "6 types" : "None")}\n" +
                   $"Sequences: {(generateSequences ? sequenceCount.ToString() : "None")}";
        }

        private int GetDefaultStudentCount(LevelConfigGenerator.Difficulty diff)
        {
            return diff switch
            {
                LevelConfigGenerator.Difficulty.Easy => 5,
                LevelConfigGenerator.Difficulty.Normal => 8,
                LevelConfigGenerator.Difficulty.Hard => 10,
                _ => 8
            };
        }

        private int GetDefaultInteractableCount(LevelConfigGenerator.Difficulty diff)
        {
            return diff switch
            {
                LevelConfigGenerator.Difficulty.Easy => 3,
                LevelConfigGenerator.Difficulty.Normal => 5,
                LevelConfigGenerator.Difficulty.Hard => 8,
                _ => 5
            };
        }

        private int GetDefaultSequenceCount(LevelConfigGenerator.Difficulty diff)
        {
            return diff switch
            {
                LevelConfigGenerator.Difficulty.Easy => 3,
                LevelConfigGenerator.Difficulty.Normal => 5,
                LevelConfigGenerator.Difficulty.Hard => 8,
                _ => 5
            };
        }
    }
}

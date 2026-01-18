using UnityEngine;
using UnityEditor;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tạo các ScriptableObject configs cho level
    /// </summary>
    public static class LevelConfigGenerator
    {
        public enum Difficulty
        {
            Easy,
            Normal,
            Hard
        }

        /// <summary>
        /// Tạo LevelGoalConfig và LevelConfig
        /// </summary>
        public static (FunClass.Core.LevelGoalConfig goalConfig, FunClass.Core.LevelConfig levelConfig) 
            CreateLevelConfigs(string levelName, Difficulty difficulty)
        {
            // Create LevelGoalConfig
            var goalConfig = EditorUtils.CreateScriptableObject<FunClass.Core.LevelGoalConfig>(
                $"Assets/Configs/{levelName}/{levelName}_Goal.asset"
            );
            
            ApplyDifficultySettings(goalConfig, difficulty);
            EditorUtility.SetDirty(goalConfig);
            
            // Create LevelConfig
            var levelConfig = EditorUtils.CreateScriptableObject<FunClass.Core.LevelConfig>(
                $"Assets/Configs/{levelName}/{levelName}_Config.asset"
            );
            
            levelConfig.levelGoal = goalConfig;
            EditorUtility.SetDirty(levelConfig);
            
            AssetDatabase.SaveAssets();
            
            return (goalConfig, levelConfig);
        }

        /// <summary>
        /// Tạo LevelConfig với các parameters đã có
        /// </summary>
        public static FunClass.Core.LevelConfig CreateLevelConfig(
            string levelName,
            Difficulty difficulty,
            FunClass.Core.LevelGoalConfig goalConfig,
            System.Collections.Generic.List<FunClass.Core.StudentConfig> studentConfigs,
            System.Collections.Generic.List<FunClass.Core.StudentRoute> routes,
            System.Collections.Generic.List<FunClass.Core.StudentSequenceConfig> sequences = null)
        {
            // Create LevelConfig
            var levelConfig = EditorUtils.CreateScriptableObject<FunClass.Core.LevelConfig>(
                $"Assets/Configs/{levelName}/{levelName}_Config.asset"
            );
            
            // Assign goal config
            levelConfig.levelGoal = goalConfig;
            
            // Assign student configs
            if (studentConfigs != null && studentConfigs.Count > 0)
            {
                levelConfig.students = studentConfigs;
            }
            
            // Assign routes
            if (routes != null && routes.Count >= 2)
            {
                levelConfig.escapeRoute = routes[0];
                levelConfig.returnRoute = routes[1];
                levelConfig.availableRoutes = routes;
            }
            
            // Assign sequences
            if (sequences != null && sequences.Count > 0)
            {
                levelConfig.availableSequences = sequences;
                Debug.Log($"[LevelConfigGenerator] Assigned {sequences.Count} sequences to LevelConfig");
            }
            
            // Set door reference
            GameObject door = GameObject.Find("Door");
            if (door != null)
            {
                levelConfig.classroomDoor = door.transform;
            }
            
            EditorUtility.SetDirty(levelConfig);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[LevelConfigGenerator] Created LevelConfig for {levelName}");
            return levelConfig;
        }

        private static void ApplyDifficultySettings(FunClass.Core.LevelGoalConfig config, Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    config.maxDisruptionThreshold = 90f;
                    config.catastrophicDisruptionLevel = 100f;
                    config.maxAllowedCriticalStudents = 3;
                    config.catastrophicCriticalStudents = 5;
                    config.maxAllowedOutsideStudents = 3;
                    config.catastrophicOutsideStudents = 6;
                    config.maxOutsideTimePerStudent = 90f;
                    config.maxAllowedOutsideGracePeriod = 15f;
                    config.timeLimitSeconds = 600f;
                    config.requiredResolvedProblems = 3;
                    config.oneStarScore = 50;
                    config.twoStarScore = 150;
                    config.threeStarScore = 300;
                    break;
                    
                case Difficulty.Normal:
                    config.maxDisruptionThreshold = 80f;
                    config.catastrophicDisruptionLevel = 95f;
                    config.maxAllowedCriticalStudents = 2;
                    config.catastrophicCriticalStudents = 4;
                    config.maxAllowedOutsideStudents = 2;
                    config.catastrophicOutsideStudents = 5;
                    config.maxOutsideTimePerStudent = 60f;
                    config.maxAllowedOutsideGracePeriod = 10f;
                    config.timeLimitSeconds = 300f;
                    config.requiredResolvedProblems = 5;
                    config.oneStarScore = 100;
                    config.twoStarScore = 250;
                    config.threeStarScore = 500;
                    break;
                    
                case Difficulty.Hard:
                    config.maxDisruptionThreshold = 70f;
                    config.catastrophicDisruptionLevel = 90f;
                    config.maxAllowedCriticalStudents = 1;
                    config.catastrophicCriticalStudents = 3;
                    config.maxAllowedOutsideStudents = 1;
                    config.catastrophicOutsideStudents = 3;
                    config.maxOutsideTimePerStudent = 45f;
                    config.maxAllowedOutsideGracePeriod = 5f;
                    config.timeLimitSeconds = 180f;
                    config.requiredResolvedProblems = 8;
                    config.oneStarScore = 150;
                    config.twoStarScore = 400;
                    config.threeStarScore = 800;
                    break;
            }
        }
    }
}

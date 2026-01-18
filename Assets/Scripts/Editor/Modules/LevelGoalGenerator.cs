using UnityEngine;
using UnityEditor;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tự động tạo level goals theo difficulty
    /// </summary>
    public static class LevelGoalGenerator
    {
        /// <summary>
        /// Tạo LevelGoalConfig theo difficulty
        /// </summary>
        public static FunClass.Core.LevelGoalConfig GenerateLevelGoal(
            string levelName, 
            LevelConfigGenerator.Difficulty difficulty)
        {
            var goalConfig = ScriptableObject.CreateInstance<FunClass.Core.LevelGoalConfig>();

            // Configure based on difficulty
            switch (difficulty)
            {
                case LevelConfigGenerator.Difficulty.Easy:
                    ConfigureEasyGoal(goalConfig);
                    break;
                case LevelConfigGenerator.Difficulty.Normal:
                    ConfigureNormalGoal(goalConfig);
                    break;
                case LevelConfigGenerator.Difficulty.Hard:
                    ConfigureHardGoal(goalConfig);
                    break;
            }

            // Save as asset
            string folderPath = $"Assets/Configs/{levelName}";
            EditorUtils.CreateFolderIfNotExists(folderPath);
            
            string assetPath = $"{folderPath}/{levelName}_GoalConfig.asset";
            AssetDatabase.CreateAsset(goalConfig, assetPath);
            EditorUtility.SetDirty(goalConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelGoalGenerator] Generated goal config for {levelName} ({difficulty})");
            return goalConfig;
        }

        /// <summary>
        /// Tạo custom goal với parameters cụ thể
        /// </summary>
        public static FunClass.Core.LevelGoalConfig GenerateCustomGoal(
            string levelName,
            float maxDisruption,
            float catastrophicDisruption,
            int maxCriticalStudents,
            int maxOutsideStudents,
            float timeLimit,
            int requiredProblems,
            int oneStarScore,
            int twoStarScore,
            int threeStarScore)
        {
            var goalConfig = ScriptableObject.CreateInstance<FunClass.Core.LevelGoalConfig>();

            // Set custom values
            goalConfig.maxDisruptionThreshold = maxDisruption;
            goalConfig.catastrophicDisruptionLevel = catastrophicDisruption;
            goalConfig.maxAllowedCriticalStudents = maxCriticalStudents;
            goalConfig.catastrophicCriticalStudents = maxCriticalStudents + 2;
            goalConfig.maxAllowedOutsideStudents = maxOutsideStudents;
            goalConfig.catastrophicOutsideStudents = maxOutsideStudents + 3;
            goalConfig.maxOutsideTimePerStudent = 60f;
            goalConfig.maxAllowedOutsideGracePeriod = 10f;
            goalConfig.timeLimitSeconds = timeLimit;
            goalConfig.requiredResolvedProblems = requiredProblems;
            goalConfig.oneStarScore = oneStarScore;
            goalConfig.twoStarScore = twoStarScore;
            goalConfig.threeStarScore = threeStarScore;

            // Save
            string folderPath = $"Assets/Configs/{levelName}";
            EditorUtils.CreateFolderIfNotExists(folderPath);
            
            string assetPath = $"{folderPath}/{levelName}_GoalConfig.asset";
            AssetDatabase.CreateAsset(goalConfig, assetPath);
            EditorUtility.SetDirty(goalConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return goalConfig;
        }

        private static void ConfigureEasyGoal(FunClass.Core.LevelGoalConfig config)
        {
            // Lenient thresholds
            config.maxDisruptionThreshold = 90f;
            config.catastrophicDisruptionLevel = 100f;
            config.maxAllowedCriticalStudents = 3;
            config.catastrophicCriticalStudents = 5;
            config.maxAllowedOutsideStudents = 3;
            config.catastrophicOutsideStudents = 6;
            config.maxOutsideTimePerStudent = 90f;
            config.maxAllowedOutsideGracePeriod = 15f;
            
            // Generous time
            config.timeLimitSeconds = 600f; // 10 minutes
            
            // Low requirements
            config.requiredResolvedProblems = 3;
            
            // Easy star thresholds
            config.oneStarScore = 50;
            config.twoStarScore = 150;
            config.threeStarScore = 300;
        }

        private static void ConfigureNormalGoal(FunClass.Core.LevelGoalConfig config)
        {
            // Moderate thresholds
            config.maxDisruptionThreshold = 80f;
            config.catastrophicDisruptionLevel = 95f;
            config.maxAllowedCriticalStudents = 2;
            config.catastrophicCriticalStudents = 4;
            config.maxAllowedOutsideStudents = 2;
            config.catastrophicOutsideStudents = 5;
            config.maxOutsideTimePerStudent = 60f;
            config.maxAllowedOutsideGracePeriod = 10f;
            
            // Standard time
            config.timeLimitSeconds = 300f; // 5 minutes
            
            // Moderate requirements
            config.requiredResolvedProblems = 5;
            
            // Normal star thresholds
            config.oneStarScore = 100;
            config.twoStarScore = 250;
            config.threeStarScore = 500;
        }

        private static void ConfigureHardGoal(FunClass.Core.LevelGoalConfig config)
        {
            // Strict thresholds
            config.maxDisruptionThreshold = 60f;
            config.catastrophicDisruptionLevel = 80f;
            config.maxAllowedCriticalStudents = 1;
            config.catastrophicCriticalStudents = 2;
            config.maxAllowedOutsideStudents = 1;
            config.catastrophicOutsideStudents = 3;
            config.maxOutsideTimePerStudent = 30f;
            config.maxAllowedOutsideGracePeriod = 5f;
            
            // Limited time
            config.timeLimitSeconds = 180f; // 3 minutes
            
            // High requirements
            config.requiredResolvedProblems = 8;
            
            // Hard star thresholds
            config.oneStarScore = 150;
            config.twoStarScore = 400;
            config.threeStarScore = 800;
        }

        /// <summary>
        /// Generate goal for tutorial level
        /// </summary>
        public static FunClass.Core.LevelGoalConfig GenerateTutorialGoal(string levelName)
        {
            var goalConfig = ScriptableObject.CreateInstance<FunClass.Core.LevelGoalConfig>();

            // Very lenient for tutorial
            goalConfig.maxDisruptionThreshold = 100f;
            goalConfig.catastrophicDisruptionLevel = 100f;
            goalConfig.maxAllowedCriticalStudents = 5;
            goalConfig.catastrophicCriticalStudents = 10;
            goalConfig.maxAllowedOutsideStudents = 5;
            goalConfig.catastrophicOutsideStudents = 10;
            goalConfig.maxOutsideTimePerStudent = 120f;
            goalConfig.maxAllowedOutsideGracePeriod = 30f;
            
            // No time limit
            goalConfig.timeLimitSeconds = 0f;
            
            // Minimal requirements
            goalConfig.requiredResolvedProblems = 1;
            
            // Very easy stars
            goalConfig.oneStarScore = 10;
            goalConfig.twoStarScore = 50;
            goalConfig.threeStarScore = 100;

            // Save
            string folderPath = $"Assets/Configs/{levelName}";
            EditorUtils.CreateFolderIfNotExists(folderPath);
            
            string assetPath = $"{folderPath}/{levelName}_GoalConfig.asset";
            AssetDatabase.CreateAsset(goalConfig, assetPath);
            EditorUtility.SetDirty(goalConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelGoalGenerator] Generated tutorial goal config for {levelName}");
            return goalConfig;
        }

        /// <summary>
        /// Generate goal for boss level
        /// </summary>
        public static FunClass.Core.LevelGoalConfig GenerateBossGoal(string levelName)
        {
            var goalConfig = ScriptableObject.CreateInstance<FunClass.Core.LevelGoalConfig>();

            // Extremely strict for boss
            goalConfig.maxDisruptionThreshold = 40f;
            goalConfig.catastrophicDisruptionLevel = 60f;
            goalConfig.maxAllowedCriticalStudents = 0;
            goalConfig.catastrophicCriticalStudents = 1;
            goalConfig.maxAllowedOutsideStudents = 0;
            goalConfig.catastrophicOutsideStudents = 2;
            goalConfig.maxOutsideTimePerStudent = 15f;
            goalConfig.maxAllowedOutsideGracePeriod = 3f;
            
            // Very limited time
            goalConfig.timeLimitSeconds = 120f; // 2 minutes
            
            // Very high requirements
            goalConfig.requiredResolvedProblems = 12;
            
            // Boss star thresholds
            goalConfig.oneStarScore = 200;
            goalConfig.twoStarScore = 600;
            goalConfig.threeStarScore = 1200;

            // Save
            string folderPath = $"Assets/Configs/{levelName}";
            EditorUtils.CreateFolderIfNotExists(folderPath);
            
            string assetPath = $"{folderPath}/{levelName}_GoalConfig.asset";
            AssetDatabase.CreateAsset(goalConfig, assetPath);
            EditorUtility.SetDirty(goalConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelGoalGenerator] Generated boss goal config for {levelName}");
            return goalConfig;
        }

        /// <summary>
        /// Quick create sample goals
        /// </summary>
        [MenuItem("Tools/FunClass/Quick Create/Sample Goals")]
        public static void QuickCreateSampleGoals()
        {
            string folderPath = "Assets/Configs/Goals";
            EditorUtils.CreateFolderIfNotExists(folderPath);

            // Create one for each difficulty
            GenerateLevelGoal("Sample_Easy", LevelConfigGenerator.Difficulty.Easy);
            GenerateLevelGoal("Sample_Normal", LevelConfigGenerator.Difficulty.Normal);
            GenerateLevelGoal("Sample_Hard", LevelConfigGenerator.Difficulty.Hard);
            GenerateTutorialGoal("Sample_Tutorial");
            GenerateBossGoal("Sample_Boss");

            EditorUtility.DisplayDialog("Success", 
                "Created 5 sample goal configs:\n" +
                "- Easy\n- Normal\n- Hard\n- Tutorial\n- Boss", 
                "OK");
        }
    }
}

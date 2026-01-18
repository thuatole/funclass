using UnityEngine;
using UnityEditor;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tạo StudentConfig cho các học sinh
    /// </summary>
    public static class StudentConfigGenerator
    {
        private static readonly string[] DefaultStudentNames = 
            { "Nam", "Lan", "Minh", "Hoa", "Tuan", "Mai", "Khoa", "Linh", "Duc", "Nga" };

        /// <summary>
        /// Tạo student configs cho level
        /// </summary>
        public static FunClass.Core.StudentConfig[] CreateStudentConfigs(
            string levelName, 
            int studentCount, 
            LevelConfigGenerator.Difficulty difficulty)
        {
            var configs = new FunClass.Core.StudentConfig[studentCount];
            float difficultyMultiplier = GetDifficultyMultiplier(difficulty);
            
            for (int i = 0; i < studentCount; i++)
            {
                string studentName = DefaultStudentNames[i % DefaultStudentNames.Length];
                configs[i] = CreateSingleStudentConfig(levelName, studentName, difficulty, difficultyMultiplier);
            }
            
            AssetDatabase.SaveAssets();
            return configs;
        }

        private static FunClass.Core.StudentConfig CreateSingleStudentConfig(
            string levelName, 
            string studentName, 
            LevelConfigGenerator.Difficulty difficulty,
            float difficultyMultiplier)
        {
            var config = EditorUtils.CreateScriptableObject<FunClass.Core.StudentConfig>(
                $"Assets/Configs/{levelName}/Students/Student_{studentName}.asset"
            );
            
            config.studentId = $"student_{studentName.ToLower()}";
            config.studentName = studentName;
            config.initialState = FunClass.Core.StudentState.Calm;
            
            // Personality parameters
            config.patience = Random.Range(0.3f, 0.7f) / difficultyMultiplier;
            config.attentionSpan = Random.Range(0.3f, 0.7f) / difficultyMultiplier;
            config.impulsiveness = Random.Range(0.3f, 0.7f) * difficultyMultiplier;
            
            // Autonomous behaviors
            SetAutonomousBehaviors(config, difficulty);
            
            // Object interactions
            SetObjectInteractions(config, difficulty);
            
            // Behavior timing
            SetBehaviorTiming(config, difficulty);
            
            // State-based interaction chances
            SetInteractionChances(config, difficultyMultiplier);
            
            // Influence settings
            SetInfluenceSettings(config, difficultyMultiplier);
            
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void SetAutonomousBehaviors(FunClass.Core.StudentConfig config, LevelConfigGenerator.Difficulty difficulty)
        {
            config.canFidget = true;
            config.canLookAround = true;
            config.canStandUp = difficulty != LevelConfigGenerator.Difficulty.Easy;
            config.canMoveAround = difficulty == LevelConfigGenerator.Difficulty.Hard;
        }

        private static void SetObjectInteractions(FunClass.Core.StudentConfig config, LevelConfigGenerator.Difficulty difficulty)
        {
            config.canDropItems = difficulty != LevelConfigGenerator.Difficulty.Easy;
            config.canKnockOverObjects = difficulty == LevelConfigGenerator.Difficulty.Hard || 
                                         (difficulty == LevelConfigGenerator.Difficulty.Normal && Random.value > 0.5f);
            config.canMakeNoiseWithObjects = true;
            config.canThrowObjects = difficulty == LevelConfigGenerator.Difficulty.Hard;
            config.canTouchObjects = true;
            config.interactionRange = 2f;
        }

        private static void SetBehaviorTiming(FunClass.Core.StudentConfig config, LevelConfigGenerator.Difficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelConfigGenerator.Difficulty.Easy:
                    config.minIdleTime = 3f;
                    config.maxIdleTime = 10f;
                    break;
                case LevelConfigGenerator.Difficulty.Normal:
                    config.minIdleTime = 2f;
                    config.maxIdleTime = 8f;
                    break;
                case LevelConfigGenerator.Difficulty.Hard:
                    config.minIdleTime = 1f;
                    config.maxIdleTime = 5f;
                    break;
            }
        }

        private static void SetInteractionChances(FunClass.Core.StudentConfig config, float difficultyMultiplier)
        {
            config.calmInteractionChance = Mathf.Clamp01(0.1f * difficultyMultiplier);
            config.distractedInteractionChance = Mathf.Clamp01(0.3f * difficultyMultiplier);
            config.actingOutInteractionChance = Mathf.Clamp01(0.6f * difficultyMultiplier);
            config.criticalInteractionChance = Mathf.Clamp01(0.9f * difficultyMultiplier);
        }

        private static void SetInfluenceSettings(FunClass.Core.StudentConfig config, float difficultyMultiplier)
        {
            config.influenceSusceptibility = Mathf.Clamp01(Random.Range(0.5f, 0.9f) * difficultyMultiplier);
            config.influenceResistance = Mathf.Clamp01(Random.Range(0.1f, 0.5f) / difficultyMultiplier);
            config.panicThreshold = Mathf.Clamp01(Random.Range(0.6f, 0.8f) / difficultyMultiplier);
        }

        private static float GetDifficultyMultiplier(LevelConfigGenerator.Difficulty difficulty)
        {
            return difficulty switch
            {
                LevelConfigGenerator.Difficulty.Easy => 0.7f,
                LevelConfigGenerator.Difficulty.Normal => 1f,
                LevelConfigGenerator.Difficulty.Hard => 1.3f,
                _ => 1f
            };
        }
    }
}

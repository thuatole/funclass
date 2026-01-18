using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tự động tạo students với personality và behaviors
    /// </summary>
    public static class StudentGenerator
    {
        private static readonly string[] studentNames = { 
            "Nam", "Lan", "Minh", "Hoa", "Tuan", 
            "Mai", "Khoa", "Linh", "Duc", "Nga" 
        };

        public enum StudentArchetype
        {
            WellBehaved,    // Học sinh ngoan
            Average,        // Trung bình
            Mischievous,    // Nghịch ngợm
            Troublemaker,   // Gây rối
            Hyperactive     // Hiếu động
        }

        /// <summary>
        /// Tạo students cho level theo difficulty
        /// </summary>
        public static List<FunClass.Core.StudentConfig> GenerateStudents(
            string levelName, 
            LevelConfigGenerator.Difficulty difficulty)
        {
            int studentCount = difficulty switch
            {
                LevelConfigGenerator.Difficulty.Easy => 5,
                LevelConfigGenerator.Difficulty.Normal => 8,
                LevelConfigGenerator.Difficulty.Hard => 10,
                _ => 8
            };

            return GenerateStudents(levelName, studentCount, difficulty);
        }

        /// <summary>
        /// Tạo số lượng students cụ thể
        /// </summary>
        public static List<FunClass.Core.StudentConfig> GenerateStudents(
            string levelName, 
            int count, 
            LevelConfigGenerator.Difficulty difficulty)
        {
            List<FunClass.Core.StudentConfig> configs = new List<FunClass.Core.StudentConfig>();

            // Create folder
            string folderPath = $"Assets/Configs/{levelName}/Students";
            EditorUtils.CreateFolderIfNotExists(folderPath);

            // Determine archetype distribution based on difficulty
            var archetypes = GetArchetypeDistribution(count, difficulty);

            for (int i = 0; i < count; i++)
            {
                string studentName = studentNames[i % studentNames.Length];
                StudentArchetype archetype = archetypes[i];
                
                var config = CreateStudentConfig(studentName, archetype, folderPath);
                configs.Add(config);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StudentGenerator] Generated {configs.Count} students for {levelName}");
            return configs;
        }

        /// <summary>
        /// Tạo student config theo archetype
        /// </summary>
        public static FunClass.Core.StudentConfig CreateStudentConfig(
            string studentName, 
            StudentArchetype archetype, 
            string savePath)
        {
            var config = ScriptableObject.CreateInstance<FunClass.Core.StudentConfig>();
            config.studentName = studentName;

            // Configure based on archetype
            switch (archetype)
            {
                case StudentArchetype.WellBehaved:
                    ConfigureWellBehaved(config);
                    break;
                case StudentArchetype.Average:
                    ConfigureAverage(config);
                    break;
                case StudentArchetype.Mischievous:
                    ConfigureMischievous(config);
                    break;
                case StudentArchetype.Troublemaker:
                    ConfigureTroublemaker(config);
                    break;
                case StudentArchetype.Hyperactive:
                    ConfigureHyperactive(config);
                    break;
            }

            // Save as asset
            string assetPath = $"{savePath}/{studentName}_{archetype}.asset";
            AssetDatabase.CreateAsset(config, assetPath);
            EditorUtility.SetDirty(config);

            return config;
        }

        private static void ConfigureWellBehaved(FunClass.Core.StudentConfig config)
        {
            // Personality
            config.patience = Random.Range(0.7f, 0.9f);
            config.attentionSpan = Random.Range(0.7f, 0.9f);
            config.impulsiveness = Random.Range(0.1f, 0.3f);
            config.influenceSusceptibility = Random.Range(0.2f, 0.4f);
            config.influenceResistance = Random.Range(0.6f, 0.8f);
            config.panicThreshold = Random.Range(0.8f, 0.95f);

            // Behaviors - very limited
            config.canFidget = true;
            config.canLookAround = true;
            config.canStandUp = false;
            config.canMoveAround = false;
            config.canDropItems = false;
            config.canKnockOverObjects = false;
            config.canMakeNoiseWithObjects = false;
            config.canThrowObjects = false;
            config.canTouchObjects = true;

            config.minIdleTime = 5f;
            config.maxIdleTime = 15f;
        }

        private static void ConfigureAverage(FunClass.Core.StudentConfig config)
        {
            // Personality
            config.patience = Random.Range(0.4f, 0.6f);
            config.attentionSpan = Random.Range(0.5f, 0.7f);
            config.impulsiveness = Random.Range(0.3f, 0.5f);
            config.influenceSusceptibility = Random.Range(0.4f, 0.6f);
            config.influenceResistance = Random.Range(0.4f, 0.6f);
            config.panicThreshold = Random.Range(0.6f, 0.8f);

            // Behaviors - moderate
            config.canFidget = true;
            config.canLookAround = true;
            config.canStandUp = true;
            config.canMoveAround = false;
            config.canDropItems = true;
            config.canKnockOverObjects = false;
            config.canMakeNoiseWithObjects = true;
            config.canThrowObjects = false;
            config.canTouchObjects = true;

            config.minIdleTime = 3f;
            config.maxIdleTime = 10f;
        }

        private static void ConfigureMischievous(FunClass.Core.StudentConfig config)
        {
            // Personality
            config.patience = Random.Range(0.3f, 0.5f);
            config.attentionSpan = Random.Range(0.3f, 0.5f);
            config.impulsiveness = Random.Range(0.5f, 0.7f);
            config.influenceSusceptibility = Random.Range(0.6f, 0.8f);
            config.influenceResistance = Random.Range(0.2f, 0.4f);
            config.panicThreshold = Random.Range(0.5f, 0.7f);

            // Behaviors - many enabled
            config.canFidget = true;
            config.canLookAround = true;
            config.canStandUp = true;
            config.canMoveAround = true;
            config.canDropItems = true;
            config.canKnockOverObjects = true;
            config.canMakeNoiseWithObjects = true;
            config.canThrowObjects = false;
            config.canTouchObjects = true;

            config.minIdleTime = 2f;
            config.maxIdleTime = 7f;
        }

        private static void ConfigureTroublemaker(FunClass.Core.StudentConfig config)
        {
            // Personality
            config.patience = Random.Range(0.1f, 0.3f);
            config.attentionSpan = Random.Range(0.2f, 0.4f);
            config.impulsiveness = Random.Range(0.7f, 0.9f);
            config.influenceSusceptibility = Random.Range(0.8f, 0.95f);
            config.influenceResistance = Random.Range(0.1f, 0.2f);
            config.panicThreshold = Random.Range(0.4f, 0.6f);

            // Behaviors - almost all enabled
            config.canFidget = true;
            config.canLookAround = true;
            config.canStandUp = true;
            config.canMoveAround = true;
            config.canDropItems = true;
            config.canKnockOverObjects = true;
            config.canMakeNoiseWithObjects = true;
            config.canThrowObjects = true;
            config.canTouchObjects = true;

            config.minIdleTime = 1f;
            config.maxIdleTime = 5f;
        }

        private static void ConfigureHyperactive(FunClass.Core.StudentConfig config)
        {
            // Personality
            config.patience = Random.Range(0.05f, 0.2f);
            config.attentionSpan = Random.Range(0.1f, 0.3f);
            config.impulsiveness = Random.Range(0.8f, 0.95f);
            config.influenceSusceptibility = Random.Range(0.7f, 0.9f);
            config.influenceResistance = Random.Range(0.05f, 0.15f);
            config.panicThreshold = Random.Range(0.3f, 0.5f);

            // Behaviors - all enabled
            config.canFidget = true;
            config.canLookAround = true;
            config.canStandUp = true;
            config.canMoveAround = true;
            config.canDropItems = true;
            config.canKnockOverObjects = true;
            config.canMakeNoiseWithObjects = true;
            config.canThrowObjects = true;
            config.canTouchObjects = true;

            config.minIdleTime = 0.5f;
            config.maxIdleTime = 3f;
        }

        private static List<StudentArchetype> GetArchetypeDistribution(
            int count, 
            LevelConfigGenerator.Difficulty difficulty)
        {
            List<StudentArchetype> archetypes = new List<StudentArchetype>();

            switch (difficulty)
            {
                case LevelConfigGenerator.Difficulty.Easy:
                    // Mostly well-behaved and average
                    for (int i = 0; i < count; i++)
                    {
                        if (i < count * 0.6f)
                            archetypes.Add(StudentArchetype.WellBehaved);
                        else if (i < count * 0.9f)
                            archetypes.Add(StudentArchetype.Average);
                        else
                            archetypes.Add(StudentArchetype.Mischievous);
                    }
                    break;

                case LevelConfigGenerator.Difficulty.Normal:
                    // Mix of all types
                    for (int i = 0; i < count; i++)
                    {
                        if (i < count * 0.3f)
                            archetypes.Add(StudentArchetype.WellBehaved);
                        else if (i < count * 0.6f)
                            archetypes.Add(StudentArchetype.Average);
                        else if (i < count * 0.85f)
                            archetypes.Add(StudentArchetype.Mischievous);
                        else
                            archetypes.Add(StudentArchetype.Troublemaker);
                    }
                    break;

                case LevelConfigGenerator.Difficulty.Hard:
                    // Mostly troublemakers
                    for (int i = 0; i < count; i++)
                    {
                        if (i < count * 0.2f)
                            archetypes.Add(StudentArchetype.Average);
                        else if (i < count * 0.5f)
                            archetypes.Add(StudentArchetype.Mischievous);
                        else if (i < count * 0.8f)
                            archetypes.Add(StudentArchetype.Troublemaker);
                        else
                            archetypes.Add(StudentArchetype.Hyperactive);
                    }
                    break;
            }

            // Shuffle for variety
            for (int i = 0; i < archetypes.Count; i++)
            {
                int randomIndex = Random.Range(i, archetypes.Count);
                var temp = archetypes[i];
                archetypes[i] = archetypes[randomIndex];
                archetypes[randomIndex] = temp;
            }

            return archetypes;
        }

        /// <summary>
        /// Quick create sample students
        /// </summary>
        [MenuItem("Tools/FunClass/Quick Create/Sample Students")]
        public static void QuickCreateSampleStudents()
        {
            string folderPath = "Assets/Configs/Students";
            EditorUtils.CreateFolderIfNotExists(folderPath);

            var students = new List<FunClass.Core.StudentConfig>();
            
            // Create one of each archetype
            foreach (StudentArchetype archetype in System.Enum.GetValues(typeof(StudentArchetype)))
            {
                string name = studentNames[students.Count % studentNames.Length];
                var student = CreateStudentConfig(name, archetype, folderPath);
                students.Add(student);
            }

            EditorUtility.DisplayDialog("Success", 
                $"Created {students.Count} sample students in {folderPath}", 
                "OK");
        }
    }
}

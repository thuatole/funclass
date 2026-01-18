using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tự động tạo interaction sequences cho students
    /// </summary>
    public static class SequenceGenerator
    {
        public enum SequenceTemplate
        {
            SimpleWarning,          // Teacher warns → Student calms
            EscalatingBehavior,     // Student escalates → Teacher intervenes
            ObjectConfiscation,     // Student uses object → Teacher takes it
            MessCleanup,           // Student creates mess → Teacher cleans
            OutsideRecall,         // Student escapes → Teacher recalls
            PeerInfluence,         // Student influences others → Chain reaction
            ComplexIntervention    // Multi-step teacher intervention
        }

        /// <summary>
        /// Tạo sample sequences cho level
        /// </summary>
        public static List<FunClass.Core.StudentSequenceConfig> CreateSampleSequences(
            string levelName, 
            LevelConfigGenerator.Difficulty difficulty)
        {
            List<FunClass.Core.StudentSequenceConfig> sequences = new List<FunClass.Core.StudentSequenceConfig>();

            // Create folder
            string folderPath = $"Assets/Configs/{levelName}/Sequences";
            EditorUtils.CreateFolderIfNotExists(folderPath);

            // Create sequences based on difficulty
            int sequenceCount = difficulty switch
            {
                LevelConfigGenerator.Difficulty.Easy => 3,
                LevelConfigGenerator.Difficulty.Normal => 5,
                LevelConfigGenerator.Difficulty.Hard => 8,
                _ => 5
            };

            // Create basic sequences
            sequences.Add(CreateSequence(SequenceTemplate.SimpleWarning, levelName, folderPath));
            sequences.Add(CreateSequence(SequenceTemplate.ObjectConfiscation, levelName, folderPath));
            sequences.Add(CreateSequence(SequenceTemplate.MessCleanup, levelName, folderPath));

            if (difficulty >= LevelConfigGenerator.Difficulty.Normal)
            {
                sequences.Add(CreateSequence(SequenceTemplate.EscalatingBehavior, levelName, folderPath));
                sequences.Add(CreateSequence(SequenceTemplate.OutsideRecall, levelName, folderPath));
            }

            if (difficulty == LevelConfigGenerator.Difficulty.Hard)
            {
                sequences.Add(CreateSequence(SequenceTemplate.PeerInfluence, levelName, folderPath));
                sequences.Add(CreateSequence(SequenceTemplate.ComplexIntervention, levelName, folderPath));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SequenceGenerator] Created {sequences.Count} sequences for {levelName}");
            return sequences;
        }

        /// <summary>
        /// Tạo sequence theo template
        /// </summary>
        public static FunClass.Core.StudentSequenceConfig CreateSequence(
            SequenceTemplate template, 
            string levelName, 
            string savePath)
        {
            var sequence = ScriptableObject.CreateInstance<FunClass.Core.StudentSequenceConfig>();
            
            switch (template)
            {
                case SequenceTemplate.SimpleWarning:
                    ConfigureSimpleWarning(sequence);
                    break;
                case SequenceTemplate.EscalatingBehavior:
                    ConfigureEscalatingBehavior(sequence);
                    break;
                case SequenceTemplate.ObjectConfiscation:
                    ConfigureObjectConfiscation(sequence);
                    break;
                case SequenceTemplate.MessCleanup:
                    ConfigureMessCleanup(sequence);
                    break;
                case SequenceTemplate.OutsideRecall:
                    ConfigureOutsideRecall(sequence);
                    break;
                case SequenceTemplate.PeerInfluence:
                    ConfigurePeerInfluence(sequence);
                    break;
                case SequenceTemplate.ComplexIntervention:
                    ConfigureComplexIntervention(sequence);
                    break;
            }

            // Save as asset
            string assetPath = $"{savePath}/{template}.asset";
            AssetDatabase.CreateAsset(sequence, assetPath);
            EditorUtility.SetDirty(sequence);

            return sequence;
        }

        private static void ConfigureSimpleWarning(FunClass.Core.StudentSequenceConfig sequence)
        {
            sequence.sequenceId = "simple_warning";
            sequence.entryState = FunClass.Core.StudentState.Distracted;
            sequence.entryTeacherAction = FunClass.Core.TeacherActionType.Talk;
            sequence.finalOutcomeDescription = "Student calms down after warning";

            sequence.steps = new List<FunClass.Core.StudentActionStepConfig>
            {
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Distracted,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Scold,
                    resultingReaction = FunClass.Core.StudentReactionType.Embarrassed,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Calm,
                    stepDescription = "Teacher warns student → Student becomes embarrassed and calms down"
                }
            };
        }

        private static void ConfigureEscalatingBehavior(FunClass.Core.StudentSequenceConfig sequence)
        {
            sequence.sequenceId = "escalating_behavior";
            sequence.entryState = FunClass.Core.StudentState.Calm;
            sequence.entryTeacherAction = FunClass.Core.TeacherActionType.Talk;
            sequence.finalOutcomeDescription = "Student escalates through states until teacher intervenes";

            sequence.steps = new List<FunClass.Core.StudentActionStepConfig>
            {
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Calm,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Talk,
                    resultingReaction = FunClass.Core.StudentReactionType.Confused,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Distracted,
                    stepDescription = "Student gets bored → becomes distracted"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Distracted,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Talk,
                    resultingReaction = FunClass.Core.StudentReactionType.Angry,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.ActingOut,
                    stepDescription = "Student becomes frustrated → starts acting out"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.ActingOut,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Calm,
                    resultingReaction = FunClass.Core.StudentReactionType.Apologize,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Calm,
                    stepDescription = "Teacher calms student → Student feels relieved and calms down"
                }
            };
        }

        private static void ConfigureObjectConfiscation(FunClass.Core.StudentSequenceConfig sequence)
        {
            sequence.sequenceId = "object_confiscation";
            sequence.entryState = FunClass.Core.StudentState.Distracted;
            sequence.entryTeacherAction = FunClass.Core.TeacherActionType.Talk;
            sequence.finalOutcomeDescription = "Teacher confiscates object from student";

            sequence.steps = new List<FunClass.Core.StudentActionStepConfig>
            {
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Distracted,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Talk,
                    resultingReaction = FunClass.Core.StudentReactionType.None,
                    changeState = false,
                    stepDescription = "Student plays with object → becomes amused"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Distracted,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.UseItem,
                    resultingReaction = FunClass.Core.StudentReactionType.Embarrassed,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Calm,
                    stepDescription = "Teacher confiscates object → Student embarrassed, calms down"
                }
            };
        }

        private static void ConfigureMessCleanup(FunClass.Core.StudentSequenceConfig sequence)
        {
            sequence.sequenceId = "mess_cleanup";
            sequence.entryState = FunClass.Core.StudentState.Critical;
            sequence.entryTeacherAction = FunClass.Core.TeacherActionType.Talk;
            sequence.finalOutcomeDescription = "Student creates mess, teacher cleans it";

            sequence.steps = new List<FunClass.Core.StudentActionStepConfig>
            {
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Critical,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Talk,
                    resultingReaction = FunClass.Core.StudentReactionType.Embarrassed,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Distracted,
                    stepDescription = "Student vomits → creates mess, becomes embarrassed"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Distracted,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.CleanMess,
                    resultingReaction = FunClass.Core.StudentReactionType.Apologize,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Calm,
                    stepDescription = "Teacher cleans mess → Student relieved, calms down"
                }
            };
        }

        private static void ConfigureOutsideRecall(FunClass.Core.StudentSequenceConfig sequence)
        {
            sequence.sequenceId = "outside_recall";
            sequence.entryState = FunClass.Core.StudentState.Critical;
            sequence.entryTeacherAction = FunClass.Core.TeacherActionType.Talk;
            sequence.finalOutcomeDescription = "Student escapes, teacher recalls them";

            sequence.steps = new List<FunClass.Core.StudentActionStepConfig>
            {
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Critical,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Talk,
                    resultingReaction = FunClass.Core.StudentReactionType.Scared,
                    changeState = false,
                    stepDescription = "Student panics → runs out of classroom"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Critical,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.CallStudentBack,
                    resultingReaction = FunClass.Core.StudentReactionType.Confused,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Distracted,
                    stepDescription = "Teacher calls student back → Student confused, returns"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Distracted,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Calm,
                    resultingReaction = FunClass.Core.StudentReactionType.Apologize,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Calm,
                    stepDescription = "Teacher calms student → Student relieved"
                }
            };
        }

        private static void ConfigurePeerInfluence(FunClass.Core.StudentSequenceConfig sequence)
        {
            sequence.sequenceId = "peer_influence";
            sequence.entryState = FunClass.Core.StudentState.ActingOut;
            sequence.entryTeacherAction = FunClass.Core.TeacherActionType.Talk;
            sequence.finalOutcomeDescription = "Student influences peers, teacher manages group";

            sequence.steps = new List<FunClass.Core.StudentActionStepConfig>
            {
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.ActingOut,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Talk,
                    resultingReaction = FunClass.Core.StudentReactionType.None,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Critical,
                    stepDescription = "Student acts out loudly → influences nearby students"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Critical,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Calm,
                    resultingReaction = FunClass.Core.StudentReactionType.Angry,
                    changeState = false,
                    stepDescription = "Teacher tries to calm → Student defiant, resists"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Critical,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.EscortStudentBack,
                    resultingReaction = FunClass.Core.StudentReactionType.Embarrassed,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Calm,
                    stepDescription = "Teacher escorts student → Student embarrassed, complies"
                }
            };
        }

        private static void ConfigureComplexIntervention(FunClass.Core.StudentSequenceConfig sequence)
        {
            sequence.sequenceId = "complex_intervention";
            sequence.entryState = FunClass.Core.StudentState.Calm;
            sequence.entryTeacherAction = FunClass.Core.TeacherActionType.Talk;
            sequence.finalOutcomeDescription = "Multi-step escalation and intervention";

            sequence.steps = new List<FunClass.Core.StudentActionStepConfig>
            {
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Calm,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Talk,
                    resultingReaction = FunClass.Core.StudentReactionType.Confused,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Distracted,
                    stepDescription = "Student gets bored"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Distracted,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Scold,
                    resultingReaction = FunClass.Core.StudentReactionType.Angry,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.ActingOut,
                    stepDescription = "Teacher warns → Student becomes defiant, escalates"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.ActingOut,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.UseItem,
                    resultingReaction = FunClass.Core.StudentReactionType.Angry,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Critical,
                    stepDescription = "Teacher confiscates item → Student frustrated, critical"
                },
                new FunClass.Core.StudentActionStepConfig
                {
                    requiredStudentState = FunClass.Core.StudentState.Critical,
                    requiredTeacherAction = FunClass.Core.TeacherActionType.Calm,
                    resultingReaction = FunClass.Core.StudentReactionType.Apologize,
                    changeState = true,
                    resultingStateChange = FunClass.Core.StudentState.Calm,
                    stepDescription = "Teacher calms student → Student finally calms down"
                }
            };
        }

        /// <summary>
        /// Quick create sample sequences
        /// </summary>
        [MenuItem("Tools/FunClass/Quick Create/Sample Sequences")]
        public static void QuickCreateSampleSequences()
        {
            string folderPath = "Assets/Configs/Sequences";
            EditorUtils.CreateFolderIfNotExists(folderPath);

            var sequences = new List<FunClass.Core.StudentSequenceConfig>();
            foreach (SequenceTemplate template in System.Enum.GetValues(typeof(SequenceTemplate)))
            {
                var sequence = CreateSequence(template, "Sample", folderPath);
                sequences.Add(sequence);
            }

            EditorUtility.DisplayDialog("Success", 
                $"Created {sequences.Count} sample sequences in {folderPath}", 
                "OK");
        }
    }
}

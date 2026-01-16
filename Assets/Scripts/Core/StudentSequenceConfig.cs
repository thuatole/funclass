using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    [System.Serializable]
    public class StudentActionStepConfig
    {
        [Header("Step Requirements")]
        public StudentState requiredStudentState;
        public TeacherActionType requiredTeacherAction;
        
        [Header("Step Outcome")]
        public StudentReactionType resultingReaction;
        public bool changeState = false;
        public StudentState resultingStateChange;
        [TextArea(2, 3)]
        public string stepDescription;

        [Header("Branching (Optional)")]
        public bool enableBranching = false;
        public int successNextStepIndex = -1;
        public int failureNextStepIndex = -1;
        public bool allowAnyActionToFail = false;
        
        [Header("Timeout (Optional)")]
        public bool enableTimeout = false;
        public float timeoutSeconds = 10f;
        public int timeoutNextStepIndex = -1;
        public StudentReactionType timeoutReaction = StudentReactionType.None;

        public StudentActionStep ToActionStep()
        {
            return new StudentActionStep(
                requiredStudentState,
                requiredTeacherAction,
                resultingReaction,
                stepDescription,
                changeState ? resultingStateChange : (StudentState?)null,
                null,
                enableBranching,
                successNextStepIndex,
                failureNextStepIndex,
                allowAnyActionToFail,
                enableTimeout,
                timeoutSeconds,
                timeoutNextStepIndex,
                timeoutReaction
            );
        }
    }

    [CreateAssetMenu(fileName = "StudentSequence", menuName = "FunClass/Student Sequence Config")]
    public class StudentSequenceConfig : ScriptableObject
    {
        [Header("Sequence Identity")]
        public string sequenceId;

        [Header("Entry Conditions")]
        public StudentState entryState;
        public TeacherActionType entryTeacherAction;

        [Header("Sequence Steps")]
        public List<StudentActionStepConfig> steps = new List<StudentActionStepConfig>();

        [Header("Outcome")]
        [TextArea(2, 3)]
        public string finalOutcomeDescription;

        public StudentInteractionSequence ToInteractionSequence()
        {
            List<StudentActionStep> actionSteps = new List<StudentActionStep>();
            
            foreach (StudentActionStepConfig stepConfig in steps)
            {
                actionSteps.Add(stepConfig.ToActionStep());
            }

            return new StudentInteractionSequence(
                sequenceId,
                entryState,
                entryTeacherAction,
                actionSteps,
                finalOutcomeDescription
            );
        }
    }
}

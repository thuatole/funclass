using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    [System.Serializable]
    public class StudentActionStep
    {
        public StudentState requiredStudentState;
        public TeacherActionType requiredTeacherAction;
        public StudentReactionType resultingReaction;
        public StudentState? resultingStateChange;
        public string stepDescription;
        public string nextStepId;

        public bool enableBranching;
        public int successNextStepIndex;
        public int failureNextStepIndex;
        public bool allowAnyActionToFail;

        public bool enableTimeout;
        public float timeoutSeconds;
        public int timeoutNextStepIndex;
        public StudentReactionType timeoutReaction;

        public StudentActionStep(
            StudentState requiredState,
            TeacherActionType requiredAction,
            StudentReactionType reaction,
            string description,
            StudentState? stateChange = null,
            string nextStep = null,
            bool enableBranching = false,
            int successNextStepIndex = -1,
            int failureNextStepIndex = -1,
            bool allowAnyActionToFail = false,
            bool enableTimeout = false,
            float timeoutSeconds = 10f,
            int timeoutNextStepIndex = -1,
            StudentReactionType timeoutReaction = StudentReactionType.None)
        {
            requiredStudentState = requiredState;
            requiredTeacherAction = requiredAction;
            resultingReaction = reaction;
            resultingStateChange = stateChange;
            stepDescription = description;
            nextStepId = nextStep;
            this.enableBranching = enableBranching;
            this.successNextStepIndex = successNextStepIndex;
            this.failureNextStepIndex = failureNextStepIndex;
            this.allowAnyActionToFail = allowAnyActionToFail;
            this.enableTimeout = enableTimeout;
            this.timeoutSeconds = timeoutSeconds;
            this.timeoutNextStepIndex = timeoutNextStepIndex;
            this.timeoutReaction = timeoutReaction;
        }
    }

    [System.Serializable]
    public class StudentInteractionSequence
    {
        public string sequenceId;
        public StudentState entryState;
        public TeacherActionType entryAction;
        public List<StudentActionStep> steps;
        public string finalOutcomeDescription;

        private int currentStepIndex = 0;
        private float stepStartTime = 0f;

        public StudentInteractionSequence(
            string id,
            StudentState entryState,
            TeacherActionType entryAction,
            List<StudentActionStep> steps,
            string finalOutcome)
        {
            sequenceId = id;
            this.entryState = entryState;
            this.entryAction = entryAction;
            this.steps = steps;
            finalOutcomeDescription = finalOutcome;
            currentStepIndex = 0;
        }

        public StudentActionStep GetCurrentStep()
        {
            if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
            {
                return steps[currentStepIndex];
            }
            return null;
        }

        public StudentActionStep GetStepAtIndex(int index)
        {
            if (index >= 0 && index < steps.Count)
            {
                return steps[index];
            }
            return null;
        }

        public bool CanAdvance(TeacherActionType action, StudentState currentState)
        {
            StudentActionStep currentStep = GetCurrentStep();
            if (currentStep == null) return false;

            return currentStep.requiredTeacherAction == action &&
                   currentStep.requiredStudentState == currentState;
        }

        public bool CanBranch(TeacherActionType action, StudentState currentState, out bool isSuccess)
        {
            StudentActionStep currentStep = GetCurrentStep();
            isSuccess = false;

            if (currentStep == null || !currentStep.enableBranching) return false;

            bool stateMatches = currentStep.requiredStudentState == currentState;
            bool actionMatches = currentStep.requiredTeacherAction == action;

            if (stateMatches && actionMatches)
            {
                isSuccess = true;
                return true;
            }

            if (currentStep.allowAnyActionToFail && stateMatches)
            {
                isSuccess = false;
                return true;
            }

            return false;
        }

        public StudentActionStep AdvanceStep()
        {
            currentStepIndex++;
            stepStartTime = Time.time;
            return GetCurrentStep();
        }

        public StudentActionStep AdvanceStepWithResult(bool success)
        {
            StudentActionStep currentStep = GetCurrentStep();
            if (currentStep == null) return null;

            if (currentStep.enableBranching)
            {
                int nextIndex = GetNextStepIndex(currentStepIndex, success);
                if (nextIndex >= 0)
                {
                    currentStepIndex = nextIndex;
                    stepStartTime = Time.time;
                    return GetCurrentStep();
                }
            }

            currentStepIndex++;
            stepStartTime = Time.time;
            return GetCurrentStep();
        }

        public int GetNextStepIndex(int currentIndex, bool success)
        {
            StudentActionStep step = GetStepAtIndex(currentIndex);
            if (step == null || !step.enableBranching) return currentIndex + 1;

            return success ? step.successNextStepIndex : step.failureNextStepIndex;
        }

        public bool IsComplete()
        {
            return currentStepIndex >= steps.Count;
        }

        public void Reset()
        {
            currentStepIndex = 0;
            stepStartTime = Time.time;
        }

        public bool CanAutoFailStep()
        {
            StudentActionStep currentStep = GetCurrentStep();
            if (currentStep == null) return false;

            if (currentStep.enableTimeout)
            {
                float elapsed = Time.time - stepStartTime;
                return elapsed >= currentStep.timeoutSeconds;
            }

            return false;
        }

        public StudentActionStep HandleTimeout()
        {
            StudentActionStep currentStep = GetCurrentStep();
            if (currentStep == null || !currentStep.enableTimeout) return null;

            if (currentStep.timeoutNextStepIndex >= 0)
            {
                currentStepIndex = currentStep.timeoutNextStepIndex;
                stepStartTime = Time.time;
                return GetCurrentStep();
            }

            return null;
        }

        public void SetStepStartTime(float time)
        {
            stepStartTime = time;
        }

        public bool MatchesEntry(StudentState state, TeacherActionType action)
        {
            return entryState == state && entryAction == action;
        }
    }
}

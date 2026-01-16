using UnityEngine;

namespace FunClass.Core
{
    [CreateAssetMenu(fileName = "Student", menuName = "FunClass/Student Config")]
    public class StudentConfig : ScriptableObject
    {
        [Header("Identity")]
        public string studentId;
        public string studentName;

        [Header("Initial State")]
        public StudentState initialState = StudentState.Calm;

        [Header("Personality Parameters")]
        [Range(0f, 1f)]
        public float patience = 0.5f;
        [Range(0f, 1f)]
        public float attentionSpan = 0.5f;
        [Range(0f, 1f)]
        public float impulsiveness = 0.5f;

        [Header("Autonomous Behaviors")]
        public bool canFidget = true;
        public bool canLookAround = true;
        public bool canStandUp = false;
        public bool canMoveAround = false;

        [Header("Object Interactions")]
        public bool canDropItems = false;
        public bool canKnockOverObjects = false;
        public bool canMakeNoiseWithObjects = false;
        public bool canThrowObjects = false;
        public bool canTouchObjects = true;
        public float interactionRange = 2f;

        [Header("Behavior Timing")]
        public float minIdleTime = 2f;
        public float maxIdleTime = 8f;

        [Header("State-Based Behavior Triggers")]
        [Range(0f, 1f)]
        public float calmInteractionChance = 0.1f;
        [Range(0f, 1f)]
        public float distractedInteractionChance = 0.3f;
        [Range(0f, 1f)]
        public float actingOutInteractionChance = 0.6f;
        [Range(0f, 1f)]
        public float criticalInteractionChance = 0.9f;
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    [CreateAssetMenu(fileName = "Level", menuName = "FunClass/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Level Identity")]
        public string levelId;
        public int grade;
        [TextArea(3, 5)]
        public string description;

        [Header("Students")]
        public List<StudentConfig> students = new List<StudentConfig>();

        [Header("Interaction Sequences")]
        public List<StudentSequenceConfig> availableSequences = new List<StudentSequenceConfig>();

        [Header("Student Interactions")]
        [Tooltip("Scripted events that trigger at specific times or conditions")]
        public List<RuntimeStudentInteraction> studentInteractions = new List<RuntimeStudentInteraction>();

        [Header("Level Goals")]
        public LevelGoalConfig levelGoal;

        [Header("Movement Routes")]
        [Tooltip("Available routes students can follow in this level")]
        public List<StudentRoute> availableRoutes = new List<StudentRoute>();
        
        [Tooltip("Escape route students take when panicking")]
        public StudentRoute escapeRoute;
        
        [Tooltip("Return route for students coming back to class")]
        public StudentRoute returnRoute;

        [Header("Influence Settings")]
        [Tooltip("Influence scope configuration for this level")]
        public InfluenceScopeConfig influenceScopeConfig;
    
        [Header("Key Locations")]
        [Tooltip("Classroom door position")]
        public Transform classroomDoor;
        
        [Tooltip("Outside classroom area")]
        public Transform outsideArea;
        
        [Tooltip("Default seat positions for students")]
        public List<Transform> seatPositions = new List<Transform>();
    }
}

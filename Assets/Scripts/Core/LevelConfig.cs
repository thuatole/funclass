using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    [CreateAssetMenu(fileName = "Level", menuName = "FunClass/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        public string levelId;
        public int grade;
        [TextArea(3, 5)]
        public string description;

        [Header("Students")]
        public List<StudentConfig> students = new List<StudentConfig>();

        [Header("Interaction Sequences")]
        public List<StudentSequenceConfig> availableSequences = new List<StudentSequenceConfig>();

        [Header("Level Goals")]
        public LevelGoalConfig levelGoal;
    }
}

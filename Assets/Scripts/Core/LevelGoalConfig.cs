using UnityEngine;

namespace FunClass.Core
{
    [CreateAssetMenu(fileName = "LevelGoal", menuName = "FunClass/Level Goal Config")]
    public class LevelGoalConfig : ScriptableObject
    {
        [Header("Disruption Limits")]
        [Range(0, 100)]
        public float maxDisruptionThreshold = 80f;
        [Tooltip("If disruption exceeds this, instant lose")]
        [Range(0, 100)]
        public float catastrophicDisruptionLevel = 95f;

        [Header("Student Limits")]
        public int maxAllowedCriticalStudents = 2;
        [Tooltip("If this many students reach Critical state simultaneously, instant lose")]
        public int catastrophicCriticalStudents = 4;

        [Header("Time Limit")]
        public bool hasTimeLimit = true;
        public float timeLimitSeconds = 300f;

        [Header("Required Objectives")]
        public int requiredResolvedProblems = 5;
        [Tooltip("Number of students that must be calmed down")]
        public int requiredCalmDowns = 3;

        [Header("Star Rating Thresholds")]
        [Tooltip("Score needed for 1 star (minimum to pass)")]
        public int oneStarScore = 100;
        [Tooltip("Score needed for 2 stars")]
        public int twoStarScore = 250;
        [Tooltip("Score needed for 3 stars (perfect)")]
        public int threeStarScore = 500;

        [Header("Win Conditions")]
        [Tooltip("Final disruption must be below this to win")]
        [Range(0, 100)]
        public float winDisruptionThreshold = 50f;

        public int GetStarRating(int score)
        {
            if (score >= threeStarScore) return 3;
            if (score >= twoStarScore) return 2;
            if (score >= oneStarScore) return 1;
            return 0;
        }

        public bool MeetsWinConditions(float finalDisruption, int resolvedProblems, int calmDowns)
        {
            return finalDisruption <= winDisruptionThreshold &&
                   resolvedProblems >= requiredResolvedProblems &&
                   calmDowns >= requiredCalmDowns;
        }
    }
}

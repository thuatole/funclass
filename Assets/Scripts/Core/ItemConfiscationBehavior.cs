using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Defines how a student reacts when a specific item is confiscated by the teacher.
    /// This allows for flexible, item-specific behavior rules.
    /// </summary>
    [System.Serializable]
    public class ItemConfiscationBehavior
    {
        [Header("Item Identification")]
        [Tooltip("Keywords to match against item name (case-insensitive)")]
        public string[] itemKeywords;

        [Header("Reaction")]
        [Tooltip("The emotional reaction the student will have")]
        public StudentReactionType reaction = StudentReactionType.Embarrassed;
        
        [Tooltip("Duration of the reaction in seconds")]
        public float reactionDuration = 3f;

        [Header("State Change")]
        [Tooltip("Should the student's state change?")]
        public bool changeState = false;
        
        [Tooltip("The new state if changeState is true")]
        public StudentState newState = StudentState.Calm;

        [Header("Disruption Impact")]
        [Tooltip("How much this confiscation reduces classroom disruption (0-20)")]
        public float disruptionReduction = 5f;

        /// <summary>
        /// Checks if this behavior applies to the given item name
        /// </summary>
        public bool MatchesItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return false;
            
            string lowerItemName = itemName.ToLower();
            
            foreach (string keyword in itemKeywords)
            {
                if (!string.IsNullOrEmpty(keyword) && lowerItemName.Contains(keyword.ToLower()))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}

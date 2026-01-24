using UnityEngine;
using System;

namespace FunClass.Core
{
    public enum StudentEventType
    {
        KnockedOverObject,
        MakingNoise,
        DroppedItem,
        LeftSeat,
        WanderingAround,
        TouchedObject,
        ThrowingObject,
        TeacherInteracted,
        StudentCalmed,
        StudentReturnedToSeat,
        StudentStoppedAction,
        ObjectTakenAway,
        StudentReacted,
        ReactionEnded,
        MessCreated,
        MessCleaned,
        StudentActedOut
    }

    /// <summary>
    /// Defines the scope of influence for an event
    /// </summary>
    public enum InfluenceScope
    {
        None,           // No influence
        WholeClass,     // Affects all students in class (no distance check)
        SingleStudent   // Affects one specific student (no distance check)
    }

    public class StudentEvent
    {
        public StudentAgent student;
        public StudentEventType eventType;
        public string description;
        public GameObject targetObject;
        public StudentAgent targetStudent;  // For SingleStudent scope - specific student to influence
        public InfluenceScope influenceScope;  // Scope of influence
        public float timestamp;

        public StudentEvent(StudentAgent student, StudentEventType eventType, string description, GameObject targetObject = null, StudentAgent targetStudent = null, InfluenceScope? scope = null)
        {
            this.student = student;
            this.eventType = eventType;
            this.description = description;
            this.targetObject = targetObject;
            this.targetStudent = targetStudent;
            this.timestamp = Time.time;
            
            // Auto-determine scope if not specified
            if (scope.HasValue)
            {
                this.influenceScope = scope.Value;
            }
            else
            {
                this.influenceScope = DetermineInfluenceScope(eventType, targetStudent);
            }
        }

        private InfluenceScope DetermineInfluenceScope(StudentEventType type, StudentAgent target)
        {
            // If target student specified, it's SingleStudent scope
            if (target != null)
            {
                return InfluenceScope.SingleStudent;
            }

            // Try to get scope from config
            if (LevelManager.Instance != null)
            {
                var levelConfig = LevelManager.Instance.GetCurrentLevelConfig();
                if (levelConfig?.influenceScopeConfig != null)
                {
                    string scopeStr = levelConfig.influenceScopeConfig.GetScope(type.ToString());
                    if (scopeStr == "WholeClass") return InfluenceScope.WholeClass;
                    if (scopeStr == "SingleStudent") return InfluenceScope.SingleStudent;
                    if (scopeStr == "None") return InfluenceScope.None;
                }
            }

            // Fallback to hardcoded mapping
            return type switch
            {
                StudentEventType.ThrowingObject => InfluenceScope.SingleStudent,  // Hitting someone
                StudentEventType.MessCreated => InfluenceScope.WholeClass,        // Vomit affects all
                StudentEventType.MakingNoise => InfluenceScope.WholeClass,        // Noise affects all
                StudentEventType.KnockedOverObject => InfluenceScope.WholeClass,  // Disruption affects all
                StudentEventType.WanderingAround => InfluenceScope.WholeClass,    // Movement affects all
                _ => InfluenceScope.None
            };
        }

        public override string ToString()
        {
            string studentName = student?.Config?.studentName ?? "Unknown Student";
            return $"[StudentEvent] {studentName}: {description}";
        }
    }

}

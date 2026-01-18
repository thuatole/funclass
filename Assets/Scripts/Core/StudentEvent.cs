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

    public class StudentEvent
    {
        public StudentAgent student;
        public StudentEventType eventType;
        public string description;
        public GameObject targetObject;
        public float timestamp;

        public StudentEvent(StudentAgent student, StudentEventType eventType, string description, GameObject targetObject = null)
        {
            this.student = student;
            this.eventType = eventType;
            this.description = description;
            this.targetObject = targetObject;
            this.timestamp = Time.time;
        }

        public override string ToString()
        {
            string studentName = student?.Config?.studentName ?? "Unknown Student";
            return $"[StudentEvent] {studentName}: {description}";
        }
    }

}

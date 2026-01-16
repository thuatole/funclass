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
        ObjectTakenAway
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

    public class StudentEventManager : MonoBehaviour
    {
        public static StudentEventManager Instance { get; private set; }

        public event Action<StudentEvent> OnStudentEvent;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LogEvent(StudentEvent studentEvent)
        {
            Debug.Log(studentEvent.ToString());
            OnStudentEvent?.Invoke(studentEvent);
        }

        public void LogEvent(StudentAgent student, StudentEventType eventType, string description, GameObject targetObject = null)
        {
            StudentEvent evt = new StudentEvent(student, eventType, description, targetObject);
            LogEvent(evt);
        }
    }
}

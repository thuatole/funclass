using UnityEngine;
using System;

namespace FunClass.Core
{
    public class StudentEventManager : MonoBehaviour
    {
        public static StudentEventManager Instance { get; private set; }

        public event Action<StudentEvent> OnStudentEvent;
        public event Action<StudentEvent> OnEventLogged;

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
            Debug.Log($"[StudentEventManager] About to fire event: {studentEvent.eventType}");
            
            int onEventLoggedSubscribers = 0;
            if (OnEventLogged != null)
            {
                onEventLoggedSubscribers = OnEventLogged.GetInvocationList().Length;
            }
            
            Debug.Log($"[StudentEventManager] OnEventLogged has {onEventLoggedSubscribers} subscribers");
            
            OnStudentEvent?.Invoke(studentEvent);
            OnEventLogged?.Invoke(studentEvent);
            
            Debug.Log($"[StudentEventManager] Event {studentEvent.eventType} fired successfully");
        }

        public void LogEvent(StudentAgent student, StudentEventType eventType, string description, GameObject targetObject = null)
        {
            StudentEvent evt = new StudentEvent(student, eventType, description, targetObject);
            LogEvent(evt);
        }

        /// <summary>
        /// Log event with specific target student and influence scope (for SingleStudent influence)
        /// </summary>
        public void LogEvent(StudentAgent student, StudentEventType eventType, string description, GameObject targetObject, StudentAgent targetStudent, InfluenceScope scope)
        {
            StudentEvent evt = new StudentEvent(student, eventType, description, targetObject, targetStudent, scope);
            LogEvent(evt);
        }
    }
}

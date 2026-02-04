using System;
using System.Collections.Generic;
using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Runtime configuration for student-to-student interactions.
    /// Used at runtime to trigger scripted events based on conditions.
    /// </summary>
    [Serializable]
    public class RuntimeStudentInteraction
    {
        public string id;
        public string sourceStudentId;
        public string targetStudentId;  // Can be null for self-events
        public string eventType;        // StudentEventType as string
        public string triggerCondition; // "timeElapsed", "stateChanged", etc.
        public float triggerValue;      // For timeElapsed: seconds; For stateChanged: state enum
        public float probability = 1.0f;
        public bool oneTimeOnly = true;
        public bool hasTriggered = false;
        public string description;

        public RuntimeStudentInteraction() { }

        public RuntimeStudentInteraction(string id, string sourceStudentId, string targetStudentId,
            string eventType, string triggerCondition, float triggerValue, float probability = 1.0f)
        {
            this.id = id;
            this.sourceStudentId = sourceStudentId;
            this.targetStudentId = targetStudentId;
            this.eventType = eventType;
            this.triggerCondition = triggerCondition;
            this.triggerValue = triggerValue;
            this.probability = probability;
            this.oneTimeOnly = true;
            this.hasTriggered = false;
        }
    }

    /// <summary>
    /// Container for all student interactions in a level.
    /// </summary>
    [CreateAssetMenu(fileName = "StudentInteractions", menuName = "FunClass/Student Interactions Config")]
    public class StudentInteractionsConfig : ScriptableObject
    {
        public List<RuntimeStudentInteraction> interactions = new List<RuntimeStudentInteraction>();

        public void AddInteraction(RuntimeStudentInteraction interaction)
        {
            interactions.Add(interaction);
        }

        public void Clear()
        {
            interactions.Clear();
        }

        public List<RuntimeStudentInteraction> GetInteractionsForSource(string sourceStudentId)
        {
            return interactions.FindAll(i => i.sourceStudentId == sourceStudentId);
        }

        public RuntimeStudentInteraction GetInteractionById(string id)
        {
            return interactions.Find(i => i.id == id);
        }
    }
}

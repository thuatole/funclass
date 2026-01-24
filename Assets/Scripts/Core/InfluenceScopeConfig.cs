using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    [CreateAssetMenu(fileName = "InfluenceScopeConfig", menuName = "FunClass/Influence Scope Config")]
    public class InfluenceScopeConfig : ScriptableObject
    {
        [System.Serializable]
        public class EventScopeEntry
        {
            public string eventTypeName; // e.g., "MessCreated", "ThrowingObject"
            public string scope; // "None", "WholeClass", "SingleStudent"
            public float baseSeverity = 0.5f;
            public string description;
        }

        [Header("Influence Scope Settings")]
        [TextArea(3, 5)]
        public string description = "Influence scope configuration for this level";
        
        [Tooltip("Disruption penalty added per unresolved influence source")]
        public float disruptionPenaltyPerUnresolvedSource = 10f;
        
        [Header("Event Scopes")]
        [Tooltip("Event type to scope mapping")]
        public List<EventScopeEntry> eventScopes = new List<EventScopeEntry>();

        /// <summary>
        /// Get scope for an event type
        /// </summary>
        public string GetScope(string eventTypeName)
        {
            var entry = eventScopes.Find(e => e.eventTypeName == eventTypeName);
            return entry?.scope ?? "None"; // Default to None if not found
        }

        /// <summary>
        /// Get base severity for an event type
        /// </summary>
        public float GetBaseSeverity(string eventTypeName)
        {
            var entry = eventScopes.Find(e => e.eventTypeName == eventTypeName);
            return entry?.baseSeverity ?? 0.5f; // Default to 0.5 if not found
        }

        public bool ContainsEventType(string eventTypeName)
        {
            return eventScopes.Exists(e => e.eventTypeName == eventTypeName);
        }

        /// <summary>
        /// Get all event scopes as dictionary for easy lookup
        /// </summary>
        public Dictionary<string, (string scope, float severity)> GetEventScopesDictionary()
        {
            var dict = new Dictionary<string, (string, float)>();
            foreach (var entry in eventScopes)
            {
                dict[entry.eventTypeName] = (entry.scope, entry.baseSeverity);
            }
            return dict;
        }


    }
}
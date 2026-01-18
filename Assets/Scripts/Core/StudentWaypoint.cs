using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Represents a single waypoint in a movement route.
    /// Students can move to this point and optionally wait before continuing.
    /// </summary>
    public class StudentWaypoint : MonoBehaviour
    {
        [Header("Waypoint Identity")]
        [Tooltip("Unique identifier for this waypoint")]
        public string waypointId;
        
        [Tooltip("Display name for logging")]
        public string waypointName;

        [Header("Behavior")]
        [Tooltip("How long to wait at this waypoint before moving to next (seconds)")]
        public float waitDuration = 0f;
        
        [Tooltip("Should the student perform an action at this waypoint?")]
        public bool triggerAction = false;
        
        [Tooltip("Action to trigger when reaching this waypoint")]
        public StudentActionType actionToTrigger = StudentActionType.Idle;

        [Header("Visual Debug")]
        [Tooltip("Show waypoint gizmo in scene view")]
        public bool showGizmo = true;
        
        [Tooltip("Gizmo color")]
        public Color gizmoColor = Color.yellow;
        
        [Tooltip("Gizmo size")]
        public float gizmoSize = 0.5f;

        void OnDrawGizmos()
        {
            if (showGizmo)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(transform.position, gizmoSize);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, waypointName);
                #endif
            }
        }

        void OnDrawGizmosSelected()
        {
            if (showGizmo)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawSphere(transform.position, gizmoSize);
            }
        }
    }
}

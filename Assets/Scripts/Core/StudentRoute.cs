using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Defines a route consisting of multiple waypoints that students can follow.
    /// Routes can be one-time or looping, with configurable speeds and behaviors.
    /// </summary>
    [CreateAssetMenu(fileName = "StudentRoute", menuName = "FunClass/Student Route")]
    public class StudentRoute : ScriptableObject
    {
        [Header("Route Identity")]
        [Tooltip("Unique identifier for this route")]
        public string routeId;
        
        [Tooltip("Display name for this route")]
        public string routeName;

        [Header("Route Configuration")]
        [Tooltip("Waypoints in this route (in order)")]
        public List<StudentWaypoint> waypoints = new List<StudentWaypoint>();
        
        [Tooltip("Should the route loop back to the start?")]
        public bool isLooping = false;
        
        [Tooltip("Should the route reverse when reaching the end?")]
        public bool isPingPong = false;

        [Header("Movement Settings")]
        [Tooltip("Movement speed along this route (m/s)")]
        public float movementSpeed = 2f;
        
        [Tooltip("Is this a running route? (affects speed and animation)")]
        public bool isRunning = false;
        
        [Tooltip("Rotation speed when turning toward waypoints (degrees/s)")]
        public float rotationSpeed = 180f;

        [Header("Completion Behavior")]
        [Tooltip("What happens when the route completes?")]
        public RouteCompletionBehavior completionBehavior = RouteCompletionBehavior.Stop;
        
        [Tooltip("State to transition to on completion")]
        public StudentState completionState = StudentState.Calm;
        
        [Tooltip("Should trigger an event when completed?")]
        public bool triggerEventOnCompletion = true;

        /// <summary>
        /// Validates that the route has valid waypoints
        /// </summary>
        public bool IsValid()
        {
            return waypoints != null && waypoints.Count > 0;
        }

        /// <summary>
        /// Gets the total number of waypoints in this route
        /// </summary>
        public int WaypointCount => waypoints?.Count ?? 0;

        /// <summary>
        /// Gets a waypoint at the specified index
        /// </summary>
        public StudentWaypoint GetWaypoint(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Count)
                return null;
            
            return waypoints[index];
        }
    }

    /// <summary>
    /// Defines what happens when a route is completed
    /// </summary>
    public enum RouteCompletionBehavior
    {
        Stop,                  // Stop at final waypoint
        ReturnToSeat,         // Return to original seat position
        ResumeAutonomous,     // Resume normal autonomous behavior
        StartNewRoute,        // Start another route (configured separately)
        WaitForTeacher        // Wait for teacher intervention
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Manages student movement along predefined routes and waypoints.
    /// This is a standalone system that integrates with StudentAgent without modifying core logic.
    /// </summary>
    public class StudentMovementManager : MonoBehaviour
    {
        public static StudentMovementManager Instance { get; private set; }

        [Header("Movement Settings")]
        [Tooltip("Default movement speed (m/s)")]
        [SerializeField] private float defaultMovementSpeed = 2f;
        
        [Tooltip("Default running speed (m/s)")]
        [SerializeField] private float defaultRunningSpeed = 4f;
        
        [Tooltip("Distance threshold to consider waypoint reached")]
        [SerializeField] private float waypointReachThreshold = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool drawRouteGizmos = true;

        private Dictionary<StudentAgent, StudentMovementState> activeMovements = new Dictionary<StudentAgent, StudentMovementState>();
        private bool isActive = false;

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

        void OnEnable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        void Start()
        {
            if (GameStateManager.Instance != null)
            {
                HandleGameStateChanged(GameStateManager.Instance.CurrentState, GameStateManager.Instance.CurrentState);
            }
        }

        void Update()
        {
            if (!isActive) return;

            UpdateActiveMovements();
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.InLevel)
            {
                ActivateMovementSystem();
            }
            else
            {
                DeactivateMovementSystem();
            }
        }

        private void ActivateMovementSystem()
        {
            isActive = true;
            activeMovements.Clear();
            Log("[StudentMovementManager] Movement system activated");
        }

        private void DeactivateMovementSystem()
        {
            isActive = false;
            activeMovements.Clear();
            Log("[StudentMovementManager] Movement system deactivated");
        }

        /// <summary>
        /// Starts a student on a route
        /// </summary>
        public void StartRoute(StudentAgent student, StudentRoute route)
        {
            if (!isActive || student == null || route == null || !route.IsValid())
            {
                LogWarning($"[Movement] Cannot start route - invalid parameters");
                return;
            }

            // Stop any existing movement
            StopMovement(student);

            // Create new movement state
            StudentMovementState movementState = new StudentMovementState
            {
                student = student,
                route = route,
                currentWaypointIndex = 0,
                isMoving = true,
                isWaiting = false,
                waitEndTime = 0f,
                movementSpeed = route.movementSpeed > 0 ? route.movementSpeed : (route.isRunning ? defaultRunningSpeed : defaultMovementSpeed),
                isReversing = false
            };

            activeMovements[student] = movementState;

            Log($"[Movement] {student.Config?.studentName} started route: {route.routeName}");

            // Log event
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.WanderingAround,
                    $"started route: {route.routeName}"
                );
            }
        }

        /// <summary>
        /// Stops a student's current movement
        /// </summary>
        public void StopMovement(StudentAgent student)
        {
            if (student == null) return;

            if (activeMovements.ContainsKey(student))
            {
                StudentMovementState state = activeMovements[student];
                Log($"[Movement] {student.Config?.studentName} stopped route: {state.route.routeName}");
                activeMovements.Remove(student);
            }
        }

        /// <summary>
        /// Checks if a student is currently following a route
        /// </summary>
        public bool IsMoving(StudentAgent student)
        {
            return student != null && activeMovements.ContainsKey(student);
        }

        /// <summary>
        /// Gets the current route a student is following (null if not moving)
        /// </summary>
        public StudentRoute GetCurrentRoute(StudentAgent student)
        {
            if (student != null && activeMovements.TryGetValue(student, out StudentMovementState state))
            {
                return state.route;
            }
            return null;
        }

        /// <summary>
        /// Forces a student to return to their seat
        /// </summary>
        public void ReturnToSeat(StudentAgent student)
        {
            if (!isActive || student == null) return;

            StopMovement(student);

            // Move student back to original position
            Vector3 seatPosition = student.transform.position; // This should be stored in StudentAgent
            student.transform.position = seatPosition;

            Log($"[Movement] {student.Config?.studentName} returned to seat");

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.StudentReturnedToSeat,
                    "returned to seat"
                );
            }
        }

        private void UpdateActiveMovements()
        {
            List<StudentAgent> toRemove = new List<StudentAgent>();

            foreach (var kvp in activeMovements)
            {
                StudentAgent student = kvp.Key;
                StudentMovementState state = kvp.Value;

                if (student == null || state.route == null)
                {
                    toRemove.Add(student);
                    continue;
                }

                // Handle waiting at waypoint
                if (state.isWaiting)
                {
                    if (Time.time >= state.waitEndTime)
                    {
                        state.isWaiting = false;
                        state.isMoving = true;
                        
                        // Move to next waypoint
                        if (!AdvanceToNextWaypoint(state))
                        {
                            toRemove.Add(student);
                        }
                    }
                    continue;
                }

                // Handle movement to current waypoint
                if (state.isMoving)
                {
                    StudentWaypoint targetWaypoint = state.route.GetWaypoint(state.currentWaypointIndex);
                    
                    if (targetWaypoint == null)
                    {
                        toRemove.Add(student);
                        continue;
                    }

                    Vector3 targetPosition = targetWaypoint.transform.position;
                    Vector3 currentPosition = student.transform.position;
                    float distance = Vector3.Distance(currentPosition, targetPosition);

                    // Check if reached waypoint
                    if (distance <= waypointReachThreshold)
                    {
                        OnWaypointReached(student, state, targetWaypoint);
                    }
                    else
                    {
                        // Move toward waypoint
                        MoveTowardWaypoint(student, state, targetPosition);
                    }
                }
            }

            // Remove completed movements
            foreach (StudentAgent student in toRemove)
            {
                if (activeMovements.TryGetValue(student, out StudentMovementState state))
                {
                    OnRouteCompleted(student, state);
                }
                activeMovements.Remove(student);
            }
        }

        private void MoveTowardWaypoint(StudentAgent student, StudentMovementState state, Vector3 targetPosition)
        {
            Vector3 currentPosition = student.transform.position;
            Vector3 direction = (targetPosition - currentPosition).normalized;

            // Rotate toward target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                student.transform.rotation = Quaternion.RotateTowards(
                    student.transform.rotation,
                    targetRotation,
                    state.route.rotationSpeed * Time.deltaTime
                );
            }

            // Move toward target
            Vector3 movement = direction * state.movementSpeed * Time.deltaTime;
            student.transform.position += movement;
        }

        private void OnWaypointReached(StudentAgent student, StudentMovementState state, StudentWaypoint waypoint)
        {
            Log($"[Movement] {student.Config?.studentName} reached waypoint: {waypoint.waypointName}");

            // Trigger action if configured
            if (waypoint.triggerAction && waypoint.actionToTrigger != StudentActionType.Idle)
            {
                Log($"[Movement] {student.Config?.studentName} triggered action: {waypoint.actionToTrigger}");
            }

            // Wait at waypoint if configured
            if (waypoint.waitDuration > 0)
            {
                state.isMoving = false;
                state.isWaiting = true;
                state.waitEndTime = Time.time + waypoint.waitDuration;
            }
            else
            {
                // Move to next waypoint immediately
                if (!AdvanceToNextWaypoint(state))
                {
                    // Route completed
                    return;
                }
            }
        }

        private bool AdvanceToNextWaypoint(StudentMovementState state)
        {
            if (state.route.isPingPong)
            {
                // Ping-pong mode
                if (state.isReversing)
                {
                    state.currentWaypointIndex--;
                    if (state.currentWaypointIndex < 0)
                    {
                        state.currentWaypointIndex = 1;
                        state.isReversing = false;
                    }
                }
                else
                {
                    state.currentWaypointIndex++;
                    if (state.currentWaypointIndex >= state.route.WaypointCount)
                    {
                        state.currentWaypointIndex = state.route.WaypointCount - 2;
                        state.isReversing = true;
                        
                        if (state.currentWaypointIndex < 0)
                        {
                            return false; // Route completed
                        }
                    }
                }
            }
            else
            {
                // Normal or looping mode
                state.currentWaypointIndex++;
                
                if (state.currentWaypointIndex >= state.route.WaypointCount)
                {
                    if (state.route.isLooping)
                    {
                        state.currentWaypointIndex = 0;
                    }
                    else
                    {
                        return false; // Route completed
                    }
                }
            }

            return true;
        }

        private void OnRouteCompleted(StudentAgent student, StudentMovementState state)
        {
            Log($"[Movement] {student.Config?.studentName} completed route: {state.route.routeName}");

            if (state.route.triggerEventOnCompletion && StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.WanderingAround,
                    $"completed route: {state.route.routeName}"
                );
            }

            // Handle completion behavior
            switch (state.route.completionBehavior)
            {
                case RouteCompletionBehavior.Stop:
                    // Do nothing, student stays at final position
                    break;

                case RouteCompletionBehavior.ReturnToSeat:
                    ReturnToSeat(student);
                    break;

                case RouteCompletionBehavior.ResumeAutonomous:
                    // Student will resume autonomous behavior automatically
                    break;

                case RouteCompletionBehavior.WaitForTeacher:
                    // Student waits for teacher intervention
                    break;
            }

            // Change state if configured
            if (state.route.completionState != student.CurrentState)
            {
                student.ChangeState(state.route.completionState);
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning(message);
            }
        }

        void OnDrawGizmos()
        {
            if (!drawRouteGizmos || !isActive) return;

            foreach (var kvp in activeMovements)
            {
                StudentAgent student = kvp.Key;
                StudentMovementState state = kvp.Value;

                if (student == null || state.route == null) continue;

                // Draw line from student to current target waypoint
                StudentWaypoint targetWaypoint = state.route.GetWaypoint(state.currentWaypointIndex);
                if (targetWaypoint != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(student.transform.position, targetWaypoint.transform.position);
                }

                // Draw entire route
                Gizmos.color = Color.cyan;
                for (int i = 0; i < state.route.WaypointCount - 1; i++)
                {
                    StudentWaypoint wp1 = state.route.GetWaypoint(i);
                    StudentWaypoint wp2 = state.route.GetWaypoint(i + 1);
                    
                    if (wp1 != null && wp2 != null)
                    {
                        Gizmos.DrawLine(wp1.transform.position, wp2.transform.position);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tracks the current movement state of a student following a route
    /// </summary>
    internal class StudentMovementState
    {
        public StudentAgent student;
        public StudentRoute route;
        public int currentWaypointIndex;
        public bool isMoving;
        public bool isWaiting;
        public float waitEndTime;
        public float movementSpeed;
        public bool isReversing;
    }
}

# Student Waypoint-Based Movement System

## Overview

The Waypoint-Based Movement System enables students to follow predefined paths through the classroom, supporting behaviors like escaping, returning to seats, and purposeful navigation. This system integrates seamlessly with existing architecture without breaking any current functionality.

## Architecture

### Core Components

1. **StudentWaypoint.cs** - Individual waypoint markers in the scene
2. **StudentRoute.cs** - ScriptableObject defining a sequence of waypoints
3. **StudentMovementManager.cs** - Central manager handling all route navigation
4. **StudentAgent** - Extended with movement control methods
5. **LevelConfig** - Stores available routes per level

### Integration Points

```
StudentInfluenceManager (panic) → StudentAgent.StartRoute()
                                        ↓
                            StudentMovementManager.StartRoute()
                                        ↓
                            Update() → MoveTowardWaypoint()
                                        ↓
                            OnWaypointReached() → AdvanceToNextWaypoint()
                                        ↓
                            OnRouteCompleted() → CompletionBehavior
```

## How It Works

### 1. Waypoint System

**StudentWaypoint Component:**
- Place in scene as empty GameObjects
- Configure waypoint ID, name, and behavior
- Optional wait duration at waypoint
- Optional action trigger (look around, fidget, etc.)
- Visual gizmos for scene editing

**Example Setup:**
```
Classroom Layout:
- Waypoint "Seat_Nam" (student's desk)
- Waypoint "Door" (classroom exit)
- Waypoint "Outside" (hallway)
- Waypoint "TeacherDesk" (front of class)
```

### 2. Route Configuration

**StudentRoute ScriptableObject:**
- Create via: Assets → Create → FunClass → Student Route
- Define ordered list of waypoints
- Configure movement speed and behavior
- Set completion behavior

**Route Types:**
- **One-Time**: Student follows route once, then stops
- **Looping**: Route repeats indefinitely
- **Ping-Pong**: Route reverses at end, bounces back and forth

### 3. Movement Mechanics

**Frame-Rate Independent:**
- Uses `Time.deltaTime` for smooth movement
- Configurable rotation speed for turning
- Distance threshold for waypoint detection

**Movement Speed:**
- Walking: 2 m/s (default)
- Running: 4 m/s (default)
- Per-route override available

**Rotation:**
- Smooth rotation toward target waypoint
- Configurable rotation speed (degrees/second)
- Quaternion.RotateTowards for smooth turning

### 4. Integration with Influence System

When peer influence exceeds panic threshold:
```csharp
// In StudentInfluenceManager.ApplyStrongInfluence()
if (strength >= targetStudent.Config.panicThreshold)
{
    LevelConfig level = LevelManager.Instance.GetCurrentLevelConfig();
    if (level.escapeRoute != null)
    {
        targetStudent.StartRoute(level.escapeRoute);
    }
}
```

## Configuration

### Setting Up Waypoints

1. **Create Waypoint GameObject:**
   - Create empty GameObject in scene
   - Add `StudentWaypoint` component
   - Position at desired location

2. **Configure Waypoint:**
   ```
   Waypoint ID: "door_01"
   Waypoint Name: "Classroom Door"
   Wait Duration: 0.5 (seconds)
   Trigger Action: false
   Show Gizmo: true
   Gizmo Color: Yellow
   ```

3. **Repeat for all waypoints** in your level

### Creating Routes

1. **Create Route Asset:**
   - Right-click in Project → Create → FunClass → Student Route
   - Name it descriptively (e.g., "EscapeRoute", "ReturnToSeatRoute")

2. **Configure Route:**
   ```
   Route ID: "escape_route_01"
   Route Name: "Escape Route"
   
   Waypoints:
   - [0] Seat_Nam
   - [1] Door
   - [2] Outside
   
   Is Looping: false
   Is Ping Pong: false
   
   Movement Speed: 4.0 (running)
   Is Running: true
   Rotation Speed: 180
   
   Completion Behavior: Stop
   Completion State: Critical
   Trigger Event On Completion: true
   ```

3. **Assign to LevelConfig:**
   - Open your LevelConfig asset
   - Add route to "Available Routes"
   - Set as "Escape Route" if applicable

### LevelConfig Setup

**Movement Routes Section:**
```
Available Routes:
- EscapeRoute
- ReturnRoute
- WanderRoute

Escape Route: EscapeRoute (assigned)
Return Route: ReturnRoute (assigned)
```

**Key Locations Section:**
```
Classroom Door: [Assign Transform]
Outside Area: [Assign Transform]
Seat Positions:
- [0] Seat_Nam
- [1] Seat_Lan
- [2] Seat_Minh
```

## Usage Examples

### Example 1: Escape Route (Panic Behavior)

**Scenario:** Student panics from peer influence and runs out of classroom

**Setup:**
1. Create waypoints: Seat → Door → Outside
2. Create EscapeRoute with these waypoints
3. Set movement speed to 4.0 (running)
4. Assign to LevelConfig.escapeRoute

**Trigger:**
```csharp
// Automatically triggered by StudentInfluenceManager
// when panic threshold exceeded
```

**Expected Behavior:**
```
[Influence] Nam → reaction Scared (strength 0.85)
[Influence] Nam started escape route due to panic
[Movement] Nam started route: Escape Route
[Movement] Nam reached waypoint: Door
[Movement] Nam reached waypoint: Outside
[Movement] Nam completed route: Escape Route
```

### Example 2: Return to Seat Route

**Scenario:** Teacher calls student back to their seat

**Setup:**
1. Create waypoints: Outside → Door → Seat
2. Create ReturnRoute with these waypoints
3. Set movement speed to 2.0 (walking)
4. Set completion behavior to "ReturnToSeat"

**Trigger:**
```csharp
// Teacher action or manual call
student.StartRoute(levelConfig.returnRoute);
```

**Expected Behavior:**
```
[Movement] Nam starting route: Return Route
[Movement] Nam reached waypoint: Door
[Movement] Nam reached waypoint: Seat_Nam
[Movement] Nam completed route: Return Route
[StudentEvent] Nam: returned to seat
```

### Example 3: Looping Patrol Route

**Scenario:** Student wanders around classroom in a loop

**Setup:**
1. Create waypoints: Seat → Window → BookShelf → Door → Seat
2. Create WanderRoute with these waypoints
3. Set "Is Looping" to true
4. Set movement speed to 1.5 (slow walk)

**Trigger:**
```csharp
student.StartRoute(wanderRoute);
```

**Expected Behavior:**
- Student continuously walks the loop
- Can be interrupted by teacher actions
- Resumes autonomous behavior if stopped

### Example 4: Ping-Pong Route

**Scenario:** Student paces back and forth

**Setup:**
1. Create waypoints: Seat → Window
2. Create PaceRoute with these waypoints
3. Set "Is Ping Pong" to true
4. Set movement speed to 2.0

**Expected Behavior:**
- Student walks to Window
- Turns around and walks back to Seat
- Repeats indefinitely

## StudentAgent API

### Public Methods

**StartRoute(StudentRoute route)**
```csharp
// Start student on a predefined route
student.StartRoute(escapeRoute);
```

**StopRoute()**
```csharp
// Stop current route movement
student.StopRoute();
```

**ReturnToSeat()**
```csharp
// Force student back to original seat
student.ReturnToSeat();
```

**GetCurrentRoute()**
```csharp
// Get the route student is currently following
StudentRoute currentRoute = student.GetCurrentRoute();
if (currentRoute != null)
{
    Debug.Log($"Following: {currentRoute.routeName}");
}
```

### Public Properties

**IsFollowingRoute**
```csharp
// Check if student is currently on a route
if (student.IsFollowingRoute)
{
    Debug.Log("Student is moving along a route");
}
```

**OriginalSeatPosition**
```csharp
// Get student's original seat position
Vector3 seatPos = student.OriginalSeatPosition;
```

## Teacher Interaction

### Calling Student Back

```csharp
// In TeacherController or custom script
if (Input.GetKeyDown(KeyCode.R))
{
    if (currentStudentTarget != null)
    {
        currentStudentTarget.ReturnToSeat();
    }
}
```

### Stopping Student Movement

```csharp
// Stop any route the student is following
student.StopRoute();

// Or use existing teacher actions
student.HandleTeacherAction(TeacherActionType.Stop);
```

### Future Extensions

The system is designed to support:
- **Escort behavior**: Teacher walks student back
- **Follow teacher**: Student follows teacher's position
- **Group movement**: Multiple students follow same route
- **Dynamic waypoints**: Runtime waypoint creation

## Route Completion Behaviors

### Stop
```
Completion Behavior: Stop
```
- Student stops at final waypoint
- Remains at that position
- Can be moved by teacher or new route

### ReturnToSeat
```
Completion Behavior: ReturnToSeat
```
- Automatically returns to original seat
- Triggers StudentReturnedToSeat event
- Resumes normal behavior

### ResumeAutonomous
```
Completion Behavior: ResumeAutonomous
```
- Student resumes autonomous behavior
- Starts performing random actions again
- Normal state transitions apply

### WaitForTeacher
```
Completion Behavior: WaitForTeacher
```
- Student waits at final waypoint
- No autonomous behavior
- Requires teacher intervention

## Event Logging

All movement actions generate detailed logs:

```
[StudentMovementManager] Movement system activated
[Movement] Nam started route: Escape Route
[Movement] Nam moving to waypoint: Door
[Movement] Nam reached waypoint: Door
[Movement] Nam reached waypoint: Outside
[Movement] Nam completed route: Escape Route
[StudentEvent] Nam: started route: Escape Route
[StudentEvent] Nam: completed route: Escape Route
```

## Performance Considerations

### Efficient Design

- **Update Loop**: Only active students are updated
- **Distance Checks**: Simple Vector3.Distance, no physics
- **Early Exit**: Completed routes removed from update loop
- **No Raycasts**: Direct position manipulation

### Performance Tips

1. **Limit Active Routes**: Keep number of simultaneously moving students reasonable
2. **Waypoint Count**: 3-5 waypoints per route is optimal
3. **Update Frequency**: Movement updates every frame, but efficiently
4. **Gizmo Drawing**: Disable in builds for performance

## Integration with Existing Systems

### ✅ Compatible With

- **StudentAgent**: Uses public methods, no core logic changes
- **StudentInfluenceManager**: Triggers escape routes on panic
- **StudentEventManager**: Logs all movement events
- **TeacherController**: Can call ReturnToSeat() and StopRoute()
- **Sequence System**: Routes can be interrupted by sequences
- **ClassroomManager**: Works alongside disruption tracking

### ❌ Does Not Break

- Autonomous student behaviors (skipped when on route)
- Teacher-student interactions
- Item confiscation system
- Peer influence system
- Level objectives and scoring

## Troubleshooting

### Student not moving along route

**Check:**
- StudentMovementManager exists in scene
- Route has valid waypoints assigned
- Waypoints are positioned correctly in scene
- Student is not in a sequence
- Debug logs are enabled

### Student moves too fast/slow

**Solutions:**
- Adjust `movementSpeed` in StudentRoute
- Use `isRunning` flag for automatic speed adjustment
- Check `defaultMovementSpeed` in StudentMovementManager

### Student doesn't reach waypoint

**Check:**
- `waypointReachThreshold` in StudentMovementManager (default: 0.3m)
- Waypoint positions are accessible
- No obstacles blocking path
- Student is not being interrupted

### Route doesn't loop

**Check:**
- `isLooping` is set to true in StudentRoute
- Route has at least 2 waypoints
- Completion behavior is not overriding loop

### Escape route not triggering

**Check:**
- LevelConfig has escapeRoute assigned
- Student's panic threshold is configured
- Influence strength exceeds panic threshold
- StudentInfluenceManager is active

## Advanced Features

### Custom Waypoint Actions

Extend `StudentActionType` enum for custom actions:
```csharp
public enum StudentActionType
{
    None,
    LookAround,
    Fidget,
    StandStill,
    TurnAround,
    CustomAction  // Add your own
}
```

### Dynamic Route Creation

Create routes at runtime:
```csharp
StudentRoute dynamicRoute = ScriptableObject.CreateInstance<StudentRoute>();
dynamicRoute.routeName = "Dynamic Route";
dynamicRoute.waypoints = new List<StudentWaypoint> { wp1, wp2, wp3 };
dynamicRoute.movementSpeed = 3f;
student.StartRoute(dynamicRoute);
```

### Route Chaining

Implement sequential routes:
```csharp
// In OnRouteCompleted callback
if (state.route.completionBehavior == RouteCompletionBehavior.StartNewRoute)
{
    StudentRoute nextRoute = GetNextRoute();
    student.StartRoute(nextRoute);
}
```

## Best Practices

1. **Name Waypoints Clearly**: Use descriptive names like "Door_Main", "Seat_Row1_Desk3"
2. **Test Routes in Scene View**: Use gizmos to visualize paths
3. **Reasonable Speeds**: 1-2 m/s for walking, 3-5 m/s for running
4. **Short Routes**: 3-5 waypoints keeps movement purposeful
5. **Wait Durations**: 0.5-2 seconds at waypoints feels natural
6. **Completion Behaviors**: Match behavior to narrative intent

## Future Extensions

Potential areas for expansion:

- **Obstacle Avoidance**: NavMesh integration for pathfinding
- **Animation Integration**: Trigger walk/run animations
- **Group Coordination**: Multiple students follow leader
- **Dynamic Waypoints**: Create waypoints at runtime
- **Route Conditions**: Conditional branching in routes
- **Speed Variation**: Gradual acceleration/deceleration
- **Path Smoothing**: Bezier curves between waypoints

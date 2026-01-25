# FunClass - Classroom Management Simulation Game

## 📋 Table of Contents
1. [Project Overview](#project-overview)
2. [Business Logic & Game Concept](#business-logic--game-concept)
3. [Technical Architecture](#technical-architecture)
4. [Core Systems](#core-systems)
5. [Current Progress](#current-progress)
6. [Known Issues & Bugs](#known-issues--bugs)
7. [Future Roadmap](#future-roadmap)

---

## 🎮 Project Overview

**FunClass** is a Unity-based classroom management simulation game where players take the role of a teacher managing a chaotic classroom. The game focuses on understanding student behavior, managing disruptions, and maintaining classroom order through strategic interventions.

### Core Concept
Players must manage a classroom of students with unique personalities and behaviors. Students influence each other through various interactions (hitting, throwing objects, making noise, vomiting). The teacher must identify root causes of disruptions and resolve them before the classroom becomes unmanageable.

### Target Platform
- Unity 2021.3+ (LTS)
- PC (Windows/Mac/Linux)
- 3D First-Person Perspective

---

## 🎯 Business Logic & Game Concept

### Game Objectives

**Primary Goal:** Maintain classroom order by keeping disruption below threshold

**Win Conditions:**
- Keep disruption level below maximum threshold (default: 80/100)
- Survive for the level duration (default: 300 seconds)
- Complete required objectives (calm X students, resolve Y problems)

**Loss Conditions:**
- Disruption reaches catastrophic level (95/100)
- Disruption stays above timeout threshold (70) for too long (45 seconds)
- Time runs out with objectives incomplete

### Core Gameplay Loop

```
Student Event Occurs (vomit, hit, throw, etc.)
    ↓
Event affects nearby students (influence system)
    ↓
Affected students escalate (Calm → Distracted → Acting Out → Critical)
    ↓
Disruption increases
    ↓
Teacher identifies problem source
    ↓
Teacher takes action (calm student, clean mess, resolve influence)
    ↓
Students de-escalate, disruption decreases
    ↓
Loop continues with new events
```

### Student State Machine

**States:**
1. **Calm** (😌) - Normal, focused, no disruption
2. **Distracted** (😐) - Slightly unfocused, minor disruption
3. **Acting Out** (😠) - Disruptive behavior, moderate disruption
4. **Critical** (😡) - Severe disruption, may leave classroom

**Escalation Triggers:**
- Influenced by other students (hit, thrown object, noise)
- Affected by mess objects (vomit, poop)
- Personality traits (low patience, high impulsiveness)
- Time spent in current state

**De-escalation:**
- Teacher intervention (Calm action)
- Resolving influence sources
- Natural cooldown (if no active influences)

### Influence System

**Influence Types:**

1. **Student-to-Student Influence**
   - **ThrowingObject**: Student A throws object at Student B
   - **Hitting**: Student A hits Student B
   - **MakingNoise**: Student A makes noise affecting nearby students
   - **WanderingAround**: Student A wanders, distracting others

2. **Object-Based Influence**
   - **Vomit Mess**: Affects all students in range (smell)
   - **Poop Mess**: Affects all students in range (smell)
   - **Knocked Over Objects**: Visual distraction

**Influence Scope:**
- **SingleStudent**: Affects one specific target
- **WholeClass**: Affects all students in range
- **None**: No influence (internal state change)

**Influence Resolution:**
- **Student sources**: Calm the source student
- **Object sources**: Clean the mess object
- **Unresolved sources**: Students may re-escalate after being calmed

### Personality System

Each student has unique personality traits affecting behavior:

```json
{
  "patience": 0.0-1.0,           // Low = escalates quickly
  "attentionSpan": 0.0-1.0,      // Low = easily distracted
  "impulsiveness": 0.0-1.0,      // High = unpredictable actions
  "panicThreshold": 0.0-1.0,     // Low = panics easily
  "influenceSusceptibility": 0.0-1.0,  // High = easily influenced
  "influenceResistance": 0.0-1.0       // High = resists influence
}
```

### Teacher Actions

**Available Actions:**
1. **Calm** - De-escalate student one state level (-5 disruption)
2. **Escort Back** - Return outside student to seat (if Critical state)
3. **Call Back** - Verbally recall student to seat (if not Critical)
4. **Clean Mess** - Remove mess object, resolve influence source
5. **Resolve Influence** - Via GUI popup system (new)

---

## 🏗️ Technical Architecture

### Project Structure

```
Assets/
├── Scripts/
│   ├── Core/                    # Core game systems
│   │   ├── StudentAgent.cs      # Student behavior & state
│   │   ├── TeacherController.cs # Player controller
│   │   ├── ClassroomManager.cs  # Classroom state management
│   │   ├── StudentInfluenceManager.cs  # Influence system
│   │   ├── StudentMovementManager.cs   # Student movement
│   │   ├── LevelManager.cs      # Level loading & goals
│   │   ├── GameStateManager.cs  # Game state machine
│   │   └── UI/                  # UI systems
│   │       ├── PopupManager.cs
│   │       ├── StudentInteractionPopup.cs
│   │       └── PopupTextLoader.cs
    │   └── Editor/                  # Editor tools
    │       └── Modules/
    │           ├── JSONLevelImporter.cs       # Legacy level creation
    │           ├── UnifiedLevelImporter.cs    # Unified import system (Auto/Manual/Hybrid)
    │           ├── EnhancedLevelImportWindow.cs # Import UI window
    │           ├── StudentPlacementManager.cs # Student placement logic
    │           ├── RouteGenerator.cs          # Route auto-generation
    │           ├── EnvironmentSetup.cs        # Environment configuration
    │           └── EditorUtils.cs             # Common editor utilities
├── LevelTemplates/              # JSON level configurations
├── Resources/                   # Runtime-loaded assets
└── Scenes/                      # Unity scenes
```

### Key Design Patterns

1. **Singleton Pattern**
   - `ClassroomManager`, `StudentInfluenceManager`, `LevelManager`
   - Ensures single instance, global access

2. **Event System**
   - `StudentEvent` - Encapsulates student actions
   - `StudentEventManager` - Broadcasts events
   - Decoupled communication between systems

3. **State Machine**
   - `StudentState` enum
   - `StudentAgent.EscalateState()` / `DeescalateState()`
   - Clear state transitions

4. **Component-Based Architecture**
   - `StudentAgent` - Core behavior
   - `StudentMovementManager` - Movement logic
   - `StudentMessCreator` - Mess creation
   - Separation of concerns

5. **Data-Driven Design**
   - Unified JSON schema with Auto/Manual/Hybrid import modes
   - Legacy JSON level configurations (backward compatible)
   - Externalized text (popup system)
   - ScriptableObjects for configs (StudentConfig, LevelConfig, etc.)

### Core Systems Integration

```
GameStateManager
    ↓
LevelManager → Loads LevelConfig
    ↓
ClassroomManager ← Tracks disruption
    ↓
StudentAgent ← Handles behavior
    ↓
StudentInfluenceManager ← Processes influence
    ↓
StudentEventManager ← Broadcasts events
    ↓
TeacherController ← Player input
    ↓
PopupManager ← GUI interaction
```

---

## ⚙️ Core Systems

### 1. Student Behavior System

**File:** `StudentAgent.cs` (42KB, ~1400 lines)

**Responsibilities:**
- State management (Calm/Distracted/Acting Out/Critical)
- Behavior execution (fidget, look around, stand up, wander)
- Teacher action handling
- Influence source tracking

**Key Methods:**
```csharp
void EscalateState()           // Move to next worse state
void DeescalateState()         // Move to next better state
void HandleTeacherAction()     // Process teacher intervention
void ExecuteBehavior()         // Perform behavior based on state
void InteractWithTeacher()     // Teacher interaction callback
```

**State Transition Logic:**
- Automatic escalation based on time + personality
- Influence-driven escalation (hit, noise, mess)
- Teacher-driven de-escalation (calm action)

### 2. Influence System

**File:** `StudentInfluenceManager.cs` (27KB, ~600 lines)

**Responsibilities:**
- Process student events into influences
- Track influence sources per student
- Calculate influence strength based on distance/personality
- Resolve influences when sources are removed

**Key Concepts:**

**InfluenceSource:**
```csharp
class InfluenceSource {
    StudentAgent sourceStudent;    // Who caused it
    StudentEventType eventType;    // What they did
    float strength;                // How strong (0-1)
    float timestamp;               // When it happened
    bool isResolved;               // Has it been resolved?
}
```

**StudentInfluenceSources:**
```csharp
class StudentInfluenceSources {
    List<InfluenceSource> sources;
    
    void AddSource()               // Add new influence
    void ResolveSource()           // Mark as resolved
    int GetUnresolvedCount()       // Count active influences
    List<InfluenceSource> GetActiveSources()
}
```

**Influence Processing:**
1. Student performs action (vomit, hit, throw)
2. Event fired via `StudentEventManager`
3. `StudentInfluenceManager` receives event
4. Determines affected students (range, line of sight)
5. Creates `InfluenceSource` for each affected student
6. Affected students escalate based on influence strength

### 3. Movement System

**File:** `StudentMovementManager.cs` (21KB, ~600 lines)

**Responsibilities:**
- Student pathfinding and navigation
- Route following (escape routes)
- Return to seat logic
- Waypoint system integration

**Movement Types:**
1. **Idle at Seat** - Default position
2. **Wander Around** - Random movement near seat
3. **Follow Route** - Escape to door/outside
4. **Return to Seat** - Escort or recall

**Route System:**
```csharp
class StudentRoute {
    string routeName;              // e.g., "EscapeRoute"
    List<StudentWaypoint> waypoints;
    RouteCompletionBehavior behavior;  // Stop, Loop, Reverse
}
```

### 4. Mess System

**File:** `MessObject.cs` (6KB)

**Responsibilities:**
- Represent mess objects (vomit, poop)
- Emit continuous influence to nearby students
- Cleanable by teacher
- Visual representation

**Mess Types:**
1. **VomitMess** - Created when student vomits
2. **PoopMess** - Created when student poops (future)
3. **KnockedOverObject** - Furniture/objects knocked over

**Cleanup Flow:**
```
Teacher approaches mess
    ↓
Press E to clean
    ↓
Mess object destroyed
    ↓
Influence sources resolved for all affected students
    ↓
Students may de-escalate if no other sources
```

### 5. Level Management System

**Files:** 
- `LevelManager.cs` (12KB)
- `LevelConfig.cs` (1.5KB)
- `LevelGoalConfig.cs` (3KB)

**Responsibilities:**
- Load level configurations from JSON
- Spawn students with configs
- Track level goals and completion
- Handle level win/loss conditions

**Level Configuration Formats:**

**Legacy JSON Format:**
```json
{
  "levelName": "Level 1",
  "sceneName": "ClassroomScene",
  "goalSettings": {
    "maxDisruptionThreshold": 80,
    "timeLimitSeconds": 300,
    "requiredCalmDowns": 3
  },
  "students": [
    {
      "studentName": "Student_A",
      "position": {"x": -1.0, "y": 0.1, "z": 0},
      "personality": {
        "patience": 0.2,
        "impulsiveness": 0.8
      }
    }
  ]
}
```

**Unified JSON Schema (New):**
Supports three import modes: **Auto** (full auto-generation), **Manual** (legacy positions), **Hybrid** (mix of auto and manual).
```json
{
  "levelId": "UnifiedAutoDemo",
  "difficulty": "medium",
  "students": 6,
  "deskLayout": {
    "spacingX": 2.2,
    "spacingZ": 2.5,
    "aisleWidth": 1.8
  },
  "classroom": {
    "width": 12,
    "depth": 10,
    "height": 3.5
  },
  "environment": {
    "boardMaterial": "White",
    "floorMaterial": "Floor",
    "wallMaterial": "Wall",
    "autoSetupLighting": true
  },
  "routeGeneration": {
    "autoGenerateRoutes": true,
    "escapeRouteSpeed": 3.0,
    "returnRouteSpeed": 2.0
  },
  "goalSettings": { ... },
  "influenceScopeSettings": { ... },
  "studentInteractions": [ ... ],
  "studentConfigs": [ ... ]
}
```

**Import Modes:**
1. **Auto Mode**: Generates desk grid, places students, creates routes automatically
2. **Manual Mode**: Uses legacy JSON format with exact positions
3. **Hybrid Mode**: Mix of auto-generation with manual overrides

**Import Tool:** `Tools > FunClass > Import Level From JSON`

### 6. GUI Popup System

**Files:**
- `PopupManager.cs` (8KB)
- `StudentInteractionPopup.cs` (18KB)
- `PopupTextLoader.cs` (10KB)

**Responsibilities:**
- Display student information on click
- Show influence sources (who is affecting whom)
- Provide action buttons for resolving influences
- Dynamic content based on student role (source vs target)

**Popup Types:**

1. **Target Student Popup** - Shows who/what is affecting this student
2. **Source Info Only** - Shows impact, no actions (object-based)
3. **Source WholeClass Action** - Resolve for entire class
4. **Source Individual Actions** - Resolve for each affected student

**Current Implementation Status:** ✅ Implemented, 🐛 Has bugs (see Known Issues)

### 7. Teacher Controller

**File:** `TeacherController.cs` (38KB, ~900 lines)

**Responsibilities:**
- First-person camera control
- Student interaction detection (raycast)
- Input handling (WASD, mouse, E key, R key)
- Action execution (calm, escort, clean)

**Input Mapping:**
- **Left Click** - Open student popup (new)
- **E Key** - Calm student / Clean mess
- **R Key** - Recall/Escort student back
- **ESC** - Close popup
- **WASD** - Movement
- **Mouse** - Camera rotation

---

## 📊 Current Progress

### ✅ Completed Features

#### Core Gameplay (100%)
- ✅ Student state machine (4 states)
- ✅ Student behavior system (fidget, look, stand, wander)
- ✅ Teacher first-person controller
- ✅ Disruption tracking and thresholds
- ✅ Win/loss conditions
- ✅ Level timer and goals

#### Influence System (100%)
- ✅ Student-to-student influence (hit, throw, noise)
- ✅ Object-based influence (vomit mess)
- ✅ Influence source tracking per student
- ✅ Influence resolution on source removal
- ✅ Range-based influence calculation
- ✅ Personality-based susceptibility

#### Movement System (100%)
- ✅ Student navigation and pathfinding
- ✅ Route following (escape routes)
- ✅ Return to seat logic
- ✅ Waypoint system
- ✅ Runtime waypoint creation from JSON

#### Mess System (100%)
- ✅ Vomit mess creation
- ✅ Mess cleanup by teacher
- ✅ Continuous influence emission
- ✅ Visual representation
- ✅ Integration with influence system

#### Level System (100%)
- ✅ JSON level configuration
- ✅ Auto level creation from JSON
- ✅ Student spawning with configs
- ✅ Level goal tracking
- ✅ Level completion detection

#### Unified Import System (100%)
- ✅ Unified JSON schema with Auto, Manual, Hybrid modes
- ✅ Desk grid auto-generation with proper spacing
- ✅ Student placement and desk binding
- ✅ Route auto-generation with waypoint duplication
- ✅ Environment setup (board, walls, floor, lighting)
- ✅ Config creation (LevelConfig, LevelGoalConfig, InfluenceScopeConfig)
- ✅ Material fixing system
- ✅ StudentConfig assignment to StudentAgent (FIXED)

#### Teacher Actions (100%)
- ✅ Calm student (de-escalate)
- ✅ Clean mess objects
- ✅ Escort student back to seat
- ✅ Call student back verbally
- ✅ Student interaction detection
- ✅ Popup-based influence resolution (fully functional)

#### UI Systems (90%)
- ✅ Disruption meter
- ✅ Score display
- ✅ Timer display
- ✅ Student highlight on hover
- ✅ Student interaction popup (fully functional)
- ❌ Tutorial system (not implemented)

### 🚧 In Progress

#### GUI Popup System (100% complete)
**Status:** ✅ Fully functional and tested

**Completed:**
- ✅ Popup manager with screen-space overlay
- ✅ Dynamic popup type determination
- ✅ Influence source data integration
- ✅ Text externalization (JSON configs)
- ✅ Popup animation (fade/scale)
- ✅ ESC to close popup
- ✅ Camera lock when popup open
- ✅ Student names displaying correctly
- ✅ Action buttons visible and functional
- ✅ Accurate popup content display
- ✅ Fixed font rendering issues

**Recent Work:**
- Fixed race condition causing popup to destroy immediately
- Fixed duplicate CanvasGroup causing visibility issues
- Changed WanderingAround from WholeClass to Individual action
- Cleaned up excessive debug logs
- Fixed ExtractLetter() to return full student names

#### Unified JSON Import System (90% complete)
**Status:** Functional with one remaining StudentConfig assignment issue

**Completed:**
- ✅ Unified JSON schema supporting Auto, Manual, and Hybrid import modes
- ✅ Enhanced JSON importer with improved error handling
- ✅ StudentConfig creation with proper serialization using SerializedObject
- ✅ Desk grid auto-generation with proper student placement
- ✅ Route auto-generation with door/outside waypoint duplication
- ✅ Environment setup (board, walls, floor materials)
- ✅ LevelConfig, LevelGoalConfig, InfluenceScopeConfig creation
- ✅ Enhanced debug logging throughout import pipeline
- ✅ Material fixing system (pink materials)

**Current Issue:**
- 🐛 **StudentConfig assignment to StudentAgent components**: Only 1 of 6 students gets their config assigned during Auto mode import

**Root Cause Identified:**
- StudentConfig assets were being created with empty `studentId` and `studentName` fields due to direct field assignment not persisting to disk
- Config creation in `UnifiedLevelImporter.cs` now uses `SerializedObject` for proper serialization
- Enhanced debug logging added to verify field values at creation time

**Recent Fixes Applied:**
- ✅ **Route Errors Fixed**: "Cannot find route group" errors resolved by enhancing `StudentRoute.RefreshWaypointsFromScene()` with recursive search and fallback matching
- ✅ **StudentConfig Serialization**: Replaced direct field assignment with `SerializedObject` for all config fields
- ✅ **Validation Added**: Added checks for null/empty `studentId` and `studentName` fields
- ✅ **Clean State**: Now deleting and recreating config assets to ensure clean state
- ✅ **Debug Logging Enhanced**: Added comprehensive logging to `StudentPlacementManager`, `UnifiedLevelImporter`, and `RouteGenerator`

**Files Modified:**
- `Assets/Scripts/Editor/Modules/UnifiedLevelImporter.cs` - Config serialization fixes, enhanced logging
- `Assets/Scripts/Editor/Modules/StudentPlacementManager.cs` - Student identifier creation logging
- `Assets/Scripts/Core/StudentRoute.cs` - Route group finding logic
- `Assets/Scripts/Editor/Modules/RouteGenerator.cs` - Waypoint duplication for student-specific routes
- `Assets/Scripts/Editor/EnhancedLevelImportWindow.cs` - Import UI window

**Remaining Investigation Needed:**
- Verify config-to-agent matching logic in `AssignStudentConfigsToAgents()`
- Check GameObject naming conventions vs config identifiers
- Test fresh import with deleted old configs

**Immediate Next Steps:**
1. Delete old configs: `Assets/Configs/UnifiedAutoDemo/Students/`
2. Run fresh import of `unified_auto_example.json`
3. Analyze console logs for config creation and assignment
4. Fix any remaining matching logic issues

### ❌ Not Started

- Tutorial system
- Meta-progression (unlocks, achievements)
- Sound effects and music
- Advanced visual effects
- Multiplayer/co-op mode
- Mobile platform support

---

## 🐛 Known Issues & Bugs

### Critical Bugs (Blocking Gameplay)

**None currently** - Game is playable end-to-end

### High Priority Bugs

#### 1. Popup System Display Issues
**Status:** 🟢 FIXED (2026-01-19)
**Severity:** High
**Impact:** GUI system now fully functional

**Symptoms (RESOLVED):**
- ~~Student names not showing in complaint text (only icons visible)~~
- ~~Action buttons for source students not appearing or too small~~
- ~~Popup content sometimes shows wrong student's data~~
- ~~Layout overlap between complaint and target containers~~

**Root Causes Identified:**
- ✅ `CreateTargetActionItem()` was creating buttons separately, not embedded with target items
- ✅ Layout containers had overlapping anchor ranges (both started at 0.3f)
- ✅ Text color (0.9, 0.9, 0.9) had low contrast on dark background
- ✅ Button container too small (only 50px height)

**Fixes Applied:**
- ✅ Created `CreateTargetActionItemWithButton()` - embeds resolve button with each target name
- ✅ Fixed layout anchors: Complaint (50%-100%), Target (15%-50%), Buttons (0%-15%)
- ✅ Changed text color to pure white (1.0, 1.0, 1.0) for better contrast
- ✅ Added debug logging to `DeterminePopupType()` for diagnostics
- ✅ Increased button container vertical space from 50px to 15% of popup height
- ✅ Added horizontal layout for target items (name + button inline)

**Files Modified:**
- Assets/Scripts/Core/UI/StudentInteractionPopup.cs - Major refactor (~150 lines)
- Assets/Scripts/Core/UI/PopupManager.cs - Layout fixes (~40 lines)

**See:** Assets/Scripts/Core/UI/POPUP_BUG_FIXES.md for detailed fix documentation

#### 2. StudentConfig Assignment Issue in Unified Import
**Status:** 🔴 High Priority (Active Investigation)  
**Severity:** High  
**Impact:** Students lack proper personality configs, causing runtime errors

**Symptoms:**
- Only 1 of 6 students gets StudentConfig assigned during Auto mode import
- Runtime error: `[StudentAgent] Could not find config for [studentId] in LevelLoader`
- StudentConfig assets created but fields (`studentId`, `studentName`) may be empty
- StudentAgent components have null Config fields

**Root Cause Identified:**
- StudentConfig serialization issue: Direct field assignment wasn't persisting to disk
- Config-to-agent matching logic may have naming convention mismatch
- Potential issue with SerializedObject property assignment

**Recent Fixes Applied:**
- ✅ Enhanced `UnifiedLevelImporter.CreateStudentConfigsFromPairs()` to use `SerializedObject` for all field assignments
- ✅ Added validation for null/empty `studentId` and `studentName` fields
- ✅ Added debug logging to verify field values at creation and assignment time
- ✅ Now deleting and recreating config assets to ensure clean state
- ✅ Enhanced `AssignStudentConfigsToAgents()` with detailed logging of matching attempts

**Files Modified:**
- `Assets/Scripts/Editor/Modules/UnifiedLevelImporter.cs` - Serialization fixes, enhanced logging
- `Assets/Scripts/Editor/Modules/StudentPlacementManager.cs` - Student identifier creation logging

**Immediate Investigation:**
1. Verify config creation: Are `studentId`/`studentName` properly serialized to disk?
2. Check matching logic: Do config identifiers match student GameObject names?
3. Test fresh import after deleting old configs

#### 3. Student Highlight Turns White on Click
**Status:** 🟡 Minor  
**Severity:** Low  
**Impact:** Visual feedback issue

**Symptoms:**
- When clicking a student, their model turns white instead of highlighted color

**Root Cause:**
- `StudentHighlight.SetHighlight()` brightness calculation issue
- Line 56-58 in StudentHighlight.cs: `brightColor = originalMaterials[i].color * 1.5f`

**Fix Required:**
- Adjust highlight color calculation
- Use proper shader properties instead of material color multiplication

### Medium Priority Bugs

#### 3. Influence Event Logging Spam
**Status:** 🟢 Fixed  
**Severity:** Low  
**Impact:** Console spam, performance

**Symptoms:**
- Excessive debug logs from StudentRoute, StudentInfluenceManager
- Console becomes unreadable during gameplay

**Fix Applied:**
- Removed ~50+ debug log statements
- Kept only essential logs (popup type, errors)
- Console now clean and readable

#### 4. Popup Animation Timing
**Status:** 🟡 Minor  
**Severity:** Low  
**Impact:** UX polish

**Symptoms:**
- Popup fade/scale animation may feel too slow or fast
- Animation timing not tuned

**Fix Required:**
- Adjust fadeInDuration and scaleInDuration in PopupAnimator
- Test with different values for best feel

### Low Priority Issues

#### 5. Student Pathfinding Edge Cases
**Status:** 🟡 Known  
**Severity:** Low  
**Impact:** Occasional navigation issues

**Symptoms:**
- Students occasionally get stuck on furniture
- Waypoint following may fail if waypoints are missing

**Workaround:**
- Ensure proper NavMesh baking
- Verify waypoint placement in scene

#### 6. Performance with Many Students
**Status:** 🟡 Known  
**Severity:** Low  
**Impact:** FPS drop with 20+ students

**Symptoms:**
- Frame rate drops with large number of students
- Influence calculations may be expensive

**Optimization Needed:**
- Spatial partitioning for influence range checks
- Object pooling for mess objects
- Reduce Update() frequency for non-critical systems

---

## 🔮 Future Roadmap

### Phase 1: Bug Fixes & Polish (Current)
**Timeline:** 1-2 weeks  
**Priority:** Critical

- [x] Fix popup text rendering issues (✅ COMPLETED)
- [x] Fix action button visibility (✅ COMPLETED)
- [x] Verify popup content accuracy (✅ COMPLETED)
- [ ] Fix StudentConfig assignment in Unified Import System
- [ ] Polish student highlight effect
- [ ] Performance optimization
- [ ] Complete Unified Import System testing

### Phase 2: Core Feature Completion
**Timeline:** 2-3 weeks  
**Priority:** High

- [ ] Tutorial system implementation
- [ ] Sound effects and music
- [ ] Visual effects polish (particles, shaders)
- [ ] Level progression system
- [ ] Save/load system

### Phase 3: Advanced Features
**Timeline:** 3-4 weeks  
**Priority:** Medium

- [ ] Auto re-escalate system (see IDEAS.md)
- [ ] Advanced personality system
- [ ] More student behaviors
- [ ] More mess types
- [ ] More teacher actions
- [ ] Achievement system

### Phase 4: Content Expansion
**Timeline:** 4-6 weeks  
**Priority:** Low

- [ ] More levels (10+ levels)
- [ ] Different classroom layouts
- [ ] Special events (fire drill, assembly)
- [ ] Student customization
- [ ] Difficulty modes

### Phase 5: Polish & Release
**Timeline:** 2-3 weeks  
**Priority:** Critical

- [ ] Full QA testing
- [ ] Performance optimization
- [ ] UI/UX polish
- [ ] Localization (English/Vietnamese)
- [ ] Build and packaging
- [ ] Release preparation

---

## 📝 Design Decisions & Rationale

### Why Screen-Space Overlay for Popups?
**Decision:** Use ScreenSpaceOverlay instead of WorldSpace for popup canvas

**Rationale:**
- Web-style modal popup feel
- Always centered on screen
- No rotation with camera
- Easier to implement overlay background
- Better for UI interaction

**Alternative Considered:** WorldSpace canvas above student
**Rejected Because:** Rotates with camera, hard to read, positioning issues

### Why Influence Source Tracking?
**Decision:** Track individual influence sources per student

**Rationale:**
- Enables root cause analysis
- Allows targeted resolution
- Supports GUI popup system
- Realistic behavior (students remember who hit them)

**Alternative Considered:** Simple state-based system
**Rejected Because:** No way to show player what's wrong, less strategic depth

### Why JSON Level Configuration?
**Decision:** Use JSON files for level configs instead of Unity scenes

**Rationale:**
- Easy to create new levels without Unity Editor
- Version control friendly
- Can be edited by non-programmers
- Supports procedural generation
- Faster iteration
- **Evolution to Unified Schema**: Extended to support Auto, Manual, and Hybrid import modes for greater flexibility

**Alternative Considered:** ScriptableObjects
**Rejected Because:** Requires Unity Editor, harder to version control

### Why No Auto-Resolve on Calm?
**Decision:** Calming a student doesn't automatically resolve their influence sources

**Rationale:**
- Forces player to understand root causes
- Creates strategic depth (who to calm first?)
- Prevents "spam calm" strategy
- More realistic (calming victim doesn't fix bully)

**Alternative Considered:** Auto-resolve all sources on calm
**Rejected Because:** Too easy, removes strategy, unrealistic

---

## 🛠️ Development Tools & Workflow

### Editor Tools

#### Unified Level Import System
**Primary File:** `UnifiedLevelImporter.cs`  
**Supporting Files:** `EnhancedLevelImportWindow.cs`, `StudentPlacementManager.cs`, `RouteGenerator.cs`, `EnvironmentSetup.cs`  
**Purpose:** Advanced level creation from unified JSON schema with multiple import modes

**Unified JSON Schema Features:**
- **Three Import Modes**: Auto (full auto-generation), Manual (legacy positions), Hybrid (mix)
- **Desk Grid Auto-Generation**: Creates classroom layout based on parameters
- **Student Placement**: Binds students to desks with proper spacing
- **Route Auto-Generation**: Creates escape/return routes with waypoint duplication
- **Environment Setup**: Auto-configures board, walls, floor, lighting
- **Config Creation**: Generates LevelConfig, LevelGoalConfig, InfluenceScopeConfig, StudentConfigs
- **Material Fixing**: Automatically fixes pink/missing materials

**Enhanced Features (vs Legacy):**
- Better error handling and validation
- Enhanced debug logging throughout pipeline
- SerializedObject-based config creation for reliable serialization
- StudentConfig-to-StudentAgent automatic assignment
- Support for student interactions and influence scopes in JSON

**Usage:**
```
Unity Menu → Tools → FunClass → Import Level From JSON
```

#### Legacy Level Creation Tool
**File:** `JSONLevelImporter.cs`  
**Purpose:** Legacy level creation from original JSON format (manual positions only)

**Features:**
- Parse legacy JSON level configuration
- Spawn students at exact specified positions
- Create waypoints for routes
- Set up classroom objects
- One-click level creation

**Usage:** Still available but superseded by Unified Import System

### Debug Tools

#### Level Config Diagnostic
**File:** `LevelConfigDiagnostic.cs`  
**Purpose:** Validate level configurations

**Checks:**
- Student configs valid
- Waypoints exist
- Routes properly configured
- Goal settings reasonable

### Testing Workflow

1. **Unit Testing** - Core systems (influence calculation, state machine)
2. **Integration Testing** - System interactions (influence → escalation)
3. **Playtest** - Full gameplay loop
4. **Bug Tracking** - Document issues in this file
5. **Fix & Verify** - Implement fix, test, update docs

---

## 📚 Documentation Files

### Core Documentation
- `PROJECT_OVERVIEW.md` (this file) - Complete project overview
- `README.md` - Quick start guide (to be created)

### System Documentation
- `GUI_POPUP_SYSTEM_PLAN.md` - Popup system design and implementation
- `StudentInfluenceManager_README.md` - Influence system details
- `StudentMovementSystem_README.md` - Movement and pathfinding
- `MessCleanupSystem_README.md` - Mess object system
- `TeacherRecallSystem_README.md` - Escort and recall mechanics
- `TEACHER_ACTIONS_GUIDE.md` - Teacher action reference

### Design Documentation
- `IDEAS.md` - Future features and design concepts
- `InfluenceScopeSystem_README.md` - Influence scope design (reverted)
- `ItemConfiscationBehavior_README.md` - Item confiscation system

---

## 🎓 Learning Resources

### For New Developers

**Start Here:**
1. Read this file (PROJECT_OVERVIEW.md)
2. Read GUI_POPUP_SYSTEM_PLAN.md (current focus)
3. Open Unity project, explore ClassroomScene
4. Read StudentAgent.cs (core behavior)
5. Read StudentInfluenceManager.cs (influence system)

**Key Concepts to Understand:**
- Unity component-based architecture
- State machines
- Event-driven programming
- Singleton pattern
- Coroutines and async operations

### Code Style Guidelines

**Naming Conventions:**
- Classes: PascalCase (`StudentAgent`)
- Methods: PascalCase (`EscalateState()`)
- Private fields: camelCase (`currentState`)
- Constants: UPPER_SNAKE_CASE (`MAX_DISRUPTION`)

**Documentation:**
- XML comments for public methods
- Inline comments for complex logic
- README files for systems
- Update this file when adding features

---

## 🤝 Contributing

### Git Workflow
1. Create feature branch from `main`
2. Implement feature with tests
3. Update documentation
4. Commit with descriptive message
5. Merge to `main` when stable

### Commit Message Format
```
[Category] Brief description

Detailed explanation if needed

- Change 1
- Change 2
```

**Categories:** Feature, Bugfix, Refactor, Docs, Test, Polish

---

## 📞 Contact & Support

**Project Lead:** [Your Name]  
**Repository:** [Git URL]  
**Issue Tracker:** [Issue Tracker URL]

---

## 📄 License

[License Type] - See LICENSE file for details

---

**Last Updated:** January 24, 2026  
**Version:** 0.9.0 (Pre-Alpha)  
**Status:** Active Development - Unified Import System & StudentConfig Serialization Fixes

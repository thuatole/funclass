# FunClass – Auto Level Import & Asset Setup (A–Z Plan)

## Scope
Upgrade the **current JSON level import system** so that:
- User imports **ONE JSON file**
- Unity automatically:
  - Creates a playable Level Scene
  - Instantiates all required assets & prefabs
  - Applies materials correctly (no pink)
  - Positions desks, students, board, mess zones
  - Generates EscapeRoute & ReturnRoute per student
- Result: **Press Play → level is fully playable**

This document ONLY covers:
- Asset import
- Prefab instantiation
- Positioning
- Route generation
- Visual correctness

❌ Not covered: AI behavior, scoring, win/lose logic.

---

## Phase 0 – Prerequisites (Required)

### 0.1 Unity Project Setup
- Unity version fixed
- Rendering pipeline decided (Built-in or URP)
- Folder structure:
```
Assets/
 ├── Art/
 │    ├── Materials/
 │    ├── Models/
 │    └── Prefabs/
 ├── Levels/
 │    ├── Json/
 │    └── Generated/
 ├── Scripts/
 │    ├── Editor/
 │    └── Runtime/
```

### 0.2 Asset Constraints
- Low-poly preferred
- Single material per mesh if possible
- Desk size roughly uniform

---

## Phase 1 – Asset Discovery & Mapping (Required)

### 1.1 Unity Asset Store Selection
Required asset categories:
- Classroom room / walls
- Desk prefab
- Whiteboard prefab
- Simple props (book, trash)

Rules:
- No baked lighting
- No custom shaders if possible

### 1.2 Asset Mapping Table
Create a ScriptableObject or JSON:
```
AssetKey → Prefab Reference
DESK → Desk_Prefab
BOARD → Whiteboard_Prefab
STUDENT → Student_Prefab
```

Purpose:
- Decouple JSON level data from actual prefab names
- Allow asset swap without changing JSON format

---

## Phase 2 – JSON Level Schema Upgrade (Required)

### 2.1 New JSON Structure
```json
{
  "levelId": "level_01",
  "difficulty": "easy",
  "students": 6,
  "deskLayout": {
    "rows": 2,
    "spacingX": 2.0,
    "spacingZ": 2.5
  },
  "classroom": {
    "width": 10,
    "depth": 8
  }
}
```

### 2.2 Validation Rules
- students must be even
- students between 4–10
- rows always = 2

---

## Phase 3 – Desk Grid Generator (Required)

### 3.1 Desk Placement Logic
- Always 2 rows
- Columns = students / 2
- Origin at classroom center
- Leave aisle gap between rows

### 3.2 Desk Standard
Each desk:
- Has local space for:
  - student standing position
  - mess spawn (front / top)

---

## Phase 4 – Student Placement (Required)

### 4.1 Student–Desk Binding
- Each desk auto-assigns 1 student
- Student position = desk.StudentAnchor
- Store mapping:
```
StudentId → DeskId
```

### 4.2 Visual Debug
- Gizmo shows student slot
- Gizmo shows mess slot

---

## Phase 5 – Route Auto Generation (Required)

### 5.1 Escape Route
Per student:
```
Desk → Aisle → Door → Outside
```

### 5.2 Return Route
Reverse of escape route:
```
Outside → Door → Aisle → Desk
```

Rules:
- Waypoints auto-generated from desk positions
- Door transform configurable
- Routes stored in LevelConfig

---

## Phase 6 – Material Auto-Fix (Required)

### 6.1 Pink Material Detection
On import / play:
- Scan renderers
- Detect missing shader

### 6.2 Auto Apply Default Material
- Create fallback URP/Built-in material
- Assign automatically

---

## Phase 7 – Board & Environment Setup (Required)

### 7.1 Board Placement
- Against front wall
- Centered on classroom width
- Slight offset to avoid Z-fighting

### 7.2 Test Material
- Simple white material
- Used to validate lighting & visibility

---

## Phase 8 – One-Click Level Import Tool (Required)

### 8.1 Editor Tool
Menu:
```
Tools > FunClass > Import Level From JSON
```

Flow:
1. Select JSON file
2. Validate JSON
3. Clear previous generated level
4. Instantiate assets
5. Generate routes
6. Fix materials
7. Save scene

---

## Phase 9 – Validation & Logging (Required)

### 9.1 Console Output
- Desk count
- Student count
- Route count
- Material fixes applied

### 9.2 Fail Fast
If validation fails:
- Abort import
- Log clear error

---

## Phase 10 – Optional Extensions (Optional)

- Multiple classroom sizes
- Different desk layouts
- Randomized prop placement
- Export generated scene to prefab

---

# SINGLE PROMPT FOR AI CODE AGENT

```
You are working on a Unity project called FunClass.

Your task:
Implement an end-to-end system that upgrades the existing JSON level import feature.

Goal:
Given ONE JSON file, Unity should automatically:
- Create a playable classroom level
- Instantiate desks, students, board using mapped prefabs
- Auto-place desks in a 2-row grid
- Bind one student per desk
- Auto-generate EscapeRoute and ReturnRoute per student
- Fix missing/pink materials automatically
- Save the generated level as a scene

Constraints:
- Use Editor scripts where appropriate
- No gameplay logic (AI, scoring, win/lose)
- Focus only on asset import, prefab instantiation, positioning, and routes
- Must validate JSON strictly and fail fast with clear logs

Follow the phases and rules described in the provided Markdown spec exactly.
```

---
# Student Interaction Popup GUI System - Implementation Plan

## Overview
A popup GUI system that appears when the teacher clicks on a student, displaying relevant information and actions based on whether the student is a source of influence or a target of influence.

---

## Design Goals

1. **Clear Information Display**: Show why students are affected and who is affecting them
2. **Contextual Actions**: Only show actions that are actually available for the situation
3. **Natural Language**: Use conversational Vietnamese text that feels natural
4. **Externalized Text**: All text stored in JSON files for easy customization
5. **No Hints**: Don't guide player, let them discover solutions
6. **Dynamic Updates**: Popup updates as influences are resolved

---

## Popup Types

### Type 1: Target Student Popup (Victim)
**When:** Click on a student who is being affected by others
**Purpose:** Show who/what is affecting this student
**Actions:** Escort Back (if applicable)

### Type 2: Source Student Popup - Info Only
**When:** Click on student causing object-based influence (vomit, poop)
**Purpose:** Show impact of their action
**Actions:** None (must resolve via object interaction)

### Type 3: Source Student Popup - WholeClass Action
**When:** Click on student causing class-wide student-resolvable influence (noise)
**Purpose:** Show impact and allow resolution for entire class
**Actions:** "Giải quyết cho cả lớp"

### Type 4: Source Student Popup - Individual Actions
**When:** Click on student causing individual influence (hit, throw)
**Purpose:** Show each affected student and allow individual resolution
**Actions:** "Giải quyết cho X" for each unresolved target

---

## GUI Structure

### Target Student Popup Layout

```
┌─────────────────────────────────────────┐
│  [Student Name] - [State] [Emoji]       │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "[Opening phrase]"                   │
│                                          │
│  [Icon] [Complaint 1]                    │
│  [Icon] [Complaint 2]                    │
│  ...                                     │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [Action Buttons]                        │
└─────────────────────────────────────────┘
```

**Components:**
- Header: Student name, state, emoji
- Opening phrase: "Cô ơi!"
- Complaint list: Natural language complaints
- Action buttons: Escort Back (if applicable), Close

---

### Source Student Popup Layout (Info Only)

```
┌─────────────────────────────────────────┐
│  [Student Name] - [State] [Emoji]       │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "[Student excuse/statement]"         │
│                                          │
│  ⚠️ Đang ảnh hưởng cả lớp (X học sinh)   │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [❌ Close]                              │
└─────────────────────────────────────────┘
```

**Components:**
- Header: Student name, state, emoji
- Student statement: Their excuse/explanation
- Impact info: How many students affected
- Close button only

---

### Source Student Popup Layout (WholeClass Action)

```
┌─────────────────────────────────────────┐
│  [Student Name] - [State] [Emoji]       │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "[Student excuse/statement]"         │
│                                          │
│  ⚠️ Đang ảnh hưởng cả lớp (X học sinh)   │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [✅ Giải quyết cho cả lớp]              │
│  [❌ Close]                              │
└─────────────────────────────────────────┘
```

**Components:**
- Header: Student name, state, emoji
- Student statement: Their excuse/explanation
- Impact info: How many students affected
- Resolve button for whole class
- Close button

---

### Source Student Popup Layout (Individual Actions)

```
┌─────────────────────────────────────────┐
│  [Student Name] - [State] [Emoji]       │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "[Student excuse/statement]"         │
│                                          │
│  ⚠️ Đang ảnh hưởng:                      │
│                                          │
│  • Student X                             │
│    [✅ Giải quyết cho X]                 │
│                                          │
│  • Student Y                             │
│    [✅ Giải quyết cho Y]                 │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [❌ Close]                              │
└─────────────────────────────────────────┘
```

**Components:**
- Header: Student name, state, emoji
- Student statement: Their excuse/explanation
- Impact list: Each unresolved target
- Resolve button for each target
- Close button

---

## Complex Scenario Examples

### Example 1: Target Student Affected by Multiple Sources

**Scenario:** Student C is affected by:
- Student A (vomited - mess)
- Student B (hit C)
- Student D (making noise)

```
┌─────────────────────────────────────────┐
│  [Student C] - Critical 😰               │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "Cô ơi!"                             │
│                                          │
│  😷 Bạn A ói, thúi quá!                 │
│  😢 Bạn B đánh con, đau lắm!            │
│  🔊 Bạn D làm ồn, con không học được!   │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [🏠 Escort Back] [❌ Close]            │
└─────────────────────────────────────────┘
```

**Notes:**
- Shows all 3 complaints naturally
- Escort Back disabled (3 unresolved sources)
- Each complaint has icon + natural language
- No formal structure, just list of complaints

---

### Example 2: Source Student with Multiple Individual Actions

**Scenario:** Student B is aggressive and has:
- Hit Student C (PhysicalInteraction)
- Hit Student D (PhysicalInteraction)
- Thrown object at Student E (ThrowingObject)

```
┌─────────────────────────────────────────┐
│  [Student B] - Critical 😡               │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "Em tức quá cô ơi, nên em đánh      │
│     bạn C và D..."                       │
│                                          │
│  ⚠️ Đang ảnh hưởng:                      │
│  • Student C                             │
│    [✅ Giải quyết cho C]                 │
│  • Student D                             │
│    [✅ Giải quyết cho D]                 │
│                                          │
│  💬 "Em không cố ý cô ơi, em ném đồ     │
│     vào bạn E..."                        │
│                                          │
│  ⚠️ Đang ảnh hưởng:                      │
│  • Student E                             │
│    [✅ Giải quyết cho E]                 │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [❌ Close]                              │
└─────────────────────────────────────────┘
```

**Notes:**
- Groups targets by action type
- Each action type has specific statement mentioning the action and targets
- PhysicalInteraction: "Em tức quá cô ơi, nên em đánh bạn C và D..." (for C and D)
- ThrowingObject: "Em không cố ý cô ơi, em ném đồ vào bạn E..." (for E)
- Statement template includes {action} and {targets} variables
- Each target has individual resolve button
- As targets are resolved, statement updates (e.g., "đánh bạn C và D" → "đánh bạn D")
- If all targets of one action type resolved, that action section disappears

---

### Example 3: Source Student with Mixed Actions (WholeClass + Individual)

**Scenario:** Student A has:
- Vomited (affects whole class - 5 students)
- Also hit Student B before vomiting

**Popup shows dominant action (vomit - WholeClass):**

```
┌─────────────────────────────────────────┐
│  [Student A] - Acting Out 🤢            │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "Em ói rồi cô ơi..."                │
│                                          │
│  ⚠️ Đang ảnh hưởng cả lớp (5 học sinh)   │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [❌ Close]                              │
└─────────────────────────────────────────┘
```

**Notes:**
- Shows only the WholeClass action (vomit)
- Info only, no action buttons (must clean mess)
- Individual action (hit B) is secondary, not shown
- Prioritizes most impactful action

---

### Example 4: Target Student After Partial Resolution

**Scenario:** Student C was affected by A, B, D
- A's influence resolved (mess cleaned)
- B and D still unresolved

```
┌─────────────────────────────────────────┐
│  [Student C] - Acting Out 😠            │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "Cô ơi!"                             │
│                                          │
│  😢 Bạn B đánh con, đau lắm!            │
│  🔊 Bạn D làm ồn, con không học được!   │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [🏠 Escort Back] [❌ Close]            │
└─────────────────────────────────────────┘
```

**Notes:**
- A's complaint no longer shown (resolved)
- Only 2 complaints remain
- State improved (Critical → Acting Out)
- Escort still disabled (2 unresolved)

---

### Example 5: Source Student After Partial Resolution

**Scenario:** Student B hit C, D, E
- C's influence resolved
- D and E still unresolved

```
┌─────────────────────────────────────────┐
│  [Student B] - Critical 😡               │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "Em tức quá cô ơi..."                │
│                                          │
│  ⚠️ Đang ảnh hưởng:                      │
│                                          │
│  • Student D                             │
│    [✅ Giải quyết cho D]                 │
│                                          │
│  • Student E                             │
│    [✅ Giải quyết cho E]                 │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [❌ Close]                              │
└─────────────────────────────────────────┘
```

**Notes:**
- C no longer in list (resolved)
- Only D and E shown
- No "✅ Resolved" checkmark for C
- List dynamically updates

---

### Example 6: Target Student All Sources Resolved

**Scenario:** Student C had 3 sources, all now resolved

```
┌─────────────────────────────────────────┐
│  [Student C] - Calm 😌                   │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "Em ổn rồi cô!"                      │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [🏠 Escort Back] ✓ [❌ Close]          │
└─────────────────────────────────────────┘
```

**Notes:**
- No complaints shown
- State is Calm
- Escort Back enabled
- Simple, clean popup

---

### Example 7: Source Student All Targets Resolved

**Scenario:** Student B hit C, D, E - all now resolved

```
┌─────────────────────────────────────────┐
│  [Student B] - Acting Out 😠            │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "Em tức quá cô ơi..."                │
│                                          │
│  ⚠️ Đang ảnh hưởng: (0)                  │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [❌ Close]                              │
└─────────────────────────────────────────┘
```

**Notes:**
- Shows 0 affected students
- No target list
- Student B still Acting Out (their own state)
- No actions available

---

## JSON Text Configuration

### File Structure

```
Assets/
└── Configs/
    └── GUI/
        ├── PopupText.json           # Main popup text
        ├── ComplaintTemplates.json  # Target student complaints
        ├── SourceStatements.json    # Source student statements
        └── ButtonLabels.json        # All button text
```

---

### PopupText.json

```json
{
  "targetStudent": {
    "openingPhrase": "Cô ơi!",
    "noComplaints": "Em ổn rồi cô!",
    "escortButtonEnabled": "🏠 Escort Back",
    "escortButtonDisabled": "🏠 Escort Back",
    "closeButton": "❌ Close"
  },
  "sourceStudent": {
    "impactWholeClass": "⚠️ Đang ảnh hưởng cả lớp ({count} học sinh)",
    "impactIndividual": "⚠️ Đang ảnh hưởng:",
    "resolveWholeClassButton": "✅ Giải quyết cho cả lớp",
    "resolveIndividualButton": "✅ Giải quyết cho {studentName}",
    "closeButton": "❌ Close"
  },
  "stateEmojis": {
    "Calm": "😌",
    "Distracted": "😕",
    "ActingOut": "😠",
    "Critical": "😰"
  }
}
```

---

### ComplaintTemplates.json

```json
{
  "complaints": {
    "MessCreated": {
      "template": "Bạn {source} ói, thúi quá!",
      "icon": "😷"
    },
    "PhysicalInteraction": {
      "template": "Bạn {source} đánh con, đau lắm!",
      "icon": "😢"
    },
    "ThrowingObject": {
      "template": "Bạn {source} ném đồ vào con!",
      "icon": "🎯"
    },
    "MakingNoise": {
      "template": "Bạn {source} làm ồn, con không học được!",
      "icon": "🔊"
    },
    "Distraction": {
      "template": "Bạn {source} làm con mất tập trung!",
      "icon": "😵"
    },
    "Poop": {
      "template": "Bạn {source} ỉa, thúi lắm cô!",
      "icon": "💩"
    }
  }
}
```

---

### SourceStatements.json

```json
{
  "statements": {
    "Vomit": [
      "Em ói rồi cô ơi...",
      "Em không kìm được cô...",
      "Em bị ốm cô ơi..."
    ],
    "Poop": [
      "Em không kìm được cô ơi...",
      "Em đau bụng quá cô...",
      "Em xin lỗi cô..."
    ],
    "Hit": [
      "Em tức quá cô ơi, nên em đánh bạn {targets}...",
      "Bạn ấy chọc em trước cô, nên em đánh bạn {targets}!",
      "Em không chịu được, em đánh bạn {targets}..."
    ],
    "ThrowObject": [
      "Em không cố ý cô ơi, em ném đồ vào bạn {targets}...",
      "Em tức quá nên em ném đồ vào bạn {targets}...",
      "Em chỉ muốn ném thôi cô, em ném vào bạn {targets}..."
    ],
    "MakeNoise": [
      "Em đang nói chuyện với bạn {targets} cô ơi...",
      "Em hỏi bài bạn {targets} thôi mà cô...",
      "Em không ồn lắm đâu cô, em chỉ nói với bạn {targets}..."
    ],
    "Push": [
      "Em không cố ý cô ơi, em đẩy bạn {targets}...",
      "Bạn {targets} chặn đường em cô ơi...",
      "Em tức quá nên em đẩy bạn {targets}..."
    ],
    "TakeItem": [
      "Em mượn đồ của bạn {targets} thôi cô...",
      "Em thích đồ của bạn {targets} quá cô ơi...",
      "Bạn {targets} không cho em mượn nên em lấy..."
    ],
    "Tease": [
      "Em chỉ đùa với bạn {targets} thôi cô...",
      "Em không cố ý làm bạn {targets} khóc cô ơi...",
      "Em chọc bạn {targets} chơi thôi mà cô..."
    ],
    "Distract": [
      "Em chỉ kêu bạn {targets} chơi thôi cô...",
      "Em không cố ý làm bạn {targets} mất tập trung cô ơi...",
      "Em chỉ nói chuyện với bạn {targets} một chút..."
    ]
  }
}
```

**Notes:**
- Each action type is separate and specific
- Templates include `{targets}` variable for dynamic text
- `{targets}` will be replaced with "C", "C và D", "C, D và E", etc.
- Easy to add new action types in the future
- Random selection from array for variety

---

### ButtonLabels.json

```json
{
  "actions": {
    "resolveWholeClass": "✅ Giải quyết cho cả lớp",
    "resolveIndividual": "✅ Giải quyết cho {name}",
    "escortBack": "🏠 Escort Back",
    "close": "❌ Close",
    "calm": "💙 Calm",
    "punish": "⚡ Punish"
  },
  "tooltips": {
    "escortDisabled": "Cần giải quyết các nguồn gốc trước",
    "escortEnabled": "Đưa học sinh về chỗ ngồi",
    "resolveWholeClass": "Giải quyết ảnh hưởng cho tất cả học sinh",
    "resolveIndividual": "Giải quyết ảnh hưởng cho {name}"
  }
}
```

---

## Logic Flow

### Determining Popup Type

```csharp
public enum PopupType
{
    TargetStudent,           // Student being affected
    SourceInfoOnly,          // Source with no student actions (vomit, poop)
    SourceWholeClassAction,  // Source with class-wide action (noise)
    SourceIndividualActions  // Source with individual actions (hit, throw)
}

private PopupType DeterminePopupType(StudentAgent student)
{
    // Check if student is affecting others
    var affectedStudents = GetAffectedStudents(student);
    
    if (affectedStudents.Count == 0)
    {
        // Not affecting anyone, show as target
        return PopupType.TargetStudent;
    }
    
    // Student is a source, determine action type
    var eventType = GetSourceEventType(student);
    
    if (!HasStudentResolveAction(eventType))
    {
        // Object-resolvable only (vomit, poop)
        return PopupType.SourceInfoOnly;
    }
    else if (IsWholeClassAction(eventType))
    {
        // Student-resolvable, whole class (noise)
        return PopupType.SourceWholeClassAction;
    }
    else
    {
        // Student-resolvable, individual (hit, throw)
        return PopupType.SourceIndividualActions;
    }
}
```

---

### Event Type Classification

```csharp
private bool HasStudentResolveAction(StudentEventType eventType)
{
    return eventType switch
    {
        // Object-resolvable only
        StudentEventType.MessCreated => false,
        StudentEventType.Poop => false,
        
        // Student-resolvable
        StudentEventType.MakingNoise => true,
        StudentEventType.PhysicalInteraction => true,
        StudentEventType.ThrowingObject => true,
        StudentEventType.Distraction => true,
        
        _ => false
    };
}

private bool IsWholeClassAction(StudentEventType eventType)
{
    return eventType switch
    {
        StudentEventType.MakingNoise => true,
        StudentEventType.Distraction => true,
        _ => false
    };
}
```

---

## Implementation Components

### 1. PopupManager.cs
**Purpose:** Singleton manager for popup lifecycle
**Responsibilities:**
- Create/destroy popups
- Ensure only one popup at a time
- Handle popup positioning above student
- Manage popup canvas

---

### 2. StudentInteractionPopup.cs
**Purpose:** Main popup controller
**Responsibilities:**
- Determine popup type
- Generate content based on type
- Handle button clicks
- Update content dynamically
- Load text from JSON

---

### 3. PopupTextLoader.cs
**Purpose:** Load and manage JSON text files
**Responsibilities:**
- Load all JSON files on startup
- Provide text lookup methods
- Handle missing keys gracefully
- Support text templates with variables

---

### 4. ComplaintItem.cs
**Purpose:** Individual complaint display component
**Responsibilities:**
- Display icon and text
- Handle visual states (resolved/unresolved)
- Animate appearance

---

### 5. TargetActionItem.cs
**Purpose:** Individual target with action button
**Responsibilities:**
- Display target name
- Show resolve button
- Handle button click
- Update when resolved

---

### 6. PopupAnimator.cs
**Purpose:** Handle popup animations
**Responsibilities:**
- Fade in/out
- Scale animations
- Smooth transitions
- Billboard effect (face camera)

---

## Implementation Steps

### Phase 1: Core Structure (2-3 hours)
- [ ] Create popup prefabs in Unity
- [ ] Implement PopupManager singleton
- [ ] Create StudentInteractionPopup base class
- [ ] Implement popup type determination logic
- [ ] Add popup positioning above student
- [ ] Test basic popup show/hide

### Phase 2: JSON System (1-2 hours)
- [ ] Create JSON file structure
- [ ] Implement PopupTextLoader
- [ ] Add text template system (replace {source}, {name}, etc.)
- [ ] Test JSON loading and fallbacks
- [ ] Add editor tool to validate JSON

### Phase 3: Target Student Popup (2 hours)
- [ ] Implement target popup layout
- [ ] Load complaints from JSON
- [ ] Display influence sources as natural complaints
- [ ] Add Escort Back button with enable/disable logic
- [ ] Test with multiple influence sources

### Phase 4: Source Student Popups (3 hours)
- [ ] Implement info-only popup (vomit, poop)
- [ ] Implement whole-class action popup (noise)
- [ ] Implement individual actions popup (hit, throw)
- [ ] Add dynamic student list (hide resolved)
- [ ] Add resolve action buttons
- [ ] Test all source popup types

### Phase 5: Actions & Integration (2-3 hours)
- [ ] Implement ResolveInfluenceForTarget()
- [ ] Implement ResolveInfluenceForWholeClass()
- [ ] Integrate with StudentInfluenceManager
- [ ] Update popup when influences resolved
- [ ] Add visual feedback for actions
- [ ] Test action execution and updates

### Phase 6: Polish & Animation (2 hours)
- [ ] Add fade in/out animations
- [ ] Add scale/bounce effects
- [ ] Implement billboard (face camera)
- [ ] Add button hover effects
- [ ] Add sound effects
- [ ] Polish visual appearance

### Phase 7: Testing & Refinement (2 hours)
- [ ] Test all popup types
- [ ] Test with multiple students
- [ ] Test edge cases (no sources, all resolved)
- [ ] Test JSON text customization
- [ ] Performance testing
- [ ] Bug fixes

**Total Estimated Time:** 14-17 hours

---

## File Structure

```
Assets/
├── Prefabs/
│   └── UI/
│       ├── StudentInteractionPopup.prefab
│       ├── ComplaintItem.prefab
│       └── TargetActionItem.prefab
├── Scripts/
│   └── Core/
│       └── UI/
│           ├── PopupManager.cs
│           ├── StudentInteractionPopup.cs
│           ├── PopupTextLoader.cs
│           ├── ComplaintItem.cs
│           ├── TargetActionItem.cs
│           └── PopupAnimator.cs
└── Configs/
    └── GUI/
        ├── PopupText.json
        ├── ComplaintTemplates.json
        ├── SourceStatements.json
        └── ButtonLabels.json
```

---

## Integration Points

### TeacherController.cs
```csharp
private void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            StudentAgent student = hit.collider.GetComponent<StudentAgent>();
            if (student != null)
            {
                PopupManager.Instance.ShowPopup(student);
            }
        }
    }
}
```

### StudentInfluenceManager.cs
```csharp
public void ResolveInfluenceForTarget(StudentAgent source, StudentAgent target)
{
    target.InfluenceSources.ResolveSource(source);
    
    // Notify popup to update
    PopupManager.Instance.OnInfluenceResolved(source, target);
}

public void ResolveInfluenceForWholeClass(StudentAgent source)
{
    var affectedStudents = GetAffectedStudents(source);
    foreach (var target in affectedStudents)
    {
        target.InfluenceSources.ResolveSource(source);
    }
    
    // Notify popup to update
    PopupManager.Instance.OnWholeClassResolved(source);
}
```

---

## Testing Scenarios

### Test Case 1: Target Student with Multiple Sources
1. Student A vomits (mess created)
2. Student B hits Student C
3. Click on Student C
4. Verify popup shows both complaints
5. Verify Escort Back is disabled
6. Clean mess (resolve A)
7. Verify popup updates (1 complaint)
8. Calm B (resolve B)
9. Verify Escort Back is enabled

### Test Case 2: Source Student - Info Only
1. Student A vomits
2. Click on Student A
3. Verify info-only popup (no action buttons)
4. Verify shows affected count
5. Close popup
6. Clean mess
7. Click on Student A again
8. Verify affected count is 0

### Test Case 3: Source Student - WholeClass Action
1. Student A makes noise
2. Click on Student A
3. Verify whole-class action popup
4. Verify shows affected count
5. Click "Giải quyết cho cả lớp"
6. Verify all influences resolved
7. Verify popup updates

### Test Case 4: Source Student - Individual Actions
1. Student B hits C and D
2. Click on Student B
3. Verify individual actions popup
4. Verify shows C and D
5. Click "Giải quyết cho C"
6. Verify C removed from list
7. Verify D still shown
8. Click "Giải quyết cho D"
9. Verify popup shows no targets

### Test Case 5: JSON Text Customization
1. Modify ComplaintTemplates.json
2. Change vomit complaint text
3. Reload game
4. Verify new text appears in popup
5. Test with missing keys (fallback)

---

## Edge Cases

### No Influence Sources
**Target Student:** Show "Em ổn rồi cô!" message, no complaints

### All Sources Resolved
**Source Student:** Show "Đang ảnh hưởng cả lớp (0 học sinh)"

### Student Both Source and Target
**Priority:** Show as source if affecting others, otherwise show as target

### Multiple Popups
**Solution:** Close existing popup before showing new one

### Popup During Sequence
**Solution:** Don't allow popup during student sequences

### Student Destroyed While Popup Open
**Solution:** Close popup if student reference becomes null

---

## Performance Considerations

### Optimization Strategies
1. **Object Pooling:** Reuse popup instances instead of destroy/create
2. **Lazy Loading:** Only load JSON once on startup
3. **Caching:** Cache text lookups to avoid repeated JSON parsing
4. **Update Throttling:** Don't update popup every frame
5. **Culling:** Hide popup if student off-screen

### Memory Management
- Unload unused popup prefabs
- Clear cached text when changing levels
- Properly destroy popup GameObjects

---

## Accessibility Considerations

### Font Size
- Minimum 16pt for readability
- Scalable based on screen resolution

### Color Contrast
- High contrast between text and background
- Color-blind friendly icons

### Localization Support
- JSON structure supports multiple languages
- Easy to add new language files

---

## Future Enhancements

### Phase 2 Features
- Student portrait images in popup
- Animated student expressions
- Voice lines for complaints
- More detailed influence information
- History log of past influences

### Phase 3 Features
- Multi-student comparison view
- Batch actions (resolve multiple at once)
- Drag-and-drop to resolve
- Keyboard shortcuts
- Tutorial tooltips

---

## Success Criteria

### Must Have
- ✅ Popup appears when clicking student
- ✅ Correct popup type based on situation
- ✅ All text loaded from JSON
- ✅ Actions work correctly
- ✅ Popup updates dynamically
- ✅ No performance issues

### Nice to Have
- ✅ Smooth animations
- ✅ Sound effects
- ✅ Tooltips
- ✅ Visual polish

### Won't Have (This Phase)
- ❌ Voice acting
- ❌ Student portraits
- ❌ Advanced animations
- ❌ Multi-language support

---

## Notes

- Keep popup simple and focused
- Prioritize clarity over fancy effects
- Test with real gameplay scenarios
- Iterate based on player feedback
- Maintain consistent visual style

**Status:** Design complete, ready for implementation

**Priority:** High - Core gameplay feature

**Dependencies:** StudentInfluenceManager, TeacherController

**Estimated Total Effort:** 14-17 hours

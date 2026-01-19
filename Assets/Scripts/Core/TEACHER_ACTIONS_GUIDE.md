# Teacher Actions Guide

## Separate Keys for Calm vs Escort

### Problem
Previously, both "Calm" and "Escort" actions used the same key (E), making it unclear which action would be performed.

### Solution
Two separate keys with clear purposes:

## Key Bindings

### **E Key - CALM Student**
**Purpose:** De-escalate student and resolve their influence on others

**When to use:**
- Student is Acting Out or Critical
- Student is causing influence on other students
- You want to resolve the student's influence sources

**What it does:**
1. De-escalates student state (Critical → Acting Out → Distracted → Calm)
2. Triggers `StudentCalmed` event
3. **Resolves this student's influence on ALL other students**
4. Student stays in current location (does NOT move)
5. Reduces disruption by 5 points

**Example:**
```
Student_B hits Student_C → C has influence source from B
Teacher presses E on B → B calmed, B's influence on C resolved
C can now be escorted back (if A also calmed)
```

### **R Key - RECALL/ESCORT Student**
**Purpose:** Bring student back to their seat

**When to use:**
- Student is outside classroom
- Student is following escape route
- All influence sources on this student are resolved

**What it does:**
1. Checks if student is outside/on route
2. Checks if all influence sources are resolved
3. If YES: Student returns to seat via return route
4. If NO: Student returns to outdoor, +10 disruption per unresolved source

**Example:**
```
Student_C is outside with 2 sources (A, B)
Teacher presses E on A → A's influence resolved
Teacher presses E on B → B's influence resolved
Teacher presses R on C → C returns to seat (SUCCESS)
```

## Complete Workflow

### Scenario: Student_C influenced by A (vomit) and B (hit)

**Step 1: Calm A**
- Look at Student_A
- Press **E** (Calm)
- A's influence on C resolved
- A stays in current location

**Step 2: Calm B**
- Look at Student_B
- Press **E** (Calm)
- B's influence on C resolved
- B stays outside

**Step 3: Escort C**
- Look at Student_C (outside)
- Press **R** (Recall/Escort)
- Check: C has 0 unresolved sources ✓
- C returns to seat

**Step 4: Escort B**
- Look at Student_B (outside)
- Press **R** (Recall/Escort)
- Check: B has 0 unresolved sources ✓
- B returns to seat

## UI Prompts (Future Enhancement)

When looking at a student, the game should show:
- **[E] Calm Student_A** (if student is acting out)
- **[R] Escort Student_B** (if student is outside)

Both prompts can appear simultaneously if both actions are available.

## Key Differences

| Action | Key | Purpose | Moves Student? | Resolves Influence? |
|--------|-----|---------|----------------|---------------------|
| **Calm** | E | De-escalate & resolve influence | ❌ No | ✅ Yes (this student's influence on others) |
| **Escort** | R | Bring back to seat | ✅ Yes | ❌ No (checks if resolved) |

## Important Notes

1. **Calm does NOT move students** - they stay where they are
2. **Escort requires all sources resolved** - otherwise student returns to outdoor
3. **You must Calm THEN Escort** - two separate actions
4. **Calm resolves influence on OTHERS** - not the student's own sources
5. **Both actions can be used on the same student** - Calm first, then Escort

## Debug Logs

**Calm action:**
```
[Teacher] === CALMING Student_B ===
[Teacher] De-escalating Student_B from Critical...
[Teacher] ✓ Student_B de-escalated to Calm (took 3 steps)
[Teacher] ✓ Logged StudentCalmed event for Student_B
[Influence] === TEACHER ACTION: Calming Student_B ===
[Influence] ✓ Resolved Student_B's influence on Student_C
[Influence] === Teacher calmed Student_B - resolved influence on 1 student(s) ===
```

**Escort action:**
```
[Teacher] Attempting to escort Student_C back to seat
[Teacher] ✓ All influence sources resolved - proceeding with escort
[Teacher] ✓ Student_C escorted back to seat
```

## Summary

- **E = Calm** (resolve influence, stay in place)
- **R = Recall/Escort** (move to seat, requires resolved sources)
- Use both keys strategically to manage classroom disruption
- Always calm influence sources before escorting affected students

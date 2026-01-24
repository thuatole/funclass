using UnityEngine;
using System.Collections.Generic;
using FunClass.Editor.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif
using FunClass.Core.UI;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Handles student placement and binding to desks (Phase 4)
    /// </summary>
    public static class StudentPlacementManager
    {
        /// <summary>
        /// Instantiate desks and students, bind them together
        /// </summary>
        public static List<EnhancedLevelImporter.StudentDeskPair> PlaceStudentsAndDesks(
            EnhancedLevelSchema schema, 
            List<EnhancedLevelImporter.DeskData> desks, 
            AssetMapConfig assetMap)
        {
            Debug.Log($"[StudentPlacementManager] Placing {desks.Count} desks and {schema.students} students");
            
            // Create classroom group
            GameObject classroomGroup = SceneSetupManager.GetOrCreateClassroomGroup();
            
            // Create desks group
            GameObject desksGroup = SceneSetupManager.CreateOrFindGameObject("Desks", classroomGroup.transform);
            
            // Create students group - use standard "=== STUDENTS ===" group at root (compatible with old system)
            GameObject studentsGroup = SceneSetupManager.GetOrCreateStudentsGroup();
            int initialChildCount = studentsGroup.transform.childCount;
            Debug.Log($"[StudentPlacementManager] Using students group: '{studentsGroup.name}', parent: '{studentsGroup.transform.parent?.name}', active: {studentsGroup.activeSelf}, initial child count: {initialChildCount}");
            
            // Clear existing students (like old system does)
            for (int i = studentsGroup.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(studentsGroup.transform.GetChild(i).gameObject);
            }
            Debug.Log($"[StudentPlacementManager] Cleared {initialChildCount} existing students from group");
            
            List<EnhancedLevelImporter.StudentDeskPair> pairs = new List<EnhancedLevelImporter.StudentDeskPair>();
            
            // 1. Instantiate desks
            for (int i = 0; i < desks.Count; i++)
            {
                var desk = desks[i];
                
                // Get desk prefab (try DESK first, then CHAIR as fallback)
                GameObject deskPrefab = assetMap.GetPrefab("DESK") ?? assetMap.GetPrefab("CHAIR");
                
                if (deskPrefab == null)
                {
                    Debug.LogError("[StudentPlacementManager] No desk or chair prefab found in asset map!");
                    continue;
                }
                
                // Instantiate desk
                GameObject deskObj = PrefabUtility.InstantiatePrefab(deskPrefab) as GameObject;
                deskObj.name = desk.deskId;
                deskObj.transform.position = desk.position;
                deskObj.transform.rotation = desk.rotation;
                deskObj.transform.SetParent(desksGroup.transform);
                
                // Parent student and mess slots to desk
                if (desk.studentSlot != null)
                {
                    desk.studentSlot.transform.SetParent(deskObj.transform);
                    desk.studentSlot.transform.localPosition = desk.studentSlot.transform.position - desk.position;
                }
                
                if (desk.messSlot != null)
                {
                    desk.messSlot.transform.SetParent(deskObj.transform);
                    desk.messSlot.transform.localPosition = desk.messSlot.transform.position - desk.position;
                }
                
                desk.deskObject = deskObj;
                
                Debug.Log($"[StudentPlacementManager] Instantiated desk {desk.deskId}");
            }
            
            // 2. Instantiate and bind students
            int studentIndex = 0;
            for (int i = 0; i < Mathf.Min(desks.Count, schema.students); i++)
            {
                var desk = desks[i];
                
                Debug.Log($"[StudentPlacementManager] Processing desk {i}: {desk.deskId} at position {desk.position}");
                
                // Check if we have manual student config for this desk
                EnhancedStudentData studentConfig = GetStudentConfigForDesk(schema, desk.deskId, studentIndex);
                
                try
                {
                    // Get student prefab
                    GameObject studentPrefab = assetMap.GetPrefab("STUDENT");
                    Debug.Log($"[StudentPlacementManager] Desk {desk.deskId}: Student prefab {(studentPrefab != null ? $"found: {studentPrefab.name}" : "NOT FOUND")}");
                    
                    GameObject studentObj;
                    
                    // Get student position
                    Vector3 studentPosition = desk.studentSlot != null ? 
                        desk.studentSlot.transform.position : desk.position;
                    Debug.Log($"[StudentPlacementManager] Desk {desk.deskId}: Student position {studentPosition}, studentSlot: {(desk.studentSlot != null ? "EXISTS" : "NULL")}, desk position: {desk.position}");
                    
                    // Adjust height if too low (prevent underground students)
                    if (studentPosition.y < 0.5f)
                    {
                        float newY = 1f; // Place capsule bottom at y=0 (capsule height is 2)
                        Debug.Log($"[StudentPlacementManager] Adjusting student height from {studentPosition.y} to {newY} (capsule height: 2)");
                        studentPosition.y = newY;
                    }
                    
                    if (studentPrefab == null)
                    {
                        Debug.LogWarning("[StudentPlacementManager] No student prefab found in asset map! Creating basic student GameObject.");
                        studentObj = new GameObject();
                        studentObj.transform.position = studentPosition;
                        studentObj.transform.rotation = Quaternion.identity;
                        Debug.Log($"[StudentPlacementManager] Created basic GameObject for student {studentIndex}");
                    }
                    else
                    {
                        studentObj = PrefabUtility.InstantiatePrefab(studentPrefab) as GameObject;
                        studentObj.transform.position = studentPosition;
                        studentObj.transform.rotation = Quaternion.identity;
                        Debug.Log($"[StudentPlacementManager] Instantiated prefab {studentPrefab.name} for student {studentIndex}");
                    }
                    
                    // Determine student identifier (without "Student_" prefix)
                    string studentIdentifier = studentConfig?.studentName ?? studentIndex.ToString();
                    Debug.Log($"[StudentPlacementManager] Desk {desk.deskId}: studentConfig={studentConfig?.studentName ?? "NULL"} (id={studentConfig?.studentId ?? "NULL"}), studentIndex={studentIndex}, studentIdentifier='{studentIdentifier}'");
                    // Remove "Student_" prefix if present in identifier
                    studentIdentifier = studentIdentifier.Replace("Student_", "");
                    Debug.Log($"[StudentPlacementManager] Desk {desk.deskId}: after replace studentIdentifier='{studentIdentifier}'");
                    
                    // GameObject name always has "Student_" prefix for consistency
                    string gameObjectName = studentIdentifier.StartsWith("Student_") ? studentIdentifier : "Student_" + studentIdentifier;
                    studentObj.name = gameObjectName;
                    Debug.Log($"[StudentPlacementManager] Setting student {studentObj.name} (identifier: {studentIdentifier}) parent to {studentsGroup.name}");
                    studentObj.transform.SetParent(studentsGroup.transform);
                    Debug.Log($"[StudentPlacementManager] Student {studentObj.name} parent set to: {(studentObj.transform.parent != null ? studentObj.transform.parent.name : "NULL")}");
                    
                    // Create student-desk pair
                    EnhancedLevelImporter.StudentDeskPair pair = new EnhancedLevelImporter.StudentDeskPair
                    {
                        studentId = studentConfig?.studentId ?? $"student_{studentIdentifier}",
                        studentName = studentIdentifier, // Store without prefix for config matching
                        desk = desk,
                        studentObject = studentObj
                    };
                    Debug.Log($"[StudentPlacementManager] Created pair: studentId='{pair.studentId}', studentName='{pair.studentName}', deskId='{pair.desk?.deskId}', studentObj='{pair.studentObject?.name}'");
                    
                    pairs.Add(pair);
                    
                    // Configure student component if it exists
                    ConfigureStudentComponent(studentObj, studentConfig, schema.difficulty);
                    
                    Debug.Log($"[StudentPlacementManager] Placed student {pair.studentName} at desk {desk.deskId}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[StudentPlacementManager] ERROR creating student for desk {desk.deskId}: {e.Message}\n{e.StackTrace}");
                }
                studentIndex++;
            }
            
            // Check if we have more students than desks (should not happen due to validation)
            if (schema.students > desks.Count)
            {
                Debug.LogWarning($"[StudentPlacementManager] More students ({schema.students}) than desks ({desks.Count}). Some students not placed.");
            }
            
            Debug.Log($"[StudentPlacementManager] Placed {pairs.Count} student-desk pairs");
            Debug.Log($"[StudentPlacementManager] Students group hierarchy:");
            Debug.Log($"[StudentPlacementManager] - Name: {studentsGroup.name}");
            Debug.Log($"[StudentPlacementManager] - Active: {studentsGroup.activeSelf}");
            Debug.Log($"[StudentPlacementManager] - Parent: {studentsGroup.transform.parent?.name}");
            Debug.Log($"[StudentPlacementManager] - Child count: {studentsGroup.transform.childCount}");
            
            // Log all children
            for (int i = 0; i < studentsGroup.transform.childCount; i++)
            {
                var child = studentsGroup.transform.GetChild(i);
                Debug.Log($"[StudentPlacementManager]   Child {i}: {child.name}, active: {child.gameObject.activeSelf}, position: {child.position}");
            }
            
            return pairs;
        }
        
        /// <summary>
        /// Get student configuration for a specific desk
        /// </summary>
        private static EnhancedStudentData GetStudentConfigForDesk(
            EnhancedLevelSchema schema, 
            string deskId, 
            int studentIndex)
        {
            // If schema has manual student configs, try to find one for this desk
            if (schema.studentConfigs != null && schema.studentConfigs.Count > 0)
            {
                foreach (var config in schema.studentConfigs)
                {
                    if (config.deskId == deskId)
                    {
                        return config;
                    }
                }
                
                // If we have configs but not enough for all desks, use sequential assignment
                if (studentIndex < schema.studentConfigs.Count)
                {
                    return schema.studentConfigs[studentIndex];
                }
            }
            
            // Auto-generate student config
            return GenerateStudentConfig(schema, studentIndex, deskId);
        }
        
        /// <summary>
        /// Generate student configuration based on difficulty
        /// </summary>
        private static EnhancedStudentData GenerateStudentConfig(
            EnhancedLevelSchema schema, 
            int index, 
            string deskId)
        {
            // Generate student name based on pattern
            string studentName = $"Student_{index}";
            
            // Generate personality based on difficulty
            PersonalityData personality = GeneratePersonality(schema.difficulty);
            
            // Generate behaviors based on difficulty
            BehaviorData behaviors = GenerateBehaviors(schema.difficulty);
            
            return new EnhancedStudentData
            {
                studentId = $"student_{index}",
                studentName = studentName,
                deskId = deskId,
                personality = personality,
                behaviors = behaviors
            };
        }
        
        /// <summary>
        /// Generate personality based on difficulty
        /// </summary>
        private static PersonalityData GeneratePersonality(string difficulty)
        {
            PersonalityData personality = new PersonalityData();
            
            switch (difficulty.ToLower())
            {
                case "easy":
                    // Easy: students are more patient, less impulsive
                    personality.patience = Random.Range(0.6f, 0.9f);
                    personality.attentionSpan = Random.Range(0.5f, 0.8f);
                    personality.impulsiveness = Random.Range(0.1f, 0.4f);
                    personality.influenceSusceptibility = Random.Range(0.3f, 0.6f);
                    personality.influenceResistance = Random.Range(0.4f, 0.7f);
                    personality.panicThreshold = Random.Range(0.6f, 0.9f);
                    break;
                    
                case "hard":
                    // Hard: students are less patient, more impulsive
                    personality.patience = Random.Range(0.2f, 0.5f);
                    personality.attentionSpan = Random.Range(0.2f, 0.5f);
                    personality.impulsiveness = Random.Range(0.5f, 0.9f);
                    personality.influenceSusceptibility = Random.Range(0.6f, 0.9f);
                    personality.influenceResistance = Random.Range(0.1f, 0.4f);
                    personality.panicThreshold = Random.Range(0.3f, 0.6f);
                    break;
                    
                case "medium":
                default:
                    // Medium: balanced
                    personality.patience = Random.Range(0.4f, 0.7f);
                    personality.attentionSpan = Random.Range(0.3f, 0.7f);
                    personality.impulsiveness = Random.Range(0.3f, 0.7f);
                    personality.influenceSusceptibility = Random.Range(0.4f, 0.7f);
                    personality.influenceResistance = Random.Range(0.3f, 0.6f);
                    personality.panicThreshold = Random.Range(0.4f, 0.8f);
                    break;
            }
            
            return personality;
        }
        
        /// <summary>
        /// Generate behaviors based on difficulty
        /// </summary>
        private static BehaviorData GenerateBehaviors(string difficulty)
        {
            BehaviorData behaviors = new BehaviorData();
            
            switch (difficulty.ToLower())
            {
                case "easy":
                    // Easy: fewer disruptive behaviors
                    behaviors.canStandUp = Random.value < 0.3f;
                    behaviors.canMoveAround = Random.value < 0.2f;
                    behaviors.canDropItems = Random.value < 0.1f;
                    behaviors.canKnockOverObjects = Random.value < 0.1f;
                    behaviors.canThrowObjects = Random.value < 0.1f;
                    break;
                    
                case "hard":
                    // Hard: more disruptive behaviors
                    behaviors.canStandUp = Random.value < 0.8f;
                    behaviors.canMoveAround = Random.value < 0.7f;
                    behaviors.canDropItems = Random.value < 0.6f;
                    behaviors.canKnockOverObjects = Random.value < 0.5f;
                    behaviors.canThrowObjects = Random.value < 0.5f;
                    break;
                    
                case "medium":
                default:
                    // Medium: moderate behaviors
                    behaviors.canStandUp = Random.value < 0.5f;
                    behaviors.canMoveAround = Random.value < 0.4f;
                    behaviors.canDropItems = Random.value < 0.3f;
                    behaviors.canKnockOverObjects = Random.value < 0.3f;
                    behaviors.canThrowObjects = Random.value < 0.2f;
                    break;
            }
            
            // Always allow basic behaviors
            behaviors.canFidget = true;
            behaviors.canLookAround = true;
            behaviors.canMakeNoiseWithObjects = true;
            
            // Set idle time ranges
            behaviors.minIdleTime = 2f;
            behaviors.maxIdleTime = 8f;
            
            return behaviors;
        }
        
        /// <summary>
        /// Configure StudentAgent component if it exists on the student prefab
        /// </summary>
        private static void ConfigureStudentComponent(
            GameObject studentObj, 
            EnhancedStudentData studentConfig,
            string difficulty)
        {
            // Ensure student has all required components (compatible with old system)
            EnsureStudentComponents(studentObj);
            
            // Try to get StudentAgent component
            var studentAgent = studentObj.GetComponent<FunClass.Core.StudentAgent>();
            if (studentAgent == null)
            {
                // Try to find it in children
                studentAgent = studentObj.GetComponentInChildren<FunClass.Core.StudentAgent>();
            }
            
            if (studentAgent == null)
            {
                // Add StudentAgent if still missing
                studentAgent = studentObj.AddComponent<FunClass.Core.StudentAgent>();
                Debug.Log($"[StudentPlacementManager] Added missing StudentAgent to {studentObj.name}");
            }
            
            // Note: StudentConfig is a ScriptableObject, not a MonoBehaviour component
            // Custom student configuration from JSON is not fully implemented yet
            // StudentConfig assets should be created separately and assigned to StudentAgent
            
            if (studentConfig != null)
            {
                Debug.Log($"[StudentPlacementManager] Student '{studentConfig.studentName}' has custom configuration, but full implementation requires StudentConfig ScriptableObject assets");
            }
            
            Debug.Log($"[StudentPlacementManager] Configured student component for {studentObj.name}");
        }
        
        /// <summary>
        /// Ensure student GameObject has all required components (mimics old system)
        /// </summary>
        private static void EnsureStudentComponents(GameObject studentObj)
        {
            Debug.Log($"[StudentPlacementManager] EnsureStudentComponents for {studentObj.name}, transform.childCount: {studentObj.transform.childCount}");
            
            // Ensure student GameObject is active
            if (!studentObj.activeSelf)
            {
                studentObj.SetActive(true);
                Debug.Log($"[StudentPlacementManager] Activated student GameObject {studentObj.name}");
            }
            
            // Add NavMeshAgent if missing
            var navAgent = studentObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = studentObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
                navAgent.radius = 0.3f;
                navAgent.height = 1.8f;
                navAgent.speed = 2f;
                navAgent.angularSpeed = 180f;
                navAgent.acceleration = 8f;
                navAgent.enabled = false;
                Debug.Log($"[StudentPlacementManager] Added NavMeshAgent to {studentObj.name}");
            }
            
            // Add Rigidbody if missing
            var rigidbody = studentObj.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = studentObj.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                Debug.Log($"[StudentPlacementManager] Added Rigidbody to {studentObj.name}");
            }
            
            // Add CapsuleCollider if missing
            var collider = studentObj.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = studentObj.AddComponent<CapsuleCollider>();
                collider.radius = 0.3f;
                collider.height = 1.8f;
                collider.center = new Vector3(0, 0.9f, 0);
                Debug.Log($"[StudentPlacementManager] Added CapsuleCollider to {studentObj.name}");
            }
            
            // Add StudentMessCreator if missing
            var messCreator = studentObj.GetComponent<FunClass.Core.StudentMessCreator>();
            if (messCreator == null)
            {
                messCreator = studentObj.AddComponent<FunClass.Core.StudentMessCreator>();
                Debug.Log($"[StudentPlacementManager] Added StudentMessCreator to {studentObj.name}");
            }
            
            // Add StudentVisualMarker if missing
            var visualMarker = studentObj.GetComponent<FunClass.Core.StudentVisualMarker>();
            if (visualMarker == null)
            {
                visualMarker = studentObj.AddComponent<FunClass.Core.StudentVisualMarker>();
                Debug.Log($"[StudentPlacementManager] Added StudentVisualMarker to {studentObj.name}");
            }
            
            // Add InfluenceStatusIcon if missing
            var influenceIcon = studentObj.GetComponent<FunClass.Core.UI.InfluenceStatusIcon>();
            if (influenceIcon == null)
            {
                influenceIcon = studentObj.AddComponent<FunClass.Core.UI.InfluenceStatusIcon>();
                Debug.Log($"[StudentPlacementManager] Added InfluenceStatusIcon to {studentObj.name}");
            }
            
            // Add visual capsule if no visual child exists
            bool hasVisual = false;
            int visualChildIndex = -1;
            GameObject visual = null;
            foreach (Transform child in studentObj.transform)
            {
                if (child.name == "Visual" || child.gameObject.GetComponent<Renderer>() != null)
                {
                    hasVisual = true;
                    visualChildIndex = child.GetSiblingIndex();
                    visual = child.gameObject;
                    break;
                }
            }
            
            if (!hasVisual)
            {
                Debug.Log($"[StudentPlacementManager] No visual found for {studentObj.name}, creating capsule...");
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(studentObj.transform);
                visual.transform.localPosition = Vector3.zero;
                Debug.Log($"[StudentPlacementManager] Created visual capsule for {studentObj.name}, localPosition: {visual.transform.localPosition}");
                
                // Remove duplicate collider from primitive
                var primitiveCollider = visual.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    Object.DestroyImmediate(primitiveCollider);
                    Debug.Log($"[StudentPlacementManager] Removed duplicate collider from visual capsule");
                }
            }
            else
            {
                Debug.Log($"[StudentPlacementManager] {studentObj.name} already has visual child at index {visualChildIndex}: {visual.name}");
            }
            
            // Ensure visual is visible
            EnsureVisualVisibility(visual, studentObj.name);
            
            // Ensure root scale is reasonable
            if (studentObj.transform.localScale == Vector3.zero || studentObj.transform.localScale.magnitude < 0.01f)
            {
                studentObj.transform.localScale = Vector3.one;
                Debug.Log($"[StudentPlacementManager] Fixed zero root scale for {studentObj.name}, set to (1,1,1)");
            }
            else if (studentObj.transform.localScale.magnitude > 10f)
            {
                studentObj.transform.localScale = Vector3.one;
                Debug.Log($"[StudentPlacementManager] Fixed oversized root scale for {studentObj.name}, set to (1,1,1)");
            }
            
            // Final check: count all components
            int componentCount = studentObj.GetComponents<Component>().Length;
            Debug.Log($"[StudentPlacementManager] {studentObj.name} now has {componentCount} components, {studentObj.transform.childCount} children");
            
            // Log position and scale
            Debug.Log($"[StudentPlacementManager] {studentObj.name} position: {studentObj.transform.position}, localPosition: {studentObj.transform.localPosition}, scale: {studentObj.transform.localScale}");
        }
        
        /// <summary>
        /// Ensure visual GameObject has renderer enabled, proper scale, and material
        /// </summary>
        private static void EnsureVisualVisibility(GameObject visual, string studentName)
        {
            if (visual == null)
            {
                Debug.LogError($"[StudentPlacementManager] Visual is null for {studentName}");
                return;
            }
            
            // Ensure visual is active
            if (!visual.activeSelf)
            {
                visual.SetActive(true);
                Debug.Log($"[StudentPlacementManager] Activated visual for {studentName}");
            }
            
            // Check and enable renderer
            var renderer = visual.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"[StudentPlacementManager] Visual for {studentName} has no Renderer component");
                // Try to add a MeshRenderer if missing
                renderer = visual.AddComponent<MeshRenderer>();
                // Also need a MeshFilter with a mesh
                var meshFilter = visual.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = visual.AddComponent<MeshFilter>();
                    meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                }
                Debug.Log($"[StudentPlacementManager] Added Renderer and MeshFilter to visual for {studentName}");
            }
            
            if (renderer != null)
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                    Debug.Log($"[StudentPlacementManager] Enabled renderer for {studentName}");
                }
                
                // Ensure material exists
                if (renderer.sharedMaterial == null)
                {
                    // Try to get default material from asset map
                    var assetMap = AssetDatabase.LoadAssetAtPath<AssetMapConfig>("Assets/Configs/DefaultAssetMap.asset");
                    Material defaultMat = assetMap?.GetMaterial("Default");
                    if (defaultMat != null)
                    {
                        renderer.sharedMaterial = defaultMat;
                        Debug.Log($"[StudentPlacementManager] Set default material for {studentName}");
                    }
                    else
                    {
                        // Create a simple colored material
                        Material newMat = new Material(Shader.Find("Standard"));
                        newMat.color = Color.blue;
                        renderer.sharedMaterial = newMat;
                        Debug.Log($"[StudentPlacementManager] Created fallback material for {studentName}");
                    }
                }
            }
            
            // Check scale
            if (visual.transform.localScale == Vector3.zero || visual.transform.localScale.magnitude < 0.01f)
            {
                visual.transform.localScale = Vector3.one;
                Debug.Log($"[StudentPlacementManager] Fixed zero scale for {studentName}, set to (1,1,1)");
            }
            else if (visual.transform.localScale.magnitude > 10f)
            {
                visual.transform.localScale = Vector3.one;
                Debug.Log($"[StudentPlacementManager] Fixed oversized scale for {studentName}, set to (1,1,1)");
            }
            
            // Log visual details
            Debug.Log($"[StudentPlacementManager] Visual '{visual.name}' for {studentName}: active={visual.activeSelf}, renderer={(renderer != null ? $"enabled={renderer.enabled}, material={renderer.sharedMaterial?.name}" : "NULL")}, scale={visual.transform.localScale}, position={visual.transform.position}, localPosition={visual.transform.localPosition}");
        }
    }
}
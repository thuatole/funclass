using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module validate level sau khi generate
    /// </summary>
    public static class LevelValidator
    {
        public class ValidationResult
        {
            public bool isValid = true;
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
            public List<string> info = new List<string>();

            public void AddError(string message)
            {
                errors.Add(message);
                isValid = false;
            }

            public void AddWarning(string message)
            {
                warnings.Add(message);
            }

            public void AddInfo(string message)
            {
                info.Add(message);
            }

            public string GetReport()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== LEVEL VALIDATION REPORT ===\n");

                if (isValid)
                {
                    sb.AppendLine("✅ VALIDATION PASSED\n");
                }
                else
                {
                    sb.AppendLine("❌ VALIDATION FAILED\n");
                }

                if (errors.Count > 0)
                {
                    sb.AppendLine($"ERRORS ({errors.Count}):");
                    foreach (var error in errors)
                    {
                        sb.AppendLine($"  ❌ {error}");
                    }
                    sb.AppendLine();
                }

                if (warnings.Count > 0)
                {
                    sb.AppendLine($"WARNINGS ({warnings.Count}):");
                    foreach (var warning in warnings)
                    {
                        sb.AppendLine($"  ⚠️ {warning}");
                    }
                    sb.AppendLine();
                }

                if (info.Count > 0)
                {
                    sb.AppendLine($"INFO ({info.Count}):");
                    foreach (var infoMsg in info)
                    {
                        sb.AppendLine($"  ℹ️ {infoMsg}");
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Validate toàn bộ level
        /// </summary>
        public static ValidationResult ValidateLevel(FunClass.Core.LevelConfig levelConfig)
        {
            var result = new ValidationResult();

            if (levelConfig == null)
            {
                result.AddError("LevelConfig is null");
                return result;
            }

            result.AddInfo($"Validating level: {levelConfig.name}");

            // Validate các components
            ValidateGoals(levelConfig, result);
            ValidateRoutes(levelConfig, result);
            ValidateSequences(levelConfig, result);
            ValidateInteractables(result);
            ValidateMessSystem(result);
            ValidateStudents(result);
            ValidateReferences(levelConfig, result);
            ValidateSceneHierarchy(result);

            return result;
        }

        /// <summary>
        /// Validate goals hợp lý
        /// </summary>
        private static void ValidateGoals(FunClass.Core.LevelConfig levelConfig, ValidationResult result)
        {
            result.AddInfo("Checking goals...");

            if (levelConfig.levelGoal == null)
            {
                result.AddError("LevelGoal is null");
                return;
            }

            var goal = levelConfig.levelGoal;

            // Check disruption thresholds
            if (goal.maxDisruptionThreshold <= 0 || goal.maxDisruptionThreshold > 100)
            {
                result.AddError($"Invalid maxDisruptionThreshold: {goal.maxDisruptionThreshold} (should be 0-100)");
            }

            if (goal.catastrophicDisruptionLevel <= goal.maxDisruptionThreshold)
            {
                result.AddWarning($"catastrophicDisruptionLevel ({goal.catastrophicDisruptionLevel}) should be higher than maxDisruptionThreshold ({goal.maxDisruptionThreshold})");
            }

            // Check critical students
            if (goal.maxAllowedCriticalStudents < 0)
            {
                result.AddError($"Invalid maxAllowedCriticalStudents: {goal.maxAllowedCriticalStudents}");
            }

            if (goal.catastrophicCriticalStudents <= goal.maxAllowedCriticalStudents)
            {
                result.AddWarning($"catastrophicCriticalStudents ({goal.catastrophicCriticalStudents}) should be higher than maxAllowedCriticalStudents ({goal.maxAllowedCriticalStudents})");
            }

            // Check outside students
            if (goal.maxAllowedOutsideStudents < 0)
            {
                result.AddError($"Invalid maxAllowedOutsideStudents: {goal.maxAllowedOutsideStudents}");
            }

            if (goal.catastrophicOutsideStudents <= goal.maxAllowedOutsideStudents)
            {
                result.AddWarning($"catastrophicOutsideStudents ({goal.catastrophicOutsideStudents}) should be higher than maxAllowedOutsideStudents ({goal.maxAllowedOutsideStudents})");
            }

            // Check time limit
            if (goal.timeLimitSeconds < 0)
            {
                result.AddError($"Invalid timeLimitSeconds: {goal.timeLimitSeconds}");
            }
            else if (goal.timeLimitSeconds == 0)
            {
                result.AddInfo("No time limit set (tutorial mode)");
            }

            // Check star thresholds
            if (goal.oneStarScore <= 0)
            {
                result.AddError($"Invalid oneStarScore: {goal.oneStarScore}");
            }

            if (goal.twoStarScore <= goal.oneStarScore)
            {
                result.AddError($"twoStarScore ({goal.twoStarScore}) must be higher than oneStarScore ({goal.oneStarScore})");
            }

            if (goal.threeStarScore <= goal.twoStarScore)
            {
                result.AddError($"threeStarScore ({goal.threeStarScore}) must be higher than twoStarScore ({goal.twoStarScore})");
            }

            result.AddInfo($"✓ Goals validated: {goal.maxDisruptionThreshold}% disruption, {goal.timeLimitSeconds}s time limit");
        }

        /// <summary>
        /// Validate routes hợp lệ
        /// </summary>
        private static void ValidateRoutes(FunClass.Core.LevelConfig levelConfig, ValidationResult result)
        {
            result.AddInfo("Checking routes...");

            // Check escape route
            if (levelConfig.escapeRoute == null)
            {
                result.AddError("Escape route is null");
            }
            else
            {
                ValidateRoute(levelConfig.escapeRoute, "Escape", result);
            }

            // Check return route
            if (levelConfig.returnRoute == null)
            {
                result.AddError("Return route is null");
            }
            else
            {
                ValidateRoute(levelConfig.returnRoute, "Return", result);
            }

            // Check available routes
            if (levelConfig.availableRoutes == null || levelConfig.availableRoutes.Count == 0)
            {
                result.AddWarning("No available routes assigned to LevelConfig");
            }
            else
            {
                result.AddInfo($"✓ {levelConfig.availableRoutes.Count} routes available");
            }

            // Check door reference
            if (levelConfig.classroomDoor == null)
            {
                result.AddWarning("Classroom door reference is null");
            }
            else
            {
                result.AddInfo($"✓ Door reference: {levelConfig.classroomDoor.name}");
            }
        }

        /// <summary>
        /// Validate sequences được gắn vào LevelConfig
        /// </summary>
        private static void ValidateSequences(FunClass.Core.LevelConfig levelConfig, ValidationResult result)
        {
            result.AddInfo("Checking sequences...");

            if (levelConfig.availableSequences == null || levelConfig.availableSequences.Count == 0)
            {
                result.AddWarning("No sequences assigned to LevelConfig");
                return;
            }

            int validSequences = 0;
            int totalSteps = 0;

            foreach (var sequence in levelConfig.availableSequences)
            {
                if (sequence == null)
                {
                    result.AddWarning("Null sequence found in availableSequences");
                    continue;
                }

                validSequences++;

                // Check sequence ID
                if (string.IsNullOrEmpty(sequence.sequenceId))
                {
                    result.AddWarning($"Sequence has empty sequenceId");
                }

                // Check steps
                if (sequence.steps == null || sequence.steps.Count == 0)
                {
                    result.AddError($"Sequence '{sequence.sequenceId}' has no steps");
                    continue;
                }

                totalSteps += sequence.steps.Count;

                // Validate each step
                for (int i = 0; i < sequence.steps.Count; i++)
                {
                    var step = sequence.steps[i];
                    
                    if (step == null)
                    {
                        result.AddError($"Sequence '{sequence.sequenceId}' step {i} is null");
                        continue;
                    }

                    // Check step has valid reaction (None is valid for some cases)
                    // No validation needed for reactions

                    // Check step description
                    if (string.IsNullOrEmpty(step.stepDescription))
                    {
                        result.AddWarning($"Sequence '{sequence.sequenceId}' step {i} has no description");
                    }
                }

                result.AddInfo($"  ✓ Sequence '{sequence.sequenceId}': {sequence.steps.Count} steps");
            }

            if (validSequences == 0)
            {
                result.AddError("All sequences are null");
            }
            else
            {
                result.AddInfo($"✓ {validSequences} sequences validated ({totalSteps} total steps)");
            }
        }

        /// <summary>
        /// Validate interactable objects trong scene
        /// </summary>
        private static void ValidateInteractables(ValidationResult result)
        {
            result.AddInfo("Checking interactable objects...");

            GameObject classroom = GameObject.Find("=== CLASSROOM ===");
            if (classroom == null)
            {
                result.AddWarning("Classroom group not found, skipping interactables check");
                return;
            }

            Transform interactablesGroup = classroom.transform.Find("InteractableObjects");
            if (interactablesGroup == null)
            {
                result.AddWarning("InteractableObjects group not found in classroom");
                return;
            }

            int interactableCount = interactablesGroup.childCount;
            if (interactableCount == 0)
            {
                result.AddWarning("No interactable objects in scene");
                return;
            }

            int validInteractables = 0;
            int missingComponent = 0;
            int missingCollider = 0;
            int missingRenderer = 0;

            for (int i = 0; i < interactableCount; i++)
            {
                GameObject obj = interactablesGroup.GetChild(i).gameObject;

                // Check StudentInteractableObject component
                var interactable = obj.GetComponent<FunClass.Core.StudentInteractableObject>();
                if (interactable == null)
                {
                    missingComponent++;
                    result.AddWarning($"Interactable '{obj.name}' missing StudentInteractableObject component");
                    continue;
                }

                validInteractables++;

                // Check collider
                var collider = obj.GetComponent<Collider>();
                if (collider == null)
                {
                    missingCollider++;
                    result.AddWarning($"Interactable '{obj.name}' missing Collider");
                }

                // Check renderer (for visibility)
                var renderer = obj.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    missingRenderer++;
                    result.AddWarning($"Interactable '{obj.name}' missing Renderer");
                }

                // Check position (not at origin)
                if (obj.transform.position == Vector3.zero && i > 0)
                {
                    result.AddWarning($"Interactable '{obj.name}' at origin position");
                }
            }

            if (validInteractables == 0)
            {
                result.AddError("No valid interactable objects found");
            }
            else
            {
                result.AddInfo($"✓ {validInteractables}/{interactableCount} interactables validated");
                
                if (missingComponent > 0)
                {
                    result.AddWarning($"{missingComponent} objects missing StudentInteractableObject component");
                }
                if (missingCollider > 0)
                {
                    result.AddWarning($"{missingCollider} objects missing Collider");
                }
                if (missingRenderer > 0)
                {
                    result.AddWarning($"{missingRenderer} objects missing Renderer");
                }
            }
        }

        /// <summary>
        /// Validate mess system (prefabs và components)
        /// </summary>
        private static void ValidateMessSystem(ValidationResult result)
        {
            result.AddInfo("Checking mess system...");

            // Check mess prefabs exist
            string[] messTypes = { "Vomit", "Spill", "Trash", "Stain", "BrokenGlass", "TornPaper" };
            int foundPrefabs = 0;
            int missingPrefabs = 0;

            foreach (var messType in messTypes)
            {
                string prefabPath = $"Assets/Prefabs/Mess/{messType}Mess.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab != null)
                {
                    foundPrefabs++;
                    
                    // Validate prefab structure
                    var messComponent = prefab.GetComponent<FunClass.Core.MessObject>();
                    var vomitComponent = prefab.GetComponent<FunClass.Core.VomitMess>();
                    
                    if (messComponent == null && vomitComponent == null)
                    {
                        result.AddWarning($"{messType}Mess prefab missing MessObject/VomitMess component");
                    }
                    
                    var collider = prefab.GetComponent<Collider>();
                    if (collider == null)
                    {
                        result.AddWarning($"{messType}Mess prefab missing Collider");
                    }
                }
                else
                {
                    missingPrefabs++;
                }
            }

            if (foundPrefabs == 0)
            {
                result.AddWarning("No mess prefabs found (may not be generated yet)");
            }
            else
            {
                result.AddInfo($"✓ {foundPrefabs}/{messTypes.Length} mess prefabs found");
                
                if (missingPrefabs > 0)
                {
                    result.AddWarning($"{missingPrefabs} mess prefab types missing");
                }
            }

            // Check VomitMess instances in scene (if any)
            var vomitMesses = Object.FindObjectsOfType<FunClass.Core.VomitMess>();
            if (vomitMesses.Length > 0)
            {
                result.AddInfo($"Found {vomitMesses.Length} VomitMess instances in scene");
            }

            // Check MessObject instances in scene (if any)
            var messObjects = Object.FindObjectsOfType<FunClass.Core.MessObject>();
            if (messObjects.Length > 0)
            {
                result.AddInfo($"Found {messObjects.Length} MessObject instances in scene");
            }

            // Check ClassroomManager has mess handling
            var classroomManager = Object.FindObjectOfType<FunClass.Core.ClassroomManager>();
            if (classroomManager != null)
            {
                result.AddInfo("✓ ClassroomManager available for mess handling");
            }
            else
            {
                result.AddWarning("ClassroomManager not found (needed for mess handling)");
            }
        }

        private static void ValidateRoute(FunClass.Core.StudentRoute route, string routeName, ValidationResult result)
        {
            if (route.movementSpeed <= 0)
            {
                result.AddError($"{routeName} route has invalid movement speed: {route.movementSpeed}");
            }

            if (route.rotationSpeed <= 0)
            {
                result.AddWarning($"{routeName} route has low rotation speed: {route.rotationSpeed}");
            }

            // Check waypoints in scene
            GameObject waypointsGroup = GameObject.Find("Waypoints");
            if (waypointsGroup != null)
            {
                Transform routeGroup = waypointsGroup.transform.Find($"{routeName}Route");
                if (routeGroup == null)
                {
                    result.AddError($"{routeName}Route group not found in scene");
                }
                else
                {
                    int waypointCount = routeGroup.childCount;
                    if (waypointCount < 2)
                    {
                        result.AddError($"{routeName}Route has only {waypointCount} waypoints (minimum 2 required)");
                    }
                    else
                    {
                        result.AddInfo($"✓ {routeName} route: {waypointCount} waypoints, speed {route.movementSpeed}");
                    }
                }
            }
        }

        /// <summary>
        /// Validate students có seat
        /// </summary>
        private static void ValidateStudents(ValidationResult result)
        {
            result.AddInfo("Checking students...");

            GameObject studentsGroup = GameObject.Find("=== STUDENTS ===");
            if (studentsGroup == null)
            {
                result.AddError("Students group not found in scene");
                return;
            }

            int studentCount = studentsGroup.transform.childCount;
            if (studentCount == 0)
            {
                result.AddWarning("No students in scene");
                return;
            }

            int studentsWithoutAgent = 0;
            int studentsWithoutConfig = 0;
            int studentsWithoutPosition = 0;

            for (int i = 0; i < studentCount; i++)
            {
                GameObject student = studentsGroup.transform.GetChild(i).gameObject;
                
                // Check StudentAgent component
                var agent = student.GetComponent<FunClass.Core.StudentAgent>();
                if (agent == null)
                {
                    studentsWithoutAgent++;
                    continue;
                }

                // Check config
                var configField = typeof(FunClass.Core.StudentAgent).GetField("config", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (configField != null)
                {
                    var config = configField.GetValue(agent) as FunClass.Core.StudentConfig;
                    if (config == null)
                    {
                        studentsWithoutConfig++;
                    }
                }

                // Check position (seat)
                if (student.transform.position == Vector3.zero && i > 0)
                {
                    studentsWithoutPosition++;
                }
            }

            if (studentsWithoutAgent > 0)
            {
                result.AddError($"{studentsWithoutAgent} students missing StudentAgent component");
            }

            if (studentsWithoutConfig > 0)
            {
                result.AddWarning($"{studentsWithoutConfig} students missing config");
            }

            if (studentsWithoutPosition > 0)
            {
                result.AddWarning($"{studentsWithoutPosition} students at origin position");
            }

            result.AddInfo($"✓ {studentCount} students validated");
        }

        /// <summary>
        /// Validate references không null
        /// </summary>
        private static void ValidateReferences(FunClass.Core.LevelConfig levelConfig, ValidationResult result)
        {
            result.AddInfo("Checking references...");

            // Check ClassroomManager
            var classroomManager = Object.FindObjectOfType<FunClass.Core.ClassroomManager>();
            if (classroomManager == null)
            {
                result.AddError("ClassroomManager not found in scene");
            }
            else
            {
                // Check if LevelConfig is assigned
                var so = new SerializedObject(classroomManager);
                var levelConfigProp = so.FindProperty("levelConfig");
                if (levelConfigProp.objectReferenceValue == null)
                {
                    result.AddWarning("LevelConfig not assigned to ClassroomManager");
                }
                else
                {
                    result.AddInfo("✓ ClassroomManager has LevelConfig");
                }
            }

            // Check TeacherController
            var teacher = Object.FindObjectOfType<FunClass.Core.TeacherController>();
            if (teacher == null)
            {
                result.AddError("TeacherController not found in scene");
            }
            else
            {
                result.AddInfo("✓ TeacherController found");
            }

            // Check other managers
            ValidateManager<FunClass.Core.GameStateManager>("GameStateManager", result);
            ValidateManager<FunClass.Core.LevelManager>("LevelManager", result);
            ValidateManager<FunClass.Core.StudentEventManager>("StudentEventManager", result);
            ValidateManager<FunClass.Core.TeacherScoreManager>("TeacherScoreManager", result);
        }

        private static void ValidateManager<T>(string managerName, ValidationResult result) where T : Object
        {
            var manager = Object.FindObjectOfType<T>();
            if (manager == null)
            {
                result.AddWarning($"{managerName} not found in scene");
            }
            else
            {
                result.AddInfo($"✓ {managerName} found");
            }
        }

        /// <summary>
        /// Validate scene hierarchy
        /// </summary>
        private static void ValidateSceneHierarchy(ValidationResult result)
        {
            result.AddInfo("Checking scene hierarchy...");

            string[] requiredGroups = {
                "=== MANAGERS ===",
                "=== CLASSROOM ===",
                "=== STUDENTS ===",
                "=== TEACHER ===",
                "=== UI ==="
            };

            foreach (var groupName in requiredGroups)
            {
                GameObject group = GameObject.Find(groupName);
                if (group == null)
                {
                    result.AddError($"Required group not found: {groupName}");
                }
                else
                {
                    result.AddInfo($"✓ {groupName} exists");
                }
            }
        }

        /// <summary>
        /// Quick validate và show dialog
        /// </summary>
        [MenuItem("Tools/FunClass/Validate Current Level")]
        public static void ValidateCurrentLevel()
        {
            var classroomManager = Object.FindObjectOfType<FunClass.Core.ClassroomManager>();
            if (classroomManager == null)
            {
                EditorUtility.DisplayDialog("Validation Failed", 
                    "ClassroomManager not found in scene", 
                    "OK");
                return;
            }

            var so = new SerializedObject(classroomManager);
            var levelConfigProp = so.FindProperty("levelConfig");
            var levelConfig = levelConfigProp.objectReferenceValue as FunClass.Core.LevelConfig;

            if (levelConfig == null)
            {
                EditorUtility.DisplayDialog("Validation Failed", 
                    "LevelConfig not assigned to ClassroomManager", 
                    "OK");
                return;
            }

            var result = ValidateLevel(levelConfig);
            
            Debug.Log(result.GetReport());

            if (result.isValid)
            {
                EditorUtility.DisplayDialog("Validation Passed ✅", 
                    $"Level is valid!\n\n" +
                    $"Errors: {result.errors.Count}\n" +
                    $"Warnings: {result.warnings.Count}\n" +
                    $"Info: {result.info.Count}\n\n" +
                    "Check Console for detailed report.", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Failed ❌", 
                    $"Level has {result.errors.Count} error(s)\n\n" +
                    "Check Console for detailed report.", 
                    "OK");
            }
        }
    }
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tự động fix các issues trong level
    /// </summary>
    public static class LevelAutoFixer
    {
        public class FixResult
        {
            public int issuesFixed = 0;
            public List<string> fixedIssues = new List<string>();
            public List<string> failedFixes = new List<string>();

            public void AddFixed(string message)
            {
                fixedIssues.Add(message);
                issuesFixed++;
            }

            public void AddFailed(string message)
            {
                failedFixes.Add(message);
            }

            public string GetReport()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== AUTO FIX REPORT ===\n");

                sb.AppendLine($"Issues Fixed: {issuesFixed}\n");

                if (fixedIssues.Count > 0)
                {
                    sb.AppendLine("FIXED:");
                    foreach (var fix in fixedIssues)
                    {
                        sb.AppendLine($"  ✅ {fix}");
                    }
                    sb.AppendLine();
                }

                if (failedFixes.Count > 0)
                {
                    sb.AppendLine("FAILED:");
                    foreach (var fail in failedFixes)
                    {
                        sb.AppendLine($"  ❌ {fail}");
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Auto fix tất cả issues có thể
        /// </summary>
        [MenuItem("Tools/FunClass/Auto Fix Level Issues")]
        public static void AutoFixLevelIssues()
        {
            EditorUtility.DisplayProgressBar("Auto Fix", "Analyzing level...", 0f);

            var result = new FixResult();

            try
            {
                // 1. Fix missing managers
                EditorUtility.DisplayProgressBar("Auto Fix", "Fixing managers...", 0.2f);
                FixMissingManagers(result);

                // 2. Fix missing routes
                EditorUtility.DisplayProgressBar("Auto Fix", "Fixing routes...", 0.4f);
                FixMissingRoutes(result);

                // 3. Fix null configs
                EditorUtility.DisplayProgressBar("Auto Fix", "Fixing configs...", 0.6f);
                FixNullConfigs(result);

                // 4. Fix missing components
                EditorUtility.DisplayProgressBar("Auto Fix", "Fixing components...", 0.8f);
                FixMissingComponents(result);

                // 5. Fix scene hierarchy
                EditorUtility.DisplayProgressBar("Auto Fix", "Fixing hierarchy...", 0.9f);
                FixSceneHierarchy(result);

                // Save changes
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();

                // Show report
                Debug.Log(result.GetReport());

                string message = $"Auto Fix Complete!\n\n" +
                                $"Issues Fixed: {result.issuesFixed}\n" +
                                $"Failed: {result.failedFixes.Count}\n\n" +
                                "Check Console for detailed report.";

                EditorUtility.DisplayDialog("Auto Fix Complete", message, "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Auto Fix Error", $"Error during auto fix:\n{e.Message}", "OK");
                Debug.LogError($"[LevelAutoFixer] Error: {e}");
            }
        }

        /// <summary>
        /// Fix missing managers
        /// </summary>
        private static void FixMissingManagers(FixResult result)
        {
            GameObject managersGroup = GameObject.Find("=== MANAGERS ===");
            if (managersGroup == null)
            {
                managersGroup = new GameObject("=== MANAGERS ===");
                Undo.RegisterCreatedObjectUndo(managersGroup, "Create Managers Group");
                result.AddFixed("Created MANAGERS group");
            }

            // Check and create missing managers
            FixManager<FunClass.Core.GameStateManager>(managersGroup, "GameStateManager", result);
            FixManager<FunClass.Core.LevelManager>(managersGroup, "LevelManager", result);
            FixManager<FunClass.Core.ClassroomManager>(managersGroup, "ClassroomManager", result);
            FixManager<FunClass.Core.StudentEventManager>(managersGroup, "StudentEventManager", result);
            FixManager<FunClass.Core.TeacherScoreManager>(managersGroup, "TeacherScoreManager", result);
            FixManager<FunClass.Core.StudentInfluenceManager>(managersGroup, "StudentInfluenceManager", result);
            FixManager<FunClass.Core.StudentMovementManager>(managersGroup, "StudentMovementManager", result);
        }

        private static void FixManager<T>(GameObject parent, string managerName, FixResult result) where T : Component
        {
            var existing = Object.FindObjectOfType<T>();
            if (existing == null)
            {
                GameObject managerObj = new GameObject(managerName);
                managerObj.transform.SetParent(parent.transform);
                managerObj.AddComponent<T>();
                Undo.RegisterCreatedObjectUndo(managerObj, $"Create {managerName}");
                result.AddFixed($"Created {managerName}");
            }
        }

        /// <summary>
        /// Fix missing routes
        /// </summary>
        private static void FixMissingRoutes(FixResult result)
        {
            var classroomManager = Object.FindObjectOfType<FunClass.Core.ClassroomManager>();
            if (classroomManager == null)
            {
                result.AddFailed("Cannot fix routes: ClassroomManager not found");
                return;
            }

            var so = new SerializedObject(classroomManager);
            var levelConfigProp = so.FindProperty("levelConfig");
            var levelConfig = levelConfigProp.objectReferenceValue as FunClass.Core.LevelConfig;

            if (levelConfig == null)
            {
                result.AddFailed("Cannot fix routes: LevelConfig not assigned");
                return;
            }

            bool routesFixed = false;

            // Fix escape route
            if (levelConfig.escapeRoute == null)
            {
                // Try to find existing route
                string routePath = $"Assets/Configs/{levelConfig.name}/Routes/EscapeRoute.asset";
                var route = AssetDatabase.LoadAssetAtPath<FunClass.Core.StudentRoute>(routePath);
                
                if (route == null)
                {
                    // Create new route
                    var routes = WaypointRouteBuilder.CreateDefaultRoutes(levelConfig.name);
                    if (routes.Count > 0)
                    {
                        levelConfig.escapeRoute = routes[0];
                        routesFixed = true;
                        result.AddFixed("Created and assigned Escape route");
                    }
                }
                else
                {
                    levelConfig.escapeRoute = route;
                    routesFixed = true;
                    result.AddFixed("Assigned existing Escape route");
                }
            }

            // Fix return route
            if (levelConfig.returnRoute == null)
            {
                string routePath = $"Assets/Configs/{levelConfig.name}/Routes/ReturnRoute.asset";
                var route = AssetDatabase.LoadAssetAtPath<FunClass.Core.StudentRoute>(routePath);
                
                if (route == null && levelConfig.escapeRoute != null)
                {
                    // Create return route if escape exists
                    var routes = WaypointRouteBuilder.CreateDefaultRoutes(levelConfig.name);
                    if (routes.Count > 1)
                    {
                        levelConfig.returnRoute = routes[1];
                        routesFixed = true;
                        result.AddFixed("Created and assigned Return route");
                    }
                }
                else if (route != null)
                {
                    levelConfig.returnRoute = route;
                    routesFixed = true;
                    result.AddFixed("Assigned existing Return route");
                }
            }

            if (routesFixed)
            {
                EditorUtility.SetDirty(levelConfig);
            }
        }

        /// <summary>
        /// Fix null configs
        /// </summary>
        private static void FixNullConfigs(FixResult result)
        {
            var classroomManager = Object.FindObjectOfType<FunClass.Core.ClassroomManager>();
            if (classroomManager == null)
            {
                result.AddFailed("Cannot fix configs: ClassroomManager not found");
                return;
            }

            var so = new SerializedObject(classroomManager);
            var levelConfigProp = so.FindProperty("levelConfig");

            // Fix null LevelConfig
            if (levelConfigProp.objectReferenceValue == null)
            {
                // Try to find any LevelConfig in project
                string[] guids = AssetDatabase.FindAssets("t:LevelConfig");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var foundLevelConfig = AssetDatabase.LoadAssetAtPath<FunClass.Core.LevelConfig>(path);
                    
                    if (foundLevelConfig != null)
                    {
                        levelConfigProp.objectReferenceValue = foundLevelConfig;
                        so.ApplyModifiedProperties();
                        result.AddFixed($"Assigned LevelConfig: {foundLevelConfig.name}");
                    }
                }
                else
                {
                    result.AddFailed("No LevelConfig found in project");
                }
            }

            // Fix null LevelGoalConfig
            var levelConfig = levelConfigProp.objectReferenceValue as FunClass.Core.LevelConfig;
            if (levelConfig != null && levelConfig.levelGoal == null)
            {
                // Try to find goal config
                string goalPath = $"Assets/Configs/{levelConfig.name}/{levelConfig.name}_Goal.asset";
                var goalConfig = AssetDatabase.LoadAssetAtPath<FunClass.Core.LevelGoalConfig>(goalPath);
                
                if (goalConfig == null)
                {
                    // Create new goal config
                    goalConfig = LevelGoalGenerator.GenerateLevelGoal(levelConfig.name, LevelConfigGenerator.Difficulty.Normal);
                    result.AddFixed("Created new LevelGoalConfig");
                }
                
                levelConfig.levelGoal = goalConfig;
                EditorUtility.SetDirty(levelConfig);
                result.AddFixed("Assigned LevelGoalConfig");
            }

            // Fix door reference
            if (levelConfig != null && levelConfig.classroomDoor == null)
            {
                GameObject door = GameObject.Find("Door");
                if (door != null)
                {
                    levelConfig.classroomDoor = door.transform;
                    EditorUtility.SetDirty(levelConfig);
                    result.AddFixed("Assigned door reference");
                }
            }
        }

        /// <summary>
        /// Fix missing components
        /// </summary>
        private static void FixMissingComponents(FixResult result)
        {
            // Fix students missing StudentAgent
            GameObject studentsGroup = GameObject.Find("=== STUDENTS ===");
            if (studentsGroup != null)
            {
                int fixedCount = 0;
                for (int i = 0; i < studentsGroup.transform.childCount; i++)
                {
                    GameObject student = studentsGroup.transform.GetChild(i).gameObject;
                    var agent = student.GetComponent<FunClass.Core.StudentAgent>();
                    
                    if (agent == null)
                    {
                        student.AddComponent<FunClass.Core.StudentAgent>();
                        fixedCount++;
                    }
                }
                
                if (fixedCount > 0)
                {
                    result.AddFixed($"Added StudentAgent to {fixedCount} students");
                }
            }

            // Fix interactables missing components
            GameObject classroom = GameObject.Find("=== CLASSROOM ===");
            if (classroom != null)
            {
                Transform interactablesGroup = classroom.transform.Find("InteractableObjects");
                if (interactablesGroup != null)
                {
                    int fixedInteractables = 0;
                    int fixedColliders = 0;

                    for (int i = 0; i < interactablesGroup.childCount; i++)
                    {
                        GameObject obj = interactablesGroup.GetChild(i).gameObject;
                        
                        // Add StudentInteractableObject if missing
                        var interactable = obj.GetComponent<FunClass.Core.StudentInteractableObject>();
                        if (interactable == null)
                        {
                            obj.AddComponent<FunClass.Core.StudentInteractableObject>();
                            fixedInteractables++;
                        }

                        // Add Collider if missing
                        var collider = obj.GetComponent<Collider>();
                        if (collider == null)
                        {
                            obj.AddComponent<SphereCollider>();
                            fixedColliders++;
                        }
                    }

                    if (fixedInteractables > 0)
                    {
                        result.AddFixed($"Added StudentInteractableObject to {fixedInteractables} objects");
                    }
                    if (fixedColliders > 0)
                    {
                        result.AddFixed($"Added Collider to {fixedColliders} objects");
                    }
                }
            }

            // Fix teacher missing TeacherController
            GameObject teacherGroup = GameObject.Find("=== TEACHER ===");
            if (teacherGroup != null)
            {
                var teacher = Object.FindObjectOfType<FunClass.Core.TeacherController>();
                if (teacher == null)
                {
                    Transform teacherObj = teacherGroup.transform.Find("Teacher");
                    if (teacherObj != null)
                    {
                        teacherObj.gameObject.AddComponent<FunClass.Core.TeacherController>();
                        result.AddFixed("Added TeacherController component");
                    }
                }
            }
        }

        /// <summary>
        /// Fix scene hierarchy
        /// </summary>
        private static void FixSceneHierarchy(FixResult result)
        {
            string[] requiredGroups = {
                "=== MANAGERS ===",
                "=== CLASSROOM ===",
                "=== STUDENTS ===",
                "=== TEACHER ===",
                "=== UI ==="
            };

            int createdCount = 0;
            foreach (var groupName in requiredGroups)
            {
                GameObject group = GameObject.Find(groupName);
                if (group == null)
                {
                    group = new GameObject(groupName);
                    Undo.RegisterCreatedObjectUndo(group, $"Create {groupName}");
                    createdCount++;
                }
            }

            if (createdCount > 0)
            {
                result.AddFixed($"Created {createdCount} missing hierarchy groups");
            }

            // Fix classroom subgroups
            GameObject classroom = GameObject.Find("=== CLASSROOM ===");
            if (classroom != null)
            {
                string[] subgroups = { "Environment", "Furniture", "Waypoints", "InteractableObjects" };
                int subgroupsCreated = 0;

                foreach (var subgroupName in subgroups)
                {
                    Transform subgroup = classroom.transform.Find(subgroupName);
                    if (subgroup == null)
                    {
                        GameObject newSubgroup = new GameObject(subgroupName);
                        newSubgroup.transform.SetParent(classroom.transform);
                        Undo.RegisterCreatedObjectUndo(newSubgroup, $"Create {subgroupName}");
                        subgroupsCreated++;
                    }
                }

                if (subgroupsCreated > 0)
                {
                    result.AddFixed($"Created {subgroupsCreated} classroom subgroups");
                }
            }
        }

        /// <summary>
        /// Quick fix specific issue type
        /// </summary>
        public static void QuickFixRoutes()
        {
            var result = new FixResult();
            FixMissingRoutes(result);
            
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            
            Debug.Log(result.GetReport());
            EditorUtility.DisplayDialog("Quick Fix Routes", 
                $"Fixed {result.issuesFixed} route issues", 
                "OK");
        }

        public static void QuickFixManagers()
        {
            var result = new FixResult();
            FixMissingManagers(result);
            
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            Debug.Log(result.GetReport());
            EditorUtility.DisplayDialog("Quick Fix Managers", 
                $"Fixed {result.issuesFixed} manager issues", 
                "OK");
        }

        public static void QuickFixConfigs()
        {
            var result = new FixResult();
            FixNullConfigs(result);
            
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            
            Debug.Log(result.GetReport());
            EditorUtility.DisplayDialog("Quick Fix Configs", 
                $"Fixed {result.issuesFixed} config issues", 
                "OK");
        }
    }
}

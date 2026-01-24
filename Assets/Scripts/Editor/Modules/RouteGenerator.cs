using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FunClass.Editor.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Generates EscapeRoute and ReturnRoute for each student (Phase 5)
    /// </summary>
    public static class RouteGenerator
    {
        /// <summary>
        /// Generate escape and return routes for all student-desk pairs
        /// </summary>
        public static void GenerateRoutes(EnhancedLevelSchema schema, List<EnhancedLevelImporter.StudentDeskPair> studentDeskPairs)
        {
            if (!schema.routeGeneration.autoGenerateRoutes)
            {
                Debug.Log("[RouteGenerator] Auto route generation disabled");
                return;
            }
            
            Debug.Log($"[RouteGenerator] Generating routes for {studentDeskPairs.Count} students");
            
            // Calculate key positions
            Vector3 doorPosition = DeskGridGenerator.CalculateDoorPosition(schema);
            Vector3 outsidePosition = DeskGridGenerator.CalculateOutsidePosition(schema);
            
            // Create routes group
            GameObject routesGroup = SceneSetupManager.CreateOrFindGameObject("Routes");
            GameObject waypointsGroup = SceneSetupManager.CreateOrFindGameObject("Waypoints", routesGroup.transform);
            
            // Create door waypoint (shared)
            GameObject doorWaypoint = CreateWaypoint("Door", doorPosition, waypointsGroup.transform);
            
            // Create outside waypoint (shared)
            GameObject outsideWaypoint = CreateWaypoint("Outside", outsidePosition, waypointsGroup.transform);
            
            // Generate routes for each student
            foreach (var pair in studentDeskPairs)
            {
                GenerateStudentRoutes(pair, doorWaypoint, outsideWaypoint, waypointsGroup.transform, schema);
            }
            
            Debug.Log($"[RouteGenerator] Generated routes for {studentDeskPairs.Count} students");
        }
        
        /// <summary>
        /// Generate escape and return routes for a single student
        /// </summary>
        private static void GenerateStudentRoutes(
            EnhancedLevelImporter.StudentDeskPair pair,
            GameObject doorWaypoint,
            GameObject outsideWaypoint,
            Transform waypointsParent,
            EnhancedLevelSchema schema)
        {
            string studentId = pair.studentId;
            Vector3 deskPosition = pair.desk.position;
            
            // Create student-specific route group
            GameObject studentRouteGroup = SceneSetupManager.CreateOrFindGameObject(
                $"Routes_{studentId}", waypointsParent);
            
            // 1. Escape Route: Desk → Aisle → Door → Outside
            GameObject escapeRouteGroup = CreateRouteGroup($"EscapeRoute_{studentId}", studentRouteGroup.transform);
            
            // Waypoint 1: Desk position (starting point)
            GameObject escapeWp1 = CreateWaypoint(
                $"{studentId}_Escape_01_Desk", 
                deskPosition, 
                escapeRouteGroup.transform);
            
            // Waypoint 2: Aisle position (midpoint between desk rows)
            Vector3 aislePosition = CalculateAislePosition(deskPosition, schema);
            GameObject escapeWp2 = CreateWaypoint(
                $"{studentId}_Escape_02_Aisle", 
                aislePosition, 
                escapeRouteGroup.transform);
            
            // Waypoint 3: Door (student-specific duplicate)
            GameObject escapeDoorWp = DuplicateWaypoint(doorWaypoint, $"{studentId}_Escape_03_Door", escapeRouteGroup.transform);
            
            // Waypoint 4: Outside (student-specific duplicate)
            GameObject escapeOutsideWp = DuplicateWaypoint(outsideWaypoint, $"{studentId}_Escape_04_Outside", escapeRouteGroup.transform);
            
            // Create escape route ScriptableObject
            CreateRouteScriptableObject(
                $"EscapeRoute_{studentId}",
                new List<GameObject> { escapeWp1, escapeWp2, escapeDoorWp, escapeOutsideWp },
                schema.routeGeneration.escapeRouteSpeed,
                false, // isLooping
                false, // isPingPong
                schema.routeGeneration.isRunning);
            
            // 2. Return Route: Outside → Door → Aisle → Desk (reverse of escape)
            GameObject returnRouteGroup = CreateRouteGroup($"ReturnRoute_{studentId}", studentRouteGroup.transform);
            
            // Waypoint 1: Outside (student-specific duplicate)
            GameObject returnOutsideWp = DuplicateWaypoint(outsideWaypoint, $"{studentId}_Return_01_Outside", returnRouteGroup.transform);
            
            // Waypoint 2: Door (student-specific duplicate)
            GameObject returnDoorWp = DuplicateWaypoint(doorWaypoint, $"{studentId}_Return_02_Door", returnRouteGroup.transform);
            
            // Waypoint 3: Aisle position
            GameObject returnWp3 = CreateWaypoint(
                $"{studentId}_Return_03_Aisle", 
                aislePosition, 
                returnRouteGroup.transform);
            
            // Waypoint 4: Desk position
            GameObject returnWp4 = CreateWaypoint(
                $"{studentId}_Return_04_Desk", 
                deskPosition, 
                returnRouteGroup.transform);
            
            // Create return route ScriptableObject
            CreateRouteScriptableObject(
                $"ReturnRoute_{studentId}",
                new List<GameObject> { returnOutsideWp, returnDoorWp, returnWp3, returnWp4 },
                schema.routeGeneration.returnRouteSpeed,
                false, // isLooping
                false, // isPingPong
                schema.routeGeneration.isRunning);
            
            Debug.Log($"[RouteGenerator] Generated routes for student {studentId}");
        }
        
        /// <summary>
        /// Calculate aisle position (midpoint between desk and door)
        /// </summary>
        private static Vector3 CalculateAislePosition(Vector3 deskPosition, EnhancedLevelSchema schema)
        {
            Vector3 doorPosition = DeskGridGenerator.CalculateDoorPosition(schema);
            
            // Aisle is at the same X as desk, but Z is halfway to door
            float aisleZ = (deskPosition.z + doorPosition.z) / 2f;
            
            // Adjust to be in the center aisle (X = 0 for center aisle)
            // Or keep desk's X if we want aisle along desk row
            float aisleX = 0f; // Center aisle
            
            return new Vector3(aisleX, 0, aisleZ);
        }
        
        /// <summary>
        /// Create a route group GameObject
        /// </summary>
        private static GameObject CreateRouteGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent);
            return group;
        }
        
        /// <summary>
        /// Create a waypoint GameObject with StudentWaypoint component
        /// </summary>
        private static GameObject CreateWaypoint(string name, Vector3 position, Transform parent)
        {
            GameObject waypointObj = new GameObject(name);
            waypointObj.transform.position = position;
            waypointObj.transform.SetParent(parent);
            
            // Add StudentWaypoint component
            var waypoint = waypointObj.AddComponent<FunClass.Core.StudentWaypoint>();
            waypoint.waypointName = name;
            waypoint.waitDuration = 0f;
            
            // Add visual marker (small sphere) for editor visibility
            #if UNITY_EDITOR
            var sphere = waypointObj.AddComponent<UnityEngine.SphereCollider>();
            sphere.radius = 0.1f;
            sphere.isTrigger = true;
            #endif
            
            Debug.Log($"[RouteGenerator] Created waypoint {name} at {position}");
            return waypointObj;
        }
        
        /// <summary>
        /// Duplicate an existing waypoint under a new parent
        /// </summary>
        private static GameObject DuplicateWaypoint(GameObject sourceWaypoint, string newName, Transform newParent)
        {
            GameObject duplicate = new GameObject(newName);
            duplicate.transform.position = sourceWaypoint.transform.position;
            duplicate.transform.SetParent(newParent);
            
            // Copy StudentWaypoint component
            var sourceComponent = sourceWaypoint.GetComponent<FunClass.Core.StudentWaypoint>();
            var destComponent = duplicate.AddComponent<FunClass.Core.StudentWaypoint>();
            destComponent.waypointName = newName;
            destComponent.waitDuration = sourceComponent != null ? sourceComponent.waitDuration : 0f;
            
            // Add visual marker
            #if UNITY_EDITOR
            var sphere = duplicate.AddComponent<UnityEngine.SphereCollider>();
            sphere.radius = 0.1f;
            sphere.isTrigger = true;
            #endif
            
            Debug.Log($"[RouteGenerator] Duplicated waypoint {sourceWaypoint.name} as {newName}");
            return duplicate;
        }
        
        /// <summary>
        /// Create Route ScriptableObject and save it to Assets
        /// </summary>
        private static void CreateRouteScriptableObject(
            string routeName,
            List<GameObject> waypoints,
            float movementSpeed,
            bool isLooping,
            bool isPingPong,
            bool isRunning)
        {
            // Create folder for routes if it doesn't exist
            string routesFolder = "Assets/Configs/AutoGenerated/Routes";
            EditorUtils.CreateFolderIfNotExists(routesFolder);
            
            // Create ScriptableObject
            string assetPath = $"{routesFolder}/{routeName}.asset";
            var route = EditorUtils.CreateScriptableObject<FunClass.Core.StudentRoute>(assetPath);
            
            // Configure route
            route.routeName = routeName;
            route.movementSpeed = movementSpeed;
            route.rotationSpeed = 180f;
            route.isRunning = isRunning;
            route.isLooping = isLooping;
            route.isPingPong = isPingPong;
            
            // Convert GameObject references to StudentWaypoint references
            var waypointComponents = new List<FunClass.Core.StudentWaypoint>();
            foreach (var wpObj in waypoints)
            {
                var wpComponent = wpObj.GetComponent<FunClass.Core.StudentWaypoint>();
                if (wpComponent != null)
                {
                    waypointComponents.Add(wpComponent);
                }
            }
            
            route.waypoints = waypointComponents;
            
            // Save
            #if UNITY_EDITOR
            EditorUtility.SetDirty(route);
            AssetDatabase.SaveAssets();
            #endif
            
            Debug.Log($"[RouteGenerator] Created route asset: {routeName} with {waypointComponents.Count} waypoints");
            Debug.Log($"[RouteGenerator] Route waypoints: {string.Join(", ", waypointComponents.Select(wp => wp?.waypointName ?? "null"))}");
        }
        
        /// <summary>
        /// Visualize routes with gizmos (for editor debugging)
        /// </summary>
        public static void DrawRouteGizmos(List<EnhancedLevelImporter.StudentDeskPair> studentDeskPairs, EnhancedLevelSchema schema)
        {
            #if UNITY_EDITOR
            Vector3 doorPosition = DeskGridGenerator.CalculateDoorPosition(schema);
            Vector3 outsidePosition = DeskGridGenerator.CalculateOutsidePosition(schema);
            
            // Draw door and outside positions
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(doorPosition, 0.2f);
            Gizmos.DrawWireSphere(outsidePosition, 0.2f);
            
            // Draw escape routes for each student
            foreach (var pair in studentDeskPairs)
            {
                Vector3 deskPosition = pair.desk.position;
                Vector3 aislePosition = CalculateAislePosition(deskPosition, schema);
                
                // Escape route: desk → aisle → door → outside
                Gizmos.color = Color.red;
                DrawArrow(deskPosition, aislePosition);
                DrawArrow(aislePosition, doorPosition);
                DrawArrow(doorPosition, outsidePosition);
                
                // Return route: outside → door → aisle → desk  
                Gizmos.color = Color.green;
                DrawArrow(outsidePosition, doorPosition);
                DrawArrow(doorPosition, aislePosition);
                DrawArrow(aislePosition, deskPosition);
            }
            #endif
        }
        
        #if UNITY_EDITOR
        private static void DrawArrow(Vector3 from, Vector3 to)
        {
            Gizmos.DrawLine(from, to);
            
            // Draw arrowhead
            Vector3 direction = (to - from).normalized;
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 30, 0) * Vector3.back;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -30, 0) * Vector3.back;
            
            Gizmos.DrawLine(to, to + right * 0.2f);
            Gizmos.DrawLine(to, to + left * 0.2f);
        }
        #endif
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tạo waypoints và routes
    /// </summary>
    public static class WaypointRouteBuilder
    {
        /// <summary>
        /// Tạo escape route và return route
        /// </summary>
        public static (FunClass.Core.StudentRoute escapeRoute, FunClass.Core.StudentRoute returnRoute) 
            CreateRoutes(string levelName)
        {
            GameObject waypointsGroup = GameObject.Find("Waypoints");
            if (waypointsGroup == null)
            {
                Debug.LogWarning("[WaypointRouteBuilder] Waypoints group not found");
                return (null, null);
            }
            
            var escapeRoute = CreateEscapeRoute(levelName, waypointsGroup);
            var returnRoute = CreateReturnRoute(levelName, waypointsGroup);
            
            AssetDatabase.SaveAssets();
            
            return (escapeRoute, returnRoute);
        }

        /// <summary>
        /// Tạo default routes và return as list
        /// </summary>
        public static List<FunClass.Core.StudentRoute> CreateDefaultRoutes(string levelName)
        {
            GameObject waypointsGroup = GameObject.Find("Waypoints");
            if (waypointsGroup == null)
            {
                Debug.LogWarning("[WaypointRouteBuilder] Waypoints group not found");
                return new List<FunClass.Core.StudentRoute>();
            }
            
            var routes = new List<FunClass.Core.StudentRoute>();
            
            var escapeRoute = CreateEscapeRoute(levelName, waypointsGroup);
            if (escapeRoute != null) routes.Add(escapeRoute);
            
            var returnRoute = CreateReturnRoute(levelName, waypointsGroup);
            if (returnRoute != null) routes.Add(returnRoute);
            
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[WaypointRouteBuilder] Created {routes.Count} default routes");
            return routes;
        }

        private static FunClass.Core.StudentRoute CreateEscapeRoute(string levelName, GameObject parent)
        {
            GameObject escapeGroup = parent.transform.Find("EscapeRoute")?.gameObject;
            if (escapeGroup == null) return null;
            
            // Clear existing waypoints
            for (int i = escapeGroup.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(escapeGroup.transform.GetChild(i).gameObject);
            }
            
            // Create waypoints
            Vector3[] positions = new Vector3[]
            {
                new Vector3(0, 0, 0),      // Start (classroom)
                new Vector3(5, 0, 0),      // Middle
                new Vector3(10, 0, 0)      // Door/Outside
            };
            
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject wpObj = new GameObject($"Waypoint_{i}");
                wpObj.transform.SetParent(escapeGroup.transform);
                wpObj.transform.position = positions[i];
                
                var waypoint = wpObj.AddComponent<FunClass.Core.StudentWaypoint>();
                waypoint.waypointName = $"Escape_{i}";
                waypoint.waitDuration = 0f;
            }
            
            // Create route ScriptableObject
            var route = EditorUtils.CreateScriptableObject<FunClass.Core.StudentRoute>(
                $"Assets/Configs/{levelName}/Routes/EscapeRoute.asset"
            );
            
            route.routeName = "EscapeRoute";
            route.isRunning = true;
            route.movementSpeed = 4f;
            route.rotationSpeed = 360f;
            
            EditorUtility.SetDirty(route);
            
            return route;
        }

        private static FunClass.Core.StudentRoute CreateReturnRoute(string levelName, GameObject parent)
        {
            GameObject returnGroup = parent.transform.Find("ReturnRoute")?.gameObject;
            if (returnGroup == null) return null;
            
            // Clear existing waypoints
            for (int i = returnGroup.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(returnGroup.transform.GetChild(i).gameObject);
            }
            
            // Create waypoints (reverse of escape)
            Vector3[] positions = new Vector3[]
            {
                new Vector3(10, 0, 0),     // Start (outside)
                new Vector3(5, 0, 0),      // Middle
                new Vector3(0, 0, 0)       // Classroom
            };
            
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject wpObj = new GameObject($"Waypoint_{i}");
                wpObj.transform.SetParent(returnGroup.transform);
                wpObj.transform.position = positions[i];
                
                var waypoint = wpObj.AddComponent<FunClass.Core.StudentWaypoint>();
                waypoint.waypointName = $"Return_{i}";
                waypoint.waitDuration = 0f;
            }
            
            // Create route ScriptableObject
            var route = EditorUtils.CreateScriptableObject<FunClass.Core.StudentRoute>(
                $"Assets/Configs/{levelName}/Routes/ReturnRoute.asset"
            );
            
            route.routeName = "ReturnRoute";
            route.isRunning = false;
            route.movementSpeed = 2f;
            route.rotationSpeed = 180f;
            
            EditorUtility.SetDirty(route);
            
            return route;
        }

        /// <summary>
        /// Assign routes to level config
        /// </summary>
        public static void AssignRoutesToLevelConfig(
            FunClass.Core.LevelConfig levelConfig, 
            FunClass.Core.StudentRoute escapeRoute, 
            FunClass.Core.StudentRoute returnRoute)
        {
            if (levelConfig == null) return;
            
            levelConfig.escapeRoute = escapeRoute;
            levelConfig.returnRoute = returnRoute;
            
            // Set door reference
            GameObject door = GameObject.Find("Door");
            if (door != null)
            {
                levelConfig.classroomDoor = door.transform;
            }
            
            EditorUtility.SetDirty(levelConfig);
            AssetDatabase.SaveAssets();
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Creates waypoints at runtime from route data
    /// Solves issue where editor-created waypoints don't persist into Play mode
    /// </summary>
    public class RuntimeWaypointCreator : MonoBehaviour
    {
        void Start()
        {
            // Subscribe to level loaded event
            if (LevelLoader.Instance != null)
            {
                LevelLoader.Instance.OnLevelLoaded += OnLevelLoaded;
                Debug.Log("[RuntimeWaypointCreator] Subscribed to OnLevelLoaded event");
            }
            else
            {
                Debug.LogError("[RuntimeWaypointCreator] LevelLoader.Instance is null!");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from event
            if (LevelLoader.Instance != null)
            {
                LevelLoader.Instance.OnLevelLoaded -= OnLevelLoaded;
            }
        }

        private void OnLevelLoaded(LevelConfig level)
        {
            Debug.Log($"[RuntimeWaypointCreator] OnLevelLoaded called for level: {level.levelId}");
            CreateWaypointsForRoutes(level);
        }

        private void CreateWaypointsForRoutes(LevelConfig level)
        {
            if (level == null)
            {
                Debug.LogWarning("[RuntimeWaypointCreator] Level is null");
                return;
            }
            
            // Create waypoints for escape route
            if (level.escapeRoute != null)
            {
                CreateWaypointsForRoute(level.escapeRoute);
            }
            
            // Create waypoints for return route
            if (level.returnRoute != null)
            {
                CreateWaypointsForRoute(level.returnRoute);
            }
        }

        private void CreateWaypointsForRoute(StudentRoute route)
        {
            if (route == null)
            {
                Debug.LogError("[RuntimeWaypointCreator] Route is null!");
                return;
            }
            
            string routeName = string.IsNullOrEmpty(route.routeName) ? route.name : route.routeName;
            Debug.Log($"[RuntimeWaypointCreator] Creating waypoints for route: '{routeName}' (asset name: '{route.name}')");
            
            // Find or create Waypoints root
            GameObject waypointsRoot = GameObject.Find("Waypoints");
            if (waypointsRoot == null)
            {
                waypointsRoot = new GameObject("Waypoints");
                Debug.Log("[RuntimeWaypointCreator] Created Waypoints root");
            }
            
            // Find or create route group
            Transform routeGroup = waypointsRoot.transform.Find(routeName);
            if (routeGroup == null)
            {
                GameObject routeGroupObj = new GameObject(routeName);
                routeGroupObj.transform.SetParent(waypointsRoot.transform);
                routeGroup = routeGroupObj.transform;
                Debug.Log($"[RuntimeWaypointCreator] Created route group: {routeName}");
            }
            
            // Clear existing waypoints
            for (int i = routeGroup.childCount - 1; i >= 0; i--)
            {
                Destroy(routeGroup.GetChild(i).gameObject);
            }
            
            // Create waypoints based on route name (hardcoded for now)
            List<Vector3> waypointPositions = GetWaypointPositions(routeName);
            List<StudentWaypoint> waypoints = new List<StudentWaypoint>();
            
            Debug.Log($"[RuntimeWaypointCreator] Got {waypointPositions.Count} waypoint positions for '{routeName}'");
            
            for (int i = 0; i < waypointPositions.Count; i++)
            {
                GameObject wpObj = new GameObject($"Waypoint_{i}");
                wpObj.transform.SetParent(routeGroup);
                wpObj.transform.position = waypointPositions[i];
                
                var waypoint = wpObj.AddComponent<StudentWaypoint>();
                waypoint.waypointName = $"{routeName}_WP{i}";
                waypoint.waitDuration = 0f;
                
                waypoints.Add(waypoint);
                
                Debug.Log($"[RuntimeWaypointCreator] Created waypoint '{waypoint.waypointName}' at {waypointPositions[i]}");
            }
            
            // Assign waypoints to route
            route.waypoints = waypoints;
            Debug.Log($"[RuntimeWaypointCreator] ✓ Assigned {waypoints.Count} waypoints to '{routeName}'");
        }

        private List<Vector3> GetWaypointPositions(string routeName)
        {
            // Hardcoded waypoint positions from JSON
            if (routeName.ToLower().Contains("escape"))
            {
                return new List<Vector3>
                {
                    new Vector3(0, 0, 5),   // Door
                    new Vector3(0, 0, 10)   // Outside
                };
            }
            else if (routeName.ToLower().Contains("return"))
            {
                return new List<Vector3>
                {
                    new Vector3(0, 0, 5),    // Door
                    new Vector3(-2, 0, 0)    // Seat
                };
            }
            
            return new List<Vector3>();
        }
    }
}

using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Diagnostic script to check LevelConfig references at runtime
    /// Add this to any GameObject to debug route issues
    /// </summary>
    public class LevelConfigDiagnostic : MonoBehaviour
    {
        [Header("Auto-run on Start")]
        [SerializeField] private bool runOnStart = true;
        
        [Header("Manual Test")]
        [SerializeField] private bool runTest = false;

        void Start()
        {
            if (runOnStart)
            {
                Invoke(nameof(RunDiagnostic), 1f); // Wait 1s for managers to initialize
            }
        }

        void Update()
        {
            if (runTest)
            {
                runTest = false;
                RunDiagnostic();
            }
        }

        [ContextMenu("Run Diagnostic")]
        public void RunDiagnostic()
        {
            Debug.Log("=== LEVEL CONFIG DIAGNOSTIC ===");
            
            // Check LevelLoader
            if (LevelLoader.Instance == null)
            {
                Debug.LogError("✗ LevelLoader.Instance is NULL");
            }
            else
            {
                Debug.Log("✓ LevelLoader.Instance exists");
                
                if (LevelLoader.Instance.CurrentLevel == null)
                {
                    Debug.LogError("✗ LevelLoader.Instance.CurrentLevel is NULL");
                }
                else
                {
                    var level = LevelLoader.Instance.CurrentLevel;
                    Debug.Log($"✓ CurrentLevel: {level.name}");
                    Debug.Log($"  - Escape Route: {(level.escapeRoute != null ? level.escapeRoute.routeName : "NULL")}");
                    Debug.Log($"  - Return Route: {(level.returnRoute != null ? level.returnRoute.routeName : "NULL")}");
                    Debug.Log($"  - Classroom Door: {(level.classroomDoor != null ? level.classroomDoor.position.ToString() : "NULL")}");
                    
                    if (level.escapeRoute != null)
                    {
                        Debug.Log($"  - Escape Route Waypoints: {level.escapeRoute.waypoints.Count}");
                        for (int i = 0; i < level.escapeRoute.waypoints.Count; i++)
                        {
                            var wp = level.escapeRoute.waypoints[i];
                            Debug.Log($"    [{i}] {wp.name} at {wp.transform.position}");
                        }
                    }
                }
            }
            
            // Check LevelManager
            if (LevelManager.Instance == null)
            {
                Debug.LogError("✗ LevelManager.Instance is NULL");
            }
            else
            {
                Debug.Log("✓ LevelManager.Instance exists");
                var config = LevelManager.Instance.GetCurrentLevelConfig();
                if (config == null)
                {
                    Debug.LogError("✗ LevelManager.GetCurrentLevelConfig() returned NULL");
                }
                else
                {
                    Debug.Log($"✓ LevelManager has config: {config.name}");
                }
            }
            
            // Check if there's a LevelConfig in Assets
            var allConfigs = Resources.FindObjectsOfTypeAll<LevelConfig>();
            Debug.Log($"Found {allConfigs.Length} LevelConfig assets in project:");
            foreach (var cfg in allConfigs)
            {
                Debug.Log($"  - {cfg.name}");
                Debug.Log($"    Escape: {(cfg.escapeRoute != null ? cfg.escapeRoute.routeName : "NULL")}");
                Debug.Log($"    Return: {(cfg.returnRoute != null ? cfg.returnRoute.routeName : "NULL")}");
            }
            
            Debug.Log("=== END DIAGNOSTIC ===");
        }
    }
}

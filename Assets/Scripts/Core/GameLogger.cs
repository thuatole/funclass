using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Unified logging system with 3 tiers: MILESTONE, DETAIL, TRACE
    /// All logs follow format: [ComponentName] symbol (elapsed-time) message
    /// </summary>
    public static class GameLogger
    {
        // Static flags controlled by GameLoggerConfig
        public static bool MilestoneEnabled = true;
        public static bool TraceEnabled = false;
        
        // Per-component detail flags
        private static Dictionary<string, bool> detailFlags = new Dictionary<string, bool>();
        
        // Reference to config for periodic summary
        private static GameLoggerConfig config;

        /// <summary>
        /// Initialize the logger with a config reference
        /// </summary>
        public static void Initialize(GameLoggerConfig configRef)
        {
            config = configRef;
        }

        /// <summary>
        /// Set detail flag for a component
        /// </summary>
        public static void SetDetailEnabled(string componentName, bool enabled)
        {
            detailFlags[componentName] = enabled;
        }

        /// <summary>
        /// Get detail flag for a component
        /// </summary>
        public static bool IsDetailEnabled(string componentName)
        {
            return detailFlags.TryGetValue(componentName, out bool enabled) && enabled;
        }

        /// <summary>
        /// MILESTONE: Critical game events that define scenario flow
        /// Always logged - format: [Component] ★ (time) message
        /// </summary>
        public static void Milestone(string componentName, string message)
        {
            if (!MilestoneEnabled) return;
            
            float elapsed = GetElapsedTime();
            Debug.Log($"[{componentName}] ★ ({elapsed:F1}) {message}");
        }

        /// <summary>
        /// DETAIL: Useful for debugging specific systems
        /// Logged only if component detail flag is enabled - format: [Component] ● (time) message
        /// </summary>
        public static void Detail(string componentName, string message)
        {
            if (!IsDetailEnabled(componentName)) return;
            
            float elapsed = GetElapsedTime();
            Debug.Log($"[{componentName}] ● ({elapsed:F1}) {message}");
        }

        /// <summary>
        /// TRACE: Verbose per-frame data, off by default
        /// Logged only if TraceEnabled is true - format: [Component] ~ (time) message
        /// </summary>
        public static void Trace(string componentName, string message)
        {
            if (!TraceEnabled) return;
            
            float elapsed = GetElapsedTime();
            Debug.Log($"[{componentName}] ~ ({elapsed:F1}) {message}");
        }

        /// <summary>
        /// WARNING: For non-critical issues that need attention
        /// </summary>
        public static void Warning(string componentName, string message)
        {
            float elapsed = GetElapsedTime();
            Debug.LogWarning($"[{componentName}] ⚠ ({elapsed:F1}) {message}");
        }

        /// <summary>
        /// ERROR: For critical failures
        /// </summary>
        public static void Error(string componentName, string message)
        {
            float elapsed = GetElapsedTime();
            Debug.LogError($"[{componentName}] ✗ ({elapsed:F1}) {message}");
        }

        /// <summary>
        /// Get elapsed time from LevelManager, or Time.time if not available
        /// </summary>
        private static float GetElapsedTime()
        {
            if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive)
            {
                return LevelManager.Instance.LevelTimeElapsed;
            }
            return Time.time;
        }

        /// <summary>
        /// Trigger periodic summary output
        /// </summary>
        public static void OutputPeriodicSummary()
        {
            config?.TriggerSummaryNow();
        }
    }
}

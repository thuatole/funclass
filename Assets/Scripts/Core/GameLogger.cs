using UnityEngine;
using System;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Captured milestone event for automated scenario testing
    /// </summary>
    [Serializable]
    public struct CapturedEvent
    {
        public string component;
        public string eventType;
        public string source;
        public string target;
        public float elapsed;
        public string rawMessage;
    }

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

        // Captured events for automated scenario testing
        private static List<CapturedEvent> capturedEvents = new List<CapturedEvent>();

        /// <summary>
        /// Event triggered when a milestone is logged - for ScenarioRunner to capture
        /// </summary>
        public static event Action<CapturedEvent> OnMilestoneLogged;

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

            // Capture event for automated testing (backward compatibility - empty structured fields)
            CaptureEvent(componentName, string.Empty, string.Empty, string.Empty, elapsed, message);
        }

        /// <summary>
        /// MILESTONE with structured fields for automated scenario testing
        /// </summary>
        public static void Milestone(string componentName, string message, string eventType, string source, string target)
        {
            if (!MilestoneEnabled) return;

            float elapsed = GetElapsedTime();
            Debug.Log($"[{componentName}] ★ ({elapsed:F1}) {message}");

            // Capture event with structured fields
            CaptureEvent(componentName, eventType, source, target, elapsed, message);
        }

        /// <summary>
        /// Capture a milestone event for automated testing
        /// </summary>
        private static void CaptureEvent(string component, string eventType, string source, string target, float elapsed, string rawMessage)
        {
            var capturedEvent = new CapturedEvent
            {
                component = component,
                eventType = eventType,
                source = source,
                target = target,
                elapsed = elapsed,
                rawMessage = rawMessage
            };

            capturedEvents.Add(capturedEvent);
            OnMilestoneLogged?.Invoke(capturedEvent);
        }

        /// <summary>
        /// Clear all captured events - call at start of scenario test
        /// </summary>
        public static void ClearCapturedEvents()
        {
            capturedEvents.Clear();
        }

        /// <summary>
        /// Get all captured events - for ScenarioAsserter validation
        /// </summary>
        public static List<CapturedEvent> GetCapturedEvents()
        {
            return new List<CapturedEvent>(capturedEvents);
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

using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace FunClass.Editor
{
    /// <summary>
    /// Report containing all information about generated level
    /// </summary>
    public class LevelGenerationReport
    {
        public string LevelName { get; set; }
        public string Difficulty { get; set; }
        public float GenerationTime { get; set; }
        
        // Scene Objects
        public int ManagerCount { get; set; }
        public int StudentCount { get; set; }
        public int RouteCount { get; set; }
        public int WaypointCount { get; set; }
        public int InteractableCount { get; set; }
        public int MessPrefabCount { get; set; }
        public int SequenceCount { get; set; }
        
        // Configs
        public string LevelConfigPath { get; set; }
        public string GoalConfigPath { get; set; }
        public List<string> StudentConfigPaths { get; set; } = new List<string>();
        public List<string> RoutePaths { get; set; } = new List<string>();
        public List<string> SequencePaths { get; set; } = new List<string>();
        
        // Student Details
        public Dictionary<string, string> StudentArchetypes { get; set; } = new Dictionary<string, string>();
        
        // Route Details
        public Dictionary<string, int> RouteWaypoints { get; set; } = new Dictionary<string, int>();
        
        // Level Goals
        public float MaxDisruption { get; set; }
        public float TimeLimit { get; set; }
        public int MinStudentsSeated { get; set; }
        
        // Validation
        public bool ValidationPassed { get; set; }
        public int ValidationErrors { get; set; }
        public int ValidationWarnings { get; set; }
        public List<string> ValidationMessages { get; set; } = new List<string>();
        
        // Scene Hierarchy
        public List<string> HierarchyGroups { get; set; } = new List<string>();
        
        /// <summary>
        /// Generate formatted report text
        /// </summary>
        public string GetFormattedReport()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine($"    LEVEL GENERATION REPORT - {LevelName}");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine();
            
            // Basic Info
            sb.AppendLine("📊 BASIC INFO");
            sb.AppendLine($"  • Level Name: {LevelName}");
            sb.AppendLine($"  • Difficulty: {Difficulty}");
            sb.AppendLine($"  • Generation Time: {GenerationTime:F2}s");
            sb.AppendLine();
            
            // Scene Objects
            sb.AppendLine("🎮 SCENE OBJECTS");
            sb.AppendLine($"  • Managers: {ManagerCount}");
            sb.AppendLine($"  • Students: {StudentCount}");
            sb.AppendLine($"  • Routes: {RouteCount} ({WaypointCount} waypoints total)");
            sb.AppendLine($"  • Interactables: {InteractableCount}");
            sb.AppendLine($"  • Mess Prefabs: {MessPrefabCount}");
            sb.AppendLine($"  • Sequences: {SequenceCount}");
            sb.AppendLine();
            
            // Students Detail
            if (StudentArchetypes.Count > 0)
            {
                sb.AppendLine("👨‍🎓 STUDENTS");
                foreach (var student in StudentArchetypes)
                {
                    sb.AppendLine($"  • {student.Key}: {student.Value}");
                }
                sb.AppendLine();
            }
            
            // Routes Detail
            if (RouteWaypoints.Count > 0)
            {
                sb.AppendLine("🛤️ ROUTES");
                foreach (var route in RouteWaypoints)
                {
                    sb.AppendLine($"  • {route.Key}: {route.Value} waypoints");
                }
                sb.AppendLine();
            }
            
            // Level Goals
            sb.AppendLine("🎯 LEVEL GOALS");
            sb.AppendLine($"  • Max Disruption: {MaxDisruption}%");
            sb.AppendLine($"  • Time Limit: {(TimeLimit > 0 ? TimeLimit + "s" : "None")}");
            sb.AppendLine($"  • Min Students Seated: {MinStudentsSeated}");
            sb.AppendLine();
            
            // Configs
            sb.AppendLine("📁 GENERATED CONFIGS");
            sb.AppendLine($"  • Level Config: {LevelConfigPath}");
            sb.AppendLine($"  • Goal Config: {GoalConfigPath}");
            sb.AppendLine($"  • Student Configs: {StudentConfigPaths.Count} files");
            sb.AppendLine($"  • Route Configs: {RoutePaths.Count} files");
            sb.AppendLine($"  • Sequence Configs: {SequencePaths.Count} files");
            sb.AppendLine();
            
            // Scene Hierarchy
            if (HierarchyGroups.Count > 0)
            {
                sb.AppendLine("📂 SCENE HIERARCHY");
                foreach (var group in HierarchyGroups)
                {
                    sb.AppendLine($"  • {group}");
                }
                sb.AppendLine();
            }
            
            // Validation
            sb.AppendLine("✅ VALIDATION");
            if (ValidationPassed)
            {
                sb.AppendLine($"  • Status: ✅ PASSED");
            }
            else
            {
                sb.AppendLine($"  • Status: ⚠️ ISSUES FOUND");
            }
            sb.AppendLine($"  • Errors: {ValidationErrors}");
            sb.AppendLine($"  • Warnings: {ValidationWarnings}");
            
            if (ValidationMessages.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("  Validation Details:");
                foreach (var msg in ValidationMessages)
                {
                    sb.AppendLine($"    {msg}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get summary for dialog display
        /// </summary>
        public string GetSummary()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"Level '{LevelName}' generated successfully!");
            sb.AppendLine();
            sb.AppendLine($"Difficulty: {Difficulty}");
            sb.AppendLine($"Students: {StudentCount}");
            sb.AppendLine($"Routes: {RouteCount} ({WaypointCount} waypoints)");
            sb.AppendLine($"Interactables: {InteractableCount}");
            sb.AppendLine($"Sequences: {SequenceCount}");
            sb.AppendLine();
            
            if (ValidationPassed)
            {
                sb.AppendLine("✅ Validation: PASSED");
            }
            else
            {
                sb.AppendLine($"⚠️ Validation: {ValidationErrors} errors, {ValidationWarnings} warnings");
            }
            
            sb.AppendLine();
            sb.AppendLine($"Generation Time: {GenerationTime:F2}s");
            sb.AppendLine();
            sb.AppendLine("Check Console for detailed report.");
            
            return sb.ToString();
        }
    }
}

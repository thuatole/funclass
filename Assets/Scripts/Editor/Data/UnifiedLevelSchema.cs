using System;
using System.Collections.Generic;
using UnityEngine;

namespace FunClass.Editor.Data
{
    /// <summary>
    /// Unified JSON schema supporting both manual placement and auto-generation
    /// Can import legacy JSON files and enhanced JSON files
    /// </summary>
    [Serializable]
    public class UnifiedLevelSchema
    {
        // Basic level info (from both schemas)
        public string levelId; // Enhanced field
        public string levelName; // Legacy field (maps to levelId)
        public string difficulty; // From both
        
        // ===== AUTO-GENERATION FIELDS (Enhanced) =====
        // If these fields are present, auto-generation is enabled
        
        // Student count for auto-generation (if students array not provided)
        public int students = -1; // -1 means not specified, use students array if present
        
        // Desk layout for auto-generation
        public DeskLayoutData deskLayout;
        
        // Classroom dimensions for auto-generation
        public ClassroomData classroom;
        
        // Asset mapping overrides
        public AssetMappingData assetMapping;
        
        // Route generation settings
        public RouteGenerationData routeGeneration;
        
        // Environment settings for auto-generation
        public EnvironmentSettingsData environment;
        
        // Enhanced student configs (optional overrides for auto-generated students)
        public List<EnhancedStudentData> studentConfigs;
        
        // ===== MANUAL PLACEMENT FIELDS (Legacy) =====
        // If these fields are present, manual placement is used
        
        // Manual student definitions with positions
        public List<StudentData> studentsManual;
        
        // Manual route definitions
        public List<RouteData> routesManual;
        
        // Manual prefab placements
        public List<PrefabData> prefabs;
        
        // Manual interactable object placements
        public List<InteractableObjectData> interactableObjects;
        
        // Mess prefab definitions
        public List<MessPrefabData> messPrefabs;
        
        // Sequence definitions
        public List<SequenceData> sequences;
        
        // Manual environment data
        public EnvironmentData environmentManual;
        
        // ===== GAME SYSTEM CONFIGURATION (From both) =====
        // These are always used if present
        
        public LevelGoalData goalSettings;
        public InfluenceScopeSettingsData influenceScopeSettings;
        public List<StudentInteractionData> studentInteractions;
        
        // Manual route overrides (Enhanced field)
        public List<RouteData> routes; // Overrides auto-generated routes
        
        // Expected flow description (Enhanced field)
        public ExpectedFlowData expectedFlow;
        
        // ===== IMPORT MODE DETECTION =====
        // Not stored in JSON, computed by importer
        
        [NonSerialized]
        public ImportMode importMode = ImportMode.Auto;
        
        /// <summary>
        /// Determine import mode based on which fields are present
        /// </summary>
        public ImportMode DetectImportMode()
        {
            // Check for manual placement fields
            bool hasManualStudents = studentsManual != null && studentsManual.Count > 0;
            bool hasManualPrefabs = prefabs != null && prefabs.Count > 0;
            bool hasManualEnvironment = environmentManual != null;
            
            // Check for auto-generation fields
            bool hasStudentCount = students > 0;
            bool hasDeskLayout = deskLayout != null;
            bool hasClassroom = classroom != null;
            
            if (hasManualStudents || hasManualPrefabs || hasManualEnvironment)
            {
                // Has manual placement data
                if (hasStudentCount || hasDeskLayout || hasClassroom)
                {
                    // Also has auto-generation fields = Hybrid mode
                    return ImportMode.Hybrid;
                }
                else
                {
                    // Only manual fields = Manual mode
                    return ImportMode.Manual;
                }
            }
            else if (hasStudentCount || hasDeskLayout || hasClassroom)
            {
                // Has auto-generation fields, no manual fields = Auto mode
                return ImportMode.Auto;
            }
            else
            {
                // No fields specified, default to Auto with defaults
                return ImportMode.Auto;
            }
        }
        
        /// <summary>
        /// Normalize data after loading (e.g., map levelName to levelId)
        /// </summary>
        public void Normalize()
        {
            // Map legacy levelName to levelId if levelId not set
            if (string.IsNullOrEmpty(levelId) && !string.IsNullOrEmpty(levelName))
            {
                levelId = levelName;
            }
            
            // Ensure levelId is set
            if (string.IsNullOrEmpty(levelId))
            {
                levelId = "UnifiedLevel_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            
            // Detect import mode
            importMode = DetectImportMode();
            
            // Set defaults for auto-generation if needed
            if (importMode == ImportMode.Auto || importMode == ImportMode.Hybrid)
            {
                if (deskLayout == null)
                {
                    deskLayout = new DeskLayoutData();
                }
                
                if (classroom == null)
                {
                    classroom = new ClassroomData();
                }
                
                if (students <= 0)
                {
                    // Default to 4 students if not specified
                    students = 4;
                }
                
                if (environment == null)
                {
                    environment = new EnvironmentSettingsData();
                }
                
                if (routeGeneration == null)
                {
                    routeGeneration = new RouteGenerationData();
                }
            }
        }
    }
    
    /// <summary>
    /// Import mode determined by schema fields
    /// </summary>
    public enum ImportMode
    {
        Auto,      // Auto-generate desks, environment, routes
        Manual,    // Use manual placements from JSON
        Hybrid     // Mix of auto-generation and manual overrides
    }
    
    // ===== REUSED DATA STRUCTURES FROM EXISTING SCHEMAS =====
    // These are identical copies from EnhancedLevelSchema and LevelDataSchema
    // We keep them separate to avoid breaking existing code
    
    // Note: All the nested data classes from EnhancedLevelSchema and LevelDataSchema
    // will be available since they're in the same namespace (FunClass.Editor.Data)
    // We don't need to redefine them here
}
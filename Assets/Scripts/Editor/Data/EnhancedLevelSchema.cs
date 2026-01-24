using System;
using System.Collections.Generic;
using UnityEngine;

namespace FunClass.Editor.Data
{
    /// <summary>
    /// Enhanced JSON schema for simplified level import with auto-generation features
    /// Follows the spec from FunClass_Auto_Level_JSON_Import_A-Z.md
    /// </summary>
    [Serializable]
    public class EnhancedLevelSchema
    {
        public string levelId;
        public string difficulty; // "easy", "medium", "hard"
        public int students; // Total number of students (must be even, 4-10)
        
        public DeskLayoutData deskLayout;
        public ClassroomData classroom;
        
        // Optional: Override prefab mappings (JSON path to AssetMapConfig or inline mapping)
        public AssetMappingData assetMapping;
        
        // Optional: Manual student configuration (if not provided, auto-generated)
        public List<EnhancedStudentData> studentConfigs;
        
        // Optional: Manual route overrides
        public RouteGenerationData routeGeneration;
        
        // Optional: Environment settings
        public EnvironmentSettingsData environment;
        
        // Optional: Game system settings (extends simple JSON to full game setup)
        public LevelGoalData goalSettings;
        public InfluenceScopeSettingsData influenceScopeSettings;
        public List<StudentInteractionData> studentInteractions;
        public List<RouteData> routes; // Manual route definitions (overrides auto-generation)
        public ExpectedFlowData expectedFlow; // Optional: describes expected scenario flow
    }
    
    [Serializable]
    public class DeskLayoutData
    {
        public int rows = 2; // Always 2 rows according to spec
        public float spacingX = 2.0f; // Spacing between desks in X direction (width)
        public float spacingZ = 2.5f; // Spacing between desks in Z direction (depth)
        public float aisleWidth = 1.5f; // Gap between rows for aisle
    }
    
    [Serializable]
    public class ClassroomData
    {
        public float width = 10f; // Classroom width (X axis)
        public float depth = 8f; // Classroom depth (Z axis)
        public float height = 3f; // Classroom height (Y axis)
        
        // Door position (auto-calculated if not specified)
        public Vector3Data doorPosition;
        
        // Board position (auto-calculated if not specified)
        public Vector3Data boardPosition;
    }
    
    [Serializable]
    public class AssetMappingData
    {
        // AssetKey -> Prefab path mapping
        public Dictionary<string, string> prefabMapping;
        
        // Material overrides for pink material fix
        public Dictionary<string, string> materialOverrides;
    }
    
    [Serializable]
    public class EnhancedStudentData
    {
        public string studentId;
        public string studentName;
        public string deskId; // Reference to specific desk if manual placement
        
        // Personality (if not provided, random within difficulty bounds)
        public PersonalityData personality;
        
        // Behaviors (if not provided, default)
        public BehaviorData behaviors;
    }
    
    [Serializable]
    public class RouteGenerationData
    {
        public bool autoGenerateRoutes = true;
        
        // Door position for escape/return routes
        public Vector3Data doorPosition;
        
        // Outside position (where students go when escaping)
        public Vector3Data outsidePosition;
        
        // Waypoint naming pattern
        public string waypointNamePattern = "{studentId}_{routeType}_{index:00}";
        
        // Route settings
        public float escapeRouteSpeed = 3.0f;
        public float returnRouteSpeed = 2.0f;
        public bool isRunning = false;
    }
    
    [Serializable]
    public class EnvironmentSettingsData
    {
        // Board settings
        public Vector3Data boardSize = new Vector3Data(4f, 2f, 0.1f);
        public string boardMaterial = "White";
        
        // Floor material
        public string floorMaterial = "Floor";
        
        // Wall material  
        public string wallMaterial = "Wall";
        
        // Lighting settings
        public bool autoSetupLighting = true;
        public float ambientIntensity = 1.0f;
    }
    
    /// <summary>
    /// Student interaction definition for complex scenarios
    /// </summary>
    [Serializable]
    public class StudentInteractionData
    {
        public string sourceStudent;
        public string targetStudent;
        public string eventType;
        public string triggerCondition;
        public float probability = 1.0f;
        public float customSeverity = -1f;
        public string description;
    }
    
    /// <summary>
    /// Expected flow description for complex scenarios (descriptive only)
    /// </summary>
    [Serializable]
    public class ExpectedFlowData
    {
        // Can't use Dictionary with JsonUtility, so we'll use List of key-value pairs
        // or just a descriptive text field
        public string description;
        public List<FlowStep> steps;
    }
    
    [Serializable]
    public class FlowStep
    {
        public string stepId;
        public string description;
    }
}
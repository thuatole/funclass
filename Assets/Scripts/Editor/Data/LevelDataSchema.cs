using System;
using System.Collections.Generic;
using UnityEngine;

namespace FunClass.Editor.Data
{
    /// <summary>
    /// JSON schema cho level data - có thể serialize/deserialize
    /// </summary>
    [Serializable]
    public class LevelDataSchema
    {
        public string levelName;
        public string difficulty; // "Easy", "Normal", "Hard"
        public LevelGoalData goalSettings;
        public List<StudentData> students;
        public List<RouteData> routes;
        public List<PrefabData> prefabs;
        public List<InteractableObjectData> interactableObjects;
        public List<MessPrefabData> messPrefabs;
        public List<SequenceData> sequences;
        public EnvironmentData environment;
    }

    [Serializable]
    public class LevelGoalData
    {
        public float maxDisruptionThreshold = 80f;
        public float catastrophicDisruptionLevel = 95f;
        public int maxAllowedCriticalStudents = 2;
        public int catastrophicCriticalStudents = 4;
        public int maxAllowedOutsideStudents = 2;
        public int catastrophicOutsideStudents = 5;
        public float maxOutsideTimePerStudent = 60f;
        public float maxAllowedOutsideGracePeriod = 10f;
        public float timeLimitSeconds = 300f;
        public int requiredResolvedProblems = 5;
        public int requiredCalmDowns = 3;
        
        // Disruption Timeout
        public bool enableDisruptionTimeout = false;
        public float disruptionTimeoutThreshold = 80f;
        public float disruptionTimeoutSeconds = 60f;
        public float disruptionTimeoutWarningSeconds = 15f;
        
        public int oneStarScore = 100;
        public int twoStarScore = 250;
        public int threeStarScore = 500;
    }

    [Serializable]
    public class StudentData
    {
        public string studentName;
        public Vector3Data position;
        public PersonalityData personality;
        public BehaviorData behaviors;
    }

    [Serializable]
    public class PersonalityData
    {
        public float patience = 0.5f;
        public float attentionSpan = 0.5f;
        public float impulsiveness = 0.5f;
        public float influenceSusceptibility = 0.5f;
        public float influenceResistance = 0.2f;
        public float panicThreshold = 0.7f;
    }

    [Serializable]
    public class BehaviorData
    {
        public bool canFidget = true;
        public bool canLookAround = true;
        public bool canStandUp = false;
        public bool canMoveAround = false;
        public bool canDropItems = false;
        public bool canKnockOverObjects = false;
        public bool canMakeNoiseWithObjects = true;
        public bool canThrowObjects = false;
        public float minIdleTime = 2f;
        public float maxIdleTime = 8f;
    }

    [Serializable]
    public class RouteData
    {
        public string routeName;
        public string routeType; // "Escape", "Return", "Wander", "Custom"
        public List<WaypointData> waypoints;
        public float movementSpeed = 2f;
        public float rotationSpeed = 180f;
        public bool isRunning = false;
        public bool isLooping = false;
        public bool isPingPong = false;
    }

    [Serializable]
    public class WaypointData
    {
        public string waypointName;
        public Vector3Data position;
        public float waitDuration = 0f;
        public string actionToTrigger = "Idle"; // StudentActionType as string
    }

    [Serializable]
    public class PrefabData
    {
        public string prefabName;
        public string prefabType; // "Student", "Furniture", "Interactable", "Decoration"
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public string prefabPath; // Path to prefab asset
    }

    [Serializable]
    public class EnvironmentData
    {
        public Vector3Data classroomSize;
        public Vector3Data doorPosition;
        public List<Vector3Data> windowPositions;
        public string floorMaterial;
        public string wallMaterial;
    }

    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data() { }
        
        public Vector3Data(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Data(Vector3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static implicit operator Vector3(Vector3Data data)
        {
            return data.ToVector3();
        }

        public static implicit operator Vector3Data(Vector3 v)
        {
            return new Vector3Data(v);
        }
    }

    [Serializable]
    public class InteractableObjectData
    {
        public string objectName;
        public string objectType; // "Book", "Pencil", "Ball", etc.
        public Vector3Data position;
        public bool canKnockOver = true;
        public bool canThrow = true;
        public bool canMakeNoise = true;
    }

    [Serializable]
    public class MessPrefabData
    {
        public string messType; // "Vomit", "Spill", "Trash", etc.
        public string prefabPath;
        public bool autoGenerate = true; // Auto-generate if prefab not found
    }

    [Serializable]
    public class SequenceData
    {
        public string sequenceId;
        public string sequenceTemplate; // "SimpleWarning", "ObjectConfiscation", etc.
        public string entryState;
        public string description;
    }
}

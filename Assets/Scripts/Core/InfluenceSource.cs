using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Tracks influence sources affecting a student
    /// A student can be influenced by multiple sources
    /// All sources must be resolved before student can be escorted back successfully
    /// </summary>
    public class InfluenceSource
    {
        public StudentAgent sourceStudent;      // Student causing the influence
        public StudentEventType eventType;      // Type of event causing influence
        public float influenceStrength;         // Strength of influence
        public float timestamp;                 // When influence started
        public bool isResolved;                 // Whether source has been resolved (calmed down)

        public InfluenceSource(StudentAgent source, StudentEventType type, float strength)
        {
            sourceStudent = source;
            eventType = type;
            influenceStrength = strength;
            timestamp = Time.time;
            isResolved = false;
        }

        public override string ToString()
        {
            string sourceName = sourceStudent?.Config?.studentName ?? "Unknown";
            return $"{sourceName} ({eventType}, strength: {influenceStrength:F2}, resolved: {isResolved})";
        }
    }

    /// <summary>
    /// Manages influence sources for a student
    /// </summary>
    public class StudentInfluenceSources
    {
        private StudentAgent targetStudent;
        private List<InfluenceSource> activeSources = new List<InfluenceSource>();

        public StudentInfluenceSources(StudentAgent student)
        {
            targetStudent = student;
        }

        /// <summary>
        /// Add a new influence source
        /// </summary>
        public void AddSource(StudentAgent sourceStudent, StudentEventType eventType, float strength)
        {
            string targetName = targetStudent?.Config?.studentName ?? "Unknown";
            string sourceName = sourceStudent?.Config?.studentName ?? "Unknown";
            
            Debug.Log($"[InfluenceSources] >>> AddSource called: {sourceName} → {targetName} ({eventType}, strength: {strength:F2})");
            
            // Check if source already exists
            var existing = activeSources.Find(s => 
                s.sourceStudent == sourceStudent && 
                s.eventType == eventType && 
                !s.isResolved
            );

            if (existing != null)
            {
                // Update strength if stronger
                if (strength > existing.influenceStrength)
                {
                    Debug.Log($"[InfluenceSources] Updated existing source strength: {existing.influenceStrength:F2} → {strength:F2}");
                    existing.influenceStrength = strength;
                    existing.timestamp = Time.time;
                }
                else
                {
                    Debug.Log($"[InfluenceSources] Source already exists with equal/higher strength ({existing.influenceStrength:F2}), skipping");
                }
            }
            else
            {
                // Add new source
                var newSource = new InfluenceSource(sourceStudent, eventType, strength);
                activeSources.Add(newSource);
                
                Debug.Log($"[InfluenceSources] ✓ Added NEW source to {targetName}: {newSource}");
                Debug.Log($"[InfluenceSources] Total sources for {targetName}: {activeSources.Count} ({GetUnresolvedSourceCount()} unresolved)");
            }
        }

        /// <summary>
        /// Mark a source as resolved when source student is calmed down
        /// </summary>
        public void ResolveSource(StudentAgent sourceStudent)
        {
            string targetName = targetStudent?.Config?.studentName ?? "Unknown";
            string sourceName = sourceStudent?.Config?.studentName ?? "Unknown";
            
            Debug.Log($"[InfluenceSources] >>> ResolveSource called: {sourceName} affecting {targetName}");
            
            int resolvedCount = 0;
            
            foreach (var source in activeSources)
            {
                if (source.sourceStudent == sourceStudent && !source.isResolved)
                {
                    Debug.Log($"[InfluenceSources] Resolving source: {source}");
                    source.isResolved = true;
                    resolvedCount++;
                }
            }

            if (resolvedCount > 0)
            {
                int remainingUnresolved = GetUnresolvedSourceCount();
                Debug.Log($"[InfluenceSources] ✓ Resolved {resolvedCount} sources from {sourceName} affecting {targetName}");
                Debug.Log($"[InfluenceSources] Remaining unresolved sources for {targetName}: {remainingUnresolved}");
            }
            else
            {
                Debug.Log($"[InfluenceSources] No unresolved sources from {sourceName} found for {targetName}");
            }
        }

        /// <summary>
        /// Check if all sources are resolved
        /// </summary>
        public bool AreAllSourcesResolved()
        {
            string targetName = targetStudent?.Config?.studentName ?? "Unknown";
            
            if (activeSources.Count == 0)
            {
                Debug.Log($"[InfluenceSources] {targetName} has no sources - all resolved");
                return true;
            }

            int unresolvedCount = 0;
            foreach (var source in activeSources)
            {
                if (!source.isResolved)
                {
                    unresolvedCount++;
                }
            }

            bool allResolved = (unresolvedCount == 0);
            Debug.Log($"[InfluenceSources] {targetName} sources check: {activeSources.Count} total, {unresolvedCount} unresolved → All resolved: {allResolved}");
            
            return allResolved;
        }

        /// <summary>
        /// Get count of unresolved sources
        /// </summary>
        public int GetUnresolvedSourceCount()
        {
            int count = 0;
            foreach (var source in activeSources)
            {
                if (!source.isResolved)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Get list of unresolved source students
        /// </summary>
        public List<StudentAgent> GetUnresolvedSourceStudents()
        {
            var students = new List<StudentAgent>();
            
            foreach (var source in activeSources)
            {
                if (!source.isResolved && source.sourceStudent != null)
                {
                    if (!students.Contains(source.sourceStudent))
                    {
                        students.Add(source.sourceStudent);
                    }
                }
            }

            return students;
        }

        /// <summary>
        /// Clear all resolved sources
        /// </summary>
        public void ClearResolvedSources()
        {
            activeSources.RemoveAll(s => s.isResolved);
        }

        /// <summary>
        /// Clear all sources (when student is successfully escorted)
        /// </summary>
        public void ClearAllSources()
        {
            activeSources.Clear();
            Debug.Log($"[InfluenceSources] Cleared all sources for {targetStudent.Config?.studentName}");
        }

        /// <summary>
        /// Get total influence strength from all unresolved sources
        /// </summary>
        public float GetTotalInfluenceStrength()
        {
            float total = 0f;
            
            foreach (var source in activeSources)
            {
                if (!source.isResolved)
                {
                    total += source.influenceStrength;
                }
            }

            return total;
        }

        public List<InfluenceSource> GetActiveSources()
        {
            return new List<InfluenceSource>(activeSources);
        }

        public override string ToString()
        {
            int total = activeSources.Count;
            int unresolved = GetUnresolvedSourceCount();
            return $"{targetStudent.Config?.studentName}: {unresolved}/{total} unresolved sources";
        }
    }
}

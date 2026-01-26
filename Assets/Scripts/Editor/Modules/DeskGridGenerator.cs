using UnityEngine;
using System.Collections.Generic;
using FunClass.Editor.Data;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Generates a 2-row grid of desks according to Phase 3 of the spec
    /// </summary>
    public static class DeskGridGenerator
    {
        /// <summary>
        /// Generate desk grid based on schema
        /// </summary>
        public static List<EnhancedLevelImporter.DeskData> GenerateDeskGrid(EnhancedLevelSchema schema)
        {
            Debug.Log($"[DeskGridGenerator] Generating desk grid for {schema.students} students");
            
            // Calculate grid dimensions
            int rows = schema.deskLayout.rows; // Always 2
            int columns = schema.students / rows;
            
            Debug.Log($"[DeskGridGenerator] Grid: {rows} rows x {columns} columns");
            
            // Calculate classroom bounds
            float classroomWidth = schema.classroom.width;
            float classroomDepth = schema.classroom.depth;
            
            // Calculate total grid width and depth
            float totalGridWidth = (columns - 1) * schema.deskLayout.spacingX;
            float totalGridDepth = (rows - 1) * schema.deskLayout.spacingZ;
            
            // Calculate starting position (center of grid at classroom center)
            Vector3 gridCenter = new Vector3(0, 0, 0); // Classroom center
            Vector3 startPos = gridCenter - new Vector3(totalGridWidth / 2f, 0, totalGridDepth / 2f);
            
            // Adjust for aisle between rows
            float aisleOffset = schema.deskLayout.aisleWidth / 2f;
            
            List<EnhancedLevelImporter.DeskData> desks = new List<EnhancedLevelImporter.DeskData>();
            
            // Generate desks
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    // Calculate position
                    float x = startPos.x + col * schema.deskLayout.spacingX;
                    float z = startPos.z + row * schema.deskLayout.spacingZ;
                    
                    // Adjust for aisle (gap between rows)
                    if (row == 1) // Back row
                    {
                        z += aisleOffset;
                    }
                    else // Front row  
                    {
                        z -= aisleOffset;
                    }
                    
                    Vector3 position = new Vector3(x, 0, z);
                    
                    // Create desk data
                    EnhancedLevelImporter.DeskData desk = new EnhancedLevelImporter.DeskData
                    {
                        deskId = $"Desk_{row}_{col}",
                        position = position,
                        rotation = Quaternion.identity,
                        studentSlot = null,
                        messSlot = null
                    };
                    
                    // Calculate student slot (slightly in front of desk)
                    desk.studentSlot = new GameObject($"{desk.deskId}_StudentSlot");
                    desk.studentSlot.transform.position = position + new Vector3(0, 0, -0.3f);
                    desk.studentSlot.transform.SetParent(null); // Will be parented later
                    
                    // Calculate mess slot (on top of desk)
                    desk.messSlot = new GameObject($"{desk.deskId}_MessSlot");
                    desk.messSlot.transform.position = position + new Vector3(0, 0.8f, 0);
                    desk.messSlot.transform.SetParent(null); // Will be parented later
                    
                    desks.Add(desk);
                    
                    Debug.Log($"[DeskGridGenerator] Created desk {desk.deskId} at {position}");
                }
            }
            
            // Validate desk positions are within classroom bounds
            ValidateDeskPositions(desks, schema);
            
            Debug.Log($"[DeskGridGenerator] Generated {desks.Count} desks");
            return desks;
        }
        
        /// <summary>
        /// Validate that all desks are within classroom bounds
        /// </summary>
        private static void ValidateDeskPositions(List<EnhancedLevelImporter.DeskData> desks, EnhancedLevelSchema schema)
        {
            float halfWidth = schema.classroom.width / 2f;
            float halfDepth = schema.classroom.depth / 2f;
            
            int outOfBoundsCount = 0;
            
            foreach (var desk in desks)
            {
                if (Mathf.Abs(desk.position.x) > halfWidth ||
                    Mathf.Abs(desk.position.z) > halfDepth)
                {
                    outOfBoundsCount++;
                    Debug.LogWarning($"[DeskGridGenerator] Desk {desk.deskId} at {desk.position} is outside classroom bounds");
                }
            }
            
            if (outOfBoundsCount > 0)
            {
                Debug.LogWarning($"[DeskGridGenerator] {outOfBoundsCount} desks are outside classroom bounds. Consider adjusting spacing or classroom size.");
            }
        }
        
        /// <summary>
        /// Calculate door position (default at back wall, 60% from left edge)
        /// This ensures alignment with wall gap and escape routes
        /// </summary>
        public static Vector3 CalculateDoorPosition(EnhancedLevelSchema schema)
        {
            float halfWidth = schema.classroom.width / 2f;
            float halfDepth = schema.classroom.depth / 2f;

            if (schema.classroom.doorPosition != null && schema.classroom.doorPosition.ToVector3() != Vector3.zero)
            {
                Vector3 manualPos = schema.classroom.doorPosition.ToVector3();

                // Validate door is within classroom bounds
                bool outOfBounds = false;
                if (Mathf.Abs(manualPos.x) > halfWidth)
                {
                    Debug.LogWarning($"[DeskGridGenerator] Door X={manualPos.x} outside classroom width (half={halfWidth}). Clamping.");
                    outOfBounds = true;
                }
                if (Mathf.Abs(manualPos.z) > halfDepth)
                {
                    Debug.LogWarning($"[DeskGridGenerator] Door Z={manualPos.z} outside classroom depth (half={halfDepth}). Should be between -{halfDepth} and +{halfDepth}.");
                    outOfBounds = true;
                }

                if (outOfBounds)
                {
                    Debug.LogWarning($"[DeskGridGenerator] Door position {manualPos} is out of bounds! Classroom: W={schema.classroom.width}, D={schema.classroom.depth}");
                    Debug.Log($"[DeskGridGenerator] Using auto-calculated door position instead.");
                }
                else
                {
                    Debug.Log($"[DeskGridGenerator] Using manual door position: {manualPos}");
                    return manualPos;
                }
            }

            // Default door position: back wall, at 60% from left edge
            // This ensures alignment with wall gap and escape routes
            float doorX = -halfWidth + (schema.classroom.width * 0.6f);  // 60% from left
            Vector3 pos = new Vector3(doorX, 0, halfDepth);
            Debug.Log($"[DeskGridGenerator] Calculated door position: {pos} (60% from left, classroom width={schema.classroom.width})");

            return pos;
        }
        
        /// <summary>
        /// Calculate board position (default at front wall)
        /// </summary>
        public static Vector3 CalculateBoardPosition(EnhancedLevelSchema schema)
        {
            Vector3 pos;
            bool isManual = false;
            
            if (schema.classroom.boardPosition != null && schema.classroom.boardPosition.ToVector3() != Vector3.zero)
            {
                pos = schema.classroom.boardPosition.ToVector3();
                Debug.Log($"[DeskGridGenerator] Using manual board position: {pos}");
                isManual = true;
            }
            else
            {
                // Default board position: front center, slightly above floor
                pos = new Vector3(0, 1.5f, -schema.classroom.depth / 2f + 0.1f);
                Debug.Log($"[DeskGridGenerator] Calculated board position: {pos} (classroom depth={schema.classroom.depth})");
            }
            
            // Validate board is not too close to desks
            Vector2 gridBounds = CalculateDeskGridBounds(schema);
            float frontDeskZ = gridBounds.x; // Most forward (negative) desk position
            float backDeskZ = gridBounds.y;  // Most backward (positive) desk position
            
            // Board should be in front of desks (more negative Z than front desk row)
            float minClearance = 1.0f;
            if (pos.z > frontDeskZ - minClearance) // Board is behind or too close to front row
            {
                if (isManual)
                {
                    Debug.LogWarning($"[DeskGridGenerator] Manual board position {pos.z} is too close to desks (front desk Z: {frontDeskZ}). Consider moving board further forward.");
                }
                else
                {
                    Debug.LogWarning($"[DeskGridGenerator] Board position {pos.z} is too close to desks (front desk Z: {frontDeskZ}). Adjusting.");
                    pos.z = frontDeskZ - 1.5f; // Ensure 1.5 units clearance
                }
            }
            
            // Also ensure board is within classroom bounds (not behind front wall)
            float frontWallZ = -schema.classroom.depth / 2f;
            if (pos.z < frontWallZ)
            {
                Debug.LogWarning($"[DeskGridGenerator] Board position {pos.z} is behind front wall ({frontWallZ}). Clamping to wall.");
                pos.z = frontWallZ + 0.1f; // Place just inside wall
            }
            
            Debug.Log($"[DeskGridGenerator] Final board position: {pos}");
            return pos;
        }
        
        /// <summary>
        /// Calculate approximate desk grid bounds (min Z, max Z) where min Z is most forward (negative) desk position,
        /// max Z is most backward (positive) desk position. Returns Vector2(minZ, maxZ).
        /// </summary>
        public static Vector2 CalculateDeskGridBounds(EnhancedLevelSchema schema)
        {
            int rows = schema.deskLayout.rows;
            int columns = schema.students / rows;
            
            float totalGridDepth = (rows - 1) * schema.deskLayout.spacingZ;
            float aisleOffset = schema.deskLayout.aisleWidth / 2f;
            
            // Front row Z (most forward/negative)
            float frontRowZ = -totalGridDepth / 2f - aisleOffset;
            // Back row Z (most backward/positive)
            float backRowZ = -totalGridDepth / 2f + (rows - 1) * schema.deskLayout.spacingZ + aisleOffset;
            
            // Ensure front row is more negative than back row
            if (frontRowZ > backRowZ)
            {
                float temp = frontRowZ;
                frontRowZ = backRowZ;
                backRowZ = temp;
            }
            
            Debug.Log($"[DeskGridGenerator] Desk grid bounds: front Z={frontRowZ}, back Z={backRowZ}");
            return new Vector2(frontRowZ, backRowZ);
        }
        
        /// <summary>
        /// Calculate outside position (where students go when escaping)
        /// </summary>
        public static Vector3 CalculateOutsidePosition(EnhancedLevelSchema schema)
        {
            // Default outside position: outside classroom from door
            Vector3 doorPos = CalculateDoorPosition(schema);
            
            // Determine direction: if door is at back wall (positive Z), outside is further positive Z
            // If door is at front wall (negative Z), outside is further negative Z
            // Default: assume door faces outward from classroom
            float outsideOffset = 2f;
            float doorZ = doorPos.z;
            float classroomHalfDepth = schema.classroom.depth / 2f;
            
            // Determine which wall door is on and set offset direction
            Vector3 outsidePos;
            if (Mathf.Abs(doorZ - classroomHalfDepth) < 0.1f) // Door at back wall (+Z)
            {
                outsidePos = doorPos + new Vector3(0, 0, outsideOffset); // Further outside (positive Z)
                Debug.Log($"[DeskGridGenerator] Door at back wall, outside position: {outsidePos} (offset +{outsideOffset} in Z)");
            }
            else if (Mathf.Abs(doorZ + classroomHalfDepth) < 0.1f) // Door at front wall (-Z)
            {
                outsidePos = doorPos + new Vector3(0, 0, -outsideOffset); // Further outside (negative Z)
                Debug.Log($"[DeskGridGenerator] Door at front wall, outside position: {outsidePos} (offset -{outsideOffset} in Z)");
            }
            else // Door on side wall or custom position
            {
                // Default: assume door faces positive Z direction (outward)
                outsidePos = doorPos + new Vector3(0, 0, outsideOffset);
                Debug.Log($"[DeskGridGenerator] Door at custom position {doorPos}, outside position: {outsidePos}");
            }
            
            return outsidePos;
        }
        
        /// <summary>
        /// Visualize desk grid with gizmos (for editor debugging)
        /// </summary>
        public static void DrawDeskGridGizmos(List<EnhancedLevelImporter.DeskData> desks)
        {
            #if UNITY_EDITOR
            foreach (var desk in desks)
            {
                // Draw desk position
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(desk.position, new Vector3(0.8f, 0.8f, 0.8f));
                
                // Draw student slot
                if (desk.studentSlot != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(desk.studentSlot.transform.position, 0.1f);
                }
                
                // Draw mess slot
                if (desk.messSlot != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(desk.messSlot.transform.position, 0.1f);
                }
            }
            #endif
        }
    }
}
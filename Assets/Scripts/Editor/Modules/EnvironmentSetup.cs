using UnityEngine;
using FunClass.Editor.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Sets up board and environment (Phase 7)
    /// </summary>
    public static class EnvironmentSetup
    {
        /// <summary>
        /// Setup classroom environment: board, walls, floor, etc.
        /// </summary>
        public static void SetupEnvironment(EnhancedLevelSchema schema, AssetMapConfig assetMap)
        {
            Debug.Log("[EnvironmentSetup] Setting up classroom environment");
            
            // Get or create classroom group
            GameObject classroomGroup = SceneSetupManager.GetOrCreateClassroomGroup();
            
            // 1. Setup board
            SetupBoard(schema, assetMap, classroomGroup);
            
            // 2. Setup door
            SetupDoor(schema, assetMap, classroomGroup);
            
            // 3. Setup walls (visual representation)
            SetupWalls(schema, assetMap, classroomGroup);
            
            // 4. Setup floor
            SetupFloor(schema, assetMap, classroomGroup);
            
            // 5. Setup teacher area
            SetupTeacherArea(schema, assetMap, classroomGroup);
            
            Debug.Log("[EnvironmentSetup] Environment setup complete");
        }
        
        /// <summary>
        /// Setup whiteboard/blackboard at front of classroom
        /// </summary>
        private static void SetupBoard(EnhancedLevelSchema schema, AssetMapConfig assetMap, GameObject classroomGroup)
        {
            // Get board prefab
            GameObject boardPrefab = assetMap.GetPrefab("BOARD");
            if (boardPrefab == null)
            {
                Debug.LogWarning("[EnvironmentSetup] No board prefab found in asset map. Creating placeholder.");
                boardPrefab = CreatePlaceholderBoard();
            }
            
            // Calculate board position
            Vector3 boardPosition = DeskGridGenerator.CalculateBoardPosition(schema);
            Debug.Log($"[EnvironmentSetup] Classroom dimensions: width={schema.classroom.width}, depth={schema.classroom.depth}, boardPosition={boardPosition}");
            
            // Create board group
            GameObject boardGroup = SceneSetupManager.CreateOrFindGameObject("Board", classroomGroup.transform);
            
            // Instantiate board
            GameObject boardObj = PrefabUtility.InstantiatePrefab(boardPrefab) as GameObject;
            boardObj.name = "ClassroomBoard";
            
            // Calculate board offset based on bounds to ensure front surface is at desired position
            Vector3Data boardSize = null;
            if (schema.environment != null)
            {
                boardSize = schema.environment.boardSize;
            }
            boardPosition = AdjustBoardPositionForBounds(boardObj, boardPosition, boardSize);
            
            boardObj.transform.position = boardPosition;
            boardObj.transform.rotation = Quaternion.Euler(0, 180, 0); // Face classroom
            boardObj.transform.SetParent(boardGroup.transform);
            
            // Debug: Log board bounds after placement
            var boardRenderer = boardObj.GetComponent<Renderer>();
            if (boardRenderer != null)
            {
                Bounds boardBounds = boardRenderer.bounds;
                Debug.Log($"[EnvironmentSetup] Board final bounds: center={boardBounds.center}, size={boardBounds.size}, min={boardBounds.min}, max={boardBounds.max}");
                
                // Warn if board is too large relative to classroom
                if (boardBounds.size.z > schema.classroom.depth * 0.5f)
                {
                    Debug.LogWarning($"[EnvironmentSetup] Board depth ({boardBounds.size.z}) is more than half of classroom depth ({schema.classroom.depth}). Board may intersect desks.");
                }
                
                // Warn if board intersects front wall (should be at front wall)
                float frontWallZ = -schema.classroom.depth / 2f;
                if (boardBounds.min.z < frontWallZ - 0.5f)
                {
                    Debug.LogWarning($"[EnvironmentSetup] Board extends behind front wall (front wall Z: {frontWallZ}, board min Z: {boardBounds.min.z})");
                }
            }
            
            // Apply board material if specified
            if (schema.environment != null && !string.IsNullOrEmpty(schema.environment.boardMaterial))
            {
                Material boardMaterial = assetMap.GetMaterial(schema.environment.boardMaterial);
                if (boardMaterial != null)
                {
                    ApplyMaterialToObject(boardObj, boardMaterial);
                }
            }
            
            Debug.Log($"[EnvironmentSetup] Board placed at {boardPosition}");
        }
        
        /// <summary>
        /// Adjust board position based on its bounds to ensure front surface is at desired position
        /// </summary>
        private static Vector3 AdjustBoardPositionForBounds(GameObject boardObj, Vector3 targetPosition, Vector3Data boardSize = null)
        {
            float boardDepth = 0f;
            string depthSource = "unknown";
            
            // First, use provided boardSize if available
            if (boardSize != null && boardSize.z > 0.01f)
            {
                boardDepth = boardSize.z;
                depthSource = "JSON boardSize";
                Debug.Log($"[EnvironmentSetup] Using board depth from JSON: {boardDepth}m");
            }
            else
            {
                // Try to get renderer bounds
                var renderer = boardObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Get bounds in world space (prefab may have existing rotation/scale)
                    Bounds bounds = renderer.bounds;
                    boardDepth = bounds.size.z;
                    depthSource = "renderer bounds";
                    Debug.Log($"[EnvironmentSetup] Board renderer bounds: size={bounds.size}, center={bounds.center}");
                }
                
                // If renderer bounds are too small (e.g., zero), check collider bounds
                if (boardDepth < 0.01f)
                {
                    var collider = boardObj.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Bounds bounds = collider.bounds;
                        boardDepth = bounds.size.z;
                        depthSource = "collider bounds";
                        Debug.Log($"[EnvironmentSetup] Board collider bounds: size={bounds.size}");
                    }
                }
            }
            
            // Determine offset
            Vector3 offset = Vector3.zero;
            
            if (boardDepth > 0.01f)
            {
                // Board faces classroom (rotation 180 around Y), so local forward is -world Z
                // To place front surface at targetPosition, move board forward by half depth
                // (assuming pivot is at center of board)
                offset = new Vector3(0, 0, -boardDepth / 2f);
                Debug.Log($"[EnvironmentSetup] Adjusting board position by {offset} (board depth: {boardDepth}m from {depthSource})");
            }
            else
            {
                // If no renderer or zero depth, use default offset for typical board
                // Typical board depth is ~0.1m (10cm)
                float defaultDepth = 0.1f;
                offset = new Vector3(0, 0, -defaultDepth / 2f);
                depthSource = "default";
                Debug.Log($"[EnvironmentSetup] Using default board offset {offset} (no board depth detected)");
            }
            
            // Log final position for debugging
            Vector3 finalPosition = targetPosition + offset;
            Debug.Log($"[EnvironmentSetup] Board position adjustment: {targetPosition} + {offset} = {finalPosition}");
            
            return finalPosition;
        }

        /// <summary>
        /// Create placeholder board if no prefab exists
        /// </summary>
        private static GameObject CreatePlaceholderBoard()
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Placeholder_Board";
            
            // Scale to typical board size
            board.transform.localScale = new Vector3(4f, 2f, 0.1f);
            
            // Create material
            Material whiteMaterial = new Material(Shader.Find("Standard"));
            whiteMaterial.color = Color.white;
            board.GetComponent<Renderer>().material = whiteMaterial;
            
            // Save as prefab for reuse
            string prefabPath = "Assets/Art/Prefabs/Placeholder_Board.prefab";
            EditorUtils.CreateFolderIfNotExists("Assets/Art/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(board, prefabPath);
            GameObject.DestroyImmediate(board);
            
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        
        /// <summary>
        /// Setup door for escape/return routes
        /// </summary>
        private static void SetupDoor(EnhancedLevelSchema schema, AssetMapConfig assetMap, GameObject classroomGroup)
        {
            // Get door prefab
            GameObject doorPrefab = assetMap.GetPrefab("DOOR");
            if (doorPrefab == null)
            {
                Debug.LogWarning("[EnvironmentSetup] No door prefab found. Creating marker only.");
                // We'll still create a marker for route waypoints
            }
            
            // Calculate door position
            Vector3 doorPosition = DeskGridGenerator.CalculateDoorPosition(schema);
            float frontWallZ = -schema.classroom.depth / 2f;
            float backWallZ = schema.classroom.depth / 2f;
            Debug.Log($"[EnvironmentSetup] Classroom dimensions: width={schema.classroom.width}, depth={schema.classroom.depth}");
            Debug.Log($"[EnvironmentSetup] Front wall Z={frontWallZ}, Back wall Z={backWallZ}, Door Z={doorPosition.z}");
            
            // Check which wall door is on
            if (Mathf.Abs(doorPosition.z - frontWallZ) < 0.1f)
                Debug.Log("[EnvironmentSetup] Door is on FRONT wall (same side as board)");
            else if (Mathf.Abs(doorPosition.z - backWallZ) < 0.1f)
                Debug.Log("[EnvironmentSetup] Door is on BACK wall (opposite board)");
            else
                Debug.Log($"[EnvironmentSetup] Door is at custom position (not on front/back wall)");
            
            // Calculate outside position for escape routes
            Vector3 outsidePosition = DeskGridGenerator.CalculateOutsidePosition(schema);
            Debug.Log($"[EnvironmentSetup] Outside position for escape routes: {outsidePosition}");
            
            // Validate door position relative to desk grid
            Vector2 deskBounds = DeskGridGenerator.CalculateDeskGridBounds(schema);
            float frontDeskZ = deskBounds.x; // Most forward (negative) desk position
            float backDeskZ = deskBounds.y;  // Most backward (positive) desk position
            float margin = 1.0f;
            
            if (doorPosition.z > frontDeskZ - margin && doorPosition.z < backDeskZ + margin)
            {
                Debug.LogWarning($"[EnvironmentSetup] Door at Z={doorPosition.z} is within desk grid bounds (front Z={frontDeskZ}, back Z={backDeskZ}). Door may intersect desks.");
            }
            else
            {
                Debug.Log($"[EnvironmentSetup] Door position OK: outside desk grid bounds (desk grid Z range: {frontDeskZ} to {backDeskZ})");
            }
            
            // Validate outside position relative to door
            float doorToOutsideZ = outsidePosition.z - doorPosition.z;
            float expectedDirection = Mathf.Sign(doorPosition.z); // Positive if door on back wall, negative if front wall
            if (Mathf.Sign(doorToOutsideZ) != Mathf.Sign(expectedDirection) && Mathf.Abs(doorToOutsideZ) > 0.1f)
            {
                Debug.LogWarning($"[EnvironmentSetup] Outside position at Z={outsidePosition.z} is on wrong side of door (door Z={doorPosition.z}). Expected outside to be further in direction {expectedDirection}.");
            }
            else
            {
                Debug.Log($"[EnvironmentSetup] Outside position OK: {Mathf.Abs(doorToOutsideZ)} units away from door in correct direction.");
            }
            
            // Create door group
            GameObject doorGroup = SceneSetupManager.CreateOrFindGameObject("Door", classroomGroup.transform);
            
            // Instantiate door if prefab exists
            if (doorPrefab != null)
            {
                GameObject doorObj = PrefabUtility.InstantiatePrefab(doorPrefab) as GameObject;
                doorObj.name = "ClassroomDoor";

                // Door dimensions - door is OPEN (90 degrees outward), hinge on left
                float doorWidth = 1.0f;
                float doorHeight = 2.5f;
                float gapWidth = 2.5f;

                // Position door with hinge at left edge of gap
                // When open 90 degrees, door extends along Z axis (not X)
                float doorX = doorPosition.x - (gapWidth / 2f);  // Hinge at gap left edge
                float doorZ = backWallZ + 0.1f + (doorWidth / 2f);  // Offset by half width since door extends along Z
                float doorY = 0f;  // Assume pivot at bottom, door touches floor

                doorObj.transform.position = new Vector3(doorX, doorY, doorZ);

                // Door rotation: open 90 degrees, hinge on left
                // When hinge is on left (negative X side), door swings outward along +Z
                // Rotation Y = -90: door opens 90 degrees, panel extends along +Z (outward)
                float doorRotationY = -90f;

                doorObj.transform.rotation = Quaternion.Euler(0, doorRotationY, 0);
                doorObj.transform.SetParent(doorGroup.transform);

                Debug.Log($"[EnvironmentSetup] Door OPEN at ({doorX}, {doorY}, {doorZ}), rotation Y={doorRotationY}");
            }

            // Always create door marker for navigation (center of gap, for navigation)
            GameObject doorMarker = new GameObject("DoorMarker");
            doorMarker.transform.position = doorPosition;
            doorMarker.transform.SetParent(doorGroup.transform);
            
            // Add collider for door area
            var collider = doorMarker.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.5f, 2f, 0.5f);
            collider.isTrigger = true;
            
            Debug.Log($"[EnvironmentSetup] Door setup at {doorPosition}");
        }
        
        /// <summary>
        /// Setup visual walls
        /// Back wall uses WallGenerator to create wall with door opening
        /// Front/side walls use cube primitives
        /// </summary>
        private static void SetupWalls(EnhancedLevelSchema schema, AssetMapConfig assetMap, GameObject classroomGroup)
        {
            // Get wall material
            Material wallMaterial = null;
            GameObject wallPrefab = assetMap.GetPrefab("WALL");
            if (wallPrefab != null)
            {
                var renderer = wallPrefab.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.sharedMaterials.Length > 0)
                {
                    wallMaterial = renderer.sharedMaterials[0];
                }
            }

            // Create walls group
            GameObject wallsGroup = SceneSetupManager.CreateOrFindGameObject("Walls", classroomGroup.transform);

            float halfWidth = schema.classroom.width / 2f;
            float halfDepth = schema.classroom.depth / 2f;
            float height = schema.classroom.height;
            float wallThickness = 0.2f;

            // Front wall - solid cube
            WallGenerator.CreateSolidWall("Wall_Front", 
                new Vector3(0, height/2, -halfDepth), 
                new Vector3(schema.classroom.width, height, wallThickness), 
                wallMaterial, wallsGroup.transform);

            // Back wall - wall with door opening (using WallGenerator)
            // IMPORTANT: Use DeskGridGenerator.CalculateDoorPosition() for consistent position
            float doorWidth = 2.5f;
            float doorHeight = 2.5f;
            Vector3 doorPosition = DeskGridGenerator.CalculateDoorPosition(schema);

            // Wall is at (0, 0, halfDepth) with thickness 0.2
            // Wall's back surface is at Z = halfDepth, front surface at Z = halfDepth - 0.2
            // Door should be at wall's front surface (Z = halfDepth - 0.2) to align with wall gap
            Vector3 wallGroupPosition = new Vector3(0, 0, halfDepth);
            float wallFrontZ = halfDepth - 0.2f;  // Front surface of wall

            // Convert door world X to local position within wall group
            // Door local X = door world X - wall group world X = doorPosition.x - 0 = doorPosition.x
            // Door local Z = wall front surface (no offset needed)
            Vector3 doorLocalPosition = new Vector3(doorPosition.x, 0f, 0f);

            WallGenerator.CreateWallWithDoor("Wall_Back",
                wallGroupPosition,
                schema.classroom.width, height, doorWidth, doorHeight, doorLocalPosition,
                wallMaterial, wallsGroup.transform);

            // Side walls - solid cubes
            WallGenerator.CreateSolidWall("Wall_Left", 
                new Vector3(-halfWidth, height/2, 0), 
                new Vector3(wallThickness, height, schema.classroom.depth), 
                wallMaterial, wallsGroup.transform);
            WallGenerator.CreateSolidWall("Wall_Right", 
                new Vector3(halfWidth, height/2, 0), 
                new Vector3(wallThickness, height, schema.classroom.depth), 
                wallMaterial, wallsGroup.transform);
            
            Debug.Log("[EnvironmentSetup] Walls created using WallGenerator");
        }
        
        /// <summary>
        /// Setup floor - extends to cover outside area for teacher escape
        /// </summary>
        private static void SetupFloor(EnhancedLevelSchema schema, AssetMapConfig assetMap, GameObject classroomGroup)
        {
            // Create floor group
            GameObject floorGroup = SceneSetupManager.CreateOrFindGameObject("Floor", classroomGroup.transform);
            
            // Extend floor to cover outside area (door at back wall)
            float outsideDepth = 3f;  // Extend 3 units outside classroom
            float totalDepth = schema.classroom.depth + outsideDepth;
            
            // Create floor plane - larger to cover classroom + outside
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ClassroomFloor";
            floor.transform.position = new Vector3(0, 0, outsideDepth / 2f);  // Shift forward to cover outside
            floor.transform.localScale = new Vector3(schema.classroom.width / 10f, 1, totalDepth / 10f);
            floor.transform.SetParent(floorGroup.transform);
            
            Debug.Log($"[EnvironmentSetup] Floor created: classroom depth={schema.classroom.depth}, outside={outsideDepth}, total depth={totalDepth}");
            
            // Apply floor material if specified
            if (schema.environment != null && !string.IsNullOrEmpty(schema.environment.floorMaterial))
            {
                Material floorMaterial = assetMap.GetMaterial(schema.environment.floorMaterial);
                if (floorMaterial != null)
                {
                    ApplyMaterialToObject(floor, floorMaterial);
                }
            }
            
            // Add NavMeshSurface component for navigation
            #if UNITY_AI_NAVIGATION
            var navMeshSurface = floor.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            navMeshSurface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
            #endif
            
            Debug.Log("[EnvironmentSetup] Floor created - extends to outside area");
        }
        
        /// <summary>
        /// Setup teacher area (teacher desk, etc.)
        /// </summary>
        private static void SetupTeacherArea(EnhancedLevelSchema schema, AssetMapConfig assetMap, GameObject classroomGroup)
        {
            // Create teacher area group
            GameObject teacherAreaGroup = SceneSetupManager.CreateOrFindGameObject("TeacherArea", classroomGroup.transform);
            
            // Position teacher area at front of classroom, opposite board
            Vector3 teacherPosition = new Vector3(
                schema.classroom.width / 3f, // Off to the side
                0,
                -schema.classroom.depth / 2f + 1f // Near front wall
            );
            
            // Create teacher desk if prefab exists
            GameObject teacherDeskPrefab = assetMap.GetPrefab("DESK") ?? assetMap.GetPrefab("CHAIR");
            if (teacherDeskPrefab != null)
            {
                GameObject teacherDesk = PrefabUtility.InstantiatePrefab(teacherDeskPrefab) as GameObject;
                teacherDesk.name = "TeacherDesk";
                teacherDesk.transform.position = teacherPosition;
                teacherDesk.transform.rotation = Quaternion.Euler(0, 180, 0); // Face classroom
                teacherDesk.transform.SetParent(teacherAreaGroup.transform);
            }
            
            Debug.Log("[EnvironmentSetup] Teacher area setup");
        }
        
        /// <summary>
        /// Apply material to GameObject and all child renderers
        /// </summary>
        private static void ApplyMaterialToObject(GameObject obj, Material material)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = material;
            }
        }
    }
}
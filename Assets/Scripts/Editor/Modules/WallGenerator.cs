using UnityEngine;
using UnityEditor;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Wall generator that creates walls with door openings using multiple colliders
    /// This avoids issues with prefab mesh/collider orientation mismatch
    /// </summary>
    public static class WallGenerator
    {
        /// <summary>
        /// Create a wall with door opening using primitive cubes with BoxColliders
        /// Door position specified in world coordinates
        /// </summary>
        public static GameObject CreateWallWithDoor(string name, Vector3 wallPosition, float wallWidth, float wallHeight, 
            float doorWidth, float doorHeight, Vector3 doorPosition, Material wallMaterial, Transform parent)
        {
            GameObject wallGroup = new GameObject(name);
            wallGroup.transform.position = wallPosition;
            wallGroup.transform.SetParent(parent);

            // Wall thickness
            float wallThickness = 0.2f;
            
            // Wall edges in world X
            float wallLeftX = wallPosition.x - wallWidth / 2f;
            float wallRightX = wallPosition.x + wallWidth / 2f;
            
            // Door edges in world X
            float doorLeftX = doorPosition.x - doorWidth / 2f;
            float doorRightX = doorPosition.x + doorWidth / 2f;
            
            // Left wall part: from wallLeftX to doorLeftX
            float leftWidth = doorLeftX - wallLeftX;
            Vector3 leftPos = new Vector3(
                wallLeftX + leftWidth / 2f,    // Center X
                wallHeight / 2f,               // Y = height/2 (pivot at center)
                0
            );
            
            // Right wall part: from doorRightX to wallRightX
            float rightWidth = wallRightX - doorRightX;
            Vector3 rightPos = new Vector3(
                doorRightX + rightWidth / 2f,  // Center X
                wallHeight / 2f,               // Y = height/2 (pivot at center)
                0
            );
            
            // Top wall part: above door
            // Top nằm ở đỉnh wall, không phải 50%
            float topHeight = 0.5f;  // Top height = 0.5 (50% của 3)
            float topY = wallHeight - topHeight / 2f;  // Y = 3 - 0.25 = 2.75 (75% chiều cao)
            
            Vector3 topPos = new Vector3(
                doorPosition.x,    // Center X = door center
                topY,              // Y = 2.75 (trên cùng)
                0
            );
            
            // Debug
            Debug.Log($"[WallGenerator] Wall: X=[{wallLeftX}, {wallRightX}], Door: X=[{doorLeftX}, {doorRightX}]");
            Debug.Log($"[WallGenerator] Left: width={leftWidth}, pos={leftPos}");
            Debug.Log($"[WallGenerator] Right: width={rightWidth}, pos={rightPos}");
            Debug.Log($"[WallGenerator] Top: height={topHeight}, pos={topPos}");
            
            // 1. Left part of wall
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Left";
            leftWall.transform.SetParent(wallGroup.transform);
            leftWall.transform.localPosition = leftPos;
            leftWall.transform.localScale = new Vector3(leftWidth, wallHeight, wallThickness);
            SetupWallMaterial(leftWall, wallMaterial);

            // 2. Right part of wall
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Right";
            rightWall.transform.SetParent(wallGroup.transform);
            rightWall.transform.localPosition = rightPos;
            rightWall.transform.localScale = new Vector3(rightWidth, wallHeight, wallThickness);
            SetupWallMaterial(rightWall, wallMaterial);

            // 3. Top part of wall (50% height)
            if (topHeight > 0.01f)
            {
                GameObject topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topWall.name = "Top";
                topWall.transform.SetParent(wallGroup.transform);
                topWall.transform.localPosition = topPos;
                topWall.transform.localScale = new Vector3(doorWidth, topHeight, wallThickness);
                SetupWallMaterial(topWall, wallMaterial);
            }

            Debug.Log($"[WallGenerator] Created wall with door: {name}");
            return wallGroup;
        }

        /// <summary>
        /// Create a solid wall using cube primitive
        /// </summary>
        public static GameObject CreateSolidWall(string name, Vector3 position, Vector3 scale, Material wallMaterial, Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.transform.SetParent(parent);
            SetupWallMaterial(wall, wallMaterial);
            
            return wall;
        }

        private static void SetupWallMaterial(GameObject wall, Material material)
        {
            if (material != null)
            {
                var renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = material;
                }
            }
        }
    }
}

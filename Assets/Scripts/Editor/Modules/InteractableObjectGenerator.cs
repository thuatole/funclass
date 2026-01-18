using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tự động tạo interactable objects cho level
    /// </summary>
    public static class InteractableObjectGenerator
    {
        public enum InteractableType
        {
            Book,
            Pencil,
            Ball,
            Phone,
            Bottle,
            Paper,
            Eraser,
            Ruler,
            Toy,
            Snack
        }

        /// <summary>
        /// Tạo interactable objects cho classroom
        /// </summary>
        public static List<GameObject> CreateInteractableObjects(int count, string levelName)
        {
            GameObject classroom = GameObject.Find("=== CLASSROOM ===");
            if (classroom == null)
            {
                Debug.LogWarning("[InteractableObjectGenerator] Classroom not found");
                return new List<GameObject>();
            }

            GameObject objectsGroup = EditorUtils.CreateChild(classroom, "InteractableObjects");
            List<GameObject> createdObjects = new List<GameObject>();

            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateRandomInteractableObject(i);
                obj.transform.SetParent(objectsGroup.transform);
                
                // Random position trong classroom
                obj.transform.position = GetRandomPositionInClassroom();
                
                createdObjects.Add(obj);
            }

            Debug.Log($"[InteractableObjectGenerator] Created {count} interactable objects");
            return createdObjects;
        }

        /// <summary>
        /// Tạo interactable object theo type
        /// </summary>
        public static GameObject CreateInteractableObject(InteractableType type, Vector3 position)
        {
            GameObject obj = null;
            string objectName = type.ToString();

            switch (type)
            {
                case InteractableType.Book:
                    obj = CreateBook();
                    break;
                case InteractableType.Pencil:
                    obj = CreatePencil();
                    break;
                case InteractableType.Ball:
                    obj = CreateBall();
                    break;
                case InteractableType.Phone:
                    obj = CreatePhone();
                    break;
                case InteractableType.Bottle:
                    obj = CreateBottle();
                    break;
                case InteractableType.Paper:
                    obj = CreatePaper();
                    break;
                case InteractableType.Eraser:
                    obj = CreateEraser();
                    break;
                case InteractableType.Ruler:
                    obj = CreateRuler();
                    break;
                case InteractableType.Toy:
                    obj = CreateToy();
                    break;
                case InteractableType.Snack:
                    obj = CreateSnack();
                    break;
            }

            if (obj != null)
            {
                obj.name = objectName;
                obj.transform.position = position;
                
                // Add StudentInteractableObject component
                var interactable = obj.AddComponent<FunClass.Core.StudentInteractableObject>();
                ConfigureInteractable(interactable, type);
            }

            return obj;
        }

        /// <summary>
        /// Tạo set interactable objects theo difficulty
        /// </summary>
        public static List<GameObject> CreateInteractableSetByDifficulty(LevelConfigGenerator.Difficulty difficulty)
        {
            int count = difficulty switch
            {
                LevelConfigGenerator.Difficulty.Easy => 3,
                LevelConfigGenerator.Difficulty.Normal => 5,
                LevelConfigGenerator.Difficulty.Hard => 8,
                _ => 5
            };

            return CreateInteractableObjects(count, "");
        }

        private static GameObject CreateRandomInteractableObject(int index)
        {
            InteractableType[] types = (InteractableType[])System.Enum.GetValues(typeof(InteractableType));
            InteractableType randomType = types[Random.Range(0, types.Length)];
            
            return CreateInteractableObject(randomType, Vector3.zero);
        }

        private static GameObject CreateBook()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(0.15f, 0.02f, 0.2f);
            SetColor(obj, new Color(0.8f, 0.2f, 0.2f)); // Red
            return obj;
        }

        private static GameObject CreatePencil()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.localScale = new Vector3(0.01f, 0.1f, 0.01f);
            SetColor(obj, new Color(1f, 0.8f, 0.2f)); // Yellow
            return obj;
        }

        private static GameObject CreateBall()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.localScale = Vector3.one * 0.15f;
            SetColor(obj, new Color(0.2f, 0.6f, 1f)); // Blue
            return obj;
        }

        private static GameObject CreatePhone()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(0.08f, 0.01f, 0.15f);
            SetColor(obj, new Color(0.1f, 0.1f, 0.1f)); // Black
            return obj;
        }

        private static GameObject CreateBottle()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            SetColor(obj, new Color(0.2f, 0.8f, 0.3f)); // Green
            return obj;
        }

        private static GameObject CreatePaper()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(0.2f, 0.001f, 0.25f);
            SetColor(obj, new Color(0.95f, 0.95f, 0.95f)); // White
            return obj;
        }

        private static GameObject CreateEraser()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(0.05f, 0.02f, 0.03f);
            SetColor(obj, new Color(0.9f, 0.5f, 0.7f)); // Pink
            return obj;
        }

        private static GameObject CreateRuler()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(0.3f, 0.005f, 0.03f);
            SetColor(obj, new Color(0.8f, 0.8f, 0.2f)); // Yellow
            return obj;
        }

        private static GameObject CreateToy()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = Vector3.one * 0.1f;
            SetColor(obj, Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            return obj;
        }

        private static GameObject CreateSnack()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(0.08f, 0.08f, 0.12f);
            SetColor(obj, new Color(0.9f, 0.7f, 0.3f)); // Orange
            return obj;
        }

        private static void SetColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }
        }

        private static void ConfigureInteractable(FunClass.Core.StudentInteractableObject interactable, InteractableType type)
        {
            // Configure based on type
            switch (type)
            {
                case InteractableType.Book:
                case InteractableType.Paper:
                    // Can touch, make noise
                    break;
                    
                case InteractableType.Ball:
                case InteractableType.Toy:
                    // Can throw, knock over
                    break;
                    
                case InteractableType.Phone:
                    // Can touch, make noise (vibrate)
                    break;
                    
                case InteractableType.Bottle:
                    // Can knock over, drop
                    break;
                    
                case InteractableType.Pencil:
                case InteractableType.Eraser:
                case InteractableType.Ruler:
                    // Can drop, throw
                    break;
                    
                case InteractableType.Snack:
                    // Can touch, make noise (eating)
                    break;
            }
        }

        private static Vector3 GetRandomPositionInClassroom()
        {
            // Random position in classroom area
            float x = Random.Range(-4f, 4f);
            float z = Random.Range(-4f, 4f);
            return new Vector3(x, 0.5f, z); // 0.5f height (on desk/floor)
        }

        /// <summary>
        /// Quick create common classroom objects
        /// </summary>
        [MenuItem("Tools/FunClass/Quick Create/Classroom Objects")]
        public static void QuickCreateClassroomObjects()
        {
            CreateInteractableObjects(5, "Classroom");
            EditorUtility.DisplayDialog("Success", "Created 5 classroom objects", "OK");
        }
    }
}

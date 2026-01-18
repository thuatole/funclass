using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tự động tạo mess prefabs (vomit, spill, etc.)
    /// </summary>
    public static class MessPrefabGenerator
    {
        public enum MessType
        {
            Vomit,
            Spill,
            Trash,
            Stain,
            BrokenGlass,
            TornPaper
        }

        /// <summary>
        /// Tạo mess prefabs cho level
        /// </summary>
        public static List<GameObject> CreateMessPrefabs(string levelName)
        {
            List<GameObject> prefabs = new List<GameObject>();

            // Create folder
            string folderPath = $"Assets/Prefabs/{levelName}/Mess";
            EditorUtils.CreateFolderIfNotExists(folderPath);

            // Create each mess type
            foreach (MessType type in System.Enum.GetValues(typeof(MessType)))
            {
                GameObject prefab = CreateMessPrefab(type, folderPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MessPrefabGenerator] Created {prefabs.Count} mess prefabs in {folderPath}");
            return prefabs;
        }

        /// <summary>
        /// Tạo một mess prefab theo type
        /// </summary>
        public static GameObject CreateMessPrefab(MessType type, string savePath)
        {
            GameObject messObj = new GameObject($"{type}Mess");
            
            // Add MessObject component based on type
            switch (type)
            {
                case MessType.Vomit:
                    CreateVomitMess(messObj);
                    break;
                case MessType.Spill:
                    CreateSpillMess(messObj);
                    break;
                case MessType.Trash:
                    CreateTrashMess(messObj);
                    break;
                case MessType.Stain:
                    CreateStainMess(messObj);
                    break;
                case MessType.BrokenGlass:
                    CreateBrokenGlassMess(messObj);
                    break;
                case MessType.TornPaper:
                    CreateTornPaperMess(messObj);
                    break;
            }

            // Save as prefab
            string prefabPath = $"{savePath}/{type}Mess.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(messObj, prefabPath);
            
            // Clean up scene object
            Object.DestroyImmediate(messObj);

            return prefab;
        }

        /// <summary>
        /// Tạo vomit mess visual
        /// </summary>
        private static void CreateVomitMess(GameObject parent)
        {
            // Visual puddle
            GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "Visual";
            puddle.transform.SetParent(parent.transform);
            puddle.transform.localPosition = Vector3.zero;
            puddle.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);

            // Set color
            var renderer = puddle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.6f, 0.5f, 0.2f, 0.8f); // Brownish
                renderer.material = mat;
            }

            // Add collider
            var collider = parent.AddComponent<SphereCollider>();
            collider.radius = 0.3f;
            collider.center = new Vector3(0, 0.05f, 0);

            // Add VomitMess component
            var vomitMess = parent.AddComponent<FunClass.Core.VomitMess>();
        }

        /// <summary>
        /// Tạo spill mess visual
        /// </summary>
        private static void CreateSpillMess(GameObject parent)
        {
            GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "Visual";
            puddle.transform.SetParent(parent.transform);
            puddle.transform.localPosition = Vector3.zero;
            puddle.transform.localScale = new Vector3(0.4f, 0.005f, 0.4f);

            var renderer = puddle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.5f, 0.8f, 0.7f); // Blueish (water)
                renderer.material = mat;
            }

            var collider = parent.AddComponent<SphereCollider>();
            collider.radius = 0.25f;

            var messPrefab = parent.AddComponent<FunClass.Core.VomitMess>();
            messPrefab.messName = "Vomit";
            messPrefab.severityLevel = 7;
            messPrefab.disruptionAmount = 15f;
        }

        /// <summary>
        /// Tạo trash mess visual
        /// </summary>
        private static void CreateTrashMess(GameObject parent)
        {
            // Multiple small cubes for trash pieces
            for (int i = 0; i < 5; i++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = $"TrashPiece_{i}";
                piece.transform.SetParent(parent.transform);
                piece.transform.localPosition = new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    0.02f,
                    Random.Range(-0.2f, 0.2f)
                );
                piece.transform.localScale = Vector3.one * Random.Range(0.03f, 0.08f);
                piece.transform.rotation = Random.rotation;

                var renderer = piece.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = Random.ColorHSV(0f, 1f, 0.3f, 0.7f, 0.3f, 0.7f);
                    renderer.material = mat;
                }
            }

            var collider = parent.AddComponent<SphereCollider>();
            collider.radius = 0.3f;

            var messPrefab = parent.AddComponent<FunClass.Core.VomitMess>();
            messPrefab.messName = "Spill";
            messPrefab.severityLevel = 5;
            messPrefab.disruptionAmount = 10f;
        }

        /// <summary>
        /// Tạo stain mess visual
        /// </summary>
        private static void CreateStainMess(GameObject parent)
        {
            GameObject stain = GameObject.CreatePrimitive(PrimitiveType.Plane);
            stain.name = "Visual";
            stain.transform.SetParent(parent.transform);
            stain.transform.localPosition = new Vector3(0, 0.001f, 0);
            stain.transform.localScale = Vector3.one * 0.15f;

            var renderer = stain.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.4f, 0.3f, 0.2f, 0.6f); // Brown stain
                renderer.material = mat;
            }

            var collider = parent.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.3f, 0.05f, 0.3f);

            var messPrefab = parent.AddComponent<FunClass.Core.VomitMess>();
            messPrefab.messName = "Trash";
            messPrefab.severityLevel = 4;
            messPrefab.disruptionAmount = 8f;
        }

        /// <summary>
        /// Tạo broken glass mess visual
        /// </summary>
        private static void CreateBrokenGlassMess(GameObject parent)
        {
            // Sharp pieces
            for (int i = 0; i < 8; i++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = $"GlassShard_{i}";
                piece.transform.SetParent(parent.transform);
                piece.transform.localPosition = new Vector3(
                    Random.Range(-0.25f, 0.25f),
                    0.01f,
                    Random.Range(-0.25f, 0.25f)
                );
                piece.transform.localScale = new Vector3(
                    Random.Range(0.02f, 0.05f),
                    0.005f,
                    Random.Range(0.05f, 0.1f)
                );
                piece.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                var renderer = piece.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.9f, 0.9f, 0.95f, 0.8f); // Clear glass
                    renderer.material = mat;
                }
            }

            var collider = parent.AddComponent<SphereCollider>();
            collider.radius = 0.35f;

            var messPrefab = parent.AddComponent<FunClass.Core.VomitMess>();
            messPrefab.messName = "Stain";
            messPrefab.severityLevel = 6;
            messPrefab.disruptionAmount = 12f;
        }

        /// <summary>
        /// Tạo torn paper mess visual
        /// </summary>
        private static void CreateTornPaperMess(GameObject parent)
        {
            // Paper pieces
            for (int i = 0; i < 6; i++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = $"PaperPiece_{i}";
                piece.transform.SetParent(parent.transform);
                piece.transform.localPosition = new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    0.005f,
                    Random.Range(-0.2f, 0.2f)
                );
                piece.transform.localScale = new Vector3(
                    Random.Range(0.05f, 0.1f),
                    0.001f,
                    Random.Range(0.08f, 0.15f)
                );
                piece.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), Random.Range(-10f, 10f));

                var renderer = piece.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.95f, 0.95f, 0.9f); // White paper
                    renderer.material = mat;
                }
            }

            var collider = parent.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.4f, 0.05f, 0.4f);

            var messPrefab = parent.AddComponent<FunClass.Core.VomitMess>();
            messPrefab.messName = "Torn Paper";
            messPrefab.severityLevel = 3;
            messPrefab.disruptionAmount = 6f;
        }

        /// <summary>
        /// Quick create all mess prefabs
        /// </summary>
        [MenuItem("Tools/FunClass/Quick Create/Mess Prefabs")]
        public static void QuickCreateMessPrefabs()
        {
            string folderPath = "Assets/Prefabs/Mess";
            EditorUtils.CreateFolderIfNotExists(folderPath);

            var prefabs = new List<GameObject>();
            foreach (MessType type in System.Enum.GetValues(typeof(MessType)))
            {
                GameObject prefab = CreateMessPrefab(type, folderPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }

            EditorUtility.DisplayDialog("Success", 
                $"Created {prefabs.Count} mess prefabs in {folderPath}", 
                "OK");
        }

        /// <summary>
        /// Get mess prefab path
        /// </summary>
        public static string GetMessPrefabPath(MessType type, string levelName = "")
        {
            if (string.IsNullOrEmpty(levelName))
            {
                return $"Assets/Prefabs/Mess/{type}Mess.prefab";
            }
            return $"Assets/Prefabs/{levelName}/Mess/{type}Mess.prefab";
        }
    }
}

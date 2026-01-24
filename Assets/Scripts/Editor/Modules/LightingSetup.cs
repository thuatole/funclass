using UnityEngine;
using FunClass.Editor.Data;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Sets up basic lighting for the classroom
    /// </summary>
    public static class LightingSetup
    {
        /// <summary>
        /// Setup basic lighting for the classroom
        /// </summary>
        public static void SetupLighting(EnvironmentSettingsData environmentSettings)
        {
            if (environmentSettings != null && !environmentSettings.autoSetupLighting)
            {
                Debug.Log("[LightingSetup] Auto lighting setup disabled");
                return;
            }
            
            Debug.Log("[LightingSetup] Setting up classroom lighting");
            
            // Create lighting group
            GameObject lightingGroup = SceneSetupManager.CreateOrFindGameObject("Lighting");
            
            // 1. Setup directional light (main sun/moon)
            SetupDirectionalLight(lightingGroup.transform);
            
            // 2. Setup ambient lighting
            SetupAmbientLighting(environmentSettings);
            
            // 3. Setup classroom ceiling lights
            SetupClassroomLights(lightingGroup.transform);
            
            Debug.Log("[LightingSetup] Lighting setup complete");
        }
        
        /// <summary>
        /// Setup directional light (sun/moon)
        /// </summary>
        private static void SetupDirectionalLight(Transform parent)
        {
            // Find existing directional light
            Light directionalLight = Object.FindObjectOfType<Light>();
            if (directionalLight != null && directionalLight.type == LightType.Directional)
            {
                // Use existing
                directionalLight.transform.SetParent(parent);
            }
            else
            {
                // Create new directional light
                GameObject lightObj = new GameObject("Directional Light");
                lightObj.transform.SetParent(parent);
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
                
                directionalLight = lightObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }
            
            // Configure directional light
            directionalLight.color = new Color(1f, 0.95f, 0.9f); // Warm white
            directionalLight.intensity = 1.0f;
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.shadowStrength = 0.8f;
            
            Debug.Log("[LightingSetup] Directional light configured");
        }
        
        /// <summary>
        /// Setup ambient lighting (skybox/ambient light)
        /// </summary>
        private static void SetupAmbientLighting(EnvironmentSettingsData environmentSettings)
        {
            // Set ambient mode to Trilight for better control
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            
            // Set ambient colors
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.5f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.3f, 0.35f);
            
            // Set ambient intensity
            if (environmentSettings != null)
            {
                RenderSettings.ambientIntensity = environmentSettings.ambientIntensity;
            }
            else
            {
                RenderSettings.ambientIntensity = 1.0f;
            }
            
            // Disable fog for clean classroom look
            RenderSettings.fog = false;
            
            Debug.Log("[LightingSetup] Ambient lighting configured");
        }
        
        /// <summary>
        /// Setup classroom ceiling lights
        /// </summary>
        private static void SetupClassroomLights(Transform parent)
        {
            // Create classroom lights group
            GameObject classroomLightsGroup = SceneSetupManager.CreateOrFindGameObject("Classroom Lights", parent);
            
            // Calculate classroom size (default if not available)
            float classroomWidth = 10f;
            float classroomDepth = 8f;
            float ceilingHeight = 3f;
            
            // Create 4 ceiling lights in a grid
            int rows = 2;
            int cols = 2;
            
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    // Calculate position
                    float x = (col + 0.5f) * (classroomWidth / cols) - classroomWidth / 2f;
                    float z = (row + 0.5f) * (classroomDepth / rows) - classroomDepth / 2f;
                    Vector3 position = new Vector3(x, ceilingHeight - 0.2f, z);
                    
                    // Create light
                    CreateCeilingLight($"ClassroomLight_{row}_{col}", position, classroomLightsGroup.transform);
                }
            }
            
            Debug.Log($"[LightingSetup] Created {rows * cols} classroom ceiling lights");
        }
        
        /// <summary>
        /// Create a single ceiling light
        /// </summary>
        private static void CreateCeilingLight(string name, Vector3 position, Transform parent)
        {
            GameObject lightObj = new GameObject(name);
            lightObj.transform.position = position;
            lightObj.transform.SetParent(parent);
            
            // Add point light component
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.95f, 0.9f); // Warm white
            light.intensity = 2.0f;
            light.range = 5f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.5f;
            
            // Add visual representation (optional)
            #if UNITY_EDITOR
            GameObject lightVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lightVisual.name = "LightVisual";
            lightVisual.transform.SetParent(lightObj.transform);
            lightVisual.transform.localPosition = Vector3.zero;
            lightVisual.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            
            // Make visual semi-transparent
            Renderer renderer = lightVisual.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 1f, 0.8f, 0.3f);
            renderer.material = mat;
            #endif
        }
        
        /// <summary>
        /// Setup camera for gameplay (first-person teacher camera)
        /// </summary>
        public static void SetupGameplayCamera()
        {
            // Find existing camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindObjectOfType<Camera>();
            }
            
            if (mainCamera == null)
            {
                // Create new camera
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
                
                // Tag as MainCamera
                cameraObj.tag = "MainCamera";
            }
            
            // Position camera at teacher area (back of classroom)
            mainCamera.transform.position = new Vector3(0, 1.6f, 3f); // Eye height, back of room
            mainCamera.transform.rotation = Quaternion.Euler(0, 180, 0); // Face front of classroom
            
            // Configure camera
            mainCamera.fieldOfView = 60f;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            
            Debug.Log("[LightingSetup] Gameplay camera configured");
        }
    }
}
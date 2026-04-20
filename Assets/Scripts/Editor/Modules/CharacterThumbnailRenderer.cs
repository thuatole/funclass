using UnityEngine;
using UnityEditor;
using System.IO;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Pre-renders character FBX thumbnails at import time and saves them as sprites to
    /// Assets/Resources/StudentAvatars/{studentId}.png so StudentIntroScreen.LoadStudentAvatar
    /// can find them via Resources.Load at runtime.
    /// </summary>
    public static class CharacterThumbnailRenderer
    {
        const int TexSize      = 256;
        const string OutputDir = "Assets/Resources/StudentAvatars";

        /// <summary>
        /// Render a thumbnail for the given student and save it as a sprite asset.
        /// Safe to call multiple times – skips if thumbnail already exists and forceRegen is false.
        /// </summary>
        public static void RenderAndSave(string studentId, string characterModel, bool forceRegen = false)
        {
            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(characterModel)) return;

            // Ensure output directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(OutputDir))
                AssetDatabase.CreateFolder("Assets/Resources", "StudentAvatars");

            string pngPath = $"{OutputDir}/{studentId}.png";

            if (!forceRegen && File.Exists(pngPath))
            {
                Debug.Log($"[CharacterThumbnailRenderer] Thumbnail already exists for {studentId}, skipping");
                return;
            }

            // Load FBX
            string fbxPath = $"Assets/Characters/{characterModel}.fbx";
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                Debug.LogWarning($"[CharacterThumbnailRenderer] FBX not found at {fbxPath}, skipping thumbnail for {studentId}");
                return;
            }

            // --- Setup offscreen scene objects ---
            // Place far offscreen so nothing in the real scene is captured
            Vector3 offset = new Vector3(0, -5000f, 0);

            GameObject model = Object.Instantiate(fbxAsset);
            model.name = "__ThumbModel__";
            model.transform.position = offset;
            model.transform.rotation = Quaternion.Euler(0, 180f, 0); // face toward camera (-Z)

            // Approximate bounds to frame the character
            Bounds bounds = GetRendererBounds(model);
            float modelHeight = bounds.size.y > 0 ? bounds.size.y : 1.8f;
            float modelWidth  = Mathf.Max(bounds.size.x, bounds.size.z);

            // Camera position: in front of model (+Z side), slightly above center
            float camDist = Mathf.Max(modelHeight, modelWidth) * 1.1f;
            Vector3 camPos = offset + new Vector3(0, modelHeight * 0.55f, camDist);

            GameObject camGo = new GameObject("__ThumbCam__");
            Camera cam = camGo.AddComponent<Camera>();
            cam.transform.position = camPos;
            cam.transform.LookAt(offset + new Vector3(0, modelHeight * 0.55f, 0));
            cam.orthographic        = true;
            cam.orthographicSize    = modelHeight * 0.6f;
            cam.clearFlags          = CameraClearFlags.SolidColor;
            cam.backgroundColor     = new Color(0.15f, 0.15f, 0.18f, 1f); // dark neutral bg
            cam.nearClipPlane       = 0.01f;
            cam.farClipPlane        = camDist * 3f;
            cam.cullingMask         = ~0; // render everything (offscreen layer is fine)

            // Lighting: a directional light aimed at the model
            GameObject lightGo = new GameObject("__ThumbLight__");
            Light light = lightGo.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            light.color     = Color.white;
            lightGo.transform.position = camPos + new Vector3(-1f, 1f, 0);
            lightGo.transform.LookAt(offset + new Vector3(0, modelHeight * 0.5f, 0));

            // --- Render ---
            RenderTexture rt = new RenderTexture(TexSize, TexSize, 16, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, TexSize, TexSize), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // --- Cleanup ---
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(model);
            Object.DestroyImmediate(rt);

            // --- Save PNG ---
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(pngPath, png);

            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            // Configure as Sprite
            TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType        = TextureImporterType.Sprite;
                importer.spriteImportMode   = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled      = false;
                importer.maxTextureSize     = TexSize;
                importer.SaveAndReimport();
            }

            Debug.Log($"[CharacterThumbnailRenderer] Saved thumbnail for '{studentId}' → {pngPath}");
        }

        static Bounds GetRendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 1.8f);

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}

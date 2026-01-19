using UnityEngine;

namespace FunClass.Core.UI
{
    public class StudentHighlight : MonoBehaviour
    {
        [Header("Highlight Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.3f);
        [SerializeField] private float outlineWidth = 0.1f;
        
        private Renderer[] renderers;
        private Material[] originalMaterials;
        private Material[] highlightMaterials;
        private bool isHighlighted = false;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                originalMaterials = new Material[renderers.Length];
                highlightMaterials = new Material[renderers.Length];
                
                for (int i = 0; i < renderers.Length; i++)
                {
                    originalMaterials[i] = renderers[i].material;
                    
                    highlightMaterials[i] = new Material(renderers[i].material);
                    highlightMaterials[i].color = highlightColor;
                    highlightMaterials[i].SetFloat("_Mode", 3);
                    highlightMaterials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    highlightMaterials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    highlightMaterials[i].SetInt("_ZWrite", 0);
                    highlightMaterials[i].DisableKeyword("_ALPHATEST_ON");
                    highlightMaterials[i].EnableKeyword("_ALPHABLEND_ON");
                    highlightMaterials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    highlightMaterials[i].renderQueue = 3000;
                }
            }
        }

        public void SetHighlight(bool highlight)
        {
            if (isHighlighted == highlight) return;

            Debug.Log($"[StudentHighlight] {gameObject.name}: SetHighlight({highlight})");

            isHighlighted = highlight;

            if (renderers != null && renderers.Length > 0)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        if (highlight)
                        {
                            // Use emission or additive blending instead of color multiplication
                            // This creates a yellow/white highlight glow without changing base color
                            Material mat = renderers[i].material;

                            // Store original color
                            mat.SetColor("_EmissionColor", highlightColor * 2f);
                            mat.EnableKeyword("_EMISSION");

                            // Optional: Also brighten the base color slightly
                            Color baseColor = originalMaterials[i].color;
                            Color highlightedColor = Color.Lerp(baseColor, Color.yellow, 0.3f);
                            mat.color = highlightedColor;

                            Debug.Log($"[StudentHighlight] {gameObject.name}: Highlighted renderer {i}, enabled={renderers[i].enabled}");
                        }
                        else
                        {
                            // Restore original
                            Material mat = renderers[i].material;
                            mat.SetColor("_EmissionColor", Color.black);
                            mat.DisableKeyword("_EMISSION");
                            mat.color = originalMaterials[i].color;

                            Debug.Log($"[StudentHighlight] {gameObject.name}: Restored renderer {i}, enabled={renderers[i].enabled}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[StudentHighlight] {gameObject.name}: Renderer {i} is NULL!");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (highlightMaterials != null)
            {
                foreach (var mat in highlightMaterials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
            }
        }
    }
}

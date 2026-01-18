using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Vomit mess created when a student vomits
    /// </summary>
    public class VomitMess : MessObject
    {
        [Header("Vomit Specific Settings")]
        [Tooltip("Prefab for the vomit puddle visual")]
        public GameObject vomitPuddlePrefab;
        
        [Tooltip("Scale of the vomit puddle")]
        public Vector3 puddleScale = Vector3.one;

        protected override void Awake()
        {
            base.Awake();
            
            // Set default values for vomit
            messName = "vomit";
            severityLevel = 7;
            disruptionAmount = 12f;
            cleanupDisruptionReduction = 15f;
            cleanupScore = 15;
            cleanupTime = 0f; // Instant cleanup for now
        }

        public override void Initialize(StudentAgent student)
        {
            base.Initialize(student);
            
            // Create visual puddle if prefab is assigned
            if (vomitPuddlePrefab != null)
            {
                messVisual = Instantiate(vomitPuddlePrefab, transform.position, Quaternion.identity, transform);
                messVisual.transform.localScale = puddleScale;
            }
            else
            {
                // Create default visual (yellow sphere) if no prefab
                GameObject defaultVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                defaultVisual.name = "VomitVisual";
                defaultVisual.transform.SetParent(transform);
                defaultVisual.transform.localPosition = Vector3.zero;
                defaultVisual.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f); // Flat puddle shape
                
                // Make it yellow/green for vomit
                Renderer renderer = defaultVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.8f, 0.9f, 0.3f); // Yellow-green
                }
                
                // Remove collider from visual (parent already has collider)
                Collider visualCollider = defaultVisual.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    Object.Destroy(visualCollider);
                }
                
                messVisual = defaultVisual;
                Debug.Log("[VomitMess] Created default visual (no prefab assigned)");
            }
        }

        protected override void OnCleanupComplete()
        {
            base.OnCleanupComplete();
            
            Debug.Log($"[VomitMess] Vomit puddle cleaned up at {transform.position}");
            Debug.Log($"[VomitMess] Influence source removed - students will no longer be affected by this mess");
        }

        /// <summary>
        /// Static factory method to create a vomit mess at a specific location
        /// </summary>
        public static VomitMess Create(Vector3 position, StudentAgent creator, GameObject puddlePrefab = null)
        {
            GameObject messObject = new GameObject("VomitMess");
            messObject.transform.position = position;
            
            VomitMess vomitMess = messObject.AddComponent<VomitMess>();
            vomitMess.vomitPuddlePrefab = puddlePrefab;
            
            // Add collider for interaction
            SphereCollider collider = messObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
            collider.isTrigger = false;
            
            // Set layer for teacher interaction
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer != -1)
            {
                messObject.layer = interactableLayer;
            }
            else
            {
                // Fallback if Interactable layer doesn't exist
                messObject.layer = 0; // Default layer
                Debug.LogWarning("[VomitMess] 'Interactable' layer not found, using Default layer");
            }
            
            vomitMess.Initialize(creator);
            
            return vomitMess;
        }
    }
}

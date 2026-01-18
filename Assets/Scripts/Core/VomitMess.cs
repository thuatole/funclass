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
        }

        protected override void OnCleanupComplete()
        {
            base.OnCleanupComplete();
            
            // Optional: Play cleanup effect/sound
            Debug.Log($"[VomitMess] Vomit puddle cleaned up at {transform.position}");
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
            messObject.layer = LayerMask.NameToLayer("Interactable");
            if (messObject.layer == -1)
            {
                // Fallback if Interactable layer doesn't exist
                messObject.layer = 0; // Default layer
            }
            
            vomitMess.Initialize(creator);
            
            return vomitMess;
        }
    }
}

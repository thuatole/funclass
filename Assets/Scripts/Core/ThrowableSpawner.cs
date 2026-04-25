using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    /// <summary>
    /// Spawns throwable and knockable objects on desks and around classroom at level start.
    /// Reads DeskLoadoutConfig and ClassroomPropsConfig from LevelConfig.
    /// </summary>
    public class ThrowableSpawner : MonoBehaviour
    {
        public static ThrowableSpawner Instance { get; private set; }

        private const string THROWABLES_SMALL_PATH = "Throwables/Small/";
        private const string THROWABLES_LARGE_PATH = "Throwables/Large/";

        // objectType → prefab id prefix mapping (matches ThrowablePrefabGenerator ids)
        private static readonly Dictionary<string, string[]> SmallTypeToIds = new Dictionary<string, string[]>
        {
            { "Book",  new[] { "book1","book2","book3","book4","book5","book6","book7","book8",
                               "book9","book10","book11","book12","book13","book14","book15","book16" } },
            { "Sheet", new[] { "sheet","sheet1","sheet2","sheet3","sheet4" } },
            { "Tray",  new[] { "tray" } },
        };

        private static readonly Dictionary<string, string[]> LargeTypeToIds = new Dictionary<string, string[]>
        {
            { "Laptop",    new[] { "laptop" } },
            { "Computer",  new[] { "computer","computer1","computer2","computer3" } },
            { "Speaker",   new[] { "speaker" } },
            { "Projector", new[] { "projector" } },
            { "Chair",     new[] { "chair","chair1" } },
        };

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        // Cached prefabs to avoid repeated Resources.Load
        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        // Cache nearest desk per student to avoid repeated lookup during spawn loop
        private readonly Dictionary<StudentAgent, Transform> studentDeskCache = new Dictionary<StudentAgent, Transform>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private LevelConfig currentLevelConfig;

        public void SetCurrentLevelConfig(LevelConfig levelConfig)
        {
            currentLevelConfig = levelConfig;
        }

        /// <summary>
        /// On-demand spawn: instantiate a throwable at the target student.
        /// Used when scripted/autonomous interaction fires — object appears at target only.
        /// Returns the spawned GameObject (with StudentInteractableObject component) for event linkage.
        /// </summary>
        public GameObject SpawnAtTarget(StudentAgent target, SizeCategory size = SizeCategory.Small)
        {
            if (target == null) return null;

            // Pick objectType from level config pool (fallback to Book/Laptop)
            string objectType = PickObjectTypeFromConfig(size);
            GameObject prefab = LoadPrefab(objectType, isLarge: size == SizeCategory.Large, randomize: true);
            if (prefab == null)
            {
                GameLogger.Warning("ThrowableSpawner",
                    $"SpawnAtTarget: no prefab for type '{objectType}' (size={size})");
                return null;
            }

            GameObject obj = Instantiate(prefab);
            obj.name = $"Throwable_{objectType}_{target.Config?.studentName}_dynamic";
            spawnedObjects.Add(obj);

            var interactable = obj.GetComponent<StudentInteractableObject>();
            interactable?.AttachToTarget(target);

            // Visual feedback at target's feet
            MessType messType = size == SizeCategory.Small ? MessType.TornPaper : MessType.BrokenGlass;
            MessSpawner.Instance?.SpawnAt(target.transform.position, messType);

            GameLogger.Detail("ThrowableSpawner",
                $"On-demand spawn {objectType} ({size}) at target {target.Config?.studentName}");

            return obj;
        }

        private string PickObjectTypeFromConfig(SizeCategory size)
        {
            DeskLoadoutConfig loadout = currentLevelConfig?.deskLoadout;
            if (size == SizeCategory.Small)
            {
                List<string> pool = loadout?.smallObjectPool;
                if (pool != null && pool.Count > 0)
                    return pool[Random.Range(0, pool.Count)];
                return "Book";
            }
            else
            {
                List<string> pool = loadout?.largeObjectPool;
                if (pool != null && pool.Count > 0)
                    return pool[Random.Range(0, pool.Count)];
                return "Laptop";
            }
        }

        public void DespawnAll()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            spawnedObjects.Clear();
            studentDeskCache.Clear();
            StudentInteractableObject.ClearDeskStackCounts();
            GameLogger.Detail("ThrowableSpawner", "All throwables despawned");
        }

        /// <summary>
        /// Find the nearest desk gameobject for a student. Cached after first lookup.
        /// Returns null if no Desks group found in scene.
        /// </summary>
        public Transform GetDeskForStudent(StudentAgent student)
        {
            if (student == null) return null;
            if (studentDeskCache.TryGetValue(student, out Transform cached) && cached != null)
                return cached;

            Transform desksGroup = GameObject.Find("Desks")?.transform;
            if (desksGroup == null) return null;

            Transform nearest = null;
            float minSqr = float.MaxValue;
            Vector3 seatPos = student.OriginalSeatPosition;
            foreach (Transform desk in desksGroup)
            {
                float sqr = (desk.position - seatPos).sqrMagnitude;
                if (sqr < minSqr)
                {
                    minSqr = sqr;
                    nearest = desk;
                }
            }

            if (nearest != null) studentDeskCache[student] = nearest;
            return nearest;
        }

        /// <summary>
        /// Get the world Y of the desk top surface (uses Collider.bounds if available).
        /// Falls back to desk.position.y + 0.75f if no collider.
        /// </summary>
        public static float GetDeskTopY(Transform desk)
        {
            Collider col = desk.GetComponent<Collider>();
            if (col != null) return col.bounds.max.y;
            return desk.position.y + 0.75f;
        }

        // ------------------------------------------------------------------
        // Prefab loading
        // ------------------------------------------------------------------

        private GameObject LoadPrefab(string objectType, bool isLarge, bool randomize)
        {
            Dictionary<string, string[]> lookup = isLarge ? LargeTypeToIds : SmallTypeToIds;

            if (!lookup.TryGetValue(objectType, out string[] ids) || ids.Length == 0)
                return null;

            string id = randomize ? ids[Random.Range(0, ids.Length)] : ids[0];
            string folder = isLarge ? THROWABLES_LARGE_PATH : THROWABLES_SMALL_PATH;
            string key = folder + "Throwable_" + id;

            if (prefabCache.TryGetValue(key, out GameObject cached))
                return cached;

            GameObject prefab = Resources.Load<GameObject>(key);
            if (prefab != null)
                prefabCache[key] = prefab;

            return prefab;
        }
    }
}

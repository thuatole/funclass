using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    public enum MessType
    {
        None,         // No mess spawned
        TornPaper,
        BrokenGlass,
        Spill,
        Vomit,
        Stain,
        Trash
    }

    public class MessSpawner : MonoBehaviour
    {
        public static MessSpawner Instance { get; private set; }

        [Header("Mess Prefabs")]
        [Tooltip("Assign prefabs from Assets/Prefabs/AutoLevel_01/Mess/ or QuickLevel")]
        public GameObject tornPaperPrefab;
        public GameObject brokenGlassPrefab;
        public GameObject spillPrefab;
        public GameObject vomitPrefab;
        public GameObject stainPrefab;
        public GameObject trashPrefab;

        [Header("Fallback")]
        [Tooltip("Used when specific prefab is unassigned")]
        public GameObject defaultMessPrefab;

        private readonly List<GameObject> spawnedMess = new List<GameObject>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            AutoLoadPrefabs();
        }

        private void AutoLoadPrefabs()
        {
            tornPaperPrefab   ??= Resources.Load<GameObject>("Mess/TornPaperMess");
            brokenGlassPrefab ??= Resources.Load<GameObject>("Mess/BrokenGlassMess");
            spillPrefab       ??= Resources.Load<GameObject>("Mess/SpillMess");
            vomitPrefab       ??= Resources.Load<GameObject>("Mess/VomitMess");
            stainPrefab       ??= Resources.Load<GameObject>("Mess/StainMess");
            trashPrefab       ??= Resources.Load<GameObject>("Mess/TrashMess");

            // Any loaded prefab works as fallback
            defaultMessPrefab ??= tornPaperPrefab ?? brokenGlassPrefab ?? spillPrefab;

            int loaded = (tornPaperPrefab != null ? 1 : 0) + (brokenGlassPrefab != null ? 1 : 0)
                       + (spillPrefab != null ? 1 : 0)    + (vomitPrefab != null ? 1 : 0)
                       + (stainPrefab != null ? 1 : 0)    + (trashPrefab != null ? 1 : 0);
            GameLogger.Detail("MessSpawner", $"Auto-loaded {loaded}/6 mess prefabs from Resources/Mess/");
        }

        public void SpawnAt(Vector3 worldPosition, MessType type)
        {
            if (type == MessType.None) return;  // explicit "no mess" marker

            GameObject prefab = GetPrefab(type);
            if (prefab == null)
            {
                GameLogger.Warning("MessSpawner", $"No prefab for {type} and no defaultMessPrefab assigned");
                return;
            }

            // Snap to floor level
            Vector3 spawnPos = worldPosition;
            spawnPos.y = 0f;

            float randomAngle = Random.Range(0f, 360f);
            GameObject mess = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, randomAngle, 0f));
            mess.name = $"Mess_{type}_{spawnedMess.Count}";
            spawnedMess.Add(mess);

            GameLogger.Detail("MessSpawner", $"Spawned {type} at {spawnPos}");
        }

        public void ClearAll()
        {
            foreach (var mess in spawnedMess)
            {
                if (mess != null) Destroy(mess);
            }
            spawnedMess.Clear();
            GameLogger.Detail("MessSpawner", "All mess cleared");
        }

        private GameObject GetPrefab(MessType type) => type switch
        {
            MessType.TornPaper   => tornPaperPrefab   ?? defaultMessPrefab,
            MessType.BrokenGlass => brokenGlassPrefab ?? defaultMessPrefab,
            MessType.Spill       => spillPrefab        ?? defaultMessPrefab,
            MessType.Vomit       => vomitPrefab        ?? defaultMessPrefab,
            MessType.Stain       => stainPrefab        ?? defaultMessPrefab,
            MessType.Trash       => trashPrefab        ?? defaultMessPrefab,
            _                    => defaultMessPrefab
        };
    }
}

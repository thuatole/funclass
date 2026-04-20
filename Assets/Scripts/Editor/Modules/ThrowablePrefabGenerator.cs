using UnityEditor;
using UnityEngine;
using System.IO;
using FunClass.Core;

namespace FunClass.Editor.Modules
{
    public class ThrowablePrefabGenerator : EditorWindow
    {
        private const string OUTPUT_BASE = "Assets/Resources/Throwables";

        private static readonly ThrowableDefinition[] SmallObjects = new[]
        {
            new ThrowableDefinition("book1",    "Assets/school/Prefabs/props/book1.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book2",    "Assets/school/Prefabs/props/book2.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book3",    "Assets/school/Prefabs/props/book3.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book4",    "Assets/school/Prefabs/props/book4.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book5",    "Assets/school/Prefabs/props/book5.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book6",    "Assets/school/Prefabs/props/book6.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book7",    "Assets/school/Prefabs/props/book7.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book8",    "Assets/school/Prefabs/props/book8.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book9",    "Assets/school/Prefabs/props/book9.prefab",    "sách",      SizeCategory.Small),
            new ThrowableDefinition("book10",   "Assets/school/Prefabs/props/book10.prefab",   "sách",      SizeCategory.Small),
            new ThrowableDefinition("book11",   "Assets/school/Prefabs/props/book11.prefab",   "sách",      SizeCategory.Small),
            new ThrowableDefinition("book12",   "Assets/school/Prefabs/props/book12.prefab",   "sách",      SizeCategory.Small),
            new ThrowableDefinition("book13",   "Assets/school/Prefabs/props/book13.prefab",   "sách",      SizeCategory.Small),
            new ThrowableDefinition("book14",   "Assets/school/Prefabs/props/book14.prefab",   "sách",      SizeCategory.Small),
            new ThrowableDefinition("book15",   "Assets/school/Prefabs/props/book15.prefab",   "sách",      SizeCategory.Small),
            new ThrowableDefinition("book16",   "Assets/school/Prefabs/props/book16.prefab",   "sách",      SizeCategory.Small),
            new ThrowableDefinition("sheet",    "Assets/school/Prefabs/props/sheet.prefab",    "tờ giấy",   SizeCategory.Small),
            new ThrowableDefinition("sheet1",   "Assets/school/Prefabs/props/sheet1.prefab",   "tờ giấy",   SizeCategory.Small),
            new ThrowableDefinition("sheet2",   "Assets/school/Prefabs/props/sheet2.prefab",   "tờ giấy",   SizeCategory.Small),
            new ThrowableDefinition("sheet3",   "Assets/school/Prefabs/props/sheet3.prefab",   "tờ giấy",   SizeCategory.Small),
            new ThrowableDefinition("sheet4",   "Assets/school/Prefabs/props/sheet4.prefab",   "tờ giấy",   SizeCategory.Small),
            new ThrowableDefinition("tray",     "Assets/school/Prefabs/props/tray.prefab",     "khay",      SizeCategory.Small),
        };

        private static readonly ThrowableDefinition[] LargeObjects = new[]
        {
            new ThrowableDefinition("laptop",      "Assets/school/Prefabs/props/a laptop.prefab",   "laptop",     SizeCategory.Large),
            new ThrowableDefinition("computer",    "Assets/school/Prefabs/props/computer.prefab",   "máy tính",   SizeCategory.Large),
            new ThrowableDefinition("computer1",   "Assets/school/Prefabs/props/computer1.prefab",  "máy tính",   SizeCategory.Large),
            new ThrowableDefinition("computer2",   "Assets/school/Prefabs/props/computer2.prefab",  "máy tính",   SizeCategory.Large),
            new ThrowableDefinition("computer3",   "Assets/school/Prefabs/props/computer3.prefab",  "máy tính",   SizeCategory.Large),
            new ThrowableDefinition("speaker",     "Assets/school/Prefabs/props/speaker.prefab",    "loa",        SizeCategory.Large),
            new ThrowableDefinition("projector",   "Assets/school/Prefabs/props/projector.prefab",  "máy chiếu",  SizeCategory.Large),
            new ThrowableDefinition("chair",       "Assets/school/Prefabs/props/chair.prefab",      "ghế",        SizeCategory.Large),
            new ThrowableDefinition("chair1",      "Assets/school/Prefabs/props/chair1.prefab",     "ghế",        SizeCategory.Large),
        };

        [MenuItem("FunClass/Generate Throwable Prefabs")]
        public static void GenerateAll()
        {
            EnsureFolder(OUTPUT_BASE);
            EnsureFolder($"{OUTPUT_BASE}/Small");
            EnsureFolder($"{OUTPUT_BASE}/Large");

            int created = 0;
            int skipped = 0;

            foreach (var def in SmallObjects)
                ProcessDefinition(def, $"{OUTPUT_BASE}/Small", ref created, ref skipped);

            foreach (var def in LargeObjects)
                ProcessDefinition(def, $"{OUTPUT_BASE}/Large", ref created, ref skipped);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ThrowablePrefabGenerator] Done — {created} created, {skipped} skipped (already exist or source missing)");
            EditorUtility.DisplayDialog("Throwable Generator", $"Done!\n{created} prefabs created\n{skipped} skipped", "OK");
        }

        private static void ProcessDefinition(ThrowableDefinition def, string outputFolder, ref int created, ref int skipped)
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "..", def.sourcePath)))
            {
                Debug.LogWarning($"[ThrowablePrefabGenerator] Source not found: {def.sourcePath}");
                skipped++;
                return;
            }

            string destPath = $"{outputFolder}/Throwable_{def.id}.prefab";

            if (File.Exists(Path.Combine(Application.dataPath, "..", destPath)))
            {
                // Prefab already exists — update component fields only
                GameObject existing = PrefabUtility.LoadPrefabContents(destPath);
                if (existing != null)
                {
                    ApplyComponentSettings(existing, def);
                    PrefabUtility.SaveAsPrefabAsset(existing, destPath);
                    PrefabUtility.UnloadPrefabContents(existing);
                }
                skipped++;
                return;
            }

            // Duplicate source prefab
            if (!AssetDatabase.CopyAsset(def.sourcePath, destPath))
            {
                Debug.LogError($"[ThrowablePrefabGenerator] Failed to copy {def.sourcePath} → {destPath}");
                skipped++;
                return;
            }

            // Load, configure, save
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(destPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[ThrowablePrefabGenerator] Failed to load prefab contents at {destPath}");
                skipped++;
                return;
            }

            // Reset local position baked into source prefab
            prefabRoot.transform.localPosition = Vector3.zero;
            prefabRoot.transform.localRotation = Quaternion.identity;

            ApplyComponentSettings(prefabRoot, def);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, destPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            created++;
            Debug.Log($"[ThrowablePrefabGenerator] Created {destPath}");
        }

        private static void ApplyComponentSettings(GameObject go, ThrowableDefinition def)
        {
            StudentInteractableObject interactable = go.GetComponent<StudentInteractableObject>();
            if (interactable == null)
                interactable = go.AddComponent<StudentInteractableObject>();

            interactable.objectName = def.id;
            interactable.displayName = def.displayName;
            interactable.sizeCategory = def.sizeCategory;
            interactable.canBeThrown = true;
            interactable.canBeKnockedOver = def.sizeCategory == SizeCategory.Large;
            interactable.canMakeNoise = def.sizeCategory == SizeCategory.Large;
            interactable.canBeDropped = def.sizeCategory == SizeCategory.Small;
            // Large knock → kính/đồ vỡ, Small knock (hiếm) → giấy vò
            interactable.knockMessType = def.sizeCategory == SizeCategory.Large
                ? MessType.BrokenGlass
                : MessType.TornPaper;

            // Ensure trigger collider for interaction detection
            Collider col = go.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private struct ThrowableDefinition
        {
            public string id;
            public string sourcePath;
            public string displayName;
            public SizeCategory sizeCategory;

            public ThrowableDefinition(string id, string sourcePath, string displayName, SizeCategory sizeCategory)
            {
                this.id = id;
                this.sourcePath = sourcePath;
                this.displayName = displayName;
                this.sizeCategory = sizeCategory;
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    [CreateAssetMenu(fileName = "DeskLoadout", menuName = "FunClass/Desk Loadout Config")]
    public class DeskLoadoutConfig : ScriptableObject
    {
        [Range(1, 6)]
        public int perStudentMin = 2;
        [Range(1, 6)]
        public int perStudentMax = 3;

        [Tooltip("objectType keys matching throwable prefab ids: Book, Sheet, Tray")]
        public List<string> smallObjectPool = new List<string> { "Book", "Sheet" };

        [Range(0f, 1f)]
        public float largeObjectChancePerDesk = 0.2f;

        [Tooltip("objectType keys for large objects: Laptop, Computer, Speaker, Projector")]
        public List<string> largeObjectPool = new List<string> { "Laptop" };

        public bool randomizeVariants = true;
    }

    [CreateAssetMenu(fileName = "ClassroomProps", menuName = "FunClass/Classroom Props Config")]
    public class ClassroomPropsConfig : ScriptableObject
    {
        [Tooltip("Shared interactable props placed around classroom: Speaker, Projector, Computer")]
        public List<string> sharedProps = new List<string> { "Speaker", "Projector" };

        [Range(0, 8)]
        public int extraDecoCount = 4;

        public bool autoPlace = true;
    }
}

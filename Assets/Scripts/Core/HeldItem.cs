using UnityEngine;

namespace FunClass.Core
{
    public class HeldItem : MonoBehaviour
    {
        public string itemName;
        [TextArea(2, 3)]
        public string itemDescription;
    }
}

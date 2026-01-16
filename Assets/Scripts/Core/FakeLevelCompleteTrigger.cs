using UnityEngine;

namespace FunClass.Core
{
    public class FakeLevelCompleteTrigger : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CompleteLevel();
            }
        }

        public void CompleteLevel()
        {
            if (GameStateManager.Instance != null)
            {
                Debug.Log("[FakeLevelCompleteTrigger] Completing level...");
                GameStateManager.Instance.CompleteLevel();
            }
        }
    }
}

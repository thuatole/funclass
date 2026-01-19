using UnityEngine;

namespace FunClass.Core.UI
{
    public class PopupAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [SerializeField] private float scaleInDuration = 0.3f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Billboard Settings")]
        [SerializeField] private bool enableBillboard = false;
        [SerializeField] private Camera targetCamera;

        private CanvasGroup canvasGroup;
        private Vector3 targetScale = Vector3.one;
        private float animationTime = 0f;
        private bool isAnimatingIn = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            PlayFadeIn();
            PlayScaleIn();
        }

        private void Update()
        {
            if (isAnimatingIn)
            {
                UpdateScaleAnimation();
            }
        }
        
        private void LateUpdate()
        {
            if (enableBillboard && targetCamera != null)
            {
                UpdateBillboard();
            }
        }

        private void UpdateBillboard()
        {
            if (transform.parent != null)
            {
                Vector3 directionToCamera = targetCamera.transform.position - transform.parent.position;
                directionToCamera.y = 0;
                
                if (directionToCamera != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                    transform.parent.rotation = targetRotation;
                }
            }
        }

        private void PlayFadeIn()
        {
            if (canvasGroup != null)
            {
                StartCoroutine(FadeInCoroutine());
            }
        }

        private System.Collections.IEnumerator FadeInCoroutine()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * t);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private void PlayScaleIn()
        {
            isAnimatingIn = true;
            animationTime = 0f;
            transform.localScale = Vector3.zero;
        }

        private void UpdateScaleAnimation()
        {
            animationTime += Time.deltaTime;
            float progress = Mathf.Clamp01(animationTime / scaleInDuration);
            float curveValue = scaleCurve.Evaluate(progress);
            
            transform.localScale = targetScale * curveValue;

            if (progress >= 1f)
            {
                isAnimatingIn = false;
            }
        }

        public void PlayFadeOut(System.Action onComplete = null)
        {
            if (canvasGroup != null)
            {
                StartCoroutine(FadeOutCoroutine(onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private System.Collections.IEnumerator FadeOutCoroutine(System.Action onComplete)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t * t);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            onComplete?.Invoke();
        }

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
        }
    }
}

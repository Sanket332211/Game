using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Controls the Paytable & Rules popup modal dialog.
    /// </summary>
    public class PaytableModal : MonoBehaviour
    {
        [Header("Modal Components")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.2f;

        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (canvasGroup == null && modalRoot != null)
            {
                canvasGroup = modalRoot.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = modalRoot.AddComponent<CanvasGroup>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            HideInstant();
        }

        public void Show()
        {
            if (modalRoot == null) return;
            modalRoot.SetActive(true);
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(0f, 1f));
        }

        public void Hide()
        {
            if (modalRoot == null) return;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, () => modalRoot.SetActive(false)));
        }

        public void HideInstant()
        {
            if (modalRoot == null) return;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            modalRoot.SetActive(false);
        }

        private IEnumerator FadeRoutine(float from, float to, System.Action onComplete = null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                }
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = to;
            onComplete?.Invoke();
        }
    }
}


using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Displays animated celebration banners for standard wins, jackpots,
    /// and free spins triggers with dynamic credit count-up.
    /// </summary>
    public class WinPopupController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text payoutAmountText;
        [SerializeField] private Button continueButton;

        [Header("Animation Settings")]
        [SerializeField] private float countUpDuration = 1.0f;
        [SerializeField] private float autoDismissDelay = 2.5f;

        private Coroutine _displayRoutine;
        private Action _onDismissCallback;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(Dismiss);
            }
            HideInstant();
        }

        public void ShowWin(string title, string description, int payout, bool isJackpot, Action onDismiss = null)
        {
            _onDismissCallback = onDismiss;
            if (popupRoot == null) return;

            popupRoot.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;

            if (_displayRoutine != null) StopCoroutine(_displayRoutine);
            _displayRoutine = StartCoroutine(AnimateWinRoutine(payout, isJackpot));
        }

        private IEnumerator AnimateWinRoutine(int targetPayout, bool isJackpot)
        {
            // Fade In
            float fadeElapsed = 0f;
            while (fadeElapsed < 0.2f)
            {
                fadeElapsed += Time.deltaTime;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(fadeElapsed / 0.2f);
                yield return null;
            }

            // Count Up Number Effect
            float countElapsed = 0f;
            float dur = isJackpot ? countUpDuration * 1.5f : countUpDuration;

            while (countElapsed < dur)
            {
                countElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(countElapsed / dur);
                int currentVal = Mathf.RoundToInt(Mathf.Lerp(0, targetPayout, progress));

                if (payoutAmountText != null)
                {
                    payoutAmountText.text = $"+{currentVal:N0} CREDITS";
                }
                yield return null;
            }

            if (payoutAmountText != null)
            {
                payoutAmountText.text = $"+{targetPayout:N0} CREDITS";
            }

            // Wait before auto dismiss if not dismissed manually
            yield return new WaitForSeconds(isJackpot ? autoDismissDelay * 1.5f : autoDismissDelay);
            Dismiss();
        }

        public void Dismiss()
        {
            if (popupRoot == null || !popupRoot.activeSelf) return;

            if (_displayRoutine != null) StopCoroutine(_displayRoutine);
            StartCoroutine(FadeOutRoutine(() =>
            {
                popupRoot.SetActive(false);
                _onDismissCallback?.Invoke();
                _onDismissCallback = null;
            }));
        }

        public void HideInstant()
        {
            if (popupRoot == null) return;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            popupRoot.SetActive(false);
        }

        private IEnumerator FadeOutRoutine(Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.15f);
                yield return null;
            }
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            onComplete?.Invoke();
        }
    }
}


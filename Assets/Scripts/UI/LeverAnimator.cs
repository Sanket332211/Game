using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Animates the classic slot machine pull-lever using sprite frame sequences
    /// (slot-machine1.png through slot-machine5.png).
    /// </summary>
    public class LeverAnimator : MonoBehaviour
    {
        [Header("Lever Sprites (Sequential Frames)")]
        [Tooltip("Array containing the 5 sequential lever frame sprites.")]
        [SerializeField] private Sprite[] leverFrames;

        [Header("Target Image")]
        [SerializeField] private Image leverImage;

        [Header("Animation Settings")]
        [Tooltip("Time between frames during the down-stroke.")]
        [SerializeField] private float frameDurationDown = 0.05f;

        [Tooltip("Time between frames during the return spring-back.")]
        [SerializeField] private float frameDurationUp = 0.04f;

        private Coroutine _pullRoutine;
        private bool _isPulling = false;

        public bool IsPulling => _isPulling;

        private void Awake()
        {
            if (leverImage == null) leverImage = GetComponent<Image>();
            ResetToIdle();
        }

        public void PlayLeverPull(Action onBottomReached = null, Action onComplete = null)
        {
            if (_isPulling || leverFrames == null || leverFrames.Length < 5) return;

            if (_pullRoutine != null) StopCoroutine(_pullRoutine);
            _pullRoutine = StartCoroutine(PullSequence(onBottomReached, onComplete));
        }

        private IEnumerator PullSequence(Action onBottomReached, Action onComplete)
        {
            _isPulling = true;

            // Frame 1 -> Frame 2 -> Frame 3 -> Frame 4 (Pull Down)
            for (int i = 0; i < 4; i++)
            {
                if (i < leverFrames.Length && leverFrames[i] != null)
                {
                    leverImage.sprite = leverFrames[i];
                }
                yield return new WaitForSeconds(frameDurationDown);
            }

            // Bottom reached - trigger reel release!
            onBottomReached?.Invoke();

            // Frame 5 -> Frame 1 (Return Spring-back)
            if (leverFrames.Length > 4 && leverFrames[4] != null)
            {
                leverImage.sprite = leverFrames[4];
            }
            yield return new WaitForSeconds(frameDurationUp);

            ResetToIdle();
            _isPulling = false;
            onComplete?.Invoke();
        }

        public void ResetToIdle()
        {
            if (leverFrames != null && leverFrames.Length > 0 && leverFrames[0] != null)
            {
                leverImage.sprite = leverFrames[0];
            }
        }
    }
}

